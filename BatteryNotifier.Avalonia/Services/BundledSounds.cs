using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Platform;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// Manages curated sound files bundled as Avalonia resources in Assets/Sounds/.
/// Settings format: "bundled:FileName.mp3" — extracted to a cache directory for playback.
/// </summary>
public static class BundledSounds
{
    public const string Prefix = "bundled:";

    private static readonly string CacheDir =
        Path.Combine(Core.Constants.AppTempDirectory, "bundled-sounds");

    /// <summary>
    /// Editor's choice sounds bundled with the app.
    /// Key = display name, Value = filename in Assets/Sounds/.
    /// </summary>
    private static readonly (string Name, string FileName, string Category)[] Catalog =
    [
        ("Rock N Roll", "FullBattery_RockNRoll.mp3", "Full Battery"),
        ("Admonition (Full)", "FullBattery_Admonition.mp3", "Full Battery"),
        ("Legacy (Full)", "BatteryFull_Legacy.wav", "Full Battery"),
        ("Snowy Glow", "LowBattery_Snowy_Glow.mp3", "Low Battery"),
        ("Admonition (Low)", "LowBattery_Admonition.mp3", "Low Battery"),
        ("Legacy (Low)", "BatteryLow_Legacy.wav", "Low Battery"),
    ];

    public static bool IsBundled(string? value) =>
        value != null && value.StartsWith(Prefix, StringComparison.Ordinal);

    public static string? GetFileName(string? value) =>
        IsBundled(value) ? value![Prefix.Length..] : null;

    public static string ToSettingsValue(string fileName) => Prefix + fileName;

    /// <summary>
    /// Resolves a "bundled:FileName.mp3" settings value to a cached file path.
    /// Extracts the resource on first access. Returns null if not found.
    /// </summary>
    public static string? Resolve(string? settingsValue)
    {
        var fileName = GetFileName(settingsValue);
        if (string.IsNullOrEmpty(fileName))
            return null;

        // Prevent path traversal
        if (fileName.Contains(Path.DirectorySeparatorChar) ||
            fileName.Contains(Path.AltDirectorySeparatorChar) ||
            fileName.Contains('\0'))
            return null;

        // Check if already cached
        Directory.CreateDirectory(CacheDir);
        var cachedPath = Path.Combine(CacheDir, fileName);
        if (File.Exists(cachedPath))
            return cachedPath;

        // Extract from Avalonia resources
        try
        {
            var uri = new Uri($"avares://BatteryNotifier/Assets/Sounds/{fileName}");
            using var stream = AssetLoader.Open(uri);
            using var fileStream = File.Create(cachedPath);
            stream.CopyTo(fileStream);
            return cachedPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the catalog of bundled sounds grouped by category.
    /// </summary>
    public static IReadOnlyList<(string Name, string SettingsValue, string Category)> GetCatalog()
    {
        return Catalog
            .Select(s => (s.Name, ToSettingsValue(s.FileName), s.Category))
            .ToList();
    }
}
