namespace UFO_50_Mod_Loader.Services;
public static class LogService
{
    public static event Action<string>? OnLog;

    public static void Log(string message)
    {
        OnLog?.Invoke(message);
    }
}