using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BatteryNotifier.Core.Logger;
using Serilog;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace BatteryNotifier.Core.Managers
{
    /// <summary>
    /// Cross-platform audio playback.
    /// - macOS: afplay subprocess (ArgumentList for injection safety)
    /// - Linux: paplay subprocess, falls back to aplay
    /// - Windows: SoundFlow (MiniAudio backend)
    /// </summary>
    public class SoundManager : IDisposable
    {
        private const int DefaultPlayDurationMs = 30000;

        private readonly ILogger _logger;
        private readonly object _playLock = new();
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

            // Resolve built-in sounds to their cached WAV file paths
            var resolvedPath = BuiltInSounds.Resolve(source);
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath)) return;

            // Canonicalize path — on macOS /var is a symlink to /private/var,
            // so GetTempPath() returns /var/... but GetFullPath() resolves to /private/var/...
            try
            {
                resolvedPath = Path.GetFullPath(resolvedPath);
            }
            catch
            {
                _logger.Warning("Failed to canonicalize sound file path: {Path}", resolvedPath);
                return;
            }

            // Validate the path is a real, rooted file path
            if (!Path.IsPathRooted(resolvedPath) || !File.Exists(resolvedPath))
            {
                _logger.Warning("Rejected invalid sound file path: {Path}", resolvedPath);
                return;
            }

            // Reject symlinks — prevents reading arbitrary files
            var fileInfo = new FileInfo(resolvedPath);
            if (fileInfo.LinkTarget != null)
            {
                _logger.Warning("Rejected symlink sound file path: {Path}", resolvedPath);
                return;
            }

            // Reject files larger than 50 MB
            if (fileInfo.Length > 50 * 1024 * 1024)
            {
                _logger.Warning("Rejected oversized sound file ({Size} bytes): {Path}", fileInfo.Length, resolvedPath);
                return;
            }

            CancellationToken token;
            lock (_playLock)
            {
                if (_isPlaying) return;
                _isPlaying = true;

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                token = _cancellationTokenSource.Token;
            }

            try
            {
                await Task.Run(() => PlaySound(resolvedPath, loop, durationMs, token), token);
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

        private void PlaySound(string source, bool loop, int durationMs, CancellationToken token)
        {
            if (OperatingSystem.IsMacOS())
                PlayWithSubprocess("afplay", source, loop, durationMs, token);
            else if (OperatingSystem.IsLinux())
                PlayWithLinuxSubprocess(source, loop, durationMs, token);
            else if (OperatingSystem.IsWindows())
                PlayWithSoundFlow(source, loop, durationMs, token);
            else
                _logger.Warning("Unsupported platform for sound playback");
        }

        // ── macOS / Linux: subprocess playback ──────────────────────

        private void PlayWithSubprocess(string command, string source, bool loop,
            int durationMs, CancellationToken token)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);

            do
            {
                token.ThrowIfCancellationRequested();

                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                // Use ArgumentList for safe argument passing (no shell injection)
                psi.ArgumentList.Add(source);

                using var process = new Process { StartInfo = psi };
                _currentProcess = process;

                try
                {
                    process.Start();

                    var remainingMs = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                    if (remainingMs <= 0) remainingMs = 100;

                    // Wait for process to exit or timeout/cancellation
                    while (!process.WaitForExit(200))
                    {
                        if (token.IsCancellationRequested || DateTime.UtcNow >= deadline)
                        {
                            KillProcess(process);
                            return;
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Subprocess playback failed for {Command}", command);
                    return;
                }
                finally
                {
                    _currentProcess = null;
                }
            } while (loop && !token.IsCancellationRequested && DateTime.UtcNow < deadline);
        }

        private void PlayWithLinuxSubprocess(string source, bool loop,
            int durationMs, CancellationToken token)
        {
            // Try paplay first (PulseAudio/PipeWire), then aplay (ALSA)
            try
            {
                PlayWithSubprocess("paplay", source, loop, durationMs, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.Debug("paplay failed, falling back to aplay");
                PlayWithSubprocess("aplay", source, loop, durationMs, token);
            }
        }

        // ── Windows: SoundFlow (MiniAudio) ──────────────────────────

        // SoundFlow engine — lazy-init, only used on Windows
        private static MiniAudioEngine? _sfEngine;
        private static AudioPlaybackDevice? _sfDevice;
        private static readonly object _sfLock = new();
        private static bool _sfFailed;
        private SoundPlayer? _sfCurrentPlayer;

        private void PlayWithSoundFlow(string source, bool loop, int durationMs, CancellationToken token)
        {
            var device = EnsureSoundFlowEngine();
            if (device == null)
            {
                _logger.Warning("SoundFlow audio engine initialization failed — cannot play sound.");
                return;
            }

            using var stream = File.OpenRead(source);
            using var provider = new StreamDataProvider(_sfEngine!, stream);
            using var player = new SoundPlayer(_sfEngine!, device.Format, provider) { IsLooping = loop };
            using var playbackDone = new ManualResetEventSlim(false);

            EventHandler<EventArgs> onPlaybackEnded = (_, _) =>
            {
                try { playbackDone.Set(); }
                catch (ObjectDisposedException) { }
            };
            player.PlaybackEnded += onPlaybackEnded;

            _sfCurrentPlayer = player;
            device.MasterMixer.AddComponent(player);
            player.Play();

            try
            {
                playbackDone.Wait(durationMs, token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                player.Stop();
                player.PlaybackEnded -= onPlaybackEnded;
                device.MasterMixer.RemoveComponent(player);
                _sfCurrentPlayer = null;
            }
        }

        private static AudioPlaybackDevice? EnsureSoundFlowEngine()
        {
            if (_sfFailed) return null;
            if (_sfDevice != null) return _sfDevice;

            lock (_sfLock)
            {
                if (_sfDevice != null) return _sfDevice;
                if (_sfFailed) return null;

                try
                {
                    var engine = new MiniAudioEngine();
                    var device = engine.InitializePlaybackDevice(null, AudioFormat.Cd);
                    _sfEngine = engine;
                    _sfDevice = device;
                    Log.Debug("SoundManager: SoundFlow audio engine initialized");
                    return device;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "SoundManager: SoundFlow audio engine initialization failed");
                    _sfFailed = true;
                    return null;
                }
            }
        }

        // ── Stop / Dispose ──────────────────────────────────────────

        public void StopSound()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                KillProcess(_currentProcess);
                _sfCurrentPlayer?.Stop();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while stopping sound playback.");
            }
        }

        private static void KillProcess(Process? process)
        {
            try
            {
                if (process != null && !process.HasExited)
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
        }
    }
}
