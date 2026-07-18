using ASMILE.BO.App.Services;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Reservation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class ReservationWorkspaceViewModel : ViewModelBase, IDisposable
{
    private readonly IReservationUiService service;
    private readonly IFrontOfficePropertyContextHost propertyContextHost;
    private CancellationTokenSource? _searchCancellation;
    private CancellationTokenSource? _searchDebounce;
    private ReservationDto? _current;
    private bool _applyingSelection;

    public ReservationWorkspaceViewModel(IReservationUiService service, IFrontOfficePropertyContextHost propertyContextHost)
    {
        this.service = service;
        this.propertyContextHost = propertyContextHost;
        propertyContextHost.PropertyChanged += OnPropertyContextChanged;
    }
    public ObservableCollection<ReservationDto> Reservations { get; } = new();
    public ObservableCollection<HotelRoomTypeDto> RoomTypes { get; } = new();
    public ObservableCollection<HotelRoomDto> AssignableRooms { get; } = new();
    public ObservableCollection<ReservationStatus?> StatusFilters { get; } = new([null, ReservationStatus.Draft, ReservationStatus.Tentative, ReservationStatus.Confirmed, ReservationStatus.Cancelled, ReservationStatus.NoShow]);
    [ObservableProperty] private ReservationDto? selectedReservation;
    [ObservableProperty] private HotelRoomTypeDto? selectedRoomType;
    [ObservableProperty] private HotelRoomDto? selectedRoom;
    [ObservableProperty] private ReservationStatus? selectedStatus;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private DateTime? filterArrivalFrom;
    [ObservableProperty] private DateTime? filterArrivalTo;
    [ObservableProperty] private string guestName = string.Empty;
    [ObservableProperty] private DateTime? arrivalDate = DateTime.Today;
    [ObservableProperty] private DateTime? departureDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private int adults = 1;
    [ObservableProperty] private int children;
    [ObservableProperty] private string source = "Direct";
    [ObservableProperty] private string? internalNotes;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool isRoomPanelOpen;
    [ObservableProperty] private bool isCreateMode;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalCount;
    public int PageSize => 50;
    public bool HasPropertyContext => propertyContextHost.HasCurrentProperty && service.HasContext;
    public bool CanView => HasPropertyContext && service.CanView;
    public bool CanCreate => HasPropertyContext && service.CanCreate;
    public bool CanUpdate => HasPropertyContext && service.CanUpdate;
    public bool CanAssignRoom => HasPropertyContext && service.CanAssignRoom;
    public bool CanChangeStatus => HasPropertyContext && service.CanChangeStatus;
    public bool CanCancel => HasPropertyContext && service.CanCancel;
    public bool HasSelection => _current is not null;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage * PageSize < TotalCount;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool CanSave => !IsSaving && (IsCreateMode ? CanCreate : CanUpdate);
    public bool CanEditDetails => !IsSaving && (IsCreateMode ? CanCreate : CanUpdate);
    public bool CanChangeStayAndGuests => !IsSaving && HasSelection && CanUpdate;
    public bool CanManageRoom => !IsSaving && HasSelection && CanAssignRoom;
    public bool CanUnassignRoom => CanManageRoom && _current?.AssignedRoomId is not null;
    public bool CanMarkTentative => !IsSaving && CanChangeStatus && _current?.Status == ReservationStatus.Draft;
    public bool CanConfirm => !IsSaving && CanChangeStatus && _current?.Status is ReservationStatus.Draft or ReservationStatus.Tentative;
    public bool CanCancelReservation => !IsSaving && CanCancel && _current?.Status is ReservationStatus.Draft or ReservationStatus.Tentative or ReservationStatus.Confirmed;
    public bool CanNoShow => !IsSaving && CanChangeStatus && _current?.Status == ReservationStatus.Confirmed;
    public bool HasData => Reservations.Count > 0;
    public bool IsEmpty => !IsLoading && !HasData;
    public int Nights => ArrivalDate is not null && DepartureDate is not null ? Math.Max(0, (DepartureDate.Value.Date - ArrivalDate.Value.Date).Days) : 0;
    public string StatusText => _current?.Status.ToString() ?? "New";
    public string RoomText => _current?.AssignedRoomId is not null ? "Da phan phong" : "Chua phan phong";

    partial void OnSelectedReservationChanged(ReservationDto? value)
    {
        if (_applyingSelection || value is null) return;
        _ = OpenAsync(value);
    }
    partial void OnArrivalDateChanged(DateTime? value) => OnPropertyChanged(nameof(Nights));
    partial void OnDepartureDateChanged(DateTime? value) => OnPropertyChanged(nameof(Nights));
    partial void OnCurrentPageChanged(int value) { OnPropertyChanged(nameof(HasPreviousPage)); OnPropertyChanged(nameof(HasNextPage)); }
    partial void OnSearchTextChanged(string value) => StartDebouncedSearch();
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInfoMessageChanged(string value) => OnPropertyChanged(nameof(HasInfo));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnIsSavingChanged(bool value) => NotifyState();
    partial void OnIsCreateModeChanged(bool value) => NotifyState();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!HasPropertyContext) { ErrorMessage = ReservationErrorMessageMapper.ToMessage(ASMILE.BO.Shared.Foundation.Error.Authorization(propertyContextHost.ErrorCode ?? "FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED", "Property context is required.")); return; }
        if (!CanView) { ErrorMessage = "Ban khong co quyen xem dat phong."; return; }
        _searchCancellation?.Cancel(); _searchCancellation = new CancellationTokenSource(); var ct = _searchCancellation.Token;
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            var result = await service.SearchAsync(SearchText, SelectedStatus, FilterArrivalFrom is null ? null : DateOnly.FromDateTime(FilterArrivalFrom.Value), FilterArrivalTo is null ? null : DateOnly.FromDateTime(FilterArrivalTo.Value), CurrentPage, PageSize, ct);
            if (result.IsFailure) { ErrorMessage = ReservationErrorMessageMapper.ToMessage(result.Error); return; }
            Reservations.Clear(); foreach (var item in result.Value!.Items) Reservations.Add(item); TotalCount = result.Value.Total; OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(IsEmpty)); OnPropertyChanged(nameof(HasNextPage));
        }
        catch (OperationCanceledException) { }
        finally { IsLoading = false; }
    }
    [RelayCommand] public async Task RefreshAsync() => await LoadAsync();
    [RelayCommand] public async Task SearchAsync() { CurrentPage = 1; await LoadAsync(); }
    [RelayCommand] public async Task NextPageAsync() { if (HasNextPage) { CurrentPage++; await LoadAsync(); } }
    [RelayCommand] public async Task PreviousPageAsync() { if (HasPreviousPage) { CurrentPage--; await LoadAsync(); } }
    [RelayCommand] public async Task ClearFiltersAsync() { SearchText = string.Empty; SelectedStatus = null; FilterArrivalFrom = null; FilterArrivalTo = null; CurrentPage = 1; await LoadAsync(); }
    [RelayCommand]
    public async Task NewAsync()
    {
        if (!CanCreate) { ErrorMessage = "Ban khong co quyen tao dat phong."; return; }
        IsCreateMode = true; _current = null; SelectedReservation = null; GuestName = string.Empty; Adults = 1; Children = 0; Source = "Direct"; InternalNotes = null; SelectedRoomType = null; ArrivalDate = DateTime.Today; DepartureDate = DateTime.Today.AddDays(1); ErrorMessage = string.Empty; InfoMessage = string.Empty;
        await LoadRoomTypesAsync(); NotifyState();
    }
    [RelayCommand] public void CancelEdit() { IsCreateMode = false; ErrorMessage = string.Empty; InfoMessage = string.Empty; if (_current is not null) CopyFrom(_current); NotifyState(); }
    [RelayCommand]
    public async Task SaveAsync()
    {
        if (IsSaving || (_current is null ? !CanCreate : !CanUpdate) || ArrivalDate is null || DepartureDate is null) return;
        if (string.IsNullOrWhiteSpace(GuestName)) { ErrorMessage = "Vui long nhap ten khach."; return; }
        IsSaving = true; ErrorMessage = string.Empty;
        try
        {
            var result = _current is null
                ? await service.CreateAsync(GuestName, null, DateOnly.FromDateTime(ArrivalDate.Value), DateOnly.FromDateTime(DepartureDate.Value), Adults, Children, SelectedRoomType?.HotelRoomTypeId, Source, CancellationToken.None)
                : await service.UpdateAsync(_current, GuestName, SelectedRoomType?.HotelRoomTypeId, InternalNotes, CancellationToken.None);
            if (result.IsFailure) { HandleFailure(result.Error); return; }
            Apply(result.Value!); IsCreateMode = false; InfoMessage = "Da luu dat phong."; await LoadAsync();
        }
        finally { IsSaving = false; }
    }
    [RelayCommand] public async Task SaveStayAsync() => await RunMutationAsync(x => service.ChangeStayAsync(x, DateOnly.FromDateTime(ArrivalDate!.Value), DateOnly.FromDateTime(DepartureDate!.Value), CancellationToken.None));
    [RelayCommand] public async Task SaveGuestsAsync() => await RunMutationAsync(x => service.ChangeGuestsAsync(x, Adults, Children, CancellationToken.None));
    [RelayCommand]
    public async Task OpenRoomPanelAsync()
    {
        if (_current is null || !CanAssignRoom) return;
        IsRoomPanelOpen = true; ErrorMessage = string.Empty; SelectedRoom = null; AssignableRooms.Clear();
        if (_current.RoomTypeId is null) { ErrorMessage = "Dat phong chua co loai phong de phan phong."; return; }
        var result = await service.GetAssignableRoomsAsync(_current.RoomTypeId.Value, CancellationToken.None);
        if (result.IsFailure) { HandleFailure(result.Error); return; }
        foreach (var room in result.Value!.Items) AssignableRooms.Add(room);
    }
    [RelayCommand] public void CloseRoomPanel() { IsRoomPanelOpen = false; SelectedRoom = null; }
    [RelayCommand]
    public async Task AssignRoomAsync()
    {
        if (_current is null || SelectedRoom is null || IsSaving) return;
        ErrorMessage = string.Empty;
        var conflict = await service.CheckRoomConflictAsync(_current, SelectedRoom.HotelRoomId, CancellationToken.None);
        if (conflict.IsFailure) { HandleFailure(conflict.Error); return; }
        if (conflict.Value!.HasConflict) { ErrorMessage = "Phong khong con phu hop trong khoang ngay da chon. Hay chon phong khac."; return; }
        await RunMutationAsync(x => service.AssignAsync(x, SelectedRoom.HotelRoomId, CancellationToken.None));
        if (string.IsNullOrEmpty(ErrorMessage)) CloseRoomPanel();
    }
    [RelayCommand] public async Task UnassignRoomAsync() => await RunMutationAsync(x => service.UnassignAsync(x, CancellationToken.None));
    [RelayCommand] public async Task TentativeAsync() => await RunMutationAsync(x => service.TentativeAsync(x, CancellationToken.None));
    [RelayCommand] public async Task ConfirmAsync() => await RunMutationAsync(x => service.ConfirmAsync(x, CancellationToken.None));
    [RelayCommand] public async Task CancelReservationAsync() => await RunMutationAsync(x => service.CancelAsync(x, "Cancelled by front office", CancellationToken.None));
    [RelayCommand] public async Task NoShowAsync() => await RunMutationAsync(x => service.NoShowAsync(x, CancellationToken.None));
    [RelayCommand] public async Task ReloadAfterConflictAsync() { if (_current is not null) await OpenAsync(_current); }

    private async Task OpenAsync(ReservationDto selected)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try { var result = await service.GetAsync(selected.ReservationId, CancellationToken.None); if (result.IsFailure) { HandleFailure(result.Error); return; } Apply(result.Value!); await LoadRoomTypesAsync(); }
        finally { IsLoading = false; }
    }
    private async Task LoadRoomTypesAsync()
    {
        var result = await service.GetActiveRoomTypesAsync(CancellationToken.None); if (result.IsFailure) { HandleFailure(result.Error); return; }
        RoomTypes.Clear(); foreach (var type in result.Value!.Items) RoomTypes.Add(type);
        if (_current?.RoomTypeId is { } roomTypeId) SelectedRoomType = RoomTypes.FirstOrDefault(x => x.HotelRoomTypeId == roomTypeId);
    }
    private async Task RunMutationAsync(Func<ReservationDto, Task<ASMILE.BO.Shared.Foundation.Result<ReservationDto>>> action)
    {
        if (_current is null || IsSaving) return; IsSaving = true; ErrorMessage = string.Empty;
        try { var result = await action(_current); if (result.IsFailure) { HandleFailure(result.Error); return; } Apply(result.Value!); InfoMessage = "Da cap nhat dat phong."; await LoadAsync(); }
        finally { IsSaving = false; }
    }
    private void HandleFailure(ASMILE.BO.Shared.Foundation.Error? error)
    {
        ErrorMessage = ReservationErrorMessageMapper.ToMessage(error);
        if (error?.Code == "RESERVATION_CONCURRENCY_CONFLICT") InfoMessage = "Du lieu hien tai cua ban chua duoc luu. Hay tai lai dat phong truoc khi tiep tuc.";
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
    private void CopyFrom(ReservationDto dto) { GuestName = dto.GuestName; ArrivalDate = dto.ArrivalDate.ToDateTime(TimeOnly.MinValue); DepartureDate = dto.DepartureDate.ToDateTime(TimeOnly.MinValue); Adults = dto.AdultCount; Children = dto.ChildCount; Source = dto.Source; OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(RoomText)); }
    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasSelection));
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
        service.Dispose();
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
        AssignableRooms.Clear();
        _current = null;
        _applyingSelection = true;
        SelectedReservation = null;
        _applyingSelection = false;
        SelectedRoom = null;
        SelectedRoomType = null;
        IsRoomPanelOpen = false;
        IsCreateMode = false;
        TotalCount = 0;
        CurrentPage = 1;
        InfoMessage = string.Empty;
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(IsEmpty));
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
