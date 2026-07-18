using System.Collections.ObjectModel;

namespace ASMILE.BO.App.Models;

public sealed class SettingsPageViewModel
{
    public SettingsPageViewModel(string key, string title, string subtitle, string iconGlyph)
    {
        Key = key;
        Title = title;
        Subtitle = subtitle;
        IconGlyph = iconGlyph;
    }

    public string Key { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string IconGlyph { get; }
    public ObservableCollection<SettingItemViewModel> Rows { get; } = [];
}
