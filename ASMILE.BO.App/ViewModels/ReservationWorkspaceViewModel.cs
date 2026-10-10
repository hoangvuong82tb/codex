using ASMILE.BO.App.Services;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Reservation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public enum WorkspaceMode { Search, Status, Confirmation, Cancellation, RoomAssignment }

public sealed record ReservationRatePlanOption(
    Guid RatePlanId,
    string PlanCode,
    string PlanName,
    decimal BaseRate,
    DateTime EffectiveFrom,
    DateTime EffectiveTo)
{
    public string DisplayText => $"{PlanCode} · {PlanName} · {BaseRate:N0} VND";
}

public sealed record ReservationCommercialOption(Guid Id, string Code, string Name)
{
    public string DisplayText => string.IsNullOrWhiteSpace(Name) ? Code : $"{Code} · {Name}";
}

public sealed partial class ReservationWorkspaceViewModel : ViewModelBase, IDisposable
{
    private readonly IReservationUiService service;
    private readonly IFrontOfficePropertyContextHost propertyContextHost;
    private readonly IRateRepository? rateRepository;
    private readonly IHotelStructureMasterDataRepository? hotelRepository;
    private CancellationTokenSource? _searchCancellation;
    private CancellationTokenSource? _searchDebounce;
    private CancellationTokenSource? _ratePlanCancellation;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly SemaphoreSlim _ratePlanLoadGate = new(1, 1);
    private ReservationDto? _current;
    private bool _applyingSelection;
    private bool _suppressStatusSearch;
    private static readonly ReservationStatus?[] AllStatusFilterValues =
    [
        null, ReservationStatus.Draft, ReservationStatus.Tentative, ReservationStatus.Confirmed,
        ReservationStatus.Guaranteed, ReservationStatus.CheckedIn, ReservationStatus.Completed,
        ReservationStatus.Cancelled, ReservationStatus.NoShow
    ];
    private static readonly ReservationStatus?[] ConfirmationStatusFilterValues =
    [ReservationStatus.Draft, ReservationStatus.Tentative];

    [ObservableProperty] private WorkspaceMode mode = WorkspaceMode.Search;
    [ObservableProperty] private ReservationDto? selectedReservationDetail;

    public string LegacyWorkspaceTitle => WorkspaceTitle;
    public string WorkspaceTitle => Mode switch
    {
        WorkspaceMode.Search => "T\u00ECm \u0111\u1EB7t ph\u00F2ng",
        WorkspaceMode.Status => "T\u00ECnh tr\u1EA1ng \u0111\u1EB7t ph\u00F2ng",
        WorkspaceMode.Confirmation => "X\u00E1c nh\u1EADn \u0111\u1EB7t ph\u00F2ng",
        WorkspaceMode.Cancellation => "H\u1EE7y \u0111\u1EB7t ph\u00F2ng",
        WorkspaceMode.RoomAssignment => "Ph\u00E2n ph\u00F2ng",
        _ => "\u0110\u1EB7t ph\u00F2ng"
    };
    public bool IsStatusMode => Mode == WorkspaceMode.Status;
    public bool IsConfirmationMode => Mode == WorkspaceMode.Confirmation;
    public bool IsCancellationMode => Mode == WorkspaceMode.Cancellation;
    public bool ShowKpiBar => Mode == WorkspaceMode.Status;
    public string? SelectedGuestName => SelectedReservation?.GuestName;
    public string? SelectedPhone => SelectedReservation?.GuestPhone;
    public string? SelectedEmail => SelectedReservation?.GuestEmail;
    public DateOnly? SelectedArrival => SelectedReservation?.ArrivalDate;
    public DateOnly? SelectedDeparture => SelectedReservation?.DepartureDate;
    public int SelectedNights => SelectedReservation is not null ? Math.Max(0, (SelectedReservation.DepartureDate.DayNumber - SelectedReservation.ArrivalDate.DayNumber)) : 0;
    public string? SelectedSource => SelectedReservation?.Source;
    public string? SelectedRoomTypeName { get; private set; }
    public string? SelectedRoomNumber { get; private set; }
    public string? SelectedStatusText => SelectedReservation?.Status.ToString();

    partial void OnModeChanged(WorkspaceMode value)
    {
        OnPropertyChanged(nameof(WorkspaceTitle));
        OnPropertyChanged(nameof(IsStatusMode));
        OnPropertyChanged(nameof(IsConfirmationMode));
        OnPropertyChanged(nameof(IsCancellationMode));
        OnPropertyChanged(nameof(ShowKpiBar));
        UpdateStatusFilters(value);
    }
    partial void OnSelectedReservationChanged(ReservationDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanCheckInSelection));
        // Apply() refreshes the selected DTO after loading the full record.  That
        // internal update must not clear the inspector that the user just opened.
        if (_applyingSelection) return;
        if (value is null) { ClearDetail(); return; }
        _ = OpenAsync(value);
    }
    private void ClearDetail()
    {
        SelectedReservationDetail = null;
        SelectedRoomTypeName = null; SelectedRoomNumber = null;
        OnPropertyChanged(nameof(SelectedGuestName)); OnPropertyChanged(nameof(SelectedPhone));
        OnPropertyChanged(nameof(SelectedEmail)); OnPropertyChanged(nameof(SelectedArrival));
        OnPropertyChanged(nameof(SelectedDeparture)); OnPropertyChanged(nameof(SelectedNights));
        OnPropertyChanged(nameof(SelectedSource)); OnPropertyChanged(nameof(SelectedRoomTypeName));
        OnPropertyChanged(nameof(SelectedRoomNumber)); OnPropertyChanged(nameof(SelectedStatusText));
    }
    public ReservationWorkspaceViewModel(
        IReservationUiService service,
        IFrontOfficePropertyContextHost propertyContextHost,
        IRateRepository? rateRepository = null,
        IHotelStructureMasterDataRepository? hotelRepository = null)
    {
        this.service = service;
        this.propertyContextHost = propertyContextHost;
        this.rateRepository = rateRepository;
        this.hotelRepository = hotelRepository;
        propertyContextHost.PropertyChanged += OnPropertyContextChanged;
    }
    public ObservableCollection<ReservationDto> Reservations { get; } = new();
    public ObservableCollection<HotelRoomTypeDto> RoomTypes { get; } = new();
    public ObservableCollection<ReservationRatePlanOption> RatePlans { get; } = new();
    public ObservableCollection<ReservationCommercialOption> BookingSourceOptions { get; } = new();
    public ObservableCollection<ReservationCommercialOption> BookingChannelOptions { get; } = new();
    public ObservableCollection<ReservationCommercialOption> MarketSegmentOptions { get; } = new();
    public ObservableCollection<ReservationCommercialOption> CompanyOptions { get; } = new();
    public ObservableCollection<ReservationCommercialOption> TravelAgentOptions { get; } = new();
    public ObservableCollection<ReservationCommercialOption> CommissionRuleOptions { get; } = new();
    public ObservableCollection<HotelRoomDto> AssignableRooms { get; } = new();
    public ObservableCollection<ReservationStatus?> StatusFilters { get; } = new(AllStatusFilterValues);
    public ObservableCollection<string?> SourceFilters { get; } = new([null, "Direct", "Online", "OTA", "Phone", "Corporate", "WalkIn"]);
    public ObservableCollection<string> BookingSources { get; } = new(["Direct", "Online", "OTA", "Phone", "Corporate", "WalkIn"]);
    [ObservableProperty] private ReservationDto? selectedReservation;
    [ObservableProperty] private HotelRoomTypeDto? selectedRoomType;
    [ObservableProperty] private ReservationRatePlanOption? selectedRatePlan;
    [ObservableProperty] private ReservationCommercialOption? selectedBookingSource;
    [ObservableProperty] private ReservationCommercialOption? selectedBookingChannel;
    [ObservableProperty] private ReservationCommercialOption? selectedMarketSegment;
    [ObservableProperty] private ReservationCommercialOption? selectedCompany;
    [ObservableProperty] private ReservationCommercialOption? selectedTravelAgent;
    [ObservableProperty] private ReservationCommercialOption? selectedCommissionRule;
    [ObservableProperty] private HotelRoomDto? selectedRoom;
    [ObservableProperty] private ReservationStatus? selectedStatus;
    [ObservableProperty] private string? filterSource;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private DateTime? filterArrivalFrom;
    [ObservableProperty] private DateTime? filterArrivalTo;
    [ObservableProperty] private string guestName = string.Empty;
    [ObservableProperty] private string guestPhone = string.Empty;
    [ObservableProperty] private string guestEmail = string.Empty;
    [ObservableProperty] private DateTime? arrivalDate = DateTime.Today;
    [ObservableProperty] private DateTime? departureDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private int adults = 1;
    [ObservableProperty] private int children;
    [ObservableProperty] private string source = "Direct";
    [ObservableProperty] private string? specialRequests;
    [ObservableProperty] private string? internalNotes;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool isRoomPanelOpen;
    [ObservableProperty] private bool isCreateMode;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private string selectedPageSize = "50 / trang";
    [ObservableProperty] private int totalCount;
    public ObservableCollection<string> PageSizeOptions { get; } = new(["20 / trang", "50 / trang", "100 / trang"]);
    public ObservableCollection<TablePageButton> PageButtons { get; } = new();
    public TablePagerViewModel<HotelRoomDto> AssignableRoomsPager { get; } = new() { ItemName = "phòng", SelectedPageSize = "20 / trang" };
    public int PageSize => ParsePageSize(SelectedPageSize);
    public bool HasPropertyContext => propertyContextHost.HasCurrentProperty && service.HasContext;
    public bool HasRatePlanRepository => rateRepository is not null;
    public bool HasSelectedRatePlan => SelectedRatePlan is not null;
    public bool CanView => HasPropertyContext && service.CanView;
    public bool CanCreate => HasPropertyContext && service.CanCreate;
    public bool CanUpdate => HasPropertyContext && service.CanUpdate;
    public bool CanAssignRoom => HasPropertyContext && service.CanAssignRoom;
    public bool CanChangeStatus => HasPropertyContext && service.CanChangeStatus;
    public bool CanCancel => HasPropertyContext && service.CanCancel;
    public bool HasSelection => SelectedReservation is not null;
    public bool CanCheckInSelection => !IsSaving && SelectedReservation?.Status is ReservationStatus.Confirmed or ReservationStatus.Guaranteed;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));
    public string PageText => TotalCount == 0
        ? "Hi\u1EC3n th\u1ECB 0 tr\u00EAn t\u1ED5ng 0 \u0111\u1EB7t ph\u00F2ng"
        : $"Hi\u1EC3n th\u1ECB {(CurrentPage - 1) * PageSize + 1}\u2013{Math.Min(CurrentPage * PageSize, TotalCount)} tr\u00EAn t\u1ED5ng {TotalCount} \u0111\u1EB7t ph\u00F2ng";
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool CanSave => !IsSaving && (IsCreateMode ? CanCreate : CanUpdate);
    public bool CanEditDetails => !IsSaving && (IsCreateMode ? CanCreate : CanUpdate);
    public bool CanChangeStayAndGuests => !IsSaving && HasSelection && CanUpdate;
    public bool CanManageRoom => !IsSaving && HasSelection && CanAssignRoom;
    public bool CanUnassignRoom => CanManageRoom && _current?.AssignedRoomId is not null;
    public bool CanMarkTentative => !IsSaving && CanChangeStatus && _current?.Status == ReservationStatus.Draft;
    public bool CanConfirm => !IsSaving && CanChangeStatus && _current?.Status is ReservationStatus.Draft or ReservationStatus.Tentative;
    public string ConfirmationReadinessMessage => _current?.Status switch
    {
        ReservationStatus.Draft or ReservationStatus.Tentative => CanChangeStatus
            ? "Đặt phòng đang chờ xác nhận và có thể xác nhận."
            : "Tài khoản hiện tại không có quyền xác nhận đặt phòng.",
        ReservationStatus.Confirmed => "Đặt phòng đã được xác nhận; không thể xác nhận lần nữa.",
        ReservationStatus.Guaranteed => "Đặt phòng đã được bảo đảm; không thể xác nhận lần nữa.",
        _ => $"Không thể xác nhận đặt phòng ở trạng thái {StatusText}."
    };
    public bool CanCancelReservation => !IsSaving && CanCancel && _current?.Status is ReservationStatus.Draft or ReservationStatus.Tentative or ReservationStatus.Confirmed;
    public bool CanNoShow => !IsSaving && CanChangeStatus && _current?.Status == ReservationStatus.Confirmed;
    public bool HasData => Reservations.Count > 0;
    public bool IsEmpty => !IsLoading && !HasData;
    public int Nights => ArrivalDate is not null && DepartureDate is not null ? Math.Max(0, (DepartureDate.Value.Date - ArrivalDate.Value.Date).Days) : 0;
    public string StatusText => _current?.Status.ToString() ?? "New";
    public string RoomText => _current?.AssignedRoomId is not null ? "Đã phân phòng" : "Chưa phân phòng";

    partial void OnArrivalDateChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(Nights));
        _ = LoadRatePlansAsync();
    }
    partial void OnDepartureDateChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(Nights));
        _ = LoadRatePlansAsync();
    }
    partial void OnSelectedRoomTypeChanged(HotelRoomTypeDto? value) => _ = LoadRatePlansAsync();
    partial void OnSelectedRatePlanChanged(ReservationRatePlanOption? value) => OnPropertyChanged(nameof(HasSelectedRatePlan));
    partial void OnSelectedBookingSourceChanged(ReservationCommercialOption? value)
    {
        if (value is not null && !string.Equals(Source, value.Code, StringComparison.Ordinal)) Source = value.Code;
    }
    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(PageText));
        RebuildPageButtons();
    }
    partial void OnSelectedPageSizeChanged(string value)
    {
        CurrentPage = 1;
        OnPropertyChanged(nameof(PageSize));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageText));
        _ = LoadAsync();
    }
    partial void OnSearchTextChanged(string value) => StartDebouncedSearch();
    partial void OnSelectedStatusChanged(ReservationStatus? value) { if (!_suppressStatusSearch) StartDebouncedSearch(); }
    partial void OnFilterSourceChanged(string? value) => StartDebouncedSearch();
    partial void OnFilterArrivalFromChanged(DateTime? value) => StartDebouncedSearch();
    partial void OnFilterArrivalToChanged(DateTime? value) => StartDebouncedSearch();
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInfoMessageChanged(string value) => OnPropertyChanged(nameof(HasInfo));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnIsSavingChanged(bool value) => NotifyState();
    partial void OnIsCreateModeChanged(bool value) => NotifyState();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!HasPropertyContext) { ErrorMessage = ReservationErrorMessageMapper.ToMessage(ASMILE.BO.Shared.Foundation.Error.Authorization(propertyContextHost.ErrorCode ?? "FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED", "Property context is required.")); return; }
        if (!CanView) { ErrorMessage = "Bạn không có quyền xem đặt phòng."; return; }
        await _loadGate.WaitAsync();
        try
        {
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _searchCancellation = new CancellationTokenSource();
            var ct = _searchCancellation.Token;
            IsLoading = true; ErrorMessage = string.Empty;
            var result = await service.SearchAsync(SearchText, SelectedStatus, FilterArrivalFrom is null ? null : DateOnly.FromDateTime(FilterArrivalFrom.Value), FilterArrivalTo is null ? null : DateOnly.FromDateTime(FilterArrivalTo.Value), CurrentPage, PageSize, ct, FilterSource);
            if (result.IsFailure) { ErrorMessage = ReservationErrorMessageMapper.ToMessage(result.Error); return; }
            Reservations.Clear(); foreach (var item in result.Value!.Items) Reservations.Add(item); TotalCount = result.Value.Total;
            OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(IsEmpty)); OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(PageText)); RebuildPageButtons();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ErrorMessage = $"Không thể tải dữ liệu: {ex.Message}"; }
        finally
        {
            IsLoading = false;
            _loadGate.Release();
        }
    }
    [RelayCommand] public async Task RefreshAsync() => await LoadAsync();
    public async Task RefreshWithStatusAsync(ReservationStatus? status, Guid? selectReservationId = null)
    {
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = null;
        _suppressStatusSearch = true;
        try { SelectedStatus = status; }
        finally { _suppressStatusSearch = false; }
        CurrentPage = 1;
        await LoadAsync();
        if (selectReservationId is { } reservationId)
            SelectedReservation = Reservations.FirstOrDefault(x => x.ReservationId == reservationId);
    }
    [RelayCommand] public async Task SearchAsync() { CurrentPage = 1; await LoadAsync(); }
    [RelayCommand] public async Task NextPageAsync() { if (HasNextPage) { CurrentPage++; await LoadAsync(); } }
    [RelayCommand] public async Task PreviousPageAsync() { if (HasPreviousPage) { CurrentPage--; await LoadAsync(); } }
    [RelayCommand] public async Task GoToPageAsync(int? page) { if (page is { } target && target != CurrentPage && target >= 1 && target <= TotalPages) { CurrentPage = target; await LoadAsync(); } }
    [RelayCommand] public async Task ClearFiltersAsync() { SearchText = string.Empty; SelectedStatus = null; FilterSource = null; FilterArrivalFrom = null; FilterArrivalTo = null; CurrentPage = 1; await LoadAsync(); }
    [RelayCommand]
    public async Task NewAsync()
    {
        if (!CanCreate) { ErrorMessage = "Bạn không có quyền tạo đặt phòng."; return; }
        IsCreateMode = true; _current = null; SelectedReservation = null; GuestName = string.Empty; GuestPhone = string.Empty; GuestEmail = string.Empty; Adults = 1; Children = 0; Source = "Direct"; SpecialRequests = null; InternalNotes = null; SelectedRoomType = null; SelectedRatePlan = null; SelectedBookingSource = BookingSourceOptions.FirstOrDefault(x => string.Equals(x.Code, Source, StringComparison.OrdinalIgnoreCase)); SelectedBookingChannel = null; SelectedMarketSegment = null; SelectedCompany = null; SelectedTravelAgent = null; SelectedCommissionRule = null; ArrivalDate = DateTime.Today; DepartureDate = DateTime.Today.AddDays(1); ErrorMessage = string.Empty; InfoMessage = string.Empty;
        await LoadCommercialProfilesAsync(CancellationToken.None);
        SelectedBookingSource ??= BookingSourceOptions.FirstOrDefault();
        await LoadRoomTypesAsync(); NotifyState();
    }
    [RelayCommand] public void CancelEdit() { IsCreateMode = false; ErrorMessage = string.Empty; InfoMessage = string.Empty; if (_current is not null) CopyFrom(_current); NotifyState(); }
    [RelayCommand]
    public async Task SaveAsync()
    {
        if (IsSaving) return;
        if (_current is null && !CanCreate) { ErrorMessage = "Bạn không có quyền tạo đặt phòng hoặc chưa chọn cơ sở."; return; }
        if (_current is not null && !CanUpdate) { ErrorMessage = "Bạn không có quyền cập nhật đặt phòng."; return; }
        if (ArrivalDate is null || DepartureDate is null) { ErrorMessage = "Vui lòng nhập đủ ngày đến và ngày đi."; return; }
        if (DepartureDate.Value.Date <= ArrivalDate.Value.Date) { ErrorMessage = "Ngày đi phải sau ngày đến."; return; }
        if (string.IsNullOrWhiteSpace(GuestName)) { ErrorMessage = "Vui lòng nhập tên khách."; return; }
        if (Adults < 1 || Children < 0) { ErrorMessage = "Số người lớn phải từ 1 trở lên và số trẻ em không được âm."; return; }
        IsSaving = true; ErrorMessage = string.Empty;
        var creating = _current is null;
        try
        {
            var result = creating
                ? await service.CreateAsync(GuestName.Trim(), GuestPhone, GuestEmail, null, DateOnly.FromDateTime(ArrivalDate.Value), DateOnly.FromDateTime(DepartureDate.Value), Adults, Children, SelectedRoomType?.HotelRoomTypeId, Source, CancellationToken.None, confirmImmediately: true, ratePlanId: SelectedRatePlan?.RatePlanId, bookingSourceProfileId: SelectedBookingSource?.Id, bookingChannelProfileId: SelectedBookingChannel?.Id, marketSegmentProfileId: SelectedMarketSegment?.Id, companyProfileId: SelectedCompany?.Id, travelAgentProfileId: SelectedTravelAgent?.Id, commissionRuleProfileId: SelectedCommissionRule?.Id)
                : await service.UpdateAsync(_current!, new ReservationEditRequest(
                    GuestName.Trim(), GuestPhone, GuestEmail, SelectedRoomType?.HotelRoomTypeId ?? _current!.RoomTypeId, SelectedRatePlan?.RatePlanId ?? _current!.RatePlanId,
                    SpecialRequests, InternalNotes, DateOnly.FromDateTime(ArrivalDate.Value), DateOnly.FromDateTime(DepartureDate.Value),
                    Adults, Children, Source, SelectedMarketSegment?.Name, SelectedCompany?.Name, SelectedTravelAgent?.Name, _current?.IsVip,
                    SelectedBookingSource?.Id, SelectedBookingChannel?.Id, SelectedMarketSegment?.Id, SelectedCompany?.Id, SelectedTravelAgent?.Id, SelectedCommissionRule?.Id), CancellationToken.None);
            if (result.IsFailure) { HandleFailure(result.Error); return; }
            Apply(result.Value!); IsCreateMode = false; InfoMessage = "Đã lưu đặt phòng.";
        }
        catch (Exception ex) { ErrorMessage = $"Không thể lưu đặt phòng: {ex.Message}"; }
        finally { IsSaving = false; }
    }
    public async Task<bool> RecordDepositAsync(decimal amount, DateOnly dueDate)
    {
        if (_current is null || !CanUpdate || IsSaving)
        {
            ErrorMessage = "Không thể ghi nhận tiền cọc khi đặt phòng chưa được lưu hoặc tài khoản không có quyền cập nhật.";
            return false;
        }
        if (amount <= 0)
        {
            ErrorMessage = "Số tiền cọc phải lớn hơn 0.";
            return false;
        }

        IsSaving = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await service.RecordDepositAsync(_current, amount, dueDate, CancellationToken.None);
            if (result.IsFailure)
            {
                HandleFailure(result.Error);
                return false;
            }

            Apply(result.Value!);
            InfoMessage = "Đã ghi nhận yêu cầu tiền cọc. Việc thu tiền thực tế thực hiện tại Thu ngân/Folio.";
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể ghi nhận tiền cọc: {ex.Message}";
            return false;
        }
        finally { IsSaving = false; }
    }
    public async Task<bool> OpenForEditAsync(Guid reservationId)
    {
        if (!HasPropertyContext || !CanView)
        {
            ErrorMessage = "Bạn không có quyền xem đặt phòng hoặc chưa chọn cơ sở.";
            return false;
        }

        await _loadGate.WaitAsync();
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            var result = await service.GetAsync(reservationId, CancellationToken.None);
            if (result.IsFailure)
            {
                HandleFailure(result.Error);
                return false;
            }

            Apply(result.Value!);
            IsCreateMode = false;
            await LoadRoomTypesAsync();
            await LoadCommercialProfilesAsync(CancellationToken.None);
            SelectedReservationDetail = result.Value;
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải đặt phòng để sửa: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            _loadGate.Release();
        }
    }
    [RelayCommand] public async Task SaveStayAsync() => await RunMutationAsync(x => service.ChangeStayAsync(x, DateOnly.FromDateTime(ArrivalDate!.Value), DateOnly.FromDateTime(DepartureDate!.Value), CancellationToken.None));
    [RelayCommand] public async Task SaveGuestsAsync() => await RunMutationAsync(x => service.ChangeGuestsAsync(x, Adults, Children, CancellationToken.None));
    [RelayCommand]
    public async Task OpenRoomPanelAsync()
    {
        if (_current is null || !CanAssignRoom) return;
        await _loadGate.WaitAsync();
        try
        {
            IsRoomPanelOpen = true; ErrorMessage = string.Empty; SelectedRoom = null; AssignableRooms.Clear(); AssignableRoomsPager.SetSource(Array.Empty<HotelRoomDto>());
            if (_current.RoomTypeId is null) { ErrorMessage = "Đặt phòng chưa có loại phòng để phân phòng."; return; }
            var result = await service.GetAssignableRoomsAsync(_current.RoomTypeId.Value, CancellationToken.None);
            if (result.IsFailure) { HandleFailure(result.Error); return; }
            foreach (var room in result.Value!.Items) AssignableRooms.Add(room);
            AssignableRoomsPager.SetSource(result.Value.Items);
        }
        finally
        {
            _loadGate.Release();
        }
    }
    [RelayCommand] public void CloseRoomPanel() { IsRoomPanelOpen = false; SelectedRoom = null; }
    [RelayCommand]
    public async Task AssignRoomAsync()
    {
        if (_current is null || SelectedRoom is null || IsSaving) return;
        ErrorMessage = string.Empty;
        var conflict = await service.CheckRoomConflictAsync(_current, SelectedRoom.HotelRoomId, CancellationToken.None);
        if (conflict.IsFailure) { HandleFailure(conflict.Error); return; }
        if (conflict.Value!.HasConflict) { ErrorMessage = "Phòng không còn phù hợp trong khoảng ngày đã chọn. Hãy chọn phòng khác."; return; }
        await RunMutationAsync(x => service.AssignAsync(x, SelectedRoom.HotelRoomId, CancellationToken.None));
        if (string.IsNullOrEmpty(ErrorMessage)) CloseRoomPanel();
    }
    [RelayCommand] public async Task UnassignRoomAsync() => await RunMutationAsync(x => service.UnassignAsync(x, CancellationToken.None));
    [RelayCommand] public async Task TentativeAsync() => await RunMutationAsync(x => service.TentativeAsync(x, CancellationToken.None));
    [RelayCommand]
    public async Task ConfirmAsync() => await RunMutationAsync(
        x => service.ConfirmAsync(x, CancellationToken.None),
        updated => $"Đã xác nhận đặt phòng {updated.ReservationNumber}.");
    [ObservableProperty] private string cancellationReason = string.Empty;

    [RelayCommand] public async Task CancelReservationAsync() => await RunMutationAsync(x => service.CancelAsync(x, string.IsNullOrWhiteSpace(CancellationReason) ? "Cancelled by front office" : CancellationReason, CancellationToken.None));
    [RelayCommand] public async Task NoShowAsync() => await RunMutationAsync(x => service.NoShowAsync(x, CancellationToken.None));
    [RelayCommand] public async Task ReloadAfterConflictAsync() { if (_current is not null) await OpenAsync(_current); }

    private async Task OpenAsync(ReservationDto selected)
    {
        // Selection detail and list refresh share the scoped reservation service
        // (and therefore its DbContext). Serialize both read paths so a fast
        // click while the list is still loading cannot start a second EF query
        // on the same context.
        await _loadGate.WaitAsync();
        try
        {
            IsLoading = true; ErrorMessage = string.Empty;
            var result = await service.GetAsync(selected.ReservationId, CancellationToken.None);
            if (result.IsFailure) { HandleFailure(result.Error); return; }
            var detail = result.Value!;
            Apply(detail);
            SelectedReservationDetail = detail;
            await LoadRoomTypesAsync();
            SelectedRoomTypeName = RoomTypes.FirstOrDefault(x => x.HotelRoomTypeId == detail.RoomTypeId)?.Name;
            OnPropertyChanged(nameof(SelectedRoomTypeName));
            OnPropertyChanged(nameof(SelectedRoomNumber));
        }
        finally
        {
            IsLoading = false;
            _loadGate.Release();
        }
    }
    private async Task LoadRoomTypesAsync()
    {
        var result = await service.GetActiveRoomTypesAsync(CancellationToken.None); if (result.IsFailure) { HandleFailure(result.Error); return; }
        RoomTypes.Clear(); foreach (var type in result.Value!.Items) RoomTypes.Add(type);
        if (_current?.RoomTypeId is { } roomTypeId) SelectedRoomType = RoomTypes.FirstOrDefault(x => x.HotelRoomTypeId == roomTypeId);
        await LoadRatePlansAsync();
    }

    private async Task LoadCommercialProfilesAsync(CancellationToken ct)
    {
        if (hotelRepository is null || propertyContextHost.CurrentProperty is not { } property)
            return;

        var propertyId = property.PropertyId;
        var page = new PageRequest(1, 200);
        var sources = await hotelRepository.SearchBookingSourceProfilesAsync(propertyId, null, null, true, page, "kind", ct);
        var channels = await hotelRepository.SearchBookingChannelProfilesAsync(propertyId, null, null, true, page, "kind", ct);
        var segments = await hotelRepository.SearchMarketSegmentProfilesAsync(propertyId, null, null, true, page, "kind", ct);
        var companies = await hotelRepository.SearchCompanyProfilesAsync(propertyId, null, null, true, page, "code", ct);
        var agents = await hotelRepository.SearchTravelAgentProfilesAsync(propertyId, null, null, true, page, "code", ct);
        var commissions = await hotelRepository.SearchCommissionRuleProfilesAsync(propertyId, null, null, true, page, "partner", ct);

        BookingSourceOptions.Clear();
        foreach (var item in sources.Items)
            BookingSourceOptions.Add(new ReservationCommercialOption(item.HotelBookingSourceProfileId, item.Code, item.Name));
        BookingChannelOptions.Clear();
        foreach (var item in channels.Items)
            BookingChannelOptions.Add(new ReservationCommercialOption(item.HotelBookingChannelProfileId, item.Code, item.Name));
        MarketSegmentOptions.Clear();
        foreach (var item in segments.Items)
            MarketSegmentOptions.Add(new ReservationCommercialOption(item.HotelMarketSegmentProfileId, item.Code, item.Name));
        CompanyOptions.Clear();
        foreach (var item in companies.Items)
            CompanyOptions.Add(new ReservationCommercialOption(item.HotelCompanyProfileId, item.Code, item.Name));
        TravelAgentOptions.Clear();
        foreach (var item in agents.Items)
            TravelAgentOptions.Add(new ReservationCommercialOption(item.HotelTravelAgentProfileId, item.Code, item.Name));
        CommissionRuleOptions.Clear();
        foreach (var item in commissions.Items)
            CommissionRuleOptions.Add(new ReservationCommercialOption(item.HotelCommissionRuleProfileId, item.Code, item.Name));

        SelectedBookingSource = FindOption(BookingSourceOptions, _current?.BookingSourceProfileId, Source);
        SelectedBookingChannel = FindOption(BookingChannelOptions, _current?.BookingChannelProfileId, null);
        SelectedMarketSegment = FindOption(MarketSegmentOptions, _current?.MarketSegmentProfileId, _current?.MarketSegment);
        SelectedCompany = FindOption(CompanyOptions, _current?.CompanyProfileId, _current?.CompanyName);
        SelectedTravelAgent = FindOption(TravelAgentOptions, _current?.TravelAgentProfileId, _current?.TravelAgentName);
        SelectedCommissionRule = FindOption(CommissionRuleOptions, _current?.CommissionRuleProfileId, null);
    }

    private static ReservationCommercialOption? FindOption(
        IEnumerable<ReservationCommercialOption> options,
        Guid? id,
        string? codeOrName)
    {
        if (id is { } value)
            return options.FirstOrDefault(x => x.Id == value);
        if (string.IsNullOrWhiteSpace(codeOrName))
            return null;
        return options.FirstOrDefault(x => string.Equals(x.Code, codeOrName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Name, codeOrName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task LoadRatePlansAsync()
    {
        _ratePlanCancellation?.Cancel();
        _ratePlanCancellation?.Dispose();
        _ratePlanCancellation = new CancellationTokenSource();
        var ct = _ratePlanCancellation.Token;

        await _ratePlanLoadGate.WaitAsync();
        try
        {
            RatePlans.Clear();
            SelectedRatePlan = null;
            if (rateRepository is null || propertyContextHost.CurrentProperty is not { } property || SelectedRoomType is null || ArrivalDate is null)
                return;

            var plans = await rateRepository.GetAllAsync(
                property.PropertyId,
                new RatePlanSearchFilter(property.PropertyId, RatePlanStatus.Active, null, null),
                ct);
            var arrival = DateOnly.FromDateTime(ArrivalDate.Value);
            foreach (var plan in plans
                .Where(x => x.RoomTypeId == SelectedRoomType.HotelRoomTypeId
                    && DateOnly.FromDateTime(x.EffectiveFrom) <= arrival
                    && DateOnly.FromDateTime(x.EffectiveTo) >= arrival)
                .OrderBy(x => x.Hierarchy)
                .ThenBy(x => x.PlanCode))
            {
                RatePlans.Add(new ReservationRatePlanOption(
                    plan.RatePlanId,
                    plan.PlanCode,
                    plan.PlanName,
                    plan.BaseRate,
                    plan.EffectiveFrom,
                    plan.EffectiveTo));
            }

            SelectedRatePlan = RatePlans.FirstOrDefault(x => x.RatePlanId == _current?.RatePlanId) ?? RatePlans.FirstOrDefault();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải kế hoạch giá: {ex.Message}";
        }
        finally
        {
            _ratePlanLoadGate.Release();
        }
    }
    private async Task RunMutationAsync(
        Func<ReservationDto, Task<ASMILE.BO.Shared.Foundation.Result<ReservationDto>>> action,
        Func<ReservationDto, string>? successMessage = null)
    {
        if (_current is null || IsSaving) return; IsSaving = true; ErrorMessage = string.Empty;
        try
        {
            var result = await action(_current);
            if (result.IsFailure) { HandleFailure(result.Error); return; }
            Apply(result.Value!);
            InfoMessage = successMessage?.Invoke(result.Value!) ?? "Đã cập nhật đặt phòng.";
            await LoadAsync();
        }
        finally { IsSaving = false; }
    }
    private void HandleFailure(ASMILE.BO.Shared.Foundation.Error? error)
    {
        ErrorMessage = ReservationErrorMessageMapper.ToMessage(error);
        if (error?.Code == "RESERVATION_CONCURRENCY_CONFLICT") InfoMessage = "Dữ liệu hiện tại của bạn chưa được lưu. Hãy tải lại đặt phòng trước khi tiếp tục.";
    }
    private void Apply(ReservationDto dto)
    {
        _current = dto;
        _applyingSelection = true;
        SelectedReservation = dto;
        _applyingSelection = false;
        CopyFrom(dto);
        NotifyState();
    }
    private void CopyFrom(ReservationDto dto)
    {
        GuestName = dto.GuestName;
        GuestPhone = dto.GuestPhone ?? string.Empty;
        GuestEmail = dto.GuestEmail ?? string.Empty;
        ArrivalDate = dto.ArrivalDate.ToDateTime(TimeOnly.MinValue);
        DepartureDate = dto.DepartureDate.ToDateTime(TimeOnly.MinValue);
        Adults = dto.AdultCount;
        Children = dto.ChildCount;
        Source = string.IsNullOrWhiteSpace(dto.Source) ? "Direct" : dto.Source;
        SpecialRequests = dto.SpecialRequests;
        InternalNotes = dto.InternalNotes;
        SelectedBookingSource = FindOption(BookingSourceOptions, dto.BookingSourceProfileId, dto.Source);
        SelectedBookingChannel = FindOption(BookingChannelOptions, dto.BookingChannelProfileId, null);
        SelectedMarketSegment = FindOption(MarketSegmentOptions, dto.MarketSegmentProfileId, dto.MarketSegment);
        SelectedCompany = FindOption(CompanyOptions, dto.CompanyProfileId, dto.CompanyName);
        SelectedTravelAgent = FindOption(TravelAgentOptions, dto.TravelAgentProfileId, dto.TravelAgentName);
        SelectedCommissionRule = FindOption(CommissionRuleOptions, dto.CommissionRuleProfileId, null);
        OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(RoomText));
    }
    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanCheckInSelection));
        OnPropertyChanged(nameof(ConfirmationReadinessMessage));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(RoomText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanEditDetails));
        OnPropertyChanged(nameof(CanChangeStayAndGuests));
        OnPropertyChanged(nameof(CanManageRoom));
        OnPropertyChanged(nameof(CanUnassignRoom));
        OnPropertyChanged(nameof(CanMarkTentative));
        OnPropertyChanged(nameof(CanConfirm));
        OnPropertyChanged(nameof(CanCancelReservation));
        OnPropertyChanged(nameof(CanNoShow));
        OnPropertyChanged(nameof(HasRatePlanRepository));
        OnPropertyChanged(nameof(HasSelectedRatePlan));
    }

    private void UpdateStatusFilters(WorkspaceMode mode)
    {
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = null;

        _suppressStatusSearch = true;
        try
        {
            StatusFilters.Clear();
            foreach (var status in mode == WorkspaceMode.Confirmation
                         ? ConfirmationStatusFilterValues
                         : AllStatusFilterValues)
            {
                StatusFilters.Add(status);
            }

            if (mode == WorkspaceMode.Confirmation
                && SelectedStatus is not (ReservationStatus.Draft or ReservationStatus.Tentative))
            {
                SelectedStatus = ReservationStatus.Draft;
            }
        }
        finally
        {
            _suppressStatusSearch = false;
        }
    }

    private void StartDebouncedSearch()
    {
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = new CancellationTokenSource();
        _ = DebouncedSearchAsync(_searchDebounce.Token);
    }

    private async Task DebouncedSearchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(350), cancellationToken);
            if (cancellationToken.IsCancellationRequested || !CanView) return;
            CurrentPage = 1;
            await LoadAsync();
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        propertyContextHost.PropertyChanged -= OnPropertyContextChanged;
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _ratePlanCancellation?.Cancel();
        _ratePlanCancellation?.Dispose();
        _loadGate.Dispose();
        _ratePlanLoadGate.Dispose();
    }

    private async void OnPropertyContextChanged(object? sender, FrontOfficePropertyContextChangedEventArgs e)
    {
        try
        {
            _searchDebounce?.Cancel();
            _searchCancellation?.Cancel();
            service.ResetForContextChange();
            ClearForPropertyChange();
            NotifyContextState();
            if (e.Current is not null)
            {
                await LoadAsync();
            }
            else
            {
                ErrorMessage = ReservationErrorMessageMapper.ToMessage(ASMILE.BO.Shared.Foundation.Error.Authorization(e.ErrorCode ?? "FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED", "Property context is required."));
            }
        }
        catch (OperationCanceledException) { }
    }

    private void ClearForPropertyChange()
    {
        Reservations.Clear();
        RoomTypes.Clear();
        RatePlans.Clear();
        BookingSourceOptions.Clear(); BookingChannelOptions.Clear(); MarketSegmentOptions.Clear(); CompanyOptions.Clear(); TravelAgentOptions.Clear(); CommissionRuleOptions.Clear();
        AssignableRooms.Clear();
        AssignableRoomsPager.SetSource(Array.Empty<HotelRoomDto>());
        _current = null;
        ClearDetail();
        _applyingSelection = true;
        SelectedReservation = null;
        _applyingSelection = false;
        SelectedRoom = null;
        SelectedRoomType = null;
        SelectedRatePlan = null;
        SelectedBookingSource = null; SelectedBookingChannel = null; SelectedMarketSegment = null; SelectedCompany = null; SelectedTravelAgent = null; SelectedCommissionRule = null;
        IsRoomPanelOpen = false;
        IsCreateMode = false;
        TotalCount = 0;
        CurrentPage = 1;
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageText));
        RebuildPageButtons();
        InfoMessage = string.Empty;
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void RebuildPageButtons()
    {
        PageButtons.Clear();
        foreach (var page in BuildPageIndexes(CurrentPage, TotalPages))
        {
            PageButtons.Add(page is null
                ? new TablePageButton("…", null, false, true)
                : new TablePageButton(page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), page.Value, page.Value == CurrentPage, false));
        }
    }

    private static IEnumerable<int?> BuildPageIndexes(int currentPage, int totalPages)
    {
        if (totalPages <= 5)
        {
            for (var page = 1; page <= totalPages; page++) yield return page;
            yield break;
        }

        if (currentPage <= 3)
        {
            for (var page = 1; page <= 5; page++) yield return page;
            yield return null;
            yield return totalPages;
            yield break;
        }

        if (currentPage >= totalPages - 2)
        {
            yield return 1;
            yield return null;
            for (var page = totalPages - 4; page <= totalPages; page++) yield return page;
            yield break;
        }

        yield return 1;
        yield return null;
        yield return currentPage - 1;
        yield return currentPage;
        yield return currentPage + 1;
        yield return null;
        yield return totalPages;
    }

    private static int ParsePageSize(string? value)
    {
        var first = value?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(first, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed) && parsed > 0 ? parsed : 50;
    }

    private void NotifyContextState()
    {
        OnPropertyChanged(nameof(HasPropertyContext));
        OnPropertyChanged(nameof(CanView));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanUpdate));
        OnPropertyChanged(nameof(CanAssignRoom));
        OnPropertyChanged(nameof(CanChangeStatus));
        OnPropertyChanged(nameof(CanCancel));
        NotifyState();
    }
}
