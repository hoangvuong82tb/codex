using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Security.Store;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class RoleService : IRoleService
{
    private readonly InMemorySecurityStore _store;
    private readonly AppDbContext? _dbContext;

    public RoleService(InMemorySecurityStore store, AppDbContext? dbContext = null)
    {
        _store = store;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync()
    {
        if (_dbContext is not null)
        {
            return await _dbContext.AppRoles
                .AsNoTracking()
                .Where(x => !x.IsArchived)
                .OrderBy(x => x.Code)
                .Select(x => new RoleDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    IsSystem = x.IsSystem,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        return _store.Roles
            .Select(x => new RoleDto { Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description, IsSystem = x.IsSystem, IsActive = x.IsActive })
            .ToList();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync()
    {
        if (_dbContext is not null)
        {
            return await _dbContext.AppPermissions
                .AsNoTracking()
                .OrderBy(x => x.Group)
                .ThenBy(x => x.Code)
                .Select(x => new PermissionDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Group = x.Group,
                    Description = x.Description
                })
                .ToListAsync();
        }

        return _store.Permissions
            .Select(x => new PermissionDto { Id = x.Id, Code = x.Code, Name = x.Name, Group = x.Group, Description = x.Description })
            .ToList();
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId)
    {
        if (_dbContext is not null)
        {
            return await _dbContext.AppRoles
                .AsNoTracking()
                .Where(x => x.Id == roleId && !x.IsArchived)
                .Select(x => new RoleDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    IsSystem = x.IsSystem,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
        }

        return _store.Roles
            .Where(x => x.Id == roleId)
            .Select(x => new RoleDto { Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description, IsSystem = x.IsSystem, IsActive = x.IsActive })
            .FirstOrDefault();
    }

    public async Task SaveRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes)
    {
        if (_dbContext is not null)
        {
            var roleExists = await _dbContext.AppRoles.AnyAsync(x => x.Id == roleId && !x.IsArchived);
            if (!roleExists)
            {
                throw new InvalidOperationException("Vai trò không tồn tại hoặc đã được lưu trữ.");
            }

            var desiredCodes = permissionCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var validCodes = await _dbContext.AppPermissions
                .Where(x => desiredCodes.Contains(x.Code))
                .Select(x => x.Code)
                .ToListAsync();
            var desiredSet = new HashSet<string>(validCodes, StringComparer.OrdinalIgnoreCase);

            var existing = await _dbContext.AppRolePermissions
                .Where(x => x.RoleId == roleId)
                .ToListAsync();
            _dbContext.AppRolePermissions.RemoveRange(existing.Where(x => !desiredSet.Contains(x.PermissionCode)));

            var existingSet = existing
                .Select(x => x.PermissionCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var permissionCode in desiredSet.Where(x => !existingSet.Contains(x)))
            {
                _dbContext.AppRolePermissions.Add(new AppRolePermission { RoleId = roleId, PermissionCode = permissionCode });
            }

            await _dbContext.SaveChangesAsync();
            return;
        }

        var permissionIdSet = _store.Permissions
            .Where(x => permissionCodes.Contains(x.Code, StringComparer.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .ToHashSet();
        _store.RolePermissions.RemoveAll(x => x.RoleId == roleId);
        foreach (var pid in permissionIdSet)
        {
            _store.RolePermissions.Add(new Core.Entities.RolePermission { RoleId = roleId, PermissionId = pid });
        }
    }

    public async Task<IReadOnlyList<string>> GetRolePermissionCodesAsync(Guid roleId)
    {
        if (_dbContext is not null)
        {
            return await _dbContext.AppRolePermissions
                .AsNoTracking()
                .Where(x => x.RoleId == roleId)
                .Select(x => x.PermissionCode)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        var permissionIds = _store.RolePermissions
            .Where(x => x.RoleId == roleId)
            .Select(x => x.PermissionId)
            .ToHashSet();
        return _store.Permissions
            .Where(x => permissionIds.Contains(x.Id))
            .Select(x => x.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(Guid userId)
    {
        if (_dbContext is not null)
        {
            var dbRoleIds = await _dbContext.AppUserRoles
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Join(
                    _dbContext.AppRoles.AsNoTracking().Where(role => role.IsActive && !role.IsArchived),
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, _) => userRole.RoleId)
                .ToArrayAsync();
            if (dbRoleIds.Length == 0)
            {
                return Array.Empty<string>();
            }

            return await _dbContext.AppRolePermissions
                .AsNoTracking()
                .Where(x => dbRoleIds.Contains(x.RoleId))
                .Select(x => x.PermissionCode)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        var activeRoleIds = _store.Roles
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .ToHashSet();
        var roleIds = _store.UserRoles
            .Where(x => x.UserId == userId && activeRoleIds.Contains(x.RoleId))
            .Select(x => x.RoleId)
            .ToHashSet();
        var permissionIds = _store.RolePermissions.Where(x => roleIds.Contains(x.RoleId)).Select(x => x.PermissionId).ToHashSet();
        return _store.Permissions
            .Where(x => permissionIds.Contains(x.Id))
            .Select(x => x.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
