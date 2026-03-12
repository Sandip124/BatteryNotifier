using System;
using System.Threading.Tasks;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Core.Managers
{
    public class NotificationManager : IDisposable
    {
        private readonly SoundManager _soundManager;
        private bool _disposed;

        public NotificationManager(SoundManager soundManager)
        {
            _soundManager = soundManager;
        }

        public async Task EmitGlobalNotification(NotificationMessage notificationMessage,
            Func<Task>? showNotification = null)
        {
            if (notificationMessage.Type == NotificationType.Inline) return;

            if (showNotification != null)
            {
                await showNotification();
            }

            var settings = AppSettings.Instance;

            if (notificationMessage.Tag == Constants.LowBatteryTag)
            {
                if (settings.LowBatteryNotification &&
                    !string.IsNullOrEmpty(settings.LowBatteryNotificationMusic))
                {
                    await _soundManager.PlaySoundAsync(settings.LowBatteryNotificationMusic, loop: true);
                }
            }
            else if (notificationMessage.Tag == Constants.FullBatteryTag)
            {
                if (settings.FullBatteryNotification &&
                    !string.IsNullOrEmpty(settings.FullBatteryNotificationMusic))
                {
                    await _soundManager.PlaySoundAsync(settings.FullBatteryNotificationMusic, loop: true);
                }
            }
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
                _soundManager?.StopSound();
                _disposed = true;
            }
        }
    }
}
