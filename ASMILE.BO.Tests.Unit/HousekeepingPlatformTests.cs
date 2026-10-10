using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class HousekeepingPlatformTests
{
    [Fact] public void HK_001_status_machine_allows_valid_transition() => HkStatusMachine.IsValidTransition(HkRoomStatus.VacantDirty, HkRoomStatus.CleaningInProgress).Should().BeTrue();
    [Fact] public void HK_002_status_machine_rejects_invalid_transition() => HkStatusMachine.IsValidTransition(HkRoomStatus.VacantClean, HkRoomStatus.CleaningInProgress).Should().BeFalse();
    [Fact] public void HK_003_status_machine_returns_allowed_next() => HkStatusMachine.AllowedNext(HkRoomStatus.VacantDirty).Should().Contain(HkRoomStatus.CleaningInProgress);
    [Fact] public void HK_004_status_machine_vacant_clean_next() => HkStatusMachine.AllowedNext(HkRoomStatus.VacantClean).Should().Contain([HkRoomStatus.OccupiedClean, HkRoomStatus.MaintenancePending, HkRoomStatus.DoNotDisturb]);
    [Fact]
    public void HK_005_minibar_session_requires_check_before_posting()
    {
        var session = MinibarSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var act = () => session.Post(Guid.NewGuid(), DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("MINIBAR_SESSION_NOT_CHECKED");
        session.Status.Should().Be("Open");
    }

    [Fact]
    public void HK_006_minibar_session_check_post_unpost_round_trip()
    {
        var actorId = Guid.NewGuid();
        var folioEntryId = Guid.NewGuid();
        var session = MinibarSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        session.Check(actorId, DateTime.UtcNow);
        session.Status.Should().Be("Checked");
        session.CheckedBy.Should().Be(actorId);

        session.Post(folioEntryId, DateTime.UtcNow);
        session.Status.Should().Be("Posted");
        session.FolioEntryId.Should().Be(folioEntryId);

        session.Unpost(DateTime.UtcNow);
        session.Status.Should().Be("Checked");
        session.FolioEntryId.Should().BeNull();
        session.PostedAtUtc.Should().BeNull();
    }

    [Fact]
    public void HK_007_minibar_session_cannot_be_checked_after_posting()
    {
        var session = MinibarSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        session.Check(Guid.NewGuid(), DateTime.UtcNow);
        session.Post(Guid.NewGuid(), DateTime.UtcNow);

        var act = () => session.Check(Guid.NewGuid(), DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("MINIBAR_SESSION_ALREADY_POSTED");
    }

    [Fact]
    public void HK_008_reassign_keeps_active_task_status_and_changes_housekeeper()
    {
        var first = Guid.NewGuid();
        var replacement = Guid.NewGuid();
        var task = HousekeepingTask.Create(
            Guid.NewGuid(), Guid.NewGuid(), "HK-TEST-REASSIGN",
            HousekeepingTaskType.DepartureClean, RoomPriority.Normal,
            DateTime.UtcNow, null);

        task.Assign(first, DateTime.UtcNow);
        task.Accept(DateTime.UtcNow);
        task.StartCleaning(DateTime.UtcNow);
        task.Reassign(replacement, DateTime.UtcNow);

        task.Status.Should().Be(HousekeepingTaskStatus.Cleaning);
        task.AssignedHousekeeperId.Should().Be(replacement);
    }

    [Fact]
    public void HK_009_reassign_rejects_terminal_task()
    {
        var task = HousekeepingTask.Create(
            Guid.NewGuid(), Guid.NewGuid(), "HK-TEST-REASSIGN-INVALID",
            HousekeepingTaskType.DepartureClean, RoomPriority.Normal,
            DateTime.UtcNow, null);

        var act = () => task.Reassign(Guid.NewGuid(), DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("HK_TASK_STATUS_Pending");
    }

    [Fact]
    public void HK_010_priority_can_change_for_active_task_but_not_terminal_task()
    {
        var task = HousekeepingTask.Create(
            Guid.NewGuid(), Guid.NewGuid(), "HK-TEST-PRIORITY",
            HousekeepingTaskType.DepartureClean, RoomPriority.Normal,
            DateTime.UtcNow, null);

        task.SetPriority(RoomPriority.Rush, DateTime.UtcNow);
        task.Priority.Should().Be(RoomPriority.Rush);

        task.Assign(Guid.NewGuid(), DateTime.UtcNow);
        task.Accept(DateTime.UtcNow);
        task.StartCleaning(DateTime.UtcNow);
        task.CompleteCleaning(DateTime.UtcNow);
        task.Inspect(HousekeepingInspectionResult.Approved, null, DateTime.UtcNow);

        var act = () => task.SetPriority(RoomPriority.VIP, DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("HK_TASK_STATUS_Completed");
    }
}
