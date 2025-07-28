using System.Text.Json;

namespace UFO_50_MOD_INSTALLER
{
    public static class SettingsService
    {
        private static string settingsPath = Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "settings.json");
        public static AppSettings Settings { get; private set; } = new AppSettings();

        public static void Load()
        {
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                Settings = new AppSettings();
            }

            // --- Migrate old settings files into the new system ---
            MigrateOldSettings();
        }

        public static void Save()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }

        private static void MigrateOldSettings()
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            bool migrated = false;

            // Migrate Custom_Game_Path.txt
            string customPathFile = Path.Combine(currentPath, "Custom_Game_Path.txt");
            if (File.Exists(customPathFile))
            {
                Settings.GamePath = File.ReadAllText(customPathFile);
                File.Delete(customPathFile);
                migrated = true;
            }

            // Migrate Downloaded_Mods.txt
            string downloadedModsFile = Path.Combine(currentPath, "Downloaded_Mods.txt");
            if (File.Exists(downloadedModsFile))
            {
                var modIds = File.ReadAllLines(downloadedModsFile);
                foreach (var modId in modIds)
                {
                    if (!Settings.DownloadedMods.ContainsKey(modId))
                    {
                        // We don't know the update date, so we set it to 0.
                        // It will be updated on the next download.
                        Settings.DownloadedMods[modId] = new LocalModInfo { DateUpdated = 0 };
                    }
                }
                File.Delete(downloadedModsFile);
                migrated = true;
            }

            if (migrated)
            {
                Save();
            }
        }
    }
}