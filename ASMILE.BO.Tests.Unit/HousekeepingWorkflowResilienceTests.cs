using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class HousekeepingWorkflowResilienceTests
{
    [Fact]
    public async Task Failed_room_inspection_does_not_leave_an_added_state_in_the_context()
    {
        var propertyId = Guid.NewGuid();
        await using var db = CreateDb();
        var room = AddRoom(db, propertyId, "101");
        await db.SaveChangesAsync();
        var service = new EfHousekeepingWorkflowService(db);

        var failed = await service.ExecuteAsync(
            new HkRoomActionRequest(propertyId, room.HotelRoomId, HkRoomAction.Inspect), default);

        failed.IsFailure.Should().BeTrue();
        failed.Error!.Code.Should().Be("HK_ROOM_INSPECTION_NOT_ALLOWED");
        db.RoomHousekeepingStates.Should().BeEmpty();

        var recovered = await service.ExecuteAsync(
            new HkRoomActionRequest(propertyId, room.HotelRoomId, HkRoomAction.MarkDirty), default);

        recovered.IsSuccess.Should().BeTrue();
        (await db.RoomHousekeepingStates.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Invalid_room_action_is_rejected_without_creating_housekeeping_state()
    {
        var propertyId = Guid.NewGuid();
        await using var db = CreateDb();
        var room = AddRoom(db, propertyId, "102");
        await db.SaveChangesAsync();
        var service = new EfHousekeepingWorkflowService(db);

        var failed = await service.ExecuteAsync(
            new HkRoomActionRequest(propertyId, room.HotelRoomId, (HkRoomAction)999), default);

        failed.IsFailure.Should().BeTrue();
        failed.Error!.Code.Should().Be("HK_ROOM_ACTION_INVALID");
        db.RoomHousekeepingStates.Should().BeEmpty();
    }

    [Fact]
    public async Task Failed_task_assignment_does_not_create_a_room_state()
    {
        var propertyId = Guid.NewGuid();
        await using var db = CreateDb();
        var room = AddRoom(db, propertyId, "103");
        var task = HousekeepingTask.Create(
            propertyId,
            room.HotelRoomId,
            "HK-TEST-103",
            HousekeepingTaskType.DepartureClean,
            RoomPriority.Normal,
            DateTime.UtcNow,
            null);
        db.HousekeepingTasks.Add(task);
        await db.SaveChangesAsync();
        var service = new EfHousekeepingWorkflowService(db);

        var failed = await service.ExecuteTaskAsync(
            new HkTaskActionRequest(propertyId, task.HousekeepingTaskId, HkTaskAction.Assign), default);

        failed.IsFailure.Should().BeTrue();
        failed.Error!.Code.Should().Be("HK_TASK_HOUSEKEEPER_REQUIRED");
        db.RoomHousekeepingStates.Should().BeEmpty();
    }

    [Fact]
    public async Task Room_status_changes_are_persisted_as_immutable_history()
    {
        var propertyId = Guid.NewGuid();
        await using var db = CreateDb();
        var room = AddRoom(db, propertyId, "104");
        await db.SaveChangesAsync();
        var service = new EfHousekeepingWorkflowService(db);

        (await service.ExecuteAsync(new HkRoomActionRequest(propertyId, room.HotelRoomId, HkRoomAction.MarkDirty), default)).IsSuccess.Should().BeTrue();
        (await service.ExecuteAsync(new HkRoomActionRequest(propertyId, room.HotelRoomId, HkRoomAction.MarkClean), default)).IsSuccess.Should().BeTrue();

        var history = await db.RoomHousekeepingStatusHistories.ToListAsync();
        history.Should().HaveCount(2);
        var dirtied = history.Single(x => x.ToStatus == HousekeepingRoomStatus.VacantDirty);
        dirtied.FromStatus.Should().BeNull();
        dirtied.Source.Should().Be(nameof(HkRoomAction.MarkDirty));
        var cleaned = history.Single(x => x.ToStatus == HousekeepingRoomStatus.VacantClean);
        cleaned.FromStatus.Should().Be(HousekeepingRoomStatus.VacantDirty);
    }

    private static HotelRoom AddRoom(AppDbContext db, Guid propertyId, string number)
    {
        var roomType = HotelRoomType.Create(
            new PropertyId(propertyId),
            $"STD-{number}",
            "Standard",
            null,
            1,
            0,
            2,
            1,
            1,
            1,
            DateTime.UtcNow,
            null);
        var room = HotelRoom.Create(
            new PropertyId(propertyId),
            roomType.HotelRoomTypeId,
            number,
            null,
            "1",
            null,
            null,
            null,
            DateTime.UtcNow,
            null);
        db.HotelRoomTypes.Add(roomType);
        db.HotelRooms.Add(room);
        return room;
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }
}
