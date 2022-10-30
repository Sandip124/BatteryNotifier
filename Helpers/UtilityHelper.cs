using BatteryNotifier.Providers;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace BatteryNotifier.Helpers
{
    internal class UtilityHelper
    {
        /// <summary>
        /// Somethimes registry strings can have garbage in them and are not properly null-terminated for C#, this function is required for all strings retrieved from the registry
        /// </summary>
        /// <param name="str">String fresh out of the registry</param>
        /// <returns>Properly null-terminated string suitable for future use with C#</returns>
        public static string NullTerminate(string str)
        {
            if (!str.Contains("\0"))
            {
                return str;
            }

            return str.Substring(0, str.IndexOf('\0'));
        }

        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        public static string AppName => Assembly.GetExecutingAssembly().GetName().Name!.ToString();

        public static void StartExternalUrlProcess(string url)
        {
            ProcessStartInfo sInfo = new(url)
            {
                UseShellExecute = true
            };
            Process.Start(sInfo);
        }

        /// <summary>
        /// Opens and returns a key where Windows stores paths to executables that load on startup
        /// </summary>
        public static RegistryKey GetWindowsStartupAppsKey()
        {
            var currentUserRegKey = Registry.CurrentUser;

            return currentUserRegKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        }

        /// <summary>
        ///     Determine whether it is a light theme
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool IsLightTheme()
        {
            using var personalizeKey =
                   Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            return (int)(personalizeKey?.GetValue("SystemUsesLightTheme", 0) ?? 0) == 1;
        }

        //Just Checking extension for wav file
        public static bool IsValidWavFile(string fileName)
        {
            return fileName.EndsWith(".wav");
        }

        public static void RenderCheckboxState(Control control, bool @checked)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));

            var checkboxControl = (control as CheckBox)!;

            checkboxControl.Checked = @checked;
            checkboxControl.Text = @checked ? "On" : "Off";
        }

        public static Icon RenderBadge(Bitmap bitmap, int width, int height, int textWidth, string batteryPercentage)
        {
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle textborder = new(bitmap.Width / 2 - textWidth / 2, bitmap.Height / 2 - height / 3, textWidth, height);

            var font = FontProvider.GetBoldFont(96);

            StringFormat stringFormat = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.FillEllipse(Brushes.DarkGreen, bitmap.Width / 2 - (width / 2), bitmap.Height / 2 - height / 3, width, height);
            g.DrawString(batteryPercentage.ToString(), font, Brushes.White, textborder, stringFormat);
            g.Dispose();
            font.Dispose();

            return Icon.FromHandle(bitmap.GetHicon());
        }

    }
}
