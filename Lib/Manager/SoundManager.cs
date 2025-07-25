using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BatteryNotifier.Utils;

namespace BatteryNotifier.Lib.Manager
{
    public class SoundManager : IDisposable
    {
        private readonly SoundPlayer _batteryNotification;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPlaying;
        private bool _disposed;
        private readonly Debouncer _debouncer;

        private const int DEFAULT_MUSIC_PLAYING_DURATION_MS = 30000;

        public bool IsPlaying => _isPlaying;

        public SoundManager()
        {
            _batteryNotification = new SoundPlayer();
            _cancellationTokenSource = new CancellationTokenSource();
            _debouncer = new Debouncer();
        }

        public async Task PlaySoundAsync(string source, UnmanagedMemoryStream fallbackSoundSource, bool loop = false,
            int durationMs = DEFAULT_MUSIC_PLAYING_DURATION_MS)
        {
            if (_disposed || _isPlaying) return;

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound: {ex.Message}");
            }
            finally
            {
                _isPlaying = false;
            }
        }

        public void PlaySoundSync(string source, UnmanagedMemoryStream fallbackSoundSource, bool loop = false,
            int durationMs = DEFAULT_MUSIC_PLAYING_DURATION_MS)
        {
            if (_disposed || _isPlaying) return;

            try
            {
                _isPlaying = true;

                SetupSoundSourceSync(source, fallbackSoundSource);

                if (loop)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            _batteryNotification.PlayLooping();
                            await Task.Delay(durationMs, _cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        finally
                        {
                            StopSound();
                        }
                    }, _cancellationTokenSource.Token);
                }
                else
                {
                    _batteryNotification.PlaySync();
                    _isPlaying = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound sync: {ex.Message}");
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
                _batteryNotification.SoundLocation = source;
                _batteryNotification.Stream = null;
            }
            else
            {
                _batteryNotification.SoundLocation = null;
                _batteryNotification.Stream = fallbackSoundSource;
            }

            _batteryNotification.LoadAsync();
        }

        private async Task PlayLoopingWithTimeout(int durationMs, CancellationToken cancellationToken)
        {
            var playTask = Task.Run(() => { _batteryNotification.PlayLooping(); }, cancellationToken);

            var timeoutTask = Task.Delay(durationMs, cancellationToken);

            await Task.WhenAny(playTask, timeoutTask);
        }

        private async Task PlayOnceAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _batteryNotification.PlaySync();
                }
            }, cancellationToken);
        }

        public void StopSound()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _batteryNotification?.Stop();
                _isPlaying = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping sound: {ex.Message}");
            }
        }

        public string BrowseForSoundFile(Label notificationLabel)
        {
            using var fileBrowser = new OpenFileDialog
            {
                DefaultExt = "wav",
                Filter = "Wave files (*.wav)|*.wav|All files (*.*)|*.*",
                Title = "Select Sound File"
            };

            if (fileBrowser.ShowDialog() != DialogResult.OK)
                return string.Empty;

            var fileName = fileBrowser.FileName;

            if (!UtilityHelper.IsValidWavFile(fileName))
            {
                notificationLabel.Text = "Only .wav file is supported.";
                _debouncer.Debounce(() => { notificationLabel.Text = string.Empty; }, 3000);
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

                _batteryNotification?.Stop();
                _batteryNotification?.Dispose();
                _debouncer.Dispose();
                _disposed = true;
            }
        }
    }
}