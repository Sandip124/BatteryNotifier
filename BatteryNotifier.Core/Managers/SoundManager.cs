using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Utils;
using NAudio.Wave;
using Serilog;

namespace BatteryNotifier.Core.Managers
{
    public class SoundManager : IDisposable
    {
        private const int DefaultPlayDurationMs = 30000;

        private readonly ILogger _logger;
        private readonly Debouncer _debouncer = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isPlaying;
        private bool _disposed;

        public SoundManager()
        {
            _logger = BatteryNotifierAppLogger.ForContext<SoundManager>();
        }

        public async Task PlaySoundAsync(string? source, bool loop = false,
            int durationMs = DefaultPlayDurationMs)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SoundManager));
            if (_isPlaying) return;
            if (string.IsNullOrEmpty(source) || !File.Exists(source)) return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _isPlaying = true;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    await PlayWithNAudio(source, loop, durationMs, token);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    await PlayWithAfplay(source, loop, durationMs, token);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    await PlayWithAplay(source, loop, durationMs, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelled — expected
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while playing sound.");
            }
            finally
            {
                _isPlaying = false;
            }
        }

        private async Task PlayWithNAudio(string source, bool loop, int durationMs, CancellationToken token)
        {
            using var audioFile = new AudioFileReader(source);
            using var outputDevice = new WaveOutEvent();

            outputDevice.Init(audioFile);
            outputDevice.Play();

            var timeout = Task.Delay(durationMs, token);

            if (loop)
            {
                outputDevice.PlaybackStopped += (s, e) =>
                {
                    if (!token.IsCancellationRequested && _isPlaying)
                    {
                        audioFile.Position = 0;
                        outputDevice.Play();
                    }
                };
                await timeout;
            }
            else
            {
                var playbackDone = new TaskCompletionSource<bool>();
                outputDevice.PlaybackStopped += (s, e) => playbackDone.TrySetResult(true);
                await Task.WhenAny(playbackDone.Task, timeout);
            }

            outputDevice.Stop();
        }

        private async Task PlayWithAfplay(string source, bool loop, int durationMs, CancellationToken token)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);
            do
            {
                await RunProcess("afplay", $"\"{source}\"", token);
            } while (loop && !token.IsCancellationRequested && DateTime.UtcNow < deadline);
        }

        private async Task PlayWithAplay(string source, bool loop, int durationMs, CancellationToken token)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);
            do
            {
                // Try paplay first (PulseAudio), fall back to aplay (ALSA)
                try
                {
                    await RunProcess("paplay", $"\"{source}\"", token);
                }
                catch
                {
                    await RunProcess("aplay", $"\"{source}\"", token);
                }
            } while (loop && !token.IsCancellationRequested && DateTime.UtcNow < deadline);
        }

        private static async Task RunProcess(string command, string arguments, CancellationToken token)
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var tcs = new TaskCompletionSource<bool>();
            process.Exited += (s, e) => tcs.TrySetResult(true);

            process.Start();

            using var reg = token.Register(() =>
            {
                try { process.Kill(); } catch { }
                tcs.TrySetCanceled();
            });

            await tcs.Task;
        }

        public void StopSound()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _isPlaying = false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while stopping sound playback.");
            }
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
            _debouncer?.Dispose();

            _disposed = true;
        }
    }
}
