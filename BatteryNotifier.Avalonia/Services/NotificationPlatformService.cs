using System;
using System.IO;
using System.Runtime.InteropServices;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// Platform-specific notification service for native notifications.
/// All user-facing strings are sanitized before being passed to subprocesses.
/// </summary>
public static class NotificationPlatformService
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("NotificationPlatformService");
    private static string? _iconPath;

    /// <summary>
    /// Call once at startup (on the UI thread) to extract the icon for notifications.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BatteryNotifier");
            Directory.CreateDirectory(tempDir);
            var iconFile = Path.Combine(tempDir, "notification-icon.png");

            // Only re-extract if missing (survives across app restarts)
            if (!File.Exists(iconFile))
            {
                using var stream = global::Avalonia.Platform.AssetLoader.Open(
                    new Uri("avares://BatteryNotifier/Assets/battery-notifier-logo-128.png"));
                using var fs = File.Create(iconFile);
                stream.CopyTo(fs);
            }

            _iconPath = iconFile;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to extract notification icon — notifications will have no icon");
        }
    }

    public static void ShowNativeNotification(string title, string message)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ShowMacNotification(title, message);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ShowWindowsNotification(title, message);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ShowLinuxNotification(title, message);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show native notification");
        }
    }

    private static void ShowMacNotification(string title, string message)
    {
        try
        {
            // Try terminal-notifier first (supports custom icon), fall back to osascript.
            if (TryShowWithTerminalNotifier(title, message))
                return;

            // osascript: icon comes from the calling app's bundle (set via MacOSDockIconHelper).
            var script = $"display notification \"{SanitizeForAppleScript(message)}\" with title \"{SanitizeForAppleScript(title)}\"";
            ExecuteCommandWithStdin("osascript", script);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show macOS notification");
        }
    }

    private static bool TryShowWithTerminalNotifier(string title, string message)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "terminal-notifier",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("-title");
            process.StartInfo.ArgumentList.Add(title);
            process.StartInfo.ArgumentList.Add("-message");
            process.StartInfo.ArgumentList.Add(message);
            process.StartInfo.ArgumentList.Add("-sender");
            process.StartInfo.ArgumentList.Add("com.batterynotifier.app");
            if (_iconPath != null)
            {
                process.StartInfo.ArgumentList.Add("-appIcon");
                process.StartInfo.ArgumentList.Add(_iconPath);
            }
            process.Start();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
                return false;
            }
            return process.ExitCode == 0;
        }
        catch
        {
            // terminal-notifier not installed — fall back to osascript
            return false;
        }
    }

    // ── Windows: Shell_NotifyIcon balloon tip (same API WinForms used) ──

    private static WindowsBalloonNotifier? _winNotifier;

    private static void ShowWindowsNotification(string title, string message)
    {
        try
        {
            _winNotifier ??= new WindowsBalloonNotifier();
            _winNotifier.ShowBalloonTip(title, message);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Windows notification");
        }
    }
    
    
    private sealed class WindowsBalloonNotifier : IDisposable
    {
        private const int NimAdd = 0x00;
        private const int NimModify = 0x01;
        private const int NimDelete = 0x02;
        private const int NimSetVersion = 0x04;
        private const int NifTip = 0x04;
        private const int NifInfo = 0x10;
        private const int NiifInfo = 0x01;
        private const int NotifyIconVersion4 = 4;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NotifyIconData
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Shell_NotifyIconW(int dwMessage, ref NotifyIconData lpData);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowExW(
            int dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandleW(IntPtr lpModuleName);

        private IntPtr _hWnd;
        private bool _iconAdded;
        private bool _disposed;
        private const int IconId = 1;

        public WindowsBalloonNotifier()
        {
            // Create a hidden message-only window (HWND_MESSAGE parent)
            var hInstance = GetModuleHandleW(IntPtr.Zero);
            _hWnd = CreateWindowExW(
                0, "STATIC", "BatteryNotifier_NotifyWnd", 0,
                0, 0, 0, 0,
                new IntPtr(-3), // HWND_MESSAGE
                IntPtr.Zero, hInstance, IntPtr.Zero);

            if (_hWnd == IntPtr.Zero) return;

            // Register a hidden tray icon (no visible icon, just for balloon tips)
            var nid = new NotifyIconData
            {
                cbSize = Marshal.SizeOf<NotifyIconData>(),
                hWnd = _hWnd,
                uID = IconId,
                uFlags = NifTip,
                szTip = "BatteryNotifier",
                szInfo = "",
                szInfoTitle = ""
            };

            _iconAdded = Shell_NotifyIconW(NimAdd, ref nid);
            if (_iconAdded)
            {
                nid.uTimeoutOrVersion = NotifyIconVersion4;
                Shell_NotifyIconW(NimSetVersion, ref nid);
            }
        }

        public void ShowBalloonTip(string title, string message)
        {
            if (!_iconAdded || _hWnd == IntPtr.Zero) return;

            // Truncate to Win32 limits
            if (title.Length > 63) title = title[..63];
            if (message.Length > 255) message = message[..255];

            var nid = new NotifyIconData
            {
                cbSize = Marshal.SizeOf<NotifyIconData>(),
                hWnd = _hWnd,
                uID = IconId,
                uFlags = NifInfo,
                szInfo = message,
                szInfoTitle = title,
                dwInfoFlags = NiifInfo,
                szTip = "BatteryNotifier"
            };

            Shell_NotifyIconW(NimModify, ref nid);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_iconAdded)
            {
                var nid = new NotifyIconData
                {
                    cbSize = Marshal.SizeOf<NotifyIconData>(),
                    hWnd = _hWnd,
                    uID = IconId,
                    szInfo = "",
                    szInfoTitle = "",
                    szTip = ""
                };
                Shell_NotifyIconW(NimDelete, ref nid);
            }

            if (_hWnd != IntPtr.Zero)
            {
                DestroyWindow(_hWnd);
                _hWnd = IntPtr.Zero;
            }
        }
    }

    private static void ShowLinuxNotification(string title, string message)
    {
        try
        {
            var safeTitle = SanitizePlainText(title);
            var safeMessage = SanitizePlainText(message);

            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "notify-send",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            if (_iconPath != null)
            {
                process.StartInfo.ArgumentList.Add("-i");
                process.StartInfo.ArgumentList.Add(_iconPath);
            }
            process.StartInfo.ArgumentList.Add(safeTitle);
            process.StartInfo.ArgumentList.Add(safeMessage);
            process.Start();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Linux notification");
        }
    }

    private static string SanitizeForAppleScript(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    private static string SanitizePlainText(string input)
    {
        var chars = new char[input.Length];
        int j = 0;
        foreach (var c in input)
        {
            if (!char.IsControl(c))
                chars[j++] = c;
        }
        return new string(chars, 0, j);
    }

    private static void ExecuteCommandWithStdin(string command, string stdinContent)
    {
        ExecuteCommandWithStdin(command, [], stdinContent);
    }

    private static void ExecuteCommandWithStdin(string command, string[] args, string stdinContent)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = command,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            foreach (var arg in args)
                process.StartInfo.ArgumentList.Add(arg);

            process.Start();
            process.StandardInput.Write(stdinContent);
            process.StandardInput.Close();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
                Logger.Warning("Command {Command} timed out after 5s", command);
            }
            else if (process.ExitCode != 0)
            {
                var stderr = process.StandardError.ReadToEnd();
                Logger.Warning("Command {Command} exited with code {Code}: {Error}",
                    command, process.ExitCode, stderr.Length > 500 ? stderr[..500] : stderr);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to execute command via stdin: {Command}", command);
        }
    }
}
