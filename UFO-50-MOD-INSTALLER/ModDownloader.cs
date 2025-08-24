using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Text;

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
        private static readonly HttpClient client = new HttpClient();
        public async Task DownloadMods(string downloadPath, List<ModFile> filesToDownload, string? localModInfoPath, Dictionary<string, ModInfo> fileToModInfoMap) {
            var localMods = new Dictionary<string, LocalModInfo>();
            if (!string.IsNullOrEmpty(localModInfoPath)) {
                if (File.Exists(localModInfoPath)) {
                    var json = File.ReadAllText(localModInfoPath);
                    localMods = JsonSerializer.Deserialize<Dictionary<string, LocalModInfo>>(json) ?? new Dictionary<string, LocalModInfo>();
                }
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
            catch (Exception ex) {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
            finally {
                if (!string.IsNullOrEmpty(localModInfoPath)) {
                    var updatedJson = JsonSerializer.Serialize(localMods, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(localModInfoPath, updatedJson);
                }
            }
        }
        public static ModInfo? FindBestModMatch(List<ModInfo> allMods, string titleToMatch, string creatorToMatch) {
            var exactMatches = allMods.Where(m => m.Name.Equals(titleToMatch, StringComparison.OrdinalIgnoreCase)).ToList();

            if (exactMatches.Count == 1) {
                return exactMatches[0];
            }
            if (exactMatches.Count > 1) {
                var creatorMatch = exactMatches
                    .FirstOrDefault(m => m.Creator.Equals(creatorToMatch, StringComparison.OrdinalIgnoreCase));
                if (creatorMatch != null) return creatorMatch;
            }
            return null;
        }
        private static async Task<JsonDocument> GetJson(string url) {
            if (!client.DefaultRequestHeaders.UserAgent.Any()) {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("UFO50ModInstaller/1.0");
            }

            HttpResponseMessage response;
            try {
                response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e) {
                throw new Exception($"Failed to connect to Gamebanana at {url}. Message: {e.Message}", e);
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json);
        }
        public static async Task<List<ModInfo>> GetModInfo(string game_id) {
            var page = 1;
            var mods = new ConcurrentBag<ModInfo>();
            var throttler = new SemaphoreSlim(10); // Limit concurrent requests
            var tasks = new List<Task>();

            while (true) {
                string url = $"https://gamebanana.com/apiv11/Game/{game_id}/Subfeed?_nPage={page}&_sSort=default";
                using JsonDocument json = await GetJson(url);
                var records = json.RootElement.GetProperty("_aRecords");
                if (records.GetArrayLength() == 0)
                    break;

                foreach (var element in records.EnumerateArray()) {
                    var elementCopy = JsonDocument.Parse(element.GetRawText()).RootElement;
                    tasks.Add(Task.Run(async () => {
                        await throttler.WaitAsync();
                        try {
                            if (!elementCopy.TryGetProperty("_sModelName", out var modelNameElement) ||
                                modelNameElement.GetString() != "Mod")
                                return;

                            elementCopy.TryGetProperty("_idRow", out var idElement);
                            string id = idElement.ToString();
                            elementCopy.TryGetProperty("_sName", out var nameElement);
                            elementCopy.TryGetProperty("_sProfileUrl", out var pageUrlElement);
                            elementCopy.TryGetProperty("_sText", out var descElement);
                            elementCopy.TryGetProperty("_sVersion", out var versionElement);
                            string version = versionElement.ValueKind != JsonValueKind.Undefined ? versionElement.GetString() ?? "1.0" : "1.0";

                            long dateUpdated = 0;
                            if (elementCopy.TryGetProperty("_tsDateUpdated", out var dateUpdatedElement)) {
                                dateUpdated = dateUpdatedElement.TryGetInt64(out var val) ? val : 0;
                            }

                            long dateAdded = 0;
                            if (elementCopy.TryGetProperty("_tsDateAdded", out var dateAddedElement)) {
                                dateAdded = dateAddedElement.TryGetInt64(out var val) ? val : 0;
                            }

                            int views = 0;
                            if (elementCopy.TryGetProperty("_nViewCount", out var viewsElement)) {
                                views = viewsElement.TryGetInt32(out var val) ? val : 0;
                            }

                            int likes = 0;
                            if (elementCopy.TryGetProperty("_nLikeCount", out var likesElement)) {
                                likes = likesElement.TryGetInt32(out var val) ? val : 0;
                            }

                            string mod_url = $"https://gamebanana.com/apiv11/Mod/{id}?_csvProperties=@gbprofile";
                            using JsonDocument mod_url_json = await GetJson(mod_url);
                            string creatorName = "-";
                            bool createdBySubmitter = false;
                            if (mod_url_json.RootElement.TryGetProperty("_bCreatedBySubmitter", out var createdBySubmitterProp)) {
                                createdBySubmitter = createdBySubmitterProp.GetBoolean();
                            }

                            if (createdBySubmitter) {
                                if (elementCopy.TryGetProperty("_aSubmitter", out var submitter) &&
                                    submitter.TryGetProperty("_sName", out var submitterName)) {
                                    creatorName = submitterName.GetString() ?? "-";
                                }
                            }
                            else {
                                if (mod_url_json.RootElement.TryGetProperty("_aCredits", out var credits) &&
                                    credits.ValueKind == JsonValueKind.Object &&
                                    credits.TryGetProperty("Creator", out var creatorArray) &&
                                    creatorArray.ValueKind == JsonValueKind.Array &&
                                    creatorArray.GetArrayLength() > 0) {

                                    var innerArray = creatorArray[0];
                                    if (innerArray.ValueKind == JsonValueKind.Array &&
                                        innerArray.GetArrayLength() > 0 &&
                                        innerArray[0].ValueKind == JsonValueKind.String) {
                                        creatorName = innerArray[0].GetString() ?? "-";
                                    }
                                }
                            }

                            string imageUrl = "";
                            if (elementCopy.TryGetProperty("_aPreviewMedia", out var media) &&
                                media.TryGetProperty("_aImages", out var imgs) &&
                                imgs.ValueKind == JsonValueKind.Array &&
                                imgs.GetArrayLength() > 0) {
                                if (imgs[0].TryGetProperty("_sBaseUrl", out var bu) &&
                                    imgs[0].TryGetProperty("_sFile", out var f)) {
                                    imageUrl = $"{bu.GetString()}/{f.GetString()}";
                                }
                            }

                            mods.Add(new ModInfo
                            {
                                Id = id,
                                Name = nameElement.GetString() ?? "",
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
                        catch (Exception ex) {
                            Console.WriteLine($"Error processing mod {elementCopy}: {ex.Message}");
                        }
                        finally {
                            throttler.Release();
                        }
                    }));
                }

                page++;
            }

            await Task.WhenAll(tasks);
            return mods.ToList();
        }
        public static async Task<string> GetModFullDescription(string modId) {
            if (string.IsNullOrEmpty(modId)) return "No description available.";

            try {
                if (!client.DefaultRequestHeaders.UserAgent.Any()) {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("UFO50ModInstaller/1.0");
                }

                // Use the ProfilePage endpoint to get the full description
                var url = $"https://gamebanana.com/apiv11/Mod/{modId}/ProfilePage";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);

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

                    // 2. Replace <br> with newline
                    string plainText = Regex.Replace(decodedText, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

                    // 3. Replace </li> with newline
                    plainText = plainText.Replace("</li>", "\n");

                    // 4. Replace closing header tags </h1> to </h6> with newline
                    plainText = Regex.Replace(plainText, @"</h[1-6]>", "\n", RegexOptions.IgnoreCase);

                    // 5. Use a regular expression to strip out all remaining HTML tags
                    plainText = Regex.Replace(plainText, "<.*?>", String.Empty);

                    return plainText.Trim();
                }
            }
            catch (Exception ex) {
                return $"Could not load full description. Error: {ex.Message}";
            }

            return "Description not found.";
        }
        public static async Task<List<ModFile>> GetModFileInfo(string modId) {
            var files = new List<ModFile>();
            string url = $"https://gamebanana.com/apiv11/Mod/{modId}/DownloadPage";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);
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
            var response = await client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();
            await File.WriteAllBytesAsync(filePath, await response.Content.ReadAsByteArrayAsync());
        }
    }
}