using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(string key, string title, string icon)
    {
        Key = key;
        Title = title;
        Icon = icon;
    }

    public string Key { get; }
    public string Title { get; }
    public string Icon { get; }

    [ObservableProperty]
    private bool isSelected;
}

