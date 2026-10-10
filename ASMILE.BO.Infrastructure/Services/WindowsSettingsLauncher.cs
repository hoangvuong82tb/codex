using System.Diagnostics;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class WindowsSettingsLauncher : IWindowsSettingsLauncher
{
    private const string DefaultAppsSettingsUri = "ms-settings:defaultapps";
    private const string StartupAppsSettingsUri = "ms-settings:startupapps";
    private const string AboutSettingsUri = "ms-settings:about";
    private const string DateTimeSettingsUri = "ms-settings:dateandtime";
    private const string FirewallSettingsUri = "ms-settings:windowsdefender";
    private const string PrintersSettingsUri = "ms-settings:printers";

    public void OpenDefaultAppsPage() => OpenSettingsPage(DefaultAppsSettingsUri, "Default apps");

    public void OpenStartupAppsPage() => OpenSettingsPage(StartupAppsSettingsUri, "Startup apps");
    public void OpenAboutPage() => OpenSettingsPage(AboutSettingsUri, "About");
    public void OpenDateTimePage() => OpenSettingsPage(DateTimeSettingsUri, "Date & time");
    public void OpenFirewallPage() => OpenSettingsPage(FirewallSettingsUri, "Windows Firewall");
    public void OpenPrintersPage() => OpenSettingsPage(PrintersSettingsUri, "Printers & scanners");

    public void OpenServicesConsole()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows Services is only available on Windows.");
        }

        var servicesConsole = Path.Combine(Environment.SystemDirectory, "services.msc");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = servicesConsole,
            UseShellExecute = true
        });

        if (process is null)
        {
            throw new InvalidOperationException("Windows did not start the Services console.");
        }
    }

    private static void OpenSettingsPage(string uri, string pageName)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException($"Windows {pageName} settings are only available on Windows.");
        }

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = uri,
            UseShellExecute = true
        });

        if (process is null)
        {
            throw new InvalidOperationException($"Windows did not start the {pageName} settings page.");
        }
    }
}
