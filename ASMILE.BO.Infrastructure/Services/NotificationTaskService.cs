using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
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

    public NotificationTaskService(AppDbContext dbContext, ICurrentUserService currentUserService, IAuditService? auditService = null, IAuditCorrelationService? auditCorrelationService = null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _auditCorrelationService = auditCorrelationService;
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
            TargetRoute = request.TargetRoute,
            MetadataJson = request.MetadataJson,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserNotifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
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
            query = query.Where(x => !x.IsRead);
        }

        return await query
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
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.CurrentUser?.Id;
        return await _dbContext.UserNotifications.AsNoTracking()
            .CountAsync(x => !x.IsRead && (x.UserId == null || x.UserId == currentUserId), cancellationToken);
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.UserNotifications.FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);
        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveNotificationCommentAsync(Guid notificationId, string commentText, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.UserNotifications.FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);
        if (notification is null) return;

        List<CommentItemDto> comments;
        if (!string.IsNullOrWhiteSpace(notification.MetadataJson))
        {
            try { comments = JsonSerializer.Deserialize<List<CommentItemDto>>(notification.MetadataJson, JsonOptions) ?? new(); }
            catch { comments = new(); }
        }
        else { comments = new(); }
        comments.Add(new CommentItemDto(commentText, "Administrator", DateTime.UtcNow, true));
        notification.MetadataJson = JsonSerializer.Serialize(comments, JsonOptions);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<CommentItemDto>> GetNotificationCommentsAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.UserNotifications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);
        if (notification is null || string.IsNullOrWhiteSpace(notification.MetadataJson)) return new();
        try { return JsonSerializer.Deserialize<List<CommentItemDto>>(notification.MetadataJson, JsonOptions) ?? new(); }
        catch { return new(); }
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
        var task = await _dbContext.TaskInboxItems.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy công việc.");

        if (!string.IsNullOrWhiteSpace(request.Title))
            task.Title = request.Title.Trim();
        if (request.Description is not null)
            task.Description = request.Description.Trim();
        if (!string.IsNullOrWhiteSpace(request.Priority))
            task.Priority = request.Priority.Trim();
        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
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
