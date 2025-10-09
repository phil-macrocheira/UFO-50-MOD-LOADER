using System.Text.Json;

namespace UFO_50_Mod_Loader
{
    public static class SettingsService
    {
        private static string settingsPath = Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "settings.json");
        public static AppSettings Settings { get; private set; } = new AppSettings();

        public static void Load() {
            if (File.Exists(settingsPath)) {
                var json = File.ReadAllText(settingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else {
                Settings = new AppSettings();
            }
        }

        public static void Save() {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
    }
}