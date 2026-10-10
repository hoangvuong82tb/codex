using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class RoomMoveWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private static readonly string[] StepNames =
    {
        "Chọn khách",
        "Chọn phòng",
        "Điều chỉnh",
        "Bàn giao",
        "Xác nhận"
    };

    private readonly List<RoomMoveGuestInfo> allGuests = new();
    private readonly List<RoomMoveRoomInfo> allRooms = new();
    private readonly HashSet<string> handoverChecks = new(StringComparer.Ordinal);

    private List<RoomMoveGuestInfo> filteredGuests = new();
    private List<RoomMoveRoomInfo> filteredRooms = new();
    private RoomMoveGuestInfo? selectedGuest;
    private RoomMoveRoomInfo? previewRoom;
    private RoomMoveRoomInfo? selectedNewRoom;
    private int guestPage;
    private int roomPage;
    private int guestPageSize = 20;
    private int roomPageSize = 20;
    private bool isInitializing = true;
    private bool isSubmitting;
    private readonly IHotelStayRepository? stayRepository;
    private readonly IHotelRoomMasterDataRepository? roomRepository;
    private readonly HotelStayApplicationService? stayService;
    private readonly IFrontOfficeExecutionContextAccessor? contextAccessor;
    private readonly Guid? initialStayId;

    public int CurrentStep { get; private set; } = 1;

    public RoomMoveWindow() : this(null)
    {
    }

    public RoomMoveWindow(Guid? initialStayId)
    {
        this.initialStayId = initialStayId;
        InitializeComponent();

        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        stayRepository = services?.GetService<IHotelStayRepository>();
        roomRepository = services?.GetService<IHotelRoomMasterDataRepository>();
        stayService = services?.GetService<HotelStayApplicationService>();
        contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();

        InitializeFilters();
        isInitializing = false;
        RefreshGuestPage(resetPage: true);
        RefreshRoomPage(resetPage: true);
        EffectiveDatePicker.SelectedDate = DateTime.Today;
        RenderCurrentStep();
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (stayRepository is null || roomRepository is null || contextAccessor?.Current is not { } context)
        {
            FooterStatusText.Text = "Chưa cấu hình Stay/Room service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var propertyId = context.Property.PropertyId.Value;
            var stays = await stayRepository.GetCurrentInHouseAsync(propertyId, CancellationToken.None);
            var rooms = await roomRepository.SearchRoomsAsync(
                propertyId, null, null, null, null, HotelRoomOperationalStatus.Available, true,
                new PageRequest(1, 500), "number", true, CancellationToken.None);

            allGuests.Clear();
            allGuests.AddRange(stays.Select(stay => new RoomMoveGuestInfo(
                stay.RoomNumber,
                stay.GuestName,
                stay.StayNumber,
                stay.ScheduledArrivalDate.ToDateTime(TimeOnly.MinValue),
                stay.ScheduledDepartureDate.ToDateTime(TimeOnly.MinValue),
                stay.RoomTypeCode,
                stay.Adults + stay.Children,
                false,
                stay.StayNumber,
                "Đang lưu trú",
                stay.HotelStayId,
                stay.RoomId,
                stay.RoomTypeId,
                stay.RowVersion)));

            if (initialStayId is { } requestedStayId && requestedStayId != Guid.Empty)
            {
                selectedGuest = allGuests.FirstOrDefault(guest => guest.StayId == requestedStayId);
                if (selectedGuest is not null)
                    UpdateGuestDetails();
            }

            allRooms.Clear();
            allRooms.AddRange(rooms.Items.Select(room => new RoomMoveRoomInfo(
                room.RoomNumber,
                room.HotelRoomTypeCode,
                room.Floor ?? "—",
                "—",
                room.OperationalStatus == HotelRoomOperationalStatus.Available ? "Sẵn sàng" : room.OperationalStatus.ToString(),
                "—",
                null,
                room.HotelRoomId,
                room.HotelRoomTypeId,
                room.RowVersion)));

            RefreshGuestPage(resetPage: true);
            RefreshRoomPage(resetPage: true);
            FooterStatusText.Text = $"Dữ liệu thật · {allGuests.Count:N0} stay · {allRooms.Count:N0} phòng khả dụng";
        }
        catch (Exception ex)
        {
            allGuests.Clear();
            allRooms.Clear();
            RefreshGuestPage(resetPage: true);
            RefreshRoomPage(resetPage: true);
            FooterStatusText.Text = "Không thể tải dữ liệu đổi phòng thật: " + ex.Message;
        }
    }

    private void InitializeFilters()
    {
        SetComboItems(GuestRoomTypeFilter, "Tất cả", "Standard", "Superior", "Deluxe", "Junior", "Suite");
        SetComboItems(GuestVipFilter, "Tất cả", "VIP", "Không VIP");
        SetComboItems(GuestFolioFilter, "Tất cả", "Đang mở", "Chờ đối soát", "Đã bảo lãnh");

        SetComboItems(RoomTypeFilter, "Tất cả", "Standard", "Superior", "Deluxe", "Junior", "Suite");
        SetComboItems(RoomFloorFilter, "Tất cả", "1", "2", "3", "4", "5");
        SetComboItems(RoomBedFilter, "Tất cả", "King", "Twin");
        SetComboItems(RoomStatusFilter, "Tất cả", "Sẵn sàng", "Sạch", "Đang dọn", "Có khách đến", "Khóa", "OOO");
        SetComboItems(RoomHousekeepingFilter, "Tất cả", "Sạch", "Đã KT", "Đang dọn", "Bẩn");
        SetComboItems(RoomPriceFilter, "Tất cả", "Dưới 1 triệu", "1–1,5 triệu", "Trên 1,5 triệu");

        SetComboItems(GuestPageSizeBox, "20 dòng / trang", "50 dòng / trang", "100 dòng / trang");
        SetComboItems(RoomPageSizeBox, "20 dòng / trang", "50 dòng / trang", "100 dòng / trang");

        SetComboItems(EffectiveModeBox, "Đổi ngay", "Cuối ngày", "Chọn ngày giờ khác");
        SetComboItems(
            ReasonBox,
            "Khách yêu cầu",
            "Phòng có sự cố",
            "Tiếng ồn",
            "Thiết bị hỏng",
            "Nâng hạng",
            "Hạ hạng",
            "Yêu cầu VIP",
            "Điều chỉnh vận hành",
            "Khác");
        SetComboItems(PricePolicyBox, "Áp dụng giá mới", "Giữ nguyên giá cũ", "Giá thỏa thuận", "Miễn chênh lệch");
    }

    private static void SetComboItems(ComboBox comboBox, params string[] values)
    {
        comboBox.ItemsSource = values;
        comboBox.SelectedIndex = 0;
    }

    private void RenderCurrentStep()
    {
        StepRuntimeError.Visibility = Visibility.Collapsed;

        try
        {
            CurrentStep = Math.Clamp(CurrentStep, 1, 5);
            Step1Content.Visibility = CurrentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Content.Visibility = CurrentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Content.Visibility = CurrentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4Content.Visibility = CurrentStep == 4 ? Visibility.Visible : Visibility.Collapsed;
            Step5Content.Visibility = CurrentStep == 5 ? Visibility.Visible : Visibility.Collapsed;

            UpdateStepButtons();
            UpdateSummary();
            UpdateAdjustmentStep();
            UpdateConfirmationStep();
            UpdateFooter();
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Room Move wizard render failed. Step={CurrentStep}; Method={Method}; Inner={InnerException}",
                CurrentStep,
                nameof(RenderCurrentStep),
                exception.InnerException?.ToString() ?? "(null)");

            StepRuntimeErrorText.Text = $"Không thể hiển thị bước {CurrentStep}. Chi tiết kỹ thuật đã được ghi vào nhật ký.";
            StepRuntimeError.Visibility = Visibility.Visible;
        }
    }

    private void UpdateStepButtons()
    {
        var buttons = new[] { Step1Button, Step2Button, Step3Button, Step4Button, Step5Button };
        for (var index = 0; index < buttons.Length; index++)
        {
            var isActive = index + 1 == CurrentStep;
            buttons[index].Background = Brush(isActive ? "#EAF2FF" : "#FFFFFF");
            buttons[index].BorderBrush = Brush(isActive ? "#2F6BFF" : "#E2E8F0");
            buttons[index].Foreground = Brush(isActive ? "#1D4ED8" : "#64748B");
        }
    }

    private void UpdateSummary()
    {
        SummaryGuestText.Text = selectedGuest?.Name ?? "Chưa chọn khách";
        SummaryRoomText.Text = selectedGuest is null
            ? "Chưa chọn phòng"
            : selectedNewRoom is null
                ? $"{selectedGuest.Room} → Chưa chọn"
                : $"{selectedGuest.Room} → {selectedNewRoom.Room}";
        SummaryEffectiveText.Text = BuildEffectiveDisplay();
        var summaryDifference = selectedNewRoom is null ? null : CalculateDifference(selectedNewRoom);
        SummaryDifferenceText.Text = summaryDifference.HasValue
            ? FormatSignedCurrency(summaryDifference.Value)
            : "Chưa có giá thật";

        var isReady = IsReadyToConfirm();
        SummaryReadinessBadge.Background = Brush(isReady ? "#DCFCE7" : "#FEF3C7");
        SummaryReadinessText.Foreground = Brush(isReady ? "#15803D" : "#A16207");
        SummaryReadinessText.Text = isReady ? "Sẵn sàng xác nhận" : "Chưa đủ thông tin";
    }

    private void UpdateFooter()
    {
        FooterStepLabel.Text = $"Bước {CurrentStep}/5 · {StepNames[CurrentStep - 1]}";
        BackButton.IsEnabled = CurrentStep > 1;
        NextButton.Visibility = CurrentStep == 5 ? Visibility.Collapsed : Visibility.Visible;
        ConfirmButton.Visibility = CurrentStep == 5 ? Visibility.Visible : Visibility.Collapsed;
        ConfirmButton.IsEnabled = IsReadyToConfirm();
    }

    private void OnStepClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string stepText } &&
            int.TryParse(stepText, NumberStyles.None, CultureInfo.InvariantCulture, out var step))
        {
            CurrentStep = step;
            RenderCurrentStep();
        }
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (CurrentStep < 5)
        {
            CurrentStep++;
            RenderCurrentStep();
        }
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            RenderCurrentStep();
        }
    }

    private void OnApplyGuestFilters(object sender, RoutedEventArgs e) => RefreshGuestPage(resetPage: true);

    private void OnClearGuestFilters(object sender, RoutedEventArgs e)
    {
        GuestSearchBox.Clear();
        GuestRoomFilterBox.Clear();
        GuestRoomTypeFilter.SelectedIndex = 0;
        GuestDepartureFilter.SelectedDate = null;
        GuestVipFilter.SelectedIndex = 0;
        GuestFolioFilter.SelectedIndex = 0;
        RefreshGuestPage(resetPage: true);
    }

    private void RefreshGuestPage(bool resetPage)
    {
        if (isInitializing)
        {
            return;
        }

        if (resetPage)
        {
            guestPage = 0;
        }

        var search = GuestSearchBox.Text.Trim();
        var room = GuestRoomFilterBox.Text.Trim();
        var roomType = GuestRoomTypeFilter.SelectedItem?.ToString();
        var vip = GuestVipFilter.SelectedItem?.ToString();
        var folioStatus = GuestFolioFilter.SelectedItem?.ToString();
        var departure = GuestDepartureFilter.SelectedDate?.Date;

        filteredGuests = allGuests
            .Where(guest =>
                string.IsNullOrWhiteSpace(search) ||
                guest.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                guest.Room.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                guest.ProfileCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                guest.Folio.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Where(guest => string.IsNullOrWhiteSpace(room) || guest.Room.Contains(room, StringComparison.OrdinalIgnoreCase))
            .Where(guest => roomType is null or "Tất cả" || guest.RoomType == roomType)
            .Where(guest => departure is null || guest.DepartureDate.Date == departure.Value)
            .Where(guest => vip is null or "Tất cả" || (vip == "VIP" ? guest.IsVip : !guest.IsVip))
            .Where(guest => folioStatus is null or "Tất cả" || guest.FolioStatus == folioStatus)
            .ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredGuests.Count / (double)guestPageSize));
        guestPage = Math.Clamp(guestPage, 0, totalPages - 1);
        GuestGrid.ItemsSource = filteredGuests.Skip(guestPage * guestPageSize).Take(guestPageSize).ToList();
        GuestGrid.SelectedItem = selectedGuest;
        UpdateGuestPager(totalPages);
    }

    private void UpdateGuestPager(int totalPages)
    {
        var start = filteredGuests.Count == 0 ? 0 : guestPage * guestPageSize + 1;
        var end = Math.Min(filteredGuests.Count, start + guestPageSize - 1);
        GuestPagerSummary.Text = $"Hiển thị {start}–{end} trên tổng {filteredGuests.Count} khách";
        GuestPreviousButton.IsEnabled = guestPage > 0;
        GuestNextButton.IsEnabled = guestPage < totalPages - 1;
        BuildPageButtons(GuestPageButtons, guestPage, totalPages, OnGuestPageButtonClick);
    }

    private void OnGuestSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not RoomMoveGuestInfo guest || ReferenceEquals(selectedGuest, guest))
        {
            return;
        }

        selectedGuest = guest;
        previewRoom = null;
        selectedNewRoom = null;
        RoomGrid.SelectedItem = null;
        UpdateGuestDetails();
        RefreshRoomPage(resetPage: true);
        RenderCurrentStep();
    }

    private void UpdateGuestDetails()
    {
        var hasGuest = selectedGuest is not null;
        GuestEmptyState.Visibility = hasGuest ? Visibility.Collapsed : Visibility.Visible;
        GuestDetailPanel.Visibility = hasGuest ? Visibility.Visible : Visibility.Collapsed;
        if (selectedGuest is null)
        {
            return;
        }

        GuestDetailName.Text = selectedGuest.Name;
        GuestDetailProfile.Text = $"{selectedGuest.ProfileCode} · {selectedGuest.Folio}";
        GuestDetailRoom.Text = $"Phòng hiện tại: {selectedGuest.Room} · {selectedGuest.RoomType}";
        GuestDetailDates.Text = $"Lưu trú: {selectedGuest.Arrival} → {selectedGuest.Departure}";
        GuestDetailFolio.Text = $"Folio: {selectedGuest.FolioStatus} · {selectedGuest.GuestCount} khách";
        GuestDetailVip.Text = selectedGuest.IsVip ? "Khách VIP" : "Khách thường";
    }

    private void OnGuestPreviousPage(object sender, RoutedEventArgs e)
    {
        if (guestPage > 0)
        {
            guestPage--;
            RefreshGuestPage(resetPage: false);
        }
    }

    private void OnGuestNextPage(object sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredGuests.Count / (double)guestPageSize));
        if (guestPage < totalPages - 1)
        {
            guestPage++;
            RefreshGuestPage(resetPage: false);
        }
    }

    private void OnGuestPageButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int page })
        {
            guestPage = page;
            RefreshGuestPage(resetPage: false);
        }
    }

    private void OnGuestPageSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializing || !TryReadPageSize(GuestPageSizeBox, out var pageSize))
        {
            return;
        }

        guestPageSize = pageSize;
        RefreshGuestPage(resetPage: true);
    }

    private void OnApplyRoomFilters(object sender, RoutedEventArgs e) => RefreshRoomPage(resetPage: true);

    private void OnClearRoomFilters(object sender, RoutedEventArgs e)
    {
        RoomTypeFilter.SelectedIndex = 0;
        RoomFloorFilter.SelectedIndex = 0;
        RoomBedFilter.SelectedIndex = 0;
        RoomStatusFilter.SelectedIndex = 0;
        RoomHousekeepingFilter.SelectedIndex = 0;
        RoomPriceFilter.SelectedIndex = 0;
        ReadyRoomsOnlyCheckBox.IsChecked = false;
        RefreshRoomPage(resetPage: true);
    }

    private void RefreshRoomPage(bool resetPage)
    {
        if (isInitializing)
        {
            return;
        }

        if (resetPage)
        {
            roomPage = 0;
        }

        var roomType = RoomTypeFilter.SelectedItem?.ToString();
        var floor = RoomFloorFilter.SelectedItem?.ToString();
        var bed = RoomBedFilter.SelectedItem?.ToString();
        var status = RoomStatusFilter.SelectedItem?.ToString();
        var housekeeping = RoomHousekeepingFilter.SelectedItem?.ToString();
        var price = RoomPriceFilter.SelectedItem?.ToString();
        var readyOnly = ReadyRoomsOnlyCheckBox.IsChecked == true;

        filteredRooms = allRooms
            .Where(room => roomType is null or "Tất cả" || room.RoomType == roomType)
            .Where(room => floor is null or "Tất cả" || room.Floor == floor)
            .Where(room => bed is null or "Tất cả" || room.Bed == bed)
            .Where(room => status is null or "Tất cả" || room.Status == status)
            .Where(room => housekeeping is null or "Tất cả" || room.HousekeepingStatus == housekeeping)
            .Where(room => price is null or "Tất cả" ||
                room.Price is { } roomPrice &&
                (price == "Dưới 1 triệu" && roomPrice < 1_000_000m ||
                 price == "1–1,5 triệu" && roomPrice >= 1_000_000m && roomPrice <= 1_500_000m ||
                 price == "Trên 1,5 triệu" && roomPrice > 1_500_000m))
            .Where(room => !readyOnly || room.IsSelectable)
            .ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredRooms.Count / (double)roomPageSize));
        roomPage = Math.Clamp(roomPage, 0, totalPages - 1);
        var pageRows = filteredRooms.Skip(roomPage * roomPageSize).Take(roomPageSize).ToList();
        RoomGrid.ItemsSource = pageRows;
        RoomGrid.SelectedItem = previewRoom;
        UpdateRoomPager(totalPages);
        UpdateRoomPreview();
    }

    private void UpdateRoomPager(int totalPages)
    {
        var start = filteredRooms.Count == 0 ? 0 : roomPage * roomPageSize + 1;
        var end = Math.Min(filteredRooms.Count, start + roomPageSize - 1);
        RoomPagerSummary.Text = $"Hiển thị {start}–{end} trên tổng {filteredRooms.Count} phòng";
        RoomPreviousButton.IsEnabled = roomPage > 0;
        RoomNextButton.IsEnabled = roomPage < totalPages - 1;
        BuildPageButtons(RoomPageButtons, roomPage, totalPages, OnRoomPageButtonClick);
    }

    private void OnRoomPreviewChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RoomGrid.SelectedItem is RoomMoveRoomInfo room)
        {
            previewRoom = room;
            UpdateRoomPreview();
        }
    }

    private void UpdateRoomPreview()
    {
        RoomGuestWarning.Visibility = selectedGuest is null ? Visibility.Visible : Visibility.Collapsed;
        var hasPreview = previewRoom is not null;
        RoomEmptyState.Visibility = hasPreview ? Visibility.Collapsed : Visibility.Visible;
        RoomPreviewPanel.Visibility = hasPreview ? Visibility.Visible : Visibility.Collapsed;

        if (previewRoom is null)
        {
            SelectPreviewRoomButton.IsEnabled = false;
            return;
        }

        RoomPreviewTitle.Text = $"Phòng {previewRoom.Room}";
        RoomPreviewType.Text = $"{previewRoom.RoomType} · tầng {previewRoom.Floor}";
        RoomPreviewStatus.Text = $"Trạng thái: {previewRoom.Status} · Nhà buồng: {previewRoom.HousekeepingStatus}";
        RoomPreviewBed.Text = $"Giường: {previewRoom.Bed}";
        RoomPreviewPrice.Text = $"Giá: {previewRoom.PriceDisplay}";
        RoomPreviewDifference.Text = $"Chênh lệch: {previewRoom.DifferenceDisplay}";
        SelectPreviewRoomButton.Content = previewRoom.IsSelectable ? "Chọn phòng này" : "Phòng không khả dụng";
        SelectPreviewRoomButton.IsEnabled = selectedGuest is not null && previewRoom.IsSelectable;
    }

    private void OnSelectPreviewRoom(object sender, RoutedEventArgs e)
    {
        if (selectedGuest is null || previewRoom is null || !previewRoom.IsSelectable)
        {
            return;
        }

        selectedNewRoom = previewRoom;
        FooterStatusText.Text = $"Đã chọn phòng {selectedNewRoom.Room}; chưa có dữ liệu nào được ghi.";
        RenderCurrentStep();
    }

    private void OnRoomPreviousPage(object sender, RoutedEventArgs e)
    {
        if (roomPage > 0)
        {
            roomPage--;
            RefreshRoomPage(resetPage: false);
        }
    }

    private void OnRoomNextPage(object sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredRooms.Count / (double)roomPageSize));
        if (roomPage < totalPages - 1)
        {
            roomPage++;
            RefreshRoomPage(resetPage: false);
        }
    }

    private void OnRoomPageButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int page })
        {
            roomPage = page;
            RefreshRoomPage(resetPage: false);
        }
    }

    private void OnRoomPageSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializing || !TryReadPageSize(RoomPageSizeBox, out var pageSize))
        {
            return;
        }

        roomPageSize = pageSize;
        RefreshRoomPage(resetPage: true);
    }

    private void UpdateAdjustmentStep()
    {
        var hasRequiredSelection = selectedGuest is not null && selectedNewRoom is not null;
        AdjustmentWarning.Visibility = hasRequiredSelection ? Visibility.Collapsed : Visibility.Visible;
        AdjustmentWarningText.Text = selectedGuest is null
            ? "Chưa chọn khách. Bước Điều chỉnh vẫn hiển thị; hãy chọn khách để tính giá và số đêm còn lại."
            : selectedNewRoom is null
                ? "Chưa chọn phòng mới. Bước Điều chỉnh vẫn hiển thị; hãy chọn phòng để tính chênh lệch."
                : string.Empty;

        OldPriceText.Text = selectedGuest is null ? "—" : "Chưa có rate plan thật";
        NewPriceText.Text = selectedNewRoom is null ? "—" : selectedNewRoom.PriceDisplay;
        var difference = selectedNewRoom is null ? (decimal?)null : CalculateDifference(selectedNewRoom);
        PriceDifferenceText.Text = difference is null ? "Chưa có giá thật" : FormatSignedCurrency(difference.Value);
        var remainingNights = selectedGuest is null
            ? 0
            : Math.Max(1, (selectedGuest.DepartureDate.Date - DateTime.Today).Days);
        RemainingNightsText.Text = selectedGuest is null ? "—" : $"{remainingNights} đêm";
        TotalDifferenceText.Text = difference is null ? "Chưa có giá thật" : FormatSignedCurrency(difference.Value * remainingNights);

        var pricePolicy = PricePolicyBox.SelectedItem?.ToString();
        NegotiatedPriceBox.IsEnabled = hasRequiredSelection && pricePolicy == "Giá thỏa thuận";
        WaiverReasonBox.IsEnabled = hasRequiredSelection && pricePolicy == "Miễn chênh lệch";
    }

    private void OnAdjustmentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isInitializing)
        {
            UpdateSummary();
            UpdateAdjustmentStep();
            UpdateConfirmationStep();
        }
    }

    private void OnAdjustmentDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isInitializing)
        {
            UpdateSummary();
            UpdateConfirmationStep();
        }
    }

    private void OnAdjustmentTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!isInitializing)
        {
            UpdateSummary();
            UpdateConfirmationStep();
        }
    }

    private void OnHandoverChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: string key } checkBox)
        {
            return;
        }

        if (checkBox.IsChecked == true)
        {
            handoverChecks.Add(key);
        }
        else
        {
            handoverChecks.Remove(key);
        }

        UpdateSummary();
        UpdateConfirmationStep();
    }

    private void UpdateConfirmationStep()
    {
        ConfirmGuestText.Text = selectedGuest?.Name ?? "Chưa chọn khách";
        ConfirmOldRoomText.Text = selectedGuest is null ? "—" : $"{selectedGuest.Room} · {selectedGuest.RoomType}";
        ConfirmNewRoomText.Text = selectedNewRoom is null ? "Chưa chọn phòng" : $"{selectedNewRoom.Room} · {selectedNewRoom.RoomType}";
        ConfirmEffectiveText.Text = BuildEffectiveDisplay();
        ConfirmReasonText.Text = ReasonBox.SelectedItem?.ToString() ?? "Khách yêu cầu";
        ConfirmPriceText.Text = selectedGuest is null
            ? "—"
            : $"Chưa có rate plan thật → {selectedNewRoom?.PriceDisplay ?? "—"}";
        var confirmationDifference = selectedNewRoom is null ? null : CalculateDifference(selectedNewRoom);
        ConfirmDifferenceText.Text = confirmationDifference.HasValue
            ? FormatSignedCurrency(confirmationDifference.Value)
            : "Chưa có giá thật";
        ConfirmApproverText.Text = string.IsNullOrWhiteSpace(ApprovedByBox.Text) ? "Chưa nhập" : ApprovedByBox.Text.Trim();
        ConfirmHandoverText.Text = $"{handoverChecks.Count}/18 tác vụ đã đánh dấu";

        ReadinessIssuesPanel.Children.Clear();
        var issues = new List<string>();
        if (selectedGuest is null)
        {
            issues.Add("Chưa chọn khách đang lưu trú.");
        }

        if (selectedNewRoom is null)
        {
            issues.Add("Chưa chọn phòng mới.");
        }
        else if (!selectedNewRoom.IsSelectable)
        {
            issues.Add("Phòng mới không ở trạng thái khả dụng.");
        }

        if (handoverChecks.Count < 18)
        {
            issues.Add($"Còn {18 - handoverChecks.Count} tác vụ bàn giao chưa đánh dấu.");
        }

        var isReady = IsReadyToConfirm();
        ReadinessPanel.Background = Brush(isReady ? "#DCFCE7" : "#FEF3C7");
        ReadinessTitleText.Foreground = Brush(isReady ? "#15803D" : "#A16207");
        ReadinessDescriptionText.Foreground = Brush(isReady ? "#166534" : "#92400E");
        ReadinessTitleText.Text = isReady ? "Sẵn sàng xác nhận" : "Chưa đủ thông tin";
        ReadinessDescriptionText.Text = isReady
            ? "Khách và phòng mới đã hợp lệ. Sau khi xác nhận, hệ thống sẽ ghi đổi phòng vào dữ liệu thật."
            : "Hoàn thiện các mục bắt buộc trước khi xác nhận.";

        if (issues.Count == 0)
        {
            issues.Add("Không có blocking issue.");
        }

        foreach (var issue in issues)
        {
            ReadinessIssuesPanel.Children.Add(new TextBlock
            {
                Text = $"• {issue}",
                Margin = new Thickness(0, 0, 0, 6),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Foreground = Brush(issue.StartsWith("Không", StringComparison.Ordinal) ? "#15803D" : "#92400E"),
                TextWrapping = TextWrapping.Wrap
            });
        }

        ConfirmButton.IsEnabled = isReady;
    }

    private async void OnConfirm(object sender, RoutedEventArgs e)
    {
        if (isSubmitting || !IsReadyToConfirm() || selectedGuest is null || selectedNewRoom is null)
        {
            return;
        }

        isSubmitting = true;
        ConfirmButton.IsEnabled = false;
        try
        {
            var confirmation = new RoomMoveConfirmationWindow(new RoomMoveConfirmationSummary(
                GuestName: selectedGuest.Name,
                OldRoom: $"{selectedGuest.Room} · {selectedGuest.RoomType}",
                NewRoom: $"{selectedNewRoom.Room} · {selectedNewRoom.RoomType}",
                EffectiveAt: BuildEffectiveDisplay(),
                Reason: ReasonBox.SelectedItem?.ToString() ?? "Khách yêu cầu",
                PriceDifference: CalculateDifference(selectedNewRoom) is { } difference
                    ? FormatSignedCurrency(difference)
                    : "Chưa có giá thật",
                RemainingTasks: 18 - handoverChecks.Count))
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (confirmation.ShowDialog() == true)
            {
                if (stayService is null || selectedGuest.StayId == Guid.Empty || selectedNewRoom.RoomId == Guid.Empty || selectedGuest.RowVersion is not { Length: 8 })
                {
                    MessageBox.Show("Chưa đủ dữ liệu thật để đổi phòng: cần stay, phòng đích và row-version.", "Đổi phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var reason = ReasonBox.SelectedItem?.ToString() ?? "Khách yêu cầu";
                var result = await stayService.MoveRoomAsync(new MoveRoomCommand(selectedGuest.StayId, selectedNewRoom.RoomId, reason, selectedGuest.RowVersion), CancellationToken.None);
                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error?.Message ?? "Không thể đổi phòng.", "Đổi phòng", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                FooterStatusText.Text = $"Đã đổi phòng thật: {result.Value!.StayNumber} → {result.Value.RoomNumber}.";
                MessageBox.Show($"Đã đổi phòng stay {result.Value.StayNumber} sang phòng {result.Value.RoomNumber}.", "Đổi phòng", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể hoàn tất đổi phòng: " + ex.Message, "Đổi phòng", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            isSubmitting = false;
            if (IsLoaded) UpdateConfirmationStep();
        }
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        selectedGuest = null;
        previewRoom = null;
        selectedNewRoom = null;
        handoverChecks.Clear();
        GuestGrid.SelectedItem = null;
        RoomGrid.SelectedItem = null;
        UpdateGuestDetails();
        UpdateRoomPreview();
        OnClearGuestFilters(sender, e);
        OnClearRoomFilters(sender, e);
        CurrentStep = 1;
        FooterStatusText.Text = "Đang tải lại dữ liệu thật…";
        OnLoadedAsync(this, new RoutedEventArgs());
        RenderCurrentStep();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private bool IsReadyToConfirm() =>
        selectedGuest is not null &&
        selectedNewRoom is { IsSelectable: true };

    private decimal? CalculateDifference(RoomMoveRoomInfo room) =>
        room.Price is { } newPrice && room.CurrentRoomPrice is { } currentPrice
            ? newPrice - currentPrice
            : null;

    private string BuildEffectiveDisplay()
    {
        var mode = EffectiveModeBox.SelectedItem?.ToString() ?? "Đổi ngay";
        if (mode == "Đổi ngay")
        {
            return "Ngay";
        }

        if (mode == "Cuối ngày")
        {
            return $"{DateTime.Today:dd/MM/yyyy} · 23:59";
        }

        return $"{(EffectiveDatePicker.SelectedDate ?? DateTime.Today):dd/MM/yyyy} · {EffectiveTimeBox.Text.Trim()}";
    }

    private static string FormatCurrency(decimal value) =>
        $"{value:N0} ₫".Replace(",", ".", StringComparison.Ordinal);

    private static string FormatSignedCurrency(decimal value)
    {
        var prefix = value > 0 ? "+" : value < 0 ? "−" : string.Empty;
        return $"{prefix}{FormatCurrency(Math.Abs(value))}";
    }

    private static bool TryReadPageSize(ComboBox comboBox, out int pageSize)
    {
        var text = comboBox.SelectedItem?.ToString() ?? string.Empty;
        var firstToken = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(firstToken, NumberStyles.None, CultureInfo.InvariantCulture, out pageSize);
    }

    private void BuildPageButtons(
        StackPanel host,
        int currentPage,
        int totalPages,
        RoutedEventHandler clickHandler)
    {
        host.Children.Clear();
        var pageIndexes = BuildVisiblePageIndexes(currentPage, totalPages);
        int? previousIndex = null;

        foreach (var pageIndex in pageIndexes)
        {
            if (previousIndex.HasValue && pageIndex - previousIndex.Value > 1)
            {
                host.Children.Add(new TextBlock
                {
                    Text = "…",
                    Width = 24,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 12,
                    Foreground = Brush("#94A3B8")
                });
            }

            var button = new Button
            {
                Content = (pageIndex + 1).ToString(CultureInfo.InvariantCulture),
                Tag = pageIndex,
                Style = (Style)FindResource(pageIndex == currentPage ? "ActivePagerButtonStyle" : "PagerButtonStyle")
            };
            button.Click += clickHandler;
            host.Children.Add(button);
            previousIndex = pageIndex;
        }
    }

    private static IReadOnlyList<int> BuildVisiblePageIndexes(int currentPage, int totalPages)
    {
        if (totalPages <= 7)
        {
            return Enumerable.Range(0, totalPages).ToArray();
        }

        var pages = new SortedSet<int>
        {
            0,
            totalPages - 1,
            Math.Clamp(currentPage - 1, 0, totalPages - 1),
            Math.Clamp(currentPage, 0, totalPages - 1),
            Math.Clamp(currentPage + 1, 0, totalPages - 1)
        };
        return pages.ToArray();
    }

    private static SolidColorBrush Brush(string color) =>
        new((Color)ColorConverter.ConvertFromString(color));
}

internal sealed record RoomMoveGuestInfo(
    string Room,
    string Name,
    string ProfileCode,
    DateTime ArrivalDate,
    DateTime DepartureDate,
    string RoomType,
    int GuestCount,
    bool IsVip,
    string Folio,
    string FolioStatus,
    Guid StayId = default,
    Guid RoomId = default,
    Guid RoomTypeId = default,
    byte[]? RowVersion = null)
{
    public string Arrival => ArrivalDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    public string Departure => DepartureDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    public string VipDisplay => IsVip ? "VIP" : "—";
}

internal sealed record RoomMoveRoomInfo(
    string Room,
    string RoomType,
    string Floor,
    string Bed,
    string Status,
    string HousekeepingStatus,
    decimal? Price,
    Guid RoomId = default,
    Guid RoomTypeId = default,
    byte[]? RowVersion = null)
{
    public decimal? CurrentRoomPrice { get; set; }
    public bool IsSelectable => Status is not ("Khóa" or "OOO" or "Có khách đến");
    public string PriceDisplay => Price is { } price
        ? $"{price:N0} ₫".Replace(",", ".", StringComparison.Ordinal)
        : "Chưa có giá thật";
    public string DifferenceDisplay
    {
        get
        {
            if (CurrentRoomPrice is null)
            {
                return "—";
            }

            if (Price is not { } price)
            {
                return "Chưa có giá thật";
            }

            var difference = price - CurrentRoomPrice.Value;
            var prefix = difference > 0 ? "+" : difference < 0 ? "−" : string.Empty;
            return $"{prefix}{Math.Abs(difference):N0} ₫".Replace(",", ".", StringComparison.Ordinal);
        }
    }
}

internal sealed record RoomMoveConfirmationSummary(
    string GuestName,
    string OldRoom,
    string NewRoom,
    string EffectiveAt,
    string Reason,
    string PriceDifference,
    int RemainingTasks);
