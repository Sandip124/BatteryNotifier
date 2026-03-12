using System;
using System.Collections.Generic;
using System.IO;

namespace BatteryNotifier.Core.Managers;

/// <summary>
/// Generates simple notification tones as WAV files and caches them on disk.
/// Each sound is a short synthesized PCM tone — no external files needed.
/// </summary>
public static class BuiltInSounds
{
    public const string Prefix = "builtin:";
    private const int SampleRate = 44100;
    private const short BitsPerSample = 16;
    private const short Channels = 1;

    private static readonly string CacheDir =
        Path.Combine(Path.GetTempPath(), "BatteryNotifier", "sounds");

    private static readonly Dictionary<string, Func<short[]>> Generators = new()
    {
        ["Chime"] = GenerateChime,
        ["Alert"] = GenerateAlert,
        ["Gentle"] = GenerateGentle,
        ["Ping"] = GeneratePing,
        ["Beacon"] = GenerateBeacon,
    };

    /// <summary>Names of all available built-in sounds.</summary>
    public static IReadOnlyList<string> Names { get; } = new List<string>(Generators.Keys).AsReadOnly();

    /// <summary>
    /// Returns true if the settings value represents a built-in sound.
    /// </summary>
    public static bool IsBuiltIn(string? value) =>
        value != null && value.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>
    /// Extracts the sound name from a settings value like "builtin:Chime".
    /// </summary>
    public static string? GetName(string? value) =>
        IsBuiltIn(value) ? value![Prefix.Length..] : null;

    /// <summary>
    /// Converts a built-in sound name to its settings value.
    /// </summary>
    public static string ToSettingsValue(string name) => Prefix + name;

    /// <summary>
    /// Resolves a settings value to an actual file path.
    /// For built-in sounds, generates the WAV if not cached. For custom paths, returns as-is.
    /// Returns null for null/empty input (= default/no sound).
    /// </summary>
    public static string? Resolve(string? settingsValue)
    {
        if (string.IsNullOrEmpty(settingsValue))
            return null;

        if (!IsBuiltIn(settingsValue))
            return settingsValue; // custom file path

        var name = GetName(settingsValue)!;
        return GetOrGenerate(name);
    }

    private static string? GetOrGenerate(string name)
    {
        // Strict lookup — only exact matches from the known generator dictionary.
        // Prevents path traversal via crafted names like "../../etc/passwd".
        if (!Generators.TryGetValue(name, out var generator))
            return null;

        Directory.CreateDirectory(CacheDir);
        var path = Path.Combine(CacheDir, $"{name}.wav");

        if (File.Exists(path))
            return path;

        var samples = generator();
        WriteWav(path, samples);
        return path;
    }

    // ── Sound Generators ──────────────────────────────────────────

    /// <summary>Pleasant ascending two-note chime (C5 → E5).</summary>
    private static short[] GenerateChime()
    {
        var duration = 0.6;
        var samples = new short[(int)(SampleRate * duration)];
        var half = samples.Length / 2;

        // Note 1: C5 (523 Hz)
        FillTone(samples, 0, half, 523.25, 0.5, fadeIn: 0.02, fadeOut: 0.08);
        // Note 2: E5 (659 Hz)
        FillTone(samples, half, samples.Length - half, 659.25, 0.5, fadeIn: 0.02, fadeOut: 0.15);

        return samples;
    }

    /// <summary>Urgent two-beep alert (A5, pause, A5).</summary>
    private static short[] GenerateAlert()
    {
        var duration = 0.8;
        var samples = new short[(int)(SampleRate * duration)];
        var beepLen = (int)(SampleRate * 0.15);
        var gapStart = beepLen;
        var gap = (int)(SampleRate * 0.1);

        FillTone(samples, 0, beepLen, 880.0, 0.6, fadeIn: 0.01, fadeOut: 0.03);
        FillTone(samples, gapStart + gap, beepLen, 880.0, 0.6, fadeIn: 0.01, fadeOut: 0.03);

        return samples;
    }

    /// <summary>Soft single tone with long fade (G4).</summary>
    private static short[] GenerateGentle()
    {
        var duration = 1.0;
        var samples = new short[(int)(SampleRate * duration)];
        FillTone(samples, 0, samples.Length, 392.0, 0.35, fadeIn: 0.1, fadeOut: 0.6);
        return samples;
    }

    /// <summary>Short high-pitched ping (E6).</summary>
    private static short[] GeneratePing()
    {
        var duration = 0.3;
        var samples = new short[(int)(SampleRate * duration)];
        FillTone(samples, 0, samples.Length, 1318.5, 0.45, fadeIn: 0.005, fadeOut: 0.2);
        return samples;
    }

    /// <summary>Three ascending notes: C5 → E5 → G5 (major triad arpeggio).</summary>
    private static short[] GenerateBeacon()
    {
        var noteLen = (int)(SampleRate * 0.2);
        var gap = (int)(SampleRate * 0.05);
        var total = noteLen * 3 + gap * 2;
        var samples = new short[total];

        var offset = 0;
        FillTone(samples, offset, noteLen, 523.25, 0.45, fadeIn: 0.01, fadeOut: 0.06); // C5
        offset += noteLen + gap;
        FillTone(samples, offset, noteLen, 659.25, 0.45, fadeIn: 0.01, fadeOut: 0.06); // E5
        offset += noteLen + gap;
        FillTone(samples, offset, noteLen, 783.99, 0.45, fadeIn: 0.01, fadeOut: 0.10); // G5

        return samples;
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static void FillTone(short[] buffer, int offset, int count,
        double frequency, double amplitude, double fadeIn, double fadeOut)
    {
        var fadeInSamples = (int)(fadeIn * SampleRate);
        var fadeOutSamples = (int)(fadeOut * SampleRate);

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / SampleRate;
            double sample = Math.Sin(2 * Math.PI * frequency * t) * amplitude;

            // Envelope
            if (i < fadeInSamples)
                sample *= (double)i / fadeInSamples;
            else if (i > count - fadeOutSamples)
                sample *= (double)(count - i) / fadeOutSamples;

            buffer[offset + i] = (short)(sample * short.MaxValue);
        }
    }

    private static void WriteWav(string path, short[] samples)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        int dataSize = samples.Length * (BitsPerSample / 8);
        int fileSize = 36 + dataSize;

        // RIFF header
        bw.Write("RIFF"u8);
        bw.Write(fileSize);
        bw.Write("WAVE"u8);

        // fmt chunk
        bw.Write("fmt "u8);
        bw.Write(16); // chunk size
        bw.Write((short)1); // PCM
        bw.Write(Channels);
        bw.Write(SampleRate);
        bw.Write(SampleRate * Channels * BitsPerSample / 8); // byte rate
        bw.Write((short)(Channels * BitsPerSample / 8)); // block align
        bw.Write(BitsPerSample);

        // data chunk
        bw.Write("data"u8);
        bw.Write(dataSize);
        foreach (var sample in samples)
            bw.Write(sample);
    }
}
