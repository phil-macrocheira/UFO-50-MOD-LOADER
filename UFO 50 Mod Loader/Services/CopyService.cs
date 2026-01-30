namespace UFO_50_Mod_Loader.Services;

public class CopyService
{
    public static void CopyDirectory(string sourcePath, string destinationPath, HashSet<string>? versionFileSet = null, string relativePath = "")
    {
        Directory.CreateDirectory(destinationPath);

        foreach (var file in Directory.GetFiles(sourcePath)) {
            string fileName = Path.GetFileName(file);
            string relativeFilePath = string.IsNullOrEmpty(relativePath)
                ? fileName
                : Path.Combine(relativePath, fileName);

            if (versionFileSet != null && !versionFileSet.Contains(relativeFilePath)) {
                continue;
            }

            string destFile = Path.Combine(destinationPath, fileName);
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourcePath)) {
            string dirName = Path.GetFileName(dir);
            string destDir = Path.Combine(destinationPath, dirName);
            string newRelativePath = string.IsNullOrEmpty(relativePath)
                ? dirName
                : Path.Combine(relativePath, dirName);
            CopyDirectory(dir, destDir, versionFileSet, newRelativePath);
        }
    }
    public static async Task CopyDirectoryAsync(string sourcePath, string destinationPath, HashSet<string>? versionFileSet = null, string relativePath = "")
    {
        Directory.CreateDirectory(destinationPath);

        var files = Directory.GetFiles(sourcePath);
        var filesToCopy = new List<(string source, string dest)>();

        foreach (var file in files) {
            string fileName = Path.GetFileName(file);
            string relativeFilePath = string.IsNullOrEmpty(relativePath)
                ? fileName
                : Path.Combine(relativePath, fileName);

            if (versionFileSet != null && !versionFileSet.Contains(relativeFilePath)) {
                continue;
            }

            string destFile = Path.Combine(destinationPath, fileName);
            filesToCopy.Add((file, destFile));
        }

        // Copy files in parallel
        await Parallel.ForEachAsync(filesToCopy, async (filePair, ct) => {
            await using var sourceStream = new FileStream(filePair.source, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            await using var destStream = new FileStream(filePair.dest, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await sourceStream.CopyToAsync(destStream, ct);
        });

        // Process subdirectories in parallel
        var dirs = Directory.GetDirectories(sourcePath);
        var tasks = dirs.Select(dir => {
            string dirName = Path.GetFileName(dir);
            string destDir = Path.Combine(destinationPath, dirName);
            string newRelativePath = string.IsNullOrEmpty(relativePath)
                ? dirName
                : Path.Combine(relativePath, dirName);
            return CopyDirectoryAsync(dir, destDir, versionFileSet, newRelativePath);
        });

        await Task.WhenAll(tasks);
    }
}