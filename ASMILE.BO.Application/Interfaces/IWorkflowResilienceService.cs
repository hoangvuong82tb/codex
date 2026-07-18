using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IWorkflowResilienceService
{
    Task<WorkflowExecutionDto> BeginWorkflowAsync(string workflowType, string workflowKey, string? idempotencyKey = null, object? payload = null, string? correlationId = null, Guid? parentWorkflowExecutionId = null, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionStepDto> BeginStepAsync(Guid workflowExecutionId, string stepName, object? metadata = null, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionStepDto> CompleteStepAsync(Guid stepId, object? metadata = null, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionStepDto> FailStepAsync(Guid stepId, Exception exception, object? metadata = null, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionDto> ResumeWorkflowAsync(Guid workflowExecutionId, object? recoveryState = null, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionDto> RetryWorkflowAsync(Guid workflowExecutionId, string reason, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionDto> CancelWorkflowAsync(Guid workflowExecutionId, string reason, CancellationToken cancellationToken = default);
    Task RegisterCompensationAsync(Guid workflowExecutionId, Guid? workflowExecutionStepId, string stepName, object? compensationState = null, CancellationToken cancellationToken = default);
    Task<int> ExecuteCompensationAsync(Guid workflowExecutionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowConsistencyViolationDto>> ValidateConsistencyAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> CleanupStaleWorkflowAsync(DateTime utcNow, TimeSpan staleAfter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecutionDto>> GetWorkflowHistoryAsync(string? workflowType = null, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecutionStepDto>> GetWorkflowStepsAsync(Guid workflowExecutionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowConsistencyViolationDto>> GetConsistencyViolationsAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<WorkflowLeaseDto> AcquireLeaseAsync(string workflowType, string workflowKey, TimeSpan leaseDuration, Guid? workflowExecutionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task<WorkflowLeaseDto> HeartbeatLeaseAsync(Guid leaseId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task ReleaseLeaseAsync(Guid leaseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowLeaseDto>> GetLeasesAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<WorkflowDistributedLockDto> AcquireDistributedLockAsync(string lockScope, string lockKey, TimeSpan leaseDuration, Guid? workflowExecutionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task<WorkflowDistributedLockDto> HeartbeatDistributedLockAsync(Guid lockId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task ReleaseDistributedLockAsync(Guid lockId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDistributedLockDto>> GetDistributedLocksAsync(int take = 100, bool includeExpired = false, CancellationToken cancellationToken = default);
    Task<int> ReleaseStaleLocksAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<WorkflowDeadLetterDto> DeadLetterAsync(Guid? workflowExecutionId, string workflowType, string workflowKey, string reason, object? payload = null, Exception? exception = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDeadLetterDto>> GetDeadLettersAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<WorkflowRecoveryCheckpointDto> CreateRecoveryCheckpointAsync(Guid? workflowExecutionId, string workflowType, string workflowKey, string checkpointName, object? state = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionDto> ResumeFromCheckpointAsync(Guid checkpointId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowRecoveryCheckpointDto>> GetRecoveryCheckpointsAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionMetricDto> RecordWorkflowExecutionMetricAsync(Guid? workflowExecutionId, string workflowType, string workflowKey, string metricName, double metricValue, string category, string severity = "Information", long durationMs = 0, object? metadata = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecutionMetricDto>> GetWorkflowExecutionMetricsAsync(int take = 200, CancellationToken cancellationToken = default);
    Task<WorkflowReplaySessionDto> ReplayWorkflowAsync(Guid workflowExecutionId, CancellationToken cancellationToken = default);
}
