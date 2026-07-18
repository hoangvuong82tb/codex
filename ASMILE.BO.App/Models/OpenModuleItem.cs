using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.Models;

public sealed partial class OpenModuleItem : ObservableObject
{
    public OpenModuleItem(AppModuleInfo module, object viewModel)
    {
        Module = module;
        ViewModel = viewModel;
    }

    public AppModuleInfo Module { get; }
    public object ViewModel { get; }

    [ObservableProperty]
    private bool isActive;
}
