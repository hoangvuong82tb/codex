namespace ASMILE.BO.Application.FrontOffice.Housekeeping;

public sealed record HousekeepingLinenTypeDto(
    int Id,
    string Code,
    string Name,
    string Category,
    int DefaultParLevel,
    int MaxWashCycles,
    bool IsActive,
    byte[]? RowVersion);

public sealed record HousekeepingAmenityTypeDto(
    int Id,
    string Code,
    string Name,
    string Category,
    int DefaultParLevel,
    int MinAlertThreshold,
    bool IsActive,
    byte[]? RowVersion);

public sealed record HousekeepingLinenTypeInput(
    int? Id,
    string Code,
    string Name,
    string Category,
    int DefaultParLevel,
    int MaxWashCycles,
    byte[]? ExpectedRowVersion);

public sealed record HousekeepingAmenityTypeInput(
    int? Id,
    string Code,
    string Name,
    string Category,
    int DefaultParLevel,
    int MinAlertThreshold,
    byte[]? ExpectedRowVersion);

public interface IHousekeepingSupplyMasterDataService
{
    Task<IReadOnlyList<HousekeepingLinenTypeDto>> SearchLinenTypesAsync(Guid propertyId, string? keyword, bool? isActive, CancellationToken ct = default);
    Task<IReadOnlyList<HousekeepingAmenityTypeDto>> SearchAmenityTypesAsync(Guid propertyId, string? keyword, bool? isActive, CancellationToken ct = default);
    Task<HousekeepingLinenTypeDto> SaveLinenTypeAsync(Guid propertyId, HousekeepingLinenTypeInput input, CancellationToken ct = default);
    Task<HousekeepingAmenityTypeDto> SaveAmenityTypeAsync(Guid propertyId, HousekeepingAmenityTypeInput input, CancellationToken ct = default);
    Task<HousekeepingLinenTypeDto> SetLinenTypeActiveAsync(Guid propertyId, int id, bool isActive, byte[]? expectedRowVersion, CancellationToken ct = default);
    Task<HousekeepingAmenityTypeDto> SetAmenityTypeActiveAsync(Guid propertyId, int id, bool isActive, byte[]? expectedRowVersion, CancellationToken ct = default);
}
