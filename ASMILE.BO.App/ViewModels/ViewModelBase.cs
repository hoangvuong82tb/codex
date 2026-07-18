using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;
}

