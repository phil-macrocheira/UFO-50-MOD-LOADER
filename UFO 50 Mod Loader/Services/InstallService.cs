using GMLoader;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services
{
    public class InstallService
    {
        public static async Task<bool> InstallModsAsync()
        {
            Logger._showingConflicts = false; // Return to main log if installation started with conflict warnings
            string gamePath = SettingsService.Settings.GamePath;

            if (!SettingsService.Settings.CopiedGameFiles) {
                Logger.Log($"Cannot Install Mods: Vanilla game files have not been copied yet");
                return false;
            }

            await AddModNamesAsync();

            if (!SettingsService.Settings.OverwriteMode) {
                if (Directory.Exists(Constants.ModdedCopyPath))
                    Directory.Delete(Constants.ModdedCopyPath, recursive: true);
                await Task.Run(() => CopyService.CopyDirectory(Constants.VanillaCopyPath, Constants.ModdedCopyPath));
                gamePath = Constants.ModdedCopyPath;
                File.WriteAllText(Constants.ModdedCopySteamAppID, "1147860"); // Create steam_appid.txt
            }

            await GenerateModsFolderAsync(gamePath);

            return await RunGMLoaderAsync(gamePath);
        }

        private static async Task AddModNamesAsync()
        {
            if (!Path.Exists(Constants.ModdingSettingsPath))
                return;

            await File.WriteAllTextAsync(Constants.ModdingSettingsNameListPath, "global.mod_list = ds_list_create();\n");

            foreach (string modPath in SettingsService.Settings.EnabledMods) {
                string modName = Path.GetFileName(modPath);
                if (modName == "UFO 50 Modding Settings")
                    continue;
                await File.AppendAllTextAsync(Constants.ModdingSettingsNameListPath, $"ds_list_add(global.mod_list, \"{modName}\");\n");
            }
        }

        private static async Task GenerateModsFolderAsync(string gamePath)
        {
            string GameExtPath = Path.Combine(gamePath, "ext");

            await Task.Run(() => File.Copy(Constants.VanillaDataWinPath, Constants.GMLoaderDataWinPath, overwrite: true));

            if (Directory.Exists(Constants.GMLoaderModsPath))
                Directory.Delete(Constants.GMLoaderModsPath, recursive: true);
            await Task.Run(() => CopyService.CopyDirectory(Constants.GMLoaderModsBasePath, Constants.GMLoaderModsPath));

            var enabledModsPaths = MainWindow.FilteredMods
                .Where(m => m.IsEnabled)
                .Select(m => Path.Combine(Constants.MyModsPath, m.Name))
                .ToList();
            var enabledModsPaths2 = enabledModsPaths
                .OrderBy(m => Path.GetFileName(m) != "UFO 50 Modding Settings")
                .ThenBy(m => m);

            foreach (string mod in enabledModsPaths2) {
                foreach (string subFolder in Directory.GetDirectories(mod)) {
                    string folderName = Path.GetFileName(subFolder);
                    string destSubFolder = Path.Combine(Constants.GMLoaderModsPath, folderName);

                    if (folderName == "ext") {
                        destSubFolder = Path.Combine(gamePath, "ext");
                    }
                    else if (folderName == "audio") {
                        foreach (string audiogroup in Directory.GetFiles(subFolder)) {
                            string destFile = Path.Combine(gamePath, Path.GetFileName(audiogroup));
                            await Task.Run(() => File.Copy(audiogroup, destFile, true));
                        }
                        continue;
                    }
                    else if (folderName == "dll") {
                        foreach (string dll in Directory.GetFiles(subFolder)) {
                            string destFile = Path.Combine(gamePath, Path.GetFileName(dll));
                            await Task.Run(() => File.Copy(dll, destFile, true));
                        }
                        continue;
                    }

                    await Task.Run(() => CopyService.CopyDirectory(subFolder, destSubFolder));
                }
            }
        }

        private static async Task<bool> RunGMLoaderAsync(string gamePath)
        {
            try {
                return await Task.Run(() => {
                    GMLoaderProgram.Initialize((level, message) => {
                        switch (level) {
                            case GMLogLevel.Info:
                                Logger.Log($"{message}");
                                break;
                            case GMLogLevel.Warning:
                                Logger.Log($"[WARNING] {message}");
                                break;
                            case GMLogLevel.Error:
                                Logger.Log($"[ERROR] {message}");
                                break;
                        }
                    });

                    GMLoaderResult result = GMLoaderProgram.Run(Constants.GMLoaderIniPath);

                    if (result.Success) {
                        // Copy and delete modded data.win
                        var GameDataWinPath = Path.Combine(gamePath, "data.win");
                        File.Copy(Constants.GMLoaderDataWinPath, GameDataWinPath, overwrite: true);
                        File.Delete(Constants.GMLoaderDataWinPath);

                        if (SettingsService.Settings.OverwriteMode)
                            Logger.Log("Mods installed successfully!");
                        else
                            Logger.Log("Mods loaded successfully!");
                        return true;
                    }
                    else {
                        Logger.Log($"Mod installation failed.");
                        return false;
                    }
                });
            }
            catch (Exception ex) {
                Logger.Log($"[ERROR] {ex.Message}");
                return false;
            }
        }
    }
}