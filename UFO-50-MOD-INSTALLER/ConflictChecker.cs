namespace UFO_50_MOD_INSTALLER
{
    internal class ConflictChecker
    {
        public (bool, string, string, bool, bool) CheckConflicts(string myModsPath, List<string> enabledMods) {
            var conflicts = new List<string>();
            var otherTextList = new List<string>();
            var mods = enabledMods.ToArray();
            var modFiles = new Dictionary<string, Dictionary<string, string>>();
            bool anySettingsConflict = false;
            bool anyIconsConflict = false;

            foreach (var mod in mods) {
                var files = Directory.GetFiles(mod, "*", SearchOption.AllDirectories)
                     .Where(f => Path.GetRelativePath(mod, f).Contains(Path.DirectorySeparatorChar))
                     .ToArray();
                var relativePaths = new Dictionary<string, string>();
                foreach (var file in files) {
                    var relativePath = Path.GetRelativePath(mod, file);
                    relativePaths[relativePath] = file;
                }
                modFiles[Path.GetFileName(mod)] = relativePaths;
            }

            for (int i = 0; i < mods.Length; i++) {
                for (int j = i + 1; j < mods.Length; j++) {
                    string mod1 = Path.GetFileName(mods[i]);
                    string mod2 = Path.GetFileName(mods[j]);

                    foreach (var relativePath in modFiles[mod1].Keys) {
                        if (modFiles[mod2].ContainsKey(relativePath)) {
                            string parentFolder = Path.GetFileName(Path.GetDirectoryName(relativePath));
                            if (parentFolder.Equals("textures_properties", StringComparison.OrdinalIgnoreCase)) {
                                string newName1 = Path.Combine(Path.GetDirectoryName(modFiles[mod1][relativePath])!, $"{mod1}SpriteConfig{Path.GetExtension(relativePath)}");
                                string newName2 = Path.Combine(Path.GetDirectoryName(modFiles[mod2][relativePath])!, $"{mod2}SpriteConfig{Path.GetExtension(relativePath)}");
                                File.Move(modFiles[mod1][relativePath], newName1);
                                File.Move(modFiles[mod2][relativePath], newName2);
                            }
                            else {
                                string fileName = Path.GetFileName(relativePath);

                                if (fileName.Contains("sIconCart", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                bool isFramework = false;
                                string FrameworkName = "UFO 50 Modding Framework";
                                string nonFrameworkMod = "";
                                if (mod1 == FrameworkName) {
                                    nonFrameworkMod = mod2;
                                    isFramework = true;
                                }
                                else if (mod2 == FrameworkName) {
                                    nonFrameworkMod = mod1;
                                    isFramework = true;
                                }

                                // there's definitely a better way to code this but whatever
                                bool settingsConflict = false;
                                bool iconsConflict = false;
                                switch (fileName) {
                                    case "gml_GlobalScript_scrAch.gml":
                                        settingsConflict = true;
                                        break;
                                    case "gml_Object_oLibrary_Create_0.gml":
                                        settingsConflict = true;
                                        break;
                                    case "gml_Object_oPauseMenu_Create_0.gml":
                                        settingsConflict = true;
                                        break;
                                    case "gml_Object_oPauseMenu_Draw_0.gml":
                                        settingsConflict = true;
                                        break;
                                    case "gml_Object_oPauseMenu_Other_22.gml":
                                        settingsConflict = true;
                                        break;
                                    case "gml_Object_oIcon_Draw_0.gml":
                                        iconsConflict = true;
                                        break;
                                    case "gml_Object_oLibrary_Draw_0.gml":
                                        iconsConflict = true;
                                        break;
                                }

                                if (isFramework) {
                                    string disabledFeature = "";
                                    if (settingsConflict) {
                                        anySettingsConflict = true;
                                        disabledFeature = "modding settings menu";
                                    }
                                    else if (iconsConflict) {
                                        anyIconsConflict = true;
                                        disabledFeature = "custom cartridge icons";
                                    }
                                    string text = $"The {disabledFeature} feature has been disabled to allow installation of {nonFrameworkMod}";
                                    if (!otherTextList.Contains(text))
                                        otherTextList.Add(text);
                                }
                                else
                                    conflicts.Add($"{mod1} and {mod2} are incompatible due to a file conflict with {fileName}");
                            }
                        }
                    }
                }
            }

            string otherText = string.Join(Environment.NewLine, otherTextList);

            if (conflicts.Count == 0)
                return (false, "", otherText, anySettingsConflict, anyIconsConflict);
            else {
                string conflictText = string.Join(Environment.NewLine, conflicts);
                return (true, conflictText, otherText, anySettingsConflict, anyIconsConflict);
            }
        }
    }
}