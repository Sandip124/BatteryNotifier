using System.Collections.Frozen;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Maps known process names to actionable battery-saving tips,
/// and identifies system processes that should be excluded from drainer lists.
/// </summary>
public static class ProcessTips
{
    /// <summary>
    /// Low-level OS processes that always run and aren't actionable by the user.
    /// Used by PowerUsageService to filter noise from the process list.
    /// </summary>
    public static readonly FrozenSet<string> SystemProcesses = ((string[])
    [
        // macOS
        "kernel_task", "launchd", "WindowServer", "loginwindow",
        // Windows
        "svchost", "System Idle Process", "System", "Registry",
        // Linux
        "systemd", "idle", "init", "kthreadd",
    ]).ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    // Shared tip strings to avoid repeated literals
    private const string CloseTabs = "Close unused tabs to save battery";
    private const string CloseSpaces = "Close unused spaces or tabs";
    private const string QuitMessaging = "Quit when not actively messaging";
    private const string SpotlightIndexing = "Spotlight indexing — will finish soon";
    private const string PauseContainers = "Pause unused containers or VMs";
    private const string LowerResolution = "Lower resolution to save battery";

    /// <summary>Exact-match tips (case-insensitive).</summary>
    private static readonly Dictionary<string, string> ExactTips = new(StringComparer.OrdinalIgnoreCase)
    {
        ["node"] = "Check for runaway processes",
        ["nodejs"] = "Check for runaway processes",
        ["mds"] = SpotlightIndexing,
        ["mds_stores"] = SpotlightIndexing,
        ["mdworker"] = SpotlightIndexing,
        ["mdworker_shared"] = SpotlightIndexing,
        ["photoanalysisd"] = "Photo analysis — will finish soon",
        ["backupd"] = "Time Machine backup in progress",
        ["compilerassetscatalog"] = "Xcode build in progress",
    };

    /// <summary>Substring-match tips, checked in order. First match wins.</summary>
    private static readonly (string Pattern, string Tip)[] SubstringTips =
    [
        // Browsers
        ("chrome", CloseTabs),
        ("chromium", CloseTabs),
        ("firefox", CloseTabs),
        ("safari", CloseTabs),
        ("edge", CloseTabs),
        ("opera", CloseTabs),
        ("brave", CloseTabs),
        ("arc", CloseSpaces),
        // Communication
        ("slack", QuitMessaging),
        ("discord", "Quit when not in a call"),
        ("teams", "Quit when not in a meeting"),
        ("zoom", "Quit after your meeting ends"),
        ("telegram", QuitMessaging),
        ("whatsapp", QuitMessaging),
        // Media
        ("spotify", "Download music instead of streaming"),
        ("vlc", LowerResolution),
        ("iina", LowerResolution),
        // Dev tools & VMs
        ("docker", PauseContainers),
        ("hyperkit", PauseContainers),
        ("qemu", PauseContainers),
        // System
        ("softwareupdate", "System update in progress"),
        ("xcodebuild", "Xcode build in progress"),
    ];

    public static string? GetTip(string processName)
    {
        if (ExactTips.TryGetValue(processName, out var exact))
            return exact;

        var lower = processName.ToLowerInvariant();
        foreach (var (pattern, tip) in SubstringTips)
        {
            if (lower.Contains(pattern, StringComparison.Ordinal))
                return tip;
        }

        return null;
    }
}
