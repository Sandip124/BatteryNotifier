using System.Runtime.InteropServices;
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

    private readonly object _statusLock = new();
    private BatteryInfo? _lastPowerStatus;
    private volatile int _lowBatteryThreshold = 25;
    private volatile int _fullBatteryThreshold = 96;

    // macOS: Darwin notify covers plug/unplug AND level changes — poll infrequently as safety net.
    // Windows: WMI covers plug/unplug only — poll every 60s for level changes (reduced to 5s if WMI fails).
    // Linux: no event API — poll every 5s.
    private static readonly int DefaultPollMs =
        OperatingSystem.IsMacOS() ? 120_000   // 2 min safety net (Darwin notify is primary)
        : OperatingSystem.IsLinux() ? 5_000   // no events, must poll
        : 60_000;                             // Windows: WMI handles plug/unplug, poll for % changes

    private int _actualPollMs = DefaultPollMs;

    public event EventHandler<BatteryStatusEventArgs>? BatteryStatusChanged;
    public event EventHandler<BatteryStatusEventArgs>? PowerLineStatusChanged;

    private readonly ILogger _logger;
    private CancellationTokenSource? _cts;
    private IDisposable? _powerEventWatcher;
    private int _macNotifyToken = -1;
    private int _macNotifyFd = -1;
    private Thread? _macNotifyThread;
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
        else if (OperatingSystem.IsMacOS())
        {
            InitializeMacPowerWatcher();
        }

        _cts = new CancellationTokenSource();
        _ = RunBatteryLevelMonitorAsync(_cts.Token);
    }

    private void InitializeWmiWatcher()
    {
#if WINDOWS
        try
        {
            var query = new System.Management.WqlEventQuery("SELECT * FROM Win32_PowerManagementEvent");
            var watcher = new System.Management.ManagementEventWatcher(query);
            watcher.EventArrived += OnWmiPowerEvent;
            watcher.Start();
            _powerEventWatcher = watcher;
            return;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize WMI Power event watcher. Falling back to faster polling.");
        }
#endif
        // WMI not available (not compiled, trimmed, or failed) — poll aggressively for both events and level
        _actualPollMs = 5_000;
        _logger.Information("WMI unavailable, using {Interval}ms polling fallback", _actualPollMs);
    }

#if WINDOWS
    private void OnWmiPowerEvent(object sender, System.Management.EventArrivedEventArgs e)
    {
        CheckBatteryAndPowerStatus(forceCheck: true);
    }
#endif

    // ── macOS: Darwin notify API for instant power source changes ──

    [DllImport("libSystem.dylib")]
    private static extern int notify_register_file_descriptor(
        string name, ref int notify_fd, int flags, out int out_token);

    [DllImport("libSystem.dylib")]
    private static extern int notify_cancel(int token);

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern nint read(int fd, byte[] buf, nint count);

    [DllImport("libSystem.dylib")]
    private static extern int close(int fd);

    private void InitializeMacPowerWatcher()
    {
        try
        {
            int fd = -1;
            var status = notify_register_file_descriptor(
                "com.apple.system.powersources.timeremaining",
                ref fd, 0, out var token);

            if (status != 0)
            {
                _logger.Warning("Failed to register Darwin power notify (status={Status}). " +
                                "Falling back to polling.", status);
                _actualPollMs = 5_000;
                _logger.Information("Darwin notify unavailable, using {Interval}ms polling fallback", _actualPollMs);
                return;
            }

            _macNotifyFd = fd;
            _macNotifyToken = token;

            _macNotifyThread = new Thread(MacPowerNotifyLoop)
            {
                Name = "MacPowerNotify",
                IsBackground = true
            };
            _macNotifyThread.Start();

        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize macOS power source watcher.");
        }
    }

    private void MacPowerNotifyLoop()
    {
        // Each notification delivers a 4-byte int (the token) on the fd.
        var buf = new byte[4];
        while (!_disposed)
        {
            // Blocks until a power source change notification arrives
            var bytesRead = read(_macNotifyFd, buf, 4);
            if (bytesRead <= 0 || _disposed) break;

            CheckBatteryAndPowerStatus(forceCheck: true);
        }
    }

    private async Task RunBatteryLevelMonitorAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_actualPollMs));
        try
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
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

        var currentStatus = BatteryInfoProvider.GetBatteryInfo();

        if (currentStatus.BatteryChargeStatus is BatteryChargeStatus.NoSystemBattery or BatteryChargeStatus.Unknown)
            return;

        var currentLevel = (int)(currentStatus.BatteryLifePercent * 100);

        bool powerLineChanged;
        bool levelChanged;

        lock (_statusLock)
        {
            var lastLevel = _lastPowerStatus != null ? (int)(_lastPowerStatus.BatteryLifePercent * 100) : 0;
            powerLineChanged = _lastPowerStatus?.PowerLineStatus != currentStatus.PowerLineStatus;
            levelChanged = currentLevel != lastLevel;
        }

        bool isLowBattery = currentLevel <= _lowBatteryThreshold &&
                            currentStatus.BatteryChargeStatus != BatteryChargeStatus.Charging;

        // Full battery notification only when charger is connected (plugged in).
        // Unplugging while above threshold should NOT trigger a notification.
        bool isPluggedIn = currentStatus.PowerLineStatus == BatteryPowerLineStatus.Online;
        bool isFullBattery = currentLevel >= _fullBatteryThreshold && isPluggedIn;

        // Always update the store so it reflects the latest battery state
        UpdateBatteryManagerStore(currentStatus, currentLevel);

        // Always fire UI update events when anything changed (level, power line, or first check)
        bool anythingChanged;
        bool wasAlreadyTracking;
        lock (_statusLock)
        {
            wasAlreadyTracking = _lastPowerStatus != null;
            anythingChanged = forceCheck || powerLineChanged || levelChanged || !wasAlreadyTracking;
        }

        if (anythingChanged)
        {
            if (powerLineChanged && wasAlreadyTracking)
            {
                _logger.Information("Power line state changed: {Status} (battery {Level}%)",
                    currentStatus.PowerLineStatus, currentLevel);
                PowerLineStatusChanged?.Invoke(this, CreateBatteryEventArgs(currentStatus));
            }

            if (levelChanged)
            {
                _logger.Information("Battery level changed: {Level}% ({ChargeStatus}, {PowerLine})",
                    currentLevel, currentStatus.BatteryChargeStatus, currentStatus.PowerLineStatus);
            }

            BatteryStatusChanged?.Invoke(this, CreateBatteryEventArgs(currentStatus));

            lock (_statusLock)
            {
                _lastPowerStatus = currentStatus;
            }

            // Reset notification trackers on power state change so notifications fire eagerly
            if (powerLineChanged && wasAlreadyTracking)
            {
                _logger.Information("Resetting notification trackers due to power line change");
                NotificationService.Instance.ResetAllTrackers();
            }
        }

        // Publish threshold notifications only on real state transitions (level or power changed).
        // forceCheck is for UI refresh only — it should not trigger notifications when nothing changed.
        // This prevents spurious "unplug charger" notifications on app startup.
        if (publishNotifications && (powerLineChanged || levelChanged))
        {
            PublishThresholdNotifications(currentStatus, currentLevel, isLowBattery, isFullBattery);
        }
    }

    private void PublishThresholdNotifications(BatteryInfo currentStatus, int currentLevel,
        bool isLowBattery, bool isFullBattery)
    {
        if (!isLowBattery && !isFullBattery) return;

        if (ShouldSuppressNotifications(currentStatus))
        {
            _logger.Information("Notification suppressed: external display detected (charger must stay connected)");
            return;
        }

        var settings = AppSettings.Instance;

        if (isLowBattery)
            PublishIfEnabled(settings.LowBatteryNotification, currentLevel, Constants.LowBatteryTag, GetLowBatteryMessage);

        if (isFullBattery)
            PublishIfEnabled(settings.FullBatteryNotification, currentLevel, Constants.FullBatteryTag, GetFullBatteryMessage);
    }

    private void PublishIfEnabled(bool enabled, int level, string tag, Func<int, string> messageFactory)
    {
        if (!enabled)
        {
            _logger.Information("{Tag} at {Level}% but notification is disabled in settings", tag, level);
            return;
        }

        var message = messageFactory(level);
        _logger.Information("Publishing {Tag} notification at {Level}%: {Message}", tag, level, message);
        NotificationService.Instance.PublishNotification(
            message, NotificationType.Global, Constants.DefaultNotificationTimeout, tag);
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
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "system_profiler",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("SPDisplaysDataType");
            process.StartInfo = psi;
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
                return false;
            }

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

    /// <summary>
    /// Triggers an immediate battery status check (e.g., when the window becomes visible).
    /// </summary>
    public void ForceCheck() => CheckBatteryAndPowerStatus(forceCheck: true);

    public void SetThresholds(int lowThreshold, int fullThreshold)
    {
        _lowBatteryThreshold = lowThreshold;
        _fullBatteryThreshold = fullThreshold;
    }

    /// <summary>
    /// Evaluates what actions should be taken given a battery state transition.
    /// Extracted for testability — the core decision logic of CheckBatteryAndPowerStatus.
    /// </summary>
    internal static BatteryChangeResult EvaluateBatteryChange(
        BatteryInfo? lastStatus, BatteryInfo currentStatus,
        int lowThreshold, int fullThreshold, bool forceCheck)
    {
        var currentLevel = (int)(currentStatus.BatteryLifePercent * 100);
        var lastLevel = lastStatus != null ? (int)(lastStatus.BatteryLifePercent * 100) : 0;

        bool powerLineChanged = lastStatus?.PowerLineStatus != currentStatus.PowerLineStatus;
        bool levelChanged = currentLevel != lastLevel;
        bool wasAlreadyTracking = lastStatus != null;

        bool shouldUpdateUI = forceCheck || powerLineChanged || levelChanged || !wasAlreadyTracking;

        bool isLowBattery = currentLevel <= lowThreshold &&
                            currentStatus.BatteryChargeStatus != BatteryChargeStatus.Charging;

        bool isFullBattery = currentLevel >= fullThreshold &&
                             currentStatus.PowerLineStatus == BatteryPowerLineStatus.Online;

        bool realStateChange = powerLineChanged || levelChanged;

        return new BatteryChangeResult
        {
            ShouldUpdateUI = shouldUpdateUI,
            ShouldFirePowerLineChanged = powerLineChanged && wasAlreadyTracking,
            ShouldPublishNotification = realStateChange && (isLowBattery || isFullBattery),
            IsLowBattery = isLowBattery,
            IsFullBattery = isFullBattery,
            CurrentLevel = currentLevel
        };
    }

    internal struct BatteryChangeResult
    {
        public bool ShouldUpdateUI;
        public bool ShouldFirePowerLineChanged;
        public bool ShouldPublishNotification;
        public bool IsLowBattery;
        public bool IsFullBattery;
        public int CurrentLevel;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        // Unsubscribe WMI event handler before disposing the watcher
#if WINDOWS
        if (_powerEventWatcher is System.Management.ManagementEventWatcher wmiWatcher)
        {
            wmiWatcher.EventArrived -= OnWmiPowerEvent;
        }
#endif
        _powerEventWatcher?.Dispose();
        _powerEventWatcher = null;

        // Clean up macOS Darwin notify resources.
        // Closing the fd unblocks the read() call in MacPowerNotifyLoop.
        if (_macNotifyToken >= 0)
        {
            notify_cancel(_macNotifyToken);
            _macNotifyToken = -1;
        }
        if (_macNotifyFd >= 0)
        {
            close(_macNotifyFd);
            _macNotifyFd = -1;
        }
        // Wait for the notify thread to exit (read() returns after fd is closed)
        _macNotifyThread?.Join(3000);
        _macNotifyThread = null;

        BatteryStatusChanged = null;
        PowerLineStatusChanged = null;
    }
}

public sealed class BatteryStatusEventArgs : EventArgs
{
    public int BatteryLevel { get; set; }
    public bool IsCharging { get; set; }
    public bool IsLowBattery { get; set; }
    public bool IsFullBattery { get; set; }
    public BatteryPowerLineStatus PowerLineStatus { get; set; }
    public BatteryChargeStatus BatteryChargeStatus { get; set; }
    public int BatteryLifeRemaining { get; set; }
}
