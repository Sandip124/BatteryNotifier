using System.Reflection;

namespace BatteryNotifier.Core;

public static class Constants
{
    internal const string GitHubOwner = "Sandip124";
    internal const string GitHubRepo = "BatteryNotifier";
    public static readonly string SourceRepositoryUrl = $"https://github.com/{GitHubOwner}/{GitHubRepo}";

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

    /// <summary>
    /// Unified duration for notification card display, screen flash, and sound playback.
    /// </summary>
    public const int NotificationDurationMs = 30000;

    public const int DefaultNotificationTimeout = 3000;
    /// <summary>Timeout for quick subprocess queries (battery info, UID lookup).</summary>
    public const int ProcessTimeoutShortMs = 3000;

    /// <summary>Timeout for heavier subprocess queries (health check, display profiler).</summary>
    public const int ProcessTimeoutMs = 5000;

    /// <summary>Max characters to read from subprocess stdout to prevent OOM.</summary>
    public const int MaxProcessOutputLength = 32768;

    public const string LowBatteryTag = "LowBattery";
    public const string FullBatteryTag = "FullBattery";

    /// <summary>App data directory (settings, custom sounds, logs).</summary>
    public static string AppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

    /// <summary>App temp directory (cached built-in sounds, bundled sound extraction).</summary>
    public static string AppTempDirectory => Path.Combine(Path.GetTempPath(), AppName);

    /// <summary>
    /// Resolves a command name to an absolute path using known system directories only.
    /// Avoids PATH-based resolution which could execute binaries from writable directories.
    /// Returns the absolute path if found, or the original name as fallback (e.g., Windows commands
    /// or user-installed tools like terminal-notifier that have no fixed location).
    /// </summary>
    public static string ResolveCommand(string command)
    {
        // Only resolve on Unix-like systems where we know the standard directories
        if (OperatingSystem.IsWindows())
            return command;

        ReadOnlySpan<string> searchDirs =
        [
            "/usr/bin",
            "/usr/sbin",
            "/usr/local/bin",
            "/bin",
            "/sbin",
        ];

        foreach (var dir in searchDirs)
        {
            var fullPath = Path.Combine(dir, command);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return command; // fallback for user-installed tools (e.g., terminal-notifier via Homebrew)
    }
}