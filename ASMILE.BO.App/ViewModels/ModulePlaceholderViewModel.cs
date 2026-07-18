using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class ModulePlaceholderViewModel : ViewModelBase
{
    public ModulePlaceholderViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;
}
