using System;

namespace BatteryNotifier.Avalonia;

/// <summary>
/// Centralized Avalonia resource URIs for assets bundled in the app.
/// Avoids duplicating "avares://BatteryNotifier/Assets/..." strings across the codebase.
/// </summary>
internal static class AssetUris
{
    private const string Base = "avares://BatteryNotifier/Assets";

    public static readonly Uri Logo48 = new($"{Base}/battery-notifier-logo-48.png");
    public static readonly Uri Logo128 = new($"{Base}/battery-notifier-logo-128.png");
    public static readonly Uri LogoIco = new($"{Base}/battery-notifier-logo.ico");

    public static Uri ForAsset(string fileName) => new($"{Base}/{fileName}");
    public static Uri ForSound(string fileName) => new($"{Base}/Sounds/{fileName}");
}
