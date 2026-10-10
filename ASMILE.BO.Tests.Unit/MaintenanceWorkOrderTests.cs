using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Maintenance;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class MaintenanceWorkOrderTests
{
    [Fact]
    public void Create_and_transition_keeps_a_persistable_audit_trail()
    {
        var propertyId = Guid.NewGuid();
        var actorId = ActorId.New();
        var createdAt = new DateTime(2026, 9, 8, 1, 0, 0, DateTimeKind.Utc);
        var order = MaintenanceWorkOrder.Create(
            propertyId,
            "WO-20260908-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "101",
            "Corrective",
            "High",
            "HVAC",
            "Điều hòa không làm mát",
            60,
            createdAt,
            actorId);

        order.Status.Should().Be("Open");
        order.Transition("Assigned", "Đã tiếp nhận", actorId.Value, createdAt.AddMinutes(1));
        order.Transition("InProgress", "Đang xử lý", actorId.Value, createdAt.AddMinutes(5));
        order.Transition("Completed", "Đã thay tụ quạt", actorId.Value, createdAt.AddMinutes(35));

        order.Status.Should().Be("Completed");
        order.ActualMinutes.Should().Be(30);
        order.Notes.Should().HaveCount(3);
        order.Notes.Select(x => x.Action).Should().ContainInOrder("Open->Assigned", "Assigned->InProgress", "InProgress->Completed");
    }

    [Fact]
    public void Assign_only_allows_open_or_escalated_orders()
    {
        var order = MaintenanceWorkOrder.Create(
            Guid.NewGuid(), "WO-20260908-002", null, Guid.NewGuid(), "102",
            "Corrective", "Medium", "Plumbing", "Vòi nước rò rỉ", 30,
            DateTime.UtcNow, null);

        order.Assign(Guid.NewGuid(), "Kỹ thuật viên 1", DateTime.UtcNow, null);
        order.Status.Should().Be("Assigned");
        Action assignAgain = () => order.Assign(Guid.NewGuid(), "Kỹ thuật viên 2", DateTime.UtcNow, null);
        assignAgain.Should().Throw<InvalidOperationException>().WithMessage("MAINTENANCE_ASSIGN_INVALID_STATUS_ASSIGNED");
    }
}
