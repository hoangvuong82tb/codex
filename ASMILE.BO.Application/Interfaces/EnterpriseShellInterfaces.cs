using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IPermissionService
{
    Task<PermissionCheckResultDto> CheckAsync(string permissionCode, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default);
    Task<PermissionCheckResultDto> CheckAsync(string permissionCode, Guid? companyId, Guid? branchId = null, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string permissionCode, Guid? companyId, Guid? branchId = null, CancellationToken cancellationToken = default);
    Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnterpriseRoleDto>> GetRolesAsync(string? searchText = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnterprisePermissionDto>> GetPermissionsAsync(Guid? roleId = null, string? searchText = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnterpriseUserDto>> GetUsersAsync(string? searchText = null, CancellationToken cancellationToken = default);
    Task<EnterpriseRoleDto> CreateRoleAsync(string code, string name, string? description, CancellationToken cancellationToken = default);
    Task RenameRoleAsync(Guid roleId, string name, string? description, CancellationToken cancellationToken = default);
    Task<EnterpriseRoleDto> CloneRoleAsync(Guid roleId, string newCode, string newName, CancellationToken cancellationToken = default);
    Task ArchiveRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task AssignPermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default);
    Task RemovePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default);
    Task AssignUsersToRoleAsync(Guid roleId, IReadOnlyList<Guid> userIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDiffDto>> CompareRolesAsync(Guid sourceRoleId, Guid targetRoleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionTreeNodeDto>> GetPermissionTreeAsync(Guid? roleId = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionMatrixRowDto>> GetModuleActionMatrixAsync(Guid? roleId = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task SetRoleInheritanceAsync(Guid roleId, IReadOnlyList<Guid> parentRoleIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataPermissionDto>> GetDataPermissionsAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken = default);
    Task SetDataPermissionAsync(DataPermissionDto permission, CancellationToken cancellationToken = default);
    Task<EffectivePermissionPreviewDto> PreviewEffectivePermissionsAsync(Guid userId, Guid? companyId = null, Guid? branchId = null, CancellationToken cancellationToken = default);
    Task<bool> HasDataAccessAsync(string scopeType, Guid scopeId, Guid? companyId = null, Guid? branchId = null, CancellationToken cancellationToken = default);
}

public interface ICurrentCompanyContext
{
    Guid? CompanyId { get; }
    Guid? BranchId { get; }
    string CompanyName { get; }
    string? BranchName { get; }
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
    Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default);
}

public interface ICompanyContextAccessor
{
    Guid? CurrentCompanyId { get; }
    Guid? CurrentBranchId { get; }
    string? CurrentCompanyName { get; }
    string? CurrentBranchName { get; }
    Guid? CurrentUserId { get; }
    void SetCurrentScope(Guid? companyId, string? companyName = null, Guid? branchId = null, string? branchName = null);
    CompanyScope EnterCompanyScope(Guid companyId, string? companyName = null, Guid? branchId = null, string? branchName = null);
    BranchScope EnterBranchScope(Guid? branchId, string? branchName = null);
}

public sealed class CompanyScope : IDisposable
{
    private readonly Action _restore;
    public CompanyScope(Action restore)
    {
        _restore = restore;
    }

    public void Dispose()
    {
        _restore();
    }
}

public sealed class BranchScope : IDisposable
{
    private readonly Action _restore;
    public BranchScope(Action restore)
    {
        _restore = restore;
    }

    public void Dispose()
    {
        _restore();
    }
}

public interface IWorkspaceService
{
    Task<WorkspaceStateDto> LoadWorkspaceAsync(CancellationToken cancellationToken = default);
    Task SavePinnedModulesAsync(IReadOnlyList<string> moduleKeys, CancellationToken cancellationToken = default);
    Task AddRecentModuleAsync(string moduleKey, CancellationToken cancellationToken = default);
    Task AddFavoriteReportAsync(string reportCode, CancellationToken cancellationToken = default);
    Task AddRecentReportAsync(string reportCode, CancellationToken cancellationToken = default);
    Task AddRecentDrilldownAsync(string drilldownKey, CancellationToken cancellationToken = default);
    Task SaveLastOpenedTabsAsync(IReadOnlyList<string> moduleKeys, CancellationToken cancellationToken = default);
    Task SaveShellLayoutAsync(string layoutJson, CancellationToken cancellationToken = default);
    Task<WorkspaceSettingsDto> LoadSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(WorkspaceSettingsDto settings, CancellationToken cancellationToken = default);
    Task ClearWorkspaceCacheAsync(CancellationToken cancellationToken = default);
}

public interface IGlobalSearchService
{
    Task<IReadOnlyList<GlobalSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
