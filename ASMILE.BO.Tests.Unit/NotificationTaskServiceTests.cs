using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class NotificationTaskServiceTests
{
    private static readonly Guid UserA = Guid.Parse("30303030-3030-3030-3030-3030303030a1");
    private static readonly Guid UserB = Guid.Parse("30303030-3030-3030-3030-3030303030b2");
    private static readonly Guid CompanyA = Guid.Parse("31313131-3131-3131-3131-3131313131a1");
    private static readonly Guid CompanyB = Guid.Parse("31313131-3131-3131-3131-3131313131b2");
    private static readonly Guid BranchA = Guid.Parse("32323232-3232-3232-3232-3232323232a1");
    private static readonly Guid BranchB = Guid.Parse("32323232-3232-3232-3232-3232323232b2");

    [Fact]
    public async Task NotificationDelivery_AndUnreadCounters_WorkPerUser()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var serviceA = new NotificationTaskService(db, new TestCurrentUserService(UserA));

        await serviceA.PublishAsync(new NotificationPublishRequest { UserId = UserA, NotificationType = "posting failed", Severity = "Error", Title = "Posting failed", Message = "Voucher cannot post.", MetadataJson = "{\"Fingerprint\":\"ABC123\"}" });
        await serviceA.PublishAsync(new NotificationPublishRequest { UserId = UserB, NotificationType = "low stock", Severity = "Warning", Title = "Low stock", Message = "Item below minimum." });

        var unreadA = await serviceA.GetUnreadCountAsync();
        var notificationsA = await serviceA.GetNotificationsAsync();

        unreadA.Should().Be(1);
        notificationsA.Should().ContainSingle(x => x.Title == "Posting failed");
        notificationsA.Single().MetadataJson.Should().Be("{\"Fingerprint\":\"ABC123\"}");
    }

    [Fact]
    public async Task MarkRead_RemovesNotificationFromUnreadCounter()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new NotificationTaskService(db, new TestCurrentUserService(UserA));
        var notification = await service.PublishAsync(new NotificationPublishRequest { UserId = UserA, NotificationType = "import failed", Title = "Import failed", Message = "Template mismatch." });

        await service.MarkReadAsync(notification.Id);

        (await service.GetUnreadCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LegacyNotificationCenter_UsesPersistedNotificationsAndCommands()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new NotificationTaskService(db, new TestCurrentUserService(UserA));
        var notification = await service.PublishAsync(new NotificationPublishRequest
        {
            UserId = UserA,
            NotificationType = "hotel",
            Severity = "Critical",
            Title = "Phòng chưa sẵn sàng",
            Message = "Phòng 301 cần kiểm tra lại.",
            SourceType = "Housekeeping"
        });
        var viewModel = new NotificationCenterViewModel(service);

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.Notifications.Should().ContainSingle();
        viewModel.Notifications[0].NotificationId.Should().Be(notification.Id);
        viewModel.TotalUnread.Should().Be(1);
        viewModel.TotalCritical.Should().Be(1);

        viewModel.SelectCommand.Execute(viewModel.Notifications[0]);
        await viewModel.MarkReadCommand.ExecuteAsync(null);

        (await service.GetUnreadCountAsync()).Should().Be(0);
        viewModel.InfoMessage.Should().Be("Đã đánh dấu đã đọc.");
    }

    [Fact]
    public async Task NotificationTaskService_RespectsCompanyAndBranchScope()
    {
        var databaseName = $"notification-scope-{Guid.NewGuid():N}";

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA, BranchA), databaseName, seedAccountingCore: false))
        {
            var service = new NotificationTaskService(db, new TestCurrentUserService(UserA));
            await service.PublishAsync(new NotificationPublishRequest
            {
                NotificationType = "hotel",
                Title = "Company A / Branch A",
                Message = "Only Branch A should see this notification."
            });
            await service.CreateTaskAsync(new TaskCreateRequest
            {
                Title = "Branch A task",
                Description = "Only Branch A should see this task."
            });
        }

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA, BranchB), databaseName, seedAccountingCore: false))
        {
            var service = new NotificationTaskService(db, new TestCurrentUserService(UserB));

            (await service.GetNotificationsAsync()).Should().BeEmpty();
            (await service.GetTasksAsync()).Should().BeEmpty();
        }

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB, BranchB), databaseName, seedAccountingCore: false))
        {
            var service = new NotificationTaskService(db, new TestCurrentUserService(UserB));

            (await service.GetNotificationsAsync()).Should().BeEmpty();
            (await service.GetTasksAsync()).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Publish_with_same_idempotency_key_returns_one_persisted_notification()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new NotificationTaskService(db, new TestCurrentUserService(UserA));
        var request = new NotificationPublishRequest
        {
            UserId = UserA,
            NotificationType = "TaskOverdue",
            Severity = "Warning",
            Title = "Công việc quá hạn",
            Message = "Task đã quá hạn.",
            SourceType = "TaskInbox",
            SourceId = Guid.Parse("30303030-3030-3030-3030-3030303030c3"),
            IdempotencyKey = "task-overdue:303030303030303030303030303030c3:638000000000000000"
        };

        var first = await service.PublishAsync(request);
        var second = await service.PublishAsync(new NotificationPublishRequest
        {
            UserId = UserA,
            NotificationType = request.NotificationType,
            Severity = request.Severity,
            Title = request.Title,
            Message = "Nội dung lặp lại không được tạo thêm.",
            SourceType = request.SourceType,
            SourceId = request.SourceId,
            IdempotencyKey = request.IdempotencyKey
        });

        second.Id.Should().Be(first.Id);
        db.UserNotifications.Should().ContainSingle(x => x.IdempotencyKey == request.IdempotencyKey);
    }

    [Fact]
    public async Task NotificationComments_AndReactions_DoNotOverwriteSourceMetadata()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new NotificationTaskService(db, new TestCurrentUserService(UserA));
        var notification = await service.PublishAsync(new NotificationPublishRequest
        {
            UserId = UserA,
            NotificationType = "reservation",
            Title = "Reservation changed",
            Message = "Room assignment changed.",
            MetadataJson = "{\"SourceFingerprint\":\"ABC123\"}"
        });

        await service.SaveNotificationCommentAsync(notification.Id, "Please review this change.");
        await service.SaveNotificationReactionAsync(notification.Id, "👍");

        db.UserNotifications.Single(x => x.Id == notification.Id).MetadataJson
            .Should().Be("{\"SourceFingerprint\":\"ABC123\"}");
        db.NotificationComments.Should().HaveCount(2);
        var comments = await service.GetNotificationCommentsAsync(notification.Id);
        comments.Should().Contain(x => x.Text == "Please review this change.");
        comments.Should().Contain(x => x.Text == "👍 Đã thả cảm xúc");
    }

    [Fact]
    public async Task TargetedNotification_CannotBeMutatedByAnotherUser()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var serviceA = new NotificationTaskService(db, new TestCurrentUserService(UserA));
        var serviceB = new NotificationTaskService(db, new TestCurrentUserService(UserB));
        var notification = await serviceA.PublishAsync(new NotificationPublishRequest
        {
            UserId = UserA,
            NotificationType = "security",
            Title = "Private alert",
            Message = "Only user A may update this notification."
        });

        await serviceB.MarkReadAsync(notification.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            serviceB.SaveNotificationCommentAsync(notification.Id, "Unauthorized comment"));

        (await serviceA.GetUnreadCountAsync()).Should().Be(1);
        (await serviceA.GetNotificationCommentsAsync(notification.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastNotification_UserState_IsolatedAndDismissalAffectsOnlyCurrentUser()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var serviceA = new NotificationTaskService(db, new TestCurrentUserService(UserA));
        var serviceB = new NotificationTaskService(db, new TestCurrentUserService(UserB));
        var notification = await serviceA.PublishAsync(new NotificationPublishRequest
        {
            NotificationType = "system",
            Title = "System maintenance",
            Message = "Please acknowledge before continuing."
        });

        await serviceA.AcknowledgeNotificationAsync(notification.Id);
        await serviceA.DismissNotificationAsync(notification.Id);

        var a = (await serviceA.GetNotificationsAsync()).Single(x => x.Id == notification.Id);
        var b = (await serviceB.GetNotificationsAsync()).Single(x => x.Id == notification.Id);
        a.IsAcknowledged.Should().BeTrue();
        a.IsDismissed.Should().BeTrue();
        b.IsAcknowledged.Should().BeFalse();
        b.IsDismissed.Should().BeFalse();
        (await serviceA.GetUnreadCountAsync()).Should().Be(0);
        (await serviceB.GetUnreadCountAsync()).Should().Be(1);

        await serviceA.RestoreNotificationAsync(notification.Id);
        var restored = (await serviceA.GetNotificationsAsync()).Single(x => x.Id == notification.Id);
        restored.IsAcknowledged.Should().BeTrue();
        restored.IsDismissed.Should().BeFalse();
        (await serviceA.GetUnreadCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TargetedNotification_CannotChangeAcknowledgementOrDismissalForAnotherUser()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var serviceA = new NotificationTaskService(db, new TestCurrentUserService(UserA));
        var serviceB = new NotificationTaskService(db, new TestCurrentUserService(UserB));
        var notification = await serviceA.PublishAsync(new NotificationPublishRequest
        {
            UserId = UserA,
            NotificationType = "approval",
            Title = "Private approval",
            Message = "Only user A may acknowledge this."
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => serviceB.AcknowledgeNotificationAsync(notification.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => serviceB.DismissNotificationAsync(notification.Id));
        db.NotificationUserStates.Should().BeEmpty();
    }

    [Fact]
    public async Task TaskAssignment_CompleteAndReopenFlow_IsPersisted()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new NotificationTaskService(db, new TestCurrentUserService(UserA));
        var task = await service.CreateTaskAsync(new TaskCreateRequest { Title = "Reconcile bank", Description = "Mismatch", Priority = "High" });

        await service.AssignTaskAsync(task.Id, UserB);
        await service.CompleteTaskAsync(task.Id);
        await service.ReopenTaskAsync(task.Id);
        await service.AddCommentAsync(task.Id, "@user please review", [UserB]);

        var stored = (await service.GetTasksAsync()).Single(x => x.Id == task.Id);
        var comments = await service.GetCommentsAsync(task.Id);

        stored.AssignedToUserId.Should().Be(UserB);
        stored.Status.Should().Be("Open");
        comments.Should().ContainSingle(x => x.MentionUserIdsJson!.Contains(UserB.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateTask_PersistsSnoozeDueAt()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new NotificationTaskService(db, new TestCurrentUserService(UserA));
        var task = await service.CreateTaskAsync(new TaskCreateRequest { Title = "Follow up", DueAt = DateTime.UtcNow });
        var dueAt = DateTime.UtcNow.AddMinutes(45);

        await service.UpdateTaskAsync(task.Id, new TaskCreateRequest { DueAt = dueAt });

        (await service.GetTasksAsync()).Single(x => x.Id == task.Id).DueAt.Should().Be(dueAt);
    }

    private sealed class TestCompanyContext : ICurrentCompanyContext
    {
        public TestCompanyContext(Guid companyId, Guid? branchId)
        {
            CompanyId = companyId;
            BranchId = branchId;
            CompanyName = companyId == CompanyA ? "Company A" : "Company B";
            BranchName = branchId.HasValue ? "Selected Branch" : null;
        }

        public Guid? CompanyId { get; private set; }
        public Guid? BranchId { get; private set; }
        public string CompanyName { get; private set; }
        public string? BranchName { get; private set; }

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CompanyId = companyId;
            BranchId = null;
            CompanyName = companyId == CompanyA ? "Company A" : "Company B";
            BranchName = null;
            return Task.CompletedTask;
        }

        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default)
        {
            BranchId = branchId;
            BranchName = branchId.HasValue ? "Selected Branch" : null;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([]);

        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public TestCurrentUserService(Guid userId)
        {
            CurrentUser = new UserAccountDto
            {
                Id = userId,
                UserName = $"user-{userId:N}",
                DisplayName = "Notification User",
                Roles = [],
                IsActive = true
            };
        }

        public UserAccountDto? CurrentUser { get; private set; }
        public Guid? CurrentSessionId { get; private set; } = Guid.NewGuid();
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => false;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear()
        {
            CurrentUser = null;
            CurrentSessionId = null;
        }
    }
}
