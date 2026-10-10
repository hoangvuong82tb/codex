using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Incident;
using ASMILE.BO.App.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public partial class IncidentLogWindow : Window
{
    private readonly IServiceScope? _scope;
    private readonly List<IlIncident> _incidents = [];
    private readonly IIncidentQueryService? _query;
    private readonly IIncidentCommandService? _commands;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private IlIncident? _selected;
    private int _page;
    private int _pageSize = 30;
    private int _inspectorTab;
    private string _timelineFilter = "All";
    private DateOnly _businessDate = DateOnly.FromDateTime(DateTime.UtcNow);
    private string? _dataError;

    public IncidentLogWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _scope = rootServices?.CreateScope();
        var services = _scope?.ServiceProvider;
        _query = services?.GetService<IIncidentQueryService>();
        _commands = services?.GetService<IIncidentCommandService>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        InitFilters();
        BuildKpi();
        BuildTimeline();
        InitInspectorTabs();
        RefreshPage(true);
        FooterText.Text = "Đang tải nhật ký sự cố từ database thật...";
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _scope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        if (_contextAccessor?.Current is not { } context || _query is null)
        {
            _dataError = "Chưa cấu hình context hoặc Incident query service.";
            _incidents.Clear();
            UpdateView();
            return;
        }

        try
        {
            _businessDate = context.BusinessDate.BusinessDate;
            var rows = await _query.SearchAsync(new IncidentSearchRequest(context.Property.PropertyId.Value), CancellationToken.None);
            _incidents.Clear();
            _incidents.AddRange(rows.Select(Map));
            _selected = _selected is null ? null : _incidents.FirstOrDefault(x => x.Id == _selected.Id);
            _dataError = null;
        }
        catch (Exception ex)
        {
            _incidents.Clear();
            _selected = null;
            _dataError = $"Không tải được nhật ký sự cố thật: {ex.Message}";
        }
        UpdateView();
    }

    private void UpdateView()
    {
        BuildKpi();
        BuildTimeline();
        InitInspectorTabs();
        RefreshPage(true);
        FooterText.Text = _dataError ?? $"Đã tải {_incidents.Count:N0} sự cố từ database thật.";
    }

    private void InitFilters()
    {
        CatFilter.ItemsSource = Categories;
        CatFilter.SelectedIndex = 0;
        PriFilter.ItemsSource = new[] { "Tất cả", "Low", "Medium", "High", "Critical" };
        PriFilter.SelectedIndex = 0;
        StFilter.ItemsSource = new[] { "Tất cả", "Open", "Assigned", "Investigating", "Pending", "Resolved", "Closed", "Cancelled" };
        StFilter.SelectedIndex = 0;
        PageSizeBox.ItemsSource = new[] { "30 / trang", "50 / trang", "100 / trang" };
        PageSizeBox.SelectedIndex = 0;
    }

    private void BuildKpi()
    {
        KpiStrip.Children.Clear();
        var today = _incidents.Where(x => x.Time.Date == _businessDate.ToDateTime(TimeOnly.MinValue).Date).ToList();
        var kpis = new (string Label, int Value, string Color)[]
        {
            ("Hôm nay", today.Count, "#2F6BFF"),
            ("Đang mở", today.Count(x => x.Status is "Open" or "Assigned" or "Investigating"), "#F59E0B"),
            ("Đã xử lý", today.Count(x => x.Status == "Resolved"), "#16A34A"),
            ("Critical", today.Count(x => x.Priority == "Critical"), "#DC2626"),
            ("An ninh", today.Count(x => x.Category == "Security"), "#DC2626"),
            ("Bảo trì", today.Count(x => x.Category is "Door Lock" or "Power Failure" or "Elevator" or "Air Conditioner"), "#8B5CF6"),
            ("Khách", today.Count(x => x.Category == "Guest Incident"), "#F59E0B")
        };
        foreach (var item in kpis) KpiStrip.Children.Add(Kpi(item.Label, item.Value.ToString(), item.Color));
    }

    private static FrameworkElement Kpi(string label, string value, string color) => new Border
    {
        Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8),
        Margin = new Thickness(0, 0, 8, 0),
        Child = new StackPanel { Children =
        {
            new TextBlock { Text = value, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C(color) },
            new TextBlock { Text = label, FontSize = 10, Foreground = C("#94A3B8") }
        }}
    };

    private void BuildTimeline()
    {
        TimelinePanel.Children.Clear();
        var ranges = new[] { ("Hôm nay", "Today"), ("Hôm qua", "Yesterday"), ("7 ngày", "7Days"), ("30 ngày", "30Days"), ("Tất cả", "All") };
        foreach (var (label, key) in ranges)
        {
            var active = _timelineFilter == key;
            var border = new Border
            {
                CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3),
                Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"),
                Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569") }
            };
            border.MouseLeftButtonDown += (_, _) => { _timelineFilter = key; BuildTimeline(); RefreshPage(true); };
            TimelinePanel.Children.Add(border);
        }
        TimelinePanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        var filtered = GetFiltered();
        TimelinePanel.Children.Add(new TextBlock { Text = $"Tổng: {filtered.Count:N0} sự cố", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 4) });
        TimelinePanel.Children.Add(new TextBlock { Text = $"Mở: {filtered.Count(x => x.Status is "Open" or "Assigned" or "Investigating")} · Đã xử lý: {filtered.Count(x => x.Status == "Resolved")}", FontSize = 10, Foreground = C("#64748B") });
    }

    private List<IlIncident> GetFiltered()
    {
        IEnumerable<IlIncident> query = _incidents;
        var keyword = SearchBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(x => x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) || x.IncidentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (x.Guest?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) || (x.Room?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true));
        if (CatFilter.SelectedItem is string cat && cat != "Tất cả") query = query.Where(x => x.Category == cat);
        if (PriFilter.SelectedItem is string pri && pri != "Tất cả") query = query.Where(x => x.Priority == pri);
        if (StFilter.SelectedItem is string status && status != "Tất cả") query = query.Where(x => x.Status == status);
        var today = _businessDate.ToDateTime(TimeOnly.MinValue);
        query = _timelineFilter switch
        {
            "Today" => query.Where(x => x.Time.Date == today.Date),
            "Yesterday" => query.Where(x => x.Time.Date == today.AddDays(-1).Date),
            "7Days" => query.Where(x => x.Time >= today.AddDays(-6)),
            "30Days" => query.Where(x => x.Time >= today.AddDays(-29)),
            _ => query
        };
        return query.OrderByDescending(x => x.Time).ThenByDescending(x => PriorityRank(x.Priority)).ToList();
    }

    private void RefreshPage(bool reset)
    {
        if (reset) _page = 0;
        var filtered = GetFiltered();
        var totalPages = Math.Max(1, (filtered.Count + _pageSize - 1) / _pageSize);
        if (_page >= totalPages) _page = totalPages - 1;
        GridPanel.Children.Clear();

        var header = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        var widths = new[] { 115.0, 100.0, 180.0, 105.0, 65.0, 65.0, 110.0, 75.0, 100.0, 95.0, 85.0 };
        var labels = new[] { "Mã SC", "Thời gian", "Tiêu đề", "Danh mục", "Ưu tiên", "Phòng", "Khách", "BP", "Người báo", "Phân công", "Trạng thái" };
        for (var j = 0; j < widths.Length; j++) { header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[j]) }); AddHdr(header, j, labels[j]); }
        GridPanel.Children.Add(header);

        foreach (var incident in filtered.Skip(_page * _pageSize).Take(_pageSize))
        {
            var active = _selected?.Id == incident.Id;
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1), Background = active ? Cb("#EFF6FF") : incident.Priority == "Critical" ? Cb("#FFF5F5") : Cb("Transparent") };
            foreach (var width in widths) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
            var values = new[] { incident.IncidentNo, incident.Time.ToString("dd/MM HH:mm"), incident.Title, incident.Category, incident.Priority, incident.Room ?? "—", incident.Guest ?? "—", incident.Department ?? "—", incident.Reporter, incident.AssignedTo ?? "—", incident.Status };
            var colors = new[] { "#64748B", "#64748B", "#17365D", CategoryColor(incident.Category), PriorityColor(incident.Priority), "#64748B", "#17365D", "#64748B", "#64748B", "#64748B", StatusColor(incident.Status) };
            for (var j = 0; j < values.Length; j++) AddCell(row, j, values[j], colors[j], j is 2 or 10);
            row.Cursor = System.Windows.Input.Cursors.Hand;
            row.MouseLeftButtonDown += (_, _) => { _selected = incident; RefreshPage(false); BuildInspector(); };
            GridPanel.Children.Add(row);
        }

        PagerSummary.Text = filtered.Count == 0 ? "0 / 0 sự cố" : $"{_page * _pageSize + 1}–{Math.Min((_page + 1) * _pageSize, filtered.Count)} / {filtered.Count:N0} sự cố";
        PrevBtn.IsEnabled = _page > 0;
        NextBtn.IsEnabled = _page < totalPages - 1;
        BuildPageBtns(totalPages);
        FooterText.Text = _dataError ?? $"Tổng: {filtered.Count:N0} · Mở: {filtered.Count(x => x.Status is "Open" or "Assigned" or "Investigating")} · Đã xử lý: {filtered.Count(x => x.Status == "Resolved")} · Critical: {filtered.Count(x => x.Priority == "Critical")}";
    }

    private void BuildPageBtns(int totalPages)
    {
        PageBtns.Children.Clear();
        for (var i = 0; i < Math.Min(totalPages, 10); i++)
        {
            var index = i;
            var button = new Button { Content = (i + 1).ToString(), Style = (Style)FindResource("PagerButtonStyle"), Background = i == _page ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _page ? Cb("#FFFFFF") : Cb("#475569") };
            button.Click += (_, _) => { _page = index; RefreshPage(false); };
            PageBtns.Children.Add(button);
        }
    }

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Khách", "Phòng", "Điều tra", "Timeline", "Lịch sử" };
        for (var i = 0; i < tabs.Length; i++)
        {
            var index = i;
            var active = i == _inspectorTab;
            var border = new Border { CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 1, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("White") : Cb("Transparent"), BorderBrush = active ? Cb("#E2E8F0") : Cb("Transparent"), BorderThickness = new Thickness(1, 1, 1, 0), Child = new TextBlock { Text = tabs[i], FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#2F6BFF") : C("#64748B") } };
            border.MouseLeftButtonDown += (_, _) => { _inspectorTab = index; InitInspectorTabs(); };
            InspectorTabs.Children.Add(border);
        }
        BuildInspector();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_selected is not { } incident)
        {
            InspectorPanel.Children.Add(new TextBlock { Text = "Chọn sự cố để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8") });
            return;
        }

        switch (_inspectorTab)
        {
            case 0:
                InspectorPanel.Children.Add(STitle(incident.Title));
                InspectorPanel.Children.Add(Badge(incident.Priority, PriorityColor(incident.Priority)));
                InspectorPanel.Children.Add(Row("Mã SC", incident.IncidentNo));
                InspectorPanel.Children.Add(Row("Danh mục", incident.Category));
                InspectorPanel.Children.Add(Row("Trạng thái", incident.Status));
                InspectorPanel.Children.Add(Row("Ảnh hưởng", incident.Impact));
                InspectorPanel.Children.Add(Row("Người báo", incident.Reporter));
                InspectorPanel.Children.Add(Row("Mô tả", incident.Description));
                if (incident.Status is "Open" or "Assigned" or "Pending")
                {
                    var start = new Button { Content = "Bắt đầu xử lý", Style = (Style)FindResource("SmallButton"), Margin = new Thickness(0, 10, 0, 0) };
                    start.Click += async (_, _) => await ChangeStatusAsync(new ChangeIncidentStatusRequest(incident.Id, IncidentStatusAction.StartInvestigation, ExpectedRowVersion: incident.RowVersion));
                    InspectorPanel.Children.Add(start);
                }
                if (incident.Status is "Open" or "Assigned" or "Investigating" or "Pending")
                {
                    AddActionButton(incident, IncidentStatusAction.Assign, "Phân công");
                    AddActionButton(incident, IncidentStatusAction.SetPending, "Tạm chờ");
                    AddActionButton(incident, IncidentStatusAction.Cancel, "Hủy sự cố");
                }
                if (incident.Status == "Investigating") AddActionButton(incident, IncidentStatusAction.Resolve, "Đánh dấu đã xử lý");
                if (incident.Status == "Resolved") AddActionButton(incident, IncidentStatusAction.Close, "Đóng sự cố");
                break;
            case 1:
                InspectorPanel.Children.Add(STitle("Khách / đặt phòng"));
                InspectorPanel.Children.Add(Row("Khách", incident.Guest ?? "Chưa liên kết"));
                InspectorPanel.Children.Add(Row("Phòng", incident.Room ?? "Chưa liên kết"));
                InspectorPanel.Children.Add(Row("Reservation", incident.Reservation ?? "Chưa liên kết"));
                InspectorPanel.Children.Add(Row("VIP", incident.IsVip ? "Có" : "Không"));
                break;
            case 2:
                InspectorPanel.Children.Add(STitle("Phòng"));
                InspectorPanel.Children.Add(Row("Tham chiếu phòng", incident.Room ?? "Chưa liên kết"));
                InspectorPanel.Children.Add(Row("Dữ liệu trạng thái phòng", "Chưa có query liên kết trong Incident"));
                break;
            case 3:
                InspectorPanel.Children.Add(STitle("Điều tra"));
                InspectorPanel.Children.Add(Row("Nguyên nhân gốc", incident.RootCause ?? "Chưa có"));
                InspectorPanel.Children.Add(Row("Xử lý tạm thời", incident.TemporaryAction ?? "Chưa có"));
                InspectorPanel.Children.Add(Row("Khắc phục", incident.CorrectiveAction ?? "Chưa có"));
                InspectorPanel.Children.Add(Row("Phòng ngừa", incident.PreventiveAction ?? "Chưa có"));
                break;
            case 4:
                InspectorPanel.Children.Add(STitle("Timeline"));
                InspectorPanel.Children.Add(Row("Xảy ra", incident.Time.ToString("dd/MM/yyyy HH:mm 'UTC'")));
                InspectorPanel.Children.Add(Row("Tạo lúc", incident.CreatedAtUtc.ToString("dd/MM/yyyy HH:mm 'UTC'")));
                if (incident.ResolvedAtUtc is DateTime resolved) InspectorPanel.Children.Add(Row("Xử lý xong", resolved.ToString("dd/MM/yyyy HH:mm 'UTC'")));
                if (incident.ClosedAtUtc is DateTime closed) InspectorPanel.Children.Add(Row("Đóng", closed.ToString("dd/MM/yyyy HH:mm 'UTC'")));
                break;
            case 5:
                InspectorPanel.Children.Add(STitle("Lịch sử"));
                InspectorPanel.Children.Add(Row("Cập nhật cuối", incident.UpdatedAtUtc.ToString("dd/MM/yyyy HH:mm 'UTC'")));
                InspectorPanel.Children.Add(Row("Phân công", incident.AssignedTo ?? "Chưa phân công"));
                InspectorPanel.Children.Add(Row("Audit", "Đã ghi qua FrontOfficePersistenceSession"));
                break;
        }
    }

    private async Task ChangeStatusAsync(ChangeIncidentStatusRequest request)
    {
        if (_commands is null) { MessageBox.Show("Chưa cấu hình Incident command service.", "Nhật ký sự cố", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try { await _commands.ChangeStatusAsync(request, CancellationToken.None); await ReloadAsync(); }
        catch (Exception ex) { MessageBox.Show($"Không cập nhật được sự cố: {ex.Message}", "Nhật ký sự cố", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void AddActionButton(IlIncident incident, IncidentStatusAction action, string label)
    {
        var button = new Button { Content = label, Style = (Style)FindResource("SmallButton"), Margin = new Thickness(0, 4, 0, 0) };
        button.Click += async (_, _) => await RunActionDialogAsync(incident, action);
        InspectorPanel.Children.Add(button);
    }

    private async Task RunActionDialogAsync(IlIncident incident, IncidentStatusAction action)
    {
        var dialog = new IncidentActionWindow(action) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var request = action switch
        {
            IncidentStatusAction.Assign => new ChangeIncidentStatusRequest(incident.Id, action, Assignee: dialog.ValueOne, ExpectedRowVersion: incident.RowVersion),
            IncidentStatusAction.SetPending or IncidentStatusAction.Cancel => new ChangeIncidentStatusRequest(incident.Id, action, Reason: dialog.ValueOne, ExpectedRowVersion: incident.RowVersion),
            IncidentStatusAction.Resolve => new ChangeIncidentStatusRequest(incident.Id, action, RootCause: dialog.ValueOne, CorrectiveAction: dialog.ValueTwo, ExpectedRowVersion: incident.RowVersion),
            IncidentStatusAction.Close => new ChangeIncidentStatusRequest(incident.Id, action, PreventiveAction: dialog.ValueOne, ExpectedRowVersion: incident.RowVersion),
            _ => new ChangeIncidentStatusRequest(incident.Id, action, ExpectedRowVersion: incident.RowVersion)
        };
        await ChangeStatusAsync(request);
    }

    private async void OnNewIncident(object sender, RoutedEventArgs e)
    {
        if (_commands is null) { MessageBox.Show("Chưa cấu hình Incident command service.", "Nhật ký sự cố", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var dialog = new CreateIncidentWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Request is not { } request) return;
        try { await _commands.CreateAsync(request, CancellationToken.None); await ReloadAsync(); }
        catch (Exception ex) { MessageBox.Show($"Không tạo được sự cố: {ex.Message}", "Nhật ký sự cố", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void OnQuickToday(object sender, RoutedEventArgs e) { _timelineFilter = "Today"; BuildTimeline(); RefreshPage(true); }
    private void OnQuickCritical(object sender, RoutedEventArgs e) { PriFilter.SelectedIndex = 4; RefreshPage(true); }
    private void OnQuickOpen(object sender, RoutedEventArgs e) { StFilter.SelectedIndex = 1; RefreshPage(true); }
    private void OnApplyFilter(object sender, RoutedEventArgs e) { BuildTimeline(); RefreshPage(true); }
    private void OnClearFilter(object sender, RoutedEventArgs e) { SearchBox.Text = string.Empty; CatFilter.SelectedIndex = 0; PriFilter.SelectedIndex = 0; StFilter.SelectedIndex = 0; _timelineFilter = "All"; BuildTimeline(); RefreshPage(true); }
    private void OnPrevPage(object sender, RoutedEventArgs e) { if (_page > 0) { _page--; RefreshPage(false); } }
    private void OnNextPage(object sender, RoutedEventArgs e) { _page++; RefreshPage(false); }
    private void OnPageSizeChanged(object sender, SelectionChangedEventArgs e) { if (PageSizeBox is null) return; _pageSize = PageSizeBox.SelectedIndex switch { 1 => 50, 2 => 100, _ => 30 }; RefreshPage(true); }
    private async void OnRefresh(object sender, RoutedEventArgs e) { _selected = null; await ReloadAsync(); }
    private void OnExport(object sender, RoutedEventArgs e)
    {
        var rows = GetFiltered();
        if (rows.Count == 0)
        {
            MessageBox.Show("Không có sự cố thật phù hợp bộ lọc để xuất.", "Nhật ký sự cố", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Xuất nhật ký sự cố",
            Filter = "CSV UTF-8 (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            FileName = $"nhat-ky-su-co-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            AddExtension = true,
            DefaultExt = ".csv",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Thời gian,Mã sự cố,Tiêu đề,Loại,Ưu tiên,Trạng thái,Phòng,Khách,Đặt phòng,Bộ phận,Người báo cáo,Người phụ trách,VIP,Ảnh hưởng,Mô tả,Nguyên nhân,Ghi nhận tạm thời,Khắc phục,Phòng ngừa");
            foreach (var row in rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(row.Time.ToString("yyyy-MM-dd HH:mm:ss")), Csv(row.IncidentNo), Csv(row.Title), Csv(row.Category),
                    Csv(row.Priority), Csv(row.Status), Csv(row.Room), Csv(row.Guest), Csv(row.Reservation),
                    Csv(row.Department), Csv(row.Reporter), Csv(row.AssignedTo), Csv(row.IsVip ? "Có" : "Không"),
                    Csv(row.Impact), Csv(row.Description), Csv(row.RootCause), Csv(row.TemporaryAction),
                    Csv(row.CorrectiveAction), Csv(row.PreventiveAction)
                }));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            FooterText.Text = $"Đã xuất {rows.Count:N0} sự cố thật: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xuất nhật ký sự cố thật: " + ex.Message, "Nhật ký sự cố", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnPrint(object sender, RoutedEventArgs e)
    {
        var rows = GetFiltered();
        if (rows.Count == 0)
        {
            MessageBox.Show("Không có sự cố thật phù hợp bộ lọc để in.", "Nhật ký sự cố", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var content = new StringBuilder()
            .AppendLine("BÁO CÁO NHẬT KÝ SỰ CỐ")
            .AppendLine($"Thời điểm xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine($"Số bản ghi: {rows.Count:N0}")
            .AppendLine(new string('-', 120));
        foreach (var row in rows)
        {
            content.AppendLine($"{row.Time:dd/MM/yyyy HH:mm} | {row.IncidentNo} | {row.Priority} | {row.Status} | {row.Category} | {row.Title}");
            content.AppendLine($"Phòng: {row.Room ?? "-"} | Khách: {row.Guest ?? "-"} | Người phụ trách: {row.AssignedTo ?? "-"}");
            content.AppendLine($"Mô tả: {row.Description}");
            content.AppendLine();
        }

        new PrintPreviewDialog("Báo cáo nhật ký sự cố", content.ToString()).ShowDialog();
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private static IlIncident Map(IncidentDto x) => new(x.IncidentId, x.IncidentNumber, x.OccurredAtUtc, x.Title, x.Category, x.Priority.ToString(), x.RoomReference, x.GuestReference, x.ReservationReference, x.Department, x.ReporterName, x.AssignedTo, x.Status.ToString(), x.Impact, x.Description, x.RootCause, x.TemporaryAction, x.CorrectiveAction, x.PreventiveAction, x.IsVip, x.CreatedAtUtc, x.UpdatedAtUtc, x.ResolvedAtUtc, x.ClosedAtUtc, x.RowVersion);
    private static Border Badge(string text, string color) => new() { Background = Cb(color), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 0, 7), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = text, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("#FFFFFF") } };
    private static TextBlock STitle(string text) => new() { Text = text, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 6) };
    private static TextBlock Row(string label, string value) { var text = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) }; text.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = C("#94A3B8") }); text.Inlines.Add(new System.Windows.Documents.Run(value) { Foreground = C("#17365D"), FontWeight = FontWeights.SemiBold }); return text; }
    private static void AddHdr(Grid grid, int column, string text) { var block = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3) }; Grid.SetColumn(block, column); grid.Children.Add(block); }
    private static void AddCell(Grid grid, int column, string text, string color, bool bold) { var block = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2), TextTrimming = TextTrimming.CharacterEllipsis }; Grid.SetColumn(block, column); grid.Children.Add(block); }
    private static int PriorityRank(string value) => value switch { "Critical" => 4, "High" => 3, "Medium" => 2, _ => 1 };
    private static string PriorityColor(string value) => value switch { "Critical" => "#DC2626", "High" => "#F59E0B", "Medium" => "#2F6BFF", _ => "#16A34A" };
    private static string StatusColor(string value) => value switch { "Open" => "#DC2626", "Assigned" or "Investigating" => "#2F6BFF", "Pending" => "#F59E0B", "Resolved" => "#16A34A", "Closed" => "#94A3B8", _ => "#64748B" };
    private static string CategoryColor(string value) => value is "Fire" or "Security" or "Theft" or "Violence" or "Medical" ? "#DC2626" : value is "Power Failure" or "Water Leak" or "Elevator" ? "#F59E0B" : "#64748B";
    private static SolidColorBrush C(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static SolidColorBrush Cb(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static readonly string[] Categories = { "Tất cả", "Guest Incident", "Security", "Fire", "Medical", "Power Failure", "Water Leak", "Door Lock", "Network", "POS", "Elevator", "Air Conditioner", "Housekeeping", "Cashiering", "Lost Property", "Theft", "Other" };
}

internal sealed record IlIncident(
    Guid Id, string IncidentNo, DateTime Time, string Title, string Category, string Priority, string? Room,
    string? Guest, string? Reservation, string? Department, string Reporter, string? AssignedTo, string Status,
    string Impact, string Description, string? RootCause, string? TemporaryAction, string? CorrectiveAction,
    string? PreventiveAction, bool IsVip, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ResolvedAtUtc,
    DateTime? ClosedAtUtc, byte[]? RowVersion);
