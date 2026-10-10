using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.TaskShift;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.TaskShift;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.TaskShift;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ASMILE.BO.Core.FrontOffice.TaskShift.TaskStatus;

namespace ASMILE.BO.Tests.Unit;

public sealed class TaskShiftPersistenceTests
{
    [Fact]
    public async Task create_assign_start_complete_round_trips_and_scopes_property()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var clock = new FixedClock(new DateTime(2026, 9, 8, 5, 0, 0, DateTimeKind.Utc));
        var service = new EfTaskShiftService(db, accessor, clock);

        var created = await service.CreateAsync(new CreateTaskAssignmentRequest(
            "Housekeeping", "Housekeeping", "High", "HK", ShiftType.Morning,
            "Clean room", clock.UtcNow.AddHours(1), 45));
        var assigned = await service.AssignAsync(created.TaskAssignmentId, "Mai Lan", null, ShiftType.Morning);
        var started = await service.StartAsync(created.TaskAssignmentId);
        var completed = await service.CompleteAsync(created.TaskAssignmentId);

        completed.Status.Should().Be(TaskStatus.Completed);
        completed.AssignedStaffName.Should().Be("Mai Lan");
        completed.AssignedShift.Should().Be(ShiftType.Morning);
        completed.ActualMinutes.Should().Be(0);
        completed.History.Should().ContainInOrder("Assigned to Mai Lan", "Started", "Completed");
        assigned.Status.Should().Be(TaskStatus.Assigned);
        started.Status.Should().Be(TaskStatus.InProgress);

        var rows = await service.QueryAsync();
        rows.Should().ContainSingle(x => x.TaskAssignmentId == created.TaskAssignmentId);

        using var otherScope = accessor.BeginScope(Context(PropertyId.New()));
        (await service.QueryAsync()).Should().BeEmpty();
    }

    private static FrontOfficeExecutionContext Context(PropertyId id) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "session", "workstation", false),
        new PropertyContext(id, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(id, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime UtcNow { get; } = now;
    }
}
