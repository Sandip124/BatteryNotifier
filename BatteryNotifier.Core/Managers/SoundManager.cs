using System;
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
    /// Cross-platform audio playback using SoundFlow (MiniAudio backend).
    /// Replaces NAudio (Windows-only) and subprocess spawning (afplay/paplay/aplay)
    /// to eliminate AV-suspicious process creation for sound playback.
    /// </summary>
    public class SoundManager : IDisposable
    {
        private const int DefaultPlayDurationMs = 30000;

        // Shared audio engine — initialized once, lives for the app lifetime.
        // MiniAudioEngine must NOT be created on Avalonia's UI thread (causes freeze).
        private static MiniAudioEngine? _engine;
        private static volatile AudioPlaybackDevice? _playbackDevice;
        private static readonly object _engineLock = new();
        private static bool _engineFailed;

        private readonly ILogger _logger;
        private readonly object _playLock = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private SoundPlayer? _currentPlayer;
        private bool _isPlaying;
        private volatile bool _disposed;

        public SoundManager()
        {
            _logger = BatteryNotifierAppLogger.ForContext<SoundManager>();
        }

        private static AudioPlaybackDevice? EnsureEngine()
        {
            if (_engineFailed) return null;
            if (_playbackDevice != null) return _playbackDevice;

            lock (_engineLock)
            {
                if (_playbackDevice != null) return _playbackDevice;
                if (_engineFailed) return null;

                try
                {
                    var engine = new MiniAudioEngine();
                    var device = engine.InitializePlaybackDevice(null, AudioFormat.Cd);
                    _engine = engine;
                    _playbackDevice = device;
                    return device;
                }
                catch
                {
                    _engineFailed = true;
                    return null;
                }
            }
        }

        public async Task PlaySoundAsync(string? source, bool loop = false,
            int durationMs = DefaultPlayDurationMs)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SoundManager));

            // Resolve built-in sounds to their cached WAV file paths
            var resolvedPath = BuiltInSounds.Resolve(source);
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath)) return;

            // Validate the path is a real, rooted file path (no command injection via crafted paths)
            if (!Path.IsPathRooted(resolvedPath) || Path.GetFullPath(resolvedPath) != resolvedPath)
            {
                _logger.Warning("Rejected non-canonical sound file path: {Path}", resolvedPath);
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
                // Run on background thread — MiniAudioEngine must not touch the UI thread
                await Task.Run(() => PlayWithSoundFlow(resolvedPath, loop, durationMs, token), token);
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

        private void PlayWithSoundFlow(string source, bool loop, int durationMs, CancellationToken token)
        {
            var device = EnsureEngine();
            if (device == null)
            {
                _logger.Warning("Audio engine initialization failed — cannot play sound.");
                return;
            }

            using var stream = File.OpenRead(source);
            using var provider = new StreamDataProvider(_engine!, stream);
            using var player = new SoundPlayer(_engine!, device.Format, provider) { IsLooping = loop };
            using var playbackDone = new ManualResetEventSlim(false);

            // Subscribe BEFORE Play() so we don't miss a fast completion.
            // Use a named handler so we can unsubscribe to break the delegate→player reference chain.
            EventHandler<EventArgs> onPlaybackEnded = (_, _) =>
            {
                try { playbackDone.Set(); }
                catch (ObjectDisposedException) { /* already cleaned up */ }
            };
            player.PlaybackEnded += onPlaybackEnded;

            _currentPlayer = player;
            device.MasterMixer.AddComponent(player);
            player.Play();

            try
            {
                // Wait for natural completion, cancellation, or timeout
                playbackDone.Wait(durationMs, token);
            }
            catch (OperationCanceledException)
            {
                // StopSound() was called
            }
            finally
            {
                player.Stop();
                player.PlaybackEnded -= onPlaybackEnded;
                device.MasterMixer.RemoveComponent(player);
                _currentPlayer = null;
            }
        }

        public void StopSound()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _currentPlayer?.Stop();
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
