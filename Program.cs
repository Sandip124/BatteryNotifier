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

        private static string? version = UtilityHelper.AssemblyVersion;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                var appId = Setting.appSetting.Default.AppId;
                if (string.IsNullOrEmpty(appId))
                {
                    appId = Setting.appSetting.Default.AppId = Guid.NewGuid().ToString();
                    Setting.appSetting.Default.Save();
                }

                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledExpection);

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
                    dashboard?.Notify("🤿 Checking for update ...");
                    Task.Run( () =>InitUpdateManager()).Wait();
                    Task UpdateTask = new(CheckForUpdates);
                    UpdateTask.Start();
                    version = UpdateManager?.CurrentlyInstalledVersion().ToString();
                }
#endif
                dashboard?.SetVersion(version);

                Application.Run(dashboard);
            }
            catch (Exception e)
            {
                (MainForm as Dashboard)?.Notify(e.Message);
            }

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
                var updateInfo = await UpdateManager?.CheckForUpdate()!;

                if (updateInfo.ReleasesToApply.Count > 0)
                {
                    var releaseEntry = await UpdateManager.UpdateApp();

                    if (releaseEntry != null)
                    {
                        (MainForm as Dashboard)?.Notify($"✅ Battery Notifier {releaseEntry.Version} downloaded. Restart to apply.");
                    }
                }
                else
                {
                    (MainForm as Dashboard)?.Notify("✌ No Update Available");
                }
            }
            catch (Exception)
            {
                (MainForm as Dashboard)?.Notify("💀 Could not update app!");
            }
        }

        static void OnUnhandledExpection(object? sender, UnhandledExceptionEventArgs args)
        {
            (MainForm as Dashboard)?.Notify("Unhandled exception occured.");
        }

    }
}