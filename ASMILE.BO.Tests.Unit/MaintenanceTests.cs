using ASMILE.BO.Application.FrontOffice.Maintenance;
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
    [Fact] public void MT_009_event_names_unique() { typeof(MaintenanceEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems(); }
}
