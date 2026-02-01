using Avalonia;
using UFO_50_Mod_Loader.Models;
using Velopack;

namespace UFO_50_Mod_Loader
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Load settings first!
            SettingsService.Load();

#if DEBUG
            // For some reason Velopack asks for a packages path for debug, but it doesn't actually work so we need to clear it
            if (Directory.Exists(Constants.PackagesPath))
            {
                Directory.Delete(Constants.PackagesPath, true);
            }
            Directory.CreateDirectory(Constants.PackagesPath);
#endif

            VelopackApp.Build()
                .OnFirstRun((version) => SelfUpdaterService.OnUpdateOrInstall())
                .OnRestarted((version) => SelfUpdaterService.OnUpdateOrInstall())
                .SetAutoApplyOnStartup(SettingsService.Settings.CheckForUpdatesAutomatically)
#if DEBUG
                .SetLocator(SelfUpdaterService.GetDebugLocator())
#endif
                .Run();
            
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
