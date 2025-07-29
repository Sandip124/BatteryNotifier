using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BatteryNotifier.Lib.Services;
using BatteryNotifier.Utils;

namespace BatteryNotifier.Lib.Manager
{
    public class SoundManager : IDisposable
    {
        private const int DEFAULT_MUSIC_PLAYING_DURATION_MS = 30000;
        
        private readonly SoundPlayer _batteryNotificationPlayer = new();
        private CancellationTokenSource? _cancellationTokenSource = new();
        private bool _isPlaying;
        private bool _disposed;
        private readonly Debouncer _debouncer = new();

        public async Task PlaySoundAsync(string source, UnmanagedMemoryStream fallbackSoundSource, bool loop = false,
            int durationMs = DEFAULT_MUSIC_PLAYING_DURATION_MS)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SoundManager));

            if (_isPlaying) return;
            
            try
            {
                _isPlaying = true;

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                await SetupSoundSource(source, fallbackSoundSource);

                if (loop)
                {
                    await PlayLoopingWithTimeout(durationMs, _cancellationTokenSource.Token);
                }
                else
                {
                    await PlayOnceAsync(_cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // TODO: handle cancellation gracefully if needed
            }
            catch (Exception ex)
            {
                // TODO:
                Console.WriteLine($"Error playing sound: {ex.Message}");
            }
            finally
            {
                _isPlaying = false;
            }
        }

        private async Task SetupSoundSource(string source, UnmanagedMemoryStream fallbackSoundSource)
        {
            await Task.Run(() => SetupSoundSourceSync(source, fallbackSoundSource));
        }

        private void SetupSoundSourceSync(string source, UnmanagedMemoryStream fallbackSoundSource)
        {
            if (!string.IsNullOrEmpty(source) && File.Exists(source))
            {
                _batteryNotificationPlayer.SoundLocation = source;
                _batteryNotificationPlayer.Stream = null;
            }
            else
            {
                _batteryNotificationPlayer.SoundLocation = string.Empty;
                _batteryNotificationPlayer.Stream = fallbackSoundSource;
            }

            _batteryNotificationPlayer.LoadAsync();
        }

        private async Task PlayLoopingWithTimeout(int durationMs, CancellationToken cancellationToken)
        {
            var playTask = Task.Run(() => { _batteryNotificationPlayer.PlayLooping(); }, cancellationToken);
            var timeoutTask = Task.Delay(durationMs, cancellationToken);
            await Task.WhenAny(playTask, timeoutTask);
        }

        private async Task PlayOnceAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _batteryNotificationPlayer.PlaySync();
                }
            }, cancellationToken);
        }

        public void StopSound()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _batteryNotificationPlayer?.Stop();
                _isPlaying = false;
            }
            catch (Exception ex)
            {
                //TODO: internal notification service can be used to log errors
                Console.WriteLine($"Error stopping sound: {ex.Message}");
            }
        }

        public string BrowseForSoundFile()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SoundManager));
            
            string fileName;

            using (var fileBrowser = new OpenFileDialog
                   {
                       DefaultExt = "wav",
                       Filter = @"Wave files (*.wav)|*.wav|All files (*.*)|*.*",
                       Title = @"Select Sound File"
                   })
            {
                if (fileBrowser.ShowDialog() != DialogResult.OK)
                    return string.Empty;
                
                fileName = fileBrowser.FileName;
            }

            // Force a garbage collection to reclaim dialog resources
            _debouncer.Debounce(() => 
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            });
            
            if (!UtilityHelper.IsValidWavFile(fileName))
            {
                NotificationService.Instance.PublishNotification("Only .wav file is supported.", NotificationType.Inline);
                return string.Empty;
            }

            return File.Exists(fileName) ? fileName.Trim() : string.Empty;
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
                StopSound();

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _batteryNotificationPlayer?.Stop();
                _batteryNotificationPlayer?.Dispose();
                _debouncer?.Dispose();
        
                _disposed = true;
            }
        }
    }
}