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

            if (Setting.appSetting.Default.PinToNotificationArea)
            {
                form.Location = new Point(
                    workingArea.Right - form.Width,
                    workingArea.Bottom - form.Height);

                form.ShowInTaskbar = false;
                form.ShowIcon = false;
            }
            else
            {
                var x = Setting.appSetting.Default.WindowPositionX;
                var y = Setting.appSetting.Default.WindowPositionY;

                var clampedX = Math.Max(workingArea.Left, Math.Min(x, workingArea.Right - form.Width));
                var clampedY = Math.Max(workingArea.Top, Math.Min(y, workingArea.Bottom - form.Height));
                form.Location = new Point(clampedX, clampedY);

                form.ShowInTaskbar = true;
                form.ShowIcon = true;
            }

            notifyIcon.Visible = true;
        }
    }
}