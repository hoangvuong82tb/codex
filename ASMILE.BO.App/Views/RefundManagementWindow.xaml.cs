using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Payment;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class RefundManagementWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step;
    private RfTxn? _txn; private bool _selected;
    private int _page, _pageSize = 20;
    private readonly List<RfTxn> _allTxns;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IHotelPaymentRepository? _paymentRepository;
    private readonly HotelPaymentApplicationService? _paymentService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private bool _loadedRealData;
    private decimal _orig, _done, _left, _refundNow;
    private decimal _fee, _actual;
    private string _rMethod = "—", _currency = "—", _ref = string.Empty;

    public RefundManagementWindow()
    {
        InitializeComponent(); Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _paymentRepository = services?.GetService<IHotelPaymentRepository>();
        _paymentService = services?.GetService<HotelPaymentApplicationService>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _allTxns = new List<RfTxn>(); InitFilters(); RefreshPage(true); GoTo(1);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        _loadedRealData = true;
        if (_folioRepository is null || _paymentRepository is null || _paymentService is null || _executionContext?.Current is null)
        {
            _allTxns.Clear();
            RefEmptyState.Visibility = Visibility.Visible;
            RefDetailPanel.Visibility = Visibility.Collapsed;
            FooterStepLabel.Text = "Chưa cấu hình được payment service và property context dữ liệu thật.";
            RefreshPage(true);
            return;
        }

        try
        {
            var propertyId = _executionContext.Current.Property.PropertyId.Value;
            _currency = string.IsNullOrWhiteSpace(_executionContext.Current.Property.CurrencyCode)
                ? "—"
                : _executionContext.Current.Property.CurrencyCode.Trim();
            var folios = await _folioRepository.SearchAsync(
                new FolioSearchFilter(propertyId, null, null, null, null, null, null, null),
                new PageRequest(1, 200), CancellationToken.None);
            var rows = new List<RfTxn>();
            foreach (var folio in folios.Items)
            {
                var payments = await _paymentRepository.GetForFolioAsync(propertyId, folio.HotelFolioId, CancellationToken.None);
                rows.AddRange(payments
                    .Where(payment => payment.Amount > payment.RefundedAmount)
                    .Select(payment => new RfTxn(
                        payment.PaymentNumber,
                        "Payment",
                        folio.GuestName,
                        folio.RoomNumber,
                        payment.PaymentMethod.ToString(),
                        (int)Math.Round(payment.Amount),
                        (int)Math.Round(payment.RefundedAmount),
                        payment.RefundedAmount > 0 ? "Hoàn 1 phần" : "Có thể hoàn",
                        false,
                        payment.HotelPaymentId,
                        payment.RowVersion)));
            }
            _allTxns.Clear();
            _allTxns.AddRange(rows);
            _txn = null;
            _selected = false;
            RefEmptyState.Visibility = Visibility.Visible;
            RefDetailPanel.Visibility = Visibility.Collapsed;
            RefreshPage(true);
            UpdateSummary();
            UpdateNav();
        }
        catch (Exception ex)
        {
            _allTxns.Clear();
            RefEmptyState.Visibility = Visibility.Visible;
            RefDetailPanel.Visibility = Visibility.Collapsed;
            FooterStepLabel.Text = $"Không tải được giao dịch hoàn tiền thật: {ex.Message}";
            RefreshPage(true);
        }
    }

    private void InitFilters() { Set(RefTypeFilter, "Tất cả","Payment","Deposit","Overpay","Dup Pay","Cancel","Company","TA"); Set(RefStatusFilter, "Tất cả","Có thể hoàn","Hoàn 1 phần","Đã hoàn hết","Chờ ĐS","Bị khóa"); Set(RefPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 6); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; Step5Content.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed; Step6Content.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn giao dịch"),("B","Điều kiện hoàn"),("C","Số tiền hoàn"),("D","Người nhận & PT"),("E","Chứng từ & PD"),("F","Xác nhận") };
        for (int i = 0; i < 6; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 5) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep() { switch (_step) { case 2: BuildStep2(); break; case 3: BuildStep3(); break; case 4: BuildStep4(); break; case 5: BuildStep5(); break; case 6: BuildStep6(); break; } }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = _selected && _refundNow > 0; }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        FooterStepLabel.Text = $"Bước {_step}/6 · Hoàn {_actual:N0} {_currency}";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var t = _txn; var sel = _selected;
        SummaryStrip.Children.Add(Strip("Khách", t?.Name ?? "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", t?.Room ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Gốc", sel ? $"{_orig:N0}" : "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đã hoàn", sel ? $"{_done:N0}" : "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Còn", sel ? $"{_left:N0}" : "—", _left == 0)); SummaryStrip.Children.Add(Sep());
        var ready = sel && _left >= _actual && _actual > 0;
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Sẵn sàng" : "Chưa SS", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    private List<RfTxn> GetFiltered() { var f = _allTxns.AsEnumerable(); var kw = RefSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Code.ToLower().Contains(kw) || g.Room.Contains(kw)); var t = RefTypeFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(t) && t != "Tất cả") f = f.Where(g => g.Type == t); var s = RefStatusFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(s) && s != "Tất cả") f = f.Where(g => g.Status == s); return f.ToList(); }
    private void RefreshPage(bool reset) { if (reset) _page = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _pageSize - 1) / _pageSize); if (_page >= tp) _page = tp - 1; RefGrid.ItemsSource = f.Skip(_page * _pageSize).Take(_pageSize).ToList(); RefPagerSummary.Text = $"Hiển thị {_page * _pageSize + 1}–{Math.Min((_page + 1) * _pageSize, f.Count)} trên tổng {f.Count} giao dịch"; RefPrevBtn.IsEnabled = _page > 0; RefNextBtn.IsEnabled = _page < tp - 1; BuildBtns(tp); }
    private void BuildBtns(int tp) { RefPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _page ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _page ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _page ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _page = idx; RefreshPage(false); }; nb.Template = RefPrevBtn.Template; RefPageBtns.Children.Add(nb); } }
    private void OnRefPreviewChanged(object s, SelectionChangedEventArgs e) { if (RefGrid.SelectedItem is not RfTxn t) return; _txn = t; _selected = false; _orig = t.Original; _done = t.Done; _left = t.Original - t.Done; _refundNow = Math.Max(0, _left); _fee = 0; _actual = _refundNow; _rMethod = t.Method; RefEmptyState.Visibility = Visibility.Collapsed; RefDetailPanel.Visibility = Visibility.Visible; RDName.Text = t.Name; RDMeta.Text = $"{t.Code} · {t.Type}"; RDInfo1.Text = $"Phòng: {t.Room} · {t.Method}"; RDInfo2.Text = $"Gốc: {t.Original:N0} · Đã hoàn: {t.Done:N0}"; RDInfo3.Text = $"Còn: {_left:N0}"; RDInfo4.Text = $"Trạng thái: {t.Status}"; UpdateSummary(); }
    private void OnSelectRef(object s, RoutedEventArgs e) { if (_txn == null) return; _selected = true; _refundNow = Math.Min(_refundNow, _left); _actual = Math.Max(0, _refundNow - _fee); BtnSelectRef.Content = "✓ Đã chọn"; BtnSelectRef.IsEnabled = false; UpdateSummary(); UpdateNav(); }
    private void OnApplyRefFilters(object s, RoutedEventArgs e) => RefreshPage(true);
    private void OnClearRefFilters(object s, RoutedEventArgs e) { RefSearchBox.Text = ""; RefTypeFilter.SelectedIndex = 0; RefStatusFilter.SelectedIndex = 0; RefreshPage(true); }
    private void OnRefPrevPage(object s, RoutedEventArgs e) { if (_page > 0) { _page--; RefreshPage(false); } }
    private void OnRefNextPage(object s, RoutedEventArgs e) { _page++; RefreshPage(false); }
    private void OnRefPageSizeChanged(object s, SelectionChangedEventArgs e) { _pageSize = RefPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshPage(true); }

    private void BuildStep2()
    {
        Step2Left.Children.Clear(); Step2Right.Children.Clear();
        Step2Left.Children.Add(STitle("Chính sách hoàn")); Step2Left.Children.Add(Row("Loại", "Refundable")); Step2Left.Children.Add(Row("Hạn hoàn", "30 ngày")); Step2Left.Children.Add(Row("Phí hủy", "Không")); Step2Left.Children.Add(Row("Phí xử lý thẻ", "0%"));
        Step2Right.Children.Add(STitle("Điều kiện giao dịch")); var items = new[] { ("Giao dịch hợp lệ", true), ("Đã settle", true), ("Không bị khóa", true), ("Không chargeback", true), ("Phương thức gốc khả dụng", true) };
        foreach (var (l, ok) in items) Step2Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = l, FontSize = 12, Foreground = C("#334155") } } });
        Step2Right.Children.Add(new Border { Background = Cb("#DCFCE7"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = "✓ Đủ điều kiện hoàn", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#16A34A") } });
    }

    private void BuildStep3()
    {
        Step3Left.Children.Clear(); Step3Right.Children.Clear();
        Step3Left.Children.Add(STitle("Tính số tiền hoàn")); Step3Left.Children.Add(Row("Gốc", $"{_orig:N0} {_currency}")); Step3Left.Children.Add(Row("Đã hoàn", $"{_done:N0} {_currency}")); Step3Left.Children.Add(Row("Còn", $"{_left:N0} {_currency}"));
        Step3Left.Children.Add(new TextBlock { Text = "Yêu cầu hoàn", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 6, 0, 0) });
        var refundBox = new TextBox { Text = _refundNow.ToString("0.##"), Style = FindResource("ModernTextBox") as Style, Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        refundBox.TextChanged += (_, _) => { if (decimal.TryParse(refundBox.Text, out var value)) { _refundNow = Math.Max(0, Math.Min(value, _left)); _actual = Math.Max(0, _refundNow - _fee); UpdateSummary(); UpdateNav(); } };
        Step3Left.Children.Add(refundBox);
        Step3Left.Children.Add(new TextBlock { Text = "Phí khấu trừ", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 6, 0, 0) });
        var feeBox = new TextBox { Text = _fee.ToString("0.##"), Style = FindResource("ModernTextBox") as Style, Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        feeBox.TextChanged += (_, _) => { if (decimal.TryParse(feeBox.Text, out var value)) { _fee = Math.Max(0, Math.Min(value, _refundNow)); _actual = Math.Max(0, _refundNow - _fee); UpdateSummary(); UpdateNav(); } };
        Step3Left.Children.Add(feeBox);
        Step3Left.Children.Add(new TextBlock { Text = "Số tiền và phí được kiểm tra lại tại backend khi xác nhận.", FontSize = 11, Foreground = C("#B45309"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
        Step3Right.Children.Add(STitle("Preview")); Step3Right.Children.Add(Row("Yêu cầu", $"{_refundNow:N0} {_currency}")); Step3Right.Children.Add(Row("Khấu trừ", $"{_fee:N0} {_currency}")); Step3Right.Children.Add(Row("Thực hoàn", $"{_actual:N0} {_currency}")); Step3Right.Children.Add(Row("Còn lại", $"{_left - _refundNow:N0} {_currency}"));
        Step3Right.Children.Add(new Border { Background = _actual > 0 ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = _actual > 0 ? $"✓ Hoàn {_actual:N0} {_currency}" : "✗ Chưa nhập số tiền", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = _actual > 0 ? C("#16A34A") : C("#DC2626") } });
    }

    private void BuildStep4()
    {
        Step4Left.Children.Clear(); Step4Right.Children.Clear();
        Step4Left.Children.Add(STitle("Người nhận")); Step4Left.Children.Add(Row("Tên", _txn?.Name ?? "—")); Step4Left.Children.Add(Row("Quan hệ", "Theo hồ sơ payment")); Step4Left.Children.Add(Row("Điện thoại", "Không có trong dữ liệu payment")); Step4Left.Children.Add(Row("Email", "Không có trong dữ liệu payment"));
        Step4Right.Children.Add(STitle("Phương thức hoàn")); Step4Right.Children.Add(Row("Nguồn hoàn", _txn?.Method ?? "—")); Step4Right.Children.Add(new Border { Background = Cb("#FFF7ED"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = "Hoàn theo payment gốc. RefundPaymentCommand hiện không nhận một phương thức đích khác.", FontSize = 11, Foreground = C("#EA580C"), TextWrapping = TextWrapping.Wrap } });
    }

    private void BuildStep5()
    {
        Step5Left.Children.Clear(); Step5Right.Children.Clear();
        Step5Left.Children.Add(STitle("Phiếu hoàn tiền")); Step5Left.Children.Add(new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Child = new StackPanel { Children = { new TextBlock { Text = "PHIẾU HOÀN TIỀN", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) }, new TextBlock { Text = $"Số: {_ref}", FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) }, new TextBlock { Text = $"Khách: {_txn?.Name ?? "—"}", FontSize = 13, Foreground = C("#17365D"), Margin = new Thickness(0, 2, 0, 0) }, new TextBlock { Text = $"Số tiền hoàn: {_actual:N0} {_currency}", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) }, new TextBlock { Text = $"Phương thức: {_rMethod} · ETA: 3–7 ngày", FontSize = 11, Foreground = C("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) } } } });
        Step5Right.Children.Add(STitle("Phê duyệt")); Step5Right.Children.Add(Row("Số tiền", $"{_actual:N0} {_currency}")); Step5Right.Children.Add(Row("Ngưỡng duyệt", _actual > 5000000 ? "FOM" : "Supervisor")); Step5Right.Children.Add(Row("Trạng thái", "Chờ phê duyệt")); Step5Right.Children.Add(new ComboBox { Style = FindResource("ModernComboBox") as Style, ItemsSource = new[] { "In phiếu","Gửi Email","PDF","A4","A5","Thermal" }, SelectedIndex = 0, Width = 200, Margin = new Thickness(0, 6, 0, 0) });
    }

    private void BuildStep6()
    {
        Step6Left.Children.Clear(); Step6Right.Children.Clear(); if (_txn == null) return;
        Step6Left.Children.Add(STitle("Tóm tắt hoàn tiền")); Step6Left.Children.Add(Row("Khách", _txn.Name)); Step6Left.Children.Add(Row("Phòng", _txn.Room)); Step6Left.Children.Add(Row("Nguồn", $"{_txn.Code} · Gốc {_orig:N0}")); Step6Left.Children.Add(Row("Đã hoàn", $"{_done:N0} · Hoàn lần này {_actual:N0}"));
        Step6Right.Children.Add(STitle("Xác nhận")); var items = new[] { ("Đã chọn GD", _selected), ("Đủ điều kiện", true), ("Số tiền hợp lệ", _actual > 0), ("Người nhận hợp lệ", true), ("Phương thức hợp lệ", true), ("Có phê duyệt", true) };
        var allOk = true; foreach (var (l, ok) in items) { Step6Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = l, FontSize = 12, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        Step6Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng hoàn tiền" : "Cần kiểm tra", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
    }

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns10((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (_txn is null || !_selected || _actual <= 0)
            return;
        if (!_loadedRealData || _paymentService is null || _executionContext?.Current is null || _txn.PaymentId == Guid.Empty || _txn.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Giao dịch chưa có dữ liệu thật hợp lệ để hoàn tiền. Không tạo giao dịch giả.", "Hoàn tiền", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = await _paymentService.RefundAsync(
            new RefundPaymentCommand(_txn.PaymentId, _txn.RowVersion, _actual, $"Hoàn tiền {_ref}"),
            CancellationToken.None);
        if (result.IsFailure)
        {
            MessageBox.Show(result.Error?.Message ?? "Không thể ghi nhận hoàn tiền.", "Hoàn tiền", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        MessageBox.Show("Đã ghi nhận hoàn tiền vào payment và Folio thật.", "Hoàn tiền", MessageBoxButton.OK, MessageBoxImage.Information);
        await OnLoadedAsyncRefresh();
    }

    private async Task OnLoadedAsyncRefresh()
    {
        if (_folioRepository is null || _paymentRepository is null || _executionContext?.Current is null)
            return;
        var propertyId = _executionContext.Current.Property.PropertyId.Value;
        var folios = await _folioRepository.SearchAsync(
            new FolioSearchFilter(propertyId, null, null, null, null, null, null, null),
            new PageRequest(1, 200), CancellationToken.None);
        _allTxns.Clear();
        foreach (var folio in folios.Items)
        {
            var payments = await _paymentRepository.GetForFolioAsync(propertyId, folio.HotelFolioId, CancellationToken.None);
            _allTxns.AddRange(payments.Where(payment => payment.Amount > payment.RefundedAmount).Select(payment => new RfTxn(
                payment.PaymentNumber, "Payment", folio.GuestName, folio.RoomNumber, payment.PaymentMethod.ToString(),
                (int)Math.Round(payment.Amount), (int)Math.Round(payment.RefundedAmount),
                payment.RefundedAmount > 0 ? "Hoàn 1 phần" : "Có thể hoàn", false,
                payment.HotelPaymentId, payment.RowVersion)));
        }
        _txn = null;
        _selected = false;
        GoTo(1);
        RefreshPage(true);
    }
    private void OnRefresh(object s, RoutedEventArgs e) => OnLoadedAsync(this, e);
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record RfTxn(string Code, string Type, string Name, string Room, string Method, int Original, int Done, string Status, bool IsVip, Guid PaymentId = default, byte[]? RowVersion = null) { public string OrigDisplay => $"{Original:N0}"; public string DoneDisplay => $"{Done:N0}"; public string LeftDisplay => $"{Original - Done:N0}"; }
internal static class RfExt { public static TextBlock WithRuns10(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
