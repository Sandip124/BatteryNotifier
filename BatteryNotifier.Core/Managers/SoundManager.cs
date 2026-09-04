using System.Diagnostics;
using System.Globalization;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Utils;
using Serilog;
#if WINDOWS
using NAudio.Wave;
using NLayer.NAudioSupport;
#endif

namespace BatteryNotifier.Core.Managers
{
    /// <summary>
    /// Cross-platform audio playback.
    /// - macOS: afplay subprocess (ArgumentList for injection safety)
    /// - Linux: paplay / pw-play / aplay / mpv / ffplay subprocess
    /// - Windows: NAudio (WaveOutEvent + AudioFileReader)
    /// </summary>
    public class SoundManager : IDisposable
    {
        private const int DefaultPlayDurationMs = 30000;

        /// <summary>
        /// True only if the WINDOWS symbol was defined at build time (NAudio path compiled in).
        /// Surfaced in diagnostics so a Windows build missing sound support is obvious.
        /// </summary>
#if WINDOWS
        public const bool WindowsAudioCompiled = true;
#else
        public const bool WindowsAudioCompiled = false;
#endif

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

        /// <param name="onStarted">
        /// Fired exactly once: the moment audio actually starts, or immediately if it never does
        /// (muted, invalid file, error). Lets a caller sync UI to real playback start.
        /// </param>
        public async Task PlaySoundAsync(string? source, bool loop = false,
            int durationMs = DefaultPlayDurationMs, int volumePercent = 100, Action? onStarted = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SoundManager));

            var startedFired = 0;
            void FireStarted()
            {
                if (Interlocked.Exchange(ref startedFired, 1) == 0)
                    onStarted?.Invoke();
            }

            try
            {
                if (volumePercent <= 0) return;
                volumePercent = Math.Min(volumePercent, 100);

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

                await Task.Run(() => PlaySound(resolvedPath, loop, durationMs, volumePercent, token, FireStarted), token).ConfigureAwait(false);
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
                FireStarted();
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
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
            {
                _logger.Warning("Sound source did not resolve to an existing file: source={Source} resolved={Resolved}",
                    source, resolvedPath);
                return null;
            }

            // Canonicalize path — on macOS /var is a symlink to /private/var,
            // so GetTempPath() returns /var/... but GetFullPath() resolves to /private/var/...
            if (!FileSafety.TryCanonicalize(resolvedPath, out var canonical) || !File.Exists(canonical))
            {
                _logger.Warning("Rejected invalid sound file path: {Path}", resolvedPath);
                return null;
            }

            return canonical;
        }

        /// <summary>
        /// Validates a resolved sound file: rejects symlinks and oversized files.
        /// </summary>
        private bool ValidateSoundFile(string path)
        {
            var fileInfo = new FileInfo(path);

            if (FileSafety.IsSymlink(fileInfo))
            {
                _logger.Warning("Rejected symlink sound file path: {Path}", path);
                return false;
            }

            if (FileSafety.ExceedsMaxSize(fileInfo))
            {
                _logger.Warning("Rejected oversized sound file ({Size} bytes): {Path}", fileInfo.Length, path);
                return false;
            }

            return true;
        }

        private void PlaySound(string source, bool loop, int durationMs, int volumePercent, CancellationToken token, Action onStarted)
        {
            _logger.Information(
                "PlaySound: source={Source} loop={Loop} durMs={Dur} vol={Vol} os=[mac={Mac} win={Win} linux={Linux}] windowsAudioCompiled={WinAudio}",
                source, loop, durationMs, volumePercent,
                OperatingSystem.IsMacOS(), OperatingSystem.IsWindows(), OperatingSystem.IsLinux(), WindowsAudioCompiled);

            if (OperatingSystem.IsMacOS())
                PlayWithSubprocess("afplay", null, source, loop, durationMs, volumePercent, token, onStarted);
#if WINDOWS
            else if (OperatingSystem.IsWindows())
                PlayWithNAudio(source, loop, durationMs, volumePercent, token, onStarted);
#else
            else if (OperatingSystem.IsLinux())
                PlayOnLinux(source, loop, durationMs, volumePercent, token, onStarted);
#endif
            else
            {
                _logger.Warning("Unsupported platform for sound playback");
                onStarted();
            }
        }

        // ── Subprocess playback (macOS + Linux) ──────────────────────

        private void PlayWithSubprocess(string command, string[]? extraArgs, string source,
            bool loop, int durationMs, int volumePercent, CancellationToken token, Action onStarted)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);
            var firstAttempt = true;

            do
            {
                token.ThrowIfCancellationRequested();

                var startedSignal = firstAttempt ? onStarted : null;
                firstAttempt = false;

                if (!RunSubprocessOnce(command, extraArgs, source, volumePercent, deadline, token, startedSignal))
                    return; // Process failed — don't retry

            } while (loop && !token.IsCancellationRequested && DateTime.UtcNow < deadline);
        }

        /// <summary>Runs one playback subprocess. Returns true if it completed normally.</summary>
        private bool RunSubprocessOnce(string command, string[]? extraArgs, string source,
            int volumePercent, DateTime deadline, CancellationToken token, Action? onStarted)
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
            AddVolumeArgs(psi, command, volumePercent);
            psi.ArgumentList.Add(source);

            using var process = new Process { StartInfo = psi };
            _currentProcess = process;

            try
            {
                process.Start();
                onStarted?.Invoke();
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

        /// <summary>
        /// Adds the volume flag for the given playback command. No-op at full volume, or for
        /// commands without volume support (aplay) — those play at full unless muted (0 = skipped).
        /// </summary>
        private static void AddVolumeArgs(ProcessStartInfo psi, string command, int volumePercent)
        {
            if (volumePercent >= 100) return;

            var linear = volumePercent / 100.0;
            var inv = CultureInfo.InvariantCulture;

            switch (command)
            {
                case "afplay":  // macOS: -v <0.0–1.0+>
                    psi.ArgumentList.Add("-v");
                    psi.ArgumentList.Add(linear.ToString("0.###", inv));
                    break;
                case "paplay":  // PulseAudio: --volume in 0–65536 (65536 = 100%)
                    psi.ArgumentList.Add($"--volume={(int)Math.Round(linear * 65536)}");
                    break;
                case "pw-play": // PipeWire: --volume as a linear factor (1.0 = unmodified)
                    psi.ArgumentList.Add($"--volume={linear.ToString("0.###", inv)}");
                    break;
                case "mpv":     // --volume in 0–100
                    psi.ArgumentList.Add($"--volume={volumePercent.ToString(inv)}");
                    break;
                case "ffplay":  // -volume in 0–100
                    psi.ArgumentList.Add("-volume");
                    psi.ArgumentList.Add(volumePercent.ToString(inv));
                    break;
            }
        }

#if WINDOWS
        // ── Windows: NAudio (WaveOutEvent + AudioFileReader) ──────────

        private WaveOutEvent? _naudioDevice;
        private WaveStream? _naudioReader;

        private void PlayWithNAudio(string source, bool loop, int durationMs, int volumePercent, CancellationToken token, Action onStarted)
        {
            using var reader = CreateReaderStream(source);
            using var device = new WaveOutEvent { Volume = volumePercent / 100f };
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
            onStarted();

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
        /// MP3 goes through NAudio's <see cref="Mp3FileReaderBase"/> with an NLayer (pure-managed)
        /// frame decompressor rather than <see cref="AudioFileReader"/>'s default MediaFoundationReader
        /// for that extension, which throws TypeLoadException under this app's self-contained/
        /// single-file publish (Media Foundation COM activation breaks there). Everything else
        /// still goes through AudioFileReader.
        /// </summary>
        private static WaveStream CreateReaderStream(string source)
        {
            if (source.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                var builder = new Mp3FileReaderBase.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
                return new Mp3FileReaderBase(source, builder);
            }

            return new AudioFileReader(source);
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
        // ── Linux: subprocess playback ──────────────────────────

        private void PlayOnLinux(string source, bool loop, int durationMs, int volumePercent, CancellationToken token, Action onStarted)
        {
            var (command, extraArgs) = FindLinuxAudioCommand(source);
            if (command != null)
            {
                _logger.Information("Linux audio player: {Command} {Args} for {Source}", command, extraArgs, source);
                PlayWithSubprocess(command, extraArgs, source, loop, durationMs, volumePercent, token, onStarted);
            }
            else
            {
                _logger.Warning("No audio playback command found on Linux (tried paplay, pw-play, aplay, mpv, ffplay)");
                onStarted();
            }
        }

        // Cached available commands — detected once, reused for all playback
        private static (string? cmd, string[]? args)? _linuxWavPlayer;
        private static (string? cmd, string[]? args)? _linuxCompressedPlayer;
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
