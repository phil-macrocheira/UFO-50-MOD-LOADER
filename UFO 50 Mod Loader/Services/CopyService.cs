namespace UFO_50_Mod_Loader.Services;

public class CopyService
{
    public static void CopyDirectory(string sourcePath, string destinationPath, string ignoreExt="", string ignoreExt2="")
    {
        Directory.CreateDirectory(destinationPath);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourcePath)) {
            string fileName = Path.GetFileName(file);
            string extension = Path.GetExtension(file);

            if (string.Equals(extension, ignoreExt, StringComparison.OrdinalIgnoreCase))
                continue;
            else if (string.Equals(extension, ignoreExt2, StringComparison.OrdinalIgnoreCase))
                continue;

            string destFile = Path.Combine(destinationPath, fileName);
            File.Copy(file, destFile, overwrite: true);
        }

        // Recursively copy subdirectories
        foreach (var dir in Directory.GetDirectories(sourcePath)) {
            string dirName = Path.GetFileName(dir);
            string destDir = Path.Combine(destinationPath, dirName);
            CopyDirectory(dir, destDir);
        }
    }
}