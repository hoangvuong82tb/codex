using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelTaxServiceChargeRuleViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelTaxServiceChargeRuleCommandHandler? _create;
    private readonly UpdateHotelTaxServiceChargeRuleCommandHandler? _update;
    private readonly ActivateHotelTaxServiceChargeRuleCommandHandler? _activate;
    private readonly DeactivateHotelTaxServiceChargeRuleCommandHandler? _deactivate;
    private bool _initialized;

    public HotelTaxServiceChargeRuleViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelTaxServiceChargeRuleCommandHandler? create = null, UpdateHotelTaxServiceChargeRuleCommandHandler? update = null, ActivateHotelTaxServiceChargeRuleCommandHandler? activate = null, DeactivateHotelTaxServiceChargeRuleCommandHandler? deactivate = null)
    { _repository = repository; _contextAccessor = contextAccessor; _create = create; _update = update; _activate = activate; _deactivate = deactivate; }

    public ObservableCollection<HotelTaxServiceChargeRuleDto> Items { get; } = [];
    public IReadOnlyList<string> KindOptions { get; } = ["Thuế", "Phí dịch vụ"];
    [ObservableProperty] private HotelTaxServiceChargeRuleDto? selectedItem;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedKindFilter = "Tất cả";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string selectedKind = "Thuế";
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string rate = "0";
    [ObservableProperty] private bool isInclusive;
    [ObservableProperty] private string applicableService = "Tất cả dịch vụ";
    [ObservableProperty] private string applicableRatePlan = string.Empty;
    [ObservableProperty] private string roundingMode = "NearestDong";
    [ObservableProperty] private string accountingMapping = string.Empty;
    [ObservableProperty] private string effectiveFrom = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string effectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn một dòng để xem và chỉnh sửa." : $"{SelectedItem.Code} · {SelectedItem.Name}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";
    public string SelectedKindLabel => SelectedItem?.Kind == HotelTaxServiceChargeKind.Tax ? "Thuế" : "Phí dịch vụ";

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; }
            var kind = SelectedKindFilter switch { "Thuế" => HotelTaxServiceChargeKind.Tax, "Phí dịch vụ" => HotelTaxServiceChargeKind.ServiceCharge, _ => (HotelTaxServiceChargeKind?)null };
            var result = await _repository.SearchTaxServiceChargeRulesAsync(propertyId, kind, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "effective", ct);
            Items.Clear(); foreach (var item in result.Items) Items.Add(item);
            if (SelectedItem is null || Items.All(x => x.HotelTaxServiceChargeRuleId != SelectedItem.HotelTaxServiceChargeRuleId)) SelectedItem = Items.FirstOrDefault();
            InfoMessage = $"Đã tải {Items.Count} quy tắc thuế/phí dịch vụ từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được cấu hình thuế/phí dịch vụ: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; SelectedKind = "Thuế"; Code = string.Empty; Name = string.Empty; Rate = "0"; IsInclusive = false; ApplicableService = "Tất cả dịch vụ"; ApplicableRatePlan = string.Empty; RoundingMode = "NearestDong"; AccountingMapping = string.Empty; EffectiveFrom = DateTime.Today.ToString("yyyy-MM-dd"); EffectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"); Description = string.Empty; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void EditItem() { if (SelectedItem is null) return; Fill(SelectedItem); IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (!decimal.TryParse(Rate, out var rateValue) || !DateTime.TryParse(EffectiveFrom, out var from) || !DateTime.TryParse(EffectiveTo, out var to) || !TryKind(SelectedKind, out var kind)) { ErrorMessage = "Loại, tỷ lệ và khoảng hiệu lực không hợp lệ."; RaiseState(); return; }
        try
        {
            Result<HotelTaxServiceChargeRuleDto> result;
            if (SelectedItem is null) { if (_create is null) { ErrorMessage = "Chưa đăng ký command cấu hình thật."; return; } result = await _create.HandleAsync(new CreateHotelTaxServiceChargeRuleCommand(kind, Code, Name, rateValue, IsInclusive, ApplicableService, NullIfEmpty(ApplicableRatePlan), RoundingMode, NullIfEmpty(AccountingMapping), from, to, NullIfEmpty(Description)), ct); }
            else { if (_update is null) { ErrorMessage = "Chưa đăng ký command cấu hình thật."; return; } result = await _update.HandleAsync(new UpdateHotelTaxServiceChargeRuleCommand(SelectedItem.HotelTaxServiceChargeRuleId, kind, Code, Name, rateValue, IsInclusive, ApplicableService, NullIfEmpty(ApplicableRatePlan), RoundingMode, NullIfEmpty(AccountingMapping), from, to, NullIfEmpty(Description), SelectedItem.RowVersion ?? []), ct); }
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được cấu hình."; return; }
            InfoMessage = "Đã lưu cấu hình thuế/phí dịch vụ vào dữ liệu thật."; IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được cấu hình: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedItem is null) return;
        var result = SelectedItem.IsActive ? _deactivate is null ? Missing() : await _deactivate.HandleAsync(new DeactivateHotelTaxServiceChargeRuleCommand(SelectedItem.HotelTaxServiceChargeRuleId, SelectedItem.RowVersion ?? []), ct) : _activate is null ? Missing() : await _activate.HandleAsync(new ActivateHotelTaxServiceChargeRuleCommand(SelectedItem.HotelTaxServiceChargeRuleId, SelectedItem.RowVersion ?? []), ct);
        if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; RaiseState(); return; }
        InfoMessage = result.Value!.IsActive ? "Đã bật cấu hình." : "Đã tạm ngưng cấu hình."; await LoadAsync(ct);
    }

    private void Fill(HotelTaxServiceChargeRuleDto x) { SelectedKind = x.Kind == HotelTaxServiceChargeKind.Tax ? "Thuế" : "Phí dịch vụ"; Code = x.Code; Name = x.Name; Rate = x.Rate.ToString("0.####"); IsInclusive = x.IsInclusive; ApplicableService = x.ApplicableService; ApplicableRatePlan = x.ApplicableRatePlan ?? string.Empty; RoundingMode = x.RoundingMode; AccountingMapping = x.AccountingMapping ?? string.Empty; EffectiveFrom = x.EffectiveFrom.ToString("yyyy-MM-dd"); EffectiveTo = x.EffectiveTo.ToString("yyyy-MM-dd"); Description = x.Description ?? string.Empty; }
    private static bool TryKind(string value, out HotelTaxServiceChargeKind kind) { kind = value == "Phí dịch vụ" ? HotelTaxServiceChargeKind.ServiceCharge : HotelTaxServiceChargeKind.Tax; return value is "Thuế" or "Phí dịch vụ"; }
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Result<HotelTaxServiceChargeRuleDto> Missing() => Result<HotelTaxServiceChargeRuleDto>.Failure(Error.System("HOTEL_TAX_SERVICE_CHARGE_COMMAND_MISSING", "Command is unavailable."));
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); OnPropertyChanged(nameof(SelectedKindLabel)); }
    partial void OnSelectedItemChanged(HotelTaxServiceChargeRuleDto? value) { if (!IsEditing && value is not null) Fill(value); RaiseState(); }
}
