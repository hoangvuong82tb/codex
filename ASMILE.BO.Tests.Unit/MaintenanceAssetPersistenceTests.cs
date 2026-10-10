using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Maintenance;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Maintenance;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.Maintenance;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ASMILE.BO.Tests.Unit;

public sealed class MaintenanceAssetPersistenceTests
{
    [Fact]
    public async Task MT_REAL_001_asset_failure_and_preventive_schedule_use_real_persistence()
    {
        var propertyId = PropertyId.New();
        var roomId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        var clock = new FixedClock(now);
        var persistence = new EfFrontOfficePersistenceSession(
            db,
            accessor,
            new PassthroughIntegrationEventMapper(),
            new CanonicalOutboxMessageFactory(clock),
            new FrontOfficeAuditWriter(),
            clock);
        using var scope = accessor.BeginScope(CreateContext(propertyId));

        var asset = MaintenanceAsset.Create(
            propertyId.Value,
            "HVAC-101",
            "Điều hòa phòng 101",
            nameof(AssetCategory.HVAC),
            "Tầng 1",
            roomId,
            "SN-HVAC-101",
            "VRF-2025",
            "Vendor A",
            now.AddYears(1),
            now.AddYears(-1),
            1200,
            85m,
            now.AddDays(-30),
            now.AddDays(30),
            now);
        db.MaintenanceAssets.Add(asset);

        var plan = PreventiveMaintenancePlan.Create(
            propertyId.Value,
            "Bảo trì điều hòa tháng",
            nameof(AssetCategory.HVAC),
            "Vệ sinh và kiểm tra hiệu suất điều hòa.",
            asset.AssetId,
            30,
            60);
        plan.AddChecklistItem("FILTER", "Thay lọc gió", "HVAC", true, "Dùng lọc OEM");
        db.PreventiveMaintenancePlans.Add(plan);
        db.PreventiveMaintenanceSchedules.Add(PreventiveMaintenanceSchedule.Create(
            propertyId.Value, plan.PlanId, asset.AssetId, roomId, now.AddDays(-2)));
        db.PreventiveMaintenanceSchedules.Add(PreventiveMaintenanceSchedule.Create(
            propertyId.Value, plan.PlanId, asset.AssetId, roomId, now.AddDays(3)));
        await db.SaveChangesAsync();

        var assetService = new EfMaintenanceAssetService(db, persistence, accessor, clock);
        var preventiveService = new EfPreventiveMaintenanceService(db, accessor, clock);

        (await assetService.GetAssetsByRoomAsync(roomId, CancellationToken.None)).Should().ContainSingle();
        (await assetService.GetHealthAsync(propertyId.Value, AssetCategory.HVAC, CancellationToken.None))
            .Single().ConditionScore.Should().Be(85m);

        await assetService.RecordFailureAsync(asset.AssetId, "Không làm lạnh đủ công suất.", CancellationToken.None);
        var persisted = await db.MaintenanceAssets.SingleAsync();
        persisted.FailureCount.Should().Be(1);
        persisted.TotalRepairs.Should().Be(1);
        persisted.ConditionScore.Should().Be(75m);
        (await db.MaintenanceAssetFailures.CountAsync()).Should().Be(1);

        var plans = await preventiveService.GetPlansAsync(AssetCategory.HVAC, CancellationToken.None);
        plans.Should().ContainSingle();
        plans.Single().Checklist.Should().ContainSingle(x => x.Code == "FILTER");
        (await preventiveService.GetOverdueAsync(propertyId.Value, CancellationToken.None)).Should().ContainSingle();
        (await preventiveService.GetScheduleAsync(propertyId.Value, now.AddDays(-3), now.AddDays(4), CancellationToken.None))
            .Should().HaveCount(2);
        (await db.AuditLogs.CountAsync(x => x.EntityName == nameof(MaintenanceAsset))).Should().Be(1);
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Maintenance" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
