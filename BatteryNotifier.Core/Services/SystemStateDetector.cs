using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
    /// Tracks whether the initial accessibility-based DND check has been performed.
    /// After the first check, we rely on Darwin notify for changes and skip the
    /// accessibility click (which visibly opens the Control Center dropdown).
    /// </summary>
    private static bool _macDndInitialCheckDone;
    private static bool _macDndLastKnownState;

    /// <summary>
    /// Returns true if the OS is in Do Not Disturb / silent / Focus Assist mode.
    /// </summary>
    public static bool IsDoNotDisturbActive()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return IsMacDoNotDisturbActive();

#if WINDOWS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return IsWindowsFocusAssistActive();
#endif

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return IsLinuxDndActive();

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

#if WINDOWS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return IsWindowsFullscreenActive();
#endif

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
        // Try non-invasive approaches first: Monterey defaults → Ventura assertions file
        var result = IsMacDndViaDefaults() ?? IsMacDndViaAssertionsFile();

        if (result.HasValue)
        {
            _macDndLastKnownState = result.Value;
            _macDndInitialCheckDone = true;
            return result.Value;
        }

        // Tahoe+ fallback: Assertions.json is TCC-protected.
        // The accessibility approach clicks the Control Center dropdown (visible flicker).
        // Only do this ONCE for initial state, then rely on Darwin notify for changes.
        if (!_macDndInitialCheckDone)
        {
            _macDndInitialCheckDone = true;
            var accessibilityResult = IsMacDndViaAccessibility();
            if (accessibilityResult.HasValue)
            {
                _macDndLastKnownState = accessibilityResult.Value;
                return accessibilityResult.Value;
            }
        }

        // After initial check, return last known state.
        // Darwin notify (HasPendingFocusChange) will trigger a re-check
        // via the 2-minute fallback path which calls here again — but we
        // skip the accessibility click and just toggle the cached state
        // based on whether Darwin reported a change.
        return _macDndLastKnownState;
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

#if WINDOWS
    private static bool IsWindowsFocusAssistActive()
    {
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
    }

    private static bool IsWindowsFullscreenActive()
    {
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
    }
#endif

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
            if (DarwinNotifyCheck(_macFocusToken, out int check) == NotifyStatusOk && check != 0)
            {
                // Darwin notify fired — Focus/DND state changed. Toggle cached state
                // so IsMacDoNotDisturbActive returns the correct value without the
                // accessibility click (which causes visible Control Center flicker).
                _macDndLastKnownState = !_macDndLastKnownState;
                return true;
            }
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
            DarwinNotifyCancel(_macFocusToken);
            _macFocusToken = -1;
        }
    }

    // ── Linux: DND + fullscreen detection ──

    /// <summary>
    /// Linux DND detection. Tries KDE D-Bus Inhibited property first,
    /// then falls back to GNOME gsettings show-banners.
    /// </summary>
    private static bool IsLinuxDndActive()
    {
        return IsLinuxDndViaKde() ?? IsLinuxDndViaGnome() ?? false;
    }

    /// <summary>KDE Plasma: org.freedesktop.Notifications.Inhibited property.</summary>
    private static bool? IsLinuxDndViaKde()
    {
        var output = RunProcess("dbus-send",
            "--session", "--print-reply",
            "--dest=org.freedesktop.Notifications",
            "/org/freedesktop/Notifications",
            "org.freedesktop.DBus.Properties.Get",
            "string:org.freedesktop.Notifications",
            "string:Inhibited");

        if (string.IsNullOrWhiteSpace(output)) return null;

        // D-Bus reply: "variant  boolean true" or "variant  boolean false"
        if (output.Contains("boolean true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (output.Contains("boolean false", StringComparison.OrdinalIgnoreCase))
            return false;

        return null; // Unexpected format or error (e.g., GNOME returns InvalidArgs)
    }

    /// <summary>GNOME: gsettings show-banners (false = DND active).</summary>
    private static bool? IsLinuxDndViaGnome()
    {
        var output = RunProcess("gsettings", "get",
            "org.gnome.desktop.notifications", "show-banners");

        if (string.IsNullOrWhiteSpace(output)) return null;

        var trimmed = output.Trim();
        if (trimmed == "false") return true;  // banners off = DND on
        if (trimmed == "true") return false;

        return null;
    }

    /// <summary>
    /// Linux fullscreen detection via xprop + xdotool on X11.
    /// Returns false on Wayland (no reliable unprivileged API).
    /// </summary>
    private static bool IsLinuxFullscreenActive()
    {
        // Only works on X11 — xdotool/xprop don't work on native Wayland
        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        if (sessionType != null && sessionType.Equals("wayland", StringComparison.OrdinalIgnoreCase))
            return false;

        // Get active window ID
        var windowIdStr = RunProcess("xdotool", "getactivewindow").Trim();
        if (string.IsNullOrEmpty(windowIdStr) || !long.TryParse(windowIdStr, out _))
            return false;

        // Check if it has _NET_WM_STATE_FULLSCREEN
        var xpropOutput = RunProcess("xprop", "-id", windowIdStr, "_NET_WM_STATE");
        return xpropOutput.Contains("_NET_WM_STATE_FULLSCREEN", StringComparison.Ordinal);
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
            FileName = Constants.ResolveCommand(command),
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
            FileName = Constants.ResolveCommand(command),
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
