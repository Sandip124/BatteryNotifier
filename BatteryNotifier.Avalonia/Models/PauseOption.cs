using System;
using System.Collections.Generic;

namespace BatteryNotifier.Avalonia.Models;

/// <summary>A selectable "pause notifications for" duration. Null duration = until manually resumed.</summary>
public sealed record PauseOption(string Label, TimeSpan? Duration);

/// <summary>Single source of truth for the pause durations offered from the tray menu and the main window.</summary>
public static class NotificationPauseOptions
{
    public static readonly IReadOnlyList<PauseOption> All =
    [
        new("30 minutes", TimeSpan.FromMinutes(30)),
        new("1 hour", TimeSpan.FromHours(1)),
        new("2 hours", TimeSpan.FromHours(2)),
        new("Until I turn it back on", null),
    ];
}
