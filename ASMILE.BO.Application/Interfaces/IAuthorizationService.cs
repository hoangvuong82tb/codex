using ASMILE.BO.Core.Security;

namespace ASMILE.BO.Application.Interfaces;

public interface IAuthorizationService
{
    bool HasPermission(string permissionCode);
    void Demand(string permissionCode);
    Task<bool> CanAccessAsync(string permissionCode);

    bool HasPermission(Permission permission);
    void Demand(Permission permission);
    Task<bool> ConfirmDangerousActionAsync(string title, string message);
}
