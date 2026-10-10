using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelFolioProfileViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelFolioProfileCommandHandler? _create;
    private readonly UpdateHotelFolioProfileCommandHandler? _update;
    private readonly ActivateHotelFolioProfileCommandHandler? _activate;
    private readonly DeactivateHotelFolioProfileCommandHandler? _deactivate;
    private bool _initialized;

    public HotelFolioProfileViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelFolioProfileCommandHandler? create = null, UpdateHotelFolioProfileCommandHandler? update = null, ActivateHotelFolioProfileCommandHandler? activate = null, DeactivateHotelFolioProfileCommandHandler? deactivate = null)
    { _repository = repository; _contextAccessor = contextAccessor; _create = create; _update = update; _activate = activate; _deactivate = deactivate; }

    public ObservableCollection<HotelFolioProfileDto> Items { get; } = [];
    public IReadOnlyList<string> KindOptions { get; } = ["Tất cả", "Guest", "Company", "Master", "Group", "City Ledger"];
    [ObservableProperty] private HotelFolioProfileDto? selectedItem;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedKind = "Tất cả";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string kind = "Guest";
    [ObservableProperty] private bool autoCreate = true;
    [ObservableProperty] private bool autoRouting;
    [ObservableProperty] private bool allowSplit = true;
    [ObservableProperty] private bool allowMerge = true;
    [ObservableProperty] private bool allowTransfer = true;
    [ObservableProperty] private string chargeRouting = string.Empty;
    [ObservableProperty] private string paymentRouting = string.Empty;
    [ObservableProperty] private string effectiveFrom = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string effectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn loại Folio để xem và chỉnh sửa." : $"{SelectedItem.Code} · {SelectedItem.Name}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";
    public string SelectedKindLabel => SelectedItem is null ? "-" : KindLabel(SelectedItem.Kind);

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; }
            var kind = ParseKind(SelectedKind);
            var result = await _repository.SearchFolioProfilesAsync(propertyId, kind, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "kind", ct);
            Items.Clear(); foreach (var item in result.Items) Items.Add(item);
            if (SelectedItem is null || Items.All(x => x.HotelFolioProfileId != SelectedItem.HotelFolioProfileId)) SelectedItem = Items.FirstOrDefault();
            InfoMessage = $"Đã tải {Items.Count} loại Folio từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được loại Folio: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; Code = string.Empty; Name = string.Empty; Kind = "Guest"; AutoCreate = true; AutoRouting = false; AllowSplit = true; AllowMerge = true; AllowTransfer = true; ChargeRouting = string.Empty; PaymentRouting = string.Empty; EffectiveFrom = DateTime.Today.ToString("yyyy-MM-dd"); EffectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"); Description = string.Empty; IsEditing = true; RaiseState(); }
    [RelayCommand] private void EditItem() { if (SelectedItem is null) return; Fill(SelectedItem); IsEditing = true; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (!Enum.TryParse<HotelFolioTypeKind>(Kind.Replace(" ", string.Empty), true, out var folioKind) || !DateTime.TryParse(EffectiveFrom, out var from) || !DateTime.TryParse(EffectiveTo, out var to)) { ErrorMessage = "Loại Folio hoặc khoảng hiệu lực không hợp lệ."; RaiseState(); return; }
        try
        {
            Result<HotelFolioProfileDto> result;
            if (SelectedItem is null) result = _create is null ? Missing() : await _create.HandleAsync(new CreateHotelFolioProfileCommand(Code, Name, folioKind, AutoCreate, AutoRouting, AllowSplit, AllowMerge, AllowTransfer, NullIfEmpty(ChargeRouting), NullIfEmpty(PaymentRouting), from, to, NullIfEmpty(Description)), ct);
            else result = _update is null ? Missing() : await _update.HandleAsync(new UpdateHotelFolioProfileCommand(SelectedItem.HotelFolioProfileId, Code, Name, folioKind, AutoCreate, AutoRouting, AllowSplit, AllowMerge, AllowTransfer, NullIfEmpty(ChargeRouting), NullIfEmpty(PaymentRouting), from, to, NullIfEmpty(Description), SelectedItem.RowVersion ?? []), ct);
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được loại Folio."; return; }
            InfoMessage = "Đã lưu loại Folio vào dữ liệu thật."; IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được loại Folio: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedItem is null) return;
        var result = SelectedItem.IsActive ? _deactivate is null ? Missing() : await _deactivate.HandleAsync(new DeactivateHotelFolioProfileCommand(SelectedItem.HotelFolioProfileId, SelectedItem.RowVersion ?? []), ct) : _activate is null ? Missing() : await _activate.HandleAsync(new ActivateHotelFolioProfileCommand(SelectedItem.HotelFolioProfileId, SelectedItem.RowVersion ?? []), ct);
        if (result.IsFailure) ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; else await LoadAsync(ct); RaiseState();
    }

    private void Fill(HotelFolioProfileDto x) { Code = x.Code; Name = x.Name; Kind = KindLabel(x.Kind); AutoCreate = x.AutoCreate; AutoRouting = x.AutoRouting; AllowSplit = x.AllowSplit; AllowMerge = x.AllowMerge; AllowTransfer = x.AllowTransfer; ChargeRouting = x.ChargeRouting ?? string.Empty; PaymentRouting = x.PaymentRouting ?? string.Empty; EffectiveFrom = x.EffectiveFrom.ToString("yyyy-MM-dd"); EffectiveTo = x.EffectiveTo.ToString("yyyy-MM-dd"); Description = x.Description ?? string.Empty; }
    private static HotelFolioTypeKind? ParseKind(string value) => value == "Tất cả" ? null : Enum.TryParse<HotelFolioTypeKind>(value.Replace(" ", string.Empty), true, out var kind) ? kind : null;
    private static string KindLabel(HotelFolioTypeKind value) => value switch { HotelFolioTypeKind.CityLedger => "City Ledger", _ => value.ToString() };
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Result<HotelFolioProfileDto> Missing() => Result<HotelFolioProfileDto>.Failure(Error.System("HOTEL_FOLIO_COMMAND_MISSING", "Command is unavailable."));
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); OnPropertyChanged(nameof(SelectedKindLabel)); }
    partial void OnSelectedItemChanged(HotelFolioProfileDto? value) { if (!IsEditing && value is not null) Fill(value); RaiseState(); }
}
