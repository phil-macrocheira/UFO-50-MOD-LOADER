using System.Text.Json;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader;

public class InstalledModInfo
{
    public string ID { get; set; } = "";
    public string Version { get; set; } = "";
    public string ModFolderPath { get; set; } = "";
}

public static class DownloadedModTrackerService
{
    private static Dictionary<string, InstalledModInfo> _installedMods = new();
    public static async Task ScanInstalledModsAsync()
    {
        _installedMods.Clear();

        foreach (var modDir in Directory.GetDirectories(Constants.MyModsPath)) {
            var jsonPath = Path.Combine(modDir, GameBananaMetadata.FileName);

            if (!File.Exists(jsonPath))
                continue;

            try {
                var json = await File.ReadAllTextAsync(jsonPath);
                var metadata = JsonSerializer.Deserialize<GameBananaMetadata>(json);

                if (metadata != null && !string.IsNullOrEmpty(metadata.ID)) {
                    _installedMods[metadata.ID] = new InstalledModInfo {
                        ID = metadata.ID,
                        Version = metadata.Version,
                        ModFolderPath = modDir
                    };
                }
            }
            catch {
                // Skip invalid JSON files
            }
        }
    }
    public static InstalledModInfo? GetInstalledMod(string gameBananaID)
    {
        return _installedMods.TryGetValue(gameBananaID, out var info) ? info : null;
    }
    public static bool IsInstalled(string gameBananaID)
    {
        return _installedMods.ContainsKey(gameBananaID);
    }
    public static string GetInstalledVersion(string gameBananaID)
    {
        return _installedMods.TryGetValue(gameBananaID, out var info) ? info.Version : "";
    }
}