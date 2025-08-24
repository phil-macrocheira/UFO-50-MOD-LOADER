namespace UFO_50_Mod_Loader
{
    internal class ModUninstaller
    {
        public void uninstallMods(string currentPath, string gamePath) {
            ModInstaller modInstaller = new ModInstaller();

            string vanilla_data_winPath = Path.Combine(currentPath, "vanilla.win");
            string vanilla_localizationPath = Path.Combine(currentPath, "localization", "vanilla", "ext");
            string vanilla_audioPath = Path.Combine(currentPath, "audio", "vanilla");

            string ufo50_data_winPath = Path.Combine(gamePath, "data.win");
            string ufo50_localizationPath = Path.Combine(gamePath, "ext");

            DialogResult dialogUninstall = MessageBox.Show("Uninstall all UFO 50 mods?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogUninstall == DialogResult.No)
                return;

            try {
                // Copy vanilla files back to game path
                File.Copy(vanilla_data_winPath, ufo50_data_winPath, true);
                modInstaller.CopyDirectory(vanilla_localizationPath, ufo50_localizationPath);
                modInstaller.CopyDirectory(vanilla_audioPath, gamePath, false, ".dat");

                // Delete added dlls
                foreach (string file in Directory.GetFiles(gamePath, "*.dll")) {
                    string filename = Path.GetFileName(file);
                    if (!string.Equals(filename, "steam_api64.dll", StringComparison.OrdinalIgnoreCase) && !string.Equals(filename, "Steamworks_x64.dll", StringComparison.OrdinalIgnoreCase))
                        File.Delete(file);
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"Uninstallation Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            MessageBox.Show("Mods Uninstalled", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}