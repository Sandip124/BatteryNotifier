using System.Drawing;
using System.Windows.Forms;

namespace BatteryNotifier.Helpers
{
    public static class WindowHelper
    {
        public static void RenderFormPosition(this Form form,NotifyIcon notifyIcon)
        {
            Rectangle workingArea = Screen.GetWorkingArea(form);

            var xPosition = Setting.appSetting.Default.WindowPositionX;
            var yPosition = Setting.appSetting.Default.WindowPositionY;

            var pinToNotificationArea = Setting.appSetting.Default.PinToNotificationArea;
            if(pinToNotificationArea)
            {
                form.Location = new Point(workingArea.Right - form.Size.Width,
                                          workingArea.Bottom - form.Size.Height);
                form.ShowInTaskbar = false;
                form.ShowIcon = false;
                notifyIcon.Visible = true;
            }
            else
            {
                form.ShowInTaskbar = true;
                form.ShowIcon = true;
                form.Location = new Point(xPosition, yPosition);
                notifyIcon.Visible = false;
            }
        }
    }
}
