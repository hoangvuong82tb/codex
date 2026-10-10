using ASMILE.BO.Core.FrontOffice.Group;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class GroupRoomAssignmentTests
{
    [Fact]
    public void Update_guest_allows_clearing_an_unassigned_row_and_trims_name()
    {
        var assignment = GroupRoomAssignment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "405", "DLX", "  Nguyen Van A  ", 2, 0,
            DateTime.UtcNow);

        assignment.UpdateGuest(null, "  Nguyen Van B  ");
        assignment.GuestId.Should().BeNull();
        assignment.GuestName.Should().Be("Nguyen Van B");

        assignment.UpdateGuest(null, null);
        assignment.GuestName.Should().BeNull();
    }

    [Fact]
    public void Update_occupancy_rejects_an_empty_rooming_row()
    {
        var assignment = GroupRoomAssignment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "405", "DLX", "Nguyen Van A", 2, 0,
            DateTime.UtcNow);

        var action = () => assignment.UpdateOccupancy(0, 0);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*GROUP_OCCUPANCY_REQUIRED*");
    }
}
