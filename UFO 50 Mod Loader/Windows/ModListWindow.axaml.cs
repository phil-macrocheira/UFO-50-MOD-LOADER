using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace UFO_50_Mod_Loader;

public class ModListItem
{
    public string Name { get; set; } = "";
    public int ModCount { get; set; }
    public List<string> Mods { get; set; } = new();
}

public partial class ModListWindow : Window
{
    public event Action? ModListLoaded;

    public ModListWindow()
    {
        InitializeComponent();
        RefreshModLists();
        ModListsListBox.SelectionChanged += OnSelectionChanged;
    }

    private void RefreshModLists()
    {
        var items = SettingsService.Settings.ModLists
            .Select(kvp => new ModListItem {
                Name = kvp.Key,
                ModCount = kvp.Value.Count,
                Mods = kvp.Value
            })
            .ToList();
        ModListsListBox.ItemsSource = items;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = ModListsListBox.SelectedItem != null;
        LoadButton.IsEnabled = hasSelection;
        RenameButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private void OnListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ModListsListBox.SelectedItem != null)
            LoadSelectedModList();
    }

    private void OnLoadClick(object? sender, RoutedEventArgs e)
    {
        LoadSelectedModList();
    }

    private void LoadSelectedModList()
    {
        if (ModListsListBox.SelectedItem is ModListItem selected) {
            var modListName = selected.Name;
            var modsToEnable = selected.Mods;

            // Disable all mods first
            foreach (var mod in MainWindow.FilteredMods) {
                mod.IsEnabled = false;
            }

            // Enable mods from the list
            var notFound = new List<string>();
            foreach (var modName in modsToEnable) {
                var mod = MainWindow.FilteredMods.FirstOrDefault(m => m.Name == modName);
                if (mod != null) {
                    mod.IsEnabled = true;
                }
                else {
                    notFound.Add(modName);
                }
            }

            // Update settings
            SettingsService.Settings.SelectedMods = modsToEnable.ToList();
            SettingsService.Save();

            // Log results
            Logger.Log($"Loaded {modListName}");
            if (notFound.Count > 0) {
                Logger.Log($"Mods in {modListName} missing: {string.Join(", ", notFound)}");
            }

            ModListLoaded?.Invoke();
            Close();
        }
    }

    private async void OnRenameClick(object? sender, RoutedEventArgs e)
    {
        if (ModListsListBox.SelectedItem is ModListItem selected) {
            var dialog = new TextInputDialog("Rename Mod List", "Enter new name:", selected.Name);
            var newName = await dialog.ShowDialog<string?>(this);

            if (!string.IsNullOrWhiteSpace(newName) && newName != selected.Name) {
                if (SettingsService.Settings.ModLists.ContainsKey(newName)) {
                    Logger.Log($"A mod list named '{newName}' already exists");
                    return;
                }

                var mods = selected.Mods;
                SettingsService.Settings.ModLists.Remove(selected.Name);
                SettingsService.Settings.ModLists[newName] = mods;
                SettingsService.Save();

                Logger.Log($"Renamed mod list '{selected.Name}' to '{newName}'");
                RefreshModLists();
            }
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (ModListsListBox.SelectedItem is ModListItem selected) {
            SettingsService.Settings.ModLists.Remove(selected.Name);
            SettingsService.Save();
            Logger.Log($"Deleted {selected.Name}");
            RefreshModLists();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public static async Task SaveModListAsync(Window owner)
    {
        var modLists = SettingsService.Settings.ModLists;
        int modListCount = modLists.Count + 1;
        string defaultName = $"Mod List {modListCount}";

        var dialog = new TextInputDialog("Save Mod List", "Enter mod list name:", defaultName);
        var modListName = await dialog.ShowDialog<string?>(owner);

        if (!string.IsNullOrWhiteSpace(modListName)) {
            var enabledMods = SettingsService.Settings.SelectedMods.ToList();

            if (modLists.ContainsKey(modListName)) {
                modLists[modListName] = enabledMods;
                Logger.Log($"Overwrote mod list {modListName}");
            }
            else {
                modLists[modListName] = enabledMods;
                Logger.Log($"Saved new mod list {modListName}");
            }

            SettingsService.Settings.ModLists = modLists;
            SettingsService.Save();
        }
    }
}