using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IExportPipelineService
{
    Task<IReadOnlyList<ExportRetentionPolicyDto>> GetRetentionPoliciesAsync(CancellationToken cancellationToken = default);
    Task<ExportRetentionPolicyDto> SaveRetentionPolicyAsync(ExportRetentionPolicyDto policy, CancellationToken cancellationToken = default);
    Task<ExportExecutionDto> QueueExportAsync(string exportCode, string exportType, CancellationToken cancellationToken = default);
    Task<ExportExecutionDto> MarkCompletedAsync(Guid exportExecutionId, CancellationToken cancellationToken = default);
    Task<int> CleanupRetentionAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExportExecutionDto>> GetExecutionsAsync(int take = 100, CancellationToken cancellationToken = default);
}
