using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Providers;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class AlertEvaluationServiceTests
{
    private static AlertEvaluationService CreateService()
    {
        // Use reflection to create a fresh instance (bypass singleton for test isolation)
        var ctor = typeof(AlertEvaluationService).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, Type.EmptyTypes, null);
        return (AlertEvaluationService)ctor!.Invoke(null);
    }

    private static BatteryAlert MakeAlert(int lower, int upper, string id = "test1") =>
        new() { Id = id, Label = "Test", LowerBound = lower, UpperBound = upper, IsEnabled = true };

    [Fact]
    public void EnterRange_TriggersAlert()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        // Battery at 20% — inside range, first check = trigger
        var triggered = svc.EvaluateAlerts(alerts, 20,
            BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
        Assert.Equal("test1", triggered[0].Id);
    }

    [Fact]
    public void StayInsideRange_DoesNotRetrigger()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        // First evaluation triggers
        svc.EvaluateAlerts(alerts, 20, BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        // Second evaluation — still inside, should NOT re-trigger
        var triggered = svc.EvaluateAlerts(alerts, 18,
            BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void ExitAndReenter_TriggersAgain()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        // Enter
        svc.EvaluateAlerts(alerts, 20, BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        // Exit (beyond debounce buffer of 2)
        svc.EvaluateAlerts(alerts, 30, BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        // Re-enter
        var triggered = svc.EvaluateAlerts(alerts, 20,
            BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
    }

    [Fact]
    public void Debounce_PreventsPrematureDisarm()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        // Enter
        svc.EvaluateAlerts(alerts, 20, BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        // Move just outside but within debounce buffer (25 + 2 = 27)
        svc.EvaluateAlerts(alerts, 26, BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        // Back inside — should NOT trigger since never fully disarmed
        var triggered = svc.EvaluateAlerts(alerts, 24,
            BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void DisabledAlert_IsIgnored()
    {
        var svc = CreateService();
        var alert = MakeAlert(0, 25);
        alert.IsEnabled = false;

        var triggered = svc.EvaluateAlerts(new[] { alert }, 20,
            BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void ResetAll_ClearsState()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        // Enter
        svc.EvaluateAlerts(alerts, 20, BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        // Reset
        svc.ResetAll();

        // Re-enter — should trigger since state was cleared
        var triggered = svc.EvaluateAlerts(alerts, 20,
            BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
    }

    [Fact]
    public void MultipleAlerts_IndependentTracking()
    {
        var svc = CreateService();
        var alerts = new[]
        {
            MakeAlert(0, 25, "low"),
            MakeAlert(80, 100, "high")
        };

        // Battery at 20% — only low alert triggers
        var triggered = svc.EvaluateAlerts(alerts, 20,
            BatteryChargeStatus.Low, BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
        Assert.Equal("low", triggered[0].Id);
    }
}
