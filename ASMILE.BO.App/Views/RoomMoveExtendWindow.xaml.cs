using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Rate;
using ASMILE.BO.Shared.Foundation;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class RoomMoveExtendWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private const int HandoverTaskCount = 18;
    private int _step;
    private MxGuest? _guest;
    private MxRoom? _room;
    private bool _step3Built;
    private int _guestPage, _roomPage;
    private int _guestPageSize = 20, _roomPageSize = 20;
    private readonly List<MxGuest> _allGuests;
    private readonly List<MxRoom> _allRooms;
    private readonly HashSet<string> _handover = new(StringComparer.Ordinal);
    private DateTime? _newDepartureDate;
    private bool _isInitializingAdjustment = true;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelRoomMasterDataRepository? _roomRepository;
    private readonly IRateRepository? _rateRepository;
    private readonly StayAdjustmentApplicationService? _stayAdjustmentService;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly Guid? _initialStayId;
    private readonly List<RatePlan> _ratePlans = new();
    private string _currency = "VND";
    private string? _rateLoadError;
    private bool _isSubmitting;

    public RoomMoveExtendWindow() : this(null)
    {
    }

    public RoomMoveExtendWindow(Guid? initialStayId)
    {
        InitializeComponent();
        _initialStayId = initialStayId;
        _allGuests = new List<MxGuest>();
        _allRooms = new List<MxRoom>();
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _roomRepository = services?.GetService<IHotelRoomMasterDataRepository>();
        _rateRepository = services?.GetService<IRateRepository>();
        _stayAdjustmentService = services?.GetService<StayAdjustmentApplicationService>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        InitFilters();
        RefreshGuestPage(true);
        GoTo(1);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_stayRepository is null || _roomRepository is null || _contextAccessor?.Current is not { } context)
        {
            FooterStepLabel.Text = "Chưa cấu hình Stay/Room service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var propertyId = context.Property.PropertyId.Value;
            _currency = context.Property.CurrencyCode ?? "VND";
            _ratePlans.Clear();
            _rateLoadError = null;
            if (_rateRepository is not null)
            {
                try
                {
                    _ratePlans.AddRange(await _rateRepository.GetAllAsync(
                        propertyId,
                        new RatePlanSearchFilter(propertyId, RatePlanStatus.Active, null, null),
                        CancellationToken.None));
                }
                catch (Exception ex)
                {
                    _rateLoadError = ex.Message;
                }
            }
            var stays = await _stayRepository.GetCurrentInHouseAsync(propertyId, CancellationToken.None);
            var rooms = await _roomRepository.SearchRoomsAsync(
                propertyId, null, null, null, null, HotelRoomOperationalStatus.Available, true,
                new PageRequest(1, 500), "number", true, CancellationToken.None);

            _allGuests.Clear();
            _allGuests.AddRange(stays.Select(stay => new MxGuest(
                stay.RoomNumber,
                stay.GuestName,
                stay.ScheduledArrivalDate.ToDateTime(TimeOnly.MinValue),
                stay.ScheduledDepartureDate.ToDateTime(TimeOnly.MinValue),
                stay.RoomTypeCode,
                stay.Adults + stay.Children,
                false,
                stay.StayNumber,
                0m,
                "Đang lưu trú",
                stay.HotelStayId,
                stay.RoomId,
                stay.RoomTypeId,
                stay.RowVersion)
            {
                Adults = stay.Adults,
                Children = stay.Children
            }));

            _allRooms.Clear();
            _allRooms.AddRange(rooms.Items.Select(room => new MxRoom(
                room.RoomNumber,
                room.HotelRoomTypeCode,
                room.Floor ?? "—",
                "—",
                room.OperationalStatus == HotelRoomOperationalStatus.Available ? "Sẵn sàng" : room.OperationalStatus.ToString(),
                "—",
                GetRateForRoom(room.HotelRoomTypeId, DateOnly.FromDateTime(DateTime.Today), 1) ?? 0m,
                $"Có thể chọn",
                room.HotelRoomId,
                room.HotelRoomTypeId,
                room.RowVersion)));

            RefreshGuestPage(true);
            RefreshRoomPage(true);
            if (_initialStayId is Guid stayId)
            {
                var initialIndex = _allGuests.FindIndex(guest => guest.StayId == stayId);
                if (initialIndex >= 0)
                {
                    _guestPage = initialIndex / _guestPageSize;
                    RefreshGuestPage(false);
                    GuestGrid.SelectedItem = GuestGrid.Items
                        .OfType<MxGuest>()
                        .FirstOrDefault(guest => guest.StayId == stayId);
                }
            }
            FooterStepLabel.Text = _rateLoadError is null
                ? $"Dữ liệu thật · {_allGuests.Count:N0} stay · {_allRooms.Count:N0} phòng khả dụng · {_ratePlans.Count:N0} rate plan"
                : $"Dữ liệu thật · {_allGuests.Count:N0} stay · Không tải được rate plan thật: {_rateLoadError}";
        }
        catch (Exception ex)
        {
            _allGuests.Clear();
            _allRooms.Clear();
            RefreshGuestPage(true);
            RefreshRoomPage(true);
            FooterStepLabel.Text = "Không thể tải dữ liệu điều chỉnh lưu trú thật: " + ex.Message;
        }
    }

    private void InitFilters()
    {
        Set(GuestRoomTypeFilter, "Tất cả","Standard","Superior","Deluxe","Junior","Suite");
        Set(GuestVipFilter, "Tất cả","VIP","Không VIP");
        Set(GuestPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang");
        Set(RoomPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang");
        Set(EffectiveModeBox, "Đổi ngay", "Cuối ngày", "Chọn ngày giờ khác");
        Set(ReasonBox, "Khách yêu cầu", "Phòng có sự cố", "Tiếng ồn", "Thiết bị hỏng", "Nâng hạng", "Hạ hạng", "Yêu cầu VIP", "Điều chỉnh vận hành", "Khác");
        Set(ExtendReasonBox, "Khách yêu cầu", "Công tác kéo dài", "Chờ chuyến bay", "Yêu cầu VIP", "Điều chỉnh vận hành", "Khác");
        Set(PricePolicyBox, "Áp dụng giá mới", "Giữ nguyên giá cũ", "Giá thỏa thuận", "Miễn chênh lệch");
        EffectiveDatePicker.SelectedDate = DateTime.Today;
        _isInitializingAdjustment = false;
    }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 6); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; Step5Content.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed; Step6Content.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn khách"),("B","Chọn thời gian"),("C","Chọn phòng"),("D","Giá & điều chỉnh"),("E","Bàn giao"),("F","Xác nhận") };
        for (int i = 0; i < 6; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 5) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep()
    {
        if (_step == 2) BuildStep2(); else if (_step == 3) BuildStep3();
        else if (_step == 4) BuildStep4(); else if (_step == 6) BuildStep6();
    }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        FooterStepLabel.Text = $"Bước {_step}/6 · {new[] { "Chọn khách","Chọn thời gian","Chọn phòng","Giá & điều chỉnh","Bàn giao","Xác nhận" }[_step - 1]}";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var g = _guest; var r = _room;
        SummaryStrip.Children.Add(Strip("Khách", g?.Name ?? "Chưa chọn", g != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", g != null ? (r != null ? $"{g.Room} → {r.Room}" : $"{g.Room} → ?") : "?", g != null)); SummaryStrip.Children.Add(Sep());
        var nd = _newDepartureDate; var oldD = g?.Departure ?? "?";
        SummaryStrip.Children.Add(Strip("Ngày đi", nd != null ? $"{oldD} → {nd:dd/MM}" : $"{oldD} → ?", nd != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đêm thêm", nd != null && g != null ? $"{(nd.Value - g.DepartureDate).Days}" : "—", nd != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(new Border { Background = g != null ? Cb("#DCFCE7") : Cb("#F3F4F6"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = g != null ? "Sẵn sàng" : "Chưa sẵn sàng", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = g != null ? C("#16A34A") : C("#94A3B8") } });
    }

    // === STEP 1: Guest ===
    private List<MxGuest> GetFiltered() { var f = _allGuests.AsEnumerable(); var kw = GuestSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Room.Contains(kw) || g.Folio.ToLower().Contains(kw)); var rk = GuestRoomFilterBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(rk)) f = f.Where(g => g.Room.Contains(rk)); var t = GuestRoomTypeFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(t) && t != "Tất cả") f = f.Where(g => g.RoomType == t); var v = GuestVipFilter.SelectedItem?.ToString(); if (v == "VIP") f = f.Where(g => g.IsVip); else if (v == "Không VIP") f = f.Where(g => !g.IsVip); return f.ToList(); }

    private void RefreshGuestPage(bool reset)
    {
        if (reset) _guestPage = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _guestPageSize - 1) / _guestPageSize); if (_guestPage >= tp) _guestPage = tp - 1;
        GuestGrid.ItemsSource = f.Skip(_guestPage * _guestPageSize).Take(_guestPageSize).ToList();
        GuestPagerSummary.Text = $"Hiển thị {_guestPage * _guestPageSize + 1}–{Math.Min((_guestPage + 1) * _guestPageSize, f.Count)} trên tổng {f.Count} khách";
        GuestPrevBtn.IsEnabled = _guestPage > 0; GuestNextBtn.IsEnabled = _guestPage < tp - 1; BuildGuestPageBtns(tp);
    }
    private void BuildGuestPageBtns(int tp) { GuestPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _guestPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _guestPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _guestPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _guestPage = idx; RefreshGuestPage(false); }; nb.Template = GuestPrevBtn.Template; GuestPageBtns.Children.Add(nb); } }

    private void OnGuestSelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not MxGuest g) return; _guest = g; _room = null; _newDepartureDate = null; _step3Built = false;
        ResetAdjustmentInputs();
        GuestEmptyState.Visibility = Visibility.Collapsed; GuestDetailPanel.Visibility = Visibility.Visible;
        GuestDetailName.Text = g.Name; GuestDetailMeta.Text = g.Folio.Replace("FOL-", "STY-");
        GuestDetailRoom.Text = $"Phòng: {g.Room} · {g.RoomType}"; GuestDetailDates.Text = $"Ngày: {g.Arrival} → {g.Departure}";
        GuestDetailFolio.Text = $"Folio: {g.Folio} · {g.FolioStatus}"; GuestDetailVip.Text = g.IsVip ? "★ VIP" : "Khách thường";
        GuestDetailCount.Text = $"Số khách: {g.Adults} người lớn · {g.Children} trẻ em"; GuestDetailBalance.Text = $"Balance: {g.Balance:N0} VND";
        GuestIssue1.Text = "· Minibar chưa kiểm tra · 2 thẻ cần thu hồi"; GuestIssue2.Text = "· Hành lý cần chuyển · Yêu cầu nhà buồng";
        UpdateSummary();
    }
    private void OnApplyGuestFilters(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilters(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestRoomFilterBox.Text = ""; GuestRoomTypeFilter.SelectedIndex = 0; GuestVipFilter.SelectedIndex = 0; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }

    // === STEP 2: Time ===
    private void BuildStep2()
    {
        if (_guest == null) return;
        Step2Left.Children.Clear(); Step2Right.Children.Clear();
        Step2Left.Children.Add(STitle("Thời gian lưu trú"));
        Step2Left.Children.Add(Row("Ngày đến", _guest.Arrival)); Step2Left.Children.Add(Row("Ngày đi hiện tại", _guest.Departure));
        Step2Left.Children.Add(new TextBlock { Text = "Ngày đi mới", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 8, 0, 0) });
        var dp = new DatePicker { Style = FindResource("ModernDatePicker") as Style, SelectedDate = _newDepartureDate, Width = 180, HorizontalAlignment = HorizontalAlignment.Left }; dp.SelectedDateChanged += (_, _) => { _newDepartureDate = dp.SelectedDate; BuildStep2(); BuildStep3(); BuildStep4(); UpdateSummary(); }; Step2Left.Children.Add(dp);
        var nd = dp.SelectedDate; var extra = nd != null ? (nd.Value - _guest.DepartureDate).Days : 0;
        Step2Left.Children.Add(Row("Số đêm thêm", nd != null ? extra.ToString() : "—"));
        Step2Left.Children.Add(new TextBlock { Text = "Thời điểm đổi phòng", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#0F172A"), Margin = new Thickness(0, 8, 0, 4) });
        var cb = new ComboBox { Style = FindResource("ModernComboBox") as Style, ItemsSource = new[] { "Đổi ngay", "Cuối ngày", "Chọn ngày khác" }, SelectedIndex = 0, Width = 180, HorizontalAlignment = HorizontalAlignment.Left }; Step2Left.Children.Add(cb);
        Step2Left.Children.Add(Row("Ngày hiệu lực", DateTime.Today.ToString("dd/MM")));
        Step2Right.Children.Add(STitle("Kiểm tra lịch")); Step2Right.Children.Add(TimelineItem("Hôm nay", DateTime.Today.ToString("dd/MM"), C("#2563EB")));
        Step2Right.Children.Add(TimelineItem("Ngày đi cũ", _guest.Departure, C("#64748B"))); if (nd != null) Step2Right.Children.Add(TimelineItem("Ngày đi mới", nd.Value.ToString("dd/MM"), C("#16A34A")));
        Step2Right.Children.Add(new Border
        {
            Background = Cb("#EFF6FF"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 8, 0, 0),
            Child = new TextBlock
            {
                Text = nd is null ? "Chọn ngày đi mới để kiểm tra availability thật khi xác nhận." : "Availability sẽ được xác nhận lại bằng command thật khi lưu.",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = C("#1D4ED8")
            }
        });
    }

    // === STEP 3: Room ===
    private void BuildStep3()
    {
        if (_guest == null) return;
        var nd = _newDepartureDate ?? _guest.DepartureDate;
        Step3Filter.Children.Clear(); var tf = new WrapPanel();
        var tc = new ComboBox { Style = FindResource("ModernComboBox") as Style, Width = 110, Margin = new Thickness(0, 0, 8, 0) }; Set(tc, "Tất cả", "Standard", "Superior", "Deluxe", "Junior", "Suite"); tc.SelectionChanged += (_, _) => RefreshRoomPage(true); tf.Children.Add(new StackPanel { Children = { new TextBlock { Text = "Hạng", Style = FindResource("FieldLabel") as Style }, tc } });
        var fc = new ComboBox { Style = FindResource("ModernComboBox") as Style, Width = 80, Margin = new Thickness(0, 0, 8, 0) }; Set(fc, "Tất cả", "1", "2", "3", "4", "5"); fc.SelectionChanged += (_, _) => RefreshRoomPage(true); tf.Children.Add(new StackPanel { Children = { new TextBlock { Text = "Tầng", Style = FindResource("FieldLabel") as Style }, fc } });
        var sc = new ComboBox { Style = FindResource("ModernComboBox") as Style, Width = 120, Margin = new Thickness(0, 0, 8, 0) }; Set(sc, "Tất cả", "Sẵn sàng", "Sạch", "Đang dọn"); sc.SelectionChanged += (_, _) => RefreshRoomPage(true); tf.Children.Add(new StackPanel { Children = { new TextBlock { Text = "Trạng thái", Style = FindResource("FieldLabel") as Style }, sc } });
        Step3Filter.Children.Add(tf);
        if (!_step3Built) { RoomGrid.Columns.Clear(); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Phòng", Binding = new System.Windows.Data.Binding("Room"), Width = 55 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Hạng", Binding = new System.Windows.Data.Binding("RoomType"), Width = 75 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Tầng", Binding = new System.Windows.Data.Binding("Floor"), Width = 45 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Giường", Binding = new System.Windows.Data.Binding("Bed"), Width = 55 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Trạng thái", Binding = new System.Windows.Data.Binding("Status"), Width = 85 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "NB", Binding = new System.Windows.Data.Binding("HkStatus"), Width = 55 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Giá", Binding = new System.Windows.Data.Binding("PriceDisplay"), Width = 80 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Khả dụng", Binding = new System.Windows.Data.Binding("Availability"), Width = 75 }); _step3Built = true; }
        RefreshRoomPage(true);
        RoomPreviewPanel.Children.Clear(); RoomPreviewPanel.Children.Add(STitle("Phòng đang xem"));
        if (_room == null) RoomPreviewPanel.Children.Add(new TextBlock { Text = "Click dòng để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8") });
        else { RoomPreviewPanel.Children.Add(Row("Phòng", _room.Room + " · " + _room.RoomType)); RoomPreviewPanel.Children.Add(Row("Trạng thái", _room.Status + " / " + _room.HkStatus)); RoomPreviewPanel.Children.Add(Row("Giá", _room.PriceDisplay)); RoomPreviewPanel.Children.Add(Row("Khả dụng", _room.Availability)); var blocked = _room.Status is "Khóa" or "OOO"; RoomPreviewPanel.Children.Add(new Button { Content = blocked ? "Không khả dụng" : "Chọn phòng này", Style = FindResource(blocked ? "SecondaryButton" : "PrimaryButton") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 8, 0, 0), IsEnabled = !blocked }); }
    }
    private List<MxRoom> GetFilteredRooms() => _allRooms
        .Where(room => room.IsSelectable)
        .ToList();
    private void RefreshRoomPage(bool reset) { if (reset) _roomPage = 0; var f = GetFilteredRooms(); var tp = Math.Max(1, (f.Count + _roomPageSize - 1) / _roomPageSize); if (_roomPage >= tp) _roomPage = tp - 1; RoomGrid.ItemsSource = f.Skip(_roomPage * _roomPageSize).Take(_roomPageSize).ToList(); RoomPagerSummary.Text = $"Hiển thị {_roomPage * _roomPageSize + 1}–{Math.Min((_roomPage + 1) * _roomPageSize, f.Count)} trên tổng {f.Count} phòng"; RoomPrevBtn.IsEnabled = _roomPage > 0; RoomNextBtn.IsEnabled = _roomPage < tp - 1; BuildRoomPageBtns(tp); }
    private void BuildRoomPageBtns(int tp) { RoomPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _roomPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _roomPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _roomPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _roomPage = idx; RefreshRoomPage(false); }; nb.Template = RoomPrevBtn.Template; RoomPageBtns.Children.Add(nb); } }
    private void OnRoomPreviewChanged(object s, SelectionChangedEventArgs e) { if (RoomGrid.SelectedItem is MxRoom r) { _room = r; BuildStep3(); BuildStep4(); UpdateSummary(); } }
    private void OnRoomPrevPage(object s, RoutedEventArgs e) { if (_roomPage > 0) { _roomPage--; RefreshRoomPage(false); } }
    private void OnRoomNextPage(object s, RoutedEventArgs e) { _roomPage++; RefreshRoomPage(false); }
    private void OnRoomPageSizeChanged(object s, SelectionChangedEventArgs e) { _roomPageSize = RoomPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshRoomPage(true); }

    // === STEP 4: Price & Adjust ===
    private void BuildStep4()
    {
        var hasGuest = _guest is not null;
        var hasRoom = _room is not null;
        var hasDeparture = _newDepartureDate is not null && GetExtraNights() > 0;
        var hasRequiredSelection = hasGuest && hasRoom && hasDeparture;

        AdjustmentWarning.Visibility = hasRequiredSelection ? Visibility.Collapsed : Visibility.Visible;
        AdjustmentWarningText.Text = !hasGuest
            ? "Chưa chọn khách. Hãy chọn khách để xác định giá cũ và thời gian lưu trú."
            : !hasDeparture
                ? "Chưa chọn ngày đi mới hợp lệ. Ngày đi mới phải sau ngày đi hiện tại."
                : !hasRoom
                    ? "Chưa chọn phòng mới. Hãy chọn phòng để tính chênh lệch và tổng phát sinh."
                    : string.Empty;

        var oldPrice = hasGuest ? GetRoomTypePrice(_guest!.RoomType) : (decimal?)null;
        var newPrice = hasRoom ? GetSelectedRoomPrice() : (decimal?)null;
        var remainingNights = hasGuest ? GetRemainingNights() : 0;
        var extraNights = hasGuest && _newDepartureDate is not null ? GetExtraNights() : 0;
        var effectivePrice = GetEffectiveNightlyPrice(oldPrice, newPrice);
        var priceDifference = oldPrice is null || effectivePrice is null ? (decimal?)null : effectivePrice.Value - oldPrice.Value;
        var roomMoveCharge = priceDifference is null ? (decimal?)null : priceDifference.Value * remainingNights;
        var extensionCharge = effectivePrice is null || !hasDeparture ? (decimal?)null : effectivePrice.Value * extraNights;
        var grandTotal = roomMoveCharge is null || extensionCharge is null ? (decimal?)null : roomMoveCharge.Value + extensionCharge.Value;

        OldPriceText.Text = oldPrice is null ? "—" : FormatCurrency(oldPrice.Value);
        NewPriceText.Text = newPrice is null ? "—" : FormatCurrency(newPrice.Value);
        PriceDifferenceText.Text = priceDifference is null ? "—" : FormatSignedCurrency(priceDifference.Value);
        RemainingNightsText.Text = hasGuest ? $"{remainingNights} đêm" : "—";
        ExtraNightsText.Text = hasDeparture ? $"{extraNights} đêm" : "—";
        TotalDifferenceText.Text = roomMoveCharge is null ? "—" : FormatSignedCurrency(roomMoveCharge.Value);
        ExtensionChargeText.Text = extensionCharge is null ? "—" : FormatAmount(extensionCharge.Value);
        GrandTotalText.Text = grandTotal is null ? "—" : FormatSignedCurrency(grandTotal.Value);

        var pricePolicy = PricePolicyBox.SelectedItem?.ToString();
        EffectiveModeBox.IsEnabled = hasGuest;
        EffectiveDatePicker.IsEnabled = hasGuest && EffectiveModeBox.SelectedItem?.ToString() == "Chọn ngày giờ khác";
        EffectiveTimeBox.IsEnabled = hasGuest;
        ReasonBox.IsEnabled = hasGuest;
        ExtendReasonBox.IsEnabled = hasGuest && hasDeparture;
        RequestedByBox.IsEnabled = hasGuest;
        ApprovedByBox.IsEnabled = hasRequiredSelection;
        AdjustmentNoteBox.IsEnabled = hasGuest;
        PricePolicyBox.IsEnabled = hasGuest && hasRoom && newPrice is > 0;
        NegotiatedPriceBox.IsEnabled = hasRequiredSelection && pricePolicy == "Giá thỏa thuận";
        WaiverReasonBox.IsEnabled = hasRequiredSelection && pricePolicy == "Miễn chênh lệch";
    }

    private void OnAdjustmentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingAdjustment) return;

        if (ReferenceEquals(sender, EffectiveModeBox))
        {
            var mode = EffectiveModeBox.SelectedItem?.ToString();
            if (mode is "Đổi ngay" or "Cuối ngày")
            {
                _isInitializingAdjustment = true;
                EffectiveDatePicker.SelectedDate = DateTime.Today;
                EffectiveTimeBox.Text = mode == "Cuối ngày" ? "22:00" : DateTime.Now.ToString("HH:mm");
                _isInitializingAdjustment = false;
            }
        }

        BuildStep4();
        UpdateSummary();
        if (_step == 6) BuildStep6();
    }

    private void OnAdjustmentDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingAdjustment) return;
        BuildStep4();
        UpdateSummary();
        if (_step == 6) BuildStep6();
    }

    private void OnAdjustmentTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializingAdjustment) return;
        BuildStep4();
        if (_step == 6) BuildStep6();
    }

    private void ResetAdjustmentInputs()
    {
        _isInitializingAdjustment = true;
        EffectiveModeBox.SelectedIndex = 0;
        EffectiveDatePicker.SelectedDate = DateTime.Today;
        EffectiveTimeBox.Text = DateTime.Now.ToString("HH:mm");
        ReasonBox.SelectedIndex = 0;
        ExtendReasonBox.SelectedIndex = 0;
        PricePolicyBox.SelectedIndex = 0;
        RequestedByBox.Text = "Khách lưu trú";
        ApprovedByBox.Text = "Quản lý trực ca";
        AdjustmentNoteBox.Clear();
        NegotiatedPriceBox.Clear();
        WaiverReasonBox.Clear();
        _isInitializingAdjustment = false;
        BuildStep4();
    }

    private decimal? GetEffectiveNightlyPrice(decimal? oldPrice, decimal? newPrice)
    {
        if (oldPrice is null || newPrice is null) return null;

        return PricePolicyBox.SelectedItem?.ToString() switch
        {
            "Giữ nguyên giá cũ" => oldPrice,
            "Giá thỏa thuận" when TryParseCurrency(NegotiatedPriceBox.Text, out var negotiatedPrice) => negotiatedPrice,
            "Miễn chênh lệch" => oldPrice,
            _ => newPrice
        };
    }

    private int GetRemainingNights()
    {
        if (_guest is null) return 0;
        return Math.Max(1, (_guest.DepartureDate.Date - DateTime.Today).Days);
    }

    private int GetExtraNights()
    {
        if (_guest is null || _newDepartureDate is null) return 0;
        return Math.Max(0, (_newDepartureDate.Value.Date - _guest.DepartureDate.Date).Days);
    }

    private decimal? GetGrandTotal()
    {
        if (_guest is null || _room is null || GetExtraNights() <= 0) return null;
        var oldPrice = GetRoomTypePrice(_guest.RoomType);
        var newPrice = GetSelectedRoomPrice();
        if (oldPrice is null || newPrice is null) return null;
        var effectivePrice = GetEffectiveNightlyPrice(oldPrice, newPrice);
        return effectivePrice is null
            ? null
            : (effectivePrice.Value - oldPrice) * GetRemainingNights() + effectivePrice.Value * GetExtraNights();
    }

    private bool IsPricePolicyValid()
    {
        if (_guest is null || _room is null || GetSelectedRoomPrice() is not > 0 || GetRoomTypePrice(_guest.RoomType) is null)
        {
            return false;
        }

        var policy = PricePolicyBox.SelectedItem?.ToString();
        return policy switch
        {
            "Giá thỏa thuận" => TryParseCurrency(NegotiatedPriceBox.Text, out var value) && value > 0,
            "Miễn chênh lệch" => !string.IsNullOrWhiteSpace(WaiverReasonBox.Text),
            _ => true
        };
    }

    private static bool TryParseCurrency(string? value, out decimal result)
    {
        var normalized = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return decimal.TryParse(normalized, out result);
    }

    private decimal? GetRoomTypePrice(string roomType)
        => _guest is null
            ? null
            : GetRateForRoom(_guest.RoomTypeId, DateOnly.FromDateTime(_guest.DepartureDate), Math.Max(1, _guest.Adults));

    private decimal? GetSelectedRoomPrice()
    {
        if (_room is null) return null;
        var date = _newDepartureDate is { } departure
            ? DateOnly.FromDateTime(departure)
            : DateOnly.FromDateTime(DateTime.Today);
        var adults = Math.Max(1, _guest?.Adults ?? 1);
        return GetRateForRoom(_room.RoomTypeId, date, adults) ?? (_room.Price > 0 ? _room.Price : null);
    }

    private decimal? GetRateForRoom(Guid roomTypeId, DateOnly date, int adults)
    {
        var plan = _ratePlans
            .Where(rate => (rate.RoomTypeId is null || rate.RoomTypeId == roomTypeId)
                && date >= DateOnly.FromDateTime(rate.EffectiveFrom)
                && date <= DateOnly.FromDateTime(rate.EffectiveTo))
            .OrderByDescending(rate => rate.RoomTypeId == roomTypeId)
            .ThenBy(rate => rate.EffectiveFrom)
            .FirstOrDefault();

        return plan?.CalculatePrice(date, Math.Max(adults, 1), 1, 0, 0).finalPrice;
    }

    private string FormatCurrency(decimal value) => $"{value:N0} {_currency}/đêm";
    private string FormatAmount(decimal value) => $"{value:N0} {_currency}";
    private string FormatSignedCurrency(decimal value) =>
        value == 0 ? $"0 {_currency}" : $"{(value > 0 ? "+" : "−")}{Math.Abs(value):N0} {_currency}";

    // === STEP 5: Handover ===
    private void OnHandoverChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: string key } checkBox) return;

        if (checkBox.IsChecked == true)
        {
            _handover.Add(key);
        }
        else
        {
            _handover.Remove(key);
        }

        UpdateSummary();
        if (_step == 6) BuildStep6();
    }

    // === STEP 6: Confirm ===
    private void BuildStep6()
    {
        Step6Left.Children.Clear(); Step6Right.Children.Clear();
        if (_guest == null) { Step6Left.Children.Add(new TextBlock { Text = "Chưa chọn khách.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        Step6Left.Children.Add(STitle("Tóm tắt"));
        Step6Left.Children.Add(Row("Khách", _guest.Name)); Step6Left.Children.Add(Row("Mã HS", _guest.Folio.Replace("FOL-", "STY-")));
        Step6Left.Children.Add(Row("Phòng", $"{_guest.Room} → {_room?.Room ?? "?"}"));
        Step6Left.Children.Add(Row("Hạng", $"{_guest.RoomType} → {_room?.RoomType ?? "?"}"));
        var nd = _newDepartureDate; var extra = nd != null ? (nd.Value - _guest.DepartureDate).Days : 0;
        Step6Left.Children.Add(Row("Ngày đi", nd != null ? $"{_guest.Departure} → {nd:dd/MM} (+{extra} đêm)" : "Chưa chọn"));
        Step6Left.Children.Add(Row("Lý do đổi phòng", ReasonBox.SelectedItem?.ToString() ?? "—"));
        Step6Left.Children.Add(Row("Lý do gia hạn", ExtendReasonBox.SelectedItem?.ToString() ?? "—"));
        Step6Left.Children.Add(Row("Chính sách giá", PricePolicyBox.SelectedItem?.ToString() ?? "—"));
        var total = GetGrandTotal();
        Step6Left.Children.Add(Row("Tổng phát sinh", total is null ? "—" : FormatSignedCurrency(total.Value)));
        Step6Left.Children.Add(Row("Bàn giao", $"{_handover.Count}/{HandoverTaskCount} mục"));
        Step6Right.Children.Add(STitle("Kiểm tra sẵn sàng"));
        var items = new[] { ("Đã chọn khách", _guest != null), ("Đã chọn ngày đi mới", nd != null && extra > 0), ("Đã chọn phòng mới", _room != null), ("Phòng khả dụng", _room?.Status is not "Khóa" and not "OOO"), ("Giá hợp lệ", IsPricePolicyValid()), ("Bàn giao đầy đủ", _handover.Count == HandoverTaskCount) };
        var allOk = true; foreach (var (label, ok) in items) { Step6Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 12, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        Step6Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng" : "Còn mục chưa hoàn thành", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
    }

    // Navigation
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (_isSubmitting) return;
        if (_guest is null || _room is null || _stayAdjustmentService is null ||
            _guest.StayId == Guid.Empty || _room.RoomId == Guid.Empty ||
            _guest.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Chưa đủ dữ liệu thật để thực hiện: cần stay, phòng đích và row-version.", "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_newDepartureDate is null || GetExtraNights() <= 0)
        {
            MessageBox.Show("Ngày đi mới phải sau ngày đi hiện tại.", "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!IsPricePolicyValid())
        {
            MessageBox.Show("Chưa đủ dữ liệu rate plan thật để tính khoản điều chỉnh Folio.", "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _isSubmitting = true;
            BtnConfirm.IsEnabled = false;
            var result = await _stayAdjustmentService.AdjustAsync(
                new AdjustStayCommand(
                    _guest.StayId,
                    _guest.RowVersion,
                    _room.RoomId,
                    DateOnly.FromDateTime(_newDepartureDate.Value),
                    GetGrandTotal() ?? 0m,
                    ReasonBox.SelectedItem?.ToString() ?? "Điều chỉnh vận hành"),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể điều chỉnh lưu trú và Folio.", "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saved = result.Value!;
            FooterStepLabel.Text = $"Đã cập nhật thật: {saved.Stay.StayNumber} · phòng {saved.Stay.RoomNumber} · đi {saved.Stay.ScheduledDepartureDate:dd/MM/yyyy}";
            var folioMessage = saved.FolioEntry is null
                ? "Không phát sinh khoản Folio (miễn chênh lệch/0)."
                : $"Đã ghi Folio {saved.Folio?.FolioNumber ?? "Folio"}: {saved.FolioEntry.Amount:N0} {_currency}.";
            MessageBox.Show(
                $"Đã cập nhật stay {saved.Stay.StayNumber}.\nPhòng: {saved.Stay.RoomNumber}\nNgày đi mới: {saved.Stay.ScheduledDepartureDate:dd/MM/yyyy}\n{folioMessage}",
                "Điều chỉnh lưu trú",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isSubmitting = false;
            if (IsLoaded) BtnConfirm.IsEnabled = true;
        }
    }
    private void OnRefresh(object s, RoutedEventArgs e) => GoTo(1);
    private void OnClose(object s, RoutedEventArgs e) => Close();

    // Helpers
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static FrameworkElement TimelineItem(string label, string date, Brush color) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = color, Margin = new Thickness(0, 3, 8, 0) }, new StackPanel { Children = { new TextBlock { Text = label, FontSize = 11, Foreground = C("#64748B") }, new TextBlock { Text = date, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") } } } } };
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record MxGuest(
    string Room,
    string Name,
    DateTime ArrivalDate,
    DateTime DepartureDate,
    string RoomType,
    int GuestCount,
    bool IsVip,
    string Folio,
    decimal Balance,
    string FolioStatus,
    Guid StayId = default,
    Guid RoomId = default,
    Guid RoomTypeId = default,
    byte[]? RowVersion = null)
{
    public int Adults { get; init; } = 1;
    public int Children { get; init; }
    public string Arrival => ArrivalDate.ToString("dd/MM/yyyy");
    public string Departure => DepartureDate.ToString("dd/MM/yyyy");
    public string VipDisplay => IsVip ? "★" : "";
}

internal record MxRoom(
    string Room,
    string RoomType,
    string Floor,
    string Bed,
    string Status,
    string HkStatus,
    decimal Price,
    string Availability,
    Guid RoomId = default,
    Guid RoomTypeId = default,
    byte[]? RowVersion = null)
{
    public bool IsSelectable => RoomId != Guid.Empty && Status is not ("Khóa" or "OOO" or "Có khách đến");
    public string PriceDisplay => Price > 0 ? $"{Price:N0}" : "Chưa có tariff";
}
internal static class MxExt { public static TextBlock WithRuns(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
