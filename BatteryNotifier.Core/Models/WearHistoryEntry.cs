namespace BatteryNotifier.Core.Models;

/// <summary>
/// A single wear/health reading for the monthly trend chart.
/// </summary>
public readonly record struct WearHistoryEntry(long TimestampUnixSeconds, double HealthPercent, int? CycleCount);
