namespace BatteryNotifier.Core.Store;

public sealed class BatteryManagerStore
{
    private static readonly Lazy<BatteryManagerStore> _instance = new(() => new BatteryManagerStore());
    public static BatteryManagerStore Instance => _instance.Value;

    private BatteryManagerStore() { }

    public BatteryState BatteryState { get; private set; }
    public int BatteryLifeRemaining { get; private set; }
    public TimeSpan BatteryLifeRemainingInSeconds  => TimeSpan.FromSeconds(BatteryLifeRemaining);
    public double BatteryLifePercent { get; private set; }

    public bool IsCharging { get; private set; }
    public bool HasNoBattery { get; private set; }
    public bool IsUnknown { get; private set; }

    public void SetBatteryState(int powerPercent)
    {
        BatteryState = powerPercent switch
        {
            >= 96 => BatteryState.Full,
            >= 60 and <= 95 => BatteryState.Adequate,
            >= 40 and <= 59 => BatteryState.Sufficient,
            > 14 => BatteryState.Low,
            _ => BatteryState.Critical
        };
    }

    public void SetChargingState(bool isCharging) => IsCharging = isCharging;
    public void SetHasNoBattery(bool hasNoBattery) => HasNoBattery = hasNoBattery;
    public void SetIsUnknown(bool isUnknown) => IsUnknown = isUnknown;
    public void SetBatteryLife(int batteryLife) => BatteryLifeRemaining = batteryLife;

    public void SetBatteryLifePercentage(double currentStatusBatteryLifePercent)
    {
        BatteryLifePercent = currentStatusBatteryLifePercent;
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