using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

/// <summary>
/// Persists the room-status actions used by the Housekeeping room board.
/// The board and the workflow share the same EF model so a status change is
/// visible after refresh and is protected by EF optimistic concurrency.
/// </summary>
public sealed class EfHousekeepingWorkflowService(AppDbContext db) : IHousekeepingWorkflowService
{
    public async Task<IReadOnlyList<HkAssigneeDto>> GetActiveAssigneesAsync(CancellationToken ct)
    {
        return await db.AppUsers
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.UserName)
            .Select(user => new HkAssigneeDto(
                user.Id,
                user.UserName,
                string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName))
            .ToListAsync(ct);
    }

    public async Task<Result<HkWorkflowResult>> ExecuteAsync(HkRoomActionRequest request, CancellationToken ct)
    {
        if (request.PropertyId == Guid.Empty || request.RoomId == Guid.Empty)
            return Result<HkWorkflowResult>.Failure(Error.Validation("HK_ROOM_SCOPE_REQUIRED", "Cơ sở và phòng là bắt buộc."));

        var room = await db.HotelRooms.SingleOrDefaultAsync(
            x => x.PropertyId == request.PropertyId && x.HotelRoomId == request.RoomId && x.IsActive, ct);
        if (room is null)
            return Result<HkWorkflowResult>.Failure(Error.Business("HK_ROOM_NOT_FOUND", "Không tìm thấy phòng trong cơ sở hiện tại."));

        if (!Enum.IsDefined(request.Action))
            return Result<HkWorkflowResult>.Failure(Error.Validation("HK_ROOM_ACTION_INVALID", "Thao tác Housekeeping không hợp lệ."));

        var existingState = await db.RoomHousekeepingStates.SingleOrDefaultAsync(
            x => x.PropertyId == request.PropertyId && x.HotelRoomId == request.RoomId, ct);

        // Validate preconditions before creating a missing state entity. The
        // DbContext is scoped to the workspace and can outlive this command;
        // leaving an Added state behind after an early failure could make the
        // next command attempt a duplicate insert.
        if (request.Action == HkRoomAction.Inspect
            && (existingState is null || existingState.Status is not (HousekeepingRoomStatus.Cleaning or HousekeepingRoomStatus.Inspected)))
        {
            return Result<HkWorkflowResult>.Failure(Error.Business("HK_ROOM_INSPECTION_NOT_ALLOWED", "Phòng chưa ở trạng thái chờ kiểm tra."));
        }

        if (request.Action == HkRoomAction.ClearOperationalHold
            && room.OperationalStatus is not (HotelRoomOperationalStatus.OutOfOrder or HotelRoomOperationalStatus.OutOfService))
        {
            return Result<HkWorkflowResult>.Failure(Error.Business("HK_ROOM_NOT_ON_HOLD", "Phòng không ở trạng thái OOO/OOS."));
        }

        var state = existingState;
        var stateAdded = false;
        if (state is null)
        {
            state = RoomHousekeepingState.Create(request.RoomId, request.PropertyId, DateTime.UtcNow);
            db.RoomHousekeepingStates.Add(state);
            stateAdded = true;
        }

        var now = DateTime.UtcNow;
        var previousStatus = stateAdded ? (HousekeepingRoomStatus?)null : state.Status;
        Guid? taskId = null;
        try
        {
            switch (request.Action)
            {
                case HkRoomAction.MarkClean:
                    if (state.Status == HousekeepingRoomStatus.OccupiedDirty) state.MarkOccupied(now);
                    else state.MarkClean(now);
                    break;
                case HkRoomAction.MarkDirty:
                    state.MarkDirty(now);
                    break;
                case HkRoomAction.SendToHousekeeping:
                    state.MarkDirty(now);
                    var checkoutRule = await GetActiveHousekeepingRuleAsync(request.PropertyId, HotelHousekeepingRuleKind.CheckOutCleaning, now, ct);
                    if (checkoutRule?.AutoCreateTask ?? true)
                    {
                        var activeTask = await db.HousekeepingTasks.AnyAsync(
                            x => x.PropertyId == request.PropertyId && x.HotelRoomId == request.RoomId
                                && x.Status != HousekeepingTaskStatus.Completed
                                && x.Status != HousekeepingTaskStatus.Cancelled, ct);
                        if (!activeTask)
                        {
                            var task = HousekeepingTask.Create(
                                request.PropertyId,
                                request.RoomId,
                                $"HK-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..27],
                                HousekeepingTaskType.DepartureClean,
                                RoomPriority.Normal,
                                now,
                                null);
                            db.HousekeepingTasks.Add(task);
                            taskId = task.HousekeepingTaskId;
                        }
                    }
                    break;
                case HkRoomAction.Inspect:
                    state.MarkInspected(now);
                    break;
                case HkRoomAction.SetOutOfOrder:
                    room.ChangeOperationalStatus(HotelRoomOperationalStatus.OutOfOrder, now, null);
                    state.MarkOutOfOrder("Room board action", now);
                    break;
                case HkRoomAction.SetOutOfService:
                    room.ChangeOperationalStatus(HotelRoomOperationalStatus.OutOfService, now, null);
                    state.MarkOutOfService("Room board action", now);
                    break;
                case HkRoomAction.ClearOperationalHold:
                    room.ChangeOperationalStatus(HotelRoomOperationalStatus.Available, now, null);
                    state.MarkClean(now);
                    break;
                default:
                    // Enum.IsDefined above makes this unreachable, but keep a
                    // defensive failure if a future enum value is introduced
                    // without a corresponding workflow case.
                    return Result<HkWorkflowResult>.Failure(Error.Validation("HK_ROOM_ACTION_INVALID", "Thao tác Housekeeping không hợp lệ."));
            }

            RecordRoomStatusHistory(state, previousStatus, request.Action.ToString(), taskId, now);
            await db.SaveChangesAsync(ct);
            return Result<HkWorkflowResult>.Success(new(
                room.HotelRoomId,
                request.Action,
                room.OperationalStatus,
                state.Status,
                taskId,
                state.RowVersion ?? room.RowVersion));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<HkWorkflowResult>.Failure(Error.Concurrency(
                "HK_ROOM_CONCURRENCY_CONFLICT",
                "Phòng vừa được cập nhật bởi người dùng khác. Vui lòng tải lại."));
        }
        catch (InvalidOperationException ex)
        {
            return Result<HkWorkflowResult>.Failure(Error.Business("HK_ROOM_ACTION_NOT_ALLOWED", ex.Message));
        }
    }

    public async Task<Result<Guid>> CreateTaskAsync(HkTaskCreateRequest request, CancellationToken ct)
    {
        if (request.PropertyId == Guid.Empty || request.RoomId == Guid.Empty)
            return Result<Guid>.Failure(Error.Validation("HK_TASK_SCOPE_REQUIRED", "Cơ sở và phòng là bắt buộc."));

        var roomExists = await db.HotelRooms.AsNoTracking().AnyAsync(
            x => x.PropertyId == request.PropertyId && x.HotelRoomId == request.RoomId && x.IsActive, ct);
        if (!roomExists)
            return Result<Guid>.Failure(Error.Business("HK_ROOM_NOT_FOUND", "Không tìm thấy phòng trong cơ sở hiện tại."));

        var activeTaskExists = await db.HousekeepingTasks.AsNoTracking().AnyAsync(
            x => x.PropertyId == request.PropertyId && x.HotelRoomId == request.RoomId
                && x.Status != HousekeepingTaskStatus.Completed
                && x.Status != HousekeepingTaskStatus.Cancelled, ct);
        if (activeTaskExists)
            return Result<Guid>.Failure(Error.Business("HK_TASK_ALREADY_ACTIVE", "Phòng đã có một yêu cầu Housekeeping đang xử lý."));

        var now = DateTime.UtcNow;
        var task = HousekeepingTask.Create(
            request.PropertyId,
            request.RoomId,
            $"HK-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..34],
            request.TaskType,
            request.Priority,
            now,
            null);
        db.HousekeepingTasks.Add(task);

        try
        {
            await db.SaveChangesAsync(ct);
            return Result<Guid>.Success(task.HousekeepingTaskId);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<Guid>.Failure(Error.Concurrency(
                "HK_TASK_CONCURRENCY_CONFLICT",
                "Yêu cầu vừa được tạo hoặc cập nhật bởi người dùng khác."));
        }
    }

    public async Task<Result<HkTaskWorkflowResult>> ExecuteTaskAsync(HkTaskActionRequest request, CancellationToken ct)
    {
        if (request.PropertyId == Guid.Empty || request.TaskId == Guid.Empty)
            return Result<HkTaskWorkflowResult>.Failure(Error.Validation("HK_TASK_SCOPE_REQUIRED", "Cơ sở và công việc là bắt buộc."));

        var task = await db.HousekeepingTasks.SingleOrDefaultAsync(
            x => x.PropertyId == request.PropertyId && x.HousekeepingTaskId == request.TaskId, ct);
        if (task is null)
            return Result<HkTaskWorkflowResult>.Failure(Error.Business("HK_TASK_NOT_FOUND", "Không tìm thấy công việc Housekeeping."));

        if (!Enum.IsDefined(request.Action))
            return Result<HkTaskWorkflowResult>.Failure(Error.Validation("HK_TASK_ACTION_INVALID", "Thao tác công việc Housekeeping không hợp lệ."));

        if (request.Action is HkTaskAction.Assign or HkTaskAction.Reassign
            && (request.AssignedHousekeeperId is null || request.AssignedHousekeeperId == Guid.Empty))
        {
            return Result<HkTaskWorkflowResult>.Failure(Error.Validation("HK_TASK_HOUSEKEEPER_REQUIRED", "Cần chọn nhân viên dọn phòng."));
        }

        if (request.Action == HkTaskAction.SetPriority && request.Priority is not { })
            return Result<HkTaskWorkflowResult>.Failure(Error.Validation("HK_TASK_PRIORITY_REQUIRED", "Cần chọn mức ưu tiên Housekeeping."));

        if (request.Action is HkTaskAction.Assign or HkTaskAction.Reassign)
        {
            var assigneeExists = await db.AppUsers.AsNoTracking()
                .AnyAsync(x => x.Id == request.AssignedHousekeeperId!.Value && x.IsActive, ct);
            if (!assigneeExists)
            {
                return Result<HkTaskWorkflowResult>.Failure(
                    Error.Business("HK_TASK_HOUSEKEEPER_NOT_FOUND", "Tài khoản nhân sự không tồn tại hoặc đã bị khóa."));
            }
        }

        var state = await db.RoomHousekeepingStates.SingleOrDefaultAsync(
            x => x.PropertyId == request.PropertyId && x.HotelRoomId == task.HotelRoomId, ct);
        var stateAdded = false;
        if (state is null)
        {
            state = RoomHousekeepingState.Create(task.HotelRoomId, request.PropertyId, DateTime.UtcNow);
            db.RoomHousekeepingStates.Add(state);
            stateAdded = true;
        }

        var now = DateTime.UtcNow;
        var previousStatus = stateAdded ? (HousekeepingRoomStatus?)null : state.Status;
        try
        {
            switch (request.Action)
            {
                case HkTaskAction.Assign:
                    task.Assign(request.AssignedHousekeeperId!.Value, now);
                    break;
                case HkTaskAction.Reassign:
                    task.Reassign(request.AssignedHousekeeperId!.Value, now);
                    break;
                case HkTaskAction.SetPriority:
                    task.SetPriority(request.Priority!.Value, now);
                    break;
                case HkTaskAction.Accept:
                    task.Accept(now);
                    break;
                case HkTaskAction.StartCleaning:
                    task.StartCleaning(now);
                    state.StartCleaning(now);
                    break;
                case HkTaskAction.PauseCleaning:
                    task.Pause(request.Notes, now);
                    break;
                case HkTaskAction.ResumeCleaning:
                    task.Resume(now);
                    break;
                case HkTaskAction.CompleteCleaning:
                    var ruleKind = TaskTypeToRuleKind(task.TaskType);
                    var completionRule = await GetActiveHousekeepingRuleAsync(request.PropertyId, ruleKind, now, ct);
                    var requiresInspection = completionRule?.RequiresInspection ?? true;
                    task.CompleteCleaning(now, requiresInspection);
                    if (requiresInspection)
                    {
                        state.MarkInspected(now);
                        EnsurePendingInspection(request.PropertyId, task.HousekeepingTaskId);
                    }
                    else
                    {
                        state.MarkClean(now);
                    }
                    break;
                case HkTaskAction.SaveInspectionEvidence:
                    SaveInspectionEvidence(request.PropertyId, task.HousekeepingTaskId, request.InspectorId, request.InspectionChecklistData, request.Notes);
                    break;
                case HkTaskAction.ApproveInspection:
                    task.Inspect(HousekeepingInspectionResult.Approved, request.Notes, now);
                    state.MarkClean(now);
                    CompleteInspection(request.PropertyId, task.HousekeepingTaskId, request.InspectorId, "Approved", request.InspectionChecklistData, request.Notes, now);
                    break;
                case HkTaskAction.RejectInspection:
                    task.Inspect(HousekeepingInspectionResult.Rejected, request.Notes, now);
                    state.MarkDirty(now);
                    CompleteInspection(request.PropertyId, task.HousekeepingTaskId, request.InspectorId, "Rejected", request.InspectionChecklistData, request.Notes, now);
                    break;
                case HkTaskAction.Cancel:
                    task.Cancel(request.Notes ?? "Cancelled from housekeeping workspace", now);
                    state.MarkDirty(now);
                    break;
                default:
                    return Result<HkTaskWorkflowResult>.Failure(Error.Validation("HK_TASK_ACTION_INVALID", "Thao tác công việc Housekeeping không hợp lệ."));
            }

            RecordRoomStatusHistory(state, previousStatus, $"Task.{request.Action}", task.HousekeepingTaskId, now);
            await db.SaveChangesAsync(ct);
            return Result<HkTaskWorkflowResult>.Success(new(
                task.HousekeepingTaskId,
                task.HotelRoomId,
                request.Action,
                task.Status,
                MapRoomStatus(state.Status, task.Status),
                task.InspectionResult));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (stateAdded) db.Entry(state).State = EntityState.Detached;
            return Result<HkTaskWorkflowResult>.Failure(Error.Concurrency(
                "HK_TASK_CONCURRENCY_CONFLICT",
                "Công việc vừa được cập nhật bởi người dùng khác. Vui lòng tải lại."));
        }
        catch (InvalidOperationException ex)
        {
            if (stateAdded) db.Entry(state).State = EntityState.Detached;
            return Result<HkTaskWorkflowResult>.Failure(Error.Business("HK_TASK_ACTION_NOT_ALLOWED", ex.Message));
        }
    }

    private static HkRoomStatus MapRoomStatus(HousekeepingRoomStatus status, HousekeepingTaskStatus taskStatus)
        => taskStatus == HousekeepingTaskStatus.WaitingInspection
            ? HkRoomStatus.Inspected
            : status switch
            {
                HousekeepingRoomStatus.VacantClean => HkRoomStatus.VacantClean,
                HousekeepingRoomStatus.VacantDirty => HkRoomStatus.VacantDirty,
                HousekeepingRoomStatus.OccupiedClean => HkRoomStatus.OccupiedClean,
                HousekeepingRoomStatus.OccupiedDirty => HkRoomStatus.OccupiedDirty,
                HousekeepingRoomStatus.Cleaning => HkRoomStatus.CleaningInProgress,
                HousekeepingRoomStatus.Inspected => HkRoomStatus.Inspected,
                HousekeepingRoomStatus.OutOfOrder => HkRoomStatus.OutOfOrder,
                HousekeepingRoomStatus.OutOfService => HkRoomStatus.OutOfService,
                _ => HkRoomStatus.VacantClean
            };

    private Task<HotelHousekeepingRuleProfile?> GetActiveHousekeepingRuleAsync(Guid propertyId, HotelHousekeepingRuleKind kind, DateTime atUtc, CancellationToken ct) =>
        db.HotelHousekeepingRuleProfiles.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Kind == kind && x.IsActive
                && x.EffectiveFrom <= atUtc.Date && x.EffectiveTo >= atUtc.Date)
            .OrderBy(x => x.Priority).ThenBy(x => x.Code)
            .FirstOrDefaultAsync(ct);

    private static HotelHousekeepingRuleKind TaskTypeToRuleKind(HousekeepingTaskType taskType) => taskType switch
    {
        HousekeepingTaskType.DepartureClean => HotelHousekeepingRuleKind.CheckOutCleaning,
        HousekeepingTaskType.DeepClean => HotelHousekeepingRuleKind.DeepCleaning,
        HousekeepingTaskType.Inspection => HotelHousekeepingRuleKind.Inspection,
        HousekeepingTaskType.MaintenanceFollowUp => HotelHousekeepingRuleKind.Maintenance,
        _ => HotelHousekeepingRuleKind.DailyCleaning
    };

    private void RecordRoomStatusHistory(
        RoomHousekeepingState state,
        HousekeepingRoomStatus? previousStatus,
        string source,
        Guid? housekeepingTaskId,
        DateTime occurredAtUtc)
    {
        if (previousStatus.HasValue && previousStatus.Value == state.Status)
            return;

        db.RoomHousekeepingStatusHistories.Add(RoomHousekeepingStatusHistory.Create(
            state.PropertyId,
            state.HotelRoomId,
            previousStatus,
            state.Status,
            source,
            housekeepingTaskId,
            occurredAtUtc));
    }

    private void EnsurePendingInspection(Guid propertyId, Guid taskId)
    {
        if (db.RoomInspections.Local.Any(x => x.PropertyId == propertyId && x.HousekeepingTaskId == taskId)
            || db.RoomInspections.Any(x => x.PropertyId == propertyId && x.HousekeepingTaskId == taskId))
            return;

        db.RoomInspections.Add(RoomInspection.Create(propertyId, taskId));
    }

    private void CompleteInspection(
        Guid propertyId,
        Guid taskId,
        Guid? inspectorId,
        string result,
        string? checklistData,
        string? notes,
        DateTime inspectedAtUtc)
    {
        var inspection = db.RoomInspections.Local.FirstOrDefault(
            x => x.PropertyId == propertyId && x.HousekeepingTaskId == taskId)
            ?? db.RoomInspections.SingleOrDefault(
                x => x.PropertyId == propertyId && x.HousekeepingTaskId == taskId);

        if (inspection is null)
        {
            inspection = RoomInspection.Create(propertyId, taskId);
            db.RoomInspections.Add(inspection);
        }

        inspection.Complete(inspectorId, result, checklistData, notes, inspectedAtUtc);
    }

    private void SaveInspectionEvidence(
        Guid propertyId,
        Guid taskId,
        Guid? inspectorId,
        string? checklistData,
        string? notes)
    {
        var inspection = db.RoomInspections.Local.FirstOrDefault(
            x => x.PropertyId == propertyId && x.HousekeepingTaskId == taskId)
            ?? db.RoomInspections.SingleOrDefault(
                x => x.PropertyId == propertyId && x.HousekeepingTaskId == taskId);

        if (inspection is null)
        {
            inspection = RoomInspection.Create(propertyId, taskId);
            db.RoomInspections.Add(inspection);
        }

        inspection.SaveEvidence(inspectorId, checklistData, notes);
    }
}
