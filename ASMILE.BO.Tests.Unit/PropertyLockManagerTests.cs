using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Posting;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.NightAudit;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.Posting;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class PropertyLockManagerTests
{
    [Fact]
    public async Task NightAuditLock_IsPersistedAndCanBeReleased()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var businessDate = BusinessDateRecord.Open(propertyId.Value, new DateOnly(2026, 9, 8), DateTime.UtcNow, ActorId.New());
        db.Set<BusinessDateRecord>().Add(businessDate);
        await db.SaveChangesAsync();

        var manager = new PropertyLockManager(db, accessor);
        var auditId = Guid.NewGuid();

        (await manager.IsLockedAsync(propertyId.Value, CancellationToken.None)).Should().BeFalse();
        (await manager.LockAsync(propertyId.Value, auditId, "unit test", CancellationToken.None)).Should().BeTrue();
        (await manager.GetLockStateAsync(propertyId.Value, CancellationToken.None)).Should().Match<PropertyLockState>(state =>
            state.IsLocked && state.NightAuditId == auditId && state.BusinessDate == businessDate.BusinessDate);

        (await manager.UnlockAsync(propertyId.Value, CancellationToken.None)).Should().BeTrue();
        (await manager.IsLockedAsync(propertyId.Value, CancellationToken.None)).Should().BeFalse();
        (await db.Set<BusinessDateRecord>().SingleAsync()).Status.Should().Be(BusinessDateStatus.Open);
    }

    [Fact]
    public async Task FrontOfficePostingEngine_DoesNotAllowPostingWhilePropertyIsLocked()
    {
        var propertyId = PropertyId.New();
        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var lockManager = new TestPropertyLockManager { IsLocked = true };
        var engine = new FrontOfficePostingEngine(null!, null!, accessor, null!, lockManager);

        (await engine.CanPostAsync(propertyId.Value, CancellationToken.None)).Should().BeFalse();

        lockManager.IsLocked = false;
        (await engine.CanPostAsync(propertyId.Value, CancellationToken.None)).Should().BeTrue();
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class TestPropertyLockManager : IPropertyLockManager
    {
        public bool IsLocked { get; set; }

        public Task<PropertyLockState> GetLockStateAsync(Guid propertyId, CancellationToken ct) =>
            Task.FromResult(new PropertyLockState(propertyId, new DateOnly(2026, 9, 8), IsLocked, null, null, null, null));

        public Task<bool> LockAsync(Guid propertyId, Guid nightAuditId, string reason, CancellationToken ct) =>
            Task.FromResult(true);

        public Task<bool> UnlockAsync(Guid propertyId, CancellationToken ct) =>
            Task.FromResult(true);

        public Task<bool> IsLockedAsync(Guid propertyId, CancellationToken ct) =>
            Task.FromResult(IsLocked);
    }
}
