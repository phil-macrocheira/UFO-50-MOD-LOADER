using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using UFO_50_Mod_Loader.Models;
using UFO_50_Mod_Loader.Services;

namespace UFO_50_Mod_Loader;

public class ModDownloaderService
{
    private static readonly HttpClient _client = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    static ModDownloaderService()
    {
        if (!_client.DefaultRequestHeaders.UserAgent.Any()) {
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("UFO50ModLoader/1.0");
        }
    }
    public async Task<List<ModInfo>> GetModListAsync()
    {
        var mods = new ConcurrentBag<ModInfo>();
        var throttler = new SemaphoreSlim(10);
        var tasks = new List<Task>();
        var page = 1;

        while (true) {
            var url = $"https://gamebanana.com/apiv11/Game/{Constants.GameBananaID}/Subfeed?_nPage={page}&_sSort=default";
            using var json = await GetJsonAsync(url);
            var records = json.RootElement.GetProperty("_aRecords");

            if (records.GetArrayLength() == 0) break;

            foreach (var element in records.EnumerateArray()) {
                var elementCopy = JsonDocument.Parse(element.GetRawText()).RootElement;

                tasks.Add(Task.Run(async () => {
                    await throttler.WaitAsync();
                    try {
                        var mod = await ParseModElementAsync(elementCopy);
                        if (mod != null) mods.Add(mod);
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
    private async Task<ModInfo?> ParseModElementAsync(JsonElement element)
    {
        try {
            if (!element.TryGetProperty("_sModelName", out var modelName) ||
                modelName.GetString() != "Mod")
                return null;

            var id = element.GetProperty("_idRow").ToString();
            var name = GetStringProperty(element, "_sName");
            var pageUrl = GetStringProperty(element, "_sProfileUrl");
            var description = GetStringProperty(element, "_sText");
            var version = GetStringProperty(element, "_sVersion", "1.0");
            var dateUpdated = GetLongProperty(element, "_tsDateUpdated");
            var dateAdded = GetLongProperty(element, "_tsDateAdded");

            var creator = await GetCreatorNameAsync(id, element);
            var imageUrl = GetPreviewImageUrl(element);

            return new ModInfo {
                ID = id,
                Name = name,
                PageUrl = pageUrl,
                Creator = creator,
                Description = description,
                ImageUrl = imageUrl,
                DateUpdated = dateUpdated,
                DateAdded = dateAdded,
                Version = version
            };
        }
        catch {
            return null;
        }
    }
    private async Task<string> GetCreatorNameAsync(string modId, JsonElement element)
    {
        try {
            var url = $"https://gamebanana.com/apiv11/Mod/{modId}?_csvProperties=@gbprofile";
            using var json = await GetJsonAsync(url);

            var createdBySubmitter = json.RootElement.TryGetProperty("_bCreatedBySubmitter", out var prop)
                && prop.GetBoolean();

            if (createdBySubmitter) {
                if (element.TryGetProperty("_aSubmitter", out var submitter) &&
                    submitter.TryGetProperty("_sName", out var name)) {
                    return name.GetString() ?? "-";
                }
            }
            else {
                if (json.RootElement.TryGetProperty("_aCredits", out var credits) &&
                    credits.ValueKind == JsonValueKind.Object &&
                    credits.TryGetProperty("Creator", out var creatorArray) &&
                    creatorArray.ValueKind == JsonValueKind.Array &&
                    creatorArray.GetArrayLength() > 0) {

                    var innerArray = creatorArray[0];
                    if (innerArray.ValueKind == JsonValueKind.Array &&
                        innerArray.GetArrayLength() > 0 &&
                        innerArray[0].ValueKind == JsonValueKind.String) {
                        return innerArray[0].GetString() ?? "-";
                    }
                }
            }
        }
        catch { }

        return "-";
    }
    private string GetPreviewImageUrl(JsonElement element)
    {
        try {
            if (element.TryGetProperty("_aPreviewMedia", out var media) &&
                media.TryGetProperty("_aImages", out var imgs) &&
                imgs.ValueKind == JsonValueKind.Array &&
                imgs.GetArrayLength() > 0 &&
                imgs[0].TryGetProperty("_sBaseUrl", out var baseUrl) &&
                imgs[0].TryGetProperty("_sFile", out var file)) {
                return $"{baseUrl.GetString()}/{file.GetString()}";
            }
        }
        catch { }

        return "";
    }
    public async Task<string> GetFullDescriptionAsync(string modId)
    {
        if (string.IsNullOrEmpty(modId)) return "No description available.";

        try {
            var url = $"https://gamebanana.com/apiv11/Mod/{modId}/ProfilePage";
            using var json = await GetJsonAsync(url);

            if (json.RootElement.TryGetProperty("_sText", out var textElement)) {
                string rawHtml = textElement.ValueKind == JsonValueKind.Array && textElement.GetArrayLength() > 0
                    ? textElement[0].GetString() ?? ""
                    : textElement.GetString() ?? "";

                return CleanHtml(rawHtml);
            }
        }
        catch (Exception ex) {
            return $"Could not load description: {ex.Message}";
        }

        return "No description available.";
    }
    public async Task<List<ModFile>> GetModFilesAsync(string modID)
    {
        var files = new List<ModFile>();
        var url = $"https://gamebanana.com/apiv11/Mod/{modID}/DownloadPage";

        using var json = await GetJsonAsync(url);

        foreach (var fileElement in json.RootElement.GetProperty("_aFiles").EnumerateArray()) {
            files.Add(new ModFile {
                FileName = fileElement.GetProperty("_sFile").GetString() ?? "unknown.zip",
                DownloadUrl = fileElement.GetProperty("_sDownloadUrl").GetString() ?? "",
                ID = modID
            });
        }

        return files;
    }
    public async Task<byte[]?> DownloadImageAsync(string url)
    {
        try {
            return await _client.GetByteArrayAsync(url);
        }
        catch {
            return null;
        }
    }
    public async Task DownloadAndExtractModAsync(ModFile file, string destinationFolder, ModInfo modInfo)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), file.FileName);

        try {
            var response = await _client.GetAsync(file.DownloadUrl);
            response.EnsureSuccessStatusCode();
            await File.WriteAllBytesAsync(tempPath, await response.Content.ReadAsByteArrayAsync());

            var archiveInDest = Path.Combine(destinationFolder, file.FileName);
            File.Move(tempPath, archiveInDest, overwrite: true);

            var extractedFolder = await ExtractService.ExtractAsync(archiveInDest);

            // Write metadata JSON to extracted folder
            if (extractedFolder != null) {
                await WriteMetadataAsync(extractedFolder, modInfo);
            }
        }
        finally {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
    private async Task WriteMetadataAsync(string modFolder, ModInfo modInfo)
    {
        var metadata = new GameBananaMetadata {
            ID = modInfo.ID,
            Name = modInfo.Name,
            Version = modInfo.Version
        };

        var jsonPath = Path.Combine(modFolder, GameBananaMetadata.FileName);
        var json = JsonSerializer.Serialize(metadata, _jsonOptions);
        await File.WriteAllTextAsync(jsonPath, json);
    }
    private async Task<JsonDocument> GetJsonAsync(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }
    private static string GetStringProperty(JsonElement element, string name, string defaultValue = "")
    {
        return element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? defaultValue
            : defaultValue;
    }
    private static long GetLongProperty(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var prop) && prop.TryGetInt64(out var val) ? val : 0;
    }
    private static string CleanHtml(string html)
    {
        var text = WebUtility.HtmlDecode(html);
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = text.Replace("</li>", "\n");
        text = Regex.Replace(text, @"</h[1-6]>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<.*?>", string.Empty);
        return text.Trim();
    }
}