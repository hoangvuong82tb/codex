using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public partial class InvoiceManagementWindow : Window
{
    private readonly List<IvInvoice> _invoices;
    private IvInvoice? _selected;
    private int _page, _pageSize = 50;
    private int _inspectorTab;
    private string _categoryFilter = "All";
    private readonly IEInvoiceService? _invoiceService;
    private readonly IEmailDispatchService? _emailDispatchService;
    private readonly IServiceScope _serviceScope;
    private CancellationTokenSource? _loadCts;
    private Task _loadTask = Task.CompletedTask;
    private bool _loaded;

    public InvoiceManagementWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var app = System.Windows.Application.Current as ASMILE.BO.App.App
            ?? throw new InvalidOperationException("Chưa khởi tạo DI container cho quản lý hóa đơn.");
        var services = app.Services ?? throw new InvalidOperationException("Chưa khởi tạo DI container cho quản lý hóa đơn.");
        _serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        _invoiceService = _serviceScope.ServiceProvider.GetService<IEInvoiceService>();
        _emailDispatchService = _serviceScope.ServiceProvider.GetService<IEmailDispatchService>();
        _invoices = new List<IvInvoice>();
        InitFilters();
        BuildKpi();
        BuildCategoryPanel();
        InitInspectorTabs();
        RefreshPage(true);
        FooterText.Text = _invoiceService is null
            ? "Chưa có dịch vụ truy vấn hóa đơn trong phiên hiện tại. Không hiển thị dữ liệu mẫu."
            : "Đang tải hóa đơn từ cơ sở dữ liệu...";
        Loaded += OnLoadedAsync;
        Closed += OnClosedAsync;
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        _loadTask = LoadRealInvoicesAsync();
        await _loadTask;
    }

    private async void OnClosedAsync(object? sender, EventArgs e)
    {
        _loadCts?.Cancel();
        try
        {
            await _loadTask;
        }
        finally
        {
            _serviceScope.Dispose();
        }
    }

    private async Task LoadRealInvoicesAsync()
    {
        if (_invoiceService is null) return;
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        try
        {
            FooterText.Text = "Đang tải hóa đơn từ cơ sở dữ liệu...";
            var rows = await _invoiceService.GetInvoicesAsync(take: 1_000, cancellationToken: _loadCts.Token);
            _invoices.Clear();
            _invoices.AddRange(rows.Select(MapInvoice));
            _selected = null;
            BuildKpi();
            BuildCategoryPanel();
            RefreshPage(true);
            FooterText.Text = $"Nguồn dữ liệu: cơ sở dữ liệu · Đã tải {_invoices.Count:N0} hóa đơn.";
        }
        catch (OperationCanceledException) when (_loadCts?.IsCancellationRequested == true) { }
        catch (Exception ex)
        {
            _invoices.Clear();
            _selected = null;
            BuildKpi();
            BuildCategoryPanel();
            RefreshPage(true);
            FooterText.Text = $"Không tải được dữ liệu hóa đơn thật: {ex.Message}";
        }
    }

    private static IvInvoice MapInvoice(InvoiceSummaryDto x)
    {
        var type = string.Equals(x.ProviderCode, "MISA", StringComparison.OrdinalIgnoreCase)
            ? "Electronic"
            : string.IsNullOrWhiteSpace(x.ProviderCode) ? "Invoice" : x.ProviderCode;
        var roomCharge = SumLines(x.Lines, "phòng", "room", "room charge");
        var foodBeverage = SumLines(x.Lines, "ăn", "f&b", "food", "restaurant", "nhà hàng");
        var laundry = SumLines(x.Lines, "giặt", "laundry");
        var minibar = SumLines(x.Lines, "minibar", "mini bar");
        var spa = SumLines(x.Lines, "spa");
        var transport = SumLines(x.Lines, "đưa đón", "vận chuyển", "transport", "transfer");
        var discount = SumLines(x.Lines, "giảm giá", "discount");
        var serviceCharge = SumLines(x.Lines, "phí dịch vụ", "service charge", "service fee");
        var categorized = roomCharge + foodBeverage + laundry + minibar + spa + transport + discount + serviceCharge;
        var other = x.SubtotalAmount - categorized;
        return new IvInvoice(
            x.Id,
            x.InvoiceNo,
            x.InvoiceDate,
            type,
            x.Status,
            x.BuyerName,
            null,
            x.BuyerTaxCode,
            "—",
            "—",
            roomCharge,
            foodBeverage,
            laundry,
            minibar,
            spa,
            transport,
            other,
            discount,
            x.TaxAmount,
            serviceCharge,
            x.TotalAmount,
            x.ReceivableTransactionId,
            x.ReceivableDebitAmount,
            x.ReceivableCreditAmount,
            x.ReceivableBalanceAmount,
            x.ReceivableStatus,
            x.ProviderCode,
            null,
            x.IssuedAt,
            x.ProviderInvoiceId);
    }

    private static decimal SumLines(IReadOnlyList<InvoiceLineSummaryDto> lines, params string[] terms)
        => lines
            .Where(line => terms.Any(term => line.ItemName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || line.ItemCode.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Sum(line => line.LineAmount);

    private void InitFilters()
    {
        TypeFilter.ItemsSource = new[] { "Tất cả", "VAT", "Official", "Proforma", "Electronic", "Deposit Invoice", "Refund Invoice", "Group Invoice" };
        TypeFilter.SelectedIndex = 0;
        StFilter.ItemsSource = new[] { "Tất cả", "Draft", "Pending", "Issued", "Sent", "Cancelled", "Adjusted" };
        StFilter.SelectedIndex = 0;
        PageSizeBox.ItemsSource = new[] { "50 / trang", "100 / trang", "200 / trang" }; PageSizeBox.SelectedIndex = 0;
    }

    private void BuildKpi()
    {
        KpiStrip.Children.Clear();
        var kpis = new (string, int, string)[] {
            ("Tổng hóa đơn", _invoices.Count, "#2F6BFF"), ("Đã phát hành", _invoices.Count(i => i.Status == "Issued"), "#16A34A"),
            ("Đã gửi", _invoices.Count(i => i.Status == "Sent"), "#F59E0B"), ("Đã hủy", _invoices.Count(i => i.Status == "Cancelled"), "#DC2626"),
            ("VAT", _invoices.Count(i => i.Type == "VAT"), "#8B5CF6"), ("Điều chỉnh", _invoices.Count(i => i.Status == "Adjusted"), "#F59E0B"),
            ("Nháp", _invoices.Count(i => i.Status == "Draft"), "#94A3B8")
        };
        foreach (var (label, value, color) in kpis) KpiStrip.Children.Add(Kpi(label, $"{value}", color));
    }

    private static FrameworkElement Kpi(string l, string v, string color) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };

    private void BuildCategoryPanel()
    {
        CatPanel.Children.Clear();
        var cats = new[] { ("Nháp", "Draft"), ("Sẵn sàng", "Ready"), ("Đã phát hành", "Issued"), ("Đã gửi", "Sent"), ("Đã điều chỉnh", "Adjusted"), ("Đã hủy", "Cancelled"), ("HĐ điện tử", "Electronic"), ("Lưu trữ", "Archived") };
        foreach (var (label, key) in cats) { var active = _categoryFilter == key; var count = key == "Ready" ? _invoices.Count(i => i.Status == "Draft" || i.Status == "Pending") : _invoices.Count(i => i.Status == key || i.Type == key); var b = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = label, FontSize = 12, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569"), Width = 158, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = label }, new TextBlock { Text = $"{count:N0}", FontSize = 11, Foreground = C("#64748B") } } } }; b.MouseLeftButtonDown += (_, _) => { _categoryFilter = key; BuildCategoryPanel(); RefreshPage(true); }; CatPanel.Children.Add(b); }
    }

    private List<IvInvoice> GetFiltered()
    {
        var i = _invoices.AsEnumerable(); var kw = SearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) i = i.Where(x => x.InvoiceNo.ToLower().Contains(kw) || x.Guest.ToLower().Contains(kw) || (x.Company?.ToLower().Contains(kw) ?? false) || x.Room.Contains(kw));
        var type = TypeFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(type) && type != "Tất cả") i = i.Where(x => x.Type == type);
        var st = StFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(st) && st != "Tất cả") i = i.Where(x => x.Status == st);
        if (_categoryFilter == "Draft") i = i.Where(x => x.Status == "Draft" || x.Status == "Pending"); else if (_categoryFilter == "Ready") i = i.Where(x => x.Status is "Draft" or "Pending"); else if (_categoryFilter == "Issued") i = i.Where(x => x.Status == "Issued"); else if (_categoryFilter == "Sent") i = i.Where(x => x.Status == "Sent"); else if (_categoryFilter == "Adjusted") i = i.Where(x => x.Status == "Adjusted"); else if (_categoryFilter == "Cancelled") i = i.Where(x => x.Status == "Cancelled"); else if (_categoryFilter == "Electronic") i = i.Where(x => x.Type == "Electronic"); else if (_categoryFilter == "Archived") i = i.Where(x => x.Status == "Archived");
        return i.ToList();
    }

    private void RefreshPage(bool reset)
    {
        if (reset) _page = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _pageSize - 1) / _pageSize);
        if (_page >= tp) _page = tp - 1; GridPanel.Children.Clear();
        var hdr = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        var cw = new[] { 90.0, 75.0, 90.0, 110.0, 90.0, 45.0, 85.0, 75.0, 80.0, 80.0, 85.0, 70.0, 70.0 };
        var ch = new[] { "Số HĐ", "Ngày", "Reservation", "Khách", "Công ty", "Phòng", "Loại HĐ", "MST", "Tiền", "Thuế", "Tổng", "Trạng thái", "Người lập" };
        for (int j = 0; j < cw.Length; j++) { hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) }); AddHdr(hdr, j, ch[j]); }
        GridPanel.Children.Add(hdr);

        foreach (var inv in f.Skip(_page * _pageSize).Take(_pageSize))
        {
            var active = _selected?.Id == inv.Id;
            var stColor = inv.Status switch { "Issued" => "#16A34A", "Sent" => "#F59E0B", "Cancelled" => "#DC2626", "Draft" => "#94A3B8", "Adjusted" => "#8B5CF6", _ => "#64748B" };
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1), Background = active ? Cb("#EFF6FF") : inv.Status == "Cancelled" ? Cb("#FFF5F5") : Cb("Transparent") };
            for (int j = 0; j < cw.Length; j++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) });
            var vals = new[] { inv.InvoiceNo, inv.InvoiceDate.ToString("dd/MM/yy"), inv.Reservation, inv.Guest, inv.Company ?? "—", inv.Room, inv.Type, inv.TaxCode ?? "—", $"{inv.GrandTotal:N0}", $"{inv.Tax:N0}", $"{inv.GrandTotal:N0}", inv.Status, inv.IssuedBy };
            var colors = new[] { "#64748B", "#64748B", "#64748B", "#17365D", "#64748B", "#64748B", "#64748B", "#64748B", "#17365D", "#64748B", "#17365D", stColor, "#64748B" };
            for (int j = 0; j < vals.Length; j++) AddCell(row, j, vals[j], colors[j], j == 3 || j == 8 || j == 10);
            var invId = inv.Id; row.Cursor = System.Windows.Input.Cursors.Hand;
            row.MouseLeftButtonDown += (_, _) => { _selected = inv; RefreshPage(false); BuildInspector(); };
            GridPanel.Children.Add(row);
        }
        IssueDraftButton.IsEnabled = _selected is { Status: "Draft" or "Adjusted" };
        PagerSummary.Text = $"{_page * _pageSize + 1}–{Math.Min((_page + 1) * _pageSize, f.Count)} / {f.Count:N0}";
        PrevBtn.IsEnabled = _page > 0; NextBtn.IsEnabled = _page < tp - 1; BuildPageBtns(tp);
        var filteredTotal = f.Sum(i => i.GrandTotal);
        FooterText.Text = $"Tổng: {f.Count:N0} · Đã phát hành: {f.Count(i => i.Status == "Issued")} · Tổng tiền: {filteredTotal:N0}đ · Nguồn: cơ sở dữ liệu";
    }

    private void BuildPageBtns(int tp) { PageBtns.Children.Clear(); for (int i = 0; i < Math.Min(tp, 10); i++) { var idx = i; var nb = PagerBtn(i, _page, idx); nb.Click += (_, _) => { _page = idx; RefreshPage(false); }; PageBtns.Children.Add(nb); } }
    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 26, Height = 24, FontSize = 10, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = PrevBtn.Template };
    private static void AddHdr(Grid g, int col, string text) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = text }; Grid.SetColumn(tb, col); g.Children.Add(tb); }
    private static void AddCell(Grid g, int col, string text, string color, bool bold) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = text }; Grid.SetColumn(tb, col); g.Children.Add(tb); }

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Khách hàng", "Chi tiết", "Thanh toán", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _inspectorTab; var b = new Border { CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(9, 5, 9, 5), Margin = new Thickness(0, 0, 2, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("White") : Cb("Transparent"), BorderBrush = active ? Cb("#E2E8F0") : Cb("Transparent"), BorderThickness = new Thickness(1, 1, 1, 0), Child = new TextBlock { Text = tabs[i], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#2F6BFF") : C("#64748B") } }; b.MouseLeftButtonDown += (_, _) => { _inspectorTab = idx; InitInspectorTabs(); BuildInspector(); }; InspectorTabs.Children.Add(b); }
        BuildInspector();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_selected == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn hóa đơn để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        var i = _selected;
        switch (_inspectorTab)
        {
            case 0:
                InspectorPanel.Children.Add(STitle(i.InvoiceNo));
                InspectorPanel.Children.Add(new Border { Background = Cb(i.Status == "Cancelled" ? "#DC2626" : "#16A34A"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 0, 6), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = i.Status, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
                InspectorPanel.Children.Add(Row("Ngày", i.InvoiceDate.ToString("dd/MM/yyyy")));
                InspectorPanel.Children.Add(Row("Loại", i.Type)); InspectorPanel.Children.Add(Row("Người lập", i.IssuedBy));
                break;
            case 1:
                InspectorPanel.Children.Add(STitle("Khách hàng"));
                InspectorPanel.Children.Add(Row("Khách", i.Guest)); InspectorPanel.Children.Add(Row("Phòng", i.Room));
                InspectorPanel.Children.Add(Row("Reservation", i.Reservation));
                if (i.Company != null) InspectorPanel.Children.Add(Row("Công ty", i.Company));
                if (i.TaxCode != null) InspectorPanel.Children.Add(Row("MST", i.TaxCode));
                break;
            case 2:
                InspectorPanel.Children.Add(STitle("Chi tiết hóa đơn"));
                InspectorPanel.Children.Add(Row("Tiền phòng", $"{i.RoomCharge:N0}đ"));
                InspectorPanel.Children.Add(Row("F&B", $"{i.FoodBeverage:N0}đ"));
                InspectorPanel.Children.Add(Row("Giặt là", $"{i.Laundry:N0}đ"));
                InspectorPanel.Children.Add(Row("Mini Bar", $"{i.MiniBar:N0}đ"));
                InspectorPanel.Children.Add(Row("Spa", $"{i.Spa:N0}đ"));
                InspectorPanel.Children.Add(Row("Vận chuyển", $"{i.Transport:N0}đ"));
                InspectorPanel.Children.Add(Row("Khác", $"{i.Other:N0}đ"));
                InspectorPanel.Children.Add(Row("Giảm giá", $"-{i.Discount:N0}đ"));
                InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 4) });
                InspectorPanel.Children.Add(Row("Thuế VAT", $"{i.Tax:N0}đ"));
                InspectorPanel.Children.Add(Row("Phí DV", $"{i.ServiceCharge:N0}đ"));
                InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#2F6BFF"), Margin = new Thickness(0, 4, 0, 4) });
                InspectorPanel.Children.Add(Row("TỔNG CỘNG", $"{i.GrandTotal:N0}đ"));
                break;
            case 3:
                InspectorPanel.Children.Add(STitle("Thanh toán"));
                InspectorPanel.Children.Add(Row("Tổng hóa đơn", $"{i.GrandTotal:N0}đ"));
                if (i.ReceivableTransactionId is Guid receivableId)
                {
                    InspectorPanel.Children.Add(Row("Mã công nợ", receivableId.ToString("N")));
                    InspectorPanel.Children.Add(Row("Nợ phát sinh", $"{i.ReceivableDebit:N0}đ"));
                    InspectorPanel.Children.Add(Row("Đã thu / ghi Có", $"{i.ReceivableCredit:N0}đ"));
                    InspectorPanel.Children.Add(Row("Còn phải thu", $"{i.ReceivableBalance:N0}đ"));
                    InspectorPanel.Children.Add(Row("Trạng thái", i.ReceivableStatus ?? "—"));
                }
                else
                {
                    InspectorPanel.Children.Add(Row("Công nợ", "Hóa đơn chưa liên kết giao dịch phải thu"));
                }
                break;
            case 4:
                InspectorPanel.Children.Add(STitle("Lịch sử"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ {i.InvoiceDate:dd/MM HH:mm} — Tạo — {i.IssuedBy}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                if (i.Status is "Issued" or "Sent") InspectorPanel.Children.Add(new TextBlock { Text = "⏱ — Phát hành", FontSize = 11, Foreground = C("#16A34A"), Margin = new Thickness(0, 2, 0, 0) });
                if (i.SentDate.HasValue) InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ {i.SentDate.Value:dd/MM HH:mm} — Gửi email", FontSize = 11, Foreground = C("#F59E0B"), Margin = new Thickness(0, 2, 0, 0) });
                if (i.Status == "Cancelled") InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ — Hủy: {i.CancelReason}", FontSize = 11, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) });
                break;
        }
    }

    private void OnQuickToday(object s, RoutedEventArgs e) { /* already filtered */ RefreshPage(true); }
    private void OnQuickIssued(object s, RoutedEventArgs e) { StFilter.SelectedIndex = StFilter.ItemsSource is string[] sts ? Array.IndexOf(sts, "Issued") : 0; RefreshPage(true); }
    private void OnQuickVat(object s, RoutedEventArgs e) { TypeFilter.SelectedIndex = TypeFilter.ItemsSource is string[] types ? Array.IndexOf(types, "VAT") : 0; RefreshPage(true); }
    private async void OnNewInvoice(object s, RoutedEventArgs e)
    {
        var existing = System.Windows.Application.Current.Windows.OfType<IssueInvoiceWizard>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null)
        {
            existing.Activate();
            return;
        }

        var wizard = new IssueInvoiceWizard
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        if (wizard.ShowDialog() == true)
            await LoadRealInvoicesAsync();
    }
    private void OnProviderSettings(object s, RoutedEventArgs e)
    {
        var window = new InvoiceProviderConfigurationWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        window.ShowDialog();
    }
    private async void OnIssueDraft(object s, RoutedEventArgs e)
    {
        if (_selected is null || _selected.Status is not ("Draft" or "Adjusted"))
        {
            ShowUnavailable("Hãy chọn một hóa đơn nháp hoặc điều chỉnh nháp để phát hành.");
            return;
        }

        if (_invoiceService is null)
        {
            ShowUnavailable("Chưa cấu hình dịch vụ hóa đơn thật trong phiên hiện tại.");
            return;
        }

        try
        {
            var result = await _invoiceService.QueueIssueAsync(_selected.Id, CancellationToken.None);
            MessageBox.Show(
                $"Đã đưa hóa đơn {_selected.InvoiceNo} vào hàng đợi phát hành ({result.Status}).",
                "Hóa đơn điện tử",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            await LoadRealInvoicesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể phát hành hóa đơn thật: {ex.Message}",
                "Hóa đơn điện tử",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
    private async void OnAdjust(object s, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            ShowUnavailable("Hãy chọn một hóa đơn thật trước khi điều chỉnh.");
            return;
        }

        if (_invoiceService is null)
        {
            ShowUnavailable("Chưa cấu hình dịch vụ hóa đơn thật trong phiên hiện tại.");
            return;
        }

        if (_selected.Status is not ("Issued" or "Sent") || string.IsNullOrWhiteSpace(_selected.AdjustedRef))
        {
            ShowUnavailable("Chỉ hóa đơn đã phát hành và có mã provider mới được tạo hóa đơn điều chỉnh.");
            return;
        }

        var input = PromptForAdjustment();
        if (input is null) return;

        try
        {
            var adjustmentId = await _invoiceService.CreateAdjustmentDraftAsync(
                new InvoiceAdjustmentDraftDto
                {
                    SourceInvoiceId = _selected.Id,
                    AdjustmentType = input.Type,
                    Reason = input.Reason,
                    Description = input.Description,
                    Amount = input.Amount,
                    TaxRate = input.TaxRate
                },
                CancellationToken.None);

            MessageBox.Show(
                $"Đã tạo hóa đơn điều chỉnh nháp {adjustmentId:N}. Hóa đơn chưa được phát hành; bước phát hành vẫn phải qua provider thật.",
                "Hóa đơn điều chỉnh",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            await LoadRealInvoicesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tạo hóa đơn điều chỉnh thật: " + ex.Message, "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private async void OnCancelInvoice(object s, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            ShowUnavailable("Hãy chọn một hóa đơn thật trước khi hủy.");
            return;
        }

        if (_invoiceService is null)
        {
            ShowUnavailable("Chưa cấu hình dịch vụ hóa đơn thật trong phiên hiện tại.");
            return;
        }

        var reason = PromptForCancelReason();
        if (string.IsNullOrWhiteSpace(reason)) return;

        if (MessageBox.Show(
                $"Hủy hóa đơn nháp {_selected.InvoiceNo}?\n\nLý do: {reason}",
                "Xác nhận hủy hóa đơn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var result = await _invoiceService.CancelAsync(_selected.Id, reason, CancellationToken.None);
            MessageBox.Show(result.Message, "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadRealInvoicesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể hủy hóa đơn thật: " + ex.Message, "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private async void OnPrint(object s, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            ShowUnavailable("Hãy chọn một hóa đơn thật trước khi xem bản in.");
            return;
        }

        if (_invoiceService is null)
        {
            ShowUnavailable("Chưa cấu hình dịch vụ hóa đơn thật trong phiên hiện tại.");
            return;
        }

        try
        {
            var bytes = await _invoiceService.GeneratePdfPreviewAsync(_selected.Id, CancellationToken.None);
            var content = Encoding.UTF8.GetString(bytes);
            new PrintPreviewDialog($"Xem trước hóa đơn {_selected.InvoiceNo}", content)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không tạo được bản xem trước hóa đơn thật: " + ex.Message, "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private async void OnEmail(object s, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            ShowUnavailable("Hãy chọn một hóa đơn thật trước khi gửi email.");
            return;
        }

        if (_emailDispatchService is null)
        {
            ShowUnavailable("Chưa cấu hình email outbox trong phiên hiện tại.");
            return;
        }

        var input = PromptForEmail();
        if (input is null) return;

        try
        {
            var result = await _emailDispatchService.QueueAsync(
                new EmailDispatchRequestDto
                {
                    Recipient = input.Recipient,
                    Subject = input.Subject,
                    Body = input.Body,
                    SourceType = "Invoice",
                    SourceId = _selected.Id
                },
                CancellationToken.None);

            MessageBox.Show(
                $"Đã lưu email vào hàng đợi thật ({result.Id:N}). Email sẽ được gửi khi SMTP được bật và cấu hình hợp lệ.",
                "Email hóa đơn",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể lưu email vào outbox: " + ex.Message, "Email hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private void OnApplyFilter(object s, RoutedEventArgs e) => RefreshPage(true);
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = ""; TypeFilter.SelectedIndex = 0; StFilter.SelectedIndex = 0; _categoryFilter = "All"; BuildCategoryPanel(); RefreshPage(true); }
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_page > 0) { _page--; RefreshPage(false); } }
    private void OnNextPage(object s, RoutedEventArgs e) { _page++; RefreshPage(false); }
    private void OnPageSizeChanged(object s, SelectionChangedEventArgs e) { _pageSize = PageSizeBox.SelectedIndex switch { 0 => 50, 1 => 100, 2 => 200, _ => 50 }; RefreshPage(true); }
    private async void OnRefresh(object s, RoutedEventArgs e)
    {
        _selected = null;
        InitFilters();
        BuildKpi();
        BuildCategoryPanel();
        InitInspectorTabs();
        await LoadRealInvoicesAsync();
    }
    private void OnExport(object s, RoutedEventArgs e)
    {
        var rows = GetFiltered();
        if (rows.Count == 0)
        {
            ShowUnavailable("Không có hóa đơn thật phù hợp bộ lọc để xuất.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Xuất danh sách hóa đơn",
            Filter = "CSV UTF-8 (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            FileName = $"hoa-don-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            AddExtension = true,
            DefaultExt = ".csv",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Số hóa đơn,Ngày,Loại,Trạng thái,Khách hàng,Công ty,Mã số thuế,Phòng,Đặt phòng,Tiền phòng,Ăn uống,Giặt là,Minibar,Spa,Đưa đón,Khác,Giảm giá,Thuế,Phí dịch vụ,Tổng cộng,Người lập");
            foreach (var row in rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(row.InvoiceNo),
                    Csv(row.InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss")),
                    Csv(row.Type),
                    Csv(row.Status),
                    Csv(row.Guest),
                    Csv(row.Company),
                    Csv(row.TaxCode),
                    Csv(row.Room),
                    Csv(row.Reservation),
                    Csv(row.RoomCharge.ToString("0.##")),
                    Csv(row.FoodBeverage.ToString("0.##")),
                    Csv(row.Laundry.ToString("0.##")),
                    Csv(row.MiniBar.ToString("0.##")),
                    Csv(row.Spa.ToString("0.##")),
                    Csv(row.Transport.ToString("0.##")),
                    Csv(row.Other.ToString("0.##")),
                    Csv(row.Discount.ToString("0.##")),
                    Csv(row.Tax.ToString("0.##")),
                    Csv(row.ServiceCharge.ToString("0.##")),
                    Csv(row.GrandTotal.ToString("0.##")),
                    Csv(row.IssuedBy)
                }));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            FooterText.Text = $"Đã xuất {rows.Count:N0} hóa đơn thật theo bộ lọc hiện tại.";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không xuất được danh sách hóa đơn: " + ex.Message, "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
    private void OnClose(object s, RoutedEventArgs e) => Close();
    private string? PromptForCancelReason()
    {
        var dialog = new Window
        {
            Title = "Lý do hủy hóa đơn",
            Width = 460,
            Height = 205,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        var input = new TextBox
        {
            Margin = new Thickness(0, 6, 0, 10),
            MinHeight = 58,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var ok = new Button
        {
            Content = "Xác nhận",
            Padding = new Thickness(12, 5, 12, 5),
            IsDefault = true,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                MessageBox.Show("Vui lòng nhập lý do hủy hóa đơn.", "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
                input.Focus();
                return;
            }

            dialog.DialogResult = true;
        };
        dialog.Content = new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Lý do nghiệp vụ *", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") },
                    input,
                    ok
                }
            }
        };
        return dialog.ShowDialog() == true ? input.Text.Trim() : null;
    }

    private InvoiceAdjustmentInput? PromptForAdjustment()
    {
        var dialog = new Window
        {
            Title = "Tạo hóa đơn điều chỉnh nháp",
            Width = 520,
            Height = 430,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        var type = new ComboBox { ItemsSource = new[] { "Increase", "Decrease", "Information" }, SelectedIndex = 0, Margin = new Thickness(0, 6, 0, 8) };
        var amount = new TextBox { Text = "0", Margin = new Thickness(0, 0, 0, 8) };
        var taxRate = new TextBox { Text = "10", Margin = new Thickness(0, 0, 0, 8) };
        var description = new TextBox { Margin = new Thickness(0, 0, 0, 8), MinHeight = 48, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
        var reason = new TextBox { Margin = new Thickness(0, 0, 0, 12), MinHeight = 58, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
        var ok = new Button { Content = "Tạo nháp", Padding = new Thickness(12, 5, 12, 5), IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) =>
        {
            if (!decimal.TryParse(amount.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amountValue)
                || !decimal.TryParse(taxRate.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var taxValue))
            {
                MessageBox.Show("Số tiền và thuế suất phải là số hợp lệ.", "Hóa đơn điều chỉnh", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (type.SelectedItem?.ToString() is not "Information" && amountValue <= 0m)
            {
                MessageBox.Show("Điều chỉnh tăng/giảm phải có số tiền lớn hơn 0.", "Hóa đơn điều chỉnh", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (taxValue is < 0m or > 100m || string.IsNullOrWhiteSpace(description.Text) || string.IsNullOrWhiteSpace(reason.Text))
            {
                MessageBox.Show("Cần nhập diễn giải, lý do và thuế suất hợp lệ.", "Hóa đơn điều chỉnh", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dialog.Tag = new InvoiceAdjustmentInput(type.SelectedItem?.ToString() ?? "Increase", amountValue, taxValue, description.Text.Trim(), reason.Text.Trim());
            dialog.DialogResult = true;
        };
        dialog.Content = new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Loại điều chỉnh", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, type,
                    new TextBlock { Text = "Số tiền điều chỉnh", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, amount,
                    new TextBlock { Text = "Thuế suất (%)", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, taxRate,
                    new TextBlock { Text = "Diễn giải dòng hóa đơn *", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, description,
                    new TextBlock { Text = "Lý do nghiệp vụ *", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, reason, ok
                }
            }
        };

        return dialog.ShowDialog() == true ? dialog.Tag as InvoiceAdjustmentInput : null;
    }

    private InvoiceEmailInput? PromptForEmail()
    {
        if (_selected is null) return null;

        var dialog = new Window
        {
            Title = "Gửi email hóa đơn",
            Width = 560,
            Height = 430,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        var recipient = new TextBox { Margin = new Thickness(0, 6, 0, 8) };
        var subject = new TextBox { Text = $"Hóa đơn {_selected.InvoiceNo}", Margin = new Thickness(0, 0, 0, 8) };
        var body = new TextBox
        {
            Text = $"Kính gửi { _selected.Guest },\n\nHóa đơn: {_selected.InvoiceNo}\nNgày: {_selected.InvoiceDate:dd/MM/yyyy HH:mm}\nTổng cộng: {_selected.GrandTotal:N0}\n\nTrân trọng.",
            Margin = new Thickness(0, 0, 0, 12),
            MinHeight = 150,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var ok = new Button { Content = "Lưu vào hàng đợi", Padding = new Thickness(12, 5, 12, 5), IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(recipient.Text) || string.IsNullOrWhiteSpace(subject.Text) || string.IsNullOrWhiteSpace(body.Text))
            {
                MessageBox.Show("Cần nhập người nhận, tiêu đề và nội dung email.", "Email hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dialog.Tag = new InvoiceEmailInput(recipient.Text.Trim(), subject.Text.Trim(), body.Text);
            dialog.DialogResult = true;
        };
        dialog.Content = new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Email người nhận *", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, recipient,
                    new TextBlock { Text = "Tiêu đề *", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, subject,
                    new TextBlock { Text = "Nội dung *", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, body, ok
                }
            }
        };

        return dialog.ShowDialog() == true ? dialog.Tag as InvoiceEmailInput : null;
    }

    private static void ShowUnavailable(string message) => MessageBox.Show(message, "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Information);

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 6) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsIv(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal sealed record InvoiceAdjustmentInput(string Type, decimal Amount, decimal TaxRate, string Description, string Reason);
internal sealed record InvoiceEmailInput(string Recipient, string Subject, string Body);
internal record IvInvoice(Guid Id, string InvoiceNo, DateTime InvoiceDate, string Type, string Status, string Guest, string? Company, string? TaxCode, string Room, string Reservation, decimal RoomCharge, decimal FoodBeverage, decimal Laundry, decimal MiniBar, decimal Spa, decimal Transport, decimal Other, decimal Discount, decimal Tax, decimal ServiceCharge, decimal GrandTotal, Guid? ReceivableTransactionId, decimal ReceivableDebit, decimal ReceivableCredit, decimal ReceivableBalance, string? ReceivableStatus, string IssuedBy, string? CancelReason, DateTime? SentDate, string? AdjustedRef);
internal static class IvExt { public static TextBlock WithRunsIv(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
