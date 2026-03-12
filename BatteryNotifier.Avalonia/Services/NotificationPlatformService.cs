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
            Logger.Information("Notification icon extracted to {Path}", _iconPath);
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

    private static void ShowWindowsNotification(string title, string message)
    {
        try
        {
            var safeTitle = SanitizeForXml(SanitizeForPowerShell(title));
            var safeMessage = SanitizeForXml(SanitizeForPowerShell(message));

            var iconXml = "";
            if (_iconPath != null)
            {
                var safeIconPath = _iconPath.Replace("\\", "/");
                iconXml = $"<image placement='appLogoOverride' src='file:///{SanitizeForXml(SanitizeForPowerShell(safeIconPath))}'/>";
            }

            var script = $@"
                [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                [Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

                $template = @""
                <toast>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text>{safeTitle}</text>
                            <text>{safeMessage}</text>
                            {iconXml}
                        </binding>
                    </visual>
                </toast>
                ""@

                $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
                $xml.LoadXml($template)
                $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
                [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('BatteryNotifier').Show($toast)
            ";

            ExecuteCommandWithStdin("powershell",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command -",
                script);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Windows notification");
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

    private static string SanitizeForPowerShell(string input)
    {
        return input
            .Replace("$", "")
            .Replace("`", "")
            .Replace("\"", "'")
            .Replace("\n", " ")
            .Replace("\r", "");
    }

    private static string SanitizeForXml(string input)
    {
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;");
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
        ExecuteCommandWithStdin(command, string.Empty, stdinContent);
    }

    private static void ExecuteCommandWithStdin(string command, string arguments, string stdinContent)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.StandardInput.Write(stdinContent);
            process.StandardInput.Close();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to execute command via stdin: {Command}", command);
        }
    }
}
