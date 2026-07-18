using ASMILE.BO.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class MenuRecipeViewModel
{
    private static readonly JsonSerializerOptions PreparationAreaJsonOptions = new() { WriteIndented = true };

    partial void OnEditPreparationAreaNameChanged(string value) => OnPropertyChanged(nameof(CanSavePreparationArea));

    private string GetPreparationAreaSettingsPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ASMILE-BO",
            "Data",
            "Settings",
            "menu-preparation-areas.json");

    private void LoadPreparationAreaSettings()
    {
        try
        {
            var path = GetPreparationAreaSettingsPath();
            List<PreparationAreaSettingState>? states = null;
            if (File.Exists(path))
            {
                states = JsonSerializer.Deserialize<List<PreparationAreaSettingState>>(File.ReadAllText(path), PreparationAreaJsonOptions);
            }

            var rows = (states is { Count: > 0 } ? states.Select(ToPreparationAreaRow) : GetDefaultPreparationAreaRows())
                .Where(x => !string.IsNullOrWhiteSpace(x.Code) && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            ResetCollection(PreparationAreaSettings, rows);
            RefreshPreparationAreaOptions();
            ClearPreparationAreaForm();
        }
        catch
        {
            ResetCollection(PreparationAreaSettings, GetDefaultPreparationAreaRows());
            RefreshPreparationAreaOptions();
            ClearPreparationAreaForm();
        }
    }

    private async Task SavePreparationAreaSettingsFileAsync()
    {
        var path = GetPreparationAreaSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var states = PreparationAreaSettings
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => new PreparationAreaSettingState
            {
                Code = x.Code,
                Name = x.Name,
                StationGroup = x.StationGroup,
                IsActive = x.IsActive
            })
            .ToList();
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(states, PreparationAreaJsonOptions));
    }

    private void RefreshPreparationAreaOptions()
    {
        ResetCollection(PreparationAreas, PreparationAreaSettings
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => x.Code));

        if (PreparationAreas.Count == 0)
        {
            PreparationAreas.Add(PreparationAreaSettingRow.DefaultBreakfastKitchenCode);
        }

        if (string.IsNullOrWhiteSpace(SelectedPreparationArea) || !PreparationAreas.Contains(SelectedPreparationArea))
        {
            SelectedPreparationArea = GetDefaultPreparationAreaCode();
        }

        OnPropertyChanged(nameof(PreparationAreaSettings));
    }

    private string GetDefaultPreparationAreaCode()
        => PreparationAreaSettings.FirstOrDefault(x => x.IsActive && string.Equals(x.StationGroup, "Bếp", StringComparison.OrdinalIgnoreCase))?.Code
            ?? PreparationAreaSettingRow.DefaultBreakfastKitchenCode;

    private string ResolvePreparationAreaSelection(string? preparationArea)
    {
        if (string.IsNullOrWhiteSpace(preparationArea))
        {
            return GetDefaultPreparationAreaCode();
        }

        var normalizedCode = NormalizePreparationAreaCode(preparationArea);
        if (PreparationAreaSettings.Any(x => string.Equals(x.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            return normalizedCode;
        }

        var inferred = CreateFallbackPreparationArea(normalizedCode, preparationArea.Trim());
        PreparationAreaSettings.Add(inferred);
        RefreshPreparationAreaOptions();
        return inferred.Code;
    }

    private static string NormalizePreparationAreaCode(string? preparationArea)
    {
        var value = (preparationArea ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return PreparationAreaSettingRow.DefaultBreakfastKitchenCode;
        }

        if (value.StartsWith("BAR", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "Bar", StringComparison.OrdinalIgnoreCase))
        {
            return PreparationAreaSettingRow.DefaultBarCode;
        }

        if (value.StartsWith("KITCHEN_BREAKFAST", StringComparison.OrdinalIgnoreCase))
        {
            return PreparationAreaSettingRow.DefaultBreakfastKitchenCode;
        }

        if (value.StartsWith("KITCHEN_PARTY", StringComparison.OrdinalIgnoreCase))
        {
            return "KITCHEN_PARTY";
        }

        if (value.StartsWith("KITCHEN_RESTAURANT", StringComparison.OrdinalIgnoreCase))
        {
            return "KITCHEN_RESTAURANT";
        }

        if (value.StartsWith("KITCHEN_OFFSITE", StringComparison.OrdinalIgnoreCase))
        {
            return "KITCHEN_OFFSITE";
        }

        if (value.StartsWith("KITCHEN_STAFF", StringComparison.OrdinalIgnoreCase))
        {
            return "KITCHEN_STAFF";
        }

        if (value.StartsWith("KITCHEN", StringComparison.OrdinalIgnoreCase) || value.Contains("bếp", StringComparison.OrdinalIgnoreCase))
        {
            return PreparationAreaSettingRow.DefaultBreakfastKitchenCode;
        }

        if (value.StartsWith("SERVICE", StringComparison.OrdinalIgnoreCase) || value.Contains("dịch vụ", StringComparison.OrdinalIgnoreCase))
        {
            return "SERVICE";
        }

        if (value.StartsWith("OTHER", StringComparison.OrdinalIgnoreCase) || value.Contains("khác", StringComparison.OrdinalIgnoreCase))
        {
            return "OTHER";
        }

        return CreateSlug(value).ToUpperInvariant();
    }

    private static PreparationAreaSettingRow CreateFallbackPreparationArea(string code, string sourceText)
    {
        var stationGroup = InferStationGroup(sourceText);
        return new PreparationAreaSettingRow(code, sourceText, stationGroup, true);
    }

    private static string InferStationGroup(string? text)
    {
        var value = (text ?? string.Empty).Trim();
        if (value.Contains("bar", StringComparison.OrdinalIgnoreCase))
        {
            return "Bar";
        }

        if (value.Contains("dịch vụ", StringComparison.OrdinalIgnoreCase) || value.Contains("service", StringComparison.OrdinalIgnoreCase))
        {
            return "Dịch vụ";
        }

        if (value.Contains("khác", StringComparison.OrdinalIgnoreCase) || value.Contains("other", StringComparison.OrdinalIgnoreCase))
        {
            return "Khác";
        }

        return "Bếp";
    }

    private static PreparationAreaSettingRow ToPreparationAreaRow(PreparationAreaSettingState state)
        => new(
            string.IsNullOrWhiteSpace(state.Code) ? CreateSlug(state.Name ?? "KITCHEN").ToUpperInvariant() : state.Code.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(state.Name) ? state.Code?.Trim() ?? "Khu chế biến" : state.Name.Trim(),
            string.IsNullOrWhiteSpace(state.StationGroup) ? InferStationGroup(state.Name) : state.StationGroup.Trim(),
            state.IsActive);

    private static List<PreparationAreaSettingRow> GetDefaultPreparationAreaRows()
        => new()
        {
            new(PreparationAreaSettingRow.DefaultBarCode, "Bar", "Bar", true),
            new(PreparationAreaSettingRow.DefaultBreakfastKitchenCode, "Bếp ăn sáng", "Bếp", true),
            new("KITCHEN_PARTY", "Bếp tiệc", "Bếp", true),
            new("KITCHEN_RESTAURANT", "Bếp nhà hàng", "Bếp", true),
            new("KITCHEN_OFFSITE", "Bếp tiệc ngoài", "Bếp", true),
            new("KITCHEN_STAFF", "Bếp nhân viên", "Bếp", true),
            new("SERVICE", "Dịch vụ", "Dịch vụ", true),
            new("OTHER", "Loại khác", "Khác", true)
        };

    [RelayCommand]
    private void EditPreparationAreaSettings(PreparationAreaSettingRow? row)
    {
        if (row is null) return;
        SelectedPreparationAreaSetting = row;
        EditPreparationAreaCode = row.Code;
        EditPreparationAreaName = row.Name;
        EditPreparationAreaStationGroup = row.StationGroup;
        EditPreparationAreaIsActive = row.IsActive;
        OnPropertyChanged(nameof(CanSavePreparationArea));
    }

    [RelayCommand]
    private void ClearPreparationAreaForm()
    {
        SelectedPreparationAreaSetting = null;
        EditPreparationAreaCode = string.Empty;
        EditPreparationAreaName = string.Empty;
        EditPreparationAreaStationGroup = "Bếp";
        EditPreparationAreaIsActive = true;
        OnPropertyChanged(nameof(CanSavePreparationArea));
    }

    [RelayCommand]
    private async Task SavePreparationAreaSettingsAsync()
    {
        if (!CanSavePreparationArea) return;

        IsBusy = true;
        try
        {
            var areaCode = string.IsNullOrWhiteSpace(EditPreparationAreaCode)
                ? CreateSlug(EditPreparationAreaName).ToUpperInvariant()
                : CreateSlug(EditPreparationAreaCode).ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(areaCode))
            {
                StatusMessage = "Vui lòng nhập tên khu chế biến.";
                StatusIsSuccess = false;
                return;
            }

            var duplicate = PreparationAreaSettings.FirstOrDefault(x =>
                !ReferenceEquals(x, SelectedPreparationAreaSetting)
                && string.Equals(x.Code, areaCode, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
            {
                StatusMessage = $"Mã khu {areaCode} đã tồn tại.";
                StatusIsSuccess = false;
                return;
            }

            var oldCode = SelectedPreparationAreaSetting?.Code;
            var row = new PreparationAreaSettingRow(
                areaCode,
                EditPreparationAreaName.Trim(),
                string.IsNullOrWhiteSpace(EditPreparationAreaStationGroup) ? "Bếp" : EditPreparationAreaStationGroup.Trim(),
                EditPreparationAreaIsActive);

            if (SelectedPreparationAreaSetting is null)
            {
                PreparationAreaSettings.Add(row);
            }
            else
            {
                var index = PreparationAreaSettings.IndexOf(SelectedPreparationAreaSetting);
                if (index >= 0)
                {
                    PreparationAreaSettings[index] = row;
                }
            }

            if (!string.IsNullOrWhiteSpace(oldCode) && !string.Equals(oldCode, row.Code, StringComparison.OrdinalIgnoreCase))
            {
                var affectedItems = _allMenuItems
                    .Where(x => string.Equals(NormalizePreparationAreaCode(x.PreparationArea), oldCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var item in affectedItems)
                {
                    await _menuCatalogService.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
                    {
                        Id = item.Id,
                        MenuCategoryId = item.MenuCategoryId,
                        Code = item.Code,
                        Name = item.Name,
                        UnitName = item.UnitName,
                        UnitPrice = item.UnitPrice,
                        VatRate = item.VatRate,
                        HasRecipeManagement = item.HasRecipeManagement,
                        IsCombo = item.IsCombo,
                        IsHotelService = item.IsHotelService,
                        IsActive = item.IsActive,
                        Note = item.Note,
                        PreparationArea = row.Code,
                        ImagePath = item.ImagePath
                    });
                }
            }

            await SavePreparationAreaSettingsFileAsync();
            RefreshPreparationAreaOptions();
            if (!string.IsNullOrWhiteSpace(oldCode) && string.Equals(SelectedPreparationArea, oldCode, StringComparison.OrdinalIgnoreCase))
            {
                SelectedPreparationArea = row.Code;
            }

            await LoadAllMenuItemsAsync();
            ApplyFilters();
            ClearPreparationAreaForm();
            StatusMessage = $"Đã lưu khu chế biến {row.Name}.";
            StatusIsSuccess = true;
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
    }

    [RelayCommand]
    private async Task DeletePreparationAreaSettingsAsync(PreparationAreaSettingRow? row)
    {
        if (row is null) return;

        var inUseCount = _allMenuItems.Count(x => string.Equals(NormalizePreparationAreaCode(x.PreparationArea), row.Code, StringComparison.OrdinalIgnoreCase));
        if (inUseCount > 0)
        {
            StatusMessage = $"Không thể xóa khu {row.Name} vì đang có {inUseCount} món sử dụng.";
            StatusIsSuccess = false;
            return;
        }

        var result = MessageBox.Show($"Xóa khu chế biến {row.Name}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        PreparationAreaSettings.Remove(row);
        await SavePreparationAreaSettingsFileAsync();
        RefreshPreparationAreaOptions();
        if (ReferenceEquals(SelectedPreparationAreaSetting, row))
        {
            ClearPreparationAreaForm();
        }

        StatusMessage = $"Đã xóa khu chế biến {row.Name}.";
        StatusIsSuccess = true;
        NotifyComputedState();
    }
}

public sealed record PreparationAreaSettingRow(
    string Code,
    string Name,
    string StationGroup,
    bool IsActive)
{
    public const string DefaultBarCode = "BAR";
    public const string DefaultBreakfastKitchenCode = "KITCHEN_BREAKFAST";
    public string StatusText => IsActive ? "Đang dùng" : "Ngưng dùng";
}

public sealed class PreparationAreaSettingState
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? StationGroup { get; set; }
    public bool IsActive { get; set; } = true;
}
