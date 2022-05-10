using System;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using BatteryNotifier.Helpers;
using System.Threading;
using BatteryNotifier.Manager;

namespace BatteryNotifier
{
    internal static class Program
    {
        private static string appGuid = "D2ED1949-C00C-4F99-87DD-B5A6CE56A733";

        private static Form? MainForm;

        private static string version = UtilityHelper.AssemblyVersion;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledExpection);
            AppDomain.CurrentDomain.ProcessExit += OnExit;

            using Mutex mutex = new Mutex(false, "Global\\" + appGuid);
            if (!mutex.WaitOne(0, false))
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            MainForm = new Dashboard();
            var dashboard = MainForm as Dashboard;
#if RELEASE
            dashboard.TryUpdate();
#endif

            dashboard?.SetVersion(version);

            Application.Run(dashboard);

        }


        private static void OnExit(object? sender, EventArgs e)
        {
            UpdateHelper.UpdateManager?.Dispose();
        }

        static void OnUnhandledExpection(object sender, UnhandledExceptionEventArgs args)
        {
            MessageBox.Show(args.ExceptionObject.ToString(), "Battery Notifier error!");
        }

    }
}