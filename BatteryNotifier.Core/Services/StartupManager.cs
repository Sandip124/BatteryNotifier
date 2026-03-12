using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

            Logger.Information("Startup {Action} successfully", enabled ? "enabled" : "disabled");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to {Action} startup", enabled ? "enable" : "disable");
        }
    }

    private static void SetWindowsStartup(bool enabled)
    {
        try
        {
            var startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (string.IsNullOrEmpty(startupDir)) return;

            var lnkFile = Path.Combine(startupDir, $"{AppName}.lnk");

            // Migrate: clean up legacy VBScript-based startup from previous versions
            var oldVbsFile = Path.Combine(startupDir, $"{AppName}.vbs");
            if (File.Exists(oldVbsFile))
            {
                try { File.Delete(oldVbsFile); } catch { }
            }

            if (enabled)
            {
                var executablePath = GetExecutablePath();
                if (string.IsNullOrEmpty(executablePath)) return;

#if WINDOWS
                CreateShortcut(lnkFile, executablePath);
#endif
            }
            else
            {
                if (File.Exists(lnkFile))
                {
                    File.Delete(lnkFile);
                }
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
                // No launchctl load — macOS automatically loads plist files
                // from ~/Library/LaunchAgents at login. Calling load here
                // would immediately spawn a duplicate instance.
            }
            else
            {
                if (File.Exists(plistPath))
                {
                    // Unload to prevent it from launching at next login
                    ExecuteCommand("launchctl", "bootout", $"gui/{GetUid()}", plistPath);
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
                // Look for a .png icon next to the executable for the desktop entry
                var iconPath = Path.Combine(Path.GetDirectoryName(executablePath) ?? ".", "BatteryNotifierLogo.png");

                var desktopContent = $"[Desktop Entry]\nType=Application\nName={AppName}\nExec={executablePath}\nHidden=false\nNoDisplay=false\nX-GNOME-Autostart-enabled=true";

                if (File.Exists(iconPath))
                {
                    desktopContent += $"\nIcon={iconPath}";
                }

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

    private static void ExecuteCommand(string command, params string[] args)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            // Use ArgumentList to prevent shell injection via crafted file paths
            foreach (var arg in args)
                process.StartInfo.ArgumentList.Add(arg);

            process.Start();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to execute command: {Command}", command);
        }
    }

    private static string GetUid()
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "id",
                    ArgumentList = { "-u" },
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var uid = process.StandardOutput.ReadToEnd().Trim();
            if (!process.WaitForExit(3000))
            {
                try { process.Kill(); } catch { }
            }
            return uid;
        }
        catch
        {
            return Environment.GetEnvironmentVariable("UID") ?? "501";
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
            var startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (string.IsNullOrEmpty(startupDir)) return false;

            // Check for current .lnk shortcut
            if (File.Exists(Path.Combine(startupDir, $"{AppName}.lnk")))
                return true;

            // Check for legacy .vbs (from previous versions, will be migrated on next enable)
            return File.Exists(Path.Combine(startupDir, $"{AppName}.vbs"));
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

    // ── Windows .lnk shortcut creation via COM interop ────────────────
    // Replaces the previous VBScript approach which triggered AV false positives.

#if WINDOWS
    private static void CreateShortcut(string lnkPath, string targetPath)
    {
        var link = (IShellLinkW)new ShellLink();
        try
        {
            link.SetPath(targetPath);
            link.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? "");
            link.SetDescription("BatteryNotifier - Battery monitoring app");

            var persistFile = (IPersistFile)link;
            persistFile.Save(lnkPath, true);
        }
        finally
        {
            Marshal.ReleaseComObject(link);
        }
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out ushort pwHotkey);
        void SetHotkey(ushort wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
#endif
}
