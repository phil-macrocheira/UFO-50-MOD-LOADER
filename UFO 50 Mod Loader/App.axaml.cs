using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Serilog;
using UFO_50_Mod_Loader.Helpers;

namespace UFO_50_Mod_Loader
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            Dispatcher.UIThread.UnhandledException += (sender, e) =>
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await ShowFatal(e.Exception, "Unhandled Exception");
                });

                // we set handled to true for now to let the app keep running after the exception,
                // but we shutdown as soon as the error message is closed so we don't continue in a bad state
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await ShowFatal(e.Exception, "Unhandled Task Exception");
                });

                // same thing as above, mark it as observed to prevent the app from dying for now
                e.SetObserved();
            };

            base.OnFrameworkInitializationCompleted();
        }

        private static async Task ShowFatal(Exception ex, string title)
        {
            Logger.Log($"{title}: {ex}");

            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.MainWindow;
                if (window is not null)
                {
                    await MessageBoxHelper.Show(window, title, ex.ToString());
                }

                desktop.Shutdown(1);
            }
            else
            {
                Environment.Exit(1);
            }
        }
    }
}