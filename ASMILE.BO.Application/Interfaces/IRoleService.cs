using ASMILE.BO.Application.DTOs.Security;

namespace ASMILE.BO.Application.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync();
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync();
    Task<RoleDto?> GetRoleByIdAsync(Guid roleId);
    Task<IReadOnlyList<string>> GetRolePermissionCodesAsync(Guid roleId);
    Task SaveRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes);
    Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(Guid userId);
}
