using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Infrastructure.Security.Store;
using ASMILE.BO.Shared.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly InMemorySecurityStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRoleService _roleService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAuditSessionService? _auditSessionService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppDbContext? _dbContext;
    private readonly ISettingsService? _settingsService;
    private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;

    public AuthenticationService(
        InMemorySecurityStore store,
        IPasswordHasher passwordHasher,
        IRoleService roleService,
        IAuditLogService auditLogService,
        IServiceScopeFactory scopeFactory,
        IAuditSessionService? auditSessionService = null,
        AppDbContext? dbContext = null,
        ISettingsService? settingsService = null,
        IDbContextFactory<AppDbContext>? dbContextFactory = null)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _roleService = roleService;
        _auditLogService = auditLogService;
        _auditSessionService = auditSessionService;
        _scopeFactory = scopeFactory;
        _dbContext = dbContext;
        _settingsService = settingsService;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request)
    {
        var userName = InputSanitizer.TrimAndLimit(request.UserName, 64);
        var normalized = userName.ToUpperInvariant();
        var user = await LoadPersistedUserAsync(userName, normalized);
        user ??= _store.Users.FirstOrDefault(x => x.NormalizedUserName == normalized);

        if (user is null)
        {
            await RecordLoginAuditAsync(new LoginAudit { UserName = userName, Success = false, FailureReason = "Invalid credentials", MachineName = request.MachineName, IpAddress = request.IpAddress });
            await _auditLogService.LogAsync("LoginFailed", $"Login failed for '{userName}'", "security");
            return Fail();
        }

        if (!user.IsActive || user.IsLocked)
        {
            await RecordLoginAuditAsync(new LoginAudit { UserName = userName, UserId = user.Id, Success = false, FailureReason = user.IsLocked ? "Locked" : "Inactive", MachineName = request.MachineName, IpAddress = request.IpAddress });
            return Fail();
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            var maxFailedAttempts = await GetSecuritySettingIntAsync("Security.MaxFailedLoginAttempts", 5, 3, 10);
            if (user.FailedLoginCount >= maxFailedAttempts)
            {
                user.IsLocked = true;
            }

            await PersistUserAsync(user);

            await RecordLoginAuditAsync(new LoginAudit { UserName = userName, UserId = user.Id, Success = false, FailureReason = "Invalid credentials", MachineName = request.MachineName, IpAddress = request.IpAddress });
            await _auditLogService.LogAsync("LoginFailed", $"Login failed for '{userName}'", "security");
            return Fail();
        }

        var permissions = await _roleService.GetUserPermissionCodesAsync(user.Id);
        if (await IsMaintenanceModeEnabledAsync()
            && !permissions.Any(x => string.Equals(x, "System.Maintenance", StringComparison.OrdinalIgnoreCase)))
        {
            await RecordLoginAuditAsync(new LoginAudit
            {
                UserName = userName,
                UserId = user.Id,
                Success = false,
                FailureReason = "Maintenance mode",
                MachineName = request.MachineName,
                IpAddress = request.IpAddress
            });
            await _auditLogService.LogAsync("LoginBlockedMaintenance", $"Login blocked by maintenance mode for '{userName}'", "security");
            return Fail("Hệ thống đang bảo trì; hiện chỉ tài khoản có quyền bảo trì mới được đăng nhập.");
        }

        user.FailedLoginCount = 0;
        user.LastLoginAt = DateTime.UtcNow;
        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = _passwordHasher.HashPassword(request.Password);
        }

        await PersistUserAsync(user);

        var sessionTimeoutMinutes = await GetSecuritySettingIntAsync("Security.SessionTimeoutMinutes", 30, 5, 1440);
        var session = new LoginSession
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            MachineName = request.MachineName,
            IpAddress = request.IpAddress,
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes),
            IsActive = true
        };
        _store.Sessions.Add(session);
        await PersistSessionAsync(session);

        var roles = _dbContext is not null
            ? await _dbContext.AppUserRoles
                .AsNoTracking()
                .Where(x => x.UserId == user.Id && x.Role != null && x.Role.IsActive && !x.Role.IsArchived)
                .Select(x => x.Role!.Name)
                .Distinct()
                .ToListAsync()
            : _store.UserRoles.Where(x => x.UserId == user.Id)
                .Join(_store.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name).Distinct().ToList();
        await RecordLoginAuditAsync(new LoginAudit { UserName = userName, UserId = user.Id, Success = true, MachineName = request.MachineName, IpAddress = request.IpAddress });
        await _auditLogService.LogAsync("LoginSuccess", $"Login success for '{userName}'", "security");
        if (_auditSessionService is not null)
        {
            await _auditSessionService.StartSessionAsync("Authentication.Login", session.Token, request.MachineName, request.MachineName, Environment.OSVersion.Platform.ToString(), cancellationToken: default);
        }

        await UpsertActiveCompanySessionAsync(user.Id, session.Id, request, session.ExpiresAt, cancellationToken: default);

        return new LoginResultDto
        {
            Success = true,
            UserId = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            SessionId = session.Id,
            Roles = roles,
            PermissionCodes = permissions,
            MustChangePassword = user.MustChangePassword
        };
    }

    public async Task LogoutAsync(Guid sessionId)
    {
        var session = _store.Sessions.FirstOrDefault(x => x.Id == sessionId);
        if (session is null && _dbContext is not null)
        {
            var persisted = await _dbContext.LoginSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sessionId);
            if (persisted is not null)
            {
                session = new LoginSession
                {
                    Id = persisted.Id,
                    UserId = persisted.UserId,
                    TokenHash = persisted.TokenHash,
                    MachineName = persisted.MachineName,
                    IpAddress = persisted.IpAddress,
                    StartedAt = persisted.StartedAt,
                    ExpiresAt = persisted.ExpiresAt,
                    EndedAt = persisted.EndedAt,
                    IsActive = persisted.IsActive
                };
                _store.Sessions.Add(session);
            }
        }
        if (session is not null)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            await PersistSessionAsync(session);
            if (_auditSessionService is not null)
            {
                await _auditSessionService.CompleteSessionAsync(session.Token, "Logout");
            }

            await CompleteActiveCompanySessionAsync(session.UserId, session.Id, "Completed");
        }
    }

    public async Task<bool> ValidateSessionAsync(Guid sessionId)
    {
        if (_dbContextFactory is not null)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var session = await db.LoginSessions.AsNoTracking()
                .Where(x => x.Id == sessionId)
                .Select(x => new { x.UserId, x.IsActive, x.ExpiresAt })
                .FirstOrDefaultAsync();
            if (session is null || !session.IsActive || session.ExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            if (!await IsAccountEligibleAsync(db, session.UserId))
            {
                return false;
            }

            if (await IsAllowedDuringMaintenanceAsync(db, session.UserId))
            {
                return true;
            }

            await RevokePersistedSessionAsync(db, sessionId, "MaintenanceMode");
            return false;
        }

        if (_dbContext is not null)
        {
            var session = await _dbContext.LoginSessions.AsNoTracking()
                .Where(x => x.Id == sessionId)
                .Select(x => new { x.UserId, x.IsActive, x.ExpiresAt })
                .FirstOrDefaultAsync();
            if (session is null || !session.IsActive || session.ExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            if (!await IsAccountEligibleAsync(_dbContext, session.UserId))
            {
                return false;
            }

            if (await IsAllowedDuringMaintenanceAsync(_dbContext, session.UserId))
            {
                return true;
            }

            await RevokePersistedSessionAsync(_dbContext, sessionId, "MaintenanceMode");
            return false;
        }

        var inMemorySession = _store.Sessions.FirstOrDefault(x => x.Id == sessionId);
        if (inMemorySession is null || !inMemorySession.IsActive || inMemorySession.ExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        var inMemoryUser = _store.Users.FirstOrDefault(x => x.Id == inMemorySession.UserId);
        if (inMemoryUser is null || !inMemoryUser.IsActive || inMemoryUser.IsLocked)
        {
            return false;
        }

        if (!await IsMaintenanceModeEnabledAsync())
        {
            return true;
        }

        var permissions = await _roleService.GetUserPermissionCodesAsync(inMemorySession.UserId);
        if (permissions.Any(x => string.Equals(x, "System.Maintenance", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        RevokeInMemorySession(inMemorySession);
        return false;
    }

    public async Task<IReadOnlyList<string>?> GetSessionPermissionCodesAsync(Guid sessionId)
    {
        if (_dbContextFactory is not null)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await GetPersistedSessionPermissionCodesAsync(db, sessionId);
        }

        if (_dbContext is not null)
        {
            return await GetPersistedSessionPermissionCodesAsync(_dbContext, sessionId);
        }

        var session = _store.Sessions.FirstOrDefault(x => x.Id == sessionId);
        if (session is null || !session.IsActive || session.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        var user = _store.Users.FirstOrDefault(x => x.Id == session.UserId);
        if (user is null || !user.IsActive || user.IsLocked)
        {
            return null;
        }

        if (await IsMaintenanceModeEnabledAsync())
        {
            var permissions = await _roleService.GetUserPermissionCodesAsync(session.UserId);
            if (!permissions.Any(x => string.Equals(x, "System.Maintenance", StringComparison.OrdinalIgnoreCase)))
            {
                RevokeInMemorySession(session);
                return null;
            }
        }

        return await _roleService.GetUserPermissionCodesAsync(session.UserId);
    }

    public async Task<bool> RenewSessionAsync(Guid sessionId)
    {
        if (_dbContextFactory is not null)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await RenewPersistedSessionAsync(db, sessionId);
        }

        if (_dbContext is not null)
        {
            return await RenewPersistedSessionAsync(_dbContext, sessionId);
        }

        var session = _store.Sessions.FirstOrDefault(x => x.Id == sessionId);
        if (session is null || !session.IsActive || session.ExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        var user = _store.Users.FirstOrDefault(x => x.Id == session.UserId);
        if (user is null || !user.IsActive || user.IsLocked)
        {
            return false;
        }

        if (await IsMaintenanceModeEnabledAsync())
        {
            var permissions = await _roleService.GetUserPermissionCodesAsync(session.UserId);
            if (!permissions.Any(x => string.Equals(x, "System.Maintenance", StringComparison.OrdinalIgnoreCase)))
            {
                RevokeInMemorySession(session);
                return false;
            }
        }

        var timeoutMinutes = await GetSecuritySettingIntAsync("Security.SessionTimeoutMinutes", 30, 5, 1440);
        var now = DateTime.UtcNow;
        session.ExpiresAt = now.AddMinutes(timeoutMinutes);
        return true;
    }

    private async Task<bool> RenewPersistedSessionAsync(AppDbContext db, Guid sessionId)
    {
        var session = await db.LoginSessions.FirstOrDefaultAsync(x => x.Id == sessionId);
        var now = DateTime.UtcNow;
        if (session is null || !session.IsActive || session.ExpiresAt <= now)
        {
            return false;
        }

        if (!await IsAccountEligibleAsync(db, session.UserId))
        {
            return false;
        }

        if (!await IsAllowedDuringMaintenanceAsync(db, session.UserId))
        {
            await RevokePersistedSessionAsync(db, sessionId, "MaintenanceMode");
            return false;
        }

        var timeoutMinutes = await GetSecuritySettingIntAsync("Security.SessionTimeoutMinutes", 30, 5, 1440, db);
        var expiresAt = now.AddMinutes(timeoutMinutes);
        session.ExpiresAt = expiresAt;

        var activeCompanySessions = await db.ActiveCompanySessions.IgnoreQueryFilters()
            .Where(x => x.SessionId == sessionId && x.UserId == session.UserId && x.IsActive && !x.IsArchived)
            .ToListAsync();
        foreach (var activeSession in activeCompanySessions)
        {
            activeSession.LastActivityAtUtc = now;
            activeSession.ExpiresAtUtc = expiresAt;
            activeSession.ModifiedAtUtc = now;
        }

        await db.SaveChangesAsync();

        var inMemorySession = _store.Sessions.FirstOrDefault(x => x.Id == sessionId);
        if (inMemorySession is not null)
        {
            inMemorySession.ExpiresAt = expiresAt;
        }

        return true;
    }

    private static Task<bool> IsAccountEligibleAsync(AppDbContext db, Guid userId)
    {
        return db.AppUsers.AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.IsActive && !x.IsLocked);
    }

    private async Task<IReadOnlyList<string>?> GetPersistedSessionPermissionCodesAsync(AppDbContext db, Guid sessionId)
    {
        var session = await db.LoginSessions.AsNoTracking()
            .Where(x => x.Id == sessionId)
            .Select(x => new { x.UserId, x.IsActive, x.ExpiresAt })
            .FirstOrDefaultAsync();
        if (session is null || !session.IsActive || session.ExpiresAt <= DateTime.UtcNow
            || !await IsAccountEligibleAsync(db, session.UserId))
        {
            return null;
        }

        if (!await IsAllowedDuringMaintenanceAsync(db, session.UserId))
        {
            await RevokePersistedSessionAsync(db, sessionId, "MaintenanceMode");
            return null;
        }

        var activeRoleIds = await db.AppUserRoles.AsNoTracking()
            .Where(x => x.UserId == session.UserId)
            .Join(
                db.AppRoles.AsNoTracking().Where(role => role.IsActive && !role.IsArchived),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, _) => userRole.RoleId)
            .ToArrayAsync();
        if (activeRoleIds.Length == 0)
        {
            return Array.Empty<string>();
        }

        return await db.AppRolePermissions.AsNoTracking()
            .Where(x => activeRoleIds.Contains(x.RoleId))
            .Select(x => x.PermissionCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    private async Task<bool> IsAllowedDuringMaintenanceAsync(AppDbContext db, Guid userId)
    {
        if (!await IsMaintenanceModeEnabledAsync(db))
        {
            return true;
        }

        var roleIds = await db.AppUserRoles.AsNoTracking()
            .Where(x => x.UserId == userId && x.Role != null && x.Role.IsActive && !x.Role.IsArchived)
            .Select(x => x.RoleId)
            .ToArrayAsync();
        if (roleIds.Length == 0)
        {
            return false;
        }

        return await db.AppRolePermissions.AsNoTracking()
            .AnyAsync(x => roleIds.Contains(x.RoleId) && x.PermissionCode == "System.Maintenance");
    }

    private async Task RevokePersistedSessionAsync(AppDbContext db, Guid sessionId, string reason)
    {
        var session = await db.LoginSessions.FirstOrDefaultAsync(x => x.Id == sessionId);
        if (session is null || !session.IsActive)
        {
            return;
        }

        var now = DateTime.UtcNow;
        session.IsActive = false;
        session.EndedAt = now;

        var activeCompanySessions = await db.ActiveCompanySessions.IgnoreQueryFilters()
            .Where(x => x.SessionId == sessionId && x.UserId == session.UserId && x.IsActive && !x.IsArchived)
            .ToListAsync();
        foreach (var activeCompanySession in activeCompanySessions)
        {
            activeCompanySession.IsActive = false;
            activeCompanySession.Status = reason;
            activeCompanySession.CompletedAtUtc = now;
            activeCompanySession.ModifiedAtUtc = now;
        }

        await db.SaveChangesAsync();
        await _auditLogService.LogAsync(
            "SessionRevoked",
            $"Login session '{sessionId}' for user '{session.UserId}' was revoked by '{reason}' policy.",
            "system");

        var cachedSession = _store.Sessions.FirstOrDefault(x => x.Id == sessionId);
        if (cachedSession is not null)
        {
            cachedSession.IsActive = false;
            cachedSession.EndedAt = now;
        }
    }

    private static void RevokeInMemorySession(LoginSession session)
    {
        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
    }

    public async Task ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        var user = _store.Users.FirstOrDefault(x => x.Id == request.UserId) ?? throw new InvalidOperationException("User không tồn tại.");
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        ValidateNewPassword(request.NewPassword);
        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.LastPasswordChangedAt = DateTime.UtcNow;
        await PersistUserAsync(user);
        await _auditLogService.LogAsync("PasswordChanged", $"Password changed for user '{user.UserName}'", "security");
    }

    private static void ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new InvalidOperationException("Mật khẩu phải tối thiểu 8 ký tự.");
        }

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
        {
            throw new InvalidOperationException("Mật khẩu phải có chữ hoa, chữ thường và số.");
        }
    }

    private async Task<UserAccount?> LoadPersistedUserAsync(string userName, string normalized)
    {
        if (_dbContext is null)
        {
            return null;
        }

        var persisted = await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.UserName == userName);
        if (persisted is null)
        {
            return null;
        }

        var user = _store.Users.FirstOrDefault(x => x.Id == persisted.Id)
            ?? _store.Users.FirstOrDefault(x => x.NormalizedUserName == normalized);
        if (user is null)
        {
            user = new UserAccount { Id = persisted.Id };
            _store.Users.Add(user);
        }

        user.Id = persisted.Id;
        user.UserName = persisted.UserName;
        user.NormalizedUserName = persisted.UserName.ToUpperInvariant();
        user.DisplayName = persisted.DisplayName;
        user.Email = persisted.Email;
        user.PasswordHash = persisted.PasswordHash;
        user.IsActive = persisted.IsActive;
        user.IsLocked = persisted.IsLocked;
        user.MustChangePassword = persisted.MustChangePassword;
        user.FailedLoginCount = persisted.FailedLoginCount;
        user.LastLoginAt = persisted.LastLoginAt;
        user.LastPasswordChangedAt = persisted.LastPasswordChangedAt;
        user.Notes = persisted.Notes;
        return user;
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
        await _dbContext.SaveChangesAsync();
    }

    private async Task RecordLoginAuditAsync(LoginAudit audit)
    {
        _store.Audits.Add(audit);
        if (_dbContext is null)
        {
            return;
        }

        _dbContext.LoginAudits.Add(new LoginAudit
        {
            Id = audit.Id,
            UserName = audit.UserName,
            UserId = audit.UserId,
            Success = audit.Success,
            FailureReason = audit.FailureReason,
            MachineName = audit.MachineName,
            IpAddress = audit.IpAddress,
            CreatedAt = audit.CreatedAt
        });
        await _dbContext.SaveChangesAsync();
    }

    private async Task PersistSessionAsync(LoginSession session)
    {
        if (_dbContext is null)
        {
            return;
        }

        var persisted = await _dbContext.LoginSessions.FirstOrDefaultAsync(x => x.Id == session.Id);
        if (persisted is null)
        {
            persisted = new LoginSession { Id = session.Id };
            _dbContext.LoginSessions.Add(persisted);
        }

        persisted.UserId = session.UserId;
        persisted.TokenHash = string.IsNullOrWhiteSpace(session.Token)
            ? session.TokenHash
            : ComputeTokenHash(session.Token);
        persisted.MachineName = session.MachineName;
        persisted.IpAddress = session.IpAddress;
        persisted.StartedAt = session.StartedAt;
        persisted.ExpiresAt = session.ExpiresAt;
        persisted.EndedAt = session.EndedAt;
        persisted.IsActive = session.IsActive;
        session.TokenHash = persisted.TokenHash;
        await _dbContext.SaveChangesAsync();
    }

    private async Task<int> GetSecuritySettingIntAsync(string key, int defaultValue, int minimum, int maximum, AppDbContext? db = null)
    {
        string? value;
        if (db is not null)
        {
            value = await db.SettingValues.AsNoTracking()
                .Where(x => x.DefinitionKey == key && x.Scope == SettingScope.System && x.ScopeTarget == null)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();
        }
        else if (_settingsService is not null)
        {
            value = (await _settingsService.GetValueAsync(key, SettingScope.System, null))?.Value;
        }
        else
        {
            return defaultValue;
        }

        return int.TryParse(value, out var parsed)
            ? Math.Clamp(parsed, minimum, maximum)
            : defaultValue;
    }

    private async Task<bool> IsMaintenanceModeEnabledAsync(AppDbContext? db = null)
    {
        string? value;
        if (db is not null)
        {
            value = await db.SettingValues.AsNoTracking()
                .Where(x => x.DefinitionKey == "System.MaintenanceMode"
                    && x.Scope == SettingScope.System
                    && x.ScopeTarget == null)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();
        }
        else if (_settingsService is not null)
        {
            value = (await _settingsService.GetValueAsync("System.MaintenanceMode", SettingScope.System, null))?.Value;
        }
        else
        {
            return false;
        }

        return bool.TryParse(value, out var enabled) && enabled;
    }

    private static string ComputeTokenHash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static LoginResultDto Fail(string? errorMessage = null) => new()
    {
        Success = false,
        ErrorMessage = errorMessage ?? "Tên đăng nhập hoặc mật khẩu không đúng."
    };

    private async Task UpsertActiveCompanySessionAsync(Guid userId, Guid sessionId, LoginRequestDto request, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var row = await db.ActiveCompanySessions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SessionId == sessionId, cancellationToken);

        if (row is null)
        {
            row = new ActiveCompanySession
            {
                UserId = userId,
                SessionId = sessionId,
                CompanyId = CurrentCompanyContext.DefaultCompanyId,
                BranchId = null,
                Status = "Active",
                Source = "authentication",
                MachineName = request.MachineName ?? Environment.MachineName,
                AppVersion = typeof(AuthenticationService).Assembly.GetName().Version?.ToString(),
                SessionMetadataJson = JsonSerializer.Serialize(new
                {
                    request.UserName,
                    request.MachineName,
                    request.IpAddress
                })
            };
            db.ActiveCompanySessions.Add(row);
        }
        else
        {
            row.CompanyId = row.CompanyId ?? CurrentCompanyContext.DefaultCompanyId;
            row.Status = "Active";
            row.IsActive = true;
            row.IsArchived = false;
            row.LastActivityAtUtc = now;
            row.CompletedAtUtc = null;
            row.ExpiresAtUtc = expiresAtUtc;
            row.ModifiedAtUtc = now;
        }

        row.StartedAtUtc = row.StartedAtUtc == default ? now : row.StartedAtUtc;
        row.LastActivityAtUtc = now;
        row.ExpiresAtUtc = expiresAtUtc;
        row.ModifiedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CompleteActiveCompanySessionAsync(Guid userId, Guid sessionId, string status)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.ActiveCompanySessions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SessionId == sessionId, CancellationToken.None);

        // Validation may already have ended the company session with a policy
        // reason (for example MaintenanceMode). A follow-up client LogoutAsync
        // must not replace that terminal reason with the generic Completed.
        if (row is null || !row.IsActive)
        {
            return;
        }

        var now = DateTime.UtcNow;
        row.IsActive = false;
        row.Status = status;
        row.CompletedAtUtc = now;
        row.ModifiedAtUtc = now;
        await db.SaveChangesAsync(CancellationToken.None);
    }
}
