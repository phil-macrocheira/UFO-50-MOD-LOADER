using UFO_50_MOD_INSTALLER;

namespace UFO_50_MOD_INSTALLER
{
    internal class ConflictChecker
    {
        public (bool,string) CheckConflicts(string myModsPath) {
            var conflicts = new List<string>();
            var mods = Directory.GetDirectories(myModsPath);
            var modFiles = new Dictionary<string, Dictionary<string, string>>();

            foreach (var mod in mods) {
                var files = Directory.GetFiles(mod, "*", SearchOption.AllDirectories);
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
                                conflicts.Add($"Mods {mod1} and {mod2} are incompatible due to a file conflict with {fileName}");
                            }
                        }
                    }
                }
            }

            if (conflicts.Count == 0)
                return (false, "");
            else {
                string conflictText = string.Join(Environment.NewLine, conflicts);
                return (true, conflictText);
            }
        }
    }
}