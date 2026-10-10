using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.App.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class ReceiptVoucherWindow : Window
{
    private List<RvReceipt> _receipts = new();
    private RvReceipt? _selected;
    private int _page, _pageSize = 50;
    private int _inspectorTab;
    private string _statusFilter = "All";
    private PaymentReceiptsViewModel? _realViewModel;
    private IServiceScope? _dataScope;
    private Task? _realLoadTask;
    private bool _isClosed;

    public ReceiptVoucherWindow() : this(forceMock: false)
    {
    }

    /// <summary>
    /// The optional design/test flag is retained for isolated hosts only.
    /// The parameterless production route keeps the real PaymentReceiptsView integration.
    /// </summary>
    public ReceiptVoucherWindow(bool forceMock)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _receipts = new List<RvReceipt>();
        InitFilters();
        BuildKpi();
        BuildNavPanel();
        InitInspectorTabs();
        RefreshPage(true);
        FooterText.Text = forceMock
            ? "Chế độ thiết kế — không hiển thị dữ liệu mẫu."
            : "Đang tải phiếu thu từ CSDL thật…";
        if (!forceMock)
        {
            // This window is created from a shell, not by the scoped navigation
            // host. Give it its own scope so its EF DbContext cannot be shared
            // with another cashier window or a root-provider resolution.
            var services = (System.Windows.Application.Current as App)?.Services;
            if (services is null)
            {
                FooterText.Text = "Chưa đăng ký service phiếu thu; không hiển thị dữ liệu mẫu.";
                return;
            }

            _dataScope = services.CreateScope();
            Closed += OnWindowClosed;
            _realLoadTask = LoadRealDataAsync(_dataScope?.ServiceProvider.GetService<PaymentReceiptsViewModel>());
        }
    }

    private async Task LoadRealDataAsync(PaymentReceiptsViewModel? viewModel)
    {
        if (viewModel is null)
        {
            FooterText.Text = "Chưa đăng ký service phiếu thu; không hiển thị dữ liệu mẫu.";
            return;
        }

        _realViewModel = viewModel;
        _realViewModel.Receipts.CollectionChanged += OnRealReceiptsChanged;

        try
        {
            // The VM starts its initial load in the constructor. Await that
            // exact operation so a second DbContext query cannot race it.
            await _realViewModel.WaitForInitializationAsync();
            if (!_isClosed)
                SyncRealReceipts();
        }
        catch (Exception ex)
        {
            if (!_isClosed)
                FooterText.Text = $"Không tải được phiếu thu thật: {ex.Message}";
        }
        finally
        {
            if (_isClosed)
                DisposeDataScope();
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        if (_realViewModel is not null)
            _realViewModel.Receipts.CollectionChanged -= OnRealReceiptsChanged;
        if (_realLoadTask is null || _realLoadTask.IsCompleted)
            DisposeDataScope();
    }

    private void DisposeDataScope()
    {
        _dataScope?.Dispose();
        _dataScope = null;
    }

    private void OnRealReceiptsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Dispatcher.CheckAccess())
            SyncRealReceipts();
        else
            Dispatcher.BeginInvoke(new Action(SyncRealReceipts));
    }

    private void SyncRealReceipts()
    {
        if (_realViewModel is null) return;
        _receipts = _realViewModel.AllRows.Select(row =>
        {
            var amount = row.Amount < 0 ? 0 : row.Amount > int.MaxValue ? int.MaxValue : (int)row.Amount;
            var status = row.Status switch
            {
                "Lập phiếu" => "Draft",
                "Đã duyệt" => "Issued",
                "Đã hủy" => "Cancelled",
                "Đã điều chỉnh" => "Adjusted",
                _ => "Pending"
            };
            return new RvReceipt(
                row.ReceiptId,
                row.DocumentNo,
                row.Date,
                "Thu khác",
                status,
                string.IsNullOrWhiteSpace(row.CustomerName) ? "—" : row.CustomerName,
                null,
                "—",
                "—",
                "—",
                string.IsNullOrWhiteSpace(row.Method) ? "—" : row.Method,
                "VND",
                amount,
                0,
                amount,
                "—",
                "—");
        }).ToList();
        _selected = null;
        BuildKpi();
        BuildNavPanel();
        RefreshPage(true);
        FooterText.Text = $"Đã tải {_receipts.Count:N0} phiếu thu từ CSDL thật.";
    }

    private void InitFilters()
    {
        TypeFilter.ItemsSource = new[] { "Tất cả", "Thu Folio", "Thu đặt cọc", "Thu công nợ", "Thu đại lý", "Thu đoàn", "Thu tạm ứng" };
        TypeFilter.SelectedIndex = 0;
        StFilter.ItemsSource = new[] { "Tất cả", "Draft", "Pending", "Issued", "Printed", "Sent", "Adjusted", "Cancelled" };
        StFilter.SelectedIndex = 0;
        MethodFilter.ItemsSource = new[] { "Tất cả", "Tiền mặt", "Chuyển khoản", "Thẻ", "QR", "Công ty" };
        MethodFilter.SelectedIndex = 0;
        PageSizeBox.ItemsSource = new[] { "50 / trang", "100 / trang", "200 / trang" }; PageSizeBox.SelectedIndex = 0;
    }

    private void BuildKpi()
    {
        KpiStrip.Children.Clear();
        var kpis = new (string, int, string)[] {
            ("Tổng phiếu", _receipts.Count, "#2F6BFF"), ("Đã phát hành", _receipts.Count(r => r.Status == "Issued"), "#16A34A"),
            ("Chờ xác nhận", _receipts.Count(r => r.Status is "Draft" or "Pending"), "#F59E0B"), ("Đã hủy", _receipts.Count(r => r.Status == "Cancelled"), "#DC2626"),
            ("Tiền mặt", _receipts.Count(r => r.Method == "Tiền mặt"), "#8B5CF6"), ("Chuyển khoản", _receipts.Count(r => r.Method is "Chuyển khoản" or "Thẻ"), "#06B6D4"),
            ("Đã điều chỉnh", _receipts.Count(r => r.Status == "Adjusted"), "#F59E0B")
        };
        foreach (var (label, value, color) in kpis) KpiStrip.Children.Add(Kpi(label, $"{value}", color));
    }

    private void BuildNavPanel()
    {
        NavPanel.Children.Clear();
        NavPanel.Children.Add(new TextBlock { Text = "TRẠNG THÁI", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 6) });
        var statuses = new[] { ("Tất cả", "All"), ("Nháp", "Draft"), ("Chờ xác nhận", "Pending"), ("Đã phát hành", "Issued"), ("Đã in", "Printed"), ("Đã gửi", "Sent"), ("Đã điều chỉnh", "Adjusted"), ("Đã hủy", "Cancelled") };
        foreach (var (label, key) in statuses) AddNavItem(label, key, key == "All" ? _receipts.Count : _receipts.Count(r => r.Status == key));

        NavPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        NavPanel.Children.Add(new TextBlock { Text = "NGUỒN THU", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 6) });
        var types = new[] { "Thu Folio", "Thu đặt cọc", "Thu công nợ", "Thu đại lý", "Thu đoàn", "Thu tạm ứng", "Thu khác" };
        foreach (var t in types) AddNavItem(t, t, _receipts.Count(r => r.Type == t));
    }

    private void AddNavItem(string label, string key, int count)
    {
        var active = _statusFilter == key;
        var b = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569"), Width = 158, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = label }, new TextBlock { Text = $"{count:N0}", FontSize = 11, Foreground = C("#64748B"), Width = 45, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left } } } };
        b.MouseLeftButtonDown += (_, _) => { _statusFilter = key; BuildNavPanel(); RefreshPage(true); };
        NavPanel.Children.Add(b);
    }

    private List<RvReceipt> GetFiltered()
    {
        var r2 = _receipts.AsEnumerable(); var kw = SearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) r2 = r2.Where(x => x.Code.ToLower().Contains(kw) || x.Payer.ToLower().Contains(kw) || (x.Company?.ToLower().Contains(kw) ?? false) || x.Room.Contains(kw));
        var type = TypeFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(type) && type != "Tất cả") r2 = r2.Where(x => x.Type == type);
        var st = StFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(st) && st != "Tất cả") r2 = r2.Where(x => x.Status == st);
        var method = MethodFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(method) && method != "Tất cả") r2 = r2.Where(x => x.Method == method);
        if (_statusFilter != "All" && new[] { "Draft", "Pending", "Issued", "Printed", "Sent", "Adjusted", "Cancelled" }.Contains(_statusFilter)) r2 = r2.Where(x => x.Status == _statusFilter);
        else if (!new[] { "All", "Draft", "Pending", "Issued", "Printed", "Sent", "Adjusted", "Cancelled" }.Contains(_statusFilter)) r2 = r2.Where(x => x.Type == _statusFilter);
        return r2.ToList();
    }

    private void RefreshPage(bool reset)
    {
        if (reset) _page = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _pageSize - 1) / _pageSize);
        if (_page >= tp) _page = tp - 1; GridPanel.Children.Clear();
        var cw = new[] { 65.0, 90.0, 72.0, 80.0, 120.0, 90.0, 42.0, 80.0, 80.0, 80.0, 55.0, 65.0, 70.0 };
        var ch = new[] { "T.Thái", "Số phiếu", "Ngày", "Loại", "Người nộp", "Công ty", "Phòng", "Reservation", "Folio", "Phương thức", "Tiền tệ", "Số tiền", "Người lập" };
        var hdr = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        for (int j = 0; j < cw.Length; j++) { hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) }); AddHdr(hdr, j, ch[j]); }
        GridPanel.Children.Add(hdr);

        foreach (var r3 in f.Skip(_page * _pageSize).Take(_pageSize))
        {
            var active = _selected?.Id == r3.Id;
            var stColor = r3.Status switch { "Issued" => "#16A34A", "Draft" => "#94A3B8", "Pending" => "#F59E0B", "Cancelled" => "#DC2626", "Adjusted" => "#8B5CF6", _ => "#64748B" };
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1), Background = active ? Cb("#EFF6FF") : r3.Status == "Cancelled" ? Cb("#FFF5F5") : Cb("Transparent") };
            for (int j = 0; j < cw.Length; j++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) });
            var vals = new[] { r3.Status, r3.Code, r3.Date.ToString("dd/MM/yy"), r3.Type, r3.Payer, r3.Company ?? "—", r3.Room, r3.Reservation, r3.Folio, r3.Method, r3.Currency, $"{r3.Amount:N0}", r3.CreatedBy };
            var colors = new[] { stColor, "#64748B", "#64748B", "#64748B", "#17365D", "#64748B", "#64748B", "#64748B", "#64748B", "#64748B", "#64748B", "#17365D", "#64748B" };
            for (int j = 0; j < vals.Length; j++) AddCell(row, j, vals[j], colors[j], j == 4 || j == 11);
            var rid = r3.Id; row.Cursor = System.Windows.Input.Cursors.Hand;
            row.MouseLeftButtonDown += (_, _) => { _selected = r3; RefreshPage(false); BuildInspector(); };
            GridPanel.Children.Add(row);
        }
        PagerSummary.Text = $"{_page * _pageSize + 1}–{Math.Min((_page + 1) * _pageSize, f.Count)} / {f.Count:N0}";
        PrevBtn.IsEnabled = _page > 0; NextBtn.IsEnabled = _page < tp - 1; BuildPageBtns(tp);
        // 3,000 receipts can exceed Int32 when their amounts are aggregated.
        // Keep the row model as int, but promote the total before summing.
        var filteredAmountTotal = f.Sum(r3 => (long)r3.Amount);
        FooterText.Text = $"Tổng: {f.Count:N0} · Tổng tiền: {filteredAmountTotal:N0}đ · Tiền mặt: {f.Count(r3 => r3.Method == "Tiền mặt")} · Đã hủy: {f.Count(r3 => r3.Status == "Cancelled")}";
    }

    private void BuildPageBtns(int tp) { PageBtns.Children.Clear(); for (int i = 0; i < Math.Min(tp, 10); i++) { var idx = i; var nb = PagerBtn(i, _page, idx); nb.Click += (_, _) => { _page = idx; RefreshPage(false); }; PageBtns.Children.Add(nb); } }
    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 26, Height = 24, FontSize = 10, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = PrevBtn.Template };
    private static void AddHdr(Grid g, int col, string text) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }
    private static void AddCell(Grid g, int col, string text, string color, bool bold) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Người nộp", "Nguồn thu", "Phân bổ", "Preview", "Timeline", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _inspectorTab; var b = new Border { CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(3, 3, 3, 3), Margin = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("White") : Cb("Transparent"), BorderBrush = active ? Cb("#E2E8F0") : Cb("Transparent"), BorderThickness = new Thickness(1, 1, 1, 0), Child = new TextBlock { Text = tabs[i], FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#2F6BFF") : C("#64748B") } }; b.MouseLeftButtonDown += (_, _) => { _inspectorTab = idx; InitInspectorTabs(); BuildInspector(); }; InspectorTabs.Children.Add(b); }
        BuildInspector();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_selected == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn một phiếu thu để xem nguồn tiền, phân bổ, preview và lịch sử.", FontSize = 12, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap }); return; }
        var r3 = _selected;
        var stColor = r3.Status switch { "Issued" => "#16A34A", "Draft" => "#94A3B8", "Pending" => "#F59E0B", "Cancelled" => "#DC2626", _ => "#64748B" };

        switch (_inspectorTab)
        {
            case 0:
                InspectorPanel.Children.Add(STitle(r3.Code));
                InspectorPanel.Children.Add(new Border { Background = Cb(stColor), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 0, 6), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = r3.Status, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
                InspectorPanel.Children.Add(Row("Loại", r3.Type)); InspectorPanel.Children.Add(Row("Ngày", r3.Date.ToString("dd/MM/yyyy")));
                InspectorPanel.Children.Add(Row("Phương thức", r3.Method)); InspectorPanel.Children.Add(Row("Currency", r3.Currency));
                InspectorPanel.Children.Add(Row("Số tiền", $"{r3.Amount:N0}")); InspectorPanel.Children.Add(Row("Đã phân bổ", $"{r3.Allocated:N0}"));
                InspectorPanel.Children.Add(Row("Chưa phân bổ", $"{r3.Unallocated:N0}"));
                InspectorPanel.Children.Add(Row("Người lập", r3.CreatedBy)); InspectorPanel.Children.Add(Row("Ca", r3.Shift));
                break;
            case 1:
                InspectorPanel.Children.Add(STitle("Người nộp"));
                InspectorPanel.Children.Add(Row("Tên", r3.Payer));
                if (r3.Company != null) InspectorPanel.Children.Add(Row("Công ty", r3.Company));
                InspectorPanel.Children.Add(Row("Phòng", r3.Room));
                InspectorPanel.Children.Add(Row("Reservation", r3.Reservation));
                InspectorPanel.Children.Add(Row("Folio", r3.Folio));
                break;
            case 2:
                InspectorPanel.Children.Add(STitle("Nguồn thu"));
                InspectorPanel.Children.Add(Row("Loại", r3.Type)); InspectorPanel.Children.Add(Row("Folio", r3.Folio));
                InspectorPanel.Children.Add(Row("Reservation", r3.Reservation)); InspectorPanel.Children.Add(Row("Phòng", r3.Room));
                break;
            case 3:
                InspectorPanel.Children.Add(STitle("Phân bổ"));
                InspectorPanel.Children.Add(Row("Tổng phiếu", $"{r3.Amount:N0}"));
                InspectorPanel.Children.Add(Row("Đã phân bổ", $"{r3.Allocated:N0}"));
                InspectorPanel.Children.Add(Row("Còn lại", $"{r3.Unallocated:N0}"));
                var status = r3.Unallocated == 0 ? "Đã phân bổ đủ" : r3.Unallocated < r3.Amount ? "Phân bổ một phần" : "Chưa phân bổ";
                InspectorPanel.Children.Add(new Border { Background = r3.Unallocated == 0 ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = status, FontSize = 11, Foreground = r3.Unallocated == 0 ? C("#16A34A") : C("#A16207") } });
                break;
            case 4:
                InspectorPanel.Children.Add(STitle("Preview phiếu thu"));
                InspectorPanel.Children.Add(new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(20), Child = new StackPanel { Children = { new TextBlock { Text = "PHIẾU THU", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) }, new TextBlock { Text = $"Số: {r3.Code}", FontSize = 13, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) }, Row("Người nộp", r3.Payer), Row("Số tiền", $"{r3.Amount:N0} {r3.Currency}"), Row("Phương thức", r3.Method), Row("Ngày", r3.Date.ToString("dd/MM/yyyy")), Row("Người lập", r3.CreatedBy) } } });
                break;
            case 5:
                InspectorPanel.Children.Add(STitle("Timeline"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ {r3.Date:dd/MM HH:mm} — Tạo — {r3.CreatedBy}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                if (r3.Status is "Issued" or "Printed") InspectorPanel.Children.Add(new TextBlock { Text = "⏱ — Phát hành", FontSize = 11, Foreground = C("#16A34A"), Margin = new Thickness(0, 2, 0, 0) });
                if (r3.Status == "Cancelled") InspectorPanel.Children.Add(new TextBlock { Text = "⏱ — Hủy", FontSize = 11, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 6:
                InspectorPanel.Children.Add(STitle("Lịch sử"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"{r3.Date:dd/MM HH:mm} — Tạo — {r3.CreatedBy} · Ca {r3.Shift}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                break;
        }
    }

    private void OnNewReceipt(object s, RoutedEventArgs e)
    {
        var existing = System.Windows.Application.Current.Windows.OfType<CreateReceiptVoucherWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { existing.Activate(); return; }
        new CreateReceiptVoucherWindow { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
    }
    private void OnQuickToday(object s, RoutedEventArgs e) => RefreshPage(true);
    private void OnQuickIssued(object s, RoutedEventArgs e) { StFilter.SelectedIndex = StFilter.ItemsSource is string[] sts ? Array.IndexOf(sts, "Issued") : 0; RefreshPage(true); }
    private void OnApplyFilter(object s, RoutedEventArgs e) => RefreshPage(true);
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = ""; TypeFilter.SelectedIndex = 0; StFilter.SelectedIndex = 0; MethodFilter.SelectedIndex = 0; _statusFilter = "All"; BuildNavPanel(); RefreshPage(true); }
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_page > 0) { _page--; RefreshPage(false); } }
    private void OnNextPage(object s, RoutedEventArgs e) { _page++; RefreshPage(false); }
    private void OnPageSizeChanged(object s, SelectionChangedEventArgs e) { _pageSize = PageSizeBox.SelectedIndex switch { 0 => 50, 1 => 100, 2 => 200, _ => 50 }; RefreshPage(true); }
    private async void OnRefresh(object s, RoutedEventArgs e)
    {
        _selected = null;
        InitFilters();
        InitInspectorTabs();
        if (_realViewModel is not null)
            await _realViewModel.RefreshAsync();
        else
            RefreshPage(true);
    }
    private void OnPrint(object s, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            ShowUnavailable("In phiếu thu: hãy chọn một phiếu thật trước");
            return;
        }

        var content = string.Join(Environment.NewLine, new[]
        {
            $"Số phiếu: {_selected.Code}",
            $"Ngày thu: {_selected.Date:dd/MM/yyyy}",
            $"Người nộp: {_selected.Payer}",
            $"Số tiền: {_selected.Amount:N0} {_selected.Currency}",
            $"Phương thức: {_selected.Method}",
            $"Trạng thái: {_selected.Status}",
            $"Người lập: {_selected.CreatedBy}"
        });
        new PrintPreviewDialog("PHIẾU THU", content)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();
    }

    private void OnExport(object s, RoutedEventArgs e)
    {
        var rows = GetFiltered();
        if (rows.Count == 0)
        {
            ShowUnavailable("Xuất phiếu thu: không có dữ liệu thật phù hợp bộ lọc");
            return;
        }

        var exportRows = rows.Select(row => (IReadOnlyList<string>)new[]
        {
            row.Date.ToString("yyyy-MM-dd"), row.Code, row.Payer, row.Amount.ToString("0.##"),
            row.Currency, row.Method, row.Status, row.CreatedBy
        }).ToList();
        var path = CsvExportHelper.Export("phieu-thu-shell",
            new[] { "Ngày thu", "Số phiếu", "Người nộp", "Số tiền", "Tiền tệ", "Phương thức", "Trạng thái", "Người lập" },
            exportRows);
        MessageBox.Show($"Đã xuất {rows.Count:N0} phiếu thu thật:\n{path}", "Phiếu thu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnAdjust(object s, RoutedEventArgs e)
    {
        if (_selected is null || _realViewModel is null || !_realViewModel.SelectReceiptByDocumentNo(_selected.Code))
        {
            ShowUnavailable("Điều chỉnh phiếu thu: hãy chọn một phiếu thật trước");
            return;
        }

        // The real workflow deliberately prevents in-place edits after saving.
        // It instructs the cashier to cancel and recreate, preserving audit data.
        _realViewModel.EditSelectedCommand.Execute(null);
    }

    private async void OnCancel(object s, RoutedEventArgs e)
    {
        if (_selected is null || _realViewModel is null)
        {
            ShowUnavailable("Hủy phiếu thu: hãy chọn một phiếu thật trước");
            return;
        }

        await _realViewModel.CancelReceiptByDocumentNoAsync(_selected.Code);
    }
    private void OnClose(object s, RoutedEventArgs e) => Close();
    private static void ShowUnavailable(string action) => MessageBox.Show($"{action}. Không có dữ liệu nào được ghi.", "Phiếu thu", MessageBoxButton.OK, MessageBoxImage.Information);

    private static FrameworkElement Kpi(string l, string v, string color) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 6) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsRv(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record RvReceipt(string Id, string Code, DateTime Date, string Type, string Status, string Payer, string? Company, string Room, string Reservation, string Folio, string Method, string Currency, int Amount, int Allocated, int Unallocated, string CreatedBy, string Shift);
internal static class RvExt { public static TextBlock WithRunsRv(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
