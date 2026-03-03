using System;
using System.IO;
using System.Runtime.InteropServices;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Manages application auto-startup on system boot
/// </summary>
public static class StartupManager
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("StartupManager");
    private const string AppName = "BatteryNotifier";

    public static void SetStartup(bool enabled)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetWindowsStartup(enabled);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                SetMacStartup(enabled);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                SetLinuxStartup(enabled);
            }

            Logger.Information($"Startup {(enabled ? "enabled" : "disabled")} successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to {(enabled ? "enable" : "disable")} startup");
        }
    }

    private static void SetWindowsStartup(bool enabled)
    {
        try
        {
            var executablePath = GetExecutablePath();
            var keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, true);
            if (key == null) return;

            if (enabled)
            {
                key.SetValue(AppName, $"\"{executablePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set Windows startup");
        }
    }

    private static void SetMacStartup(bool enabled)
    {
        try
        {
            var executablePath = GetExecutablePath();
            var launchAgentsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "LaunchAgents");

            if (!Directory.Exists(launchAgentsDir))
            {
                Directory.CreateDirectory(launchAgentsDir);
            }

            var plistPath = Path.Combine(launchAgentsDir, $"com.{AppName.ToLower()}.plist");

            if (enabled)
            {
                var plistContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>com.{AppName.ToLower()}</string>
    <key>ProgramArguments</key>
    <array>
        <string>{executablePath}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <false/>
</dict>
</plist>";

                File.WriteAllText(plistPath, plistContent);

                // Load the launch agent
                ExecuteCommand("launchctl", $"load \"{plistPath}\"");
            }
            else
            {
                if (File.Exists(plistPath))
                {
                    // Unload the launch agent
                    ExecuteCommand("launchctl", $"unload \"{plistPath}\"");
                    File.Delete(plistPath);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set macOS startup");
        }
    }

    private static void SetLinuxStartup(bool enabled)
    {
        try
        {
            var executablePath = GetExecutablePath();
            var autostartDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "autostart");

            if (!Directory.Exists(autostartDir))
            {
                Directory.CreateDirectory(autostartDir);
            }

            var desktopFilePath = Path.Combine(autostartDir, $"{AppName}.desktop");

            if (enabled)
            {
                var desktopContent = $@"[Desktop Entry]
Type=Application
Name={AppName}
Exec={executablePath}
Hidden=false
NoDisplay=false
X-GNOME-Autostart-enabled=true";

                File.WriteAllText(desktopFilePath, desktopContent);
            }
            else
            {
                if (File.Exists(desktopFilePath))
                {
                    File.Delete(desktopFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set Linux startup");
        }
    }

    private static string GetExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }

        // Fallback
        return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
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
            process.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to execute command: {command} {arguments}");
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IsWindowsStartupEnabled();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return IsMacStartupEnabled();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return IsLinuxStartupEnabled();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to check startup status");
        }

        return false;
    }

    private static bool IsWindowsStartupEnabled()
    {
        try
        {
            var keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsMacStartupEnabled()
    {
        try
        {
            var launchAgentsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "LaunchAgents");

            var plistPath = Path.Combine(launchAgentsDir, $"com.{AppName.ToLower()}.plist");
            return File.Exists(plistPath);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLinuxStartupEnabled()
    {
        try
        {
            var autostartDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "autostart");

            var desktopFilePath = Path.Combine(autostartDir, $"{AppName}.desktop");
            return File.Exists(desktopFilePath);
        }
        catch
        {
            return false;
        }
    }
}
