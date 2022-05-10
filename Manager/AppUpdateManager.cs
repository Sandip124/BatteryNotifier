using BatteryNotifier.Forms;
using BatteryNotifier.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            catch (Exception)
            {
                MessageBox.Show("Update failed");
            }
            finally
            {
                Thread.Sleep(5000);
                UpdateHelper.IsUpdateInProgress = false;
            }
        }
    }
}
