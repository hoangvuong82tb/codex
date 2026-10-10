namespace ASMILE.BO.Application.DTOs;

public sealed class NotificationPublishRequest
{
    public Guid? UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    /// <summary>
    /// Optional durable deduplication key for notifications emitted by a real
    /// condition producer. The same key must resolve to the same notification
    /// inside the current property scope.
    /// </summary>
    public string? IdempotencyKey { get; set; }
    public string? TargetRoute { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class NotificationDto
{
    public Guid Id { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? TargetRoute { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsRead { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool IsDismissed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class NotificationAttachmentDto
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string VirtualPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class TaskCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? TargetRoute { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? StartAt { get; set; }
}

public sealed class TaskInboxDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? TargetRoute { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed record CommentItemDto(string Text, string Sender, DateTime Time, bool IsMine);

public sealed class TaskCommentDto
{
    public Guid Id { get; set; }
    public Guid TaskInboxId { get; set; }
    public Guid? UserId { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public string? MentionUserIdsJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
