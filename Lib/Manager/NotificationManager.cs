using System;
using System.Threading.Tasks;
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
        private bool _disposed = false;
        
        private bool _fullBatteryNotificationShown = false;
        private bool _lowBatteryNotificationShown = false;
        private DateTime _lastFullBatteryCheck = DateTime.MinValue;
        private DateTime _lastLowBatteryCheck = DateTime.MinValue;
        
        private const int DEFAULT_NOTIFICATION_TIMEOUT = 3000;
        private const int NOTIFICATION_COOLDOWN_MS = 60000;

        public NotificationManager(SoundManager soundManager)
        {
            _debouncer = new Debouncer();
            _soundManager = soundManager;
        }

        public void ShowInAppNotification(Label notificationLabel, string status, int timeout = DEFAULT_NOTIFICATION_TIMEOUT)
        {
            if (_disposed || notificationLabel == null || notificationLabel.IsDisposed) return;

            if (notificationLabel.InvokeRequired)
            {
                notificationLabel.Invoke(new Action(() => ShowInAppNotification(notificationLabel, status, timeout)));
                return;
            }

            notificationLabel.Text = status;
            _debouncer.Debounce(() => 
            { 
                if (!notificationLabel.IsDisposed)
                {
                    notificationLabel.Text = string.Empty; 
                }
            }, timeout);
        }

        public async Task CheckAndShowNotificationsAsync(PowerStatus powerStatus, NotifyIcon notifyIcon, Label notificationLabel = null)
        {
            if (_disposed) return;

            await CheckFullBatteryNotificationAsync(powerStatus, notifyIcon, notificationLabel);
            await CheckLowBatteryNotificationAsync(powerStatus, notifyIcon, notificationLabel);
        }

        public void CheckAndShowNotifications(PowerStatus powerStatus, NotifyIcon notifyIcon, Label notificationLabel = null)
        {
            _ = Task.Run(async () => await CheckAndShowNotificationsAsync(powerStatus, notifyIcon, notificationLabel));
        }

        private async Task CheckFullBatteryNotificationAsync(PowerStatus powerStatus, NotifyIcon notifyIcon, Label notificationLabel)
        {
            if (!ShouldShowFullBatteryNotification(powerStatus)) 
            {
                _fullBatteryNotificationShown = false;
                return;
            }

            if (_fullBatteryNotificationShown && 
                DateTime.Now - _lastFullBatteryCheck < TimeSpan.FromMilliseconds(NOTIFICATION_COOLDOWN_MS))
            {
                return;
            }

            const string message = "🔋 Battery is full please unplug the charger.";
            
            if (notificationLabel != null)
            {
                ShowInAppNotification(notificationLabel, message);
            }
            
            if (appSetting.Default.fullBatteryNotification)
            {
                ShowBalloonTip(notifyIcon, "Full Battery", message);
                
                var soundPath = appSetting.Default.fullBatteryNotificationMusic;
                await _soundManager.PlaySoundAsync(soundPath, Resources.BatteryFull, true, 30000);
            }

            _fullBatteryNotificationShown = true;
            _lastFullBatteryCheck = DateTime.Now;
        }

        private async Task CheckLowBatteryNotificationAsync(PowerStatus powerStatus, NotifyIcon notifyIcon, Label notificationLabel)
        {
            if (!ShouldShowLowBatteryNotification(powerStatus)) 
            {
                _lowBatteryNotificationShown = false;
                return;
            }

            if (_lowBatteryNotificationShown && 
                DateTime.Now - _lastLowBatteryCheck < TimeSpan.FromMilliseconds(NOTIFICATION_COOLDOWN_MS))
            {
                return;
            }

            const string message = "🔋 Battery is low, please Connect to Charger.";
            
            if (notificationLabel != null)
            {
                ShowInAppNotification(notificationLabel, message);
            }
            
            if (appSetting.Default.lowBatteryNotification)
            {
                ShowBalloonTip(notifyIcon, "Low Battery", message);
                
                var soundPath = appSetting.Default.lowBatteryNotificationMusic;
                await _soundManager.PlaySoundAsync(soundPath, Resources.LowBatterySound, true, 30000);
            }

            _lowBatteryNotificationShown = true;
            _lastLowBatteryCheck = DateTime.Now;
        }

        private bool ShouldShowFullBatteryNotification(PowerStatus powerStatus)
        {
            return powerStatus.PowerLineStatus == PowerLineStatus.Online &&
                   powerStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery &&
                   powerStatus.BatteryLifePercent >= (float)appSetting.Default.fullBatteryNotificationValue / 100;
        }

        private bool ShouldShowLowBatteryNotification(PowerStatus powerStatus)
        {
            return powerStatus.PowerLineStatus != PowerLineStatus.Online &&
                   powerStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery &&
                   powerStatus.BatteryLifePercent <= (float)appSetting.Default.lowBatteryNotificationValue / 100;
        }

        private void ShowBalloonTip(NotifyIcon notifyIcon, string title, string message)
        {
            try
            {
                if (notifyIcon != null)
                {
                    notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing balloon tip: {ex.Message}");
            }
        }

        public void StopAllNotifications()
        {
            _soundManager?.StopSound();
            _fullBatteryNotificationShown = false;
            _lowBatteryNotificationShown = false;
        }

        public void ResetNotificationCooldowns()
        {
            _fullBatteryNotificationShown = false;
            _lowBatteryNotificationShown = false;
            _lastFullBatteryCheck = DateTime.MinValue;
            _lastLowBatteryCheck = DateTime.MinValue;
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
                StopAllNotifications();
                _debouncer?.Dispose();
                _disposed = true;
            }
        }
    }
}