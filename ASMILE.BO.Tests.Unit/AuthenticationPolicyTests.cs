using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Infrastructure.Security.Store;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Tests.Unit;

public sealed class AuthenticationPolicyTests
{
    [Fact]
    public async Task ChangePassword_requires_a_strong_different_password_and_clears_the_first_login_flag()
    {
        var store = new InMemorySecurityStore();
        var hasher = new Pbkdf2PasswordHasher();
        var user = new UserAccount
        {
            UserName = "first-login",
            NormalizedUserName = "FIRST-LOGIN",
            DisplayName = "First Login",
            PasswordHash = hasher.HashPassword("Temporary123"),
            MustChangePassword = true
        };
        store.Users.Add(user);
        var audit = new RecordingAuditLogService();
        var service = new AuthenticationService(
            store,
            hasher,
            new RoleService(store),
            audit,
            new DbScopeFactory(null!));

        await FluentActions.Awaiting(() => service.ChangePasswordAsync(new ChangePasswordRequestDto
            {
                UserId = user.Id,
                CurrentPassword = "Temporary123",
                NewPassword = "weak"
            }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Mật khẩu phải tối thiểu 8 ký tự.");

        await FluentActions.Awaiting(() => service.ChangePasswordAsync(new ChangePasswordRequestDto
            {
                UserId = user.Id,
                CurrentPassword = "Temporary123",
                NewPassword = "Temporary123"
            }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");

        user.MustChangePassword.Should().BeTrue();
        await service.ChangePasswordAsync(new ChangePasswordRequestDto
        {
            UserId = user.Id,
            CurrentPassword = "Temporary123",
            NewPassword = "NewStrongPass1"
        });

        user.MustChangePassword.Should().BeFalse();
        hasher.VerifyPassword("NewStrongPass1", user.PasswordHash).Should().BeTrue();
        audit.Actions.Should().Contain("PasswordChanged");
    }

    [Fact]
    public async Task ChangePassword_persists_new_password_and_first_login_flag_to_database()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var hasher = new Pbkdf2PasswordHasher();
        var oldHash = hasher.HashPassword("Temporary123");
        var persistedUser = new AppUser
        {
            UserName = "first-login-db",
            DisplayName = "First Login DB",
            PasswordHash = oldHash,
            MustChangePassword = true
        };
        db.AppUsers.Add(persistedUser);
        await db.SaveChangesAsync();

        var store = new InMemorySecurityStore();
        var user = new UserAccount
        {
            Id = persistedUser.Id,
            UserName = persistedUser.UserName,
            NormalizedUserName = persistedUser.UserName.ToUpperInvariant(),
            DisplayName = persistedUser.DisplayName,
            PasswordHash = oldHash,
            MustChangePassword = true
        };
        store.Users.Add(user);
        var audit = new RecordingAuditLogService();
        var service = new AuthenticationService(
            store,
            hasher,
            new RoleService(store),
            audit,
            new DbScopeFactory(null!),
            dbContext: db);

        await service.ChangePasswordAsync(new ChangePasswordRequestDto
        {
            UserId = user.Id,
            CurrentPassword = "Temporary123",
            NewPassword = "NewStrongPass1"
        });

        var savedUser = await db.AppUsers.AsNoTracking().SingleAsync(x => x.Id == persistedUser.Id);
        savedUser.MustChangePassword.Should().BeFalse();
        hasher.VerifyPassword("NewStrongPass1", savedUser.PasswordHash).Should().BeTrue();
        savedUser.LastPasswordChangedAt.Should().NotBeNull();
        audit.Actions.Should().Contain("PasswordChanged");
    }

    [Fact]
    public async Task Login_blocks_non_maintenance_users_but_allows_users_with_maintenance_permission()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var store = new InMemorySecurityStore();
        var hasher = new Pbkdf2PasswordHasher();
        var user = new UserAccount
        {
            UserName = "operator",
            NormalizedUserName = "OPERATOR",
            DisplayName = "Operator",
            PasswordHash = hasher.HashPassword("CorrectPass123"),
            MustChangePassword = false
        };
        store.Users.Add(user);
        var role = new Role { Code = "Operator", Name = "Operator" };
        var maintenancePermission = new AppPermission { Code = "System.Maintenance", Name = "System maintenance" };
        store.Roles.Add(role);
        store.Permissions.Add(maintenancePermission);
        store.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.AppUsers.Add(new AppUser { Id = user.Id, UserName = user.UserName, DisplayName = user.DisplayName, PasswordHash = user.PasswordHash, MustChangePassword = false });
        await db.SaveChangesAsync();

        var audit = new RecordingAuditLogService();
        var settings = new StubSettingsService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["System.MaintenanceMode"] = "true"
        });
        var service = new AuthenticationService(
            store,
            hasher,
            new RoleService(store),
            audit,
            new DbScopeFactory(db),
            settingsService: settings);

        var denied = await service.LoginAsync(new LoginRequestDto { UserName = user.UserName, Password = "CorrectPass123", MachineName = "UNIT" });
        denied.Success.Should().BeFalse();
        denied.ErrorMessage.Should().Contain("đang bảo trì");
        store.Sessions.Should().BeEmpty();
        user.LastLoginAt.Should().BeNull();
        store.Audits.Should().ContainSingle(x => !x.Success && x.FailureReason == "Maintenance mode");
        audit.Actions.Should().Contain("LoginBlockedMaintenance");

        store.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = maintenancePermission.Id });
        var allowed = await service.LoginAsync(new LoginRequestDto { UserName = user.UserName, Password = "CorrectPass123", MachineName = "UNIT" });
        allowed.Success.Should().BeTrue();
        store.Sessions.Should().ContainSingle(x => x.IsActive);
    }

    [Fact]
    public async Task Maintenance_mode_permanently_revokes_existing_sessions_without_permission()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new AppUser { UserName = "existing-operator", DisplayName = "Operator", PasswordHash = "hash" };
        var role = new AppRole { Code = "Operator", Name = "Operator" };
        var session = new LoginSession { UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsActive = true };
        var presence = new ActiveCompanySession { UserId = user.Id, SessionId = session.Id, IsActive = true, Status = "Active" };
        db.AppUsers.Add(user);
        db.AppRoles.Add(role);
        db.AppUserRoles.Add(new AppUserRole { UserId = user.Id, RoleId = role.Id });
        db.LoginSessions.Add(session);
        db.ActiveCompanySessions.Add(presence);
        db.SettingValues.Add(new SettingValue
        {
            DefinitionKey = "System.MaintenanceMode",
            Scope = SettingScope.System,
            Value = "true"
        });
        await db.SaveChangesAsync();

        var store = new InMemorySecurityStore();
        var auditLog = new RecordingAuditLogService();
        var service = new AuthenticationService(
            store,
            new Pbkdf2PasswordHasher(),
            new RoleService(store),
            auditLog,
            new DbScopeFactory(db),
            dbContextFactory: new SqliteAppDbContextFactory(options));

        (await service.ValidateSessionAsync(session.Id)).Should().BeFalse();
        (await service.RenewSessionAsync(session.Id)).Should().BeFalse();
        await db.Entry(session).ReloadAsync();
        session.IsActive.Should().BeFalse();
        session.EndedAt.Should().NotBeNull();
        var endedPresence = await db.ActiveCompanySessions.AsNoTracking().SingleAsync(x => x.Id == presence.Id);
        endedPresence.IsActive.Should().BeFalse();
        endedPresence.Status.Should().Be("MaintenanceMode");
        endedPresence.CompletedAtUtc.Should().NotBeNull();
        auditLog.Actions.Should().Contain("SessionRevoked");

        // The desktop client sends LogoutAsync after its next validation tick.
        // That cleanup must preserve the persisted policy-revocation reason.
        await service.LogoutAsync(session.Id);
        (await db.ActiveCompanySessions.AsNoTracking().SingleAsync(x => x.Id == presence.Id))
            .Status.Should().Be("MaintenanceMode");

        db.AppRolePermissions.Add(new AppRolePermission { RoleId = role.Id, PermissionCode = "System.Maintenance" });
        var maintenanceSetting = await db.SettingValues.SingleAsync(x => x.DefinitionKey == "System.MaintenanceMode");
        maintenanceSetting.Value = "false";
        await db.SaveChangesAsync();

        (await service.ValidateSessionAsync(session.Id)).Should().BeFalse();
        (await service.RenewSessionAsync(session.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task RenewSession_extends_timeout_from_real_user_activity_and_updates_client_presence()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new AppUser { UserName = "active-operator", DisplayName = "Operator", PasswordHash = "hash" };
        var session = new LoginSession
        {
            UserId = user.Id,
            TokenHash = "active-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(1),
            IsActive = true
        };
        var presence = new ActiveCompanySession
        {
            UserId = user.Id,
            SessionId = session.Id,
            LastActivityAtUtc = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAtUtc = session.ExpiresAt,
            IsActive = true
        };
        db.AppUsers.Add(user);
        db.LoginSessions.Add(session);
        db.ActiveCompanySessions.Add(presence);
        db.SettingValues.Add(new SettingValue
        {
            DefinitionKey = "Security.SessionTimeoutMinutes",
            Scope = SettingScope.System,
            Value = "5"
        });
        await db.SaveChangesAsync();

        var store = new InMemorySecurityStore();
        var service = new AuthenticationService(
            store,
            new Pbkdf2PasswordHasher(),
            new RoleService(store),
            new RecordingAuditLogService(),
            new DbScopeFactory(db),
            dbContextFactory: new SqliteAppDbContextFactory(options));

        (await service.RenewSessionAsync(session.Id)).Should().BeTrue();

        var savedSession = await db.LoginSessions.AsNoTracking().SingleAsync(x => x.Id == session.Id);
        var savedPresence = await db.ActiveCompanySessions.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == presence.Id);
        savedSession.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), precision: TimeSpan.FromSeconds(15));
        savedPresence.LastActivityAtUtc.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(15));
        savedPresence.ExpiresAtUtc.Should().Be(savedSession.ExpiresAt);
    }

    [Fact]
    public async Task ValidateSession_rejects_expired_or_inactive_database_sessions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new AppUser { UserName = "expired-operator", DisplayName = "Operator", PasswordHash = "hash" };
        var expired = new LoginSession { UserId = user.Id, TokenHash = "expired-token", ExpiresAt = DateTime.UtcNow.AddMinutes(-1), IsActive = true };
        var inactive = new LoginSession { UserId = user.Id, TokenHash = "inactive-token", ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsActive = false };
        db.AppUsers.Add(user);
        db.LoginSessions.AddRange(expired, inactive);
        await db.SaveChangesAsync();

        var store = new InMemorySecurityStore();
        var service = new AuthenticationService(
            store,
            new Pbkdf2PasswordHasher(),
            new RoleService(store),
            new RecordingAuditLogService(),
            new DbScopeFactory(db),
            dbContext: db);

        (await service.ValidateSessionAsync(expired.Id)).Should().BeFalse();
        (await service.ValidateSessionAsync(inactive.Id)).Should().BeFalse();
        (await service.ValidateSessionAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Validate_and_renew_session_rejects_locked_or_disabled_accounts(bool isActive, bool isLocked)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new AppUser
        {
            UserName = "revoked-operator",
            DisplayName = "Revoked operator",
            PasswordHash = "hash",
            IsActive = true,
            IsLocked = false
        };
        var session = new LoginSession
        {
            UserId = user.Id,
            TokenHash = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsActive = true
        };
        db.AppUsers.Add(user);
        db.LoginSessions.Add(session);
        await db.SaveChangesAsync();

        user.IsActive = isActive;
        user.IsLocked = isLocked;
        await db.SaveChangesAsync();

        var store = new InMemorySecurityStore();
        var service = new AuthenticationService(
            store,
            new Pbkdf2PasswordHasher(),
            new RoleService(store),
            new RecordingAuditLogService(),
            new DbScopeFactory(db),
            dbContextFactory: new SqliteAppDbContextFactory(options));

        (await service.ValidateSessionAsync(session.Id)).Should().BeFalse();
        (await service.RenewSessionAsync(session.Id)).Should().BeFalse();

        var persistedSession = await db.LoginSessions.AsNoTracking().SingleAsync(x => x.Id == session.Id);
        persistedSession.ExpiresAt.Should().Be(session.ExpiresAt);
        persistedSession.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetSessionPermissionCodes_reads_current_database_role_assignments()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new AppUser { UserName = "permission-refresh", DisplayName = "Permission refresh", PasswordHash = "hash" };
        var role = new AppRole { Code = "Reception", Name = "Reception", IsActive = true };
        var session = new LoginSession
        {
            UserId = user.Id,
            TokenHash = "permission-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsActive = true
        };
        db.AppUsers.Add(user);
        db.AppRoles.Add(role);
        db.AppUserRoles.Add(new AppUserRole { UserId = user.Id, RoleId = role.Id });
        db.AppRolePermissions.Add(new AppRolePermission { RoleId = role.Id, PermissionCode = "FrontOffice.Stay.View" });
        db.LoginSessions.Add(session);
        await db.SaveChangesAsync();

        var store = new InMemorySecurityStore();
        var service = new AuthenticationService(
            store,
            new Pbkdf2PasswordHasher(),
            new RoleService(store, db),
            new RecordingAuditLogService(),
            new DbScopeFactory(db),
            dbContextFactory: new SqliteAppDbContextFactory(options));

        (await service.GetSessionPermissionCodesAsync(session.Id))
            .Should().BeEquivalentTo(["FrontOffice.Stay.View"]);

        var assignment = await db.AppRolePermissions.SingleAsync(x => x.RoleId == role.Id);
        db.AppRolePermissions.Remove(assignment);
        await db.SaveChangesAsync();

        (await service.GetSessionPermissionCodesAsync(session.Id)).Should().BeEmpty();

        role.IsActive = false;
        await db.SaveChangesAsync();
        (await service.GetSessionPermissionCodesAsync(session.Id)).Should().BeEmpty();

        session.IsActive = false;
        await db.SaveChangesAsync();
        (await service.GetSessionPermissionCodesAsync(session.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Login_uses_persisted_lockout_and_session_timeout_policies()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var store = new InMemorySecurityStore();
        var hasher = new Pbkdf2PasswordHasher();
        var user = new UserAccount
        {
            UserName = "operator",
            NormalizedUserName = "OPERATOR",
            DisplayName = "Operator",
            PasswordHash = hasher.HashPassword("correct-password"),
            MustChangePassword = false
        };
        store.Users.Add(user);

        var settings = new StubSettingsService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Security.MaxFailedLoginAttempts"] = "3",
            ["Security.SessionTimeoutMinutes"] = "5"
        });
        var service = new AuthenticationService(
            store,
            hasher,
            new RoleService(store),
            new RecordingAuditLogService(),
            new DbScopeFactory(db),
            settingsService: settings);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var failed = await service.LoginAsync(new LoginRequestDto { UserName = "operator", Password = "wrong" });
            failed.Success.Should().BeFalse();
        }

        user.IsLocked.Should().BeTrue();
        user.FailedLoginCount.Should().Be(3);

        user.IsLocked = false;
        user.FailedLoginCount = 0;
        var result = await service.LoginAsync(new LoginRequestDto { UserName = "operator", Password = "correct-password", MachineName = "UNIT" });

        result.Success.Should().BeTrue();
        var active = await db.ActiveCompanySessions.SingleAsync(x => x.SessionId == result.SessionId);
        active.ExpiresAtUtc.Should().NotBeNull();
        active.ExpiresAtUtc!.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), precision: TimeSpan.FromSeconds(10));
    }

    private sealed class DbScopeFactory(AppDbContext db) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new Scope(db);

        private sealed class Scope(AppDbContext db) : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new Provider(db);
            public void Dispose() { }
        }

        private sealed class Provider(AppDbContext db) : IServiceProvider
        {
            public object? GetService(Type serviceType)
                => serviceType == typeof(AppDbContext) ? db : null;
        }
    }

    private sealed class SqliteAppDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }

    private sealed class RecordingAuditLogService : IAuditLogService
    {
        public List<string> Actions { get; } = [];

        public Task LogAsync(string action, string detail, string username = "system", CancellationToken cancellationToken = default)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }

    private sealed class StubSettingsService(IReadOnlyDictionary<string, string> values) : ISettingsService
    {
        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult(values.TryGetValue(key, out var value)
                ? new SettingValueDto { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = value }
                : null);

        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task SaveValueAsync(SaveSettingValueRequest request) => throw new NotSupportedException();
        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests) => throw new NotSupportedException();
        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget) => throw new NotSupportedException();
    }
}
