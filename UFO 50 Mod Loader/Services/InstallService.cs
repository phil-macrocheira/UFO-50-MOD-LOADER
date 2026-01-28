using GMLoader;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services
{
    public class InstallService
    {
        public static bool InstallMods()
        {
            string gamePath = SettingsService.Settings.GamePath;

            if (!SettingsService.Settings.CopiedGameFiles) {
                Logger.Log($"Cannot Install Mods: Vanilla game files have not been copied yet");
                return false;
            }

            AddModNames();

            if (!SettingsService.Settings.OverwriteMode) {
                if (Directory.Exists(Constants.ModdedCopyPath))
                    Directory.Delete(Constants.ModdedCopyPath, recursive: true);
                CopyService.CopyDirectory(Constants.VanillaCopyPath, Constants.ModdedCopyPath);
                gamePath = Constants.ModdedCopyPath;
            }

            GenerateModsFolder(gamePath);

            return RunGMLoader(gamePath);
        }
        private static void AddModNames()
        {
            if (!Path.Exists(Constants.ModdingSettingsPath))
                return;

            File.WriteAllText(Constants.ModdingSettingsNameListPath, "global.mod_list = ds_list_create();\n");

            foreach (string modPath in SettingsService.Settings.EnabledMods) {
                string modName = Path.GetFileName(modPath);
                if (modName == "UFO 50 Modding Settings")
                    continue;
                using (var stream = new FileStream(Constants.ModdingSettingsNameListPath, FileMode.Append, FileAccess.Write))
                using (var writer = new StreamWriter(stream)) {
                    writer.WriteLine($"ds_list_add(global.mod_list, \"{modName}\");");
                }
            }
            return;
        }
        private static void GenerateModsFolder(string gamePath)
        {
            string GameExtPath = Path.Combine(gamePath, "ext");

            File.Copy(Constants.VanillaDataWinPath, Constants.GMLoaderDataWinPath, overwrite: true);

            if (Directory.Exists(Constants.GMLoaderModsPath))
                Directory.Delete(Constants.GMLoaderModsPath, recursive: true);
            CopyService.CopyDirectory(Constants.GMLoaderModsBasePath, Constants.GMLoaderModsPath);

            var enabledModsPaths = MainWindow.FilteredMods.Where(m => m.IsEnabled).Select(m => Path.Combine(Constants.MyModsPath, m.Name)).ToList();
            var enabledModsPaths2 = enabledModsPaths.OrderBy(m => Path.GetFileName(m) != "UFO 50 Modding Settings").ThenBy(m => m); // Ensures we copy modding settings first
            foreach (string mod in enabledModsPaths2) {
                foreach (string subFolder in Directory.GetDirectories(mod)) {
                    string folderName = Path.GetFileName(subFolder);
                    string destSubFolder = Path.Combine(Constants.GMLoaderModsPath, folderName);

                    // Copy Localization, Audio, and DLL files straight to game path
                    if (folderName == "ext")
                        destSubFolder = Path.Combine(gamePath, "ext");
                    else if (folderName == "audio") {
                        foreach (string audiogroup in Directory.GetFiles(subFolder)) {
                            string destFile = Path.Combine(gamePath, Path.GetFileName(audiogroup));
                            File.Copy(audiogroup, destFile, true);
                        }
                        continue;
                    }
                    else if (folderName == "dll") {
                        foreach (string dll in Directory.GetFiles(subFolder)) {
                            string destFile = Path.Combine(gamePath, Path.GetFileName(dll));
                            File.Copy(dll, destFile, true);
                        }
                        continue;
                    }

                    CopyService.CopyDirectory(subFolder, destSubFolder);
                }
            }
        }
        private static bool RunGMLoader(string gamePath) {
            try {
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
                    var GameDataWinPath = Path.Combine(gamePath, "data.win");
                    File.Copy(Constants.GMLoaderDataWinPath, GameDataWinPath, overwrite: true);
                    Logger.Log("Mods installed successfully!");
                    return true;
                }
                else {
Logger.Log($"Mod installation failed: {result.ErrorMessage}");
if (result.Exception != null) Logger.Log($"Stack trace: {result.Exception.StackTrace}");
                    Logger.Log($"Mod installation failed.");
                    return false;
                }
            }
            catch (Exception ex) {
                Logger.Log($"[ERROR] {ex.Message}");
                return false;
            }
        }
    }
}
