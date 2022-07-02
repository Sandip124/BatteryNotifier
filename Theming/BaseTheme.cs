using System.Drawing;

namespace BatteryNotifier.Theming
{
    internal abstract class BaseTheme
    {
        public abstract string Name { get; }
        public abstract Color BackgroundColor { get; }
        public abstract Color AccentColor { get; }
        public abstract Color Accent2Color { get; }
        public abstract Color Accent3Color { get; }
        public abstract Color ForegroundColor { get; }
        
        
    }
}
