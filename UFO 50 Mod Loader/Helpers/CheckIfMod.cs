namespace UFO_50_Mod_Loader.Helpers
{
    public static class CheckIfMod
    {
        public static bool Check(string modFolder)
        {
            var validModFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "code", "textures", "config", "ext", "audio" };
            foreach (var subDir in Directory.GetDirectories(modFolder, "*", SearchOption.TopDirectoryOnly)) {
                if (validModFolders.Contains(Path.GetFileName(subDir))) {
                    return true;
                }
            }
            return false;
        }
    }
}