using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Incident;
using ASMILE.BO.Application.FrontOffice.ShiftLog;
using ASMILE.BO.Core.FrontOffice.Cashier;
using ASMILE.BO.Core.FrontOffice.Incident;
using ASMILE.BO.App.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public partial class ShiftLogWindow : Window
{
    private readonly IServiceScope? _scope;
    private readonly List<SlLog> _logs;
    private readonly ICashShiftService? _cashShiftService;
    private readonly IIncidentQueryService? _incidentQueryService;
    private readonly IShiftLogEntryQueryService? _shiftLogQueryService;
    private readonly IShiftLogEntryCommandService? _shiftLogCommandService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private SlLog? _selectedLog;
    private int _logPage, _logPageSize = 30;
    private int _inspectorTab;
    private string _shiftFilter = "All";

    public ShiftLogWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _logs = new List<SlLog>();
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _scope = rootServices?.CreateScope();
        var services = _scope?.ServiceProvider;
        _cashShiftService = services?.GetService<ICashShiftService>();
        _incidentQueryService = services?.GetService<IIncidentQueryService>();
        _shiftLogQueryService = services?.GetService<IShiftLogEntryQueryService>();
        _shiftLogCommandService = services?.GetService<IShiftLogEntryCommandService>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        InitFilters();
        BuildKpiStrip();
        BuildTimeline();
        InitInspectorTabs();
        RefreshLogPage(true);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _scope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_executionContext?.Current is not { } context)
        {
            FooterText.Text = "Chưa cấu hình được property context dữ liệu thật.";
            return;
        }

        try
        {
            var (shifts, incidents, entries) = await LoadRealLogsAsync(context.Property.PropertyId.Value);
            ReplaceLogs(shifts, incidents, entries);
            RebuildView();
            FooterText.Text = $"Đã tải {shifts.Count:N0} ca tiền mặt, {incidents.Count:N0} sự cố và {entries.Count:N0} nhật ký ca từ dữ liệu thật.";
        }
        catch (Exception ex)
        {
            _logs.Clear();
            RebuildView();
            FooterText.Text = "Không tải được nhật ký ca thật: " + ex.Message;
        }
    }

    private async Task<(IReadOnlyList<CashShiftDto> Shifts, IReadOnlyList<IncidentDto> Incidents, IReadOnlyList<ShiftLogEntryDto> Entries)> LoadRealLogsAsync(Guid propertyId)
    {
        var fromUtc = DateTime.UtcNow.AddDays(-31);
        var toUtc = DateTime.UtcNow.AddDays(1);
        var shifts = _cashShiftService is null
            ? Array.Empty<CashShiftDto>()
            : await _cashShiftService.GetShiftHistoryAsync(propertyId, fromUtc, toUtc, CancellationToken.None);
        var incidents = _incidentQueryService is null
            ? Array.Empty<IncidentDto>()
            : await _incidentQueryService.SearchAsync(
                new IncidentSearchRequest(propertyId, FromUtc: fromUtc, ToUtc: toUtc),
                CancellationToken.None);
        var entries = _shiftLogQueryService is null
            ? Array.Empty<ShiftLogEntryDto>()
            : await _shiftLogQueryService.SearchAsync(
                new ShiftLogEntrySearchRequest(propertyId, FromUtc: fromUtc, ToUtc: toUtc),
                CancellationToken.None);
        return (shifts, incidents, entries);
    }

    private void ReplaceLogs(IReadOnlyList<CashShiftDto> shifts, IReadOnlyList<IncidentDto> incidents, IReadOnlyList<ShiftLogEntryDto> entries)
    {
        _logs.Clear();
        var index = 0;
        foreach (var shift in shifts)
        {
            var localTime = (shift.ClosedAtUtc ?? shift.OpenedAtUtc).ToLocalTime();
            var isClosed = shift.Status == CashierShiftStatus.Closed;
            var variance = shift.Variance ?? 0m;
            var priority = Math.Abs(variance) > 0.01m ? "High" : "Low";
            var status = isClosed ? "Completed" : "Open";
            var completedTasks = isClosed ? shift.Movements.Count : 0;
            _logs.Add(new SlLog(
                ++index,
                localTime,
                ShiftName(localTime),
                isClosed ? $"Đóng ca {shift.ShiftNumber}" : $"Mở ca {shift.ShiftNumber}",
                "Cashiering",
                priority,
                "—",
                string.IsNullOrWhiteSpace(shift.CashierName) ? shift.CashierId.ToString() : shift.CashierName,
                null,
                shift.CashierName,
                shift.CashierName,
                status,
                $"Mở ca {shift.OpeningCash:N0}; kỳ vọng {shift.ExpectedCash:N0}; " +
                $"thực tế {(shift.ActualCash?.ToString("N0") ?? "—")}; chênh lệch {variance:N0}.",
                null,
                false,
                false,
                shift.Movements.Count,
                completedTasks));
        }

        foreach (var incident in incidents)
        {
            var localTime = incident.OccurredAtUtc.ToLocalTime();
            var category = incident.Category;
            var title = string.IsNullOrWhiteSpace(incident.IncidentNumber)
                ? incident.Title
                : $"{incident.IncidentNumber} · {incident.Title}";
            var hasComplaint = category.Contains("Complaint", StringComparison.OrdinalIgnoreCase)
                || incident.Title.Contains("Complaint", StringComparison.OrdinalIgnoreCase)
                || incident.Title.Contains("Khiếu nại", StringComparison.OrdinalIgnoreCase);
            _logs.Add(new SlLog(
                ++index,
                localTime,
                ShiftName(localTime),
                title,
                category,
                incident.Priority.ToString(),
                incident.RoomReference ?? "—",
                incident.GuestReference ?? "—",
                incident.ReservationReference,
                incident.AssignedTo ?? incident.Department ?? "—",
                string.IsNullOrWhiteSpace(incident.ReporterName) ? "—" : incident.ReporterName,
                incident.Status.ToString(),
                incident.Description,
                null,
                incident.IsVip,
                hasComplaint,
                0,
                0));
        }

        foreach (var entry in entries)
        {
            var localTime = entry.OccurredAtUtc.ToLocalTime();
            var hasComplaint = entry.Category.Contains("Complaint", StringComparison.OrdinalIgnoreCase)
                || entry.Title.Contains("Complaint", StringComparison.OrdinalIgnoreCase)
                || entry.Title.Contains("Khiếu nại", StringComparison.OrdinalIgnoreCase);
            _logs.Add(new SlLog(
                ++index,
                localTime,
                string.IsNullOrWhiteSpace(entry.ShiftName) ? ShiftName(localTime) : entry.ShiftName,
                entry.Title,
                entry.Category,
                entry.Priority.ToString(),
                entry.RoomReference ?? "—",
                entry.GuestReference ?? "—",
                entry.ReservationReference,
                entry.AssignedTo ?? "—",
                "—",
                entry.Status.ToString(),
                entry.Description,
                null,
                entry.IsVip,
                hasComplaint,
                0,
                0));
        }

        _logs.Sort((left, right) => right.Time.CompareTo(left.Time));
    }

    private static string ShiftName(DateTime localTime) => localTime.Hour < 12 ? "Ca sáng" : localTime.Hour < 18 ? "Ca chiều" : "Ca đêm";
    private static bool IsOpenStatus(string status) => status is "Open" or "Assigned" or "Investigating" or "Pending" or "In Progress";
    private static bool IsCompletedStatus(string status) => status is "Completed" or "Resolved" or "Closed";

    private void RebuildView()
    {
        _selectedLog = null;
        BuildKpiStrip();
        BuildTimeline();
        InitInspectorTabs();
        RefreshLogPage(true);
    }

    private void InitFilters()
    {
        ShiftFilterCombo.ItemsSource = new[] { "Tất cả", "Ca sáng", "Ca chiều", "Ca đêm" };
        ShiftFilterCombo.SelectedIndex = 0;
        PriorityFilterCombo.ItemsSource = new[] { "Tất cả", "Low", "Medium", "High", "Critical" };
        PriorityFilterCombo.SelectedIndex = 0;
        StatusFilterCombo.ItemsSource = new[] { "Tất cả", "Open", "Assigned", "Investigating", "Pending", "In Progress", "Resolved", "Closed", "Completed", "Cancelled" };
        StatusFilterCombo.SelectedIndex = 0;
        CategoryFilterCombo.ItemsSource = new[]
        {
            "Tất cả", "VIP", "Complaint", "Cashiering", "Housekeeping", "Maintenance", "Security", "Lost & Found",
            "Night Audit", "Reservation", "Check-in", "Checkout", "Guest Incident", "Fire", "Medical", "Power Failure",
            "Water Leak", "Door Lock", "Network", "POS", "Printer", "Elevator", "Air Conditioner",
            "Lost Property", "Theft", "Violence", "Noise", "Parking", "Other"
        };
        CategoryFilterCombo.SelectedIndex = 0;
        LogPageSizeBox.ItemsSource = new[] { "30 / trang", "50 / trang", "100 / trang" };
        LogPageSizeBox.SelectedIndex = 0;
    }

    // ═══ KPI ═══

    private void BuildKpiStrip()
    {
        KpiStrip.Children.Clear();
        var today = _logs.Where(l => l.Time.Date == DateTime.Today).ToList();
        var kpis = new (string, int, string)[] {
            ("Nhật ký hôm nay", today.Count, "#2F6BFF"),
            ("Đang mở", today.Count(l => IsOpenStatus(l.Status)), "#F59E0B"),
            ("Hoàn thành", today.Count(l => IsCompletedStatus(l.Status)), "#16A34A"),
            ("VIP", today.Count(l => l.IsVip), "#F59E0B"),
            ("Khiếu nại", today.Count(l => l.HasComplaint), "#DC2626"),
            ("Bảo trì", today.Count(l => l.Category == "Maintenance"), "#8B5CF6"),
            ("Critical", today.Count(l => l.Priority == "Critical"), "#DC2626")
        };
        foreach (var (label, value, color) in kpis)
            KpiStrip.Children.Add(Kpi(label, $"{value}", color));
    }

    private static FrameworkElement Kpi(string l, string v, string color) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };

    // ═══ TIMELINE ═══

    private void BuildTimeline()
    {
        TimelinePanel.Children.Clear();
        var ranges = new[] { ("Hôm nay", "Today"), ("Ca sáng", "Morning"), ("Ca chiều", "Afternoon"), ("Ca đêm", "Night"), ("Hôm qua", "Yesterday"), ("Tuần này", "Week") };
        foreach (var (label, key) in ranges)
        {
            var active = _shiftFilter == key;
            var b = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569") } };
            b.MouseLeftButtonDown += (_, _) => { _shiftFilter = key; BuildTimeline(); RefreshLogPage(true); };
            TimelinePanel.Children.Add(b);
        }

        TimelinePanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        var filtered = GetFilteredLogs();
        TimelinePanel.Children.Add(new TextBlock { Text = $"Tổng: {filtered.Count} nhật ký", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 4) });
        TimelinePanel.Children.Add(new TextBlock { Text = $"Mở: {filtered.Count(l => IsOpenStatus(l.Status))} · Hoàn thành: {filtered.Count(l => IsCompletedStatus(l.Status))}", FontSize = 10, Foreground = C("#64748B"), Margin = new Thickness(0, 0, 0, 4) });
        TimelinePanel.Children.Add(new TextBlock { Text = $"Critical: {filtered.Count(l => l.Priority == "Critical")} · VIP: {filtered.Count(l => l.IsVip)}", FontSize = 10, Foreground = C("#DC2626") });
    }

    // ═══ LOG GRID ═══

    private List<SlLog> GetFilteredLogs()
    {
        var l = _logs.AsEnumerable();
        var kw = LogSearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) l = l.Where(x => x.Title.ToLower().Contains(kw) || x.Guest.ToLower().Contains(kw) || x.Room.Contains(kw) || (x.Reservation?.ToLower().Contains(kw) ?? false));
        var shift = ShiftFilterCombo.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(shift) && shift != "Tất cả") l = l.Where(x => x.Shift == shift);
        var pri = PriorityFilterCombo.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(pri) && pri != "Tất cả") l = l.Where(x => x.Priority == pri);
        var status = StatusFilterCombo.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(status) && status != "Tất cả") l = l.Where(x => x.Status == status);
        var cat = CategoryFilterCombo.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(cat) && cat != "Tất cả") l = l.Where(x => x.Category == cat);
        if (_shiftFilter == "Today") l = l.Where(x => x.Time.Date == DateTime.Today);
        else if (_shiftFilter == "Yesterday") l = l.Where(x => x.Time.Date == DateTime.Today.AddDays(-1));
        else if (_shiftFilter == "Morning") l = l.Where(x => x.Time.Date == DateTime.Today && x.Shift == "Ca sáng");
        else if (_shiftFilter == "Afternoon") l = l.Where(x => x.Time.Date == DateTime.Today && x.Shift == "Ca chiều");
        else if (_shiftFilter == "Night") l = l.Where(x => x.Time.Date == DateTime.Today && x.Shift == "Ca đêm");
        return l.ToList();
    }

    private void RefreshLogPage(bool reset)
    {
        if (reset) _logPage = 0;
        var f = GetFilteredLogs();
        var tp = Math.Max(1, (f.Count + _logPageSize - 1) / _logPageSize);
        if (_logPage >= tp) _logPage = tp - 1;

        LogGridPanel.Children.Clear();
        var hdr = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        var cw = new[] { 70.0, 65.0, 160.0, 85.0, 55.0, 45.0, 95.0, 85.0, 80.0, 70.0 };
        var ch = new[] { "Thời gian", "Ca", "Tiêu đề", "Danh mục", "Ưu tiên", "Phòng", "Khách", "Phân công", "Người tạo", "Trạng thái" };
        for (int i = 0; i < cw.Length; i++) { hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[i]) }); AddHdr(hdr, i, ch[i]); }
        LogGridPanel.Children.Add(hdr);

        foreach (var log in f.Skip(_logPage * _logPageSize).Take(_logPageSize))
        {
            var active = _selectedLog?.Id == log.Id;
            var priColor = log.Priority switch { "Critical" => "#DC2626", "High" => "#F59E0B", "Medium" => "#2F6BFF", _ => "#16A34A" };
            var statusColor = log.Status switch { "Open" => "#DC2626", "Pending" => "#F59E0B", "In Progress" => "#2F6BFF", "Completed" => "#16A34A", _ => "#94A3B8" };
            var catColor = log.Category switch { "VIP" => "#F59E0B", "Complaint" => "#DC2626", "Security" => "#DC2626", "Maintenance" => "#8B5CF6", _ => "#64748B" };
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1), Background = active ? Cb("#EFF6FF") : log.Priority == "Critical" ? Cb("#FFF5F5") : Cb("Transparent") };
            for (int i = 0; i < cw.Length; i++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[i]) });
            var vals = new[] { log.Time.ToString("dd/MM HH:mm"), log.Shift, log.Title, log.Category, log.Priority, log.Room, log.Guest, log.AssignedTo, log.CreatedBy, log.Status };
            var colors = new[] { "#64748B", "#64748B", "#17365D", catColor, priColor, "#64748B", "#17365D", "#64748B", "#64748B", statusColor };
            for (int i = 0; i < vals.Length; i++) AddCell(row, i, vals[i], colors[i], i == 2 || i == 9);
            var logId = log.Id; row.Cursor = System.Windows.Input.Cursors.Hand;
            row.MouseLeftButtonDown += (_, _) => { _selectedLog = log; RefreshLogPage(false); BuildInspector(); };
            LogGridPanel.Children.Add(row);
        }

        LogPagerSummary.Text = $"{_logPage * _logPageSize + 1}–{Math.Min((_logPage + 1) * _logPageSize, f.Count)} / {f.Count} nhật ký";
        LogPrevBtn.IsEnabled = _logPage > 0; LogNextBtn.IsEnabled = _logPage < tp - 1;
        BuildLogPageBtns(tp);
        FooterText.Text = $"Tổng: {f.Count} nhật ký · Mở: {f.Count(l => IsOpenStatus(l.Status))} · Hoàn thành: {f.Count(l => IsCompletedStatus(l.Status))} · Critical: {f.Count(l => l.Priority == "Critical")}";
    }

    private void BuildLogPageBtns(int tp) { LogPageBtns.Children.Clear(); for (int i = 0; i < Math.Min(tp, 10); i++) { var idx = i; var nb = PagerBtn(i, _logPage, idx); nb.Click += (_, _) => { _logPage = idx; RefreshLogPage(false); }; LogPageBtns.Children.Add(nb); } }
    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 26, Height = 24, FontSize = 10, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = LogPrevBtn.Template };

    private static void AddHdr(Grid g, int col, string text) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }
    private static void AddCell(Grid g, int col, string text, string color, bool bold) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }

    // ═══ INSPECTOR ═══

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Công việc", "Khách", "Reservation", "Timeline", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _inspectorTab; var b = new Border { CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 1, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("White") : Cb("Transparent"), BorderBrush = active ? Cb("#E2E8F0") : Cb("Transparent"), BorderThickness = new Thickness(1, 1, 1, 0), Child = new TextBlock { Text = tabs[i], FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#2F6BFF") : C("#64748B") } }; b.MouseLeftButtonDown += (_, _) => { _inspectorTab = idx; InitInspectorTabs(); BuildInspector(); }; InspectorTabs.Children.Add(b); }
        BuildInspector();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_selectedLog == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn nhật ký để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        var l = _selectedLog;
        var priColor = l.Priority switch { "Critical" => "#DC2626", "High" => "#F59E0B", "Medium" => "#2F6BFF", _ => "#16A34A" };

        switch (_inspectorTab)
        {
            case 0:
                InspectorPanel.Children.Add(STitle(l.Title));
                InspectorPanel.Children.Add(new Border { Background = Cb(priColor), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 0, 6), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = l.Priority, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
                InspectorPanel.Children.Add(new TextBlock { Text = l.Description, FontSize = 12, Foreground = C("#334155"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
                InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 0, 0, 6) });
                InspectorPanel.Children.Add(Row("Ca", l.Shift)); InspectorPanel.Children.Add(Row("Danh mục", l.Category));
                InspectorPanel.Children.Add(Row("Trạng thái", l.Status)); InspectorPanel.Children.Add(Row("Phân công", l.AssignedTo));
                InspectorPanel.Children.Add(Row("Người tạo", l.CreatedBy));
                if (l.FollowUpTime.HasValue) InspectorPanel.Children.Add(Row("Theo dõi", l.FollowUpTime.Value.ToString("dd/MM/yyyy HH:mm")));
                break;
            case 1:
                InspectorPanel.Children.Add(STitle("Công việc"));
                InspectorPanel.Children.Add(Row("Tổng task", $"{l.SubTasks}"));
                InspectorPanel.Children.Add(Row("Hoàn thành", $"{l.CompletedTasks}"));
                InspectorPanel.Children.Add(Row("Còn lại", $"{Math.Max(0, l.SubTasks - l.CompletedTasks)}"));
                break;
            case 2:
                InspectorPanel.Children.Add(STitle("Khách"));
                InspectorPanel.Children.Add(Row("Tên", l.Guest)); InspectorPanel.Children.Add(Row("Phòng", l.Room));
                if (l.Reservation != null) InspectorPanel.Children.Add(Row("Reservation", l.Reservation));
                if (l.IsVip) InspectorPanel.Children.Add(new Border { Background = Cb("#FEF3C7"), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 4, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = "★ VIP", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#F59E0B") } });
                break;
            case 3:
                InspectorPanel.Children.Add(STitle("Reservation"));
                InspectorPanel.Children.Add(Row("Mã Res", l.Reservation ?? "—"));
                InspectorPanel.Children.Add(Row("Khách", l.Guest)); InspectorPanel.Children.Add(Row("Phòng", l.Room));
                break;
            case 4:
                InspectorPanel.Children.Add(STitle("Timeline"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ {l.Time:dd/MM HH:mm} — Tạo bởi {l.CreatedBy}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                if (l.Status != "Open") InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ — Cập nhật: {l.Status}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 5:
                InspectorPanel.Children.Add(STitle("Lịch sử chỉnh sửa"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"{l.Time:dd/MM HH:mm} — Tạo — {l.CreatedBy}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                break;
        }
    }

    // ═══ EVENT HANDLERS ═══

    private void OnQuickToday(object s, RoutedEventArgs e) { _shiftFilter = "Today"; BuildTimeline(); RefreshLogPage(true); }
    private void OnQuickMorning(object s, RoutedEventArgs e) { _shiftFilter = "Morning"; BuildTimeline(); RefreshLogPage(true); }
    private void OnQuickCritical(object s, RoutedEventArgs e) { PriorityFilterCombo.SelectedIndex = PriorityFilterCombo.ItemsSource is string[] items ? Array.IndexOf(items, "Critical") : 0; RefreshLogPage(true); }
    private void OnQuickVip(object s, RoutedEventArgs e) { CategoryFilterCombo.SelectedIndex = CategoryFilterCombo.ItemsSource is string[] cats ? Array.IndexOf(cats, "VIP") : 0; RefreshLogPage(true); }
    private async void OnCreateLog(object s, RoutedEventArgs e)
    {
        if (_shiftLogCommandService is null)
        {
            MessageBox.Show("Chưa cấu hình ShiftLog command service để ghi nhật ký vận hành thật.", "Nhật ký ca", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new CreateShiftLogWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Request is not { } request) return;

        try
        {
            await _shiftLogCommandService.CreateAsync(request, CancellationToken.None);
            await ReloadRealLogsAsync(resetFilters: false);
            FooterText.Text = "Đã lưu nhật ký ca thật và tải lại danh sách.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tạo được nhật ký vận hành: {ex.Message}", "Nhật ký ca", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void OnApplyFilter(object s, RoutedEventArgs e) => RefreshLogPage(true);
    private void OnClearFilter(object s, RoutedEventArgs e) { LogSearchBox.Text = ""; ShiftFilterCombo.SelectedIndex = 0; PriorityFilterCombo.SelectedIndex = 0; StatusFilterCombo.SelectedIndex = 0; CategoryFilterCombo.SelectedIndex = 0; _shiftFilter = "All"; BuildTimeline(); RefreshLogPage(true); }
    private void OnLogPrevPage(object s, RoutedEventArgs e) { if (_logPage > 0) { _logPage--; RefreshLogPage(false); } }
    private void OnLogNextPage(object s, RoutedEventArgs e) { _logPage++; RefreshLogPage(false); }
    private void OnLogPageSizeChanged(object s, SelectionChangedEventArgs e) { _logPageSize = LogPageSizeBox.SelectedIndex switch { 0 => 30, 1 => 50, 2 => 100, _ => 30 }; RefreshLogPage(true); }
    private async void OnRefresh(object s, RoutedEventArgs e)
    {
        await ReloadRealLogsAsync(resetFilters: true);
    }

    private async Task ReloadRealLogsAsync(bool resetFilters)
    {
        if (_executionContext?.Current is not { } context)
        {
            FooterText.Text = "Chưa cấu hình được property context dữ liệu thật.";
            return;
        }

        try
        {
            var (shifts, incidents, entries) = await LoadRealLogsAsync(context.Property.PropertyId.Value);
            ReplaceLogs(shifts, incidents, entries);
            if (resetFilters)
            {
                _shiftFilter = "All";
                InitFilters();
            }
            RebuildView();
            FooterText.Text = $"Đã tải {shifts.Count:N0} ca tiền mặt, {incidents.Count:N0} sự cố và {entries.Count:N0} nhật ký ca từ dữ liệu thật.";
        }
        catch (Exception ex)
        {
            _logs.Clear();
            RebuildView();
            FooterText.Text = "Không tải được nhật ký vận hành thật: " + ex.Message;
        }
    }
    private void OnExport(object s, RoutedEventArgs e)
    {
        var rows = GetFilteredLogs();
        if (rows.Count == 0)
        {
            ShowNoData("Không có nhật ký ca thật phù hợp bộ lọc để xuất.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Xuất nhật ký ca",
            Filter = "CSV UTF-8 (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            FileName = $"nhat-ky-ca-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            AddExtension = true,
            DefaultExt = ".csv",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Thời gian,Ca,Tiêu đề,Nhóm,Ưu tiên,Phòng,Khách,Đặt phòng,Người phụ trách,Người tạo,Trạng thái,Mô tả,VIP,Có khiếu nại,Số việc,Số việc hoàn tất");
            foreach (var row in rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(row.Time.ToString("yyyy-MM-dd HH:mm:ss")),
                    Csv(row.Shift),
                    Csv(row.Title),
                    Csv(row.Category),
                    Csv(row.Priority),
                    Csv(row.Room),
                    Csv(row.Guest),
                    Csv(row.Reservation),
                    Csv(row.AssignedTo),
                    Csv(row.CreatedBy),
                    Csv(row.Status),
                    Csv(row.Description),
                    Csv(row.IsVip ? "Có" : "Không"),
                    Csv(row.HasComplaint ? "Có" : "Không"),
                    Csv(row.SubTasks.ToString()),
                    Csv(row.CompletedTasks.ToString())
                }));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            FooterText.Text = $"Đã xuất {rows.Count:N0} nhật ký ca thật theo bộ lọc hiện tại.";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không xuất được nhật ký ca: " + ex.Message, "Nhật ký ca", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
    private void OnPrint(object s, RoutedEventArgs e)
    {
        var rows = GetFilteredLogs();
        if (rows.Count == 0)
        {
            ShowNoData("Không có nhật ký ca thật phù hợp bộ lọc để in.");
            return;
        }

        var content = new StringBuilder()
            .AppendLine("BÁO CÁO NHẬT KÝ CA")
            .AppendLine($"Thời điểm xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine($"Số bản ghi: {rows.Count:N0}")
            .AppendLine(new string('-', 110))
            .AppendLine("Thời gian | Ca | Phòng | Khách | Tiêu đề | Trạng thái | Người phụ trách")
            .AppendLine(new string('-', 110));

        foreach (var row in rows)
        {
            content.AppendLine(string.Join(" | ", new[]
            {
                row.Time.ToString("dd/MM/yyyy HH:mm"),
                row.Shift,
                row.Room,
                row.Guest,
                row.Title,
                row.Status,
                row.AssignedTo
            }));
        }

        new PrintPreviewDialog("Báo cáo nhật ký ca", content.ToString())
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();
    }
    private void OnClose(object s, RoutedEventArgs e) => Close();
    private static void ShowNoData(string message) => MessageBox.Show(message, "Nhật ký ca", MessageBoxButton.OK, MessageBoxImage.Information);

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 6) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsSl(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record SlLog(int Id, DateTime Time, string Shift, string Title, string Category, string Priority, string Room, string Guest, string? Reservation, string AssignedTo, string CreatedBy, string Status, string Description, DateTime? FollowUpTime, bool IsVip, bool HasComplaint, int SubTasks, int CompletedTasks);
internal static class SlExt { public static TextBlock WithRunsSl(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
