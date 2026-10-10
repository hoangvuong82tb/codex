using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class HousekeepingDashboardPersistenceTests
{
    [Fact]
    public async Task HK_REAL_001_attendant_dashboard_estimates_remaining_minutes_from_real_history()
    {
        var propertyId = PropertyId.New();
        var actorId = ActorId.New();
        var attendantId = Guid.NewGuid();
        // Keep the fixture on the same UTC calendar day as the production
        // dashboard's "completed today" calculation.
        var now = DateTime.UtcNow.Date.AddHours(8);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var roomType = HotelRoomType.Create(propertyId, "STD", "Standard", null, 1, 0, 2, 1, 1, 1, now, actorId);
        var completedRoom = HotelRoom.Create(propertyId, roomType.HotelRoomTypeId, "101", null, "1", null, null, null, now, actorId);
        var activeRoom = HotelRoom.Create(propertyId, roomType.HotelRoomTypeId, "102", null, "1", null, null, null, now, actorId);
        var completed = HousekeepingTask.Create(propertyId.Value, completedRoom.HotelRoomId, "HK-101", HousekeepingTaskType.DepartureClean, RoomPriority.Normal, now.AddMinutes(-30), actorId);
        completed.Assign(attendantId, now.AddMinutes(-29));
        completed.Accept(now.AddMinutes(-28));
        completed.StartCleaning(now.AddMinutes(-20));
        completed.CompleteCleaning(now);
        completed.Inspect(HousekeepingInspectionResult.Approved, null, now);
        var active = HousekeepingTask.Create(propertyId.Value, activeRoom.HotelRoomId, "HK-102", HousekeepingTaskType.StayoverService, RoomPriority.Rush, now, actorId);
        active.Assign(attendantId, now);

        db.AddRange(roomType, completedRoom, activeRoom, completed, active);
        await db.SaveChangesAsync();

        var service = new EfHousekeepingBoardService(db);
        var dashboard = await service.GetAttendantDashboardAsync(propertyId.Value, attendantId, CancellationToken.None);

        dashboard.AssignedRooms.Should().Be(2);
        dashboard.CompletedToday.Should().Be(1);
        dashboard.RemainingRooms.Should().Be(1);
        dashboard.AvgTimePerRoom.Should().Be(20m);
        dashboard.EstimatedMinutesLeft.Should().Be(20);
        dashboard.CurrentTasks.Should().ContainSingle(x => x.TaskNumber == "HK-102");
        dashboard.NextUp.Should().ContainSingle(x => x.TaskNumber == "HK-102");
    }
}
