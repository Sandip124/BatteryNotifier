using System;
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
            // Pass the script via stdin to avoid shell argument injection entirely.
            var script = $"display notification \"{SanitizeForAppleScript(message)}\" with title \"{SanitizeForAppleScript(title)}\"";
            ExecuteCommandWithStdin("osascript", script);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show macOS notification");
        }
    }

    private static void ShowWindowsNotification(string title, string message)
    {
        try
        {
            // Sanitize inputs to prevent PowerShell injection.
            var safeTitle = SanitizeForXml(SanitizeForPowerShell(title));
            var safeMessage = SanitizeForXml(SanitizeForPowerShell(message));

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
                        </binding>
                    </visual>
                </toast>
                ""@

                $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
                $xml.LoadXml($template)
                $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
                [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Battery Notifier').Show($toast)
            ";

            // Pass script via stdin instead of -Command argument to avoid argument injection.
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
            // Pass title and message as separate arguments (no shell interpretation).
            // notify-send expects: notify-send <summary> [body]
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
            process.StartInfo.ArgumentList.Add(safeTitle);
            process.StartInfo.ArgumentList.Add(safeMessage);
            process.Start();
            process.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Linux notification");
        }
    }

    /// <summary>
    /// Escapes characters that could break out of an AppleScript string literal.
    /// </summary>
    private static string SanitizeForAppleScript(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    /// <summary>
    /// Strips characters that could be used for PowerShell injection.
    /// </summary>
    private static string SanitizeForPowerShell(string input)
    {
        // Remove $, `, and backtick which PowerShell uses for variable expansion and escaping
        return input
            .Replace("$", "")
            .Replace("`", "")
            .Replace("\"", "'")
            .Replace("\n", " ")
            .Replace("\r", "");
    }

    /// <summary>
    /// Escapes XML special characters for the toast notification XML template.
    /// </summary>
    private static string SanitizeForXml(string input)
    {
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;");
    }

    /// <summary>
    /// Strips control characters from plain text (for Linux notify-send).
    /// </summary>
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
            process.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to execute command via stdin: {Command}", command);
        }
    }
}
