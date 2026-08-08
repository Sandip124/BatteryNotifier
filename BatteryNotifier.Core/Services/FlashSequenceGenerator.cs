using System.Diagnostics;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Models;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Builds a <see cref="FlashSequence"/> — a smoothed loudness envelope — from a sound file so the
/// screen flash reflects the sound (louder → brighter). WAV is parsed directly in managed code, so
/// the built-in tones react on every OS with no external tool. Other formats are first decoded to
/// WAV with a system tool (<c>afconvert</c> on macOS, <c>ffmpeg</c> on Linux/Windows). If decoding
/// isn't available the method returns <c>null</c> and the caller falls back to the default pulse.
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

            string? tempWav = null;
            try
            {
                var wavPath = path;
                if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    tempWav = DecodeToWav(path);
                    if (tempWav == null)
                        return null; // no decoder available → caller uses the default pulse
                    wavPath = tempWav;
                }

                var (samples, sampleRate) = ReadWavMono(wavPath);
                return samples.Length == 0 ? null : BuildSequence(samples, sampleRate);
            }
            finally
            {
                if (tempWav != null) TryDelete(tempWav);
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

        // macOS ships afconvert; Linux/Windows rely on ffmpeg (graceful fallback if absent).
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

            // Drain both pipes so a chatty tool can't deadlock on a full buffer.
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

    // ── WAV parsing (16-bit PCM, any channel count → mono) ────────

    private static (float[] Samples, int SampleRate) ReadWavMono(string path)
    {
        using var fs = File.OpenRead(path);
        using var reader = new BinaryReader(fs);

        if (new string(reader.ReadChars(4)) != "RIFF") return ([], 0);
        reader.ReadInt32();                                   // overall size
        if (new string(reader.ReadChars(4)) != "WAVE") return ([], 0);

        short audioFormat = 0, channels = 0, bitsPerSample = 0;
        int sampleRate = 0;
        byte[]? data = null;

        while (fs.Position + 8 <= fs.Length)
        {
            var chunkId = new string(reader.ReadChars(4));
            int chunkSize = reader.ReadInt32();
            if (chunkSize < 0 || fs.Position + chunkSize > fs.Length + 1) break;

            if (chunkId == "fmt ")
            {
                audioFormat = reader.ReadInt16();
                channels = reader.ReadInt16();
                sampleRate = reader.ReadInt32();
                reader.ReadInt32();                           // byte rate
                reader.ReadInt16();                           // block align
                bitsPerSample = reader.ReadInt16();
                if (chunkSize > 16) reader.ReadBytes(chunkSize - 16); // fmt extension
            }
            else if (chunkId == "data")
            {
                data = reader.ReadBytes(chunkSize);
            }
            else
            {
                reader.ReadBytes(chunkSize);                  // skip unknown chunk
            }

            if ((chunkSize & 1) == 1 && fs.Position < fs.Length)
                reader.ReadByte();                            // chunks are word-aligned
        }

        if (data == null || sampleRate <= 0 || channels <= 0 || audioFormat != 1 || bitsPerSample != 16)
            return ([], 0);

        int frameCount = data.Length / (2 * channels);
        var mono = new float[frameCount];
        int idx = 0;
        for (int f = 0; f < frameCount; f++)
        {
            int sum = 0;
            for (int c = 0; c < channels; c++)
            {
                sum += (short)(data[idx] | (data[idx + 1] << 8));
                idx += 2;
            }
            mono[f] = sum / channels / 32768f;
        }
        return (mono, sampleRate);
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
