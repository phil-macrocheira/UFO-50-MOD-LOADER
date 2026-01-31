using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using UFO_50_Mod_Loader.Helpers;
using UFO_50_Mod_Loader.Services;

namespace UFO_50_Mod_Loader;

public class ModFolderItem
{
    public string Name { get; set; } = "";
    public int ModCount { get; set; }
}

public partial class ModFolderManagerWindow : Window
{
    public ModFolderManagerWindow()
    {
        InitializeComponent();
        RefreshFolderList();
        FolderListBox.SelectionChanged += OnSelectionChanged;
    }

    private void RefreshFolderList()
    {
        var items = ModFolderService.ModFolders.Select(folder => {
            int modCount = 0;
            string fullPath = ModFolderService.GetFullPath(folder);
            if (Directory.Exists(fullPath)) {
                modCount = Directory.GetDirectories(fullPath)
                    .Count(dir => CheckIfMod.Check(dir));
            }
            return new ModFolderItem { Name = folder, ModCount = modCount };
        }).ToList();

        FolderListBox.ItemsSource = items;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = FolderListBox.SelectedItem != null;
        RenameButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private void OnFolderDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (FolderListBox.SelectedItem is ModFolderItem item) {
            OpenFolderInExplorer(item.Name);
        }
    }

    private void OpenFolderInExplorer(string folder)
    {
        string fullPath = ModFolderService.GetFullPath(folder);
        if (Directory.Exists(fullPath)) {
            try {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = fullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex) {
                Logger.Log($"ERROR: Failed to open folder: {ex.Message}");
            }
        }
    }

    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new TextInputDialog("Add Mod Folder", "Enter folder name:");
        var result = await dialog.ShowDialog<string?>(this);

        if (!string.IsNullOrWhiteSpace(result)) {
            if (ModFolderService.Add(result)) {
                RefreshFolderList();
            }
        }
    }

    private async void OnRenameClick(object? sender, RoutedEventArgs e)
    {
        if (FolderListBox.SelectedItem is ModFolderItem item) {
            var dialog = new TextInputDialog("Rename Mod Folder", "Enter new name:", item.Name);
            var newName = await dialog.ShowDialog<string?>(this);

            if (!string.IsNullOrWhiteSpace(newName) && newName != item.Name) {
                if (ModFolderService.Rename(item.Name, newName)) {
                    RefreshFolderList();
                }
            }
        }
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (FolderListBox.SelectedItem is ModFolderItem item) {
            string fullPath = ModFolderService.GetFullPath(item.Name);

            // Check if folder contains mods
            if (Directory.Exists(fullPath) && item.ModCount > 0) {
                var confirmed = await QuestionDialog.Show(this,
                    "Delete Mod Folder",
                    $"The folder '{item.Name}' contains {item.ModCount} mods.\n\nAre you sure you want to delete it?");

                if (!confirmed) return;
            }

            if (ModFolderService.Remove(item.Name)) {
                // Delete the actual folder if it exists
                if (Directory.Exists(fullPath)) {
                    try {
                        Directory.Delete(fullPath, true);
                        Logger.Log($"Deleted folder: {fullPath}");
                    }
                    catch (Exception ex) {
                        Logger.Log($"ERROR: Failed to delete folder: {ex.Message}");
                    }
                }
                RefreshFolderList();
            }
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}