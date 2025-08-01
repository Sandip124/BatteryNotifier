using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using NAudio.Wave;

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

            var hasValidExtension = fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);
            if (!hasValidExtension) return false;

            try
            {
                using var reader = new WaveFileReader(fileName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
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

        public static void SafeInvoke(Control? control, Action action)
        {
            if (control.InvokeRequired)
                control.BeginInvoke(action);
            else
                action();
        }

        public static void EnableDoubleBuffering(Control ctrl)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(ctrl, true, null);
        }

        public static void EnableDoubleBufferingRecursively(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                EnableDoubleBuffering(ctrl);
                if (ctrl.HasChildren)
                {
                    EnableDoubleBufferingRecursively(ctrl);
                }
            }
        }

        public static Rectangle GetImageUpdateRegion(Image? oldImage, Image newImage)
        {
            // For GIF animations, typically the entire image changes
            // but you could implement more sophisticated clipping here
            // based on your specific animation patterns

            if (oldImage != null && oldImage.Size != newImage.Size)
            {
                // Size changed, need full redraw
                return new Rectangle(0, 0,
                    Math.Max(oldImage.Width, newImage.Width),
                    Math.Max(oldImage.Height, newImage.Height));
            }

            // For most GIF animations, return the full image bounds
            // In more complex scenarios, you could analyze pixel differences
            return new Rectangle(0, 0, newImage.Width, newImage.Height);
        }
    }
}