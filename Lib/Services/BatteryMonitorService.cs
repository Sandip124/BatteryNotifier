using System;
using System.ComponentModel;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using BatteryNotifier.Lib.Store;

namespace BatteryNotifier.Lib.Services;

public sealed class BatteryMonitorService : IDisposable
{
    private static readonly Lazy<BatteryMonitorService> _instance =
        new Lazy<BatteryMonitorService>(() => new BatteryMonitorService());

    public static BatteryMonitorService Instance => _instance.Value;

    private PowerStatus? _lastPowerStatus = null;
    private int _lowBatteryThreshold = 20;
    private int _fullBatteryThreshold = 90;

    private const int BATTERY_LEVEL_CHECK_THRESHOLD = 120000;

    public event EventHandler<BatteryStatusEventArgs>? BatteryStatusChanged;
    public event EventHandler<BatteryStatusEventArgs>? PowerLineStatusChanged;

    private BackgroundWorker? _backgroundWorker;
    private ManagementEventWatcher? _powerEventWatcher;
    private bool _disposed;

    private BatteryMonitorService()
    {
        InitializeWmiWatcher();
        StartBatteryLevelMonitor();
    }

    private void InitializeWmiWatcher()
    {
        try
        {
            var query = new WqlEventQuery("SELECT * FROM Win32_PowerManagementEvent");
            _powerEventWatcher = new ManagementEventWatcher(query);
            _powerEventWatcher.EventArrived += OnWmiPowerEvent;
            _powerEventWatcher.Start();

            Console.WriteLine("WMI Power event watcher started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize WMI watcher: {ex.Message}");
        }
    }

    private void OnWmiPowerEvent(object sender, EventArrivedEventArgs e)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WMI Power event detected");
        CheckBatteryAndPowerStatus(forceCheck: true);
    }

    private void StartBatteryLevelMonitor()
    {
        _backgroundWorker = new BackgroundWorker
        {
            WorkerSupportsCancellation = true,
        };

        _backgroundWorker.DoWork += (sender, e) =>
        {
            while (!_backgroundWorker.CancellationPending)
            {
                CheckBatteryAndPowerStatus();
                Thread.Sleep(BATTERY_LEVEL_CHECK_THRESHOLD);
            }
        };

        _backgroundWorker.RunWorkerAsync();
    }

    private void CheckBatteryAndPowerStatus(bool forceCheck = false)
    {
        if (_disposed) return;

        var currentStatus = SystemInformation.PowerStatus;

        if (currentStatus.BatteryChargeStatus is BatteryChargeStatus.NoSystemBattery or BatteryChargeStatus.Unknown)
            return;

        var currentLevel = (int)(currentStatus.BatteryLifePercent * 100);
        var lastLevel = _lastPowerStatus != null ? (int)(_lastPowerStatus.BatteryLifePercent * 100) : 0;

        bool powerLineChanged = _lastPowerStatus?.PowerLineStatus != currentStatus.PowerLineStatus;

        bool batteryLevelChanged = Math.Abs(currentLevel - lastLevel) >= 5;

        bool isLowBattery = currentLevel <= _lowBatteryThreshold &&
                            currentStatus.BatteryChargeStatus != BatteryChargeStatus.Charging;

        bool isFullBattery = currentLevel >= _fullBatteryThreshold &&
                             ((currentStatus.BatteryChargeStatus & BatteryChargeStatus.Charging) ==
                              BatteryChargeStatus.Charging ||
                              currentStatus.BatteryChargeStatus == BatteryChargeStatus.High);

        bool shouldNotify = forceCheck || powerLineChanged || batteryLevelChanged || isLowBattery || isFullBattery ||
                            _lastPowerStatus == null;

        UpdateBatteryManagerStore(currentStatus, currentLevel);

        if (shouldNotify)
        {
            Console.WriteLine(
                $@"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Invoked by: {(forceCheck ? "Force Check" : "Background Worker")}, Battery Level: {currentLevel}%, Power Line Status: {currentStatus.PowerLineStatus}");

            if (powerLineChanged && _lastPowerStatus != null)
            {
                PowerLineStatusChanged?.Invoke(this, CreateBatteryEventArgs(currentStatus));
            }

            BatteryStatusChanged?.Invoke(this, CreateBatteryEventArgs(currentStatus));
            _lastPowerStatus = currentStatus;
        }
    }

    private static void UpdateBatteryManagerStore(PowerStatus currentStatus, int currentLevel)
    {
        BatteryManagerStore.Instance.SetChargingState(currentStatus.PowerLineStatus == PowerLineStatus.Online &&
                                                      currentStatus.BatteryChargeStatus !=
                                                      BatteryChargeStatus.NoSystemBattery &&
                                                      currentStatus.BatteryChargeStatus !=
                                                      BatteryChargeStatus.Charging);
        BatteryManagerStore.Instance.SetBatteryState(currentLevel);
        BatteryManagerStore.Instance.SetBatteryLife(currentStatus.BatteryLifeRemaining);
        BatteryManagerStore.Instance.SetBatteryLifePercentage(Math.Round(currentStatus.BatteryLifePercent * 100, 0));
        BatteryManagerStore.Instance.SetHasNoBattery(
            currentStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery);
        BatteryManagerStore.Instance.SetIsUnknown(
            currentStatus.BatteryChargeStatus == BatteryChargeStatus.Unknown);
    }

    private BatteryStatusEventArgs CreateBatteryEventArgs(PowerStatus status)
    {
        var level = (int)(status.BatteryLifePercent * 100);
        return new BatteryStatusEventArgs
        {
            BatteryLevel = level,
            IsCharging = status.PowerLineStatus == PowerLineStatus.Online,
            IsLowBattery = level <= _lowBatteryThreshold,
            IsFullBattery = level >= _fullBatteryThreshold,
            PowerLineStatus = status.PowerLineStatus,
            BatteryChargeStatus = status.BatteryChargeStatus,
            BatteryLifeRemaining = status.BatteryLifeRemaining
        };
    }

    public void SetThresholds(int lowThreshold, int fullThreshold)
    {
        _lowBatteryThreshold = lowThreshold;
        _fullBatteryThreshold = fullThreshold;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _backgroundWorker?.CancelAsync();
        _backgroundWorker?.Dispose();
        _backgroundWorker = null;

        if (_powerEventWatcher != null)
        {
            _powerEventWatcher.EventArrived -= OnWmiPowerEvent;
            _powerEventWatcher?.Stop();
            _powerEventWatcher?.Dispose();
        }

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
    public PowerLineStatus PowerLineStatus { get; set; }
    public BatteryChargeStatus BatteryChargeStatus { get; set; }
    public int BatteryLifeRemaining { get; set; }
}