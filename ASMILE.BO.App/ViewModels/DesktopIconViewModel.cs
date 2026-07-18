using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.ViewModels;

public sealed class DesktopIconViewModel
{
    public DesktopIconViewModel(AppModuleInfo app)
    {
        App = app;
    }

    public AppModuleInfo App { get; }
}
