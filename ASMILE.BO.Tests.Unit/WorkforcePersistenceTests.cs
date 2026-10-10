using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class WorkforcePersistenceTests
{
    [Fact]
    public async Task Shift_and_area_changes_round_trip_through_people_directory()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        db.People.Add(new Person
        {
            PersonCode = "WF-REAL-001",
            FullName = "Nhân sự Workforce thật",
            DepartmentId = 1,
            Position = "Nhân viên nhà buồng",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var command = new EfWorkforceCommandService(db);
        await command.ChangeShiftAsync(new WorkforceShiftChange("WF-REAL-001", "Chiều", "Bàn giao ca"));
        await command.ChangeAreaAsync(new WorkforceAreaChange("WF-REAL-001", "B", "5", "Điều phối"));

        var snapshot = (await new EfWorkforceQueryService(db).QueryAsync())
            .Single(x => x.StaffCode == "WF-REAL-001");

        snapshot.Shift.Should().Be("Chiều");
        snapshot.Building.Should().Be("B");
        snapshot.Floor.Should().Be("5");
        snapshot.Status.Should().Be("Chưa xác định");
    }

    [Fact]
    public async Task Workforce_snapshot_joins_today_housekeeping_workload_from_operational_account()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var user = new AppUser
        {
            UserName = "WF-REAL-002",
            DisplayName = "Nhân sự tác nghiệp",
            IsActive = true,
            IsLocked = false,
            PasswordHash = "test"
        };
        db.AppUsers.Add(user);
        db.People.Add(new Person
        {
            PersonCode = user.UserName,
            FullName = user.DisplayName,
            DepartmentId = 1,
            Position = "Nhân viên nhà buồng",
            IsActive = true
        });
        var now = DateTime.UtcNow;
        var task = HousekeepingTask.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "HK-WF-002",
            HousekeepingTaskType.DepartureClean,
            RoomPriority.Rush,
            now,
            ActorId.New());
        task.Assign(user.Id, now.AddMinutes(1));
        task.Accept(now.AddMinutes(2));
        task.StartCleaning(now.AddMinutes(3));
        task.CompleteCleaning(now.AddMinutes(33));
        task.Inspect(HousekeepingInspectionResult.Approved, null, now.AddMinutes(34));
        db.HousekeepingTasks.Add(task);
        await db.SaveChangesAsync();

        var snapshot = (await new EfWorkforceQueryService(db).QueryAsync())
            .Single(x => x.StaffCode == user.UserName);

        snapshot.HasLinkedOperationalAccount.Should().BeTrue();
        snapshot.TotalAssigned.Should().Be(1);
        snapshot.Completed.Should().Be(1);
        snapshot.InProgress.Should().Be(0);
        snapshot.PendingInspection.Should().Be(0);
        snapshot.RushRooms.Should().Be(1);
        snapshot.AvgCleaningMinutes.Should().Be(30);
        snapshot.InspectionPass.Should().Be(1);
        snapshot.InspectionFail.Should().Be(0);
        snapshot.Assignments.Should().ContainSingle(x =>
            x.RoomNumber == task.HotelRoomId.ToString()
            && x.Priority == nameof(RoomPriority.Rush)
            && x.Status == "Completed"
            && x.ActualMinutes == 30);
    }
}
