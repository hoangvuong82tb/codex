using ASMILE.BO.Core.FrontOffice.Group;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class GroupReservationWorkflowTests
{
    [Fact]
    public void Confirm_then_check_in_updates_group_and_assignment_state()
    {
        var now = DateTime.UtcNow;
        var group = GroupReservation.Create(
            Guid.NewGuid(),
            "GRP-TEST-001",
            "Đoàn kiểm thử",
            DateOnly.FromDateTime(now.AddDays(-1)),
            DateOnly.FromDateTime(now.AddDays(2)),
            1,
            2,
            1,
            now,
            null);
        var assignment = group.AddRoom(Guid.NewGuid(), "101", "DLX", "Khách kiểm thử", 2, 1, now);

        group.Confirm(now);
        assignment.MarkCheckedIn();
        group.MarkCheckedIn(now);

        group.Status.Should().Be(GroupReservationStatus.CheckedIn);
        assignment.IsCheckedIn.Should().BeTrue();
        assignment.IsCheckedOut.Should().BeFalse();
    }

    [Fact]
    public void Check_in_rejects_assignment_that_was_checked_out()
    {
        var now = DateTime.UtcNow;
        var assignment = GroupReservation.Create(
            Guid.NewGuid(),
            "GRP-TEST-002",
            "Đoàn kiểm thử",
            DateOnly.FromDateTime(now.AddDays(-1)),
            DateOnly.FromDateTime(now.AddDays(2)),
            1,
            1,
            0,
            now,
            null).AddRoom(Guid.NewGuid(), "102", "STD", "Khách", 1, 0, now);

        assignment.MarkCheckedIn();
        assignment.MarkCheckedOut();

        var act = () => assignment.MarkCheckedIn();
        act.Should().Throw<InvalidOperationException>().WithMessage("GROUP_ROOM_ALREADY_CHECKED_OUT");
    }

    [Fact]
    public void Assign_guest_normalizes_name_and_requires_guest_id()
    {
        var now = DateTime.UtcNow;
        var assignment = GroupReservation.Create(
            Guid.NewGuid(),
            "GRP-TEST-003",
            "Đoàn kiểm thử",
            DateOnly.FromDateTime(now.AddDays(-1)),
            DateOnly.FromDateTime(now.AddDays(2)),
            1,
            1,
            0,
            now,
            null).AddRoom(Guid.NewGuid(), "103", "STD", null, 1, 0, now);

        var guestId = Guid.NewGuid();
        assignment.AssignGuest(guestId, "  Nguyễn Văn A  ");

        assignment.GuestId.Should().Be(guestId);
        assignment.GuestName.Should().Be("Nguyễn Văn A");
    }

    [Fact]
    public void Cancel_requires_a_reason_and_preserves_existing_notes()
    {
        var now = DateTime.UtcNow;
        var group = GroupReservation.Create(
            Guid.NewGuid(),
            "GRP-TEST-004",
            "Đoàn kiểm thử",
            DateOnly.FromDateTime(now.AddDays(1)),
            DateOnly.FromDateTime(now.AddDays(3)),
            1,
            1,
            0,
            now,
            null);
        group.UpdateDetails(null, null, null, null, null, null, null, null, false, ChargeRouting.CompanyPaysAll, null, "Ghi chú cũ", now, null);

        group.Cancel("Khách hủy chuyến", now);

        group.Status.Should().Be(GroupReservationStatus.Cancelled);
        group.Notes.Should().Contain("Ghi chú cũ").And.Contain("Hủy: Khách hủy chuyến");
    }

    [Fact]
    public void Confirm_rejects_a_group_that_is_already_checked_in()
    {
        var now = DateTime.UtcNow;
        var group = GroupReservation.Create(
            Guid.NewGuid(),
            "GRP-TEST-005",
            "Đoàn kiểm thử",
            DateOnly.FromDateTime(now.AddDays(-1)),
            DateOnly.FromDateTime(now.AddDays(2)),
            1,
            1,
            0,
            now,
            null);
        group.Confirm(now);
        group.MarkCheckedIn(now);

        var act = () => group.Confirm(now);
        act.Should().Throw<InvalidOperationException>().WithMessage("GROUP_STATUS_CheckedIn");
    }

    [Fact]
    public void Close_is_allowed_after_checkout_but_not_while_group_is_active()
    {
        var now = DateTime.UtcNow;
        var group = GroupReservation.Create(
            Guid.NewGuid(),
            "GRP-TEST-CLOSE",
            "Đoàn kiểm thử",
            DateOnly.FromDateTime(now.AddDays(-2)),
            DateOnly.FromDateTime(now.AddDays(-1)),
            1,
            1,
            0,
            now,
            null);

        var activeClose = () => group.Close(now);
        activeClose.Should().Throw<InvalidOperationException>().WithMessage("GROUP_STATUS_Draft");

        group.Confirm(now);
        group.MarkCheckedIn(now);
        group.MarkCheckedOut(now);
        group.Close(now);
        group.Status.Should().Be(GroupReservationStatus.Closed);
    }

    [Fact]
    public void Record_and_reverse_deposit_update_the_group_ledger()
    {
        var now = DateTime.UtcNow;
        var group = GroupReservation.Create(
            Guid.NewGuid(),
            "GRP-TEST-DEPOSIT",
            "Đoàn đặt cọc",
            DateOnly.FromDateTime(now.AddDays(1)),
            DateOnly.FromDateTime(now.AddDays(3)),
            1,
            2,
            0,
            now,
            null);

        group.RecordDeposit(100.005m, now, null);
        group.TotalDeposit.Should().Be(100.01m);

        group.ReverseDeposit(40.004m, now.AddMinutes(1), null);
        group.TotalDeposit.Should().Be(60.01m);
    }

    [Fact]
    public void Deposit_rejects_invalid_amounts_and_over_reversal()
    {
        var now = DateTime.UtcNow;
        var group = GroupReservation.Create(
            Guid.NewGuid(),
            "GRP-TEST-DEPOSIT-VALIDATION",
            "Đoàn đặt cọc",
            DateOnly.FromDateTime(now.AddDays(1)),
            DateOnly.FromDateTime(now.AddDays(3)),
            1,
            1,
            0,
            now,
            null);

        var record = () => group.RecordDeposit(0m, now, null);
        record.Should().Throw<ArgumentException>().WithMessage("GROUP_DEPOSIT_AMOUNT_REQUIRED*");

        group.RecordDeposit(50m, now, null);
        var reverse = () => group.ReverseDeposit(50.01m, now, null);
        reverse.Should().Throw<InvalidOperationException>().WithMessage("GROUP_DEPOSIT_REVERSE_EXCEEDS_TOTAL");
    }

    [Fact]
    public void Room_block_can_be_modified_released_and_picked_up()
    {
        var now = DateTime.UtcNow;
        var block = GroupRoomBlock.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10,
            new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 22), new DateOnly(2026, 9, 13), 1200000m, now, null);

        block.Modify(2, new DateOnly(2026, 9, 14), now.AddMinutes(1), null);
        block.RecordPickup(4, now.AddMinutes(2), null);
        block.Release(3, now.AddMinutes(3), null);

        block.RequestedRooms.Should().Be(12);
        block.BlockedRooms.Should().Be(9);
        block.PickedUpRooms.Should().Be(4);
        block.Status.Should().Be(GroupRoomBlockStatus.Tentative);
        block.CutoffDate.Should().Be(new DateOnly(2026, 9, 14));
    }

    [Fact]
    public void Room_block_rejects_cutoff_after_arrival_and_pickup_above_block()
    {
        var create = () => GroupRoomBlock.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 22), new DateOnly(2026, 9, 21), null, DateTime.UtcNow, null);
        create.Should().Throw<ArgumentException>().WithMessage("GROUP_ROOM_BLOCK_CUTOFF_INVALID*");

        var block = GroupRoomBlock.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 22), null, null, DateTime.UtcNow, null);
        var pickup = () => block.RecordPickup(2, DateTime.UtcNow, null);
        pickup.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deposit_policy_persists_schedule_owner_dates_and_percentages()
    {
        var now = DateTime.UtcNow;
        var propertyId = Guid.NewGuid();
        var group = GroupReservation.Create(
            propertyId,
            "GRP-TEST-SCHEDULE",
            "Đoàn có lịch đặt cọc",
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 22),
            2,
            4,
            0,
            now,
            null);
        var schedules = new[]
        {
            GroupDepositSchedule.Create(propertyId, group.GroupReservationId, new DateOnly(2026, 9, 20), 30m, now, null),
            GroupDepositSchedule.Create(propertyId, group.GroupReservationId, new DateOnly(2026, 10, 1), 70m, now, null)
        };

        group.ConfigureDepositPolicy(true, "Bank Transfer", new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 22), schedules, now, null);

        group.DepositRequired.Should().BeTrue();
        group.GuaranteeType.Should().Be("Bank Transfer");
        group.DepositSchedules.Should().HaveCount(2);
        group.DepositSchedules.Sum(x => x.Percent).Should().Be(100m);
        group.DepositSchedules.Should().OnlyContain(x => x.GroupReservationId == group.GroupReservationId && x.PropertyId == propertyId);
        group.DepositSchedules.Should().OnlyContain(x => x.Status == GroupDepositScheduleStatus.Pending);
    }

    [Fact]
    public void Deposit_policy_rejects_missing_schedule_or_non_100_percent_total()
    {
        var now = DateTime.UtcNow;
        var propertyId = Guid.NewGuid();
        var group = GroupReservation.Create(propertyId, "GRP-TEST-SCHEDULE-VALIDATION", "Đoàn", new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 22), 1, 1, 0, now, null);

        var missing = () => group.ConfigureDepositPolicy(true, "Cash", null, null, Array.Empty<GroupDepositSchedule>(), now, null);
        missing.Should().Throw<ArgumentException>().WithMessage("GROUP_DEPOSIT_SCHEDULE_REQUIRED");

        var schedule = GroupDepositSchedule.Create(propertyId, group.GroupReservationId, new DateOnly(2026, 10, 1), 40m, now, null);
        var total = () => group.ConfigureDepositPolicy(true, "Cash", null, null, new[] { schedule }, now, null);
        total.Should().Throw<ArgumentException>().WithMessage("GROUP_DEPOSIT_SCHEDULE_PERCENT_TOTAL_INVALID");
    }

    [Fact]
    public void Deposit_policy_allows_no_deposit_without_schedules()
    {
        var now = DateTime.UtcNow;
        var group = GroupReservation.Create(Guid.NewGuid(), "GRP-TEST-NO-DEPOSIT", "Đoàn không đặt cọc", new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 22), 1, 1, 0, now, null);

        group.ConfigureDepositPolicy(false, null, null, null, Array.Empty<GroupDepositSchedule>(), now, null);

        group.DepositRequired.Should().BeFalse();
        group.GuaranteeType.Should().BeNull();
        group.DepositSchedules.Should().BeEmpty();
    }
}
