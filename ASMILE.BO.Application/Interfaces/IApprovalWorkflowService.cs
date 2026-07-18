using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IApprovalWorkflowService
{
    Task<ApprovalWorkflowDto> SubmitAsync(ApprovalSubmitRequestDto request, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflowDto> ApproveAsync(Guid workflowId, string? comment = null, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflowDto> RejectAsync(Guid workflowId, string? comment = null, CancellationToken cancellationToken = default);
    Task DelegateAsync(Guid assignmentId, Guid toUserId, string? reason = null, DateTime? expiresAtUtc = null, CancellationToken cancellationToken = default);
    Task<int> EscalateTimeoutAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalHistoryDto>> ReplayTrailAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflowDto> RecoverStateAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalWorkflowDto>> GetWorkflowsAsync(int take = 200, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalStepDto>> GetStepsAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalSnapshotDto>> GetSnapshotsAsync(Guid workflowId, CancellationToken cancellationToken = default);
}
