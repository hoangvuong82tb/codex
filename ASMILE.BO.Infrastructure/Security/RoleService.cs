using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Security.Store;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class RoleService : IRoleService
{
    private readonly InMemorySecurityStore _store;

    public RoleService(InMemorySecurityStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<RoleDto>> GetRolesAsync()
    {
        var roles = _store.Roles.Select(x => new RoleDto { Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description, IsSystem = x.IsSystem, IsActive = x.IsActive }).ToList();
        return Task.FromResult((IReadOnlyList<RoleDto>)roles);
    }

    public Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync()
    {
        var permissions = _store.Permissions.Select(x => new PermissionDto { Id = x.Id, Code = x.Code, Name = x.Name, Group = x.Group, Description = x.Description }).ToList();
        return Task.FromResult((IReadOnlyList<PermissionDto>)permissions);
    }

    public Task<RoleDto?> GetRoleByIdAsync(Guid roleId)
    {
        var role = _store.Roles.Where(x => x.Id == roleId).Select(x => new RoleDto { Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description, IsSystem = x.IsSystem, IsActive = x.IsActive }).FirstOrDefault();
        return Task.FromResult(role);
    }

    public Task SaveRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes)
    {
        var permissionIdSet = _store.Permissions.Where(x => permissionCodes.Contains(x.Code, StringComparer.OrdinalIgnoreCase)).Select(x => x.Id).ToHashSet();
        _store.RolePermissions.RemoveAll(x => x.RoleId == roleId);
        foreach (var pid in permissionIdSet)
        {
            _store.RolePermissions.Add(new Core.Entities.RolePermission { RoleId = roleId, PermissionId = pid });
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(Guid userId)
    {
        var roleIds = _store.UserRoles.Where(x => x.UserId == userId).Select(x => x.RoleId).ToHashSet();
        var permissionIds = _store.RolePermissions.Where(x => roleIds.Contains(x.RoleId)).Select(x => x.PermissionId).ToHashSet();
        var codes = _store.Permissions.Where(x => permissionIds.Contains(x.Id)).Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return Task.FromResult((IReadOnlyList<string>)codes);
    }
}
