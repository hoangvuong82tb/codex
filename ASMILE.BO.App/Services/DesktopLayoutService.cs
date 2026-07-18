namespace ASMILE.BO.App.Services;

public sealed class DesktopLayoutService : IDesktopLayoutService
{
    public double WorkspaceWidth { get; private set; } = 1320;
    public double WorkspaceHeight { get; private set; } = 760;

    public void UpdateBounds(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        WorkspaceWidth = width;
        WorkspaceHeight = height;
    }
}
