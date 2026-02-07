using Avalonia.Controls;
using GMLoader;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader.Services
{
    public class InstallService
    {
        public static async Task<GMLoaderResult?> InstallModsAsync(Window parent, List<string> enabledModPaths, InstalledGameService gameService)
        {
            Logger._showingConflicts = false;
            string gamePath = SettingsService.Settings.GamePath;

            if (SettingsService.Settings.CopiedVanillaVersion == null) {
                Logger.Log($"Cannot Install Mods: Vanilla game files have not been copied yet");
                return null;
            }

            await ModdingSettingsAddModNamesAsync(enabledModPaths);

            if (!SettingsService.Settings.OverwriteMode) {
                if (Directory.Exists(Constants.ModdedCopyPath)) {
                    try {
                        Directory.Delete(Constants.ModdedCopyPath, recursive: true);
                    }
                    catch (Exception ex) {
                        Logger.Log($"[ERROR] Failed to delete {Path.GetFileName(Constants.ModdedCopyPath)}: {ex.Message}");
                        return null;
                    }
                }
                await Task.Run(() => CopyService.CopyDirectory(Constants.VanillaCopyPath, Constants.ModdedCopyPath));
                gamePath = Constants.ModdedCopyPath;
                File.WriteAllText(Constants.ModdedCopySteamAppID, Constants.SteamAppID);

                // Time to do some hacky shit!
                // Modded Copy crashes under some circumstances on startup due to handling an event too early.
                //
                // Specifically it is the WM_DEVICECHANGE event with the DBT_DEVNODES_CHANGED param.
                // If this event is handled at an early point in the game's initialization, it causes a crash!
                // For some reason, this problem doesn't happen when launching the game through Steam (still need to
                // investigate why!) which is why we only need to do this for Modded Copy and not for Overwrite mode.
                //
                // To fix this, we're just gonna patch out one early call to the runtime's ProcessMessages function.
                // This function is called in multiple places, we're just removing one specific very early call to it,
                // which shouldn't have any noticeable impact on how the game runs.
                //
                // To patch it out, we just need to replace some specific bytes at a specific location in ufo50.exe with
                // the "nop" opcode. On x64 this is just a single byte opcode with the value 0x90. We need to nop a call
                // instruction, which is multiple bytes long (check the code for the exact number of bytes), so we just
                // use a string of nop bytes.
                //
                // To find the correct place in the code to patch, use a disassembler like Ghidra and search for usages
                // of the string "Process Messages\n". There should be only one usage, and you should find code that
                // looks somewhat like this:
                //     lea  rdx, [rel data_process_messages] { "Process Messages\n"}
                //     call qword [rax + 0x10]
                //     call ProcessMessages
                // Obviously you won't see the names of things, I've added those in, but this is the layout of code you
                // should see.
                //
                // We will be replacing that second call instruction with nops. We just need to note down the address of
                // that call instruction (`processMessagesVirtualAddress` in the code below), and how many bytes the
                // call opcode is (`processMessagesCallSize`). Then we just need to translate that virtual address to a
                // raw file offset inside the .exe file. We can do this by looking at the file headers. Our code is in
                // the .text segment (this will always be true) so we will need to know both the base virtual address
                // for that segment (`textVirtualBaseAddress`), and the base file offset for it (`textRawBaseAddress`).
                // From these it's pretty simple to just subtract them and calculate the offset into the file we need.
                //
                // All of these file offsets are, of course, only going to be true for one specific version of the
                // GameMaker runtime. If the UFO 50 devs update Game Maker, then the runtime will change and we will
                // need to update these offsets for the new version. To account for this, let's make sure to only patch
                // the file if it matches the hash that we expect. This means we can handle past and future versions of
                // UFO 50 that ship with the same runtime for each hash we check for.
                //
                // - Blaise

                uint expectedExeHash = 557362388;                
                if (gameService.HashFile(Constants.ModdedCopyExePath) == expectedExeHash)
                {
                    // addresses from the headers
                    long textVirtualBaseAddress = 0x140001000;
                    long textRawBaseAddress = 0x400;

                    // address and size for the code to replace
                    long processMessagesVirtualAddress = 0x1402b6da7;
                    int processMessagesCallSize = 5;

                    // final address to write to the file at
                    long processMessagesRawAddress = processMessagesVirtualAddress - textVirtualBaseAddress + textRawBaseAddress;

                    using (var stream = new FileStream(Constants.ModdedCopyExePath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        stream.Position = processMessagesRawAddress;

                        byte nopOpcode = 0x90;
                        stream.Write(Enumerable.Repeat(nopOpcode, processMessagesCallSize).ToArray(), 0, processMessagesCallSize);
                    }
                }
            }

            await GenerateModsFolderAsync(gamePath, enabledModPaths);

            return await RunGMLoaderAsync(gamePath, enabledModPaths);
        }

        private static async Task ModdingSettingsAddModNamesAsync(List<string> enabledModPaths)
        {
            string? moddingSettingsPath = Path.Combine(Constants.MyModsPath, "UFO 50 Modding Settings");

            if (moddingSettingsPath == null)
                return;

            string nameListPath = Path.Combine(moddingSettingsPath, "code", "gml_Object_oModding_Other_10.gml");

            // Ensure the code directory exists
            string codeDir = Path.GetDirectoryName(nameListPath)!;
            if (!Directory.Exists(codeDir)) {
                Directory.CreateDirectory(codeDir);
            }

            await File.WriteAllTextAsync(nameListPath, "global.mod_list = ds_list_create();\n");

            foreach (string modPath in enabledModPaths) {
                string modName = Path.GetFileName(modPath);
                if (modName == "UFO 50 Modding Settings")
                    continue;
                await File.AppendAllTextAsync(nameListPath, $"ds_list_add(global.mod_list, \"{modName}\");\n");
            }
        }

        private static async Task GenerateModsFolderAsync(string gamePath, List<string> enabledModPaths)
        {
            await Task.Run(() => File.Copy(Constants.VanillaDataWinPath, Constants.GMLoaderDataWinPath, overwrite: true));

            if (Directory.Exists(Constants.GMLoaderModsPath)) {
                try {
                    Directory.Delete(Constants.GMLoaderModsPath, recursive: true);
                }
                catch (Exception ex) {
                    Logger.Log($"[ERROR] Failed to delete existing mods workspace folder: {ex.Message}");
                }
            }
            await Task.Run(() => CopyService.CopyDirectory(Constants.GMLoaderModsBasePath, Constants.GMLoaderModsPath));

            var reorderedModPaths = enabledModPaths
                .OrderBy(m => Path.GetFileName(m) != "UFO 50 Modding Settings")
                .ThenBy(m => m);

            foreach (string mod in reorderedModPaths) {
                if (!Directory.Exists(mod)) {
                    Logger.Log($"[WARNING] Mod folder not found, skipping: {mod}");
                    continue;
                }

                foreach (string subFolder in Directory.GetDirectories(mod)) {
                    string folderName = Path.GetFileName(subFolder);
                    string destSubFolder = Path.Combine(Constants.GMLoaderModsPath, folderName);

                    if (folderName == "ext") {
                        destSubFolder = Path.Combine(gamePath, "ext");
                    }
                    else if (folderName == "audio") {
                        foreach (string audiogroup in Directory.GetFiles(subFolder)) {
                            string destFile = Path.Combine(gamePath, Path.GetFileName(audiogroup));
                            await Task.Run(() => File.Copy(audiogroup, destFile, true));
                        }
                        continue;
                    }
                    else if (folderName == "dll") {
                        foreach (string dll in Directory.GetFiles(subFolder)) {
                            string destFile = Path.Combine(gamePath, Path.GetFileName(dll));
                            await Task.Run(() => File.Copy(dll, destFile, true));
                        }
                        continue;
                    }

                    await Task.Run(() => CopyService.CopyDirectory(subFolder, destSubFolder));
                }
            }
        }

        private static async Task<GMLoaderResult?> RunGMLoaderAsync(string gamePath, List<string> enabledModPaths)
        {
            try {
                return await Task.Run(() => {
                    GMLoaderProgram.Initialize((level, message) => {
                        switch (level) {
                            case GMLogLevel.Info:
                                Logger.Log($"{message}");
                                break;
                            case GMLogLevel.Warning:
                                Logger.Log($"[WARNING] {message}");
                                break;
                            case GMLogLevel.Error:
                                Logger.Log($"[ERROR] {message}");
                                break;
                        }
                    });

                    GMLoaderResult result = GMLoaderProgram.Run(Constants.GMLoaderIniPath, Constants.ModLoaderWorkspacePath, enabledModPaths);

                    if (result.Success) {
                        var GameDataWinPath = Path.Combine(gamePath, "data.win");
                        File.Copy(Constants.GMLoaderDataWinPath, GameDataWinPath, overwrite: true);
                        File.Delete(Constants.GMLoaderDataWinPath);

                        if (SettingsService.Settings.OverwriteMode)
                            Logger.Log("Mods installed successfully!");
                        else
                            Logger.Log("Mods loaded successfully!");
                    }
                    else {
                        Logger.Log($"Mod installation failed.");
                    }

                    return result;
                });
            }
            catch (Exception ex) {
                Logger.Log($"[ERROR] {ex.Message}");
                return null;
            }
        }
    }
}