using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface ISchedulerService
{
    Task<IReadOnlyList<JobDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobExecutionStatusDto>> GetExecutionsAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<Guid> ManualTriggerAsync(string jobCode, string? triggeredBy = null, CancellationToken cancellationToken = default);
    Task RetryAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<int> QueueDueJobsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
