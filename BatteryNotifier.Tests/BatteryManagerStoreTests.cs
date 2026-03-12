using BatteryNotifier.Core.Store;

namespace BatteryNotifier.Tests;

[Collection("BatteryManagerStore")]
public class BatteryManagerStoreTests
{
    [Theory]
    [InlineData(100, BatteryState.Full)]
    [InlineData(96, BatteryState.Full)]
    [InlineData(95, BatteryState.Adequate)]
    [InlineData(60, BatteryState.Adequate)]
    [InlineData(59, BatteryState.Sufficient)]
    [InlineData(40, BatteryState.Sufficient)]
    [InlineData(39, BatteryState.Low)]
    [InlineData(15, BatteryState.Low)]
    [InlineData(14, BatteryState.Critical)]
    [InlineData(0, BatteryState.Critical)]
    public void SetBatteryState_MapsPercentageCorrectly(int percent, BatteryState expected)
    {
        var store = BatteryManagerStore.Instance;
        store.SetBatteryState(percent);
        Assert.Equal(expected, store.BatteryState);
    }

    [Fact]
    public void SetChargingState_UpdatesProperties()
    {
        var store = BatteryManagerStore.Instance;

        store.SetChargingState(isCharging: true, isPluggedIn: true);
        Assert.True(store.IsCharging);
        Assert.True(store.IsPluggedIn);

        store.SetChargingState(isCharging: false, isPluggedIn: false);
        Assert.False(store.IsCharging);
        Assert.False(store.IsPluggedIn);
    }

    [Fact]
    public void SetBatteryLifePercentage_UpdatesProperty()
    {
        var store = BatteryManagerStore.Instance;
        store.SetBatteryLifePercentage(75.0);
        Assert.Equal(75.0, store.BatteryLifePercent);
    }

    [Fact]
    public void SetBatteryLife_UpdatesRemainingAndTimeSpan()
    {
        var store = BatteryManagerStore.Instance;
        store.SetBatteryLife(3600); // 1 hour in seconds
        Assert.Equal(3600, store.BatteryLifeRemaining);
        Assert.Equal(TimeSpan.FromSeconds(3600), store.BatteryLifeRemainingInSeconds);
    }

    [Fact]
    public void SetHasNoBattery_UpdatesProperty()
    {
        var store = BatteryManagerStore.Instance;
        store.SetHasNoBattery(true);
        Assert.True(store.HasNoBattery);
        store.SetHasNoBattery(false);
        Assert.False(store.HasNoBattery);
    }

    [Fact]
    public void SetIsUnknown_UpdatesProperty()
    {
        var store = BatteryManagerStore.Instance;
        store.SetIsUnknown(true);
        Assert.True(store.IsUnknown);
        store.SetIsUnknown(false);
        Assert.False(store.IsUnknown);
    }
}
