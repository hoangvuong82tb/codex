namespace ASMILE.BO.App.ViewModels;

public sealed class StartMenuViewModel
{
    public StartMenuViewModel(DesktopShellViewModel shell)
    {
        Shell = shell;
    }

    public DesktopShellViewModel Shell { get; }
}
