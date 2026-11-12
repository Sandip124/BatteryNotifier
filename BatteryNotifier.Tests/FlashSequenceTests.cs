using System.Text.Json;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class FlashSequenceTests
{
    [Fact]
    public void FlashSequence_ClampsIntensities_AndComputesDuration()
    {
        var seq = new FlashSequence(100, [-1.0, 0.5, 2.0]);

        Assert.Equal(0.0, seq.Intensities[0]);   // clamped up from -1
        Assert.Equal(0.5, seq.Intensities[1]);
        Assert.Equal(1.0, seq.Intensities[2]);   // clamped down from 2
        Assert.Equal(200, seq.DurationMs);       // 100ms * (3 - 1)
    }

    [Fact]
    public void FlashSequence_ClampsFrameInterval_ToAtLeastOne()
    {
        var seq = new FlashSequence(0, [0.1, 0.2]);
        Assert.True(seq.FrameIntervalMs >= 1);
    }

    [Fact]
    public void Generate_BuiltInTone_ProducesSmoothNormalizedEnvelope()
    {
        // Built-in tones are synthesized 16-bit PCM WAV in managed code, so this exercises the
        // WAV-parse + envelope path end to end on every OS (no external decoder needed).
        var seq = FlashSequenceGenerator.Generate("builtin:Harp");

        Assert.NotNull(seq);
        Assert.True(seq!.Intensities.Count > 1);
        Assert.Equal(50, seq.FrameIntervalMs);          // 50ms resolution for tight sync
        Assert.All(seq.Intensities, v => Assert.InRange(v, 0.0, 1.0));
        Assert.Contains(seq.Intensities, v => v > 0.5); // normalization lifts the loudest part
    }

    [Fact]
    public void Generate_BuiltInTone_ReflectsLoudAndQuiet()
    {
        // The envelope must vary (loud vs quiet), not sit at a flat level — that's what drives the
        // glow's intensity and band height.
        var seq = FlashSequenceGenerator.Generate("builtin:Klaxon");

        Assert.NotNull(seq);
        var min = seq!.Intensities.Min();
        var max = seq.Intensities.Max();
        Assert.True(max - min > 0.2, $"expected dynamic range, got {min:F2}..{max:F2}");
    }

    [Fact]
    public void Generate_UnknownSound_ReturnsNull()
    {
        Assert.Null(FlashSequenceGenerator.Generate("builtin:DoesNotExist"));
        Assert.Null(FlashSequenceGenerator.Generate(null));
    }

    [Fact]
    public async Task Library_GeneratesPersistsAndReloads_BuiltInEnvelope()
    {
        var lib = FlashSequenceLibrary.Instance;
        lib.Invalidate("builtin:Harp"); // clear memory + any persisted copy

        // First call generates and persists to disk.
        var first = await lib.GetOrGenerateAsync("builtin:Harp");
        Assert.NotNull(first);
        Assert.True(first!.Intensities.Count > 1);

        // Second call returns an equivalent sequence (from memory or the persisted file).
        var second = await lib.GetOrGenerateAsync("builtin:Harp");
        Assert.NotNull(second);
        Assert.Equal(first.Intensities.Count, second!.Intensities.Count);

        lib.Invalidate("builtin:Harp"); // cleanup
    }

    [Fact]
    public void CachedSequence_RoundTripsThroughSourceGeneratedJson()
    {
        // Reflection-based JSON is disabled app-wide, so persistence must use the source generator.
        // This guards against the "Reflection-based serialization has been disabled" regression.
        var dto = new CachedSequence("builtin:Harp", "v2:stable", 50, [0.0, 0.5, 1.0]);

        var json = JsonSerializer.Serialize(dto, FlashSequenceJsonContext.Default.CachedSequence);
        var back = JsonSerializer.Deserialize(json, FlashSequenceJsonContext.Default.CachedSequence);

        Assert.NotNull(back);
        Assert.Equal(dto.Source, back!.Source);
        Assert.Equal(dto.Signature, back.Signature);
        Assert.Equal(dto.FrameIntervalMs, back.FrameIntervalMs);
        Assert.Equal(dto.Intensities, back.Intensities);
    }
}
