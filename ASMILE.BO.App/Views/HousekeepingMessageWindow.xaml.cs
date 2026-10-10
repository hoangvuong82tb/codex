using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class HousekeepingMessageWindow : Window
{
    private List<HskRequest> _allRequests;
    private int _currentPage;
    private const int PageSize = 20;
    private readonly IHousekeepingBoardService? _boardService;
    private readonly IHousekeepingWorkflowService? _workflowService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IServiceScope? _serviceScope;
    private readonly string? _initialRoomNumber;

    public HousekeepingMessageWindow(string? initialRoomNumber = null)
    {
        InitializeComponent();
        _initialRoomNumber = initialRoomNumber;
        Owner = System.Windows.Application.Current.MainWindow;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _boardService = scopedServices?.GetService<IHousekeepingBoardService>();
        _workflowService = scopedServices?.GetService<IHousekeepingWorkflowService>();
        _executionContext = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _allRequests = new List<HskRequest>();
        _currentPage = 0;
        ShowTab(0);
        UpdateMetrics();
        ApplyFilters();
        Loaded += async (_, _) => await LoadPersistedTasksAsync();
        Closed += (_, _) => _serviceScope?.Dispose();
    }

    private async Task LoadPersistedTasksAsync()
    {
        if (_boardService is null || _executionContext?.Current is not { } context)
        {
            _allRequests.Clear();
            ApplyFilters();
            FooterTotal.Text = "Chưa cấu hình được Housekeeping service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var tasks = await _boardService.GetTaskBoardAsync(
                context.Property.PropertyId.Value, null, null, null, CancellationToken.None);
            _allRequests = tasks.Select(MapPersistedTask).ToList();
            if (!string.IsNullOrWhiteSpace(_initialRoomNumber))
                SearchBox.Text = _initialRoomNumber.Trim();
            ApplyFilters();
            FooterTotal.Text = $"Đã tải {_allRequests.Count:N0} yêu cầu nhà buồng từ dữ liệu thật.";
        }
        catch (Exception ex)
        {
            _allRequests.Clear();
            ApplyFilters();
            FooterTotal.Text = $"Không tải được yêu cầu nhà buồng thật: {ex.Message}";
        }
    }

    private static HskRequest MapPersistedTask(HkTaskBoardDto task)
    {
        var priority = task.Priority switch
        {
            RoomPriority.VIP or RoomPriority.ManagerPriority => ("Khẩn cấp", "#FEE2E2", "#DC2626", "⚡"),
            RoomPriority.ArrivingToday or RoomPriority.EarlyCheckIn or RoomPriority.LateCheckOut => ("Cao", "#FFF7ED", "#EA580C", "⚠"),
            _ => ("Bình thường", "#DBEAFE", "#2563EB", "")
        };
        var status = task.TaskStatus switch
        {
            HousekeepingTaskStatus.Pending => ("Mới tạo", "#DBEAFE", "#2563EB"),
            HousekeepingTaskStatus.Assigned or HousekeepingTaskStatus.Accepted => ("Chờ xác nhận", "#FEF9C3", "#A16207"),
            HousekeepingTaskStatus.Cleaning or HousekeepingTaskStatus.Paused => ("Đang xử lý", "#FFF7ED", "#EA580C"),
            HousekeepingTaskStatus.WaitingInspection => ("Chờ xác nhận", "#FEF9C3", "#A16207"),
            HousekeepingTaskStatus.Completed => ("Hoàn thành", "#DCFCE7", "#16A34A"),
            HousekeepingTaskStatus.Cancelled => ("Đã hủy", "#F1F5F9", "#64748B"),
            _ => (task.TaskStatus.ToString(), "#F1F5F9", "#64748B")
        };
        var type = task.TaskType switch
        {
            HousekeepingTaskType.DepartureClean => "Dọn phòng ngay",
            HousekeepingTaskType.StayoverService => "Thay ga/gối",
            HousekeepingTaskType.DeepClean => "Kiểm tra phòng",
            HousekeepingTaskType.Inspection => "Kiểm tra phòng",
            HousekeepingTaskType.MaintenanceFollowUp => "Sửa thiết bị",
            HousekeepingTaskType.VIPPreparation => "Dọn phòng ngay",
            HousekeepingTaskType.ExtraBed => "Bổ sung đồ dùng",
            _ => "Khác"
        };
        var deadline = task.StartedAt?.AddMinutes(task.EstimatedMinutesLeft ?? 0).ToString("HH:mm") ?? "—";
        var alert = task.IsOverdue ? "⚡" : task.IsVip ? "★" : "";
        return new HskRequest(
            priority.Item1,
            priority.Item2,
            priority.Item3,
            task.TaskNumber,
            task.RoomNumber,
            type,
            $"Tác vụ {type} cho phòng {task.RoomNumber}",
            "Housekeeping",
            task.AssignedName ?? "Chưa phân công",
            task.StartedAt?.ToLocalTime().ToString("HH:mm") ?? "—",
            deadline,
            status.Item1,
            status.Item2,
            status.Item3,
            alert)
        {
            TaskId = task.TaskId,
            AssignedHousekeeperId = task.AssignedHousekeeperId,
            TaskStatus = task.TaskStatus
        };
    }

    private void ApplyFilters()
    {
        var f = _allRequests.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text)) { var kw = SearchBox.Text.ToLower(); f = f.Where(r => r.Content.ToLower().Contains(kw) || r.RoomNumber.Contains(kw) || r.RequestId.ToLower().Contains(kw)); }
        if (FloorFilter.SelectedItem is ComboBoxItem fl && fl.Content is string fv && fv != "Tất cả") f = f.Where(r => r.RoomNumber.StartsWith(fv));
        if (TypeFilter.SelectedItem is ComboBoxItem tp && tp.Content is string tv && tv != "Tất cả") f = f.Where(r => r.RequestType == tv);
        if (PriorityFilter.SelectedItem is ComboBoxItem pr && pr.Content is string pv && pv != "Tất cả") f = f.Where(r => r.Priority == pv);
        if (StatusFilter.SelectedItem is ComboBoxItem st && st.Content is string sv && sv != "Tất cả") f = f.Where(r => r.Status == sv);
        if (ChkUrgent.IsChecked == true) f = f.Where(r => r.Priority == "Khẩn cấp");
        if (ChkOverdue.IsChecked == true) f = f.Where(r => r.Status == "Quá hạn");
        var list = f.ToList(); _currentPage = 0; ApplyPage(list); UpdateMetrics(list);
    }

    private void ApplyPage(List<HskRequest> list)
    {
        var paged = list.Skip(_currentPage * PageSize).Take(PageSize).ToList();
        RequestGrid.ItemsSource = paged;
        ResultCount.Text = $"{list.Count} yêu cầu";
        FooterTotal.Text = $"Tổng: {list.Count} yêu cầu · {list.Count(r => r.Status != "Hoàn thành")} đang mở";
    }

    private void UpdateMetrics(List<HskRequest>? list = null)
    {
        list ??= _allRequests;
        MetricOpen.Text = list.Count(r => r.Status != "Hoàn thành").ToString();
        MetricUrgent.Text = list.Count(r => r.Priority == "Khẩn cấp").ToString();
        MetricProcessing.Text = list.Count(r => r.Status == "Đang xử lý").ToString();
        MetricPendingConfirm.Text = list.Count(r => r.Status == "Chờ xác nhận").ToString();
        MetricOverdue.Text = list.Count(r => r.Status == "Quá hạn").ToString();
        MetricCompleted.Text = list.Count(r => r.Status == "Hoàn thành").ToString();
    }

    private void OnRequestSelected(object sender, SelectionChangedEventArgs e)
    {
        if (RequestGrid.SelectedItem is not HskRequest r) { DetailEmpty.Visibility = Visibility.Visible; DetailContent.Visibility = Visibility.Collapsed; return; }
        DetailEmpty.Visibility = Visibility.Collapsed; DetailContent.Visibility = Visibility.Visible;
        DetailInitials.Text = r.RequestId[^3..]; DetailReqId.Text = r.RequestId;
        DetailPriority.Text = r.Priority; DetailPriority.Foreground = C(r.PriorityColor);
        DetailStatus.Text = r.Status; DetailStatus.Foreground = C(r.StatusColor);
        DetailRoomMeta.Text = $"Phòng {r.RoomNumber} · {r.RequestType}"; DetailCreated.Text = r.CreatedAt;
        ShowTab(int.TryParse((TabContent.Tag as string) ?? "0", out var t) ? t : 0);
    }

    private void ShowTab(int tab) { TabContent.Tag = tab.ToString(); TabContent.Children.Clear(); if (RequestGrid.SelectedItem is not HskRequest r) return; switch (tab) { case 0: ShowContent(r); break; case 1: ShowChat(r); break; case 2: ShowHistory(r); break; case 3: ShowRoom(r); break; } }

    private void ShowContent(HskRequest r)
    {
        TabContent.Children.Add(S("Yêu cầu", R("Loại", r.RequestType), R("Nội dung", r.Content), R("Ưu tiên", r.Priority), R("Hạn xử lý", r.Deadline), R("Người phụ trách", r.Assignee), R("Trạng thái", r.Status)));
        TabContent.Children.Add(S("Ghi chú", R("Lễ tân", r.Content.Length > 60 ? "Đã trao đổi với khách." : "—")));
    }

    private void ShowChat(HskRequest r)
    {
        TabContent.Children.Add(S("Trao đổi", R("Dữ liệu hội thoại", "Chưa có lịch sử trao đổi thật trong API Housekeeping"), R("Người phụ trách", r.Assignee), R("Trạng thái", r.Status)));
    }

    private void ShowHistory(HskRequest r)
    {
        TabContent.Children.Add(S("Lịch sử", R("Thời điểm tạo", r.CreatedAt), R("Trạng thái hiện tại", r.Status), R("Chi tiết lịch sử", "Chưa có API audit trail Housekeeping để hiển thị các mốc thật")));
    }

    private void ShowRoom(HskRequest r)
    {
        TabContent.Children.Add(S("Phòng", R("Số phòng", r.RoomNumber), R("Hạng phòng", "Chưa có dữ liệu room master trong response tác vụ"), R("Tầng", "Chưa có dữ liệu"), R("Trạng thái NB", r.Status)));
        TabContent.Children.Add(S("Khách", R("Tên", "Chưa có dữ liệu khách trong response tác vụ"), R("VIP", "Chưa có dữ liệu")));
    }

    // Helpers
    private static SolidColorBrush C(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static SolidColorBrush Cb(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static TextBlock T11(string text, string color, bool bold) => new() { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(0, 1, 0, 0) };
    private static TextBlock T12(string text, string color, bool top) => new() { Text = text, FontSize = 12, FontWeight = top ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), TextWrapping = TextWrapping.Wrap, Margin = top ? new Thickness(0) : new Thickness(0, 2, 0, 0) };
    private static TextBlock T10(string text, string color, bool right) => new() { Text = text, FontSize = 10, Foreground = C(color), HorizontalAlignment = right ? HorizontalAlignment.Right : HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0) };
    private static FrameworkElement S(string title, params FrameworkElement[] children) { var sp = new StackPanel(); foreach (var c in children) sp.Children.Add(c); var h = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#0F172A"), Margin = new Thickness(0, 0, 0, 6) }; var i = new StackPanel(); i.Children.Add(h); i.Children.Add(sp); return new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8), BorderBrush = Cb("#F1F5F9"), BorderThickness = new Thickness(1), Child = i }; }
    private static TextBlock R(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.Inlines.Add(new System.Windows.Documents.Run(l + ": ") { Foreground = C("#94A3B8") }); tb.Inlines.Add(new System.Windows.Documents.Run(v) { FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }); return tb; }

    private void OnTabClick(object s, RoutedEventArgs e) { if (s is Button b && b.Tag is string t && int.TryParse(t, out var tab)) ShowTab(tab); }
    private void OnFilterChanged(object s, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnQuickFilterChanged(object s, RoutedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = ""; FloorFilter.SelectedIndex = 0; TypeFilter.SelectedIndex = 0; PriorityFilter.SelectedIndex = 0; StatusFilter.SelectedIndex = 0; ChkUrgent.IsChecked = false; ChkOverdue.IsChecked = false; ApplyFilters(); }
    private async void OnRefresh(object s, RoutedEventArgs e) => await LoadPersistedTasksAsync();
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_currentPage > 0) { _currentPage--; ApplyFilters(); } }
    private void OnNextPage(object s, RoutedEventArgs e) { _currentPage++; ApplyFilters(); }
    private async void OnCreateRequest(object s, RoutedEventArgs e)
    {
        if (_boardService is null || _workflowService is null || _executionContext?.Current is not { } context)
        {
            Msg("Chưa cấu hình được Housekeeping service và property context dữ liệu thật");
            return;
        }

        IReadOnlyList<HkRoomSnapshot> rooms;
        try
        {
            rooms = await _boardService.GetRoomSnapshotAsync(context.Property.PropertyId.Value, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Msg("Không tải được danh sách phòng thật: " + ex.Message);
            return;
        }

        if (rooms.Count == 0)
        {
            Msg("Chưa có phòng thật để tạo yêu cầu Housekeeping.");
            return;
        }

        var dialog = new Window
        {
            Title = "Tạo yêu cầu nhà buồng",
            Width = 440,
            Height = 330,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        var roomBox = new ComboBox { ItemsSource = rooms, DisplayMemberPath = nameof(HkRoomSnapshot.RoomNumber), SelectedIndex = 0, Margin = new Thickness(0, 4, 0, 10) };
        var typeBox = new ComboBox { ItemsSource = Enum.GetValues<HousekeepingTaskType>(), SelectedIndex = 0, Margin = new Thickness(0, 4, 0, 10) };
        var priorityBox = new ComboBox { ItemsSource = Enum.GetValues<RoomPriority>(), SelectedIndex = 0, Margin = new Thickness(0, 4, 0, 14) };
        var save = new Button { Content = "Tạo yêu cầu", IsDefault = true, Padding = new Thickness(16, 6, 16, 6), HorizontalAlignment = HorizontalAlignment.Right };
        save.Click += (_, _) =>
        {
            if (roomBox.SelectedItem is null || typeBox.SelectedItem is null || priorityBox.SelectedItem is null)
                return;
            dialog.DialogResult = true;
            dialog.Close();
        };

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = "Phòng", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(roomBox);
        panel.Children.Add(new TextBlock { Text = "Loại yêu cầu", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(typeBox);
        panel.Children.Add(new TextBlock { Text = "Ưu tiên", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(priorityBox);
        panel.Children.Add(save);
        dialog.Content = panel;

        if (dialog.ShowDialog() != true
            || roomBox.SelectedItem is not HkRoomSnapshot room
            || typeBox.SelectedItem is not HousekeepingTaskType taskType
            || priorityBox.SelectedItem is not RoomPriority priority)
            return;

        var result = await _workflowService.CreateTaskAsync(
            new HkTaskCreateRequest(context.Property.PropertyId.Value, room.RoomId, taskType, priority),
            CancellationToken.None);
        if (result.IsFailure)
        {
            Msg(result.Error?.Message ?? "Không tạo được yêu cầu Housekeeping thật");
            return;
        }

        await LoadPersistedTasksAsync();
        Msg("Đã tạo yêu cầu Housekeeping trong CSDL.");
    }
    private async void OnUpdateProgress(object s, RoutedEventArgs e)
    {
        if (RequestGrid.SelectedItem is not HskRequest request)
        {
            Msg("Vui lòng chọn yêu cầu");
            return;
        }

        var action = request.TaskStatus switch
        {
            HousekeepingTaskStatus.Assigned => HkTaskAction.Accept,
            HousekeepingTaskStatus.Accepted => HkTaskAction.StartCleaning,
            HousekeepingTaskStatus.Paused => HkTaskAction.ResumeCleaning,
            HousekeepingTaskStatus.WaitingInspection => HkTaskAction.ApproveInspection,
            _ => (HkTaskAction?)null
        };
        if (action is null)
        {
            Msg(request.TaskStatus == HousekeepingTaskStatus.Cleaning
                ? "Yêu cầu đang được xử lý; dùng Hoàn thành khi đã dọn xong"
                : "Không có bước tiến độ hợp lệ");
            return;
        }

        await ExecuteTaskActionAsync(request, action.Value);
    }

    private async void OnAssign(object s, RoutedEventArgs e)
    {
        if (RequestGrid.SelectedItem is not HskRequest request)
        {
            Msg("Vui lòng chọn yêu cầu Housekeeping.");
            return;
        }

        if (_workflowService is null)
        {
            Msg("Chưa cấu hình workflow Housekeeping thật.");
            return;
        }

        IReadOnlyList<HkAssigneeDto> staff;
        try
        {
            staff = await _workflowService.GetActiveAssigneesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Msg("Không tải được danh sách nhân sự thật: " + ex.Message);
            return;
        }

        if (staff.Count == 0)
        {
            Msg("Chưa có nhân sự Housekeeping đang hoạt động để phân công.");
            return;
        }

        var dialog = new Window
        {
            Title = $"Phân công {request.RequestId}",
            Width = 460,
            Height = 220,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        var staffBox = new ComboBox
        {
            ItemsSource = staff,
            DisplayMemberPath = nameof(HkAssigneeDto.DisplayName),
            SelectedIndex = Math.Max(0, staff.ToList().FindIndex(person => person.UserId == request.AssignedHousekeeperId)),
            Margin = new Thickness(0, 6, 0, 16)
        };
        var save = new Button
        {
            Content = "Phân công dữ liệu thật",
            IsDefault = true,
            Padding = new Thickness(16, 6, 16, 6),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        save.Click += (_, _) =>
        {
            if (staffBox.SelectedItem is HkAssigneeDto)
            {
                dialog.DialogResult = true;
                dialog.Close();
            }
        };
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = $"Yêu cầu {request.RequestId} · phòng {request.RoomNumber}", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Nhân viên Housekeeping", Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(staffBox);
        panel.Children.Add(save);
        dialog.Content = panel;

        if (dialog.ShowDialog() != true || staffBox.SelectedItem is not HkAssigneeDto selected)
            return;

        var assignmentAction = request.AssignedHousekeeperId.HasValue
            ? HkTaskAction.Reassign
            : HkTaskAction.Assign;
        await ExecuteTaskActionAsync(request, assignmentAction, selected.UserId);
    }

    private async void OnComplete(object s, RoutedEventArgs e)
    {
        if (RequestGrid.SelectedItem is not HskRequest request)
        {
            Msg("Vui lòng chọn yêu cầu");
            return;
        }

        var action = request.TaskStatus switch
        {
            HousekeepingTaskStatus.Cleaning or HousekeepingTaskStatus.Paused => HkTaskAction.CompleteCleaning,
            HousekeepingTaskStatus.WaitingInspection => HkTaskAction.ApproveInspection,
            _ => (HkTaskAction?)null
        };
        if (action is null)
        {
            Msg("Yêu cầu chưa ở trạng thái có thể hoàn thành");
            return;
        }

        await ExecuteTaskActionAsync(request, action.Value);
    }

    private async void OnCancelRequest(object s, RoutedEventArgs e)
    {
        if (RequestGrid.SelectedItem is HskRequest request)
            await ExecuteTaskActionAsync(request, HkTaskAction.Cancel);
        else
            Msg("Vui lòng chọn yêu cầu");
    }

    private void OnMoreActions(object s, RoutedEventArgs e) => Msg("Thao tác khác");
    private async Task ExecuteTaskActionAsync(HskRequest request, HkTaskAction action, Guid? assignedUserId = null)
    {
        if (_workflowService is null || _executionContext?.Current is not { } context)
        {
            Msg("Chưa cấu hình workflow Housekeeping và property context dữ liệu thật");
            return;
        }

        try
        {
            var result = await _workflowService.ExecuteTaskAsync(
                new HkTaskActionRequest(
                    context.Property.PropertyId.Value,
                    request.TaskId,
                    action,
                    assignedUserId ?? request.AssignedHousekeeperId),
                CancellationToken.None);
            if (result.IsFailure)
            {
                Msg(result.Error?.Message ?? "Không thực hiện được thao tác trên dữ liệu thật");
                return;
            }

            await LoadPersistedTasksAsync();
        }
        catch (Exception ex)
        {
            Msg("Không thể cập nhật Housekeeping thật: " + ex.Message);
        }
    }

    private void Msg(string a)
    {
        MessageBox.Show(a, "Nhắn nhà buồng", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record HskRequest(
    string Priority,
    string PriorityBadge,
    string PriorityColor,
    string RequestId,
    string RoomNumber,
    string RequestType,
    string Content,
    string Creator,
    string Assignee,
    string CreatedAt,
    string Deadline,
    string Status,
    string StatusBadge,
    string StatusColor,
    string Alert)
{
    public Guid TaskId { get; init; }
    public Guid? AssignedHousekeeperId { get; init; }
    public HousekeepingTaskStatus TaskStatus { get; init; }
}
