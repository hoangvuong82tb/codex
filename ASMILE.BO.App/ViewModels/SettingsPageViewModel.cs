using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels.Settings;

public sealed partial class SettingsPageViewModel : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Breadcrumb { get; set; } = string.Empty;
    public ObservableCollection<EditableSettingItemViewModel> Rows { get; } = [];
    public ObservableCollection<object> Cards { get; } = [];
    public bool CanEdit { get; set; } = true;
    public IRelayCommand? SaveCommand { get; set; }
    public IRelayCommand? BackCommand { get; set; }
}

