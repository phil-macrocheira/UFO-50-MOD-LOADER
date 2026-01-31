using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services
{
    public static class ModFolderService
    {
        private static readonly List<string> _modFolders = new();
        public static IReadOnlyList<string> ModFolders => _modFolders.AsReadOnly();

        public static event Action? ModFoldersChanged;
        public static void Initialize()
        {
            _modFolders.Clear();

            var savedFolders = SettingsService.Settings.ModFolders;
            if (savedFolders != null && savedFolders.Count > 0) {
                _modFolders.AddRange(savedFolders);
            }
            else {
                _modFolders.Add("downloaded mods");
                _modFolders.Add("my mods");
                Save();
            }

            EnsureDirectoriesExist();
        }
        public static void Set(List<string> modFolders)
        {
            _modFolders.Clear();
            _modFolders.AddRange(modFolders);
            EnsureDirectoriesExist();
            Save();
            ModFoldersChanged?.Invoke();
        }
        public static bool Add(string modFolder)
        {
            modFolder = SanitizeFolderName(modFolder);

            if (_modFolders.Contains(modFolder, StringComparer.OrdinalIgnoreCase)) {
                Logger.Log("ERROR: Mod Folder already exists.");
                return false;
            }

            _modFolders.Add(modFolder);
            EnsureDirectoryExists(modFolder);
            Save();
            ModFoldersChanged?.Invoke();

            Logger.Log($"Added mod folder: {modFolder}");
            return true;
        }
        public static bool Remove(string modFolder)
        {
            if (_modFolders.Count <= 1) {
                Logger.Log("ERROR: Cannot remove the last mod folder.");
                return false;
            }

            if (!_modFolders.Remove(modFolder)) {
                Logger.Log("ERROR: Mod folder not found.");
                return false;
            }

            Save();
            ModFoldersChanged?.Invoke();

            Logger.Log($"Removed mod folder: {modFolder}");
            return true;
        }
        public static bool Rename(string oldName, string newName)
        {
            newName = SanitizeFolderName(newName);

            if (_modFolders.Contains(newName, StringComparer.OrdinalIgnoreCase)) {
                Logger.Log("ERROR: Mod Folder with that name already exists.");
                return false;
            }

            int index = _modFolders.FindIndex(f =>
                f.Equals(oldName, StringComparison.OrdinalIgnoreCase));

            if (index < 0) {
                Logger.Log("ERROR: Original mod folder not found.");
                return false;
            }

            string oldPath = GetFullPath(oldName);
            string newPath = GetFullPath(newName);

            if (Directory.Exists(oldPath) && !Directory.Exists(newPath)) {
                try {
                    Directory.Move(oldPath, newPath);
                }
                catch (Exception ex) {
                    Logger.Log($"ERROR: Failed to rename directory: {ex.Message}");
                    return false;
                }
            }

            _modFolders[index] = newName;
            Save();
            ModFoldersChanged?.Invoke();

            Logger.Log($"Renamed mod folder: {oldName} -> {newName}");
            return true;
        }
        public static string GetFullPath(string modFolder)
        {
            return Path.Combine(Constants.ModLoaderRoot, modFolder);
        }
        public static IEnumerable<string> GetAllFullPaths()
        {
            return _modFolders.Select(GetFullPath);
        }
        public static bool Exists(string modFolder)
        {
            return _modFolders.Contains(modFolder, StringComparer.OrdinalIgnoreCase);
        }
        private static void Save()
        {
            SettingsService.Settings.ModFolders = _modFolders.ToList();
            SettingsService.Save();
        }
        private static void EnsureDirectoriesExist()
        {
            foreach (var folder in _modFolders) {
                EnsureDirectoryExists(folder);
            }
        }
        private static string SanitizeFolderName(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return string.Empty;

            folderName = folderName.Trim();

            // Remove invalid filename characters (includes slashes, colons, etc.)
            foreach (char c in Path.GetInvalidFileNameChars()) {
                folderName = folderName.Replace(c.ToString(), "");
            }

            folderName = folderName.Replace("..", "");
            return folderName.Trim();
        }
        private static void EnsureDirectoryExists(string modFolder)
        {
            string fullPath = GetFullPath(modFolder);
            if (!Directory.Exists(fullPath)) {
                try {
                    Directory.CreateDirectory(fullPath);
                    Logger.Log($"Created mod folder: {modFolder}");
                }
                catch (Exception ex) {
                    Logger.Log($"ERROR: Failed to create mod folder '{modFolder}': {ex.Message}");
                }
            }
        }
    }
}