using System.Security.Policy;

namespace UFO_50_Mod_Loader
{
    public class AppSettings
    {
        public string Version { get; set; } = MainForm.version;
        public bool DarkModeEnabled { get; set; } = true;
        public bool AlwaysSelectFile { get; set; } = false;
        public bool QuickInstall { get; set; } = false;
        public string? GamePath { get; set; }
        public Size MainWindowSize { get; set; } = new Size(1454, 954);
        public List<string> EnabledMods { get; set; } = new List<string>();
    }
}