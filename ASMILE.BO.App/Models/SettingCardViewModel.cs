using System.Collections.ObjectModel;

namespace ASMILE.BO.App.Models;

public sealed class SettingCardViewModel
{
    public SettingCardViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }
    public string Description { get; }
    public ObservableCollection<SettingItemViewModel> Items { get; } = [];
}
