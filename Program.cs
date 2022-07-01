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
            var appId = Setting.appSetting.Default.AppId;
            if (string.IsNullOrEmpty(appId))
            {
                appId = Setting.appSetting.Default.AppId = Guid.NewGuid().ToString();
                Setting.appSetting.Default.Save();
            }
                        
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledExpection);
            AppDomain.CurrentDomain.ProcessExit += OnExit;

            using Mutex mutex = new(false, "Global\\" + appId);
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
                dashboard?.UpdateStatus("🤿 Checking for update ...");
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
                UpdateManager = await UpdateManager.GitHubUpdateManager($@"{Constants.Constant.SourceRepositoryUrl}");
            }
            catch (Exception)
            {
                (MainForm as Dashboard)?.Notify("🕹 Could not initialize update manager!");
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
                        (MainForm as Dashboard)?.Notify($"✅ Battery Notifier {releaseEntry.Version} downloaded. Restart to apply." );
                    }
                }
                else
                {
                   
                    IsUpdateInProgress = false;
                    (MainForm as Dashboard)?.Notify("✌ No Update Available");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("💀 Could not update app!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void OnExit(object? sender, EventArgs e)
        {
            UpdateManager?.Dispose();
        }

        static void OnUnhandledExpection(object? sender, UnhandledExceptionEventArgs args)
        {
            MessageBox.Show(args.ExceptionObject.ToString(),"Battery Notifier error!");
        }

    }
}