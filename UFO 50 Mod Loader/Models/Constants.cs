using Avalonia.Media.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UFO_50_Mod_Loader.Models
{
    public static class Constants
    {
        public static readonly string Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static readonly string ModLoaderPath = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string ModLoaderRoot = Path.GetFullPath(Path.Combine(ModLoaderPath, ".."));
        public static readonly string MyModsPath = Path.Combine(ModLoaderRoot, "my mods");
        public static readonly string VanillaCopyPath = Path.Combine(ModLoaderPath, "UFO 50 Vanilla Copy");
        public static readonly string ModdedCopyPath = Path.Combine(ModLoaderRoot, "UFO 50 Modded Copy");
        public static readonly string ModdedCopyExePath = Path.Combine(ModdedCopyPath, "ufo50.exe");
        public static readonly string ModdedCopySteamAppID = Path.Combine(ModdedCopyPath, "steam_appid.txt");
        public static readonly string VanillaDataWinPath = Path.Combine(VanillaCopyPath, "data.win");
        public static readonly string VanillaExtPath = Path.Combine(VanillaCopyPath, "ext");
        public static readonly string SettingsPath = Path.Combine(ModLoaderRoot, "settings.json");
        public static readonly string HashDataPath = Path.Combine(ModLoaderPath, "Data", "ufo50_hashes.json");
        public static readonly string GMLoaderIniPath = Path.Combine(ModLoaderPath, "GMLoader.ini");
        public static readonly string GMLoaderDataWinPath = Path.Combine(ModLoaderPath, "data.win");
        public static readonly string GMLoaderModsBasePath = Path.Combine(ModLoaderPath, "mods_base");
        public static readonly string GMLoaderModsPath = Path.Combine(ModLoaderPath, "mods");
        public static readonly string PackagesPath = Path.Combine(ModLoaderRoot, "packages");
        public static readonly string GameBananaID = "23000";
        public static readonly string SteamAppID = "1147860";
        public static readonly string RepoUrl = "https://github.com/phil-macrocheira/UFO-50-MOD-LOADER";
    }
}