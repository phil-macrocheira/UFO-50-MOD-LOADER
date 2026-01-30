using System.Diagnostics;

namespace UFO_50_Mod_Loader.Services
{
    public class LaunchGameService
    {
        public static async Task LaunchGameAsync()
        {
            if (SettingsService.Settings.OverwriteMode) {
                try {
                    Process.Start(new ProcessStartInfo {
                        FileName = "steam://run/1147860",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex) {
                    Logger.Log($"Failed to launch UFO 50 via Steam: {ex.Message}");
                }
            }
            else {
                try {
                    Process.Start(new ProcessStartInfo {
                        FileName = Models.Constants.ModdedCopyExePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex) {
                    Logger.Log($"Failed to launch UFO 50 Modded Copy: {ex.Message}");
                }
            }
        }
    }
}
