using Avalonia.Media.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UFO_50_Mod_Loader
{
    public static class Constants
    {
        public static string Version => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        public static readonly Bitmap DefaultIcon;
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static string ModLoaderPath
        {
            get
            {
#if DEBUG
                return @"C:\Users\Phil\source\repos\UFO 50 Mod Loader";
#else
                return AppDomain.CurrentDomain.BaseDirectory;
#endif
            }
        }

        public static string MyModsPath => Path.Combine(ModLoaderPath, "my mods");
        public static string VanillaCopyPath => Path.Combine(ModLoaderPath, "UFO 50 Vanilla Copy");
        public static string ModdedCopyPath => Path.Combine(ModLoaderPath, "UFO 50 Modded Copy");
        public static string ModdedCopyExePath => Path.Combine(ModdedCopyPath, "ufo50.exe");
        public static string VanillaDataWinPath => Path.Combine(VanillaCopyPath, "data.win");
        public static string VanillaExtPath => Path.Combine(VanillaCopyPath, "ext");
        public static string SettingsPath => Path.Combine(ModLoaderPath, "settings.json");
        public static string HashDataPath => Path.Combine(ModLoaderPath, "Data", "ufo50_hashes.json");
        public static string GMLoaderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GMLoader");
        public static string GMLoaderIniPath => Path.Combine(GMLoaderPath, "GMLoader.ini");
        public static string GMLoaderDataWinPath => Path.Combine(GMLoaderPath, "data.win");
        public static string GMLoaderModsBasePath => Path.Combine(GMLoaderPath, "mods_base");
        public static string GMLoaderModsPath => Path.Combine(GMLoaderPath, "mods");
        public static string ModdingSettingsPath => Path.Combine(MyModsPath, "UFO 50 Modding Settings");
        public static string ModdingSettingsNameListPath = Path.Combine(ModdingSettingsPath, "code", "gml_Object_oModding_Other_10.gml");
        static Constants()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UFO_50_Mod_Loader.wrench.ico");
            DefaultIcon = new Bitmap(stream);
            stream.Dispose();
        }
    }
}