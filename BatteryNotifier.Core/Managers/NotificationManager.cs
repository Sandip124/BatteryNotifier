using System;
using System.Threading.Tasks;
using System.Timers;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Utils;
using Timer = System.Timers.Timer;

namespace BatteryNotifier.Core.Managers
{
    public class NotificationManager : IDisposable
    {
        private readonly SoundManager _soundManager;
        private readonly Debouncer _debouncer = new();
        private readonly Timer _pendingNotificationTimer;
        private bool _disposed;

        public NotificationManager(SoundManager soundManager)
        {
            _soundManager = soundManager;

            _pendingNotificationTimer = new Timer(3000);
            _pendingNotificationTimer.Elapsed += OnPendingNotificationTimerElapsed;
            _pendingNotificationTimer.AutoReset = true;
            _pendingNotificationTimer.Start();
        }

        private void OnPendingNotificationTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            NotificationService.Instance.FlushPendingNotifications();
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
                _pendingNotificationTimer?.Stop();
                _pendingNotificationTimer?.Dispose();

                _soundManager?.StopSound();
                _debouncer?.Dispose();
                _disposed = true;
            }
        }
    }
}
