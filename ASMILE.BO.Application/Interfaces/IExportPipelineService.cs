using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IExportPipelineService
{
    Task<ExportExecutionDto> QueueExportAsync(string exportCode, string exportType, CancellationToken cancellationToken = default);
    Task<ExportExecutionDto> MarkCompletedAsync(Guid exportExecutionId, CancellationToken cancellationToken = default);
    Task<int> CleanupRetentionAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExportExecutionDto>> GetExecutionsAsync(int take = 100, CancellationToken cancellationToken = default);
}
