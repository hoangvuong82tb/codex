using System.Text.Json;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
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

    public AuthenticationService(
        InMemorySecurityStore store,
        IPasswordHasher passwordHasher,
        IRoleService roleService,
        IAuditLogService auditLogService,
        IServiceScopeFactory scopeFactory,
        IAuditSessionService? auditSessionService = null)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _roleService = roleService;
        _auditLogService = auditLogService;
        _auditSessionService = auditSessionService;
        _scopeFactory = scopeFactory;
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request)
    {
        var userName = InputSanitizer.TrimAndLimit(request.UserName, 64);
        var normalized = userName.ToUpperInvariant();
        var user = _store.Users.FirstOrDefault(x => x.NormalizedUserName == normalized);

        if (user is null)
        {
            _store.Audits.Add(new LoginAudit { UserName = userName, Success = false, FailureReason = "Invalid credentials", MachineName = request.MachineName, IpAddress = request.IpAddress });
            await _auditLogService.LogAsync("LoginFailed", $"Login failed for '{userName}'", "security");
            return Fail();
        }

        if (!user.IsActive || user.IsLocked)
        {
            _store.Audits.Add(new LoginAudit { UserName = userName, UserId = user.Id, Success = false, FailureReason = user.IsLocked ? "Locked" : "Inactive", MachineName = request.MachineName, IpAddress = request.IpAddress });
            return Fail();
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
            {
                user.IsLocked = true;
            }

            _store.Audits.Add(new LoginAudit { UserName = userName, UserId = user.Id, Success = false, FailureReason = "Invalid credentials", MachineName = request.MachineName, IpAddress = request.IpAddress });
            await _auditLogService.LogAsync("LoginFailed", $"Login failed for '{userName}'", "security");
            return Fail();
        }

        user.FailedLoginCount = 0;
        user.LastLoginAt = DateTime.UtcNow;
        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = _passwordHasher.HashPassword(request.Password);
        }

        var session = new LoginSession
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            MachineName = request.MachineName,
            IpAddress = request.IpAddress,
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            IsActive = true
        };
        _store.Sessions.Add(session);

        var roles = _store.UserRoles.Where(x => x.UserId == user.Id)
            .Join(_store.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name).Distinct().ToList();
        var permissions = await _roleService.GetUserPermissionCodesAsync(user.Id);

        _store.Audits.Add(new LoginAudit { UserName = userName, UserId = user.Id, Success = true, MachineName = request.MachineName, IpAddress = request.IpAddress });
        await _auditLogService.LogAsync("LoginSuccess", $"Login success for '{userName}'", "security");
        if (_auditSessionService is not null)
        {
            await _auditSessionService.StartSessionAsync("Authentication.Login", session.Token, request.MachineName, request.MachineName, Environment.OSVersion.Platform.ToString(), cancellationToken: default);
        }

        await UpsertActiveCompanySessionAsync(user.Id, session.Id, request, cancellationToken: default);

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
        if (session is not null)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            if (_auditSessionService is not null)
            {
                await _auditSessionService.CompleteSessionAsync(session.Token, "Logout");
            }

            await CompleteActiveCompanySessionAsync(session.UserId, session.Id, "Completed");
        }
    }

    public Task<bool> ValidateSessionAsync(Guid sessionId)
    {
        var valid = _store.Sessions.Any(x => x.Id == sessionId && x.IsActive && x.ExpiresAt > DateTime.UtcNow);
        return Task.FromResult(valid);
    }

    public Task ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        var user = _store.Users.FirstOrDefault(x => x.Id == request.UserId) ?? throw new InvalidOperationException("User không tồn tại.");
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.LastPasswordChangedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    private static LoginResultDto Fail() => new()
    {
        Success = false,
        ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng."
    };

    private async Task UpsertActiveCompanySessionAsync(Guid userId, Guid sessionId, LoginRequestDto request, CancellationToken cancellationToken)
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
            row.ExpiresAtUtc = now.AddHours(8);
            row.ModifiedAtUtc = now;
        }

        row.StartedAtUtc = row.StartedAtUtc == default ? now : row.StartedAtUtc;
        row.LastActivityAtUtc = now;
        row.ExpiresAtUtc = now.AddHours(8);
        row.ModifiedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CompleteActiveCompanySessionAsync(Guid userId, Guid sessionId, string status)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.ActiveCompanySessions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SessionId == sessionId, CancellationToken.None);

        if (row is null)
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
