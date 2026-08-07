using Avalonia.Media;

namespace BatteryNotifier.Avalonia;

/// <summary>
/// Single source of truth for battery-alert accent colors. Used by both the per-alert
/// flash-color picker (<see cref="ViewModels.AlertRowViewModel.FlashColorOptions"/>) and the
/// auto-derived notification accent (<c>NotificationDisplayService.DetermineColor</c>), so a
/// picked color and its "Auto" equivalent are always the same shade.
/// </summary>
internal static class AlertAccent
{
    public const string RedHex = "#D32F2F";
    public const string AmberHex = "#F57A00";
    public const string GreenHex = "#388E3C";
    public const string BlueHex = "#0288D1";

    public static readonly Color Red = Color.Parse(RedHex);
    public static readonly Color Amber = Color.Parse(AmberHex);
    public static readonly Color Green = Color.Parse(GreenHex);
    public static readonly Color Blue = Color.Parse(BlueHex);
}
