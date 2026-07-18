using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.Models;

public sealed partial class SettingsCategoryItem : ObservableObject
{
    public SettingsCategoryItem(string key, string title, string iconGlyph, string description)
    {
        Key = key;
        Title = title;
        IconGlyph = iconGlyph;
        Description = description;
    }

    public string Key { get; }
    public string Title { get; }
    public string IconGlyph { get; }
    public string Description { get; }

    [ObservableProperty]
    private bool isSelected;
}
