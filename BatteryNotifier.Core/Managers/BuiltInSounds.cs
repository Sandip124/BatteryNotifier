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
        // ── General purpose ──
        ["Chime"] = GenerateChime,
        ["Alert"] = GenerateAlert,
        ["Beacon"] = GenerateBeacon,

        // ── Full battery (calm / positive) ──
        ["Zen"] = GenerateZen,
        ["Harp"] = GenerateHarp,
        ["Breeze"] = GenerateBreeze,
        ["Bloom"] = GenerateBloom,

        // ── Low battery (warning / urgent) ──
        ["Pulse"] = GeneratePulse,
        ["Klaxon"] = GenerateKlaxon,
        ["Rattle"] = GenerateRattle,
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

    // ── Full Battery Sounds (calm, positive) ─────────────────────

    /// <summary>
    /// Zen: soft low tone with gentle overtone shimmer. Very calming.
    /// Two layered notes (C4 + G4) with long fade-out — like a meditation bowl.
    /// </summary>
    private static short[] GenerateZen()
    {
        var duration = 2.0;
        var samples = new short[(int)(SampleRate * duration)];

        // Fundamental: C4 (261 Hz) — warm base
        FillTone(samples, 0, samples.Length, 261.63, 0.25, fadeIn: 0.15, fadeOut: 1.2);
        // Overtone: G4 (392 Hz) — gentle fifth
        MixTone(samples, 0, samples.Length, 392.0, 0.12, fadeIn: 0.3, fadeOut: 1.4);
        // Soft shimmer: E5 (659 Hz) — very quiet high harmonic
        MixTone(samples, 0, samples.Length, 659.25, 0.05, fadeIn: 0.5, fadeOut: 1.0);

        return samples;
    }

    /// <summary>
    /// Harp: descending arpeggio G5 → E5 → C5 → G4. Peaceful and complete.
    /// </summary>
    private static short[] GenerateHarp()
    {
        var noteLen = (int)(SampleRate * 0.35);
        var overlap = (int)(SampleRate * 0.15); // notes overlap for a harp-like sustain
        var step = noteLen - overlap;
        var total = step * 3 + noteLen + (int)(SampleRate * 0.3); // extra tail
        var samples = new short[total];

        double[] freqs = [783.99, 659.25, 523.25, 392.0]; // G5, E5, C5, G4
        for (int i = 0; i < freqs.Length; i++)
        {
            var offset = i * step;
            var len = Math.Min(noteLen + (int)(SampleRate * 0.2), total - offset);
            MixTone(samples, offset, len, freqs[i], 0.3 - i * 0.03, fadeIn: 0.008, fadeOut: 0.35);
        }

        return samples;
    }

    /// <summary>
    /// Breeze: slow ascending sweep from C4 to G4 with soft attack. Airy and gentle.
    /// </summary>
    private static short[] GenerateBreeze()
    {
        var duration = 1.5;
        var count = (int)(SampleRate * duration);
        var samples = new short[count];

        double startFreq = 261.63; // C4
        double endFreq = 392.0;    // G4
        var fadeInSamples = (int)(0.2 * SampleRate);
        var fadeOutSamples = (int)(0.8 * SampleRate);

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / SampleRate;
            double progress = (double)i / count;
            // Smooth frequency sweep (ease-in-out)
            double ease = progress * progress * (3 - 2 * progress);
            double freq = startFreq + (endFreq - startFreq) * ease;

            double sample = Math.Sin(2 * Math.PI * freq * t) * 0.28;
            // Add soft second harmonic
            sample += Math.Sin(2 * Math.PI * freq * 2 * t) * 0.06;

            // Envelope
            if (i < fadeInSamples)
                sample *= (double)i / fadeInSamples;
            else if (i > count - fadeOutSamples)
                sample *= (double)(count - i) / fadeOutSamples;

            samples[i] = ClampSample(sample);
        }

        return samples;
    }

    /// <summary>
    /// Bloom: two warm chords that "bloom" open. Major 7th feel (C4+E4 → G4+B4).
    /// </summary>
    private static short[] GenerateBloom()
    {
        var chordLen = (int)(SampleRate * 0.8);
        var gap = (int)(SampleRate * 0.1);
        var total = chordLen * 2 + gap;
        var samples = new short[total];

        // Chord 1: C4 + E4
        MixTone(samples, 0, chordLen, 261.63, 0.22, fadeIn: 0.08, fadeOut: 0.3);
        MixTone(samples, 0, chordLen, 329.63, 0.18, fadeIn: 0.12, fadeOut: 0.3);

        // Chord 2: G4 + B4 — brighter resolution
        var offset2 = chordLen + gap;
        var len2 = total - offset2;
        MixTone(samples, offset2, len2, 392.0, 0.22, fadeIn: 0.06, fadeOut: 0.4);
        MixTone(samples, offset2, len2, 493.88, 0.16, fadeIn: 0.10, fadeOut: 0.4);

        return samples;
    }

    // ── Low Battery Sounds (warning, urgent) ───────────────────

    /// <summary>
    /// Pulse: rapid repeating beeps with increasing urgency. Three short beeps.
    /// </summary>
    private static short[] GeneratePulse()
    {
        var beepLen = (int)(SampleRate * 0.1);
        var gapLen = (int)(SampleRate * 0.08);
        var total = beepLen * 3 + gapLen * 2 + (int)(SampleRate * 0.15);
        var samples = new short[total];

        double[] freqs = [784.0, 880.0, 988.0]; // G5, A5, B5 — ascending urgency
        for (int i = 0; i < 3; i++)
        {
            var offset = i * (beepLen + gapLen);
            FillTone(samples, offset, beepLen, freqs[i], 0.55, fadeIn: 0.005, fadeOut: 0.02);
        }

        return samples;
    }

    /// <summary>
    /// Klaxon: harsh low buzz (two dissonant tones). Feels like a real alarm.
    /// A3 + Bb3 played together — tritone-adjacent dissonance.
    /// </summary>
    private static short[] GenerateKlaxon()
    {
        var buzzLen = (int)(SampleRate * 0.25);
        var gapLen = (int)(SampleRate * 0.1);
        var total = buzzLen * 2 + gapLen + (int)(SampleRate * 0.1);
        var samples = new short[total];

        // Buzz 1
        FillTone(samples, 0, buzzLen, 220.0, 0.4, fadeIn: 0.005, fadeOut: 0.04);   // A3
        MixTone(samples, 0, buzzLen, 233.08, 0.3, fadeIn: 0.005, fadeOut: 0.04);    // Bb3

        // Buzz 2 — slightly higher
        var offset2 = buzzLen + gapLen;
        FillTone(samples, offset2, buzzLen, 246.94, 0.4, fadeIn: 0.005, fadeOut: 0.06); // B3
        MixTone(samples, offset2, buzzLen, 261.63, 0.3, fadeIn: 0.005, fadeOut: 0.06); // C4

        return samples;
    }

    /// <summary>
    /// Rattle: fast staccato clicks at descending pitch. Sounds like a rattlesnake warning.
    /// </summary>
    private static short[] GenerateRattle()
    {
        var clickLen = (int)(SampleRate * 0.04);
        var gapLen = (int)(SampleRate * 0.03);
        var clickCount = 6;
        var total = (clickLen + gapLen) * clickCount + (int)(SampleRate * 0.1);
        var samples = new short[total];

        for (int i = 0; i < clickCount; i++)
        {
            var offset = i * (clickLen + gapLen);
            // Descending pitch: starts at B5, drops
            double freq = 988.0 - i * 80;
            double amp = 0.5 - i * 0.04;
            FillTone(samples, offset, clickLen, freq, amp, fadeIn: 0.002, fadeOut: 0.015);
            // Add noise-like second harmonic for click texture
            MixTone(samples, offset, clickLen, freq * 2.73, amp * 0.25, fadeIn: 0.001, fadeOut: 0.01);
        }

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

    /// <summary>
    /// Like FillTone but adds (mixes) into existing buffer content instead of overwriting.
    /// </summary>
    private static void MixTone(short[] buffer, int offset, int count,
        double frequency, double amplitude, double fadeIn, double fadeOut)
    {
        var fadeInSamples = (int)(fadeIn * SampleRate);
        var fadeOutSamples = (int)(fadeOut * SampleRate);

        for (int i = 0; i < count && offset + i < buffer.Length; i++)
        {
            double t = (double)i / SampleRate;
            double sample = Math.Sin(2 * Math.PI * frequency * t) * amplitude;

            if (i < fadeInSamples)
                sample *= (double)i / fadeInSamples;
            else if (i > count - fadeOutSamples)
                sample *= (double)(count - i) / fadeOutSamples;

            // Mix: add to existing and clamp
            double existing = buffer[offset + i] / (double)short.MaxValue;
            buffer[offset + i] = ClampSample(existing + sample);
        }
    }

    private static short ClampSample(double sample) =>
        (short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);

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
