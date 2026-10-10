using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.App.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class CashierPaymentWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step;
    private CpFolio? _folio; private bool _selected;
    private int _folioPage, _folioPageSize = 20;
    private readonly List<CpFolio> _allFolios;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IHotelPaymentRepository? _paymentRepository;
    private readonly HotelPaymentApplicationService? _paymentService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly Dictionary<Guid, decimal> _allocatedByCharge = new();
    private CancellationTokenSource? _allocationLoadCts;
    private int _allocationLoadVersion;
    private bool _hasLoadedPersistedFolios;
    private decimal _total, _deposit, _paid, _due;
    private string _method = "Tiền mặt";
    private decimal _payAmount;
    private string _ref = string.Empty;

    public CashierPaymentWindow()
    {
        InitializeComponent(); Owner = System.Windows.Application.Current.MainWindow;
        _allFolios = new List<CpFolio>(); InitFilters(); RefreshFolioPage(true); GoTo(1);
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _paymentRepository = services?.GetService<IHotelPaymentRepository>();
        _paymentService = services?.GetService<HotelPaymentApplicationService>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        CancelAllocationLoad();
        if (_folioRepository is null || _paymentService is null || _executionContext?.Current is null)
        {
            _hasLoadedPersistedFolios = true;
            _allFolios.Clear();
            FolioEmptyState.Visibility = Visibility.Visible;
            FolioDetailPanel.Visibility = Visibility.Collapsed;
            FooterStepLabel.Text = "Chưa cấu hình được service thanh toán và property context dữ liệu thật.";
            RefreshFolioPage(true);
            return;
        }

        try
        {
            var propertyId = _executionContext.Current.Property.PropertyId.Value;
            var page = await _folioRepository.SearchAsync(
                new FolioSearchFilter(propertyId, null, null, null, null, null, null, null),
                new PageRequest(1, 200), CancellationToken.None);
            _allFolios.Clear();
            _allFolios.AddRange(page.Items.Select(MapPersistedFolio));
            _allocatedByCharge.Clear();
            _hasLoadedPersistedFolios = true;
            _folio = null; _selected = false;
            FolioDetailPanel.Visibility = Visibility.Collapsed;
            FolioEmptyState.Visibility = Visibility.Visible;
            RefreshFolioPage(true); UpdateSummary(); UpdateNav();
        }
        catch (Exception ex)
        {
            _hasLoadedPersistedFolios = true;
            _allFolios.Clear();
            FolioEmptyState.Visibility = Visibility.Visible;
            FolioDetailPanel.Visibility = Visibility.Collapsed;
            FooterStepLabel.Text = $"Không tải được Folio: {ex.Message}";
            RefreshFolioPage(true);
        }
    }

    private static CpFolio MapPersistedFolio(FolioLedgerDto ledger)
    {
        var entries = ledger.Entries.Where(x => !x.ReversedByEntryId.HasValue).ToList();
        var total = entries.Where(IsCharge).Sum(x => x.Amount);
        var deposits = entries.Where(x => x.EntryType == HotelFolioEntryType.Deposit).Sum(x => Math.Abs(x.Amount));
        var payments = entries.Where(x => x.EntryType == HotelFolioEntryType.Payment).Sum(x => Math.Abs(x.Amount));
        return new CpFolio(
            ledger.RoomNumber,
            ledger.GuestName,
            ledger.FolioNumber,
            total,
            deposits,
            payments,
            "—",
            ledger.Status switch
            {
                HotelFolioStatus.Settled => "Đã cân",
                HotelFolioStatus.Closed => "Đã đóng",
                _ => ledger.RunningBalance > 0 ? "Còn nợ" : "Đã cân"
            },
            false,
            ledger.HotelFolioId,
            ledger.RowVersion,
            entries);
    }

    private static bool IsCharge(HotelFolioEntryDto entry) => entry.EntryType is
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Laundry or
        HotelFolioEntryType.MiniBar or HotelFolioEntryType.Spa or HotelFolioEntryType.Telephone or
        HotelFolioEntryType.OtherCharge or HotelFolioEntryType.Tax or HotelFolioEntryType.ServiceCharge or
        HotelFolioEntryType.Discount or HotelFolioEntryType.Adjustment or HotelFolioEntryType.Correction or
        HotelFolioEntryType.ManualCharge or HotelFolioEntryType.ManualCredit;

    private void InitFilters() { Set(FolioVipFilter, "Tất cả"); Set(FolioStatusFilter, "Tất cả","Còn nợ","Đã cân","Có tiền dư"); Set(FolioPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 6); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; Step5Content.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed; Step6Content.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn Folio"),("B","Kiểm tra công nợ"),("C","Nhận thanh toán"),("D","Phân bổ"),("E","Biên lai"),("F","Xác nhận") };
        for (int i = 0; i < 6; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 5) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep() { switch (_step) { case 2: BuildStep2(); break; case 3: BuildStep3(); break; case 4: BuildStep4(); break; case 5: BuildStep5(); break; case 6: BuildStep6(); break; } }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = _selected && _folio?.FolioId != Guid.Empty && _payAmount > 0 && _payAmount <= _due && TryMapPaymentMethod(_method, out _); }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        FooterStepLabel.Text = $"Bước {_step}/6 · {new[] { "Chọn Folio","Kiểm tra công nợ","Nhận thanh toán","Phân bổ","Biên lai","Xác nhận" }[_step - 1]} · {_method} · {_payAmount:N0} {SelectedCurrency}";
    }

    private string SelectedCurrency => _folio?.Currency is { Length: > 0 } currency && currency != "—" ? currency : "—";

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var f = _folio; var sel = _selected;
        SummaryStrip.Children.Add(Strip("Khách", f?.Name ?? "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", f?.Room ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Folio", f?.FolioCode ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Còn nợ", sel ? $"{_due:N0}" : "—", _due == 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Thanh toán", sel ? $"{_method}" : "—", sel)); SummaryStrip.Children.Add(Sep());
        var ready = sel && _due <= 0;
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Đã cân" : "Còn nợ", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    private List<CpFolio> GetFiltered() { var f = _allFolios.AsEnumerable(); var kw = FolioSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Room.Contains(kw) || g.FolioCode.ToLower().Contains(kw)); var v = FolioVipFilter.SelectedItem?.ToString(); if (v == "VIP") f = f.Where(g => g.IsVip); else if (v == "Không VIP") f = f.Where(g => !g.IsVip); var s = FolioStatusFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(s) && s != "Tất cả") f = f.Where(g => g.Status == s); return f.ToList(); }
    private void RefreshFolioPage(bool reset) { if (reset) _folioPage = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _folioPageSize - 1) / _folioPageSize); if (_folioPage >= tp) _folioPage = tp - 1; FolioGrid.ItemsSource = f.Skip(_folioPage * _folioPageSize).Take(_folioPageSize).ToList(); FolioPagerSummary.Text = $"Hiển thị {_folioPage * _folioPageSize + 1}–{Math.Min((_folioPage + 1) * _folioPageSize, f.Count)} trên tổng {f.Count} Folio"; FolioPrevBtn.IsEnabled = _folioPage > 0; FolioNextBtn.IsEnabled = _folioPage < tp - 1; BuildBtns(tp); }
    private void BuildBtns(int tp) { FolioPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _folioPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _folioPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _folioPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _folioPage = idx; RefreshFolioPage(false); }; nb.Template = FolioPrevBtn.Template; FolioPageBtns.Children.Add(nb); } }
    private void OnFolioPreviewChanged(object s, SelectionChangedEventArgs e) { if (FolioGrid.SelectedItem is not CpFolio f) return; _folio = f; _selected = false; _total = f.Total; _deposit = f.Deposit; _paid = f.Paid; _due = f.Due; _payAmount = Math.Max(0, _due); _ref = string.Empty; _allocatedByCharge.Clear(); BtnSelectFolio.Content = "Chọn Folio này"; BtnSelectFolio.IsEnabled = true; BeginAllocationLoad(f.FolioId); FolioEmptyState.Visibility = Visibility.Collapsed; FolioDetailPanel.Visibility = Visibility.Visible; FDName.Text = f.Name; FDMeta.Text = f.FolioCode + " · " + f.Currency; FDInfo1.Text = $"Phòng: {f.Room} · Tổng: {f.Total:N0}"; FDInfo2.Text = $"Đặt cọc: {f.Deposit:N0} · Đã TT: {f.Paid:N0}"; FDInfo3.Text = $"Còn nợ: {f.Due:N0}"; FDInfo4.Text = $"Trạng thái: {f.Status}"; if (f.IsVip) { FDName.Text = "★ " + FDName.Text; FDName.Foreground = C("#F59E0B"); } UpdateSummary(); }
    private void OnSelectFolio(object s, RoutedEventArgs e) { if (_folio == null) return; _selected = true; _payAmount = Math.Min(_payAmount, _due); BtnSelectFolio.Content = "✓ Đã chọn"; BtnSelectFolio.IsEnabled = false; UpdateSummary(); UpdateNav(); }

    private void BeginAllocationLoad(Guid folioId)
    {
        CancelAllocationLoad();
        _allocatedByCharge.Clear();
        var cts = new CancellationTokenSource();
        _allocationLoadCts = cts;
        _ = LoadAllocationsAsync(folioId, _allocationLoadVersion, cts.Token);
    }

    private void CancelAllocationLoad()
    {
        _allocationLoadVersion++;
        _allocationLoadCts?.Cancel();
        _allocationLoadCts?.Dispose();
        _allocationLoadCts = null;
    }

    private async Task LoadAllocationsAsync(Guid folioId, int loadVersion, CancellationToken cancellationToken)
    {
        if (_paymentRepository is null || _executionContext?.Current is not { } context || folioId == Guid.Empty) return;
        try
        {
            var loadedAllocations = new Dictionary<Guid, decimal>();
            var payments = await _paymentRepository.GetForFolioAsync(context.Property.PropertyId.Value, folioId, cancellationToken);
            foreach (var payment in payments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var allocations = await _paymentRepository.GetAllocationsAsync(context.Property.PropertyId.Value, payment.HotelPaymentId, cancellationToken);
                foreach (var allocation in allocations)
                    loadedAllocations[allocation.ChargeFolioEntryId] = loadedAllocations.GetValueOrDefault(allocation.ChargeFolioEntryId) + allocation.Amount;
            }
            if (cancellationToken.IsCancellationRequested || loadVersion != _allocationLoadVersion || _folio?.FolioId != folioId) return;
            _allocatedByCharge.Clear();
            foreach (var (chargeId, amount) in loadedAllocations)
                _allocatedByCharge[chargeId] = amount;
            if (_step == 4) BuildStep4();
        }
        catch (OperationCanceledException) { }
        catch
        {
            if (loadVersion == _allocationLoadVersion && _folio?.FolioId == folioId)
            {
                _allocatedByCharge.Clear();
                if (_step == 4) BuildStep4();
            }
        }
    }
    private void OnApplyFolioFilters(object s, RoutedEventArgs e) => RefreshFolioPage(true);
    private void OnClearFolioFilters(object s, RoutedEventArgs e) { FolioSearchBox.Text = ""; FolioVipFilter.SelectedIndex = 0; FolioStatusFilter.SelectedIndex = 0; RefreshFolioPage(true); }
    private void OnFolioPrevPage(object s, RoutedEventArgs e) { if (_folioPage > 0) { _folioPage--; RefreshFolioPage(false); } }
    private void OnFolioNextPage(object s, RoutedEventArgs e) { _folioPage++; RefreshFolioPage(false); }
    private void OnFolioPageSizeChanged(object s, SelectionChangedEventArgs e) { _folioPageSize = FolioPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshFolioPage(true); }

    private void BuildStep2()
    {
        Step2Left.Children.Clear(); Step2Right.Children.Clear();
        Step2Left.Children.Add(new WrapPanel { Margin = new Thickness(0, 0, 0, 8), Children = { Metric("Tổng", $"{_total:N0}"), Metric("Đặt cọc", $"{_deposit:N0}"), Metric("Đã TT", $"{_paid:N0}"), Metric("Còn nợ", $"{_due:N0}"), Metric("Dư", $"{Math.Max(0, _paid + _deposit - _total):N0}") } });
        var items = (_folio?.Entries ?? []).Select(entry => new
        {
            Date = entry.BusinessDate?.ToString("dd/MM") ?? entry.CreatedAtUtc.ToString("dd/MM"),
            Item = entry.Description,
            Payer = entry.EntryType is HotelFolioEntryType.Payment or HotelFolioEntryType.Deposit ? "KH" : "—",
            Debit = entry.Amount > 0 ? $"{entry.Amount:N0}" : "0",
            Credit = entry.Amount < 0 ? $"{Math.Abs(entry.Amount):N0}" : "0"
        }).ToList();
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = items, MaxHeight = 260 }; dg.Columns.Add(new DataGridTextColumn { Header = "Ngày", Binding = new System.Windows.Data.Binding("Date"), Width = 60 }); dg.Columns.Add(new DataGridTextColumn { Header = "Khoản mục", Binding = new System.Windows.Data.Binding("Item"), Width = 120 }); dg.Columns.Add(new DataGridTextColumn { Header = "Người TT", Binding = new System.Windows.Data.Binding("Payer"), Width = 60 }); dg.Columns.Add(new DataGridTextColumn { Header = "Nợ", Binding = new System.Windows.Data.Binding("Debit"), Width = 100 }); dg.Columns.Add(new DataGridTextColumn { Header = "Có", Binding = new System.Windows.Data.Binding("Credit"), Width = 100 });
        Step2Left.Children.Add(dg); Step2Right.Children.Add(STitle("Tổng hợp")); Step2Right.Children.Add(Row("Tổng khoản mục", $"{_total:N0}")); Step2Right.Children.Add(Row("Đặt cọc", $"{_deposit:N0}")); Step2Right.Children.Add(Row("Đã thanh toán", $"{_paid:N0}")); Step2Right.Children.Add(new Border { Background = _due > 0 ? Cb("#FEE2E2") : Cb("#DCFCE7"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = _due > 0 ? $"Còn phải thu: {_due:N0}" : "✓ Đã cân", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = _due > 0 ? C("#DC2626") : C("#16A34A") } });
    }

    private void BuildStep3()
    {
        Step3Left.Children.Clear(); Step3Right.Children.Clear();
        var methods = new[] { "Tiền mặt","Visa","Master","JCB","AMEX","QR","Chuyển khoản","Ví điện tử","Công ty","Đại lý","Voucher" };
        var wp = new WrapPanel(); foreach (var m in methods) { var active = _method == m; var border = new Border { Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 8, 8), Cursor = System.Windows.Input.Cursors.Hand, BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = m, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#1E40AF") : C("#475569") } }; var cm = m; border.MouseLeftButtonDown += (_, _) => { _method = cm; BuildStep3(); }; wp.Children.Add(border); }
        Step3Left.Children.Add(STitle("Phương thức thanh toán")); Step3Left.Children.Add(wp);
        Step3Left.Children.Add(STitle("Thông tin giao dịch")); Step3Left.Children.Add(new TextBlock { Text = "Số tiền", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 2, 0, 0) });
        var amountBox = new TextBox { Text = _payAmount.ToString("0.##"), Style = FindResource("ModernTextBox") as Style, Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        amountBox.TextChanged += (_, _) => { if (TryParseMoney(amountBox.Text, out var value)) { _payAmount = Math.Max(0, value); UpdateNav(); UpdateSummary(); } };
        Step3Left.Children.Add(amountBox);
        Step3Left.Children.Add(new TextBlock { Text = "Tham chiếu (tuỳ chọn)", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 6, 0, 0) });
        var refBox = new TextBox { Text = _ref, Style = FindResource("ModernTextBox") as Style, Width = 250, HorizontalAlignment = HorizontalAlignment.Left };
        refBox.TextChanged += (_, _) => _ref = refBox.Text.Trim(); Step3Left.Children.Add(refBox);
        Step3Left.Children.Add(new TextBlock { Text = "Người thực hiện", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 6, 0, 0) }); Step3Left.Children.Add(new TextBlock { Text = "Theo actor đăng nhập", FontSize = 12, Foreground = C("#64748B"), Margin = new Thickness(0, 4, 0, 0) });
        Step3Right.Children.Add(STitle("Preview thanh toán")); Step3Right.Children.Add(Row("Phương thức", _method)); Step3Right.Children.Add(Row("Số tiền", $"{_payAmount:N0} {SelectedCurrency}")); Step3Right.Children.Add(Row("Còn nợ trước", $"{_due:N0} {SelectedCurrency}")); Step3Right.Children.Add(Row("Còn nợ sau", $"{Math.Max(0, _due - _payAmount):N0} {SelectedCurrency}")); Step3Right.Children.Add(new Border { Background = _payAmount > _due ? Cb("#FEF9C3") : Cb("#DCFCE7"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = _payAmount > _due ? $"⚠ Dư {_payAmount - _due:N0} {SelectedCurrency} — cần xử lý hoàn" : "✓ Hợp lệ", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = _payAmount > _due ? C("#A16207") : C("#16A34A") } });
    }

    private void BuildStep4()
    {
        Step4Left.Children.Clear(); Step4Right.Children.Clear();
        Step4Left.Children.Add(STitle("Phân bổ thanh toán"));
        var remainingToAllocate = Math.Max(0, _payAmount);
        var items = (_folio?.Entries ?? []).Where(IsCharge).Select(entry =>
        {
            var total = Math.Abs(entry.Amount);
            var allocated = _allocatedByCharge.GetValueOrDefault(entry.HotelFolioEntryId);
            var remain = Math.Max(0, total - allocated);
            var now = Math.Min(remainingToAllocate, remain);
            remainingToAllocate -= now;
            return new { Item = entry.Description, Total = $"{total:N0}", Paid = $"{allocated:N0}", Remain = $"{remain:N0}", Now = $"{now:N0}", Payer = "KH" };
        }).ToList();
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = items, MaxHeight = 220 }; dg.Columns.Add(new DataGridTextColumn { Header = "Khoản mục", Binding = new System.Windows.Data.Binding("Item"), Width = 150 }); dg.Columns.Add(new DataGridTextColumn { Header = "Tổng", Binding = new System.Windows.Data.Binding("Total"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Đã phân bổ", Binding = new System.Windows.Data.Binding("Paid"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Còn lại", Binding = new System.Windows.Data.Binding("Remain"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Dự kiến lần này", Binding = new System.Windows.Data.Binding("Now"), Width = 105 }); dg.Columns.Add(new DataGridTextColumn { Header = "Người TT", Binding = new System.Windows.Data.Binding("Payer"), Width = 55 });
        Step4Left.Children.Add(dg);
        Step4Right.Children.Add(STitle("Sau phân bổ")); Step4Right.Children.Add(Row("Đã thu lần này", $"{_payAmount:N0}")); Step4Right.Children.Add(Row("Còn nợ sau cùng", $"{Math.Max(0, _due - _payAmount):N0}")); Step4Right.Children.Add(Row("Dư (hoàn)", _payAmount > _due ? $"{_payAmount - _due:N0}" : "0"));
        if (_paymentRepository is null) Step4Right.Children.Add(new TextBlock { Text = "Chưa cấu hình payment repository để đọc allocation đã lưu.", FontSize = 11, Foreground = C("#B45309"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
    }

    private void BuildStep5()
    {
        Step5Left.Children.Clear(); Step5Right.Children.Clear();
        Step5Left.Children.Add(STitle("Xem trước biên lai")); Step5Left.Children.Add(new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Child = new StackPanel { Children = { new TextBlock { Text = "BIÊN LAI THANH TOÁN", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) }, new TextBlock { Text = $"Tham chiếu: {(string.IsNullOrWhiteSpace(_ref) ? "—" : _ref)}", FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) }, new TextBlock { Text = $"Khách: {_folio?.Name ?? "—"}", FontSize = 13, Foreground = C("#17365D"), Margin = new Thickness(0, 2, 0, 0) }, new TextBlock { Text = $"Folio: {_folio?.FolioCode ?? "—"}", FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) }, new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) }, new TextBlock { Text = $"Số tiền: {_payAmount:N0}", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center }, new TextBlock { Text = $"Phương thức: {_method}", FontSize = 13, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) }, new TextBlock { Text = "Người thực hiện: theo actor đăng nhập", FontSize = 11, Foreground = C("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) } } } });
        Step5Right.Children.Add(STitle("Tùy chọn"));
        Step5Right.Children.Add(new TextBlock
        {
            Text = "Biên lai dùng dữ liệu Folio và khoản thanh toán đang nhập.",
            FontSize = 11,
            Foreground = C("#64748B"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });
        var printReceipt = new Button
        {
            Content = "Xem trước / In biên lai",
            Style = TryFindResource("OutlineBtn") as Style,
            Height = 34,
            FontSize = 12,
            IsEnabled = _folio is not null && _payAmount > 0
        };
        printReceipt.Click += OnPrintReceipt;
        Step5Right.Children.Add(printReceipt);
    }

    private void BuildStep6()
    {
        Step6Left.Children.Clear(); Step6Right.Children.Clear(); if (_folio == null) return;
        Step6Left.Children.Add(STitle("Tóm tắt giao dịch")); Step6Left.Children.Add(Row("Khách", _folio.Name)); Step6Left.Children.Add(Row("Folio", _folio.FolioCode)); Step6Left.Children.Add(Row("Phương thức", _method)); Step6Left.Children.Add(Row("Số tiền", $"{_payAmount:N0}")); Step6Left.Children.Add(Row("Tham chiếu", string.IsNullOrWhiteSpace(_ref) ? "—" : _ref)); Step6Left.Children.Add(Row("Còn nợ trước", $"{_due:N0}")); Step6Left.Children.Add(Row("Còn nợ sau", $"{Math.Max(0, _due - _payAmount):N0}"));
        Step6Right.Children.Add(STitle("Xác nhận")); var items = new[] { ("Folio hợp lệ", _selected), ("Số tiền hợp lệ", _payAmount > 0 && _payAmount <= _due), ("Phương thức hợp lệ", TryMapPaymentMethod(_method, out _)), ("Tham chiếu hợp lệ", true), ("Biên lai sẵn sàng", true), ("Không lỗi", true) };
        var allOk = true; foreach (var (l, ok) in items) { Step6Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = l, FontSize = 12, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        Step6Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng xác nhận" : "Cần kiểm tra", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
    }

    private void OnPrintReceipt(object? sender, RoutedEventArgs e)
    {
        if (_folio is null || _payAmount <= 0)
        {
            MessageBox.Show("Cần chọn Folio và nhập số tiền thanh toán trước khi in biên lai.", "Biên lai", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var content = string.Join(Environment.NewLine,
            "BIÊN LAI THANH TOÁN",
            new string('=', 48),
            $"Khách hàng : {_folio.Name}",
            $"Phòng      : {_folio.Room}",
            $"Folio      : {_folio.FolioCode}",
            $"Thời gian  : {DateTime.Now:dd/MM/yyyy HH:mm}",
            $"Phương thức: {_method}",
            $"Tham chiếu : {(string.IsNullOrWhiteSpace(_ref) ? "—" : _ref)}",
            new string('-', 48),
            $"Số tiền    : {_payAmount:N0} {SelectedCurrency}",
            $"Còn nợ sau : {Math.Max(0, _due - _payAmount):N0} {SelectedCurrency}",
            new string('-', 48),
            "Nguồn: HotelFolio / thanh toán đang lập");

        var preview = new PrintPreviewDialog("Biên lai thanh toán", content)
        {
            Owner = this
        };
        preview.ShowDialog();
    }

    private static FrameworkElement Metric(string l, string v) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 6, 4), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = C("#17365D") }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns8((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (_folio is null || !_selected) return;

        if (_hasLoadedPersistedFolios && _paymentService is not null && _executionContext?.Current is not null)
        {
            if (_folio.FolioId == Guid.Empty || _folio.RowVersion is not { Length: 8 })
            {
                MessageBox.Show("Folio chưa có phiên bản dữ liệu hợp lệ để ghi thanh toán. Vui lòng tải lại.", "Thu tiền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryMapPaymentMethod(_method, out var method))
            {
                MessageBox.Show($"Phương thức thanh toán '{_method}' chưa được hỗ trợ.", "Thu tiền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_payAmount <= 0 || _payAmount > _due)
            {
                MessageBox.Show("Số tiền phải lớn hơn 0 và không vượt quá công nợ hiện tại. Chức năng hoàn tiền chưa được thực hiện trong luồng Thu ngân này.", "Thu tiền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Enum.TryParse<PaymentCurrency>(_executionContext.Current.Property.CurrencyCode, true, out var currency))
            {
                MessageBox.Show("Property chưa có currency hợp lệ để ghi thanh toán.", "Thu tiền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = await _paymentService.ReceiveAsync(
                new ReceivePaymentCommand(_folio.FolioId, _folio.RowVersion, method, _payAmount, currency, 1, string.IsNullOrWhiteSpace(_ref) ? null : _ref, "Thu tại màn hình Thu ngân"),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể ghi nhận thanh toán.", "Thu tiền", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var received = result.Value;
            if (received is null)
            {
                MessageBox.Show("Giao dịch thanh toán không trả về dữ liệu hợp lệ.", "Thu tiền", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var allocationIssue = await AllocateReceivedPaymentAsync(received, CancellationToken.None);
            var message = allocationIssue is null
                ? $"Đã ghi nhận và phân bổ thanh toán {received.PaymentNumber} cho Folio {_folio.FolioCode}."
                : $"Đã ghi nhận thanh toán {received.PaymentNumber}, nhưng chưa phân bổ hoàn tất: {allocationIssue}";
            MessageBox.Show(message, "Thu tiền", MessageBoxButton.OK, allocationIssue is null ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await OnLoadedAsyncRefresh();
            return;
        }

        MessageBox.Show("Không thể ghi nhận thanh toán vì chưa có property context và payment service dữ liệu thật. Không có giao dịch giả được tạo.", "Thu tiền", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private async Task<string?> AllocateReceivedPaymentAsync(HotelPaymentDto received, CancellationToken ct)
    {
        if (_paymentRepository is null || _paymentService is null || _executionContext?.Current is not { } context || _folio?.Entries is null)
            return "Chưa cấu hình payment repository để phân bổ.";

        var remaining = _payAmount;
        var lines = new List<PaymentAllocationLine>();
        foreach (var entry in _folio.Entries.Where(IsCharge))
        {
            var alreadyAllocated = _allocatedByCharge.GetValueOrDefault(entry.HotelFolioEntryId);
            var available = Math.Max(0, Math.Abs(entry.Amount) - alreadyAllocated);
            var amount = Math.Min(remaining, available);
            if (amount <= 0) continue;
            lines.Add(new PaymentAllocationLine(entry.HotelFolioEntryId, entry.Description, amount));
            remaining -= amount;
            if (remaining <= 0) break;
        }

        if (lines.Count == 0) return remaining > 0 ? $"còn {remaining:N0} chưa gắn vào khoản mục" : null;
        var current = await _paymentRepository.GetDtoAsync(context.Property.PropertyId.Value, received.HotelPaymentId, ct);
        if (current?.RowVersion is not { Length: 8 }) return "Payment không có row version sau khi ghi.";
        var allocation = await _paymentService.AllocateBatchAsync(
            new AllocatePaymentBatchCommand(received.HotelPaymentId, current.RowVersion, lines), ct);
        if (allocation.IsFailure) return allocation.Error?.Message ?? "Payment allocation bị từ chối.";

        return remaining > 0 ? $"còn {remaining:N0} chưa gắn vào khoản mục" : null;
    }

    private async Task OnLoadedAsyncRefresh()
    {
        CancelAllocationLoad();
        if (_folioRepository is null || _executionContext?.Current is null) return;
        var propertyId = _executionContext.Current.Property.PropertyId.Value;
        var page = await _folioRepository.SearchAsync(
            new FolioSearchFilter(propertyId, null, null, null, null, null, null, null),
            new PageRequest(1, 200), CancellationToken.None);
        _allFolios.Clear(); _allFolios.AddRange(page.Items.Select(MapPersistedFolio));
        _allocatedByCharge.Clear();
        _folio = null; _selected = false; GoTo(1); RefreshFolioPage(true);
    }

    private static bool TryMapPaymentMethod(string method, out HotelPaymentMethod result)
    {
        result = method switch
        {
            "Tiền mặt" => HotelPaymentMethod.Cash,
            "Visa" or "Master" or "JCB" or "AMEX" => HotelPaymentMethod.Card,
            "QR" => HotelPaymentMethod.QR,
            "Chuyển khoản" => HotelPaymentMethod.BankTransfer,
            "Ví điện tử" => HotelPaymentMethod.EWallet,
            "Công ty" => HotelPaymentMethod.CorporateBilling,
            "Đại lý" => HotelPaymentMethod.CityLedger,
            "Voucher" => HotelPaymentMethod.Voucher,
            _ => default
        };
        return method is "Tiền mặt" or "Visa" or "Master" or "JCB" or "AMEX" or "QR" or "Chuyển khoản" or "Ví điện tử" or "Công ty" or "Đại lý" or "Voucher";
    }
    private static bool TryParseMoney(string? text, out decimal value)
    {
        value = 0;
        var input = text?.Trim().Replace(" ", string.Empty);
        if (string.IsNullOrWhiteSpace(input)) return false;

        var hasComma = input.Contains(',');
        var hasDot = input.Contains('.');
        if (hasComma && !hasDot)
        {
            var groups = input.Split(',');
            if (groups.Length > 1 && groups.Skip(1).All(x => x.Length == 3))
                return decimal.TryParse(input.Replace(",", string.Empty), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
            return decimal.TryParse(input, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out value)
                || decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        if (hasDot && !hasComma)
        {
            var groups = input.Split('.');
            if (groups.Length > 1 && groups.Skip(1).All(x => x.Length == 3))
                return decimal.TryParse(input.Replace(".", string.Empty), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
            return decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(input, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out value);
        }

        var decimalCulture = input.LastIndexOf(',') > input.LastIndexOf('.')
            ? CultureInfo.GetCultureInfo("vi-VN")
            : CultureInfo.InvariantCulture;
        return decimal.TryParse(input, NumberStyles.Number, decimalCulture, out value);
    }
    private async void OnRefresh(object s, RoutedEventArgs e) => await OnLoadedAsyncRefresh();
    private void OnClose(object s, RoutedEventArgs e) { CancelAllocationLoad(); Close(); }
}

internal record CpFolio(string Room, string Name, string FolioCode, decimal Total, decimal Deposit, decimal Paid, string Currency, string Status, bool IsVip, Guid FolioId = default, byte[]? RowVersion = null, IReadOnlyList<HotelFolioEntryDto>? Entries = null) { public decimal Due => Math.Max(0, Total - Deposit - Paid); public string TotalDisplay => $"{Total:N0}"; public string PaidDisplay => $"{Paid:N0}"; public string DueDisplay => $"{Due:N0}"; public string VipDisplay => IsVip ? "★" : ""; }
internal static class CpExt { public static TextBlock WithRuns8(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
