using System.Text.Json;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services;

public static class SettingsService
{
    private static readonly string _settingsPath = Constants.SettingsPath;

    public static Settings Settings { get; private set; } = new();

    public static void Load()
    {
        if (File.Exists(_settingsPath)) {
            try {
                var json = File.ReadAllText(_settingsPath);
                Settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
            catch (Exception ex) {
                Logger.Log($"Failed to load settings: {ex.Message}");
                Settings = new Settings();
            }
        }
        else {
            Settings = new Settings();
            Save();
        }
    }

    public static void Save()
    {
        Settings.Version = Constants.Version;

        try {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex) {
            Logger.Log($"Failed to save settings: {ex.Message}");
        }
    }
}