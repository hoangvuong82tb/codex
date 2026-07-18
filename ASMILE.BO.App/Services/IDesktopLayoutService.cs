namespace ASMILE.BO.App.Services;

public interface IDesktopLayoutService
{
    double WorkspaceWidth { get; }
    double WorkspaceHeight { get; }
    void UpdateBounds(double width, double height);
}
