using System.IO;
using System.Reflection;

namespace BatteryNotifier.Core;

public static class Constants
{
    public const string SourceRepositoryUrl = "https://github.com/Sandip124/BatteryNotifier";
    public const string ReleaseUrl = "https://github.com/Sandip124/BatteryNotifier/releases/latest";

    /// <summary>
    /// Application version derived from the assembly's informational version attribute,
    /// which is set automatically from the .csproj &lt;Version&gt; property.
    /// Single source of truth: BatteryNotifier.Avalonia.csproj → &lt;Version&gt;
    /// </summary>
    public static readonly string ApplicationVersion = ResolveVersion();

    private static string ResolveVersion()
    {
        // Try the entry assembly first (normal app launch)
        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        // Fallback: executing assembly (works in test runners where GetEntryAssembly() is null)
        version ??= Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (string.IsNullOrEmpty(version))
            return "0.0.0";

        // Strip git hash suffix if present (e.g. "3.2.0+abc123def" → "3.2.0")
        var plusIndex = version.IndexOf('+');
        return plusIndex >= 0 ? version[..plusIndex] : version;
    }

    public const string AppName = "BatteryNotifier";

    public const int DefaultNotificationTimeout = 3000;
    public const string LowBatteryTag = "LowBattery";
    public const string FullBatteryTag = "FullBattery";

    /// <summary>App data directory (settings, custom sounds, logs).</summary>
    public static string AppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

    /// <summary>App temp directory (cached built-in sounds, bundled sound extraction).</summary>
    public static string AppTempDirectory => Path.Combine(Path.GetTempPath(), AppName);
}