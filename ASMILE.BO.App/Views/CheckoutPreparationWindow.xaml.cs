using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.App.Services;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class CheckoutPreparationWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step;
    private CoGuest? _guest;
    private int _guestPage, _guestPageSize = 20;
    private readonly List<CoGuest> _allGuests = new();
    private int _folioBalance, _unpostedServices, _warnings;
    private string _currency = "VND";
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IReceptionUiService? _receptionUiService;
    private readonly IHousekeepingWorkflowService? _housekeepingWorkflowService;
    private readonly IKeyCardService? _keyCardService;
    private readonly Dictionary<Guid, IReadOnlyList<KeyCardDto>> _keysByStay = new();
    private Guid? _housekeepingTaskId;
    private readonly Guid? _initialStayId;
    private bool _persistedMode;

    public CheckoutPreparationWindow() : this(null)
    {
    }

    public CheckoutPreparationWindow(Guid? initialStayId)
    {
        _initialStayId = initialStayId;
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _receptionUiService = services?.GetService<IReceptionUiService>();
        _housekeepingWorkflowService = services?.GetService<IHousekeepingWorkflowService>();
        _keyCardService = services?.GetService<IKeyCardService>();
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
                var services = folio is null
                    ? Array.Empty<CheckoutServiceRow>()
                    : await LoadServiceRowsAsync(context.Property.PropertyId.Value, folio.HotelFolioId);
                if (_keyCardService is not null)
                {
                    try { _keysByStay[stay.HotelStayId] = await _keyCardService.GetKeysForStayAsync(stay.HotelStayId, CancellationToken.None); }
                    catch { _keysByStay[stay.HotelStayId] = Array.Empty<KeyCardDto>(); }
                }
                _allGuests.Add(MapRealGuest(stay, folio, services, _currency));
            }

            _persistedMode = true;
            _guest = null;
            RefreshGuestPage(true);
            SelectInitialGuest();
            UpdateSummary();
            FooterStepLabel.Text = $"Dữ liệu thật · { _allGuests.Count:N0} khách đang lưu trú";
        }
        catch (Exception ex)
        {
            _allGuests.Clear();
            RefreshGuestPage(true);
            FooterStepLabel.Text = "Không thể tải danh sách chuẩn bị Check-out: " + ex.Message;
        }
    }

    private void SelectInitialGuest()
    {
        if (_initialStayId is not { } stayId || stayId == Guid.Empty) return;
        var selected = _allGuests.FirstOrDefault(g => g.StayId == stayId);
        if (selected is null) return;

        var filtered = GetFiltered();
        var index = filtered.IndexOf(selected);
        if (index >= 0)
        {
            _guestPage = index / _guestPageSize;
            RefreshGuestPage(false);
        }

        GuestGrid.SelectedItem = selected;
        GuestGrid.ScrollIntoView(selected);
    }

    private async Task<IReadOnlyList<CheckoutServiceRow>> LoadServiceRowsAsync(Guid propertyId, Guid folioId)
    {
        var ledger = await _folioRepository!.GetLedgerAsync(propertyId, folioId, CancellationToken.None);
        if (ledger is null) return Array.Empty<CheckoutServiceRow>();
        return ledger.Entries
            .Where(e => e.EntryType is HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Laundry or HotelFolioEntryType.MiniBar or HotelFolioEntryType.Spa or HotelFolioEntryType.Telephone or HotelFolioEntryType.OtherCharge or HotelFolioEntryType.Tax or HotelFolioEntryType.ServiceCharge or HotelFolioEntryType.ManualCharge)
            .Select(e => new CheckoutServiceRow(
                string.IsNullOrWhiteSpace(e.Description) ? e.EntryType.ToString() : e.Description,
                e.EntryType.ToString(),
                e.CreatedAtUtc.ToLocalTime().ToString("dd/MM HH:mm"),
                "1",
                e.Amount.ToString("N0"),
                "Đã chốt"))
            .ToArray();
    }

    private static CoGuest MapRealGuest(HotelStayDto stay, HotelFolioDto? folio, IReadOnlyList<CheckoutServiceRow> services, string currency)
    {
        var balance = folio?.OutstandingBalance ?? 0m;
        return new CoGuest(
            stay.RoomNumber,
            stay.GuestName,
            stay.ScheduledArrivalDate.ToString("dd/MM"),
            stay.ScheduledDepartureDate.ToString("dd/MM"),
            stay.RoomTypeCode,
            $"{stay.Adults + stay.Children}",
            false,
            folio?.FolioNumber ?? "—",
            decimal.ToInt32(decimal.Round(balance, 0, MidpointRounding.AwayFromZero)),
            stay.HotelStayId,
            stay.RowVersion,
            decimal.ToInt32(decimal.Round(folio?.TotalCharges ?? 0m, 0, MidpointRounding.AwayFromZero)),
            decimal.ToInt32(decimal.Round(folio?.TotalPayments ?? 0m, 0, MidpointRounding.AwayFromZero)),
            services,
            stay.RoomId,
            currency);
    }


    private void InitFilters() { Set(GuestVipFilter, "Tất cả","VIP","Không VIP"); Set(GuestPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 6); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; Step5Content.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed; Step6Content.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn khách"),("B","Kiểm tra Folio"),("C","Kiểm tra dịch vụ"),("D","Bàn giao phòng"),("E","Hóa đơn & TT"),("F","Xác nhận") };
        for (int i = 0; i < 6; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 5) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep() { switch (_step) { case 2: BuildStep2(); break; case 3: BuildStep3(); break; case 4: BuildStep4(); break; case 5: BuildStep5(); break; case 6: BuildStep6(); break; } }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        var activeKeys = _guest is null ? 0 : GetActiveKeys(_guest).Count;
        _warnings = (_folioBalance > 0 ? 1 : 0)
            + (_unpostedServices > 0 ? 1 : 0)
            + (_housekeepingTaskId is null ? 1 : 0)
            + (_keyCardService is null || activeKeys > 0 ? 1 : 0);
        FooterStepLabel.Text = $"Bước {_step}/6 · {new[] { "Chọn khách","Kiểm tra Folio","Kiểm tra dịch vụ","Bàn giao phòng","Hóa đơn & TT","Xác nhận" }[_step - 1]} · {_warnings} cảnh báo";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var g = _guest;
        SummaryStrip.Children.Add(Strip("Khách", g?.Name ?? "Chưa chọn", g != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", g?.Room ?? "—", g != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Ngày đi", g?.Departure ?? "—", g != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Còn P/Thu", g != null ? $"{_folioBalance:N0}" : "—", g != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Chưa chốt", g != null ? $"{_unpostedServices}" : "—", _unpostedServices > 0)); SummaryStrip.Children.Add(Sep());
        var ready = _guest is not null
            && _folioBalance == 0
            && _unpostedServices == 0
            && _housekeepingTaskId.HasValue
            && _keyCardService is not null
            && GetActiveKeys(_guest).Count == 0;
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Sẵn sàng" : "Cần KT", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    // STEP 1
    private List<CoGuest> GetFiltered() { var f = _allGuests.AsEnumerable(); var kw = GuestSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Room.Contains(kw) || g.Folio.ToLower().Contains(kw)); var rk = GuestRoomBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(rk)) f = f.Where(g => g.Room.Contains(rk)); var v = GuestVipFilter.SelectedItem?.ToString(); if (v == "VIP") f = f.Where(g => g.IsVip); else if (v == "Không VIP") f = f.Where(g => !g.IsVip); return f.ToList(); }
    private void RefreshGuestPage(bool reset) { if (reset) _guestPage = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _guestPageSize - 1) / _guestPageSize); if (_guestPage >= tp) _guestPage = tp - 1; GuestGrid.ItemsSource = f.Skip(_guestPage * _guestPageSize).Take(_guestPageSize).ToList(); GuestPagerSummary.Text = $"Hiển thị {_guestPage * _guestPageSize + 1}–{Math.Min((_guestPage + 1) * _guestPageSize, f.Count)} trên tổng {f.Count} khách"; GuestPrevBtn.IsEnabled = _guestPage > 0; GuestNextBtn.IsEnabled = _guestPage < tp - 1; BuildGuestBtns(tp); }
    private void BuildGuestBtns(int tp) { GuestPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _guestPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _guestPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _guestPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _guestPage = idx; RefreshGuestPage(false); }; nb.Template = GuestPrevBtn.Template; GuestPageBtns.Children.Add(nb); } }
    private void OnGuestSelectionChanged(object s, SelectionChangedEventArgs e) { if (GuestGrid.SelectedItem is not CoGuest g) return; _guest = g; _folioBalance = g.Balance; _unpostedServices = g.Services?.Count(x => x.Status == "Chưa chốt") ?? 0; _housekeepingTaskId = null; GuestEmptyState.Visibility = Visibility.Collapsed; GuestDetailPanel.Visibility = Visibility.Visible; GDName.Text = g.Name; GDMeta.Text = g.Folio.Replace("FOL-", "STY-"); GDRoom.Text = $"Phòng: {g.Room} · {g.RoomType}"; GDDates.Text = $"Ngày: {g.Arrival} → {g.Departure} · 12:00"; GDFolio.Text = $"Folio: {g.Folio}"; GDVip.Text = g.IsVip ? "★ VIP" : "Khách thường"; GDBalance.Text = $"Còn phải thu: {_folioBalance:N0} {g.Currency}"; GDIssue1.Text = _unpostedServices > 0 ? $"· {_unpostedServices} khoản mục dịch vụ chưa chốt" : "· Không có khoản mục dịch vụ chưa chốt trong dữ liệu đã tải"; GDIssue2.Text = "· Bàn giao HSK và key-card sẽ được kiểm tra từ dữ liệu thật ở bước D"; UpdateSummary(); }
    private void OnApplyGuestFilters(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilters(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestRoomBox.Text = ""; GuestVipFilter.SelectedIndex = 0; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }

    // STEP 2: Folio
    private void BuildStep2()
    {
        Step2Left.Children.Clear(); Step2Right.Children.Clear();
        Step2Left.Children.Add(STitle("Tổng quan Folio")); if (_guest == null) return;
        Step2Left.Children.Add(Row("Mã Folio", _guest.Folio)); Step2Left.Children.Add(Row("Trạng thái", _folioBalance > 0 ? "Còn phải thu" : "Đã cân"));
        Step2Left.Children.Add(Row("Tổng phát sinh", $"{_guest.TotalCharges:N0} {_guest.Currency}")); Step2Left.Children.Add(Row("Đã thanh toán", $"{_guest.TotalPayments:N0} {_guest.Currency}")); Step2Left.Children.Add(Row("Đặt cọc", "Đã gộp trong tổng thanh toán của Folio")); Step2Left.Children.Add(Row("Còn phải thu", $"{_folioBalance:N0} {_guest.Currency}"));
        Step2Left.Children.Add(new Border { Background = _folioBalance > 0 ? Cb("#FEF9C3") : Cb("#DCFCE7"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = _folioBalance > 0 ? "⚠ Còn phải thu " + _folioBalance.ToString("N0") + " " + _guest.Currency : "✓ Folio đã cân", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = _folioBalance > 0 ? C("#A16207") : C("#16A34A") } });
        Step2Right.Children.Add(STitle("Khoản mục Folio"));
        var items = _guest.Services ?? Array.Empty<CheckoutServiceRow>();
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = items, MaxHeight = 220 };
        dg.Columns.Add(new DataGridTextColumn { Header = "Thời gian", Binding = new System.Windows.Data.Binding("Time"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Khoản mục", Binding = new System.Windows.Data.Binding("Svc"), Width = 130 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Loại", Binding = new System.Windows.Data.Binding("Dept"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Thành tiền", Binding = new System.Windows.Data.Binding("Amt"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Trạng thái", Binding = new System.Windows.Data.Binding("Status"), Width = 100 });
        Step2Right.Children.Add(dg);
    }

    // STEP 3: Services
    private void BuildStep3()
    {
        Step3Panel.Children.Clear();
        Step3Panel.Children.Add(STitle("Kiểm tra dịch vụ"));
        var svcs = _guest?.Services ?? Array.Empty<CheckoutServiceRow>();
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = svcs, MaxHeight = 300 };
        dg.Columns.Add(new DataGridTextColumn { Header = "Dịch vụ", Binding = new System.Windows.Data.Binding("Svc"), Width = 120 }); dg.Columns.Add(new DataGridTextColumn { Header = "Bộ phận", Binding = new System.Windows.Data.Binding("Dept"), Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Thời gian", Binding = new System.Windows.Data.Binding("Time"), Width = 100 }); dg.Columns.Add(new DataGridTextColumn { Header = "SL", Binding = new System.Windows.Data.Binding("Qty"), Width = 60 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Thành tiền", Binding = new System.Windows.Data.Binding("Amt"), Width = 90 });
        var statusCol = new DataGridTextColumn { Header = "Trạng thái", Binding = new System.Windows.Data.Binding("Status"), Width = 100 }; dg.Columns.Add(statusCol);
        Step3Panel.Children.Add(dg);
        _unpostedServices = svcs.Count(s => s.Status == "Chưa chốt");
        Step3Panel.Children.Add(new Border { Background = _unpostedServices > 0 ? Cb("#FFF7ED") : Cb("#DCFCE7"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 8, 0, 0), Child = new TextBlock { Text = _unpostedServices > 0 ? $"⚠ {_unpostedServices} dịch vụ chưa chốt — cần xác nhận trước Check-out" : "✓ Tất cả dịch vụ đã được chốt", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = _unpostedServices > 0 ? C("#EA580C") : C("#16A34A") } });
        UpdateSummary();
    }

    // STEP 4: Handover — chỉ hiển thị và ghi nhận bằng chứng thật.
    private void BuildStep4()
    {
        Step4Left.Children.Clear(); Step4Right.Children.Clear();
        var keys = _guest is null ? Array.Empty<KeyCardDto>() : GetActiveKeys(_guest);
        Step4Left.Children.Add(STitle("Nhà buồng"));
        Step4Left.Children.Add(Row("Task hậu kiểm", _housekeepingTaskId.HasValue ? $"Đã tạo · {_housekeepingTaskId.Value:D}" : "Chưa tạo"));
        var createTask = new Button { Content = _housekeepingTaskId.HasValue ? "✓ Đã tạo task DepartureClean" : "Tạo task DepartureClean", IsEnabled = !_housekeepingTaskId.HasValue && _housekeepingWorkflowService is not null && _guest?.RoomId != Guid.Empty, Style = FindResource("PrimaryButton") as Style, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        createTask.Click += OnCreateDepartureTask;
        Step4Left.Children.Add(createTask);

        Step4Right.Children.Add(STitle("Key-card và quyền truy cập"));
        Step4Right.Children.Add(Row("Thẻ đang hoạt động", _keyCardService is null ? "Chưa cấu hình key-card service" : $"{keys.Count:N0}"));
        var returnKeys = new Button { Content = keys.Count == 0 ? "✓ Không còn thẻ hoạt động" : $"Thu hồi {keys.Count:N0} thẻ thật", IsEnabled = _keyCardService is not null && keys.Count > 0, Style = FindResource("SecondaryButton") as Style, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        returnKeys.Click += OnReturnKeys;
        Step4Right.Children.Add(returnKeys);
        Step4Right.Children.Add(new TextBlock { Text = "Các mục hành lý, tài sản và Lost & Found không được đánh dấu giả tại đây; mở đúng module nghiệp vụ để ghi nhận bằng chứng nếu phát sinh.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = C("#64748B"), Margin = new Thickness(0, 12, 0, 0) });
    }

    private IReadOnlyList<KeyCardDto> GetActiveKeys(CoGuest guest) => _keysByStay.GetValueOrDefault(guest.StayId, Array.Empty<KeyCardDto>())
        .Where(x => x.Status is KeyStatus.Active or KeyStatus.Encoded)
        .ToArray();

    private async void OnCreateDepartureTask(object sender, RoutedEventArgs e)
    {
        if (_guest is null || _housekeepingWorkflowService is null || _contextAccessor?.Current is not { } context || _guest.RoomId == Guid.Empty) return;
        try
        {
            var result = await _housekeepingWorkflowService.CreateTaskAsync(new HkTaskCreateRequest(context.Property.PropertyId.Value, _guest.RoomId, HousekeepingTaskType.DepartureClean, RoomPriority.Normal, $"Chuẩn bị checkout · Stay {_guest.StayId:D}"), CancellationToken.None);
            if (result.IsFailure) { MessageBox.Show(result.Error?.Message ?? "Không thể tạo task hậu kiểm.", "Chuẩn bị Check-out", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            _housekeepingTaskId = result.Value;
            BuildStep4(); BuildStep6(); UpdateNav();
        }
        catch (Exception ex) { MessageBox.Show("Không thể tạo task hậu kiểm: " + ex.Message, "Chuẩn bị Check-out", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void OnReturnKeys(object sender, RoutedEventArgs e)
    {
        if (_guest is null || _keyCardService is null) return;
        try
        {
            foreach (var key in GetActiveKeys(_guest)) await _keyCardService.ReturnKeyAsync(key.KeyId, CancellationToken.None);
            _keysByStay[_guest.StayId] = await _keyCardService.GetKeysForStayAsync(_guest.StayId, CancellationToken.None);
            BuildStep4(); BuildStep6(); UpdateNav();
        }
        catch (Exception ex) { MessageBox.Show("Không thể thu hồi key-card thật: " + ex.Message, "Chuẩn bị Check-out", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    // STEP 5: Invoice & Payment
    private void BuildStep5()
    {
        Step5Left.Children.Clear(); Step5Right.Children.Clear();
        Step5Left.Children.Add(STitle("Hóa đơn và chứng từ"));
        Step5Left.Children.Add(Row("Nguồn dữ liệu", "Folio thật của hồ sơ đang chọn"));
        Step5Left.Children.Add(Row("Tổng phát sinh", $"{_guest?.TotalCharges:N0} {_guest?.Currency ?? _currency}"));
        Step5Left.Children.Add(Row("Đã thanh toán", $"{_guest?.TotalPayments:N0} {_guest?.Currency ?? _currency}"));
        Step5Left.Children.Add(new TextBlock { Text = "Việc chọn loại hóa đơn, thông tin người mua và lập nháp thực hiện tại luồng Thu ngân/Folio có lưu dữ liệu. Màn hình chuẩn bị không giữ các ô nhập tạm thời.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = C("#64748B"), Margin = new Thickness(0, 8, 0, 0) });
        Step5Right.Children.Add(STitle("Thanh toán"));
        Step5Right.Children.Add(Row("Còn phải thu", $"{_folioBalance:N0} {_guest?.Currency ?? _currency}"));
        Step5Right.Children.Add(Row("Đặt cọc", "Không có trường tách riêng trong dữ liệu Folio hiện tại"));
        Step5Right.Children.Add(Row("Trạng thái", _folioBalance == 0 ? "Đã cân bằng" : "Cần xử lý tại Thu ngân"));
        var folioButton = new Button { Content = "Mở Folio thật để xử lý", Style = FindResource("SecondaryButton") as Style, Height = 30, FontSize = 11, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, IsEnabled = _guest is not null };
        folioButton.Click += (_, _) => { if (_guest is null) return; new FolioWorkspaceWindow(_guest.StayId) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog(); OnRefresh(this, new RoutedEventArgs()); };
        Step5Right.Children.Add(folioButton);
    }

    // STEP 6: Confirm
    private void BuildStep6()
    {
        Step6Left.Children.Clear(); Step6Right.Children.Clear();
        if (_guest == null) return;
        Step6Left.Children.Add(STitle("Tóm tắt chuẩn bị"));
        Step6Left.Children.Add(Row("Khách", _guest.Name)); Step6Left.Children.Add(Row("Phòng", _guest.Room + " · " + _guest.RoomType));
        Step6Left.Children.Add(Row("Ngày đi", _guest.Departure + " · 12:00"));
        Step6Left.Children.Add(Row("Folio", $"Còn {_folioBalance:N0} {_guest.Currency}"));
        Step6Left.Children.Add(Row("Dịch vụ", _unpostedServices > 0 ? $"{_unpostedServices} chưa chốt" : "Đã chốt hết"));
        Step6Left.Children.Add(Row("Task hậu kiểm", _housekeepingTaskId.HasValue ? "Đã tạo DepartureClean" : "Chưa tạo"));
        Step6Left.Children.Add(Row("Key-card", _keyCardService is null ? "Chưa cấu hình service" : $"Còn {GetActiveKeys(_guest).Count:N0} thẻ hoạt động"));
        Step6Right.Children.Add(STitle("Kiểm tra sẵn sàng"));
        var folioLoaded = _guest?.Folio is { Length: > 0 } and not "—";
        var activeKeys = _guest is { } guest ? GetActiveKeys(guest) : Array.Empty<KeyCardDto>();
        var items = new[] { ("Đã chọn khách", _guest != null), ("Folio đã kiểm tra", folioLoaded), ("Không blocking balance", _folioBalance == 0), ("Dịch vụ đã chốt", _unpostedServices == 0), ("Đã tạo task HSK thật", _housekeepingTaskId.HasValue), ("Không còn key-card hoạt động", _keyCardService is not null && activeKeys.Count == 0) };
        var allOk = true; foreach (var (label, ok) in items) { Step6Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 12, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        Step6Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng Check-out" : "Cần kiểm tra thêm", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#A16207") } });
    }

    // Helpers
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns3((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (_guest is null) return;
        if (!_persistedMode || _guest.StayId == Guid.Empty || _guest.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ chưa có định danh/row version hợp lệ từ dữ liệu thật.", "Chuẩn bị Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var ready = _folioBalance == 0
            && _unpostedServices == 0
            && _housekeepingTaskId.HasValue
            && _keyCardService is not null
            && GetActiveKeys(_guest).Count == 0;
        if (!ready)
        {
            MessageBox.Show("Chưa đủ điều kiện Check-out theo dữ liệu thật: kiểm tra số dư, dịch vụ và các mục bàn giao.", "Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_receptionUiService is null)
        {
            MessageBox.Show("Chưa cấu hình được Stay service dữ liệu thật. Không tạo giao dịch giả.", "Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Xác nhận Check-out khách { _guest.Name } phòng { _guest.Room }?\n\nFolio đã cân và các mục bàn giao đã được xác nhận.",
            "Xác nhận Check-out",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        BtnConfirm.IsEnabled = false;
        try
        {
            var result = await _receptionUiService.CheckOutAsync(_guest.StayId, _guest.RowVersion, CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể ghi nhận Check-out.", "Check-out", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Đã Check-out thành công {result.Value!.StayNumber} · phòng {result.Value.RoomNumber}.", "Check-out", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể ghi nhận Check-out: {ex.Message}", "Check-out", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (IsVisible) BtnConfirm.IsEnabled = true;
        }
    }
    private void OnRefresh(object s, RoutedEventArgs e) => GoTo(1);
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record CoGuest(string Room, string Name, string Arrival, string Departure, string RoomType, string GuestCount, bool IsVip, string Folio, int Balance, Guid StayId = default, byte[]? RowVersion = null, int TotalCharges = 0, int TotalPayments = 0, IReadOnlyList<CheckoutServiceRow>? Services = null, Guid RoomId = default, string Currency = "VND") { public string VipDisplay => IsVip ? "★" : ""; public string BalanceDisplay => $"{Balance:N0}"; }
internal record CheckoutServiceRow(string Svc, string Dept, string Time, string Qty, string Amt, string Status);
internal static class CoExt { public static TextBlock WithRuns3(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
