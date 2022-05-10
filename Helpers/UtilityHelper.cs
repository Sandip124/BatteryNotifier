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

        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version!.ToString();

    }
}
