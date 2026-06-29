using System.Text.Json;

namespace UFO_50_Mod_Loader.Models
{
   public class Game
   {
        public static Game? Metadata { get; private set; }
        public static GamePaths? Paths { get; private set; }
        
        // Metadata JSON Properties
        public string GameName { get; init; } = "UFO 50"; // Change to name of selected game metadata folder if this becomes a generic game maker program
        public string InstallFolderName { get; init; } = string.Empty;
        public string ExeName { get; init; } = string.Empty;
        public string SteamAppID { get; init; } = string.Empty;
        public string GameBananaID { get; init; } = string.Empty;
        public string DiscordLink { get; init; } = string.Empty;
        public bool IsUFO50 { get; init; }

        public static bool Load()
        {
            string gameName = "UFO 50"; // Change to name of selected game metadata folder if this becomes a generic game maker program
            string metadataPath = Path.Combine(Constants.ModLoaderPath, "Games", gameName, "metadata.json");

            if (!File.Exists(metadataPath)) {
                throw new FileNotFoundException($"CRITICAL ERROR: UFO 50 metadata file not found at {metadataPath}");
            }

            string jsonString = File.ReadAllText(metadataPath);
            var options = new JsonSerializerOptions {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            };

            Metadata = JsonSerializer.Deserialize<Game>(jsonString, options)
                ?? throw new InvalidOperationException("CRITICAL ERROR: UFO 50 metadata deserialized to null");

            Paths = new GamePaths(Metadata);

            return true;
        }
    }
    public class GamePaths
    {
        public string MyModsPath { get; }
        public string VanillaCopyPath { get; }
        public string VanillaDataWinPath { get; }
        public string ModdedCopyPath { get; }
        public string ModdedCopyExePath { get; }
        public string ModdedCopySteamAppID { get; }
        public string HashDataPath { get; }
        public GamePaths(Game Metadata)
        {
            MyModsPath = Path.Combine(Constants.ModLoaderRoot, $"{Metadata.GameName} mods");
            VanillaCopyPath = Path.Combine(Constants.ModLoaderWorkspacePath, Metadata.InstallFolderName + " Vanilla Copy");
            VanillaDataWinPath = Path.Combine(VanillaCopyPath, "data.win");
            ModdedCopyPath = Path.Combine(Constants.ModLoaderWorkspacePath, Metadata.InstallFolderName + " Modded Copy");
            ModdedCopyExePath = Path.Combine(ModdedCopyPath, Metadata.ExeName);
            ModdedCopySteamAppID = Path.Combine(ModdedCopyPath, "steam_appid.txt");
            HashDataPath = Path.Combine(Constants.ModLoaderPath, "Games", Metadata.GameName, "hashes.json");
        }
    }
}