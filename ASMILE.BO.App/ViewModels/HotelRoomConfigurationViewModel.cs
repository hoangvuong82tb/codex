using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

/// <summary>Settings-only room master editor. Operational lookup remains in RoomMasterViewModel.</summary>
public sealed partial class HotelRoomConfigurationViewModel : ViewModelBase
{
    private readonly IHotelRoomMasterDataRepository? _roomRepository;
    private readonly IHotelStructureMasterDataRepository? _structureRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelRoomCommandHandler? _createHandler;
    private readonly UpdateHotelRoomCommandHandler? _updateHandler;
    private readonly ActivateHotelRoomCommandHandler? _activateHandler;
    private readonly DeactivateHotelRoomCommandHandler? _deactivateHandler;
    private bool _initialized;

    public HotelRoomConfigurationViewModel(IHotelRoomMasterDataRepository? roomRepository = null, IHotelStructureMasterDataRepository? structureRepository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelRoomCommandHandler? createHandler = null, UpdateHotelRoomCommandHandler? updateHandler = null, ActivateHotelRoomCommandHandler? activateHandler = null, DeactivateHotelRoomCommandHandler? deactivateHandler = null)
    { _roomRepository = roomRepository; _structureRepository = structureRepository; _contextAccessor = contextAccessor; _createHandler = createHandler; _updateHandler = updateHandler; _activateHandler = activateHandler; _deactivateHandler = deactivateHandler; }

    public ObservableCollection<HotelRoomDto> Rooms { get; } = [];
    public ObservableCollection<HotelRoomTypeDto> RoomTypes { get; } = [];
    public ObservableCollection<HotelBuildingDto> Buildings { get; } = [];
    public ObservableCollection<HotelFloorDto> Floors { get; } = [];
    public ObservableCollection<HotelAreaDto> Areas { get; } = [];
    [ObservableProperty] private HotelRoomDto? selectedRoom;
    [ObservableProperty] private HotelRoomTypeDto? selectedRoomType;
    [ObservableProperty] private HotelBuildingDto? selectedBuilding;
    [ObservableProperty] private HotelFloorDto? selectedFloor;
    [ObservableProperty] private HotelAreaDto? selectedArea;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string roomNumber = string.Empty;
    [ObservableProperty] private string displayName = string.Empty;
    [ObservableProperty] private string zone = string.Empty;
    [ObservableProperty] private string roomCondition = string.Empty;
    [ObservableProperty] private string view = string.Empty;
    [ObservableProperty] private string bedType = string.Empty;
    [ObservableProperty] private bool isSmoking;
    [ObservableProperty] private bool isConnecting;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedRoom is null ? "Chọn một phòng để xem và chỉnh sửa." : $"{SelectedRoom.RoomNumber} · {SelectedRoom.HotelRoomTypeName}";
    public string SelectedStatus => SelectedRoom?.IsActive == true ? "Đang khai thác" : "Tạm ngưng";

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty;
        try
        {
            if (_roomRepository is null || _structureRepository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty)
            { Rooms.Clear(); ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context để tải danh mục phòng."; return; }
            var types = await _roomRepository.SearchRoomTypesAsync(propertyId, null, true, new PageRequest(1, 200), "displayOrder", ct);
            RoomTypes.Clear(); foreach (var item in types.Items) RoomTypes.Add(item);
            var buildings = await _structureRepository.SearchBuildingsAsync(propertyId, null, true, new PageRequest(1, 200), "displayOrder", ct);
            Buildings.Clear(); foreach (var item in buildings.Items) Buildings.Add(item);
            var floors = await _structureRepository.SearchFloorsAsync(propertyId, null, null, true, new PageRequest(1, 500), "level", ct);
            Floors.Clear(); foreach (var item in floors.Items) Floors.Add(item);
            var areas = await _structureRepository.SearchAreasAsync(propertyId, null, null, null, true, new PageRequest(1, 500), "building", ct);
            Areas.Clear(); foreach (var item in areas.Items) Areas.Add(item);
            var rooms = await _roomRepository.SearchRoomsAsync(propertyId, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, null, null, null, true, new PageRequest(1, 500), "number", false, ct);
            Rooms.Clear(); foreach (var item in rooms.Items) Rooms.Add(item);
            if (SelectedRoom is null || Rooms.All(x => x.HotelRoomId != SelectedRoom.HotelRoomId)) SelectedRoom = Rooms.FirstOrDefault();
            InfoMessage = $"Đã tải {Rooms.Count} phòng từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được danh mục phòng: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewRoom() { SelectedRoom = null; SelectedRoomType = RoomTypes.FirstOrDefault(); SelectedBuilding = Buildings.FirstOrDefault(); SelectedFloor = Floors.FirstOrDefault(x => x.HotelBuildingId == SelectedBuilding?.HotelBuildingId); SelectedArea = Areas.FirstOrDefault(x => x.HotelBuildingId == SelectedBuilding?.HotelBuildingId); RoomNumber = string.Empty; DisplayName = string.Empty; Zone = string.Empty; RoomCondition = string.Empty; View = string.Empty; BedType = string.Empty; IsSmoking = false; IsConnecting = false; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void EditRoom() { if (SelectedRoom is null) return; SelectedRoomType = RoomTypes.FirstOrDefault(x => x.HotelRoomTypeId == SelectedRoom.HotelRoomTypeId); SelectedBuilding = Buildings.FirstOrDefault(x => string.Equals(x.Code, SelectedRoom.Building, StringComparison.OrdinalIgnoreCase)); SelectedFloor = Floors.FirstOrDefault(x => x.HotelBuildingId == SelectedBuilding?.HotelBuildingId && string.Equals(x.Code, SelectedRoom.Floor, StringComparison.OrdinalIgnoreCase)); SelectedArea = Areas.FirstOrDefault(x => x.HotelAreaId == SelectedRoom.HotelAreaId) ?? Areas.FirstOrDefault(x => string.Equals(x.Code, SelectedRoom.Zone, StringComparison.OrdinalIgnoreCase)); RoomNumber = SelectedRoom.RoomNumber; DisplayName = SelectedRoom.DisplayName ?? string.Empty; Zone = SelectedRoom.Zone ?? string.Empty; RoomCondition = SelectedRoom.RoomCondition ?? string.Empty; View = SelectedRoom.View ?? string.Empty; BedType = SelectedRoom.BedType ?? string.Empty; IsSmoking = SelectedRoom.IsSmoking; IsConnecting = SelectedRoom.IsConnecting; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(RoomNumber)) { ErrorMessage = "Số phòng là bắt buộc."; RaiseState(); return; }
        if (SelectedRoomType is null) { ErrorMessage = "Phải chọn hạng phòng."; RaiseState(); return; }
        if (SelectedBuilding is null || SelectedFloor is null) { ErrorMessage = "Phải chọn tòa nhà và tầng từ danh mục độc lập."; RaiseState(); return; }
        if (SelectedArea is not null && (SelectedArea.HotelBuildingId != SelectedBuilding.HotelBuildingId || (SelectedArea.HotelFloorId is not null && SelectedArea.HotelFloorId != SelectedFloor.HotelFloorId)))
        { ErrorMessage = "Khu vực phải thuộc đúng tòa nhà và tầng của phòng."; RaiseState(); return; }
        try
        {
            Result<HotelRoomDto> result;
            var building = SelectedBuilding.Code;
            var floor = SelectedFloor.Code;
            var areaId = SelectedArea?.HotelAreaId;
            if (SelectedRoom is null)
            {
                if (_createHandler is null) { ErrorMessage = "Chưa đăng ký command phòng thật."; return; }
                result = await _createHandler.HandleAsync(new CreateHotelRoomCommand(SelectedRoomType.HotelRoomTypeId, RoomNumber.Trim(), NullIfEmpty(DisplayName), floor, building, NullIfEmpty(Zone), null, NullIfEmpty(RoomCondition), NullIfEmpty(View), NullIfEmpty(BedType), IsSmoking, IsConnecting, areaId), ct);
            }
            else
            {
                if (_updateHandler is null) { ErrorMessage = "Chưa đăng ký command phòng thật."; return; }
                result = await _updateHandler.HandleAsync(new UpdateHotelRoomCommand(SelectedRoom.HotelRoomId, RoomNumber.Trim(), NullIfEmpty(DisplayName), floor, building, NullIfEmpty(Zone), null, SelectedRoom.RowVersion ?? [], NullIfEmpty(RoomCondition), NullIfEmpty(View), NullIfEmpty(BedType), IsSmoking, IsConnecting, areaId), ct);
            }
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được phòng."; return; }
            InfoMessage = SelectedRoom is null ? $"Đã thêm phòng {result.Value!.RoomNumber}." : $"Đã cập nhật phòng {result.Value!.RoomNumber}.";
            IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được phòng: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedRoom is null) return;
        var result = SelectedRoom.IsActive
            ? _deactivateHandler is null ? MissingCommand() : await _deactivateHandler.HandleAsync(new DeactivateHotelRoomCommand(SelectedRoom.HotelRoomId, SelectedRoom.RowVersion ?? []), ct)
            : _activateHandler is null ? MissingCommand() : await _activateHandler.HandleAsync(new ActivateHotelRoomCommand(SelectedRoom.HotelRoomId, SelectedRoom.RowVersion ?? []), ct);
        if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái phòng."; RaiseState(); return; }
        InfoMessage = result.Value!.IsActive ? "Đã bật phòng." : "Đã tạm ngưng phòng."; await LoadAsync(ct);
    }

    partial void OnSelectedBuildingChanged(HotelBuildingDto? value)
    {
        if (!IsEditing || SelectedFloor is null || SelectedFloor.HotelBuildingId != value?.HotelBuildingId) SelectedFloor = Floors.FirstOrDefault(x => x.HotelBuildingId == value?.HotelBuildingId);
        if (SelectedArea is not null && SelectedArea.HotelBuildingId != value?.HotelBuildingId) SelectedArea = Areas.FirstOrDefault(x => x.HotelBuildingId == value?.HotelBuildingId);
        RaiseState();
    }
    private static Result<HotelRoomDto> MissingCommand() => Result<HotelRoomDto>.Failure(Error.System("HOTEL_ROOM_COMMAND_MISSING", "Command is unavailable."));
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
}
