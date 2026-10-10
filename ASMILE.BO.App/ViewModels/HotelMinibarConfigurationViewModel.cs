using System.Collections.ObjectModel;
using ASMILE.BO.App.Services;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

/// <summary>Property-scoped minibar configuration backed by HangMinibarFO.</summary>
public sealed partial class HotelMinibarConfigurationViewModel : ViewModelBase
{
    private readonly IHotelMinibarMasterDataService _service;
    private readonly IFrontOfficeExecutionContextAccessor _contextAccessor;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<HotelMinibarItemProfileDto> Items { get; } = [];
    [ObservableProperty] private HotelMinibarItemProfileEditModel editing = NewItem();
    [ObservableProperty] private HotelMinibarItemProfileDto? selectedItem;
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    public bool HasSelection => SelectedItem is not null;
    public bool CanDelete => SelectedItem is not null;
    public string StatusActionText => SelectedItem?.IsActive == true ? "Ngừng kinh doanh" : "Kích hoạt";
    public int ActiveCount => Items.Count(x => x.IsActive);
    public string DataMessage => Items.Count == 0 ? "Chưa có cấu hình minibar theo cơ sở. Hãy liên kết mã hàng tồn kho rồi lưu cấu hình." : $"{Items.Count} cấu hình minibar · {ActiveCount} đang kinh doanh · dữ liệu theo cơ sở lưu trú";

    public HotelMinibarConfigurationViewModel(IHotelMinibarMasterDataService service, IFrontOfficeExecutionContextAccessor contextAccessor, INotificationService notificationService, IDialogService dialogService)
    {
        _service = service; _contextAccessor = contextAccessor; _notificationService = notificationService; _dialogService = dialogService; _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        IsBusy = true; ErrorMessage = string.Empty;
        try
        {
            var rows = await _service.SearchAsync(CurrentPropertyId(), Keyword, null, cancellationToken);
            Items.Clear(); foreach (var row in rows) Items.Add(row);
            if (SelectedItem is not null) SelectedItem = Items.FirstOrDefault(x => x.HotelMinibarItemProfileId == SelectedItem.HotelMinibarItemProfileId);
            InfoMessage = $"Đã tải {Items.Count} cấu hình minibar từ dữ liệu thật của cơ sở."; NotifySummary();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể tải cấu hình minibar: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void New() { SelectedItem = null; Editing = NewItem(); ErrorMessage = string.Empty; InfoMessage = "Mã hàng phải tồn tại trong danh mục hàng hóa kho."; }
    [RelayCommand] private async Task ClearFiltersAsync() { Keyword = string.Empty; await LoadAsync(); }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var saved = await _service.SaveAsync(CurrentPropertyId(), Editing.ToInput(SelectedItem?.HotelMinibarItemProfileId, SelectedItem?.RowVersion), CurrentActorId(), CancellationToken.None);
            _notificationService.Show("Đã lưu cấu hình minibar theo cơ sở."); await LoadAsync(); SelectedItem = Items.FirstOrDefault(x => x.HotelMinibarItemProfileId == saved.HotelMinibarItemProfileId);
        }
        catch (Exception ex) { _dialogService.Error(ex.Message); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (SelectedItem is null) return;
        var action = SelectedItem.IsActive ? "ngừng kinh doanh" : "kích hoạt";
        if (!_dialogService.Confirm($"Bạn có chắc muốn {action} mặt hàng minibar đang chọn?")) return;
        try { await _service.SetActiveAsync(CurrentPropertyId(), SelectedItem.HotelMinibarItemProfileId, !SelectedItem.IsActive, SelectedItem.RowVersion, CurrentActorId(), CancellationToken.None); _notificationService.Show(SelectedItem.IsActive ? "Đã ngừng cấu hình minibar." : "Đã kích hoạt cấu hình minibar."); await LoadAsync(); }
        catch (Exception ex) { _dialogService.Error(ex.Message); }
    }

    partial void OnSelectedItemChanged(HotelMinibarItemProfileDto? value) { if (value is not null) Editing = HotelMinibarItemProfileEditModel.From(value); OnPropertyChanged(nameof(HasSelection)); OnPropertyChanged(nameof(CanDelete)); OnPropertyChanged(nameof(StatusActionText)); }
    private Guid CurrentPropertyId() => _contextAccessor.Current?.Property.PropertyId.Value ?? throw new InvalidOperationException("Chưa có property context để cấu hình minibar.");
    private Guid? CurrentActorId() => _contextAccessor.Current?.Actor.ActorId?.Value;
    private void NotifySummary() { OnPropertyChanged(nameof(ActiveCount)); OnPropertyChanged(nameof(DataMessage)); OnPropertyChanged(nameof(HasSelection)); OnPropertyChanged(nameof(CanDelete)); OnPropertyChanged(nameof(StatusActionText)); }
    private static HotelMinibarItemProfileEditModel NewItem() => new() { EffectiveFrom = DateTime.Today, EffectiveTo = DateTime.Today.AddYears(1), VatRate = 10m, StandardQuantity = 1m, MaxStock = 1m, IsActive = true };
}

public sealed class HotelMinibarItemProfileEditModel
{
    public int ItemId { get; set; } public string ItemCode { get; set; } = string.Empty; public string ItemName { get; set; } = string.Empty; public string Unit { get; set; } = string.Empty; public string Category { get; set; } = string.Empty; public decimal RetailPrice { get; set; } public decimal VatRate { get; set; } public decimal StandardQuantity { get; set; } public decimal MinStock { get; set; } public decimal MaxStock { get; set; } public string ChargeCode { get; set; } = string.Empty; public string ServiceChargeCode { get; set; } = string.Empty; public bool AutoPostToFolio { get; set; } public bool RequiresApproval { get; set; } public int ReplenishmentLeadMinutes { get; set; } public DateTime EffectiveFrom { get; set; } = DateTime.Today; public DateTime EffectiveTo { get; set; } = DateTime.Today.AddYears(1); public bool IsActive { get; set; } = true;
    public HotelMinibarItemProfileInput ToInput(Guid? id, byte[]? rowVersion) => new(id, ItemId, ItemCode, ItemName, Unit, Category, RetailPrice, VatRate, StandardQuantity, MinStock, MaxStock, string.IsNullOrWhiteSpace(ChargeCode) ? null : ChargeCode, string.IsNullOrWhiteSpace(ServiceChargeCode) ? null : ServiceChargeCode, AutoPostToFolio, RequiresApproval, ReplenishmentLeadMinutes, EffectiveFrom, EffectiveTo, rowVersion);
    public static HotelMinibarItemProfileEditModel From(HotelMinibarItemProfileDto x) => new() { ItemId = x.ItemId, ItemCode = x.ItemCode, ItemName = x.ItemName, Unit = x.Unit, Category = x.Category, RetailPrice = x.RetailPrice, VatRate = x.VatRate, StandardQuantity = x.StandardQuantity, MinStock = x.MinStock, MaxStock = x.MaxStock, ChargeCode = x.ChargeCode ?? string.Empty, ServiceChargeCode = x.ServiceChargeCode ?? string.Empty, AutoPostToFolio = x.AutoPostToFolio, RequiresApproval = x.RequiresApproval, ReplenishmentLeadMinutes = x.ReplenishmentLeadMinutes, EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, IsActive = x.IsActive };
}
