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

        public async Task EmitGlobalNotification(NotificationMessageEventArgs notificationMessageEventArgs,
            Func<Task>? showNotification = null)
        {
            if (notificationMessageEventArgs.Type == NotificationType.Inline) return;

            if (showNotification != null)
            {
                await showNotification().ConfigureAwait(false);
            }

            // Look up sound from the alert that triggered this notification
            var tag = notificationMessageEventArgs.Tag;
            string? sound = null;

            if (!string.IsNullOrEmpty(tag))
            {
                var alert = AppSettings.Instance.Alerts.Find(a => a.Id == tag);
                if (alert != null)
                {
                    sound = alert.Sound;
                }
            }

            // Fallback to legacy settings for backward compatibility
            if (string.IsNullOrEmpty(sound))
            {
                var settings = AppSettings.Instance;
                if (tag == Constants.LowBatteryTag)
                    sound = settings.LowBatteryNotificationMusic;
                else if (tag == Constants.FullBatteryTag)
                    sound = settings.FullBatteryNotificationMusic;
            }

            if (!string.IsNullOrEmpty(sound))
            {
                // Loop all sounds for the notification duration — short sounds repeat,
                // long sounds get cut at the deadline. StopSound() ends playback on dismiss.
                await _soundManager.PlaySoundAsync(sound, loop: true,
                    durationMs: Constants.NotificationDurationMs).ConfigureAwait(false);
            }
        }

        public void StopSound() => _soundManager.StopSound();

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
