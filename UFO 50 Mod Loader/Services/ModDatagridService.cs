using Avalonia;
using Avalonia.Media.Imaging;

namespace UFO_50_Mod_Loader.Services;

public class ModDatagridService : IDisposable
{
    private readonly FileWatcherService _fileWatcher;
    private bool _disposed;

    public event Action? ModsChanged;

    public ModDatagridService()
    {
        _fileWatcher = new FileWatcherService(Constants.MyModsPath);
        _fileWatcher.FolderChanged += OnFolderChanged;
    }

    public string ModsFolder
    {
        get => _fileWatcher.WatchPath;
        set
        {
            if (_fileWatcher.WatchPath != value) {
                _fileWatcher.WatchPath = value;
                ModsChanged?.Invoke();
            }
        }
    }

    public void Initialize()
    {
        _fileWatcher.Start();
    }

    public List<Mod> LoadMods()
    {
        var mods = new List<Mod>();

        try {
            var modFolders = Directory.GetDirectories(_fileWatcher.WatchPath);
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

    private void OnFolderChanged()
    {
        ModsChanged?.Invoke();
    }

    public void Dispose()
    {
        if (!_disposed) {
            _fileWatcher.FolderChanged -= OnFolderChanged;
            _fileWatcher.Dispose();
            _disposed = true;
        }
    }
}