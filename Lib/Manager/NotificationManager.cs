using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using BatteryNotifier.Lib.Services;
using BatteryNotifier.Properties;
using BatteryNotifier.Utils;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Lib.Manager
{
    public class NotificationManager(SoundManager soundManager, NotifyIcon notifyIcon) : IDisposable
    {
        private const int NOTIFICATION_COOLDOWN_MS = 5000;
        
        private readonly Debouncer _debouncer = new();
        private bool _disposed;

        public async Task EmitGlobalNotification(NotificationMessage notificationMessage)
        {
            if(notificationMessage.Type == NotificationType.Inline) return;
                
            if (appSetting.Default.lowBatteryNotification)
            {
                ShowBalloonTip( "Low Battery", notificationMessage.Message);
                await soundManager.PlaySoundAsync(appSetting.Default.lowBatteryNotificationMusic, Resources.LowBatterySound, true);
            }
            
            if (appSetting.Default.fullBatteryNotification)
            {
                ShowBalloonTip("Full Battery", notificationMessage.Message);
                await soundManager.PlaySoundAsync(appSetting.Default.fullBatteryNotificationMusic, Resources.LowBatterySound, true);
            }
        }

        private void ShowBalloonTip(string title, string message)
        {
            try
            {
                notifyIcon.ShowBalloonTip(NOTIFICATION_COOLDOWN_MS, title, message, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                //TODO : use internal notification service to log errors
                //NotificationService.Instance.PublishNotification($"Error showing balloon tip: {ex.Message}");
            }
        }

        private void StopAllNotifications()
        {
            soundManager?.StopSound();
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