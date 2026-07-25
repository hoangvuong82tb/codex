using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class HousekeepingPlatformTests
{
    [Fact] public void HK_001_status_machine_allows_valid_transition() => HkStatusMachine.IsValidTransition(HkRoomStatus.VacantDirty, HkRoomStatus.CleaningInProgress).Should().BeTrue();
    [Fact] public void HK_002_status_machine_rejects_invalid_transition() => HkStatusMachine.IsValidTransition(HkRoomStatus.VacantClean, HkRoomStatus.CleaningInProgress).Should().BeFalse();
    [Fact] public void HK_003_status_machine_returns_allowed_next() => HkStatusMachine.AllowedNext(HkRoomStatus.VacantDirty).Should().Contain(HkRoomStatus.CleaningInProgress);
    [Fact] public void HK_004_status_machine_vacant_clean_next() => HkStatusMachine.AllowedNext(HkRoomStatus.VacantClean).Should().Contain([HkRoomStatus.OccupiedClean, HkRoomStatus.MaintenancePending, HkRoomStatus.DoNotDisturb]);
    [Fact] public void HK_005_assignment_engine_returns_result() { var engine = new AssignmentEngine(); var r = engine.AssignRoomsAsync(new AssignmentRequest(Guid.NewGuid(), null, null, 10, AssignmentStrategy.Balanced), default).Result; r.IsBalanced.Should().BeTrue(); }
    [Fact] public void HK_006_inspection_checklist_departure_clean() { var engine = new InspectionEngine(); var cl = engine.GetChecklistAsync(HousekeepingTaskType.DepartureClean, default).Result; cl.Items.Should().HaveCount(4); cl.Items.Should().Contain(i => i.Code == "BED"); }
    [Fact] public void HK_007_inspection_result_passes_at_80pct() { var engine = new InspectionEngine(); var cl = engine.GetChecklistAsync(HousekeepingTaskType.DepartureClean, default).Result; var r = engine.InspectAsync(Guid.NewGuid(), Guid.NewGuid(), cl, default).Result; r.Passed.Should().BeTrue(); r.PassPercent.Should().BeGreaterThanOrEqualTo(80); }
    [Fact] public void HK_008_linen_types_have_categories() { var svc = new LinenService(); var types = svc.GetLinenTypesAsync(default).Result; types.Should().NotBeEmpty(); types[0].Category.Should().Be("Bedding"); }
    [Fact] public void HK_009_amenity_refill_creates_request() { var svc = new AmenityService(); var req = svc.CreateRefillRequestAsync(Guid.NewGuid(), default).Result; req.Items.Should().NotBeEmpty(); }
    [Fact] public void HK_010_executive_dashboard_has_metrics() { var svc = new HousekeepingBoardService(); var d = svc.GetExecutiveDashboardAsync(Guid.NewGuid(), default).Result; d.TotalRooms.Should().BeGreaterThan(0); d.ReadinessPct.Should().BeGreaterThan(0); }
    [Fact] public void HK_011_task_board_returns_tasks() { var svc = new HousekeepingBoardService(); var tasks = svc.GetTaskBoardAsync(Guid.NewGuid(), "1", null, RoomPriority.ArrivingToday, default).Result; tasks.Should().NotBeEmpty(); }
    [Fact] public void HK_012_event_names_are_unique() { var events = typeof(HkEventNames).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)?.ToString()).ToList(); events.Should().OnlyHaveUniqueItems(); }
}
