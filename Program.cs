using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using BatteryNotifier.Lib.Logger;
using BatteryNotifier.Lib.Services;
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
            try
            {
                BatteryNotifierLoggerConfig.InitializeLogger();
                BatteryNotifierAppLogger.LogStartup();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Log application information
                BatteryNotifierAppLogger.Info("Application Version: {Version}", Application.ProductVersion);
                BatteryNotifierAppLogger.Info("OS Version: {OS}", Environment.OSVersion);
                BatteryNotifierAppLogger.Info("CLR Version: {CLR}", Environment.Version);
                BatteryNotifierAppLogger.Info("Working Directory: {WorkingDir}", Environment.CurrentDirectory);

                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                using var eventWaitHandle =
                    new EventWaitHandle(false, EventResetMode.AutoReset, EventName, out bool createdNew);

                if (!createdNew)
                {
                    MessageBox.Show(@"Another instance of Battery Notifier is already running.", @"Instance Running",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _dashboard = new Dashboard();
                _dashboard.SetVersion(Version);

#if RELEASE
            if (InternetConnectivityHelper.CheckForInternetConnection())
            {
                NotificationService.Instance.PublishNotification("🤿 Checking for update ...");
                InitializeUpdateManagerAndCheckUpdatesAsync().ConfigureAwait(false);
            }
#endif

                Application.Run(_dashboard);
            }
            catch (Exception ex)
            {
                BatteryNotifierAppLogger.Fatal(ex, "Critical error during application startup");
                MessageBox.Show($@"A critical error occurred during startup:\n{ex.Message}", 
                    @"Battery Notifier Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                BatteryNotifierAppLogger.LogShutdown();
                BatteryNotifierLoggerConfig.ShutdownLogger();
            }
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
                            NotificationService.Instance.PublishNotification($"✅ Battery Notifier {release.Version} downloaded. Restart to apply.");
                        }
                    }
                    else
                    {
                        NotificationService.Instance.PublishNotification("✌ No Update Available");
                    }
                }
                else
                {
                    NotificationService.Instance.PublishNotification("🕹 Could not initialize update manager!",NotificationType.Inline);
                }
            }
            catch (Exception ex)
            {
                BatteryNotifierAppLogger.Fatal(ex, "Error checking for updates");
                NotificationService.Instance.PublishNotification("💀 Could not update app!",NotificationType.Inline);
            }
            finally
            {
                await Task.Delay(1000);
                NotificationService.Instance.PublishNotification(string.Empty);
            }
        }

        private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            BatteryNotifierAppLogger.Fatal(ex, "Unhandled domain exception occurred. Terminating: {IsTerminating}", e.IsTerminating);
            
            MessageBox.Show(
                $@"A critical error occurred:\n{ex?.Message ?? "Unknown error"}",
                @"Battery Notifier Critical Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            BatteryNotifierAppLogger.Error(e.Exception, "Unhandled thread exception occurred");
            
            var result = MessageBox.Show(
                $@"An unexpected error occurred:\n{e.Exception.Message}\n\nDo you want to continue?",
                @"Battery Notifier Application Error",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error);

            if (result == DialogResult.No)
            {
                Application.Exit();
            }
        }
    }
}
