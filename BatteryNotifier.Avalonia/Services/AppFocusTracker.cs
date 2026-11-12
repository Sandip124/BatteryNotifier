using System;
using System.Runtime.InteropServices;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// Answers "does this application currently own the OS input focus?" — the primitive used
/// to decide whether the flyout-style main window should auto-hide when it deactivates.
///
/// Returns <c>null</c> when focus can't be reliably determined (Linux/X11/Wayland), letting
/// callers fall back to a best-effort window-active check.
/// </summary>
internal static class AppFocusTracker
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("AppFocusTracker");

    /// <summary>true = our app is frontmost, false = another app is, null = unknown.</summary>
    public static bool? IsApplicationFocused()
    {
        if (OperatingSystem.IsWindows()) return IsWindowsAppFocused();
        if (OperatingSystem.IsMacOS()) return IsMacAppActive();
        return null; // Linux — caller falls back to a window-active check
    }

    // ── Windows: is the foreground window owned by our process? ──

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private static bool? IsWindowsAppFocused()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            _ = GetWindowThreadProcessId(hwnd, out var pid);
            return pid == (uint)Environment.ProcessId;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Windows foreground-window focus check failed");
            return null;
        }
    }

    // ── macOS: NSApplication.sharedApplication.isActive ──
    // Stays true while our own status-bar menu or child windows are open; goes false only
    // when the user switches to another application.

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector);

    private static bool? IsMacAppActive()
    {
        try
        {
            var nsApp = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
            if (nsApp == IntPtr.Zero) return null;
            return objc_msgSend_bool(nsApp, sel_registerName("isActive"));
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "macOS NSApp.isActive focus check failed");
            return null;
        }
    }
}
