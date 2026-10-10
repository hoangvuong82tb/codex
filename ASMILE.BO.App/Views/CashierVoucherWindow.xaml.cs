using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class CashierVoucherWindow : Window
{
    private List<PvVoucher> _vouchers = new();
    private PvVoucher? _selected;
    private int _page, _pageSize = 50;
    private int _inspectorTab;
    private string _statusFilter = "All";
    private PaymentVouchersViewModel? _realViewModel;
    private IServiceScope? _dataScope;
    private Task? _realLoadTask;
    private bool _isClosed;

    public CashierVoucherWindow() : this(forceMock: false)
    {
    }

    /// <summary>
    /// The optional design/test flag is retained for isolated hosts only.
    /// The parameterless production route keeps the real PaymentVouchersView integration.
    /// </summary>
    public CashierVoucherWindow(bool forceMock)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _vouchers = new List<PvVoucher>();
        InitFilters();
        BuildKpi();
        BuildNavPanel();
        InitInspectorTabs();
        RefreshPage(true);
        FooterText.Text = forceMock
            ? "Chế độ thiết kế — không hiển thị dữ liệu mẫu."
            : "Đang tải phiếu chi từ CSDL thật…";
        if (!forceMock)
        {
            // This window is created from a shell, not by the scoped navigation
            // host. Give it its own scope so its EF DbContext cannot be shared
            // with another cashier window or a root-provider resolution.
            var services = (System.Windows.Application.Current as App)?.Services;
            if (services is null)
            {
                FooterText.Text = "Chưa đăng ký service phiếu chi; không hiển thị dữ liệu mẫu.";
                return;
            }

            _dataScope = services.CreateScope();
            Closed += OnWindowClosed;
            _realLoadTask = LoadRealDataAsync(_dataScope?.ServiceProvider.GetService<PaymentVouchersViewModel>());
        }
    }

    private async Task LoadRealDataAsync(PaymentVouchersViewModel? viewModel)
    {
        if (viewModel is null)
        {
            FooterText.Text = "Chưa đăng ký service phiếu chi; không hiển thị dữ liệu mẫu.";
            return;
        }

        _realViewModel = viewModel;
        _realViewModel.Vouchers.CollectionChanged += OnRealVouchersChanged;

        try
        {
            // The VM starts its initial load in the constructor. Await that
            // exact operation so a second DbContext query cannot race it.
            await _realViewModel.WaitForInitializationAsync();
            if (!_isClosed)
                SyncRealVouchers();
        }
        catch (Exception ex)
        {
            if (!_isClosed)
                FooterText.Text = $"Không tải được phiếu chi thật: {ex.Message}";
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
            _realViewModel.Vouchers.CollectionChanged -= OnRealVouchersChanged;
        if (_realLoadTask is null || _realLoadTask.IsCompleted)
            DisposeDataScope();
    }

    private void DisposeDataScope()
    {
        _dataScope?.Dispose();
        _dataScope = null;
    }

    private void OnRealVouchersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Dispatcher.CheckAccess())
            SyncRealVouchers();
        else
            Dispatcher.BeginInvoke(new Action(SyncRealVouchers));
    }

    private void SyncRealVouchers()
    {
        if (_realViewModel is null) return;
        _vouchers = _realViewModel.AllRows.Select(row =>
        {
            var amount = row.Amount < 0 ? 0 : row.Amount > int.MaxValue ? int.MaxValue : (int)row.Amount;
            var isApproved = row.Status == "Đã duyệt" || row.WorkflowStatus.ToString() == "Approved";
            var isRejected = row.Status == "Từ chối";
            var isCancelled = row.Status == "Đã hủy" || row.WorkflowStatus == VoucherWorkflowStatus.Cancelled;
            var status = isCancelled ? "Cancelled" : isRejected ? "Rejected" : isApproved ? "Approved" : "Pending Approval";
            return new PvVoucher(
                row.DocumentNo,
                row.DocumentNo,
                row.Date,
                "Chi khác",
                status,
                string.IsNullOrWhiteSpace(row.SupplierName) ? "—" : row.SupplierName,
                null,
                null,
                string.IsNullOrWhiteSpace(row.Method) ? "—" : row.Method,
                "VND",
                amount,
                0,
                amount,
                isCancelled ? "Đã hủy" : isRejected ? "Từ chối" : isApproved ? "Đã duyệt" : "Chờ duyệt",
                "—",
                null);
        }).ToList();
        _selected = null;
        BuildKpi();
        BuildNavPanel();
        RefreshPage(true);
        FooterText.Text = $"Đã tải {_vouchers.Count:N0} phiếu chi từ CSDL thật.";
    }


    private void InitFilters()
    {
        TypeFilter.ItemsSource = new[] { "Tất cả", "Hoàn tiền khách", "Hoàn đặt cọc", "Hoàn Folio dư", "Tạm ứng", "Nhà cung cấp", "Hoa hồng ĐL", "Chi vận hành" };
        TypeFilter.SelectedIndex = 0;
        StFilter.ItemsSource = new[] { "Tất cả", "Draft", "Pending Approval", "Approved", "Issued", "Rejected", "Adjusted", "Cancelled" };
        StFilter.SelectedIndex = 0;
        PageSizeBox.ItemsSource = new[] { "50 / trang", "100 / trang", "200 / trang" }; PageSizeBox.SelectedIndex = 0;
    }

    private void BuildKpi()
    {
        KpiStrip.Children.Clear();
        var kpis = new (string, int, string)[] {
            ("Tổng phiếu", _vouchers.Count, "#2F6BFF"), ("Chờ duyệt", _vouchers.Count(v => v.Status == "Pending Approval" || v.Approval == "Chờ duyệt"), "#F59E0B"),
            ("Đã duyệt/Phát hành", _vouchers.Count(v => v.Status is "Approved" or "Issued"), "#16A34A"), ("Bị từ chối", _vouchers.Count(v => v.Status == "Rejected"), "#DC2626"),
            ("Tiền mặt", _vouchers.Count(v => v.Method == "Tiền mặt"), "#8B5CF6"), ("Chuyển khoản", _vouchers.Count(v => v.Method == "Chuyển khoản"), "#06B6D4"),
            ("Đã hủy", _vouchers.Count(v => v.Status == "Cancelled"), "#DC2626")
        };
        foreach (var (label, value, color) in kpis) KpiStrip.Children.Add(Kpi(label, $"{value}", color));
    }

    private void BuildNavPanel()
    {
        NavPanel.Children.Clear();
        NavPanel.Children.Add(new TextBlock { Text = "TRẠNG THÁI", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 6) });
        var statuses = new[] { ("Tất cả", "All"), ("Nháp", "Draft"), ("Chờ duyệt", "Pending Approval"), ("Đã duyệt", "Approved"), ("Đã phát hành", "Issued"), ("Bị từ chối", "Rejected"), ("Đã điều chỉnh", "Adjusted"), ("Đã hủy", "Cancelled") };
        foreach (var (label, key) in statuses) AddNavItem(label, key, key == "All" ? _vouchers.Count : _vouchers.Count(v => v.Status == key));
        NavPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        NavPanel.Children.Add(new TextBlock { Text = "NGUỒN CHI", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 6) });
        var types = new[] { "Hoàn tiền khách", "Hoàn đặt cọc", "Tạm ứng", "Nhà cung cấp", "Hoa hồng ĐL", "Chi vận hành", "Chi khác" };
        foreach (var t in types) AddNavItem(t, t, _vouchers.Count(v => v.Type == t));
    }

    private void AddNavItem(string label, string key, int count)
    {
        var active = _statusFilter == key;
        var b = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569"), Width = 158, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = label }, new TextBlock { Text = $"{count:N0}", FontSize = 11, Foreground = C("#64748B"), Width = 45, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left } } } };
        b.MouseLeftButtonDown += (_, _) => { _statusFilter = key; BuildNavPanel(); RefreshPage(true); }; NavPanel.Children.Add(b);
    }

    private List<PvVoucher> GetFiltered()
    {
        var v = _vouchers.AsEnumerable(); var kw = SearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) v = v.Where(x => x.Code.ToLower().Contains(kw) || x.Recipient.ToLower().Contains(kw) || (x.Reservation?.ToLower().Contains(kw) ?? false));
        var type = TypeFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(type) && type != "Tất cả") v = v.Where(x => x.Type == type);
        var st = StFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(st) && st != "Tất cả") v = v.Where(x => x.Status == st);
        if (_statusFilter != "All" && new[] { "Draft", "Pending Approval", "Approved", "Issued", "Rejected", "Adjusted", "Cancelled" }.Contains(_statusFilter)) v = v.Where(x => x.Status == _statusFilter);
        else if (!new[] { "All", "Draft", "Pending Approval", "Approved", "Issued", "Rejected", "Adjusted", "Cancelled" }.Contains(_statusFilter)) v = v.Where(x => x.Type == _statusFilter);
        return v.ToList();
    }

    private void RefreshPage(bool reset)
    {
        if (reset) _page = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _pageSize - 1) / _pageSize);
        if (_page >= tp) _page = tp - 1; GridPanel.Children.Clear();
        var cw = new[] { 70.0, 90.0, 72.0, 90.0, 100.0, 90.0, 42.0, 75.0, 75.0, 80.0, 55.0, 65.0 };
        var ch = new[] { "T.Thái", "Số phiếu", "Ngày", "Loại", "Người nhận", "Res/Folio", "PT", "Phương thức", "Số tiền", "Phân bổ", "Phê duyệt", "Người lập" };
        var hdr = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        for (int j = 0; j < cw.Length; j++) { hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) }); AddHdr(hdr, j, ch[j]); }
        GridPanel.Children.Add(hdr);

        foreach (var v2 in f.Skip(_page * _pageSize).Take(_pageSize))
        {
            var active = _selected?.Id == v2.Id;
            var stColor = v2.Status switch { "Issued" or "Approved" => "#16A34A", "Draft" => "#94A3B8", "Pending Approval" => "#F59E0B", "Cancelled" => "#DC2626", "Rejected" => "#DC2626", "Adjusted" => "#8B5CF6", _ => "#64748B" };
            var appColor = v2.Approval switch { "Đã duyệt" or "Bỏ qua" => "#16A34A", "Chờ duyệt" => "#F59E0B", "Từ chối" => "#DC2626", _ => "#94A3B8" };
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1), Background = active ? Cb("#EFF6FF") : v2.Status == "Rejected" || v2.Status == "Cancelled" ? Cb("#FFF5F5") : Cb("Transparent") };
            for (int j = 0; j < cw.Length; j++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) });
            var vals = new[] { v2.Status, v2.Code, v2.Date.ToString("dd/MM/yy"), v2.Type, v2.Recipient, v2.Reservation ?? v2.Folio ?? "—", v2.Approval, v2.Method, $"{v2.Amount:N0}", $"{v2.Allocated:N0}", v2.Approval, v2.CreatedBy };
            var colors = new[] { stColor, "#64748B", "#64748B", "#64748B", "#17365D", "#64748B", appColor, "#64748B", "#17365D", "#64748B", appColor, "#64748B" };
            for (int j = 0; j < vals.Length; j++) AddCell(row, j, vals[j], colors[j], j == 4 || j == 8);
            var vid = v2.Id; row.Cursor = System.Windows.Input.Cursors.Hand;
            row.MouseLeftButtonDown += (_, _) => { _selected = v2; RefreshPage(false); BuildInspector(); };
            GridPanel.Children.Add(row);
        }
        PagerSummary.Text = $"{_page * _pageSize + 1}–{Math.Min((_page + 1) * _pageSize, f.Count)} / {f.Count:N0}";
        PrevBtn.IsEnabled = _page > 0; NextBtn.IsEnabled = _page < tp - 1; BuildPageBtns(tp);
        FooterText.Text = $"Tổng: {f.Count:N0} · Tổng tiền: {f.Sum(v2 => (long)v2.Amount):N0}đ · Chờ duyệt: {f.Count(v2 => v2.Status == "Pending Approval")} · Hủy: {f.Count(v2 => v2.Status == "Cancelled")}";
    }

    private void BuildPageBtns(int tp) { PageBtns.Children.Clear(); for (int i = 0; i < Math.Min(tp, 10); i++) { var idx = i; var nb = PagerBtn(i, _page, idx); nb.Click += (_, _) => { _page = idx; RefreshPage(false); }; PageBtns.Children.Add(nb); } }
    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 26, Height = 24, FontSize = 10, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = PrevBtn.Template };
    private static void AddHdr(Grid g, int col, string text) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }
    private static void AddCell(Grid g, int col, string text, string color, bool bold) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Người nhận", "Nguồn chi", "Phân bổ", "Phê duyệt", "Preview", "Timeline", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _inspectorTab; var b = new Border { CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(3, 3, 3, 3), Margin = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("White") : Cb("Transparent"), BorderBrush = active ? Cb("#E2E8F0") : Cb("Transparent"), BorderThickness = new Thickness(1, 1, 1, 0), Child = new TextBlock { Text = tabs[i], FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#2F6BFF") : C("#64748B") } }; b.MouseLeftButtonDown += (_, _) => { _inspectorTab = idx; InitInspectorTabs(); BuildInspector(); }; InspectorTabs.Children.Add(b); }
        BuildInspector();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_selected == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn một phiếu chi để xem người nhận, nguồn chi, phê duyệt và lịch sử.", FontSize = 12, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap }); return; }
        var v = _selected;
        var stColor = v.Status switch { "Issued" or "Approved" => "#16A34A", "Draft" => "#94A3B8", "Pending Approval" => "#F59E0B", "Cancelled" or "Rejected" => "#DC2626", _ => "#64748B" };

        switch (_inspectorTab)
        {
            case 0:
                InspectorPanel.Children.Add(STitle(v.Code));
                InspectorPanel.Children.Add(new Border { Background = Cb(stColor), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 0, 6), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = v.Status, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
                InspectorPanel.Children.Add(Row("Loại", v.Type)); InspectorPanel.Children.Add(Row("Ngày", v.Date.ToString("dd/MM/yyyy")));
                InspectorPanel.Children.Add(Row("Phương thức", v.Method)); InspectorPanel.Children.Add(Row("Currency", v.Currency));
                InspectorPanel.Children.Add(Row("Số tiền", $"{v.Amount:N0}")); InspectorPanel.Children.Add(Row("Đã phân bổ", $"{v.Allocated:N0}"));
                InspectorPanel.Children.Add(Row("Phê duyệt", v.Approval)); InspectorPanel.Children.Add(Row("Người lập", v.CreatedBy));
                if (v.Approver != null) InspectorPanel.Children.Add(Row("Người duyệt", v.Approver));
                break;
            case 1:
                InspectorPanel.Children.Add(STitle("Người nhận"));
                InspectorPanel.Children.Add(Row("Tên", v.Recipient));
                if (v.Reservation != null) InspectorPanel.Children.Add(Row("Reservation", v.Reservation));
                if (v.Folio != null) InspectorPanel.Children.Add(Row("Folio", v.Folio));
                break;
            case 2:
                InspectorPanel.Children.Add(STitle("Nguồn chi"));
                InspectorPanel.Children.Add(Row("Loại", v.Type));
                if (v.Reservation != null) InspectorPanel.Children.Add(Row("Reservation", v.Reservation));
                if (v.Folio != null) InspectorPanel.Children.Add(Row("Folio", v.Folio));
                InspectorPanel.Children.Add(Row("Trạng thái duyệt", v.Approval));
                break;
            case 3:
                InspectorPanel.Children.Add(STitle("Phân bổ"));
                InspectorPanel.Children.Add(Row("Tổng phiếu", $"{v.Amount:N0}"));
                InspectorPanel.Children.Add(Row("Đã phân bổ", $"{v.Allocated:N0}"));
                InspectorPanel.Children.Add(Row("Còn lại", $"{v.Unallocated:N0}"));
                var allocStatus = v.Unallocated == 0 ? "Đã phân bổ đủ" : "Phân bổ một phần";
                InspectorPanel.Children.Add(new Border { Background = v.Unallocated == 0 ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = allocStatus, FontSize = 11, Foreground = v.Unallocated == 0 ? C("#16A34A") : C("#A16207") } });
                break;
            case 4:
                InspectorPanel.Children.Add(STitle("Phê duyệt"));
                InspectorPanel.Children.Add(Row("Trạng thái", v.Approval));
                InspectorPanel.Children.Add(Row("Người lập", v.CreatedBy));
                if (v.Approver != null) InspectorPanel.Children.Add(Row("Người duyệt", v.Approver));
                break;
            case 5:
                InspectorPanel.Children.Add(STitle("Preview phiếu chi"));
                InspectorPanel.Children.Add(new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(20), Child = new StackPanel { Children = { new TextBlock { Text = "PHIẾU CHI", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) }, new TextBlock { Text = $"Số: {v.Code}", FontSize = 13, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) }, Row("Người nhận", v.Recipient), Row("Số tiền", $"{v.Amount:N0} {v.Currency}"), Row("Phương thức", v.Method), Row("Ngày", v.Date.ToString("dd/MM/yyyy")), Row("Người lập", v.CreatedBy) } } });
                break;
            case 6:
                InspectorPanel.Children.Add(STitle("Timeline"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ {v.Date:dd/MM HH:mm} — Tạo — {v.CreatedBy}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                if (v.Approval is "Đã duyệt" or "Bỏ qua") InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ — Phê duyệt — {v.Approver ?? "—"}", FontSize = 11, Foreground = C("#16A34A"), Margin = new Thickness(0, 2, 0, 0) });
                if (v.Status == "Rejected") InspectorPanel.Children.Add(new TextBlock { Text = "⏱ — Từ chối", FontSize = 11, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 7:
                InspectorPanel.Children.Add(STitle("Lịch sử"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"{v.Date:dd/MM HH:mm} — Tạo — {v.CreatedBy}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                break;
        }
    }

    private async void OnNewVoucher(object s, RoutedEventArgs e)
    {
        var vm = _realViewModel;
        if (vm is null)
        {
            Show("Lập phiếu chi: chưa đăng ký ViewModel dữ liệu thật");
            return;
        }

        await vm.WaitForInitializationAsync();
        var view = new PaymentVouchersView { DataContext = vm };
        var window = new Window
        {
            Title = "Lập phiếu chi",
            Width = 1280,
            Height = 820,
            MinWidth = 1000,
            MinHeight = 650,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = view
        };
        window.ShowDialog();
        await vm.RefreshAsync();
    }

    private async void OnApprove(object s, RoutedEventArgs e)
    {
        if (_selected is null || _realViewModel is null)
        {
            Show("Phê duyệt phiếu chi: hãy chọn một phiếu thật trước");
            return;
        }

        await _realViewModel.ApproveVoucherByDocumentNoAsync(_selected.Code);
    }

    private async void OnReject(object s, RoutedEventArgs e)
    {
        if (_selected is null || _realViewModel is null)
        {
            Show("Từ chối phiếu chi: hãy chọn một phiếu thật trước");
            return;
        }

        var reason = PromptForRejectReason();
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        await _realViewModel.RejectVoucherByDocumentNoAsync(_selected.Code, reason);
    }
    private void OnApplyFilter(object s, RoutedEventArgs e) => RefreshPage(true);
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = ""; TypeFilter.SelectedIndex = 0; StFilter.SelectedIndex = 0; _statusFilter = "All"; BuildNavPanel(); RefreshPage(true); }
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
            Show("In phiếu chi: hãy chọn một phiếu thật trước");
            return;
        }

        var content = string.Join(Environment.NewLine, new[]
        {
            $"Số phiếu: {_selected.Code}",
            $"Ngày chi: {_selected.Date:dd/MM/yyyy}",
            $"Người nhận: {_selected.Recipient}",
            $"Số tiền: {_selected.Amount:N0} {_selected.Currency}",
            $"Phương thức: {_selected.Method}",
            $"Trạng thái: {_selected.Status}",
            $"Phê duyệt: {_selected.Approval}"
        });
        new Views.Dialogs.PrintPreviewDialog("PHIẾU CHI", content)
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
            Show("Xuất phiếu chi: không có dữ liệu thật phù hợp bộ lọc");
            return;
        }

        var exportRows = rows.Select(row => (IReadOnlyList<string>)new[]
        {
            row.Date.ToString("yyyy-MM-dd"), row.Code, row.Recipient, row.Amount.ToString("0.##"),
            row.Currency, row.Method, row.Status, row.Approval
        }).ToList();
        var path = CsvExportHelper.Export("phieu-chi-shell",
            new[] { "Ngày chi", "Số phiếu", "Người nhận", "Số tiền", "Tiền tệ", "Phương thức", "Trạng thái", "Phê duyệt" },
            exportRows);
        MessageBox.Show($"Đã xuất {rows.Count:N0} phiếu chi thật:\n{path}", "Phiếu chi", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void OnClose(object s, RoutedEventArgs e) => Close();
    private static void Show(string a) => MessageBox.Show(a, "Phiếu chi", MessageBoxButton.OK, MessageBoxImage.Information);

    private string? PromptForRejectReason()
    {
        var reasonBox = new TextBox
        {
            MinWidth = 360,
            MinHeight = 74,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 8, 0, 12)
        };
        var ok = new Button { Content = "Từ chối", Width = 90, Height = 30, Background = Cb("#DC2626"), Foreground = Brushes.White, BorderThickness = new Thickness(0), IsDefault = true };
        var cancel = new Button { Content = "Hủy", Width = 70, Height = 30, Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
        var dialog = new Window
        {
            Title = "Lý do từ chối phiếu chi",
            Width = 450,
            Height = 205,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Children =
                {
                    new TextBlock { Text = "Nhập lý do để lưu cùng lịch sử audit:", Foreground = C("#334155") },
                    reasonBox,
                    new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Children = { ok, cancel } }
                }
            }
        };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(reasonBox.Text))
            {
                MessageBox.Show(dialog, "Lý do từ chối không được để trống.", "Phiếu chi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            dialog.DialogResult = true;
        };
        return dialog.ShowDialog() == true ? reasonBox.Text.Trim() : null;
    }

    private static FrameworkElement Kpi(string l, string v, string color) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 6) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsPv(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record PvVoucher(string Id, string Code, DateTime Date, string Type, string Status, string Recipient, string? Reservation, string? Folio, string Method, string Currency, int Amount, int Allocated, int Unallocated, string Approval, string CreatedBy, string? Approver);
internal static class PvExt { public static TextBlock WithRunsPv(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
