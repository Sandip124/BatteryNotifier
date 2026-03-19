using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Providers;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Evaluates multi-level battery alerts against the current battery state.
/// Tracks per-alert arm/disarm with a 2% debounce buffer to prevent rapid toggling.
/// </summary>
public sealed class AlertEvaluationService
{
    private static readonly Lazy<AlertEvaluationService> _instance = new(() => new AlertEvaluationService());
    public static AlertEvaluationService Instance => _instance.Value;

    private const int DebounceBuffer = 2;

    /// <summary>True = battery was inside the alert range on last check.</summary>
    private readonly Dictionary<string, bool> _wasInsideRange = new(StringComparer.Ordinal);

    private readonly object _lock = new();

    private AlertEvaluationService() { }

    /// <summary>
    /// Evaluates all enabled alerts and returns those that just triggered
    /// (outside→inside transition).
    /// </summary>
    public List<BatteryAlert> EvaluateAlerts(
        IReadOnlyList<BatteryAlert> alerts,
        int currentLevel,
        BatteryChargeStatus chargeStatus,
        BatteryPowerLineStatus powerStatus)
    {
        var triggered = new List<BatteryAlert>();

        lock (_lock)
        {
            foreach (var alert in alerts)
            {
                if (!alert.IsEnabled) continue;

                bool isInside = currentLevel >= alert.LowerBound && currentLevel <= alert.UpperBound;

                // For "full battery" style alerts (upper bound near 100), require plugged in
                if (alert.UpperBound >= 95 && alert.LowerBound >= 50)
                {
                    isInside = isInside && powerStatus == BatteryPowerLineStatus.Online;
                }

                // For "low battery" style alerts (lower bound near 0), require not charging
                if (alert.LowerBound <= 5 && alert.UpperBound <= 50)
                {
                    isInside = isInside && chargeStatus != BatteryChargeStatus.Charging;
                }

                _wasInsideRange.TryGetValue(alert.Id, out var wasInside);

                if (isInside && !wasInside)
                {
                    // Transition from outside → inside: trigger
                    triggered.Add(alert);
                    _wasInsideRange[alert.Id] = true;
                }
                else if (!isInside && wasInside)
                {
                    // Check debounce: only disarm if battery moved beyond the buffer
                    bool outsideBuffer =
                        currentLevel < alert.LowerBound - DebounceBuffer ||
                        currentLevel > alert.UpperBound + DebounceBuffer;

                    if (outsideBuffer)
                    {
                        _wasInsideRange[alert.Id] = false;
                    }
                    // else: stay armed (within debounce zone)
                }
                else if (isInside)
                {
                    // Already inside — no new trigger, but keep tracking
                    _wasInsideRange[alert.Id] = true;
                }
            }
        }

        return triggered;
    }

    /// <summary>
    /// Resets all tracking state. Called on power line changes for eager re-notification.
    /// </summary>
    public void ResetAll()
    {
        lock (_lock)
        {
            _wasInsideRange.Clear();
        }
    }
}
