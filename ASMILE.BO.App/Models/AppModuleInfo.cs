using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ASMILE.BO.App.Models;

public sealed partial class AppModuleInfo : ObservableObject
{
    private ImageSource? _iconImage;

    public AppModuleInfo(
        string key,
        string title,
        string iconGlyph,
        string subtitle,
        string accentHex,
        bool isPinned,
        bool showOnDesktop,
        string? iconPath = null)
    {
        Key = key;
        Title = title;
        IconGlyph = iconGlyph;
        Subtitle = subtitle;
        AccentHex = accentHex;
        IsPinned = isPinned;
        ShowOnDesktop = showOnDesktop;
        IconPath = iconPath;
    }

    public string Key { get; }
    [ObservableProperty]
    private string title;
    public string IconGlyph { get; }
    public string Subtitle { get; }
    public string AccentHex { get; }
    public string? IconPath { get; }

    public SolidColorBrush AccentBrush => (SolidColorBrush)new BrushConverter().ConvertFromString(AccentHex)!;

    public ImageSource? IconImage
    {
        get
        {
            if (_iconImage is not null || string.IsNullOrWhiteSpace(IconPath))
            {
                return _iconImage;
            }

            if (!File.Exists(IconPath))
            {
                return null;
            }

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(IconPath, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            _iconImage = bmp;
            return _iconImage;
        }
    }

    [ObservableProperty]
    private bool isPinned;

    [ObservableProperty]
    private bool showOnDesktop;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private bool isActiveOnTaskbar;

    [ObservableProperty]
    private bool isMinimizedOnTaskbar;

    [ObservableProperty]
    private double desktopLeft;

    [ObservableProperty]
    private double desktopTop;
}
