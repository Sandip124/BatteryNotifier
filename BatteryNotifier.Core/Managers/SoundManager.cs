using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BatteryNotifier.Core.Logger;
#if WINDOWS
using NAudio.Wave;
#endif
using Serilog;

namespace BatteryNotifier.Core.Managers
{
    public class SoundManager : IDisposable
    {
        private const int DefaultPlayDurationMs = 30000;

        private readonly ILogger _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        private volatile bool _isPlaying;
        private volatile bool _disposed;

        public SoundManager()
        {
            _logger = BatteryNotifierAppLogger.ForContext<SoundManager>();
        }

        public async Task PlaySoundAsync(string? source, bool loop = false,
            int durationMs = DefaultPlayDurationMs)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SoundManager));
            if (_isPlaying) return;

            // Resolve built-in sounds to their cached WAV file paths
            var resolvedPath = BuiltInSounds.Resolve(source);
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath)) return;

            // Validate the path is a real, rooted file path (no command injection via crafted paths)
            if (!Path.IsPathRooted(resolvedPath) || Path.GetFullPath(resolvedPath) != resolvedPath)
            {
                _logger.Warning("Rejected non-canonical sound file path: {Path}", resolvedPath);
                return;
            }

            // Reject symlinks — prevents reading arbitrary files via afplay/paplay
            var fileInfo = new FileInfo(resolvedPath);
            if (fileInfo.LinkTarget != null)
            {
                _logger.Warning("Rejected symlink sound file path: {Path}", resolvedPath);
                return;
            }

            // Reject files larger than 50 MB to prevent OOM in NAudio
            if (fileInfo.Length > 50 * 1024 * 1024)
            {
                _logger.Warning("Rejected oversized sound file ({Size} bytes): {Path}", fileInfo.Length, resolvedPath);
                return;
            }

            source = resolvedPath;

            var oldCts = _cancellationTokenSource;
            oldCts?.Cancel();
            oldCts?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _isPlaying = true;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
#if WINDOWS
                    await PlayWithNAudio(source, loop, durationMs, token);
#endif
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

#if WINDOWS
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
#endif

        private async Task PlayWithAfplay(string source, bool loop, int durationMs, CancellationToken token)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);
            do
            {
                await RunProcess("afplay", source, token);
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
                    await RunProcess("paplay", source, token);
                }
                catch
                {
                    await RunProcess("aplay", source, token);
                }
            } while (loop && !token.IsCancellationRequested && DateTime.UtcNow < deadline);
        }

        /// <summary>
        /// Runs a subprocess using ArgumentList (not Arguments string) to prevent shell injection.
        /// </summary>
        private static async Task RunProcess(string command, string filePath, CancellationToken token)
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            // ArgumentList passes the path as a single, properly-escaped argument
            psi.ArgumentList.Add(filePath);

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

            _disposed = true;
        }
    }
}
