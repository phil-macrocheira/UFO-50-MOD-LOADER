namespace UFO_50_Mod_Loader
{
    public class Settings
    {
        public string Version { get; set; } = Constants.Version;
        public string? GamePath { get; set; }
        public double MainWindowWidth { get; set; } = 700;
        public double MainWindowHeight { get; set; } = 630;
        public bool OverwriteMode { get; set; } = false;
        // public bool EnabledTop { get; set; } = false;
        public bool SelectDownloadFile { get; set; } = false;
        public bool CopiedGameFiles { get; set; } = false;
        public List<string> EnabledMods { get; set; } = new();
    }
}