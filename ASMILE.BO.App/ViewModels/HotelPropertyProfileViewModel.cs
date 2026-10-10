using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelPropertyProfileViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelPropertyProfileCommandHandler? _createHandler;
    private readonly UpdateHotelPropertyProfileCommandHandler? _updateHandler;
    private readonly ActivateHotelPropertyProfileCommandHandler? _activateHandler;
    private readonly DeactivateHotelPropertyProfileCommandHandler? _deactivateHandler;
    private bool _initialized;
    public HotelPropertyProfileViewModel(IHotelStructureMasterDataRepository? repository = null, IFrontOfficeExecutionContextAccessor? contextAccessor = null, CreateHotelPropertyProfileCommandHandler? createHandler = null, UpdateHotelPropertyProfileCommandHandler? updateHandler = null, ActivateHotelPropertyProfileCommandHandler? activateHandler = null, DeactivateHotelPropertyProfileCommandHandler? deactivateHandler = null)
    { _repository = repository; _contextAccessor = contextAccessor; _createHandler = createHandler; _updateHandler = updateHandler; _activateHandler = activateHandler; _deactivateHandler = deactivateHandler; }
    [ObservableProperty] private HotelPropertyProfileDto? profile;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string code = "HOTEL";
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string address = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string taxCode = string.Empty;
    [ObservableProperty] private string timeZoneId = "Asia/Ho_Chi_Minh";
    [ObservableProperty] private string currencyCode = "VND";
    [ObservableProperty] private string checkInTime = "14:00";
    [ObservableProperty] private string checkOutTime = "12:00";
    [ObservableProperty] private string businessDate = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string Status => Profile?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";
    public string ScopeSummary => _contextAccessor?.Current?.Property.PropertyId.Value is { } id ? $"Property: {id:D}" : "Chưa có property context";
    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }
    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; }
            Profile = await _repository.GetPropertyProfileDtoAsync(propertyId, ct);
            if (Profile is null) { IsEditing = true; BusinessDate = DateTime.Today.ToString("yyyy-MM-dd"); InfoMessage = "Chưa có hồ sơ cơ sở. Nhập thông tin để tạo cấu hình đầu tiên."; }
            else { Fill(Profile); InfoMessage = "Đã tải hồ sơ cơ sở từ dữ liệu thật."; }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được hồ sơ cơ sở: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }
    [RelayCommand] private void EditItem() { if (Profile is not null) { Fill(Profile); IsEditing = true; RaiseState(); } }
    [RelayCommand] private void CancelEdit() { if (Profile is not null) Fill(Profile); IsEditing = false; RaiseState(); }
    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (!TryMinutes(CheckInTime, out var checkIn) || !TryMinutes(CheckOutTime, out var checkOut) || !DateTime.TryParse(BusinessDate, out var date)) { ErrorMessage = "Giờ nhận/trả phải là HH:mm và business date phải hợp lệ (yyyy-MM-dd)."; RaiseState(); return; }
        try
        {
            Result<HotelPropertyProfileDto> result;
            if (Profile is null)
            {
                if (_createHandler is null) { ErrorMessage = "Chưa đăng ký command hồ sơ cơ sở thật."; return; }
                result = await _createHandler.HandleAsync(new CreateHotelPropertyProfileCommand(Code, Name, NullIfEmpty(Address), NullIfEmpty(Phone), NullIfEmpty(Email), NullIfEmpty(TaxCode), TimeZoneId, CurrencyCode, checkIn, checkOut, date.Date), ct);
            }
            else
            {
                if (_updateHandler is null) { ErrorMessage = "Chưa đăng ký command hồ sơ cơ sở thật."; return; }
                result = await _updateHandler.HandleAsync(new UpdateHotelPropertyProfileCommand(Profile.HotelPropertyProfileId, Code, Name, NullIfEmpty(Address), NullIfEmpty(Phone), NullIfEmpty(Email), NullIfEmpty(TaxCode), TimeZoneId, CurrencyCode, checkIn, checkOut, date.Date, Profile.RowVersion ?? []), ct);
            }
            if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không lưu được hồ sơ cơ sở."; return; }
            Profile = result.Value; IsEditing = false; InfoMessage = "Đã lưu hồ sơ cơ sở và business date.";
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được hồ sơ cơ sở: {ex.Message}"; }
        finally { RaiseState(); }
    }
    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (Profile is null) return;
        var result = Profile.IsActive ? _deactivateHandler is null ? Missing() : await _deactivateHandler.HandleAsync(new DeactivateHotelPropertyProfileCommand(Profile.HotelPropertyProfileId, Profile.RowVersion ?? []), ct) : _activateHandler is null ? Missing() : await _activateHandler.HandleAsync(new ActivateHotelPropertyProfileCommand(Profile.HotelPropertyProfileId, Profile.RowVersion ?? []), ct);
        if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái."; RaiseState(); return; }
        var updated = result.Value!; Profile = updated; InfoMessage = updated.IsActive ? "Đã bật hồ sơ cơ sở." : "Đã tạm ngưng hồ sơ cơ sở."; RaiseState();
    }
    private void Fill(HotelPropertyProfileDto value) { Code = value.Code; Name = value.Name; Address = value.Address ?? string.Empty; Phone = value.Phone ?? string.Empty; Email = value.Email ?? string.Empty; TaxCode = value.TaxCode ?? string.Empty; TimeZoneId = value.TimeZoneId; CurrencyCode = value.CurrencyCode; CheckInTime = TimeText(value.CheckInMinutes); CheckOutTime = TimeText(value.CheckOutMinutes); BusinessDate = value.BusinessDate.ToString("yyyy-MM-dd"); }
    private static bool TryMinutes(string value, out int minutes) { minutes = 0; if (!TimeSpan.TryParse(value, out var time) || time.TotalMinutes < 0 || time.TotalMinutes > 1439) return false; minutes = (int)time.TotalMinutes; return time.Seconds == 0; }
    private static string TimeText(int value) => $"{value / 60:00}:{value % 60:00}";
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Result<HotelPropertyProfileDto> Missing() => Result<HotelPropertyProfileDto>.Failure(Error.System("HOTEL_PROPERTY_PROFILE_COMMAND_MISSING", "Command is unavailable."));
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(Status)); OnPropertyChanged(nameof(ScopeSummary)); }
}
