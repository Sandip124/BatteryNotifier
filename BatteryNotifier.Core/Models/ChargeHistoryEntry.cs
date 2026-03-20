namespace BatteryNotifier.Core.Models;

/// <summary>
/// A single charge-level reading for the 24h sparkline.
/// </summary>
public readonly record struct ChargeHistoryEntry(long TimestampUnixSeconds, byte Percent, bool IsCharging);
