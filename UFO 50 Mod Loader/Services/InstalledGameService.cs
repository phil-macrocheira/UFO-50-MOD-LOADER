using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Collections.Concurrent;
using System.Text.Json;
using UFO_50_Mod_Loader.Helpers;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services;

public class GameVersionStatus
{
    public string Exe { get; set; } = "Unknown";
    public string DataWin { get; set; } = "Unknown";
    public string AudioFiles { get; set; } = "Unknown";
    public string TextureGroupFiles { get; set; } = "Unknown";
    public string FontFiles { get; set; } = "Unknown";
    public string LocalizationFiles { get; set; } = "Unknown";

    public override string ToString()
    {
        return
            $"ufo50.exe: {Exe}\n" +
            $"data.win: {DataWin}\n" +
            $"Audio Files: {AudioFiles}\n" +
            $"Texture Group Files: {TextureGroupFiles}\n" +
            $"Font Files: {FontFiles}\n" +
            $"Localization Files: {LocalizationFiles}";
    }
}
public class InstalledGameService
{
    private string? _gamePath;
    private readonly Window _parentWindow;

    public string? GamePath => _gamePath;
    public Dictionary<string, Dictionary<string, long>>? hashData;

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
    public bool LoadHashData()
    {
        if (!Path.Exists(Constants.HashDataPath)) {
            return false;
        }
        using var stream = File.OpenRead(Constants.HashDataPath);
        hashData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, long>>>(stream);

        if (Constants.IsLinux) {
            hashData = hashData.ToDictionary(
                kvp => kvp.Key.Replace('\\', '/'),
                kvp => kvp.Value
            );
        }

        return true;
    }
    private uint HashFile(string file)
    {
        using var stream = File.OpenRead(file);
        uint hash = CRC32.Compute(stream);
        return hash;
    }
    private async Task<Dictionary<string, uint>> HashAllFilesAsync(string folder)
    {
        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
        var hashes = new ConcurrentDictionary<string, uint>();

        await Task.Run(() => {
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file => {
                hashes[file] = HashFile(file);
            });
        });

        return new Dictionary<string, uint>(hashes);
    }
    public string GetFileVersion(string file, uint hash)
    {
        if (hashData == null)
            return "Unknown";

        if (hashData.TryGetValue(file, out var versions)) {
            foreach (var v in versions.Reverse()) {
                if ((ulong)hash == (ulong)v.Value)
                    return v.Key; // Return most recent version
            }
        }
        return "Modded";
    }
    private string GetLatestVersion()
    {
        if (hashData == null || hashData.Count == 0) {
            return "0.0.0";
        }
        return hashData.Values.First().Keys.Max(v => new Version(v)).ToString();
    }
    public async Task<bool> GetGameVersionAsync(string gamePath, bool uninstallMode=false)
    {
        var fileHashes = await HashAllFilesAsync(gamePath);
        var fileVersions = new Dictionary<string, string>();

        foreach (var kvp in fileHashes) {
            string file = Path.GetRelativePath(gamePath, kvp.Key);
            uint hash = kvp.Value;
            string fileVersion = GetFileVersion(file, hash);
            fileVersions[file] = fileVersion;
        }

        var groups = new Dictionary<string, List<string>> {
            ["Exe"] = new List<string>(),
            ["DataWin"] = new List<string>(),
            ["AudioFiles"] = new List<string>(),
            ["TextureGroupFiles"] = new List<string>(),
            ["FontFiles"] = new List<string>(),
            ["LocalizationFiles"] = new List<string>()
        };

        foreach (var kvp in fileVersions) {
            string path = kvp.Key;

            if (path.Equals("ufo50.exe", StringComparison.OrdinalIgnoreCase))
                groups["Exe"].Add(path);
            else if (path.Equals("data.win", StringComparison.OrdinalIgnoreCase))
                groups["DataWin"].Add(path);
            else if (path.StartsWith("ext" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                groups["LocalizationFiles"].Add(path);
            else if (path.StartsWith("fonts" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                groups["FontFiles"].Add(path);
            else if (path.StartsWith("Textures" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                groups["TextureGroupFiles"].Add(path);
            else if (Path.GetFileName(path).StartsWith("audiogroup", StringComparison.OrdinalIgnoreCase))
                groups["AudioFiles"].Add(path);
        }

        string DetermineGroupVersion(IEnumerable<string> files)
        {
            var versions = files.Select(f => fileVersions[f]).ToList();
            if (versions.Contains("Modded")) return "Modded";
            return versions.Distinct().Count() == 1 ? versions.First() : "Mismatched Vanilla";
        }

        var status = new GameVersionStatus {
            Exe = DetermineGroupVersion(groups["Exe"]),
            DataWin = DetermineGroupVersion(groups["DataWin"]),
            AudioFiles = DetermineGroupVersion(groups["AudioFiles"]),
            TextureGroupFiles = DetermineGroupVersion(groups["TextureGroupFiles"]),
            FontFiles = DetermineGroupVersion(groups["FontFiles"]),
            LocalizationFiles = DetermineGroupVersion(groups["LocalizationFiles"])
        };

        var allGroupVersions = new[] {
            status.Exe,
            status.DataWin,
            status.AudioFiles,
            status.TextureGroupFiles,
            status.FontFiles,
            status.LocalizationFiles
        };

        string gameVersion = "";
        string latestVersion = GetLatestVersion();
        bool CanCopy = true;

        if (allGroupVersions.Any(v => v == "Modded")) {
            Logger.Log("SETUP ERROR: Cannot copy UFO 50 files. Installed UFO 50 version is already modded. Verify UFO 50 in Steam and select 'Verify Vanilla Copy' in the Mod Loader.");
            CanCopy = false;
        }
        else if (allGroupVersions.Any(v => v == "Mismatched Vanilla")) {
            Logger.Log("SETUP ERROR: Cannot copy UFO 50 files. Installed UFO 50 version is somehow a mix of different versions. Verify UFO 50 in Steam and select 'Verify Vanilla Copy' in the Mod Loader.");
            CanCopy = false;
        }
        else if (allGroupVersions.Distinct().Count() == 1) {
            gameVersion = allGroupVersions.First();
        }
        else {
            Logger.Log("SETUP ERROR: Cannot copy UFO 50 files. Installed UFO 50 version is somehow a mix of different versions. Verify UFO 50 in Steam and select 'Verify Vanilla Copy' in the Mod Loader.");
            CanCopy = false;
        }

        if (CanCopy && gameVersion != latestVersion) {
            if (!uninstallMode)
                Logger.Log("WARNING: Copying outdated UFO 50 version. Select 'Verify Vanilla Copy' to update your vanilla copy in the future.");
            else
                Logger.Log("WARNING: Installing outdated UFO 50 version. Verify UFO 50 in Steam to update to latest version.");
        }

        return CanCopy;
    }
    public bool CheckExe()
    {
        string fullPath = Path.Join(SettingsService.Settings.GamePath, "ufo50.exe");
        var actualExeVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(fullPath);

        uint hash = HashFile(fullPath);
        string fileVersion = GetFileVersion("ufo50.exe", hash);

        if (fileVersion == "Modded") {
            Logger.Log($"WARNING: Installed UFO 50 version ({actualExeVersion}) is probably a new version. Mod Loader update recommended.");
            SettingsService.Settings.CopiedGameFiles = false;
            SettingsService.Save();
            return false;
        }
        return true;
    }
}