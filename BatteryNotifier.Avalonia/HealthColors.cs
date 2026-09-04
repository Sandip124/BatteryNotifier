using BatteryNotifier.Core.Models;

namespace BatteryNotifier.Avalonia;

/// <summary>Accent hex colors + labels for battery-health metrics, keyed by <see cref="MetricStatus"/>.</summary>
internal static class HealthColors
{
    public const string Good = "#388E3C";
    public const string Fair = "#F9A825";
    public const string Poor = "#D32F2F";
    public const string Unknown = "#808080";

    private const string TemperatureCool = "#0288D1";

    /// <summary>Overall-health / cycle accent color.</summary>
    public static string ForHealth(MetricStatus status) => status switch
    {
        MetricStatus.Good => Good,
        MetricStatus.Fair => Fair,
        MetricStatus.Poor => Poor,
        _ => Unknown
    };

    /// <summary>Temperature accent color (cool → warm → hot).</summary>
    public static string ForTemperature(MetricStatus status) => status switch
    {
        MetricStatus.Good => TemperatureCool,
        MetricStatus.Fair => Fair,
        MetricStatus.Poor => Poor,
        _ => Unknown
    };

    /// <summary>Human-readable temperature status.</summary>
    public static string TemperatureText(MetricStatus status) => status switch
    {
        MetricStatus.Good => "Normal",
        MetricStatus.Fair => "Warm",
        MetricStatus.Poor => "Too Hot",
        _ => "Not supported"
    };
}
