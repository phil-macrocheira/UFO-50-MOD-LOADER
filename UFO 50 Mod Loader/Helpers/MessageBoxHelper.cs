using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace UFO_50_Mod_Loader.Helpers;
public static class MessageBoxHelper
{
    public static async Task Show(Window parent, string title, string message)
    {
        var dialog = new Window {
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E"))
        };

        var panel = new StackPanel {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(new TextBlock {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 600,
            Foreground = Brushes.White
        });

        var button = new Button {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 60,
            Height = 30,
            Padding = new Avalonia.Thickness(19, 4, 0, 0)
        };
        button.Click += (s, e) => dialog.Close();

        panel.Children.Add(button);
        dialog.Content = panel;

        await dialog.ShowDialog(parent);
    }
}