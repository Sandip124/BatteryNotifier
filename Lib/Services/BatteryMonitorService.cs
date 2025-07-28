using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BatteryNotifier.Lib.Services;

public sealed class BatteryMonitorService
{
    private static readonly Lazy<BatteryMonitorService> _instance = 
        new Lazy<BatteryMonitorService>(() => new BatteryMonitorService());
    
    public static BatteryMonitorService Instance => _instance.Value;
    
    private PowerStatus? _lastPowerStatus = null;
    private int _lowBatteryThreshold = 20;
    private int _fullBatteryThreshold = 95;
    
    private const int BATTERY_LEVEL_CHECK_THRESHOLD = 1000;
    
    public event EventHandler<BatteryStatusEventArgs> BatteryStatusChanged;
    public event EventHandler<BatteryStatusEventArgs> PowerLineStatusChanged;
    
    private BatteryMonitorService()
    {
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        _lastPowerStatus = SystemInformation.PowerStatus;
        StartBatteryLevelMonitor();
    }
    
    private void StartBatteryLevelMonitor()
    {
        var backgroundWorker = new BackgroundWorker
        {
            WorkerSupportsCancellation = true,
            
        };
        
        backgroundWorker.DoWork += (sender, e) =>
        {
            while (!backgroundWorker.CancellationPending)
            {
                CheckBatteryLevel();
                Thread.Sleep(BATTERY_LEVEL_CHECK_THRESHOLD);
            }
        };
        
        backgroundWorker.RunWorkerAsync();
    }
    
    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        var currentStatus = SystemInformation.PowerStatus;
        
        if (_lastPowerStatus?.PowerLineStatus != currentStatus.PowerLineStatus)
        {
            PowerLineStatusChanged?.Invoke(this, CreateBatteryEventArgs(currentStatus));
        }
        
        BatteryStatusChanged?.Invoke(this, CreateBatteryEventArgs(currentStatus));
        
        _lastPowerStatus = currentStatus;
    }
    
    private void CheckBatteryLevel()
    {
        var currentStatus = SystemInformation.PowerStatus;

        if (currentStatus.BatteryChargeStatus is BatteryChargeStatus.NoSystemBattery or BatteryChargeStatus.Unknown) return;

        var currentLevel = (int)(currentStatus.BatteryLifePercent * 100);
        var lastLevel = (int)(_lastPowerStatus?.BatteryLifePercent * 100 ?? 0);
        
        bool shouldNotify = (currentLevel <= _lowBatteryThreshold &&
                             currentStatus.BatteryChargeStatus is not BatteryChargeStatus.Charging or BatteryChargeStatus.Low)
                             || (currentLevel >= _fullBatteryThreshold &&
                                 currentStatus.BatteryChargeStatus is BatteryChargeStatus.Charging or BatteryChargeStatus.High)
                             || Math.Abs(currentLevel - lastLevel) >= 5;
        
        if (shouldNotify)
        {
            BatteryStatusChanged?.Invoke(this, CreateBatteryEventArgs(currentStatus));
            _lastPowerStatus = currentStatus;
        }
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
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
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