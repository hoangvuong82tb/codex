using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public partial class KeyCardAuditLogWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private readonly List<KlEvent> _events;
    private KlEvent? _selectedEvent;
    private int _eventPage, _eventPageSize = 50;
    private int _inspectorTab;
    private string _timelineFilter = "All";
    private readonly AppDbContext? _db;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;

    public KeyCardAuditLogWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _events = [];
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _db = services?.GetService<AppDbContext>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        Loaded += async (_, _) => await LoadRealEventsAsync();
        InitFilters();
        BuildKpiStrip();
        BuildTimeline();
        InitInspectorTabs();
        RefreshEventPage(true);
        FooterText.Text = "Sẵn sàng · Chọn sự kiện để xem chi tiết";
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async Task LoadRealEventsAsync()
    {
        _events.Clear();
        var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
        if (_db is null || propertyId is null)
        {
            RefreshEventPage(true);
            return;
        }

        var credentials = await _db.KeyCardCredentials.AsNoTracking()
            .Where(x => x.PropertyId == propertyId.Value)
            .ToDictionaryAsync(x => x.CredentialId);
        var stays = await _db.HotelStays.AsNoTracking()
            .Where(x => x.PropertyId == propertyId.Value)
            .ToDictionaryAsync(x => x.HotelStayId);
        var events = await _db.KeyCardEvents.AsNoTracking()
            .Where(x => x.PropertyId == propertyId.Value)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(5000)
            .ToListAsync();

        var id = 0;
        foreach (var item in events)
        {
            id++;
            credentials.TryGetValue(item.CredentialId, out var credential);
            stays.TryGetValue(credential?.StayId ?? Guid.Empty, out var stay);
            var eventType = item.EventType switch
            {
                DoorLockEvents.KeyIssued => "Issue Card",
                DoorLockEvents.KeyRevoked => "Disable Card",
                DoorLockEvents.KeyLost => "Lost Card",
                DoorLockEvents.KeyReturned => "Return Card",
                DoorLockEvents.KeyExpired => "Expired",
                _ => item.EventType
            };
            var success = item.AccessGranted;
            var risk = eventType is "Lost Card" or "Disable Card" ? "Medium" : success ? "Low" : "High";
            _events.Add(new KlEvent(id, item.EventId.ToString(), item.OccurredAtUtc,
                credential?.CardSerial ?? "—", credential?.AccessLevel ?? "Guest",
                stay?.GuestNameSnapshot ?? "—", stay?.ReservationNumberSnapshot ?? "—",
                stay?.RoomNumberSnapshot ?? item.DoorId ?? "—", "Operator hiện tại", "Front Office",
                eventType, item.Notes, success ? "Success" : "Failed", "Completed", risk,
                credential is { EncoderId: > 0 } c ? $"Encoder {c.EncoderId}" : "Chưa ghi nhận",
                "—", "—", 0, 0));
        }

        InitFilters();
        BuildKpiStrip();
        BuildTimeline();
        InitInspectorTabs();
        RefreshEventPage(true);
    }

    private void InitFilters()
    {
        EventTypeFilter.ItemsSource = new[] { "Tất cả", "Issue Card", "Reissue Card", "Disable Card", "Return Card", "Lost Card", "Broken Card", "Access Denied", "Access Granted", "Security Alert" };
        EventTypeFilter.SelectedIndex = 0;
        EventResultFilter.ItemsSource = new[] { "Tất cả", "Success", "Warning", "Failed", "Blocked" };
        EventResultFilter.SelectedIndex = 0;
        EventRiskFilter.ItemsSource = new[] { "Tất cả", "Low", "Medium", "High", "Critical" };
        EventRiskFilter.SelectedIndex = 0;
        EventPageSizeBox.ItemsSource = new[] { "50 / trang", "100 / trang", "200 / trang" };
        EventPageSizeBox.SelectedIndex = 0;
    }

    // ═══ KPI ═══

    private void BuildKpiStrip()
    {
        KpiStrip.Children.Clear();
        var today = _events.Where(e => e.Time.ToLocalTime().Date == DateTime.Now.Date).ToList();
        var kpis = new (string, int, string)[] {
            ("Sự kiện hôm nay", today.Count, "#2F6BFF"),
            ("Cấp thẻ", today.Count(e => e.EventType == "Issue Card"), "#16A34A"),
            ("Cấp lại", today.Count(e => e.EventType == "Reissue Card"), "#F59E0B"),
            ("Khóa thẻ", today.Count(e => e.EventType == "Disable Card"), "#DC2626"),
            ("Thu hồi", today.Count(e => e.EventType == "Return Card"), "#64748B"),
            ("Mất/Hỏng", today.Count(e => e.EventType is "Lost Card" or "Broken Card"), "#8B5CF6"),
            ("Cảnh báo AN", today.Count(e => e.EventType == "Security Alert" || e.Risk == "Critical"), "#DC2626")
        };
        foreach (var (label, value, color) in kpis)
            KpiStrip.Children.Add(Kpi(label, $"{value}", color));
    }

    private static FrameworkElement Kpi(string l, string v, string color) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };

    // ═══ TIMELINE ═══

    private void BuildTimeline()
    {
        TimelinePanel.Children.Clear();
        var ranges = new[] { ("Hôm nay", "Today"), ("Hôm qua", "Yesterday"), ("7 ngày", "7Days"), ("30 ngày", "30Days"), ("Tất cả", "All") };
        foreach (var (label, key) in ranges)
        {
            var active = _timelineFilter == key;
            var b = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569") } };
            b.MouseLeftButtonDown += (_, _) => { _timelineFilter = key; BuildTimeline(); RefreshEventPage(true); };
            TimelinePanel.Children.Add(b);
        }

        // Stats
        TimelinePanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        var filtered = GetFilteredEvents();
        var typeGroups = filtered.GroupBy(e => e.EventType).OrderByDescending(g => g.Count()).Take(8);
        foreach (var g in typeGroups)
        {
            var color = g.Key switch { "Issue Card" => "#16A34A", "Reissue Card" => "#F59E0B", "Disable Card" => "#DC2626", "Return Card" => "#64748B", "Lost Card" => "#8B5CF6", "Security Alert" => "#DC2626", _ => "#94A3B8" };
            TimelinePanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0), Children = { new Border { Width = 8, Height = 8, Background = Cb(color), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = g.Key, FontSize = 10, Foreground = C("#64748B"), Width = 100 }, new TextBlock { Text = $"{g.Count()}", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") } } });
        }
    }

    // ═══ EVENT GRID ═══

    private List<KlEvent> GetFilteredEvents()
    {
        var e = _events.AsEnumerable();
        var kw = EventSearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) e = e.Where(x => x.Guest.ToLower().Contains(kw) || x.CardNo.ToLower().Contains(kw) || x.Room.Contains(kw) || x.EventId.ToLower().Contains(kw));
        var type = EventTypeFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(type) && type != "Tất cả") e = e.Where(x => x.EventType == type);
        var result = EventResultFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(result) && result != "Tất cả") e = e.Where(x => x.Result == result);
        var risk = EventRiskFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(risk) && risk != "Tất cả") e = e.Where(x => x.Risk == risk);
        var today = DateTime.Now.Date;
        if (_timelineFilter == "Today") e = e.Where(x => x.Time.ToLocalTime().Date == today);
        else if (_timelineFilter == "Yesterday") e = e.Where(x => x.Time.ToLocalTime().Date == today.AddDays(-1));
        else if (_timelineFilter == "7Days") e = e.Where(x => x.Time.ToLocalTime() >= today.AddDays(-6));
        else if (_timelineFilter == "30Days") e = e.Where(x => x.Time.ToLocalTime() >= today.AddDays(-29));
        return e.ToList();
    }

    private void RefreshEventPage(bool reset)
    {
        if (reset) _eventPage = 0;
        var f = GetFilteredEvents();
        var tp = Math.Max(1, (f.Count + _eventPageSize - 1) / _eventPageSize);
        if (_eventPage >= tp) _eventPage = tp - 1;

        EventGridPanel.Children.Clear();
        // Header
        var hdr = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        var colWidths = new[] { 75.0, 100.0, 90.0, 55.0, 130.0, 95.0, 50.0, 85.0, 95.0, 100.0, 65.0, 55.0 };
        var colHeaders = new[] { "Thời gian", "Mã SK", "Số thẻ", "Loại", "Khách", "Phòng/Res", "Phòng", "Operator", "Sự kiện", "Kết quả", "Rủi ro", "T.Thái" };
        for (int i = 0; i < colWidths.Length; i++) { hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(colWidths[i]) }); AddHdr(hdr, i, colHeaders[i]); }
        EventGridPanel.Children.Add(hdr);

        foreach (var evt in f.Skip(_eventPage * _eventPageSize).Take(_eventPageSize))
        {
            var active = _selectedEvent?.Id == evt.Id;
            var riskColor = evt.Risk switch { "Critical" => "#DC2626", "High" => "#F59E0B", "Medium" => "#2F6BFF", _ => "#16A34A" };
            var eventColor = evt.EventType switch { "Issue Card" => "#16A34A", "Reissue Card" => "#F59E0B", "Disable Card" => "#DC2626", "Return Card" => "#64748B", "Security Alert" => "#DC2626", "Access Denied" => "#DC2626", _ => "#94A3B8" };
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1), Background = active ? Cb("#EFF6FF") : Cb("Transparent") };
            for (int i = 0; i < colWidths.Length; i++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(colWidths[i]) });

            var vals = new[] { evt.Time.ToString("dd/MM HH:mm"), evt.EventId, evt.CardNo, evt.CardType, evt.Guest, evt.Reservation, evt.Room, evt.Operator, evt.EventType, evt.Result, evt.Risk, evt.Status };
            var colors = new[] { "#64748B", "#64748B", "#17365D", "#64748B", "#17365D", "#64748B", "#64748B", "#64748B", eventColor, evt.Result == "Success" ? "#16A34A" : "#DC2626", riskColor, "#64748B" };
            for (int i = 0; i < vals.Length; i++) AddCell(row, i, vals[i], colors[i], i == 4 || i == 8);
            var evtId = evt.Id; row.Cursor = System.Windows.Input.Cursors.Hand;
            row.MouseLeftButtonDown += (_, _) => { _selectedEvent = evt; RefreshEventPage(false); BuildInspector(); };
            EventGridPanel.Children.Add(row);
        }

        EventPagerSummary.Text = $"{_eventPage * _eventPageSize + 1}–{Math.Min((_eventPage + 1) * _eventPageSize, f.Count)} / {f.Count:N0} sự kiện";
        EventPrevBtn.IsEnabled = _eventPage > 0; EventNextBtn.IsEnabled = _eventPage < tp - 1;
        BuildEventPageBtns(tp);
        FooterText.Text = $"Hiển thị {Math.Min(_eventPageSize, f.Count - _eventPage * _eventPageSize)} / {f.Count:N0} sự kiện · Tổng: {f.Count:N0}";
    }

    private void BuildEventPageBtns(int tp) { EventPageBtns.Children.Clear(); for (int i = 0; i < Math.Min(tp, 10); i++) { var idx = i; var nb = PagerBtn(i, _eventPage, idx); nb.Click += (_, _) => { _eventPage = idx; RefreshEventPage(false); }; EventPageBtns.Children.Add(nb); } }
    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 28, Height = 26, FontSize = 11, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = EventPrevBtn.Template };

    private static void AddHdr(Grid g, int col, string text) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }
    private static void AddCell(Grid g, int col, string text, string color, bool bold) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }

    // ═══ INSPECTOR ═══

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Timeline", "Bảo mật", "Reservation", "Thẻ", "Operator" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _inspectorTab; var b = new Border { CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 1, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("White") : Cb("Transparent"), BorderBrush = active ? Cb("#E2E8F0") : Cb("Transparent"), BorderThickness = new Thickness(1, 1, 1, 0), Child = new TextBlock { Text = tabs[i], FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#2F6BFF") : C("#64748B") } }; b.MouseLeftButtonDown += (_, _) => { _inspectorTab = idx; InitInspectorTabs(); BuildInspector(); }; InspectorTabs.Children.Add(b); }
        BuildInspector();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_selectedEvent == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn một sự kiện để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        var e = _selectedEvent;
        var riskColor = e.Risk switch { "Critical" => "#DC2626", "High" => "#F59E0B", "Medium" => "#2F6BFF", _ => "#16A34A" };

        switch (_inspectorTab)
        {
            case 0: // Overview
                InspectorPanel.Children.Add(STitle("Tổng quan sự kiện"));
                InspectorPanel.Children.Add(Row("Mã SK", e.EventId)); InspectorPanel.Children.Add(Row("Thời gian", e.Time.ToString("dd/MM/yyyy HH:mm:ss")));
                InspectorPanel.Children.Add(Row("Loại SK", e.EventType)); InspectorPanel.Children.Add(Row("Kết quả", e.Result));
                InspectorPanel.Children.Add(Row("Trạng thái", e.Status));
                InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
                InspectorPanel.Children.Add(Row("Khách", e.Guest)); InspectorPanel.Children.Add(Row("Phòng", e.Room));
                InspectorPanel.Children.Add(Row("Reservation", e.Reservation));
                InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
                InspectorPanel.Children.Add(Row("Thẻ", e.CardNo)); InspectorPanel.Children.Add(Row("Loại thẻ", e.CardType));
                InspectorPanel.Children.Add(Row("Operator", e.Operator)); InspectorPanel.Children.Add(Row("Bộ phận", e.Dept));
                if (!string.IsNullOrEmpty(e.Reason)) InspectorPanel.Children.Add(Row("Lý do", e.Reason));
                break;
            case 1: // Timeline
                InspectorPanel.Children.Add(STitle("Timeline sự kiện"));
                var relatedEvents = _events.Where(x => x.CardNo == e.CardNo).OrderByDescending(x => x.Time).Take(10);
                foreach (var re in relatedEvents)
                {
                    var ec = re.EventType switch { "Issue Card" => "#16A34A", "Disable Card" => "#DC2626", "Return Card" => "#64748B", _ => "#94A3B8" };
                    InspectorPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6), Children = { new Border { Width = 3, Background = Cb(ec), CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 8, 0) }, new StackPanel { Children = { new TextBlock { Text = $"{re.Time:dd/MM HH:mm} · {re.EventType}", FontSize = 11, Foreground = C("#94A3B8") }, new TextBlock { Text = $"{re.Operator} · {re.Result}", FontSize = 10, Foreground = C("#64748B"), Margin = new Thickness(0, 1, 0, 0) } } } } });
                }
                break;
            case 2: // Security
                InspectorPanel.Children.Add(STitle("Đánh giá bảo mật"));
                InspectorPanel.Children.Add(new Border { Background = Cb(riskColor), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 8), Child = new TextBlock { Text = $"Rủi ro: {e.Risk}", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Cb("White") } });
                InspectorPanel.Children.Add(Row("Cảnh báo AN", e.EventType == "Security Alert" ? "Có" : "Không"));
                InspectorPanel.Children.Add(Row("Truy cập từ chối", e.EventType == "Access Denied" ? $"{_events.Count(x => x.CardNo == e.CardNo && x.EventType == "Access Denied")} lần" : "0"));
                InspectorPanel.Children.Add(Row("Thẻ nhạy cảm", e.CardType is "Master" or "Emergency" ? "Có" : "Không"));
                break;
            case 3: // Reservation
                InspectorPanel.Children.Add(STitle("Reservation"));
                InspectorPanel.Children.Add(Row("Mã Res", e.Reservation)); InspectorPanel.Children.Add(Row("Khách", e.Guest));
                InspectorPanel.Children.Add(Row("Phòng", e.Room));
                break;
            case 4: // Card
                InspectorPanel.Children.Add(STitle("Thông tin thẻ"));
                InspectorPanel.Children.Add(Row("Số thẻ", e.CardNo)); InspectorPanel.Children.Add(Row("Loại", e.CardType));
                InspectorPanel.Children.Add(Row("Encoder", e.Encoder));
                break;
            case 5: // Operator
                InspectorPanel.Children.Add(STitle("Operator"));
                InspectorPanel.Children.Add(Row("Người dùng", e.Operator)); InspectorPanel.Children.Add(Row("Bộ phận", e.Dept));
                InspectorPanel.Children.Add(Row("Terminal", e.Terminal)); InspectorPanel.Children.Add(Row("Workstation", e.Workstation));
                break;
        }
    }

    // ═══ QUICK FILTERS ═══

    private void OnQuickToday(object s, RoutedEventArgs e) { _timelineFilter = "Today"; BuildTimeline(); RefreshEventPage(true); }
    private void OnQuick7Days(object s, RoutedEventArgs e) { _timelineFilter = "7Days"; BuildTimeline(); RefreshEventPage(true); }
    private void OnQuick30Days(object s, RoutedEventArgs e) { _timelineFilter = "30Days"; BuildTimeline(); RefreshEventPage(true); }
    private void OnQuickAll(object s, RoutedEventArgs e) { _timelineFilter = "All"; BuildTimeline(); RefreshEventPage(true); }
    private void OnQuickSecurity(object s, RoutedEventArgs e) { EventTypeFilter.SelectedIndex = EventTypeFilter.ItemsSource is string[] items ? Array.IndexOf(items, "Security Alert") : 0; EventRiskFilter.SelectedIndex = EventRiskFilter.ItemsSource is string[] rItems ? Array.IndexOf(rItems, "Critical") : 0; RefreshEventPage(true); }

    private void OnApplyFilter(object s, RoutedEventArgs e) => RefreshEventPage(true);
    private void OnClearFilter(object s, RoutedEventArgs e) { EventSearchBox.Text = ""; EventTypeFilter.SelectedIndex = 0; EventResultFilter.SelectedIndex = 0; EventRiskFilter.SelectedIndex = 0; _timelineFilter = "All"; BuildTimeline(); RefreshEventPage(true); }
    private void OnEventPrevPage(object s, RoutedEventArgs e) { if (_eventPage > 0) { _eventPage--; RefreshEventPage(false); } }
    private void OnEventNextPage(object s, RoutedEventArgs e) { _eventPage++; RefreshEventPage(false); }
    private void OnEventPageSizeChanged(object s, SelectionChangedEventArgs e) { _eventPageSize = EventPageSizeBox.SelectedIndex switch { 0 => 50, 1 => 100, 2 => 200, _ => 50 }; RefreshEventPage(true); }
    private async void OnRefresh(object s, RoutedEventArgs e) { _selectedEvent = null; await LoadRealEventsAsync(); }
    private void OnExport(object s, RoutedEventArgs e)
    {
        var rows = GetFilteredEvents();
        if (rows.Count == 0)
        {
            Show("Không có sự kiện nhật ký thẻ thật phù hợp bộ lọc để xuất.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = $"key-card-audit-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("EventId,Time,CardNo,CardType,Guest,Reservation,Room,Operator,Department,EventType,Reason,Result,Status,Risk,Encoder");
            foreach (var evt in rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(evt.EventId),
                    Csv(evt.Time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
                    Csv(evt.CardNo),
                    Csv(evt.CardType),
                    Csv(evt.Guest),
                    Csv(evt.Reservation),
                    Csv(evt.Room),
                    Csv(evt.Operator),
                    Csv(evt.Dept),
                    Csv(evt.EventType),
                    Csv(evt.Reason),
                    Csv(evt.Result),
                    Csv(evt.Status),
                    Csv(evt.Risk),
                    Csv(evt.Encoder)
                }));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Show($"Đã xuất {rows.Count:N0} sự kiện nhật ký thẻ thật.");
        }
        catch (Exception ex)
        {
            Show("Không thể xuất nhật ký thẻ thật: " + ex.Message);
        }
    }

    private void OnPrint(object s, RoutedEventArgs e)
    {
        var rows = GetFilteredEvents();
        if (rows.Count == 0)
        {
            Show("Không có sự kiện nhật ký thẻ thật phù hợp bộ lọc để in.");
            return;
        }

        var content = new StringBuilder()
            .AppendLine("BÁO CÁO NHẬT KÝ THẺ")
            .AppendLine($"Thời điểm xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine($"Số sự kiện: {rows.Count:N0}")
            .AppendLine(new string('-', 110))
            .AppendLine("Thời gian | Mã SK | Số thẻ | Loại | Khách | Phòng | Sự kiện | Kết quả | Rủi ro")
            .AppendLine(new string('-', 110));
        foreach (var evt in rows)
        {
            content.AppendLine($"{evt.Time.ToLocalTime():dd/MM/yyyy HH:mm} | {evt.EventId} | {evt.CardNo} | {evt.CardType} | {evt.Guest} | {evt.Room} | {evt.EventType} | {evt.Result} | {evt.Risk}");
        }

        new PrintPreviewDialog("Báo cáo nhật ký thẻ", content.ToString()).ShowDialog();
    }
    private void OnClose(object s, RoutedEventArgs e) => Close();
    private static void Show(string a) => MessageBox.Show(a, "Nhật ký thẻ", MessageBoxButton.OK, MessageBoxImage.Information);
    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsKl(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record KlEvent(int Id, string EventId, DateTime Time, string CardNo, string CardType, string Guest, string Reservation, string Room, string Operator, string Dept, string EventType, string? Reason, string Result, string Status, string Risk, string Encoder, string Terminal, string Workstation, int GuestId, int CardId);
internal static class KlExt { public static TextBlock WithRunsKl(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
