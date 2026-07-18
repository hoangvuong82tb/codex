namespace ASMILE.BO.Core.Entities;

public sealed class SearchIndexEntry : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
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
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RecentSearch : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? UserId { get; set; }
    public string Query { get; set; } = string.Empty;
    public string? SelectedEntityType { get; set; }
    public string? SelectedEntityId { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
