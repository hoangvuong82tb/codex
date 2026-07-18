using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class PermissionService : IPermissionService
{
    private static readonly SemaphoreSlim SeedGate = new(1, 1);
    private static readonly Dictionary<string, string> LegacyPermissionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dashboard.view"] = "System.View",
        ["virtual-file-center.view"] = "VFC.View",
        ["inventory.view"] = "Warehouse.View",
        ["inventory.approve"] = "Warehouse.Approve",
        ["inventory.post"] = "Accounting.Approve",
        ["receivable.view"] = "Accounting.View",
        ["payable.view"] = "Accounting.View",
        ["cash.view"] = "Accounting.View",
        ["cash.post"] = "Accounting.Approve",
        ["bank.view"] = "Accounting.View",
        ["bank.post"] = "Accounting.Approve",
        ["asset.view"] = "Accounting.View",
        ["asset.post"] = "Accounting.Approve",
        ["tax.view"] = "Accounting.View",
        ["tax.lock"] = "Accounting.Approve",
        ["financial.view"] = "Accounting.View",
        ["financial.close-period"] = "Accounting.Approve",
        ["general-ledger.view"] = "Accounting.View",
        ["general-ledger.post"] = "Accounting.Approve",
        ["master-data.view"] = "System.View",
        ["hr.view"] = "HR.View",
        ["reports.view"] = "Accounting.View",
        ["settings.view"] = "Settings.View",
        ["security.view"] = "Security.Users.View",
        ["security.admin"] = "Security.Roles.Edit",
        ["operations.view"] = "System.Maintenance",
        ["audit.view"] = "Logs.View",
        ["menu-recipe.view"] = "Sales.View",
        ["menu-recipe.edit"] = "Sales.Edit",
        ["menu-recipe.cost-preview"] = "Sales.View",
        ["menu-recipe.deactivate"] = "Sales.Edit",
        ["menu-recipe.admin"] = "Sales.ConfigureClient",
        ["breakfast-cafe.view"] = "Sales.View",
        ["breakfast-cafe.sell"] = "Sales.Create",
        ["breakfast-cafe.cancel"] = "Sales.Edit",
        ["breakfast-cafe.discount"] = "Sales.Edit",
        ["breakfast-cafe.admin"] = "Sales.ConfigureClient",
        ["restaurant.alacarte.view"] = "Sales.View",
        ["restaurant.alacarte.manage"] = "Sales.Edit",
        ["restaurant.banquet.view"] = "Sales.View",
        ["restaurant.banquet.manage"] = "Sales.Edit"
    };

    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly IAuditLogService? _auditLogService;
    private readonly Dictionary<string, HashSet<string>> _permissionCache = new();
    private readonly Dictionary<string, bool> _accessCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _seedChecked;

    public PermissionService(AppDbContext dbContext, ICurrentUserService currentUserService, ICurrentCompanyContext companyContext, IAuditLogService? auditLogService = null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _companyContext = companyContext;
        _auditLogService = auditLogService;
    }

    public async Task<PermissionCheckResultDto> CheckAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        await _companyContext.EnsureInitializedAsync(cancellationToken);
        return await CheckAsync(permissionCode, _companyContext.CompanyId, _companyContext.BranchId, cancellationToken);
    }

    public async Task<PermissionCheckResultDto> CheckAsync(string permissionCode, Guid? companyId, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.CurrentUser is null)
        {
            return new PermissionCheckResultDto { IsAllowed = false, PermissionCode = permissionCode, Reason = "Người dùng chưa đăng nhập." };
        }

        if (await HasPermissionAsync(permissionCode, companyId, branchId, cancellationToken))
        {
            return new PermissionCheckResultDto { IsAllowed = true, PermissionCode = permissionCode };
        }

        return new PermissionCheckResultDto
        {
            IsAllowed = false,
            PermissionCode = permissionCode,
            Reason = $"Bạn không có quyền {permissionCode} trong phạm vi {BuildScopeLabel(companyId, branchId)}."
        };
    }

    public async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        await _companyContext.EnsureInitializedAsync(cancellationToken);
        return await HasPermissionAsync(permissionCode, _companyContext.CompanyId, _companyContext.BranchId, cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(string permissionCode, Guid? companyId, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return true;
        }

        var user = _currentUserService.CurrentUser;
        if (user is null)
        {
            return false;
        }

        if (user.Roles.Any(x => string.Equals(x, "Administrator", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        await EnsureSeededAsync(cancellationToken);
        if (!await HasCompanyAccessAsync(user.Id, companyId, cancellationToken))
        {
            return false;
        }

        if (branchId.HasValue && !await HasBranchAccessAsync(user.Id, companyId, branchId.Value, cancellationToken))
        {
            return false;
        }

        if (_currentUserService.HasPermission(permissionCode))
        {
            return true;
        }

        if (LegacyPermissionMap.TryGetValue(permissionCode, out var legacyCode) && _currentUserService.HasPermission(legacyCode))
        {
            return true;
        }

        var permissions = await GetCachedPermissionsAsync(user.Id, user.Roles, companyId, branchId, cancellationToken);
        return permissions.Contains(permissionCode);
    }

    public Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        foreach (var key in _permissionCache.Keys.Where(x => x.StartsWith($"{userId:N}|", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            _permissionCache.Remove(key);
        }

        foreach (var key in _accessCache.Keys.Where(x => x.StartsWith($"{userId:N}|", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            _accessCache.Remove(key);
        }

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<EnterpriseRoleDto>> GetRolesAsync(string? searchText = null, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var query = _dbContext.AppRoles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(x => x.Code.Contains(searchText) || x.Name.Contains(searchText));
        }

        return await query
            .OrderBy(x => x.IsArchived)
            .ThenBy(x => x.Code)
            .Select(x => new EnterpriseRoleDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsSystem = x.IsSystem,
                IsActive = x.IsActive,
                IsArchived = x.IsArchived,
                PermissionCount = _dbContext.AppRolePermissions.Count(p => p.RoleId == x.Id),
                UserCount = _dbContext.AppUserRoles.Count(u => u.RoleId == x.Id)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EnterprisePermissionDto>> GetPermissionsAsync(Guid? roleId = null, string? searchText = null, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var assigned = roleId.HasValue
            ? await _dbContext.AppRolePermissions.AsNoTracking().Where(x => x.RoleId == roleId.Value).Select(x => x.PermissionCode).ToListAsync(cancellationToken)
            : [];
        var assignedSet = new HashSet<string>(assigned, StringComparer.OrdinalIgnoreCase);

        var baseQuery = _dbContext.AppPermissions.AsNoTracking();
        List<AppPermission> permissions;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var trimmedFilter = searchText.Trim();
            permissions = await baseQuery
                .Where(x => x.Code.Contains(trimmedFilter) || x.Name.Contains(trimmedFilter) || x.Group.Contains(trimmedFilter))
                .OrderBy(x => x.Group)
                .ThenBy(x => x.Code)
                .ToListAsync(cancellationToken);

            // Avoid empty permission grid when user types a role name into the shared search box.
            if (permissions.Count == 0)
            {
                permissions = await baseQuery
                    .OrderBy(x => x.Group)
                    .ThenBy(x => x.Code)
                    .ToListAsync(cancellationToken);
            }
        }
        else
        {
            permissions = await baseQuery
                .OrderBy(x => x.Group)
                .ThenBy(x => x.Code)
                .ToListAsync(cancellationToken);
        }

        return permissions.Select(x => new EnterprisePermissionDto
        {
            Code = x.Code,
            Name = x.Name,
            Group = x.Group,
            Description = x.Description,
            IsDangerous = IsDangerousPermission(x.Code),
            IsAssigned = assignedSet.Contains(x.Code),
            Module = SplitPermissionCode(x.Code).Module,
            Action = SplitPermissionCode(x.Code).Action,
            DenialReason = assignedSet.Contains(x.Code) ? string.Empty : "Chưa được cấp quyền."
        }).ToArray();
    }

    public async Task<IReadOnlyList<EnterpriseUserDto>> GetUsersAsync(string? searchText = null, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        await EnsureCurrentUserMirroredAndSavedAsync(cancellationToken);
        var query = _dbContext.AppUsers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(x => x.UserName.Contains(searchText) || x.DisplayName.Contains(searchText));
        }

        var users = await query.OrderBy(x => x.UserName).ToListAsync(cancellationToken);
        var userIds = users.Select(x => x.Id).ToArray();
        var roles = await _dbContext.AppUserRoles
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new { x.UserId, RoleCode = x.Role!.Code })
            .ToListAsync(cancellationToken);

        return users.Select(x => new EnterpriseUserDto
        {
            Id = x.Id,
            UserName = x.UserName,
            DisplayName = x.DisplayName,
            Email = x.Email,
            IsActive = x.IsActive,
            Roles = roles.Where(r => r.UserId == x.Id).Select(r => r.RoleCode).ToArray()
        }).ToArray();
    }

    public async Task<EnterpriseRoleDto> CreateRoleAsync(string code, string name, string? description, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var normalizedCode = NormalizeCode(code);
        if (await _dbContext.AppRoles.AnyAsync(x => x.Code == normalizedCode, cancellationToken))
        {
            throw new InvalidOperationException($"Mã vai trò ''{normalizedCode}'' đã tồn tại.");
        }

        var role = new AppRole { Code = normalizedCode, Name = name.Trim(), Description = description, IsSystem = false, IsActive = true };
        _dbContext.AppRoles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditPermissionChangeAsync("Permission.Role.Create", $"Tạo vai trò {role.Code}.", cancellationToken);
        return (await GetRolesAsync(role.Code, cancellationToken)).First(x => x.Id == role.Id);
    }

    public async Task RenameRoleAsync(Guid roleId, string name, string? description, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.AppRoles.FirstAsync(x => x.Id == roleId, cancellationToken);
        role.Name = name.Trim();
        role.Description = description;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditPermissionChangeAsync("Permission.Role.Update", $"Cập nhật vai trò {role.Code}.", cancellationToken);
    }

    public async Task<EnterpriseRoleDto> CloneRoleAsync(Guid roleId, string newCode, string newName, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var source = await _dbContext.AppRoles.AsNoTracking().FirstAsync(x => x.Id == roleId, cancellationToken);
        var clone = new AppRole { Code = NormalizeCode(newCode), Name = newName.Trim(), Description = $"Copy of {source.Code}", IsActive = true };
        _dbContext.AppRoles.Add(clone);
        var sourcePermissions = await _dbContext.AppRolePermissions.AsNoTracking().Where(x => x.RoleId == roleId).Select(x => x.PermissionCode).ToListAsync(cancellationToken);
        foreach (var permissionCode in sourcePermissions)
        {
            _dbContext.AppRolePermissions.Add(new AppRolePermission { RoleId = clone.Id, PermissionCode = permissionCode });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _permissionCache.Clear();
        await AuditPermissionChangeAsync("Permission.Role.Clone", $"Copy vai trò {source.Code} sang {clone.Code}.", cancellationToken);
        return (await GetRolesAsync(clone.Code, cancellationToken)).First(x => x.Id == clone.Id);
    }

    public async Task ArchiveRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.AppRoles.FirstAsync(x => x.Id == roleId, cancellationToken);
        role.IsArchived = true;
        role.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
        _permissionCache.Clear();
        await AuditPermissionChangeAsync("Permission.Role.Archive", $"Lưu trữ vai trò {role.Code}.", cancellationToken);
    }

    public async Task AssignPermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.AppRolePermissions.Where(x => x.RoleId == roleId).Select(x => x.PermissionCode).ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var permissionCode in permissionCodes.Where(x => !existingSet.Contains(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _dbContext.AppRolePermissions.Add(new AppRolePermission { RoleId = roleId, PermissionCode = permissionCode });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _permissionCache.Clear();
        await AuditPermissionChangeAsync("Permission.Role.AssignPermissions", $"Gán {permissionCodes.Count} quyền cho role {roleId}.", cancellationToken);
    }

    public async Task RemovePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var removeSet = new HashSet<string>(permissionCodes, StringComparer.OrdinalIgnoreCase);
        var rows = await _dbContext.AppRolePermissions.Where(x => x.RoleId == roleId && removeSet.Contains(x.PermissionCode)).ToListAsync(cancellationToken);
        _dbContext.AppRolePermissions.RemoveRange(rows);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _permissionCache.Clear();
        await AuditPermissionChangeAsync("Permission.Role.RemovePermissions", $"Gỡ {permissionCodes.Count} quyền khỏi role {roleId}.", cancellationToken);
    }

    public async Task AssignUsersToRoleAsync(Guid roleId, IReadOnlyList<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.AppUserRoles.Where(x => x.RoleId == roleId).Select(x => x.UserId).ToListAsync(cancellationToken);
        var existingSet = new HashSet<Guid>(existing);
        foreach (var userId in userIds.Where(x => !existingSet.Contains(x)).Distinct())
        {
            _dbContext.AppUserRoles.Add(new AppUserRole { RoleId = roleId, UserId = userId });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _permissionCache.Clear();
        await AuditPermissionChangeAsync("Permission.User.AssignRole", $"Gán {userIds.Count} người dùng vào role {roleId}.", cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDiffDto>> CompareRolesAsync(Guid sourceRoleId, Guid targetRoleId, CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.AppRolePermissions.AsNoTracking().Where(x => x.RoleId == sourceRoleId).Select(x => x.PermissionCode).ToListAsync(cancellationToken);
        var target = await _dbContext.AppRolePermissions.AsNoTracking().Where(x => x.RoleId == targetRoleId).Select(x => x.PermissionCode).ToListAsync(cancellationToken);
        var sourceSet = new HashSet<string>(source, StringComparer.OrdinalIgnoreCase);
        var targetSet = new HashSet<string>(target, StringComparer.OrdinalIgnoreCase);
        var catalog = await _dbContext.AppPermissions.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Group, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return sourceSet.Except(targetSet, StringComparer.OrdinalIgnoreCase)
            .Select(x => new PermissionDiffDto { PermissionCode = x, ChangeType = "Only source", Group = catalog.GetValueOrDefault(x, string.Empty) })
            .Concat(targetSet.Except(sourceSet, StringComparer.OrdinalIgnoreCase)
                .Select(x => new PermissionDiffDto { PermissionCode = x, ChangeType = "Only target", Group = catalog.GetValueOrDefault(x, string.Empty) }))
            .OrderBy(x => x.Group)
            .ThenBy(x => x.PermissionCode)
            .ToArray();
    }

    public async Task<IReadOnlyList<PermissionTreeNodeDto>> GetPermissionTreeAsync(Guid? roleId = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(roleId, cancellationToken: cancellationToken);
        var denied = userId.HasValue
            ? await _dbContext.AppUserPermissions.IgnoreQueryFilters().AsNoTracking().Where(x => x.UserId == userId.Value && !x.IsGranted).Select(x => x.PermissionCode).ToListAsync(cancellationToken)
            : [];
        var deniedSet = new HashSet<string>(denied, StringComparer.OrdinalIgnoreCase);

        return permissions
            .GroupBy(x => x.Group)
            .OrderBy(x => x.Key)
            .Select(group => new PermissionTreeNodeDto
            {
                Code = group.Key,
                Name = group.Key,
                Group = group.Key,
                Children = group.OrderBy(x => x.Code).Select(x => new PermissionTreeNodeDto
                {
                    Code = x.Code,
                    Name = x.Name,
                    Group = x.Group,
                    IsAssigned = x.IsAssigned,
                    IsDenied = deniedSet.Contains(x.Code),
                    DenialReason = deniedSet.Contains(x.Code) ? "Deny override đang có hiệu lực." : x.DenialReason
                }).ToArray()
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<PermissionMatrixRowDto>> GetModuleActionMatrixAsync(Guid? roleId = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(roleId, cancellationToken: cancellationToken);
        HashSet<string> effective = [];
        HashSet<string> denied = [];

        if (userId.HasValue)
        {
            var preview = await PreviewEffectivePermissionsAsync(userId.Value, cancellationToken: cancellationToken);
            effective = preview.Matrix.Where(x => x.IsGranted).Select(x => x.PermissionCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            denied = preview.Matrix.Where(x => x.IsDenied).Select(x => x.PermissionCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return permissions.Select(x => new PermissionMatrixRowDto
        {
            Module = x.Module,
            Action = x.Action,
            PermissionCode = x.Code,
            IsGranted = userId.HasValue ? effective.Contains(x.Code) : x.IsAssigned,
            IsDenied = denied.Contains(x.Code),
            Reason = denied.Contains(x.Code) ? "Bị từ chối trực tiếp." : (userId.HasValue ? effective.Contains(x.Code) : x.IsAssigned) ? "Được cấp." : "Chưa được cấp."
        }).OrderBy(x => x.Module).ThenBy(x => x.Action).ToArray();
    }

    public async Task SetRoleInheritanceAsync(Guid roleId, IReadOnlyList<Guid> parentRoleIds, CancellationToken cancellationToken = default)
    {
        if (parentRoleIds.Contains(roleId))
        {
            throw new InvalidOperationException("Vai trò không thể kế thừa chính nó.");
        }

        var existing = await _dbContext.AppRoleInheritances.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
        _dbContext.AppRoleInheritances.RemoveRange(existing);
        foreach (var parentRoleId in parentRoleIds.Distinct())
        {
            _dbContext.AppRoleInheritances.Add(new AppRoleInheritance
            {
                RoleId = roleId,
                ParentRoleId = parentRoleId,
                CreatedBy = _currentUserService.CurrentUser?.UserName
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _permissionCache.Clear();
        await AuditPermissionChangeAsync("Permission.Role.Inheritance", $"Cập nhật kế thừa vai trò {roleId}.", cancellationToken);
    }

    public async Task<IReadOnlyList<DataPermissionDto>> GetDataPermissionsAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AppDataPermissions.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.SubjectType == subjectType && x.SubjectId == subjectId)
            .OrderBy(x => x.ScopeType)
            .Select(x => new DataPermissionDto
            {
                Id = x.Id,
                SubjectType = x.SubjectType,
                SubjectId = x.SubjectId,
                ScopeType = x.ScopeType,
                ScopeId = x.ScopeId,
                IsGranted = x.IsGranted,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task SetDataPermissionAsync(DataPermissionDto permission, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AppDataPermissions.IgnoreQueryFilters().FirstOrDefaultAsync(x =>
            x.SubjectType == permission.SubjectType
            && x.SubjectId == permission.SubjectId
            && x.ScopeType == permission.ScopeType
            && x.ScopeId == permission.ScopeId
            && x.CompanyId == permission.CompanyId
            && x.BranchId == permission.BranchId, cancellationToken);

        if (entity is null)
        {
            entity = new AppDataPermission
            {
                SubjectType = permission.SubjectType,
                SubjectId = permission.SubjectId,
                ScopeType = permission.ScopeType,
                ScopeId = permission.ScopeId,
                CompanyId = permission.CompanyId,
                BranchId = permission.BranchId,
                CreatedBy = _currentUserService.CurrentUser?.UserName
            };
            _dbContext.AppDataPermissions.Add(entity);
        }

        entity.IsGranted = permission.IsGranted;
        await _dbContext.SaveChangesAsync(cancellationToken);
        _accessCache.Clear();
        await AuditPermissionChangeAsync("Permission.Data.Update", $"Cập nhật quyền dữ liệu {permission.ScopeType}:{permission.ScopeId}.", cancellationToken);
    }

    public async Task<EffectivePermissionPreviewDto> PreviewEffectivePermissionsAsync(Guid userId, Guid? companyId = null, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var roleCodes = await _dbContext.AppUserRoles.AsNoTracking()
            .Where(x => x.UserId == userId && x.Role != null)
            .Select(x => x.Role!.Code)
            .ToListAsync(cancellationToken);
        var permissions = await GetCachedPermissionsAsync(userId, roleCodes, companyId, branchId, cancellationToken);
        var denied = await _dbContext.AppUserPermissions.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsGranted)
            .Select(x => x.PermissionCode)
            .ToListAsync(cancellationToken);
        var deniedSet = new HashSet<string>(denied, StringComparer.OrdinalIgnoreCase);
        var catalog = await _dbContext.AppPermissions.AsNoTracking().OrderBy(x => x.Group).ThenBy(x => x.Code).ToListAsync(cancellationToken);

        return new EffectivePermissionPreviewDto
        {
            UserId = userId,
            Roles = roleCodes,
            Matrix = catalog.Select(x =>
            {
                var split = SplitPermissionCode(x.Code);
                var isDenied = deniedSet.Contains(x.Code);
                var isGranted = permissions.Contains(x.Code);
                return new PermissionMatrixRowDto
                {
                    Module = split.Module,
                    Action = split.Action,
                    PermissionCode = x.Code,
                    IsGranted = isGranted,
                    IsDenied = isDenied,
                    Reason = isDenied ? "Bị deny trực tiếp." : isGranted ? "Có hiệu lực." : "Không có hiệu lực."
                };
            }).ToArray(),
            DataPermissions = await GetDataPermissionsAsync("User", userId, cancellationToken)
        };
    }

    public async Task<bool> HasDataAccessAsync(string scopeType, Guid scopeId, Guid? companyId = null, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var user = _currentUserService.CurrentUser;
        if (user is null)
        {
            await AuditDeniedAsync($"{scopeType}:{scopeId}", "Người dùng chưa đăng nhập.", cancellationToken);
            return false;
        }

        if (user.Roles.Any(x => string.Equals(x, "Administrator", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var roleIds = await GetRoleIdsForUserAsync(user.Id, user.Roles, cancellationToken);
        var rows = await _dbContext.AppDataPermissions.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.ScopeType == scopeType
                && x.ScopeId == scopeId
                && (x.CompanyId == null || x.CompanyId == companyId)
                && (branchId == null || x.BranchId == null || x.BranchId == branchId)
                && ((x.SubjectType == "User" && x.SubjectId == user.Id) || (x.SubjectType == "Role" && roleIds.Contains(x.SubjectId))))
            .ToListAsync(cancellationToken);

        if (rows.Any(x => !x.IsGranted))
        {
            await AuditDeniedAsync($"{scopeType}:{scopeId}", "Bị deny override ở quyền dữ liệu.", cancellationToken);
            return false;
        }

        return rows.Count == 0 || rows.Any(x => x.IsGranted);
    }

    private async Task<HashSet<string>> GetCachedPermissionsAsync(Guid userId, IReadOnlyList<string> roleCodes, Guid? companyId, Guid? branchId, CancellationToken cancellationToken)
    {
        var cacheKey = BuildScopeCacheKey(userId, companyId, branchId);
        if (_permissionCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var roleIds = await GetRoleIdsForUserAsync(userId, roleCodes, cancellationToken);
        var rolePermissions = await _dbContext.AppRolePermissions
            .AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => x.PermissionCode)
            .ToListAsync(cancellationToken);

        var directPermissions = await _dbContext.AppUserPermissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsGranted
                && (x.CompanyId == null || x.CompanyId == companyId)
                && (branchId == null || x.BranchId == null || x.BranchId == branchId))
            .Select(x => x.PermissionCode)
            .ToListAsync(cancellationToken);

        var deniedPermissions = await _dbContext.AppUserPermissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsGranted
                && (x.CompanyId == null || x.CompanyId == companyId)
                && (branchId == null || x.BranchId == null || x.BranchId == branchId))
            .Select(x => x.PermissionCode)
            .ToListAsync(cancellationToken);

        var merged = new HashSet<string>(rolePermissions.Concat(directPermissions), StringComparer.OrdinalIgnoreCase);
        merged.ExceptWith(deniedPermissions);
        _permissionCache[cacheKey] = merged;
        return merged;
    }

    private async Task<bool> HasCompanyAccessAsync(Guid userId, Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
        {
            return true;
        }

        var cacheKey = $"{userId:N}|company|{companyId:N}";
        if (_accessCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var accessRows = await _dbContext.UserCompanyAccesses.AsNoTracking().Where(x => x.UserId == userId && x.IsActive).ToListAsync(cancellationToken);
        var allowed = accessRows.Count == 0
            ? companyId == CurrentCompanyContext.DefaultCompanyId
            : accessRows.Any(x => x.CompanyId == companyId.Value);
        _accessCache[cacheKey] = allowed;
        return allowed;
    }

    private async Task<bool> HasBranchAccessAsync(Guid userId, Guid? companyId, Guid branchId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{userId:N}|branch|{companyId:N}|{branchId:N}";
        if (_accessCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var branchRows = await _dbContext.UserBranchAccesses.AsNoTracking().Where(x => x.UserId == userId && x.IsActive).ToListAsync(cancellationToken);
        var allowed = branchRows.Count == 0
            ? branchId == CurrentCompanyContext.DefaultBranchId
            : branchRows.Any(x => x.CompanyId == companyId && x.BranchId == branchId);
        _accessCache[cacheKey] = allowed;
        return allowed;
    }

    private async Task<HashSet<Guid>> GetRoleIdsForUserAsync(Guid userId, IReadOnlyList<string> roleCodes, CancellationToken cancellationToken)
    {
        var normalizedRoles = roleCodes.SelectMany(ExpandRoleCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var roleIds = await _dbContext.AppRoles
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsArchived && normalizedRoles.Contains(x.Code))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var assignedRoleIds = await _dbContext.AppUserRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Role != null && x.Role.IsActive && !x.Role.IsArchived)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        var result = roleIds.Concat(assignedRoleIds).ToHashSet();
        var changed = true;
        while (changed)
        {
            changed = false;
            var parents = await _dbContext.AppRoleInheritances
                .AsNoTracking()
                .Where(x => x.IsActive && result.Contains(x.RoleId))
                .Select(x => x.ParentRoleId)
                .ToListAsync(cancellationToken);
            foreach (var parent in parents)
            {
                changed |= result.Add(parent);
            }
        }

        return result;
    }

    private async Task AuditPermissionChangeAsync(string action, string detail, CancellationToken cancellationToken)
    {
        if (_auditLogService is null)
        {
            return;
        }

        await _auditLogService.LogAsync(action, detail, _currentUserService.CurrentUser?.UserName ?? "system", cancellationToken);
    }

    private async Task AuditDeniedAsync(string target, string reason, CancellationToken cancellationToken)
    {
        if (_auditLogService is null)
        {
            return;
        }

        await _auditLogService.LogAsync("Permission.Denied", $"{target}: {reason}", _currentUserService.CurrentUser?.UserName ?? "anonymous", cancellationToken);
    }

    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
        if (_seedChecked && await HasSeedDataAsync(cancellationToken))
        {
            return;
        }

        await SeedGate.WaitAsync(cancellationToken);
        try
        {
            if (_seedChecked && await HasSeedDataAsync(cancellationToken))
            {
                return;
            }

            var permissions = DefaultPermissions();
            foreach (var permission in permissions)
            {
                if (!await _dbContext.AppPermissions.AnyAsync(x => x.Code == permission.Code, cancellationToken))
                {
                    _dbContext.AppPermissions.Add(permission);
                }
            }

            var hasRoles = await _dbContext.AppRoles.AnyAsync(cancellationToken);
            if (!hasRoles)
            {
                var roles = new[]
                {
                    new AppRole { Code = "Administrator", Name = "Administrator", Description = "Full enterprise access", IsSystem = true },
                    new AppRole { Code = "Accountant", Name = "Accountant", Description = "Accounting and reporting workflow", IsSystem = true },
                    new AppRole { Code = "Warehouse", Name = "Warehouse", Description = "Inventory workflow", IsSystem = true },
                    new AppRole { Code = "Viewer", Name = "Viewer", Description = "Read-only enterprise workspace", IsSystem = true }
                };

                _dbContext.AppRoles.AddRange(roles);
                foreach (var permission in permissions)
                {
                    _dbContext.AppRolePermissions.Add(new AppRolePermission { RoleId = roles[0].Id, PermissionCode = permission.Code });
                }

                foreach (var permission in permissions.Where(x => x.Code.Contains(".view", StringComparison.OrdinalIgnoreCase) || x.Code == "reports.view"))
                {
                    _dbContext.AppRolePermissions.Add(new AppRolePermission { RoleId = roles[3].Id, PermissionCode = permission.Code });
                }

                foreach (var permission in permissions.Where(x => x.Code.StartsWith("inventory.", StringComparison.OrdinalIgnoreCase) || x.Code == "master-data.view"))
                {
                    _dbContext.AppRolePermissions.Add(new AppRolePermission { RoleId = roles[2].Id, PermissionCode = permission.Code });
                }

                foreach (var permission in permissions.Where(x => !x.Code.StartsWith("security.", StringComparison.OrdinalIgnoreCase)))
                {
                    _dbContext.AppRolePermissions.Add(new AppRolePermission { RoleId = roles[1].Id, PermissionCode = permission.Code });
                }
            }
            else
            {
                var administratorRole = await _dbContext.AppRoles.FirstOrDefaultAsync(x => x.Code == "Administrator", cancellationToken);
                if (administratorRole is not null)
                {
                    foreach (var permission in permissions)
                    {
                        if (!await _dbContext.AppRolePermissions.AnyAsync(x => x.RoleId == administratorRole.Id && x.PermissionCode == permission.Code, cancellationToken))
                        {
                            _dbContext.AppRolePermissions.Add(new AppRolePermission { RoleId = administratorRole.Id, PermissionCode = permission.Code });
                        }
                    }
                }
            }

            await EnsureCurrentUserMirroredAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _seedChecked = true;
        }
        finally
        {
            SeedGate.Release();
        }
    }

    private async Task<bool> HasSeedDataAsync(CancellationToken cancellationToken)
    {
        var hasPermissions = await _dbContext.AppPermissions.AsNoTracking().AnyAsync(cancellationToken);
        var hasRoles = await _dbContext.AppRoles.AsNoTracking().AnyAsync(cancellationToken);
        return hasPermissions && hasRoles;
    }

    private async Task EnsureCurrentUserMirroredAndSavedAsync(CancellationToken cancellationToken)
    {
        await SeedGate.WaitAsync(cancellationToken);
        try
        {
            await EnsureCurrentUserMirroredAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            SeedGate.Release();
        }
    }

    private async Task EnsureCurrentUserMirroredAsync(CancellationToken cancellationToken)
    {
        var current = _currentUserService.CurrentUser;
        if (current is null)
        {
            return;
        }

        var appUser = await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.Id == current.Id, cancellationToken);
        if (appUser is null)
        {
            appUser = await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.UserName == current.UserName, cancellationToken);
            if (appUser is null)
            {
                appUser = new AppUser { Id = current.Id, UserName = current.UserName, DisplayName = current.DisplayName, Email = current.Email, IsActive = current.IsActive };
                _dbContext.AppUsers.Add(appUser);
            }
        }

        if (appUser.Id == current.Id)
        {
            appUser.UserName = current.UserName;
        }

        appUser.DisplayName = current.DisplayName;
        appUser.Email = current.Email;
        appUser.IsActive = current.IsActive;
        appUser.UpdatedAt = DateTime.UtcNow;

        foreach (var roleCode in current.Roles.SelectMany(ExpandRoleCode).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var role = _dbContext.AppRoles.Local.FirstOrDefault(x => string.Equals(x.Code, roleCode, StringComparison.OrdinalIgnoreCase))
                ?? await _dbContext.AppRoles.FirstOrDefaultAsync(x => x.Code == roleCode, cancellationToken);
            if (role is not null && !await _dbContext.AppUserRoles.AnyAsync(x => x.UserId == appUser.Id && x.RoleId == role.Id, cancellationToken))
            {
                _dbContext.AppUserRoles.Add(new AppUserRole { UserId = appUser.Id, RoleId = role.Id });
            }
        }

        if (!await _dbContext.UserCompanyAccesses.AnyAsync(x => x.UserId == appUser.Id && x.CompanyId == CurrentCompanyContext.DefaultCompanyId, cancellationToken))
        {
            _dbContext.UserCompanyAccesses.Add(new UserCompanyAccess
            {
                UserId = appUser.Id,
                CompanyId = CurrentCompanyContext.DefaultCompanyId,
                IsDefault = true,
                IsActive = true
            });
        }

        if (!await _dbContext.UserBranchAccesses.AnyAsync(x => x.UserId == appUser.Id && x.BranchId == CurrentCompanyContext.DefaultBranchId, cancellationToken))
        {
            _dbContext.UserBranchAccesses.Add(new UserBranchAccess
            {
                UserId = appUser.Id,
                CompanyId = CurrentCompanyContext.DefaultCompanyId,
                BranchId = CurrentCompanyContext.DefaultBranchId,
                IsDefault = true,
                IsActive = true
            });
        }
    }

    private static string BuildScopeCacheKey(Guid userId, Guid? companyId, Guid? branchId)
    {
        return $"{userId:N}|{companyId?.ToString("N") ?? "global"}|{branchId?.ToString("N") ?? "all"}";
    }

    private static string BuildScopeLabel(Guid? companyId, Guid? branchId)
    {
        var companyText = companyId?.ToString("N") ?? "toàn cục";
        var branchText = branchId?.ToString("N") ?? "toàn bộ chi nhánh";
        return $"{companyText} / {branchText}";
    }

    private static (string Module, string Action) SplitPermissionCode(string permissionCode)
    {
        var parts = permissionCode.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : (permissionCode, string.Empty);
    }

    private static AppPermission[] DefaultPermissions()
    {
        var data = new[]
        {
            ("dashboard.view", "View dashboard", "Shell", "Open operational dashboard"),
            ("virtual-file-center.view", "View Virtual File Center", "Shell", "Open virtual file center"),
            ("inventory.view", "View inventory", "Inventory", "Open inventory workflow"),
            ("inventory.approve", "Approve inventory", "Inventory", "Approve stock documents"),
            ("inventory.post", "Post inventory", "Inventory", "Post stock documents"),
            ("inventory.adjust", "Adjust inventory", "Inventory", "Create inventory adjustments"),
            ("receivable.view", "View receivables", "Accounting", "Open AR workflow"),
            ("receivable.manage", "Manage receivables", "Accounting", "Manage receivable transactions"),
            ("payable.view", "View payables", "Accounting", "Open AP workflow"),
            ("payable.manage", "Manage payables", "Accounting", "Manage payable transactions"),
            ("cash.view", "View cash", "CashBank", "Open cash workflow"),
            ("cash.post", "Post cash", "CashBank", "Post cash transactions"),
            ("bank.view", "View bank", "CashBank", "Open bank workflow"),
            ("bank.post", "Post bank", "CashBank", "Post bank transactions"),
            ("asset.view", "View assets", "Assets", "Open fixed/tool asset workflow"),
            ("asset.post", "Post assets", "Assets", "Post asset documents"),
            ("tax.view", "View tax", "Tax", "Open VAT/tax workflow"),
            ("tax.lock", "Lock tax declaration", "Tax", "Lock VAT declaration"),
            ("financial.view", "View financial statements", "Financial", "Open financial statements"),
            ("financial.close-period", "Close period", "Financial", "Close accounting period"),
            ("general-ledger.view", "View general ledger", "Accounting", "Open general ledger"),
            ("general-ledger.post", "Post journals", "Accounting", "Post journal entries"),
            ("master-data.view", "View master data", "MasterData", "Open customers/suppliers/items"),
            ("hr.view", "View HR", "HR", "Open HR module"),
            ("reports.view", "View reports", "Reports", "Open report center"),
            ("reports.export", "Export reports", "Reports", "Export reports"),
            ("settings.view", "View settings", "Settings", "Open settings"),
            ("security.view", "View security", "Security", "Open security admin"),
            ("security.admin", "Administer security", "Security", "Manage roles and permissions"),
            ("security.permissions.edit", "Edit permissions", "Security", "Edit role/user permissions"),
            ("security.data-permissions.edit", "Edit data permissions", "Security", "Edit company, branch, warehouse, cash fund and bank account data permissions"),
            ("operations.view", "View operations center", "Operations", "Open operations center"),
            ("audit.view", "View audit center", "Audit", "Open audit center"),
            ("training-data.admin", "Administer training data", "Operations", "Execute reset and seed training data operations"),
            ("menu-recipe.view", "View menu recipe module", "Sales", "Open menu and recipe module"),
            ("menu-recipe.edit", "Edit menu recipe data", "Sales", "Create and update menu items and recipe lines"),
            ("menu-recipe.cost-preview", "Preview menu cost", "Sales", "Preview expected cost and margin"),
            ("menu-recipe.deactivate", "Deactivate menu item", "Sales", "Set menu item inactive"),
            ("menu-recipe.admin", "Admin menu recipe module", "Sales", "Manage menu recipe configuration"),
            ("breakfast-cafe.view", "View breakfast cafe sales", "Sales", "Open breakfast & cafe sales module"),
            ("breakfast-cafe.sell", "Sell breakfast cafe order", "Sales", "Create order and add menu lines"),
            ("breakfast-cafe.cancel", "Cancel breakfast cafe order", "Sales", "Cancel unpaid orders"),
            ("breakfast-cafe.discount", "Discount breakfast cafe order", "Sales", "Apply order discount"),
            ("breakfast-cafe.admin", "Admin breakfast cafe module", "Sales", "Manage menu and operational settings"),
            ("restaurant.alacarte.view", "View À La Carte restaurant", "Sales", "Open À La Carte restaurant POS module"),
            ("restaurant.alacarte.manage", "Manage À La Carte restaurant", "Sales", "Manage rooms, tables, orders, quotes and services"),
            ("restaurant.banquet.view", "View banquet restaurant", "Sales", "Open banquet restaurant module"),
            ("restaurant.banquet.manage", "Manage banquet restaurant", "Sales", "Manage banquet events, contracts and deposits"),
            ("FrontOffice.Reservation.View", "Xem đặt phòng", "FrontOffice", "Mở workspace đặt phòng"),
            ("FrontOffice.Reservation.Create", "Tạo đặt phòng", "FrontOffice", "Tạo đặt phòng mới"),
            ("FrontOffice.Reservation.Update", "Cập nhật đặt phòng", "FrontOffice", "Cập nhật thông tin lưu trú và khách"),
            ("FrontOffice.Reservation.AssignRoom", "Phân phòng đặt phòng", "FrontOffice", "Phân hoặc bỏ phân phòng"),
            ("FrontOffice.Reservation.ChangeStatus", "Đổi trạng thái đặt phòng", "FrontOffice", "Tạm giữ, xác nhận hoặc no-show"),
            ("FrontOffice.Reservation.Cancel", "Hủy đặt phòng", "FrontOffice", "Hủy đặt phòng")
        };

        return data.Select(x => new AppPermission { Code = x.Item1, Name = x.Item2, Group = x.Item3, Description = x.Item4 }).ToArray();
    }

    private static IEnumerable<string> ExpandRoleCode(string roleCode)
    {
        yield return roleCode;
        if (string.Equals(roleCode, "Accounting", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Accountant";
        }
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().Replace(' ', '-');
    }

    private static bool IsDangerousPermission(string code)
    {
        return code.Contains("close-period", StringComparison.OrdinalIgnoreCase)
               || code.Contains("reverse", StringComparison.OrdinalIgnoreCase)
               || code.Contains("post", StringComparison.OrdinalIgnoreCase)
               || code.Contains("lock", StringComparison.OrdinalIgnoreCase)
               || code.Contains("admin", StringComparison.OrdinalIgnoreCase);
    }
}

