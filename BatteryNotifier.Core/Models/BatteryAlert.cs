namespace BatteryNotifier.Core.Models;

/// <summary>Semantic classification of an alert's range, driving both message tone and accent color.</summary>
public enum AlertTone { Neutral, Low, Full }

public sealed class BatteryAlert
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Label { get; set; } = string.Empty;
    public int LowerBound { get; set; }
    public int UpperBound { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
    public string? Sound { get; set; }
    /// <summary>
    /// Hex color for screen flash (e.g. "#D32F2F"). Null = auto-detect from battery level.
    /// </summary>
    public string? FlashColor { get; set; }

    /// <summary>
    /// Classifies the range so the message tone and accent color always agree — even for wide,
    /// custom, or overlapping ranges. A range that reaches empty (lower ≤ 5) or sits in the low
    /// half (upper ≤ 50) is Low; one that reaches full (upper ≥ 95) or sits in the high half
    /// (lower ≥ 50) is Full; a range that spans both extremes, or neither, is Neutral.
    /// </summary>
    public AlertTone Tone
    {
        get
        {
            var isLow = UpperBound <= 50 || LowerBound <= 5;
            var isFull = LowerBound >= 50 || UpperBound >= 95;

            if (isLow && isFull) return AlertTone.Neutral; // spans both extremes (e.g. 0–100)
            if (isLow) return AlertTone.Low;
            if (isFull) return AlertTone.Full;
            return AlertTone.Neutral;
        }
    }
}
