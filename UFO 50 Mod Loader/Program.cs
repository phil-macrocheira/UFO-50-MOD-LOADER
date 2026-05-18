using Avalonia;
using System.Runtime.InteropServices;
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
            // Workspace directory is required for loading the settings
            try {
                Directory.CreateDirectory(Constants.ModLoaderWorkspacePath);
            }
            catch (Exception ex) {
                Logger.Log($"[ERROR] Failed to create workspace folder: {ex.Message}");
            }

            // ─── HEADLESS ────────────────────────────────────────────────────
            if (args.Contains("--headless")) {
                // WinExe GUI apps have no console by default; attach to the calling terminal
                // so --json output and error messages are visible to the caller.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    AttachConsole(-1);

                var opt = CliParser.Parse(args);
                if (opt is null) {
                    Console.Error.WriteLine(CliParser.Usage);
                    Environment.Exit(10);
                }
                int code = HeadlessRunner.RunAsync(opt).GetAwaiter().GetResult();
                Environment.Exit(code);
                return;
            }
            // ─── END HEADLESS ────────────────────────────────────────────────

            // Load settings first!
            SettingsService.Load();

#if DEBUG
            // For some reason Velopack asks for a packages path for debug, but it doesn't actually work so we need to clear it
            if (Directory.Exists(Constants.PackagesPath)) {
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

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
