using ASMILE.BO.Application.DTOs.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels.Settings;

public sealed partial class AppSettingsViewModel : ObservableObject
{
    public ObservableCollection<AppModuleDto> Apps { get; } = [];
    [ObservableProperty] private AppModuleDto? selectedApp;
    public ObservableCollection<EditableSettingItemViewModel> AppSettings { get; } = [];
    public IRelayCommand? SaveAppCommand { get; set; }
    public IRelayCommand? ResetAppCommand { get; set; }
    public IRelayCommand? ClearCacheCommand { get; set; }
    public IRelayCommand? OpenDataLocationCommand { get; set; }
}

