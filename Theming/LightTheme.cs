using System.Drawing;

namespace BatteryNotifier.Theming
{
    internal class LightTheme() : BaseTheme(name: "Light",
        backgroundColor: Color.White,
        accentColor: Color.White,
        accent2Color: Color.FromArgb(245, 245, 245),
        accent3Color: Color.FromArgb(230, 230, 230),
        foregroundColor: Color.Black,
        borderColor: SystemColors.ActiveBorder);
}