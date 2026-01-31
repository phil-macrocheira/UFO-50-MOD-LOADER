using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace UFO_50_Mod_Loader;

public partial class ModDownloaderWindow : Window
{
    private readonly ModDownloaderService _downloaderService;
    private readonly ObservableCollection<DownloadMod> _allMods = new();
    private readonly ObservableCollection<DownloadMod> _filteredMods = new();
    private DownloadMod? _selectedMod;

    public ModDownloaderWindow()
    {
        InitializeComponent();
        _downloaderService = new ModDownloaderService();
        ModDataGrid.ItemsSource = _filteredMods;
        ModDataGrid.SelectionChanged += OnModSelectionChanged;
        SearchBox.TextChanged += OnSearchTextChanged;
        Loaded += OnWindowLoaded;
    }

    private async void OnWindowLoaded(object? sender, EventArgs e)
    {
        await LoadModsAsync();
    }

    private async Task LoadModsAsync()
    {
        try {
            Title = "GameBanana Mod Downloader - Loading...";
            StatusText.Text = "Scanning installed mods...";
            DownloadButton.IsEnabled = false;

            // Scan installed mods first
            await DownloadedModTrackerService.ScanInstalledModsAsync();

            StatusText.Text = "Fetching mod list from GameBanana...";

            var mods = await _downloaderService.GetModListAsync();

            _allMods.Clear();
            foreach (var mod in mods.OrderByDescending(m => m.DateUpdated)) {
                _allMods.Add(new DownloadMod(mod));
            }

            ApplyFilter();

            var updateCount = _allMods.Count(m => m.HasUpdate);
            Title = "GameBanana Mod Downloader";
            StatusText.Text = $"{_allMods.Count} mods available" +
                (updateCount > 0 ? $" ({updateCount} updates)" : "");
            DownloadButton.IsEnabled = true;

            if (_filteredMods.Count > 0) {
                ModDataGrid.SelectedItem = _filteredMods[0];
            }
        }
        catch (Exception ex) {
            StatusText.Text = $"Error: {ex.Message}";
            Logger.Log($"Failed to load mods from GameBanana: {ex.Message}");
        }
    }

    private void ApplyFilter()
    {
        var searchText = SearchBox?.Text ?? "";
        _filteredMods.Clear();

        var filtered = string.IsNullOrWhiteSpace(searchText)
            ? _allMods
            : _allMods.Where(m =>
                m.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                m.Creator.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        foreach (var mod in filtered) {
            _filteredMods.Add(mod);
        }
    }

    private void OnDataGridLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is DownloadMod mod) {
            if (mod.HasUpdate) {
                e.Row.Classes.Add("hasUpdate");
            }
            else {
                e.Row.Classes.Remove("hasUpdate");
            }
        }
    }

    private async void OnModSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ModDataGrid.SelectedItem is DownloadMod mod) {
            _selectedMod = mod;
            ModTitleText.Text = mod.Name;
            ModderText.Text = $"by {mod.Creator}";
            MetadataText.Text = $"Added: {mod.DateAddedFormatted}";
            ViewPageButton.IsEnabled = !string.IsNullOrEmpty(mod.PageUrl);

            DescriptionText.Text = string.IsNullOrEmpty(mod.Description)
                ? "Loading..."
                : $"{mod.Description}\n\nLoading full description...";

            await LoadPreviewImageAsync(mod.ImageUrl);

            var fullDescription = await _downloaderService.GetFullDescriptionAsync(mod.ID);

            if (_selectedMod?.ID == mod.ID) {
                DescriptionText.Text = fullDescription;
            }
        }
    }

    private async Task LoadPreviewImageAsync(string? imageUrl)
    {
        PreviewImage.Source = null;
        if (string.IsNullOrEmpty(imageUrl)) return;

        try {
            var imageData = await _downloaderService.DownloadImageAsync(imageUrl);
            if (imageData != null) {
                using var stream = new MemoryStream(imageData);
                PreviewImage.Source = new Bitmap(stream);
            }
        }
        catch { }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void OnClearSearchClick(object? sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
    }

    private void OnSelectAllClick(object? sender, RoutedEventArgs e)
    {
        foreach (var mod in _filteredMods) {
            mod.IsSelected = true;
        }
    }

    private void OnSelectNoneClick(object? sender, RoutedEventArgs e)
    {
        foreach (var mod in _filteredMods) {
            mod.IsSelected = false;
        }
    }

    private void OnSelectUpdatesClick(object? sender, RoutedEventArgs e)
    {
        foreach (var mod in _filteredMods) {
            mod.IsSelected = mod.HasUpdate;
        }
    }

    private void OnSortByInstalledClick(object? sender, RoutedEventArgs e)
    {
        var sorted = _filteredMods
            .OrderByDescending(m => !string.IsNullOrEmpty(m.InstalledVersion))
            .ThenBy(m => m.Name)
            .ToList();

        // Rebind to clear DataGrid's internal sort state
        ModDataGrid.ItemsSource = null;
        _filteredMods.Clear();
        foreach (var mod in sorted) {
            _filteredMods.Add(mod);
        }
        ModDataGrid.ItemsSource = _filteredMods;
    }

    private void OnViewPageClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedMod != null && !string.IsNullOrEmpty(_selectedMod.PageUrl)) {
            try {
                Process.Start(new ProcessStartInfo {
                    FileName = _selectedMod.PageUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex) {
                Logger.Log($"Failed to open URL: {ex.Message}");
            }
        }
    }

    private async void OnDownloadClick(object? sender, RoutedEventArgs e)
    {
        var selectedMods = _filteredMods.Where(m => m.IsSelected).ToList();
        if (selectedMods.Count == 0) {
            Logger.Log("No mods selected for download.");
            return;
        }

        DownloadButton.IsEnabled = false;
        DownloadButton.Content = "Downloading...";

        try {
            int downloaded = 0;
            int failed = 0;

            foreach (var mod in selectedMods) {
                StatusText.Text = $"Downloading {mod.Name}...";

                try {
                    var files = await _downloaderService.GetModFilesAsync(mod.ID);
                    string logMessage = $"Downloaded {mod.Name}";

                    if (files.Count == 0) {
                        Logger.Log($"No files found for {mod.Name}");
                        failed++;
                        continue;
                    }

                    ModFile fileToDownload = files[0];

                    var modInfo = new ModInfo {
                        ID = mod.ID,
                        Name = mod.Name,
                        Version = mod.Version
                    };

                    // If this is an update, delete the old mod folder first
                    if (mod.HasUpdate) {
                        var installedMod = DownloadedModTrackerService.GetInstalledMod(mod.ID);
                        if (installedMod != null && Directory.Exists(installedMod.ModFolderPath)) {
                            Directory.Delete(installedMod.ModFolderPath, recursive: true);
                        }
                        logMessage = $"Updated {mod.Name}";
                    }

                    await _downloaderService.DownloadAndExtractModAsync(
                        fileToDownload,
                        Models.Constants.DownloadedModsPath,
                        modInfo
                    );

                    mod.InstalledVersion = mod.Version;

                    downloaded++;
                    Logger.Log(logMessage);
                }
                catch (Exception ex) {
                    Logger.Log($"Failed to download {mod.Name}: {ex.Message}");
                    failed++;
                }
            }

            // Refresh the tracker and update count
            await DownloadedModTrackerService.ScanInstalledModsAsync();
            var updateCount = _allMods.Count(m => m.HasUpdate);

            StatusText.Text = $"Downloaded {downloaded} mods" + (failed > 0 ? $" ({failed} failed)" : "");
            Logger.Log($"Downloaded {downloaded} mods" + (failed > 0 ? $" ({failed} failed)" : ""));

            // Refresh row styles
            ModDataGrid.ItemsSource = null;
            ModDataGrid.ItemsSource = _filteredMods;
        }
        finally {
            DownloadButton.IsEnabled = true;
            DownloadButton.Content = "Download Selected";
        }
    }
}