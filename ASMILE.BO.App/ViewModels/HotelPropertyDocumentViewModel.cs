using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelPropertyDocumentViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelPropertyDocumentCommandHandler? _createHandler;
    private readonly UpdateHotelPropertyDocumentCommandHandler? _updateHandler;
    private readonly ActivateHotelPropertyDocumentCommandHandler? _activateHandler;
    private readonly DeactivateHotelPropertyDocumentCommandHandler? _deactivateHandler;
    private bool _initialized;

    public HotelPropertyDocumentViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelPropertyDocumentCommandHandler? createHandler = null, UpdateHotelPropertyDocumentCommandHandler? updateHandler = null, ActivateHotelPropertyDocumentCommandHandler? activateHandler = null, DeactivateHotelPropertyDocumentCommandHandler? deactivateHandler = null)
    { _repository = repository; _contextAccessor = contextAccessor; _createHandler = createHandler; _updateHandler = updateHandler; _activateHandler = activateHandler; _deactivateHandler = deactivateHandler; }

    public ObservableCollection<HotelPropertyDocumentDto> Items { get; } = [];
    [ObservableProperty] private HotelPropertyDocumentDto? selectedItem;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string documentType = string.Empty;
    [ObservableProperty] private string documentNumber = string.Empty;
    [ObservableProperty] private string issuedBy = string.Empty;
    [ObservableProperty] private string issuedDate = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string expiryDate = string.Empty;
    [ObservableProperty] private string fileReference = string.Empty;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private bool isRequired;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn hồ sơ để xem và chỉnh sửa." : $"{SelectedItem.DocumentType} · {SelectedItem.DocumentNumber}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }
    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    { IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; try { if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; } var result = await _repository.SearchPropertyDocumentsAsync(propertyId, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "expiry", ct); Items.Clear(); foreach (var item in result.Items) Items.Add(item); if (SelectedItem is null || Items.All(x => x.HotelPropertyDocumentId != SelectedItem.HotelPropertyDocumentId)) SelectedItem = Items.FirstOrDefault(); InfoMessage = $"Đã tải {Items.Count} hồ sơ từ dữ liệu thật."; } catch (OperationCanceledException) when (ct.IsCancellationRequested) { } catch (Exception ex) { ErrorMessage = $"Không tải được hồ sơ: {ex.Message}"; } finally { IsLoading = false; RaiseState(); } }
    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; DocumentType = string.Empty; DocumentNumber = string.Empty; IssuedBy = string.Empty; IssuedDate = DateTime.Today.ToString("yyyy-MM-dd"); ExpiryDate = string.Empty; FileReference = string.Empty; Notes = string.Empty; IsRequired = true; IsEditing = true; RaiseState(); }
    [RelayCommand] private void EditItem() { if (SelectedItem is null) return; Fill(SelectedItem); IsEditing = true; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }
    [RelayCommand] private async Task SaveAsync(CancellationToken ct)
    { ErrorMessage = string.Empty; InfoMessage = string.Empty; DateTime expiry = default; if (!DateTime.TryParse(IssuedDate, out var issued) || ( !string.IsNullOrWhiteSpace(ExpiryDate) && !DateTime.TryParse(ExpiryDate, out expiry))) { ErrorMessage = "Ngày cấp / hết hạn không hợp lệ."; RaiseState(); return; } DateTime? expiryValue = string.IsNullOrWhiteSpace(ExpiryDate) ? null : expiry; try { Result<HotelPropertyDocumentDto> result; if (SelectedItem is null) { if (_createHandler is null) { ErrorMessage = "Chưa đăng ký command hồ sơ thật."; return; } result = await _createHandler.HandleAsync(new CreateHotelPropertyDocumentCommand(DocumentType, DocumentNumber, NullIfEmpty(IssuedBy), issued, expiryValue, NullIfEmpty(FileReference), NullIfEmpty(Notes), IsRequired), ct); } else { if (_updateHandler is null) { ErrorMessage = "Chưa đăng ký command hồ sơ thật."; return; } result = await _updateHandler.HandleAsync(new UpdateHotelPropertyDocumentCommand(SelectedItem.HotelPropertyDocumentId, DocumentType, DocumentNumber, NullIfEmpty(IssuedBy), issued, expiryValue, NullIfEmpty(FileReference), NullIfEmpty(Notes), IsRequired, SelectedItem.RowVersion ?? []), ct); } if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được hồ sơ."; return; } InfoMessage = "Đã lưu hồ sơ pháp lý / vận hành."; IsEditing = false; await LoadAsync(ct); } catch (Exception ex) { ErrorMessage = $"Không lưu được hồ sơ: {ex.Message}"; } finally { RaiseState(); } }
    [RelayCommand] private async Task ToggleActiveAsync(CancellationToken ct) { if (SelectedItem is null) return; var result = SelectedItem.IsActive ? _deactivateHandler is null ? Missing() : await _deactivateHandler.HandleAsync(new DeactivateHotelPropertyDocumentCommand(SelectedItem.HotelPropertyDocumentId, SelectedItem.RowVersion ?? []), ct) : _activateHandler is null ? Missing() : await _activateHandler.HandleAsync(new ActivateHotelPropertyDocumentCommand(SelectedItem.HotelPropertyDocumentId, SelectedItem.RowVersion ?? []), ct); if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; RaiseState(); return; } InfoMessage = result.Value!.IsActive ? "Đã bật hồ sơ." : "Đã tạm ngưng hồ sơ."; await LoadAsync(ct); }
    private void Fill(HotelPropertyDocumentDto x) { DocumentType = x.DocumentType; DocumentNumber = x.DocumentNumber; IssuedBy = x.IssuedBy ?? string.Empty; IssuedDate = x.IssuedDate.ToString("yyyy-MM-dd"); ExpiryDate = x.ExpiryDate?.ToString("yyyy-MM-dd") ?? string.Empty; FileReference = x.FileReference ?? string.Empty; Notes = x.Notes ?? string.Empty; IsRequired = x.IsRequired; }
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private static Result<HotelPropertyDocumentDto> Missing() => Result<HotelPropertyDocumentDto>.Failure(Error.System("HOTEL_PROPERTY_DOCUMENT_COMMAND_MISSING", "Command is unavailable.")); private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); } partial void OnSelectedItemChanged(HotelPropertyDocumentDto? value) { if (!IsEditing && value is not null) Fill(value); RaiseState(); }
}
