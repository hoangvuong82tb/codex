using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Rate;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class ExtendStayWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step;
    private ExtGuest? _guest;
    private readonly List<ExtGuest> _allGuests = new();
    private int _guestPage;
    private int _guestPageSize = 20;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IRateRepository? _rateRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IReservationRepository? _reservationRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly StayAdjustmentApplicationService? _stayAdjustmentService;
    private readonly Guid? _initialStayId;
    private readonly List<RatePlan> _ratePlans = new();
    private RoomConflictResult? _roomConflict;
    private bool _ratePlansLoaded;
    private bool _availabilityCheckLoaded;
    private int _availabilityCheckVersion;
    private string? _availabilityError;
    private decimal? _extraRoomCharge;
    private RatePlan? _selectedRatePlan;
    private string _currency = "VND";
    private bool _isSubmitting;

    public ExtendStayWindow() : this(null)
    {
    }

    public ExtendStayWindow(Guid? initialStayId)
    {
        InitializeComponent();
        _initialStayId = initialStayId;
        var owner = System.Windows.Application.Current?.MainWindow;
        if (owner is not null && !ReferenceEquals(owner, this) && owner.IsVisible)
        {
            Owner = owner;
        }
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _rateRepository = services?.GetService<IRateRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _reservationRepository = services?.GetService<IReservationRepository>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _stayAdjustmentService = services?.GetService<StayAdjustmentApplicationService>();
        InitializeFilters();
        RefreshGuestPage(true);
        NewDeparturePicker.SelectedDate = null;
        GoTo(1);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_stayRepository is null || _contextAccessor?.Current is not { } context)
        {
            FooterStepLabel.Text = "Chưa cấu hình Stay service và property context dữ liệu thật.";
            return;
        }

        try
        {
            _currency = context.Property.CurrencyCode ?? "VND";
            var stays = await _stayRepository.GetCurrentInHouseAsync(context.Property.PropertyId.Value, CancellationToken.None);
            _ratePlans.Clear();
            string? rateLoadError = null;
            if (_rateRepository is not null)
            {
                try
                {
                    _ratePlans.AddRange(await _rateRepository.GetAllAsync(
                        context.Property.PropertyId.Value,
                        new RatePlanSearchFilter(context.Property.PropertyId.Value, RatePlanStatus.Active, null, null),
                        CancellationToken.None));
                }
                catch (Exception ex)
                {
                    rateLoadError = ex.Message;
                }
            }
            _ratePlansLoaded = true;
            SetRatePlanItems();
            _allGuests.Clear();
            foreach (var stay in stays)
            {
                HotelFolioDto? folio = null;
                if (_folioRepository is not null)
                {
                    try
                    {
                        folio = await _folioRepository.GetByStayAsync(
                            context.Property.PropertyId.Value,
                            stay.HotelStayId,
                            CancellationToken.None);
                    }
                    catch
                    {
                        // Keep the stay visible when the optional Folio query is unavailable.
                    }
                }

                _allGuests.Add(new ExtGuest(
                    stay.RoomNumber,
                    stay.GuestName,
                    stay.ScheduledArrivalDate.ToString("dd/MM"),
                    stay.ScheduledDepartureDate.ToString("dd/MM"),
                    stay.RoomTypeCode,
                    $"{stay.Adults + stay.Children}",
                    false,
                    folio?.FolioNumber ?? "Chưa có Folio thật",
                    folio?.OutstandingBalance ?? 0m,
                    folio is null ? "Chưa có Folio" : folio.OutstandingBalance > 0 ? "Còn phải thu" : "Đã cân bằng",
                    stay.HotelStayId,
                    stay.RowVersion,
                    stay.ScheduledDepartureDate,
                    stay.RoomId,
                    stay.RoomTypeId,
                    stay.ReservationId,
                    stay.Adults)
                {
                    PropertyId = context.Property.PropertyId.Value,
                    Children = stay.Children
                });
            }
            RefreshGuestPage(true);
            if (_initialStayId is Guid stayId)
            {
                var initialIndex = _allGuests.FindIndex(guest => guest.StayId == stayId);
                if (initialIndex >= 0)
                {
                    _guestPage = initialIndex / _guestPageSize;
                    RefreshGuestPage(false);
                    GuestGrid.SelectedItem = GuestGrid.Items
                        .OfType<ExtGuest>()
                        .FirstOrDefault(guest => guest.StayId == stayId);
                }
            }
            FooterStepLabel.Text = rateLoadError is null
                ? $"Dữ liệu thật · {_allGuests.Count:N0} khách đang lưu trú · {_ratePlans.Count:N0} rate plan"
                : $"Dữ liệu thật · {_allGuests.Count:N0} khách đang lưu trú · Không tải được rate plan thật: {rateLoadError}";
        }
        catch (Exception ex)
        {
            _allGuests.Clear();
            RefreshGuestPage(true);
            FooterStepLabel.Text = "Không thể tải danh sách gia hạn: " + ex.Message;
        }
    }


    private void InitializeFilters()
    {
        SetComboItems(GuestRoomTypeFilter, "Tất cả","Standard","Superior","Deluxe","Junior","Suite");
        SetComboItems(GuestVipFilter, "Tất cả","VIP","Không VIP");
        SetComboItems(ExtendReasonBox, "Khách yêu cầu","Công tác kéo dài","Chờ chuyến bay","Gia hạn đoàn","VIP yêu cầu","Khác");
        SetComboItems(RatePlanBox, "Đang tải rate plan thật…");
        SetComboItems(GuestPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang");
    }

    private void SetRatePlanItems()
    {
        RatePlanBox.ItemsSource = _ratePlans.Count == 0
            ? new[] { "Chưa có rate plan thật" }
            : _ratePlans.Select(p => $"{p.PlanCode} · {p.PlanName}").ToArray();
        RatePlanBox.SelectedIndex = _ratePlans.Count == 0 ? 0 : 0;
        SelectRatePlanForGuest();
    }

    private static void SetComboItems(ComboBox cb, params string[] values) { cb.ItemsSource = values; cb.SelectedIndex = 0; }

    private void GoTo(int step)
    {
        _step = Math.Clamp(step, 1, 4);
        BuildStepBar();
        Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
        Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed;
        UpdateStep2();
        UpdateStep3();
        UpdateStep4();
        UpdateNav();
        UpdateSummary();
    }

    private void BuildStepBar()
    {
        StepBar.Children.Clear();
        var steps = new[] { ("A","Chọn khách"), ("B","Điều chỉnh thời gian"), ("C","Kiểm tra giá"), ("D","Xác nhận") };
        for (int i = 0; i < steps.Length; i++) { var idx = i + 1; var active = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(14,8,14,8), Margin = new Thickness(2,6,2,6), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = active ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = active ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,0,0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 3) StepBar.Children.Add(Sep()); }
    }
    private static TextBlock Sep() => new() { Text = " › ", FontSize = 16, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,2,0) };

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 4) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        FooterStepLabel.Text = $"Bước {_step}/4 · {new[] { "Chọn khách","Điều chỉnh thời gian","Kiểm tra giá","Xác nhận" }[_step - 1]}";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear();
        var g = _guest; var oldDep = g?.Departure ?? "?"; var newDep = NewDeparturePicker.SelectedDate?.ToString("dd/MM") ?? "?";
        SummaryStrip.Children.Add(Strip("Khách", g?.Name ?? "Chưa chọn", g != null));
        SummaryStrip.Children.Add(Sep()); SummaryStrip.Children.Add(Strip("Phòng", g?.Room ?? "?", g != null));
        SummaryStrip.Children.Add(Sep()); SummaryStrip.Children.Add(Strip("Ngày đi", $"{oldDep} → {newDep}", NewDeparturePicker.SelectedDate != null));
        SummaryStrip.Children.Add(Sep()); SummaryStrip.Children.Add(Strip("Đêm thêm", NewDeparturePicker.SelectedDate != null && g != null ? $"{(NewDeparturePicker.SelectedDate.Value - DateTime.ParseExact(g.Departure, "dd/MM", null)).Days}" : "—", NewDeparturePicker.SelectedDate != null));
        SummaryStrip.Children.Add(Sep()); SummaryStrip.Children.Add(new Border { Background = g != null ? Cb("#DCFCE7") : Cb("#F3F4F6"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8,2,8,2), Child = new TextBlock { Text = g != null ? "Sẵn sàng" : "Chưa sẵn sàng", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = g != null ? C("#16A34A") : C("#94A3B8") } });
    }
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4,0,4,0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }

    // === Guest filtering ===
    private List<ExtGuest> GetFiltered()
    {
        var f = _allGuests.AsEnumerable();
        var kw = GuestSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Room.Contains(kw) || g.Folio.ToLower().Contains(kw));
        var roomKw = GuestRoomFilterBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(roomKw)) f = f.Where(g => g.Room.Contains(roomKw));
        var type = GuestRoomTypeFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(type) && type != "Tất cả") f = f.Where(g => g.RoomType == type);
        var vip = GuestVipFilter.SelectedItem?.ToString(); if (vip == "VIP") f = f.Where(g => g.IsVip); else if (vip == "Không VIP") f = f.Where(g => !g.IsVip);
        return f.ToList();
    }

    private void RefreshGuestPage(bool resetPage)
    {
        if (resetPage) _guestPage = 0;
        var filtered = GetFiltered();
        var tp = Math.Max(1, (filtered.Count + _guestPageSize - 1) / _guestPageSize);
        if (_guestPage >= tp) _guestPage = tp - 1;
        var paged = filtered.Skip(_guestPage * _guestPageSize).Take(_guestPageSize).ToList();
        GuestGrid.ItemsSource = paged;
        GuestPagerSummary.Text = $"Hiển thị {_guestPage * _guestPageSize + 1}–{Math.Min((_guestPage + 1) * _guestPageSize, filtered.Count)} trên tổng {filtered.Count} khách";
        GuestPrevBtn.IsEnabled = _guestPage > 0; GuestNextBtn.IsEnabled = _guestPage < tp - 1;
        BuildPageButtons(tp);
    }

    private void BuildPageButtons(int totalPages)
    {
        GuestPageBtns.Children.Clear();
        for (int i = 0; i < totalPages && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2,0,2,0), Background = i == _guestPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _guestPage ? Cb("#FFFFFF") : Cb("#475569"), BorderThickness = new Thickness(i == _guestPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _guestPage = idx; RefreshGuestPage(false); }; nb.Template = ((Button)GuestPrevBtn).Template; GuestPageBtns.Children.Add(nb); }
    }

    private void OnGuestSelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not ExtGuest g) return;
        _guest = g; NewDeparturePicker.SelectedDate = null;
        GuestEmptyState.Visibility = Visibility.Collapsed; GuestDetailPanel.Visibility = Visibility.Visible;
        GuestDetailName.Text = g.Name; GuestDetailProfile.Text = g.Folio;
        GuestDetailRoom.Text = $"Phòng: {g.Room} · {g.RoomType}"; GuestDetailDates.Text = $"Ngày: {g.Arrival} → {g.Departure}";
        GuestDetailFolio.Text = $"Folio: {g.Folio}"; GuestDetailVip.Text = g.IsVip ? "★ VIP · Gold" : "Khách thường";
        GuestDetailCount.Text = $"Số khách: {g.Adults} người lớn · {g.Children} trẻ em";
        GuestDetailBalance.Text = $"Giá phòng: {(_ratePlansLoaded ? "Đang tra rate plan thật" : "Đang tải dữ liệu")} · Balance Folio: {g.Balance:N0} {_currency}";
        GuestCheck1.Text = "· Chưa có dữ liệu booking tiếp theo";
        GuestCheck2.Text = g.Balance > 0 ? $"· Có công nợ Folio: {g.Balance:N0} {_currency}" : $"· Folio: {g.FolioStatus}";
        GuestCheck3.Text = "· Chưa có dữ liệu Housekeeping liên quan";
        GuestCheck2.Foreground = g.Balance > 0 ? C("#92400E") : C("#16A34A");
        UpdateSummary(); UpdateStep2(); UpdateStep3(); UpdateStep4();
    }

    private void OnApplyGuestFilters(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilters(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestRoomFilterBox.Text = ""; GuestRoomTypeFilter.SelectedIndex = 0; GuestVipFilter.SelectedIndex = 0; GuestDepartureFilter.SelectedDate = null; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }

    // === Step 2 ===
    private void UpdateStep2()
    {
        if (_guest == null) return;
        CurrentDepartureLabel.Text = _guest.Departure;
        ExtraNightsLabel.Text = NewDeparturePicker.SelectedDate != null ? $"Số đêm gia hạn: {(NewDeparturePicker.SelectedDate.Value - DateTime.ParseExact(_guest.Departure, "dd/MM", null)).Days}" : "Chọn ngày đi mới";
        if (NewDeparturePicker.SelectedDate is null)
        {
            AvailabilityBadge.Background = Cb("#F1F5F9");
            AvailabilityText.Text = "Chưa chọn ngày đi mới";
            AvailabilityText.Foreground = C("#64748B");
        }
        else if (!_availabilityCheckLoaded)
        {
            AvailabilityBadge.Background = Cb("#FFF7ED");
            AvailabilityText.Text = "⏳ Đang kiểm tra availability thật…";
            AvailabilityText.Foreground = C("#A16207");
        }
        else if (_roomConflict?.HasConflict == true)
        {
            AvailabilityBadge.Background = Cb("#FEE2E2");
            AvailabilityText.Text = "✕ Phòng đã có đặt phòng trùng thời gian";
            AvailabilityText.Foreground = C("#DC2626");
        }
        else if (_roomConflict is not null)
        {
            AvailabilityBadge.Background = Cb("#DCFCE7");
            AvailabilityText.Text = "✓ Phòng còn khả dụng theo dữ liệu đặt phòng";
            AvailabilityText.Foreground = C("#16A34A");
        }
        else
        {
            AvailabilityBadge.Background = Cb("#FEE2E2");
            AvailabilityText.Text = _availabilityError is null ? "⚠ Chưa có dịch vụ kiểm tra availability thật" : $"✕ Không kiểm tra được availability: {_availabilityError}";
            AvailabilityText.Foreground = C("#DC2626");
        }
        BookingNextLabel.Text = "Booking tiếp: Chưa có dữ liệu";
        HkLabel.Text = "Housekeeping: Chưa có dữ liệu"; MaintenanceLabel.Text = "Maintenance: Chưa có dữ liệu";
    }
    private async void OnNewDepartureChanged(object s, SelectionChangedEventArgs e)
    {
        UpdateStep2(); UpdateStep3(); UpdateStep4(); UpdateSummary();
        await RefreshExtensionChecksAsync();
    }

    private void OnRatePlanChanged(object s, SelectionChangedEventArgs e)
    {
        SelectRatePlanForGuest();
        UpdateStep3(); UpdateStep4();
    }

    private void SelectRatePlanForGuest()
    {
        _selectedRatePlan = null;
        if (_guest is null || _ratePlans.Count == 0) return;
        var date = _guest.DepartureDate ?? DateOnly.FromDateTime(DateTime.Today);
        _selectedRatePlan = _ratePlans
            .Where(p => (p.RoomTypeId is null || p.RoomTypeId == _guest.RoomTypeId) &&
                        date >= DateOnly.FromDateTime(p.EffectiveFrom) && date <= DateOnly.FromDateTime(p.EffectiveTo))
            .OrderByDescending(p => p.RoomTypeId == _guest.RoomTypeId)
            .ThenBy(p => p.EffectiveFrom)
            .FirstOrDefault();
        if (_selectedRatePlan is not null)
        {
            var index = _ratePlans.IndexOf(_selectedRatePlan);
            if (index >= 0 && RatePlanBox.SelectedIndex != index) RatePlanBox.SelectedIndex = index;
        }
    }

    private async Task RefreshExtensionChecksAsync()
    {
        var guest = _guest;
        var newDeparture = NewDeparturePicker.SelectedDate;
        var version = ++_availabilityCheckVersion;
        _availabilityCheckLoaded = false;
        _roomConflict = null;
        _availabilityError = null;
        _extraRoomCharge = null;
        _selectedRatePlan = null;
        if (guest is null || newDeparture is null)
        {
            UpdateStep2(); UpdateStep3(); UpdateStep4();
            return;
        }

        SelectRatePlanForGuest();
        if (_selectedRatePlan is not null && guest.DepartureDate is { } oldDeparture)
        {
            var oldDate = oldDeparture;
            var targetDate = DateOnly.FromDateTime(newDeparture.Value);
            var nights = targetDate.DayNumber - oldDate.DayNumber;
            if (nights > 0)
            {
                var adults = Math.Max(1, guest.Adults);
                _extraRoomCharge = Enumerable.Range(0, nights)
                    .Select(offset => _selectedRatePlan.CalculatePrice(oldDate.AddDays(offset), adults, 1, 0, 0).finalPrice)
                    .Sum();
            }
        }

        if (_reservationRepository is not null && guest.RoomId != Guid.Empty && guest.DepartureDate is { } departure)
        {
            var targetDate = DateOnly.FromDateTime(newDeparture.Value);
            if (targetDate > departure)
            {
                try
                {
                    _roomConflict = await _reservationRepository.CheckAsync(
                        guest.PropertyId,
                        guest.RoomId,
                        departure,
                        targetDate,
                        guest.ReservationId,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _availabilityError = ex.Message;
                }
            }
        }

        if (version != _availabilityCheckVersion || !ReferenceEquals(guest, _guest)) return;
        _availabilityCheckLoaded = true;
        UpdateStep2(); UpdateStep3(); UpdateStep4(); UpdateSummary();
    }

    // === Step 3 ===
    private void UpdateStep3()
    {
        if (_guest == null) return;
        CurrentPriceLabel.Text = $"Giá phòng hiện tại: {(_selectedRatePlan is null ? "Chưa có rate plan phù hợp" : $"{_selectedRatePlan.PlanCode} · {_selectedRatePlan.BaseRate:N0} {_currency}")}";
        ExtraNightsPriceLabel.Text = NewDeparturePicker.SelectedDate != null ? $"Số đêm thêm: {(NewDeparturePicker.SelectedDate.Value - DateTime.ParseExact(_guest.Departure, "dd/MM", null)).Days}" : "Chưa chọn ngày";
        NewPriceLabel.Text = _extraRoomCharge is { } charge ? $"Tiền phòng thêm: {charge:N0} {_currency}" : "Tiền phòng thêm: Chưa đủ rate plan thật";
        ExtraChargeLabel.Text = $"Rate: {_selectedRatePlan?.PlanCode ?? "Chưa có"}";
        RoomChargeLabel.Text = _extraRoomCharge is { } roomCharge ? $"Tiền phòng thêm: {roomCharge:N0} {_currency}" : "Tiền phòng thêm: Chưa tính được";
        TaxLabel.Text = "Thuế/phí: Chưa có chính sách thuế thật trong contract gia hạn";
        DepositLabel.Text = $"Đặt cọc hiện có: Theo Folio ({_guest.FolioStatus})";
        BalanceLabel.Text = $"Balance hiện tại: {_guest.Balance:N0} {_currency}";
        TotalLabel.Text = _extraRoomCharge is { } total ? $"Tổng phát sinh: {total:N0} {_currency} (chưa gồm thuế/phí)" : "Tổng phát sinh: Chưa thể tính";
        PriceBadge.Background = _extraRoomCharge is not null ? Cb("#DCFCE7") : Cb("#FEE2E2");
        PriceBadgeText.Text = _extraRoomCharge is not null ? "Đã tính theo rate plan thật" : "Cần rate plan thật";
        PriceBadgeText.Foreground = _extraRoomCharge is not null ? C("#16A34A") : C("#DC2626");
    }

    // === Step 4 ===
    private void UpdateStep4()
    {
        if (_guest == null) return;
        ConfirmOldDep.Text = $"Ngày đi cũ: {_guest.Departure}";
        ConfirmNewDep.Text = $"Ngày đi mới: {NewDeparturePicker.SelectedDate?.ToString("dd/MM") ?? "Chưa chọn"}";
        ConfirmGuest.Text = $"Khách: {_guest.Name}"; ConfirmRoom.Text = $"Phòng: {_guest.Room} · {_guest.RoomType}";
        var nd = NewDeparturePicker.SelectedDate; var extraDays = nd != null ? (nd.Value - DateTime.ParseExact(_guest.Departure, "dd/MM", null)).Days : 0;
        ConfirmNights.Text = $"Số đêm thêm: {(extraDays > 0 ? extraDays.ToString() : "—")}";
        ConfirmRate.Text = $"Rate: {_selectedRatePlan?.PlanCode ?? "Chưa có"}";
        ConfirmTotal.Text = _extraRoomCharge is { } total ? $"Tổng phát sinh: {total:N0} {_currency} (chưa gồm thuế/phí)" : "Tổng phát sinh: Chưa thể tính — thiếu rate plan thật";

        var hasGuest = _guest != null; var hasDate = nd != null; var noConflict = _roomConflict is { HasConflict: false };
        CheckGuest.Text = (hasGuest ? "✓ " : "○ ") + "Đã chọn khách"; CheckGuest.Foreground = hasGuest ? C("#16A34A") : C("#DC2626");
        CheckDate.Text = (hasDate ? "✓ " : "○ ") + "Đã chọn ngày đi mới"; CheckDate.Foreground = hasDate ? C("#16A34A") : C("#DC2626");
        CheckAvail.Text = _roomConflict is { HasConflict: false } ? "✓ Không có đặt phòng trùng" : _roomConflict is { HasConflict: true } ? "✕ Có đặt phòng trùng" : "○ Chưa kiểm tra availability";
        CheckAvail.Foreground = _roomConflict is null ? C("#DC2626") : _roomConflict.HasConflict ? C("#DC2626") : C("#16A34A");
        CheckPrice.Text = _extraRoomCharge is not null ? "✓ Đã tính theo rate plan" : "○ Chưa có rate plan phù hợp";
        CheckPrice.Foreground = _extraRoomCharge is not null ? C("#16A34A") : C("#DC2626");
        CheckBalance.Text = _guest is null
            ? "○ Chưa chọn khách"
            : _guest.Balance <= 0m
                ? "✓ Folio hiện tại đã cân bằng"
                : $"⚠ Folio còn phải thu {_guest.Balance:N0} {_currency}";
        CheckBalance.Foreground = _guest is null ? C("#DC2626") : _guest.Balance <= 0m ? C("#16A34A") : C("#A16207");
        var allOk = hasGuest && hasDate && noConflict && _extraRoomCharge is not null;
        ConfirmReadiness.Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2");
        ConfirmReadinessText.Text = allOk ? "Sẵn sàng gia hạn" : "Còn mục chưa hoàn thành";
        ConfirmReadinessText.Foreground = allOk ? C("#16A34A") : C("#DC2626");
    }

    private void OnNext(object s, RoutedEventArgs e) { if (_step < 4) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (_isSubmitting) return;
        if (_guest == null) return;
        if (_stayAdjustmentService is null || _guest.StayId == Guid.Empty || _guest.RoomId == Guid.Empty || _guest.RowVersion is not { Length: 8 } || NewDeparturePicker.SelectedDate is not { } newDeparture || _roomConflict is not { HasConflict: false } || _extraRoomCharge is null)
        {
            MessageBox.Show("Chưa đủ dữ liệu thật để gia hạn: cần stay, row-version, ngày đi mới, rate plan và kiểm tra không trùng phòng.", "Gia hạn lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isSubmitting = true;
        BtnConfirm.IsEnabled = false;
        try
        {
            var result = await _stayAdjustmentService.AdjustAsync(
                new AdjustStayCommand(
                    _guest.StayId,
                    _guest.RowVersion,
                    _guest.RoomId,
                    DateOnly.FromDateTime(newDeparture),
                    _extraRoomCharge.Value,
                    "Gia hạn lưu trú"),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể gia hạn lưu trú.", "Gia hạn lưu trú", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saved = result.Value!;
            var folioMessage = saved.FolioEntry is null
                ? "Không phát sinh khoản Folio."
                : $"Đã ghi Folio {saved.Folio?.FolioNumber ?? "Folio"}: {saved.FolioEntry.Amount:N0} {_currency}.";
            MessageBox.Show($"Đã gia hạn stay {saved.Stay.StayNumber} đến {saved.Stay.ScheduledDepartureDate:dd/MM/yyyy}.\n{folioMessage}", "Gia hạn lưu trú", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể gia hạn lưu trú: " + ex.Message, "Gia hạn lưu trú", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isSubmitting = false;
            if (IsLoaded) BtnConfirm.IsEnabled = true;
        }
    }
    private async void OnRefresh(object s, RoutedEventArgs e) { await Task.CompletedTask; OnLoadedAsync(this, new RoutedEventArgs()); }
    private void OnClose(object s, RoutedEventArgs e) => Close();

    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record ExtGuest(string Room, string Name, string Arrival, string Departure, string RoomType, string GuestCount, bool IsVip, string Folio, decimal Balance, string FolioStatus, Guid StayId = default, byte[]? RowVersion = null, DateOnly? DepartureDate = null, Guid RoomId = default, Guid? RoomTypeId = null, Guid? ReservationId = null, int Adults = 1)
{
    public Guid PropertyId { get; init; }
    public int Children { get; init; }
    public string VipDisplay => IsVip ? "★" : "";
}
