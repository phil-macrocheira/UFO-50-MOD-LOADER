namespace UFO_50_Mod_Loader.Models
{
    public class Settings
    {
        public string Version { get; set; } = Constants.Version;
        public string? GamePath { get; set; }
        public double MainWindowWidth { get; set; } = 700;
        public double MainWindowHeight { get; set; } = 585;
        public double TextBoxHeight { get; set; } = 145;
        public bool FirstTimeRun { get; set; } = true;
        public bool OverwriteMode { get; set; } = false;
        // public bool EnabledTop { get; set; } = false;
        public bool CopiedGameFiles { get; set; } = false;
        // public List<string> ModFolders { get; set; } = new() {"downloaded mods", "my mods"};
        public List<String> SelectedMods { get; set; } = new();
        public Dictionary<string, List<string>> ModLists { get; set; } = new();
        public bool CheckForUpdatesAutomatically { get; set; } = true;
    }
}