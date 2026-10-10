using ASMILE.BO.Core.Entities;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Core.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class ClientRegistryServiceTests
{
    [Fact]
    public async Task Reads_only_active_non_archived_sessions_for_current_company()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"client-registry-{Guid.NewGuid():N}")
            .Options;

        await using (var seed = new AppDbContext(options))
        {
            seed.ActiveCompanySessions.AddRange(
                new ActiveCompanySession
                {
                    CompanyId = CurrentCompanyContext.DefaultCompanyId,
                    MachineName = "FO-01",
                    Source = "FO",
                    AppVersion = "1.2.3",
                    Status = "Active",
                    IsActive = true,
                    IsArchived = false,
                    LastActivityAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
                },
                new ActiveCompanySession
                {
                    CompanyId = CurrentCompanyContext.DefaultCompanyId,
                    MachineName = "EXPIRED-01",
                    Status = "Active",
                    IsActive = true,
                    IsArchived = false,
                    LastActivityAtUtc = DateTime.UtcNow.AddHours(-9),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
                },
                new ActiveCompanySession
                {
                    CompanyId = CurrentCompanyContext.DefaultCompanyId,
                    MachineName = "UNKNOWN-EXPIRY-01",
                    Status = "Active",
                    IsActive = true,
                    IsArchived = false,
                    LastActivityAtUtc = DateTime.UtcNow
                },
                new ActiveCompanySession
                {
                    CompanyId = CurrentCompanyContext.DefaultCompanyId,
                    MachineName = "OLD-01",
                    Status = "Completed",
                    IsActive = false,
                    IsArchived = false
                },
                new ActiveCompanySession
                {
                    CompanyId = CurrentCompanyContext.DefaultCompanyId,
                    MachineName = "ARCHIVED-01",
                    Status = "Active",
                    IsActive = true,
                    IsArchived = true
                });
            await seed.SaveChangesAsync();
        }

        var service = CreateService(options);
        var rows = await service.GetActiveClientsAsync();

        rows.Should().ContainSingle();
        rows[0].MachineName.Should().Be("FO-01");
        rows[0].Source.Should().Be("FO");
        rows[0].AppVersion.Should().Be("1.2.3");
    }

    [Fact]
    public async Task Revokes_login_and_company_presence_and_writes_audit()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"client-registry-revoke-{Guid.NewGuid():N}")
            .Options;
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var presenceId = Guid.NewGuid();

        await using (var seed = new AppDbContext(options))
        {
            seed.LoginSessions.Add(new LoginSession
            {
                Id = sessionId,
                UserId = userId,
                TokenHash = Guid.NewGuid().ToString("N"),
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            });
            seed.ActiveCompanySessions.Add(new ActiveCompanySession
            {
                Id = presenceId,
                CompanyId = CurrentCompanyContext.DefaultCompanyId,
                UserId = userId,
                SessionId = sessionId,
                Status = "Active",
                IsActive = true,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            });
            await seed.SaveChangesAsync();
        }

        var service = CreateService(options);
        var revoked = await service.RevokeSessionAsync(sessionId);

        revoked.Should().BeTrue();
        await using var verify = new AppDbContext(options);
        var login = await verify.LoginSessions.SingleAsync(x => x.Id == sessionId);
        login.IsActive.Should().BeFalse();
        login.EndedAt.Should().NotBeNull();

        var presence = await verify.ActiveCompanySessions.IgnoreQueryFilters().SingleAsync(x => x.Id == presenceId);
        presence.IsActive.Should().BeFalse();
        presence.Status.Should().Be("Revoked");
        presence.CompletedAtUtc.Should().NotBeNull();
        presence.ExpiresAtUtc.Should().Be(presence.CompletedAtUtc);

        var audit = await verify.AuditLogs.SingleAsync(x => x.EntityId == sessionId.ToString("N"));
        audit.Action.Should().Be("WorkstationSessionRevoked");
        audit.EntityName.Should().Be(nameof(LoginSession));
        audit.CompanyId.Should().Be(CurrentCompanyContext.DefaultCompanyId);
    }

    [Fact]
    public async Task Revoke_returns_false_when_session_is_not_visible_or_already_inactive()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"client-registry-stale-{Guid.NewGuid():N}")
            .Options;
        var service = CreateService(options);

        (await service.RevokeSessionAsync(Guid.NewGuid())).Should().BeFalse();
    }

    private static ClientRegistryService CreateService(DbContextOptions<AppDbContext> options)
        => new(new InMemoryDbContextFactory(options), new AllowAllAuthorizationService(), new TestCurrentUserService());

    private sealed class AllowAllAuthorizationService : IAuthorizationService
    {
        public bool HasPermission(string permissionCode) => true;
        public void Demand(string permissionCode) { }
        public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(true);
        public bool HasPermission(Permission permission) => true;
        public void Demand(Permission permission) { }
        public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(true);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser => new() { UserName = "admin" };
        public Guid? CurrentSessionId => null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permissionCode) => true;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear() { }
    }

    private sealed class InMemoryDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(options));
    }
}
