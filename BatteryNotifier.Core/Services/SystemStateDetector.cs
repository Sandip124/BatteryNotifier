using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    /// <summary>Extended timeout for accessibility-based checks that involve UI interaction.</summary>
    private const int AccessibilityTimeoutMs = 5000;

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
        // Try each detection approach in order: Monterey → Ventura+ → Tahoe+ fallback
        return IsMacDndViaDefaults()
            ?? IsMacDndViaAssertionsFile()
            ?? IsMacDndViaAccessibility()
            ?? false;
    }

    /// <summary>macOS Monterey and earlier: direct defaults key.</summary>
    private static bool? IsMacDndViaDefaults()
    {
        try
        {
            var output = RunProcess("defaults",
                "-currentHost", "read", "com.apple.notificationcenterui", "doNotDisturb");
            if (output.Trim() == "1")
                return true;
        }
        catch { /* Key doesn't exist on this macOS version */ }

        return null; // inconclusive — try next approach
    }

    /// <summary>macOS Ventura+: Focus state via notification center assertions JSON.</summary>
    private static bool? IsMacDndViaAssertionsFile()
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var assertionsPath = Path.Combine(home, "Library", "DoNotDisturb", "DB", "Assertions.json");

            if (!File.Exists(assertionsPath))
                return null;

            var output = RunProcess("plutil", "-convert", "json", "-o", "-", assertionsPath);
            if (string.IsNullOrWhiteSpace(output))
                return null;

            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;

            if (HasActiveAssertions(root, "storeAssertionRecords"))
                return true;

            // Some macOS versions nest under "data" array
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in data.EnumerateArray())
                {
                    if (HasActiveAssertions(entry, "storeAssertionRecords"))
                        return true;
                }
            }

            return false; // File readable, no active assertions — DND is off
        }
        catch
        {
            return null; // TCC blocked or parse error — try next approach
        }
    }

    /// <summary>
    /// macOS Tahoe+ fallback: Assertions.json is TCC-protected.
    /// Briefly opens Control Center Focus dropdown to check for "On" text.
    /// Requires Accessibility permission. Script is a hardcoded literal.
    /// </summary>
    private static bool? IsMacDndViaAccessibility()
    {
        try
        {
            var script = @"
tell application ""System Events""
    tell process ""ControlCenter""
        try
            set focusItem to (first menu bar item of menu bar 1 whose description contains ""Focus"")
        on error
            return ""false""
        end try
        click focusItem
        delay 0.4
        try
            tell group 1 of window ""Control Center""
                set allTexts to value of every static text
                repeat with t in allTexts
                    if t as text is ""On"" then
                        click focusItem
                        return ""true""
                    end if
                end repeat
            end tell
        end try
        click focusItem
        return ""false""
    end tell
end tell";
            var output = RunProcessWithStdin("osascript", script, AccessibilityTimeoutMs);
            if (output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        catch { /* Accessibility not granted — fail open */ }

        return null;
    }

    /// <summary>
    /// Checks if a JSON element contains a non-empty array property (active Focus assertions).
    /// </summary>
    private static bool HasActiveAssertions(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var records)
            && records.ValueKind == JsonValueKind.Array
            && records.GetArrayLength() > 0)
        {
            return true;
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

#if WINDOWS
    // P/Invoke declarations for fullscreen detection
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    // WNF (Windows Notification Facility) for Focus Assist / DND detection.
    // Stable across Windows 10 1803+ through Windows 11 24H2.
    // Source: https://github.com/DCourtel/Windows_10_Focus_Assist
    [DllImport("ntdll.dll")]
    private static extern int NtQueryWnfStateData(
        ref WNF_STATE_NAME stateName,
        IntPtr typeId,
        IntPtr explicitScope,
        out uint changeStamp,
        ref int buffer,
        ref int bufferSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct WNF_STATE_NAME { public uint Data1; public uint Data2; }
#endif

    private static bool IsWindowsFocusAssistActive()
    {
#if WINDOWS
        // WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED
        // Returns: 0 = OFF, 1 = PRIORITY_ONLY, 2 = ALARMS_ONLY
        try
        {
            var stateName = new WNF_STATE_NAME { Data1 = 0xA3BF1C75, Data2 = 0x0D83063E };
            int buffer = 0, bufferSize = sizeof(int);
            int status = NtQueryWnfStateData(ref stateName, IntPtr.Zero, IntPtr.Zero,
                out _, ref buffer, ref bufferSize);
            return status == 0 && buffer >= 1;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    private static bool IsWindowsFullscreenActive()
    {
#if WINDOWS
        // Direct P/Invoke — no PowerShell subprocess needed
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            if (!GetWindowRect(hwnd, out var rect)) return false;
            int w = GetSystemMetrics(SM_CXSCREEN);
            int h = GetSystemMetrics(SM_CYSCREEN);
            return rect.Left <= 0 && rect.Top <= 0 && rect.Right >= w && rect.Bottom >= h;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
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

        // KDE Plasma (and other freedesktop-compliant notification daemons):
        // Query the standardized Inhibited property.
        try
        {
            var output = RunProcess("dbus-send",
                "--session",
                "--dest=org.freedesktop.Notifications",
                "--print-reply",
                "/org/freedesktop/Notifications",
                "org.freedesktop.DBus.Properties.Get",
                "string:org.freedesktop.Notifications",
                "string:Inhibited");
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
            if (!Regex.IsMatch(windowId, @"^\d+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
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

    // ── macOS Focus State Change Monitor (Darwin notifications) ──

    /// <summary>Darwin notification token for Focus/DND state changes. -1 = not initialized.</summary>
    private static int _macFocusToken = -1;

    private static bool _macFocusMonitorInitialized;

    // P/Invoke for Darwin notification API (libSystem.B.dylib — macOS only).
    // These are only called when RuntimeInformation.IsOSPlatform(OSPlatform.OSX) is true.
    [DllImport("libSystem.B.dylib", EntryPoint = "notify_register_check")]
    private static extern uint DarwinNotifyRegisterCheck(string name, out int outToken);

    [DllImport("libSystem.B.dylib", EntryPoint = "notify_check")]
    private static extern uint DarwinNotifyCheck(int token, out int check);

    [DllImport("libSystem.B.dylib", EntryPoint = "notify_cancel")]
    private static extern uint DarwinNotifyCancel(int token);

    private const uint NotifyStatusOk = 0;

    /// <summary>
    /// Registers for macOS Focus/DND state change notifications via Darwin notification center.
    /// Call once at app startup. Safe to call on non-macOS platforms (no-op).
    /// </summary>
    public static void InitializeFocusMonitor()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || _macFocusMonitorInitialized)
            return;

        _macFocusMonitorInitialized = true;

        try
        {
            if (DarwinNotifyRegisterCheck("com.apple.donotdisturb.stateChanged", out _macFocusToken) != NotifyStatusOk)
                _macFocusToken = -1;

            // Consume the initial "dirty" flag so the first call to HasPendingFocusChange() is clean
            if (_macFocusToken >= 0)
                DarwinNotifyCheck(_macFocusToken, out _);
        }
        catch
        {
            _macFocusToken = -1;
        }
    }

    /// <summary>
    /// Returns true if a Focus/DND state change has been posted since the last call.
    /// This is a trivial in-process memory read — no subprocess, no IPC, safe to poll frequently.
    /// Returns false on non-macOS platforms or if the monitor failed to initialize.
    /// </summary>
    public static bool HasPendingFocusChange()
    {
        if (_macFocusToken < 0)
            return false;

        try
        {
            if (DarwinNotifyCheck(_macFocusToken, out int check) == NotifyStatusOk)
                return check != 0;
        }
        catch { /* P/Invoke failure — monitor is broken */ }

        return false;
    }

    /// <summary>
    /// Cleans up the Darwin notification registration. Call on app shutdown.
    /// </summary>
    public static void CleanupFocusMonitor()
    {
        if (_macFocusToken >= 0)
        {
            try { DarwinNotifyCancel(_macFocusToken); } catch { }
            _macFocusToken = -1;
        }
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
    /// Runs a subprocess that receives input via stdin (e.g., osascript with hardcoded scripts).
    /// </summary>
    private static string RunProcessWithStdin(string command, string stdinContent, int? timeoutMs = null)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        process.Start();
        process.StandardInput.Write(stdinContent);
        process.StandardInput.Close();
        return ReadOutputWithTimeout(process, timeoutMs ?? ProcessTimeoutMs);
    }

    /// <summary>
    /// Reads stdout with bounded size and enforced timeout.
    /// Kills the process if it doesn't complete in time to prevent thread deadlocks.
    /// </summary>
    private static string ReadOutputWithTimeout(Process process, int timeoutMs = 0)
    {
        if (timeoutMs <= 0) timeoutMs = ProcessTimeoutMs;
        var outputBuffer = new StringBuilder();
        using var outputDone = new ManualResetEventSlim(false);
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
        if (!outputDone.Wait(timeoutMs))
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
public sealed class NotificationSuppressionState
{
    public bool IsDoNotDisturb { get; init; }
    public bool IsFullscreen { get; init; }

    /// <summary>True if any suppression condition is active.</summary>
    public bool ShouldSuppressToast => IsDoNotDisturb || IsFullscreen;

    /// <summary>Sound should be suppressed in DND, but allowed in fullscreen (user may have headphones).</summary>
    public bool ShouldSuppressSound => IsDoNotDisturb;
}
