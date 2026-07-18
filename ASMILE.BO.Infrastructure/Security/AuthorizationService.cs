using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Security;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class AuthorizationService : IAuthorizationService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISecurityPromptService _securityPromptService;

    private static readonly Dictionary<Permission, string> PermissionMap = new()
    {
        { Permission.ViewSystemSettings, "Settings.View" },
        { Permission.EditSystemSettings, "Settings.Edit" },
        { Permission.ManageDatabase, "Database.View" },
        { Permission.RunMigration, "Database.RunMigration" },
        { Permission.BackupDatabase, "Database.Backup" },
        { Permission.RestoreDatabase, "Database.Restore" },
        { Permission.ManageUsers, "Security.Users.Edit" },
        { Permission.ViewAuditLogs, "Logs.View" },
        { Permission.ManageServices, "System.Maintenance" },
        { Permission.ExportData, "VFC.Export" },
        { Permission.PrintDocuments, "VFC.Print" },
        { Permission.OpenSensitiveFolder, "VFC.OpenPhysicalLocation" }
    };

    public AuthorizationService(ICurrentUserService currentUserService, ISecurityPromptService securityPromptService)
    {
        _currentUserService = currentUserService;
        _securityPromptService = securityPromptService;
    }

    public bool HasPermission(string permissionCode) => _currentUserService.HasPermission(permissionCode);

    public void Demand(string permissionCode)
    {
        if (!HasPermission(permissionCode))
        {
            throw new UnauthorizedAccessException($"Permission denied: {permissionCode}");
        }
    }

    public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(HasPermission(permissionCode));

    public bool HasPermission(Permission permission)
    {
        return PermissionMap.TryGetValue(permission, out var code) && HasPermission(code);
    }

    public void Demand(Permission permission)
    {
        if (!HasPermission(permission))
        {
            throw new UnauthorizedAccessException($"Permission denied: {permission}");
        }
    }

    public Task<bool> ConfirmDangerousActionAsync(string title, string message)
    {
        return _securityPromptService.ConfirmDangerousActionAsync(title, message);
    }
}
