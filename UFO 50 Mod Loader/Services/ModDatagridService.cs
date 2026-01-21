using Avalonia;
using Avalonia.Media.Imaging;
using SkiaSharp;
using System.IO;
using System.Reflection;

namespace UFO_50_Mod_Loader.Services;

public class ModDatagridService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private string _mymodsFolder;
    private bool _disposed;

    public event Action? ModsChanged;

    public ModDatagridService()
    {
        _mymodsFolder = Constants.MyModsPath;
    }

    public string ModsFolder
    {
        get => _mymodsFolder;
        set
        {
            if (_mymodsFolder != value) {
                _mymodsFolder = value;
                RestartWatcher();
                ModsChanged?.Invoke();
            }
        }
    }

    public void Initialize()
    {
        EnsureModsFolderExists();
        StartWatcher();
    }

    private void EnsureModsFolderExists()
    {
        if (!Directory.Exists(_mymodsFolder)) {
            Directory.CreateDirectory(_mymodsFolder);
        }
    }

    public List<Mod> LoadMods()
    {
        var mods = new List<Mod>();

        try {
            var modFolders = Directory.GetDirectories(_mymodsFolder);
            foreach (var modFolder in modFolders) {
                var mod = LoadModFromFolder(modFolder);
                if (mod != null) {
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
            var folderName = Path.GetFileName(modFolder);

            // Load icon
            Bitmap icon = Constants.DefaultIcon;
            string? iconPath = Directory.GetFiles(modFolder, "*.png").FirstOrDefault();
            if (!string.IsNullOrEmpty(iconPath)) {
                try {
                    using var original = new Bitmap(iconPath);
                    icon = original.CreateScaledBitmap(new PixelSize(50, 50), BitmapInterpolationMode.None);
                }
                catch {
                    icon = Constants.DefaultIcon;
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
                Icon = icon
            };
        }
        catch {
            return null;
        }
    }

    private void StartWatcher()
    {
        if (!Directory.Exists(_mymodsFolder))
            return;

        _watcher = new FileSystemWatcher(_mymodsFolder) {
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnModsFolderChanged;
        _watcher.Deleted += OnModsFolderChanged;
        _watcher.Renamed += OnModsFolderRenamed;
    }

    private void StopWatcher()
    {
        if (_watcher != null) {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnModsFolderChanged;
            _watcher.Deleted -= OnModsFolderChanged;
            _watcher.Renamed -= OnModsFolderRenamed;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void RestartWatcher()
    {
        StopWatcher();
        EnsureModsFolderExists();
        StartWatcher();
    }

    private void OnModsFolderChanged(object sender, FileSystemEventArgs e)
    {
        ModsChanged?.Invoke();
    }

    private void OnModsFolderRenamed(object sender, RenamedEventArgs e)
    {
        ModsChanged?.Invoke();
    }

    public void Dispose()
    {
        if (!_disposed) {
            StopWatcher();
            _disposed = true;
        }
    }
}