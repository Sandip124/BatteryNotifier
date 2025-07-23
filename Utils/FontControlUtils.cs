using System;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Lib.Providers;

namespace BatteryNotifier.Utils
{
    internal static class FontControlUtils
    {
        public static void ApplyRegularFont(this Control control, float? size = null)
        {
            float targetSize = size ?? control.Font.Size;

            if (!IsSameFont(control.Font, FontStyle.Regular, targetSize))
            {
                control.Font = FontProvider.GetRegularFont(targetSize);
            }
        }

        public static void ApplyBoldFont(this Control control, float? size = null)
        {
            float targetSize = size ?? control.Font.Size;

            if (!IsSameFont(control.Font, FontStyle.Bold, targetSize))
            {
                control.Font = FontProvider.GetBoldFont(targetSize);
            }
        }

        private static bool IsSameFont(Font currentFont, FontStyle targetStyle, float targetSize)
        {
            return currentFont.FontFamily.Name == FontProvider.FontFamilyName &&
                   currentFont.Style == targetStyle &&
                   Math.Abs(currentFont.Size - targetSize) < 0.01f;
        }
    }

}
