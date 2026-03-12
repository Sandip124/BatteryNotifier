using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Providers;
using BatteryNotifier.Core.Store;
using Serilog;

namespace BatteryNotifier.Core.Services;

public sealed class BatteryMonitorService : IDisposable
{
    private static readonly Lazy<BatteryMonitorService> _instance =
        new Lazy<BatteryMonitorService>(() => new BatteryMonitorService());

    public static BatteryMonitorService Instance => _instance.Value;

    private BatteryInfo? _lastPowerStatus = null;
    private int _lowBatteryThreshold = 25;
    private int _fullBatteryThreshold = 96;

    private const int BatteryLevelCheckThresholdMs = 120000;

    public event EventHandler<BatteryStatusEventArgs>? BatteryStatusChanged;
    public event EventHandler<BatteryStatusEventArgs>? PowerLineStatusChanged;

    private ILogger _logger;
    private CancellationTokenSource? _cts;
    private IDisposable? _powerEventWatcher;
    private bool _disposed;

    private BatteryMonitorService()
    {
        _logger = BatteryNotifierAppLogger.ForContext<BatteryMonitorService>();

        // Load thresholds from settings
        var settings = AppSettings.Instance;
        _lowBatteryThreshold = settings.LowBatteryNotificationValue;
        _fullBatteryThreshold = settings.FullBatteryNotificationValue;

        // Synchronous initial check so BatteryManagerStore is populated before UI reads it
        CheckBatteryAndPowerStatus(forceCheck: false, publishNotifications: false);

        if (OperatingSystem.IsWindows())
        {
            InitializeWmiWatcher();
        }

        _cts = new CancellationTokenSource();
        _ = RunBatteryLevelMonitorAsync(_cts.Token);
    }

    private void InitializeWmiWatcher()
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            // Use reflection to avoid compile-time dependency on System.Management
            var queryType = Type.GetType("System.Management.WqlEventQuery, System.Management");
            var watcherType = Type.GetType("System.Management.ManagementEventWatcher, System.Management");

            if (queryType != null && watcherType != null)
            {
                var query = Activator.CreateInstance(queryType, "SELECT * FROM Win32_PowerManagementEvent");
                var watcher = Activator.CreateInstance(watcherType, query);

                if (watcher != null)
                {
                    var eventInfo = watcherType.GetEvent("EventArrived");
                    if (eventInfo != null)
                    {
                        var handler = Delegate.CreateDelegate(
                            eventInfo.EventHandlerType!,
                            this,
                            typeof(BatteryMonitorService).GetMethod(nameof(OnWmiPowerEventReflection),
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);
                        eventInfo.AddEventHandler(watcher, handler);
                    }

                    watcherType.GetMethod("Start")?.Invoke(watcher, null);
                    _powerEventWatcher = watcher as IDisposable;
                }

                _logger.Information("WMI Power event watcher initialized.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize WMI Power event watcher.");
        }
    }

    private void OnWmiPowerEventReflection(object sender, EventArgs e)
    {
        _logger.Information($"WMI Power event detected at [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
        CheckBatteryAndPowerStatus(forceCheck: true);
    }

    private async Task RunBatteryLevelMonitorAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(BatteryLevelCheckThresholdMs));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                CheckBatteryAndPowerStatus();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private void CheckBatteryAndPowerStatus(bool forceCheck = false, bool publishNotifications = true)
    {
        if (_disposed) return;

        var currentStatus = BatteryInfoProvider.Instance.GetBatteryInfo();

        if (currentStatus.BatteryChargeStatus is BatteryChargeStatus.NoSystemBattery or BatteryChargeStatus.Unknown)
            return;

        var currentLevel = (int)(currentStatus.BatteryLifePercent * 100);
        var lastLevel = _lastPowerStatus != null ? (int)(_lastPowerStatus.BatteryLifePercent * 100) : 0;

        bool powerLineChanged = _lastPowerStatus?.PowerLineStatus != currentStatus.PowerLineStatus;

        bool batteryLevelChanged = Math.Abs(currentLevel - lastLevel) >= 5;

        bool isLowBattery = currentLevel <= _lowBatteryThreshold &&
                            currentStatus.BatteryChargeStatus != BatteryChargeStatus.Charging;

        bool isFullBattery = currentLevel >= _fullBatteryThreshold &&
                             (currentStatus.BatteryChargeStatus == BatteryChargeStatus.Charging ||
                              currentStatus.BatteryChargeStatus == BatteryChargeStatus.High ||
                              currentStatus.PowerLineStatus == BatteryPowerLineStatus.Online);

        bool shouldNotify = forceCheck || powerLineChanged || batteryLevelChanged || isLowBattery || isFullBattery ||
                            _lastPowerStatus == null;

        UpdateBatteryManagerStore(currentStatus, currentLevel);

        if (shouldNotify)
        {
            _logger.Information($@"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Invoked by: {(forceCheck ? "Force Check" : "Periodic Timer")}, Battery Level: {currentLevel}%, Power Line Status: {currentStatus.PowerLineStatus}");

            if (powerLineChanged && _lastPowerStatus != null)
            {
                PowerLineStatusChanged?.Invoke(this, CreateBatteryEventArgs(currentStatus));
            }

            BatteryStatusChanged?.Invoke(this, CreateBatteryEventArgs(currentStatus));
            _lastPowerStatus = currentStatus;

            // Reset notification trackers on power state change so notifications fire eagerly
            if (powerLineChanged && _lastPowerStatus != null)
            {
                NotificationService.Instance.ResetAllTrackers();
                _logger.Information("Power line state changed — notification trackers reset");
            }

            // Publish notifications when thresholds are crossed
            if (publishNotifications)
            {
                // On macOS with an external display, the charger must stay connected,
                // so battery notifications are not useful.
                if (ShouldSuppressNotifications(currentStatus))
                {
                    _logger.Information("Suppressing battery notification — macOS external display detected while on AC power.");
                }
                else
                {
                    var settings = AppSettings.Instance;

                    if (isLowBattery && settings.LowBatteryNotification)
                    {
                        var message = GetLowBatteryMessage(currentLevel);
                        NotificationService.Instance.PublishNotification(
                            message,
                            NotificationType.Global,
                            Constants.DefaultNotificationTimeout,
                            Constants.LowBatteryTag);
                    }

                    if (isFullBattery && settings.FullBatteryNotification)
                    {
                        var message = GetFullBatteryMessage(currentLevel);
                        NotificationService.Instance.PublishNotification(
                            message,
                            NotificationType.Global,
                            Constants.DefaultNotificationTimeout,
                            Constants.FullBatteryTag);
                    }
                }
            }
        }
    }

    private static void UpdateBatteryManagerStore(BatteryInfo currentStatus, int currentLevel)
    {
        bool isPluggedIn = currentStatus.PowerLineStatus == BatteryPowerLineStatus.Online &&
                           currentStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery;
        bool isActivelyCharging = currentStatus.BatteryChargeStatus == BatteryChargeStatus.Charging;
        BatteryManagerStore.Instance.SetChargingState(isActivelyCharging, isPluggedIn);
        BatteryManagerStore.Instance.SetBatteryState(currentLevel);
        BatteryManagerStore.Instance.SetBatteryLife(currentStatus.BatteryLifeRemaining);
        BatteryManagerStore.Instance.SetBatteryLifePercentage(Math.Round(currentStatus.BatteryLifePercent * 100, 0));
        BatteryManagerStore.Instance.SetHasNoBattery(
            currentStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery);
        BatteryManagerStore.Instance.SetIsUnknown(
            currentStatus.BatteryChargeStatus == BatteryChargeStatus.Unknown);
    }

    private BatteryStatusEventArgs CreateBatteryEventArgs(BatteryInfo status)
    {
        var level = (int)(status.BatteryLifePercent * 100);
        return new BatteryStatusEventArgs
        {
            BatteryLevel = level,
            IsCharging = status.PowerLineStatus == BatteryPowerLineStatus.Online,
            IsLowBattery = level <= _lowBatteryThreshold,
            IsFullBattery = level >= _fullBatteryThreshold,
            PowerLineStatus = status.PowerLineStatus,
            BatteryChargeStatus = status.BatteryChargeStatus,
            BatteryLifeRemaining = status.BatteryLifeRemaining
        };
    }

    private static bool ShouldSuppressNotifications(BatteryInfo status)
    {
        // On macOS, when an external display is connected the charger must remain
        // plugged in. Battery notifications are noise in this scenario.
        if (!OperatingSystem.IsMacOS()) return false;
        if (status.PowerLineStatus != BatteryPowerLineStatus.Online) return false;

        return HasExternalDisplay();
    }

    private static bool HasExternalDisplay()
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "system_profiler",
                Arguments = "SPDisplaysDataType",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            // Count "Resolution:" lines — each physical display has one
            int displayCount = 0;
            foreach (var line in output.Split('\n'))
            {
                if (line.TrimStart().StartsWith("Resolution:", StringComparison.OrdinalIgnoreCase))
                    displayCount++;
            }

            return displayCount > 1;
        }
        catch
        {
            return false;
        }
    }

    private static string GetLowBatteryMessage(int level)
    {
        var escalation = NotificationService.Instance.GetEscalationCount(Constants.LowBatteryTag);
        return NotificationTemplates.GetLowBatteryMessage(level, escalation);
    }

    private static string GetFullBatteryMessage(int level)
    {
        var escalation = NotificationService.Instance.GetEscalationCount(Constants.FullBatteryTag);
        return NotificationTemplates.GetFullBatteryMessage(level, escalation);
    }

    public void SetThresholds(int lowThreshold, int fullThreshold)
    {
        _lowBatteryThreshold = lowThreshold;
        _fullBatteryThreshold = fullThreshold;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _powerEventWatcher?.Dispose();
        _powerEventWatcher = null;

        BatteryStatusChanged = null;
        PowerLineStatusChanged = null;

        _disposed = true;
    }
}

public class BatteryStatusEventArgs : EventArgs
{
    public int BatteryLevel { get; set; }
    public bool IsCharging { get; set; }
    public bool IsLowBattery { get; set; }
    public bool IsFullBattery { get; set; }
    public BatteryPowerLineStatus PowerLineStatus { get; set; }
    public BatteryChargeStatus BatteryChargeStatus { get; set; }
    public int BatteryLifeRemaining { get; set; }
}
