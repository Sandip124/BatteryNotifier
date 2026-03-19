namespace BatteryNotifier.Core.Models;

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
}
