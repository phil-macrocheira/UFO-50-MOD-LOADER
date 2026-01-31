using System.Diagnostics;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services
{
    public class LaunchGameService
    {
        public static Task LaunchGameAsync()
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
                if (Constants.IsLinux) {
                    var chmod = Process.Start(new ProcessStartInfo {
                        FileName = "chmod",
                        ArgumentList = { "+x", Constants.ModdedCopyExePath },
                        UseShellExecute = false
                    });
                    chmod?.WaitForExit();
                }

                try {
                    Process.Start(new ProcessStartInfo {
                        FileName = Constants.ModdedCopyExePath,
                        WorkingDirectory = Path.GetDirectoryName(Constants.ModdedCopyExePath),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex) {
                    Logger.Log($"Failed to launch UFO 50 Modded Copy: {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }
    }
}
