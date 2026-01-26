using GMLoader;

namespace UFO_50_Mod_Loader.Services
{
    public class InstallService
    {
        public static void InstallMods()
        {
            GMLoaderProgram.Initialize((level, message) =>
            {
                switch (level)
                {
                    case GMLogLevel.Debug:
                        Logger.Log($"[DEBUG] {message}");
                        break;
                    case GMLogLevel.Info:
                        Logger.Log($"{message}");
                        break;
                    case GMLogLevel.Warning:
                        Logger.Log($"[WARNING] {message}");
                        break;
                    case GMLogLevel.Error:
                        Logger.Log($"[ERROR] {message}");
                        break;
                }
            });

            GMLoaderResult result = GMLoaderProgram.Run(Constants.GMLoaderIniPath);

            if (result.Success) {
                Logger.Log("Mods intalled successfully!");
            }
            else {
                Logger.Log($"Mod installation failed: {result.ErrorMessage}");

                if (result.Exception != null) {
                    Logger.Log($"Mod installation failed: {result.ErrorMessage}");
                }
            }
        }
    }
}
