using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;

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

        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static string AppName => Assembly.GetExecutingAssembly().GetName().Name.ToString();

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

    }
}
