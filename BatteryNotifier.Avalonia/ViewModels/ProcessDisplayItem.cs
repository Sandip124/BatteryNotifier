using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Avalonia.ViewModels;

/// <summary>
/// Display-ready wrapper around ProcessPowerInfo with estimated watts and actionable tips.
/// </summary>
public sealed class ProcessDisplayItem
{
    public required string Name { get; init; }
    public required double CpuPercent { get; init; }
    public required int Pid { get; init; }

    /// <summary>Battery impact display: time cost ("~25min"), watts ("~6.3W"), or CPU% fallback.</summary>
    public string? PowerDisplay { get; init; }

    /// <summary>Actionable tip for known apps, e.g. "Close unused tabs".</summary>
    public string? Tip { get; init; }

    public bool HasTip => !string.IsNullOrEmpty(Tip);

    /// <summary>Shows time cost or watts. Card is hidden when neither is available.</summary>
    public string DisplayValue => PowerDisplay ?? "--";

    /// <summary>Delegates to Core's ProcessTips for tip resolution.</summary>
    public static string? GetTipForProcess(string processName) => ProcessTips.GetTip(processName);
}
