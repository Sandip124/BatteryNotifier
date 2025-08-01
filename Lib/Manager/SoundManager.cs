using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BatteryNotifier.Lib.Logger;
using BatteryNotifier.Lib.Services;
using BatteryNotifier.Setting;
using BatteryNotifier.Utils;
using NAudio.Wave;
using Serilog;

namespace BatteryNotifier.Lib.Manager
{
    public class SoundManager : IDisposable
    {
        private const int DEFAULT_MUSIC_PLAYING_DURATION_MS = 30000;

        private readonly ILogger _logger;
        
        private readonly SoundPlayer _batteryNotificationPlayer = new();
        private CancellationTokenSource? _cancellationTokenSource = new();
        private bool _isPlaying;
        private bool _disposed;
        private readonly Debouncer _debouncer = new();

        public SoundManager()
        {
            _logger = BatteryNotifierAppLogger.ForContext<SoundManager>();
        }

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

                SetupSoundSource(source, fallbackSoundSource);

                if (loop)
                {
                    await PlayLoopingWithTimeout(durationMs, _cancellationTokenSource.Token);
                }
                else
                {
                    await PlayOnceAsync(_cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.Error(ex," Sound playback was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex," An error occurred while playing sound.");
            }
            finally
            {
                _isPlaying = false;
            }
        }

        private void SetupSoundSource(string source, UnmanagedMemoryStream fallbackSoundSource)
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
                _logger.Error(ex, "An error occurred while stopping sound playback.");
            }
        }

        public string BrowseForSoundFile(string soundFilePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SoundManager));

            string outputFileName = soundFilePath;

            try
            {
                using var fileBrowser = new OpenFileDialog();
                
                fileBrowser.DefaultExt = "wav";
                fileBrowser.Filter =
                    @"Audio files (*.wav;*.mp3;*.m4a;*.wma)|*.wav;*.mp3;*.m4a;*.wma|Wave files (*.wav)|*.wav|MP3 files (*.mp3)|*.mp3|All files (*.*)|*.*";
                fileBrowser.Title = @"Select Sound File";
                if (fileBrowser.ShowDialog() == DialogResult.OK)
                {
                    var newFileName = fileBrowser.FileName;

                    if (!UtilityHelper.IsValidWavFile(newFileName))
                    {
                        outputFileName = Path.ChangeExtension(newFileName, ".wav");
                    
                        using var reader = new MediaFoundationReader(newFileName);
                        WaveFileWriter.CreateWaveFile(outputFileName, reader);
                    }
                    else
                    {
                        outputFileName = newFileName;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, " An error occurred while browsing for sound file.");
                NotificationService.Instance.PublishNotification("💀 Error while browsing for sound file!",
                    NotificationType.Inline);
                return string.Empty;
            }
            return !string.IsNullOrEmpty(outputFileName) && File.Exists(outputFileName) ? outputFileName : string.Empty;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            
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