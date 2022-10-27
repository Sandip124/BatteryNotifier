using BatteryNotifier.Providers;
using System.Windows.Forms;

namespace BatteryNotifier.Helpers
{
    internal static class ApplyFontHelper
    {
        public static Control ApplyRegularFont(this Control control, float? size = null)
        {
            control.Font = FontProvider.GetRegularFont(size ?? control.Font.Size);
            return control;
        }

        public static Control ApplyBoldFont(this Control control, float? size = null)
        {
            control.Font = FontProvider.GetBoldFont(size ?? control.Font.Size);
            return control;
        }
    }
}
