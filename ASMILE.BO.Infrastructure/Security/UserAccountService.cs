using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Security.Store;
using ASMILE.BO.Shared.Security;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class UserAccountService : IUserAccountService
{
    private readonly InMemorySecurityStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;

    public UserAccountService(InMemorySecurityStore store, IPasswordHasher passwordHasher, IAuditLogService auditLogService)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
    }

    public Task<IReadOnlyList<UserAccountDto>> GetUsersAsync()
    {
        var users = _store.Users.Select(Map).ToList();
        return Task.FromResult((IReadOnlyList<UserAccountDto>)users);
    }

    public Task<UserAccountDto?> GetUserByIdAsync(Guid id)
    {
        return Task.FromResult(_store.Users.Where(x => x.Id == id).Select(Map).FirstOrDefault());
    }

    public async Task<UserAccountDto> CreateUserAsync(CreateUserRequestDto request)
    {
        var userName = InputSanitizer.TrimAndLimit(request.UserName, 64);
        if (string.IsNullOrWhiteSpace(userName)) throw new InvalidOperationException("UserName không được để trống.");
        if (_store.Users.Any(x => x.NormalizedUserName == userName.ToUpperInvariant())) throw new InvalidOperationException("UserName đã tồn tại.");
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

        await _auditLogService.LogAsync("UpdateUser", $"Cập nhật user {user.UserName}", "security");
    }

    public Task SetUserActiveAsync(Guid userId, bool isActive)
    {
        var user = _store.Users.FirstOrDefault(x => x.Id == userId) ?? throw new InvalidOperationException("User không tồn tại.");
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
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
        await _auditLogService.LogAsync("ResetPassword", $"Reset mật khẩu user {user.UserName}", "security");
    }

    public Task UnlockUserAsync(Guid userId)
    {
        var user = _store.Users.FirstOrDefault(x => x.Id == userId) ?? throw new InvalidOperationException("User không tồn tại.");
        user.IsLocked = false;
        user.FailedLoginCount = 0;
        return Task.CompletedTask;
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
        await _auditLogService.LogAsync("DeleteUser", $"Xóa user {user.UserName}", "security");
    }

    public Task<IReadOnlyList<LoginAuditDto>> GetLoginAuditsAsync()
    {
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

        return Task.FromResult((IReadOnlyList<LoginAuditDto>)data);
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
            Notes = user.Notes
        };
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
