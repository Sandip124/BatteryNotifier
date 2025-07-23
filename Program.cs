using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using BatteryNotifier.Utils;
using Squirrel;

namespace BatteryNotifier
{
    internal static class Program
    {
        private static UpdateManager? _updateManager;
        private static Dashboard? _dashboard;
        private static readonly string EventName = "BatteryNotifier.Instance.WaitHandle";
        private static readonly string Version = UtilityHelper.AssemblyVersion;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            using var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventName, out bool createdNew);

            if (!createdNew)
            {
                MessageBox.Show("Another instance of Battery Notifier is already running.", "Instance Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _dashboard = new Dashboard();
            _dashboard.SetVersion(Version);

#if RELEASE
            if (InternetConnectivityHelper.CheckForInternetConnection())
            {
                _dashboard.Notify("🤿 Checking for update ...");
                InitializeUpdateManagerAndCheckUpdatesAsync().ConfigureAwait(false);
            }
#endif

            Application.Run(_dashboard);
        }

        private static async Task InitializeUpdateManagerAndCheckUpdatesAsync()
        {
            try
            {
                _updateManager = await UpdateManager.GitHubUpdateManager(Constants.Constant.SourceRepositoryUrl);

                if (_updateManager is not null)
                {
                    var updateInfo = await _updateManager.CheckForUpdate();
                    if (updateInfo.ReleasesToApply.Count > 0)
                    {
                        var release = await _updateManager.UpdateApp();
                        if (release != null)
                        {
                            _dashboard?.Notify($"✅ Battery Notifier {release.Version} downloaded. Restart to apply.");
                        }
                    }
                    else
                    {
                        _dashboard?.Notify("✌ No Update Available");
                    }
                }
                else
                {
                    _dashboard?.Notify("🕹 Could not initialize update manager!");
                }
            }
            catch (Exception)
            {
                _dashboard?.Notify("💀 Could not update app!");
            }
            finally
            {
                await Task.Delay(1000);
                _dashboard?.Notify(string.Empty);
            }
        }

        private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            _dashboard?.Notify("Unhandled exception occurred.");
        }
    }
}
