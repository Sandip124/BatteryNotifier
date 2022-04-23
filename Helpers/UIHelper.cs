using System;
using System.Drawing;
using System.Windows.Forms;

namespace BatteryNotifier.Helpers
{
    public static class UiHelper
    {
        public static void ShowModal(this Form form,bool showAsModal)
        {
            if (!showAsModal)
            {
                Rectangle workingArea = Screen.GetWorkingArea(form);
                form.Location = new Point(workingArea.Right - form.Size.Width,
                                          workingArea.Bottom - form.Size.Height);
                form.Update();
                form.ShowInTaskbar = false;
                form.ShowIcon = false;
            }
            else
            {
                form.ShowInTaskbar = true;
                form.ShowIcon = true;

                var xPosition = Setting.appSetting.Default.WindowPositionX;
                var yPosition = Setting.appSetting.Default.WindowPositionY;

                form.Location = new Point(xPosition, yPosition);
                form.StartPosition = FormStartPosition.CenterScreen;
            }
        }
    }
}
