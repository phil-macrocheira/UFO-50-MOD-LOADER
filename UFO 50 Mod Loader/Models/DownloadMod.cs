using System.ComponentModel;

namespace UFO_50_Mod_Loader;

public class ModInfo
{
    public string ID { get; set; } = "";
    public string Name { get; set; } = "";
    public string Creator { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public long DateUpdated { get; set; }
    public long DateAdded { get; set; }
    public string Version { get; set; } = "1.0";
}

public class ModFile
{
    public string FileName { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string ID { get; set; } = "";
}

public class DownloadMod : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _installedVersion = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ID { get; }
    public string Name { get; }
    public string Creator { get; }
    public string Description { get; }
    public string ImageUrl { get; }
    public string PageUrl { get; }
    public long DateUpdated { get; }
    public long DateAdded { get; }
    public string Version { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value) {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public string InstalledVersion
    {
        get => _installedVersion;
        set
        {
            if (_installedVersion != value) {
                _installedVersion = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InstalledVersion)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUpdate)));
            }
        }
    }

    public bool HasUpdate =>
        !string.IsNullOrEmpty(InstalledVersion) &&
        InstalledVersion != Version;

    public string DateUpdatedFormatted => FormatDate(DateUpdated);
    public string DateAddedFormatted => FormatDate(DateAdded);

    public DownloadMod(ModInfo mod)
    {
        ID = mod.ID;
        Name = mod.Name;
        Creator = mod.Creator;
        Description = mod.Description;
        ImageUrl = mod.ImageUrl;
        PageUrl = mod.PageUrl;
        DateUpdated = mod.DateUpdated;
        DateAdded = mod.DateAdded;
        Version = string.IsNullOrEmpty(mod.Version) ? "1.0" : mod.Version.TrimStart('v');

        // Check for installed version
        InstalledVersion = DownloadedModTrackerService.GetInstalledVersion(ID);
    }

    private static string FormatDate(long unixTimestamp)
    {
        if (unixTimestamp == 0) return "N/A";
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).LocalDateTime;
        return dateTime.ToString("yyyy-MM-dd");
    }
}