using ASMILE.BO.Application.FrontOffice.Maintenance;
using ASMILE.BO.Infrastructure.FrontOffice.Maintenance;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class MaintenanceTests
{
    [Fact] public void MT_001_open_to_assigned_valid() => WorkOrderStateMachine.IsValid(WorkOrderStatus.Open, WorkOrderStatus.Assigned).Should().BeTrue();
    [Fact] public void MT_002_open_to_completed_invalid() => WorkOrderStateMachine.IsValid(WorkOrderStatus.Open, WorkOrderStatus.Completed).Should().BeFalse();
    [Fact] public void MT_003_completed_to_verified_valid() => WorkOrderStateMachine.IsValid(WorkOrderStatus.Completed, WorkOrderStatus.Verified).Should().BeTrue();
    [Fact] public void MT_004_paused_to_inprogress_valid() => WorkOrderStateMachine.IsValid(WorkOrderStatus.Paused, WorkOrderStatus.InProgress).Should().BeTrue();
    [Fact] public void MT_005_cancelled_to_open_valid() => WorkOrderStateMachine.IsValid(WorkOrderStatus.Cancelled, WorkOrderStatus.Open).Should().BeTrue();
    [Fact] public void MT_006_closed_is_terminal() => WorkOrderStateMachine.IsTerminal(WorkOrderStatus.Closed).Should().BeTrue();
    [Fact] public void MT_007_cancelled_is_terminal() => WorkOrderStateMachine.IsTerminal(WorkOrderStatus.Cancelled).Should().BeTrue();
    [Fact] public void MT_008_inprogress_is_not_terminal() => WorkOrderStateMachine.IsTerminal(WorkOrderStatus.InProgress).Should().BeFalse();
    [Fact] public void MT_009_work_order_created_from_request() { var svc = new WorkOrderService(); var req = new MaintenanceRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "101", MaintenanceType.Emergency, MaintenancePriority.Critical, AssetCategory.DoorLock, "Lock broken", "Housekeeping", DateTime.UtcNow, true, false, null); var wo = svc.CreateAsync(req, default).Result; wo.Status.Should().Be(WorkOrderStatus.Open); wo.Priority.Should().Be(MaintenancePriority.Critical); }
    [Fact] public void MT_010_asset_health_has_condition_score() { var svc = new AssetService(); var health = svc.GetHealthAsync(Guid.NewGuid(), AssetCategory.Elevator, default).Result; health.Should().NotBeEmpty(); health[0].ConditionScore.Should().BeGreaterThan(0); }
    [Fact] public void MT_011_preventive_plan_has_checklist() { var svc = new PreventiveService(); var plans = svc.GetPlansAsync(AssetCategory.HVAC, default).Result; plans.Should().NotBeEmpty(); plans[0].Checklist.Should().NotBeEmpty(); }
    [Fact] public void MT_012_preventive_overdue_detected() { var svc = new PreventiveService(); var overdue = svc.GetOverdueAsync(Guid.NewGuid(), default).Result; overdue.Should().NotBeEmpty(); overdue[0].IsOverdue.Should().BeTrue(); }
    [Fact] public void MT_013_dashboard_has_metrics() { var svc = new WorkOrderService(); var d = svc.GetDashboardAsync(Guid.NewGuid(), default).Result; d.TotalOpenWorkOrders.Should().BeGreaterThan(0); d.SlaCompliancePct.Should().BeGreaterThan(0); }
    [Fact] public void MT_014_asset_dashboard_active_assets() { var svc = new AssetService(); var d = svc.GetDashboardAsync(Guid.NewGuid(), default).Result; d.TotalAssets.Should().BeGreaterThan(0); d.Active.Should().BeGreaterThan(0); }
    [Fact] public void MT_015_event_names_unique() { typeof(MaintenanceEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems(); }
}
