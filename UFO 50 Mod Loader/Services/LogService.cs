using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services;

public static class LogService
{
    public static event Action<string>? OnLog;
    public static string MainLog { get; private set; } = "";
    public static bool _showingConflicts = false;
    public static bool _showingDependencies = false;
    private static string _currentConflictMessage = "";
    private static string _currentDependencyMessage = "";

    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        MainLog += $"[{timestamp}] {message}\n";
        if (!_showingConflicts && !_showingDependencies)
            OnLog?.Invoke(MainLog);
    }

    public static void ShowConflicts(string conflictMessage)
    {
        _currentConflictMessage = conflictMessage;
        _showingConflicts = true;
        UpdateDisplay();
    }

    public static void HideConflicts()
    {
        _showingConflicts = false;
        UpdateDisplay();
    }
    public static void ShowDependencies(string dependencyMessage)
    {
        _currentDependencyMessage = dependencyMessage;
        _showingDependencies = true;
        UpdateDisplay();
    }

    public static void HideDependencies()
    {
        _showingDependencies = false;
        UpdateDisplay();
    }
    private static void UpdateDisplay()
    {
        if (_showingConflicts) {
            OnLog?.Invoke(_currentConflictMessage);
        }
        else if (_showingDependencies) {
            OnLog?.Invoke(_currentDependencyMessage);
        }
        else {
            OnLog?.Invoke(MainLog);
        }
    }
    public static void SaveLogToFile()
    {
        try {
            if (string.IsNullOrWhiteSpace(MainLog))
                return;

            File.AppendAllText(Constants.LogPath, MainLog);

            // Trim log file if exceeds 10mb
            if (LogFileIsMaxSize(10 * 1024 * 1024)) {
                TrimLogFile();
            }
        }
        catch (Exception ex) {
            OnLog?.Invoke($"Failed to save log to file: {ex.Message}");
        }
    }
    private static bool LogFileIsMaxSize(long maxBytes)
    {
        if (File.Exists(Constants.LogPath)) {
            var info = new FileInfo(Constants.LogPath);
            if (info.Length >= maxBytes)
                return true;
            else
                return false;
        }
        return false;
    }
    private static void TrimLogFile()
    {
        var content = File.ReadAllText(Constants.LogPath);
        var trimmed = content.Substring(content.Length / 2);
        var newlineIndex = trimmed.IndexOf('\n');
        if (newlineIndex >= 0)
            trimmed = trimmed.Substring(newlineIndex + 1);
        File.WriteAllText(Constants.LogPath, trimmed);
    }
}