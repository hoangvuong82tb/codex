namespace ASMILE.BO.Application.DTOs;

public sealed class PermissionCheckResultDto
{
    public bool IsAllowed { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class CompanyContextOptionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public sealed class BranchContextOptionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public sealed class WorkspaceStateDto
{
    public IReadOnlyList<string> PinnedModules { get; set; } = [];
    public bool HasPinnedModulesPreference { get; set; }
    public IReadOnlyList<string> FavoriteReports { get; set; } = [];
    public IReadOnlyList<string> RecentModules { get; set; } = [];
    public IReadOnlyList<string> RecentReports { get; set; } = [];
    public IReadOnlyList<string> RecentDrilldowns { get; set; } = [];
    public IReadOnlyList<string> LastOpenedTabs { get; set; } = [];
    public IReadOnlyList<string> StartupModules { get; set; } = [];
    public bool HasStartupModulesPreference { get; set; }
    public string ShellLayoutJson { get; set; } = string.Empty;
}

public sealed class GlobalSearchResultDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetRoute { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public string? SourceType { get; set; }
}
