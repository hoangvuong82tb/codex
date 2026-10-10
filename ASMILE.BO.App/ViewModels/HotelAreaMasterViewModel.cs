using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelAreaMasterViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelAreaCommandHandler? _createHandler;
    private readonly UpdateHotelAreaCommandHandler? _updateHandler;
    private readonly ActivateHotelAreaCommandHandler? _activateHandler;
    private readonly DeactivateHotelAreaCommandHandler? _deactivateHandler;
    private bool _initialized;

    public HotelAreaMasterViewModel(
        IHotelStructureMasterDataRepository? repository = null,
        IFrontOfficeExecutionContextAccessor? contextAccessor = null,
        CreateHotelAreaCommandHandler? createHandler = null,
        UpdateHotelAreaCommandHandler? updateHandler = null,
        ActivateHotelAreaCommandHandler? activateHandler = null,
        DeactivateHotelAreaCommandHandler? deactivateHandler = null)
    {
        _repository = repository;
        _contextAccessor = contextAccessor;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _activateHandler = activateHandler;
        _deactivateHandler = deactivateHandler;
    }

    public ObservableCollection<HotelAreaDto> Areas { get; } = [];
    public ObservableCollection<HotelBuildingDto> Buildings { get; } = [];
    public ObservableCollection<HotelFloorDto> Floors { get; } = [];

    [ObservableProperty] private HotelAreaDto? selectedArea;
    [ObservableProperty] private HotelBuildingDto? selectedBuilding;
    [ObservableProperty] private HotelFloorDto? selectedFloor;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string areaType = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private int displayOrder;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedArea is null ? "Chọn một khu vực để xem và chỉnh sửa." : $"{SelectedArea.Code} · {SelectedArea.Name}";
    public string SelectedStatus => SelectedArea?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (_initialized) return;
        _initialized = true;
        await LoadAsync(ct);
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty)
            {
                Areas.Clear(); Buildings.Clear(); Floors.Clear();
                ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context để tải khu vực.";
                return;
            }

            var buildings = await _repository.SearchBuildingsAsync(propertyId, null, true, new PageRequest(1, 300), "displayOrder", ct);
            Buildings.Clear();
            foreach (var item in buildings.Items) Buildings.Add(item);

            var floors = await _repository.SearchFloorsAsync(propertyId, null, null, true, new PageRequest(1, 500), "building", ct);
            Floors.Clear();
            foreach (var item in floors.Items) Floors.Add(item);

            var areas = await _repository.SearchAreasAsync(propertyId, null, null, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 500), "building", ct);
            Areas.Clear();
            foreach (var item in areas.Items) Areas.Add(item);
            if (SelectedArea is null || Areas.All(x => x.HotelAreaId != SelectedArea.HotelAreaId)) SelectedArea = Areas.FirstOrDefault();
            InfoMessage = $"Đã tải {Areas.Count} khu vực từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được khu vực: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);

    [RelayCommand]
    private void NewArea()
    {
        SelectedArea = null;
        Code = string.Empty; Name = string.Empty; AreaType = string.Empty; Description = string.Empty;
        DisplayOrder = Areas.Count + 1; SelectedBuilding = null; SelectedFloor = null;
        IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState();
    }

    [RelayCommand]
    private void EditArea()
    {
        if (SelectedArea is null) return;
        SelectedBuilding = Buildings.FirstOrDefault(x => x.HotelBuildingId == SelectedArea.HotelBuildingId);
        SelectedFloor = Floors.FirstOrDefault(x => x.HotelFloorId == SelectedArea.HotelFloorId);
        Code = SelectedArea.Code; Name = SelectedArea.Name; AreaType = SelectedArea.AreaType ?? string.Empty;
        Description = SelectedArea.Description ?? string.Empty; DisplayOrder = SelectedArea.DisplayOrder;
        IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState();
    }

    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        { ErrorMessage = "Mã và tên khu vực là bắt buộc."; RaiseState(); return; }
        if (SelectedFloor is not null && SelectedBuilding is null)
        { ErrorMessage = "Phải chọn tòa nhà khi chọn tầng."; RaiseState(); return; }
        try
        {
            Result<HotelAreaDto> result;
            var buildingId = SelectedBuilding?.HotelBuildingId;
            var floorId = SelectedFloor?.HotelFloorId;
            if (SelectedArea is null)
            {
                if (_createHandler is null) { ErrorMessage = "Chưa đăng ký command khu vực thật."; return; }
                result = await _createHandler.HandleAsync(new CreateHotelAreaCommand(buildingId, floorId, Code.Trim(), Name.Trim(), NullIfEmpty(AreaType), NullIfEmpty(Description), DisplayOrder), ct);
            }
            else
            {
                if (_updateHandler is null) { ErrorMessage = "Chưa đăng ký command khu vực thật."; return; }
                result = await _updateHandler.HandleAsync(new UpdateHotelAreaCommand(SelectedArea.HotelAreaId, buildingId, floorId, Code.Trim(), Name.Trim(), NullIfEmpty(AreaType), NullIfEmpty(Description), DisplayOrder, SelectedArea.RowVersion ?? []), ct);
            }
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được khu vực."; return; }
            InfoMessage = SelectedArea is null ? $"Đã thêm khu vực {result.Value!.Code}." : $"Đã cập nhật khu vực {result.Value!.Code}.";
            IsEditing = false;
            await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được khu vực: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedArea is null) return;
        var result = SelectedArea.IsActive
            ? _deactivateHandler is null ? MissingCommand() : await _deactivateHandler.HandleAsync(new DeactivateHotelAreaCommand(SelectedArea.HotelAreaId, SelectedArea.RowVersion ?? []), ct)
            : _activateHandler is null ? MissingCommand() : await _activateHandler.HandleAsync(new ActivateHotelAreaCommand(SelectedArea.HotelAreaId, SelectedArea.RowVersion ?? []), ct);
        if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái khu vực."; RaiseState(); return; }
        InfoMessage = result.Value!.IsActive ? "Đã bật khu vực." : "Đã tạm ngưng khu vực.";
        await LoadAsync(ct);
    }

    private static Result<HotelAreaDto> MissingCommand() => Result<HotelAreaDto>.Failure(Error.System("HOTEL_AREA_COMMAND_MISSING", "Command is unavailable."));
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void RaiseState()
    {
        OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible));
        OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus));
    }

    partial void OnSelectedAreaChanged(HotelAreaDto? value)
    {
        if (!IsEditing && value is not null)
        {
            Code = value.Code; Name = value.Name; AreaType = value.AreaType ?? string.Empty;
            Description = value.Description ?? string.Empty; DisplayOrder = value.DisplayOrder;
            SelectedBuilding = Buildings.FirstOrDefault(x => x.HotelBuildingId == value.HotelBuildingId);
            SelectedFloor = Floors.FirstOrDefault(x => x.HotelFloorId == value.HotelFloorId);
        }
        RaiseState();
    }
}
