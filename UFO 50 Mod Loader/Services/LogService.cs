using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services;

public static class LogService
{
    public static event Action<string>? OnLog;
    public static string MainLog { get; private set; } = "";
    public static bool _showingConflicts = false;

    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        MainLog += $"[{timestamp}] {message}\n";
        if (!_showingConflicts)
            OnLog?.Invoke(MainLog);
    }

    public static void ShowConflicts(string conflictMessage)
    {
        _showingConflicts = true;
        OnLog?.Invoke(conflictMessage);
    }

    public static void HideConflicts()
    {
        _showingConflicts = false;
        OnLog?.Invoke(MainLog);
    }
    public static void SaveLogToFile()
    {
        try {
            if (string.IsNullOrWhiteSpace(MainLog))
                return;
            File.WriteAllText(Constants.LogPath, MainLog);
        }
        catch (Exception ex) {
            OnLog?.Invoke($"Failed to save log to file: {ex.Message}");
        }
    }
}