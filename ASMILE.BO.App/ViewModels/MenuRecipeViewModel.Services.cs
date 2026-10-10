using ASMILE.BO.Application.DTOs;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class MenuRecipeViewModel
{
    private static readonly (string Code, string Label)[] ServiceTypeCatalog =
    [
        ("Equipment", "Thiết bị / sân khấu"),
        ("Staff", "Nhân sự biểu diễn"),
        ("Decor", "Trang trí"),
        ("Fee", "Phụ thu / phụ phí"),
        ("Other", "Khác")
    ];

    private void BuildServiceTypeOptions()
    {
        var source = new List<(string Code, string Label)>
        {
            ("All", "Tất cả")
        };

        source.AddRange(ServiceTypeCatalog
            .Concat(_allExtraServices
                .Select(x => x.ServiceType)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => ServiceTypeCatalog.All(s => !string.Equals(s.Code, x, StringComparison.OrdinalIgnoreCase)))
                .Select(x => (x, x)))
            .ToList());

        ResetCollection(ServiceTypeOptions, source.Select(x => new ServiceTypeOption { Code = x.Code, Label = x.Label }));

        if (!string.IsNullOrWhiteSpace(SelectedServiceTypeFilter)
            && !string.Equals(SelectedServiceTypeFilter, "All", StringComparison.OrdinalIgnoreCase)
            && ServiceTypeOptions.All(x => !string.Equals(x.Code, SelectedServiceTypeFilter, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedServiceTypeFilter = "All";
        }
    }

    private void CreateNewService()
    {
        IsCreatingNewMenu = true;
        EditingService = new SaveRestaurantExtraServiceRequest
        {
            ServiceType = "Other",
            VatRate = 0,
            IsVatIncluded = true,
            IsActive = true
        };
        SelectedMenuCard = null;
        SelectedWorkspaceTab = 0;
        IsEditingStatusActive = true;
        ClearRecipeState();
        MenuHistoryEntries.Clear();
        StatusMessage = "Nhập thông tin dịch vụ mới.";
        StatusIsSuccess = true;
        NotifyComputedState();
    }

    private async Task LoadAllExtraServicesAsync()
    {
        var services = await _alacarteRestaurantService.GetExtraServicesAsync();

        _allExtraServices.Clear();
        _allExtraServices.AddRange(services);

        MenuCards.Clear();
        foreach (var service in _allExtraServices.OrderBy(x => x.ServiceType).ThenBy(x => x.Name))
        {
            MenuCards.Add(new MenuItemCardState
            {
                Id = service.Id,
                Name = service.Name,
                Code = service.Code,
                CategoryName = GetServiceTypeDisplay(service.ServiceType),
                CategoryId = Guid.Empty,
                UnitPriceDisplay = $"{service.DefaultPrice:N0} đ",
                UnitPriceNumeric = service.DefaultPrice,
                RecipeStatusBadge = "Dịch vụ",
                ActiveStatusBadge = service.IsActive ? "Đang bán" : "Ngưng bán",
                HasRecipe = false,
                IsInactive = !service.IsActive,
                IsHotelService = true,
                ServiceType = service.ServiceType,
                ImagePath = service.ImagePath
            });
        }

        BuildServiceTypeOptions();
    }

    private static string GetServiceTypeDisplay(string? serviceType)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            return "Khác";
        }

        var match = ServiceTypeCatalog.FirstOrDefault(x => string.Equals(x.Code, serviceType, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(match.Code) ? serviceType : match.Label;
    }

    private void ApplySelectedService(RestaurantExtraServiceDto service)
    {
        EditingService = new SaveRestaurantExtraServiceRequest
        {
            Id = service.Id,
            Code = service.Code,
            Name = service.Name,
            ServiceType = string.IsNullOrWhiteSpace(service.ServiceType) ? "Other" : service.ServiceType,
            UnitName = string.IsNullOrWhiteSpace(service.UnitName) ? "lần" : service.UnitName,
            DefaultPrice = service.DefaultPrice,
            VatRate = service.VatRate,
            IsVatIncluded = service.IsVatIncluded,
            IsActive = service.IsActive,
            Note = service.Note,
            ImagePath = service.ImagePath,
            ImageUrl = service.ImageUrl
        };
        IsEditingStatusActive = service.IsActive;
        IsCreatingNewMenu = false;
        SelectedWorkspaceTab = 0;
        ClearRecipeState();
        MenuHistoryEntries.Clear();
        NotifyComputedState();
    }

    private bool ValidateEditingServiceForSave()
    {
        if (string.IsNullOrWhiteSpace(EditingService.Code))
        {
            StatusMessage = "Vui lòng nhập mã dịch vụ.";
            StatusIsSuccess = false;
            return false;
        }

        if (string.IsNullOrWhiteSpace(EditingService.Name))
        {
            StatusMessage = "Vui lòng nhập tên dịch vụ.";
            StatusIsSuccess = false;
            return false;
        }

        if (EditingService.DefaultPrice < 0)
        {
            StatusMessage = "Đơn giá dịch vụ không được âm.";
            StatusIsSuccess = false;
            return false;
        }

        if (EditingService.VatRate < 0 || EditingService.VatRate > 1)
        {
            StatusMessage = "VAT dịch vụ phải nằm trong khoảng 0..1.";
            StatusIsSuccess = false;
            return false;
        }

        return true;
    }

    private async Task<RestaurantExtraServiceDto> SaveServiceCoreAsync(string successMessage)
    {
        var request = new SaveRestaurantExtraServiceRequest
        {
            Id = EditingService.Id,
            Code = EditingService.Code.Trim().ToUpperInvariant(),
            Name = EditingService.Name.Trim(),
            ServiceType = string.IsNullOrWhiteSpace(EditingService.ServiceType) ? "Other" : EditingService.ServiceType,
            UnitName = string.IsNullOrWhiteSpace(EditingService.UnitName) ? "lần" : EditingService.UnitName.Trim(),
            DefaultPrice = EditingService.DefaultPrice,
            VatRate = EditingService.VatRate,
            IsVatIncluded = EditingService.IsVatIncluded,
            IsActive = IsEditingStatusActive,
            Note = string.IsNullOrWhiteSpace(EditingService.Note) ? null : EditingService.Note.Trim(),
            ImagePath = EditingService.ImagePath,
            ImageUrl = EditingService.ImageUrl
        };

        var saved = await _alacarteRestaurantService.SaveExtraServiceAsync(request);
        ApplySelectedService(saved);
        await LoadAllExtraServicesAsync();
        ApplyFilters();
        SelectedMenuCard = FilteredMenuCards.FirstOrDefault(x => x.Id == saved.Id)
            ?? MenuCards.FirstOrDefault(x => x.Id == saved.Id);

        StatusMessage = successMessage;
        StatusIsSuccess = true;
        return saved;
    }

    [RelayCommand]
    private async Task SaveCurrentCatalogItemAsync()
    {
        if (IsServiceCatalogSelected)
        {
            if (IsBusy || !ValidateEditingServiceForSave()) return;
            IsBusy = true;
            try
            {
                await SaveServiceCoreAsync("Đã lưu dịch vụ thành công.");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
                StatusIsSuccess = false;
            }
            finally
            {
                IsBusy = false;
                NotifyComputedState();
            }

            return;
        }

        await SaveAndCalculateCostAsync();
    }

    [RelayCommand]
    private async Task ToggleCurrentCatalogItemAsync()
    {
        if (IsServiceCatalogSelected)
        {
            if (EditingService.Id is null || IsBusy) return;

            IsBusy = true;
            try
            {
                IsEditingStatusActive = !IsEditingStatusActive;
                EditingService.IsActive = IsEditingStatusActive;
                await SaveServiceCoreAsync(IsEditingStatusActive ? "Đã kích hoạt dịch vụ." : "Đã ngưng bán dịch vụ.");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
                StatusIsSuccess = false;
            }
            finally
            {
                IsBusy = false;
                NotifyComputedState();
            }

            return;
        }

        await ToggleActiveAsync();
    }

    [RelayCommand]
    private void DuplicateCurrentCatalogItem()
    {
        if (IsServiceCatalogSelected)
        {
            if (EditingService.Id is null) return;

            IsCreatingNewMenu = true;
            EditingService = new SaveRestaurantExtraServiceRequest
            {
                Code = $"{EditingService.Code}_COPY",
                Name = $"{EditingService.Name} (sao chép)",
                ServiceType = EditingService.ServiceType,
                DefaultPrice = EditingService.DefaultPrice,
                VatRate = EditingService.VatRate,
                IsVatIncluded = EditingService.IsVatIncluded,
                IsActive = true,
                Note = EditingService.Note,
                ImagePath = EditingService.ImagePath,
                ImageUrl = EditingService.ImageUrl
            };
            SelectedMenuCard = null;
            SelectedWorkspaceTab = 0;
            StatusMessage = "Đã sao chép dịch vụ, hãy lưu để hoàn tất.";
            StatusIsSuccess = true;
            NotifyComputedState();
            return;
        }

        _ = DuplicateMenuItemAsync();
    }

    [RelayCommand]
    private async Task DeleteCurrentCatalogItemAsync()
    {
        if (IsServiceCatalogSelected)
        {
            if (EditingService.Id is null || IsBusy) return;

            var result = MessageBox.Show(
                $"Ngưng bán dịch vụ {EditingService.Name} ({EditingService.Code})?",
                "Xác nhận ngưng dịch vụ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                EditingService.IsActive = false;
                IsEditingStatusActive = false;
                await SaveServiceCoreAsync($"Đã ngưng sử dụng dịch vụ {EditingService.Name}.");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
                StatusIsSuccess = false;
            }
            finally
            {
                IsBusy = false;
                NotifyComputedState();
            }

            return;
        }

        await DeleteMenuAsync();
    }
}

public sealed class ServiceTypeOption
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
