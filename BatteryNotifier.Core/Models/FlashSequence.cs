namespace BatteryNotifier.Core.Models;

/// <summary>
/// A timeline that drives the screen-flash glow: a list of intensities (each 0..1) sampled at a
/// fixed interval. Playback interpolates between samples, so a modest number of points produces a
/// smooth animation. Sequences are meant to be derived from a sound's smoothed loudness envelope
/// so the flash reflects loud vs quiet moments — not an exact waveform match, just the feel.
/// </summary>
public sealed class FlashSequence
{
    /// <summary>Milliseconds between successive intensity samples.</summary>
    public int FrameIntervalMs { get; }

    /// <summary>Glow intensity at each sample point, clamped to 0..1.</summary>
    public IReadOnlyList<double> Intensities { get; }

    /// <summary>Total length of one pass through the sequence.</summary>
    public int DurationMs => FrameIntervalMs * System.Math.Max(0, Intensities.Count - 1);

    public FlashSequence(int frameIntervalMs, IReadOnlyList<double> intensities)
    {
        FrameIntervalMs = System.Math.Max(1, frameIntervalMs);

        var clamped = new double[intensities.Count];
        for (int i = 0; i < intensities.Count; i++)
            clamped[i] = System.Math.Clamp(intensities[i], 0.0, 1.0);
        Intensities = clamped;
    }
}
