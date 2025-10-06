﻿namespace UFO_50_Mod_Loader
{
    public class AppSettings
    {
        public bool DarkModeEnabled { get; set; } = true;
        public bool AlwaysSelectFile { get; set; } = false;
        public bool QuickInstall { get; set; } = false;
        public string? GamePath { get; set; }
        public Dictionary<string, LocalModInfo> DownloadedMods { get; set; } = new Dictionary<string, LocalModInfo>();
        public Size MainWindowSize { get; set; } = new Size(1454, 954);
        public List<string> EnabledMods { get; set; } = new List<string>();
    }
}