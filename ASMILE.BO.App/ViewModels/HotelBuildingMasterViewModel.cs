using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelBuildingMasterViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelBuildingCommandHandler? _createHandler;
    private readonly UpdateHotelBuildingCommandHandler? _updateHandler;
    private readonly ActivateHotelBuildingCommandHandler? _activateHandler;
    private readonly DeactivateHotelBuildingCommandHandler? _deactivateHandler;
    private bool _initialized;

    public HotelBuildingMasterViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelBuildingCommandHandler? createHandler = null, UpdateHotelBuildingCommandHandler? updateHandler = null, ActivateHotelBuildingCommandHandler? activateHandler = null, DeactivateHotelBuildingCommandHandler? deactivateHandler = null)
    { _repository = repository; _contextAccessor = contextAccessor; _createHandler = createHandler; _updateHandler = updateHandler; _activateHandler = activateHandler; _deactivateHandler = deactivateHandler; }

    public ObservableCollection<HotelBuildingDto> Buildings { get; } = [];
    [ObservableProperty] private HotelBuildingDto? selectedBuilding;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private int displayOrder;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedBuilding is null ? "Chọn một tòa nhà để xem và chỉnh sửa." : $"{SelectedBuilding.Code} · {SelectedBuilding.Name}";
    public string SelectedStatus => SelectedBuilding?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty)
            { Buildings.Clear(); ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context để tải tòa nhà."; return; }
            var result = await _repository.SearchBuildingsAsync(propertyId, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "displayOrder", ct);
            Buildings.Clear(); foreach (var item in result.Items) Buildings.Add(item);
            if (SelectedBuilding is null || Buildings.All(x => x.HotelBuildingId != SelectedBuilding.HotelBuildingId)) SelectedBuilding = Buildings.FirstOrDefault();
            InfoMessage = $"Đã tải {Buildings.Count} tòa nhà từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được tòa nhà: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewBuilding() { SelectedBuilding = null; Code = string.Empty; Name = string.Empty; Description = string.Empty; DisplayOrder = Buildings.Count + 1; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void EditBuilding() { if (SelectedBuilding is null) return; Code = SelectedBuilding.Code; Name = SelectedBuilding.Name; Description = SelectedBuilding.Description ?? string.Empty; DisplayOrder = SelectedBuilding.DisplayOrder; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Mã và tên tòa nhà là bắt buộc."; RaiseState(); return; }
        try
        {
            Result<HotelBuildingDto> result;
            if (SelectedBuilding is null)
            {
                if (_createHandler is null) { ErrorMessage = "Chưa đăng ký command tòa nhà thật."; return; }
                result = await _createHandler.HandleAsync(new CreateHotelBuildingCommand(Code.Trim(), Name.Trim(), NullIfEmpty(Description), DisplayOrder), ct);
            }
            else
            {
                if (_updateHandler is null) { ErrorMessage = "Chưa đăng ký command tòa nhà thật."; return; }
                result = await _updateHandler.HandleAsync(new UpdateHotelBuildingCommand(SelectedBuilding.HotelBuildingId, Code.Trim(), Name.Trim(), NullIfEmpty(Description), DisplayOrder, SelectedBuilding.RowVersion ?? []), ct);
            }
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được tòa nhà."; return; }
            InfoMessage = SelectedBuilding is null ? $"Đã thêm tòa nhà {result.Value!.Code}." : $"Đã cập nhật tòa nhà {result.Value!.Code}.";
            IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được tòa nhà: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedBuilding is null) return;
        var result = SelectedBuilding.IsActive
            ? _deactivateHandler is null ? MissingCommand() : await _deactivateHandler.HandleAsync(new DeactivateHotelBuildingCommand(SelectedBuilding.HotelBuildingId, SelectedBuilding.RowVersion ?? []), ct)
            : _activateHandler is null ? MissingCommand() : await _activateHandler.HandleAsync(new ActivateHotelBuildingCommand(SelectedBuilding.HotelBuildingId, SelectedBuilding.RowVersion ?? []), ct);
        if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái tòa nhà."; RaiseState(); return; }
        InfoMessage = result.Value!.IsActive ? "Đã bật tòa nhà." : "Đã tạm ngưng tòa nhà."; await LoadAsync(ct);
    }

    private static Result<HotelBuildingDto> MissingCommand() => Result<HotelBuildingDto>.Failure(Error.System("HOTEL_BUILDING_COMMAND_MISSING", "Command is unavailable."));
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
    partial void OnSelectedBuildingChanged(HotelBuildingDto? value) { if (!IsEditing && value is not null) { Code = value.Code; Name = value.Name; Description = value.Description ?? string.Empty; DisplayOrder = value.DisplayOrder; } RaiseState(); }
}
