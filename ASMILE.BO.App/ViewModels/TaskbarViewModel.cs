namespace ASMILE.BO.App.ViewModels;

public sealed class TaskbarViewModel
{
    public TaskbarViewModel(DesktopShellViewModel shell)
    {
        Shell = shell;
    }

    public DesktopShellViewModel Shell { get; }
}
