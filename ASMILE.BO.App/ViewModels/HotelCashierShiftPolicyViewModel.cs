using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelCashierShiftPolicyViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelCashierShiftPolicyCommandHandler? _create;
    private readonly UpdateHotelCashierShiftPolicyCommandHandler? _update;
    private readonly ActivateHotelCashierShiftPolicyCommandHandler? _activate;
    private readonly DeactivateHotelCashierShiftPolicyCommandHandler? _deactivate;
    private bool _initialized;

    public ObservableCollection<HotelCashierShiftPolicyDto> Items { get; } = [];
    [ObservableProperty] private HotelCashierShiftPolicyDto? selectedItem;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string effectiveFrom = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string effectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
    [ObservableProperty] private bool requireOpeningFloat = true;
    [ObservableProperty] private string defaultOpeningFloat = "0";
    [ObservableProperty] private bool requireSupervisorClose = true;
    [ObservableProperty] private string varianceApprovalThreshold = "0";
    [ObservableProperty] private bool allowCashTransfer = true;
    [ObservableProperty] private bool requireHandoverNote = true;
    [ObservableProperty] private string supervisorRole = "Supervisor";
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;

    public HotelCashierShiftPolicyViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelCashierShiftPolicyCommandHandler? create = null, UpdateHotelCashierShiftPolicyCommandHandler? update = null, ActivateHotelCashierShiftPolicyCommandHandler? activate = null, DeactivateHotelCashierShiftPolicyCommandHandler? deactivate = null)
    { _repository = repository; _contextAccessor = contextAccessor; _create = create; _update = update; _activate = activate; _deactivate = deactivate; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn chính sách để xem và chỉnh sửa." : $"{SelectedItem.Code} · {SelectedItem.Name}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang áp dụng" : "Tạm ngưng";

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    { IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; try { if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; } var result = await _repository.SearchCashierShiftPoliciesAsync(propertyId, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "effective", ct); Items.Clear(); foreach (var item in result.Items) Items.Add(item); if (SelectedItem is null || Items.All(x => x.HotelCashierShiftPolicyId != SelectedItem.HotelCashierShiftPolicyId)) SelectedItem = Items.FirstOrDefault(); InfoMessage = $"Đã tải {Items.Count} chính sách ca thu ngân từ dữ liệu thật."; } catch (OperationCanceledException) when (ct.IsCancellationRequested) { } catch (Exception ex) { ErrorMessage = $"Không tải được chính sách ca thu ngân: {ex.Message}"; } finally { IsLoading = false; RaiseState(); } }
    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; Code = string.Empty; Name = string.Empty; EffectiveFrom = DateTime.Today.ToString("yyyy-MM-dd"); EffectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"); RequireOpeningFloat = true; DefaultOpeningFloat = "0"; RequireSupervisorClose = true; VarianceApprovalThreshold = "0"; AllowCashTransfer = true; RequireHandoverNote = true; SupervisorRole = "Supervisor"; Description = string.Empty; IsEditing = true; RaiseState(); }
    [RelayCommand] private void EditItem() { if (SelectedItem is null) return; Fill(SelectedItem); IsEditing = true; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    { ErrorMessage = string.Empty; InfoMessage = string.Empty; if (!decimal.TryParse(DefaultOpeningFloat, out var openingFloat) || !decimal.TryParse(VarianceApprovalThreshold, out var threshold) || !DateTime.TryParse(EffectiveFrom, out var from) || !DateTime.TryParse(EffectiveTo, out var to)) { ErrorMessage = "Ngày hoặc số tiền không hợp lệ."; RaiseState(); return; } try { Result<HotelCashierShiftPolicyDto> result; if (SelectedItem is null) { if (_create is null) { ErrorMessage = "Chưa đăng ký command chính sách ca thu ngân thật."; return; } result = await _create.HandleAsync(new CreateHotelCashierShiftPolicyCommand(Code, Name, from, to, RequireOpeningFloat, openingFloat, RequireSupervisorClose, threshold, AllowCashTransfer, RequireHandoverNote, NullIfEmpty(SupervisorRole), NullIfEmpty(Description)), ct); } else { if (_update is null) { ErrorMessage = "Chưa đăng ký command chính sách ca thu ngân thật."; return; } result = await _update.HandleAsync(new UpdateHotelCashierShiftPolicyCommand(SelectedItem.HotelCashierShiftPolicyId, Code, Name, from, to, RequireOpeningFloat, openingFloat, RequireSupervisorClose, threshold, AllowCashTransfer, RequireHandoverNote, NullIfEmpty(SupervisorRole), NullIfEmpty(Description), SelectedItem.RowVersion ?? []), ct); } if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được chính sách ca thu ngân."; return; } InfoMessage = "Đã lưu chính sách ca thu ngân vào dữ liệu thật."; IsEditing = false; await LoadAsync(ct); } catch (Exception ex) { ErrorMessage = $"Không lưu được chính sách ca thu ngân: {ex.Message}"; } finally { RaiseState(); } }
    [RelayCommand] private async Task ToggleActiveAsync(CancellationToken ct) { if (SelectedItem is null) return; var result = SelectedItem.IsActive ? _deactivate is null ? Missing() : await _deactivate.HandleAsync(new DeactivateHotelCashierShiftPolicyCommand(SelectedItem.HotelCashierShiftPolicyId, SelectedItem.RowVersion ?? []), ct) : _activate is null ? Missing() : await _activate.HandleAsync(new ActivateHotelCashierShiftPolicyCommand(SelectedItem.HotelCashierShiftPolicyId, SelectedItem.RowVersion ?? []), ct); if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; RaiseState(); return; } InfoMessage = result.Value!.IsActive ? "Đã bật chính sách ca thu ngân." : "Đã tạm ngưng chính sách ca thu ngân."; await LoadAsync(ct); }
    private void Fill(HotelCashierShiftPolicyDto x) { Code = x.Code; Name = x.Name; EffectiveFrom = x.EffectiveFrom.ToString("yyyy-MM-dd"); EffectiveTo = x.EffectiveTo.ToString("yyyy-MM-dd"); RequireOpeningFloat = x.RequireOpeningFloat; DefaultOpeningFloat = x.DefaultOpeningFloat.ToString("0.##"); RequireSupervisorClose = x.RequireSupervisorClose; VarianceApprovalThreshold = x.VarianceApprovalThreshold.ToString("0.##"); AllowCashTransfer = x.AllowCashTransfer; RequireHandoverNote = x.RequireHandoverNote; SupervisorRole = x.SupervisorRole ?? string.Empty; Description = x.Description ?? string.Empty; }
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private static Result<HotelCashierShiftPolicyDto> Missing() => Result<HotelCashierShiftPolicyDto>.Failure(Error.System("HOTEL_CASHIER_SHIFT_POLICY_COMMAND_MISSING", "Command is unavailable.")); private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
    partial void OnSelectedItemChanged(HotelCashierShiftPolicyDto? value) { if (!IsEditing && value is not null) Fill(value); RaiseState(); }
}
