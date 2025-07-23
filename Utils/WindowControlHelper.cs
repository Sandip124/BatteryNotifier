using System;
using System.Drawing;
using System.Windows.Forms;

namespace BatteryNotifier.Utils
{
    public static class WindowControlHelper
    {
        public static void RenderFormPosition(this Form form, NotifyIcon notifyIcon)
        {
            var settings = Setting.appSetting.Default;
            var workingArea = Screen.GetWorkingArea(form);

            if (settings.PinToNotificationArea)
            {
                form.Location = new Point(
                    workingArea.Right - form.Width,
                    workingArea.Bottom - form.Height);

                if (form.ShowInTaskbar) form.ShowInTaskbar = false;
                if (form.ShowIcon) form.ShowIcon = false;
            }
            else
            {
                var x = settings.WindowPositionX;
                var y = settings.WindowPositionY;

                var clampedX = Math.Max(workingArea.Left, Math.Min(x, workingArea.Right - form.Width));
                var clampedY = Math.Max(workingArea.Top, Math.Min(y, workingArea.Bottom - form.Height));
                form.Location = new Point(clampedX, clampedY);

                if (!form.ShowInTaskbar) form.ShowInTaskbar = true;
                if (!form.ShowIcon) form.ShowIcon = true;
            }

            if (!notifyIcon.Visible)
                notifyIcon.Visible = true;
        }
    }
}