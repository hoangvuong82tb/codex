using ASMILE.BO.Core.FrontOffice.ProcessManager;

namespace ASMILE.BO.Application.FrontOffice.ProcessManager;

public sealed record CompensationExecutionResult(bool Succeeded, bool RequiresManualReview, bool RequiresReconciliation, string? ErrorCode = null, string? AuditReference = null);
public interface ICompensationHandler { string CompensationType { get; } Task<CompensationExecutionResult> ExecuteAsync(ProcessCompensation compensation, CancellationToken cancellationToken); }
public sealed class CompensationDispatcher(IEnumerable<ICompensationHandler> handlers)
{
    private readonly IReadOnlyDictionary<string, ICompensationHandler> _handlers = handlers.ToDictionary(x => x.CompensationType, StringComparer.Ordinal);
    public async Task<CompensationExecutionResult> ExecuteAsync(ProcessCompensation compensation, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        if (compensation.Status != ProcessCompensationStatus.Requested) return new(false, false, false, "PM_COMPENSATION_INVALID_STATE");
        if (!_handlers.TryGetValue(compensation.CompensationType, out var handler)) return new(false, true, false, "PM_COMPENSATION_HANDLER_NOT_FOUND");
        compensation.Start(nowUtc);
        var result = await handler.ExecuteAsync(compensation, cancellationToken);
        if (result.Succeeded) compensation.Complete(nowUtc, result.AuditReference);
        else compensation.Fail(result.ErrorCode ?? "PM_COMPENSATION_FAILED", result.RequiresManualReview || result.RequiresReconciliation);
        return result;
    }
}
