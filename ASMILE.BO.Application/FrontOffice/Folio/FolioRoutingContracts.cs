namespace ASMILE.BO.Application.FrontOffice.Folio;

public sealed record FolioRoutingRuleDto(
    Guid HotelFolioRoutingRuleId,
    Guid PropertyId,
    Guid SourceFolioId,
    string SourceFolioNumber,
    Guid TargetFolioId,
    string TargetFolioNumber,
    string Name,
    string EntryType,
    decimal Percent,
    int Priority,
    bool IsActive,
    string? Notes,
    DateTime UpdatedAtUtc,
    byte[]? RowVersion);

public sealed record FolioRoutingSearchRequest(Guid PropertyId, Guid? SourceFolioId = null, bool ActiveOnly = true);

public sealed record CreateFolioRoutingRuleRequest(
    Guid PropertyId,
    Guid SourceFolioId,
    Guid TargetFolioId,
    string Name,
    string EntryType,
    decimal Percent,
    int Priority = 0,
    string? Notes = null);

public sealed record UpdateFolioRoutingRuleRequest(
    Guid RuleId,
    string Name,
    string EntryType,
    decimal Percent,
    int Priority,
    string? Notes,
    byte[]? ExpectedRowVersion = null);

public sealed record SetFolioRoutingRuleActiveRequest(Guid RuleId, bool IsActive, byte[]? ExpectedRowVersion = null);

public interface IFolioRoutingQueryService
{
    Task<IReadOnlyList<FolioRoutingRuleDto>> SearchAsync(FolioRoutingSearchRequest request, CancellationToken ct);
}

public interface IFolioRoutingCommandService
{
    Task<FolioRoutingRuleDto> CreateAsync(CreateFolioRoutingRuleRequest request, CancellationToken ct);
    Task<FolioRoutingRuleDto> UpdateAsync(UpdateFolioRoutingRuleRequest request, CancellationToken ct);
    Task<FolioRoutingRuleDto> SetActiveAsync(SetFolioRoutingRuleActiveRequest request, CancellationToken ct);
}
