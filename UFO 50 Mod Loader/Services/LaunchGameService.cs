using System.Diagnostics;

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
                if (Models.Constants.IsLinux) {
                    var chmod = Process.Start(new ProcessStartInfo {
                        FileName = "chmod",
                        ArgumentList = { "+x", Models.Constants.ModdedCopyExePath },
                        UseShellExecute = false
                    });
                    chmod?.WaitForExit();
                }

                try {
                    Process.Start(new ProcessStartInfo {
                        FileName = Models.Constants.ModdedCopyExePath,
                        WorkingDirectory = Path.GetDirectoryName(Models.Constants.ModdedCopyExePath),
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
