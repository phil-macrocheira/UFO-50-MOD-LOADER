using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UFO_50_Mod_Loader;

public partial class GamePathDialog : Window
{
    private readonly InstalledGameService _gameService;

    public GamePathDialog(InstalledGameService gameService)
    {
        InitializeComponent();
        _gameService = gameService;

        // Load current path
        PathTextBox.Text = SettingsService.Settings.GamePath ?? "";
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var newPath = PathTextBox.Text ?? "";

        if (_gameService.IsValidGamePath(newPath)) {
            SettingsService.Settings.GamePath = newPath;
            SettingsService.Save();
            Logger.Log($"UFO 50 install path updated to: {newPath}");
            Close();
        }
        else {
            Logger.Log("Path entered was not a UFO 50 install path. It must contain ufo50.exe and data.win.");
        }
    }
}