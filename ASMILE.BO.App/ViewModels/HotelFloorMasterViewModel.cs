using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelFloorMasterViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelFloorCommandHandler? _createHandler;
    private readonly UpdateHotelFloorCommandHandler? _updateHandler;
    private readonly ActivateHotelFloorCommandHandler? _activateHandler;
    private readonly DeactivateHotelFloorCommandHandler? _deactivateHandler;
    private bool _initialized;

    public HotelFloorMasterViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelFloorCommandHandler? createHandler = null, UpdateHotelFloorCommandHandler? updateHandler = null, ActivateHotelFloorCommandHandler? activateHandler = null, DeactivateHotelFloorCommandHandler? deactivateHandler = null)
    { _repository = repository; _contextAccessor = contextAccessor; _createHandler = createHandler; _updateHandler = updateHandler; _activateHandler = activateHandler; _deactivateHandler = deactivateHandler; }

    public ObservableCollection<HotelFloorDto> Floors { get; } = [];
    public ObservableCollection<HotelBuildingDto> Buildings { get; } = [];
    [ObservableProperty] private HotelFloorDto? selectedFloor;
    [ObservableProperty] private HotelBuildingDto? selectedBuilding;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private int levelNumber;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private int displayOrder;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedFloor is null ? "Chọn một tầng để xem và chỉnh sửa." : $"{SelectedFloor.BuildingName} · {SelectedFloor.Name}";
    public string SelectedStatus => SelectedFloor?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty)
            { Floors.Clear(); Buildings.Clear(); ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context để tải tầng."; return; }
            var buildings = await _repository.SearchBuildingsAsync(propertyId, null, true, new PageRequest(1, 200), "displayOrder", ct);
            Buildings.Clear(); foreach (var item in buildings.Items) Buildings.Add(item);
            if (SelectedBuilding is null || Buildings.All(x => x.HotelBuildingId != SelectedBuilding.HotelBuildingId)) SelectedBuilding = Buildings.FirstOrDefault();
            var floors = await _repository.SearchFloorsAsync(propertyId, SelectedBuilding?.HotelBuildingId, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 300), "level", ct);
            Floors.Clear(); foreach (var item in floors.Items) Floors.Add(item);
            if (SelectedFloor is null || Floors.All(x => x.HotelFloorId != SelectedFloor.HotelFloorId)) SelectedFloor = Floors.FirstOrDefault();
            InfoMessage = $"Đã tải {Floors.Count} tầng thuộc {SelectedBuilding?.Name ?? "các tòa nhà"} từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được tầng: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewFloor() { SelectedFloor = null; Code = string.Empty; Name = string.Empty; LevelNumber = 1; Description = string.Empty; DisplayOrder = Floors.Count + 1; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void EditFloor() { if (SelectedFloor is null) return; SelectedBuilding = Buildings.FirstOrDefault(x => x.HotelBuildingId == SelectedFloor.HotelBuildingId); Code = SelectedFloor.Code; Name = SelectedFloor.Name; LevelNumber = SelectedFloor.LevelNumber; Description = SelectedFloor.Description ?? string.Empty; DisplayOrder = SelectedFloor.DisplayOrder; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (SelectedBuilding is null) { ErrorMessage = "Phải chọn tòa nhà cho tầng."; RaiseState(); return; }
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Mã và tên tầng là bắt buộc."; RaiseState(); return; }
        try
        {
            Result<HotelFloorDto> result;
            if (SelectedFloor is null)
            {
                if (_createHandler is null) { ErrorMessage = "Chưa đăng ký command tầng thật."; return; }
                result = await _createHandler.HandleAsync(new CreateHotelFloorCommand(SelectedBuilding.HotelBuildingId, Code.Trim(), Name.Trim(), LevelNumber, NullIfEmpty(Description), DisplayOrder), ct);
            }
            else
            {
                if (_updateHandler is null) { ErrorMessage = "Chưa đăng ký command tầng thật."; return; }
                result = await _updateHandler.HandleAsync(new UpdateHotelFloorCommand(SelectedFloor.HotelFloorId, SelectedBuilding.HotelBuildingId, Code.Trim(), Name.Trim(), LevelNumber, NullIfEmpty(Description), DisplayOrder, SelectedFloor.RowVersion ?? []), ct);
            }
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được tầng."; return; }
            InfoMessage = SelectedFloor is null ? $"Đã thêm tầng {result.Value!.Name}." : $"Đã cập nhật tầng {result.Value!.Name}.";
            IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được tầng: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedFloor is null) return;
        var result = SelectedFloor.IsActive
            ? _deactivateHandler is null ? MissingCommand() : await _deactivateHandler.HandleAsync(new DeactivateHotelFloorCommand(SelectedFloor.HotelFloorId, SelectedFloor.RowVersion ?? []), ct)
            : _activateHandler is null ? MissingCommand() : await _activateHandler.HandleAsync(new ActivateHotelFloorCommand(SelectedFloor.HotelFloorId, SelectedFloor.RowVersion ?? []), ct);
        if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái tầng."; RaiseState(); return; }
        InfoMessage = result.Value!.IsActive ? "Đã bật tầng." : "Đã tạm ngưng tầng."; await LoadAsync(ct);
    }

    partial void OnSelectedBuildingChanged(HotelBuildingDto? value)
    {
        // LoadAsync assigns the first building after loading the building list and then
        // loads its floors itself. Do not start a second query from the generated
        // property-change callback while the shared DbContext is already busy.
        if (!IsEditing && !IsLoading) _ = LoadFloorsForBuildingAsync(value);
        RaiseState();
    }
    private async Task LoadFloorsForBuildingAsync(HotelBuildingDto? building)
    {
        if (!_initialized || building is null || _repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId) return;
        try
        {
            var result = await _repository.SearchFloorsAsync(propertyId, building.HotelBuildingId, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 300), "level");
            Floors.Clear(); foreach (var item in result.Items) Floors.Add(item);
            SelectedFloor = Floors.FirstOrDefault(); RaiseState();
        }
        catch (Exception ex) { ErrorMessage = $"Không tải được tầng của tòa nhà: {ex.Message}"; RaiseState(); }
    }
    private static Result<HotelFloorDto> MissingCommand() => Result<HotelFloorDto>.Failure(Error.System("HOTEL_FLOOR_COMMAND_MISSING", "Command is unavailable."));
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
}
