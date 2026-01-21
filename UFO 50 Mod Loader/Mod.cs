using Avalonia.Media.Imaging;
using System.ComponentModel;

namespace UFO_50_Mod_Loader;
public class Mod : INotifyPropertyChanged
{
    private bool _isEnabled;
    private Bitmap? _icon;
    private string _name = "";
    private string _author = "";
    private string _description = "";

    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
    }

    public Bitmap? Icon
    {
        get => _icon;
        set { _icon = value; OnPropertyChanged(nameof(Icon)); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public string Author
    {
        get => _author;
        set { _author = value; OnPropertyChanged(nameof(Author)); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(nameof(Description)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}