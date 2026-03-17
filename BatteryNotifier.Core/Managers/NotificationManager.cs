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
                await showNotification().ConfigureAwait(false);
            }

            var settings = AppSettings.Instance;

            if (notificationMessage.Tag == Constants.LowBatteryTag)
            {
                if (settings.LowBatteryNotification &&
                    !string.IsNullOrEmpty(settings.LowBatteryNotificationMusic))
                {
                    // Built-in tones are short — loop them. Custom sounds play once in full.
                    bool shouldLoop = BuiltInSounds.IsBuiltIn(settings.LowBatteryNotificationMusic);
                    await _soundManager.PlaySoundAsync(settings.LowBatteryNotificationMusic, loop: shouldLoop).ConfigureAwait(false);
                }
            }
            else if (notificationMessage.Tag == Constants.FullBatteryTag)
            {
                if (settings.FullBatteryNotification &&
                    !string.IsNullOrEmpty(settings.FullBatteryNotificationMusic))
                {
                    bool shouldLoop = BuiltInSounds.IsBuiltIn(settings.FullBatteryNotificationMusic);
                    await _soundManager.PlaySoundAsync(settings.FullBatteryNotificationMusic, loop: shouldLoop).ConfigureAwait(false);
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
