using System;
using System.Drawing;
using System.Windows.Forms;

namespace BatteryNotifier.Utils
{
    public static class WindowControlHelper
    {
        public static void RenderFormPosition(this Form form, NotifyIcon notifyIcon)
        {
            var workingArea = Screen.GetWorkingArea(form);
            
            var x = Setting.appSetting.Default.WindowPositionX;
            var y = Setting.appSetting.Default.WindowPositionY;

            var clampedX = Math.Max(workingArea.Left, Math.Min(x, workingArea.Right - form.Width));
            var clampedY = Math.Max(workingArea.Top, Math.Min(y, workingArea.Bottom - form.Height));
            form.Location = new Point(clampedX, clampedY);
        }
    }
}