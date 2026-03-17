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
            var tempDir = Core.Constants.AppTempDirectory;
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

    // ── Windows: PowerShell toast notification ──────────────────────────

    private static void ShowWindowsNotification(string title, string message)
    {
        try
        {
            var safeTitle = SanitizeForPowerShell(title);
            var safeMessage = SanitizeForPowerShell(message);
            var safeXmlTitle = SanitizeForXml(safeTitle);
            var safeXmlMessage = SanitizeForXml(safeMessage);

            // Use Windows toast notification API via PowerShell stdin.
            // AppId binds to the app's Start Menu entry so toasts group correctly.
            var iconArg = _iconPath != null
                ? $@"<image placement=""appLogoOverride"" src=""{SanitizeForXml(_iconPath)}"" />"
                : "";

            var script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom, ContentType = WindowsRuntime] | Out-Null
$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml('<toast><visual><binding template=""ToastGeneric""><text>{safeXmlTitle}</text><text>{safeXmlMessage}</text>{iconArg}</binding></visual></toast>')
$toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('BatteryNotifier').Show($toast)
";
            ExecuteCommandWithStdin("powershell", ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass"], script);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Windows notification");
        }
    }

    private static string SanitizeForPowerShell(string input)
    {
        // Strip characters that could escape PowerShell string interpolation
        return input
            .Replace("$", "")
            .Replace("`", "")
            .Replace("\n", " ")
            .Replace("\r", "");
    }

    private static string SanitizeForXml(string input)
    {
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;")
            .Replace("\"", "&quot;");
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
