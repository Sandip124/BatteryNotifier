using System;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using BatteryNotifier.Helpers;
using System.Threading;

namespace BatteryNotifier
{
    internal static class Program
    {
        private static string appGuid = "D2ED1949-C00C-4F99-87DD-B5A6CE56A733";
        private static string version = UtilityHelper.AssemblyVersion;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using Mutex mutex = new Mutex(false, "Global\\" + appGuid);
            if (!mutex.WaitOne(0, false))
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var dashboard = new Dashboard();
            dashboard?.SetVersion(version);

            Application.Run(dashboard);

        }
    }
}