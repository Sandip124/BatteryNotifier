using System.Drawing;

namespace BatteryNotifier.Theming
{
    internal abstract class BaseTheme(
        string name,
        Color backgroundColor,
        Color accentColor,
        Color accent2Color,
        Color accent3Color,
        Color foregroundColor)
    {
        public string Name { get; } = name;
        public Color BackgroundColor { get; } = backgroundColor;
        public Color AccentColor { get; } = accentColor;
        public Color Accent2Color { get; } = accent2Color;
        public Color Accent3Color { get; } = accent3Color;
        public Color ForegroundColor { get; } = foregroundColor;
    }
}