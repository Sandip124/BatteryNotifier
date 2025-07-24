using System;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using BatteryNotifier.Utils;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Lib.Manager
{
    public class WindowManager : IDisposable
    {
        private readonly Debouncer _debouncer;
        private bool _disposed;
        private Point _lastLocation;
        private bool _mouseDown;
        private readonly Dashboard dashboard;

        public WindowManager(Dashboard dashboard)
        {
            this.dashboard = dashboard;
            _debouncer = new Debouncer();
        }

        public void RenderFormPosition(NotifyIcon notifyIcon)
        {
            dashboard.RenderFormPosition(notifyIcon);
        }

        public void RenderTitleBarCursor(Label appHeaderTitle)
        {
            appHeaderTitle.Cursor = appSetting.Default.PinToNotificationArea ? Cursors.Default : Cursors.SizeAll;
        }
        
        public void HandleCloseClick()
        {
            if (appSetting.Default.PinToNotificationArea)
            {
                dashboard.Hide();
            }
            else
            {
                dashboard.WindowState = FormWindowState.Minimized;
            }
        }

        public void HandleMouseDown(MouseEventArgs e)
        {
            if (appSetting.Default.PinToNotificationArea) return;

            _mouseDown = true;
            _lastLocation = e.Location;
        }

        public void HandleMouseMove(MouseEventArgs e)
        {
            if (appSetting.Default.PinToNotificationArea || !_mouseDown) return;

            var xPosition = dashboard.Location.X - _lastLocation.X + e.X;
            var yPosition = dashboard.Location.Y - _lastLocation.Y + e.Y;
            dashboard.Location = new Point(xPosition, yPosition);
            dashboard.Update();

            _debouncer.Debounce(() =>
            {
                try
                {
                    appSetting.Default.WindowPositionX = xPosition;
                    appSetting.Default.WindowPositionY = yPosition;
                    appSetting.Default.Save();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving window position: {ex.Message}");
                }
            }, 1000);
        }

        public void HandleMouseUp(MouseEventArgs e)
        {
            if (appSetting.Default.PinToNotificationArea) return;
            _mouseDown = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _debouncer?.Dispose();
                _disposed = true;
            }
        }
    }
}