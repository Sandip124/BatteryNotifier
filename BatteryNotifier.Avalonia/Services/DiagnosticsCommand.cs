using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using BatteryNotifier.Core.Diagnostics;
using BatteryNotifier.Core.Logger;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// On-demand diagnostics: writes a fresh report reflecting the current state (clamshell, DND,
/// settings, displays)
/// </summary>
public static class DiagnosticsCommand
{
    /// <summary>Builds the display list from a window's screens</summary>
    public static IReadOnlyList<DisplayInfo>? DisplaysFrom(Window? window)
    {
        var screens = window?.Screens?.All;
        if (screens is not { Count: > 0 }) return null;

        return screens.Select(s => new DisplayInfo(
            s.DisplayName, s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height, s.Scaling, s.IsPrimary))
            .ToList();
    }

    /// <summary>Regenerates the report with live state + displays, then opens the Logs folder.</summary>
    public static void Generate()
    {
        try
        {
            var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var path = SystemDiagnostics.WriteReport(DisplaysFrom(window), includeLiveState: true);
            PlatformHelper.OpenUrl(Path.GetDirectoryName(path)!);
        }
        catch (Exception ex)
        {
            BatteryNotifierAppLogger.ForContext("DiagnosticsCommand").Warning(ex, "Failed to generate diagnostics report");
        }
    }
}
