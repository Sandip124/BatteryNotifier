namespace BatteryNotifier.Core.Services;

/// <summary>
/// Maps known process names to actionable battery-saving tips.
/// </summary>
public static class ProcessTips
{
    public static string? GetTip(string processName)
    {
        var lower = processName.ToLowerInvariant();

        // Browsers — tabs are the #1 battery drain cause
        if (lower.Contains("chrome") || lower.Contains("chromium"))
            return "Close unused tabs to save battery";
        if (lower.Contains("firefox"))
            return "Close unused tabs to save battery";
        if (lower.Contains("safari"))
            return "Close unused tabs to save battery";
        if (lower.Contains("edge"))
            return "Close unused tabs to save battery";
        if (lower.Contains("opera"))
            return "Close unused tabs to save battery";
        if (lower.Contains("brave"))
            return "Close unused tabs to save battery";
        if (lower.Contains("arc"))
            return "Close unused spaces or tabs";

        // Communication — often run in background unnecessarily
        if (lower.Contains("slack"))
            return "Quit when not actively messaging";
        if (lower.Contains("discord"))
            return "Quit when not in a call";
        if (lower.Contains("teams"))
            return "Quit when not in a meeting";
        if (lower.Contains("zoom"))
            return "Quit after your meeting ends";
        if (lower.Contains("telegram"))
            return "Quit when not actively messaging";
        if (lower.Contains("whatsapp"))
            return "Quit when not actively messaging";

        // Media
        if (lower.Contains("spotify"))
            return "Download music instead of streaming";
        if (lower.Contains("vlc") || lower.Contains("iina"))
            return "Lower resolution to save battery";

        // Dev tools & VMs
        if (lower.Contains("docker") || lower.Contains("hyperkit") || lower.Contains("qemu"))
            return "Pause unused containers or VMs";
        if (lower is "node" or "nodejs")
            return "Check for runaway processes";

        // macOS system processes
        if (lower is "mds_stores" or "mdworker" or "mds" or "mdworker_shared")
            return "Spotlight indexing — will finish soon";
        if (lower == "photoanalysisd")
            return "Photo analysis — will finish soon";
        if (lower == "backupd")
            return "Time Machine backup in progress";
        if (lower.Contains("softwareupdate"))
            return "System update in progress";
        if (lower == "compilerassetscatalog" || lower.Contains("xcodebuild"))
            return "Xcode build in progress";

        return null;
    }
}
