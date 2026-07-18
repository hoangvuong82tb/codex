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

public sealed partial class WarehousesViewModel : ViewModelBase
{
    private readonly IWarehouseAppService _service;
    private readonly IInventoryAppService _inventoryService;
    private readonly IDepartmentAppService _departmentService;
    private readonly IPersonAppService _personService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IWarehouseImportExportService _importExportService;
    private readonly IWindowManagerService _windowManagerService;

    public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
    public TablePagerViewModel<WarehouseDto> WarehousesPager { get; } = new() { ItemName = "kho" };
    public ObservableCollection<LookupItemDto> WarehouseLookup { get; } = new();
    public ObservableCollection<LookupItemDto> DepartmentLookup { get; } = new();
    public ObservableCollection<LookupItemDto> ResponsiblePersonLookup { get; } = new();

    [ObservableProperty]
    private WarehouseDto editing = new();

    [ObservableProperty]
    private WarehouseDto? selectedWarehouse;

    [ObservableProperty]
    private string keyword = string.Empty;

    public bool HasNoWarehouses => Warehouses.Count == 0;

    public bool CanDelete => Editing.Id > 0;

    public WarehousesViewModel(
        IWarehouseAppService service,
        IInventoryAppService inventoryService,
        IDepartmentAppService departmentService,
        IPersonAppService personService,
        INotificationService notificationService,
        IDialogService dialogService,
        IWarehouseImportExportService importExportService,
        IWindowManagerService windowManagerService)
    {
        _service = service;
        _inventoryService = inventoryService;
        _departmentService = departmentService;
        _personService = personService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _importExportService = importExportService;
        _windowManagerService = windowManagerService;
        Warehouses.CollectionChanged += OnWarehousesChanged;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task OpenSelectedStockInsightAsync()
    {
        if (SelectedWarehouse is null || SelectedWarehouse.Id <= 0)
        {
            return;
        }

        var rows = await _inventoryService.GetWarehouseStockInsightsAsync(SelectedWarehouse.Id);
        var moduleKey = $"warehouse-stock-insight-{SelectedWarehouse.Id}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            moduleKey,
            $"Tồn kho {SelectedWarehouse.WarehouseCode}",
            "\uE7F8",
            "Chi tiết tồn kho theo kho",
            "#2F65D9",
            isPinned: false,
            showOnDesktop: false,
            iconPath: @"D:\ASMILE-BO\Windows_Icon\Icons\applications\warehouse.ico");

        _windowManagerService.OpenOrActivate(module, () =>
            new InventoryStockInsightModuleViewModel(
                $"Chi tiết tồn kho: {SelectedWarehouse.WarehouseCode} - {SelectedWarehouse.WarehouseName}",
                "Double-click từ danh sách kho",
                moduleKey,
                rows,
                hideIssueColumns: SelectedWarehouse.IsConsumptionWarehouse,
                _windowManagerService));
    }

    partial void OnSelectedWarehouseChanged(WarehouseDto? value)
    {
        if (value is null) return;
        Editing = new WarehouseDto
        {
            Id = value.Id,
            WarehouseCode = value.WarehouseCode,
            WarehouseName = value.WarehouseName,
            Note = value.Note,
            DepartmentName = value.DepartmentName,
            ResponsiblePersonName = value.ResponsiblePersonName,
            ParentWarehouseId = value.ParentWarehouseId,
            IsConsumptionWarehouse = value.IsConsumptionWarehouse,
            IsActive = value.IsActive
        };
        OnPropertyChanged(nameof(CanDelete));
    }

    partial void OnEditingChanged(WarehouseDto value)
    {
        OnPropertyChanged(nameof(CanDelete));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var rows = await _service.SearchAsync(Keyword);
            Warehouses.Clear();
            foreach (var row in rows)
            {
                Warehouses.Add(row);
            }
            WarehousesPager.SetSource(Warehouses);
            OnPropertyChanged(nameof(HasNoWarehouses));

            var lookup = await _service.GetLookupAsync();
            WarehouseLookup.Clear();
            foreach (var l in lookup)
            {
                WarehouseLookup.Add(l);
            }

            var departments = await _departmentService.SearchAsync(string.Empty, null);
            DepartmentLookup.Clear();
            foreach (var d in departments.OrderBy(x => x.DepartmentCode))
            {
                DepartmentLookup.Add(new LookupItemDto
                {
                    Id = d.Id,
                    Code = d.DepartmentCode,
                    Name = d.DepartmentName
                });
            }

            var people = await _personService.SearchAsync(null, string.Empty, null);
            ResponsiblePersonLookup.Clear();
            foreach (var p in people.OrderBy(x => x.PersonCode))
            {
                ResponsiblePersonLookup.Add(new LookupItemDto
                {
                    Id = p.Id,
                    Code = p.PersonCode,
                    Name = p.FullName
                });
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void New()
    {
        Editing = new WarehouseDto { IsActive = true };
        SelectedWarehouse = null;
        OnPropertyChanged(nameof(CanDelete));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var saved = await _service.SaveAsync(Editing);
            _notificationService.Show("Đã lưu kho.");
            await LoadAsync();
            SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == saved.Id);
            OnPropertyChanged(nameof(CanDelete));
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Editing.Id <= 0) return;
        if (!_dialogService.Confirm("Bạn có chắc muốn xóa kho đang chọn?")) return;

        try
        {
            await _service.DeleteAsync(Editing.Id);
            _notificationService.Show("Đã xóa kho.");
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
                FileName = "warehouse_template.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _importExportService.ExportTemplateAsync(saveDialog.FileName);
            _notificationService.Show("Đã xuất dữ liệu mẫu kho.");
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
                FileName = $"warehouses_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _importExportService.ExportCurrentViewAsync(Warehouses, saveDialog.FileName);
            _notificationService.Show("Đã xuất bảng dữ liệu kho hiện tại.");
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

            var parentLookup = (await _service.GetLookupAsync())
                .ToDictionary(x => x.Code.Trim().ToUpperInvariant(), x => x.Id);

            var imported = 0;
            var skipped = 0;
            var errors = 0;
            var messages = new List<string>();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.WarehouseCode) || string.IsNullOrWhiteSpace(row.WarehouseName))
                {
                    skipped++;
                    messages.Add($"Dòng {row.RowNumber}: Thiếu Mã kho hoặc Tên kho.");
                    continue;
                }

                int? parentId = null;
                if (!string.IsNullOrWhiteSpace(row.ParentWarehouseCode))
                {
                    var key = row.ParentWarehouseCode.Trim().ToUpperInvariant();
                    if (parentLookup.TryGetValue(key, out var foundParentId))
                    {
                        parentId = foundParentId;
                    }
                    else
                    {
                        skipped++;
                        messages.Add($"Dòng {row.RowNumber}: Không tìm thấy kho cha '{row.ParentWarehouseCode}'.");
                        continue;
                    }
                }

                var parseConsumption = TryParseBool(row.IsConsumptionWarehouseRaw, out var isConsumption);
                var parseActive = TryParseBool(row.IsActiveRaw, out var isActive);
                if (!parseConsumption || !parseActive)
                {
                    skipped++;
                    messages.Add($"Dòng {row.RowNumber}: Sai định dạng Kho tiêu hao/Kích hoạt.");
                    continue;
                }

                try
                {
                    await _service.SaveAsync(new WarehouseDto
                    {
                        WarehouseCode = row.WarehouseCode.Trim(),
                        WarehouseName = row.WarehouseName.Trim(),
                        ParentWarehouseId = parentId,
                        DepartmentName = row.DepartmentName.Trim(),
                        ResponsiblePersonName = row.ResponsiblePersonName.Trim(),
                        Note = row.Note.Trim(),
                        IsConsumptionWarehouse = isConsumption,
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
            _dialogService.Info($"Nhập dữ liệu kho hoàn tất.\nThành công: {imported}\nBỏ qua: {skipped}\nLỗi: {errors}");

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

    private void OnWarehousesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasNoWarehouses));
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
            case "dang hoat dong":
            case "đang hoạt động":
                value = true;
                return true;
            case "0":
            case "false":
            case "no":
            case "n":
            case "khong":
            case "không":
            case "inactive":
            case "ngung":
            case "ngưng":
            case "đã nghỉ":
                value = false;
                return true;
            default:
                value = false;
                return false;
        }
    }
}
