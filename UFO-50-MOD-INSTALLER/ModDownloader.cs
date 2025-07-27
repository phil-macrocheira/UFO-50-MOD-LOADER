using System.Text.Json;

namespace UFO_50_MOD_INSTALLER
{
    internal class ModDownloader
    {
        public async Task DownloadMods(string downloadPath, bool skipExisting, string downloadedModsPath) {
            List<string> downloadedMods = new List<string>();
            List<string> newDownloadedMods = new List<string>();

            if (File.Exists(downloadedModsPath)) {
                downloadedMods = File.ReadAllLines(downloadedModsPath).ToList();
            }

            try {
                var (modIds, modNames) = await GetModInfo("23000");
                for (int i = 0; i < modIds.Count; i++) {
                    string modId = modIds[i];
                    string modName = modNames[i];
                    bool downloadedBefore = downloadedMods.Contains(modId);

                    if (skipExisting && downloadedBefore)
                        continue;

                    var (downloadUrl, filename) = await GetModFileInfo(modId);
                    if (filename.EndsWith(".zip")) {
                        Console.WriteLine($"Downloading {modName}...");
                        await DownloadFile(downloadUrl, Path.Combine(downloadPath, filename));
                        if (!downloadedBefore)
                            newDownloadedMods.Add(modId);
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Mod download failed: {ex.Message}");
            }
            File.AppendAllLines(downloadedModsPath, newDownloadedMods);
        }
        private static async Task DownloadFile(string downloadUrl, string filePath) {
            using var client = new HttpClient();
            var response = await client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();
            await File.WriteAllBytesAsync(filePath, await response.Content.ReadAsByteArrayAsync());
        }
        private static async Task<(string downloadUrl, string filename)> GetModFileInfo(string modId) {
            using var client = new HttpClient();
            string url = $"https://gamebanana.com/apiv11/Mod/{modId}/DownloadPage";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var firstFile = doc.RootElement.GetProperty("_aFiles")[0];
            string _sDownloadUrl = firstFile.GetProperty("_sDownloadUrl").GetString();
            string _sFile = firstFile.GetProperty("_sFile").GetString();
            return (_sDownloadUrl, _sFile);
        }
        private static async Task<(List<string> ids, List<string> names)> GetModInfo(string game_id) {
            var idRows = new List<string>();
            var names = new List<string>();
            var page = 1;

            using var client = new HttpClient();

            while (true) {
                var url = $"https://gamebanana.com/apiv11/Game/{game_id}/Subfeed?_nPage={page}";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                var hasData = false;

                foreach (var element in doc.RootElement.GetProperty("_aRecords").EnumerateArray()) {
                    if (element.TryGetProperty("_idRow", out var idRowProperty)) {
                        idRows.Add(idRowProperty.ToString());
                        hasData = true;
                    }
                    if (element.TryGetProperty("_sName", out var nameProperty)) {
                        names.Add(nameProperty.GetString());
                        hasData = true;
                    }
                }

                if (!hasData) break;
                page++;
            }

            return (idRows, names);
        }
    }
}