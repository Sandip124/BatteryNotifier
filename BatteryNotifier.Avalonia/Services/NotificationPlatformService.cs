using System;
using System.Runtime.InteropServices;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// Platform-specific notification service for native notifications
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
            // Use osascript to trigger native macOS notification
            var script = $@"display notification ""{EscapeForAppleScript(message)}"" with title ""{EscapeForAppleScript(title)}""";
            ExecuteCommand("osascript", $"-e '{script}'");
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
            // Use PowerShell to trigger native Windows notification
            var script = $@"
                [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                [Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

                $template = @""
                <toast>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text>{title}</text>
                            <text>{message}</text>
                        </binding>
                    </visual>
                </toast>
                ""@

                $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
                $xml.LoadXml($template)
                $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
                [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Battery Notifier').Show($toast)
            ";

            ExecuteCommand("powershell", $"-Command \"{script}\"");
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
            // Use notify-send for Linux notifications
            ExecuteCommand("notify-send", $"\"{title}\" \"{message}\"");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Linux notification");
        }
    }

    private static string EscapeForAppleScript(string input)
    {
        return input.Replace("\"", "\\\"").Replace("'", "\\'");
    }

    private static void ExecuteCommand(string command, string arguments)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(5000); // 5 second timeout
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to execute command: {command} {arguments}");
        }
    }
}
