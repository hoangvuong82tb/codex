using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class NotificationTaskServiceTests
{
    private static readonly Guid UserA = Guid.Parse("30303030-3030-3030-3030-3030303030a1");
    private static readonly Guid UserB = Guid.Parse("30303030-3030-3030-3030-3030303030b2");

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
