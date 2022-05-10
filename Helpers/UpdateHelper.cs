using Squirrel;
using System;
using System.Threading.Tasks;

namespace BatteryNotifier.Helpers
{
    internal static class UpdateHelper
    {

        public static Task UpdateTask = new(CheckForUpdates);

        public static UpdateManager? UpdateManager;

        public static bool IsUpdateInProgress = false;

        public static ReleaseEntry? ReleaseEntry;

        public static bool IsUpdateAvailable { get; set; } = false;

        public static async Task InitUpdateManager()
        {
            try
            {
                UpdateManager = await UpdateManager.GitHubUpdateManager($@"{Constants.Constant.SourceUrl}");
            }
            catch (Exception ex)
            {
                Logger.Logger.LogThisLine(ex.Message);                
                throw;
            }
        }

        public static async void CheckForUpdates()
        {

            try
            {
                var updateInfo = await UpdateManager?.CheckForUpdate()!;

                if (!IsUpdateInProgress) return;

                if (updateInfo.ReleasesToApply.Count > 0)
                {
                    var releaseEntry = await UpdateManager.UpdateApp();

                    if (releaseEntry != null)
                    {
                        IsUpdateInProgress = false;
                        IsUpdateAvailable = true;
                        ReleaseEntry = releaseEntry;
                    }
                }
                else
                {

                    IsUpdateInProgress = false;
                    IsUpdateAvailable = false;
                    ReleaseEntry = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.LogThisLine(ex.Message);
                throw new Exception("Could not update app!");
            }
        }

        public static void Dispose()
        {
            UpdateManager?.Dispose();
        }
    }
}
