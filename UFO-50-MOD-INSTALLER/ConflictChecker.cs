using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace UFO_50_MOD_INSTALLER
{
    internal class ConflictChecker
    {
        public Dictionary<string, List<(string find, string targetFile)>> YamlDict = new();
        public List<string> conflicts = new List<string>();
        public bool hasNormalConflicts = false;
        public bool hasPatchConflicts = false;
        public void GetYamlData(string file, string mod) {
            try {
                var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                var yaml = File.ReadAllText(file);
                var yaml_data = deserializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(yaml);

                if (!YamlDict.ContainsKey(mod))
                    YamlDict[mod] = new List<(string, string)>();

                foreach (var entry in yaml_data) {
                    var targetFile = entry.Key;
                    foreach (var item in entry.Value) {
                        if (item.TryGetValue("find", out var findStr)) {
                            YamlDict[mod].Add((findStr, targetFile));
                        }
                    }
                }
            }
            catch (YamlDotNet.Core.YamlException ex) {
                string filename = Path.GetFileName(file);
                MessageBox.Show($"MOD: {mod}\nFILE: {filename}\n\n{ex.Message}","Warning: Invalid code_patch YAML",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
        public void FindYamlConflicts() {
            var valueMap = new Dictionary<(string findStr, string targetFile), HashSet<string>>();

            foreach (var kvp in YamlDict) {
                var mod = kvp.Key;
                var uniquePairs = new HashSet<(string, string)>(kvp.Value);
                foreach (var pair in uniquePairs) {
                    if (!valueMap.TryGetValue(pair, out var mods))
                        valueMap[pair] = mods = new HashSet<string>();
                    mods.Add(mod);
                }
            }

            foreach (var kvp in valueMap) {
                if (kvp.Value.Count > 1) {
                    hasNormalConflicts = true;
                    string mods = string.Join(" and ", kvp.Value);
                    string targetFile = kvp.Key.targetFile;
                    string findStr = kvp.Key.findStr;
                    conflicts.Add($"{mods} are incompatible due to a code find-and-replace conflict with {targetFile}");
                }
            }
        }
        public (bool, string) CheckConflicts(string myModsPath, List<string> enabledMods) {
            conflicts.Clear();
            YamlDict.Clear();
            hasNormalConflicts = false;
            hasPatchConflicts = false;
            var mods = enabledMods.ToArray();
            var modFiles = new Dictionary<string, Dictionary<string, string>>();
            var modsWithFileList = new List<string>();

            foreach (var mod in mods) {
                string modFilename = Path.GetFileName(mod);
                var files = Directory.GetFiles(mod, "*", SearchOption.AllDirectories)
                     .Where(f => Path.GetRelativePath(mod, f).Contains(Path.DirectorySeparatorChar))
                     .ToArray();
                var relativePaths = new Dictionary<string, string>();
                foreach (var file in files) {
                    var parentDir = Path.GetFileName(Path.GetDirectoryName(file));
                    if (file.EndsWith("files.txt", StringComparison.OrdinalIgnoreCase)) {
                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines) {
                            var fullPath = Path.Combine(Path.GetDirectoryName(file), line);
                            var relativePath = Path.GetRelativePath(mod, fullPath);
                            relativePaths[relativePath] = fullPath;
                        }
                        if (!modsWithFileList.Contains(modFilename))
                            modsWithFileList.Add(modFilename);
                    }
                    else if ((file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                        && parentDir.Equals("code_patch", StringComparison.OrdinalIgnoreCase)) {
                        GetYamlData(file, modFilename);
                    }
                    else {
                        var relativePath = Path.GetRelativePath(mod, file);
                        relativePaths[relativePath] = file;
                    }
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
                            string fileName = Path.GetFileName(relativePath);

                            if (modsWithFileList.Contains(mod1)) {
                                conflicts.Add($"{mod1} will patch a file modded by {mod2}: {fileName}");
                                hasPatchConflicts = true;
                            }
                            else if (modsWithFileList.Contains(mod2)) {
                                conflicts.Add($"{mod2} will patch a file modded by {mod1}: {fileName}");
                                hasPatchConflicts = true;
                            }
                            else if (parentFolder.Equals("textures_properties", StringComparison.OrdinalIgnoreCase)) {
                                string newName1 = Path.Combine(Path.GetDirectoryName(modFiles[mod1][relativePath])!, $"{mod1}SpriteConfig{Path.GetExtension(relativePath)}");
                                string newName2 = Path.Combine(Path.GetDirectoryName(modFiles[mod2][relativePath])!, $"{mod2}SpriteConfig{Path.GetExtension(relativePath)}");
                                File.Move(modFiles[mod1][relativePath], newName1);
                                File.Move(modFiles[mod2][relativePath], newName2);
                            }
                            else if (parentFolder.Equals("code_patch", StringComparison.OrdinalIgnoreCase)) {
                                string newName1 = Path.Combine(Path.GetDirectoryName(modFiles[mod1][relativePath])!, $"code_patch_{mod1}{Path.GetExtension(relativePath)}");
                                string newName2 = Path.Combine(Path.GetDirectoryName(modFiles[mod2][relativePath])!, $"code_patch_{mod2}{Path.GetExtension(relativePath)}");
                                File.Move(modFiles[mod1][relativePath], newName1);
                                File.Move(modFiles[mod2][relativePath], newName2);
                            }
                            else {
                                if (fileName.Contains("sIconCart", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                conflicts.Add($"{mod1} and {mod2} are incompatible due to a file conflict with {fileName}");
                                hasNormalConflicts = true;
                            }
                        }
                    }
                }
            }

            FindYamlConflicts();

            if (!hasNormalConflicts) {
                if (!hasPatchConflicts)
                    return (false, "");
                else {
                    string conflictText = "Warning: You may install, but know that the patches listed below have a chance of causing conflicts. Installation process will tell you if there is actually a conflict."
                        + Environment.NewLine + string.Join(Environment.NewLine, conflicts);
                    return (false, conflictText);
                }
            }
            else {
                string conflictText = string.Join(Environment.NewLine, conflicts);
                return (true, conflictText);
            }
        }
    }
}