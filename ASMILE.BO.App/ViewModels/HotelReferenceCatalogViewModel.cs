using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelReferenceCatalogViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelReferenceCatalogItemCommandHandler? _createHandler;
    private readonly UpdateHotelReferenceCatalogItemCommandHandler? _updateHandler;
    private readonly ActivateHotelReferenceCatalogItemCommandHandler? _activateHandler;
    private readonly DeactivateHotelReferenceCatalogItemCommandHandler? _deactivateHandler;
    private bool _initialized;

    public HotelReferenceCatalogViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelReferenceCatalogItemCommandHandler? createHandler = null, UpdateHotelReferenceCatalogItemCommandHandler? updateHandler = null, ActivateHotelReferenceCatalogItemCommandHandler? activateHandler = null, DeactivateHotelReferenceCatalogItemCommandHandler? deactivateHandler = null)
    { _repository = repository; _contextAccessor = contextAccessor; _createHandler = createHandler; _updateHandler = updateHandler; _activateHandler = activateHandler; _deactivateHandler = deactivateHandler; }

    public ObservableCollection<HotelReferenceCatalogItemDto> Items { get; } = [];
    [ObservableProperty] private string catalogKey = "guest-type";
    [ObservableProperty] private string title = "Danh mục Front Office";
    [ObservableProperty] private string subtitle = "Danh mục dùng chung, độc lập theo cơ sở lưu trú.";
    [ObservableProperty] private HotelReferenceCatalogItemDto? selectedItem;
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
    public string SelectedSummary => SelectedItem is null ? "Chọn một dòng để xem và chỉnh sửa." : $"{SelectedItem.Code} · {SelectedItem.Name}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";

    public void Configure(string key, string pageTitle, string pageSubtitle)
    {
        CatalogKey = key; Title = pageTitle; Subtitle = pageSubtitle; _initialized = false;
    }

    public async Task InitializeAsync(CancellationToken ct)
    { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty)
            { Items.Clear(); ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context để tải danh mục."; return; }
            var result = await _repository.SearchCatalogItemsAsync(propertyId, CatalogKey, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "displayOrder", ct);
            Items.Clear(); foreach (var item in result.Items) Items.Add(item);
            if (SelectedItem is null || Items.All(x => x.HotelReferenceCatalogItemId != SelectedItem.HotelReferenceCatalogItemId)) SelectedItem = Items.FirstOrDefault();
            InfoMessage = $"Đã tải {Items.Count} mục từ dữ liệu thật theo cơ sở hiện tại.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được danh mục: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; Code = string.Empty; Name = string.Empty; Description = string.Empty; DisplayOrder = Items.Count + 1; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void EditItem() { if (SelectedItem is null) return; Code = SelectedItem.Code; Name = SelectedItem.Name; Description = SelectedItem.Description ?? string.Empty; DisplayOrder = SelectedItem.DisplayOrder; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Mã và tên là bắt buộc."; RaiseState(); return; }
        try
        {
            Result<HotelReferenceCatalogItemDto> result;
            if (SelectedItem is null)
            {
                if (_createHandler is null) { ErrorMessage = "Chưa đăng ký command danh mục thật."; return; }
                result = await _createHandler.HandleAsync(new CreateHotelReferenceCatalogItemCommand(CatalogKey, Code.Trim(), Name.Trim(), NullIfEmpty(Description), DisplayOrder), ct);
            }
            else
            {
                if (_updateHandler is null) { ErrorMessage = "Chưa đăng ký command danh mục thật."; return; }
                result = await _updateHandler.HandleAsync(new UpdateHotelReferenceCatalogItemCommand(SelectedItem.HotelReferenceCatalogItemId, CatalogKey, Code.Trim(), Name.Trim(), NullIfEmpty(Description), DisplayOrder, SelectedItem.RowVersion ?? []), ct);
            }
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được danh mục."; return; }
            InfoMessage = SelectedItem is null ? $"Đã thêm {result.Value!.Code}." : $"Đã cập nhật {result.Value!.Code}.";
            IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được danh mục: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedItem is null) return;
        var result = SelectedItem.IsActive
            ? _deactivateHandler is null ? MissingCommand() : await _deactivateHandler.HandleAsync(new DeactivateHotelReferenceCatalogItemCommand(SelectedItem.HotelReferenceCatalogItemId, SelectedItem.RowVersion ?? []), ct)
            : _activateHandler is null ? MissingCommand() : await _activateHandler.HandleAsync(new ActivateHotelReferenceCatalogItemCommand(SelectedItem.HotelReferenceCatalogItemId, SelectedItem.RowVersion ?? []), ct);
        if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; RaiseState(); return; }
        InfoMessage = result.Value!.IsActive ? "Đã bật danh mục." : "Đã tạm ngưng danh mục."; await LoadAsync(ct);
    }

    private static Result<HotelReferenceCatalogItemDto> MissingCommand() => Result<HotelReferenceCatalogItemDto>.Failure(Error.System("HOTEL_CATALOG_COMMAND_MISSING", "Command is unavailable."));
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
    partial void OnSelectedItemChanged(HotelReferenceCatalogItemDto? value) { if (!IsEditing && value is not null) { Code = value.Code; Name = value.Name; Description = value.Description ?? string.Empty; DisplayOrder = value.DisplayOrder; } RaiseState(); }
}
