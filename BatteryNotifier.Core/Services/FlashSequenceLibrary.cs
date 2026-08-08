using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Models;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Maps each sound (by its settings value) to its flash <see cref="FlashSequence"/> and generates
/// missing ones in the background via <see cref="FlashSequenceGenerator"/>. Results are cached in
/// memory for the session and persisted to disk (JSON under <c>{AppData}/flash-sequences/</c>) so
/// they survive restarts and don't re-run the decode each launch. A per-entry signature invalidates
/// the cache when the underlying sound file changes. A flash just does a cheap synchronous
/// <see cref="Get"/> — falling back to the default pulse until the envelope is ready.
/// </summary>
public sealed class FlashSequenceLibrary
{
    private static readonly Lazy<FlashSequenceLibrary> _instance = new(() => new FlashSequenceLibrary());
    public static FlashSequenceLibrary Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("FlashSequenceLibrary");

    private readonly ConcurrentDictionary<string, FlashSequence?> _cache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Task<FlashSequence?>> _inFlight = new(StringComparer.Ordinal);

    private static string CacheDir => Path.Combine(Constants.AppDataDirectory, "flash-sequences");

    private FlashSequenceLibrary() { }

    /// <summary>The in-memory envelope for a sound, or null if not loaded yet / not analyzable.</summary>
    public FlashSequence? Get(string? soundSettingsValue) =>
        !string.IsNullOrEmpty(soundSettingsValue) && _cache.TryGetValue(soundSettingsValue, out var seq)
            ? seq
            : null;

    /// <summary>
    /// Returns the envelope, loading it from disk or generating (and persisting) it off the UI
    /// thread if needed. Concurrent callers for the same sound share one generation. Used by the
    /// sound-picker preview so the audition reflects the real envelope.
    /// </summary>
    public Task<FlashSequence?> GetOrGenerateAsync(string? soundSettingsValue)
    {
        if (string.IsNullOrEmpty(soundSettingsValue))
            return Task.FromResult<FlashSequence?>(null);
        if (_cache.TryGetValue(soundSettingsValue, out var cached))
            return Task.FromResult(cached);

        // One generation per key: concurrent callers await the same Task instead of racing.
        return _inFlight.GetOrAdd(soundSettingsValue, GenerateAndCacheAsync);
    }

    /// <summary>Loads-or-generates the envelope in the background if not in memory. Idempotent.</summary>
    public void EnsureGenerated(string? soundSettingsValue) => _ = GetOrGenerateAsync(soundSettingsValue);

    /// <summary>Drops the cached envelope (memory + disk), e.g. when a custom sound is re-imported.</summary>
    public void Invalidate(string? soundSettingsValue)
    {
        if (string.IsNullOrEmpty(soundSettingsValue)) return;

        _cache.TryRemove(soundSettingsValue, out _);
        TryDelete(CacheFilePath(soundSettingsValue));
    }

    private async Task<FlashSequence?> GenerateAndCacheAsync(string settingsValue)
    {
        try
        {
            var sequence = await Task.Run(() => LoadOrGenerate(settingsValue)).ConfigureAwait(false);
            _cache[settingsValue] = sequence;
            return sequence;
        }
        finally
        {
            _inFlight.TryRemove(settingsValue, out _);
        }
    }

    // ── Load / generate / persist ─────────────────────────────────

    private static FlashSequence? LoadOrGenerate(string settingsValue)
    {
        var (hit, sequence) = TryLoadFromDisk(settingsValue);
        if (hit) return sequence;

        var generated = FlashSequenceGenerator.Generate(settingsValue);
        SaveToDisk(settingsValue, generated);
        return generated;
    }

    private static (bool Hit, FlashSequence? Sequence) TryLoadFromDisk(string settingsValue)
    {
        try
        {
            var path = CacheFilePath(settingsValue);
            if (!File.Exists(path)) return (false, null);

            var dto = JsonSerializer.Deserialize(File.ReadAllText(path), FlashSequenceJsonContext.Default.CachedSequence);
            if (dto == null || dto.Signature != ComputeSignature(settingsValue))
                return (false, null); // stale (source changed) or unreadable → regenerate

            var sequence = dto.Intensities is { Count: > 1 }
                ? new FlashSequence(dto.FrameIntervalMs, dto.Intensities)
                : null; // a cached "not analyzable" result
            return (true, sequence);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to read cached flash sequence for {Sound}", settingsValue);
            return (false, null);
        }
    }

    private static void SaveToDisk(string settingsValue, FlashSequence? sequence)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            var dto = new CachedSequence(
                settingsValue,
                ComputeSignature(settingsValue),
                sequence?.FrameIntervalMs ?? 0,
                sequence?.Intensities);

            var path = CacheFilePath(settingsValue);
            var temp = $"{path}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(dto, FlashSequenceJsonContext.Default.CachedSequence));
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to persist flash sequence for {Sound}", settingsValue);
        }
    }

    // Bump when the envelope algorithm/resolution — or a source sound's generation — changes so
    // persisted sequences regenerate. (v3: built-in tones are now repeat-filled to a single pass.)
    private const string CacheVersion = "v3";

    /// <summary>Signature that invalidates the cache when the underlying sound (or algorithm) changes.</summary>
    private static string ComputeSignature(string settingsValue)
    {
        // Built-in tones and bundled sounds have deterministic content, so the value alone is stable
        // (their files are regenerated/extracted to temp each launch with fresh timestamps).
        if (BuiltInSounds.IsBuiltIn(settingsValue) ||
            settingsValue.StartsWith("bundled:", StringComparison.OrdinalIgnoreCase))
            return $"{CacheVersion}:stable";

        try
        {
            var resolved = BuiltInSounds.Resolve(settingsValue);
            if (string.IsNullOrEmpty(resolved) || !File.Exists(resolved)) return $"{CacheVersion}:missing";
            var info = new FileInfo(resolved);
            return $"{CacheVersion}:{info.Length}:{info.LastWriteTimeUtc.Ticks}";
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to compute flash-sequence signature for {Sound}", settingsValue);
            return "unknown";
        }
    }

    private static string CacheFilePath(string settingsValue)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(settingsValue)));
        return Path.Combine(CacheDir, hash.ToLowerInvariant() + ".json");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException ex) { Logger.Debug(ex, "Could not delete cached flash sequence {Path}", path); }
        catch (UnauthorizedAccessException ex) { Logger.Debug(ex, "Could not delete cached flash sequence {Path}", path); }
    }

}

/// <summary>Persisted form of a cached flash sequence (source-generated JSON — reflection is disabled).</summary>
internal sealed record CachedSequence(
    string Source,
    string Signature,
    int FrameIntervalMs,
    IReadOnlyList<double>? Intensities);

[JsonSerializable(typeof(CachedSequence))]
[JsonSourceGenerationOptions(WriteIndented = false)]
internal partial class FlashSequenceJsonContext : JsonSerializerContext;
