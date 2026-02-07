using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Collections.Concurrent;
using System.Text.Json;
using UFO_50_Mod_Loader.Helpers;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services;

public class InstalledGameService
{
    private readonly Window _parentWindow;
    private string? _gamePath;
    private Dictionary<string, Dictionary<string, uint>>? _hashData;
    private string? _latestVersion;

    public InstalledGameService(Window parentWindow)
    {
        _parentWindow = parentWindow;
    }
    public async Task<bool> GetGamePath()
    {
        // Check if valid path already saved
        if (!string.IsNullOrEmpty(SettingsService.Settings.GamePath) &&
            IsValidGamePath(SettingsService.Settings.GamePath)) {
            _gamePath = SettingsService.Settings.GamePath;
            return true;
        }

        // Try common install locations
        var possiblePaths = GetPossibleGamePaths();
        foreach (string path in possiblePaths) {
            if (IsValidGamePath(path)) {
                _gamePath = path;
                SettingsService.Settings.GamePath = _gamePath;
                SettingsService.Save();
                Logger.Log($"UFO 50 install path found at {_gamePath}");
                return true;
            }
        }

        while (true) {
            await MessageBoxHelper.Show(_parentWindow, "Select UFO 50 Folder", "Could not find UFO 50 automatically.\nPlease select folder where UFO 50 is installed.");

            var result = await ShowFolderDialog();

            // User cancelled - close app
            if (result == null) {
                return false;
            }

            // Valid path selected
            if (IsValidGamePath(result)) {
                _gamePath = result;
                SettingsService.Settings.GamePath = _gamePath;
                SettingsService.Save();
                Logger.Log($"UFO 50 install path set to {_gamePath}");
                return true;
            }

            Logger.Log("Folder selected was not a UFO 50 install path. It must contain ufo50.exe and data.win.");
            Dispatcher.UIThread.RunJobs();
        }
    }

    private List<string> GetPossibleGamePaths()
    {
        var paths = new List<string>();

        // Windows
        if (Constants.IsWindows) {
            // Windows Steam paths
            paths.Add(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam", "steamapps", "common", "UFO 50"));
            paths.Add(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Steam", "steamapps", "common", "UFO 50"));

            // Common alternate Steam library locations
            paths.Add(@"D:\SteamLibrary\steamapps\common\UFO 50");
            paths.Add(@"E:\SteamLibrary\steamapps\common\UFO 50");
        }

        // Linux
        else if (Constants.IsLinux) {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Standard Linux Steam path
            paths.Add(Path.Combine(home, ".steam", "steam", "steamapps", "common", "UFO 50"));
            paths.Add(Path.Combine(home, ".local", "share", "Steam", "steamapps", "common", "UFO 50"));

            // Steam Deck / Flatpak Steam
            paths.Add(Path.Combine(home, ".var", "app", "com.valvesoftware.Steam",
                ".local", "share", "Steam", "steamapps", "common", "UFO 50"));
        }

        // Mac
        else if (Constants.IsOSX) {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(home, "Library", "Application Support", "Steam",
                "steamapps", "common", "UFO 50"));
        }

        return paths;
    }

    public bool IsValidGamePath(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return false;

        var dataWinPath = Path.Combine(path, "data.win");
        var exePath = Path.Combine(path, "ufo50.exe");

        return File.Exists(exePath) && File.Exists(dataWinPath);
    }

    private async Task<string?> ShowFolderDialog()
    {
        var topLevel = TopLevel.GetTopLevel(_parentWindow);
        if (topLevel == null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
            Title = "Select folder where UFO 50 is installed",
            AllowMultiple = false
        });

        if (folders.Count > 0) {
            return folders[0].Path.LocalPath;
        }

        return null;
    }
    public void LoadHashData()
    {
        if (_hashData != null) {
            return;
        }

        if (!Path.Exists(Constants.HashDataPath)) {
            Logger.Log($"[ERROR] ufo50_hashes.json file not found! Cannot verify UFO 50 version!");
            return;
        }

        try {
            using var stream = File.OpenRead(Constants.HashDataPath);
            _hashData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, uint>>>(stream);
        }
        catch (Exception e) {
            Logger.Log($"[ERROR] Failed to read ufo50_hashes.json file: {e.Message}");
            return;
        }

        try {
            _latestVersion = _hashData.Keys.Max(v => new Version(v)).ToString();
        }
        catch (Exception e) {
            _hashData = null;
            Logger.Log($"[ERROR] ufo50_hashes.json is missing the required data");
            return;
        }
    }
    public uint HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        uint hash = CRC32.Compute(stream);
        return hash;
    }
    private async Task<Dictionary<string, uint>> HashAllFilesAsync(string gamePath, IEnumerable<string> files)
    {
        var hashes = new ConcurrentDictionary<string, uint>();

        await Task.Run(() => {
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file => {
                var path = Path.Combine(gamePath, file);
                if (File.Exists(path)) {
                    hashes[file] = HashFile(path);
                }
            });
        });

        return new Dictionary<string, uint>(hashes);
    }
    public string? GetLatestVersion()
    {
        LoadHashData();
        return _latestVersion;
    }
    public async Task<string> GetGameVersionAsync(string gamePath, bool uninstallMode=false)
    {
        string version = "Unknown";
        bool canCopy = false;

        LoadHashData();
        if (_hashData == null) {
            return version;
        }

        string exePath = Path.Combine(gamePath, "ufo50.exe");
        if (!File.Exists(exePath)) {
            Logger.Log($"[ERROR] ufo50.exe was not found at {gamePath}! Can't check UFO 50 version!");
            return version;
        }

        uint exeHash = HashFile(exePath);
        foreach (var kvp in _hashData) {
            if (kvp.Value.TryGetValue("ufo50.exe", out uint hash) && hash == exeHash) {
                version = kvp.Key;
                canCopy = true;
                break;
            }
        }

        if (!canCopy) {
            var actualExeVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath).FileVersion;
            if (actualExeVersion != null && actualExeVersion.EndsWith(".0")) {
                actualExeVersion = actualExeVersion.Substring(0, actualExeVersion.Length - 2);
            }
            Logger.Log($"SETUP ERROR: Installed UFO 50 version ({actualExeVersion}) is probably a new version. Mod Loader update required.");
            return version;
        }

        var expectedFileHashes = _hashData[version];
        var fileHashes = await HashAllFilesAsync(gamePath, expectedFileHashes.Keys);

        if (fileHashes.Count != expectedFileHashes.Count) {
            canCopy = false;
            Logger.Log("SETUP ERROR: Cannot copy UFO 50 files. Installed UFO 50 version is missing files. Verify integrity of UFO 50 in Steam and select 'Verify Vanilla Copy' in the Mod Loader.");
        }
        else if (!expectedFileHashes.All(kvp => fileHashes.TryGetValue(kvp.Key, out var value) && value == kvp.Value)) {
            canCopy = false;
            Logger.Log("SETUP ERROR: Cannot copy UFO 50 files. Installed UFO 50 version is already modded. Verify integrity of UFO 50 in Steam and select 'Verify Vanilla Copy' in the Mod Loader.");
        }

        if (!canCopy) {
#if DEBUG
            // just for debugging for now, could show this to the user
            var differentKeys = fileHashes.Keys.Intersect(expectedFileHashes.Keys)
                .Where(k => fileHashes[k] != expectedFileHashes[k])
                .ToList();
            var missingKeys = fileHashes.Keys.Union(expectedFileHashes.Keys)
                .Except(fileHashes.Keys.Intersect(expectedFileHashes.Keys))
                .ToList();
#endif

            version = "Modded";
        }

        if (canCopy && version != _latestVersion) {
            if (!uninstallMode)
                Logger.Log("WARNING: Copying outdated UFO 50 version. Select 'Verify Vanilla Copy' to update your vanilla copy in the future.");
            else
                Logger.Log("WARNING: Installing outdated UFO 50 version. Verify integrity of UFO 50 in Steam to update to latest version.");
        }

        return version;
    }
    public bool CanCopy(string version)
    {
        return _hashData != null && _hashData.ContainsKey(version);
    }
    public HashSet<string> GetFileList(string version)
    {
        if (_hashData == null)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return _hashData[version].Keys.ToHashSet();
    }
}