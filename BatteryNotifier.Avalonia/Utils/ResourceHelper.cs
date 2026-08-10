using System;
using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Avalonia.Utils;

/// <summary>Shared lookups for app resources: geometry icons and cached image assets.</summary>
public static class ResourceHelper
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext(nameof(ResourceHelper));
    private static readonly ConcurrentDictionary<string, Bitmap?> BitmapCache = new();

    /// <summary>Resolves a <see cref="Geometry"/> resource by key from app + merged dictionaries.</summary>
    public static Geometry? ResolveGeometry(string key)
    {
        var app = Application.Current;
        if (app?.Resources.TryGetResource(key, null, out var res) == true && res is Geometry geo)
            return geo;

        foreach (var dict in app?.Resources.MergedDictionaries ?? [])
            if (dict.TryGetResource(key, null, out var r) && r is Geometry g)
                return g;

        return null;
    }

    /// <summary>Loads (and caches) a bitmap asset by file name from /Assets. Null on failure.</summary>
    public static Bitmap? LoadBitmap(string fileName) =>
        BitmapCache.GetOrAdd(fileName, static key =>
        {
            try
            {
                using var stream = AssetLoader.Open(AssetUris.ForAsset(key));
                return new Bitmap(stream);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to load image asset {Asset}", key);
                return null;
            }
        });
}
