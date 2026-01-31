using Avalonia.Media.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UFO_50_Mod_Loader.Models
{
    public static class Constants
    {
        public static string Version => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        public static readonly Bitmap DefaultIcon;
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static string ModLoaderPath => AppDomain.CurrentDomain.BaseDirectory;
        public static string ModLoaderRoot => Path.GetFullPath(Path.Combine(ModLoaderPath, ".."));
        public static string DownloadedModsPath => Path.Combine(ModLoaderRoot, "downloaded mods");
        public static string VanillaCopyPath => Path.Combine(ModLoaderPath, "UFO 50 Vanilla Copy");
        public static string ModdedCopyPath => Path.Combine(ModLoaderPath, "UFO 50 Modded Copy");
        public static string ModdedCopyExePath => Path.Combine(ModdedCopyPath, "ufo50.exe");
        public static string ModdedCopySteamAppID => Path.Combine(ModdedCopyPath, "steam_appid.txt");
        public static string VanillaDataWinPath => Path.Combine(VanillaCopyPath, "data.win");
        public static string VanillaExtPath => Path.Combine(VanillaCopyPath, "ext");
        public static string SettingsPath => Path.Combine(ModLoaderPath, "settings.json");
        public static string HashDataPath => Path.Combine(ModLoaderPath, "Data", "ufo50_hashes.json");
        public static string GMLoaderIniPath => Path.Combine(ModLoaderPath, "GMLoader.ini");
        public static string GMLoaderDataWinPath => Path.Combine(ModLoaderPath, "data.win");
        public static string GMLoaderModsBasePath => Path.Combine(ModLoaderPath, "mods_base");
        public static string GMLoaderModsPath => Path.Combine(ModLoaderPath, "mods");
        public static string GameBananaID = "23000";
        public static string SteamAppID = "1147860";
        public static string RepoUrl => "https://github.com/phil-macrocheira/UFO-50-MOD-LOADER";
        static Constants()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UFO_50_Mod_Loader.wrench.ico");
            DefaultIcon = new Bitmap(stream);
            stream.Dispose();
        }
    }
}