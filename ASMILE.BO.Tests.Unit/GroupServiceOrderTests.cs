using ASMILE.BO.Core.FrontOffice.Group;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class GroupServiceOrderTests
{
    [Fact]
    public void Create_calculates_commercial_totals_without_fabricated_values()
    {
        var order = GroupServiceOrder.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "GSV-TEST-001",
            "Phòng họp Grand",
            "Meeting",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            new TimeOnly(9, 0),
            120,
            2,
            100m,
            0.10m,
            0.08m,
            0.05m,
            "MasterFolio",
            DateTime.UtcNow,
            null);

        order.SubTotal.Should().Be(200m);
        order.DiscountAmount.Should().Be(20m);
        order.TaxAmount.Should().Be(14.4m);
        order.ServiceChargeAmount.Should().Be(9m);
        order.TotalAmount.Should().Be(203.4m);
        order.Status.Should().Be(GroupServiceOrderStatus.Pending);
    }

    [Fact]
    public void Status_workflow_requires_confirm_before_completion()
    {
        var order = CreateOrder();

        var act = () => order.Complete(DateTime.UtcNow, null);
        act.Should().Throw<InvalidOperationException>();

        order.Confirm(DateTime.UtcNow, null);
        order.Start(DateTime.UtcNow, null);
        order.Complete(DateTime.UtcNow, null);

        order.Status.Should().Be(GroupServiceOrderStatus.Completed);
    }

    [Fact]
    public void Cancel_requires_reason_and_cannot_cancel_completed_order()
    {
        var order = CreateOrder();
        var missingReason = () => order.Cancel(" ", DateTime.UtcNow, null);
        missingReason.Should().Throw<ArgumentException>().WithMessage("GROUP_SERVICE_CANCEL_REASON_REQUIRED");

        order.Confirm(DateTime.UtcNow, null);
        order.Cancel("Khách hủy", DateTime.UtcNow, null);
        order.Status.Should().Be(GroupServiceOrderStatus.Cancelled);
        order.Remark.Should().Contain("Hủy: Khách hủy");

        var completed = CreateOrder();
        completed.Confirm(DateTime.UtcNow, null);
        completed.Complete(DateTime.UtcNow, null);
        var act = () => completed.Cancel("Hủy sau hoàn thành", DateTime.UtcNow, null);
        act.Should().Throw<InvalidOperationException>().WithMessage("GROUP_SERVICE_STATUS_NOT_CANCELLABLE");
    }

    private static GroupServiceOrder CreateOrder() => GroupServiceOrder.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "GSV-TEST-002",
        "Ăn trưa đoàn",
        "Restaurant",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        new TimeOnly(12, 0),
        90,
        1,
        250m,
        0m,
        0.08m,
        0.05m,
        "GuestFolio",
        DateTime.UtcNow,
        null);
}
