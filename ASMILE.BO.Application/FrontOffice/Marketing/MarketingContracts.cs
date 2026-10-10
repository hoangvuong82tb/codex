using ASMILE.BO.Core.FrontOffice.Marketing;

namespace ASMILE.BO.Application.FrontOffice.Marketing;

public sealed record MarketingRecordDto(
    Guid MarketingRecordId,
    Guid PropertyId,
    MarketingSection Section,
    string Code,
    string Name,
    string GroupName,
    string Owner,
    decimal Revenue,
    MarketingRecordStatus Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    byte[]? RowVersion);

public sealed record MarketingSearchFilter(
    MarketingSection Section,
    string? Keyword = null,
    MarketingRecordStatus? Status = null,
    string? GroupName = null);

public sealed record CreateMarketingRecordRequest(
    MarketingSection Section,
    string Code,
    string Name,
    string GroupName,
    string Owner,
    string? Notes = null);

public sealed record UpdateMarketingRecordRequest(
    Guid MarketingRecordId,
    string Code,
    string Name,
    string GroupName,
    string Owner,
    string? Notes,
    byte[]? ExpectedRowVersion);

public sealed record ChangeMarketingRecordStatusRequest(
    Guid MarketingRecordId,
    MarketingRecordStatus Status,
    byte[]? ExpectedRowVersion);

public interface IMarketingQueryService
{
    Task<IReadOnlyList<MarketingRecordDto>> SearchAsync(
        Guid propertyId,
        MarketingSearchFilter filter,
        CancellationToken ct);
}

public interface IMarketingCommandService
{
    Task<MarketingRecordDto> CreateAsync(
        Guid propertyId,
        CreateMarketingRecordRequest request,
        CancellationToken ct);

    Task<MarketingRecordDto> UpdateAsync(
        Guid propertyId,
        UpdateMarketingRecordRequest request,
        CancellationToken ct);

    Task<MarketingRecordDto> ChangeStatusAsync(
        Guid propertyId,
        ChangeMarketingRecordStatusRequest request,
        CancellationToken ct);
}
