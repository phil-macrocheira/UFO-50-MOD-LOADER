using Avalonia;
using System.Diagnostics;
using System.Reflection;
using Velopack;
using Velopack.Locators;
using Velopack.Logging;

namespace UFO_50_Mod_Loader
{
    internal class ConsoleVelopackLogger : IVelopackLogger
    {
        public void Log(VelopackLogLevel logLevel, string? message, Exception? exception)
        {
            var logMessage = $"[{DateTime.Now.ToShortTimeString()}] [{logLevel}] {message}";
            if (exception is not null)
            {
                logMessage += Environment.NewLine + exception;
            }

            Debug.WriteLine(logMessage);
        }
    }

    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            VelopackApp.Build()
                .OnFirstRun((version) => {
                    string sourceFolder = "my mods";
                    string destinationFolder = "../my mods";

                    if (!Directory.Exists(destinationFolder))
                    {
                        CopyService.CopyDirectory(sourceFolder, destinationFolder);
                    }
                })
#if DEBUG
                .SetLocator(new TestVelopackLocator(appId: "UFO-50-Mod-Loader", version: Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "", packagesDir: "../packages", logger: new ConsoleVelopackLogger()))
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
