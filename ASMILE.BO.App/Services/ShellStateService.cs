using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.Services;

public sealed partial class ShellStateService : ObservableObject, IShellStateService
{
    [ObservableProperty]
    private bool isStartMenuOpen;
}
