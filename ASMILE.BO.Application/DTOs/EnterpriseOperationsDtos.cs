namespace ASMILE.BO.Application.DTOs;

public sealed class EnterprisePermissionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDangerous { get; set; }
    public bool IsAssigned { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string DenialReason { get; set; } = string.Empty;
}

public sealed class EnterpriseRoleDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public int PermissionCount { get; set; }
    public int UserCount { get; set; }
}

public sealed class EnterpriseUserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}

public sealed class PermissionDiffDto
{
    public string PermissionCode { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}

public sealed class PermissionTreeNodeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    public bool IsDenied { get; set; }
    public string DenialReason { get; set; } = string.Empty;
    public IReadOnlyList<PermissionTreeNodeDto> Children { get; set; } = [];
}

public sealed class PermissionMatrixRowDto
{
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public bool IsDenied { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class DataPermissionDto
{
    public Guid Id { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
    public bool IsGranted { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
}

public sealed class EffectivePermissionPreviewDto
{
    public Guid UserId { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<PermissionMatrixRowDto> Matrix { get; set; } = [];
    public IReadOnlyList<DataPermissionDto> DataPermissions { get; set; } = [];
}

public sealed class AuditTimelineItemDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Module { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = string.Empty;
    public string AfterJson { get; set; } = string.Empty;
    public string DiffJson { get; set; } = string.Empty;
}

public sealed class BackgroundJobDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool CanRetry { get; set; } = true;
    public bool CanCancel { get; set; }
}

public sealed class BackgroundJobExecutionDto
{
    public Guid Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string? RequestedBy { get; set; }
    public bool CancellationRequested { get; set; }
}

public sealed class WorkspaceSettingsDto
{
    public IReadOnlyList<string> PinnedModules { get; set; } = [];
    public IReadOnlyList<string> FavoriteReports { get; set; } = [];
    public int RecentHistoryLimit { get; set; } = 20;
    public bool RestoreLastSession { get; set; } = true;
    public string DefaultLandingModule { get; set; } = "dashboard";
    public string GridDensity { get; set; } = "Comfortable";
    public bool CompactMode { get; set; }
}
