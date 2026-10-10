using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelRoomStatusRuleViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelRoomStatusRuleCommandHandler? _createHandler;
    private readonly UpdateHotelRoomStatusRuleCommandHandler? _updateHandler;
    private readonly ActivateHotelRoomStatusRuleCommandHandler? _activateHandler;
    private readonly DeactivateHotelRoomStatusRuleCommandHandler? _deactivateHandler;
    private bool _initialized;

    public HotelRoomStatusRuleViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelRoomStatusRuleCommandHandler? createHandler = null, UpdateHotelRoomStatusRuleCommandHandler? updateHandler = null, ActivateHotelRoomStatusRuleCommandHandler? activateHandler = null, DeactivateHotelRoomStatusRuleCommandHandler? deactivateHandler = null)
    { _repository = repository; _contextAccessor = contextAccessor; _createHandler = createHandler; _updateHandler = updateHandler; _activateHandler = activateHandler; _deactivateHandler = deactivateHandler; }

    public ObservableCollection<HotelRoomStatusRuleDto> Items { get; } = [];
    public IReadOnlyList<string> Domains { get; } = ["FO", "HK", "OOS"];
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedDomain = string.Empty;
    [ObservableProperty] private HotelRoomStatusRuleDto? selectedItem;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string statusDomain = "OOS";
    [ObservableProperty] private string fromStatus = string.Empty;
    [ObservableProperty] private string toStatus = string.Empty;
    [ObservableProperty] private string requiredRole = string.Empty;
    [ObservableProperty] private string requiredEvent = string.Empty;
    [ObservableProperty] private bool requiresApproval;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private int displayOrder;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn một quy tắc để xem và chỉnh sửa." : $"{SelectedItem.Code} · {SelectedItem.Name}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang áp dụng" : "Tạm ngưng";

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty)
            { Items.Clear(); ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context để tải quy tắc."; return; }
            var result = await _repository.SearchRoomStatusRulesAsync(propertyId, string.IsNullOrWhiteSpace(SelectedDomain) ? null : SelectedDomain, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "domain", ct);
            Items.Clear(); foreach (var item in result.Items) Items.Add(item);
            if (SelectedItem is null || Items.All(x => x.HotelRoomStatusRuleId != SelectedItem.HotelRoomStatusRuleId)) SelectedItem = Items.FirstOrDefault();
            InfoMessage = $"Đã tải {Items.Count} quy tắc từ dữ liệu thật theo cơ sở hiện tại.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được quy tắc: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; Code = string.Empty; Name = string.Empty; StatusDomain = "OOS"; FromStatus = string.Empty; ToStatus = string.Empty; RequiredRole = string.Empty; RequiredEvent = string.Empty; RequiresApproval = false; Description = string.Empty; DisplayOrder = Items.Count + 1; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void EditItem() { if (SelectedItem is null) return; Code = SelectedItem.Code; Name = SelectedItem.Name; StatusDomain = SelectedItem.StatusDomain; FromStatus = SelectedItem.FromStatus; ToStatus = SelectedItem.ToStatus; RequiredRole = SelectedItem.RequiredRole ?? string.Empty; RequiredEvent = SelectedItem.RequiredEvent ?? string.Empty; RequiresApproval = SelectedItem.RequiresApproval; Description = SelectedItem.Description ?? string.Empty; DisplayOrder = SelectedItem.DisplayOrder; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(FromStatus) || string.IsNullOrWhiteSpace(ToStatus)) { ErrorMessage = "Mã, tên, trạng thái nguồn và trạng thái đích là bắt buộc."; RaiseState(); return; }
        try
        {
            Result<HotelRoomStatusRuleDto> result;
            if (SelectedItem is null)
            {
                if (_createHandler is null) { ErrorMessage = "Chưa đăng ký command quy tắc trạng thái thật."; return; }
                result = await _createHandler.HandleAsync(new CreateHotelRoomStatusRuleCommand(Code.Trim(), Name.Trim(), StatusDomain, FromStatus.Trim(), ToStatus.Trim(), NullIfEmpty(RequiredRole), NullIfEmpty(RequiredEvent), RequiresApproval, NullIfEmpty(Description), DisplayOrder), ct);
            }
            else
            {
                if (_updateHandler is null) { ErrorMessage = "Chưa đăng ký command quy tắc trạng thái thật."; return; }
                result = await _updateHandler.HandleAsync(new UpdateHotelRoomStatusRuleCommand(SelectedItem.HotelRoomStatusRuleId, Code.Trim(), Name.Trim(), StatusDomain, FromStatus.Trim(), ToStatus.Trim(), NullIfEmpty(RequiredRole), NullIfEmpty(RequiredEvent), RequiresApproval, NullIfEmpty(Description), DisplayOrder, SelectedItem.RowVersion ?? []), ct);
            }
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được quy tắc."; return; }
            InfoMessage = SelectedItem is null ? $"Đã thêm {result.Value!.Code}." : $"Đã cập nhật {result.Value!.Code}.";
            IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được quy tắc: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedItem is null) return;
        var result = SelectedItem.IsActive
            ? _deactivateHandler is null ? MissingCommand() : await _deactivateHandler.HandleAsync(new DeactivateHotelRoomStatusRuleCommand(SelectedItem.HotelRoomStatusRuleId, SelectedItem.RowVersion ?? []), ct)
            : _activateHandler is null ? MissingCommand() : await _activateHandler.HandleAsync(new ActivateHotelRoomStatusRuleCommand(SelectedItem.HotelRoomStatusRuleId, SelectedItem.RowVersion ?? []), ct);
        if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; RaiseState(); return; }
        InfoMessage = result.Value!.IsActive ? "Đã bật quy tắc." : "Đã tạm ngưng quy tắc."; await LoadAsync(ct);
    }

    private static Result<HotelRoomStatusRuleDto> MissingCommand() => Result<HotelRoomStatusRuleDto>.Failure(Error.System("HOTEL_ROOM_STATUS_RULE_COMMAND_MISSING", "Command is unavailable."));
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
    partial void OnSelectedItemChanged(HotelRoomStatusRuleDto? value) { if (!IsEditing && value is not null) { Code = value.Code; Name = value.Name; StatusDomain = value.StatusDomain; FromStatus = value.FromStatus; ToStatus = value.ToStatus; RequiredRole = value.RequiredRole ?? string.Empty; RequiredEvent = value.RequiredEvent ?? string.Empty; RequiresApproval = value.RequiresApproval; Description = value.Description ?? string.Empty; DisplayOrder = value.DisplayOrder; } RaiseState(); }
}
