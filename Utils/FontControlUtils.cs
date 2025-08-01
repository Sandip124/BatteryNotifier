using System;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Lib.Providers;

namespace BatteryNotifier.Utils
{
    internal static class FontControlUtils
    {
        public static void ApplyRegularFont(this Control control, float? size = null) => ApplyFont(control,size, FontStyle.Regular);

        public static void ApplyBoldFont(this Control control, float? size = null) => ApplyFont(control,size, FontStyle.Bold);

        private static void ApplyFont(this Control control, float? size,FontStyle fontStyle)
        {
            var targetSize = size ?? control.Font.Size;

            if (!IsSameFont(control.Font, fontStyle, targetSize))
            {
                UtilityHelper.SafeInvoke(control,
                    () => { control.Font = FontProvider.GetFont(targetSize, fontStyle); });
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