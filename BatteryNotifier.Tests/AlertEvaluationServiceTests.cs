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
            BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
        Assert.Equal("test1", triggered[0].Id);
    }

    [Fact]
    public void StayInsideRange_DoesNotRetrigger()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        // First evaluation triggers
        svc.EvaluateAlerts(alerts, 20, BatteryPowerLineStatus.Offline);

        // Second evaluation — still inside, should NOT re-trigger
        var triggered = svc.EvaluateAlerts(alerts, 18,
            BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void ExitAndReenter_TriggersAgain()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        // Enter
        svc.EvaluateAlerts(alerts, 20, BatteryPowerLineStatus.Offline);

        // Exit (beyond debounce buffer of 2)
        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline);

        // Re-enter
        var triggered = svc.EvaluateAlerts(alerts, 20,
            BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
    }

    [Fact]
    public void Debounce_PreventsPrematureDisarm()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        // Enter
        svc.EvaluateAlerts(alerts, 20, BatteryPowerLineStatus.Offline);

        // Move just outside but within debounce buffer (25 + 2 = 27)
        svc.EvaluateAlerts(alerts, 26, BatteryPowerLineStatus.Offline);

        // Back inside — should NOT trigger since never fully disarmed
        var triggered = svc.EvaluateAlerts(alerts, 24,
            BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void DisabledAlert_IsIgnored()
    {
        var svc = CreateService();
        var alert = MakeAlert(0, 25);
        alert.IsEnabled = false;

        var triggered = svc.EvaluateAlerts(new[] { alert }, 20,
            BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void ResetAll_ClearsState()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        // Enter
        svc.EvaluateAlerts(alerts, 20, BatteryPowerLineStatus.Offline);

        // Reset
        svc.ResetAll();

        // Re-enter — should trigger since state was cleared
        var triggered = svc.EvaluateAlerts(alerts, 20,
            BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
    }

    [Fact]
    public void RapidDrop_Retriggers_WhileInsideRange()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 40) };

        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline); // enter @30
        // A ≥5% fall since the last alert re-notifies immediately (fast/degraded drain).
        var triggered = svc.EvaluateAlerts(alerts, 25,
            BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
    }

    [Fact]
    public void SmallDrop_WithinInterval_DoesNotRetrigger()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 40) };

        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline);
        // 3% drop, no time elapsed → below the rapid-drop step and inside the interval.
        var triggered = svc.EvaluateAlerts(alerts, 27,
            BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void IntervalElapsed_Retriggers_EvenWithoutDrop()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var svc = CreateService();
        svc.Clock = () => now;
        var alerts = new[] { MakeAlert(0, 40) };

        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline); // fire @30
        now = now.AddMinutes(3); // past the first re-notify interval (2 min), same level

        var triggered = svc.EvaluateAlerts(alerts, 30,
            BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
    }

    [Fact]
    public void WithinInterval_NoDrop_DoesNotRetrigger()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var svc = CreateService();
        svc.Clock = () => now;
        var alerts = new[] { MakeAlert(0, 40) };

        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline);
        now = now.AddMinutes(1); // under the 2 min interval

        var triggered = svc.EvaluateAlerts(alerts, 30,
            BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void IgnoredDismissal_ResetsEscalation_ForEagerReminders()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var svc = CreateService();
        svc.Clock = () => now;
        var alerts = new[] { MakeAlert(0, 40) };

        // Build the escalation: after two fires the next interval would be 5 min.
        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline); // count 1
        now = now.AddMinutes(3);
        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline); // count 2

        svc.RecordDismissal("test1", userInitiated: false); // ignored → reset escalation

        now = now.AddMinutes(3); // 3 min ≥ reset interval (2 min), < the escalated 5 min
        var triggered = svc.EvaluateAlerts(alerts, 30,
            BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
    }

    [Fact]
    public void UserDismissal_KeepsEscalation_LongerBackoff()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var svc = CreateService();
        svc.Clock = () => now;
        var alerts = new[] { MakeAlert(0, 40) };

        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline); // count 1
        now = now.AddMinutes(3);
        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline); // count 2 (next = 5 min)

        svc.RecordDismissal("test1", userInitiated: true); // acknowledged → keep escalation

        now = now.AddMinutes(3); // < the escalated 5 min
        var triggered = svc.EvaluateAlerts(alerts, 30,
            BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void GetEscalationCount_ReflectsDeliveredNotifications()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var svc = CreateService();
        svc.Clock = () => now;
        var alerts = new[] { MakeAlert(0, 40) };

        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline);
        Assert.Equal(0, svc.GetEscalationCount("test1")); // first delivery → 0 prior

        now = now.AddMinutes(3);
        svc.EvaluateAlerts(alerts, 30, BatteryPowerLineStatus.Offline);
        Assert.Equal(1, svc.GetEscalationCount("test1"));
    }

    [Fact]
    public void LowAlert_WhilePluggedIn_DoesNotFire()
    {
        // Low alerts are "plug in" reminders — irrelevant once the charger is connected.
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        var triggered = svc.EvaluateAlerts(alerts, 20, BatteryPowerLineStatus.Online);

        Assert.Empty(triggered);
    }

    [Fact]
    public void LowAlert_WhileUnplugged_Fires()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(0, 25) };

        var triggered = svc.EvaluateAlerts(alerts, 20, BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
    }

    [Fact]
    public void FullAlert_WhileUnplugged_DoesNotFire()
    {
        // Full alerts are "unplug now" reminders — irrelevant when running on battery.
        var svc = CreateService();
        var alerts = new[] { MakeAlert(80, 100) };

        var triggered = svc.EvaluateAlerts(alerts, 90, BatteryPowerLineStatus.Offline);

        Assert.Empty(triggered);
    }

    [Fact]
    public void FullAlert_WhilePluggedIn_Fires()
    {
        var svc = CreateService();
        var alerts = new[] { MakeAlert(80, 100) };

        var triggered = svc.EvaluateAlerts(alerts, 90, BatteryPowerLineStatus.Online);

        Assert.Single(triggered);
    }

    [Fact]
    public void NeutralCustomAlert_FiresRegardlessOfCharger()
    {
        // A mid-range custom alert (Neutral tone) is generic — the charger state doesn't gate it.
        Assert.Single(CreateService().EvaluateAlerts(
            new[] { MakeAlert(30, 70) }, 50, BatteryPowerLineStatus.Online));
        Assert.Single(CreateService().EvaluateAlerts(
            new[] { MakeAlert(30, 70) }, 50, BatteryPowerLineStatus.Offline));
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
            BatteryPowerLineStatus.Offline);

        Assert.Single(triggered);
        Assert.Equal("low", triggered[0].Id);
    }
}
