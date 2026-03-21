using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BatteryNotifier.Core;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// Cross-platform helpers for URL opening and text sanitization.
/// </summary>
internal static class PlatformHelper
{
    /// <summary>Opens a URL in the default browser using platform-appropriate commands.</summary>
    public static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var psi = new ProcessStartInfo(Constants.ResolveCommand("open")) { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var p = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            var psi = new ProcessStartInfo(Constants.ResolveCommand("xdg-open")) { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
        }
    }

    /// <summary>Strips control characters and truncates text from external sources (e.g. GitHub API).</summary>
    public static string SanitizeExternalText(string input, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var chars = new char[Math.Min(input.Length, maxLength)];
        int j = 0;
        for (int i = 0; i < input.Length && j < chars.Length; i++)
        {
            if (!char.IsControl(input[i]))
                chars[j++] = input[i];
        }
        return new string(chars, 0, j);
    }
}
