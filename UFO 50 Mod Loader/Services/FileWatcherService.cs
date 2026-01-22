namespace UFO_50_Mod_Loader.Services;

public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private string _watchPath;
    private bool _disposed;

    public event Action? FolderChanged;

    public FileWatcherService(string watchPath)
    {
        _watchPath = watchPath;
    }

    public string WatchPath
    {
        get => _watchPath;
        set
        {
            if (_watchPath != value) {
                _watchPath = value;
                Restart();
            }
        }
    }

    public void Start()
    {
        if (!Directory.Exists(_watchPath))
            Directory.CreateDirectory(_watchPath);

        _watcher = new FileSystemWatcher(_watchPath) {
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnCreated;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;
    }

    public void Stop()
    {
        if (_watcher != null) {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnCreated;
            _watcher.Deleted -= OnChanged;
            _watcher.Renamed -= OnRenamed;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        string ext = Path.GetExtension(e.FullPath).ToLowerInvariant();

        if (ext is ".zip" or ".7z") {
            ExtractService.HandleArchiveAdded(e.FullPath);
        }
        else if (Directory.Exists(e.FullPath)) {
            FolderChanged?.Invoke();
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        FolderChanged?.Invoke();
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        FolderChanged?.Invoke();
    }

    public void Dispose()
    {
        if (!_disposed) {
            Stop();
            _disposed = true;
        }
    }
}