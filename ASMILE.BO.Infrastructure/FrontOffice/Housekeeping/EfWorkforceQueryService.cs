using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

/// <summary>
/// Reads the active staff directory from the canonical People/Departments
/// tables. Operational workload is joined to the AppUser account used by the
/// housekeeping task workflow, using the same code/name/email matching rule
/// used by the real assignment screen. KPI values remain separate until their
/// approved source is available.
/// </summary>
public sealed class EfWorkforceQueryService(AppDbContext db) : IWorkforceQueryService
{
    public async Task<IReadOnlyList<WorkforceStaffSnapshot>> QueryAsync(
        CancellationToken cancellationToken = default)
    {
        var departments = await db.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .ToDictionaryAsync(department => department.Id, cancellationToken);

        var people = await db.People
            .AsNoTracking()
            .Where(person => person.IsActive)
            .OrderBy(person => person.PersonCode)
            .ToListAsync(cancellationToken);

        var activeUsers = await db.AppUsers
            .AsNoTracking()
            .Where(user => user.IsActive && !user.IsLocked)
            .Select(user => new { user.Id, user.UserName, user.DisplayName, user.Email })
            .ToListAsync(cancellationToken);

        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);
        var taskRows = await db.HousekeepingTasks
            .AsNoTracking()
            .Where(task => task.ScheduledDate >= todayUtc
                && task.ScheduledDate < tomorrowUtc
                && task.AssignedHousekeeperId.HasValue)
            .Select(task => new
            {
                task.AssignedHousekeeperId,
                task.HotelRoomId,
                task.Status,
                task.Priority,
                task.InspectionResult,
                task.StartedAtUtc,
                task.CompletedAtUtc,
                task.TotalPauseMinutes
            })
            .ToListAsync(cancellationToken);

        var roomIds = taskRows.Select(task => task.HotelRoomId).Distinct().ToArray();
        var roomNumbers = roomIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await db.HotelRooms
                .AsNoTracking()
                .Where(room => roomIds.Contains(room.HotelRoomId))
                .ToDictionaryAsync(room => room.HotelRoomId, room => room.RoomNumber, cancellationToken);

        var tasksByUser = taskRows
            .Where(task => task.AssignedHousekeeperId.HasValue)
            .GroupBy(task => task.AssignedHousekeeperId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        return people.Select(person =>
        {
            departments.TryGetValue(person.DepartmentId, out var department);
            var departmentName = department?.DepartmentName;
            var role = IsSupervisor(person.Position, departmentName) ? "Supervisor" : "Housekeeper";
            var building = string.IsNullOrWhiteSpace(department?.DepartmentCode)
                ? "—"
                : department.DepartmentCode;

            var user = activeUsers.FirstOrDefault(candidate =>
                string.Equals(candidate.UserName, person.PersonCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.DisplayName, person.FullName, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(person.Email)
                    && string.Equals(candidate.Email, person.Email, StringComparison.OrdinalIgnoreCase)));
            var assignedTasks = user is not null && tasksByUser.TryGetValue(user.Id, out var linkedTasks)
                ? linkedTasks.Where(task => task.Status != HousekeepingTaskStatus.Cancelled).ToList()
                : [];
            var completedTasks = assignedTasks.Where(task => task.Status == HousekeepingTaskStatus.Completed).ToList();
            var inProgressTasks = assignedTasks.Where(task => task.Status is
                HousekeepingTaskStatus.Assigned
                or HousekeepingTaskStatus.Accepted
                or HousekeepingTaskStatus.Cleaning
                or HousekeepingTaskStatus.Paused).ToList();
            var pendingInspectionTasks = assignedTasks
                .Where(task => task.Status == HousekeepingTaskStatus.WaitingInspection)
                .ToList();
            var cleaningMinutes = completedTasks
                .Where(task => task.StartedAtUtc.HasValue && task.CompletedAtUtc.HasValue)
                .Select(task => Math.Max(0, (task.CompletedAtUtc!.Value - task.StartedAtUtc!.Value).TotalMinutes - task.TotalPauseMinutes))
                .ToArray();
            var assignments = assignedTasks
                .Select(task => new WorkforceAssignmentSnapshot(
                    roomNumbers.TryGetValue(task.HotelRoomId, out var roomNumber)
                        ? roomNumber
                        : task.HotelRoomId.ToString(),
                    task.Priority.ToString(),
                    MapAssignmentStatus(task.Status),
                    GetActualCleaningMinutes(task.StartedAtUtc, task.CompletedAtUtc, task.TotalPauseMinutes)))
                .ToList();

            return new WorkforceStaffSnapshot(
                person.Id,
                person.PersonCode,
                person.FullName,
                role,
                string.IsNullOrWhiteSpace(person.WorkforceShift) ? "Chưa phân ca" : person.WorkforceShift,
                string.IsNullOrWhiteSpace(person.WorkforceBuilding) ? building : person.WorkforceBuilding,
                string.IsNullOrWhiteSpace(person.WorkforceFloor) ? department?.Level.ToString() ?? "—" : person.WorkforceFloor,
                NullIfEmpty(person.Phone),
                NullIfEmpty(person.Email),
                departmentName,
                string.IsNullOrWhiteSpace(person.WorkforceStatus) ? "Chưa xác định" : person.WorkforceStatus,
                assignedTasks.Count,
                completedTasks.Count,
                inProgressTasks.Count,
                pendingInspectionTasks.Count,
                assignedTasks.Count(task => task.Priority == RoomPriority.VIP),
                assignedTasks.Count(task => task.Priority == RoomPriority.Rush),
                cleaningMinutes.Length == 0 ? 0 : cleaningMinutes.Average(),
                user is not null,
                assignments,
                assignedTasks.Count(task => task.InspectionResult == HousekeepingInspectionResult.Approved),
                assignedTasks.Count(task => task.InspectionResult == HousekeepingInspectionResult.Rejected));
        }).ToList();
    }

    private static string MapAssignmentStatus(HousekeepingTaskStatus status) => status switch
    {
        HousekeepingTaskStatus.Assigned or HousekeepingTaskStatus.Accepted => "Assigned",
        HousekeepingTaskStatus.Cleaning or HousekeepingTaskStatus.Paused => "InProgress",
        HousekeepingTaskStatus.WaitingInspection => "PendingInspection",
        HousekeepingTaskStatus.Completed => "Completed",
        _ => status.ToString()
    };

    private static int? GetActualCleaningMinutes(DateTime? startedAtUtc, DateTime? completedAtUtc, int pauseMinutes)
    {
        if (!startedAtUtc.HasValue || !completedAtUtc.HasValue)
            return null;

        return Math.Max(0, (int)Math.Floor((completedAtUtc.Value - startedAtUtc.Value).TotalMinutes) - pauseMinutes);
    }

    private static bool IsSupervisor(string? position, string? department)
    {
        var text = $"{position} {department}";
        return text.Contains("supervisor", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("quản lý", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("manager", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
