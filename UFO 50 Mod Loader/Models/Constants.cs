using System.Reflection;
using System.Runtime.InteropServices;

namespace UFO_50_Mod_Loader.Models
{
    public static class Constants
    {
        public static readonly string ProgramName = "UFO 50 Mod Loader";
        public static readonly string AppID = "UFO-50-Mod-Loader";
        public static readonly string RepoUrl = "https://github.com/phil-macrocheira/UFO-50-MOD-LOADER";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static readonly bool IsSteamOS = IsLinux && File.Exists("/etc/steamos-release");
        public static readonly string ModLoaderPath = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string ModLoaderRoot = IsWindows ? Path.GetFullPath(Path.Combine(ModLoaderPath, "..")) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppID);
        public static readonly string ModLoaderWorkspacePath = Path.Combine(ModLoaderRoot, "workspace");
        public static readonly string SettingsPath = Path.Combine(ModLoaderWorkspacePath, "settings.json");
        public static readonly string GMLoaderIniPath = Path.Combine(ModLoaderPath, "GMLoader.ini");
        public static readonly string GMLoaderModsBasePath = Path.Combine(ModLoaderPath, "mods_base");
        public static readonly string GMLoaderDataWinPath = Path.Combine(ModLoaderWorkspacePath, "data.win");
        public static readonly string GMLoaderModsPath = Path.Combine(ModLoaderWorkspacePath, "mods");
        public static readonly string PackagesPath = Path.Combine(ModLoaderRoot, "packages");
        public static readonly string PackagedModsPath = Path.Combine(ModLoaderPath, "packaged mods");
        public static readonly string LogPath = Path.Combine(ModLoaderRoot, "log.txt");
    }
}
