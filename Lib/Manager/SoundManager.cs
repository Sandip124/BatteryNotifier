using System;
using System.IO;
using System.Media;
using System.Windows.Forms;
using BatteryNotifier.Utils;

namespace BatteryNotifier.Lib.Manager
{
    public class SoundManager : IDisposable
    {
        private readonly Timer _soundPlayingTimer;
        private readonly SoundPlayer _batteryNotification;
        private readonly CustomTimer.CustomTimer _customTimer;
        private bool _disposed;
        private readonly Debouncer _debouncer;
        
        private const int DEFAULT_MUSIC_PLAYING_DURATION = 30;
        private const int DEFAULT_SOUND_PLAYING_INTERVAL = 1000;

        public SoundManager()
        {
            _soundPlayingTimer = new Timer();
            _batteryNotification = new SoundPlayer();
            _customTimer = new CustomTimer.CustomTimer();
            _debouncer = new Debouncer();
            ConfigureTimer();
        }

        private void ConfigureTimer()
        {
            _soundPlayingTimer.Enabled = true;
            _soundPlayingTimer.Interval = DEFAULT_SOUND_PLAYING_INTERVAL;
            _soundPlayingTimer.Tick += SoundPlayingTimer_Tick;
        }

        public void PlaySound(string source, UnmanagedMemoryStream fallbackSoundSource, bool loop = false)
        {
            try
            {
                _soundPlayingTimer.Start();

                if (!string.IsNullOrEmpty(source) && File.Exists(source))
                {
                    _batteryNotification.SoundLocation = source;
                }
                else
                {
                    _batteryNotification.Stream = fallbackSoundSource;
                }

                if (loop)
                {
                    _batteryNotification.PlayLooping();
                }
                else
                {
                    _batteryNotification.PlaySync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound: {ex.Message}");
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

            return File.Exists(fileName) ? fileName : string.Empty;
        }

        public void StopAllSounds()
        {
            try
            {
                _batteryNotification?.Stop();
                _soundPlayingTimer?.Stop();
                _customTimer?.ResetTimer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping sounds: {ex.Message}");
            }
        }

        private void SoundPlayingTimer_Tick(object? sender, EventArgs e)
        {
            if (_customTimer.TimerCount >= DEFAULT_MUSIC_PLAYING_DURATION)
            {
                StopAllSounds();
            }
            else
            {
                _customTimer.Increment();
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
                _soundPlayingTimer?.Stop();
                _soundPlayingTimer?.Dispose();
                _batteryNotification?.Stop();
                _batteryNotification?.Dispose();
                _customTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}