using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Infrastructure.FrontOffice.Group;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class GroupTests
{
    [Fact] public void GRP_001_create_block() { var b = new RoomBlockService().CreateBlockAsync(Guid.NewGuid(), Guid.NewGuid(), 40, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), 1200000, default).Result; b.Status.Should().Be(BlockStatus.Confirmed); b.RoomsBlocked.Should().Be(40); }
    [Fact] public void GRP_002_modify_block() { new RoomBlockService().ModifyBlockAsync(Guid.NewGuid(), 5, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), default).Result.RoomsBlocked.Should().Be(35); }
    [Fact] public void GRP_003_release_rooms() { new RoomBlockService().ReleaseRoomsAsync(Guid.NewGuid(), 5, default).Result.RoomsRemaining.Should().Be(15); }
    [Fact] public void GRP_004_pickup_on_track() { new RoomBlockService().GetPickupAsync(Guid.NewGuid(), null, default).Result.Trend.Should().Be("OnTrack"); }
    [Fact] public void GRP_005_block_dashboard_metrics() { new RoomBlockService().GetBlockDashboardAsync(Guid.NewGuid(), default).Result.OverallPickupPct.Should().BeGreaterThan(0); }
    [Fact] public void GRP_006_import_rooming_list() { var r = new RoomingListService().ImportAsync(Guid.NewGuid(), [new RoomingListItemDto(Guid.NewGuid(), Guid.NewGuid(), "101", "DLX", null, "Guest", 1, 0, false, false, false, null, null, null)], default).Result; r.Imported.Should().Be(1); }
    [Fact] public void GRP_007_billing_summary() { new GroupBillingService().GetBillingSummaryAsync(Guid.NewGuid(), default).Result.TotalCharges.Should().BeGreaterThan(0); }
    [Fact] public void GRP_008_revenue_forecast_accuracy() { new GroupBillingService().GetRevenueDashboardAsync(Guid.NewGuid(), default).Result.ForecastAccuracy.Should().BeGreaterThan(80); }
    [Fact] public void GRP_009_group_dashboard() { new GroupDashboardService().GetDashboardAsync(Guid.NewGuid(), default).Result.OverallPickup.Should().BeGreaterThan(0); }
    [Fact] public void GRP_010_events_unique() { typeof(GroupEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems(); }
}
