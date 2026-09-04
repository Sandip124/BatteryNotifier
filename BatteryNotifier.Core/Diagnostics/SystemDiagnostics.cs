using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Providers;
using BatteryNotifier.Core.Services;
using Serilog;

namespace BatteryNotifier.Core.Diagnostics;

/// <summary>
/// Collects a structured snapshot of the host + app state for bug reports.
/// Written to <c>{AppData}/BatteryNotifier/Logs/diagnostics.json</c> at startup — a single,
/// parseable file a user can send us to reason about platform-specific issues.
/// </summary>
public static class SystemDiagnostics
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("SystemDiagnostics");

    // External commands each feature depends on — presence explains failures per OS.
    private static readonly string[] MacTools = ["pmset", "afplay", "osascript", "launchctl", "defaults", "plutil"];
    private static readonly string[] LinuxTools =
    [
        "paplay", "pw-play", "aplay", "mpv", "ffplay",  // audio backends
        "notify-send",                                     // native toasts
        "gsettings", "dbus-send", "dbus-monitor",          // DND detection
        "wmctrl", "xprop", "xdotool",                      // fullscreen detection
        "upower", "launchctl"                              // battery / misc
    ];

    /// <summary>
    /// Builds the report. <paramref name="displays"/> is supplied by the UI layer (Core has no
    /// access to the screen list); pass null for a UI-free/early snapshot.
    /// </summary>
    public static DiagnosticsReport Collect(
        IReadOnlyList<DisplayInfo>? displays = null, bool includeLiveState = false)
    {
        var battery = BatteryInfoProvider.GetBatteryInfo();

        return new DiagnosticsReport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            App: new AppInfo(
                Constants.AppName,
                Constants.ApplicationVersion,
                Constants.SourceRepositoryUrl),
            Runtime: new RuntimeInfo(
                RuntimeInformation.FrameworkDescription,
                RuntimeInformation.RuntimeIdentifier,
                RuntimeInformation.OSArchitecture.ToString(),
                RuntimeInformation.ProcessArchitecture.ToString(),
                Environment.Is64BitProcess,
                Environment.ProcessorCount,
                Math.Round(Environment.WorkingSet / 1024.0 / 1024.0, 1),
                GCSettings.IsServerGC,
                CultureInfo.CurrentCulture.Name,
                CultureInfo.CurrentUICulture.Name),
            Os: new OsInfo(
                RuntimeInformation.OSDescription,
                Environment.OSVersion.VersionString,
                PlatformName(),
                Environment.MachineName,
                TimeZoneInfo.Local.Id,
                TimeZoneInfo.Local.BaseUtcOffset.ToString()),
            Windows: OperatingSystem.IsWindows() ? CollectWindows() : null,
            Mac: OperatingSystem.IsMacOS() ? CollectMac() : null,
            Linux: OperatingSystem.IsLinux() ? CollectLinux() : null,
            Battery: new BatteryInfoDto(
                battery.BatteryChargeStatus.ToString(),
                battery.PowerLineStatus.ToString(),
                (int)Math.Round(battery.BatteryLifePercent * 100),
                battery.BatteryLifeRemaining,
                OperatingSystem.IsLinux() ? Directory.Exists("/sys/class/power_supply") : null),
            Audio: new AudioInfo(
                SoundManager.WindowsAudioCompiled,
                OperatingSystem.IsLinux() ? AvailableTools(LinuxTools).Where(IsAudioPlayer).ToArray() : []),
            Settings: CollectSettings(),
            Displays: displays,
            // Live, momentary state — only for on-demand reports (heavier checks; user is reproducing).
            State: includeLiveState ? CollectLiveState() : null);
    }

    /// <summary>
    /// Writes the report to the Logs directory and returns its path (overwrites the single file).
    /// Pass <paramref name="includeLiveState"/> for an on-demand report that captures current
    /// DND/fullscreen at click time.
    /// </summary>
    public static string WriteReport(IReadOnlyList<DisplayInfo>? displays = null, bool includeLiveState = false)
    {
        var dir = Path.Combine(Constants.AppDataDirectory, "Logs");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "diagnostics.json");

        try
        {
            var json = JsonSerializer.Serialize(Collect(displays, includeLiveState), DiagnosticsJsonContext.Default.DiagnosticsReport);
            File.WriteAllText(path, json);
            Logger.Information("Diagnostics report written to {Path}", path);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to write diagnostics report to {Path}", path);
        }

        return path;
    }

    private static string PlatformName() =>
        OperatingSystem.IsWindows() ? "Windows"
        : OperatingSystem.IsMacOS() ? "macOS"
        : OperatingSystem.IsLinux() ? "Linux"
        : "Other";

    private static WindowsInfo CollectWindows() =>
        new(Environment.OSVersion.Version.ToString(), Environment.Is64BitOperatingSystem);

    private static MacInfo CollectMac() =>
        new(BatteryMonitorService.HasExternalDisplay(),
            BatteryMonitorService.IsClamshellMode(),
            SystemStateDetector.HasAccessibilityPermission(),
            ToolAvailability(MacTools));

    private static LinuxInfo CollectLinux()
    {
        var wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        var x11 = Environment.GetEnvironmentVariable("DISPLAY");
        return new LinuxInfo(
            Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"),
            wayland,
            x11,
            Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"),
            !string.IsNullOrEmpty(wayland) ? "Wayland" : !string.IsNullOrEmpty(x11) ? "X11" : "unknown",
            ReadLinuxDistro(),
            ReadFirstLine("/proc/sys/kernel/osrelease"),
            ToolAvailability(LinuxTools));
    }

    private static RuntimeState CollectLiveState()
    {
        try
        {
            var s = SystemStateDetector.GetSuppressionState();
            return new RuntimeState(SystemStateDetector.IsDoNotDisturbActive(), s.IsFullscreen);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Diagnostics: live state unavailable");
            return new RuntimeState(false, false);
        }
    }

    private static SettingsSnapshot? CollectSettings()
    {
        try
        {
            var s = AppSettings.Instance;
            return new SettingsSnapshot(
                s.ThemeMode.ToString(),
                s.NotificationPosition.ToString(),
                s.ScreenFlashEnabled,
                s.AlertVolume,
                s.LaunchAtStartup,
                StartupManager.IsStartupEnabled(), // actual OS registration — may differ from the setting
                s.AcAlerts,
                s.Alerts.Count);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Diagnostics: settings snapshot unavailable");
            return null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>Maps each candidate command to whether it's found on PATH.</summary>
    private static Dictionary<string, bool> ToolAvailability(string[] commands) =>
        commands.ToDictionary(c => c, IsOnPath);

    private static string[] AvailableTools(string[] commands) =>
        commands.Where(IsOnPath).ToArray();

    private static bool IsAudioPlayer(string cmd) =>
        cmd is "paplay" or "pw-play" or "aplay" or "mpv" or "ffplay";

    private static bool IsOnPath(string command)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar)) return false;

        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            try
            {
                if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, command)))
                    return true;
            }
            catch { /* unreadable PATH entry — skip */ }
        }
        return false;
    }

    private static string? ReadLinuxDistro()
    {
        try
        {
            foreach (var line in File.ReadLines("/etc/os-release"))
                if (line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal))
                    return line["PRETTY_NAME=".Length..].Trim('"');
        }
        catch (Exception ex) { Logger.Debug(ex, "Diagnostics: cannot read /etc/os-release"); }
        return null;
    }

    private static string? ReadFirstLine(string path)
    {
        try { return File.Exists(path) ? File.ReadLines(path).FirstOrDefault()?.Trim() : null; }
        catch { return null; }
    }
}

public sealed record DiagnosticsReport(
    string GeneratedAtUtc,
    AppInfo App,
    RuntimeInfo Runtime,
    OsInfo Os,
    WindowsInfo? Windows,
    MacInfo? Mac,
    LinuxInfo? Linux,
    BatteryInfoDto Battery,
    AudioInfo Audio,
    SettingsSnapshot? Settings,
    IReadOnlyList<DisplayInfo>? Displays,
    RuntimeState? State);

public sealed record AppInfo(string Name, string Version, string Repository);

public sealed record RuntimeInfo(
    string Framework, string RuntimeIdentifier,
    string OsArchitecture, string ProcessArchitecture,
    bool Is64BitProcess, int ProcessorCount, double WorkingSetMb,
    bool ServerGc, string Culture, string UiCulture);

public sealed record OsInfo(
    string Description, string Version, string Platform,
    string MachineName, string TimeZone, string UtcOffset);

public sealed record WindowsInfo(string OsVersion, bool Is64BitOs);

// ClamshellClosed: lid closed while running (driving an external display on power) 
public sealed record MacInfo(
    bool HasExternalDisplay, bool ClamshellClosed, bool AccessibilityPermission,
    Dictionary<string, bool> Tools);

public sealed record LinuxInfo(
    string? SessionType, string? WaylandDisplay, string? X11Display,
    string? Desktop, string ServerGuess,
    string? Distro, string? Kernel, Dictionary<string, bool> Tools);

public sealed record BatteryInfoDto(
    string ChargeStatus, string PowerLineStatus, int PercentApprox,
    int SecondsRemaining, bool? SysfsPresent);

public sealed record AudioInfo(bool WindowsAudioCompiled, string[] LinuxPlayersAvailable);

public sealed record DisplayInfo(
    string? Name, int X, int Y, int Width, int Height, double Scaling, bool IsPrimary);

public sealed record SettingsSnapshot(
    string Theme, string NotificationPosition, bool ScreenFlashEnabled,
    int AlertVolume, bool LaunchAtStartup, bool StartupRegistered, bool AcAlerts, int AlertCount);

// Momentary state captured only for on-demand reports.
public sealed record RuntimeState(bool DndActive, bool FullscreenActive);

[JsonSerializable(typeof(DiagnosticsReport))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class DiagnosticsJsonContext : JsonSerializerContext;
