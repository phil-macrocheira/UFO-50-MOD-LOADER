using Avalonia.Controls;
using Avalonia.Threading;
using System.Diagnostics;
using System.Threading;
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
        private static volatile bool checkingForUpdates;

        public static void OnUpdateOrInstall()
        {
            JustUpdatedOrInstalled = true;
        }

        public static IVelopackLocator GetDebugLocator()
        {
            return new TestVelopackLocator(
                appId: Constants.AppID,
                version: Constants.Version,
                packagesDir: Constants.PackagesPath,
                logger: new DebugVelopackLogger()
            );
        }

        public static void CheckForUpdates(Window parent, bool automatic = false)
        {
            if (checkingForUpdates)
            {
                return;
            }

            checkingForUpdates = true;

            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
#if DEBUG
                    var source = new SimpleFileSource(new DirectoryInfo("../../../../../Releases"));
#else
                    var source = new GithubSource(Constants.RepoUrl, null, true);
#endif

                    var updateManager = new UpdateManager(source, new UpdateOptions() { AllowVersionDowngrade = true });

                    UpdateInfo? updateInfo;
                    try
                    {
                        updateInfo = await updateManager.CheckForUpdatesAsync();
                    }
                    catch (Velopack.Exceptions.NotInstalledException ex)
                    {
                        Logger.Log($"Can't check for updates: {ex.Message}");
                        return;
                    }

                    if (updateInfo is not null)
                    {
                        await updateManager.DownloadUpdatesAsync(updateInfo);

                        var automaticUpdates = SettingsService.Settings.CheckForUpdatesAutomatically;
                        var applyUpdates = await MessageBoxHelper.Show(parent, "Update found",
                            $"A new version is available. {(automaticUpdates ? "Restart now to update" : "Update")} to version {updateInfo.TargetFullRelease.Version}?",
                            automaticUpdates ? "Restart Now" : "Yes", automaticUpdates ? "Restart Later" : "No");

                        if (applyUpdates)
                        {
                            Logger.Log($"Downloading update for version {updateInfo.TargetFullRelease.Version}");
                            updateManager.ApplyUpdatesAndRestart(updateInfo);
                        }
                    }
                    else if (!automatic)
                    {
                        await MessageBoxHelper.Show(parent, "No updates found", "Up to date.");
                    }
                }
                finally
                {
                    checkingForUpdates = false;
                }
            }, DispatcherPriority.Send);
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
