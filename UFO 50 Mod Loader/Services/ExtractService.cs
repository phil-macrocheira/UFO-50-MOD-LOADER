using SharpCompress.Archives;
using SharpCompress.Common;
using UFO_50_Mod_Loader.Models;

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

    private static string Extract(string archivePath)
    {
        using var archive = ArchiveFactory.Open(archivePath);
        var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
        string archiveName = Path.GetFileNameWithoutExtension(archivePath);
        string? commonRoot = GetCommonRootFolder(entries);
        string folderName;
        bool stripRoot;

        if (commonRoot != null) {
            folderName = commonRoot;
            stripRoot = true;
        }
        else {
            folderName = archiveName;
            stripRoot = false;
        }

        string destFolder = Path.Combine(Constants.MyModsPath, folderName);

        if (Directory.Exists(destFolder))
            Directory.Delete(destFolder, recursive: true);

        foreach (var entry in entries) {
            string entryPath = entry.Key!;

            if (stripRoot) {
                int separatorIndex = entryPath.IndexOfAny(['/', '\\']);
                entryPath = separatorIndex >= 0 ? entryPath[(separatorIndex + 1)..] : entryPath;
            }

            if (string.IsNullOrEmpty(entryPath))
                continue;

            string destPath = Path.Combine(destFolder, entryPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            entry.WriteToFile(destPath, new ExtractionOptions { Overwrite = true });
        }
        return destFolder;
    }

    private static string? GetCommonRootFolder(List<IArchiveEntry> entries)
    {
        if (entries.Count == 0)
            return null;

        string? commonRoot = null;

        foreach (var entry in entries) {
            string path = entry.Key!;
            int separatorIndex = path.IndexOfAny(['/', '\\']);
            if (separatorIndex < 0)
                return null;

            string root = path[..separatorIndex];

            if (commonRoot == null)
                commonRoot = root;
            else if (!commonRoot.Equals(root, StringComparison.OrdinalIgnoreCase))
                return null;
        }
        return commonRoot;
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