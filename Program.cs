using System;
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

        public static UpdateManager? UpdateManager;

        private static Form? MainForm;

        private static bool IsUpdateInProgress = false;

        private static string? version = UtilityHelper.AssemblyVersion;

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

            if (InternetConnectivityHelper.CheckForInternetConnection())
            {
                Task.Run(() => InitUpdateManager()).Wait();
                Task UpdateTask = new(CheckForUpdates);
                UpdateTask.Start();
                version = UpdateManager!.CurrentlyInstalledVersion().ToString();
                dashboard?.UpdateStatus("Checking for update ...");
                IsUpdateInProgress = true;
            }
#endif               
            dashboard?.SetVersion(version);

            Application.Run(dashboard);

        }

        public static async Task InitUpdateManager()
        {
            try
            {
                UpdateManager = await UpdateManager.GitHubUpdateManager($@"{Constants.Constant.SourceUrl}");
            }
            catch (Exception)
            {
                (MainForm as Dashboard)?.UpdateStatus("Could not initialize update manager!");
            }
        }

        static async void CheckForUpdates()
        {
            try
            {
                var updateInfo = await UpdateManager!.CheckForUpdate();

                if (!IsUpdateInProgress) return;

                if (updateInfo.ReleasesToApply.Count > 0)
                {
                    var releaseEntry = await UpdateManager.UpdateApp();

                    if (releaseEntry != null)
                    {
                        IsUpdateInProgress = false;
                        (MainForm as Dashboard)?.UpdateStatus($"Battery Notifier {releaseEntry.Version} downloaded. Restart to apply." );
                    }
                }
                else
                {
                   
                    IsUpdateInProgress = false;
                    (MainForm as Dashboard)?.UpdateStatus("No Update Available");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Could not update app!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
              Thread.Sleep(3000);
              (MainForm as Dashboard)?.UpdateStatus(string.Empty);
            }
        }

        private static void OnExit(object? sender, EventArgs e)
        {
            UpdateManager?.Dispose();
        }

        static void OnUnhandledExpection(object? sender, UnhandledExceptionEventArgs args)
        {
            MessageBox.Show(args.ExceptionObject.ToString(), "Battery Notifier error!");
        }

    }
}