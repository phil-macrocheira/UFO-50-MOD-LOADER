namespace UFO_50_Mod_Loader.Services;

public static class LogService
{
    public static event Action<string>? OnLog;
    public static string MainLog { get; private set; } = "";
    private static bool _showingConflicts = false;

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
}