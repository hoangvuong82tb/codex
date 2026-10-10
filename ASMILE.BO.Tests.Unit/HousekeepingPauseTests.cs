using ASMILE.BO.Core.FrontOffice.Housekeeping;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class HousekeepingPauseTests
{
    [Fact]
    public void Pause_and_resume_accumulate_pause_duration()
    {
        var started = DateTime.UtcNow;
        var paused = started.AddMinutes(12);
        var resumed = paused.AddMinutes(7);
        var task = HousekeepingTask.Create(
            Guid.NewGuid(), Guid.NewGuid(), "HK-PAUSE-001",
            HousekeepingTaskType.DepartureClean, RoomPriority.Normal, started, null);

        task.Assign(Guid.NewGuid(), started);
        task.Accept(started);
        task.StartCleaning(started);
        task.Pause("Thiếu ga giường", paused);
        task.Status.Should().Be(HousekeepingTaskStatus.Paused);
        task.PauseReason.Should().Be("Thiếu ga giường");

        task.Resume(resumed);

        task.Status.Should().Be(HousekeepingTaskStatus.Cleaning);
        task.TotalPauseMinutes.Should().Be(7);
        task.ResumedAtUtc.Should().Be(resumed);
    }

    [Fact]
    public void Completing_while_paused_closes_pause_before_waiting_inspection()
    {
        var started = DateTime.UtcNow;
        var task = HousekeepingTask.Create(
            Guid.NewGuid(), Guid.NewGuid(), "HK-PAUSE-002",
            HousekeepingTaskType.DepartureClean, RoomPriority.Normal, started, null);

        task.Assign(Guid.NewGuid(), started);
        task.Accept(started);
        task.StartCleaning(started);
        task.Pause(null, started.AddMinutes(5));
        task.CompleteCleaning(started.AddMinutes(20));

        task.Status.Should().Be(HousekeepingTaskStatus.WaitingInspection);
        task.TotalPauseMinutes.Should().Be(15);
        task.CompletedAtUtc.Should().Be(started.AddMinutes(20));
    }
}
