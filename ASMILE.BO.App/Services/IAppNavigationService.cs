namespace ASMILE.BO.App.Services;

public interface IAppNavigationService
{
    event Action<string>? ModuleRequested;
    void RequestOpen(string moduleKey);
}

