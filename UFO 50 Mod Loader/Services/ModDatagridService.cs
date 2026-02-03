using Avalonia;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UFO_50_Mod_Loader.Helpers;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services;
public class ModDatagridService : IDisposable
{
    public static readonly Bitmap DefaultIcon = GetDefaultIcon();
    private static Bitmap GetDefaultIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UFO_50_Mod_Loader.wrench.ico");
        return new Bitmap(stream);
    }

    private readonly List<FileWatcherService> _fileWatchers = new();
    private bool _disposed;
    public event Action? ModsChanged;
    public ModDatagridService()
    {
        CreateFileWatchers();
    }
    public void Initialize()
    {
        StartAllWatchers();
    }
    public List<Mod> LoadMods()
    {
        var mods = new List<Mod>();
        var seenMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try {
            var modDirectories = Directory.GetDirectories(Constants.MyModsPath);
            foreach (var modDir in modDirectories) {
                var mod = LoadModFromFolder(modDir);
                if (mod != null && seenMods.Add(mod.Name)) {
                    mods.Add(mod);
                }
            }
        }
        catch (Exception ex) {
            Logger.Log($"Error loading mods: {ex.Message}");
        }

        return mods.OrderBy(m => m.Name).ToList();
    }
    private Mod? LoadModFromFolder(string modFolder)
    {
        try {
            string parentModFolder = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(modFolder))!);
            string folderName = Path.GetFileName(modFolder);

            // Check if it is a mod
            if (!CheckIfMod.Check(modFolder))
                return null;

            // Load icon
            Bitmap icon = DefaultIcon;
            string? iconPath = Directory.GetFiles(modFolder, "*.png").FirstOrDefault();
            if (!string.IsNullOrEmpty(iconPath)) {
                try {
                    using var original = new Bitmap(iconPath);
                    icon = original.CreateScaledBitmap(new PixelSize(50, 50), BitmapInterpolationMode.None);
                }
                catch {
                    icon = DefaultIcon;
                    Logger.Log($"Failed to open icon at {iconPath}");
                }
            }

            // Load modder and description from txt file
            string modder = "";
            string description = "";
            string? txtPath = Directory.GetFiles(modFolder, "*.txt").FirstOrDefault();
            if (!string.IsNullOrEmpty(txtPath)) {
                try {
                    var lines = File.ReadLines(txtPath).Take(2).ToArray();
                    if (lines.Length > 0) modder = lines[0];
                    if (lines.Length > 1) description = lines[1];
                }
                catch {
                    Logger.Log($"Failed to load mod info for {folderName}");
                }
            }

            // Load version from gamebanana.json if it exists
            string modVersion = "";
            string jsonPath = Path.Combine(modFolder, GameBananaMetadata.FileName);
            if (File.Exists(jsonPath)) {
                try {
                    var json = File.ReadAllText(jsonPath);
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<GameBananaMetadata>(json);
                    if (metadata != null && !string.IsNullOrEmpty(metadata.Version)) {
                        modVersion = metadata.Version;
                    }
                }
                catch {
                    // Skip invalid JSON files
                }
            }

            // Set version for UFO 50 Modding Settings to match mod loader version
            if (folderName == "UFO 50 Modding Settings")
                modVersion = Constants.Version;

            return new Mod {
                IsEnabled = false,
                Name = folderName,
                Author = modder,
                Description = description,
                Icon = icon,
                ModVersion = modVersion
            };
        }
        catch {
            return null;
        }
    }
    private void CreateFileWatchers()
    {
        try {
            var watcher = new FileWatcherService(Constants.MyModsPath);
            watcher.FolderChanged += OnFolderChanged;
            _fileWatchers.Add(watcher);
        }
        catch (Exception ex) {
            Logger.Log($"Failed to create file watcher: {ex.Message}");
        }
    }
    private void StartAllWatchers()
    {
        foreach (var watcher in _fileWatchers) {
            watcher.Start();
        }
    }
    public void PauseWatchers()
    {
        foreach (var watcher in _fileWatchers)
            watcher.Pause();
    }
    public void ResumeWatchers()
    {
        foreach (var watcher in _fileWatchers)
            watcher.Resume();
    }
    private void OnFolderChanged()
    {
        ModsChanged?.Invoke();
    }
    private void DisposeWatchers()
    {
        foreach (var watcher in _fileWatchers) {
            watcher.FolderChanged -= OnFolderChanged;
            watcher.Dispose();
        }
        _fileWatchers.Clear();
    }
    public void Dispose()
    {
        if (_disposed) return;

        DisposeWatchers();
        _disposed = true;
    }
}