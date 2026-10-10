namespace ASMILE.BO.Core.Entities;

public sealed class UserNotification : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? TargetRoute { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public ICollection<NotificationComment> Comments { get; set; } = new List<NotificationComment>();
    public ICollection<NotificationUserState> UserStates { get; set; } = new List<NotificationUserState>();
}

public sealed class NotificationComment : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid NotificationId { get; set; }
    public Guid? UserId { get; set; }
    public string Kind { get; set; } = "Comment";
    public string CommentText { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = "Administrator";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public UserNotification? Notification { get; set; }
}

public sealed class NotificationUserState : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool IsDismissed { get; set; }
    public DateTime? DismissedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public UserNotification? Notification { get; set; }
}

public sealed class TaskInbox : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Normal";
    public Guid? AssignedToUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? TargetRoute { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ReopenedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskAttachmentMetadata> Attachments { get; set; } = new List<TaskAttachmentMetadata>();
}

public sealed class TaskComment : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid TaskInboxId { get; set; }
    public Guid? UserId { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public string? MentionUserIdsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TaskInbox? Task { get; set; }
}

public sealed class TaskAttachmentMetadata : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid TaskInboxId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string StorageProvider { get; set; } = "Local";
    public string StorageKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TaskInbox? Task { get; set; }
}
