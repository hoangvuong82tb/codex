using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.Platform.Workflow;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Platform.Workflow;

public sealed class WorkflowRegistry : IWorkflowRegistry
{
    private readonly Dictionary<string, object> _definitions = [];
    public void Register<TState, TStep, TSignal>(ProcessDefinition<TState, TStep, TSignal> d) where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum => _definitions[$"{d.ProcessType}:v{d.Version}"] = d;
    public ProcessDefinition<TState, TStep, TSignal>? Get<TState, TStep, TSignal>(string type, int ver) where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum => _definitions.TryGetValue($"{type}:v{ver}", out var d) ? d as ProcessDefinition<TState, TStep, TSignal> : null;
    public IReadOnlyList<string> GetRegisteredTypes() => _definitions.Keys.ToList();
    public bool IsKnownType(string t) => _definitions.Keys.Any(k => k.StartsWith(t));
}

public sealed class SagaOrchestrator(AppDbContext db) : ISagaOrchestrator
{
    public async Task<ProcessInstance> StartSagaAsync<TContext>(SagaDefinition saga, TContext context, Guid propertyId, Guid correlationId, CancellationToken ct) where TContext : class
    {
        var process = ProcessInstance.Start($"SAGA-{Guid.NewGuid():N}"[..80], saga.SagaType, saga.Version, new PropertyId(propertyId), DateOnly.FromDateTime(DateTime.UtcNow), "Saga", saga.SagaType, new CorrelationId(correlationId), DateTime.UtcNow, "Started", saga.Steps[0].StepName);
        for (int i = 0; i < saga.Steps.Count; i++)
            db.Set<ProcessStep>().Add(ProcessStep.Create(process.ProcessId, saga.Steps[i].StepName, 1, i + 1, saga.Steps[i].CommandType, DateTime.UtcNow.AddSeconds(saga.Steps[i].TimeoutSeconds)));
        db.Set<ProcessInstance>().Add(process);
        db.Set<ProcessHistory>().Add(ProcessHistory.Record(process.ProcessId, null, "Started", null, saga.Steps[0].StepName, "SagaStarted", correlationId, DateTime.UtcNow));
        await db.SaveChangesAsync(ct);
        return process;
    }

    public async Task<ProcessInstance> AdvanceSagaAsync(Guid processId, string stepResult, CancellationToken ct)
    {
        var process = await db.Set<ProcessInstance>().FindAsync([processId], ct) ?? throw new InvalidOperationException("SAGA_NOT_FOUND");
        var steps = await db.Set<ProcessStep>().Where(s => s.ProcessId == processId).OrderBy(s => s.Sequence).ToListAsync(ct);
        var current = steps.FirstOrDefault(s => s.Status is ProcessStepStatus.Pending or ProcessStepStatus.Running);
        if (current is not null)
        {
            if (stepResult == "Success") { current.Start(DateTime.UtcNow); current.Complete(null, DateTime.UtcNow); }
            else { current.Start(DateTime.UtcNow); process.IncrementRetry(stepResult, DateTime.UtcNow); }
            db.Set<ProcessHistory>().Add(ProcessHistory.Record(processId, null, stepResult, current.StepName, null, "StepResult", process.CorrelationId, DateTime.UtcNow));
        }
        var allDone = steps.All(s => s.Status == ProcessStepStatus.Completed);
        if (allDone) { process.GetType().GetProperty("Status")!.SetValue(process, ProcessStatus.Completed); process.GetType().GetProperty("CompletedAtUtc")!.SetValue(process, DateTime.UtcNow); }
        await db.SaveChangesAsync(ct);
        return process;
    }

    public async Task<ProcessInstance> CompensateSagaAsync(Guid processId, int fromStepIndex, string reason, CancellationToken ct)
    {
        var process = await db.Set<ProcessInstance>().FindAsync([processId], ct) ?? throw new InvalidOperationException("SAGA_NOT_FOUND");
        var steps = await db.Set<ProcessStep>().Where(s => s.ProcessId == processId && s.Sequence >= fromStepIndex).OrderByDescending(s => s.Sequence).ToListAsync(ct);
        foreach (var s in steps) { if (s.Status == ProcessStepStatus.Completed) { s.GetType().GetProperty("Status")!.SetValue(s, ProcessStepStatus.Compensated); } }
        process.SetCompensation(ProcessCompensationStatus.Completed, DateTime.UtcNow);
        db.Set<ProcessHistory>().Add(ProcessHistory.Record(processId, null, "Compensated", null, null, reason, process.CorrelationId, DateTime.UtcNow));
        await db.SaveChangesAsync(ct);
        return process;
    }

    public async Task<SagaInstanceDto?> GetSagaStatusAsync(Guid processId, CancellationToken ct)
    {
        var p = await db.Set<ProcessInstance>().AsNoTracking().FirstOrDefaultAsync(x => x.ProcessId == processId, ct);
        if (p is null) return null;
        var steps = await db.Set<ProcessStep>().AsNoTracking().Where(s => s.ProcessId == processId).OrderBy(s => s.Sequence).ToListAsync(ct);
        var results = steps.Select(s => new SagaStepResultDto(s.StepName, s.Status, s.RetryCount, s.LastErrorCode, s.StartedAtUtc, s.CompletedAtUtc, s.Status == ProcessStepStatus.Compensated)).ToList();
        return new SagaInstanceDto(p.ProcessId, p.ProcessType, steps.FindIndex(s => s.Status is ProcessStepStatus.Pending or ProcessStepStatus.Running), steps.Count, p.Status, results);
    }

    public async Task<IReadOnlyList<SagaInstanceDto>> GetActiveSagasAsync(string? sagaType, CancellationToken ct)
    {
        var query = db.Set<ProcessInstance>().AsNoTracking().Where(p => !p.IsTerminal);
        if (sagaType is not null) query = query.Where(p => p.ProcessType == sagaType);
        var processes = await query.Take(200).ToListAsync(ct);
        var results = new List<SagaInstanceDto>();
        foreach (var p in processes) { var s = await GetSagaStatusAsync(p.ProcessId, ct); if (s is not null) results.Add(s); }
        return results;
    }
}

public sealed class WorkflowQueryService(AppDbContext db) : IWorkflowQueryService
{
    public async Task<WorkflowDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var processes = await db.Set<ProcessInstance>().AsNoTracking().Where(p => p.PropertyId == propertyId).ToListAsync(ct);
        return new WorkflowDashboardDto(
            processes.Count(p => p.Status == ProcessStatus.Active),
            processes.Count(p => p.Status is ProcessStatus.WaitingSignal or ProcessStatus.WaitingManualAction),
            processes.Count(p => p.Status == ProcessStatus.Failed),
            processes.Count(p => p.Status == ProcessStatus.Completed),
            processes.Count(p => p.RequiresManualAction),
            processes.Count(p => p.CompensationStatus is not null),
            processes.Where(p => p.Status == ProcessStatus.Failed).OrderByDescending(p => p.UpdatedAtUtc).Take(10).Select(Map).ToList(),
            processes.Where(p => p.RequiresManualAction).OrderBy(p => p.UpdatedAtUtc).Take(10).Select(Map).ToList());
    }

    public async Task<PageResult<WorkflowInstanceDto>> SearchAsync(Guid propertyId, WorkflowSearchFilter f, PageRequest page, CancellationToken ct)
    {
        var q = db.Set<ProcessInstance>().AsNoTracking().Where(p => p.PropertyId == propertyId);
        if (f.ProcessType is not null) q = q.Where(p => p.ProcessType == f.ProcessType);
        if (f.Status is not null) q = q.Where(p => p.Status == f.Status);
        if (f.RequiresManualAction is not null) q = q.Where(p => p.RequiresManualAction == f.RequiresManualAction);
        if (f.ProcessKey is not null) q = q.Where(p => p.ProcessKey.Contains(f.ProcessKey));
        q = q.OrderByDescending(p => p.UpdatedAtUtc);
        var total = await q.CountAsync(ct);
        var items = (await q.Skip(page.Skip).Take(Math.Clamp(page.PageSize, 1, 200)).ToListAsync(ct)).Select(Map).ToList();
        return new PageResult<WorkflowInstanceDto>(items, total, page.Page, page.PageSize);
    }

    public async Task<WorkflowInstanceDto?> GetInstanceAsync(Guid propertyId, Guid processId, CancellationToken ct)
    {
        var instance = await db.Set<ProcessInstance>().AsNoTracking().FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.ProcessId == processId, ct);
        return instance is null ? null : Map(instance);
    }
    public async Task<IReadOnlyList<ProcessStep>> GetStepsAsync(Guid processId, CancellationToken ct) => await db.Set<ProcessStep>().AsNoTracking().Where(s => s.ProcessId == processId).OrderBy(s => s.Sequence).ToListAsync(ct);
    public async Task<IReadOnlyList<ProcessHistory>> GetHistoryAsync(Guid processId, CancellationToken ct) => await db.Set<ProcessHistory>().AsNoTracking().Where(h => h.ProcessId == processId).OrderBy(h => h.OccurredAtUtc).ToListAsync(ct);

    private static WorkflowInstanceDto Map(ProcessInstance p) => new(p.ProcessId, p.ProcessKey, p.ProcessType, p.ProcessVersion, p.CurrentState, p.CurrentStep, p.Status, p.StartedAtUtc, p.UpdatedAtUtc, p.CompletedAtUtc, p.RetryCount, p.LastErrorCode, p.RequiresManualAction, p.CompensationStatus);
}
