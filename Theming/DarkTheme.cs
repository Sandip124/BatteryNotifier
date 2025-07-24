using System.Drawing;

namespace BatteryNotifier.Theming
{
    internal class DarkTheme() : BaseTheme(name: "Dark",
        backgroundColor: Color.Black,
        accentColor: Color.FromArgb(30, 30, 30),
        accent2Color: Color.FromArgb(38, 38, 38),
        accent3Color: Color.FromArgb(50, 50, 50),
        foregroundColor: Color.White,
        borderColor: Color.FromArgb(40, 40, 40));
}