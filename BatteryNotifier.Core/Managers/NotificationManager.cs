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

        /// <param name="onSoundStarted">Fired when audio actually starts, or immediately if none plays.</param>
        public async Task EmitGlobalNotification(NotificationMessageEventArgs notificationMessageEventArgs,
            Func<Task>? showNotification = null, Action? onSoundStarted = null)
        {
            if (notificationMessageEventArgs.Type == NotificationType.Inline)
            {
                onSoundStarted?.Invoke();
                return;
            }

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
                await _soundManager.PlaySoundAsync(sound, loop: true,
                    durationMs: Constants.NotificationDurationMs,
                    volumePercent: AppSettings.Instance.AlertVolume,
                    onStarted: onSoundStarted).ConfigureAwait(false);
            }
            else
            {
                onSoundStarted?.Invoke();
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
