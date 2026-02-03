namespace UFO_50_Mod_Loader.Helpers
{
    public static class CheckIfMod
    {
        public static bool Check(string modFolder)
        {
            /* Return false if an .xdelta file is found
            if (Directory.GetFiles(modFolder, "*.xdelta", SearchOption.AllDirectories).Length > 0) {
                Logger.Log($"{Path.GetFileName(modFolder)} is a DeltaPatch mod and is not compatible with UFO 50 Mod Loader");
                return false;
            } */

            var validModFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "code", "textures", "config", "ext", "audio", "room" };
            foreach (var subDir in Directory.GetDirectories(modFolder, "*", SearchOption.TopDirectoryOnly)) {
                if (validModFolders.Contains(Path.GetFileName(subDir))) {
                    return true;
                }
            }
            return false;
        }
    }
}