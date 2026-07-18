using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class ItemsViewModel : ViewModelBase
{
    private readonly IItemAppService _service;
    private readonly IInventoryAppService _inventoryService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IItemImportExportService _importExportService;
    private readonly IWindowManagerService _windowManagerService;

    private readonly HashSet<string> _manualUnits = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _manualGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _excludedUnits = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _excludedGroups = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<ItemDto> Items { get; } = new();
    public TablePagerViewModel<ItemDto> ItemsPager { get; } = new() { ItemName = "hàng hóa" };
    public ObservableCollection<string> UnitOptions { get; } = new();
    public ObservableCollection<string> GroupOptions { get; } = new();

    [ObservableProperty]
    private ItemDto editing = new();

    [ObservableProperty]
    private ItemDto? selectedItem;

    [ObservableProperty]
    private string keyword = string.Empty;

    [ObservableProperty]
    private bool isLookupEditorOpen;

    [ObservableProperty]
    private bool isManagingUnitLookup;

    [ObservableProperty]
    private string lookupEditorTitle = "Quản lý ĐVT";

    [ObservableProperty]
    private string newLookupValue = string.Empty;

    [ObservableProperty]
    private string? selectedLookupValue;

    public bool HasNoItems => Items.Count == 0;
    public bool CanDelete => Editing.Id > 0;
    public IReadOnlyList<string> CurrentLookupOptions => IsManagingUnitLookup ? UnitOptions : GroupOptions;

    public ItemsViewModel(
        IItemAppService service,
        IInventoryAppService inventoryService,
        INotificationService notificationService,
        IDialogService dialogService,
        IItemImportExportService importExportService,
        IWindowManagerService windowManagerService)
    {
        _service = service;
        _inventoryService = inventoryService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _importExportService = importExportService;
        _windowManagerService = windowManagerService;
        Items.CollectionChanged += OnItemsChanged;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task OpenSelectedStockInsightAsync()
    {
        if (SelectedItem is null || SelectedItem.Id <= 0)
        {
            return;
        }

        var rows = await _inventoryService.GetItemStockInsightsAsync(SelectedItem.Id);
        var moduleKey = $"item-stock-insight-{SelectedItem.Id}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            moduleKey,
            $"Tồn kho {SelectedItem.ItemCode}",
            "\uE8A5",
            "Chi tiết tồn kho hàng hóa",
            "#2F65D9",
            isPinned: false,
            showOnDesktop: false,
            iconPath: @"D:\ASMILE-BO\Windows_Icon\Icons\applications\item.ico");

        _windowManagerService.OpenOrActivate(module, () =>
            new InventoryStockInsightModuleViewModel(
                $"Chi tiết tồn kho: {SelectedItem.ItemCode} - {SelectedItem.ItemName}",
                "Double-click từ danh sách hàng hóa",
                moduleKey,
                rows,
                hideIssueColumns: false,
                _windowManagerService));
    }

    partial void OnSelectedItemChanged(ItemDto? value)
    {
        if (value is null)
        {
            return;
        }

        Editing = new ItemDto
        {
            Id = value.Id,
            ItemCode = value.ItemCode,
            ItemName = value.ItemName,
            Unit = value.Unit,
            GroupName = value.GroupName,
            SupplierCode = value.SupplierCode,
            SupplierName = value.SupplierName,
            IsActive = value.IsActive
        };

        IncludeLookupValue(UnitOptions, _manualUnits, _excludedUnits, Editing.Unit);
        IncludeLookupValue(GroupOptions, _manualGroups, _excludedGroups, Editing.GroupName);
        OnPropertyChanged(nameof(CanDelete));
    }

    partial void OnEditingChanged(ItemDto value)
    {
        OnPropertyChanged(nameof(CanDelete));
    }

    partial void OnIsManagingUnitLookupChanged(bool value)
    {
        LookupEditorTitle = value ? "Quản lý ĐVT" : "Quản lý Nhóm";
        SelectedLookupValue = null;
        NewLookupValue = string.Empty;
        OnPropertyChanged(nameof(CurrentLookupOptions));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var rows = await _service.SearchAsync(Keyword);
            Items.Clear();
            foreach (var row in rows)
            {
                Items.Add(row);
            }
            ItemsPager.SetSource(Items);

            RebuildLookupOptions();
            OnPropertyChanged(nameof(HasNoItems));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void New()
    {
        Editing = new ItemDto { IsActive = true };
        SelectedItem = null;
        OnPropertyChanged(nameof(CanDelete));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var saved = await _service.SaveAsync(Editing);
            IncludeLookupValue(UnitOptions, _manualUnits, _excludedUnits, saved.Unit);
            IncludeLookupValue(GroupOptions, _manualGroups, _excludedGroups, saved.GroupName);

            _notificationService.Show("Đã lưu hàng hóa.");
            await LoadAsync();
            SelectedItem = Items.FirstOrDefault(x => x.Id == saved.Id);
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Editing.Id <= 0)
        {
            return;
        }

        if (!_dialogService.Confirm("Bạn có chắc muốn xóa hàng hóa đang chọn?"))
        {
            return;
        }

        try
        {
            await _service.DeleteAsync(Editing.Id);
            _notificationService.Show("Đã xóa hàng hóa.");
            New();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportTemplateAsync()
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV UTF-8 (*.csv)|*.csv",
                FileName = "item_template.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _importExportService.ExportTemplateAsync(saveDialog.FileName);
            _notificationService.Show("Đã xuất dữ liệu mẫu hàng hóa.");
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportCurrentViewAsync()
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV UTF-8 (*.csv)|*.csv",
                FileName = $"items_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _importExportService.ExportCurrentViewAsync(Items, saveDialog.FileName);
            _notificationService.Show("Đã xuất bảng dữ liệu hàng hóa hiện tại.");
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ImportDataAsync()
    {
        try
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV UTF-8 (*.csv)|*.csv",
                CheckFileExists = true
            };

            if (openDialog.ShowDialog() != true)
            {
                return;
            }

            var rows = await _importExportService.ReadImportRowsAsync(openDialog.FileName);
            if (rows.Count == 0)
            {
                _dialogService.Info("File không có dữ liệu để nhập.");
                return;
            }

            var imported = 0;
            var skipped = 0;
            var errors = 0;
            var messages = new List<string>();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.ItemCode) || string.IsNullOrWhiteSpace(row.ItemName))
                {
                    skipped++;
                    messages.Add($"Dòng {row.RowNumber}: Thiếu Mã hàng hoặc Tên hàng.");
                    continue;
                }

                if (!TryParseBool(row.IsActiveRaw, out var isActive))
                {
                    skipped++;
                    messages.Add($"Dòng {row.RowNumber}: Sai định dạng Kích hoạt.");
                    continue;
                }

                try
                {
                    await _service.SaveAsync(new ItemDto
                    {
                        ItemCode = row.ItemCode.Trim(),
                        ItemName = row.ItemName.Trim(),
                        Unit = row.Unit.Trim(),
                        GroupName = row.GroupName.Trim(),
                        SupplierCode = row.SupplierCode.Trim(),
                        SupplierName = row.SupplierName.Trim(),
                        IsActive = isActive
                    });
                    imported++;
                }
                catch (Exception ex)
                {
                    errors++;
                    messages.Add($"Dòng {row.RowNumber}: {ex.Message}");
                }
            }

            await LoadAsync();
            _dialogService.Info($"Nhập dữ liệu hàng hóa hoàn tất.\nThành công: {imported}\nBỏ qua: {skipped}\nLỗi: {errors}");
            if (messages.Count > 0)
            {
                _dialogService.Error(string.Join(Environment.NewLine, messages.Take(8)));
            }
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenUnitManager()
    {
        IsManagingUnitLookup = true;
        IsLookupEditorOpen = true;
    }

    [RelayCommand]
    private void OpenGroupManager()
    {
        IsManagingUnitLookup = false;
        IsLookupEditorOpen = true;
    }

    [RelayCommand]
    private void CloseLookupManager()
    {
        IsLookupEditorOpen = false;
        SelectedLookupValue = null;
        NewLookupValue = string.Empty;
    }

    [RelayCommand]
    private void AddLookupValue()
    {
        var value = NormalizeLookupValue(NewLookupValue);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (IsManagingUnitLookup)
        {
            IncludeLookupValue(UnitOptions, _manualUnits, _excludedUnits, value);
            Editing.Unit = value;
        }
        else
        {
            IncludeLookupValue(GroupOptions, _manualGroups, _excludedGroups, value);
            Editing.GroupName = value;
        }

        SelectedLookupValue = value;
        NewLookupValue = string.Empty;
    }

    [RelayCommand]
    private void RemoveLookupValue()
    {
        if (string.IsNullOrWhiteSpace(SelectedLookupValue))
        {
            return;
        }

        var value = SelectedLookupValue;
        if (IsManagingUnitLookup)
        {
            RemoveLookupValue(UnitOptions, _manualUnits, _excludedUnits, value);
            if (string.Equals(Editing.Unit, value, StringComparison.OrdinalIgnoreCase))
            {
                Editing.Unit = string.Empty;
            }
        }
        else
        {
            RemoveLookupValue(GroupOptions, _manualGroups, _excludedGroups, value);
            if (string.Equals(Editing.GroupName, value, StringComparison.OrdinalIgnoreCase))
            {
                Editing.GroupName = string.Empty;
            }
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasNoItems));
    }

    private void RebuildLookupOptions()
    {
        var units = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in Items)
        {
            var unit = NormalizeLookupValue(row.Unit);
            if (!string.IsNullOrWhiteSpace(unit) && !_excludedUnits.Contains(unit))
            {
                units.Add(unit);
            }

            var group = NormalizeLookupValue(row.GroupName);
            if (!string.IsNullOrWhiteSpace(group) && !_excludedGroups.Contains(group))
            {
                groups.Add(group);
            }
        }

        foreach (var value in _manualUnits.Where(x => !_excludedUnits.Contains(x)))
        {
            units.Add(value);
        }

        foreach (var value in _manualGroups.Where(x => !_excludedGroups.Contains(x)))
        {
            groups.Add(value);
        }

        ResetCollection(UnitOptions, units);
        ResetCollection(GroupOptions, groups);

        IncludeLookupValue(UnitOptions, _manualUnits, _excludedUnits, Editing.Unit);
        IncludeLookupValue(GroupOptions, _manualGroups, _excludedGroups, Editing.GroupName);
    }

    private static void ResetCollection(ObservableCollection<string> target, IEnumerable<string> source)
    {
        target.Clear();
        foreach (var value in source.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase))
        {
            target.Add(value);
        }
    }

    private static string NormalizeLookupValue(string? raw)
    {
        return (raw ?? string.Empty).Trim();
    }

    private static void IncludeLookupValue(
        ObservableCollection<string> target,
        HashSet<string> manualSet,
        HashSet<string> excludedSet,
        string? value)
    {
        var normalized = NormalizeLookupValue(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        excludedSet.Remove(normalized);
        manualSet.Add(normalized);

        if (target.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        target.Add(normalized);
        var ordered = target.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
        target.Clear();
        foreach (var item in ordered)
        {
            target.Add(item);
        }
    }

    private static void RemoveLookupValue(
        ObservableCollection<string> target,
        HashSet<string> manualSet,
        HashSet<string> excludedSet,
        string value)
    {
        var normalized = NormalizeLookupValue(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        manualSet.Remove(normalized);
        excludedSet.Add(normalized);

        var existed = target.FirstOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        if (existed is not null)
        {
            target.Remove(existed);
        }
    }

    private static bool TryParseBool(string? raw, out bool value)
    {
        var normalized = (raw ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            value = true;
            return true;
        }

        switch (normalized)
        {
            case "1":
            case "true":
            case "yes":
            case "y":
            case "co":
            case "có":
            case "active":
            case "kích hoạt":
                value = true;
                return true;
            case "0":
            case "false":
            case "no":
            case "n":
            case "khong":
            case "không":
            case "inactive":
            case "ngưng":
                value = false;
                return true;
            default:
                value = false;
                return false;
        }
    }
}
