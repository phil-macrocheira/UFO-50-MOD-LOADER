using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UFO_50_MOD_INSTALLER
{
    public class ModInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Creator { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string PageUrl { get; set; }
        public long DateUpdated { get; set; }
        public long DateAdded { get; set; }
        public int Views { get; set; }
        public int Likes { get; set; }
        public string Version { get; set; }
    }

    public class LocalModInfo
    {
        public long DateUpdated { get; set; }
    }

    public class ModFile
    {
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }
    }

    internal class ModDownloader
    {
        public async Task DownloadMods(string downloadPath, List<ModFile> filesToDownload, string? localModInfoPath, Dictionary<string, ModInfo> fileToModInfoMap) {
            var localMods = new Dictionary<string, LocalModInfo>();
            if (!string.IsNullOrEmpty(localModInfoPath)) {
                if (File.Exists(localModInfoPath)) {
                    var json = File.ReadAllText(localModInfoPath);
                    localMods = JsonSerializer.Deserialize<Dictionary<string, LocalModInfo>>(json) ?? new Dictionary<string, LocalModInfo>();
                }
            }

            if (File.Exists(localModInfoPath)) {
                var json = File.ReadAllText(localModInfoPath);
                localMods = JsonSerializer.Deserialize<Dictionary<string, LocalModInfo>>(json) ?? new Dictionary<string, LocalModInfo>();
            }

            try {
                foreach (var file in filesToDownload) {
                    Console.WriteLine($"Downloading {file.FileName}...");
                    await DownloadFile(file.DownloadUrl, Path.Combine(downloadPath, file.FileName));


                    if (fileToModInfoMap.TryGetValue(file.FileName, out var modInfo)) {
                        localMods[modInfo.Id] = new LocalModInfo { DateUpdated = modInfo.DateUpdated };
                    }
                }
            }
            finally {
                if (!string.IsNullOrEmpty(localModInfoPath)) {
                    var updatedJson = JsonSerializer.Serialize(localMods, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(localModInfoPath, updatedJson);
                }
            }
        }

        public static ModInfo? FindBestModMatch(List<ModInfo> allMods, string titleToMatch, string creatorToMatch) {
            // Phase 1: Exact title match, with creator as a tie-breaker.
            var exactMatches = allMods
                .Where(m => m.Name.Equals(titleToMatch, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exactMatches.Count == 1) {
                return exactMatches[0]; // Perfect match
            }
            if (exactMatches.Count > 1) {
                var creatorMatch = exactMatches
                    .FirstOrDefault(m => m.Creator.Equals(creatorToMatch, StringComparison.OrdinalIgnoreCase));
                if (creatorMatch != null) return creatorMatch; // Perfect match with tie-breaker
            }

            // Phase 2: Author fallback - find all mods by author and check titles.
            if (creatorToMatch != "Unknown") {
                var modsByAuthor = allMods.Where(m => m.Creator.Equals(creatorToMatch, StringComparison.OrdinalIgnoreCase)).ToList();
                var authorMatch = modsByAuthor.FirstOrDefault(m => m.Name.Equals(titleToMatch, StringComparison.OrdinalIgnoreCase));
                if (authorMatch != null) return authorMatch;
            }

            // Phase 3: Fuzzy title match - the ultimate fallback.
            const double FuzzinessThreshold = 0.60; // Confidence threshold (60%)
            ModInfo? bestFuzzyMatch = null;
            double highestSimilarity = 0.0;

            foreach (var remoteMod in allMods) {
                double similarity = StringSimilarity.CalculateTitleSimilarity(titleToMatch, remoteMod.Name);
                if (similarity > highestSimilarity) {
                    highestSimilarity = similarity;
                    bestFuzzyMatch = remoteMod;
                }
            }

            if (highestSimilarity >= FuzzinessThreshold) {
                return bestFuzzyMatch;
            }

            return null; // If all else fails, no confident match was found.
        }


        public static async Task<List<ModInfo>> GetModInfo(string game_id) {
            var mods = new List<ModInfo>();
            var page = 1;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "UFO50ModInstaller/1.0");

            while (true) {
                var url = $"https://gamebanana.com/apiv11/Game/{game_id}/Subfeed?_nPage={page}&_sSort=default";

                HttpResponseMessage response;
                try {
                    response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException e) {
                    throw new Exception($"Failed to connect to Gamebanana. Status Code: {e.StatusCode}. Message: {e.Message}", e);
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var records = doc.RootElement.GetProperty("_aRecords");

                if (records.GetArrayLength() == 0) break;

                foreach (var element in records.EnumerateArray()) {
                    if (!element.TryGetProperty("_sModelName", out var modelNameElement) || modelNameElement.GetString() != "Mod") {
                        continue;
                    }

                    element.TryGetProperty("_idRow", out var idElement);
                    element.TryGetProperty("_sName", out var nameElement);
                    element.TryGetProperty("_sProfileUrl", out var pageUrlElement);
                    element.TryGetProperty("_sText", out var descElement);
                    element.TryGetProperty("_sVersion", out var versionElement);
                    string version = versionElement.ValueKind != JsonValueKind.Undefined ? versionElement.GetString() ?? "N/A" : "N/A";

                    long dateUpdated = 0;
                    if (element.TryGetProperty("_tsDateUpdated", out var dateUpdatedElement)) {
                        dateUpdated = dateUpdatedElement.TryGetInt64(out var val) ? val : 0;
                    }

                    long dateAdded = 0;
                    if (element.TryGetProperty("_tsDateAdded", out var dateAddedElement)) {
                        dateAdded = dateAddedElement.TryGetInt64(out var val) ? val : 0;
                    }

                    int views = 0;
                    if (element.TryGetProperty("_nViewCount", out var viewsElement)) {
                        views = viewsElement.TryGetInt32(out var val) ? val : 0;
                    }

                    int likes = 0;
                    if (element.TryGetProperty("_nLikeCount", out var likesElement)) {
                        likes = likesElement.TryGetInt32(out var val) ? val : 0;
                    }

                    string creatorName = "N/A";
                    if (element.TryGetProperty("_aSubmitter", out var s) && s.TryGetProperty("_sName", out var n)) {
                        creatorName = n.GetString() ?? "N/A";
                    }

                    string imageUrl = "";
                    if (element.TryGetProperty("_aPreviewMedia", out var media) && media.TryGetProperty("_aImages", out var imgs) && imgs.GetArrayLength() > 0) {
                        if (imgs[0].TryGetProperty("_sBaseUrl", out var bu) && imgs[0].TryGetProperty("_sFile", out var f))
                            imageUrl = $"{bu.GetString()}/{f.GetString()}";
                    }

                    if (idElement.ValueKind != JsonValueKind.Undefined && nameElement.ValueKind != JsonValueKind.Undefined) {
                        mods.Add(new ModInfo
                        {
                            Id = idElement.ToString(),
                            Name = nameElement.GetString() ?? "Unnamed Mod",
                            PageUrl = pageUrlElement.ValueKind != JsonValueKind.Undefined ? pageUrlElement.GetString() : "",
                            Creator = creatorName,
                            Description = descElement.ValueKind != JsonValueKind.Undefined ? descElement.GetString() : "",
                            ImageUrl = imageUrl,
                            DateUpdated = dateUpdated,
                            DateAdded = dateAdded,
                            Views = views,
                            Likes = likes,
                            Version = version
                        });
                    }
                }
                page++;
            }
            return mods;
        }
        public static async Task<string> GetModFullDescription(string modId) {
            if (string.IsNullOrEmpty(modId)) return "No description available.";

            try {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "UFO50ModInstaller/1.0");

                // Use the ProfilePage endpoint to get the full description
                var url = $"https://gamebanana.com/apiv11/Mod/{modId}/ProfilePage";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                // The full body text is in the "_sText" field of this endpoint's response.
                if (doc.RootElement.TryGetProperty("_sText", out var textElement)) {
                    string rawHtml = "";
                    if (textElement.ValueKind == JsonValueKind.Array && textElement.GetArrayLength() > 0) {
                        rawHtml = textElement[0].GetString() ?? "";
                    }
                    else {
                        rawHtml = textElement.GetString() ?? "";
                    }

                    // 1. Decode HTML entities like &nbsp;
                    string decodedText = WebUtility.HtmlDecode(rawHtml);

                    // 2. Use a regular expression to strip out all remaining HTML tags like <br>
                    string plainText = Regex.Replace(decodedText, "<.*?>", String.Empty);

                    return plainText.Trim();
                }
            }
            catch (Exception ex) {
                return $"Could not load full description. Error: {ex.Message}";
            }

            return "Description not found.";
        }


        public static async Task<List<ModFile>> GetModFileInfo(string modId) {
            using var client = new HttpClient();
            var files = new List<ModFile>();
            string url = $"https://gamebanana.com/apiv11/Mod/{modId}/DownloadPage";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            foreach (var fileElement in doc.RootElement.GetProperty("_aFiles").EnumerateArray()) {
                files.Add(new ModFile
                {
                    FileName = fileElement.GetProperty("_sFile").GetString() ?? "unknown.zip",
                    DownloadUrl = fileElement.GetProperty("_sDownloadUrl").GetString() ?? ""
                });
            }
            return files;
        }

        private static async Task DownloadFile(string downloadUrl, string filePath) {
            using var client = new HttpClient();
            var response = await client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();
            await File.WriteAllBytesAsync(filePath, await response.Content.ReadAsByteArrayAsync());
        }
    }
}