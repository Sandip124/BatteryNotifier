using BatteryNotifier.Forms;
using BatteryNotifier.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BatteryNotifier.Manager
{
    public static class AppUpdatemanager
    {
        public static void TryUpdate(this Dashboard? dashboard)
        {
            try
            {
                UpdateHelper.IsUpdateInProgress = true;
                Task.Run(() => UpdateHelper.InitUpdateManager()).Wait();
                UpdateHelper.UpdateTask.Start();
                dashboard?.UpdateStatus("Checking for update ...");

                if (UpdateHelper.IsUpdateAvailable)
                {
                    dashboard?.UpdateStatus($"Battery Notifier { UpdateHelper.ReleaseEntry?.Version} downloaded.Restart to apply.");
                }
                else
                {
                    dashboard?.UpdateStatus("No update available");
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.LogThisLine(ex.Message);                
                dashboard?.UpdateStatus("Update failed.");
            }
            finally
            {
                Thread.Sleep(5000);
                UpdateHelper.IsUpdateInProgress = false;
                dashboard?.UpdateStatus("");
            }
        }
    }
}
