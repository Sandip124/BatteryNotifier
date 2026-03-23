using BatteryNotifier.Core.Models;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Calculates battery drain rate from recent charge history.
/// Pure static logic — no side effects, fully testable.
/// </summary>
public static class DrainRateAnalyzer
{
    public const double RapidDrainThreshold = 3.0;
    private const int WindowSeconds = 5 * 60;
    private const int MinReadings = 3;

    public static double? CalculateDrainRate(IReadOnlyList<ChargeHistoryEntry> history, long nowUnixSeconds)
    {
        if (history is not { Count: >= MinReadings })
            return null;

        var (first, last, count) = FindDischargeRange(history, nowUnixSeconds - WindowSeconds);

        if (count < MinReadings)
            return null;

        var elapsedMinutes = (last.TimestampUnixSeconds - first.TimestampUnixSeconds) / 60.0;
        if (elapsedMinutes < 1.0)
            return null;

        var drainPercent = first.Percent - last.Percent;
        return drainPercent > 0 ? Math.Round(drainPercent / elapsedMinutes, 1) : null;
    }

    public static bool IsRapidDrain(double? ratePerMinute)
        => ratePerMinute >= RapidDrainThreshold;

    private static (ChargeHistoryEntry first, ChargeHistoryEntry last, int count) FindDischargeRange(
        IReadOnlyList<ChargeHistoryEntry> history, long cutoff)
    {
        ChargeHistoryEntry first = default, last = default;
        int count = 0;

        foreach (var entry in history)
        {
            if (entry.TimestampUnixSeconds < cutoff || entry.IsCharging)
                continue;

            count++;
            if (count == 1) first = entry;
            last = entry;
        }

        return (first, last, count);
    }
}
