using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface INotificationTaskService
{
    Task<NotificationDto> PublishAsync(NotificationPublishRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(bool unreadOnly = false, int take = 100, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task AcknowledgeNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task DismissNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task RestoreNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task SaveNotificationCommentAsync(Guid notificationId, string commentText, CancellationToken cancellationToken = default);
    Task SaveNotificationReactionAsync(Guid notificationId, string emoji, CancellationToken cancellationToken = default);
    Task<List<CommentItemDto>> GetNotificationCommentsAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<NotificationAttachmentDto> UploadNotificationAttachmentAsync(Guid notificationId, string sourceFilePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationAttachmentDto>> GetNotificationAttachmentsAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<TaskInboxDto> CreateTaskAsync(TaskCreateRequest request, CancellationToken cancellationToken = default);
    Task<TaskInboxDto> UpdateTaskAsync(Guid taskId, TaskCreateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskInboxDto>> GetTasksAsync(string? status = null, CancellationToken cancellationToken = default);
    Task AssignTaskAsync(Guid taskId, Guid? assignedToUserId, CancellationToken cancellationToken = default);
    Task CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task ReopenTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<TaskCommentDto> AddCommentAsync(Guid taskId, string commentText, IReadOnlyList<Guid>? mentionUserIds = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskCommentDto>> GetCommentsAsync(Guid taskId, CancellationToken cancellationToken = default);
}
