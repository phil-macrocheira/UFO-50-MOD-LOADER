using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace UFO_50_Mod_Loader;

public static class QuestionDialog
{
    public static async Task<bool> Show(Window parent, string title, string message)
    {
        bool result = false;

        var dialog = new Window {
            Title = title,
            Width = 360,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
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
            Foreground = Brushes.White
        });

        var buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        var yesButton = new Button {
            Content = "Yes",
            Padding = new Avalonia.Thickness(19, 4, 0, 0),
            Width = 60,
            Height = 30
        };
        yesButton.Click += (s, e) => {
            result = true;
            dialog.Close();
        };

        var noButton = new Button {
            Content = "No",
            Padding = new Avalonia.Thickness(19, 4, 0, 0),
            Width = 60,
            Height = 30
        };
        noButton.Click += (s, e) => {
            result = false;
            dialog.Close();
        };

        buttonPanel.Children.Add(noButton);
        buttonPanel.Children.Add(yesButton);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(parent);
        return result;
    }
}