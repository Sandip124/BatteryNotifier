using System.Diagnostics;
using BatteryNotifier.Core.Logger;
using Serilog;
#if WINDOWS
using NAudio.Wave;
#endif

namespace BatteryNotifier.Core.Managers
{
    /// <summary>
    /// Cross-platform audio playback.
    /// - macOS: afplay subprocess (ArgumentList for injection safety)
    /// - Non-Windows: SoundFlow (MiniAudio backend)
    /// - Windows: SoundFlow (MiniAudio backend)
    /// </summary>
    public class SoundManager : IDisposable
    {
        private const int DefaultPlayDurationMs = 30000;

        private readonly ILogger _logger;
        private readonly Lock _playLock = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private Process? _currentProcess;
        private bool _isPlaying;
        private volatile bool _disposed;

        public SoundManager()
        {
            _logger = BatteryNotifierAppLogger.ForContext<SoundManager>();
        }

        public async Task PlaySoundAsync(string? source, bool loop = false,
            int durationMs = DefaultPlayDurationMs)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SoundManager));

            var resolvedPath = ResolveSoundPath(source);
            if (resolvedPath == null) return;

            if (!ValidateSoundFile(resolvedPath)) return;

            CancellationToken token;
            lock (_playLock)
            {
                if (_isPlaying) return;
                _isPlaying = true;

                _cancellationTokenSource?.CancelAsync();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                token = _cancellationTokenSource.Token;
            }

            try
            {
                await Task.Run(() => PlaySound(resolvedPath, loop, durationMs, token), token).ConfigureAwait(false);
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
                lock (_playLock)
                {
                    _isPlaying = false;
                }
            }
        }

        /// <summary>
        /// Resolves a sound source (builtin:, bundled:, custom:, or absolute path) to a canonical file path.
        /// </summary>
        private string? ResolveSoundPath(string? source)
        {
            var resolvedPath = BuiltInSounds.Resolve(source);
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath)) return null;

            // Canonicalize path — on macOS /var is a symlink to /private/var,
            // so GetTempPath() returns /var/... but GetFullPath() resolves to /private/var/...
            try
            {
                resolvedPath = Path.GetFullPath(resolvedPath);
            }
            catch
            {
                _logger.Warning("Failed to canonicalize sound file path: {Path}", resolvedPath);
                return null;
            }

            if (!Path.IsPathRooted(resolvedPath) || !File.Exists(resolvedPath))
            {
                _logger.Warning("Rejected invalid sound file path: {Path}", resolvedPath);
                return null;
            }

            return resolvedPath;
        }

        /// <summary>
        /// Validates a resolved sound file: rejects symlinks and oversized files.
        /// </summary>
        private bool ValidateSoundFile(string path)
        {
            var fileInfo = new FileInfo(path);

            if (fileInfo.LinkTarget != null)
            {
                _logger.Warning("Rejected symlink sound file path: {Path}", path);
                return false;
            }

            if (fileInfo.Length > 50 * 1024 * 1024)
            {
                _logger.Warning("Rejected oversized sound file ({Size} bytes): {Path}", fileInfo.Length, path);
                return false;
            }

            return true;
        }

        private void PlaySound(string source, bool loop, int durationMs, CancellationToken token)
        {
            if (OperatingSystem.IsMacOS())
                PlayWithSubprocess("afplay", null, source, loop, durationMs, token);
#if WINDOWS
            else if (OperatingSystem.IsWindows())
                PlayWithNAudio(source, loop, durationMs, token);
#else
            else if (OperatingSystem.IsLinux())
                PlayOnLinux(source, loop, durationMs, token);
#endif
            else
                _logger.Warning("Unsupported platform for sound playback");
        }

        // ── Subprocess playback (macOS + Linux) ──────────────────────

        private void PlayWithSubprocess(string command, string[]? extraArgs, string source,
            bool loop, int durationMs, CancellationToken token)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);

            do
            {
                token.ThrowIfCancellationRequested();

                if (!RunSubprocessOnce(command, extraArgs, source, deadline, token))
                    return; // Process failed — don't retry

            } while (loop && !token.IsCancellationRequested && DateTime.UtcNow < deadline);
        }

        /// <summary>Runs one playback subprocess. Returns true if it completed normally.</summary>
        private bool RunSubprocessOnce(string command, string[]? extraArgs, string source,
            DateTime deadline, CancellationToken token)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Constants.ResolveCommand(command),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (extraArgs != null)
            {
                foreach (var arg in extraArgs)
                    psi.ArgumentList.Add(arg);
            }
            psi.ArgumentList.Add(source);

            using var process = new Process { StartInfo = psi };
            _currentProcess = process;

            try
            {
                process.Start();
                return WaitForProcessOrCancel(process, deadline, token);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Subprocess sound playback failed ({Command})", command);
                return false;
            }
            finally
            {
                _currentProcess = null;
            }
        }

        private static bool WaitForProcessOrCancel(Process process, DateTime deadline, CancellationToken token)
        {
            while (!process.WaitForExit(200))
            {
                if (token.IsCancellationRequested || DateTime.UtcNow >= deadline)
                {
                    KillProcess(process);
                    return false;
                }
            }
            return true;
        }

#if WINDOWS
        // ── Windows: NAudio (WaveOutEvent + AudioFileReader) ──────────

        private WaveOutEvent? _naudioDevice;
        private AudioFileReader? _naudioReader;

        private void PlayWithNAudio(string source, bool loop, int durationMs, CancellationToken token)
        {
            using var reader = new AudioFileReader(source);
            using var device = new WaveOutEvent();
            using var playbackDone = new ManualResetEventSlim(false);

            WaveStream inputStream = loop ? new LoopStream(reader) : reader;

            device.Init(inputStream);
            device.PlaybackStopped += (_, _) =>
            {
                try { playbackDone.Set(); } catch (ObjectDisposedException) { }
            };

            _naudioDevice = device;
            _naudioReader = reader;
            device.Play();

            try
            {
                playbackDone.Wait(durationMs, token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                device.Stop();
                _naudioDevice = null;
                _naudioReader = null;
                if (loop) inputStream.Dispose();
            }
        }

        /// <summary>
        /// WaveStream wrapper that loops back to the start when the source ends.
        /// Standard NAudio pattern from Mark Heath (NAudio author).
        /// </summary>
        private sealed class LoopStream : WaveStream
        {
            private readonly WaveStream _source;

            public LoopStream(WaveStream source) => _source = source;
            public override WaveFormat WaveFormat => _source.WaveFormat;
            public override long Length => _source.Length;
            public override long Position
            {
                get => _source.Position;
                set => _source.Position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int totalRead = 0;
                while (totalRead < count)
                {
                    int read = _source.Read(buffer, offset + totalRead, count - totalRead);
                    if (read == 0)
                    {
                        if (_source.Position == 0) break; // empty stream
                        _source.Position = 0; // loop
                    }
                    totalRead += read;
                }
                return totalRead;
            }
        }
#else
        // ── Linux: try SoundFlow, fall back to subprocess ──────────────────────────

        private void PlayOnLinux(string source, bool loop, int durationMs, CancellationToken token)
        {
            var (command, extraArgs) = FindLinuxAudioCommand(source);
            if (command != null)
                PlayWithSubprocess(command, extraArgs, source, loop, durationMs, token);
            else
                _logger.Warning("No audio playback command found on Linux (tried paplay, pw-play, aplay, mpv, ffplay)");
        }

        // Cached available commands — detected once, reused for all playback
        private static (string cmd, string[]? args)? _linuxWavPlayer;
        private static (string cmd, string[]? args)? _linuxCompressedPlayer;
        private static bool _linuxAudioScanned;

        private static (string? command, string[]? extraArgs) FindLinuxAudioCommand(string source)
        {
            if (!_linuxAudioScanned)
                ScanLinuxAudioCommands();

            var ext = Path.GetExtension(source).ToLowerInvariant();
            var needsDecoder = ext is ".mp3" or ".m4a" or ".aac" or ".ogg" or ".flac" or ".wma";

            if (needsDecoder)
                return _linuxCompressedPlayer ?? _linuxWavPlayer ?? (null, null);

            return _linuxWavPlayer ?? _linuxCompressedPlayer ?? (null, null);
        }

        private static void ScanLinuxAudioCommands()
        {
            _linuxAudioScanned = true;

            _linuxWavPlayer = FindFirstAvailable(
                ("paplay", null), ("pw-play", null), ("aplay", new[] { "-q" }));

            _linuxCompressedPlayer = FindFirstAvailable(
                ("mpv", new[] { "--no-video", "--really-quiet" }),
                ("ffplay", new[] { "-nodisp", "-autoexit", "-loglevel", "quiet" }));
        }

        private static (string cmd, string[]? args)? FindFirstAvailable(
            params (string cmd, string[]? args)[] candidates)
        {
            foreach (var entry in candidates)
            {
                if (!Path.IsPathRooted(Constants.ResolveCommand(entry.cmd))) continue;
                return entry;
            }
            return null;
        }

#endif

        // ── Stop / Dispose ──────────────────────────────────────────

        public void StopSound()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                KillProcess(_currentProcess);
#if WINDOWS
                _naudioDevice?.Stop();
#endif
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while stopping sound playback.");
            }
        }

        private static void KillProcess(Process? process)
        {
            if (process == null) return;
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch { /* best effort */ }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            _disposed = true;

            StopSound();
            lock (_playLock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }

            // Dispose process handle if still held
            try { _currentProcess?.Dispose(); } catch { /* best effort */ }
            _currentProcess = null;

#if WINDOWS
            // NAudio device and reader are normally disposed by PlayWithNAudio's using blocks,
            // but if Dispose() is called while playback is active, they may still be held.
            try { _naudioDevice?.Dispose(); } catch { /* best effort */ }
            try { _naudioReader?.Dispose(); } catch { /* best effort */ }
            _naudioDevice = null;
            _naudioReader = null;
#endif
        }
    }
}
