using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IToolAssetService
{
    Task<IReadOnlyList<ToolAssetCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToolAssetDto>> GetToolsAsync(CancellationToken cancellationToken = default);
    Task<ToolAssetDto> CreateToolAsync(CreateToolAssetRequest request, CancellationToken cancellationToken = default);
    Task<ToolIssueDto> IssueToolAsync(IssueToolRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToolAllocationScheduleDto>> BuildAllocationScheduleAsync(Guid toolAssetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToolAllocationScheduleDto>> GetAllocationSchedulesAsync(Guid? toolAssetId = null, int take = 300, CancellationToken cancellationToken = default);
    Task<ToolAllocationRunDto> RunAllocationAsync(AllocationRunRequest request, CancellationToken cancellationToken = default);
    Task<ToolAllocationScheduleDto> PostAllocationAsync(Guid scheduleId, string? postedBy = null, CancellationToken cancellationToken = default);
    Task<ToolAllocationScheduleDto> ReverseAllocationAsync(Guid scheduleId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task<ToolSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task RecoverToolAsync(Guid toolAssetId, decimal quantity, string? description = null, CancellationToken cancellationToken = default);
    Task DisposeToolAsync(Guid toolAssetId, decimal quantity, string? reason = null, CancellationToken cancellationToken = default);
}
