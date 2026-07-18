namespace ASMILE.BO.Core.Entities;

public sealed class AppUserPermission : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid UserId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public bool IsGranted { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

public sealed class AppRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<AppRolePermission> Permissions { get; set; } = new List<AppRolePermission>();
    public ICollection<AppRoleInheritance> ParentRoles { get; set; } = new List<AppRoleInheritance>();
}

public sealed class AppRolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public AppRole? Role { get; set; }
}

public sealed class AppRoleInheritance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public Guid ParentRoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public AppRole? Role { get; set; }
    public AppRole? ParentRole { get; set; }
}

public sealed class AppDataPermission : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string SubjectType { get; set; } = "User";
    public Guid SubjectId { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
    public bool IsGranted { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

public sealed class UserWorkspacePreference : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PreferenceKey { get; set; } = string.Empty;
    public string ValueJson { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<AppUserRole> Roles { get; set; } = new List<AppUserRole>();
}

public sealed class AppUserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public AppUser? User { get; set; }
    public AppRole? Role { get; set; }
}

public sealed class BackgroundJobDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool AllowConcurrentExecution { get; set; }
    public int RetryCount { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 300;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<BackgroundJobSchedule> Schedules { get; set; } = new List<BackgroundJobSchedule>();
    public ICollection<BackgroundJobExecution> Executions { get; set; } = new List<BackgroundJobExecution>();
}

public sealed class BackgroundJobSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobDefinitionId { get; set; }
    public string? CronExpression { get; set; }
    public int? IntervalMinutes { get; set; }
    public DateTime? NextRunAtUtc { get; set; }
    public DateTime? LastRunAtUtc { get; set; }
    public bool IsPaused { get; set; }
    public BackgroundJobDefinition? JobDefinition { get; set; }
}

public sealed class BackgroundJobExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? JobDefinitionId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string? RequestedBy { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public bool CancellationRequested { get; set; }
    public DateTime? QueuedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int RetryAttempt { get; set; }
    public string TriggerSource { get; set; } = "manual";
    public string? ErrorMessage { get; set; }
    public BackgroundJobDefinition? JobDefinition { get; set; }
    public ICollection<BackgroundJobLog> Logs { get; set; } = new List<BackgroundJobLog>();
}

public sealed class BackgroundJobLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExecutionId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Severity { get; set; } = "Information";
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public BackgroundJobExecution? Execution { get; set; }
}

