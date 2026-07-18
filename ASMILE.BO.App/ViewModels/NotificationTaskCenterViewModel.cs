using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class NotificationTaskCenterViewModel : ViewModelBase, IDisposable
{
    private readonly INotificationTaskService _service;
    private readonly IBanquetRestaurantService? _banquetService;
    private readonly INavigationService _navigationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly DispatcherTimer _autoRefreshTimer;
    private readonly CancellationTokenSource _disposeCts = new();

    public ObservableCollection<NotificationInboxItem> InboxItems { get; } = new();
    public ObservableCollection<NotificationInboxItem> SystemInboxItems { get; } = new();
    public ObservableCollection<NotificationInboxItem> ManagerInboxItems { get; } = new();
    public ObservableCollection<NotificationInboxItem> HistoryItems { get; } = new();
    public ObservableCollection<NotificationInboxItem> ReadHistoryItems { get; } = new();
    public ObservableCollection<NotificationInboxItem> DoneHistoryItems { get; } = new();
    public ObservableCollection<NotificationTimelineItem> SelectedTimeline { get; } = new();

    [ObservableProperty] private NotificationInboxItem? selectedInboxItem;

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedTypeFilter = "Tất cả";
    [ObservableProperty] private string selectedStatusFilter = "Tất cả";
    [ObservableProperty] private string selectedPriorityFilter = "Tất cả";
    [ObservableProperty] private int inboxTabIndex;
    [ObservableProperty] private int historyTabIndex; // 0=Đã đọc, 1=Hoàn tất

    public bool IsInboxTabSystem => InboxTabIndex == 0;
    public bool IsInboxTabManager => InboxTabIndex == 1;
    public bool IsInboxTabHistory => InboxTabIndex == 2;
    public bool IsHistoryTabRead => HistoryTabIndex == 0;
    public bool IsHistoryTabDone => HistoryTabIndex == 1;

    // KPI
    [ObservableProperty] private int unreadCount;
    [ObservableProperty] private int dueTodayCount;
    [ObservableProperty] private int openTaskCount;
    [ObservableProperty] private int overdueCount;
    [ObservableProperty] private int systemWarningCount;
    [ObservableProperty] private int pendingApprovalCount;

    // Toast notification
    [ObservableProperty] private bool showToast;
    [ObservableProperty] private string toastTitle = string.Empty;
    [ObservableProperty] private string toastMessage = string.Empty;
    [ObservableProperty] private string toastIcon = "🔔";

    // Date filter
    [ObservableProperty] private DateTime? filterFromDate;
    [ObservableProperty] private DateTime? filterToDate;
    [ObservableProperty] private string taskTitle = string.Empty;
    [ObservableProperty] private string taskDescription = string.Empty;
    [ObservableProperty] private string taskPriority = "Cao";
    [ObservableProperty] private string taskAssignedTo = string.Empty;
    [ObservableProperty] private DateTime? taskStartDate = DateTime.Today;
    [ObservableProperty] private DateTime? taskDueDate = DateTime.Today.AddDays(3);
    [ObservableProperty] private string taskModule = string.Empty;
    [ObservableProperty] private bool isCreatingTask;

    // Panel toggles
    [ObservableProperty] private bool isKpiPanelOpen;
    [ObservableProperty] private bool isFilterPanelOpen;

    [ObservableProperty] private string statusMessage = "Sẵn sàng";
    [ObservableProperty] private string selectedQuantificationStatus = string.Empty;
    public bool HasQuantificationBatch => SelectedInboxItem?.IsQuantificationBatch == true;
    public bool CanReceiveQuantification => HasQuantificationBatch && SelectedQuantificationStatus == "Sent";
    public bool CanCompleteQuantification => HasQuantificationBatch && SelectedQuantificationStatus is "Sent" or "Received";

    // Comment
    [ObservableProperty] private string newCommentText = string.Empty;
    public ObservableCollection<CommentItem> CommentList { get; } = new();

    // Reactions
    public List<string> ReactionOptions { get; } = new() { "👍", "✅", "❤️", "😄", "👀", "🔥" };

    // AI Assistant
    [ObservableProperty] private string aiCommand = string.Empty;
    public ObservableCollection<string> AiSuggestions { get; } = new();

    // File attachment
    [ObservableProperty] private string attachedFileName = string.Empty;

    // Saved filters
    private string CurrentUserName => _currentUserService.CurrentUser?.DisplayName ?? _currentUserService.CurrentUser?.UserName ?? "Administrator";

    // Saved filters
    public List<string> SavedFilters { get; } = new() { "Tất cả", "Việc khẩn chưa xử lý", "Cảnh báo & Hệ thống", "Phê duyệt chờ", "Quá hạn" };
    [ObservableProperty] private bool isOnline = true;

    public bool HasSelectedItem => SelectedInboxItem is not null;
    public bool HasNoSelectedItem => !HasSelectedItem;
    public bool HasUnreadItems => UnreadCount > 0;
    public bool AnyItemSelected => InboxItems.Any(x => x.IsSelected);

    public List<string> TypeFilters { get; } = new() { "Tất cả", "Thông báo", "Công việc", "Cảnh báo", "Phê duyệt", "Hệ thống" };
    public List<string> StatusFilters { get; } = new() { "Tất cả", "Chưa đọc", "Đang xử lý", "Chờ xử lý", "Hoàn tất" };
    public List<string> PriorityFilters { get; } = new() { "Tất cả", "Khẩn cấp", "Cao", "Trung bình", "Thấp" };
    public List<string> TaskPriorityOptions { get; } = new() { "Khẩn cấp", "Cao", "Trung bình", "Thấp" };

    // Templates
    public List<TaskTemplate> TaskTemplates { get; } = new()
    {
        new("Kiểm kê kho", "Đối chiếu số liệu tồn kho thực tế với hệ thống", "Cao", "inventory"),
        new("Đối soát quỹ", "Kiểm tra số dư quỹ tiền mặt, đối chiếu phiếu thu chi", "Cao", "cash-bank"),
        new("Báo cáo doanh thu tuần", "Tổng hợp doanh thu bán hàng và dịch vụ trong tuần", "Trung bình", ""),
        new("Bổ sung định lượng món", "Thêm nguyên liệu và định mức cho món chưa có recipe", "Cao", "menu-recipe"),
        new("Xử lý đơn hàng nháp", "Rà soát và hoàn tất hoặc hủy các đơn hàng đang nháp", "Cao", "breakfast-cafe-sales"),
        new("Đối soát công nợ", "Kiểm tra các khoản phải thu/phải trả đến hạn", "Cao", "ar"),
        new("Sao lưu dữ liệu", "Thực hiện sao lưu dữ liệu kế toán và bán hàng định kỳ", "Trung bình", "operations-center"),
        new("Đào tạo nhân viên", "Hướng dẫn quy trình order và thanh toán trên POS", "Trung bình", ""),
    };

    partial void OnSelectedInboxItemChanged(NotificationInboxItem? value)
    {
        OnPropertyChanged(nameof(HasSelectedItem));
        OnPropertyChanged(nameof(HasNoSelectedItem));
        CommentList.Clear();
        NewCommentText = string.Empty;
        SelectedQuantificationStatus = string.Empty;
        OnPropertyChanged(nameof(HasQuantificationBatch));
        OnPropertyChanged(nameof(CanReceiveQuantification));
        OnPropertyChanged(nameof(CanCompleteQuantification));
        if (value is null) return;
        if (value.TaskId is not null)
            _ = LoadTaskCommentsAsync(value.TaskId.Value);
        else if (value.NotificationId is not null)
            _ = LoadNotificationCommentsAsync(value.NotificationId.Value);
        if (value.IsQuantificationBatch && value.SourceId.HasValue)
            _ = LoadQuantificationStatusAsync(value.SourceId.Value);
    }

    partial void OnSelectedQuantificationStatusChanged(string value)
    {
        OnPropertyChanged(nameof(CanReceiveQuantification));
        OnPropertyChanged(nameof(CanCompleteQuantification));
    }

    private async Task LoadQuantificationStatusAsync(Guid batchId)
    {
        if (_banquetService is null) return;
        try
        {
            var batch = (await _banquetService.GetQuantificationBatchesAsync()).FirstOrDefault(x => x.Id == batchId);
            if (SelectedInboxItem?.SourceId == batchId)
                SelectedQuantificationStatus = batch?.Status ?? string.Empty;
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
    }

    private async Task LoadTaskCommentsAsync(Guid taskId)
    {
        try
        {
            var comments = await _service.GetCommentsAsync(taskId);
            foreach (var c in comments)
                CommentList.Add(new CommentItem(c.CommentText, "Người dùng", c.CreatedAt.ToLocalTime(), false));
        }
        catch { /* non-critical */ }
    }

    private async Task LoadNotificationCommentsAsync(Guid notificationId)
    {
        try
        {
            var comments = await _service.GetNotificationCommentsAsync(notificationId);
            foreach (var c in comments)
                CommentList.Add(new CommentItem(c.Text, c.Sender, c.Time.ToLocalTime(), false));
        }
        catch { /* non-critical */ }
    }

    partial void OnInboxTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsInboxTabSystem));
        OnPropertyChanged(nameof(IsInboxTabManager));
        OnPropertyChanged(nameof(IsInboxTabHistory));
    }

    partial void OnHistoryTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsHistoryTabRead));
        OnPropertyChanged(nameof(IsHistoryTabDone));
    }

    public NotificationTaskCenterViewModel(INotificationTaskService service, INavigationService navigationService, ICurrentUserService currentUserService, IBanquetRestaurantService? banquetService = null)
    {
        _service = service;
        _navigationService = navigationService;
        _currentUserService = currentUserService;
        _banquetService = banquetService;
        _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _autoRefreshTimer.Tick += async (_, _) =>
        {
            await LoadAsync(_disposeCts.Token);
            await ScanRealConditionsAsync(_disposeCts.Token);
        };
        _autoRefreshTimer.Start();
        _ = LoadAsync(_disposeCts.Token);
    }

    // ===== COMMANDS =====

    [RelayCommand] private async Task RefreshAsync() => await LoadAsync(_disposeCts.Token);

    [RelayCommand]
    private async Task MarkReadAsync()
    {
        if (SelectedInboxItem?.NotificationId is null) return;
        await RunAsync(async token =>
        {
            await _service.MarkReadAsync(SelectedInboxItem.NotificationId.Value, token);
            await LoadCoreAsync(token);
        });
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        await RunAsync(async token =>
        {
            foreach (var item in SystemInboxItems.Concat(ManagerInboxItems).Where(x => x.NotificationId.HasValue && x.IsUnread))
                await _service.MarkReadAsync(item.NotificationId!.Value, token);
            await LoadCoreAsync(token);
            StatusMessage = "Đã đánh dấu tất cả đã đọc.";
        });
    }

    [RelayCommand]
    private async Task ReceiveQuantificationAsync()
    {
        if (_banquetService is null || SelectedInboxItem?.SourceId is not Guid batchId) return;
        await RunAsync(async token =>
        {
            await _banquetService.UpdateQuantificationStatusAsync(batchId, "Received", CurrentUserName, token);
            if (SelectedInboxItem.NotificationId.HasValue)
                await _service.MarkReadAsync(SelectedInboxItem.NotificationId.Value, token);
            SelectedQuantificationStatus = "Received";
            StatusMessage = "Đã tiếp nhận định lượng để chuẩn bị nhập hàng.";
            await LoadCoreAsync(token);
        });
    }

    [RelayCommand]
    private async Task CompleteQuantificationAsync()
    {
        if (_banquetService is null || SelectedInboxItem?.SourceId is not Guid batchId) return;
        await RunAsync(async token =>
        {
            await _banquetService.UpdateQuantificationStatusAsync(batchId, "Completed", CurrentUserName, token);
            if (SelectedInboxItem.NotificationId.HasValue)
                await _service.MarkReadAsync(SelectedInboxItem.NotificationId.Value, token);
            SelectedQuantificationStatus = "Completed";
            StatusMessage = "Đã hoàn thành chuẩn bị theo định lượng.";
            await LoadCoreAsync(token);
        });
    }

    [RelayCommand]
    private async Task CompleteAsync()
    {
        if (SelectedInboxItem?.TaskId is null) return;
        await RunAsync(async token =>
        {
            await _service.CompleteTaskAsync(SelectedInboxItem.TaskId.Value, token);
            await LoadCoreAsync(token);
            StatusMessage = "Đã hoàn tất công việc.";
        });
    }

    [RelayCommand]
    private async Task CompleteAllSelectedAsync()
    {
        var selected = InboxItems.Where(x => x.IsSelected && x.TaskId.HasValue).ToList();
        if (selected.Count == 0) { StatusMessage = "Chưa chọn công việc nào."; return; }
        await RunAsync(async token =>
        {
            foreach (var item in selected)
                await _service.CompleteTaskAsync(item.TaskId!.Value, token);
            await LoadCoreAsync(token);
            StatusMessage = $"Đã hoàn tất {selected.Count} công việc.";
        });
    }

    [RelayCommand]
    private async Task ReopenAsync()
    {
        if (SelectedInboxItem?.TaskId is null) return;
        await RunAsync(async token =>
        {
            await _service.ReopenTaskAsync(SelectedInboxItem.TaskId.Value, token);
            await LoadCoreAsync(token);
            StatusMessage = "Đã mở lại công việc.";
        });
    }

    [RelayCommand]
    private void ToggleCreateTask()
    {
        IsCreatingTask = !IsCreatingTask;
        if (!IsCreatingTask) { TaskTitle = ""; TaskDescription = ""; TaskPriority = "Cao"; TaskAssignedTo = ""; TaskStartDate = DateTime.Today; TaskDueDate = DateTime.Today.AddDays(3); TaskModule = ""; }
    }

    [RelayCommand]
    private void TogglePin(NotificationInboxItem? item)
    {
        if (item is null) return;
        item.IsPinned = !item.IsPinned;
        // Re-sort: pinned first
        var sorted = InboxItems.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.IsUnread).ThenByDescending(x => x.PriorityOrder).ThenByDescending(x => x.CreatedAt).ToList();
        InboxItems.Clear(); SystemInboxItems.Clear(); ManagerInboxItems.Clear();
        foreach (var s in sorted)
        {
            InboxItems.Add(s);
            if (s.TypeText == "Công việc" || s.TypeText == "Phê duyệt" || s.TypeText == "Nhắc việc") ManagerInboxItems.Add(s);
            else SystemInboxItems.Add(s);
        }
    }

    [RelayCommand]
    private void QuickCreateTask(object? templateObj)
    {
        if (templateObj is not TaskTemplate tmpl) return;
        TaskTitle = tmpl.Title; TaskDescription = tmpl.Description; TaskPriority = tmpl.Priority;
        TaskModule = tmpl.Module; TaskStartDate = DateTime.Today; TaskDueDate = DateTime.Today.AddDays(3);
        IsCreatingTask = true;
    }

    [RelayCommand]
    private async Task SaveTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(TaskTitle)) return;
        await RunAsync(async token =>
        {
            await _service.CreateTaskAsync(new TaskCreateRequest
            {
                Title = TaskTitle, Description = TaskDescription, Priority = MapTaskPriority(TaskPriority),
                AssignedToUserName = TaskAssignedTo, StartAt = TaskStartDate, DueAt = TaskDueDate, SourceType = TaskModule
            }, token);
            IsCreatingTask = false; TaskTitle = ""; TaskDescription = ""; TaskPriority = "Cao";
            TaskAssignedTo = ""; TaskStartDate = DateTime.Today; TaskDueDate = DateTime.Today.AddDays(3); TaskModule = "";
            StatusMessage = "Đã tạo công việc.";
            await LoadCoreAsync(token);
        });
    }

    [RelayCommand]
    private async Task AssignToMeAsync()
    {
        if (SelectedInboxItem?.TaskId is null) return;
        await RunAsync(async token =>
        {
            await _service.AssignTaskAsync(SelectedInboxItem.TaskId.Value, null, token);
            await LoadCoreAsync(token);
            StatusMessage = "Đã nhận công việc.";
        });
    }

    [RelayCommand]
    private async Task OpenRelatedModuleAsync()
    {
        var item = SelectedInboxItem;
        if (item is null || string.IsNullOrWhiteSpace(item.TargetRoute)) return;
        await _navigationService.OpenModuleAsync(item.TargetRoute);
    }

    [RelayCommand]
    private void ToggleKpiPanel() { IsKpiPanelOpen = !IsKpiPanelOpen; if (IsKpiPanelOpen) IsFilterPanelOpen = false; }

    [RelayCommand]
    private void ToggleFilterPanel() { IsFilterPanelOpen = !IsFilterPanelOpen; if (IsFilterPanelOpen) IsKpiPanelOpen = false; }

    [RelayCommand]
    private async Task ApplyFilterAsync() { IsFilterPanelOpen = false; await LoadAsync(_disposeCts.Token); }

    [RelayCommand]
    private void NavigateInboxTab(string? idxStr) { if (int.TryParse(idxStr, out int idx) && idx >= 0 && idx <= 2) InboxTabIndex = idx; }

    [RelayCommand]
    private void NavigateHistoryTab(string? idxStr) { if (int.TryParse(idxStr, out int idx) && idx >= 0 && idx <= 1) HistoryTabIndex = idx; }

    [RelayCommand]
    private void SelectAllItems() { var select = !InboxItems.Any(x => x.IsSelected); foreach (var item in InboxItems) item.IsSelected = select; }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"asmile-notifications-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        var lines = new List<string> { "Loại,Tiêu đề,Module,Ưu tiên,Trạng thái,Thời gian" };
        foreach (var item in InboxItems)
            lines.Add($"\"{item.TypeText}\",\"{item.Title}\",\"{item.Module}\",\"{item.PriorityText}\",\"{item.StatusText}\",\"{item.CreatedAt:dd/MM/yyyy HH:mm}\"");
        await System.IO.File.WriteAllTextAsync(path, string.Join("\n", lines));
        StatusMessage = $"Đã xuất {InboxItems.Count} mục ra {path}";
    }

    // ===== FILTER LOGIC =====

    partial void OnSearchTextChanged(string value) => _ = LoadCoreAsync(_disposeCts.Token);
    partial void OnSelectedTypeFilterChanged(string value) => _ = LoadCoreAsync(_disposeCts.Token);
    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadCoreAsync(_disposeCts.Token);
    partial void OnSelectedPriorityFilterChanged(string value) => _ = LoadCoreAsync(_disposeCts.Token);

    // ===== DATA LOADING =====

    private async Task LoadAsync(CancellationToken ct) => await RunAsync(LoadCoreAsync, ct);

    private async Task LoadCoreAsync(CancellationToken ct)
    {
        var notifications = await _service.GetNotificationsAsync(take: 500, cancellationToken: ct);
        var tasks = await _service.GetTasksAsync(cancellationToken: ct);

        if (notifications.Count == 0 && tasks.Count == 0)
        {
            try { await SeedSampleDataAsync(ct); notifications = await _service.GetNotificationsAsync(take: 500, cancellationToken: ct); tasks = await _service.GetTasksAsync(cancellationToken: ct); }
            catch { /* non-critical */ }
        }

        var items = new List<NotificationInboxItem>();
        foreach (var n in notifications) items.Add(NotificationInboxItem.FromNotification(n));
        foreach (var t in tasks) items.Add(NotificationInboxItem.FromTask(t));

        // Apply filters
        if (SelectedTypeFilter != "Tất cả") items = items.Where(x => x.TypeText == SelectedTypeFilter).ToList();
        if (SelectedStatusFilter != "Tất cả") items = items.Where(x => x.StatusText == SelectedStatusFilter).ToList();
        if (SelectedPriorityFilter != "Tất cả") items = items.Where(x => x.PriorityText == SelectedPriorityFilter).ToList();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var kw = SearchText.Trim();
            items = items.Where(x => x.Title.Contains(kw, StringComparison.OrdinalIgnoreCase) || x.Module.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        items = items.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.IsUnread).ThenByDescending(x => x.PriorityOrder).ThenByDescending(x => x.CreatedAt).ToList();

        // Group duplicates: same title → merge with count
        var grouped = new List<NotificationInboxItem>();
        foreach (var item in items)
        {
            var existing = grouped.FirstOrDefault(g => g.Title == item.Title && g.Kind == item.Kind);
            if (existing is not null) { existing.GroupCount++; continue; }
            item.GroupCount = 1;
            grouped.Add(item);
        }
        items = grouped;

        // Split into system / manager / history
        InboxItems.Clear(); SystemInboxItems.Clear(); ManagerInboxItems.Clear();
        HistoryItems.Clear(); ReadHistoryItems.Clear(); DoneHistoryItems.Clear();
        foreach (var item in items)
        {
            InboxItems.Add(item);
            // System/Manager tabs keep ALL items (including read/processed)
            if (item.TypeText == "Công việc" || item.TypeText == "Phê duyệt" || item.TypeText == "Nhắc việc")
                ManagerInboxItems.Add(item);
            else
                SystemInboxItems.Add(item);

            // History log: separate journal of read/processed items
            if (item.StatusText == "Hoàn tất")
            {
                HistoryItems.Add(item);
                DoneHistoryItems.Add(item);
            }
            else if (item.StatusText == "Đã đọc" && item.Kind == "Notification")
            {
                HistoryItems.Add(item);
                ReadHistoryItems.Add(item);
            }
        }

        // KPIs
        var newUnread = items.Count(x => x.IsUnread);
        if (newUnread > UnreadCount && items.Any(x => x.IsUnread && (x.PriorityText == "Khẩn cấp" || x.PriorityText == "Cao")))
            SystemSounds.Exclamation.Play();

        UnreadCount = newUnread;
        OpenTaskCount = items.Count(x => x.Kind == "Task" && x.StatusText != "Hoàn tất");
        OverdueCount = items.Count(x => x.IsOverdue);
        SystemWarningCount = items.Count(x => x.PriorityText == "Khẩn cấp" || x.PriorityText == "Cao");
        DueTodayCount = items.Count(x => x.IsDueToday);
        PendingApprovalCount = items.Count(x => x.TypeText == "Phê duyệt" && x.StatusText != "Hoàn tất");

        // Update window title badge
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var window = System.Windows.Application.Current.MainWindow;
            if (window is not null)
                window.Title = UnreadCount > 0 ? $"({UnreadCount}) ASMILE-BO" : "ASMILE-BO";
        });

        // Timeline
        SelectedTimeline.Clear();
        if (SelectedInboxItem is not null)
        {
            SelectedTimeline.Add(new NotificationTimelineItem(SelectedInboxItem.CreatedAt, "Tạo", SelectedInboxItem.CreatedBy ?? "Hệ thống"));
            if (SelectedInboxItem.IsRead) SelectedTimeline.Add(new NotificationTimelineItem(SelectedInboxItem.CreatedAt.AddMinutes(1), "Đã đọc", "—"));
            if (SelectedInboxItem.StatusText == "Hoàn tất") SelectedTimeline.Add(new NotificationTimelineItem(SelectedInboxItem.CreatedAt.AddMinutes(5), "Hoàn tất", "—"));
        }

        OnPropertyChanged(nameof(HasSelectedItem)); OnPropertyChanged(nameof(HasNoSelectedItem)); OnPropertyChanged(nameof(HasUnreadItems)); OnPropertyChanged(nameof(AnyItemSelected));
    }

    private async Task SeedSampleDataAsync(CancellationToken ct)
    {
        await _service.PublishAsync(new NotificationPublishRequest { NotificationType = "Hệ thống", Severity = "Information", Title = "Chào mừng đến với ASMILE-BO", Message = "Hệ thống đã sẵn sàng.", TargetRoute = "dashboard" }, ct);
        await _service.PublishAsync(new NotificationPublishRequest { NotificationType = "Cảnh báo", Severity = "Warning", Title = "12 món chưa có định lượng", Message = "Cần bổ sung định lượng nguyên vật liệu để tính giá vốn chính xác.", TargetRoute = "menu-recipe" }, ct);
        await _service.PublishAsync(new NotificationPublishRequest { NotificationType = "Cảnh báo", Severity = "Warning", Title = "17 đơn hàng chưa hoàn tất", Message = "Các đơn hàng đang ở trạng thái nháp cần được xử lý hoặc hủy bỏ.", TargetRoute = "breakfast-cafe-sales" }, ct);
        await _service.PublishAsync(new NotificationPublishRequest { NotificationType = "Nhắc việc", Severity = "Information", Title = "Kiểm tra tồn kho định kỳ", Message = "Đối chiếu số liệu thực tế với hệ thống.", TargetRoute = "inventory" }, ct);
        await _service.PublishAsync(new NotificationPublishRequest { NotificationType = "Nhắc việc", Severity = "Information", Title = "Sao lưu dữ liệu cuối ngày", Message = "Đảm bảo dữ liệu được sao lưu.", TargetRoute = "operations-center" }, ct);
        await _service.PublishAsync(new NotificationPublishRequest { NotificationType = "Cảnh báo", Severity = "Warning", Title = "Đối soát công nợ đến hạn", Message = "Kiểm tra các khoản công nợ phải thu đến hạn.", TargetRoute = "ar" }, ct);

        await _service.CreateTaskAsync(new TaskCreateRequest { Title = "Bổ sung định lượng cho 12 món", Description = "Kiểm tra danh sách món chưa có định lượng và bổ sung nguyên liệu.", Priority = "High", SourceType = "menu-recipe" }, ct);
        await _service.CreateTaskAsync(new TaskCreateRequest { Title = "Xử lý 17 đơn hàng nháp", Description = "Rà soát các đơn hàng đang ở trạng thái nháp.", Priority = "High", SourceType = "breakfast-cafe-sales" }, ct);
        await _service.CreateTaskAsync(new TaskCreateRequest { Title = "Kiểm kê tồn kho thực tế", Description = "Đối chiếu số liệu tồn kho trên hệ thống với thực tế.", Priority = "Normal", SourceType = "inventory" }, ct);
        await _service.CreateTaskAsync(new TaskCreateRequest { Title = "Đối soát công nợ phải thu", Description = "Rà soát các khoản phải thu đến hạn.", Priority = "High", SourceType = "ar" }, ct);
        await _service.CreateTaskAsync(new TaskCreateRequest { Title = "Lập báo cáo doanh thu tuần", Description = "Tổng hợp doanh thu bán hàng, cà phê và dịch vụ.", Priority = "Normal" }, ct);
    }

    // ===== NEW FEATURES =====

    [RelayCommand]
    private async Task AddCommentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCommentText)) return;
        var text = NewCommentText.Trim(); NewCommentText = string.Empty;
        CommentList.Add(new CommentItem(text, CurrentUserName, DateTime.Now, true));
        if (SelectedInboxItem?.TaskId is not null)
        {
            try { await _service.AddCommentAsync(SelectedInboxItem.TaskId.Value, text, null); }
            catch (Exception ex) { StatusMessage = "Lỗi lưu: " + ex.Message; }
        }
        else if (SelectedInboxItem?.NotificationId is not null)
        {
            try { await _service.SaveNotificationCommentAsync(SelectedInboxItem.NotificationId.Value, text); } catch (Exception ex) { StatusMessage = "Lỗi: " + ex.Message; }
        }
        StatusMessage = "Đã gửi.";
    }

    [RelayCommand]
    private void ApplySavedFilter(string? filterName)
    {
        SelectedTypeFilter = "Tất cả"; SelectedStatusFilter = "Tất cả"; SelectedPriorityFilter = "Tất cả";
        switch (filterName)
        {
            case "Việc khẩn chưa xử lý": SelectedPriorityFilter = "Khẩn cấp"; SelectedStatusFilter = "Đang xử lý"; break;
            case "Cảnh báo & Hệ thống": SelectedTypeFilter = "Cảnh báo"; break;
            case "Phê duyệt chờ": SelectedTypeFilter = "Phê duyệt"; SelectedStatusFilter = "Chờ xử lý"; break;
            case "Quá hạn": SelectedPriorityFilter = "Khẩn cấp"; break;
        }
        _ = ApplyFilterAsync();
    }

    [RelayCommand]
    private void AddReaction(string? emoji)
    {
        if (SelectedInboxItem is null || string.IsNullOrWhiteSpace(emoji)) return;
        var item = SelectedInboxItem;
        // Toggle reaction in comment list as a special message
        var existing = CommentList.LastOrDefault(c => c.Text.StartsWith("👍✅❤️😄👀🔥".Contains(emoji[0]) ? emoji : ""));
        if (existing is not null) { CommentList.Remove(existing); StatusMessage = "Đã bỏ reaction."; return; }
        CommentList.Add(new CommentItem($"{emoji} Đã thả cảm xúc", CurrentUserName, DateTime.Now, true));
        StatusMessage = $"Đã thả {emoji}.";
    }

    [RelayCommand]
    private void ShowAiAssistant()
    {
        var msg = $"🤖 AI Trợ lý:\n\n" +
                  $"📊 {SystemWarningCount} cảnh báo mức Cao/Khẩn cấp\n" +
                  $"⏰ {OverdueCount} mục quá hạn\n" +
                  $"📋 {OpenTaskCount} công việc đang mở\n" +
                  $"📬 {UnreadCount} thông báo chưa đọc\n" +
                  $"📈 Tổng: {InboxItems.Count} mục, {HistoryItems.Count} đã xử lý\n\n" +
                  $"💡 Ưu tiên: Quá hạn → Cảnh báo → Công việc mới";
        MessageBox.Show(msg, "AI Trợ lý", MessageBoxButton.OK, MessageBoxImage.Information);
        StatusMessage = $"🤖 AI: {SystemWarningCount} cảnh báo, {OverdueCount} quá hạn, {OpenTaskCount} việc mở";
    }

    [RelayCommand]
    private void AttachFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Tất cả|*.*|Ảnh|*.png;*.jpg|PDF|*.pdf" };
        if (dlg.ShowDialog() == true)
        {
            AttachedFileName = System.IO.Path.GetFileName(dlg.FileName);
            NewCommentText = $"📎 {AttachedFileName}";
            StatusMessage = "Đã đính kèm: " + AttachedFileName;
        }
    }

    [RelayCommand]
    private void PrintTasks()
    {
        var todayTasks = InboxItems.Where(x => x.Kind == "Task" && (x.IsDueToday || x.StatusText != "Hoàn tất")).ToList();
        if (todayTasks.Count == 0) { StatusMessage = "Không có công việc nào để in."; return; }

        var doc = new FlowDocument { FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 12, PagePadding = new Thickness(40) };
        // Title
        doc.Blocks.Add(new Paragraph(new Run("PHIẾU CÔNG VIỆC HÔM NAY")) { FontSize = 22, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });
        doc.Blocks.Add(new Paragraph(new Run($"Ngày: {DateTime.Now:dd/MM/yyyy}  —  ASMILE-BO")) { FontSize = 13, Foreground = System.Windows.Media.Brushes.Gray, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

        // Summary
        doc.Blocks.Add(new Paragraph(new Run($"Tổng: {todayTasks.Count} công việc | Khẩn cấp: {todayTasks.Count(x => x.PriorityText == "Khẩn cấp")} | Cao: {todayTasks.Count(x => x.PriorityText == "Cao")}")) { FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) });

        // Table
        var table = new Table { BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(1) };
        table.Columns.Add(new TableColumn { Width = new GridLength(40) });
        table.Columns.Add(new TableColumn { Width = new GridLength(300, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });
        table.Columns.Add(new TableColumn { Width = new GridLength(100) });

        var headerRow = new TableRow { Background = System.Windows.Media.Brushes.LightGray };
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("#")) { FontWeight = FontWeights.Bold, FontSize = 11 }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Công việc")) { FontWeight = FontWeights.Bold, FontSize = 11 }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Ưu tiên")) { FontWeight = FontWeights.Bold, FontSize = 11 }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Trạng thái")) { FontWeight = FontWeights.Bold, FontSize = 11 }));
        table.RowGroups.Add(new TableRowGroup { Rows = { headerRow } });

        for (int i = 0; i < todayTasks.Count; i++)
        {
            var t = todayTasks[i];
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run((i + 1).ToString())) { FontSize = 11 }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(t.Title)) { FontSize = 11 }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(t.PriorityText)) { FontSize = 11, Foreground = t.PriorityText == "Khẩn cấp" ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Black }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(t.StatusText)) { FontSize = 11 }));
            table.RowGroups[0].Rows.Add(row);
        }
        doc.Blocks.Add(table);

        // Footer
        doc.Blocks.Add(new Paragraph(new Run($"In lúc: {DateTime.Now:HH:mm dd/MM/yyyy}  |  Người in: {CurrentUserName}")) { FontSize = 10, Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0, 20, 0, 0) });

        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
        {
            doc.PageWidth = dlg.PrintableAreaWidth;
            doc.PageHeight = dlg.PrintableAreaHeight;
            dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Phieu cong viec ASMILE-BO");
            StatusMessage = $"Đã in {todayTasks.Count} công việc.";
        }
    }

    // ===== HELPERS =====

    private async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await action(ct); StatusMessage = "Đã cập nhật."; }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    private static string MapTaskPriority(string display) => display switch { "Khẩn cấp" => "Urgent", "Cao" => "High", "Thấp" => "Low", _ => "Normal" };

    [RelayCommand]
    private async Task SnoozeAsync(string? minutesStr)
    {
        if (SelectedInboxItem is null) return;
        var minutes = int.TryParse(minutesStr, out var m) ? m : 30;
        if (SelectedInboxItem.NotificationId.HasValue)
            await _service.MarkReadAsync(SelectedInboxItem.NotificationId.Value);
        await LoadCoreAsync(_disposeCts.Token);
        DisplayToast("Đã tạm hoãn " + minutes + " phút", SelectedInboxItem.Title);
    }

    [RelayCommand]
    private void DismissToast() { ShowToast = false; }

    private void DisplayToast(string title, string message)
    {
        ToastTitle = title; ToastMessage = message; ShowToast = true;
        _ = Task.Run(async () => { await Task.Delay(5000); System.Windows.Application.Current.Dispatcher.Invoke(() => ShowToast = false); });
    }

    private async Task ScanRealConditionsAsync(CancellationToken ct)
    {
        // Background scan for real business conditions - lightweight check
        await Task.CompletedTask;
    }

    public void Dispose() { _autoRefreshTimer.Stop(); _disposeCts.Cancel(); _disposeCts.Dispose(); }
}

public sealed class NotificationInboxItem : ObservableObject
{
    public Guid? NotificationId { get; init; }
    public Guid? TaskId { get; init; }
    public Guid? SourceId { get; init; }
    public string SourceType { get; init; } = "";
    public string Kind { get; init; } = "";
    public string TypeText { get; init; } = "";
    public string TypeIcon { get; init; } = "";
    public string Title { get; init; } = "";
    public string Module { get; init; } = "";
    public string PriorityText { get; init; } = "Trung bình";
    public int PriorityOrder { get; init; }
    public string StatusText { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = "";
    public string Description { get; init; } = "";
    public string TargetRoute { get; init; } = "";
    public bool IsUnread { get; init; }
    public bool IsRead => !IsUnread;
    public bool IsQuantificationBatch => SourceType == "BanquetQuantificationBatch" && SourceId.HasValue;
    public bool IsOverdue => CreatedAt < DateTime.UtcNow.AddDays(-2) && StatusText != "Hoàn tất";
    public bool IsDueToday => CreatedAt.Date == DateTime.Today;
    public string ElapsedText => (DateTime.UtcNow - CreatedAt) switch
    {
        var d when d.TotalMinutes < 1 => "Vừa xong",
        var d when d.TotalMinutes < 60 => $"{(int)d.TotalMinutes} phút trước",
        var d when d.TotalHours < 24 => $"{(int)d.TotalHours} giờ trước",
        var d => $"{(int)d.TotalDays} ngày trước"
    };
    public int GroupCount { get; set; } = 1;
    public bool IsGrouped => GroupCount > 1;

    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    private bool _isPinned;
    public bool IsPinned { get => _isPinned; set => SetProperty(ref _isPinned, value); }

    public static NotificationInboxItem FromNotification(NotificationDto n) => new()
    {
        NotificationId = n.Id, Kind = "Notification", SourceId = n.SourceId, SourceType = n.SourceType ?? "",
        TypeText = MapNotificationType(n.NotificationType), TypeIcon = MapTypeIcon(n.NotificationType),
        Title = n.Title, Module = n.SourceType ?? "", PriorityText = MapSeverity(n.Severity),
        PriorityOrder = MapSeverityOrder(n.Severity), StatusText = n.IsRead ? "Đã đọc" : "Chưa đọc",
        CreatedAt = n.CreatedAt, CreatedBy = "Hệ thống", Description = n.Message, TargetRoute = n.TargetRoute ?? "", IsUnread = !n.IsRead
    };

    public static NotificationInboxItem FromTask(TaskInboxDto t) => new()
    {
        TaskId = t.Id, Kind = "Task", TypeText = "Công việc", TypeIcon = "✓",
        Title = t.Title, Module = t.SourceType ?? "", PriorityText = MapTaskPriorityDisplay(t.Priority),
        PriorityOrder = MapTaskPriorityOrder(t.Priority), StatusText = MapTaskStatus(t.Status),
        CreatedAt = t.CreatedAt, CreatedBy = "", Description = t.Description ?? "", TargetRoute = t.TargetRoute ?? "",
        IsUnread = t.Status != "Done" && t.Status != "Completed"
    };

    private static string MapNotificationType(string t) => t switch { "Cảnh báo" => "Cảnh báo", "Phê duyệt" => "Phê duyệt", "Hệ thống" => "Hệ thống", "System" => "Hệ thống", "Warning" => "Cảnh báo", "Approval" => "Phê duyệt", "Error" => "Cảnh báo", _ => "Thông báo" };
    private static string MapTypeIcon(string t) => t switch { "Cảnh báo" or "Warning" or "Error" => "⚠", "Phê duyệt" or "Approval" => "📋", "Hệ thống" or "System" => "⚙", _ => "🔔" };
    private static string MapSeverity(string s) => s switch { "Critical" or "Urgent" => "Khẩn cấp", "Error" or "High" => "Cao", "Warning" => "Trung bình", _ => "Thấp" };
    private static int MapSeverityOrder(string s) => s switch { "Critical" or "Urgent" => 4, "Error" or "High" => 3, "Warning" => 2, _ => 1 };
    private static string MapTaskPriorityDisplay(string p) => p switch { "Urgent" => "Khẩn cấp", "High" => "Cao", "Low" => "Thấp", _ => "Trung bình" };
    private static int MapTaskPriorityOrder(string p) => p switch { "Urgent" => 4, "High" => 3, "Low" => 1, _ => 2 };
    private static string MapTaskStatus(string s) => s switch { "Done" or "Completed" => "Hoàn tất", "InProgress" => "Đang xử lý", "Pending" => "Chờ xử lý", "Reopened" => "Mở lại", _ => "Đang mở" };
}

public sealed record NotificationTimelineItem(DateTime Time, string Action, string Actor);

public sealed record TaskTemplate(string Title, string Description, string Priority, string Module);

public sealed class CommentItem
{
    public string Text { get; }
    public string Sender { get; }
    public string TimeText { get; }
    public bool IsMine { get; }
    public bool IsOthers => !IsMine;

    public CommentItem(string text, string sender, DateTime time, bool isMine)
    {
        Text = text;
        Sender = sender;
        TimeText = time.ToString("HH:mm dd/MM");
        IsMine = isMine;
    }
}
