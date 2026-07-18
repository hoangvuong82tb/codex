namespace ASMILE.BO.Application.DTOs;

public sealed class SearchIndexEntryDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetRoute { get; set; } = string.Empty;
    public Guid? TargetGuid { get; set; }
    public string? SourceType { get; set; }
    public string? MetadataJson { get; set; }
    public int Score { get; set; }
}

public sealed class SearchIndexUpsertRequest
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TargetType { get; set; } = "module";
    public string TargetRoute { get; set; } = string.Empty;
    public Guid? TargetGuid { get; set; }
    public string? SourceType { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public int BaseScore { get; set; } = 10;
}

public sealed class RecentSearchDto
{
    public Guid Id { get; set; }
    public string Query { get; set; } = string.Empty;
    public DateTime SearchedAt { get; set; }
    public string? MetadataJson { get; set; }
}
