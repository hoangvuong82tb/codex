using System.Collections.ObjectModel;
using System.Globalization;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Rate;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

/// <summary>Workspace cấu hình tỷ giá theo property; không dùng dữ liệu mẫu và không mở modal.</summary>
public sealed partial class HotelExchangeRateViewModel : ViewModelBase
{
    private readonly IExchangeRateQueryService _query;
    private readonly IExchangeRateCommandService _commands;
    private readonly IFrontOfficeExecutionContextAccessor _contextAccessor;
    private bool _initialized;

    public ObservableCollection<ExchangeRateDto> Items { get; } = [];
    [ObservableProperty] private ExchangeRateDto? selectedItem;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string currencyCode = string.Empty;
    [ObservableProperty] private string currencyName = string.Empty;
    [ObservableProperty] private string symbol = string.Empty;
    [ObservableProperty] private string buyRate = "1";
    [ObservableProperty] private string sellRate = "1";
    [ObservableProperty] private string hotelRate = "1";
    [ObservableProperty] private DateTime effectiveFrom = DateTime.Today;
    [ObservableProperty] private DateTime? effectiveTo;
    [ObservableProperty] private bool isDefault;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn ngoại tệ để xem và chỉnh sửa." : $"{SelectedItem.CurrencyCode} · {SelectedItem.CurrencyName}";
    public string SelectedStatus => SelectedItem is null ? "—" : SelectedItem.DisplayState switch
    {
        ExchangeRateDisplayState.Effective => "Đang hiệu lực",
        ExchangeRateDisplayState.Expiring => "Sắp hết hạn",
        ExchangeRateDisplayState.Expired => "Đã hết hạn",
        ExchangeRateDisplayState.Scheduled => "Chờ hiệu lực",
        ExchangeRateDisplayState.Inactive => "Tạm ngưng",
        _ => "Nháp"
    };

    public HotelExchangeRateViewModel(IExchangeRateQueryService query, IExchangeRateCommandService commands, IFrontOfficeExecutionContextAccessor contextAccessor)
    { _query = query; _commands = commands; _contextAccessor = contextAccessor; }

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty; InfoMessage = string.Empty;
        try
        {
            var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
            if (propertyId is not Guid id || id == Guid.Empty) { ErrorMessage = "Chưa có property context để tải tỷ giá thật."; return; }
            var result = await _query.SearchAsync(new ExchangeRateSearchRequest(id, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim()), ct);
            Items.Clear(); foreach (var item in result.OrderBy(x => x.CurrencyCode)) Items.Add(item);
            if (SelectedItem is null || Items.All(x => x.HotelExchangeRateId != SelectedItem.HotelExchangeRateId)) SelectedItem = Items.FirstOrDefault();
            InfoMessage = $"Đã tải {Items.Count} tỷ giá từ database thật theo cơ sở hiện tại.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được tỷ giá: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);

    [RelayCommand]
    private void NewItem()
    {
        SelectedItem = null; CurrencyCode = string.Empty; CurrencyName = string.Empty; Symbol = string.Empty;
        BuyRate = "1"; SellRate = "1"; HotelRate = "1"; EffectiveFrom = DateTime.Today; EffectiveTo = null; IsDefault = false; Notes = string.Empty;
        IsEditing = true; RaiseState();
    }

    [RelayCommand]
    private void EditItem()
    { if (SelectedItem is null) return; Fill(SelectedItem); IsEditing = true; RaiseState(); }

    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty; InfoMessage = string.Empty;
        if (!decimal.TryParse(BuyRate, NumberStyles.Number, CultureInfo.CurrentCulture, out var buy) ||
            !decimal.TryParse(SellRate, NumberStyles.Number, CultureInfo.CurrentCulture, out var sell) ||
            !decimal.TryParse(HotelRate, NumberStyles.Number, CultureInfo.CurrentCulture, out var hotel) ||
            buy <= 0 || sell <= 0 || hotel <= 0)
        { ErrorMessage = "Tỷ giá phải là số lớn hơn 0."; RaiseState(); return; }
        if (EffectiveTo is not null && EffectiveTo.Value.Date < EffectiveFrom.Date) { ErrorMessage = "Ngày hết hiệu lực không được trước ngày bắt đầu."; RaiseState(); return; }
        try
        {
            ExchangeRateDto saved;
            if (SelectedItem is null)
            {
                var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
                if (propertyId is not Guid id || id == Guid.Empty) { ErrorMessage = "Chưa có property context."; return; }
                saved = await _commands.CreateAsync(new CreateExchangeRateRequest(id, CurrencyCode, CurrencyName, Symbol, buy, sell, hotel, EffectiveFrom.ToUniversalTime(), EffectiveTo?.ToUniversalTime(), IsDefault, NullIfEmpty(Notes)), ct);
            }
            else
            {
                saved = await _commands.UpdateAsync(new UpdateExchangeRateRequest(SelectedItem.HotelExchangeRateId, CurrencyCode, CurrencyName, Symbol, buy, sell, hotel, EffectiveFrom.ToUniversalTime(), EffectiveTo?.ToUniversalTime(), NullIfEmpty(Notes), SelectedItem.RowVersion), ct);
            }
            InfoMessage = $"Đã lưu tỷ giá {saved.CurrencyCode} vào dữ liệu thật."; IsEditing = false; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được tỷ giá: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task SetDefaultAsync(CancellationToken ct)
    {
        if (SelectedItem is null) return;
        try { await _commands.SetDefaultAsync(new SetDefaultExchangeRateRequest(SelectedItem.HotelExchangeRateId, SelectedItem.RowVersion), ct); InfoMessage = $"Đã đặt {SelectedItem.CurrencyCode} làm tỷ giá mặc định."; await LoadAsync(ct); }
        catch (Exception ex) { ErrorMessage = $"Không đặt được tỷ giá mặc định: {ex.Message}"; RaiseState(); }
    }

    private void Fill(ExchangeRateDto x)
    { CurrencyCode = x.CurrencyCode; CurrencyName = x.CurrencyName; Symbol = x.Symbol; BuyRate = x.BuyRate.ToString(CultureInfo.CurrentCulture); SellRate = x.SellRate.ToString(CultureInfo.CurrentCulture); HotelRate = x.HotelRate.ToString(CultureInfo.CurrentCulture); EffectiveFrom = x.EffectiveFromUtc.ToLocalTime().Date; EffectiveTo = x.EffectiveToUtc?.ToLocalTime().Date; IsDefault = x.IsDefault; Notes = x.Notes ?? string.Empty; }
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
    partial void OnSelectedItemChanged(ExchangeRateDto? value) { if (!IsEditing && value is not null) Fill(value); RaiseState(); }
}
