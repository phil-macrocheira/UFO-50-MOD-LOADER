using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace UFO_50_Mod_Loader.Services;
public class ConflictResult
{
    public bool HasBlockingConflicts { get; set; }
    public bool HasPatchWarnings { get; set; }
    public List<string> Conflicts { get; } = new();
    public bool IsEmpty => Conflicts.Count == 0;

    public string GetMessage()
    {
        if (!HasBlockingConflicts && !HasPatchWarnings)
            return string.Empty;

        if (HasBlockingConflicts)
            return string.Join(Environment.NewLine, Conflicts);

        return "WARNING: You may install, but the patches listed below have a chance of causing conflicts. " +
               "Installation process will tell you if there is actually a conflict." +
               Environment.NewLine + string.Join(Environment.NewLine, Conflicts);
    }
}

public static class ModConflictService
{
    public static ConflictResult CheckConflicts(List<string> enabledModPaths)
    {
        var result = new ConflictResult();
        var yamlData = new Dictionary<string, List<(string find, string targetFile)>>();
        var modFiles = new Dictionary<string, Dictionary<string, string>>();
        var modsWithFileList = new HashSet<string>();

        // Gather file data from all mods
        foreach (var modPath in enabledModPaths) {
            string modName = Path.GetFileName(modPath);
            var relativePaths = new Dictionary<string, string>();

            var files = Directory.GetFiles(modPath, "*", SearchOption.AllDirectories)
                .Where(f => Path.GetRelativePath(modPath, f).Contains(Path.DirectorySeparatorChar))
                .ToArray();

            foreach (var file in files) {
                var parentDir = Path.GetFileName(Path.GetDirectoryName(file));

                if (file.EndsWith("conflicting_mods.txt", StringComparison.OrdinalIgnoreCase)) {
                    CheckModConflicts(file, modName, enabledModPaths.Select(path => Path.GetFileName(path)).ToHashSet(), result);
                } 
                else if (file.EndsWith("files.txt", StringComparison.OrdinalIgnoreCase)) {
                    ProcessFileList(file, modPath, relativePaths);
                    modsWithFileList.Add(modName);
                }
                else if (IsCodePatchYaml(file, parentDir)) {
                    ParseYamlData(file, modName, yamlData);
                }
                else {
                    var relativePath = Path.GetRelativePath(modPath, file);
                    relativePaths[relativePath] = file;
                }
            }

            modFiles[modName] = relativePaths;
        }

        // Check for file conflicts between mods
        CheckFileConflicts(enabledModPaths, modFiles, modsWithFileList, result);

        // Check for YAML conflicts
        CheckYamlConflicts(yamlData, result);

        return result;
    }

    private static void ProcessFileList(string fileListPath, string modPath, Dictionary<string, string> relativePaths)
    {
        var lines = File.ReadAllLines(fileListPath);
        foreach (var line in lines) {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fullPath = Path.Combine(Path.GetDirectoryName(fileListPath)!, line);
            var relativePath = Path.GetRelativePath(modPath, fullPath);
            relativePaths[relativePath] = fullPath;
        }
    }

    private static bool IsCodePatchYaml(string file, string? parentDir)
    {
        return (file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)) &&
               parentDir?.Equals("code_patch", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void ParseYamlData(string file, string modName, Dictionary<string, List<(string, string)>> yamlData)
    {
        try {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = File.ReadAllText(file);
            var data = deserializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(yaml);

            if (!yamlData.ContainsKey(modName))
                yamlData[modName] = new List<(string, string)>();

            foreach (var entry in data) {
                var targetFile = entry.Key;
                foreach (var item in entry.Value) {
                    // Skip append/prepend operations as they don't conflict
                    if (item.TryGetValue("type", out var type) &&
                        (type.StartsWith("findappend") || type.StartsWith("findprepend")))
                        continue;

                    if (item.TryGetValue("find", out var findStr))
                        yamlData[modName].Add((findStr, targetFile));
                }
            }
        }
        catch (YamlDotNet.Core.YamlException ex) {
            string filename = Path.GetFileName(file);
            Logger.Log($"WARNING: Invalid YAML in {modName}/{filename} - {ex.Message}");
        }
    }

    private static void CheckModConflicts(string modListPath, string mod1, HashSet<string> enabledMods, ConflictResult result)
    {
        var lines = File.ReadAllLines(modListPath);
        foreach (var mod2 in lines) {
            if (string.IsNullOrWhiteSpace(mod2)) continue;
            if (enabledMods.Contains(mod2)) {
                result.Conflicts.Add($"{mod1} is known to be incompatible with {mod2}");
                result.HasBlockingConflicts = true;
            }
        }
    }

    private static void CheckFileConflicts(List<string> modPaths, Dictionary<string, Dictionary<string, string>> modFiles, HashSet<string> modsWithFileList, ConflictResult result)
    {
        for (int i = 0; i < modPaths.Count; i++) {
            for (int j = i + 1; j < modPaths.Count; j++) {
                string mod1 = Path.GetFileName(modPaths[i]);
                string mod2 = Path.GetFileName(modPaths[j]);

                foreach (var relativePath in modFiles[mod1].Keys) {
                    if (!modFiles[mod2].ContainsKey(relativePath))
                        continue;

                    string? parentFolder = Path.GetFileName(Path.GetDirectoryName(relativePath));
                    string fileName = Path.GetFileName(relativePath);

                    if (modsWithFileList.Contains(mod1)) {
                        result.Conflicts.Add($"{mod1} will patch a file modded by {mod2}: {fileName}");
                        result.HasPatchWarnings = true;
                    }
                    else if (modsWithFileList.Contains(mod2)) {
                        result.Conflicts.Add($"{mod2} will patch a file modded by {mod1}: {fileName}");
                        result.HasPatchWarnings = true;
                    }
                    else if (parentFolder?.Equals("textures_properties", StringComparison.OrdinalIgnoreCase) == true) {
                        RenameConflictingFile(modFiles[mod1][relativePath], mod1, "SpriteConfig");
                        RenameConflictingFile(modFiles[mod2][relativePath], mod2, "SpriteConfig");
                    }
                    else if (parentFolder?.Equals("code_patch", StringComparison.OrdinalIgnoreCase) == true) {
                        RenameConflictingFile(modFiles[mod1][relativePath], mod1, "code_patch");
                        RenameConflictingFile(modFiles[mod2][relativePath], mod2, "code_patch");
                    }
                    else {
                        result.Conflicts.Add($"{mod1} and {mod2} are incompatible due to a file conflict with {fileName}");
                        result.HasBlockingConflicts = true;
                    }
                }
            }
        }
    }

    private static void RenameConflictingFile(string filePath, string modName, string prefix)
    {
        string dir = Path.GetDirectoryName(filePath)!;
        string ext = Path.GetExtension(filePath);
        string newName = Path.Combine(dir, $"{prefix}_{modName}{ext}");

        if (File.Exists(filePath) && !File.Exists(newName))
            File.Move(filePath, newName);
    }

    private static void CheckYamlConflicts(Dictionary<string, List<(string find, string targetFile)>> yamlData, ConflictResult result)
    {
        var valueMap = new Dictionary<(string find, string targetFile), HashSet<string>>();

        foreach (var kvp in yamlData) {
            var modName = kvp.Key;
            var uniquePairs = new HashSet<(string, string)>(kvp.Value);

            foreach (var pair in uniquePairs) {
                if (!valueMap.TryGetValue(pair, out var mods))
                    valueMap[pair] = mods = new HashSet<string>();
                mods.Add(modName);
            }
        }

        foreach (var kvp in valueMap) {
            if (kvp.Value.Count > 1) {
                result.HasBlockingConflicts = true;
                string mods = string.Join(" and ", kvp.Value);
                string targetFile = kvp.Key.targetFile;
                result.Conflicts.Add($"{mods} are incompatible due to a code find-and-replace conflict with {targetFile}");
            }
        }
    }
}