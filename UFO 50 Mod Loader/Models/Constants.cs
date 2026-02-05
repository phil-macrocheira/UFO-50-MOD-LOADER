using Avalonia.Media.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UFO_50_Mod_Loader.Models
{
    public static class Constants
    {
        public static readonly string AppID = "UFO-50-Mod-Loader";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static readonly bool IsSteamOS = IsLinux && File.Exists("/etc/steamos-release");
        public static readonly string ModLoaderPath = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string ModLoaderRoot = IsWindows ? Path.GetFullPath(Path.Combine(ModLoaderPath, "..")) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppID);
        public static readonly string ModLoaderWorkspacePath = Path.Combine(ModLoaderRoot, "workspace");
        public static readonly string MyModsPath = Path.Combine(ModLoaderRoot, "my mods");
        public static readonly string VanillaCopyPath = Path.Combine(ModLoaderWorkspacePath, "UFO 50 Vanilla Copy");
        public static readonly string ModdedCopyPath = Path.Combine(ModLoaderWorkspacePath, "UFO 50 Modded Copy");
        public static readonly string ModdedCopyExePath = Path.Combine(ModdedCopyPath, "ufo50.exe");
        public static readonly string ModdedCopySteamAppID = Path.Combine(ModdedCopyPath, "steam_appid.txt");
        public static readonly string VanillaDataWinPath = Path.Combine(VanillaCopyPath, "data.win");
        public static readonly string SettingsPath = Path.Combine(ModLoaderWorkspacePath, "settings.json");
        public static readonly string HashDataPath = Path.Combine(ModLoaderPath, "Data", "ufo50_hashes.json");
        public static readonly string GMLoaderIniPath = Path.Combine(ModLoaderPath, "GMLoader.ini");
        public static readonly string GMLoaderModsBasePath = Path.Combine(ModLoaderPath, "mods_base");
        public static readonly string GMLoaderDataWinPath = Path.Combine(ModLoaderWorkspacePath, "data.win");
        public static readonly string GMLoaderModsPath = Path.Combine(ModLoaderWorkspacePath, "mods");
        public static readonly string PackagesPath = Path.Combine(ModLoaderRoot, "packages");
        public static readonly string LogPath = Path.Combine(ModLoaderRoot, "log.txt");
        public static readonly string GameBananaID = "23000";
        public static readonly string SteamAppID = "1147860";
        public static readonly string RepoUrl = "https://github.com/phil-macrocheira/UFO-50-MOD-LOADER";
        public static readonly string PackagedMods = "packaged mods";
    }
}