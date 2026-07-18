namespace ASMILE.BO.App.Services;

public sealed class NavigationRequest
{
    public NavigationTarget Target { get; set; } = new();
    public NavigationContext Context { get; set; } = new();
    public bool OpenInNewTab { get; set; }
}

public sealed class NavigationTarget
{
    public string TargetType { get; set; } = "module";
    public string Route { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? SourceType { get; set; }
    public string? DisplayTitle { get; set; }
}

public sealed class NavigationContext
{
    public string? CorrelationId { get; set; }
    public string? SourceModule { get; set; }
    public string? SourceReport { get; set; }
    public string? SourceRowKey { get; set; }
    public string? Breadcrumb { get; set; }
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

public sealed class NavigationHistoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NavigationTarget Target { get; set; } = new();
    public NavigationContext Context { get; set; } = new();
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
}

public sealed class NavigationResult
{
    public bool Success { get; set; }
    public string Route { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
}
