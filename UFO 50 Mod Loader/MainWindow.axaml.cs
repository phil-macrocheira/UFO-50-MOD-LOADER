using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GMLoader;
using System.Collections.ObjectModel;
using System.Diagnostics;
using UFO_50_Mod_Loader.Helpers;
using UFO_50_Mod_Loader.Models;

namespace UFO_50_Mod_Loader;

public partial class MainWindow : Window
{
    private readonly ModDatagridService _modDatagridService;
    private readonly InstalledGameService _gameService;
    public static ObservableCollection<Mod> FilteredMods { get; } = new();
    private bool _isInstalling = false;

    public bool OverwriteMode
    {
        get => SettingsService.Settings.OverwriteMode;
        set => SettingsService.Settings.OverwriteMode = value;
    }
    public bool CheckForUpdatesAutomatically
    {
        get => SettingsService.Settings.CheckForUpdatesAutomatically;
        set => SettingsService.Settings.CheckForUpdatesAutomatically = value;
    }
    /*
    public bool EnabledTop
    {
        get => SettingsService.Settings.EnabledTop;
        set => SettingsService.Settings.EnabledTop = value;
    }
    */
    public MainWindow()
    {
        DataContext = this;
        InitializeComponent();

        // Set version
        Title = $"UFO 50 Mod Loader v{Constants.Version}";

        // Subscribe to log services
        LogService.OnLog += Log => {
            Dispatcher.UIThread.Post(() => {
                TextLogBox.Text = $"{Log}";
                TextLogBox.CaretIndex = TextLogBox.Text?.Length ?? 0;
            }, DispatcherPriority.Render);
        };

        // Apply saved window size
        Width = SettingsService.Settings.MainWindowWidth;
        Height = SettingsService.Settings.MainWindowHeight;
        ContentGrid.RowDefinitions[2].Height = new GridLength(SettingsService.Settings.TextBoxHeight);

        // Initialize checkbox states from settings
        OverwriteModeCheckBox.IsChecked = SettingsService.Settings.OverwriteMode;

        // Initialize Services
        _gameService = new InstalledGameService(this);
        _modDatagridService = new ModDatagridService();

        ModDataGrid.ItemsSource = FilteredMods;
        SearchBox.TextChanged += SearchBox_TextChanged;

        Loaded += OnWindowLoaded;
    }
    private void ToggleUI(bool state)
    {
        InstallButton.IsEnabled = state;
        DownloadModsButton.IsEnabled = state;
        ModDataGrid.IsEnabled = state;
        SearchBox.IsEnabled = state;
        MenuBar.IsEnabled = state;
    }
    private async void OnWindowLoaded(object? sender, EventArgs e)
    {
        // Set Install Button Text on open
        if (OverwriteModeCheckBox.IsChecked == true) {
            InstallButton.Content = "Install Mods";
        }

        // Check game installation (repeats until valid path supplied)
        if (!await _gameService.GetGamePath()) {
            Close();
            return;
        }

        // Check in case UFO 50 Vanilla Copy folder was deleted manually
        if (!Directory.Exists(Constants.VanillaCopyPath)) {
            SettingsService.Settings.CopiedGameFiles = false;
            SettingsService.Save();
        }

        // Load hash data
        bool LoadHashDataSuccess = _gameService.LoadHashData();
        if (!LoadHashDataSuccess) {
            Logger.Log($"[ERROR] ufo50_hashes.json file not found! Cannot verify UFO 50 version!");
        }
        else {
            // Copy game if exe is recognized and game not copied before
            if (_gameService.CheckExe() && !SettingsService.Settings.CopiedGameFiles) {
                CopyUFO50Vanilla();
            }

            // Initialize datagrid service
            _modDatagridService.ModsChanged += OnModsChanged;
            _modDatagridService.Initialize();

            LoadMods();

            if (SettingsService.Settings.FirstTimeRun == true)
                FirstTimeRun();
        }

        if (SettingsService.Settings.CheckForUpdatesAutomatically)
        {
            SelfUpdaterService.CheckForUpdates(this, automatic: true);
        }
    }
    private async void OnLaunchGameClick(object? sender, RoutedEventArgs e)
    {
        await LaunchGameService.LaunchGameAsync();
    }
    private async void OnVerifyCopyClick(object? sender, RoutedEventArgs e)
    {
        if (_gameService.CheckExe()) {
            CopyUFO50Vanilla();
        }
    }
    private async Task CopyUFO50Vanilla()
    {
        string version = await _gameService.GetGameVersionAsync(SettingsService.Settings.GamePath);
        HashSet<string> versionFileSet = _gameService.GetFileList(version);
        versionFileSet.Add("Steamworks_x64.dll");
        versionFileSet.Add("steam_api64.dll");
        versionFileSet.Add("options.ini");

        if (CanCopy(version)) {
            if (Directory.Exists(Constants.VanillaCopyPath))
                Directory.Delete(Constants.VanillaCopyPath, recursive: true);
            await CopyService.CopyDirectoryAsync(SettingsService.Settings.GamePath, Constants.VanillaCopyPath, versionFileSet);
            DeleteEmptyDirectories(Constants.VanillaCopyPath);
            SettingsService.Settings.CopiedGameFiles = true;
            SettingsService.Save();
            Logger.Log($"Copied UFO 50 files to 'UFO 50 Vanilla Copy' folder.");
        }
    }
    private void DeleteEmptyDirectories(string path)
    {
        foreach (var dir in Directory.GetDirectories(path)) {
            DeleteEmptyDirectories(dir);

            if (!Directory.EnumerateFileSystemEntries(dir).Any()) {
                Directory.Delete(dir);
            }
        }
    }
    private bool CanCopy(string version)
    {
        if (version == "Modded" || version == "Mismatched Vanilla")
            return false;
        return true;
    }
    private async void OnInstallClick(object? sender, RoutedEventArgs e)
    {
        if (_isInstalling) return;
        _isInstalling = true;

        ToggleUI(false);

        SearchBox.TextChanged -= SearchBox_TextChanged;
        SearchBox.Text = "";
        SearchBox.TextChanged += SearchBox_TextChanged;

        LoadMods();

        bool installedSuccessfully = false;

        if (SettingsService.Settings.OverwriteMode) {
            InstallButton.Content = "Installing Mods...";
            Logger.Log("Installing mods...");
        }
        else {
            InstallButton.Content = "Loading Mods...";
            Logger.Log("Loading mods...");
        }

        try {
            GMLoaderResult? result = await Task.Run(() => InstallService.InstallModsAsync(this));
            installedSuccessfully = result?.Success ?? false;

            if (result is not null && !result.Success) {
                await MessageBoxHelper.Show(this, "Mod installation error", "Mod installation failed" + (result.ErrorMessage != null ? $": {result.ErrorMessage}" : "."));
            }
        }
        finally {
            if (OverwriteModeCheckBox.IsChecked == false)
                InstallButton.Content = "Load Mods and Launch Game";
            else
                InstallButton.Content = "Install Mods";

            ToggleUI(true);
            _isInstalling = false;

            SortByEnabled();

            if (!SettingsService.Settings.OverwriteMode && installedSuccessfully == true) {
                await LaunchGameService.LaunchGameAsync();
            }
        }
    }
    private async void OnDownloadModsClick(object? sender, RoutedEventArgs e)
    {
        _modDatagridService.PauseWatchers();

        var dialog = new ModDownloaderWindow();
        await dialog.ShowDialog(this);

        _modDatagridService.ResumeWatchers();
        LoadMods();
    }
    private async void OnUninstallClick(object? sender, RoutedEventArgs e)
    {
        string version = await _gameService.GetGameVersionAsync(Constants.VanillaCopyPath, true);

        if (CanCopy(version)) {
            CopyService.CopyDirectory(Constants.VanillaCopyPath, SettingsService.Settings.GamePath);
            Logger.Log($"Uninstalled UFO 50 Mods.");
        }
    }
    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        if (Directory.Exists(Constants.ModLoaderRoot)) {
            try {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = Constants.ModLoaderRoot,
                    UseShellExecute = true
                });
            }
            catch (Exception ex) {
                Logger.Log($"ERROR: Failed to open folder: {ex.Message}");
            }
        }
    }
    private async void OnSaveModListClick(object? sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
        LoadMods();

        await ModListWindow.SaveModListAsync(this);

        SortByEnabled();
    }
    private void OnLoadModListClick(object? sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";

        var dialog = new ModListWindow();
        dialog.ModListLoaded += () => {
            LoadMods();
            SortByEnabled();
        };
        dialog.ShowDialog(this);
    }
    private void OnUncheckAllClick(object? sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";

        foreach (var mod in FilteredMods) {
            if (mod.Name == "UFO 50 Modding Settings")
                continue;
            mod.IsEnabled = false;
        }
    }
    private void OnCheckUpdateClick(object? sender, RoutedEventArgs e)
    {
        SelfUpdaterService.CheckForUpdates(this);
    }
    private void OnGamePathMenuClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new GamePathDialog(_gameService);
        dialog.ShowDialog(this);
    }
    /*
    private void EnabledTopClick(object? sender, RoutedEventArgs e)
    {
        EnabledTopCheckBox.IsChecked = !EnabledTopCheckBox.IsChecked;
        SaveCheckboxSetting("Keep Enabled Mods On Top", EnabledTopCheckBox.IsChecked);
        if (EnabledTopCheckBox.IsChecked == true)
            SortByEnabled();
    }
    */
    private void OnOverwriteModeClick(object? sender, RoutedEventArgs e)
    {
        OverwriteModeCheckBox.IsChecked = !OverwriteModeCheckBox.IsChecked;
        SettingsService.Settings.OverwriteMode = OverwriteModeCheckBox.IsChecked ?? false;

        if (OverwriteModeCheckBox.IsChecked == false) {
            InstallButton.Content = "Load Mods and Launch Game";
        }
        else {
            InstallButton.Content = "Install Mods";
        }
        SaveCheckboxSetting("Overwrite Installed Files", OverwriteModeCheckBox.IsChecked);
    }
    private void OnCheckForUpdatesAutomaticallyClick(object? sender, RoutedEventArgs e)
    {
        CheckForUpdatesAutomaticallyCheckBox.IsChecked = !CheckForUpdatesAutomaticallyCheckBox.IsChecked;
        SettingsService.Settings.CheckForUpdatesAutomatically = CheckForUpdatesAutomaticallyCheckBox.IsChecked ?? true;
        SaveCheckboxSetting("Check For Updates Automatically", CheckForUpdatesAutomaticallyCheckBox.IsChecked);
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
    private void SortByEnabledClick(object? sender, RoutedEventArgs e)
    {
        SortByEnabled();
    }
    private void SortByEnabled()
    {
        var sorted = FilteredMods.OrderByDescending(item => item.IsEnabled).ToList();
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
    private void FirstTimeRun()
    {
        var UFO50ModdingSettingsMod = FilteredMods.FirstOrDefault(m => m.Name == "UFO 50 Modding Settings");
        if (UFO50ModdingSettingsMod != null) {
            UFO50ModdingSettingsMod.IsEnabled = true;
            SaveEnabledMods();
        }

        SettingsService.Settings.FirstTimeRun = false;
        SettingsService.Save();
    }
    private void LoadMods()
    {
        var searchText = SearchBox?.Text ?? "";
        var mods = _modDatagridService.LoadMods();
        var enabledMods = new HashSet<string>(SettingsService.Settings.SelectedMods);

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

        //if (SettingsService.Settings.EnabledTop)
        //    SortByEnabled();

        CheckConflicts();
        UpdateVersionColumnVisibility();
    }
    private void Mod_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        //if (SettingsService.Settings.EnabledTop)
        //    SortByEnabled();
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
            .Where(path => path != null)
            .Cast<string>()
            .ToList();

        var result = ModConflictService.CheckConflicts(enabledModPaths);

        if (result.HasBlockingConflicts || result.HasPatchWarnings) {
            LogService.ShowConflicts(result.GetMessage());
        }
        else {
            LogService.HideConflicts();
        }

        if (result.HasBlockingConflicts) {
            InstallButton.IsEnabled = false;
        }
        else {
            InstallButton.IsEnabled = true;
        }
    }
    private void UpdateVersionColumnVisibility()
    {
        // Hide the Version column if no mods have version data
        bool anyModHasVersion = FilteredMods.Any(m => !string.IsNullOrEmpty(m.ModVersion) && m.Name != "UFO 50 Modding Settings");
        ModDataGrid.Columns[4].IsVisible = anyModHasVersion;
    }
    private void SaveEnabledMods()
    {
        // Get currently enabled mods from the UI
        var enabledInUI = FilteredMods
            .Where(m => m.IsEnabled)
            .Select(m => m.Name)
            .ToHashSet();

        // Keep mods that are enabled but not currently visible (filtered out by search)
        var allEnabled = SettingsService.Settings.SelectedMods
            .Where(name => !FilteredMods.Any(m => m.Name == name)) // Not in current view
            .ToHashSet();

        // Add currently visible enabled mods
        allEnabled.UnionWith(enabledInUI);

        SettingsService.Settings.SelectedMods = allEnabled.ToList();
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
        SettingsService.Settings.TextBoxHeight = ContentGrid.RowDefinitions[2].Height.Value;

        SettingsService.Save();

        Logger.SaveLogToFile();

        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e)
    {
        _modDatagridService.Dispose();
        base.OnClosed(e);
    }
}