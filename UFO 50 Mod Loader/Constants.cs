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
        public static string SettingsPath => Path.Combine(ModLoaderPath, "settings.json");
        public static string HashDataPath => Path.Combine(ModLoaderPath, "Data", "ufo50_hashes.json");
        public static string GMLoaderIniPath => Path.Combine(ModLoaderPath, "GMLoader", "GMLoader.ini");
        static Constants()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UFO_50_Mod_Loader.wrench.ico");
            DefaultIcon = new Bitmap(stream);
            stream.Dispose();
        }
    }
}