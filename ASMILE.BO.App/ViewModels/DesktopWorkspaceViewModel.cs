namespace ASMILE.BO.App.ViewModels;

public sealed class DesktopWorkspaceViewModel
{
    public DesktopWorkspaceViewModel(DesktopShellViewModel shell)
    {
        Shell = shell;
    }

    public DesktopShellViewModel Shell { get; }
}
