using Standart.Hash.xxHash;
using System.Diagnostics;

namespace UFO_50_MOD_INSTALLER
{
    internal class ModInstaller
    {
        public ulong supportedDataHash = 876354219589866315; // 1.7.6
        public List<string> enabledMods;
        public string? rootPath = "";
        public string? gamePath = "";
        public string? GMLoaderPath = "";
        public string? myModsPath = "";
        public string? modsPath = "";
        public string? mods_basePath = "";
        public string? ufo50_data_winPath = "";
        public string? modded_data_winPath = "";
        public void installMods(string currentPath, string gamePath_arg, List<string> enabledMods_arg, bool conflictsExist) {
            enabledMods = enabledMods_arg;
            rootPath = currentPath;
            gamePath = gamePath_arg;
            GMLoaderPath = Path.Combine(rootPath, "GMloader.exe");
            myModsPath = Path.Combine(rootPath, "my mods");
            modsPath = Path.Combine(rootPath, "mods");
            mods_basePath = Path.Combine(rootPath, "mods_base");
            ufo50_data_winPath = Path.Combine(gamePath, "data.win");
            modded_data_winPath = Path.Combine(rootPath, "data.win");
            string vanillaPath = Path.Combine(rootPath, "vanilla.win");
#if DEBUG
            GMLoaderPath = Path.Combine(rootPath, "GMLoader", "GMloader.exe");
#endif

            if (!checkVanillaWin(vanillaPath)) {
                MessageBox.Show("ERROR: No vanilla.win file in this folder", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (conflictsExist) {
                MessageBox.Show("Please disable mods to resolve conflicts before installation", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            addModNames();
            generateModsFolder(vanillaPath);

            if (runGMLoader()) {
                File.Copy(modded_data_winPath, ufo50_data_winPath, overwrite: true);
                MessageBox.Show("Mods Installed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else {
                MessageBox.Show("ERROR: GMLoader did not run successfully. Ask for help!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }
        private bool runGMLoader() {
            if (!File.Exists(GMLoaderPath)) {
                MessageBox.Show("ERROR: GMLoader is missing somehow!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            try {
                var process = Process.Start(GMLoaderPath);
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch {
                return false;
            }
        }
        private void generateModsFolder(string vanillaPath) {
            string localizationPath = Path.Combine(gamePath, "ext");
            string vanilla_localizationPath = Path.Combine(rootPath, "localization", "vanilla", "ext");
            string modded_localizationPath = Path.Combine(rootPath, "modded", "vanilla", "ext");

            File.Copy(vanillaPath, modded_data_winPath, overwrite: true);
            if (Directory.Exists(modsPath))
                Directory.Delete(modsPath, recursive: true);
            CopyDirectory(mods_basePath, modsPath);

            var modPaths = enabledMods.OrderBy(m => Path.GetFileName(m) != "UFO 50 Modding Framework").ThenBy(m => m); // Ensures we copy the framework first
            foreach (string mod in modPaths) {
                foreach (string subFolder in Directory.GetDirectories(mod)) {
                    string folderName = Path.GetFileName(subFolder);
                    string destSubFolder = Path.Combine(modsPath, Path.GetFileName(subFolder));
                    if (folderName == "ext")
                        destSubFolder = localizationPath;
                    /*else if (folderName == "audio") {
                        foreach (string audiogroup in Directory.GetFiles(subFolder)) {
                            string destFile = Path.Combine(gamePath, Path.GetFileName(audiogroup));
                            File.Copy(audiogroup, destFile, true);
                        }
                        continue;
                    }*/
                    else if (folderName == "dll") {
                        foreach (string dll in Directory.GetFiles(subFolder)) {
                            string destFile = Path.Combine(gamePath, Path.GetFileName(dll));
                            File.Copy(dll, destFile, true);
                        }
                        continue;
                    }

                    CopyDirectory(subFolder, destSubFolder);
                }
            }
        }
        public void CopyDirectory(string sourceDir, string destDir, List<string> skipList = null) {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir)) {
                if (skipList != null && skipList.Contains(Path.GetFileName(file)))
                    continue;
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (string subDir in Directory.GetDirectories(sourceDir)) {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir, skipList);
            }
        }
        private void addModNames() {
            if (!Path.Exists(Path.Combine(myModsPath, "UFO 50 Modding Framework")))
                return;

            string modNameListPath = Path.Combine(myModsPath, "UFO 50 Modding Framework", "code", "gml_Object_oModding_Other_10.gml");
            File.WriteAllText(modNameListPath, "global.mod_list = ds_list_create();\n");

            foreach (string modPath in enabledMods) {
                string modName = Path.GetFileName(modPath);
                if (modName == "UFO 50 Modding Framework")
                    continue;
                using (var stream = new FileStream(modNameListPath, FileMode.Append, FileAccess.Write))
                using (var writer = new StreamWriter(stream)) {
                    writer.WriteLine($"ds_list_add(global.mod_list, \"{modName}\");");
                }
            }
            return;
        }
        public bool checkVanillaWin(string vanillaPath) {
            return File.Exists(vanillaPath);
        }
        public bool checkVanillaHash(string vanillaPath, string iniPath) {
            if (!File.Exists(iniPath)) {
                MessageBox.Show("ERROR: GMLoader.ini is missing somehow!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return false;
            }

            foreach (string line in File.ReadLines(iniPath)) {
                if (line.Trim().StartsWith("SupportedDataHash=", StringComparison.OrdinalIgnoreCase)) {
                    if (ulong.TryParse(line.Split('=')[1].Trim(), out ulong value))
                        supportedDataHash = value;
                    break;
                }
            }

            ulong dataHash = ComputeFileHash3(vanillaPath);
            if (supportedDataHash != dataHash)
                return false;
            return true;
        }
        private static ulong ComputeFileHash3(string filePath) {
            using (var stream = File.OpenRead(filePath)) {
                byte[] fileBytes = new byte[stream.Length];
                stream.Read(fileBytes, 0, (int)stream.Length);
                return xxHash3.ComputeHash(fileBytes, (int)stream.Length, 0);
            }
        }
    }
}



