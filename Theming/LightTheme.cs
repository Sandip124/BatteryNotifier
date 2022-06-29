using System.Drawing;

namespace BatteryNotifier.Theming
{
    internal class LightTheme : BaseTheme
    {
        public override string Name => "Light";

        public override Color BackgroundColor => Color.White;
        
        public override Color AccentColor => Color.White;

        public override Color ForegroundColor => Color.Black;

        public override Color Accent2Color => Color.AliceBlue;

        public override Color Accent3Color => Color.AliceBlue;
    }
}
