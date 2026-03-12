using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

/// <summary>
/// Tests the battery change evaluation logic extracted from BatteryMonitorService.
/// Verifies the fix for: UI not updating on gradual 1% battery changes
/// (previously required 5% change to fire events).
/// </summary>
public class BatteryMonitorServiceTests
{
    private static BatteryInfo MakeStatus(int percent, BatteryPowerLineStatus powerLine = BatteryPowerLineStatus.Offline,
        BatteryChargeStatus charge = BatteryChargeStatus.High)
    {
        return new BatteryInfo
        {
            BatteryLifePercent = percent / 100f,
            PowerLineStatus = powerLine,
            BatteryChargeStatus = charge,
            BatteryLifeRemaining = -1
        };
    }

    // ── The bug fix: 1% change should update UI ─────────────────

    [Fact]
    public void EvaluateBatteryChange_1PercentDrop_ShouldUpdateUI()
    {
        // Previously required 5% change — this is the core bug
        var last = MakeStatus(86);
        var current = MakeStatus(85);

        var result = BatteryMonitorService.EvaluateBatteryChange(last, current, 25, 96, forceCheck: false);

        Assert.True(result.ShouldUpdateUI, "1% drop should trigger UI update");
    }

    [Fact]
    public void EvaluateBatteryChange_1PercentRise_ShouldUpdateUI()
    {
        var last = MakeStatus(50);
        var current = MakeStatus(51);

        var result = BatteryMonitorService.EvaluateBatteryChange(last, current, 25, 96, forceCheck: false);

        Assert.True(result.ShouldUpdateUI);
    }

    [Fact]
    public void EvaluateBatteryChange_SameLevel_ShouldNotUpdateUI()
    {
        var last = MakeStatus(86);
        var current = MakeStatus(86);

        var result = BatteryMonitorService.EvaluateBatteryChange(last, current, 25, 96, forceCheck: false);

        Assert.False(result.ShouldUpdateUI, "No change = no UI update needed");
    }

    // ── Bug scenario: gradual drain from 86% to 72% ────────────

    [Fact]
    public void EvaluateBatteryChange_GradualDrain_EveryPercentUpdatesUI()
    {
        // Simulate the exact user scenario: unplugged at 86%, drains to 72%
        BatteryInfo? last = MakeStatus(86, BatteryPowerLineStatus.Offline);

        for (int level = 85; level >= 72; level--)
        {
            var current = MakeStatus(level, BatteryPowerLineStatus.Offline);
            var result = BatteryMonitorService.EvaluateBatteryChange(last, current, 25, 96, forceCheck: false);

            Assert.True(result.ShouldUpdateUI, $"Drop to {level}% should update UI");
            Assert.False(result.ShouldFirePowerLineChanged, "Power line didn't change");
            Assert.Equal(level, result.CurrentLevel);

            last = current; // simulate _lastPowerStatus update
        }
    }

    // ── First check (null last status) ──────────────────────────

    [Fact]
    public void EvaluateBatteryChange_FirstCheck_ShouldUpdateUI()
    {
        var current = MakeStatus(75);

        var result = BatteryMonitorService.EvaluateBatteryChange(null, current, 25, 96, forceCheck: false);

        Assert.True(result.ShouldUpdateUI, "First check should always update UI");
    }

    // ── Force check ─────────────────────────────────────────────

    [Fact]
    public void EvaluateBatteryChange_ForceCheck_AlwaysUpdatesUI()
    {
        var last = MakeStatus(86);
        var current = MakeStatus(86); // same level

        var result = BatteryMonitorService.EvaluateBatteryChange(last, current, 25, 96, forceCheck: true);

        Assert.True(result.ShouldUpdateUI, "Force check should always update UI");
    }

    // ── Power line changes ──────────────────────────────────────

    [Fact]
    public void EvaluateBatteryChange_PlugIn_FiresPowerLineChanged()
    {
        var last = MakeStatus(72, BatteryPowerLineStatus.Offline);
        var current = MakeStatus(72, BatteryPowerLineStatus.Online, BatteryChargeStatus.Charging);

        var result = BatteryMonitorService.EvaluateBatteryChange(last, current, 25, 96, forceCheck: false);

        Assert.True(result.ShouldUpdateUI);
        Assert.True(result.ShouldFirePowerLineChanged);
    }

    [Fact]
    public void EvaluateBatteryChange_Unplug_FiresPowerLineChanged()
    {
        var last = MakeStatus(86, BatteryPowerLineStatus.Online, BatteryChargeStatus.Charging);
        var current = MakeStatus(86, BatteryPowerLineStatus.Offline);

        var result = BatteryMonitorService.EvaluateBatteryChange(last, current, 25, 96, forceCheck: false);

        Assert.True(result.ShouldUpdateUI);
        Assert.True(result.ShouldFirePowerLineChanged);
    }

    [Fact]
    public void EvaluateBatteryChange_FirstCheckPluggedIn_NoPowerLineEvent()
    {
        // First check has no previous state — should not fire PowerLineStatusChanged
        var current = MakeStatus(50, BatteryPowerLineStatus.Online, BatteryChargeStatus.Charging);

        var result = BatteryMonitorService.EvaluateBatteryChange(null, current, 25, 96, forceCheck: false);

        Assert.True(result.ShouldUpdateUI);
        Assert.False(result.ShouldFirePowerLineChanged, "No previous state = no power line event");
    }

    // ── Low battery threshold ───────────────────────────────────

    [Theory]
    [InlineData(25, true)]   // At threshold
    [InlineData(24, true)]   // Below threshold
    [InlineData(10, true)]   // Well below
    [InlineData(26, false)]  // Above threshold
    [InlineData(50, false)]  // Well above
    public void EvaluateBatteryChange_LowBatteryDetection(int level, bool expectedLow)
    {
        var current = MakeStatus(level, BatteryPowerLineStatus.Offline, BatteryChargeStatus.Low);

        var result = BatteryMonitorService.EvaluateBatteryChange(null, current, lowThreshold: 25, fullThreshold: 96, forceCheck: false);

        Assert.Equal(expectedLow, result.IsLowBattery);
    }

    [Fact]
    public void EvaluateBatteryChange_LowBattery_NotTriggeredWhileCharging()
    {
        var current = MakeStatus(10, BatteryPowerLineStatus.Online, BatteryChargeStatus.Charging);

        var result = BatteryMonitorService.EvaluateBatteryChange(null, current, 25, 96, forceCheck: false);

        Assert.False(result.IsLowBattery, "Should not alert low battery while charging");
    }

    // ── Full battery threshold ──────────────────────────────────

    [Theory]
    [InlineData(96, true)]   // At threshold
    [InlineData(100, true)]  // Full
    [InlineData(95, false)]  // Below threshold
    public void EvaluateBatteryChange_FullBatteryDetection(int level, bool expectedFull)
    {
        var current = MakeStatus(level, BatteryPowerLineStatus.Online, BatteryChargeStatus.Charging);

        var result = BatteryMonitorService.EvaluateBatteryChange(null, current, lowThreshold: 25, fullThreshold: 96, forceCheck: false);

        Assert.Equal(expectedFull, result.IsFullBattery);
    }

    [Fact]
    public void EvaluateBatteryChange_FullBattery_NotTriggeredWhenUnplugged()
    {
        // 100% but unplugged and discharging — not "full battery" scenario
        var current = MakeStatus(100, BatteryPowerLineStatus.Offline, BatteryChargeStatus.Low);

        var result = BatteryMonitorService.EvaluateBatteryChange(null, current, 25, 96, forceCheck: false);

        Assert.False(result.IsFullBattery, "Should not alert full battery when unplugged and discharging");
    }

    // ── Notifications are independent of UI updates ─────────────

    [Fact]
    public void EvaluateBatteryChange_LowBattery_DetectedEvenWhenLevelUnchanged()
    {
        // Battery stays at 20% across two checks — still low
        var last = MakeStatus(20, BatteryPowerLineStatus.Offline, BatteryChargeStatus.Low);
        var current = MakeStatus(20, BatteryPowerLineStatus.Offline, BatteryChargeStatus.Low);

        var result = BatteryMonitorService.EvaluateBatteryChange(last, current, 25, 96, forceCheck: false);

        Assert.False(result.ShouldUpdateUI, "Level didn't change");
        Assert.True(result.IsLowBattery, "Still low battery regardless of UI update");
    }
}
