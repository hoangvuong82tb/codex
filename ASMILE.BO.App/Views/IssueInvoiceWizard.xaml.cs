using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class IssueInvoiceWizard : Window
{
    private int _step = 1;
    private IwFolio? _selectedFolio;
    private readonly List<IwFolio> _folios;
    private string _invoiceType = "Company", _template = "Standard";
    private string _guestName = "", _company = "", _taxCode = "", _address = "", _receiver = "", _email = "", _phone = "", _remark = "";
    private int _folioPage, _folioPageSize = 20;
    private bool _submitted;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IEInvoiceService? _eInvoiceService;
    private readonly IServiceScope? _serviceScope;
    private readonly SemaphoreSlim _dataOperationGate = new(1, 1);
    private bool _isClosed;
    private InvoiceProviderReadinessDto? _providerReadiness;

    public IssueInvoiceWizard()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        ResetWizard();
        _folios = new List<IwFolio>();
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _folioRepository = scopedServices?.GetService<IHotelFolioRepository>();
        _executionContext = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _eInvoiceService = scopedServices?.GetService<IEInvoiceService>();
        InitFolioFilters();
        GoTo(1);
        Loaded += async (_, _) => await LoadPersistedFoliosAsync();
        Closed += OnClosed;
    }

    private async Task LoadPersistedFoliosAsync()
    {
        await _dataOperationGate.WaitAsync();
        try { await LoadPersistedFoliosCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task LoadPersistedFoliosCoreAsync()
    {
        if (_isClosed) return;
        if (_folioRepository is null || _executionContext?.Current is not { } context)
        {
            FooterLabel.Text = "Chưa cấu hình được Folio service và property context dữ liệu thật.";
            RefreshFolioPage(true);
            return;
        }

        try
        {
            var page = await _folioRepository.SearchAsync(
                new FolioSearchFilter(context.Property.PropertyId.Value, null, null, null, null, null, null, null),
                new PageRequest(1, 500), CancellationToken.None);
            _folios.Clear();
            var id = 1;
            foreach (var ledger in page.Items)
            {
                var roomCharges = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.RoomCharge).Sum(x => x.Amount);
                var tax = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.Tax).Sum(x => x.Amount);
                var charges = roomCharges + tax;
                var payments = ledger.Entries.Where(x => x.Amount < 0).Sum(x => -x.Amount);
                var deposits = ledger.Entries.Where(x => x.EntryType == ASMILE.BO.Core.FrontOffice.Folio.HotelFolioEntryType.Deposit)
                    .Sum(x => Math.Abs(x.Amount));
                _folios.Add(new IwFolio(id++, ledger.FolioNumber, ledger.GuestName, null, ledger.StayNumber,
                    ledger.RoomNumber, "VND", ToInt(charges), ToInt(deposits), ToInt(payments),
                    "—", "—", ledger.Status.ToString(), null, ledger.HotelFolioId, ledger.RowVersion, ToInt(tax)));
            }

            _selectedFolio = null;
            _submitted = false;
            RefreshFolioPage(true);
            FooterLabel.Text = $"Dữ liệu Folio thật · {_folios.Count:N0} Folio";
        }
        catch (Exception ex)
        {
            _folios.Clear();
            RefreshFolioPage(true);
            FooterLabel.Text = $"Không thể tải Folio thật: {ex.Message}";
        }
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        await _dataOperationGate.WaitAsync();
        try { _serviceScope?.Dispose(); }
        finally { _dataOperationGate.Release(); _dataOperationGate.Dispose(); }
    }

    private static int ToInt(decimal value) => value <= 0 ? 0 : value >= int.MaxValue ? int.MaxValue : (int)Math.Round(value);


    private void InitFolioFilters()
    {
        FolioStatusFilter.ItemsSource = new[] { "Tất cả", "Open", "Settled", "Issued", "Closed" };
        FolioStatusFilter.SelectedIndex = 0;
        FolioPageSizeBox.ItemsSource = new[] { "20 / trang", "50 / trang" };
        FolioPageSizeBox.SelectedIndex = 0;
    }

    // ═══ NAVIGATION ═══

    private void ResetWizard()
    {
        _step = 1;
        _selectedFolio = null;
        _submitted = false;
        _providerReadiness = null;
        _invoiceType = "Company"; _guestName = ""; _company = ""; _taxCode = ""; _address = ""; _receiver = ""; _email = ""; _phone = ""; _remark = "";
    }

    private void HideAllSteps()
    {
        Step1.Visibility = Visibility.Collapsed;
        Step2.Visibility = Visibility.Collapsed;
        Step3.Visibility = Visibility.Collapsed;
        Step4.Visibility = Visibility.Collapsed;
    }

    private void GoTo(int s)
    {
        _step = Math.Clamp(s, 1, 4);
        HideAllSteps();
        BuildStepBar();
        switch (_step)
        {
            case 1: Step1.Visibility = Visibility.Visible; RefreshFolioPage(true); break;
            case 2: Step2.Visibility = Visibility.Visible; RenderStep2(); break;
            case 3: Step3.Visibility = Visibility.Visible; RenderStep3(); break;
            case 4:
                Step4.Visibility = Visibility.Visible;
                RenderStep4();
                _ = RefreshProviderReadinessAsync();
                break;
        }
        UpdateNav();
        UpdateFooter();
    }

    private void UpdateFooter()
    {
        var label = _step switch
        {
            1 => "Chọn Folio",
            2 => "Thông tin HĐ",
            3 => "Kiểm tra tài chính",
            4 => "Xác nhận phát hành",
            _ => "Chọn Folio"
        };
        FooterLabel.Text = $"Bước {_step}/4 · {label}";
    }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        bool canProceed = _step switch
        {
            1 => _selectedFolio != null,
            2 => !string.IsNullOrWhiteSpace(_guestName),
            3 => _selectedFolio != null,
            4 => false,
            _ => false
        };
        if (_step == 4)
        {
            BtnNext.Visibility = Visibility.Collapsed;
            BtnIssue.Visibility = Visibility.Visible;
            BtnIssue.Content = _providerReadiness is { IsReady: false }
                ? "Lưu nháp hóa đơn"
                : "Phát hành hóa đơn";
            BtnIssue.IsEnabled = _selectedFolio != null && !_submitted && !string.IsNullOrWhiteSpace(_guestName);
        }
        else
        {
            BtnNext.Visibility = Visibility.Visible;
            BtnIssue.Visibility = Visibility.Collapsed;
            BtnNext.IsEnabled = canProceed;
        }
    }

    private void OnNext(object s, RoutedEventArgs e)
    {
        if (_step >= 4) return;
        if (!CanEnterStep(_step + 1)) return;
        GoTo(_step + 1);
    }

    private void OnBack(object s, RoutedEventArgs e)
    {
        if (_step <= 1) return;
        GoTo(_step - 1);
    }

    private bool CanEnterStep(int step) => step switch
    {
        1 => true,
        2 => _selectedFolio != null,
        3 => _selectedFolio != null && !string.IsNullOrWhiteSpace(_guestName),
        4 => _selectedFolio != null && !string.IsNullOrWhiteSpace(_guestName),
        _ => false
    };

    private void BuildStepBar()
    {
        StepBar.Children.Clear();
        var steps = new[]
        {
            (1, "A", "Chọn Folio"),
            (2, "B", "Thông tin HĐ"),
            (3, "C", "Kiểm tra TC"),
            (4, "D", "Xác nhận")
        };
        for (int i = 0; i < steps.Length; i++)
        {
            var stepNumber = steps[i].Item1;
            var active = stepNumber == _step;
            var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(3, 5, 3, 5), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = steps[i].Item2, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = active ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = " " + steps[i].Item3, FontSize = 12, Foreground = active ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) } } } };
            b.MouseLeftButtonDown += (_, _) =>
            {
                if (CanEnterStep(stepNumber)) GoTo(stepNumber);
            };
            StepBar.Children.Add(b);
            if (i < steps.Length - 1) StepBar.Children.Add(Sep());
        }
    }

    // ═══ STEP A: SELECT FOLIO ═══

    private List<IwFolio> GetFilteredFolios()
    {
        var f = _folios.AsEnumerable(); var kw = FolioSearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(x => x.Guest.ToLower().Contains(kw) || x.Code.ToLower().Contains(kw) || x.Room.Contains(kw));
        var st = FolioStatusFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(st) && st != "Tất cả") f = f.Where(x => x.Status == st);
        return f.ToList();
    }

    private void RefreshFolioPage(bool reset)
    {
        if (reset) _folioPage = 0;
        var f = GetFilteredFolios(); var tp = Math.Max(1, (f.Count + _folioPageSize - 1) / _folioPageSize);
        if (_folioPage >= tp) _folioPage = tp - 1;
        FolioGrid.ItemsSource = f.Skip(_folioPage * _folioPageSize).Take(_folioPageSize).ToList();
        FolioPagerSummary.Text = $"{_folioPage * _folioPageSize + 1}–{Math.Min((_folioPage + 1) * _folioPageSize, f.Count)} / {f.Count}";
        FolioPrevBtn.IsEnabled = _folioPage > 0; FolioNextBtn.IsEnabled = _folioPage < tp - 1;
        BuildFolioPageBtns(tp);
    }

    private void BuildFolioPageBtns(int tp) { FolioPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = PagerBtn(i, _folioPage, idx); nb.Click += (_, _) => { _folioPage = idx; RefreshFolioPage(false); }; FolioPageBtns.Children.Add(nb); } }
    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 28, Height = 26, FontSize = 11, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = FolioPrevBtn.Template };

    private void OnFolioSelected(object s, SelectionChangedEventArgs e)
    {
        if (FolioGrid.SelectedItem is not IwFolio f) return;
        _selectedFolio = null; FolioEmptyState.Visibility = Visibility.Collapsed; FolioDetail.Visibility = Visibility.Visible;
        FdCode.Text = f.Code; FdGuest.Text = f.Guest; FdCompany.Text = f.Company ?? "—"; FdRoom.Text = f.Room;
        FdRes.Text = f.Reservation; FdArrival.Text = f.Arrival; FdDeparture.Text = f.Departure;
        FdBalance.Text = $"{f.Total - f.Paid - f.Deposit:N0} {f.Currency}";
        FdTotal.Text = $"{f.Total:N0} {f.Currency}"; FdStatus.Text = f.Status;
        var canIssue = f.Status is "Open" or "Settled" && f.IssuedInvoice == null;
        BtnSelectFolio.IsEnabled = canIssue;
        BtnSelectFolio.Content = canIssue ? "Chọn Folio này" : f.IssuedInvoice != null ? $"Đã xuất HĐ: {f.IssuedInvoice}" : "Không khả dụng";
    }

    private void OnSelectFolio(object s, RoutedEventArgs e)
    {
        if (FolioGrid.SelectedItem is not IwFolio f) return;
        _selectedFolio = f;
        if (!string.IsNullOrEmpty(f.Company)) { _invoiceType = "Company"; _company = f.Company; _guestName = f.Guest; }
        else { _invoiceType = "Individual"; _guestName = f.Guest; }
        BtnSelectFolio.Content = "✓ Đã chọn"; BtnSelectFolio.IsEnabled = false;
        UpdateNav();
    }

    private void OnApplyFFilter(object s, RoutedEventArgs e) => RefreshFolioPage(true);
    private void OnClearFFilter(object s, RoutedEventArgs e) { FolioSearchBox.Text = ""; FolioStatusFilter.SelectedIndex = 0; RefreshFolioPage(true); }
    private void OnFPrevPage(object s, RoutedEventArgs e) { if (_folioPage > 0) { _folioPage--; RefreshFolioPage(false); } }
    private void OnFNextPage(object s, RoutedEventArgs e) { _folioPage++; RefreshFolioPage(false); }
    private void OnFPageSizeChanged(object s, SelectionChangedEventArgs e) { _folioPageSize = FolioPageSizeBox.SelectedIndex == 1 ? 50 : 20; RefreshFolioPage(true); }

    // ═══ STEP B: INVOICE INFO ═══

    private void RenderStep2()
    {
        Step2Left.Children.Clear(); Step2Right.Children.Clear();
        Step2Left.Children.Add(STitle("Loại hóa đơn"));
        var types = new[] { ("Cá nhân", "Individual"), ("Công ty", "Company"), ("Điện tử", "Electronic") };
        var wp = new WrapPanel();
        foreach (var (label, key) in types) { var active = _invoiceType == key; var b = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 12, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } }; b.MouseLeftButtonDown += (_, _) => { _invoiceType = key; RenderStep2(); }; wp.Children.Add(b); }
        Step2Left.Children.Add(wp);

        Step2Left.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 8) });
        Step2Left.Children.Add(STitle("Thông tin xuất hóa đơn"));
        Step2Left.Children.Add(Field("Tên khách hàng", _guestName, v => _guestName = v));
        if (_invoiceType == "Company") { Step2Left.Children.Add(Field("Công ty", _company, v => _company = v)); Step2Left.Children.Add(Field("Mã số thuế", _taxCode, v => _taxCode = v)); Step2Left.Children.Add(Field("Địa chỉ", _address, v => _address = v)); }
        Step2Left.Children.Add(Field("Người nhận", _receiver, v => _receiver = v));
        Step2Left.Children.Add(Field("Email", _email, v => _email = v)); Step2Left.Children.Add(Field("Điện thoại", _phone, v => _phone = v));
        Step2Left.Children.Add(Field("Ghi chú", _remark, v => _remark = v));

        Step2Right.Children.Add(STitle("Preview"));
        Step2Right.Children.Add(Row("Loại HĐ", _invoiceType));
        Step2Right.Children.Add(Row("Khách", _guestName));
        if (_invoiceType == "Company") { Step2Right.Children.Add(Row("Công ty", _company)); Step2Right.Children.Add(Row("MST", _taxCode)); }
        Step2Right.Children.Add(Row("Folio", _selectedFolio?.Code ?? "—"));
        if (_selectedFolio != null) { var bal = _selectedFolio.Total - _selectedFolio.Paid - _selectedFolio.Deposit; Step2Right.Children.Add(Row("Tổng tiền", $"{_selectedFolio.Total:N0} {_selectedFolio.Currency}")); Step2Right.Children.Add(new Border { Background = bal > 0 ? Cb("#FEF9C3") : Cb("#DCFCE7"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = bal > 0 ? $"Còn nợ: {bal:N0}" : "✓ Đã thanh toán đầy đủ", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = bal > 0 ? C("#A16207") : C("#16A34A") } }); }
    }

    private FrameworkElement Field(string label, string value, Action<string> setter)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
        sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 2) });
        var tb = new TextBox { Text = value, Height = 34, Padding = new Thickness(10, 0, 10, 0), FontSize = 12, Foreground = C("#17365D"), Background = Cb("White"), BorderBrush = Cb("#D7E2F0"), BorderThickness = new Thickness(1), VerticalContentAlignment = VerticalAlignment.Center };
        tb.TextChanged += (_, _) => setter(tb.Text);
        sp.Children.Add(tb);
        return sp;
    }

    // ═══ STEP C: FINANCIAL REVIEW ═══

    private void RenderStep3()
    {
        Step3Panel.Children.Clear();
        if (_selectedFolio == null) return;
        var f = _selectedFolio; var total = f.Total; var paid = f.Paid; var dep = f.Deposit;
        var subtotal = total - f.Tax; var grandTotal = total; var outstanding = grandTotal - paid - dep;

        Step3Panel.Children.Add(STitle("Kiểm tra tài chính"));
        Step3Panel.Children.Add(Readonly("Tiền phòng / dịch vụ", $"{subtotal:N0}"));
        Step3Panel.Children.Add(Readonly("Thuế theo ledger", $"{f.Tax:N0}"));
        Step3Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        Step3Panel.Children.Add(Readonly("Tổng cộng", $"{grandTotal:N0} {f.Currency}"));
        Step3Panel.Children.Add(Readonly("Đã thanh toán", $"{paid:N0}"));
        Step3Panel.Children.Add(Readonly("Đặt cọc", $"{dep:N0}"));
        Step3Panel.Children.Add(new Border { Height = 1, Background = Cb("#2F6BFF"), Margin = new Thickness(0, 6, 0, 6) });
        Step3Panel.Children.Add(new Border { Background = outstanding > 0 ? Cb("#FEE2E2") : Cb("#DCFCE7"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Child = new TextBlock { Text = outstanding > 0 ? $"Còn phải thu: {outstanding:N0} {f.Currency}" : "✓ Đã thanh toán đầy đủ", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = outstanding > 0 ? C("#DC2626") : C("#16A34A") } });
        Step3Panel.Children.Add(Readonly("Mẫu hóa đơn", _template));
        Step3Panel.Children.Add(Readonly("Loại HĐ", _invoiceType));
    }

    private FrameworkElement Readonly(string label, string value) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0), Children = { new TextBlock { Text = label + ": ", FontSize = 12, Foreground = C("#94A3B8"), Width = 180 }, new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") } } };

    // ═══ STEP D: REVIEW & CONFIRM ═══

    private void RenderStep4()
    {
        Step4Left.Children.Clear(); Step4Right.Children.Clear();
        if (_selectedFolio == null) return;
        var f = _selectedFolio; var total = f.Total;
        var grandTotal = total;

        Step4Left.Children.Add(STitle("Xác nhận phát hành hóa đơn"));
        Step4Left.Children.Add(Row("Khách", _guestName));
        if (_invoiceType == "Company") { Step4Left.Children.Add(Row("Công ty", _company)); Step4Left.Children.Add(Row("MST", _taxCode)); }
        Step4Left.Children.Add(Row("Folio", f.Code)); Step4Left.Children.Add(Row("Reservation", f.Reservation));
        Step4Left.Children.Add(Row("Loại HĐ", _invoiceType));
        Step4Left.Children.Add(Row("Tổng trước thuế", $"{total:N0}"));
        Step4Left.Children.Add(Row("Thuế theo ledger", $"{f.Tax:N0}"));
        Step4Left.Children.Add(Row("Tổng cộng", $"{grandTotal:N0} {f.Currency}"));
        Step4Left.Children.Add(Row("Email", string.IsNullOrEmpty(_email) ? "—" : _email));

        Step4Right.Children.Add(STitle("Điều kiện phát hành"));
        var checks = new (string, bool)[] { ("Đã chọn Folio", _selectedFolio != null), ("Folio chưa xuất HĐ", _selectedFolio?.IssuedInvoice == null), ("Thông tin khách hàng đầy đủ", !string.IsNullOrEmpty(_guestName)), ("Loại HĐ hợp lệ", !string.IsNullOrEmpty(_invoiceType)), ("Số tiền hợp lệ", total > 0) };
        var allOk = true;
        foreach (var (label, ok) in checks) { Step4Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 12, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 11, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        Step4Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 8, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng phát hành" : "Cần kiểm tra thêm", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
        Step4Right.Children.Add(BuildProviderReadinessCard());
        UpdateNav();
    }

    private FrameworkElement BuildProviderReadinessCard()
    {
        var readiness = _providerReadiness;
        var background = readiness is null ? "#FEF3C7" : readiness.IsReady ? "#DCFCE7" : "#FEE2E2";
        var foreground = readiness is null ? "#A16207" : readiness.IsReady ? "#166534" : "#B91C1C";
        var title = readiness is null ? "Đang kiểm tra provider thật..." : readiness.IsReady ? "Provider sẵn sàng phát hành" : "Chưa thể phát hành qua provider thật";
        var detail = readiness is null
            ? "Đang đối chiếu adapter và cấu hình active, không đọc secret ra giao diện."
            : readiness.Message + (readiness.IsReady ? string.Empty : " Có thể lưu hóa đơn nháp để xử lý sau.");
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C(foreground) });
        panel.Children.Add(new TextBlock { Text = detail, FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = C(foreground), Margin = new Thickness(0, 4, 0, 0) });
        return new Border { Background = Cb(background), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 8, 0, 0), Child = panel };
    }

    private async Task RefreshProviderReadinessAsync()
    {
        await _dataOperationGate.WaitAsync();
        try { await RefreshProviderReadinessCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task RefreshProviderReadinessCoreAsync()
    {
        if (_isClosed || _step != 4 || _eInvoiceService is null) return;
        try
        {
            _providerReadiness = await _eInvoiceService.GetProviderReadinessAsync(CancellationToken.None);
            if (_step == 4)
            {
                RenderStep4();
            }
        }
        catch (Exception ex)
        {
            _providerReadiness = new InvoiceProviderReadinessDto
            {
                Status = "Unavailable",
                Message = $"Không kiểm tra được readiness provider: {ex.Message}"
            };
            if (_step == 4)
            {
                RenderStep4();
            }
        }
    }

    // ═══ ISSUE ═══

    private async void OnIssue(object s, RoutedEventArgs e)
    {
        await _dataOperationGate.WaitAsync();
        try { await IssueCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task IssueCoreAsync()
    {
        if (_isClosed) return;
        if (_selectedFolio == null) return;
        if (_eInvoiceService is null || _folioRepository is null || _executionContext?.Current is not { } context || _selectedFolio.FolioId == Guid.Empty)
        {
            MessageBox.Show("Chưa có đủ e-invoice service, Folio repository hoặc property context dữ liệu thật.", "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var readiness = await _eInvoiceService.GetProviderReadinessAsync(CancellationToken.None);
            _providerReadiness = readiness;
            var ledger = await _folioRepository.GetLedgerAsync(context.Property.PropertyId.Value, _selectedFolio.FolioId, CancellationToken.None);
            if (ledger is null)
            {
                MessageBox.Show("Không tìm thấy Folio thật để lập hóa đơn.", "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var lines = ledger.Entries
                .Where(entry => entry.EntryType is not HotelFolioEntryType.Payment
                    and not HotelFolioEntryType.Deposit
                    and not HotelFolioEntryType.Refund)
                .Select(entry => new InvoiceLineDraftDto
                {
                    ItemCode = entry.EntryType.ToString(),
                    ItemName = string.IsNullOrWhiteSpace(entry.Description) ? entry.EntryType.ToString() : entry.Description,
                    UnitName = "Lần",
                    Quantity = 1,
                    UnitPrice = entry.Amount,
                    TaxRate = 0
                })
                .Where(line => line.UnitPrice != 0m)
                .ToList();
            if (lines.Count == 0)
            {
                MessageBox.Show("Folio chưa có khoản mục để lập hóa đơn.", "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var invoiceNo = $"FO-{ledger.FolioNumber}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var invoiceId = await _eInvoiceService.CreateDraftAsync(new InvoiceDraftDto
            {
                InvoiceNo = invoiceNo,
                InvoiceSeries = string.Empty,
                TemplateCode = _template,
                InvoiceDate = DateTime.Today,
                BuyerName = _guestName,
                BuyerTaxCode = string.IsNullOrWhiteSpace(_taxCode) ? null : _taxCode,
                BuyerAddress = string.IsNullOrWhiteSpace(_address) ? null : _address,
                // Let the scoped e-invoice service select the active tenant/branch provider.
                ProviderCode = string.Empty,
                Lines = lines
            }, CancellationToken.None);

            _submitted = true;
            if (!readiness.IsReady)
            {
                MessageBox.Show(
                    $"Đã lưu hóa đơn nháp {invoiceNo}; chưa phát hành vì provider thật chưa sẵn sàng.\n\n{readiness.Message}",
                    "Hóa đơn điện tử",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                DialogResult = true;
                Close();
                return;
            }

            try
            {
                var queued = await _eInvoiceService.QueueIssueAsync(invoiceId, CancellationToken.None);
                MessageBox.Show($"Đã đưa hóa đơn {invoiceNo} của Folio {_selectedFolio.Code} vào hàng đợi phát hành ({queued.Status}).", "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception queueError)
            {
                MessageBox.Show($"Đã lưu hóa đơn nháp {invoiceNo} vào cơ sở dữ liệu, nhưng chưa phát hành được qua provider thật:\n\n{queueError.Message}", "Hóa đơn điện tử", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể phát hành hóa đơn: {ex.Message}", "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══ EVENT HANDLERS ═══

    private void OnClose(object s, RoutedEventArgs e) => Close();

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsIw(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static TextBlock Sep() => new() { Text = " › ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record IwFolio(int Id, string Code, string Guest, string? Company, string Reservation, string Room, string Currency, int Total, int Deposit, int Paid, string Arrival, string Departure, string Status, string? IssuedInvoice, Guid FolioId = default, byte[]? RowVersion = null, int Tax = 0) { public string TotalDisplay => $"{Total:N0}"; }
internal static class IwExt { public static TextBlock WithRunsIw(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
