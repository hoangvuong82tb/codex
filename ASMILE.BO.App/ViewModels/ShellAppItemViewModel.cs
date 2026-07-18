using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.ViewModels;

public sealed class ShellAppItemViewModel
{
    public ShellAppItemViewModel(AppModuleInfo module)
    {
        Module = module;
    }

    public AppModuleInfo Module { get; }
}
