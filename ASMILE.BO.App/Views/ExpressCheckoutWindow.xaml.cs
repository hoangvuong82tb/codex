using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Payment;
using Microsoft.Extensions.DependencyInjection;
using ASMILE.BO.Core.FrontOffice.Folio;

namespace ASMILE.BO.App.Views;

public partial class ExpressCheckoutWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step;
    private ExGuest? _guest; private bool _selected;
    private int _guestPage, _guestPageSize = 20;
    private readonly List<ExGuest> _allGuests = new();
    private int _folioBalance;
    private string _authStatus = "Chưa có dữ liệu ủy quyền";
    private int _warnings, _blocking;
    private string _currency = "VND";
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly ApproveDepartureCommandHandler? _approveDepartureHandler;
    private readonly CheckOutHotelStayCommandHandler? _checkOutHandler;
    private readonly HotelPaymentApplicationService? _paymentService;
    private readonly IHousekeepingWorkflowService? _housekeepingWorkflowService;
    private readonly IKeyCardService? _keyCardService;
    private readonly IEInvoiceService? _eInvoiceService;
    private readonly Dictionary<Guid, IReadOnlyList<KeyCardDto>> _keysByStay = new();
    private Guid? _housekeepingTaskId;
    private Guid? _invoiceId;
    private string _paymentMethod = "Tiền mặt";
    private decimal _paymentAmount;
    private string _paymentReference = string.Empty;
    private bool _hasPayment;

    public ExpressCheckoutWindow()
    {
        InitializeComponent(); Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _approveDepartureHandler = services?.GetService<ApproveDepartureCommandHandler>();
        _checkOutHandler = services?.GetService<CheckOutHotelStayCommandHandler>();
        _paymentService = services?.GetService<HotelPaymentApplicationService>();
        _housekeepingWorkflowService = services?.GetService<IHousekeepingWorkflowService>();
        _keyCardService = services?.GetService<IKeyCardService>();
        _eInvoiceService = services?.GetService<IEInvoiceService>();
        InitFilters(); RefreshGuestPage(true); GoTo(1);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_stayRepository is null || _folioRepository is null || _contextAccessor?.Current is not { } context)
        {
            FooterStepLabel.Text = "Chưa cấu hình Stay/Folio service và property context dữ liệu thật.";
            return;
        }

        try
        {
            _currency = context.Property.CurrencyCode ?? "VND";
            var stays = await _stayRepository.GetCurrentInHouseAsync(context.Property.PropertyId.Value, CancellationToken.None);
            _allGuests.Clear();
            _keysByStay.Clear();
            foreach (var stay in stays)
            {
                var folio = await _folioRepository.GetByStayAsync(context.Property.PropertyId.Value, stay.HotelStayId, CancellationToken.None);
                var ledger = folio is null
                    ? null
                    : await _folioRepository.GetLedgerAsync(context.Property.PropertyId.Value, folio.HotelFolioId, CancellationToken.None);
                if (_keyCardService is not null)
                {
                    try { _keysByStay[stay.HotelStayId] = await _keyCardService.GetKeysForStayAsync(stay.HotelStayId, CancellationToken.None); }
                    catch { _keysByStay[stay.HotelStayId] = Array.Empty<KeyCardDto>(); }
                }
                _allGuests.Add(MapRealGuest(stay, folio, ledger, _currency));
            }

            _guest = null;
            _selected = false;
            _hasPayment = false;
            _housekeepingTaskId = null;
            _invoiceId = null;
            RefreshGuestPage(true);
            UpdateSummary();
            FooterStepLabel.Text = $"Dữ liệu thật · {_allGuests.Count:N0} khách đang lưu trú";
        }
        catch (Exception ex)
        {
            _allGuests.Clear();
            RefreshGuestPage(true);
            FooterStepLabel.Text = "Không thể tải danh sách Express Check-out: " + ex.Message;
        }
    }

    private static ExGuest MapRealGuest(HotelStayDto stay, HotelFolioDto? folio, FolioLedgerDto? ledger, string currency)
    {
        var balance = folio?.OutstandingBalance ?? 0m;
        var approved = stay.IsDepartureApproved;
        var status = approved ? (balance <= 0m ? "Sẵn sàng" : "Cần kiểm tra") : "Chờ ủy quyền";
        return new ExGuest(
            stay.RoomNumber,
            stay.GuestName,
            stay.ScheduledArrivalDate.ToString("dd/MM"),
            stay.ScheduledDepartureDate.ToString("dd/MM"),
            "—",
            false,
            "Chưa có dữ liệu ủy quyền",
            approved ? "Hợp lệ" : "Chưa có dữ liệu ủy quyền",
            status,
            decimal.ToInt32(decimal.Round(balance, 0, MidpointRounding.AwayFromZero)),
            stay.HotelStayId,
            stay.RowVersion,
            folio?.FolioNumber ?? "—",
            currency,
            ledger?.Entries ?? Array.Empty<HotelFolioEntryDto>(),
            folio?.HotelFolioId ?? Guid.Empty,
            folio?.RowVersion,
            ledger?.Entries.Any(e => e.EntryType is HotelFolioEntryType.Payment or HotelFolioEntryType.Deposit) == true,
            stay.RoomId);
    }


    private void InitFilters() { Set(GuestVipFilter, "Tất cả","VIP","Không VIP"); Set(GuestStatusFilter, "Tất cả","Sẵn sàng","Cần kiểm tra","Không đủ ĐK","Chờ ủy quyền"); Set(GuestPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 6); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; Step5Content.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed; Step6Content.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn hồ sơ"),("B","Ủy quyền"),("C","Folio & TT"),("D","DV & hậu kiểm"),("E","HĐ & bàn giao"),("F","Xác nhận") };
        for (int i = 0; i < 6; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 5) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep() { switch (_step) { case 2: BuildStep2(); break; case 3: BuildStep3(); break; case 4: BuildStep4(); break; case 5: BuildStep5(); break; case 6: BuildStep6(); break; } }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = _selected && _authStatus == "Hợp lệ" && _folioBalance <= 0 && _checkOutHandler is not null && _guest?.RowVersion is { Length: 8 }; }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        FooterStepLabel.Text = $"Bước {_step}/6 · {new[] { "Chọn hồ sơ","Ủy quyền","Folio & TT","DV & hậu kiểm","HĐ & bàn giao","Xác nhận" }[_step - 1]} · {_warnings} WB · {_blocking} BLK";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var g = _guest; var sel = _selected;
        SummaryStrip.Children.Add(Strip("Khách", g?.Name ?? "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", g?.Room ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Rời", g?.ETD ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Hình thức", g?.ExpressMethod ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Còn P/Thu", sel ? $"{_folioBalance:N0}" : "—", _folioBalance == 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Ủy quyền", sel ? _authStatus : "—", _authStatus == "Hợp lệ")); SummaryStrip.Children.Add(Sep());
        var ready = sel && _authStatus == "Hợp lệ" && _folioBalance <= 0;
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Sẵn sàng" : "Chưa SS", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    private List<ExGuest> GetFiltered() { var f = _allGuests.AsEnumerable(); var kw = GuestSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Room.Contains(kw)); var v = GuestVipFilter.SelectedItem?.ToString(); if (v == "VIP") f = f.Where(g => g.IsVip); else if (v == "Không VIP") f = f.Where(g => !g.IsVip); var s = GuestStatusFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(s) && s != "Tất cả") f = f.Where(g => g.Status == s); return f.ToList(); }
    private void RefreshGuestPage(bool reset) { if (reset) _guestPage = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _guestPageSize - 1) / _guestPageSize); if (_guestPage >= tp) _guestPage = tp - 1; GuestGrid.ItemsSource = f.Skip(_guestPage * _guestPageSize).Take(_guestPageSize).ToList(); GuestPagerSummary.Text = $"Hiển thị {_guestPage * _guestPageSize + 1}–{Math.Min((_guestPage + 1) * _guestPageSize, f.Count)} trên tổng {f.Count} hồ sơ"; GuestPrevBtn.IsEnabled = _guestPage > 0; GuestNextBtn.IsEnabled = _guestPage < tp - 1; BuildBtns(tp); }
    private void BuildBtns(int tp) { GuestPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _guestPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _guestPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _guestPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _guestPage = idx; RefreshGuestPage(false); }; nb.Template = GuestPrevBtn.Template; GuestPageBtns.Children.Add(nb); } }
    private void OnGuestPreviewChanged(object s, SelectionChangedEventArgs e) { if (GuestGrid.SelectedItem is not ExGuest g) return; _guest = g; _selected = false; _folioBalance = g.Balance; _authStatus = g.AuthStatus; _hasPayment = g.HasPayment; _paymentAmount = Math.Max(0, g.Balance); _paymentReference = string.Empty; _housekeepingTaskId = null; _invoiceId = null; GuestEmptyState.Visibility = Visibility.Collapsed; GuestDetailPanel.Visibility = Visibility.Visible; GDName.Text = g.Name; GDMeta.Text = g.FolioNumber; GDInfo1.Text = $"Phòng: {g.Room} · {g.ExpressMethod}"; GDInfo2.Text = $"Ngày: {g.Arrival} → {g.Departure} · Rời {g.ETD}"; GDInfo3.Text = $"Ủy quyền: {g.AuthStatus}"; GDInfo4.Text = $"Còn phải thu: {g.Balance:N0} {g.Currency}"; BtnSelectGuest.IsEnabled = g.Status == "Sẵn sàng" || g.Status == "Cần kiểm tra"; UpdateSummary(); }
    private void OnSelectGuest(object s, RoutedEventArgs e) { if (_guest == null) return; _selected = true; BtnSelectGuest.Content = "✓ Đã chọn"; BtnSelectGuest.IsEnabled = false; UpdateSummary(); UpdateNav(); }
    private void OnApplyGuestFilters(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilters(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestVipFilter.SelectedIndex = 0; GuestStatusFilter.SelectedIndex = 0; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }

    private void BuildStep2()
    {
        Step2Left.Children.Clear(); Step2Right.Children.Clear(); if (_guest == null) return;
        Step2Left.Children.Add(STitle("Thông tin ủy quyền")); Step2Left.Children.Add(Row("Hình thức", _guest.ExpressMethod)); Step2Left.Children.Add(Row("Ngày giờ ủy quyền", "Chưa có dữ liệu thật")); Step2Left.Children.Add(Row("Người ủy quyền", _guest.Name)); Step2Left.Children.Add(Row("Kênh nhận", "Chưa có dữ liệu thật")); Step2Left.Children.Add(Row("Mã xác nhận", "Chưa có dữ liệu thật")); Step2Left.Children.Add(Row("Người tiếp nhận", "Chưa có dữ liệu thật"));
        Step2Left.Children.Add(new Border { Background = _authStatus == "Hợp lệ" ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = _authStatus == "Hợp lệ" ? "✓ Ủy quyền hợp lệ" : "✗ Không hợp lệ", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = _authStatus == "Hợp lệ" ? C("#16A34A") : C("#DC2626") } });
        Step2Right.Children.Add(STitle("Phạm vi ủy quyền")); foreach (var l in new[] { "Phát sinh sau khi rời","Thu tiền theo PT đã lưu","Gửi HĐ điện tử","Xử lý Minibar sau","Sử dụng đặt cọc","Hoàn tiền vào PT ban đầu" }) Step2Right.Children.Add(CheckItem(l, _authStatus == "Hợp lệ"));
        var approve = new Button { Content = _authStatus == "Hợp lệ" ? "✓ Đã phê duyệt trả phòng" : "Phê duyệt trả phòng", IsEnabled = _authStatus != "Hợp lệ" && _guest.RowVersion is not null && _approveDepartureHandler is not null, Style = FindResource("PrimaryButton") as Style, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        approve.Click += OnApproveDeparture;
        Step2Right.Children.Add(approve);
        Step2Right.Children.Add(STitle("Ủy quyền thanh toán")); Step2Right.Children.Add(Row("Phương thức", "Chưa có dữ liệu thật")); Step2Right.Children.Add(Row("Hạn mức", "Chưa có dữ liệu thật")); Step2Right.Children.Add(Row("Còn có thể thu", "Chưa có dữ liệu thật"));
        _blocking = _authStatus == "Hợp lệ" && _folioBalance <= 0 ? 0 : 1;
        _warnings = _authStatus == "Hợp lệ" && _folioBalance <= 0 ? 0 : 1;
        UpdateNav();
    }

    private async void OnApproveDeparture(object sender, RoutedEventArgs e)
    {
        if (_guest is null || _guest.RowVersion is null || _approveDepartureHandler is null) return;
        try
        {
            var result = await _approveDepartureHandler.HandleAsync(new ApproveDepartureCommand(_guest.StayId, _guest.RowVersion), CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể phê duyệt trả phòng.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _guest = _guest with
            {
                AuthStatus = "Hợp lệ",
                Status = _guest.Balance <= 0 ? "Sẵn sàng" : "Cần kiểm tra",
                RowVersion = result.Value.RowVersion
            };
            _authStatus = "Hợp lệ";
            _blocking = _folioBalance <= 0 ? 0 : 1;
            _warnings = _folioBalance <= 0 ? 0 : 1;
            BuildStep2();
            UpdateSummary();
            UpdateNav();
            MessageBox.Show("Đã ghi nhận phê duyệt trả phòng vào dữ liệu Stay thật.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show("Không thể phê duyệt trả phòng: " + ex.Message, "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void BuildStep3()
    {
        Step3Left.Children.Clear(); Step3Right.Children.Clear();
        if (_guest is null) return;
        var entries = _guest.Entries ?? Array.Empty<HotelFolioEntryDto>();
        Step3Left.Children.Add(STitle("Folio items")); var folios = entries.Select(e => new { Time = e.BusinessDate?.ToString("dd/MM") ?? "—", Item = string.IsNullOrWhiteSpace(e.Description) ? e.EntryType.ToString() : e.Description, Dept = e.EntryType.ToString(), Debit = e.Amount > 0 ? e.Amount.ToString("N0") : "0", Credit = e.Amount < 0 ? (-e.Amount).ToString("N0") : "0", Payer = "Chưa có mapping", Status = "Đã ghi sổ", Post = "—" }).ToList();
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = folios, MaxHeight = 240 }; dg.Columns.Add(new DataGridTextColumn { Header = "TG", Binding = new System.Windows.Data.Binding("Time"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Khoản mục", Binding = new System.Windows.Data.Binding("Item"), Width = 100 }); dg.Columns.Add(new DataGridTextColumn { Header = "BP", Binding = new System.Windows.Data.Binding("Dept"), Width = 50 }); dg.Columns.Add(new DataGridTextColumn { Header = "Nợ", Binding = new System.Windows.Data.Binding("Debit"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Có", Binding = new System.Windows.Data.Binding("Credit"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Người TT", Binding = new System.Windows.Data.Binding("Payer"), Width = 55 }); dg.Columns.Add(new DataGridTextColumn { Header = "TT", Binding = new System.Windows.Data.Binding("Status"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "HK", Binding = new System.Windows.Data.Binding("Post"), Width = 55 });
        Step3Left.Children.Add(dg);
        Step3Right.Children.Add(STitle("Phương án thanh toán"));
        Step3Right.Children.Add(Row("Tổng số khoản", entries.Count.ToString("N0")));
        Step3Right.Children.Add(Row("Còn phải thu", $"{_folioBalance:N0} {_guest.Currency}"));
        Step3Right.Children.Add(Row("Đã ghi nhận", _hasPayment ? "Có thanh toán thật" : "Chưa ghi nhận"));

        Step3Right.Children.Add(new TextBlock { Text = "Phương thức", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 8, 0, 2) });
        var methods = new[] { "Tiền mặt", "Visa", "Master", "JCB", "AMEX", "QR", "Chuyển khoản", "Ví điện tử", "Công ty", "Đại lý", "Voucher" };
        var methodBox = new ComboBox { ItemsSource = methods, SelectedItem = _paymentMethod, Style = FindResource("ModernComboBox") as Style, Width = 190, HorizontalAlignment = HorizontalAlignment.Left };
        methodBox.SelectionChanged += (_, _) => _paymentMethod = methodBox.SelectedItem?.ToString() ?? "Tiền mặt";
        Step3Right.Children.Add(methodBox);
        Step3Right.Children.Add(new TextBlock { Text = "Số tiền", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 6, 0, 2) });
        var amountBox = new TextBox { Text = _paymentAmount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Style = FindResource("ModernTextBox") as Style, Width = 190, HorizontalAlignment = HorizontalAlignment.Left };
        amountBox.TextChanged += (_, _) => { if (TryParseMoney(amountBox.Text, out var amount)) _paymentAmount = Math.Max(0, amount); };
        Step3Right.Children.Add(amountBox);
        Step3Right.Children.Add(new TextBlock { Text = "Tham chiếu (tuỳ chọn)", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 6, 0, 2) });
        var referenceBox = new TextBox { Text = _paymentReference, Style = FindResource("ModernTextBox") as Style, Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
        referenceBox.TextChanged += (_, _) => _paymentReference = referenceBox.Text.Trim();
        Step3Right.Children.Add(referenceBox);
        var receiveButton = new Button { Content = _hasPayment ? "Ghi nhận thêm thanh toán" : "Ghi nhận thanh toán", IsEnabled = _paymentService is not null && _guest.FolioId != Guid.Empty && _guest.FolioRowVersion is { Length: 8 } && _folioBalance > 0, Style = FindResource("PrimaryButton") as Style, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        receiveButton.Click += OnRecordExpressPayment;
        Step3Right.Children.Add(receiveButton);
        Step3Right.Children.Add(new Border { Background = Cb("#EFF6FF"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 8, 0, 0), Child = new TextBlock { Text = "Thanh toán được ghi qua Payment Engine và bút toán Folio thật.", FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = C("#1D4ED8") } });
    }

    private async void OnRecordExpressPayment(object sender, RoutedEventArgs e)
    {
        if (_guest is null || _paymentService is null || _contextAccessor?.Current is not { } context || _guest.FolioId == Guid.Empty || _guest.FolioRowVersion is not { Length: 8 })
        {
            MessageBox.Show("Folio chưa có đủ dữ liệu thật để ghi nhận thanh toán.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryMapPaymentMethod(_paymentMethod, out var method))
        {
            MessageBox.Show("Phương thức thanh toán chưa hợp lệ.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_paymentAmount <= 0 || _paymentAmount > _folioBalance)
        {
            MessageBox.Show($"Số tiền phải lớn hơn 0 và không vượt quá {_folioBalance:N0} {_guest.Currency}.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Enum.TryParse<PaymentCurrency>(context.Property.CurrencyCode, true, out var currency))
        {
            MessageBox.Show("Property chưa có currency hợp lệ để ghi thanh toán.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = await _paymentService.ReceiveAsync(
                new ReceivePaymentCommand(_guest.FolioId, _guest.FolioRowVersion, method, _paymentAmount, currency, 1, string.IsNullOrWhiteSpace(_paymentReference) ? null : _paymentReference, "Thu tại Express Check-out"),
                CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể ghi nhận thanh toán.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var freshStay = _stayRepository is null ? null : await _stayRepository.GetDtoAsync(context.Property.PropertyId.Value, _guest.StayId, CancellationToken.None);
            var freshFolio = _folioRepository is null ? null : await _folioRepository.GetByStayAsync(context.Property.PropertyId.Value, _guest.StayId, CancellationToken.None);
            var freshLedger = freshFolio is null ? null : await _folioRepository!.GetLedgerAsync(context.Property.PropertyId.Value, freshFolio.HotelFolioId, CancellationToken.None);
            if (freshStay is not null && freshFolio is not null)
            {
                _guest = MapRealGuest(freshStay, freshFolio, freshLedger, _guest.Currency) with
                {
                    Arrival = _guest.Arrival,
                    Departure = _guest.Departure,
                    ETD = _guest.ETD,
                    ExpressMethod = _guest.ExpressMethod,
                    IsVip = _guest.IsVip,
                    AuthStatus = _authStatus,
                    Status = _folioBalance - _paymentAmount <= 0 ? "Sẵn sàng" : "Cần kiểm tra"
                };
                _folioBalance = _guest.Balance;
                _hasPayment = _guest.HasPayment;
                var index = _allGuests.FindIndex(x => x.StayId == _guest.StayId);
                if (index >= 0) _allGuests[index] = _guest;
            }
            else
            {
                _folioBalance = Math.Max(0, _folioBalance - decimal.ToInt32(decimal.Round(_paymentAmount, 0, MidpointRounding.AwayFromZero)));
                _hasPayment = true;
            }

            _paymentAmount = _folioBalance;
            _paymentReference = string.Empty;
            _invoiceId = null;
            BuildStep3();
            UpdateSummary();
            UpdateNav();
            MessageBox.Show($"Đã ghi nhận thanh toán {result.Value.PaymentNumber} vào Folio thật.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể ghi nhận thanh toán: " + ex.Message, "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static bool TryParseMoney(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out value)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
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

    private void BuildStep4()
    {
        Step4Left.Children.Clear(); Step4Right.Children.Clear();
        if (_guest is null) return;
        var entries = _guest.Entries ?? Array.Empty<HotelFolioEntryDto>();
        Step4Left.Children.Add(STitle("Dịch vụ & hậu kiểm")); var svcs = entries.Where(e => e.EntryType is not HotelFolioEntryType.Payment and not HotelFolioEntryType.Deposit and not HotelFolioEntryType.Refund).Select(e => new { Svc = string.IsNullOrWhiteSpace(e.Description) ? e.EntryType.ToString() : e.Description, Dept = e.EntryType.ToString(), Amt = e.Amount.ToString("N0"), Status = "Đã ghi sổ", PostOk = "—", Deadline = "Chưa có", }).ToList();
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = svcs, MaxHeight = 220 }; dg.Columns.Add(new DataGridTextColumn { Header = "Dịch vụ", Binding = new System.Windows.Data.Binding("Svc"), Width = 100 }); dg.Columns.Add(new DataGridTextColumn { Header = "BP", Binding = new System.Windows.Data.Binding("Dept"), Width = 50 }); dg.Columns.Add(new DataGridTextColumn { Header = "Thành tiền", Binding = new System.Windows.Data.Binding("Amt"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Trạng thái", Binding = new System.Windows.Data.Binding("Status"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Cho HK", Binding = new System.Windows.Data.Binding("PostOk"), Width = 55 }); dg.Columns.Add(new DataGridTextColumn { Header = "Hạn chót", Binding = new System.Windows.Data.Binding("Deadline"), Width = 70 });
        Step4Left.Children.Add(dg); Step4Right.Children.Add(STitle("Kế hoạch hậu kiểm"));
        Step4Right.Children.Add(Row("Nguồn deadline", _housekeepingTaskId.HasValue ? "Task Housekeeping thật" : "Chưa tạo task"));
        Step4Right.Children.Add(Row("Trạng thái", _housekeepingTaskId.HasValue ? $"Đã tạo: {_housekeepingTaskId.Value:D}" : "Chưa có task hậu kiểm"));
        var createTask = new Button { Content = _housekeepingTaskId.HasValue ? "✓ Đã tạo task dọn phòng" : "Tạo task dọn phòng sau checkout", IsEnabled = !_housekeepingTaskId.HasValue && _housekeepingWorkflowService is not null && _guest.RoomId != Guid.Empty, Style = FindResource("PrimaryButton") as Style, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        createTask.Click += OnCreateDepartureCleaningTask;
        Step4Right.Children.Add(createTask);
    }

    private async void OnCreateDepartureCleaningTask(object sender, RoutedEventArgs e)
    {
        if (_guest is null || _housekeepingWorkflowService is null || _contextAccessor?.Current is not { } context || _guest.RoomId == Guid.Empty) return;
        try
        {
            var result = await _housekeepingWorkflowService.CreateTaskAsync(
                new HkTaskCreateRequest(context.Property.PropertyId.Value, _guest.RoomId, HousekeepingTaskType.DepartureClean, RoomPriority.Normal, $"Express checkout · Stay {_guest.StayId:D}"),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể tạo task hậu kiểm.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _housekeepingTaskId = result.Value;
            BuildStep4();
            BuildStep6();
            UpdateNav();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tạo task hậu kiểm: " + ex.Message, "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BuildStep5()
    {
        Step5Left.Children.Clear(); Step5Right.Children.Clear();
        var entries = _guest?.Entries ?? Array.Empty<HotelFolioEntryDto>();
        Step5Left.Children.Add(STitle("Hóa đơn & chứng từ"));
        Step5Left.Children.Add(Row("Loại HĐ", _invoiceId.HasValue ? "Hóa đơn điện tử · Nháp" : "Chưa tạo nháp hóa đơn"));
        Step5Left.Children.Add(Row("Mã nháp", _invoiceId?.ToString("D") ?? "—"));
        Step5Left.Children.Add(Row("Email nhận", "Chưa có dữ liệu liên hệ"));
        Step5Left.Children.Add(Row("Thời điểm gửi", "Chưa có workflow gửi"));
        var invoiceButton = new Button { Content = _invoiceId.HasValue ? "✓ Đã tạo nháp hóa đơn" : "Tạo nháp hóa đơn từ Folio", IsEnabled = !_invoiceId.HasValue && _eInvoiceService is not null && _guest is not null && entries.Count > 0, Style = FindResource("PrimaryButton") as Style, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        invoiceButton.Click += OnCreateInvoiceDraft;
        Step5Left.Children.Add(invoiceButton);
        var keys = _guest is null ? Array.Empty<KeyCardDto>() : _keysByStay.GetValueOrDefault(_guest.StayId, Array.Empty<KeyCardDto>());
        var activeKeys = keys.Count(x => x.Status is KeyStatus.Active or KeyStatus.Encoded);
        Step5Right.Children.Add(STitle("Bàn giao Express")); Step5Right.Children.Add(Row("Hình thức trả thẻ", _guest?.ExpressMethod ?? "—")); Step5Right.Children.Add(Row("Số thẻ đã cấp", $"{keys.Count:N0} (đang hoạt động: {activeKeys:N0})")); Step5Right.Children.Add(Row("Vị trí trả", _keyCardService is null ? "Chưa cấu hình key-card service" : "Tự động thu hồi khi checkout")); Step5Right.Children.Add(Row("Hành lý", "Chưa có dữ liệu bàn giao"));
        Step5Right.Children.Add(new TextBlock { Text = "Liên lạc với khách", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#0F172A"), Margin = new Thickness(0, 8, 0, 4) }); Step5Right.Children.Add(Row("Kênh", "Chưa có dữ liệu liên hệ"));
    }

    private async void OnCreateInvoiceDraft(object sender, RoutedEventArgs e)
    {
        if (_guest is null || _eInvoiceService is null) return;
        var lines = (_guest.Entries ?? Array.Empty<HotelFolioEntryDto>())
            .Where(e => e.EntryType is not HotelFolioEntryType.Payment and not HotelFolioEntryType.Deposit and not HotelFolioEntryType.Refund)
            .Select(e => new InvoiceLineDraftDto
            {
                ItemCode = e.EntryType.ToString(),
                ItemName = string.IsNullOrWhiteSpace(e.Description) ? e.EntryType.ToString() : e.Description,
                UnitName = "Lần",
                Quantity = 1,
                UnitPrice = e.Amount,
                TaxRate = 0
            })
            .Where(x => x.UnitPrice != 0)
            .ToList();
        if (lines.Count == 0)
        {
            MessageBox.Show("Folio chưa có khoản mục để lập hóa đơn.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _invoiceId = await _eInvoiceService.CreateDraftAsync(new InvoiceDraftDto
            {
                InvoiceNo = $"EXP-{_guest.FolioNumber}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                InvoiceSeries = "",
                TemplateCode = "",
                InvoiceDate = DateTime.Today,
                BuyerName = _guest.Name,
                // Let the scoped e-invoice service select the active tenant/branch provider.
                ProviderCode = string.Empty,
                Lines = lines
            }, CancellationToken.None);
            BuildStep5();
            BuildStep6();
            UpdateNav();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tạo nháp hóa đơn: " + ex.Message, "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BuildStep6()
    {
        Step6Left.Children.Clear(); Step6Right.Children.Clear(); if (_guest == null) return;
        var entries = _guest.Entries ?? Array.Empty<HotelFolioEntryDto>();
        Step6Left.Children.Add(STitle("Tóm tắt Express Check-out")); Step6Left.Children.Add(Row("Khách", _guest.Name)); Step6Left.Children.Add(Row("Phòng", _guest.Room + " · Rời " + _guest.ETD)); Step6Left.Children.Add(Row("Hình thức", _guest.ExpressMethod)); Step6Left.Children.Add(Row("Ủy quyền", _authStatus)); Step6Left.Children.Add(Row("Folio", $"Còn {_folioBalance:N0} {_guest.Currency}")); Step6Left.Children.Add(Row("Hậu kiểm", _housekeepingTaskId.HasValue ? "Đã tạo task DepartureClean" : "Chưa tạo task hậu kiểm")); Step6Left.Children.Add(Row("Hóa đơn", _invoiceId.HasValue ? "Đã tạo nháp hóa đơn" : "Chưa tạo nháp hóa đơn")); Step6Left.Children.Add(Row("Bàn giao", _keyCardService is not null ? "Tự động thu hồi thẻ khi checkout" : "Chưa cấu hình key-card service"));
        var hasPayment = _hasPayment || entries.Any(e => e.EntryType is HotelFolioEntryType.Payment or HotelFolioEntryType.Deposit);
        var withinLimit = _folioBalance <= 0;
        var keysAvailable = _keyCardService is not null;
        Step6Right.Children.Add(STitle("Kiểm tra sẵn sàng")); var items = new[] { ("Đã chọn hồ sơ", _selected), ("Có bằng chứng ủy quyền", _authStatus == "Hợp lệ"), ("Phạm vi ủy quyền hợp lệ", _authStatus == "Hợp lệ"), ("Có PT thanh toán", hasPayment || _folioBalance <= 0), ("Hạn mức đủ", withinLimit), ("Folio đã kiểm tra", entries.Count > 0), ("Có kế hoạch hậu kiểm", _housekeepingTaskId.HasValue), ("Có kênh gửi HĐ", false), ("Có phương án trả thẻ", keysAvailable), ("Không blocking issue", false) };
        var allOk = true; foreach (var (l, ok) in items) { Step6Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = l, FontSize = 12, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        Step6Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng Express C/O" : "Cần kiểm tra thêm", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#A16207") } });
    }

    private static FrameworkElement CheckItem(string l, bool ok) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = l, FontSize = 12, Foreground = C("#334155") } } };
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns6((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (_guest is null || !_selected || _checkOutHandler is null || _guest.RowVersion is not { Length: 8 }) return;
        if (_authStatus != "Hợp lệ" || _folioBalance > 0)
        {
            MessageBox.Show("Chưa đủ điều kiện: cần phê duyệt trả phòng và Folio phải cân bằng.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = await _checkOutHandler.HandleAsync(
                new CheckOutHotelStayCommand(_guest.StayId, _guest.RowVersion, DateTime.UtcNow, "Express Check-out", "Đã xác nhận trên luồng Express", false, false),
                CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể trả phòng trên Stay thật.", "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var keyWarning = string.Empty;
            if (_keyCardService is not null)
            {
                try
                {
                    var revoked = await _keyCardService.AutoRevokeOnCheckoutAsync(_guest.StayId, CancellationToken.None);
                    _keysByStay[_guest.StayId] = revoked;
                }
                catch (Exception ex)
                {
                    keyWarning = $"\nCảnh báo thu hồi thẻ: {ex.Message}";
                }
            }

            MessageBox.Show($"Đã trả phòng thành công cho {_guest.Name}, phòng {_guest.Room}. Stay đã được cập nhật trong database thật.{keyWarning}", "Express Check-out", MessageBoxButton.OK, string.IsNullOrWhiteSpace(keyWarning) ? MessageBoxImage.Information : MessageBoxImage.Warning);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể trả phòng: " + ex.Message, "Express Check-out", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private async void OnRefresh(object s, RoutedEventArgs e) { await Task.CompletedTask; OnLoadedAsync(this, new RoutedEventArgs()); }
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record ExGuest(string Room, string Name, string Arrival, string Departure, string ETD, bool IsVip, string ExpressMethod, string AuthStatus, string Status, int Balance, Guid StayId = default, byte[]? RowVersion = null, string FolioNumber = "—", string Currency = "VND", IReadOnlyList<HotelFolioEntryDto>? Entries = null, Guid FolioId = default, byte[]? FolioRowVersion = null, bool HasPayment = false, Guid RoomId = default) { public string VipDisplay => IsVip ? "★" : ""; public string BalanceDisplay => $"{Balance:N0}"; }
internal static class ExExt { public static TextBlock WithRuns6(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
