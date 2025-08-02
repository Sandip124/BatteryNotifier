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
        private static string _version = UtilityHelper.AssemblyVersion;
        
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
               

#if RELEASE
            if (UtilityHelper.CheckForInternetConnection())
            {
                NotificationService.Instance.PublishNotification("🤿 Checking for update ...");
                Task.Run(async () => await InitializeAndCheckForUpdatesAsync());
            }
#endif
                _dashboard.SetVersion(_version);
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
        
        private static async Task InitializeAndCheckForUpdatesAsync()
        {
            try
            {
                await InitUpdateManager();
                
                if (_updateManager != null)
                {
                    await CheckForUpdates();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.PublishNotification("💀 Could not initialize update manager!", NotificationType.Inline);
                BatteryNotifierAppLogger.Error(ex, "Error during update initialization or check");
            }
        }

        private static async Task InitUpdateManager()
        {
            try
            {
                _updateManager = await UpdateManager.GitHubUpdateManager($@"{Constants.Constant.SourceRepositoryUrl}");
                _version = _updateManager?.CurrentlyInstalledVersion()?.ToString() ?? _version;
            }
            catch (Exception)
            {
                NotificationService.Instance.PublishNotification("🕹 Could not initialize update manager!",NotificationType.Inline);
                throw;
            }
        }

        private static async Task CheckForUpdates()
        {
            try
            {
                if (_updateManager == null)
                {
                    NotificationService.Instance.PublishNotification("🕹 Could not initialize update manager!",NotificationType.Inline);
                    BatteryNotifierAppLogger.Error("Update manager is not initialized.");
                    return;
                }

                var updateInfo = await _updateManager.CheckForUpdate();
                if (updateInfo.ReleasesToApply.Count > 0)
                {
                    var releaseEntry = await _updateManager.UpdateApp();
                    if (releaseEntry != null)
                    {
                        NotificationService.Instance.PublishNotification($"✅ Battery Notifier {releaseEntry.Version} downloaded. Restart to apply.");
                    }
                }
                else
                {
                    NotificationService.Instance.PublishNotification("✌ No Update Available");
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
                NotificationService.Instance.PublishNotification(string.Empty, NotificationType.Inline);
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
        
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
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
