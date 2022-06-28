using System.Drawing;
using System.Drawing.Text;

namespace BatteryNotifier.Providers
{
    internal class FontProvider
    {
        private const string FontDirectory = "Assets/fonts";

        private const string RegularFont = "Inter-Regular.ttf";
        private const string BoldFont = "Inter-Bold.ttf";


        public PrivateFontCollection FontCollection = new();

        public static FontProvider Default = new();

        private FontProvider()
        {
            FontCollection.AddFontFile($"{FontDirectory}/{RegularFont}");
            FontCollection.AddFontFile($"{FontDirectory}/{BoldFont}");
        }

        public static Font GetRegularFont(float size = 8)
        {
            return new Font(Default.FontCollection.Families[0], size, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Font GetBoldFont(float size = 8)
        {
            return new Font(Default.FontCollection.Families[0], size, FontStyle.Bold, GraphicsUnit.Point);
        }

        public static Font GetBoldUnderlineFont(float size = 8)
        {
            return new Font(Default.FontCollection.Families[0], size, FontStyle.Underline | FontStyle.Bold, GraphicsUnit.Point);
        }
    }
}
