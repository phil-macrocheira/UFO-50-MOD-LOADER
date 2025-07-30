namespace UFO_50_MOD_INSTALLER
{
    internal static class Program
    {
        [STAThread]
        static void Main() {
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            ApplicationConfiguration.Initialize();
            SettingsService.Load();
            Application.Run(new MainForm());
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e) {
            ShowExceptionDetails(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            ShowExceptionDetails(e.ExceptionObject as Exception);
        }

        private static void ShowExceptionDetails(Exception? ex) {
            if (ex == null) {
                MessageBox.Show("An unknown fatal error occurred.", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            MessageBox.Show($"A critical error occurred and the application must close.\n\n" +
                            $"Error: {ex.Message}\n\n" +
                            $"Stack Trace:\n{ex.StackTrace}",
                "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }
}