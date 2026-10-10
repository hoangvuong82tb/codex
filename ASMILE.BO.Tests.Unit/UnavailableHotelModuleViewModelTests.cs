using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Tests.Unit;

public sealed class UnavailableHotelModuleViewModelTests
{
    [Fact]
    public async Task Concierge_fails_closed_when_real_dependencies_are_missing()
    {
        var vm = new ConciergeViewModel();

        await vm.InitializeAsync(CancellationToken.None);

        Assert.Empty(vm.Requests);
        Assert.True(vm.HasError);
        Assert.Contains("dữ liệu thật", vm.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Notification_center_does_not_fabricate_alerts()
    {
        var vm = new NotificationCenterViewModel();

        await vm.InitializeAsync(CancellationToken.None);

        Assert.Empty(vm.Notifications);
        Assert.Contains("không hiển thị dữ liệu mẫu", vm.InfoMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Vip_loyalty_does_not_fabricate_members()
    {
        var vm = new VipLoyaltyViewModel();

        await vm.InitializeAsync(CancellationToken.None);

        Assert.Empty(vm.Members);
        Assert.Contains("không hiển thị dữ liệu mẫu", vm.InfoMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Fo_audit_timeline_loads_persisted_audit_entries()
    {
        var now = DateTime.Now;
        var vm = new AuditTimelineViewModel(new FakeAuditService(
        [
            new AuditLog { Id = 42, CreatedAt = now, Username = "tester", Action = "Open", EntityName = "Reservation", Detail = "Opened reservation" },
            new AuditLog { Id = 41, CreatedAt = now.AddDays(-10), Username = "old", Action = "Open", EntityName = "Reservation", Detail = "Outside window" }
        ]));

        await vm.InitializeAsync(CancellationToken.None);

        var entry = Assert.Single(vm.Entries);
        Assert.Equal("AUD-00000042", entry.AuditNumber);
        Assert.Equal("Reservation", entry.Module);
        Assert.Equal(1, vm.TotalToday);
        Assert.Equal(1, vm.TotalThisWeek);
        Assert.Contains("audit thật", vm.InfoMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Fo_audit_timeline_fails_closed_when_audit_query_fails()
    {
        var vm = new AuditTimelineViewModel(new FakeAuditService(new InvalidOperationException("database unavailable")));

        await vm.InitializeAsync(CancellationToken.None);

        Assert.Empty(vm.Entries);
        Assert.True(vm.HasError);
        Assert.Contains("Không hiển thị dữ liệu mẫu", vm.InfoMessage, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeAuditService : IAuditService
    {
        private readonly IReadOnlyList<AuditLog>? _logs;
        private readonly Exception? _error;

        public FakeAuditService(IReadOnlyList<AuditLog> logs) => _logs = logs;
        public FakeAuditService(Exception error) => _error = error;

        public Task LogAsync(string entityName, string entityId, string action, object? oldValues = null, object? newValues = null, string? userName = null, string? ipAddress = null, string? correlationId = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<AuditLog>> QueryAsync(string? entityName = null, string? entityId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            if (_error is not null) return Task.FromException<IReadOnlyList<AuditLog>>(_error);
            var filtered = _logs!
                .Where(x => !fromDate.HasValue || x.CreatedAt >= fromDate.Value)
                .Where(x => !toDate.HasValue || x.CreatedAt <= toDate.Value)
                .ToArray();
            return Task.FromResult<IReadOnlyList<AuditLog>>(filtered);
        }
    }
}
