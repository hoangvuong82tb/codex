using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IBackgroundSchedulerService
{
    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);
    Task<SchedulerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackgroundSchedulerDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackgroundSchedulerExecutionDto>> GetExecutionsAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackgroundSchedulerLogDto>> GetLogsAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackgroundSchedulerDurationPointDto>> GetDurationChartMetadataAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackgroundSchedulerFailureSummaryDto>> GetFailureSummaryAsync(CancellationToken cancellationToken = default);
    Task<Guid> RunNowAsync(string jobCode, string? requestedBy = null, CancellationToken cancellationToken = default);
    Task<int> QueueDueAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<bool> ExecuteNextQueuedAsync(CancellationToken cancellationToken = default);
    Task RetryAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task PauseScheduleAsync(Guid definitionId, CancellationToken cancellationToken = default);
    Task ResumeScheduleAsync(Guid definitionId, CancellationToken cancellationToken = default);
    Task<int> RecoverStaleExecutionsAsync(DateTime utcNow, TimeSpan staleAfter, CancellationToken cancellationToken = default);
    Task ValidateStartupAsync(CancellationToken cancellationToken = default);
}
