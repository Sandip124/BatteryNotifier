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
    public const string RedHex = "#E8574B";
    public const string AmberHex = "#F5A64B";
    public const string GreenHex = "#5FC08A";
    public const string BlueHex = "#7B9CF0";
    public const string PurpleHex = "#B584F2";

    public static readonly Color Red = Color.Parse(RedHex);
    public static readonly Color Amber = Color.Parse(AmberHex);
    public static readonly Color Green = Color.Parse(GreenHex);
    public static readonly Color Blue = Color.Parse(BlueHex);
    public static readonly Color Purple = Color.Parse(PurpleHex);
}
