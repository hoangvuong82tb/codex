using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Shared.Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public partial class TransactionCenterWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private readonly List<TxTransaction> _txns;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly ICommandHandler<ReverseFolioEntryCommand, HotelFolioEntryDto>? _reverseEntryHandler;
    private readonly ICommandHandler<AddFolioAdjustmentCommand, HotelFolioEntryDto>? _adjustmentHandler;
    private readonly ICommandHandler<AddFolioManualChargeCommand, HotelFolioEntryDto>? _manualChargeHandler;
    private readonly ICommandHandler<AddFolioManualCreditCommand, HotelFolioEntryDto>? _manualCreditHandler;
    private TxTransaction? _selected;
    private int _page, _pageSize = 50;
    private int _inspectorTab;
    private string _typeFilter = "All";

    public TransactionCenterWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _reverseEntryHandler = services?.GetService<ICommandHandler<ReverseFolioEntryCommand, HotelFolioEntryDto>>();
        _adjustmentHandler = services?.GetService<ICommandHandler<AddFolioAdjustmentCommand, HotelFolioEntryDto>>();
        _manualChargeHandler = services?.GetService<ICommandHandler<AddFolioManualChargeCommand, HotelFolioEntryDto>>();
        _manualCreditHandler = services?.GetService<ICommandHandler<AddFolioManualCreditCommand, HotelFolioEntryDto>>();
        _txns = new List<TxTransaction>();
        InitFilters();
        BuildKpi();
        BuildNavPanel();
        InitInspectorTabs();
        RefreshPage(true);
        FooterText.Text = "Sẵn sàng · Chọn giao dịch để xem chi tiết";
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        await LoadTransactionsFromDatabaseAsync();
    }

    private async Task LoadTransactionsFromDatabaseAsync()
    {
        if (_folioRepository is null || _executionContext?.Current is null)
        {
            _txns.Clear();
            RefreshPage(true);
            FooterText.Text = "Chưa cấu hình được Folio service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var propertyId = _executionContext.Current.Property.PropertyId.Value;
            var page = await _folioRepository.SearchAsync(
                new FolioSearchFilter(propertyId, null, null, null, null, null, null, null),
                new PageRequest(1, 200), CancellationToken.None);
            _txns.Clear();
            var id = 1;
            foreach (var folio in page.Items)
            {
                foreach (var entry in folio.Entries.OrderBy(item => item.CreatedAtUtc))
                    _txns.Add(MapPersistedEntry(id++, folio, entry));
            }
            _selected = null;
            BuildKpi();
            BuildNavPanel();
            InitInspectorTabs();
            RefreshPage(true);
            FooterText.Text = $"Đã tải {_txns.Count:N0} giao dịch từ dữ liệu Folio thật.";
        }
        catch (Exception ex)
        {
            _txns.Clear();
            RefreshPage(true);
            FooterText.Text = $"Không tải được giao dịch thật: {ex.Message}";
        }
    }

    private static TxTransaction MapPersistedEntry(int id, FolioLedgerDto folio, HotelFolioEntryDto entry)
    {
        var type = entry.EntryType switch
        {
            HotelFolioEntryType.RoomCharge => "Room Charge",
            HotelFolioEntryType.ServiceCharge => "Service Charge",
            HotelFolioEntryType.OtherCharge => "Other",
            HotelFolioEntryType.ManualCharge => "Adjustment",
            HotelFolioEntryType.ManualCredit => "Adjustment",
            _ => entry.EntryType.ToString()
        };
        var group = entry.EntryType switch
        {
            HotelFolioEntryType.Payment => "Payment",
            HotelFolioEntryType.Deposit => "Deposit",
            HotelFolioEntryType.Refund => "Refund",
            HotelFolioEntryType.Tax or HotelFolioEntryType.ServiceCharge => "Tax/Fee",
            HotelFolioEntryType.Discount => "Discount",
            HotelFolioEntryType.Transfer => "Transfer",
            HotelFolioEntryType.Adjustment or HotelFolioEntryType.Correction or HotelFolioEntryType.ManualCredit => "Adjustment",
            _ => "Charge"
        };
        var amount = (int)Math.Round(Math.Abs(entry.Amount));
        var status = entry.ReversedByEntryId.HasValue ? "Reversed" : "Posted";
        return new TxTransaction(
            id,
            $"FOL-{folio.FolioNumber}-{entry.SequenceNumber:D3}",
            entry.CreatedAtUtc,
            entry.BusinessDate?.ToDateTime(TimeOnly.MinValue) ?? entry.CreatedAtUtc.Date,
            type,
            group,
            entry.Description,
            "FO",
            "Front Office",
            folio.GuestName,
            folio.RoomNumber,
            null,
            folio.FolioNumber,
            "Guest",
            1,
            amount,
            0,
            0,
            group == "Tax/Fee" && type == "Tax" ? amount : 0,
            0,
            amount,
            "VND",
            1,
            "Front Office",
            "—",
            "—",
            status,
            "Chưa đối soát",
            null,
            false,
            amount >= 10_000_000,
            false,
            "Hotel Folio",
            0,
            folio.HotelFolioId,
            entry.HotelFolioEntryId,
            folio.RowVersion);
    }

    private void InitFilters()
    {
        TypeFilter.ItemsSource = new[] { "Tất cả", "Room Charge", "Restaurant", "Spa", "Laundry", "Minibar", "Payment", "Deposit", "Refund", "Tax", "Service Charge", "Discount", "Adjustment", "Transfer", "Void", "Reversal" };
        TypeFilter.SelectedIndex = 0;
        StFilter.ItemsSource = new[] { "Tất cả", "Posted", "Pending", "Voided", "Reversed", "Completed", "Settled", "Disputed" };
        StFilter.SelectedIndex = 0;
        DeptFilter.ItemsSource = new[] { "Tất cả", "FO", "F&B", "SPA", "LAUN", "MINI" };
        DeptFilter.SelectedIndex = 0;
        RecFilter.ItemsSource = new[] { "Tất cả", "Đã khớp", "Chưa đối soát", "Chênh lệch", "Chờ xác minh" };
        RecFilter.SelectedIndex = 0;
        PageSizeBox.ItemsSource = new[] { "50 / trang", "100 / trang", "200 / trang" }; PageSizeBox.SelectedIndex = 0;
    }

    private void BuildKpi()
    {
        KpiStrip.Children.Clear();
        var businessDate = _executionContext?.Current?.BusinessDate.BusinessDate
            ?? DateOnly.FromDateTime(DateTime.Today);
        var today = _txns.Where(tx => tx.BusinessDate.Date == businessDate.ToDateTime(TimeOnly.MinValue).Date).ToList();
        var kpis = new (string, string, string)[] {
            ("Giao dịch hôm nay", $"{today.Count}", "#2F6BFF"),
            ("Charge", $"{today.Where(tx => tx.Group == "Charge").Sum(tx => (long)tx.Net):N0}", "#DC2626"),
            ("Payment", $"{today.Where(tx => tx.Group == "Payment").Sum(tx => (long)tx.Net):N0}", "#16A34A"),
            ("Deposit", $"{today.Where(tx => tx.Group == "Deposit").Sum(tx => (long)tx.Net):N0}", "#F59E0B"),
            ("Refund", $"{today.Where(tx => tx.Group == "Refund").Sum(tx => (long)tx.Net):N0}", "#8B5CF6"),
            ("Đã Void/Reverse", $"{today.Count(tx => tx.Status is "Voided" or "Reversed")}", "#DC2626"),
            ("Chưa đối soát", $"{today.Count(tx => tx.RecStatus == "Chưa đối soát")}", "#F59E0B")
        };
        foreach (var (label, value, color) in kpis) KpiStrip.Children.Add(Kpi(label, value, color));
    }

    private void BuildNavPanel()
    {
        NavPanel.Children.Clear();
        NavPanel.Children.Add(new TextBlock { Text = "LOẠI GIAO DỊCH", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 6) });
        var types = new[] { ("Tất cả", "All"), ("Charge", "Charge"), ("Payment", "Payment"), ("Deposit", "Deposit"), ("Refund", "Refund"), ("Tax/Fee", "Tax/Fee"), ("Discount", "Discount"), ("Adjustment", "Adjustment"), ("Transfer", "Transfer"), ("Void/Reverse", "Void/Reverse") };
        foreach (var (label, key) in types) AddNavItem(label, key, key == "All" ? _txns.Count : _txns.Count(tx => tx.Group == key));

        NavPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        NavPanel.Children.Add(new TextBlock { Text = "KIỂM SOÁT", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 6) });
        var ctrls = new[] { ("Chưa đối soát", "Unreconciled"), ("Có cảnh báo", "Warning"), ("Giao dịch lớn", "HighValue"), ("Đã Void/Reverse", "Voided"), ("Lỗi đồng bộ", "SyncError") };
        foreach (var (label, key) in ctrls) AddNavItem(label, key, key switch { "Unreconciled" => _txns.Count(tx => tx.RecStatus == "Chưa đối soát"), "Warning" => _txns.Count(tx => tx.HasWarning), "HighValue" => _txns.Count(tx => tx.IsHighValue), "Voided" => _txns.Count(tx => tx.Status is "Voided" or "Reversed"), "SyncError" => _txns.Count(tx => tx.HasSyncError), _ => 0 });
    }

    private void AddNavItem(string label, string key, int count)
    {
        var active = _typeFilter == key;
        var b = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569"), Width = 158, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = label }, new TextBlock { Text = $"{count:N0}", FontSize = 11, Foreground = C("#64748B"), Width = 45, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left } } } };
        b.MouseLeftButtonDown += (_, _) => { _typeFilter = key; BuildNavPanel(); RefreshPage(true); }; NavPanel.Children.Add(b);
    }

    private List<TxTransaction> GetFiltered()
    {
        var t = _txns.AsEnumerable(); var kw = SearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) t = t.Where(x => x.Code.ToLower().Contains(kw) || x.Description.ToLower().Contains(kw) || x.Guest.ToLower().Contains(kw) || x.Room.Contains(kw) || (x.Reservation?.ToLower().Contains(kw) ?? false) || x.Folio.ToLower().Contains(kw));
        var type = TypeFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(type) && type != "Tất cả") t = t.Where(x => x.Type == type);
        var st = StFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(st) && st != "Tất cả") t = t.Where(x => x.Status == st);
        var dept = DeptFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(dept) && dept != "Tất cả") t = t.Where(x => x.Dept == dept);
        var rec = RecFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(rec) && rec != "Tất cả") t = t.Where(x => x.RecStatus == rec);
        if (_typeFilter != "All")
        {
            if (new[] { "Charge", "Payment", "Deposit", "Refund", "Tax/Fee", "Discount", "Adjustment", "Transfer", "Void/Reverse" }.Contains(_typeFilter)) t = t.Where(x => x.Group == _typeFilter);
            else if (_typeFilter == "Unreconciled") t = t.Where(x => x.RecStatus == "Chưa đối soát");
            else if (_typeFilter == "Warning") t = t.Where(x => x.HasWarning);
            else if (_typeFilter == "HighValue") t = t.Where(x => x.IsHighValue);
            else if (_typeFilter == "Voided") t = t.Where(x => x.Status is "Voided" or "Reversed");
            else if (_typeFilter == "SyncError") t = t.Where(x => x.HasSyncError);
        }
        return t.ToList();
    }

    private void RefreshPage(bool reset)
    {
        if (reset) _page = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _pageSize - 1) / _pageSize);
        if (_page >= tp) _page = tp - 1; GridPanel.Children.Clear();
        var cw = new[] { 65.0, 70.0, 90.0, 85.0, 130.0, 45.0, 55.0, 100.0, 48.0, 90.0, 55.0, 70.0, 65.0, 65.0 };
        var ch = new[] { "T.Thái", "Thời gian", "Mã GD", "Loại", "Diễn giải", "BP", "Outlet", "Khách", "Phòng", "Folio", "Tiền tệ", "Net", "Operator", "Đối soát" };
        var hdr = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        for (int j = 0; j < cw.Length; j++) { hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) }); AddHdr(hdr, j, ch[j]); }
        GridPanel.Children.Add(hdr);

        foreach (var tx in f.Skip(_page * _pageSize).Take(_pageSize))
        {
            var active = _selected?.Id == tx.Id;
            var stColor = tx.Status switch { "Posted" => "#16A34A", "Pending" => "#F59E0B", "Voided" => "#DC2626", "Reversed" => "#8B5CF6", "Disputed" => "#F59E0B", _ => "#64748B" };
            var typeColor = tx.Group switch { "Charge" => "#DC2626", "Payment" => "#16A34A", "Deposit" => "#F59E0B", "Refund" => "#8B5CF6", "Tax/Fee" => "#6366F1", "Discount" => "#F97316", "Adjustment" => "#06B6D4", "Transfer" => "#EC4899", _ => "#64748B" };
            var recColor = tx.RecStatus switch { "Đã khớp" => "#16A34A", "Chưa đối soát" => "#F59E0B", "Chênh lệch" => "#DC2626", _ => "#64748B" };
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1), Background = active ? Cb("#EFF6FF") : tx.Status is "Voided" or "Reversed" ? Cb("#FFF5F5") : Cb("Transparent") };
            for (int j = 0; j < cw.Length; j++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) });
            var vals = new[] { tx.Status, tx.Time.ToString("dd/MM HH:mm"), tx.Code, tx.Type, tx.Description, tx.Dept, tx.Outlet, tx.Guest, tx.Room, tx.Folio, tx.Currency, $"{tx.Net:N0}", tx.Operator, tx.RecStatus };
            var colors = new[] { stColor, "#64748B", "#64748B", typeColor, "#17365D", "#64748B", "#64748B", "#17365D", "#64748B", "#64748B", "#64748B", "#17365D", "#64748B", recColor };
            for (int j = 0; j < vals.Length; j++) AddCell(row, j, vals[j], colors[j], j == 4 || j == 7 || j == 11);
            var txid = tx.Id; row.Cursor = System.Windows.Input.Cursors.Hand;
            row.MouseLeftButtonDown += (_, _) => { _selected = tx; RefreshPage(false); BuildInspector(); UpdateBottomSummary(f); };
            GridPanel.Children.Add(row);
        }
        PagerSummary.Text = $"{_page * _pageSize + 1}–{Math.Min((_page + 1) * _pageSize, f.Count)} / {f.Count:N0}";
        PrevBtn.IsEnabled = _page > 0; NextBtn.IsEnabled = _page < tp - 1; BuildPageBtns(tp);
        UpdateBottomSummary(f);
    }

    private void UpdateBottomSummary(List<TxTransaction> f)
    {
        BottomSummary.Children.Clear();
        var charge = f.Where(tx => tx.Group == "Charge").Sum(tx => (long)tx.Net);
        var payment = f.Where(tx => tx.Group == "Payment").Sum(tx => (long)tx.Net);
        var deposit = f.Where(tx => tx.Group == "Deposit").Sum(tx => (long)tx.Net);
        var refund = f.Where(tx => tx.Group == "Refund").Sum(tx => (long)tx.Net);
        var net = payment + deposit - charge - refund;
        BottomSummary.Children.Add(Btm("Charge", $"{charge:N0}", "#DC2626")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Payment", $"{payment:N0}", "#16A34A")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Deposit", $"{deposit:N0}", "#F59E0B")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Refund", $"{refund:N0}", "#8B5CF6")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Net", $"{net:N0}", net >= 0 ? "#16A34A" : "#DC2626"));
        FooterText.Text = $"Tổng: {f.Count:N0} giao dịch · Charge: {f.Where(tx => tx.Group == "Charge").Sum(tx => (long)tx.Net):N0} · Payment: {f.Where(tx => tx.Group == "Payment").Sum(tx => (long)tx.Net):N0}";
    }

    private void BuildPageBtns(int tp) { PageBtns.Children.Clear(); for (int i = 0; i < Math.Min(tp, 10); i++) { var idx = i; var nb = PagerBtn(i, _page, idx); nb.Click += (_, _) => { _page = idx; RefreshPage(false); }; PageBtns.Children.Add(nb); } }
    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 26, Height = 24, FontSize = 10, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = PrevBtn.Template };
    private static void AddHdr(Grid g, int col, string text) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }
    private static void AddCell(Grid g, int col, string text, string color, bool bold) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Tài chính", "Nguồn", "Liên kết", "Đối soát", "Timeline", "Audit" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _inspectorTab; var b = new Border { CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(3, 3, 3, 3), Margin = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("White") : Cb("Transparent"), BorderBrush = active ? Cb("#E2E8F0") : Cb("Transparent"), BorderThickness = new Thickness(1, 1, 1, 0), Child = new TextBlock { Text = tabs[i], FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#2F6BFF") : C("#64748B") } }; b.MouseLeftButtonDown += (_, _) => { _inspectorTab = idx; InitInspectorTabs(); BuildInspector(); }; InspectorTabs.Children.Add(b); }
        BuildInspector();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_selected == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn một giao dịch để xem thông tin, ảnh hưởng tài chính và lịch sử.", FontSize = 12, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap }); return; }
        var tx = _selected;
        var stColor = tx.Status switch { "Posted" => "#16A34A", "Pending" => "#F59E0B", "Voided" or "Reversed" => "#DC2626", _ => "#64748B" };
        var typeColor = tx.Group switch { "Charge" => "#DC2626", "Payment" => "#16A34A", "Deposit" => "#F59E0B", "Refund" => "#8B5CF6", _ => "#64748B" };

        switch (_inspectorTab)
        {
            case 0:
                InspectorPanel.Children.Add(STitle(tx.Code));
                InspectorPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6), Children = { new Border { Background = Cb(stColor), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = tx.Status, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } }, new Border { Background = Cb(typeColor), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(4, 0, 0, 0), Child = new TextBlock { Text = tx.Group, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } } } });
                InspectorPanel.Children.Add(Row("Loại", tx.Type)); InspectorPanel.Children.Add(Row("Thời gian", tx.Time.ToString("dd/MM/yyyy HH:mm:ss")));
                InspectorPanel.Children.Add(Row("Business Date", tx.BusinessDate.ToString("dd/MM/yyyy")));
                InspectorPanel.Children.Add(Row("Diễn giải", tx.Description)); InspectorPanel.Children.Add(Row("BP", tx.Dept));
                InspectorPanel.Children.Add(Row("Outlet", tx.Outlet)); InspectorPanel.Children.Add(Row("Operator", tx.Operator));
                InspectorPanel.Children.Add(Row("Cashier", tx.Cashier)); InspectorPanel.Children.Add(Row("Ca", tx.Shift));
                break;
            case 1:
                InspectorPanel.Children.Add(STitle("Tài chính"));
                InspectorPanel.Children.Add(Row("Gross", $"{tx.Gross:N0}")); InspectorPanel.Children.Add(Row("Discount", $"-{tx.Discount:N0}"));
                InspectorPanel.Children.Add(Row("Phí DV", $"{tx.ServiceCharge:N0}")); InspectorPanel.Children.Add(Row("Thuế", $"{tx.Tax:N0}"));
                InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 4) });
                InspectorPanel.Children.Add(Row("Net", $"{tx.Net:N0} {tx.Currency}"));
                InspectorPanel.Children.Add(Row("Tỷ giá", $"{tx.ExchangeRate:N0}"));
                if (tx.Currency != "VND") InspectorPanel.Children.Add(Row("Quy đổi", $"{(int)(tx.Net * tx.ExchangeRate):N0} VND"));
                break;
            case 2:
                InspectorPanel.Children.Add(STitle("Nguồn"));
                InspectorPanel.Children.Add(Row("Hệ thống", tx.SourceSystem));
                InspectorPanel.Children.Add(Row("Outlet", tx.Outlet)); InspectorPanel.Children.Add(Row("Bộ phận", tx.Dept));
                if (tx.OriginalTxn != null) InspectorPanel.Children.Add(Row("GD gốc", tx.OriginalTxn));
                break;
            case 3:
                InspectorPanel.Children.Add(STitle("Liên kết"));
                InspectorPanel.Children.Add(Row("Khách", tx.Guest)); InspectorPanel.Children.Add(Row("Phòng", tx.Room));
                InspectorPanel.Children.Add(Row("Reservation", tx.Reservation ?? "—"));
                InspectorPanel.Children.Add(Row("Folio", tx.Folio)); InspectorPanel.Children.Add(Row("Payer", tx.Payer));
                break;
            case 4:
                InspectorPanel.Children.Add(STitle("Đối soát"));
                InspectorPanel.Children.Add(Row("Trạng thái", tx.RecStatus));
                var recColor2 = tx.RecStatus switch { "Đã khớp" => "#16A34A", "Chưa đối soát" => "#F59E0B", "Chênh lệch" => "#DC2626", _ => "#64748B" };
                InspectorPanel.Children.Add(new Border { Background = Cb(recColor2), CornerRadius = new CornerRadius(7), Padding = new Thickness(8), Margin = new Thickness(0, 4, 0, 0), Child = new TextBlock { Text = tx.RecStatus, FontSize = 11, Foreground = Cb("White"), HorizontalAlignment = HorizontalAlignment.Center } });
                break;
            case 5:
                InspectorPanel.Children.Add(STitle("Timeline"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ {tx.Time:dd/MM HH:mm} — Tạo — {tx.Operator}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                if (tx.Status == "Voided") InspectorPanel.Children.Add(new TextBlock { Text = "⏱ — Void", FontSize = 11, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) });
                if (tx.Status == "Reversed") InspectorPanel.Children.Add(new TextBlock { Text = "⏱ — Reverse", FontSize = 11, Foreground = C("#8B5CF6"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 6:
                InspectorPanel.Children.Add(STitle("Audit"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"{tx.Time:dd/MM HH:mm} — Tạo — {tx.Operator} · {tx.Shift}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                break;
        }
    }

    private async void OnNewPosting(object s, RoutedEventArgs e)
    {
        if (_selected is not { } tx)
        {
            Show("Hãy chọn giao dịch Folio để xác định Folio cần ghi.");
            return;
        }

        if (tx.HotelFolioId is not { } folioId)
        {
            Show("Giao dịch này chưa có liên kết Folio thật để ghi giao dịch thủ công.");
            return;
        }

        if (tx.FolioRowVersion is not { Length: 8 } rowVersion)
        {
            Show("Thiếu rowversion của Folio. Vui lòng tải lại dữ liệu trước khi ghi giao dịch.");
            return;
        }

        var type = new ComboBox { ItemsSource = new[] { "Manual charge", "Manual credit", "Adjustment" }, SelectedIndex = 0, Margin = new Thickness(0, 4, 0, 10) };
        var amount = new TextBox { Text = "0", Margin = new Thickness(0, 4, 0, 10) };
        var description = new TextBox { Text = $"Giao dịch thủ công từ Transaction Center: {tx.Code}", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 58, Margin = new Thickness(0, 4, 0, 10) };
        var dialog = new Window
        {
            Title = "Tạo giao dịch thủ công",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 430,
            SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize,
            Content = BuildManualPostingContent(type, amount, description)
        };
        var confirm = new Button { Content = "Ghi giao dịch thật", IsDefault = true, MinWidth = 130, Margin = new Thickness(0, 10, 0, 0) };
        var cancel = new Button { Content = "Hủy", IsCancel = true, MinWidth = 70, Margin = new Thickness(8, 10, 0, 0) };
        ((StackPanel)dialog.Content).Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { confirm, cancel }
        });

        confirm.Click += async (_, _) =>
        {
            if (!TryParseAmount(amount.Text, out var amountValue) || amountValue <= 0)
            {
                Show("Số tiền phải lớn hơn 0 và đúng định dạng.");
                return;
            }

            if (string.IsNullOrWhiteSpace(description.Text))
            {
                Show("Vui lòng nhập diễn giải giao dịch.");
                return;
            }

            confirm.IsEnabled = false;
            try
            {
                var result = type.SelectedIndex switch
                {
                    1 when _manualCreditHandler is not null => await _manualCreditHandler.HandleAsync(new AddFolioManualCreditCommand(folioId, rowVersion, amountValue, description.Text.Trim()), CancellationToken.None),
                    2 when _adjustmentHandler is not null => await _adjustmentHandler.HandleAsync(new AddFolioAdjustmentCommand(folioId, rowVersion, amountValue, description.Text.Trim()), CancellationToken.None),
                    0 when _manualChargeHandler is not null => await _manualChargeHandler.HandleAsync(new AddFolioManualChargeCommand(folioId, rowVersion, amountValue, description.Text.Trim()), CancellationToken.None),
                    _ => Result<HotelFolioEntryDto>.Failure(Error.Business("FOLIO_POSTING_HANDLER_UNAVAILABLE", "Chưa đăng ký command ghi Folio thật."))
                };

                if (!result.IsSuccess)
                {
                    Show(result.Error?.Message ?? "Không thể ghi giao dịch Folio thật.");
                    return;
                }

                dialog.DialogResult = true;
            }
            catch (Exception ex)
            {
                Show("Không thể ghi giao dịch Folio thật: " + ex.Message);
            }
            finally
            {
                confirm.IsEnabled = true;
            }
        };

        if (dialog.ShowDialog() == true)
        {
            _selected = null;
            await LoadTransactionsFromDatabaseAsync();
            Show("Đã ghi giao dịch thủ công vào Folio thật.");
        }
    }

    private static StackPanel BuildManualPostingContent(ComboBox type, TextBox amount, TextBox description)
    {
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = "Loại giao dịch", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(type);
        panel.Children.Add(new TextBlock { Text = "Số tiền", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(amount);
        panel.Children.Add(new TextBlock { Text = "Diễn giải", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(description);
        return panel;
    }

    private static bool TryParseAmount(string? text, out decimal amount)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
    private async void OnVoid(object s, RoutedEventArgs e)
    {
        if (_selected is not { } tx)
        {
            Show("Hãy chọn giao dịch cần Void/Reverse");
            return;
        }

        if (_reverseEntryHandler is null || tx.HotelFolioId is not { } folioId || tx.HotelFolioEntryId is not { } entryId)
        {
            Show("Giao dịch này chưa có liên kết Folio thật để thực hiện Void/Reverse.");
            return;
        }

        if (tx.FolioRowVersion is not { Length: 8 } rowVersion)
        {
            Show("Thiếu rowversion của Folio. Vui lòng tải lại dữ liệu trước khi Void/Reverse.");
            return;
        }

        if (tx.Status is "Reversed" or "Voided")
        {
            Show("Giao dịch này đã được Void/Reverse, không thể thực hiện lại.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn Void/Reverse giao dịch {tx.Code}?\nHệ thống sẽ tạo bút toán đảo và lưu audit.",
            "Xác nhận Void/Reverse",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var result = await _reverseEntryHandler.HandleAsync(
                new ReverseFolioEntryCommand(folioId, rowVersion, entryId, $"Void/Reverse từ Transaction Center: {tx.Code}"),
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                Show(result.Error?.Message ?? "Không thể Void/Reverse giao dịch.");
                return;
            }

            _selected = null;
            await LoadTransactionsFromDatabaseAsync();
            Show("Đã tạo bút toán đảo thành công.");
        }
        catch (Exception ex)
        {
            Show($"Không thể Void/Reverse giao dịch thật: {ex.Message}");
        }
    }
    private void OnReconcile(object s, RoutedEventArgs e) => Show("Đối soát");
    private void OnApplyFilter(object s, RoutedEventArgs e) => RefreshPage(true);
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = ""; TypeFilter.SelectedIndex = 0; StFilter.SelectedIndex = 0; DeptFilter.SelectedIndex = 0; RecFilter.SelectedIndex = 0; _typeFilter = "All"; BuildNavPanel(); RefreshPage(true); }
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_page > 0) { _page--; RefreshPage(false); } }
    private void OnNextPage(object s, RoutedEventArgs e) { _page++; RefreshPage(false); }
    private void OnPageSizeChanged(object s, SelectionChangedEventArgs e) { _pageSize = PageSizeBox.SelectedIndex switch { 0 => 50, 1 => 100, 2 => 200, _ => 50 }; RefreshPage(true); }
    private void OnRefresh(object s, RoutedEventArgs e) { _selected = null; InitFilters(); BuildKpi(); BuildNavPanel(); InitInspectorTabs(); RefreshPage(true); }
    private void OnExport(object s, RoutedEventArgs e)
    {
        var rows = GetFiltered();
        if (rows.Count == 0)
        {
            Show("Không có giao dịch Folio thật phù hợp bộ lọc để xuất.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = $"transactions-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Code,Time,BusinessDate,Type,Group,Description,Guest,Room,Folio,Amount,Currency,Status,Source");
            foreach (var tx in rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(tx.Code),
                    Csv(tx.Time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
                    Csv(tx.BusinessDate.ToString("yyyy-MM-dd")),
                    Csv(tx.Type),
                    Csv(tx.Group),
                    Csv(tx.Description),
                    Csv(tx.Guest),
                    Csv(tx.Room),
                    Csv(tx.Folio),
                    Csv(tx.Net.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    Csv(tx.Currency),
                    Csv(tx.Status),
                    Csv(tx.SourceSystem)
                }));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Show($"Đã xuất {rows.Count:N0} giao dịch Folio thật.");
        }
        catch (Exception ex)
        {
            Show("Không thể xuất giao dịch thật: " + ex.Message);
        }
    }

    private static string Csv(string? value) =>
        $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private void OnClose(object s, RoutedEventArgs e) => Close();
    private static void Show(string a) => MessageBox.Show(a, "Giao dịch", MessageBoxButton.OK, MessageBoxImage.Information);

    private static FrameworkElement Kpi(string l, string v, string color) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };
    private static FrameworkElement Btm(string l, string v, string c) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 12, Foreground = C("#64748B"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = v, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C(c), VerticalAlignment = VerticalAlignment.Center } } };
    private static TextBlock BtmSep() => new() { Text = "  ·  ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 6) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsTx(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record TxTransaction(int Id, string Code, DateTime Time, DateTime BusinessDate, string Type, string Group, string Description, string Dept, string Outlet, string Guest, string Room, string? Reservation, string Folio, string Payer, int Qty, int Gross, int Discount, int ServiceCharge, int Tax, int Fee, int Net, string Currency, decimal ExchangeRate, string Operator, string Cashier, string Shift, string Status, string RecStatus, string? OriginalTxn, bool HasWarning, bool IsHighValue, bool HasSyncError, string SourceSystem, int FolioId, Guid? HotelFolioId = null, Guid? HotelFolioEntryId = null, byte[]? FolioRowVersion = null);
internal static class TxExt { public static TextBlock WithRunsTx(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
