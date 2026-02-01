using Avalonia;
using Avalonia.Media.Imaging;
using System.Reflection;
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
        ModFolderService.ModFoldersChanged += OnModFoldersChanged;
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

        foreach (var modFolder in ModFolderService.ModFolders) {
            var fullPath = ModFolderService.GetFullPath(modFolder);

            if (!Directory.Exists(fullPath))
                continue;

            try {
                var modDirectories = Directory.GetDirectories(fullPath);
                foreach (var modDir in modDirectories) {
                    var mod = LoadModFromFolder(modDir);
                    if (mod != null && seenMods.Add(mod.Name)) {
                        mods.Add(mod);
                    }
                }
            }
            catch (Exception ex) {
                Logger.Log($"Error loading mods from {fullPath}: {ex.Message}");
            }
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

            return new Mod {
                IsEnabled = false,
                Name = folderName,
                Author = modder,
                Description = description,
                Icon = icon,
                ModFolder = parentModFolder
            };
        }
        catch {
            return null;
        }
    }
    private void OnModFoldersChanged()
    {
        // Recreate watchers when mod folders change
        DisposeWatchers();
        CreateFileWatchers();
        StartAllWatchers();
        ModsChanged?.Invoke();
    }
    private void CreateFileWatchers()
    {
        foreach (var modFolder in ModFolderService.ModFolders) {
            var fullPath = ModFolderService.GetFullPath(modFolder);

            if (!Directory.Exists(fullPath))
                continue;

            try {
                var watcher = new FileWatcherService(fullPath);
                watcher.FolderChanged += OnFolderChanged;
                _fileWatchers.Add(watcher);
            }
            catch (Exception ex) {
                Logger.Log($"Failed to create watcher for {fullPath}: {ex.Message}");
            }
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

        ModFolderService.ModFoldersChanged -= OnModFoldersChanged;
        DisposeWatchers();
        _disposed = true;
    }
}