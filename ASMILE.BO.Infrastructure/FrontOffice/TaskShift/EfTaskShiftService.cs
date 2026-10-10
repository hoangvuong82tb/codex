using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.TaskShift;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.TaskShift;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.TaskShift;

/// <summary>
/// EF-backed task and shift workflow. Every read and mutation is restricted to
/// the active Front Office property; the UI never fabricates task rows.
/// </summary>
public sealed class EfTaskShiftService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : ITaskShiftService
{
    public async Task<IReadOnlyList<TaskAssignmentSnapshot>> QueryAsync(CancellationToken cancellationToken = default)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var tasks = await db.TaskAssignments
            .AsNoTracking()
            .Include(x => x.History)
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(300)
            .ToListAsync(cancellationToken);

        return tasks.Select(Map).ToList();
    }

    public async Task<TaskAssignmentSnapshot> CreateAsync(
        CreateTaskAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = RequireContext();
        var taskType = request.TaskType.Trim();
        if (string.IsNullOrWhiteSpace(taskType))
            throw new InvalidOperationException("Loại công việc là bắt buộc.");

        if (request.EstimatedMinutes is < 0)
            throw new InvalidOperationException("Thời lượng dự kiến không hợp lệ.");

        var now = clock.UtcNow;
        var task = TaskAssignment.Create(
            context.Property.PropertyId.Value,
            BuildTaskNumber(now),
            taskType,
            string.IsNullOrWhiteSpace(request.SourceModule) ? "TaskShift" : request.SourceModule.Trim(),
            string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim(),
            Normalize(request.Department),
            request.AssignedShift,
            Normalize(request.Description),
            request.DueTime,
            request.EstimatedMinutes,
            now,
            context.Actor.ActorId);

        db.TaskAssignments.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return Map(task);
    }

    public Task<TaskAssignmentSnapshot> AssignAsync(
        Guid taskAssignmentId,
        string? staffName,
        Guid? staffId,
        ShiftType? shift,
        CancellationToken cancellationToken = default) =>
        MutateAsync(taskAssignmentId, task => task.Assign(Normalize(staffName), staffId, shift, clock.UtcNow), cancellationToken);

    public Task<TaskAssignmentSnapshot> StartAsync(
        Guid taskAssignmentId,
        CancellationToken cancellationToken = default) =>
        MutateAsync(taskAssignmentId, task => task.StartWork(clock.UtcNow), cancellationToken);

    public Task<TaskAssignmentSnapshot> CompleteAsync(
        Guid taskAssignmentId,
        CancellationToken cancellationToken = default) =>
        MutateAsync(taskAssignmentId, task => task.Complete(clock.UtcNow), cancellationToken);

    private async Task<TaskAssignmentSnapshot> MutateAsync(
        Guid taskAssignmentId,
        Action<TaskAssignment> mutation,
        CancellationToken cancellationToken)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var task = await db.TaskAssignments
            .Include(x => x.History)
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.TaskAssignmentId == taskAssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy công việc trong cơ sở hiện tại.");

        mutation(task);
        // TaskHistory IDs are assigned by the aggregate before EF sees the new
        // item. Explicitly mark only newly materialized history rows as Added;
        // otherwise EF treats them as existing rows and issues an UPDATE that
        // raises a false concurrency error.
        foreach (var entry in db.ChangeTracker.Entries<TaskHistory>().Where(x => x.State == EntityState.Modified))
            entry.State = EntityState.Added;
        await db.SaveChangesAsync(cancellationToken);
        return Map(task);
    }

    private FrontOfficeExecutionContext RequireContext() =>
        contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private static string BuildTaskNumber(DateTime nowUtc) =>
        $"TSK-{nowUtc:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32];

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TaskAssignmentSnapshot Map(TaskAssignment task) => new(
        task.TaskAssignmentId,
        task.TaskNumber,
        task.TaskType,
        task.SourceModule,
        task.Priority,
        task.Status,
        task.Department,
        task.AssignedShift,
        task.AssignedStaffId,
        task.AssignedStaffName,
        task.DueTime,
        task.EstimatedMinutes,
        task.ActualMinutes,
        task.Description,
        task.CreatedAtUtc,
        task.History.OrderBy(x => x.OccurredAtUtc).Select(x => x.Action).ToList());
}
