using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class NotificationTaskService : INotificationTaskService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _auditCorrelationService;
    private readonly IBusinessDocumentService? _businessDocumentService;

    public NotificationTaskService(AppDbContext dbContext, ICurrentUserService currentUserService, IAuditService? auditService = null, IAuditCorrelationService? auditCorrelationService = null, IBusinessDocumentService? businessDocumentService = null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _auditCorrelationService = auditCorrelationService;
        _businessDocumentService = businessDocumentService;
    }

    public async Task<NotificationDto> PublishAsync(NotificationPublishRequest request, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        if (string.IsNullOrWhiteSpace(request.NotificationType))
        {
            throw new InvalidOperationException("Notification type is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Notification title is required.");
        }

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();
        if (idempotencyKey is not null)
        {
            var existing = await _dbContext.UserNotifications.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return ToDto(existing);
            }
        }

        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            NotificationType = request.NotificationType.Trim(),
            Severity = NormalizeSeverity(request.Severity),
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            SourceType = request.SourceType,
            SourceId = request.SourceId,
            IdempotencyKey = idempotencyKey,
            TargetRoute = request.TargetRoute,
            MetadataJson = request.MetadataJson,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserNotifications.Add(notification);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (idempotencyKey is not null)
        {
            // A second app instance may win the unique-key race after the
            // read above. Detach the losing insert and return the durable row.
            _dbContext.Entry(notification).State = EntityState.Detached;
            var existing = await _dbContext.UserNotifications.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            if (existing is null) throw;
            return ToDto(existing);
        }
        await AuditAsync("UserNotification", notification.Id.ToString(), "Publish", null, notification, cancellationToken);
        return ToDto(notification);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(bool unreadOnly = false, int take = 100, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.CurrentUser?.Id;
        var query = _dbContext.UserNotifications.AsNoTracking()
            .Where(x => x.UserId == null || x.UserId == currentUserId);
        if (unreadOnly)
        {
            query = query.Where(x => !x.IsRead && (currentUserId == null || !x.UserStates.Any(s => s.UserId == currentUserId && (s.IsDismissed || s.IsAcknowledged))));
        }

        var notifications = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                NotificationType = x.NotificationType,
                Severity = x.Severity,
                Title = x.Title,
                Message = x.Message,
                SourceType = x.SourceType,
                SourceId = x.SourceId,
                TargetRoute = x.TargetRoute,
                MetadataJson = x.MetadataJson,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        if (currentUserId is not Guid userId || notifications.Count == 0)
        {
            return notifications;
        }

        var notificationIds = notifications.Select(x => x.Id).ToList();
        var states = await _dbContext.NotificationUserStates.AsNoTracking()
            .Where(x => x.UserId == userId && notificationIds.Contains(x.NotificationId))
            .ToDictionaryAsync(x => x.NotificationId, cancellationToken);
        foreach (var notification in notifications)
        {
            if (states.TryGetValue(notification.Id, out var state))
            {
                notification.IsAcknowledged = state.IsAcknowledged;
                notification.IsDismissed = state.IsDismissed;
            }
        }

        return notifications;
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.CurrentUser?.Id;
        return await _dbContext.UserNotifications.AsNoTracking()
            .CountAsync(x => !x.IsRead
                && (x.UserId == null || x.UserId == currentUserId)
                && (currentUserId == null || !x.UserStates.Any(s => s.UserId == currentUserId && (s.IsDismissed || s.IsAcknowledged))), cancellationToken);
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var currentUserId = _currentUserService.CurrentUser?.Id;
        var notification = await _dbContext.UserNotifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && (x.UserId == null || x.UserId == currentUserId),
            cancellationToken);
        if (notification is null || notification.IsRead)
        {
            return;
        }

        var before = new { notification.IsRead, notification.ReadAt };
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            "UserNotification",
            notification.Id.ToString(),
            "MarkRead",
            before,
            new { notification.IsRead, notification.ReadAt },
            cancellationToken);
    }

    public Task AcknowledgeNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => UpdateNotificationStateAsync(notificationId, "Acknowledge", cancellationToken);

    public Task DismissNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => UpdateNotificationStateAsync(notificationId, "Dismiss", cancellationToken);

    public Task RestoreNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => UpdateNotificationStateAsync(notificationId, "Restore", cancellationToken);

    private async Task UpdateNotificationStateAsync(Guid notificationId, string action, CancellationToken cancellationToken)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var currentUserId = _currentUserService.CurrentUser?.Id
            ?? throw new InvalidOperationException("Không thể cập nhật trạng thái thông báo khi chưa đăng nhập.");
        var notification = await _dbContext.UserNotifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && (x.UserId == null || x.UserId == currentUserId), cancellationToken)
            ?? throw new InvalidOperationException("Notification not found or access denied.");

        var state = await _dbContext.NotificationUserStates.FirstOrDefaultAsync(
            x => x.NotificationId == notificationId && x.UserId == currentUserId, cancellationToken);
        if (state is null)
        {
            state = new NotificationUserState
            {
                Id = Guid.NewGuid(),
                CompanyId = notification.CompanyId,
                BranchId = notification.BranchId,
                NotificationId = notificationId,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.NotificationUserStates.Add(state);
        }

        var before = new { state.IsAcknowledged, state.AcknowledgedAt, state.IsDismissed, state.DismissedAt };
        var now = DateTime.UtcNow;
        switch (action)
        {
            case "Acknowledge":
                state.IsAcknowledged = true;
                state.AcknowledgedAt ??= now;
                break;
            case "Dismiss":
                state.IsDismissed = true;
                state.DismissedAt ??= now;
                break;
            case "Restore":
                state.IsDismissed = false;
                state.DismissedAt = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported notification state action.");
        }

        state.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            "UserNotification",
            notificationId.ToString(),
            action,
            before,
            new { state.IsAcknowledged, state.AcknowledgedAt, state.IsDismissed, state.DismissedAt },
            cancellationToken);
    }

    public async Task SaveNotificationCommentAsync(Guid notificationId, string commentText, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        if (string.IsNullOrWhiteSpace(commentText))
        {
            throw new InvalidOperationException("Comment text is required.");
        }

        var currentUserId = _currentUserService.CurrentUser?.Id;
        var notification = await _dbContext.UserNotifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && (x.UserId == null || x.UserId == currentUserId),
            cancellationToken);
        if (notification is null)
        {
            throw new InvalidOperationException("Notification not found or access denied.");
        }

        var comment = new NotificationComment
        {
            Id = Guid.NewGuid(),
            CompanyId = notification.CompanyId,
            BranchId = notification.BranchId,
            NotificationId = notification.Id,
            UserId = currentUserId,
            Kind = "Comment",
            CommentText = commentText.Trim(),
            CreatedByName = _currentUserService.CurrentUser?.DisplayName
                ?? _currentUserService.CurrentUser?.UserName
                ?? "Administrator",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.NotificationComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            "UserNotification",
            notification.Id.ToString(),
            "AddComment",
            null,
            new { CommentAdded = true },
            cancellationToken);
    }

    public async Task SaveNotificationReactionAsync(Guid notificationId, string emoji, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        if (string.IsNullOrWhiteSpace(emoji))
        {
            throw new InvalidOperationException("Reaction is required.");
        }

        var currentUserId = _currentUserService.CurrentUser?.Id;
        var notification = await _dbContext.UserNotifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && (x.UserId == null || x.UserId == currentUserId),
            cancellationToken);
        if (notification is null)
        {
            throw new InvalidOperationException("Notification not found or access denied.");
        }

        var normalizedEmoji = emoji.Trim();
        var exists = await _dbContext.NotificationComments.AnyAsync(
            x => x.NotificationId == notificationId
                && x.UserId == currentUserId
                && x.Kind == "Reaction"
                && x.CommentText == normalizedEmoji,
            cancellationToken);
        if (exists) return;

        _dbContext.NotificationComments.Add(new NotificationComment
        {
            Id = Guid.NewGuid(),
            CompanyId = notification.CompanyId,
            BranchId = notification.BranchId,
            NotificationId = notification.Id,
            UserId = currentUserId,
            Kind = "Reaction",
            CommentText = normalizedEmoji,
            CreatedByName = _currentUserService.CurrentUser?.DisplayName
                ?? _currentUserService.CurrentUser?.UserName
                ?? "Administrator",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            "UserNotification",
            notification.Id.ToString(),
            "AddReaction",
            null,
            new { Reaction = normalizedEmoji },
            cancellationToken);
    }

    public async Task<List<CommentItemDto>> GetNotificationCommentsAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.CurrentUser?.Id;
        var notificationExists = await _dbContext.UserNotifications.AsNoTracking().AnyAsync(
            x => x.Id == notificationId && (x.UserId == null || x.UserId == currentUserId),
            cancellationToken);
        if (!notificationExists) return new();

        var comments = await _dbContext.NotificationComments.AsNoTracking()
            .Where(x => x.NotificationId == notificationId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return comments.Select(x => new CommentItemDto(
            x.Kind == "Reaction" ? $"{x.CommentText} Đã thả cảm xúc" : x.CommentText,
            x.CreatedByName,
            x.CreatedAt,
            x.UserId == currentUserId)).ToList();
    }

    public async Task<NotificationAttachmentDto> UploadNotificationAttachmentAsync(Guid notificationId, string sourceFilePath, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        await EnsureAccessibleNotificationAsync(notificationId, cancellationToken);
        if (_businessDocumentService is null)
        {
            throw new InvalidOperationException("Kho lưu file nghiệp vụ chưa được cấu hình.");
        }

        var fileName = Path.GetFileName(sourceFilePath);
        var document = await _businessDocumentService.UploadAttachmentAsync(
            BusinessEntityType.Notification,
            notificationId,
            fileName,
            sourceFilePath,
            _currentUserService.CurrentUser?.UserName ?? "system",
            cancellationToken);

        var result = ToAttachmentDto(document, notificationId);
        await AuditAsync(
            "UserNotification",
            notificationId.ToString(),
            "AddAttachment",
            null,
            new { result.Id, result.FileName, result.FileSize, result.VirtualPath },
            cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<NotificationAttachmentDto>> GetNotificationAttachmentsAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        await EnsureAccessibleNotificationAsync(notificationId, cancellationToken);
        if (_businessDocumentService is null)
        {
            return Array.Empty<NotificationAttachmentDto>();
        }

        var documents = await _businessDocumentService.GetDocumentsByEntityAsync(
            BusinessEntityType.Notification,
            notificationId);
        return documents
            .Where(x => x.DocumentType == BusinessDocumentType.Attachment && x.Status == DocumentStatus.Active)
            .Select(x => ToAttachmentDto(x, notificationId))
            .ToList();
    }

    public async Task<TaskInboxDto> CreateTaskAsync(TaskCreateRequest request, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Task title is required.");
        }

        var task = new TaskInbox
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim(),
            AssignedToUserId = request.AssignedToUserId,
            CreatedByUserId = _currentUserService.CurrentUser?.Id,
            SourceType = request.SourceType,
            SourceId = request.SourceId,
            TargetRoute = request.TargetRoute,
            DueAt = request.DueAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.TaskInboxItems.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("TaskInbox", task.Id.ToString(), "Create", null, task, cancellationToken);
        return ToDto(task);
    }

    public async Task<TaskInboxDto> UpdateTaskAsync(Guid taskId, TaskCreateRequest request, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var task = await _dbContext.TaskInboxItems.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy công việc.");

        var before = new { task.Title, task.Description, task.Priority, task.DueAt, task.UpdatedAt };

        if (!string.IsNullOrWhiteSpace(request.Title))
            task.Title = request.Title.Trim();
        if (request.Description is not null)
            task.Description = request.Description.Trim();
        if (!string.IsNullOrWhiteSpace(request.Priority))
            task.Priority = request.Priority.Trim();
        if (request.DueAt.HasValue)
            task.DueAt = request.DueAt.Value;
        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            "TaskInbox",
            task.Id.ToString(),
            "Update",
            before,
            new { task.Title, task.Description, task.Priority, task.DueAt, task.UpdatedAt },
            cancellationToken);
        return ToDto(task);
    }

    public async Task<IReadOnlyList<TaskInboxDto>> GetTasksAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TaskInboxItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderByDescending(x => x.UpdatedAt)
            .Take(300)
            .Select(x => new TaskInboxDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Status = x.Status,
                Priority = x.Priority,
                AssignedToUserId = x.AssignedToUserId,
                SourceType = x.SourceType,
                SourceId = x.SourceId,
                TargetRoute = x.TargetRoute,
                DueAt = x.DueAt,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task AssignTaskAsync(Guid taskId, Guid? assignedToUserId, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var task = await GetRequiredTaskAsync(taskId, cancellationToken);
        var before = new { task.AssignedToUserId, task.Status, task.UpdatedAt };
        task.AssignedToUserId = assignedToUserId;
        task.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("TaskInbox", task.Id.ToString(), "Assign", before, new { task.AssignedToUserId, task.Status, task.UpdatedAt }, cancellationToken);
    }

    public async Task CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var task = await GetRequiredTaskAsync(taskId, cancellationToken);
        var before = new { task.Status, task.CompletedAt, task.UpdatedAt };
        task.Status = "Completed";
        task.CompletedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("TaskInbox", task.Id.ToString(), "Complete", before, new { task.Status, task.CompletedAt, task.UpdatedAt }, cancellationToken);
    }

    public async Task ReopenTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var task = await GetRequiredTaskAsync(taskId, cancellationToken);
        var before = new { task.Status, task.CompletedAt, task.ReopenedAt, task.UpdatedAt };
        task.Status = "Open";
        task.ReopenedAt = DateTime.UtcNow;
        task.CompletedAt = null;
        task.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("TaskInbox", task.Id.ToString(), "Reopen", before, new { task.Status, task.CompletedAt, task.ReopenedAt, task.UpdatedAt }, cancellationToken);
    }

    public async Task<TaskCommentDto> AddCommentAsync(Guid taskId, string commentText, IReadOnlyList<Guid>? mentionUserIds = null, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        if (string.IsNullOrWhiteSpace(commentText))
        {
            throw new InvalidOperationException("Comment text is required.");
        }

        var task = await GetRequiredTaskAsync(taskId, cancellationToken);
        var comment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskInboxId = task.Id,
            CompanyId = task.CompanyId,
            BranchId = task.BranchId,
            UserId = _currentUserService.CurrentUser?.Id,
            CommentText = commentText.Trim(),
            MentionUserIdsJson = mentionUserIds is { Count: > 0 } ? JsonSerializer.Serialize(mentionUserIds, JsonOptions) : null,
            CreatedAt = DateTime.UtcNow
        };
        task.UpdatedAt = DateTime.UtcNow;
        _dbContext.TaskComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("TaskComment", comment.Id.ToString(), "AddComment", null, comment, cancellationToken);
        return ToDto(comment);
    }

    public async Task<IReadOnlyList<TaskCommentDto>> GetCommentsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaskComments.AsNoTracking()
            .Where(x => x.TaskInboxId == taskId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new TaskCommentDto
            {
                Id = x.Id,
                TaskInboxId = x.TaskInboxId,
                UserId = x.UserId,
                CommentText = x.CommentText,
                MentionUserIdsJson = x.MentionUserIdsJson,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<TaskInbox> GetRequiredTaskAsync(Guid taskId, CancellationToken cancellationToken)
        => await _dbContext.TaskInboxItems.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new InvalidOperationException("Task was not found.");

    private async Task EnsureAccessibleNotificationAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUser?.Id;
        var exists = await _dbContext.UserNotifications.AsNoTracking().AnyAsync(
            x => x.Id == notificationId && (x.UserId == null || x.UserId == currentUserId),
            cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("Notification not found or access denied.");
        }
    }

    private static NotificationDto ToDto(UserNotification entity) => new()
    {
        Id = entity.Id,
        NotificationType = entity.NotificationType,
        Severity = entity.Severity,
        Title = entity.Title,
        Message = entity.Message,
        SourceType = entity.SourceType,
        SourceId = entity.SourceId,
        TargetRoute = entity.TargetRoute,
        MetadataJson = entity.MetadataJson,
        IsRead = entity.IsRead,
        CreatedAt = entity.CreatedAt
    };

    private static TaskInboxDto ToDto(TaskInbox entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        Status = entity.Status,
        Priority = entity.Priority,
        AssignedToUserId = entity.AssignedToUserId,
        SourceType = entity.SourceType,
        SourceId = entity.SourceId,
        TargetRoute = entity.TargetRoute,
        DueAt = entity.DueAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static TaskCommentDto ToDto(TaskComment entity) => new()
    {
        Id = entity.Id,
        TaskInboxId = entity.TaskInboxId,
        UserId = entity.UserId,
        CommentText = entity.CommentText,
        MentionUserIdsJson = entity.MentionUserIdsJson,
        CreatedAt = entity.CreatedAt
    };

    private static NotificationAttachmentDto ToAttachmentDto(BusinessDocumentDto document, Guid notificationId) => new()
    {
        Id = document.Id,
        NotificationId = notificationId,
        Title = document.Title,
        FileName = document.FileName,
        FileExtension = document.FileExtension,
        VirtualPath = document.VirtualPath,
        FileSize = document.FileSize,
        CreatedBy = document.CreatedBy,
        CreatedAt = document.CreatedAt
    };

    private static string NormalizeSeverity(string? severity)
        => string.IsNullOrWhiteSpace(severity) ? "Info" : severity.Trim();

    private async Task AuditAsync(string entityName, string entityId, string action, object? oldValues, object? newValues, CancellationToken cancellationToken)
    {
        if (_auditService is not null)
        {
            await _auditService.LogAsync(entityName, entityId, action, oldValues, newValues, _currentUserService.CurrentUser?.UserName, correlationId: _auditCorrelationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
        }
    }
}
