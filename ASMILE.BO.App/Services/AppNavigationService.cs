namespace ASMILE.BO.App.Services;

public sealed class AppNavigationService : IAppNavigationService
{
    public event Action<string>? ModuleRequested;

    public void RequestOpen(string moduleKey)
    {
        if (string.IsNullOrWhiteSpace(moduleKey))
        {
            return;
        }

        ModuleRequested?.Invoke(moduleKey.Trim());
    }
}

