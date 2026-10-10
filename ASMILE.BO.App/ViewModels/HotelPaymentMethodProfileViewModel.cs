using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelPaymentMethodProfileViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelPaymentMethodProfileCommandHandler? _create;
    private readonly UpdateHotelPaymentMethodProfileCommandHandler? _update;
    private readonly ActivateHotelPaymentMethodProfileCommandHandler? _activate;
    private readonly DeactivateHotelPaymentMethodProfileCommandHandler? _deactivate;
    private bool _initialized;

    public HotelPaymentMethodProfileViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelPaymentMethodProfileCommandHandler? create = null, UpdateHotelPaymentMethodProfileCommandHandler? update = null, ActivateHotelPaymentMethodProfileCommandHandler? activate = null, DeactivateHotelPaymentMethodProfileCommandHandler? deactivate = null)
    { _repository = repository; _contextAccessor = contextAccessor; _create = create; _update = update; _activate = activate; _deactivate = deactivate; }

    public ObservableCollection<HotelPaymentMethodProfileDto> Items { get; } = [];
    public IReadOnlyList<string> KindOptions { get; } = ["Tất cả", "Tiền mặt", "Chuyển khoản", "Thẻ", "QR", "Ví điện tử", "Công nợ", "Doanh nghiệp", "OTA", "Đặt cọc"];
    public IReadOnlyList<string> EditKindOptions { get; } = ["Tiền mặt", "Chuyển khoản", "Thẻ", "QR", "Ví điện tử", "Công nợ", "Doanh nghiệp", "OTA", "Đặt cọc"];
    [ObservableProperty] private HotelPaymentMethodProfileDto? selectedItem;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedKindFilter = "Tất cả";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string selectedKind = "Tiền mặt";
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string currencyCode = "VND";
    [ObservableProperty] private string bankAccount = string.Empty;
    [ObservableProperty] private string cardTerminal = string.Empty;
    [ObservableProperty] private string feeRate = "0";
    [ObservableProperty] private string accountingMapping = string.Empty;
    [ObservableProperty] private bool requireReference;
    [ObservableProperty] private bool requireApproval;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn một phương thức để xem và chỉnh sửa." : $"{SelectedItem.Code} · {SelectedItem.Name}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";
    public string SelectedKindLabel => SelectedItem is null ? "-" : KindLabel(SelectedItem.Kind);

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; }
            var kind = TryKind(SelectedKindFilter, out var selected) ? selected : (HotelPaymentMethodKind?)null;
            var result = await _repository.SearchPaymentMethodProfilesAsync(propertyId, kind, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), null, new PageRequest(1, 200), "kind", ct);
            Items.Clear(); foreach (var item in result.Items) Items.Add(item);
            if (SelectedItem is null || Items.All(x => x.HotelPaymentMethodProfileId != SelectedItem.HotelPaymentMethodProfileId)) SelectedItem = Items.FirstOrDefault();
            InfoMessage = $"Đã tải {Items.Count} phương thức thanh toán từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được phương thức thanh toán: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; SelectedKind = "Tiền mặt"; Code = string.Empty; Name = string.Empty; CurrencyCode = "VND"; BankAccount = string.Empty; CardTerminal = string.Empty; FeeRate = "0"; AccountingMapping = string.Empty; RequireReference = false; RequireApproval = false; IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void EditItem() { if (SelectedItem is null) return; Fill(SelectedItem); IsEditing = true; ErrorMessage = string.Empty; InfoMessage = string.Empty; RaiseState(); }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (!decimal.TryParse(FeeRate, out var fee) || !TryKind(SelectedKind, out var kind)) { ErrorMessage = "Loại hoặc phí thanh toán không hợp lệ."; RaiseState(); return; }
        try
        {
            Result<HotelPaymentMethodProfileDto> result;
            if (SelectedItem is null) { if (_create is null) { ErrorMessage = "Chưa đăng ký command cấu hình thật."; return; } result = await _create.HandleAsync(new CreateHotelPaymentMethodProfileCommand(Code, Name, kind, CurrencyCode, NullIfEmpty(BankAccount), NullIfEmpty(CardTerminal), fee, NullIfEmpty(AccountingMapping), RequireReference, RequireApproval), ct); }
            else { if (_update is null) { ErrorMessage = "Chưa đăng ký command cấu hình thật."; return; } result = await _update.HandleAsync(new UpdateHotelPaymentMethodProfileCommand(SelectedItem.HotelPaymentMethodProfileId, Code, Name, kind, CurrencyCode, NullIfEmpty(BankAccount), NullIfEmpty(CardTerminal), fee, NullIfEmpty(AccountingMapping), RequireReference, RequireApproval, SelectedItem.RowVersion ?? []), ct); }
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được cấu hình."; return; }
            InfoMessage = "Đã lưu phương thức thanh toán vào dữ liệu thật."; IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được cấu hình: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedItem is null) return;
        var result = SelectedItem.IsActive ? _deactivate is null ? Missing() : await _deactivate.HandleAsync(new DeactivateHotelPaymentMethodProfileCommand(SelectedItem.HotelPaymentMethodProfileId, SelectedItem.RowVersion ?? []), ct) : _activate is null ? Missing() : await _activate.HandleAsync(new ActivateHotelPaymentMethodProfileCommand(SelectedItem.HotelPaymentMethodProfileId, SelectedItem.RowVersion ?? []), ct);
        if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; RaiseState(); return; }
        InfoMessage = result.Value!.IsActive ? "Đã bật cấu hình." : "Đã tạm ngưng cấu hình."; await LoadAsync(ct);
    }

    private void Fill(HotelPaymentMethodProfileDto x) { SelectedKind = KindLabel(x.Kind); Code = x.Code; Name = x.Name; CurrencyCode = x.CurrencyCode; BankAccount = x.BankAccount ?? string.Empty; CardTerminal = x.CardTerminal ?? string.Empty; FeeRate = x.FeeRate.ToString("0.####"); AccountingMapping = x.AccountingMapping ?? string.Empty; RequireReference = x.RequireReference; RequireApproval = x.RequireApproval; }
    private static bool TryKind(string value, out HotelPaymentMethodKind kind) { kind = value switch { "Chuyển khoản" => HotelPaymentMethodKind.BankTransfer, "Thẻ" => HotelPaymentMethodKind.Card, "QR" => HotelPaymentMethodKind.Qr, "Ví điện tử" => HotelPaymentMethodKind.EWallet, "Công nợ" => HotelPaymentMethodKind.Credit, "Doanh nghiệp" => HotelPaymentMethodKind.Company, "OTA" => HotelPaymentMethodKind.Ota, "Đặt cọc" => HotelPaymentMethodKind.Deposit, _ => HotelPaymentMethodKind.Cash }; return value is not "Tất cả" and not ""; }
    private static string KindLabel(HotelPaymentMethodKind kind) => kind switch { HotelPaymentMethodKind.BankTransfer => "Chuyển khoản", HotelPaymentMethodKind.Card => "Thẻ", HotelPaymentMethodKind.Qr => "QR", HotelPaymentMethodKind.EWallet => "Ví điện tử", HotelPaymentMethodKind.Credit => "Công nợ", HotelPaymentMethodKind.Company => "Doanh nghiệp", HotelPaymentMethodKind.Ota => "OTA", HotelPaymentMethodKind.Deposit => "Đặt cọc", _ => "Tiền mặt" };
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Result<HotelPaymentMethodProfileDto> Missing() => Result<HotelPaymentMethodProfileDto>.Failure(Error.System("HOTEL_PAYMENT_METHOD_COMMAND_MISSING", "Command is unavailable."));
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); OnPropertyChanged(nameof(SelectedKindLabel)); }
    partial void OnSelectedItemChanged(HotelPaymentMethodProfileDto? value) { if (!IsEditing && value is not null) Fill(value); RaiseState(); }
}
