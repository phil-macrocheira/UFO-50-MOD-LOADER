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
                        FileName = $"steam://run/{Game.Metadata.SteamAppID}",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex) {
                    Logger.Log($"Failed to launch {Game.Metadata.GameName} via Steam: {ex.Message}");
                }
            }
            else {
                if (Constants.IsLinux) {
                    var chmod = Process.Start(new ProcessStartInfo {
                        FileName = "chmod",
                        ArgumentList = { "+x", Game.Paths.ModdedCopyExePath },
                        UseShellExecute = false
                    });
                    chmod?.WaitForExit();
                }

                try {
                    Process.Start(new ProcessStartInfo {
                        FileName = Game.Paths.ModdedCopyExePath,
                        WorkingDirectory = Path.GetDirectoryName(Game.Paths.ModdedCopyExePath),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex) {
                    Logger.Log($"Failed to launch {Game.Metadata.GameName} Modded Copy: {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }
    }
}
