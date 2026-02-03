using SharpCompress.Archives;
using SharpCompress.Common;
using UFO_50_Mod_Loader.Models;
using UFO_50_Mod_Loader.Helpers;

namespace UFO_50_Mod_Loader.Services;

public static class ExtractService
{
    public static async void HandleArchiveAdded(string archivePath)
    {
        await WaitForFileReady(archivePath);

        try {
            string destFolder = Extract(archivePath);
            File.Delete(archivePath);
            Logger.Log($"Extracted {Path.GetFileName(archivePath)}");
        }
        catch (InvalidDataException ex) {
            Logger.Log($"Cannot unzip {Path.GetFileName(archivePath)} - {ex.Message}");
        }
        catch (IOException ex) {
            Logger.Log($"Cannot unzip {Path.GetFileName(archivePath)} - {ex.Message}");
        }
    }
    public static async Task<string?> ExtractAsync(string archivePath)
    {
        await WaitForFileReady(archivePath);

        try {
            string destFolder = Extract(archivePath);
            File.Delete(archivePath);
            Logger.Log($"Extracted {Path.GetFileName(archivePath)}");
            return destFolder;
        }
        catch (InvalidDataException ex) {
            Logger.Log($"Cannot unzip {Path.GetFileName(archivePath)} - {ex.Message}");
            return null;
        }
        catch (IOException ex) {
            Logger.Log($"Cannot unzip {Path.GetFileName(archivePath)} - {ex.Message}");
            return null;
        }
    }
    private static string Extract(string archivePath)
    {
        using var archive = ArchiveFactory.Open(archivePath);

        string destFolder = Path.Combine(
            Path.GetDirectoryName(archivePath)!,
            Path.GetFileNameWithoutExtension(archivePath)
        );

        foreach (var entry in archive.Entries) {
            if (entry.IsDirectory)
                continue;

            var filePath = Path.Combine(destFolder, entry.Key!);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            entry.WriteToFile(filePath, new ExtractionOptions { Overwrite = true });
        }

        string modFolder = CopyOutModFolder(destFolder);
        return modFolder;
    }
    private static string CopyOutModFolder(string extractedFolderPath)
    {
        string modFolderPath = FindModFolder(extractedFolderPath);

        if (modFolderPath == extractedFolderPath) {
            return extractedFolderPath;
        }

        string modFolderName = Path.GetFileName(modFolderPath.TrimEnd(Path.DirectorySeparatorChar));
        string destPath = Path.Combine(Constants.MyModsPath, modFolderName);

        if (Directory.Exists(destPath))
            Directory.Delete(destPath, recursive: true);

        CopyService.CopyDirectory(modFolderPath, destPath);
        Directory.Delete(extractedFolderPath, recursive: true);

        return destPath;
    }
    private static string FindModFolder(string root)
    {
        if (CheckIfMod.Check(root))
            return root;

        foreach (var dir in Directory.GetDirectories(root)) {
            var result = FindModFolder(dir);
            if (CheckIfMod.Check(result))
                return result;
        }
        return root;
    }
    private static async Task WaitForFileReady(string path, int maxAttempts = 10)
    {
        for (int i = 0; i < maxAttempts; i++) {
            try {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return;
            }
            catch (IOException) {
                await Task.Delay(300);
            }
        }
    }
}