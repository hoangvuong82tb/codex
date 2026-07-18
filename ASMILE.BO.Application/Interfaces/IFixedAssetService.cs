using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IFixedAssetService
{
    Task<IReadOnlyList<FixedAssetCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FixedAssetDto>> GetAssetsAsync(CancellationToken cancellationToken = default);
    Task<FixedAssetDto> CreateAssetAsync(CreateFixedAssetRequest request, CancellationToken cancellationToken = default);
    Task<FixedAssetDto> ActivateAssetAsync(Guid assetId, string? counterAccountCode = null, string? activatedBy = null, CancellationToken cancellationToken = default);
    Task<FixedAssetDto> ReverseAssetAsync(Guid assetId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task<FixedAssetAdjustmentDto> AdjustAssetAsync(AdjustFixedAssetRequest request, CancellationToken cancellationToken = default);
    Task<FixedAssetDepreciationDto> PostDepreciationAsync(Guid depreciationId, string? postedBy = null, CancellationToken cancellationToken = default);
    Task<FixedAssetDepreciationDto> ReverseDepreciationAsync(Guid depreciationId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FixedAssetDepreciationDto>> RunDepreciationAsync(DepreciationRunRequest request, CancellationToken cancellationToken = default);
    Task<FixedAssetDto> DisposeAssetAsync(DisposeFixedAssetRequest request, CancellationToken cancellationToken = default);
    Task<FixedAssetSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FixedAssetScheduleDto>> GetDepreciationScheduleAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FixedAssetDepreciationDto>> GetDepreciationsAsync(Guid? assetId = null, int take = 200, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FixedAssetAdjustmentDto>> GetAdjustmentsAsync(Guid? assetId = null, int take = 100, CancellationToken cancellationToken = default);
}
