using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelGuestPreferenceProfileViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelGuestPreferenceProfileCommandHandler? _create;
    private readonly UpdateHotelGuestPreferenceProfileCommandHandler? _update;
    private readonly ActivateHotelGuestPreferenceProfileCommandHandler? _activate;
    private readonly DeactivateHotelGuestPreferenceProfileCommandHandler? _deactivate;
    private bool _initialized;

    public HotelGuestPreferenceProfileViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelGuestPreferenceProfileCommandHandler? create = null, UpdateHotelGuestPreferenceProfileCommandHandler? update = null, ActivateHotelGuestPreferenceProfileCommandHandler? activate = null, DeactivateHotelGuestPreferenceProfileCommandHandler? deactivate = null)
    { _repository = repository; _contextAccessor = contextAccessor; _create = create; _update = update; _activate = activate; _deactivate = deactivate; }

    public ObservableCollection<HotelGuestPreferenceProfileDto> Items { get; } = [];
    public IReadOnlyList<string> CategoryOptions { get; } = ["Tất cả", "Room", "Food", "Accessibility", "Communication", "Service", "Safety"];
    public IReadOnlyList<string> ValueKindOptions { get; } = ["Text", "Choice", "Boolean", "Number"];
    public IReadOnlyList<string> ScopeOptions { get; } = ["Guest", "Reservation", "Stay"];

    [ObservableProperty] private HotelGuestPreferenceProfileDto? selectedItem;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedCategory = "Tất cả";
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string category = "Room";
    [ObservableProperty] private string valueKind = "Choice";
    [ObservableProperty] private string defaultValue = string.Empty;
    [ObservableProperty] private string options = string.Empty;
    [ObservableProperty] private string scope = "Guest";
    [ObservableProperty] private string priority = "100";
    [ObservableProperty] private bool isSafetyCritical;
    [ObservableProperty] private bool isChargeable;
    [ObservableProperty] private string effectiveFrom = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string effectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn một sở thích để xem và chỉnh sửa." : $"{SelectedItem.Code} · {SelectedItem.Name}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; }
            var result = await _repository.SearchGuestPreferenceProfilesAsync(propertyId, SelectedCategory == "Tất cả" ? null : SelectedCategory, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "priority", ct);
            Items.Clear(); foreach (var item in result.Items) Items.Add(item);
            if (SelectedItem is null || Items.All(x => x.HotelGuestPreferenceProfileId != SelectedItem.HotelGuestPreferenceProfileId)) SelectedItem = Items.FirstOrDefault();
            InfoMessage = $"Đã tải {Items.Count} sở thích từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được sở thích: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; Code = string.Empty; Name = string.Empty; Category = "Room"; ValueKind = "Choice"; DefaultValue = string.Empty; Options = string.Empty; Scope = "Guest"; Priority = "100"; IsSafetyCritical = false; IsChargeable = false; EffectiveFrom = DateTime.Today.ToString("yyyy-MM-dd"); EffectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"); Description = string.Empty; IsEditing = true; RaiseState(); }
    [RelayCommand] private void EditItem() { if (SelectedItem is null) return; Fill(SelectedItem); IsEditing = true; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (!int.TryParse(Priority, out var priority) || !DateTime.TryParse(EffectiveFrom, out var from) || !DateTime.TryParse(EffectiveTo, out var to)) { ErrorMessage = "Priority hoặc khoảng hiệu lực không hợp lệ."; RaiseState(); return; }
        try
        {
            Result<HotelGuestPreferenceProfileDto> result = SelectedItem is null
                ? _create is null ? Missing() : await _create.HandleAsync(new CreateHotelGuestPreferenceProfileCommand(Code, Name, Category, ValueKind, NullIfEmpty(DefaultValue), NullIfEmpty(Options), Scope, priority, IsSafetyCritical, IsChargeable, from, to, NullIfEmpty(Description)), ct)
                : _update is null ? Missing() : await _update.HandleAsync(new UpdateHotelGuestPreferenceProfileCommand(SelectedItem.HotelGuestPreferenceProfileId, Code, Name, Category, ValueKind, NullIfEmpty(DefaultValue), NullIfEmpty(Options), Scope, priority, IsSafetyCritical, IsChargeable, from, to, NullIfEmpty(Description), SelectedItem.RowVersion ?? []), ct);
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được sở thích."; return; }
            InfoMessage = "Đã lưu sở thích vào dữ liệu thật."; IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được sở thích: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedItem is null) return;
        var result = SelectedItem.IsActive ? _deactivate is null ? Missing() : await _deactivate.HandleAsync(new DeactivateHotelGuestPreferenceProfileCommand(SelectedItem.HotelGuestPreferenceProfileId, SelectedItem.RowVersion ?? []), ct) : _activate is null ? Missing() : await _activate.HandleAsync(new ActivateHotelGuestPreferenceProfileCommand(SelectedItem.HotelGuestPreferenceProfileId, SelectedItem.RowVersion ?? []), ct);
        if (result.IsFailure) ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; else await LoadAsync(ct); RaiseState();
    }

    private void Fill(HotelGuestPreferenceProfileDto x) { Code = x.Code; Name = x.Name; Category = x.Category; ValueKind = x.ValueKind; DefaultValue = x.DefaultValue ?? string.Empty; Options = x.Options ?? string.Empty; Scope = x.Scope; Priority = x.Priority.ToString(); IsSafetyCritical = x.IsSafetyCritical; IsChargeable = x.IsChargeable; EffectiveFrom = x.EffectiveFrom.ToString("yyyy-MM-dd"); EffectiveTo = x.EffectiveTo.ToString("yyyy-MM-dd"); Description = x.Description ?? string.Empty; }
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Result<HotelGuestPreferenceProfileDto> Missing() => Result<HotelGuestPreferenceProfileDto>.Failure(Error.System("HOTEL_GUEST_PREFERENCE_COMMAND_MISSING", "Command is unavailable."));
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
    partial void OnSelectedItemChanged(HotelGuestPreferenceProfileDto? value) { if (!IsEditing && value is not null) Fill(value); RaiseState(); }
}
