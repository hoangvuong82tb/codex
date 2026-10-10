using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelBookingSourceProfileViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelBookingSourceProfileCommandHandler? _create;
    private readonly UpdateHotelBookingSourceProfileCommandHandler? _update;
    private readonly ActivateHotelBookingSourceProfileCommandHandler? _activate;
    private readonly DeactivateHotelBookingSourceProfileCommandHandler? _deactivate;
    private bool _initialized;
    public HotelBookingSourceProfileViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelBookingSourceProfileCommandHandler? create = null, UpdateHotelBookingSourceProfileCommandHandler? update = null, ActivateHotelBookingSourceProfileCommandHandler? activate = null, DeactivateHotelBookingSourceProfileCommandHandler? deactivate = null) { _repository = repository; _contextAccessor = contextAccessor; _create = create; _update = update; _activate = activate; _deactivate = deactivate; }
    public ObservableCollection<HotelBookingSourceProfileDto> Items { get; } = [];
    public IReadOnlyList<string> KindOptions { get; } = ["Tất cả", "Walk-in", "Phone", "Email", "Website", "Facebook", "Zalo", "OTA", "Travel Agent", "Corporate", "Referral", "Repeat Guest"];
    [ObservableProperty] private HotelBookingSourceProfileDto? selectedItem;
    [ObservableProperty] private string selectedKind = "Tất cả";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string kind = "Walk-in";
    [ObservableProperty] private bool isCommissionable;
    [ObservableProperty] private bool requiresReference;
    [ObservableProperty] private string defaultMarketSegmentCode = string.Empty;
    [ObservableProperty] private string trackingCode = string.Empty;
    [ObservableProperty] private string effectiveFrom = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string effectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn nguồn đặt phòng để xem và chỉnh sửa." : $"{SelectedItem.Code} · {SelectedItem.Name}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";
    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }
    [RelayCommand] private async Task LoadAsync(CancellationToken ct) { IsLoading = true; ErrorMessage = string.Empty; try { if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; } var result = await _repository.SearchBookingSourceProfilesAsync(propertyId, ParseKind(SelectedKind), string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "kind", ct); Items.Clear(); foreach (var item in result.Items) Items.Add(item); if (SelectedItem is null || Items.All(x => x.HotelBookingSourceProfileId != SelectedItem.HotelBookingSourceProfileId)) SelectedItem = Items.FirstOrDefault(); InfoMessage = $"Đã tải {Items.Count} nguồn đặt phòng từ dữ liệu thật."; } catch (OperationCanceledException) when (ct.IsCancellationRequested) { } catch (Exception ex) { ErrorMessage = $"Không tải được nguồn đặt phòng: {ex.Message}"; } finally { IsLoading = false; RaiseState(); } }
    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; Code = string.Empty; Name = string.Empty; Kind = "Walk-in"; IsCommissionable = false; RequiresReference = false; DefaultMarketSegmentCode = string.Empty; TrackingCode = string.Empty; EffectiveFrom = DateTime.Today.ToString("yyyy-MM-dd"); EffectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"); Description = string.Empty; IsEditing = true; RaiseState(); }
    [RelayCommand] private void EditItem() { if (SelectedItem is null) return; Fill(SelectedItem); IsEditing = true; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }
    [RelayCommand] private async Task SaveAsync(CancellationToken ct) { ErrorMessage = string.Empty; if (!TryKind(Kind, out var sourceKind) || !DateTime.TryParse(EffectiveFrom, out var from) || !DateTime.TryParse(EffectiveTo, out var to)) { ErrorMessage = "Loại hoặc khoảng hiệu lực không hợp lệ."; RaiseState(); return; } try { Result<HotelBookingSourceProfileDto> result = SelectedItem is null ? _create is null ? Missing() : await _create.HandleAsync(new CreateHotelBookingSourceProfileCommand(Code, Name, sourceKind, IsCommissionable, RequiresReference, NullIfEmpty(DefaultMarketSegmentCode), NullIfEmpty(TrackingCode), from, to, NullIfEmpty(Description)), ct) : _update is null ? Missing() : await _update.HandleAsync(new UpdateHotelBookingSourceProfileCommand(SelectedItem.HotelBookingSourceProfileId, Code, Name, sourceKind, IsCommissionable, RequiresReference, NullIfEmpty(DefaultMarketSegmentCode), NullIfEmpty(TrackingCode), from, to, NullIfEmpty(Description), SelectedItem.RowVersion ?? []), ct); if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được nguồn đặt phòng."; return; } InfoMessage = "Đã lưu nguồn đặt phòng vào dữ liệu thật."; IsEditing = false; await LoadAsync(ct); } catch (Exception ex) { ErrorMessage = $"Không lưu được nguồn đặt phòng: {ex.Message}"; } finally { RaiseState(); } }
    [RelayCommand] private async Task ToggleActiveAsync(CancellationToken ct) { if (SelectedItem is null) return; var result = SelectedItem.IsActive ? _deactivate is null ? Missing() : await _deactivate.HandleAsync(new DeactivateHotelBookingSourceProfileCommand(SelectedItem.HotelBookingSourceProfileId, SelectedItem.RowVersion ?? []), ct) : _activate is null ? Missing() : await _activate.HandleAsync(new ActivateHotelBookingSourceProfileCommand(SelectedItem.HotelBookingSourceProfileId, SelectedItem.RowVersion ?? []), ct); if (result.IsFailure) ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; else await LoadAsync(ct); RaiseState(); }
    private void Fill(HotelBookingSourceProfileDto x) { Code = x.Code; Name = x.Name; Kind = KindLabel(x.Kind); IsCommissionable = x.IsCommissionable; RequiresReference = x.RequiresReference; DefaultMarketSegmentCode = x.DefaultMarketSegmentCode ?? string.Empty; TrackingCode = x.TrackingCode ?? string.Empty; EffectiveFrom = x.EffectiveFrom.ToString("yyyy-MM-dd"); EffectiveTo = x.EffectiveTo.ToString("yyyy-MM-dd"); Description = x.Description ?? string.Empty; }
    private static HotelBookingSourceKind? ParseKind(string value) => value == "Tất cả" ? null : TryKind(value, out var kind) ? kind : null;
    private static bool TryKind(string value, out HotelBookingSourceKind kind) => Enum.TryParse(value.Replace("-", string.Empty).Replace(" ", string.Empty), true, out kind);
    private static string KindLabel(HotelBookingSourceKind value) => value switch { HotelBookingSourceKind.WalkIn => "Walk-in", HotelBookingSourceKind.TravelAgent => "Travel Agent", HotelBookingSourceKind.RepeatGuest => "Repeat Guest", HotelBookingSourceKind.Ota => "OTA", _ => value.ToString() };
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Result<HotelBookingSourceProfileDto> Missing() => Result<HotelBookingSourceProfileDto>.Failure(Error.System("HOTEL_BOOKING_SOURCE_COMMAND_MISSING", "Command is unavailable."));
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
    partial void OnSelectedItemChanged(HotelBookingSourceProfileDto? value) { if (!IsEditing && value is not null) Fill(value); RaiseState(); }
}
