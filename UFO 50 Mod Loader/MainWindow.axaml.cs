using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace UFO_50_Mod_Loader;

public partial class MainWindow : Window
{
    private readonly ModDatagridService _modDatagridService;
    private readonly InstalledGameService _gameService;
    public ObservableCollection<Mod> FilteredMods { get; } = new();

    public bool TemporaryInstallMode
    {
        get => SettingsService.Settings.TemporaryInstallMode;
        set => SettingsService.Settings.TemporaryInstallMode = value;
    }
    public bool SelectDownloadFile
    {
        get => SettingsService.Settings.SelectDownloadFile;
        set => SettingsService.Settings.SelectDownloadFile = value;
    }
    public MainWindow()
    {
        // Load settings first, before InitializeComponent
        SettingsService.Load();

        DataContext = this;
        InitializeComponent();

        // Set version
        Title = $"UFO 50 Mod Loader v{Constants.Version}";

        // Subscribe to log services
        LogService.OnLog += Log => {
            Dispatcher.UIThread.Post(() => {
                TextLogBox.Text = $"{Log}";
                TextLogBox.CaretIndex = TextLogBox.Text?.Length ?? 0;
            });
        };

        // Apply saved window size
        Width = SettingsService.Settings.MainWindowWidth;
        Height = SettingsService.Settings.MainWindowHeight;

        // Initialize checkbox states from settings
        TemporaryInstallModeCheckBox.IsChecked = SettingsService.Settings.TemporaryInstallMode;
        SelectDownloadFileCheckBox.IsChecked = SettingsService.Settings.SelectDownloadFile;

        // Initialize Services
        _gameService = new InstalledGameService(this);
        _modDatagridService = new ModDatagridService();

        ModDataGrid.ItemsSource = FilteredMods;
        SearchBox.TextChanged += SearchBox_TextChanged;

        Loaded += OnWindowLoaded;
    }
    private async void OnWindowLoaded(object? sender, EventArgs e)
    {
        // Check game installation
        if (!_gameService.GetGamePath()) {
            Close();
            return;
        }

        // Check in case UFO 50 Vanilla Copy folder was deleted manually
        if (!Directory.Exists(Constants.VanillaCopyPath)) {
            SettingsService.Settings.CopiedGameFiles = false;
            SettingsService.Save();
        }

        // Load hash data
        _gameService.LoadHashData();

        // Copy game if exe is recognized and game not copied before
        if (_gameService.CheckExe() && !SettingsService.Settings.CopiedGameFiles) {
            bool CanCopy = await _gameService.GetGameVersionAsync(SettingsService.Settings.GamePath);
            if (CanCopy) {
                CopyService.CopyDirectory(SettingsService.Settings.GamePath, Constants.VanillaCopyPath, ".dll", ".ini");
                SettingsService.Settings.CopiedGameFiles = true;
                SettingsService.Save();
                Logger.Log($"Copied UFO 50 files to 'UFO 50 Vanilla Copy' folder.");
            }
        }

        // Initialize datagrid service
        _modDatagridService.ModsChanged += OnModsChanged;
        _modDatagridService.Initialize();

        LoadMods();
    }
    private void OnLaunchGameClick(object? sender, RoutedEventArgs e)
    {
        try {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = "steam://run/1147860",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex) {
            Logger.Log($"Failed to launch UFO 50 via Steam: {ex.Message}");
        }
    }
    private async void OnVerifyCopyClick(object? sender, RoutedEventArgs e)
    {
        if (_gameService.CheckExe()) {
            bool CanCopy = await _gameService.GetGameVersionAsync(SettingsService.Settings.GamePath);
            if (CanCopy) {
                if (Directory.Exists(Constants.VanillaCopyPath))
                    Directory.Delete(Constants.VanillaCopyPath, recursive: true); // DELETE CURRENT VANILLA COPY
                CopyService.CopyDirectory(SettingsService.Settings.GamePath, Constants.VanillaCopyPath, ".dll", ".ini");
                SettingsService.Settings.CopiedGameFiles = true;
                SettingsService.Save();
                Logger.Log($"Copied UFO 50 files to 'UFO 50 Vanilla Copy' folder.");
            }
        }
    }
    private async void OnInstallClick(object? sender, RoutedEventArgs e)
    {
        InstallService.InstallMods();
    }
    private async void OnUninstallClick(object? sender, RoutedEventArgs e)
    {
        bool CanCopy = await _gameService.GetGameVersionAsync(Constants.VanillaCopyPath, true);
        if (CanCopy) {
            CopyService.CopyDirectory(Constants.VanillaCopyPath, SettingsService.Settings.GamePath);
            Logger.Log($"Uninstalled UFO 50 Mods.");
        }
    }
    private void OnSaveModListClick(object? sender, RoutedEventArgs e)
    {
    }
    private void OnLoadModListClick(object? sender, RoutedEventArgs e)
    {
    }
    private void OnUncheckAllClick(object? sender, RoutedEventArgs e)
    {
        foreach (var mod in FilteredMods) {
            mod.IsEnabled = false;
        }
    }
    private void OnCheckUpdateClick(object? sender, RoutedEventArgs e)
    {
    }
    private void OnGamePathMenuClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new GamePathDialog(_gameService);
        dialog.ShowDialog(this);
    }
    private void OnTemporaryInstallModeClick(object? sender, RoutedEventArgs e)
    {
        TemporaryInstallModeCheckBox.IsChecked = !TemporaryInstallModeCheckBox.IsChecked;
        SaveCheckboxSetting("Temporary Install Mode", TemporaryInstallModeCheckBox.IsChecked);
    }
    private void OnSelectDownloadFileClick(object? sender, RoutedEventArgs e)
    {
        SelectDownloadFileCheckBox.IsChecked = !SelectDownloadFileCheckBox.IsChecked;
        SaveCheckboxSetting("Select File To Download", SelectDownloadFileCheckBox.IsChecked);
    }
    private void SaveCheckboxSetting(string setting, bool? enabled)
    {
        SettingsService.Save();
        string enabledStr = enabled == true ? "enabled" : "disabled";
        Logger.Log($"{setting} {enabledStr}.");
    }
    private void OnModdingGuideClick(object? sender, RoutedEventArgs e)
    {
        ProcessStartInfo psi = new ProcessStartInfo {
            FileName = "https://ufo50.miraheze.org/wiki/Guide_to_Modding_UFO_50",
            UseShellExecute = true
        };
        Process.Start(psi);
    }
    private void OnGameBananaClick(object? sender, RoutedEventArgs e)
    {
        ProcessStartInfo psi = new ProcessStartInfo {
            FileName = "https://gamebanana.com/games/23000",
            UseShellExecute = true
        };
        Process.Start(psi);
    }
    private void OnGitHubClick(object? sender, RoutedEventArgs e)
    {
        ProcessStartInfo psi = new ProcessStartInfo {
            FileName = "https://github.com/phil-macrocheira/UFO-50-MOD-LOADER",
            UseShellExecute = true
        };
        Process.Start(psi);
    }
    private void OnDiscordClick(object? sender, RoutedEventArgs e)
    {
        ProcessStartInfo psi = new ProcessStartInfo {
            FileName = "https://discord.com/invite/eRy45VzjTs",
            UseShellExecute = true
        };
        Process.Start(psi);
    }
    private void OnClearSearchClick(object? sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
    }
    private void OnModsChanged()
    {
        // FileSystemWatcher fires on a background thread, so dispatch to UI thread
        Dispatcher.UIThread.Post(() => {
            LoadMods();
        });
    }
    private void SortByEnabled(object? sender, RoutedEventArgs e)
    {
        var sorted = FilteredMods
            .OrderByDescending(item => item.IsEnabled)
            .ToList();

        FilteredMods.Clear();
        foreach (var item in sorted) {
            FilteredMods.Add(item);
        }
    }

    private void SortByMod(object? sender, RoutedEventArgs e)
    {
        var sorted = FilteredMods
            .OrderBy(item => item.Name)
            .ToList();

        FilteredMods.Clear();
        foreach (var item in sorted) {
            FilteredMods.Add(item);
        }
    }

    private void SortByModder(object? sender, RoutedEventArgs e)
    {
        var sorted = FilteredMods
            .OrderBy(item => item.Author)
            .ToList();

        FilteredMods.Clear();
        foreach (var item in sorted) {
            FilteredMods.Add(item);
        }
    }
    private void LoadMods()
    {
        var searchText = SearchBox?.Text ?? "";
        var mods = _modDatagridService.LoadMods();
        var enabledMods = new HashSet<string>(SettingsService.Settings.EnabledMods);

        foreach (var mod in FilteredMods) {
            mod.PropertyChanged -= Mod_PropertyChanged;
        }

        FilteredMods.Clear();

        var filtered = string.IsNullOrWhiteSpace(searchText)
            ? mods
            : mods.Where(m => m.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        foreach (var mod in filtered) {
            mod.IsEnabled = enabledMods.Contains(mod.Name);
            mod.PropertyChanged += Mod_PropertyChanged;
            FilteredMods.Add(mod);
        }

        CheckConflicts();
    }
    private void Mod_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Mod.IsEnabled)) {
            SaveEnabledMods();
            CheckConflicts();
        }
    }
    private void CheckConflicts()
    {
        var enabledModPaths = FilteredMods
            .Where(m => m.IsEnabled)
            .Select(m => Path.Combine(Constants.MyModsPath, m.Name))
            .ToList();

        var result = ModConflictService.CheckConflicts(enabledModPaths);

        if (result.HasBlockingConflicts || result.HasPatchWarnings) {
            LogService.ShowConflicts(result.GetMessage());
        }
        else {
            LogService.HideConflicts();
        }
    }
    private void SaveEnabledMods()
    {
        // Get currently enabled mods from the UI
        var enabledInUI = FilteredMods
            .Where(m => m.IsEnabled)
            .Select(m => m.Name)
            .ToHashSet();

        // Keep mods that are enabled but not currently visible (filtered out by search)
        var allEnabled = SettingsService.Settings.EnabledMods
            .Where(name => !FilteredMods.Any(m => m.Name == name)) // Not in current view
            .ToHashSet();

        // Add currently visible enabled mods
        allEnabled.UnionWith(enabledInUI);

        SettingsService.Settings.EnabledMods = allEnabled.ToList();
        SettingsService.Save();
    }
    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadMods();
    }
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Save window size
        SettingsService.Settings.MainWindowWidth = Width;
        SettingsService.Settings.MainWindowHeight = Height;
        SettingsService.Save();

        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e)
    {
        _modDatagridService.Dispose();
        base.OnClosed(e);
    }
}