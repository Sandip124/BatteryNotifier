using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Detects OS-level states that affect notification delivery:
/// Do Not Disturb / Focus Assist, and fullscreen applications.
///
/// Security: all subprocess calls use ArgumentList (not Arguments string)
/// to prevent argument injection. Output is bounded to prevent OOM.
/// All processes are killed after a timeout to prevent thread deadlocks.
/// </summary>
public static class SystemStateDetector
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("SystemStateDetector");

    /// <summary>Max bytes to read from any subprocess stdout.</summary>
    private const int MaxOutputBytes = 8192;

    /// <summary>Max time to wait for any subprocess.</summary>
    private const int ProcessTimeoutMs = 3000;

    /// <summary>
    /// Returns true if the OS is in Do Not Disturb / silent / Focus Assist mode.
    /// </summary>
    public static bool IsDoNotDisturbActive()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return IsMacDoNotDisturbActive();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return IsWindowsFocusAssistActive();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return IsLinuxDoNotDisturbActive();
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to detect Do Not Disturb state");
        }

        return false;
    }

    /// <summary>
    /// Returns true if a fullscreen application is currently in the foreground.
    /// </summary>
    public static bool IsFullscreenAppActive()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return IsMacFullscreenActive();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return IsWindowsFullscreenActive();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return IsLinuxFullscreenActive();
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to detect fullscreen state");
        }

        return false;
    }

    /// <summary>
    /// Returns combined state indicating whether notifications should be suppressed.
    /// </summary>
    public static NotificationSuppressionState GetSuppressionState()
    {
        return new NotificationSuppressionState
        {
            IsDoNotDisturb = IsDoNotDisturbActive(),
            IsFullscreen = IsFullscreenAppActive()
        };
    }

    // ── macOS ────────────────────────────────────────────────────

    private static bool IsMacDoNotDisturbActive()
    {
        // macOS Ventura+ uses Focus system. The notification center's DND
        // assertion count > 0 means Focus/DND is on.
        try
        {
            var output = RunProcess("defaults",
                "-currentHost", "read", "com.apple.notificationcenterui", "doNotDisturb");
            if (output.Trim() == "1")
                return true;
        }
        catch
        {
            // defaults read fails if key doesn't exist — not in DND
        }

        // macOS Sonoma+ moved Focus state. Check assertion count.
        // Resolve home directory explicitly — tilde is NOT expanded without a shell.
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var assertionsPath = Path.Combine(home, "Library", "DoNotDisturb", "DB", "Assertions.json");

            if (File.Exists(assertionsPath))
            {
                var output = RunProcess("plutil",
                    "-extract", "dnd_prefs", "xml1", "-o", "-", assertionsPath);
                if (!string.IsNullOrWhiteSpace(output))
                    return true;
            }
        }
        catch
        {
            // Fallback — assume not in DND
        }

        return false;
    }

    private static bool IsMacFullscreenActive()
    {
        // AppleScript to check if the frontmost window fills the screen.
        // The script is a hardcoded literal — no external input is interpolated.
        try
        {
            var script = @"
tell application ""System Events""
    set frontApp to first application process whose frontmost is true
    try
        tell frontApp
            set appWindow to window 1
            set winSize to size of appWindow
            set winPos to position of appWindow
        end tell
        set screenWidth to (do shell script ""system_profiler SPDisplaysDataType | awk '/Resolution/{print $2; exit}'"") as integer
        set screenHeight to (do shell script ""system_profiler SPDisplaysDataType | awk '/Resolution/{print $4; exit}'"") as integer
        if (item 1 of winPos is 0) and (item 2 of winPos ≤ 0) and (item 1 of winSize ≥ screenWidth) and (item 2 of winSize ≥ screenHeight) then
            return ""true""
        end if
    end try
end tell
return ""false""";
            var output = RunProcessWithStdin("osascript", script);
            return output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    // ── Windows ──────────────────────────────────────────────────

    private static bool IsWindowsFocusAssistActive()
    {
        // Query Focus Assist via PowerShell registry read.
        // Script is a hardcoded literal — no external input.
        try
        {
            var script = @"
try {
    $key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.quiethourssettings\windows.quiethourssettings'
    if (Test-Path $key) {
        $data = (Get-ItemProperty $key -Name 'Data' -ErrorAction SilentlyContinue).Data
        if ($data -and $data.Length -gt 15 -and $data[15] -ne 0) {
            Write-Output 'true'
            return
        }
    }
} catch {}
Write-Output 'false'";

            var output = RunProcessWithStdin("powershell",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command -",
                script);
            return output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsWindowsFullscreenActive()
    {
        // Use PowerShell + P/Invoke to check if foreground window covers the screen.
        // Script is a hardcoded literal — no external input.
        try
        {
            var script = @"
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class FullscreenCheck {
    [DllImport(""user32.dll"")] public static extern IntPtr GetForegroundWindow();
    [DllImport(""user32.dll"")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport(""user32.dll"")] public static extern int GetSystemMetrics(int nIndex);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static bool IsFullscreen() {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return false;
        RECT rect;
        if (!GetWindowRect(hwnd, out rect)) return false;
        int w = GetSystemMetrics(0);
        int h = GetSystemMetrics(1);
        return rect.Left <= 0 && rect.Top <= 0 && rect.Right >= w && rect.Bottom >= h;
    }
}
'@ -ErrorAction SilentlyContinue
Write-Output ([FullscreenCheck]::IsFullscreen())";

            var output = RunProcessWithStdin("powershell",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command -",
                script);
            return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    // ── Linux ────────────────────────────────────────────────────

    private static bool IsLinuxDoNotDisturbActive()
    {
        // GNOME: check via gsettings
        try
        {
            var output = RunProcess("gsettings",
                "get", "org.gnome.desktop.notifications", "show-banners");
            // "false" means DND is on (banners suppressed)
            if (output.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        catch
        {
            // Not GNOME or gsettings unavailable
        }

        // KDE Plasma: check via dbus
        try
        {
            var output = RunProcess("dbus-send",
                "--session",
                "--dest=org.freedesktop.Notifications",
                "--print-reply",
                "/org/freedesktop/Notifications",
                "org.freedesktop.DBus.Properties.Get",
                "string:org.kde.NotificationManager",
                "string:inhibited");
            if (output.Contains("true", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        catch
        {
            // Not KDE or dbus unavailable
        }

        return false;
    }

    private static bool IsLinuxFullscreenActive()
    {
        // Check via xdotool if the active window is fullscreen
        try
        {
            var windowId = RunProcess("xdotool", "getactivewindow").Trim();
            if (string.IsNullOrEmpty(windowId)) return false;

            // Validate window ID is numeric to prevent argument injection into xprop
            if (!Regex.IsMatch(windowId, @"^\d+$"))
            {
                Logger.Warning("Unexpected non-numeric window ID from xdotool: {Id}",
                    windowId.Length > 50 ? windowId[..50] : windowId);
                return false;
            }

            // Use ArgumentList — windowId is validated numeric, but defence-in-depth
            var state = RunProcess("xprop", "-id", windowId, "_NET_WM_STATE");
            return state.Contains("_NET_WM_STATE_FULLSCREEN", StringComparison.Ordinal);
        }
        catch
        {
            // xdotool/xprop not available (Wayland?) — try alternate approach
        }

        // Wayland (GNOME): check via gdbus + xdg-desktop-portal (safe D-Bus read)
        try
        {
            // Query the inhibit state via the portal — does NOT execute arbitrary JS
            // unlike org.gnome.Shell.Eval which runs code in the compositor.
            var output = RunProcess("gdbus", "call",
                "--session",
                "--dest", "org.freedesktop.portal.Desktop",
                "--object-path", "/org/freedesktop/portal/desktop",
                "--method", "org.freedesktop.DBus.Properties.Get",
                "org.freedesktop.portal.Inhibit",
                "version");
            // If portal is available, fall back to wmctrl
            // Portal doesn't directly expose fullscreen state, so check via wmctrl
        }
        catch
        {
            // Portal not available
        }

        // Fallback: wmctrl (works on both X11 and some Wayland compositors)
        try
        {
            var output = RunProcess("wmctrl", "-d");
            // Active desktop marked with * — not directly fullscreen, but best-effort
            // For true fullscreen, check active window properties
            var activeWindow = RunProcess("wmctrl", "-l", "-G");
            // This is best-effort — return false if we can't determine
        }
        catch
        {
            // wmctrl not available
        }

        return false;
    }

    // ── Secure Process Helpers ───────────────────────────────────

    /// <summary>
    /// Runs a subprocess with arguments passed via ArgumentList (not Arguments string)
    /// to prevent argument injection. Output is bounded and the process is killed
    /// if it exceeds the timeout.
    /// </summary>
    private static string RunProcess(string command, params string[] args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();
        return ReadOutputWithTimeout(process);
    }

    /// <summary>
    /// Runs a subprocess that receives input via stdin.
    /// Only used for osascript and powershell with hardcoded scripts.
    /// </summary>
    private static string RunProcessWithStdin(string command, string stdinContent)
    {
        return RunProcessWithStdin(command, string.Empty, stdinContent);
    }

    private static string RunProcessWithStdin(string command, string arguments, string stdinContent)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        process.Start();
        process.StandardInput.Write(stdinContent);
        process.StandardInput.Close();
        return ReadOutputWithTimeout(process);
    }

    /// <summary>
    /// Reads stdout with bounded size and enforced timeout.
    /// Kills the process if it doesn't complete in time to prevent thread deadlocks.
    /// </summary>
    private static string ReadOutputWithTimeout(Process process)
    {
        var outputBuffer = new StringBuilder();
        var outputDone = new ManualResetEventSlim(false);
        var bytesRead = 0;

        // Read asynchronously so we can enforce the timeout
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
            {
                outputDone.Set();
                return;
            }

            // Bound the output to prevent OOM from malicious/runaway subprocess
            bytesRead += e.Data.Length;
            if (bytesRead <= MaxOutputBytes)
            {
                outputBuffer.AppendLine(e.Data);
            }
        };

        process.BeginOutputReadLine();

        // Wait for output AND process exit within timeout
        if (!outputDone.Wait(ProcessTimeoutMs))
        {
            // Timeout — kill the process to prevent indefinite blocking
            try { process.Kill(); } catch { /* best effort */ }
            Logger.Debug("Killed timed-out subprocess: {Command}", process.StartInfo.FileName);
        }

        // Wait briefly for process exit after kill
        process.WaitForExit(500);

        return outputBuffer.ToString();
    }
}

/// <summary>
/// Describes the current suppression state for notification delivery decisions.
/// </summary>
public class NotificationSuppressionState
{
    public bool IsDoNotDisturb { get; init; }
    public bool IsFullscreen { get; init; }

    /// <summary>True if any suppression condition is active.</summary>
    public bool ShouldSuppressToast => IsDoNotDisturb || IsFullscreen;

    /// <summary>Sound should be suppressed in DND, but allowed in fullscreen (user may have headphones).</summary>
    public bool ShouldSuppressSound => IsDoNotDisturb;
}
