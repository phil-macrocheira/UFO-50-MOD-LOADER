using System.Text.Json;
using System.Text.Json.Serialization;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services;

public class ModDependencies
{
    public ModData data { get; set; } = new();
    public List<ModData> DependenciesList { get; set; } = new();
}
public class ModData
{
    public string ID { get; set; }
    public string Name { get; set; }
    public string? Version { get; set; } = "";
}

public class DependencyResult
{
    public bool HasMissingDependencies { get; set; }
    public List<string> DependenciesText { get; } = new();
    public List<string> MissingModIDs { get; } = new();
    public List<string> MissingModNames { get; } = new();
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
        List<ModDependencies> modDependencies = new();

        // Grab every mod's data and dependency list
        foreach (var modPath in enabledModPaths) {
            string modName = Path.GetFileName(modPath);
            List<ModData> dependenciesList = new();

            string depFile = Path.Combine(modPath, "dependencies.json");
            if (File.Exists(depFile)) {
                dependenciesList = ProcessDependencyList(depFile);
            }

            string gbJson = Path.Combine(modPath, "gamebanana.json");
            string modIdJson = Path.Combine(modPath, "mod_data.json");
            ModData modData = new ModData { ID = modName, Name = modName};

            if (File.Exists(gbJson)) {
                modData = GetModData(gbJson);
            }
            else if (File.Exists(modIdJson)) {
                modData = GetModData(modIdJson);
            }

            modDependencies.Add(new ModDependencies {
                data = modData,
                DependenciesList = dependenciesList
            });
        }

        CheckDependencies(modDependencies, result);

        return result;
    }

    private static List<ModData> ProcessDependencyList(string json)
    {
        try {
            var jsonText = File.ReadAllText(json);
            return JsonSerializer.Deserialize<List<ModData>>(jsonText, JsonOptions) ?? new List<ModData>();
        }
        catch (JsonException ex) {
            Logger.Log($"Failed to parse {json}: {ex.Message}");
            return new List<ModData>();
        }
    }
    private static readonly JsonSerializerOptions JsonOptions = new() {
        AllowTrailingCommas = true
    };
    private static ModData GetModData(string json)
    {
        try {
            string jsonText = File.ReadAllText(json);
            return JsonSerializer.Deserialize<ModData>(jsonText, JsonOptions) ?? new ModData();
        }
        catch (JsonException ex) {
            Logger.Log($"Failed to parse {json}: {ex.Message}");
            return new ModData();
        }
    }
    private static void CheckDependencies(List<ModDependencies> modDependencies, DependencyResult result)
    {
        foreach (var mod in modDependencies) {
            foreach (var dependency in mod.DependenciesList) {
                var ID_match = modDependencies.FirstOrDefault(m => m.data.ID == dependency.ID);
                var name_match = modDependencies.FirstOrDefault(m => m.data.Name == dependency.Name);
                string? match_version = "";
                bool versionSatisfied = true;

                if (name_match != null)
                    match_version = name_match.data.Version ?? "";
                if (ID_match != null)
                    match_version = ID_match.data.Version ?? "";

                if (!string.IsNullOrEmpty(dependency.Version))
                    versionSatisfied = CheckVersion(dependency.Version, match_version);

                string requiredVersionText = string.IsNullOrEmpty(dependency.Version) ? "" : $" {dependency.Version}";

                if (ID_match == null && name_match == null) {
                    result.MissingModIDs.Add(dependency.ID);
                    result.MissingModNames.Add(dependency.Name);
                    result.DependenciesText.Add($"DEPENDENCY MISSING OR UNCHECKED: {mod.data.Name} requires {dependency.Name}{requiredVersionText}");
                    result.HasMissingDependencies = true;
                }
                else if (!string.IsNullOrEmpty(dependency.Version) && !versionSatisfied) {
                    result.MissingModIDs.Add(dependency.ID);
                    result.DependenciesText.Add($"DEPENDENCY OUTDATED: {mod.data.Name} requires {dependency.Name}{requiredVersionText} (found {match_version.ToString()})");
                    result.HasMissingDependencies = true;
                }
            }
        }
        if (result.HasMissingDependencies)
            result.DependenciesText.Add("\nOpen the Mod Downloader and click \"Select Dependencies\" to automatically select these mods for download");
    }
    // Returns true if found version is greater than or equal to the required version
    private static bool CheckVersion(string requiredVersion, string foundVersion)
    {
        if (string.IsNullOrEmpty(foundVersion))
            return false;

        var requiredVersionSplit = requiredVersion.Split('.').Select(int.Parse).ToArray();
        var foundVersionSplit = foundVersion.Split('.').Select(int.Parse).ToArray();

        int length = Math.Max(requiredVersionSplit.Length, foundVersionSplit.Length);

        for (int i = 0; i < length; i++) {
            int req = i < requiredVersionSplit.Length ? requiredVersionSplit[i] : 0;
            int fnd = i < foundVersionSplit.Length ? foundVersionSplit[i] : 0;

            if (fnd > req) return true;
            if (fnd < req) return false;
        }

        return true;
    }
}