using System;
using System.Windows.Forms;
using BatteryNotifier.Properties;
using BatteryNotifier.Utils;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Lib.Manager
{
    public class NotificationManager : IDisposable
    {
        private readonly Debouncer _debouncer;
        private readonly SoundManager _soundManager;
        private bool _disposed;

        private const int DEFAULT_NOTIFICATION_TIMEOUT = 3000;

        public NotificationManager(SoundManager soundManager)
        {
            _debouncer = new Debouncer();
            _soundManager = soundManager;
        }

        public void ShowInAppNotification(Label notificationLabel, string status, int timeout = DEFAULT_NOTIFICATION_TIMEOUT)
        {
            notificationLabel.Text = status;
            _debouncer.Debounce(() => { notificationLabel.Text = string.Empty; }, timeout);
        }

        public void CheckAndShowNotifications(PowerStatus powerStatus, NotifyIcon notifyIcon)
        {
            CheckFullBatteryNotification(powerStatus, notifyIcon);
            CheckLowBatteryNotification(powerStatus, notifyIcon);
        }

        private void CheckFullBatteryNotification(PowerStatus powerStatus, NotifyIcon notifyIcon)
        {
            if (!ShouldShowFullBatteryNotification(powerStatus)) return;

            const string message = "🔋 Battery is full please unplug the charger.";
            
            if (appSetting.Default.fullBatteryNotification)
            {
                ShowBalloonTip(notifyIcon, "Full Battery", message);
                _soundManager.PlaySound(appSetting.Default.fullBatteryNotificationMusic, Resources.BatteryFull, true);
            }
        }

        private void CheckLowBatteryNotification(PowerStatus powerStatus, NotifyIcon notifyIcon)
        {
            if (!ShouldShowLowBatteryNotification(powerStatus)) return;

            const string message = "🔋 Battery is low, please Connect to Charger.";
            
            if (appSetting.Default.lowBatteryNotification)
            {
                ShowBalloonTip(notifyIcon, "Low Battery", message);
                _soundManager.PlaySound(appSetting.Default.lowBatteryNotificationMusic, Resources.LowBatterySound, true);
            }
        }

        private bool ShouldShowFullBatteryNotification(PowerStatus powerStatus)
        {
            return powerStatus.PowerLineStatus == PowerLineStatus.Online &&
                   powerStatus.BatteryLifePercent >= (float)appSetting.Default.fullBatteryNotificationValue / 100;
        }

        private bool ShouldShowLowBatteryNotification(PowerStatus powerStatus)
        {
            return powerStatus.PowerLineStatus != PowerLineStatus.Online &&
                   powerStatus.BatteryLifePercent <= (float)appSetting.Default.lowBatteryNotificationValue / 100;
        }

        private void ShowBalloonTip(NotifyIcon notifyIcon, string title, string message)
        {
            notifyIcon.ShowBalloonTip(50, title, message, ToolTipIcon.Info);
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