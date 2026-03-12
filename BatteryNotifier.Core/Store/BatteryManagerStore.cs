using System.Threading;

namespace BatteryNotifier.Core.Store;

public sealed class BatteryManagerStore
{
    private static readonly Lazy<BatteryManagerStore> _instance = new(() => new BatteryManagerStore());
    public static BatteryManagerStore Instance => _instance.Value;

    private readonly object _lock = new();

    private BatteryManagerStore() { }

    // Volatile fields ensure reads from the UI thread see the latest writes from the timer thread.
    // double uses Interlocked because volatile is not valid for 64-bit types on 32-bit platforms.
    private volatile BatteryState _batteryState;
    private volatile int _batteryLifeRemaining;
    private double _batteryLifePercent;
    private volatile bool _isCharging;
    private volatile bool _isPluggedIn;
    private volatile bool _hasNoBattery;
    private volatile bool _isUnknown;

    public BatteryState BatteryState => _batteryState;
    public int BatteryLifeRemaining => _batteryLifeRemaining;
    public TimeSpan BatteryLifeRemainingInSeconds => TimeSpan.FromSeconds(_batteryLifeRemaining);
    public double BatteryLifePercent => Interlocked.CompareExchange(ref _batteryLifePercent, 0, 0);

    public bool IsCharging => _isCharging;
    public bool IsPluggedIn => _isPluggedIn;
    public bool HasNoBattery => _hasNoBattery;
    public bool IsUnknown => _isUnknown;

    public void SetBatteryState(int powerPercent)
    {
        _batteryState = powerPercent switch
        {
            >= 96 => BatteryState.Full,
            >= 60 and <= 95 => BatteryState.Adequate,
            >= 40 and <= 59 => BatteryState.Sufficient,
            > 14 => BatteryState.Low,
            _ => BatteryState.Critical
        };
    }

    public void SetChargingState(bool isCharging, bool isPluggedIn)
    {
        lock (_lock)
        {
            _isCharging = isCharging;
            _isPluggedIn = isPluggedIn;
        }
    }

    public void SetHasNoBattery(bool hasNoBattery) => _hasNoBattery = hasNoBattery;
    public void SetIsUnknown(bool isUnknown) => _isUnknown = isUnknown;
    public void SetBatteryLife(int batteryLife) => _batteryLifeRemaining = batteryLife;

    public void SetBatteryLifePercentage(double currentStatusBatteryLifePercent)
    {
        Interlocked.Exchange(ref _batteryLifePercent, currentStatusBatteryLifePercent);
    }
}


public enum BatteryState
{
    Full,
    Adequate,
    Sufficient,
    Low,
    Critical,
}