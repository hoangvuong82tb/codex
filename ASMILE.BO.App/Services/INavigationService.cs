using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.Services;

public interface INavigationService
{
    event Action<string>? Navigated;
    void Navigate(string key);
    IReadOnlyList<AppModuleInfo> GetModuleCatalog();
    string GetPermissionCode(string route);
    Task<NavigationResult> OpenModuleAsync(string route, CancellationToken cancellationToken = default);
    Task<NavigationResult> OpenJournalAsync(Guid journalId, CancellationToken cancellationToken = default);
    Task<NavigationResult> OpenVoucherAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default);
    Task<NavigationResult> OpenReportAsync(string reportCode, CancellationToken cancellationToken = default);
    Task<NavigationResult> OpenInNewTabAsync(NavigationRequest request, CancellationToken cancellationToken = default);
    Task<NavigationResult> GoBackAsync(CancellationToken cancellationToken = default);
    Task<NavigationResult> GoForwardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NavigationHistoryItem>> RestoreWorkspaceAsync(CancellationToken cancellationToken = default);
}
