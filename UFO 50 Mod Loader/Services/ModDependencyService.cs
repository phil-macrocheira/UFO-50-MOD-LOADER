using System.Text.Json;

namespace UFO_50_Mod_Loader.Services;

public class ModDependencies
{
    public string Name { get; set; } = string.Empty;
    public List<string> DependenciesList { get; set; } = new();
}

public class DependencyResult
{
    public bool HasMissingDependencies { get; set; }
    public List<string> DependenciesText { get; } = new();
    public bool IsEmpty => DependenciesText.Count == 0;

    public string GetMessage()
    {
        return HasMissingDependencies
            ? string.Join(Environment.NewLine, DependenciesText)
            : string.Empty;
    }
}

public static class ModDependencyService
{
    public static DependencyResult CheckDependencies(List<string> enabledModPaths)
    {
        var result = new DependencyResult();
        Dictionary<string, ModDependencies> modDependencies = new();

        foreach (var modPath in enabledModPaths) {
            string modName = Path.GetFileName(modPath);
            string modID = modName;
            List<string> dependenciesList = new();

            string depFile = Path.Combine(modPath, "dependencies.txt");
            if (File.Exists(depFile)) {
                dependenciesList = ProcessDependencyList(depFile);
            }

            string gbJson = Path.Combine(modPath, "gamebanana.json");
            string modIdJson = Path.Combine(modPath, "mod_id.json");

            if (File.Exists(gbJson)) {
                modID = GetModID(gbJson, modName);
            }
            else if (File.Exists(modIdJson)) {
                modID = GetModID(modIdJson, modName);
            }

            modDependencies[modID] = new ModDependencies {
                Name = modName,
                DependenciesList = dependenciesList
            };
        }

        CheckDependencies(modDependencies, result);

        return result;
    }

    private static List<string> ProcessDependencyList(string file)
    {
        List<string> dependencies = new();
        var lines = File.ReadAllLines(file);

        foreach (var line in lines) {
            if (!string.IsNullOrWhiteSpace(line)) {
                dependencies.Add(line.Trim());
            }
        }
        return dependencies;
    }

    private static string GetModID(string json, string modName)
    {
        try {
            string jsonText = File.ReadAllText(json);
            using (JsonDocument doc = JsonDocument.Parse(jsonText)) {
                var parsedId = doc.RootElement.GetProperty("ID").GetString();
                return string.IsNullOrWhiteSpace(parsedId) ? modName : parsedId;
            }
        }
        catch {
            return modName;
        }
    }

    private static void CheckDependencies(Dictionary<string, ModDependencies> modDependencies, DependencyResult result)
    {
        foreach (var (modID, modData) in modDependencies) {
            foreach (string dependencyID in modData.DependenciesList) {
                if (!modDependencies.ContainsKey(dependencyID)) {
                    result.DependenciesText.Add($"{modData.Name} is missing a mod dependency: https://gamebanana.com/mods/{dependencyID}");
                    result.HasMissingDependencies = true;
                }
            }
        }
    }
}