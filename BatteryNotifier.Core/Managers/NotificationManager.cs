using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Utils;

namespace BatteryNotifier.Core.Managers
{
    public class NotificationManager(SoundManager soundManager) : IDisposable
    {
        private readonly Debouncer _debouncer = new();
        private bool _disposed;

        public async Task EmitGlobalNotification(NotificationMessage notificationMessage, Func<Action> showNotifcation)
        {
            if (notificationMessage.Type == NotificationType.Inline) return;

            if (notificationMessage.Tag == Constants.LowBatteryTag)
            {
                // if (appSetting.Default.lowBatteryNotification)
                // {
                //     //ShowBalloonTip("Low Battery", notificationMessage.Message);
                //     showNotifcation();
                //     await soundManager.PlaySoundAsync(appSetting.Default.lowBatteryNotificationMusic,
                //         Resources.LowBatterySound, true);
                // }
            }
            else if (notificationMessage.Tag == Constants.FullBatteryTag)
            {
                // if (appSetting.Default.fullBatteryNotification)
                // {
                //     //ShowBalloonTip("Full Battery", notificationMessage.Message);
                //     showNotifcation();
                //     await soundManager.PlaySoundAsync(appSetting.Default.fullBatteryNotificationMusic,
                //         Resources.BatteryFull, true);
                // }
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