using System;
using System.Drawing;

namespace BatteryNotifier.Theming
{
    internal class DarkTheme : BaseTheme
    {
        public override string Name => "Dark";

        public override Color BackgroundColor => Color.Black;

        public override Color AccentColor => Color.FromArgb(30, 30, 30);

        public override Color ForegroundColor => Color.White;

        public override Color Accent2Color => Color.FromArgb(40,40,40);

        public override Color Accent3Color => Color.FromArgb(60, 60, 60);
    }
}
