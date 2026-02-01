using Avalonia.Controls;
using Avalonia.Threading;
using System.Diagnostics;
using UFO_50_Mod_Loader.Helpers;
using UFO_50_Mod_Loader.Models;
using Velopack;
using Velopack.Locators;
using Velopack.Logging;
using Velopack.Sources;

namespace UFO_50_Mod_Loader.Services
{
    internal class SelfUpdaterService
    {
        public static bool JustUpdatedOrInstalled { get; private set; }

        public static void OnUpdateOrInstall()
        {
            JustUpdatedOrInstalled = true;
        }

        public static IVelopackLocator GetDebugLocator()
        {
            return new TestVelopackLocator(
                appId: "UFO-50-Mod-Loader",
                version: Constants.Version,
                packagesDir: Constants.PackagesPath,
                logger: new DebugVelopackLogger()
            );
        }

        public static void CheckForUpdates(Window parent)
        {
            Dispatcher.UIThread.Post(async () =>
            {
#if DEBUG
                var source = new SimpleFileSource(new DirectoryInfo("../../../../../Releases"));
#else
                var source = new GithubSource(Constants.RepoUrl, null, true);
#endif

                var updateManager = new UpdateManager(source, new UpdateOptions() { AllowVersionDowngrade = true });
                var updateInfo = await updateManager.CheckForUpdatesAsync();

                if (updateInfo is null)
                {
                    await MessageBoxHelper.Show(parent, "No updates found", "Up to date.");
                }
                else
                {
                    await updateManager.DownloadUpdatesAsync(updateInfo);
                    var restartNow = await MessageBoxHelper.Show(parent, "Update found", $"A new version is available. Restart now to update to version {updateInfo.TargetFullRelease.Version}?", "Restart Now", "Restart Later");

                    if (restartNow)
                    {
                        updateManager.ApplyUpdatesAndRestart(updateInfo);
                    }
                }
            });
        }

        internal class DebugVelopackLogger : IVelopackLogger
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
    }
}
