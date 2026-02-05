using Avalonia.Controls;
using GMLoader;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services
{
    public class InstallService
    {
        public static async Task<GMLoaderResult?> InstallModsAsync(Window parent, List<string> enabledModPaths)
        {
            Logger._showingConflicts = false;
            string gamePath = SettingsService.Settings.GamePath;

            if (SettingsService.Settings.CopiedVanillaVersion == null) {
                Logger.Log($"Cannot Install Mods: Vanilla game files have not been copied yet");
                return null;
            }

            await ModdingSettingsAddModNamesAsync(enabledModPaths);

            if (!SettingsService.Settings.OverwriteMode) {
                if (Directory.Exists(Constants.ModdedCopyPath)) {
                    try {
                        Directory.Delete(Constants.ModdedCopyPath, recursive: true);
                    }
                    catch (Exception ex) {
                        Logger.Log($"[ERROR] Failed to delete {Path.GetFileName(Constants.ModdedCopyPath)}: {ex.Message}");
                        return null;
                    }
                }
                await Task.Run(() => CopyService.CopyDirectory(Constants.VanillaCopyPath, Constants.ModdedCopyPath));
                gamePath = Constants.ModdedCopyPath;
                File.WriteAllText(Constants.ModdedCopySteamAppID, Constants.SteamAppID);
            }

            await GenerateModsFolderAsync(gamePath, enabledModPaths);

            return await RunGMLoaderAsync(gamePath, enabledModPaths);
        }

        private static async Task ModdingSettingsAddModNamesAsync(List<string> enabledModPaths)
        {
            string? moddingSettingsPath = Path.Combine(Constants.MyModsPath, "UFO 50 Modding Settings");

            if (moddingSettingsPath == null)
                return;

            string nameListPath = Path.Combine(moddingSettingsPath, "code", "gml_Object_oModding_Other_10.gml");

            // Ensure the code directory exists
            string codeDir = Path.GetDirectoryName(nameListPath)!;
            if (!Directory.Exists(codeDir)) {
                Directory.CreateDirectory(codeDir);
            }

            await File.WriteAllTextAsync(nameListPath, "global.mod_list = ds_list_create();\n");

            foreach (string modPath in enabledModPaths) {
                string modName = Path.GetFileName(modPath);
                if (modName == "UFO 50 Modding Settings")
                    continue;
                await File.AppendAllTextAsync(nameListPath, $"ds_list_add(global.mod_list, \"{modName}\");\n");
            }
        }

        private static async Task GenerateModsFolderAsync(string gamePath, List<string> enabledModPaths)
        {
            await Task.Run(() => File.Copy(Constants.VanillaDataWinPath, Constants.GMLoaderDataWinPath, overwrite: true));

            if (Directory.Exists(Constants.GMLoaderModsPath)) {
                try {
                    Directory.Delete(Constants.GMLoaderModsPath, recursive: true);
                }
                catch (Exception ex) {
                    Logger.Log($"[ERROR] Failed to delete existing mods workspace folder: {ex.Message}");
                }
            }
            await Task.Run(() => CopyService.CopyDirectory(Constants.GMLoaderModsBasePath, Constants.GMLoaderModsPath));

            var reorderedModPaths = enabledModPaths
                .OrderBy(m => Path.GetFileName(m) != "UFO 50 Modding Settings")
                .ThenBy(m => m);

            foreach (string mod in reorderedModPaths) {
                if (!Directory.Exists(mod)) {
                    Logger.Log($"[WARNING] Mod folder not found, skipping: {mod}");
                    continue;
                }

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

        private static async Task<GMLoaderResult?> RunGMLoaderAsync(string gamePath, List<string> enabledModPaths)
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

                    GMLoaderResult result = GMLoaderProgram.Run(Constants.GMLoaderIniPath, Constants.ModLoaderWorkspacePath, enabledModPaths);

                    if (result.Success) {
                        var GameDataWinPath = Path.Combine(gamePath, "data.win");
                        File.Copy(Constants.GMLoaderDataWinPath, GameDataWinPath, overwrite: true);
                        File.Delete(Constants.GMLoaderDataWinPath);

                        if (SettingsService.Settings.OverwriteMode)
                            Logger.Log("Mods installed successfully!");
                        else
                            Logger.Log("Mods loaded successfully!");
                    }
                    else {
                        Logger.Log($"Mod installation failed.");
                    }

                    return result;
                });
            }
            catch (Exception ex) {
                Logger.Log($"[ERROR] {ex.Message}");
                return null;
            }
        }
    }
}