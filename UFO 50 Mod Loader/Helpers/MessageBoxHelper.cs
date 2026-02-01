using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Utilities;

namespace UFO_50_Mod_Loader.Helpers;
public static class MessageBoxHelper
{
    public static async Task<bool> Show(Window parent, string title, string message, string yesText = "OK", string? noText = null)
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

        var buttonPanel = new StackPanel()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 16
        };
        panel.Children.Add(buttonPanel);

        Button AddButton(string text)
        {
            var button = new Button
            {
                Content = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Height = 30,
                Padding = new Avalonia.Thickness(19, 4, 19, 0)
            };

            button.Click += (s, e) => dialog.Close();
            buttonPanel.Children.Add(button);

            return button;
        }

        var result = false;

        AddButton(yesText).Click += (s, e) => result = true;
        if (noText != null)
        {
            AddButton(noText);
        }

        dialog.Content = panel;

        await dialog.ShowDialog(parent);

        return result;
    }
}