using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.LostFound;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

/// <summary>
/// Read-only housekeeping board backed by the hotel room/stay tables.
/// Room and housekeeping task boards are read from the operational EF model.
/// Metrics that do not have a persisted source remain explicitly empty rather
/// than being fabricated for staff.
/// </summary>
public sealed class EfHousekeepingBoardService : IHousekeepingBoardService
{
    private readonly AppDbContext _db;

    public EfHousekeepingBoardService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<HkRoomSnapshot>> GetRoomSnapshotAsync(Guid propertyId, CancellationToken ct)
    {
        var rooms = await _db.HotelRooms.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.RoomNumber)
            .Select(x => new
            {
                x.HotelRoomId,
                x.RoomNumber,
                x.Floor,
                x.Zone,
                x.OperationalStatus,
                x.HousekeepingStatus,
                x.RowVersion
            })
            .ToListAsync(ct);

        // RoomHousekeepingState is the authoritative operational source when
        // it exists. HotelRoom.HousekeepingStatus remains a compatibility
        // fallback for properties that have not backfilled the state table yet.
        var stateByRoom = await _db.Set<RoomHousekeepingState>()
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .ToDictionaryAsync(x => x.HotelRoomId, ct);

        return rooms.Select(x => new HkRoomSnapshot(
            x.HotelRoomId,
            x.RoomNumber,
            x.Floor,
            x.Zone,
            x.OperationalStatus.ToString(),
            stateByRoom.TryGetValue(x.HotelRoomId, out var state)
                ? state.Status.ToString()
                : x.HousekeepingStatus,
            state?.RowVersion ?? x.RowVersion)).ToList();
    }

    public async Task<HkExecutiveDashboard> GetExecutiveDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var roomSnapshots = await GetRoomSnapshotAsync(propertyId, ct);
        var rooms = roomSnapshots
            .Select(x => new RoomSnapshot(
                x.RoomId,
                x.Floor,
                x.Zone,
                ParseOperationalStatus(x.OperationalStatus),
                x.HousekeepingStatus))
            .ToList();
        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc);
        var stays = await _db.HotelStays.AsNoTracking()
            .Where(x => x.PropertyId == propertyId &&
                        (x.ScheduledArrivalDate == today || x.ScheduledDepartureDate == today))
            .Select(x => new StaySnapshot(x.ScheduledArrivalDate, x.ScheduledDepartureDate))
            .ToListAsync(ct);
        var rows = await LoadTaskRowsAsync(propertyId, null, null, null, ct);
        var durations = rows.Where(x => x.StartedAtUtc.HasValue && x.CompletedAtUtc.HasValue)
            .Select(x => (x.CompletedAtUtc!.Value - x.StartedAtUtc!.Value).TotalMinutes)
            .Where(x => x >= 0).ToList();
        var inspections = rows.Where(x => x.InspectionResult.HasValue).ToList();
        var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lostFoundActive = await _db.LostFoundItems.AsNoTracking()
            .Where(x => x.PropertyId == propertyId &&
                        x.Status != LostFoundStatus.Returned &&
                        x.Status != LostFoundStatus.Disposed &&
                        x.Status != LostFoundStatus.Archived)
            .CountAsync(ct);
        var linenDiscardedThisMonth = await _db.HousekeepingLinenTransactions.AsNoTracking()
            .Where(x => x.PropertyId == propertyId &&
                        x.OccurredAtUtc >= monthStart &&
                        x.Action == nameof(LinenAction.Discarded))
            .SumAsync(x => (int?)x.Quantity, ct) ?? 0;
        var amenityRefillsToday = await _db.HousekeepingAmenityRefillRequests.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.RequestedAtUtc.Date == nowUtc.Date)
            .CountAsync(ct);
        var dashboard = BuildExecutive(rooms, stays, lostFoundActive, linenDiscardedThisMonth, amenityRefillsToday);
        return dashboard with
        {
            ActiveAttendants = rows.Where(x => x.AssignedHousekeeperId.HasValue && x.Status is not HousekeepingTaskStatus.Completed and not HousekeepingTaskStatus.Cancelled)
                .Select(x => x.AssignedHousekeeperId!.Value).Distinct().Count(),
            CompletedTasks = rows.Count(x => x.Status == HousekeepingTaskStatus.Completed),
            PendingInspections = rows.Count(x => x.Status == HousekeepingTaskStatus.WaitingInspection),
            AvgCleaningMinutes = durations.Count == 0 ? 0m : Math.Round((decimal)durations.Average(), 2),
            InspectionPassRate = inspections.Count == 0 ? 0m : Math.Round((decimal)inspections.Count(x => x.InspectionResult == HousekeepingInspectionResult.Approved) / inspections.Count * 100m, 2)
        };
    }

    public async Task<HkSupervisorDashboard> GetSupervisorDashboardAsync(Guid propertyId, Guid supervisorId, CancellationToken ct)
    {
        var rows = await LoadTaskRowsAsync(propertyId, null, null, null, ct);
        var board = MapTasks(rows);
        var supervisorName = await _db.AppUsers.AsNoTracking()
            .Where(x => x.Id == supervisorId)
            .Select(x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.UserName : x.DisplayName)
            .SingleOrDefaultAsync(ct);
        var attendants = rows.Where(x => x.AssignedHousekeeperId.HasValue &&
                                         x.Status is not HousekeepingTaskStatus.Completed and not HousekeepingTaskStatus.Cancelled)
            .GroupBy(x => x.AssignedHousekeeperId!.Value)
            .Select(g => new AttendantDto(g.Key, g.First().AssignedName ?? "—", g.First().Zone, g.First().Floor,
                0, g.Count(),
                0, []))
            .ToList();
        var durations = rows.Where(x => x.StartedAtUtc.HasValue && x.CompletedAtUtc.HasValue)
            .Select(x => (x.CompletedAtUtc!.Value - x.StartedAtUtc!.Value).TotalMinutes).Where(x => x >= 0).ToList();
        var inspections = rows.Where(x => x.InspectionResult.HasValue).ToList();
        return new HkSupervisorDashboard(string.IsNullOrWhiteSpace(supervisorName) ? "—" : supervisorName, null, null, attendants,
            board.Where(x => x.HasInspectionPending).ToList(), board.Where(x => x.IsOverdue).ToList(),
            durations.Count == 0 ? 0m : Math.Round((decimal)durations.Average(), 2),
            inspections.Count == 0 ? 0m : Math.Round((decimal)inspections.Count(x => x.InspectionResult == HousekeepingInspectionResult.Approved) / inspections.Count * 100m, 2));
    }

    public async Task<HkAttendantDashboard> GetAttendantDashboardAsync(Guid propertyId, Guid attendantId, CancellationToken ct)
    {
        var rows = (await LoadTaskRowsAsync(propertyId, null, null, null, ct)).Where(x => x.AssignedHousekeeperId == attendantId).ToList();
        var board = MapTasks(rows);
        var attendantName = await _db.AppUsers.AsNoTracking()
            .Where(x => x.Id == attendantId)
            .Select(x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.UserName : x.DisplayName)
            .SingleOrDefaultAsync(ct);
        var durations = rows.Where(x => x.StartedAtUtc.HasValue && x.CompletedAtUtc.HasValue)
            .Select(x => (x.CompletedAtUtc!.Value - x.StartedAtUtc!.Value).TotalMinutes).Where(x => x >= 0).ToList();
        var averageMinutes = durations.Count == 0 ? 0m : Math.Round((decimal)durations.Average(), 2);
        var active = rows.Count(x => x.Status is not HousekeepingTaskStatus.Completed and not HousekeepingTaskStatus.Cancelled);
        var completedToday = rows.Count(x => x.Status == HousekeepingTaskStatus.Completed && x.CompletedAtUtc?.Date == DateTime.UtcNow.Date);
        var currentTasks = board.Where(x => x.TaskStatus is not HousekeepingTaskStatus.Completed and not HousekeepingTaskStatus.Cancelled).ToList();
        var nextUp = currentTasks
            .Where(x => x.TaskStatus is HousekeepingTaskStatus.Pending or HousekeepingTaskStatus.Assigned)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.TaskNumber)
            .Take(10)
            .ToList();
        var estimatedMinutesLeft = averageMinutes <= 0m ? 0 : (int)Math.Round(active * averageMinutes, MidpointRounding.AwayFromZero);
        return new HkAttendantDashboard(attendantId, string.IsNullOrWhiteSpace(attendantName) ? "—" : attendantName, rows.Count, completedToday, active,
            averageMinutes, estimatedMinutesLeft, currentTasks, nextUp);
    }

    public async Task<HkFloorDashboard> GetFloorDashboardAsync(Guid propertyId, string floor, CancellationToken ct)
    {
        var rooms = (await GetRoomSnapshotAsync(propertyId, ct))
            .Where(x => string.Equals(x.Floor, floor, StringComparison.OrdinalIgnoreCase))
            .Select(x => new RoomSnapshot(
                x.RoomId,
                x.Floor,
                x.Zone,
                ParseOperationalStatus(x.OperationalStatus),
                x.HousekeepingStatus))
            .ToList();
        var counts = rooms.GroupBy(x => MapStatus(x.OperationalStatus, x.HousekeepingStatus))
            .ToDictionary(x => x.Key, x => x.Count());
        var ready = Count(counts, HkRoomStatus.VacantClean) + Count(counts, HkRoomStatus.Inspected) + Count(counts, HkRoomStatus.ReadyForSale);
        var taskRows = await LoadTaskRowsAsync(propertyId, floor, null, null, ct);
        var activeTasks = MapTasks(taskRows).Where(x => x.TaskStatus is not HousekeepingTaskStatus.Completed and not HousekeepingTaskStatus.Cancelled).ToList();
        var attention = rooms.Where(x => MapStatus(x.OperationalStatus, x.HousekeepingStatus) is HkRoomStatus.VacantDirty or HkRoomStatus.OccupiedDirty or HkRoomStatus.OutOfOrder or HkRoomStatus.OutOfService)
            .Select(x => x.RoomId).ToHashSet();
        foreach (var row in taskRows.Where(x => x.Status is not HousekeepingTaskStatus.Completed and not HousekeepingTaskStatus.Cancelled))
            attention.Add(row.RoomId);
        return new HkFloorDashboard(floor, rooms.Count,
            Count(counts, HkRoomStatus.OccupiedClean) + Count(counts, HkRoomStatus.OccupiedDirty),
            Count(counts, HkRoomStatus.VacantClean) + Count(counts, HkRoomStatus.VacantDirty),
            Count(counts, HkRoomStatus.CleaningInProgress), ready, activeTasks, attention.ToList());
    }

    public async Task<IReadOnlyList<HkTaskBoardDto>> GetTaskBoardAsync(Guid propertyId, string? floor, string? zone, RoomPriority? priority, CancellationToken ct)
        => MapTasks(await LoadTaskRowsAsync(propertyId, floor, zone, priority, ct));

    private async Task<IReadOnlyList<TaskRow>> LoadTaskRowsAsync(Guid propertyId, string? floor, string? zone, RoomPriority? priority, CancellationToken ct)
    {
        var rooms = await _db.HotelRooms.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .Select(x => new { x.HotelRoomId, x.RoomNumber, x.Floor, x.Zone, x.OperationalStatus, x.HousekeepingStatus })
            .ToListAsync(ct);
        var roomById = rooms.ToDictionary(x => x.HotelRoomId);
        var tasks = await _db.Set<HousekeepingTask>().AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.HousekeepingTaskId, x.HotelRoomId, x.TaskNumber, x.TaskType, x.Status, x.Priority,
                x.AssignedHousekeeperId, x.CreatedAtUtc, x.StartedAtUtc, x.CompletedAtUtc,
                x.InspectedAtUtc, x.InspectionResult
            })
            .ToListAsync(ct);

        var inspectionByTask = tasks.Count == 0
            ? new Dictionary<Guid, RoomInspection>()
            : await _db.RoomInspections.AsNoTracking()
                .Where(x => x.PropertyId == propertyId && tasks.Select(t => t.HousekeepingTaskId).Contains(x.HousekeepingTaskId))
                .ToDictionaryAsync(x => x.HousekeepingTaskId, ct);

        var assignedUserIds = tasks
            .Where(x => x.AssignedHousekeeperId.HasValue)
            .Select(x => x.AssignedHousekeeperId!.Value)
            .Distinct()
            .ToArray();
        var assignedNames = assignedUserIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _db.AppUsers.AsNoTracking()
                .Where(x => assignedUserIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.UserName : x.DisplayName, ct);
        var inspectorUserIds = inspectionByTask.Values
            .Where(x => x.InspectorId.HasValue)
            .Select(x => x.InspectorId!.Value)
            .Distinct()
            .ToArray();
        var inspectorNames = inspectorUserIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _db.AppUsers.AsNoTracking()
                .Where(x => inspectorUserIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.UserName : x.DisplayName, ct);

        return tasks.Where(x => roomById.TryGetValue(x.HotelRoomId, out var room) &&
                                (string.IsNullOrWhiteSpace(floor) || string.Equals(room.Floor, floor, StringComparison.OrdinalIgnoreCase)) &&
                                (string.IsNullOrWhiteSpace(zone) || string.Equals(room.Zone, zone, StringComparison.OrdinalIgnoreCase)) &&
                                roomById.ContainsKey(x.HotelRoomId))
            .Select(x =>
            {
                var room = roomById[x.HotelRoomId];
                inspectionByTask.TryGetValue(x.HousekeepingTaskId, out var inspection);
                return new TaskRow(x.HousekeepingTaskId, x.HotelRoomId, room.RoomNumber, room.Floor, room.Zone,
                    room.OperationalStatus.ToString(), room.HousekeepingStatus, x.TaskNumber, x.TaskType, x.Status,
                    x.Priority, x.AssignedHousekeeperId, x.CreatedAtUtc, x.StartedAtUtc, x.CompletedAtUtc,
                    x.InspectedAtUtc, x.InspectionResult,
                    x.AssignedHousekeeperId.HasValue && assignedNames.TryGetValue(x.AssignedHousekeeperId.Value, out var name) ? name : null,
                    inspection,
                    inspection?.InspectorId is Guid inspectorId && inspectorNames.TryGetValue(inspectorId, out var inspectorName)
                        ? inspectorName
                        : null);
            })
            .Where(x => !priority.HasValue || x.Priority == priority.Value)
            .ToList();
    }

    private static IReadOnlyList<HkTaskBoardDto> MapTasks(IReadOnlyList<TaskRow> rows)
        => rows.Select(x =>
        {
            var roomStatus = MapStatus(ParseOperationalStatus(x.OperationalStatus), x.HousekeepingStatus);
            var pendingInspection = x.Status == HousekeepingTaskStatus.WaitingInspection;
            // There is no persisted due-at/SLA field in HousekeepingTask yet;
            // do not infer overdue state from creation time.
            return new HkTaskBoardDto(x.TaskId, x.TaskNumber, x.RoomNumber, roomStatus, x.TaskType, x.Status,
                x.Priority, x.AssignedHousekeeperId?.ToString(), x.AssignedName, x.StartedAtUtc, null, false,
                x.Priority == RoomPriority.VIP, pendingInspection, x.RoomId, x.InspectionResult,
                x.AssignedHousekeeperId, x.Inspection?.InspectorId, x.Inspection?.InspectedAtUtc,
                x.Inspection?.Notes, x.Inspection?.ChecklistData, x.InspectorName);
        }).ToList();

    private static HkExecutiveDashboard BuildExecutive(
        IReadOnlyList<RoomSnapshot> rooms,
        IReadOnlyList<StaySnapshot> stays,
        int lostFoundActive,
        int linenDiscardedThisMonth,
        int amenityRefillsToday)
    {
        var counts = rooms.GroupBy(x => MapStatus(x.OperationalStatus, x.HousekeepingStatus))
            .ToDictionary(x => x.Key, x => x.Count());
        var total = rooms.Count;
        var ready = Count(counts, HkRoomStatus.VacantClean) + Count(counts, HkRoomStatus.Inspected) + Count(counts, HkRoomStatus.ReadyForSale);
        var readiness = total == 0 ? 0m : Math.Round((decimal)ready / total * 100m, 2);
        return new HkExecutiveDashboard(total, Count(counts, HkRoomStatus.VacantClean), Count(counts, HkRoomStatus.VacantDirty),
            Count(counts, HkRoomStatus.OccupiedClean), Count(counts, HkRoomStatus.OccupiedDirty),
            Count(counts, HkRoomStatus.CleaningInProgress), Count(counts, HkRoomStatus.Inspected),
            Count(counts, HkRoomStatus.OutOfOrder), Count(counts, HkRoomStatus.OutOfService), readiness,
            stays.Count(x => x.Arrival == DateOnly.FromDateTime(DateTime.UtcNow)),
            stays.Count(x => x.Departure == DateOnly.FromDateTime(DateTime.UtcNow)), 0, 0, 0, 0m, 0m,
            lostFoundActive, linenDiscardedThisMonth, amenityRefillsToday);
    }

    private static int Count(IReadOnlyDictionary<HkRoomStatus, int> counts, HkRoomStatus status)
        => counts.TryGetValue(status, out var value) ? value : 0;

    private static HkRoomStatus MapStatus(HotelRoomOperationalStatus operationalStatus, string? status)
    {
        if (operationalStatus == HotelRoomOperationalStatus.OutOfOrder) return HkRoomStatus.OutOfOrder;
        if (operationalStatus == HotelRoomOperationalStatus.OutOfService) return HkRoomStatus.OutOfService;
        var key = new string((status ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return key switch
        {
            "vacantclean" or "vc" or "ready" => HkRoomStatus.VacantClean,
            "vacantdirty" or "vd" => HkRoomStatus.VacantDirty,
            "occupiedclean" or "oc" => HkRoomStatus.OccupiedClean,
            "occupieddirty" or "od" => HkRoomStatus.OccupiedDirty,
            "cleaning" or "cleaninginprogress" => HkRoomStatus.CleaningInProgress,
            "inspected" => HkRoomStatus.Inspected,
            "readyforsale" => HkRoomStatus.ReadyForSale,
            "pickup" => HkRoomStatus.Pickup,
            "turndown" => HkRoomStatus.Turndown,
            "dnd" or "donotdisturb" => HkRoomStatus.DoNotDisturb,
            "sleepout" => HkRoomStatus.SleepOut,
            "blocked" => HkRoomStatus.Blocked,
            "ooo" => HkRoomStatus.OutOfOrder,
            "oos" => HkRoomStatus.OutOfService,
            _ => HkRoomStatus.VacantClean
        };
    }

    private static HotelRoomOperationalStatus ParseOperationalStatus(string? value)
        => Enum.TryParse<HotelRoomOperationalStatus>(value, ignoreCase: true, out var status)
            ? status
            : HotelRoomOperationalStatus.Available;

    private sealed record RoomSnapshot(Guid RoomId, string? Floor, string? Zone, HotelRoomOperationalStatus OperationalStatus, string? HousekeepingStatus);
    private sealed record StaySnapshot(DateOnly Arrival, DateOnly Departure);
    private sealed record TaskRow(
        Guid TaskId, Guid RoomId, string RoomNumber, string? Floor, string? Zone,
        string OperationalStatus, string? HousekeepingStatus, string TaskNumber,
        HousekeepingTaskType TaskType, HousekeepingTaskStatus Status, RoomPriority Priority,
        Guid? AssignedHousekeeperId, DateTime CreatedAtUtc, DateTime? StartedAtUtc,
        DateTime? CompletedAtUtc, DateTime? InspectedAtUtc, HousekeepingInspectionResult? InspectionResult,
        string? AssignedName, RoomInspection? Inspection, string? InspectorName);
}
