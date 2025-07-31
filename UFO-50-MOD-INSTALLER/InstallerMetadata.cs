using System.Text.Json;
using System.Text.Json.Serialization;

namespace UFO_50_MOD_INSTALLER
{
    public class InstallerMetadata
    {
        // Data from GameBanana API
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long GameBananaId { get; set; }
        public long LatestVersionDate { get; set; }
        public long LastChecked { get; set; }

        // Cached local data, originally from info.txt
        public string? Title { get; set; }
        public string? Creator { get; set; }
        public string? Description { get; set; }
        public string? CachedIconPath { get; set; }
        public string? Version { get; set; }

        private static readonly JsonSerializerOptions _options = new() { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve };

        public static InstallerMetadata? Load(string modDirectory) {
            var jsonPath = Directory.GetFiles(modDirectory, "*.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (jsonPath == null || !File.Exists(jsonPath)) {
                return null;
            }
            try {
                var json = File.ReadAllText(jsonPath);
                return JsonSerializer.Deserialize<InstallerMetadata>(json);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error loading JSON for {Path.GetFileName(modDirectory)}: {ex.Message}");
                return null;
            }
        }

        public void Save(string modDirectory) {
            // Always save to a consistent filename to avoid creating multiple .json files.
            string path = Path.Combine(modDirectory, "installer_managed.json");
            try {
                var json = JsonSerializer.Serialize(this, _options);
                //File.WriteAllText(path, json); // TEMPORARILY DISABLE FOR v1.2.0
            }
            catch (Exception ex) {
                Console.WriteLine($"Error saving JSON for {Path.GetFileName(modDirectory)}: {ex.Message}");
            }
        }
    }
}