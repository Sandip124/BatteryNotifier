using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Providers;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// The single decision-maker for WHEN a battery alert should (re)fire. Per alert it tracks range
/// membership (with a 2% debounce) plus an escalation cycle, and — while the battery stays inside a
/// range — decides re-notifications from:
/// <list type="bullet">
/// <item><b>Entry</b> — fires once when the battery crosses into the range.</item>
/// <item><b>Rapid drop</b> — a ≥5% fall since the last alert (fast-draining / degraded battery).</item>
/// <item><b>Severity-capped backoff</b> — an escalating interval clamped shorter the worse the
/// battery gets, so it never stays silent for long yet doesn't spam.</item>
/// <item><b>Engagement</b> — a user dismissal lets the backoff keep growing (nag less); a timed-out
/// (ignored) alert resets it so reminders stay eager. Fed in via <see cref="RecordDismissal"/>.</item>
/// </list>
/// Delivery concerns (dedup, pause, DND, priority) live in <see cref="NotificationService"/>.
/// </summary>
public sealed class AlertEvaluationService
{
    private static readonly Lazy<AlertEvaluationService> _instance = new(() => new AlertEvaluationService());
    public static AlertEvaluationService Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("AlertEvaluationService");

    private const int DebounceBuffer = 2;

    /// <summary>A fall of this many points since the last alert forces a re-notify (fast/degraded drain).</summary>
    private const int RapidDropStep = 5;

    /// <summary>Escalating re-notify backoff for a persisting alert (index 0 = first re-notify after entry).</summary>
    private static readonly TimeSpan[] BackoffIntervals =
    [
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromMinutes(45),
    ];

    /// <summary>Upper bound on the re-notify interval by severity — the worse it gets, the sooner we return.</summary>
    private static TimeSpan SeverityCap(int level) => level switch
    {
        <= 10 => TimeSpan.FromMinutes(2),
        <= 20 => TimeSpan.FromMinutes(5),
        _ => TimeSpan.FromMinutes(15),
    };

    /// <summary>Test seam: overridable clock so re-notify timing is deterministic in tests.</summary>
    internal Func<DateTime> Clock { get; set; } = () => DateTime.UtcNow;

    private sealed class AlertState
    {
        public bool WasInside;
        public int FireCount;
        public int LastFireLevel;
        public DateTime LastFireTime;
    }

    private readonly Dictionary<string, AlertState> _states = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    private AlertEvaluationService() { }

    /// <summary>
    /// Evaluates all enabled alerts and returns those that should fire now — whether that's a fresh
    /// entry into the range or a re-notification while still inside (see the class summary).
    /// </summary>
    public List<BatteryAlert> EvaluateAlerts(
        IReadOnlyList<BatteryAlert> alerts,
        int currentLevel,
        BatteryPowerLineStatus powerStatus)
    {
        var triggered = new List<BatteryAlert>();
        var now = Clock();

        lock (_lock)
        {
            triggered.AddRange(alerts.Where(alert => alert.IsEnabled && ShouldFire(alert, currentLevel, powerStatus, now)));
        }

        return triggered;
    }

    /// <summary>
    /// Advances the alert's tracked state for this tick and returns whether it should fire now —
    /// either a fresh entry into the range or a re-notification while still inside (see <see cref="ShouldRefire"/>).
    /// </summary>
    private bool ShouldFire(
        BatteryAlert alert, int currentLevel,
        BatteryPowerLineStatus powerStatus, DateTime now)
    {
        var state = GetState(alert.Id);

        if (!IsInsideAlertRange(alert, currentLevel, powerStatus))
        {
            DisarmIfClearOfBuffer(state, alert, currentLevel);
            return false;
        }

        bool entered = !state.WasInside;
        bool fire = entered || ShouldRefire(state, currentLevel, now);
        state.WasInside = true;

        if (!fire) return false;

        if (entered) state.FireCount = 0; // fresh escalation cycle on entry
        state.FireCount++;
        state.LastFireLevel = currentLevel;
        state.LastFireTime = now;
        return true;
    }

    private AlertState GetState(string alertId)
    {
        if (!_states.TryGetValue(alertId, out var state))
        {
            state = new AlertState();
            _states[alertId] = state;
        }
        return state;
    }

    /// <summary>
    /// Disarms an alert that has left its range — but only once the level is clear of the debounce
    /// buffer, so a level hovering on the boundary doesn't re-arm and double-fire.
    /// </summary>
    private static void DisarmIfClearOfBuffer(AlertState state, BatteryAlert alert, int currentLevel)
    {
        if (!state.WasInside) return;

        bool clearOfBuffer =
            currentLevel < alert.LowerBound - DebounceBuffer ||
            currentLevel > alert.UpperBound + DebounceBuffer;

        if (clearOfBuffer)
        {
            state.WasInside = false;
            state.FireCount = 0;
        }
    }

    /// <summary>Decides whether a still-in-range alert should re-notify: rapid drop or interval elapsed.</summary>
    private static bool ShouldRefire(AlertState state, int currentLevel, DateTime now)
    {
        // Fast-draining / degraded battery: a big fall since the last alert re-notifies at once.
        if (state.LastFireLevel - currentLevel >= RapidDropStep)
            return true;

        // Otherwise wait out the escalating backoff, clamped shorter as severity rises so it
        // never stays silent for long.
        var index = Math.Clamp(state.FireCount - 1, 0, BackoffIntervals.Length - 1);
        var escalated = BackoffIntervals[index];
        var cap = SeverityCap(currentLevel);
        var interval = escalated < cap ? escalated : cap;

        return (now - state.LastFireTime) >= interval;
    }

    /// <summary>
    /// Number of prior notifications delivered for this alert's current cycle (0 on the first),
    /// used to pick escalation-aware message templates.
    /// </summary>
    public int GetEscalationCount(string alertId)
    {
        lock (_lock)
            return _states.TryGetValue(alertId, out var s) ? Math.Max(0, s.FireCount - 1) : 0;
    }

    /// <summary>
    /// Feeds dismissal engagement back into the escalation. A user dismissal keeps the escalation
    /// that already advanced (each further reminder waits longer — nag less); an ignored alert
    /// (timed out unseen) resets the cycle so reminders stay eager. No-op for previews (no id).
    /// </summary>
    public void RecordDismissal(string? alertId, bool userInitiated)
    {
        if (userInitiated || string.IsNullOrEmpty(alertId)) return;

        lock (_lock)
        {
            if (_states.TryGetValue(alertId, out var state))
            {
                Logger.Debug("Alert {Id} ignored (timed out) — resetting escalation for eager reminders", alertId);
                state.FireCount = 0;
            }
        }
    }

    private static bool IsInsideAlertRange(
        BatteryAlert alert,
        int currentLevel,
        BatteryPowerLineStatus powerStatus)
    {
        if (currentLevel < alert.LowerBound || currentLevel > alert.UpperBound)
            return false;

        // The alert's Tone (the single classifier that also drives the message wording and accent
        // color) decides whether the charger state gates it:
        //   Full  → only relevant while plugged in   (an "unplug now" reminder)
        //   Low   → only relevant while on battery    (a "plug in" reminder)
        //   Neutral (custom / mid-range) → generic, fires regardless of the charger.
        bool pluggedIn = powerStatus == BatteryPowerLineStatus.Online;
        return alert.Tone switch
        {
            AlertTone.Full => pluggedIn,
            AlertTone.Low => !pluggedIn,
            _ => true,
        };
    }

    /// <summary>
    /// Resets all tracking state. Called on power line changes for eager re-notification.
    /// </summary>
    public void ResetAll()
    {
        lock (_lock)
        {
            _states.Clear();
        }
    }
}
