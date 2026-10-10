namespace ASMILE.BO.Application.Interfaces;

/// <summary>
/// Opens an operating-system settings page that the application cannot safely
/// configure itself (for example, Windows per-user default app associations).
/// </summary>
public interface IWindowsSettingsLauncher
{
    void OpenDefaultAppsPage();
    void OpenStartupAppsPage();
    void OpenAboutPage();
    void OpenDateTimePage();
    void OpenFirewallPage();
    void OpenPrintersPage();
    void OpenServicesConsole();
}
