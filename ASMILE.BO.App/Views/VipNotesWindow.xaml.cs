using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Core.FrontOffice.Guest;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class VipNotesWindow : Window
{
    private readonly List<VnNote> _notes;
    private VnNote? _selected;
    private int _page, _pageSize = 30;
    private int _inspectorTab;
    private string _timelineFilter = "All";
    private readonly IGuestProfileRepository? _guestRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IServiceScope? _serviceScope;
    private bool _loadedRealData;

    public VipNotesWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _guestRepository = scopedServices?.GetService<IGuestProfileRepository>();
        _executionContext = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _notes = new List<VnNote>();
        InitFilters();
        BuildKpi();
        BuildTimeline();
        InitInspectorTabs();
        RefreshPage(true);
        FooterText.Text = "Đang tải ghi chú hồ sơ VIP từ cơ sở dữ liệu...";
        Loaded += async (_, _) => await LoadPersistedNotesAsync();
        Closed += (_, _) => _serviceScope?.Dispose();
    }

    private async Task LoadPersistedNotesAsync()
    {
        _notes.Clear();
        _selected = null;
        if (_guestRepository is null || _executionContext?.Current is not { } context)
        {
            _loadedRealData = false;
            FooterText.Text = "Chưa cấu hình được Guest service và property context dữ liệu thật.";
            BuildKpi(); BuildTimeline(); RefreshPage(true);
            return;
        }

        try
        {
            var page = await _guestRepository.SearchAsync(
                context.Property.PropertyId.Value,
                new GuestSearchFilter(null, VipLevel.Bronze, null, null, null, null, null, null, null),
                new PageRequest(1, 200), CancellationToken.None);
            var id = 0;
            foreach (var guest in page.Items.Where(x => !string.IsNullOrWhiteSpace(x.Notes)))
            {
                id++;
                _notes.Add(new VnNote(
                    id, guest.GuestProfileId, guest.FullName, guest.VipLevel.ToString(), "—", guest.CompanyName, "—",
                    "Ghi chú hồ sơ", "—", "Đã lưu", "Ghi chú hồ sơ", guest.Notes!, guest.CreatedAtUtc,
                    guest.CreatedAtUtc, "—", null, false, false, false, false, guest.IsBlacklisted));
            }
            _loadedRealData = true;
            FooterText.Text = $"Đã tải {_notes.Count:N0} ghi chú hồ sơ VIP từ cơ sở dữ liệu; không có dữ liệu mẫu.";
        }
        catch (Exception ex)
        {
            _loadedRealData = false;
            FooterText.Text = $"Không tải được ghi chú hồ sơ VIP: {ex.Message}";
        }

        BuildKpi(); BuildTimeline(); RefreshPage(true); InitInspectorTabs();
    }

    private void InitFilters()
    {
        LevelFilter.ItemsSource = new[] { "Tất cả", "Bronze", "Silver", "Gold", "Platinum" };
        LevelFilter.SelectedIndex = 0;
        CatFilter.ItemsSource = new[] { "Tất cả", "Ghi chú hồ sơ" };
        CatFilter.SelectedIndex = 0;
        PriFilter.ItemsSource = new[] { "Tất cả", "—" }; PriFilter.SelectedIndex = 0;
        StFilter.ItemsSource = new[] { "Tất cả", "Đã lưu" }; StFilter.SelectedIndex = 0;
        PageSizeBox.ItemsSource = new[] { "30 / trang", "50 / trang", "100 / trang" }; PageSizeBox.SelectedIndex = 0;
    }

    private void BuildKpi()
    {
        KpiStrip.Children.Clear();
        var kpis = new (string, int, string)[] {
            ("Tổng ghi chú", _notes.Count, "#F59E0B"),
            ("Platinum", _notes.Count(n => n.VipLevel == "Platinum"), "#DC2626"),
            ("Gold", _notes.Count(n => n.VipLevel == "Gold"), "#8B5CF6"),
            ("Silver", _notes.Count(n => n.VipLevel == "Silver"), "#16A34A"),
            ("Blacklist", _notes.Count(n => n.IsBlacklisted), "#DC2626"),
            ("Có công ty", _notes.Count(n => n.Company is not null), "#F59E0B"),
            ("Đã lưu", _notes.Count(n => n.Status == "Đã lưu"), "#2F6BFF")
        };
        foreach (var (label, value, color) in kpis) KpiStrip.Children.Add(Kpi(label, $"{value}", color));
    }

    private static FrameworkElement Kpi(string l, string v, string color) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };

    private void BuildTimeline()
    {
        TimelinePanel.Children.Clear();
        var ranges = new[] { ("Hôm nay", "Today"), ("Tất cả", "All") };
        foreach (var (label, key) in ranges) { var active = _timelineFilter == key; var b = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569") } }; b.MouseLeftButtonDown += (_, _) => { _timelineFilter = key; BuildTimeline(); RefreshPage(true); }; TimelinePanel.Children.Add(b); }
        TimelinePanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        var f = GetFiltered();
        TimelinePanel.Children.Add(new TextBlock { Text = $"Tổng: {f.Count} ghi chú", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 4) });
        TimelinePanel.Children.Add(new TextBlock { Text = $"Đã lưu: {f.Count(n => n.Status == "Đã lưu")}", FontSize = 10, Foreground = C("#64748B") });
    }

    private List<VnNote> GetFiltered()
    {
        var n = _notes.AsEnumerable(); var kw = SearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) n = n.Where(x => x.Guest.ToLower().Contains(kw) || x.Title.ToLower().Contains(kw) || x.Room.Contains(kw));
        var lvl = LevelFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(lvl) && lvl != "Tất cả") n = n.Where(x => x.VipLevel == lvl);
        var cat = CatFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(cat) && cat != "Tất cả") n = n.Where(x => x.Category == cat);
        var pri = PriFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(pri) && pri != "Tất cả") n = n.Where(x => x.Priority == pri);
        var st = StFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(st) && st != "Tất cả") n = n.Where(x => x.Status == st);
        if (_timelineFilter == "Today") n = n.Where(x => x.Created.Date == DateTime.UtcNow.Date);
        return n.ToList();
    }

    private void RefreshPage(bool reset)
    {
        if (reset) _page = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _pageSize - 1) / _pageSize);
        if (_page >= tp) _page = tp - 1; GridPanel.Children.Clear();
        var hdr = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        var cw = new[] { 130.0, 70.0, 50.0, 100.0, 90.0, 110.0, 55.0, 75.0, 80.0, 75.0 };
        var ch = new[] { "Khách", "VIP Level", "Phòng", "Công ty", "Reservation", "Danh mục", "Ưu tiên", "Trạng thái", "Cập nhật", "Owner" };
        for (int j = 0; j < cw.Length; j++) { hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) }); AddHdr(hdr, j, ch[j]); }
        GridPanel.Children.Add(hdr);

        foreach (var note in f.Skip(_page * _pageSize).Take(_pageSize))
        {
            var active = _selected?.Id == note.Id;
            var lvlColor = note.VipLevel switch { "Platinum" => "#DC2626", "Gold" => "#F59E0B", _ => "#64748B" };
            var priColor = "#64748B";
            var stColor = "#16A34A";
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1), Background = active ? Cb("#EFF6FF") : Cb("Transparent") };
            for (int j = 0; j < cw.Length; j++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) });
            var vals = new[] { note.Guest, note.VipLevel, note.Room, note.Company ?? "—", note.Reservation, note.Category, note.Priority, note.Status, note.LastUpdated.ToString("dd/MM HH:mm"), note.Owner };
            var colors = new[] { "#17365D", lvlColor, "#64748B", "#64748B", "#64748B", "#64748B", priColor, stColor, "#64748B", "#64748B" };
            for (int j = 0; j < vals.Length; j++) AddCell(row, j, vals[j], colors[j], j == 0 || j == 1);
            var nid = note.Id; row.Cursor = System.Windows.Input.Cursors.Hand;
            row.MouseLeftButtonDown += (_, _) => { _selected = note; RefreshPage(false); BuildInspector(); };
            GridPanel.Children.Add(row);
        }
        PagerSummary.Text = $"{_page * _pageSize + 1}–{Math.Min((_page + 1) * _pageSize, f.Count)} / {f.Count}";
        PrevBtn.IsEnabled = _page > 0; NextBtn.IsEnabled = _page < tp - 1; BuildPageBtns(tp);
        FooterText.Text = _loadedRealData
            ? $"Tổng: {f.Count} · Đã lưu: {f.Count(n => n.Status == "Đã lưu")} · Platinum: {f.Count(n => n.VipLevel == "Platinum")}"
            : "Chưa có dữ liệu thật để hiển thị.";
    }

    private void BuildPageBtns(int tp) { PageBtns.Children.Clear(); for (int i = 0; i < Math.Min(tp, 10); i++) { var idx = i; var nb = PagerBtn(i, _page, idx); nb.Click += (_, _) => { _page = idx; RefreshPage(false); }; PageBtns.Children.Add(nb); } }
    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 26, Height = 24, FontSize = 10, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = PrevBtn.Template };
    private static void AddHdr(Grid g, int col, string text) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }
    private static void AddCell(Grid g, int col, string text, string color, bool bold) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Sở thích", "Dịch vụ", "Bảo mật", "Timeline", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _inspectorTab; var b = new Border { CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 1, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("White") : Cb("Transparent"), BorderBrush = active ? Cb("#E2E8F0") : Cb("Transparent"), BorderThickness = new Thickness(1, 1, 1, 0), Child = new TextBlock { Text = tabs[i], FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#2F6BFF") : C("#64748B") } }; b.MouseLeftButtonDown += (_, _) => { _inspectorTab = idx; InitInspectorTabs(); BuildInspector(); }; InspectorTabs.Children.Add(b); }
        BuildInspector();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_selected == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn ghi chú VIP để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        var n = _selected;
        var lvlColor = n.VipLevel switch { "Platinum" => "#DC2626", "Gold" => "#F59E0B", _ => "#64748B" };

        switch (_inspectorTab)
        {
            case 0:
                InspectorPanel.Children.Add(STitle(n.Title));
                InspectorPanel.Children.Add(new Border { Background = Cb(lvlColor), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 0, 6), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = n.VipLevel, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
                InspectorPanel.Children.Add(new TextBlock { Text = n.Description, FontSize = 12, Foreground = C("#334155"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
                InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 0, 0, 6) });
                InspectorPanel.Children.Add(Row("Khách", n.Guest)); InspectorPanel.Children.Add(Row("Phòng", n.Room));
                InspectorPanel.Children.Add(Row("Reservation", n.Reservation));
                if (n.Company != null) InspectorPanel.Children.Add(Row("Công ty", n.Company));
                InspectorPanel.Children.Add(Row("Danh mục", n.Category)); InspectorPanel.Children.Add(Row("Owner", n.Owner));
                break;
            case 1:
                InspectorPanel.Children.Add(STitle("Sở thích"));
                InspectorPanel.Children.Add(new TextBlock { Text = "Chưa có preference được lưu trong dữ liệu hồ sơ này.", FontSize = 12, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap });
                break;
            case 2:
                InspectorPanel.Children.Add(STitle("Dịch vụ"));
                InspectorPanel.Children.Add(new TextBlock { Text = "Chưa có yêu cầu dịch vụ lưu trong dữ liệu thật.", FontSize = 12, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap });
                break;
            case 3:
                InspectorPanel.Children.Add(STitle("Bảo mật"));
                InspectorPanel.Children.Add(Row("Blacklist", n.IsBlacklisted ? "Có" : "Không"));
                break;
            case 4:
                InspectorPanel.Children.Add(STitle("Timeline"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ {n.Created:dd/MM HH:mm} — Tạo bởi {n.Owner}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ {n.LastUpdated:dd/MM HH:mm} — Cập nhật", FontSize = 11, Foreground = C("#2F6BFF"), Margin = new Thickness(0, 2, 0, 0) });
                if (n.FollowUp != null) InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ Follow-up: {n.FollowUp}", FontSize = 11, Foreground = C("#F59E0B"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 5:
                InspectorPanel.Children.Add(STitle("Lịch sử"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"{n.Created:dd/MM HH:mm} — Tạo — {n.Owner}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                break;
        }
    }

    private void OnQuickToday(object s, RoutedEventArgs e) { _timelineFilter = "Today"; BuildTimeline(); RefreshPage(true); }
    private void OnQuickVvip(object s, RoutedEventArgs e) { LevelFilter.SelectedIndex = LevelFilter.ItemsSource is string[] items ? Array.IndexOf(items, "Platinum") : 0; RefreshPage(true); }
    private void OnQuickArrival(object s, RoutedEventArgs e) { _timelineFilter = "Today"; BuildTimeline(); RefreshPage(true); }
    private async void OnNewNote(object s, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            ShowUnavailable("Hãy chọn một ghi chú/khách VIP trước khi tạo ghi chú mới.");
            return;
        }
        if (_guestRepository is null || _executionContext?.Current is not { } context)
        {
            ShowUnavailable("Chưa cấu hình GuestProfile service hoặc property context thật.");
            return;
        }

        var input = ShowNoteEditor(_selected.Guest);
        if (input is null) return;

        try
        {
            var guest = await _guestRepository.GetAsync(context.Property.PropertyId.Value, _selected.GuestId, CancellationToken.None);
            if (guest is null)
            {
                ShowUnavailable("Không tìm thấy GuestProfile thật được chọn.");
                return;
            }

            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            var entry = $"[{stamp}] {input.Value.Title.Trim()}: {input.Value.Content.Trim()}";
            var notes = string.IsNullOrWhiteSpace(guest.Notes) ? entry : guest.Notes.TrimEnd() + Environment.NewLine + entry;
            guest.SetNotes(notes, DateTime.UtcNow, context.Actor.ActorId);
            await _guestRepository.UpdateAsync(guest, CancellationToken.None);
            await LoadPersistedNotesAsync();
            MessageBox.Show($"Đã lưu ghi chú VIP thật cho {guest.FullName}.", "Ghi chú VIP", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể lưu ghi chú VIP thật: " + ex.Message, "Ghi chú VIP", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static (string Title, string Content)? ShowNoteEditor(string guestName)
    {
        var title = new TextBox { Height = 30, Margin = new Thickness(0, 4, 0, 10) };
        var content = new TextBox { Height = 110, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 4, 0, 12) };
        var ok = new Button { Content = "Lưu ghi chú", Width = 110, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "Hủy", Width = 80, IsCancel = true };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(ok); buttons.Children.Add(cancel);
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = $"Khách: {guestName}", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") });
        panel.Children.Add(new TextBlock { Text = "Tiêu đề", Margin = new Thickness(0, 14, 0, 0) });
        panel.Children.Add(title);
        panel.Children.Add(new TextBlock { Text = "Nội dung", Margin = new Thickness(0, 2, 0, 0) });
        panel.Children.Add(content);
        panel.Children.Add(buttons);
        var dialog = new Window
        {
            Title = "Tạo ghi chú VIP",
            Owner = System.Windows.Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 480,
            Height = 330,
            ResizeMode = ResizeMode.NoResize,
            Content = panel
        };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(title.Text) || string.IsNullOrWhiteSpace(content.Text))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề và nội dung.", "Ghi chú VIP", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            dialog.DialogResult = true;
        };
        return dialog.ShowDialog() == true ? (title.Text, content.Text) : null;
    }
    private void OnApplyFilter(object s, RoutedEventArgs e) => RefreshPage(true);
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = ""; LevelFilter.SelectedIndex = 0; CatFilter.SelectedIndex = 0; PriFilter.SelectedIndex = 0; StFilter.SelectedIndex = 0; _timelineFilter = "All"; BuildTimeline(); RefreshPage(true); }
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_page > 0) { _page--; RefreshPage(false); } }
    private void OnNextPage(object s, RoutedEventArgs e) { _page++; RefreshPage(false); }
    private void OnPageSizeChanged(object s, SelectionChangedEventArgs e) { _pageSize = PageSizeBox.SelectedIndex switch { 0 => 30, 1 => 50, 2 => 100, _ => 30 }; RefreshPage(true); }
    private async void OnRefresh(object s, RoutedEventArgs e) { _selected = null; InitFilters(); await LoadPersistedNotesAsync(); }
    private void OnExport(object s, RoutedEventArgs e)
    {
        var rows = GetFiltered();
        if (rows.Count == 0)
        {
            ShowUnavailable("Không có ghi chú VIP thật phù hợp bộ lọc để xuất.");
            return;
        }

        var path = CsvExportHelper.Export(
            "ghi-chu-vip",
            new[] { "Ngày tạo", "Khách", "VIP", "Phòng", "Công ty", "Đặt phòng", "Nhóm", "Ưu tiên", "Trạng thái", "Tiêu đề", "Nội dung", "Người phụ trách", "Theo dõi", "Sinh nhật", "Kỷ niệm", "Đến", "Dị ứng", "Blacklist" },
            rows.Select(row => (IReadOnlyList<string>)new[]
            {
                row.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                row.Guest,
                row.VipLevel,
                row.Room,
                row.Company ?? string.Empty,
                row.Reservation,
                row.Category,
                row.Priority,
                row.Status,
                row.Title,
                row.Description,
                row.Owner,
                row.FollowUp ?? string.Empty,
                row.HasBirthday ? "Có" : "Không",
                row.HasAnniversary ? "Có" : "Không",
                row.IsArrival ? "Có" : "Không",
                row.IsFoodAllergy ? "Có" : "Không",
                row.IsBlacklisted ? "Có" : "Không"
            }).ToList());
        FooterText.Text = $"Đã xuất {rows.Count:N0} ghi chú VIP thật: {path}";
    }

    private void OnPrint(object s, RoutedEventArgs e)
    {
        var rows = GetFiltered();
        if (rows.Count == 0)
        {
            ShowUnavailable("Không có ghi chú VIP thật phù hợp bộ lọc để in.");
            return;
        }

        var content = new StringBuilder()
            .AppendLine("BÁO CÁO GHI CHÚ VIP")
            .AppendLine($"Thời điểm xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine($"Số bản ghi: {rows.Count:N0}")
            .AppendLine(new string('-', 110))
            .AppendLine("Ngày tạo | Khách | VIP | Phòng | Ưu tiên | Trạng thái | Tiêu đề | Người phụ trách")
            .AppendLine(new string('-', 110));
        foreach (var row in rows)
        {
            content.AppendLine(string.Join(" | ", new[]
            {
                row.Created.ToString("dd/MM/yyyy HH:mm"),
                row.Guest,
                row.VipLevel,
                row.Room,
                row.Priority,
                row.Status,
                row.Title,
                row.Owner
            }));
        }

        new PrintPreviewDialog("Báo cáo ghi chú VIP", content.ToString())
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();
    }
    private void OnClose(object s, RoutedEventArgs e) => Close();
    private static void ShowUnavailable(string message) => MessageBox.Show(message, "Ghi chú VIP", MessageBoxButton.OK, MessageBoxImage.Information);

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 6) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsVn(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record VnNote(int Id, Guid GuestId, string Guest, string VipLevel, string Room, string? Company, string Reservation, string Category, string Priority, string Status, string Title, string Description, DateTime Created, DateTime LastUpdated, string Owner, string? FollowUp, bool HasBirthday, bool HasAnniversary, bool IsArrival, bool IsFoodAllergy, bool IsBlacklisted);
internal static class VnExt { public static TextBlock WithRunsVn(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
