using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class HotelHousekeepingSupplyCatalogViewModel : ViewModelBase
{
    private readonly IHousekeepingSupplyMasterDataService _service;
    private readonly IFrontOfficeExecutionContextAccessor _contextAccessor;
    private bool _initialized;

    public HotelHousekeepingSupplyCatalogViewModel(IHousekeepingSupplyMasterDataService service, IFrontOfficeExecutionContextAccessor contextAccessor)
    {
        _service = service;
        _contextAccessor = contextAccessor;
    }

    public ObservableCollection<HousekeepingLinenTypeDto> LinenTypes { get; } = [];
    public ObservableCollection<HousekeepingAmenityTypeDto> AmenityTypes { get; } = [];

    [ObservableProperty] private HousekeepingLinenTypeDto? selectedLinen;
    [ObservableProperty] private HousekeepingAmenityTypeDto? selectedAmenity;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedKind = "linen";
    [ObservableProperty] private int selectedTabIndex;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string category = string.Empty;
    [ObservableProperty] private string defaultParLevel = "1";
    [ObservableProperty] private string limitValue = "10";
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsLinenTab => string.Equals(SelectedKind, "linen", StringComparison.OrdinalIgnoreCase);
    public bool IsAmenityTab => !IsLinenTab;
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => IsLinenTab
        ? SelectedLinen is null ? "Chọn loại đồ vải để xem chi tiết." : $"{SelectedLinen.Code} · {SelectedLinen.Name}"
        : SelectedAmenity is null ? "Chọn loại tiện nghi để xem chi tiết." : $"{SelectedAmenity.Code} · {SelectedAmenity.Name}";
    public string SelectedStatus => IsLinenTab ? (SelectedLinen?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng") : (SelectedAmenity?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng");
    public string LimitLabel => IsLinenTab ? "Số vòng giặt tối đa" : "Ngưỡng cảnh báo tối thiểu";
    public string DataMessage => $"Đồ vải: {LinenTypes.Count} · Tiện nghi: {AmenityTypes.Count} · Theo property hiện tại";

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (_initialized) return;
        _initialized = true;
        await LoadAsync(ct);
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        try
        {
            var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
            if (propertyId is not Guid id || id == Guid.Empty)
            {
                ErrorMessage = "Chưa chọn cơ sở Front Office; không thể đọc danh mục.";
                return;
            }

            var linen = await _service.SearchLinenTypesAsync(id, NullIfEmpty(SearchText), null, ct);
            var amenity = await _service.SearchAmenityTypesAsync(id, NullIfEmpty(SearchText), null, ct);
            LinenTypes.Clear(); AmenityTypes.Clear();
            foreach (var item in linen) LinenTypes.Add(item);
            foreach (var item in amenity) AmenityTypes.Add(item);
            if (SelectedLinen is null || LinenTypes.All(x => x.Id != SelectedLinen.Id)) SelectedLinen = LinenTypes.FirstOrDefault();
            if (SelectedAmenity is null || AmenityTypes.All(x => x.Id != SelectedAmenity.Id)) SelectedAmenity = AmenityTypes.FirstOrDefault();
            InfoMessage = "Đã tải danh mục master đồ vải và tiện nghi từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được danh mục: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand]
    private async Task SelectLinenAsync(CancellationToken ct) { SelectedKind = "linen"; SelectedTabIndex = 0; IsEditing = false; RaiseState(); await LoadAsync(ct); }

    [RelayCommand]
    private async Task SelectAmenityAsync(CancellationToken ct) { SelectedKind = "amenity"; SelectedTabIndex = 1; IsEditing = false; RaiseState(); await LoadAsync(ct); }

    [RelayCommand]
    private void NewLinen()
    {
        SelectedKind = "linen"; SelectedTabIndex = 0; SelectedLinen = null; Code = string.Empty; Name = string.Empty; Category = "Phòng ngủ"; DefaultParLevel = "1"; LimitValue = "100"; IsActive = true; IsEditing = true; RaiseState();
    }

    [RelayCommand]
    private void NewAmenity()
    {
        SelectedKind = "amenity"; SelectedTabIndex = 1; SelectedAmenity = null; Code = string.Empty; Name = string.Empty; Category = "Phòng ngủ"; DefaultParLevel = "1"; LimitValue = "0"; IsActive = true; IsEditing = true; RaiseState();
    }

    [RelayCommand]
    private void EditItem()
    {
        if (IsLinenTab && SelectedLinen is not null) { Fill(SelectedLinen); IsEditing = true; }
        else if (IsAmenityTab && SelectedAmenity is not null) { Fill(SelectedAmenity); IsEditing = true; }
        RaiseState();
    }

    [RelayCommand]
    private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (!int.TryParse(DefaultParLevel, out var par) || par < 0 || !int.TryParse(LimitValue, out var limit) || limit < 0 || string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Category))
        {
            ErrorMessage = "Mã, tên, nhóm và các định mức phải hợp lệ."; RaiseState(); return;
        }
        try
        {
            var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
            if (propertyId is not Guid id || id == Guid.Empty) { ErrorMessage = "Chưa chọn cơ sở Front Office."; return; }
            if (IsLinenTab)
            {
                var result = await _service.SaveLinenTypeAsync(id, new(SelectedLinen?.Id, Code, Name, Category, par, limit, SelectedLinen?.RowVersion), ct);
                SelectedLinen = result;
            }
            else
            {
                var result = await _service.SaveAmenityTypeAsync(id, new(SelectedAmenity?.Id, Code, Name, Category, par, limit, SelectedAmenity?.RowVersion), ct);
                SelectedAmenity = result;
            }
            IsEditing = false; InfoMessage = "Đã lưu danh mục độc lập theo property."; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được danh mục: {ex.Message}"; }
        finally { RaiseState(); }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        try
        {
            var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
            if (propertyId is not Guid id || id == Guid.Empty) { ErrorMessage = "Chưa chọn cơ sở Front Office."; return; }
            if (IsLinenTab && SelectedLinen is not null) SelectedLinen = await _service.SetLinenTypeActiveAsync(id, SelectedLinen.Id, !SelectedLinen.IsActive, SelectedLinen.RowVersion, ct);
            else if (IsAmenityTab && SelectedAmenity is not null) SelectedAmenity = await _service.SetAmenityTypeActiveAsync(id, SelectedAmenity.Id, !SelectedAmenity.IsActive, SelectedAmenity.RowVersion, ct);
            InfoMessage = "Đã cập nhật trạng thái danh mục."; await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không đổi được trạng thái: {ex.Message}"; }
        finally { RaiseState(); }
    }

    private void Fill(HousekeepingLinenTypeDto item) { Code = item.Code; Name = item.Name; Category = item.Category; DefaultParLevel = item.DefaultParLevel.ToString(); LimitValue = item.MaxWashCycles.ToString(); IsActive = item.IsActive; }
    private void Fill(HousekeepingAmenityTypeDto item) { Code = item.Code; Name = item.Name; Category = item.Category; DefaultParLevel = item.DefaultParLevel.ToString(); LimitValue = item.MinAlertThreshold.ToString(); IsActive = item.IsActive; }
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void RaiseState() { OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); OnPropertyChanged(nameof(IsLinenTab)); OnPropertyChanged(nameof(IsAmenityTab)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); OnPropertyChanged(nameof(LimitLabel)); OnPropertyChanged(nameof(DataMessage)); }
    partial void OnSelectedKindChanged(string value) { SelectedTabIndex = string.Equals(value, "amenity", StringComparison.OrdinalIgnoreCase) ? 1 : 0; RaiseState(); }
    partial void OnSelectedTabIndexChanged(int value) { SelectedKind = value == 1 ? "amenity" : "linen"; RaiseState(); }
    partial void OnSelectedLinenChanged(HousekeepingLinenTypeDto? value) { if (IsLinenTab && !IsEditing && value is not null) Fill(value); RaiseState(); }
    partial void OnSelectedAmenityChanged(HousekeepingAmenityTypeDto? value) { if (IsAmenityTab && !IsEditing && value is not null) Fill(value); RaiseState(); }
}
