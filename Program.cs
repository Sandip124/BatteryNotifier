using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using System.Threading.Tasks;
using Squirrel;
using BatteryNotifier.Helpers;

namespace BatteryNotifier
{
    internal static class Program
    {

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

            if (ProcessHelper.IsAlreadyRunning())
            {
                MessageBox.Show("Battery Notifier is already running!");
                //activate main page
                Process[] proc = Process.GetProcessesByName("BatteryNotifier");
                Interaction.AppActivate(proc[0].MainWindowTitle);
                return;
            }

            Task.Run(() => InitUpdateManager()).Wait();

            UpdateTask.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Dashboard());

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
