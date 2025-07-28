using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BatteryNotifier.Utils
{
    internal static class UtilityHelper
    {
        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        public static string AssemblyVersion { get; } = ExecutingAssembly.GetName().Version?.ToString() ?? string.Empty;
        public static string AppName { get; } = ExecutingAssembly.GetName().Name ?? string.Empty;

        public static void StartExternalUrlProcess(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentException(@"URL cannot be null or empty.", nameof(url));

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Opens and returns a key where Windows stores paths to executables that load on startup
        /// </summary>
        public static RegistryKey? GetWindowsStartupAppsKey()
        {
            return Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
        }

        /// <summary>
        /// Determine whether it is a light theme
        /// </summary>
        public static bool IsLightTheme()
        {
            using var personalizeKey = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: false);

            if (personalizeKey == null) return false;

            var value = personalizeKey.GetValue("SystemUsesLightTheme") as int?;
            return value == 1;
        }

        // Checks if the filename ends with ".wav" ignoring case
        public static bool IsValidWavFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            return fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);
        }

        public static void RenderCheckboxState(Control control, bool @checked)
        {
            if (control is not CheckBox checkboxControl)
                throw new ArgumentException(@"Control must be a CheckBox", nameof(control));
            
            SafeInvoke(checkboxControl, () =>
            {
                checkboxControl.Checked = @checked;
                checkboxControl.Text = @checked ? "On" : "Off";
            });
        }
        
        public static void SafeInvoke(Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }
    }
}
