using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ASMILE.BO.Infrastructure.Security.Store;
using ASMILE.BO.Shared.Security;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class UserAccountService : IUserAccountService
{
    private readonly InMemorySecurityStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;
    private readonly AppDbContext? _dbContext;

    public UserAccountService(
        InMemorySecurityStore store,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLogService,
        AppDbContext? dbContext = null)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserAccountDto>> GetUsersAsync()
    {
        if (_dbContext is not null)
        {
            var users = await _dbContext.AppUsers.AsNoTracking().OrderBy(x => x.UserName).ToListAsync();
            var userIds = users.Select(x => x.Id).ToArray();
            var roles = await _dbContext.AppUserRoles
                .AsNoTracking()
                .Where(x => userIds.Contains(x.UserId))
                .Select(x => new { x.UserId, x.RoleId, RoleName = x.Role!.Name })
                .ToListAsync();

            return users.Select(x => Map(
                x,
                roles.Where(r => r.UserId == x.Id).Select(r => r.RoleName).ToList(),
                roles.Where(r => r.UserId == x.Id).Select(r => r.RoleId).ToList())).ToList();
        }

        var memoryUsers = _store.Users.Select(Map).ToList();
        return memoryUsers;
    }

    public async Task<UserAccountDto?> GetUserByIdAsync(Guid id)
    {
        if (_dbContext is not null)
        {
            var user = await _dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (user is null)
            {
                return null;
            }

            var roles = await _dbContext.AppUserRoles
                .AsNoTracking()
                .Where(x => x.UserId == id)
                .Select(x => new { x.RoleId, RoleName = x.Role!.Name })
                .ToListAsync();
            return Map(user, roles.Select(x => x.RoleName).ToList(), roles.Select(x => x.RoleId).ToList());
        }

        return _store.Users.Where(x => x.Id == id).Select(Map).FirstOrDefault();
    }

    public async Task<UserAccountDto> CreateUserAsync(CreateUserRequestDto request)
    {
        var userName = InputSanitizer.TrimAndLimit(request.UserName, 64);
        if (string.IsNullOrWhiteSpace(userName)) throw new InvalidOperationException("UserName không được để trống.");
        if (_store.Users.Any(x => x.NormalizedUserName == userName.ToUpperInvariant())
            || (_dbContext is not null && await _dbContext.AppUsers.AnyAsync(x => x.UserName == userName)))
        {
            throw new InvalidOperationException("UserName đã tồn tại.");
        }
        ValidatePassword(request.InitialPassword);

        var user = new UserAccount
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            DisplayName = InputSanitizer.TrimAndLimit(request.DisplayName, 128),
            Email = InputSanitizer.TrimAndLimit(request.Email, ValidationConstants.MaxEmailLength),
            Phone = InputSanitizer.TrimAndLimit(request.Phone, ValidationConstants.MaxPhoneLength),
            IsActive = request.IsActive,
            MustChangePassword = request.MustChangePassword,
            Notes = InputSanitizer.TrimAndLimit(request.Notes, ValidationConstants.MaxNameLength),
            PasswordHash = _passwordHasher.HashPassword(request.InitialPassword),
            CreatedBy = "Administrator"
        };
        _store.Users.Add(user);

        foreach (var roleId in request.RoleIds.Distinct())
        {
            _store.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await PersistUserAsync(user);

        await _auditLogService.LogAsync("CreateUser", $"Tạo user {user.UserName}", "security");
        return Map(user);
    }

    public async Task UpdateUserAsync(UpdateUserRequestDto request)
    {
        var user = _store.Users.FirstOrDefault(x => x.Id == request.UserId) ?? throw new InvalidOperationException("User không tồn tại.");
        user.DisplayName = InputSanitizer.TrimAndLimit(request.DisplayName, 128);
        user.Email = InputSanitizer.TrimAndLimit(request.Email, ValidationConstants.MaxEmailLength);
        user.Phone = InputSanitizer.TrimAndLimit(request.Phone, ValidationConstants.MaxPhoneLength);
        user.IsActive = request.IsActive;
        user.MustChangePassword = request.MustChangePassword;
        user.Notes = InputSanitizer.TrimAndLimit(request.Notes, ValidationConstants.MaxNameLength);
        user.UpdatedAt = DateTime.UtcNow;

        _store.UserRoles.RemoveAll(x => x.UserId == user.Id);
        foreach (var roleId in request.RoleIds.Distinct())
        {
            _store.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await PersistUserAsync(user);

        await _auditLogService.LogAsync("UpdateUser", $"Cập nhật user {user.UserName}", "security");
    }

    public async Task SetUserActiveAsync(Guid userId, bool isActive)
    {
        var user = _store.Users.FirstOrDefault(x => x.Id == userId) ?? throw new InvalidOperationException("User không tồn tại.");
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await PersistUserAsync(user);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, bool mustChangePassword)
    {
        ValidatePassword(newPassword);
        var user = _store.Users.FirstOrDefault(x => x.Id == userId) ?? throw new InvalidOperationException("User không tồn tại.");
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.MustChangePassword = mustChangePassword;
        user.LastPasswordChangedAt = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        user.IsLocked = false;
        await PersistUserAsync(user);
        await _auditLogService.LogAsync("ResetPassword", $"Reset mật khẩu user {user.UserName}", "security");
    }

    public async Task UnlockUserAsync(Guid userId)
    {
        var user = _store.Users.FirstOrDefault(x => x.Id == userId) ?? throw new InvalidOperationException("User không tồn tại.");
        user.IsLocked = false;
        user.FailedLoginCount = 0;
        await PersistUserAsync(user);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = _store.Users.FirstOrDefault(x => x.Id == userId) ?? throw new InvalidOperationException("User không tồn tại.");
        if (string.Equals(user.NormalizedUserName, "ADMINISTRATOR", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Không thể xóa tài khoản administrator.");
        }

        _store.Users.Remove(user);
        _store.UserRoles.RemoveAll(x => x.UserId == userId);
        if (_dbContext is not null)
        {
            var persisted = await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.Id == userId);
            if (persisted is not null)
            {
                _dbContext.AppUsers.Remove(persisted);
                await _dbContext.SaveChangesAsync();
            }
        }
        await _auditLogService.LogAsync("DeleteUser", $"Xóa user {user.UserName}", "security");
    }

    public async Task<IReadOnlyList<LoginAuditDto>> GetLoginAuditsAsync()
    {
        if (_dbContext is not null)
        {
            return await _dbContext.LoginAudits.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(200)
                .Select(x => new LoginAuditDto
                {
                    CreatedAt = x.CreatedAt,
                    UserName = x.UserName,
                    Success = x.Success,
                    FailureReason = x.FailureReason,
                    MachineName = x.MachineName,
                    IpAddress = x.IpAddress
                })
                .ToListAsync();
        }

        var data = _store.Audits.OrderByDescending(x => x.CreatedAt).Take(200)
            .Select(x => new LoginAuditDto
            {
                CreatedAt = x.CreatedAt,
                UserName = x.UserName,
                Success = x.Success,
                FailureReason = x.FailureReason,
                MachineName = x.MachineName,
                IpAddress = x.IpAddress
            }).ToList();

        return data;
    }

    private UserAccountDto Map(UserAccount user)
    {
        var roles = _store.UserRoles.Where(x => x.UserId == user.Id).Join(_store.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name).ToList();
        return new UserAccountDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Phone = user.Phone,
            IsActive = user.IsActive,
            IsLocked = user.IsLocked,
            MustChangePassword = user.MustChangePassword,
            FailedLoginCount = user.FailedLoginCount,
            LastLoginAt = user.LastLoginAt,
            Roles = roles,
            RoleIds = _store.UserRoles.Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToList(),
            Notes = user.Notes
        };
    }

    private static UserAccountDto Map(AppUser user, IReadOnlyList<string> roles, IReadOnlyList<Guid> roleIds)
    {
        return new UserAccountDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = user.IsActive,
            IsLocked = user.IsLocked,
            MustChangePassword = user.MustChangePassword,
            FailedLoginCount = user.FailedLoginCount,
            LastLoginAt = user.LastLoginAt,
            Roles = roles,
            RoleIds = roleIds,
            Notes = user.Notes
        };
    }

    private async Task PersistUserAsync(UserAccount user)
    {
        if (_dbContext is null)
        {
            return;
        }

        var persisted = await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.Id == user.Id);
        if (persisted is null)
        {
            persisted = new AppUser { Id = user.Id };
            _dbContext.AppUsers.Add(persisted);
        }

        persisted.UserName = user.UserName;
        persisted.DisplayName = user.DisplayName;
        persisted.Email = user.Email;
        persisted.PasswordHash = user.PasswordHash;
        persisted.IsActive = user.IsActive;
        persisted.IsLocked = user.IsLocked;
        persisted.MustChangePassword = user.MustChangePassword;
        persisted.FailedLoginCount = user.FailedLoginCount;
        persisted.LastLoginAt = user.LastLoginAt;
        persisted.LastPasswordChangedAt = user.LastPasswordChangedAt;
        persisted.Notes = user.Notes;
        persisted.UpdatedAt = DateTime.UtcNow;

        // SettingsViewModel receives role IDs from the DB-backed RoleService.
        // Persist those IDs directly; translating through the legacy in-memory
        // role catalog silently dropped custom roles and could remove all role
        // links after a restart when the two catalogs had different IDs.
        var requestedRoleIds = _store.UserRoles
            .Where(x => x.UserId == user.Id)
            .Select(x => x.RoleId)
            .Distinct()
            .ToArray();
        var persistedRoleIds = await _dbContext.AppRoles
            .Where(x => requestedRoleIds.Contains(x.Id) && x.IsActive && !x.IsArchived)
            .Select(x => x.Id)
            .ToListAsync();
        var currentRoleLinks = await _dbContext.AppUserRoles.Where(x => x.UserId == user.Id).ToListAsync();

        // A startup/login pass may still contain legacy in-memory role IDs. In
        // that case preserve the DB links instead of treating the stale catalog
        // as an intentional request to remove every role.
        if (requestedRoleIds.Length > 0 && persistedRoleIds.Count > 0)
        {
            var roleIdSet = persistedRoleIds.ToHashSet();
            _dbContext.AppUserRoles.RemoveRange(currentRoleLinks.Where(x => !roleIdSet.Contains(x.RoleId)));
            foreach (var roleId in roleIdSet.Where(id => currentRoleLinks.All(link => link.RoleId != id)))
            {
                _dbContext.AppUserRoles.Add(new AppUserRole { UserId = user.Id, RoleId = roleId });
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new InvalidOperationException("Mật khẩu phải tối thiểu 8 ký tự.");
        }

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        if (!(hasUpper && hasLower && hasDigit))
        {
            throw new InvalidOperationException("Mật khẩu phải có chữ hoa, chữ thường và số.");
        }
    }
}
