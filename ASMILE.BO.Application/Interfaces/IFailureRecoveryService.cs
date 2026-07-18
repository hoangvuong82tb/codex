using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IFailureRecoveryService
{
    Task<FailureAnalysisDto> AnalyzeFailureAsync(string? module = null, string? correlationId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecoveryRecommendationDto>> GenerateRecoverySuggestionsAsync(string? module = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecoveryRecommendationDto>> DetectRecurringFailureAsync(string? module = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecoveryRecommendationDto>> DetectUiFreezePatternsAsync(CancellationToken cancellationToken = default);
    Task<int> RecoverInterruptedOperationsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> TakeoverStaleWorkflowAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecoveryRecommendationDto>> DetectOrphanPatternsAsync(CancellationToken cancellationToken = default);
}
