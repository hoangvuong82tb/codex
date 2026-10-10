using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Folio;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class MergeFolioWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step = 1;
    private MfFolio? _previewSource;
    private readonly List<MfFolio> _allFolios;
    private readonly HashSet<int> _selectedSourceIds = new();
    private MfFolio? _selectedTarget;
    private readonly List<MfCompatibilityRow> _compatRows = new();
    private string _transactionRule = "Giữ nguyên ID";
    private string _payerRule = "Giữ payer từng khoản", _depositRule = "Gộp toàn bộ deposit", _paymentRule = "Gộp payment history", _refundRule = "Gộp refund history";
    private string _currencyRule = "Giữ currency gốc", _taxRule = "Giữ nguyên từng khoản", _docRule = "Giữ reference nguồn";
    private string _sourcePostRule = "Đánh dấu đã gộp";
    private readonly List<MfWarning> _warnings = new();
    private readonly List<MfWarning> _blocking = new();
    private int _sourcePage, _sourcePageSize = 20;
    private readonly List<MfMergeRecord> _mergeHistory = new();
    private readonly HotelFolioApplicationService? _folioService;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private bool _persistedMode;

    public MergeFolioWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner ? owner : null;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioService = services?.GetService<HotelFolioApplicationService>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _persistedMode = false;
        _allFolios = new List<MfFolio>();
        InitFilters();
        GoTo(1);
        Loaded += async (_, _) => await LoadPersistedFoliosAsync();
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async Task LoadPersistedFoliosAsync()
    {
        if (_folioService is null || _contextAccessor?.Current is not { } context)
        {
            _persistedMode = false;
            MessageBox.Show("Chưa cấu hình được Folio service và property context dữ liệu thật.", "Gộp Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _persistedMode = true;

        try
        {
            var page = await _folioService!.SearchAsync(
                new SearchFoliosQuery(
                    new FolioSearchFilter(context.Property.PropertyId.Value, null, null, null, null, null, null, null),
                    new PageRequest(1, 500)),
                CancellationToken.None);
            if (page.IsFailure || page.Value is null)
            {
                _allFolios.Clear();
                GoTo(1);
                MessageBox.Show(page.Error?.Message ?? "Không thể tải Folio thật.", "Gộp Folio", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _allFolios.Clear();
            var id = 0;
            foreach (var ledger in page.Value.Items)
            {
                _allFolios.Add(MapPersistedFolio(++id, ledger));
            }
            _selectedSourceIds.Clear();
            _selectedTarget = null;
            GoTo(1);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải Folio thật: " + ex.Message, "Gộp Folio", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static MfFolio MapPersistedFolio(int id, FolioLedgerDto ledger)
    {
        var charges = ledger.Entries.Where(IsCharge).Sum(x => x.Amount);
        var deposits = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.Deposit).Sum(x => -x.Amount);
        var payments = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.Payment).Sum(x => -x.Amount);
        var refunds = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.Refund).Sum(x => x.Amount);
        var payer = !string.IsNullOrWhiteSpace(ledger.CompanyName) ? "Company" : !string.IsNullOrWhiteSpace(ledger.TravelAgentName) ? "TA" : "Guest";
        var billingInstruction = !string.IsNullOrWhiteSpace(ledger.CompanyName)
            ? "Route to Company"
            : !string.IsNullOrWhiteSpace(ledger.TravelAgentName) ? "Route to TA" : "Self-pay";
        return new MfFolio(
            id,
            ledger.FolioNumber,
            "Folio",
            ledger.GuestName,
            ledger.RoomNumber,
            ledger.ReservationNumber ?? "—",
            null,
            null,
            ToInt(charges),
            ToInt(deposits),
            ToInt(payments),
            ToInt(refunds),
            "VND",
            ledger.Status.ToString(),
            payer,
            ledger.CompanyName,
            ledger.TravelAgentName,
            0,
            ledger.Entries.Count,
            0,
            0,
            billingInstruction,
            "Standard",
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            ledger.ArrivalDate?.ToString("dd/MM/yyyy") ?? "—",
            ledger.DepartureDate?.ToString("dd/MM/yyyy") ?? "—",
            ledger.HotelFolioId,
            ledger.RowVersion);
    }

    private static bool IsCharge(HotelFolioEntryDto entry) => entry.EntryType is
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Laundry or
        HotelFolioEntryType.MiniBar or HotelFolioEntryType.Spa or HotelFolioEntryType.Telephone or
        HotelFolioEntryType.OtherCharge or HotelFolioEntryType.Tax or HotelFolioEntryType.ServiceCharge or
        HotelFolioEntryType.Adjustment or HotelFolioEntryType.Correction or HotelFolioEntryType.ManualCharge;

    private static int ToInt(decimal value) => decimal.ToInt32(decimal.Round(value, 0, MidpointRounding.AwayFromZero));


    private void InitFilters()
    {
        SourceTypeFilter.ItemsSource = new[] { "Tất cả", "Master", "Room", "Guest", "Restaurant", "Spa", "Laundry", "Extra", "Company", "Agent", "Incidentals" };
        SourceTypeFilter.SelectedIndex = 0;
        SourceCurrFilter.ItemsSource = new[] { "Tất cả", "VND", "USD" };
        SourceCurrFilter.SelectedIndex = 0;
        SourceStatusFilter.ItemsSource = new[] { "Tất cả", "Open", "Settled", "Locked", "Disputed", "Closed" };
        SourceStatusFilter.SelectedIndex = 0;
        SourcePageSizeBox.ItemsSource = new[] { "20 / trang", "50 / trang", "100 / trang" };
        SourcePageSizeBox.SelectedIndex = 0;
    }

    // ═══════════════════════════════════ NAVIGATION ═══════════════════════════════════

    private void GoTo(int s)
    {
        _step = Math.Clamp(s, 1, 6);
        BuildStepBar();
        HideAllSteps();
        switch (_step)
        {
            case 1: StepAContent.Visibility = Visibility.Visible; break;
            case 2: StepBContent.Visibility = Visibility.Visible; RenderStepB(); break;
            case 3: StepCContent.Visibility = Visibility.Visible; RenderStepC(); break;
            case 4: StepDContent.Visibility = Visibility.Visible; RenderStepD(); break;
            case 5: StepEContent.Visibility = Visibility.Visible; RenderStepE(); break;
            case 6: StepFContent.Visibility = Visibility.Visible; RenderStepF(); break;
        }
        UpdateNav(); UpdateSummaryStrip();
    }

    private void HideAllSteps()
    {
        StepAContent.Visibility = StepBContent.Visibility = StepCContent.Visibility = StepDContent.Visibility = StepEContent.Visibility = StepFContent.Visibility = Visibility.Collapsed;
    }

    private void BuildStepBar()
    {
        StepBar.Children.Clear();
        var steps = new[] { ("A", "Folio nguồn"), ("B", "Tương thích"), ("C", "Folio đích"), ("D", "Quy tắc gộp"), ("E", "Đối chiếu"), ("F", "Xác nhận") };
        for (int i = 0; i < 6; i++)
        {
            var idx = i + 1; var active = idx == _step;
            var bad = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(2, 5, 2, 5), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = active ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = active ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) } } } };
            bad.MouseLeftButtonDown += (_, _) => GoTo(idx); StepBar.Children.Add(bad);
            if (i < 5) StepBar.Children.Add(Sep());
        }
    }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = !HasBlocking(); }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        var stepNames = new[] { "Chọn Folio nguồn", "Kiểm tra tương thích", "Chọn Folio đích", "Quy tắc gộp", "Đối chiếu trước/sau", "Xác nhận" };
        var srcCount = _selectedSourceIds.Count;
        var total = SourceTotal();
        FooterStepLabel.Text = $"Bước {_step}/6 · {stepNames[_step - 1]} · Nguồn: {srcCount} Folio · Tổng: {total:N0} · Đích: {_selectedTarget?.Code ?? "—"}";
    }

    private bool HasBlocking()
    {
        if (_selectedSourceIds.Count < 2) return true;
        if (_selectedTarget == null) return true;
        return _blocking.Count > 0;
    }

    private int SourceTotal()
    {
        return _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).Sum(f => f.Total);
    }

    private void UpdateSummaryStrip()
    {
        SummaryStrip.Children.Clear();
        var srcCount = _selectedSourceIds.Count;
        var total = SourceTotal();
        var targetCode = _selectedTarget?.Code;
        var sel = srcCount >= 2;
        var rooms = string.Join(", ", _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).Select(f => f.Room).Distinct().Take(3));
        var names = string.Join(", ", _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).Select(f => f.GuestName).Distinct().Take(2));
        var payerCount = _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).Select(f => f.Payer).Distinct().Count();
        var itemCount = _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).Sum(f => f.ItemCount);

        SummaryStrip.Children.Add(Strip("Khách", sel ? names : "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", sel ? rooms : "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Nguồn", sel ? $"{srcCount} Folio" : "0", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Tổng nguồn", sel ? $"{total:N0}" : "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đích", targetCode ?? "—", targetCode != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Giao dịch", sel ? $"{itemCount}" : "0", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Payer", sel ? $"{payerCount}" : "0", sel)); SummaryStrip.Children.Add(Sep());
        var ready = sel && !HasBlocking();
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Sẵn sàng" : "Cần kiểm tra", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    // ═══════════════════════════════════ STEP A — SOURCE FOLIOS ═══════════════════════════════════

    private List<MfFolio> GetFilteredSources()
    {
        var f = _allFolios.AsEnumerable();
        var kw = SourceSearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(x => x.GuestName.ToLower().Contains(kw) || x.Room.Contains(kw) || x.Code.ToLower().Contains(kw) || (x.GroupCode?.ToLower().Contains(kw) ?? false));
        var type = SourceTypeFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(type) && type != "Tất cả") f = f.Where(x => x.Type == type);
        var curr = SourceCurrFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(curr) && curr != "Tất cả") f = f.Where(x => x.Currency == curr);
        var status = SourceStatusFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(status) && status != "Tất cả") f = f.Where(x => x.Status == status);
        return f.ToList();
    }

    private void RefreshSourcePage(bool reset)
    {
        if (reset) _sourcePage = 0;
        var f = GetFilteredSources();
        var tp = Math.Max(1, (f.Count + _sourcePageSize - 1) / _sourcePageSize);
        if (_sourcePage >= tp) _sourcePage = tp - 1;
        SourceGrid.ItemsSource = f.Skip(_sourcePage * _sourcePageSize).Take(_sourcePageSize).ToList();
        SourcePagerSummary.Text = $"Hiển thị {_sourcePage * _sourcePageSize + 1}–{Math.Min((_sourcePage + 1) * _sourcePageSize, f.Count)} / {f.Count} Folio";
        SourcePrevBtn.IsEnabled = _sourcePage > 0; SourceNextBtn.IsEnabled = _sourcePage < tp - 1;
        BuildSourcePageBtns(tp);
    }

    private void BuildSourcePageBtns(int tp) { SourcePageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = PagerBtn(i, _sourcePage, idx); nb.Click += (_, _) => { _sourcePage = idx; RefreshSourcePage(false); }; SourcePageBtns.Children.Add(nb); } }

    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 28, Height = 26, FontSize = 11, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = SourcePrevBtn.Template };

    private void OnSourceSelected(object s, SelectionChangedEventArgs e)
    {
        if (SourceGrid.SelectedItem is not MfFolio f) return;
        _previewSource = f;
        SourceEmptyState.Visibility = Visibility.Collapsed; SourceDetailPanel.Visibility = Visibility.Visible;
        SetDetail(SdCode, "Mã Folio", f.Code, "#17365D", true);
        SetDetail(SdType, "Loại", f.Type); SetDetail(SdGuest, "Khách", f.GuestName); SetDetail(SdRoom, "Phòng", f.Room);
        SetDetail(SdRes, "Hồ sơ", f.Reservation); SetDetail(SdGroup, "Đoàn", f.GroupCode ?? "—");
        SetDetail(SdPayer, "Người thanh toán", f.Payer); SetDetail(SdCompany, "Công ty", f.Company ?? "—"); SetDetail(SdTA, "Đại lý du lịch", f.TA ?? "—");
        SetDetail(SdCurr, "Tiền tệ", f.Currency); SetDetail(SdBilling, "Chỉ dẫn thanh toán", f.BillingInstruction); SetDetail(SdTax, "Hồ sơ thuế", f.TaxProfile);
        SetDetail(SdTotal, "Tổng nợ", $"{f.Total:N0}", "#17365D", true);
        SetDetail(SdPaid, "Đã thanh toán", $"{f.Paid:N0}", "#16A34A", true);
        SetDetail(SdDeposit, "Đặt cọc", $"{f.Deposit:N0}", "#F59E0B", true);
        SetDetail(SdRefund, "Hoàn tiền", $"{f.Refund:N0}", "#8B5CF6", true);
        SetDetail(SdBalance, "Số dư", $"{f.Total - f.Paid - f.Deposit:N0}", "#DC2626", true);
        SetDetail(SdItems, "Số khoản mục", $"{f.ItemCount}");
        SetDetail(SdLocked, "Khoản bị khóa", $"{f.LockedCount}", "#DC2626");
        SetDetail(SdStatus, "Trạng thái", f.Status, "#2F6BFF", true);

        var selected = _selectedSourceIds.Contains(f.Id);
        var canSelect = f.Status != "Closed" && !f.HasNightAuditLock && !f.HasPendingSettlement;
        BtnToggleSource.Content = selected ? "✕ Bỏ chọn" : "✓ Chọn Folio này";
        BtnToggleSource.IsEnabled = canSelect || selected;
        if (!canSelect && !selected) BtnToggleSource.Content = f.Status == "Closed" ? "Đã đóng" : f.HasNightAuditLock ? "Night Audit lock" : "Đang xử lý";
        UpdateSummaryStrip();
    }

    private void OnToggleSource(object s, RoutedEventArgs e)
    {
        if (_previewSource == null) return;
        var id = _previewSource.Id;
        if (_selectedSourceIds.Contains(id)) { _selectedSourceIds.Remove(id); BtnToggleSource.Content = "✓ Chọn Folio này"; }
        else { _selectedSourceIds.Add(id); BtnToggleSource.Content = "✕ Bỏ chọn"; }
        RefreshSelectedPanel(); UpdateSummaryStrip(); UpdateNav();
    }

    private void RefreshSelectedPanel()
    {
        SelPanel.Children.Clear();
        var selected = _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).ToList();
        if (selected.Count < 2)
        {
            SelPanel.Children.Add(new TextBlock { Text = "Cần chọn ít nhất hai Folio để gộp.", FontSize = 12, Foreground = C("#F59E0B"), Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap });
        }
        foreach (var f in selected)
        {
            var b = new Border { Background = Cb("#EFF6FF"), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 0, 3), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = $"{f.Code} · {f.Type} · {f.Room}", FontSize = 11, Foreground = C("#17365D"), Width = 200 }, new TextBlock { Text = $"{f.Total:N0} {f.Currency}", FontSize = 11, Foreground = C("#64748B") } } } };
            var fid = f.Id; b.MouseLeftButtonDown += (_, _) => { _selectedSourceIds.Remove(fid); RefreshSelectedPanel(); UpdateSummaryStrip(); UpdateNav(); };
            b.Cursor = System.Windows.Input.Cursors.Hand; b.ToolTip = "Click để bỏ chọn";
            SelPanel.Children.Add(b);
        }
        if (selected.Count > 0)
        {
            var total = selected.Sum(f => f.Total); var paid = selected.Sum(f => f.Paid); var dep = selected.Sum(f => f.Deposit);
            SelPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 4) });
            SelPanel.Children.Add(Metric("Tổng Nợ", $"{total:N0}"));
            SelPanel.Children.Add(Metric("Tổng Có", $"{paid + dep:N0}"));
            SelPanel.Children.Add(Metric("Balance", $"{total - paid - dep:N0}"));
        }
    }

    private void OnSelectAllSameStay(object s, RoutedEventArgs e) { var first = _allFolios.FirstOrDefault(f => _selectedSourceIds.Contains(f.Id)); if (first != null) { foreach (var f in _allFolios.Where(x => x.Reservation == first.Reservation && x.Status != "Closed")) _selectedSourceIds.Add(f.Id); } RefreshSourcePage(false); RefreshSelectedPanel(); UpdateSummaryStrip(); UpdateNav(); }
    private void OnSelectAllSameGroup(object s, RoutedEventArgs e) { var first = _allFolios.FirstOrDefault(f => _selectedSourceIds.Contains(f.Id)); if (first?.GroupCode != null) { foreach (var f in _allFolios.Where(x => x.GroupCode == first.GroupCode && x.Status != "Closed")) _selectedSourceIds.Add(f.Id); } RefreshSourcePage(false); RefreshSelectedPanel(); UpdateSummaryStrip(); UpdateNav(); }
    private void OnDeselectAll(object s, RoutedEventArgs e) { _selectedSourceIds.Clear(); RefreshSourcePage(false); RefreshSelectedPanel(); UpdateSummaryStrip(); UpdateNav(); }
    private void OnApplySourceFilter(object s, RoutedEventArgs e) => RefreshSourcePage(true);
    private void OnClearSourceFilter(object s, RoutedEventArgs e) { SourceSearchBox.Text = ""; SourceTypeFilter.SelectedIndex = 0; SourceCurrFilter.SelectedIndex = 0; SourceStatusFilter.SelectedIndex = 0; RefreshSourcePage(true); }
    private void OnSourcePrevPage(object s, RoutedEventArgs e) { if (_sourcePage > 0) { _sourcePage--; RefreshSourcePage(false); } }
    private void OnSourceNextPage(object s, RoutedEventArgs e) { _sourcePage++; RefreshSourcePage(false); }
    private void OnSourcePageSizeChanged(object s, SelectionChangedEventArgs e) { _sourcePageSize = SourcePageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshSourcePage(true); }

    // ═══════════════════════════════════ STEP B — COMPATIBILITY ═══════════════════════════════════

    private void RenderStepB()
    {
        CompLeftPanel.Children.Clear(); CompRightPanel.Children.Clear();
        var sources = _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).ToList();
        if (sources.Count < 2) { CompLeftPanel.Children.Add(new TextBlock { Text = "Cần chọn ít nhất hai Folio nguồn.", FontSize = 13, Foreground = C("#F59E0B") }); return; }

        CompLeftPanel.Children.Add(STitle("Ma trận tương thích"));
        // Build header row
        var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        foreach (var _ in sources) headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        AddCell(headerGrid, 0, "Tiêu chí", true);
        for (int i = 0; i < sources.Count; i++) AddCell(headerGrid, i + 1, sources[i].Code, true);
        AddCell(headerGrid, sources.Count + 1, "Kết luận", true);
        CompLeftPanel.Children.Add(headerGrid);

        // Build compatibility rows
        var criteria = new (string, Func<MfFolio, string>, Func<List<MfFolio>, string>)[] {
            ("Hồ sơ", f => f.Reservation, list => list.Select(x=>x.Reservation).Distinct().Count()==1?"Tương thích":"Khác hồ sơ"),
            ("Khách", f => f.GuestName, list => list.Select(x=>x.GuestName).Distinct().Count()==1?"Tương thích":"Khác khách"),
            ("Phòng", f => f.Room, list => "OK"),
            ("Đoàn", f => f.GroupCode ?? "—", list => "OK"),
            ("Payer", f => f.Payer, list => list.Select(x=>x.Payer).Distinct().Count()==1?"Tương thích":"Cần mapping"),
            ("Company", f => f.Company ?? "—", list => list.Select(x=>x.Company).Distinct().Count()==1?"Tương thích":"Cần approval"),
            ("TA", f => f.TA ?? "—", list => list.Select(x=>x.TA).Distinct().Count()==1?"Tương thích":"Cần approval"),
            ("Currency", f => f.Currency, list => list.Select(x=>x.Currency).Distinct().Count()==1?"Tương thích":"Cần conversion"),
            ("Tax Profile", f => f.TaxProfile, list => list.Select(x=>x.TaxProfile).Distinct().Count()==1?"Tương thích":"Khác profile"),
            ("Billing", f => f.BillingInstruction, list => "OK"),
            ("Trạng thái", f => f.Status, list => "OK"),
        };

        _compatRows.Clear();
        foreach (var (label, valueFn, conclusionFn) in criteria)
        {
            var rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            foreach (var _ in sources) rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            AddCell(rowGrid, 0, label, false);
            for (int i = 0; i < sources.Count; i++) AddCell(rowGrid, i + 1, valueFn(sources[i]), false);
            var conclusion = conclusionFn(sources);
            var color = conclusion.Contains("Tương thích") ? "#16A34A" : conclusion == "OK" ? "#64748B" : "#F59E0B";
            AddCell(rowGrid, sources.Count + 1, conclusion, false, color);
            CompLeftPanel.Children.Add(rowGrid);
            _compatRows.Add(new MfCompatibilityRow(label, conclusion, conclusion.Contains("Tương thích") || conclusion == "OK"));
        }

        // Right: warnings
        CompRightPanel.Children.Add(STitle("Kết quả kiểm tra"));
        _warnings.Clear(); _blocking.Clear();
        var currencies = sources.Select(x => x.Currency).Distinct().ToList();
        if (currencies.Count > 1) _warnings.Add(new MfWarning("warn", $"Khác currency: {string.Join(", ", currencies)}", 2));
        var payers = sources.Select(x => x.Payer).Distinct().ToList();
        if (payers.Count > 1) _warnings.Add(new MfWarning("warn", $"Khác payer: {string.Join(", ", payers)}", 2));
        if (sources.Any(x => x.Status == "Locked")) _blocking.Add(new MfWarning("block", "Có Folio bị khóa", 1));
        if (sources.Any(x => x.HasChargeback)) _warnings.Add(new MfWarning("warn", "Có Chargeback", 2));
        if (sources.Any(x => x.HasPendingSettlement)) _blocking.Add(new MfWarning("block", "Có Folio đang settle", 1));

        foreach (var w in _blocking.Concat(_warnings))
        {
            var isBlock = w.Level == "block";
            CompRightPanel.Children.Add(new Border { Background = isBlock ? Cb("#FEE2E2") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 4), Child = new TextBlock { Text = (isBlock ? "✕ " : "⚠ ") + w.Message, FontSize = 11, Foreground = isBlock ? C("#DC2626") : C("#A16207") } });
        }

        // Approval
        CompRightPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        CompRightPanel.Children.Add(STitle("Approval"));
        var needApproval = currencies.Count > 1 || payers.Count > 1 || sources.Any(x => x.HasChargeback);
        CompRightPanel.Children.Add(new TextBlock { Text = needApproval ? "⚠ Cần approval để gộp các Folio này" : "✓ Không cần approval đặc biệt", FontSize = 12, Foreground = needApproval ? C("#F59E0B") : C("#16A34A"), TextWrapping = TextWrapping.Wrap });
        UpdateNav();
    }

    private static void AddCell(Grid g, int col, string text, bool isHeader, string? color = null)
    {
        var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal, Foreground = isHeader ? C("#102E5A") : (color != null ? C(color) : C("#334155")), Margin = new Thickness(4, 3, 4, 3) };
        Grid.SetColumn(tb, col); g.Children.Add(tb);
    }

    // ═══════════════════════════════════ STEP C — TARGET FOLIO ═══════════════════════════════════

    private void RenderStepC()
    {
        TargetListPanel.Children.Clear(); TargetPreviewPanel.Children.Clear();
        var candidates = _allFolios.Where(f => !_selectedSourceIds.Contains(f.Id) && f.Status is "Open" or "Suspended").OrderByDescending(f => f.Type == "Master" ? 3 : f.Type == "Room" ? 2 : 1).ThenByDescending(f => f.CreditLimit).Take(5).ToList();
        TargetListPanel.Children.Add(STitle("Ứng viên Folio đích"));
        foreach (var f in candidates)
        {
            var score = f.Type == "Master" ? "Rất phù hợp" : f.Type == "Room" ? "Phù hợp" : "Cần điều chỉnh";
            var b = new Border { Background = _selectedTarget?.Id == f.Id ? Cb("#EFF6FF") : Cb("#FAFBFD"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 4), BorderBrush = _selectedTarget?.Id == f.Id ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand, Child = new StackPanel { Children = { new TextBlock { Text = $"{f.Code} · {f.Type} · {f.GuestName}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, new TextBlock { Text = $"Phòng {f.Room} · {f.Currency} · {f.Payer} · {score}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) } } } };
            var fid = f.Id; b.MouseLeftButtonDown += (_, _) => PreviewTarget(fid); TargetListPanel.Children.Add(b);
        }

    }

    private void PreviewTarget(int id)
    {
        _selectedTarget = _allFolios.FirstOrDefault(f => f.Id == id);
        RenderStepC();
        TargetPreviewPanel.Children.Clear(); if (_selectedTarget == null) return;
        TargetPreviewPanel.Children.Add(STitle("Preview Folio đích"));
        var afterTotal = SourceTotal() + _selectedTarget.Total;
        TargetPreviewPanel.Children.Add(Row("Mã", _selectedTarget.Code)); TargetPreviewPanel.Children.Add(Row("Loại", _selectedTarget.Type));
        TargetPreviewPanel.Children.Add(Row("Khách", _selectedTarget.GuestName)); TargetPreviewPanel.Children.Add(Row("Currency", _selectedTarget.Currency));
        TargetPreviewPanel.Children.Add(Row("Balance hiện tại", $"{_selectedTarget.Total - _selectedTarget.Paid - _selectedTarget.Deposit:N0}"));
        TargetPreviewPanel.Children.Add(Row("Tổng sau gộp", $"{afterTotal:N0}"));
        TargetPreviewPanel.Children.Add(Row("Số khoản sau", $"{_selectedTarget.ItemCount + _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).Sum(f => f.ItemCount)}"));
        var selBtn = new Button { Content = "Chọn làm Folio đích", Style = FindResource("PrimaryButton") as Style, Margin = new Thickness(0, 8, 0, 0), Height = 30, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Left };
        selBtn.Click += (_, _) => { UpdateSummaryStrip(); UpdateNav(); };
        TargetPreviewPanel.Children.Add(selBtn);
    }

    // ═══════════════════════════════════ STEP D — MERGE RULES ═══════════════════════════════════

    private void RenderStepD()
    {
        RuleLeftPanel.Children.Clear(); RuleMiddlePanel.Children.Clear(); RuleRightPanel.Children.Clear(); RuleSummaryPanel.Children.Clear();
        var rules = new (string, string, string[])[] {
            ("Transaction", "Giao dịch", new[]{"Giữ nguyên ID","Tạo merge reference","Giữ ngày hạch toán","Giữ operator","Nhóm theo thời gian"}),
            ("Payer", "Payer", new[]{"Giữ payer từng khoản","Đổi về payer đích","Mapping theo loại DV","Theo Billing Instruction"}),
            ("Deposit", "Đặt cọc", new[]{"Gộp toàn bộ deposit","Giữ theo nguồn","Phân bổ lại theo payer","Giữ refundable status"}),
            ("Payment", "Thanh toán", new[]{"Gộp payment history","Giữ receipt reference","Giữ payment method","Chuyển allocation"}),
            ("Refund", "Hoàn tiền", new[]{"Gộp refund history","Giữ refund reference","Giữ recipient","Không gộp tranh chấp"}),
            ("Currency", "Currency", new[]{"Giữ currency gốc","Chuyển về currency đích","Dùng tỷ giá GD","Dùng tỷ giá hiện tại"}),
            ("Tax/Package", "Thuế/Package", new[]{"Giữ nguyên từng khoản","Giữ quan hệ gốc","Cảnh báo khác profile","Mapping vào đích"}),
            ("Document", "Tài liệu", new[]{"Giữ reference nguồn","Gắn alias Folio cũ","Chuyển vào đích","Giữ tại nguồn + liên kết"}) };

        var columns = new Panel[] { RuleLeftPanel, RuleMiddlePanel, RuleRightPanel };
        var columnByRule = new[] { 0, 0, 0, 1, 1, 1, 2, 2 };
        for (var ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
        {
            var (key, label, options) = rules[ruleIndex];
            var panel = columns[columnByRule[ruleIndex]];
            var currentValue = key switch { "Transaction" => _transactionRule, "Payer" => _payerRule, "Deposit" => _depositRule, "Payment" => _paymentRule, "Refund" => _refundRule, "Currency" => _currencyRule, "Tax/Package" => _taxRule, "Document" => _docRule, _ => "Keep" };
            panel.Children.Add(STitle(label));
            foreach (var opt in options)
            {
                var active = currentValue == opt;
                var b = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = opt, FontSize = 11, TextWrapping = TextWrapping.Wrap, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } };
                b.MouseLeftButtonDown += (_, _) =>
                {
                    switch (key) { case "Transaction": _transactionRule = opt; break; case "Payer": _payerRule = opt; break; case "Deposit": _depositRule = opt; break; case "Payment": _paymentRule = opt; break; case "Refund": _refundRule = opt; break; case "Currency": _currencyRule = opt; break; case "Tax/Package": _taxRule = opt; break; case "Document": _docRule = opt; break; }
                    RenderStepD();
                };
                panel.Children.Add(b);
            }
        }

        RuleSummaryPanel.Children.Add(STitle("Xử lý Folio nguồn sau gộp"));
        var postOptions = new[] { "Đánh dấu đã gộp", "Đóng Folio nguồn", "Giữ tham chiếu", "Alias tới đích" };
        foreach (var opt in postOptions)
        {
            var active = _sourcePostRule == opt;
            var b = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = opt, FontSize = 11, TextWrapping = TextWrapping.Wrap, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } };
            b.MouseLeftButtonDown += (_, _) => { _sourcePostRule = opt; RenderStepD(); };
            RuleSummaryPanel.Children.Add(b);
        }
        RuleSummaryPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        RuleSummaryPanel.Children.Add(STitle("Tóm tắt quy tắc"));
        var summaryGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2 };
        summaryGrid.Children.Add(RuleSummary("Payer", _payerRule)); summaryGrid.Children.Add(RuleSummary("Deposit", _depositRule));
        summaryGrid.Children.Add(RuleSummary("Payment", _paymentRule)); summaryGrid.Children.Add(RuleSummary("Refund", _refundRule));
        summaryGrid.Children.Add(RuleSummary("Currency", _currencyRule)); summaryGrid.Children.Add(RuleSummary("Nguồn sau gộp", _sourcePostRule));
        RuleSummaryPanel.Children.Add(summaryGrid);
    }

    // ═══════════════════════════════════ STEP E — RECONCILIATION ═══════════════════════════════════

    private void RenderStepE()
    {
        ReconcileLeftPanel.Children.Clear(); ReconcileRightPanel.Children.Clear();
        var sources = _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).ToList();
        var totalBefore = sources.Sum(f => f.Total);
        var paidBefore = sources.Sum(f => f.Paid);
        var depBefore = sources.Sum(f => f.Deposit);
        var refBefore = sources.Sum(f => f.Refund);
        var balBefore = totalBefore - paidBefore - depBefore;
        var itemsBefore = sources.Sum(f => f.ItemCount);

        ReconcileLeftPanel.Children.Add(STitle("Trước khi gộp"));
        foreach (var f in sources)
        {
            ReconcileLeftPanel.Children.Add(new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 0, 2), Child = new TextBlock { Text = $"{f.Code} · {f.Type} · {f.Room} · {f.Total:N0} {f.Currency} · Bal: {f.Total - f.Paid - f.Deposit:N0}", FontSize = 11, Foreground = C("#334155") } });
        }
        ReconcileLeftPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 4) });
        ReconcileLeftPanel.Children.Add(Row("Tổng Nợ nguồn", $"{totalBefore:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Tổng Có nguồn", $"{paidBefore + depBefore:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Deposit", $"{depBefore:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Payment", $"{paidBefore:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Refund", $"{refBefore:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Balance", $"{balBefore:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Số giao dịch", $"{itemsBefore}"));

        var targetTotal = SourceTotal() + (_selectedTarget?.Total ?? 0);
        var diff = totalBefore - SourceTotal();

        ReconcileLeftPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        ReconcileLeftPanel.Children.Add(STitle("Sau khi gộp"));
        ReconcileLeftPanel.Children.Add(Row("Folio đích", (_selectedTarget?.Code ?? "—")));
        ReconcileLeftPanel.Children.Add(Row("Tổng sau gộp", $"{targetTotal:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Nguồn sau gộp", _sourcePostRule));
        ReconcileLeftPanel.Children.Add(new Border { Background = diff == 0 ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = diff == 0 ? "✓ Tổng trước/sau cân" : $"⚠ Chênh lệch: {diff:N0}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = diff == 0 ? C("#16A34A") : C("#DC2626") } });

        // Duplicate detection + Warning center
        ReconcileRightPanel.Children.Add(STitle("Phát hiện trùng lặp"));
        var dupes = _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).GroupBy(f => f.Reservation).Where(g => g.Count() > 1).ToList();
        if (dupes.Count > 0) foreach (var d in dupes) ReconcileRightPanel.Children.Add(new TextBlock { Text = $"⚠ Cùng hồ sơ: {d.Key} — {d.Count()} Folio", FontSize = 11, Foreground = C("#F59E0B"), Margin = new Thickness(0, 2, 0, 0) });
        else ReconcileRightPanel.Children.Add(new TextBlock { Text = "✓ Không phát hiện trùng lặp", FontSize = 11, Foreground = C("#16A34A"), Margin = new Thickness(0, 0, 0, 6) });

        ReconcileRightPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 4) });
        ReconcileRightPanel.Children.Add(STitle("Warning Center"));
        foreach (var w in _blocking.Concat(_warnings))
        {
            var isBlock = w.Level == "block";
            ReconcileRightPanel.Children.Add(new Border { Background = isBlock ? Cb("#FEE2E2") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 4), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = isBlock ? "✕ " : "⚠ ", FontSize = 12, Foreground = isBlock ? C("#DC2626") : C("#A16207"), VerticalAlignment = VerticalAlignment.Center }, new StackPanel { Children = { new TextBlock { Text = w.Message, FontSize = 11, Foreground = isBlock ? C("#DC2626") : C("#A16207") }, new TextBlock { Text = $"Bước {w.Step}", FontSize = 10, Foreground = C("#94A3B8"), Margin = new Thickness(0, 1, 0, 0) } } } } } });
        }
        if (_warnings.Count == 0 && _blocking.Count == 0)
            ReconcileRightPanel.Children.Add(new TextBlock { Text = "✓ Không có warning.", FontSize = 11, Foreground = C("#16A34A"), Margin = new Thickness(0, 0, 0, 6) });
        UpdateNav();
    }

    // ═══════════════════════════════════ STEP F — CONFIRMATION ═══════════════════════════════════

    private void RenderStepF()
    {
        ConfirmLeftPanel.Children.Clear(); ConfirmRightPanel.Children.Clear();
        var sources = _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).ToList();
        var total = SourceTotal();

        ConfirmLeftPanel.Children.Add(STitle("Tóm tắt gộp Folio"));
        ConfirmLeftPanel.Children.Add(Row("Khách", string.Join(", ", sources.Select(f => f.GuestName).Distinct().Take(3))));
        ConfirmLeftPanel.Children.Add(Row("Phòng", string.Join(", ", sources.Select(f => f.Room).Distinct().Take(4))));
        ConfirmLeftPanel.Children.Add(Row("Số Folio nguồn", $"{sources.Count}"));
        ConfirmLeftPanel.Children.Add(Row("Tổng nguồn", $"{total:N0}"));
        ConfirmLeftPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 4) });
        ConfirmLeftPanel.Children.Add(Row("Folio đích", _selectedTarget?.Code ?? "—"));
        ConfirmLeftPanel.Children.Add(Row("Quy tắc payer", _payerRule));
        ConfirmLeftPanel.Children.Add(Row("Quy tắc currency", _currencyRule));
        ConfirmLeftPanel.Children.Add(Row("Nguồn sau gộp", _sourcePostRule));
        ConfirmLeftPanel.Children.Add(Row("Chênh lệch", "0"));

        // Readiness
        ConfirmRightPanel.Children.Add(STitle("Readiness"));
        var checks = new (string, bool)[] {
            ("Đã chọn ít nhất 2 Folio nguồn", _selectedSourceIds.Count >= 2),
            ("Các Folio nguồn hợp lệ", !sources.Any(f => f.Status == "Closed")),
            ("Đã kiểm tra compatibility", _compatRows.Count > 0),
            ("Đã chọn Folio đích", _selectedTarget != null),
            ("Payer mapping hợp lệ", true),
            ("Currency rule hợp lệ", true),
            ("Deposit/payment/refund hợp lệ", true),
            ("Không có duplicate blocking", true),
            ("Tổng trước/sau cân", true),
            ("Có approval nếu cần", true),
            ("Không có blocking issue", !HasBlocking())
        };
        var allOk = true;
        foreach (var (label, ok) in checks)
        {
            ConfirmRightPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 12, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 11, Foreground = C("#334155") } } });
            if (!ok) allOk = false;
        }
        ConfirmRightPanel.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng gộp Folio" : "Cần kiểm tra thêm", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
    }

    // ═══════════════════════════════════ CONFIRMATION ═══════════════════════════════════

    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (HasBlocking()) return;
        var sources = _allFolios.Where(f => _selectedSourceIds.Contains(f.Id)).ToList();
        if (!_persistedMode || _folioService is null || _contextAccessor?.Current is null)
        {
            MessageBox.Show("Không thể gộp Folio khi chưa có dữ liệu Folio thật và property context.", "Gộp Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_persistedMode)
        {
            if (_selectedTarget is null || _selectedTarget.FolioGuid == Guid.Empty || _selectedTarget.RowVersion is not { Length: 8 } || sources.Any(x => x.FolioGuid == Guid.Empty || x.RowVersion is not { Length: 8 }))
            {
                MessageBox.Show("Cần chọn Folio nguồn và Folio đích tồn tại trong dữ liệu thật, kèm rowversion hợp lệ.", "Gộp Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = await _folioService!.MergeAsync(
                new MergeFoliosCommand(
                    _selectedTarget.FolioGuid,
                    _selectedTarget.RowVersion,
                    sources.Select(x => new MergeFolioSource(x.FolioGuid, x.RowVersion!)).ToList(),
                    "Gộp Folio từ giao diện nghiệp vụ",
                    new FolioMergeRules(_transactionRule, _payerRule, _depositRule, _paymentRule, _refundRule, _currencyRule, _taxRule, _docRule, _sourcePostRule)),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể gộp Folio.", "Gộp Folio", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Đã gộp {sources.Count} Folio, tổng chuyển {result.Value!.TotalTransferred:N0}.", "Gộp Folio", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadPersistedFoliosAsync();
            return;
        }
        MessageBox.Show("Không thể gộp Folio: đường dữ liệu thật chưa sẵn sàng.", "Gộp Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // ═══════════════════════════════════ EVENT HANDLERS ═══════════════════════════════════

    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private void OnRefresh(object s, RoutedEventArgs e) { _selectedSourceIds.Clear(); _previewSource = null; _selectedTarget = null; _compatRows.Clear(); _warnings.Clear(); _blocking.Clear(); InitFilters(); GoTo(1); }
    private void OnClose(object s, RoutedEventArgs e) => Close();

    // ═══════════════════════════════════ HELPERS ═══════════════════════════════════

    private static FrameworkElement Metric(string l, string v) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 6, 4), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = C("#17365D") }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };
    private static FrameworkElement RuleSummary(string label, string value) => new Border { Background = Cb("#F8FAFC"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 5, 5), Child = new StackPanel { Children = { new TextBlock { Text = label, FontSize = 10, Foreground = C("#94A3B8") }, new TextBlock { Text = value, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D"), TextWrapping = TextWrapping.Wrap } } } };
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static void SetDetail(TextBlock target, string label, string value, string valueColor = "#334155", bool emphasize = false)
    {
        target.Inlines.Clear();
        target.FontSize = Math.Max(target.FontSize, 12);
        target.TextWrapping = TextWrapping.Wrap;
        target.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = C("#94A3B8") });
        target.Inlines.Add(new System.Windows.Documents.Run(value) { Foreground = C(valueColor), FontWeight = emphasize ? FontWeights.SemiBold : FontWeights.Normal });
    }
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsMg(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " › ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

// ═══════════════════════════════════ DATA RECORDS ═══════════════════════════════════

internal record MfFolio(int Id, string Code, string Type, string GuestName, string Room, string Reservation, string? GroupCode, string? GroupName, int Total, int Deposit, int Paid, int Refund, string Currency, string Status, string Payer, string? Company, string? TA, int CreditLimit, int ItemCount, int LockedCount, int DisputedCount, string BillingInstruction, string TaxProfile, bool HasInvoice, bool HasReceipt, bool IsAfterRoomMove, bool IsAfterSplit, bool HasPackage, bool HasChargeback, bool HasNightAuditLock, bool HasPendingSettlement, string Arrival, string Departure, Guid FolioGuid = default, byte[]? RowVersion = null) { public string TotalDisplay => $"{Total:N0}"; public string Balance => $"{Total - Paid - Deposit:N0}"; }

internal record MfCompatibilityRow(string Label, string Conclusion, bool IsCompatible);

internal record MfWarning(string Level, string Message, int Step);


internal record MfMergeRecord(DateTime Time, string BatchId, List<string> SourceCodes, string TargetCode, int SourceCount, int TotalAmount, string Operator, string Status);

internal static class MgExt { public static TextBlock WithRunsMg(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
