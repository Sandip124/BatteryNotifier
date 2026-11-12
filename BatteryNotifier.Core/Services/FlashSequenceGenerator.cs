using System.Diagnostics;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Utils;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Builds a <see cref="FlashSequence"/> — a smoothed loudness envelope — from a sound file so the
/// screen flash reflects the sound (louder → brighter). WAV is parsed directly and MP3 via NLayer,
/// both pure-managed with no external tool needed. Rarer custom-import formats (m4a/wma/ogg/flac/
/// aac) fall back to a system decoder (<c>afconvert</c> on macOS, <c>ffmpeg</c> elsewhere) and
/// return <c>null</c> — default pulse — if none is installed.
/// </summary>
public static class FlashSequenceGenerator
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("FlashSequenceGenerator");

    private const int WindowMs = 25;                                // RMS analysis window
    private const int WindowsPerFrame = 2;                          // windows pooled per output frame
    private const int FrameIntervalMs = WindowMs * WindowsPerFrame; // 50ms — shorter tween = tighter sync
    private const int MaxFrames = 6000;                            // cap (~5 min) so long sounds keep a full-length envelope
    private const int DecodeTimeoutMs = 15_000;

    /// <summary>Generates the envelope for a sound settings value, or null if it can't be analyzed.</summary>
    public static FlashSequence? Generate(string? soundSettingsValue)
    {
        try
        {
            var path = BuiltInSounds.Resolve(soundSettingsValue);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                var (samples, sampleRate) = AudioDecode.ReadWavMono(path);
                return samples.Length == 0 ? null : BuildSequence(samples, sampleRate);
            }

            if (path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                // Cap matches the envelope's own length cap — no point decoding audio it'll discard.
                var (samples, sampleRate) = AudioDecode.ReadMp3Mono(path, MaxFrames * FrameIntervalMs);
                return samples.Length == 0 ? null : BuildSequence(samples, sampleRate);
            }

            var tempWav = DecodeToWav(path);
            if (tempWav == null)
                return null;

            try
            {
                var (samples, sampleRate) = AudioDecode.ReadWavMono(tempWav);
                return samples.Length == 0 ? null : BuildSequence(samples, sampleRate);
            }
            finally
            {
                TryDelete(tempWav);
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Flash sequence generation failed for {Sound}", soundSettingsValue);
            return null;
        }
    }

    // ── Decode (non-WAV) ──────────────────────────────────────────

    private static string? DecodeToWav(string source)
    {
        var cacheDir = Path.Combine(Constants.AppTempDirectory, "flashcache");
        Directory.CreateDirectory(cacheDir);
        var dest = Path.Combine(cacheDir, Guid.NewGuid().ToString("N") + ".wav");

        bool ok = OperatingSystem.IsMacOS()
            ? RunDecoder("afconvert", "-f", "WAVE", "-d", "LEI16", source, dest)
            : RunDecoder("ffmpeg", "-y", "-loglevel", "error", "-i", source, "-ac", "1", "-ar", "22050", dest);

        return ok && File.Exists(dest) ? dest : null;
    }

    private static bool RunDecoder(string command, params string[] args)
    {
        try
        {
            using var process = new Process();
            var psi = new ProcessStartInfo
            {
                FileName = Constants.ResolveCommand(command),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);
            process.StartInfo = psi;
            process.Start();

            _ = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(DecodeTimeoutMs))
            {
                if (!process.HasExited) process.Kill();
                return false;
            }
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Audio decode command unavailable/failed: {Command}", command);
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException ex) { Logger.Debug(ex, "Could not delete temp decode file {Path}", path); }
        catch (UnauthorizedAccessException ex) { Logger.Debug(ex, "Could not delete temp decode file {Path}", path); }
    }

    // ── Envelope: RMS windows → peak-pool to frames → smooth → normalize ──

    private static FlashSequence? BuildSequence(float[] samples, int sampleRate)
    {
        int window = Math.Max(1, sampleRate * WindowMs / 1000);

        // 1) RMS energy per short window.
        var rms = new List<double>();
        for (int start = 0; start < samples.Length; start += window)
        {
            int end = Math.Min(start + window, samples.Length);
            double sum = 0;
            for (int i = start; i < end; i++)
                sum += (double)samples[i] * samples[i];
            rms.Add(Math.Sqrt(sum / Math.Max(1, end - start)));
        }
        if (rms.Count == 0) return null;

        // 2) Peak-pool windows into output frames (keeps beats from being averaged away).
        var frames = new List<double>();
        for (int i = 0; i < rms.Count && frames.Count < MaxFrames; i += WindowsPerFrame)
        {
            double peak = 0;
            for (int j = i; j < Math.Min(i + WindowsPerFrame, rms.Count); j++)
                peak = Math.Max(peak, rms[j]);
            frames.Add(peak);
        }

        // 3) Smooth with a fast attack / slow release so it tracks onsets but doesn't flicker.
        const double attack = 0.6, release = 0.2;
        double smoothed = frames[0];
        for (int i = 0; i < frames.Count; i++)
        {
            var target = frames[i];
            smoothed += (target > smoothed ? attack : release) * (target - smoothed);
            frames[i] = smoothed;
        }

        // 4) Normalize to 0..1, then a gentle gamma to lift quieter passages.
        double max = frames.Max();
        if (max <= 1e-4) return null; // effectively silent
        for (int i = 0; i < frames.Count; i++)
            frames[i] = Math.Pow(Math.Clamp(frames[i] / max, 0, 1), 0.7);

        return frames.Count > 1 ? new FlashSequence(FrameIntervalMs, frames) : null;
    }
}
