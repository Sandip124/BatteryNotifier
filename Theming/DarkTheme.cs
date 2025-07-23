using System.Drawing;

namespace BatteryNotifier.Theming
{
    internal class DarkTheme() : BaseTheme(name: "Dark",
        backgroundColor: Color.Black,
        accentColor: Color.FromArgb(30, 30, 30),
        accent2Color: Color.FromArgb(40, 40, 40),
        accent3Color: Color.FromArgb(60, 60, 60),
        foregroundColor: Color.White);
}