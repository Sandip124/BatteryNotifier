using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using System.Threading.Tasks;
using Squirrel;
using BatteryNotifier.Helpers;
using System.Threading;

namespace BatteryNotifier
{
    internal static class Program
    {
        private static string appGuid = "D2ED1949-C00C-4F99-87DD-B5A6CE56A733";

        private static Task UpdateTask = new(CheckForUpdates);

        public static UpdateManager UpdateManager;

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
                MessageBox.Show("Battery Notifier is already running!");
                Process[] proc = Process.GetProcessesByName("BatteryNotifier");
                Interaction.AppActivate(proc[0].MainWindowTitle);
                return;
            }

#if RELEASE

            Task.Run(() => InitUpdateManager()).Wait();
            UpdateTask.Start();
            var version = UpdateManager.CurrentlyInstalledVersion().ToString();
#else
            var version = UtilityHelper.AssemblyVersion;

#endif
            

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Dashboard(version));

        }

        public static async Task InitUpdateManager()
        {
            try
            {
                UpdateManager = await UpdateManager.GitHubUpdateManager($@"{Constants.Constant.SourceUrl}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not initialize update manager!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static async void CheckForUpdates()
        {
            try
            {
                var updateInfo = await UpdateManager.CheckForUpdate();

                if (updateInfo.ReleasesToApply.Count > 0)
                {
                    var releaseEntry = await UpdateManager.UpdateApp();

                    if (releaseEntry != null)
                    {
                        MessageBox.Show($"Battery Notifier version {releaseEntry.Version.ToString()} has been downloaded!" +
                            $"\nUpdates will take effect after restrat!", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not update app!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void OnExit(object sender, EventArgs e)
        {
            UpdateManager?.Dispose();
        }

        static void OnUnhandledExpection(object sender, UnhandledExceptionEventArgs args)
        {
            MessageBox.Show(args.ExceptionObject.ToString(), "Battery Notifier error!");
        }

    }
}
