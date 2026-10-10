using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace ASMILE.BO.App.ViewModels;

public partial class SuppliersViewModel : ViewModelBase
{
    private static readonly Regex PhoneRegex = new(@"^[0-9+()\-\s]{8,15}$", RegexOptions.Compiled);
    private readonly TimeSpan _searchDebounce = TimeSpan.FromMilliseconds(280);
    private CancellationTokenSource? _searchDebounceCts;
    private bool _pendingReload;

    private readonly ISupplierAppService _service;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ISupplierImportExportService _supplierImportExportService;
    private readonly ISupplierImportValidator _supplierImportValidator;
    private readonly IWindowManagerService _windowManagerService;

    private const string ImportPreviewModuleKey = "supplier-import-preview";
    private static readonly AppModuleInfo ImportPreviewModuleInfo = new(
        ImportPreviewModuleKey,
        "Xem trước dữ liệu nhập",
        "\uE8A7",
        "Nhập dữ liệu nhà cung cấp",
        "#3B6EF5",
        isPinned: false,
        showOnDesktop: false,
        iconPath: ApplicationAssetPaths.Icon(@"Icons\applications\import.ico"));

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();
    public TablePagerViewModel<SupplierDto> SuppliersPager { get; } = new() { ItemName = "nhà cung cấp" };

    [ObservableProperty] private SupplierDto editing = new();
    [ObservableProperty] private SupplierDto? selectedSupplier;
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private string formModeText = "Tạo mới nhà cung cấp";
    [ObservableProperty] private string supplierNameError = string.Empty;
    [ObservableProperty] private string phoneError = string.Empty;
    [ObservableProperty] private string dataErrorMessage = string.Empty;
    [ObservableProperty] private bool isCreatingNew;

    public bool IsCreateMode => Editing.Id <= 0 && IsCreatingNew;
    public bool IsEditMode => Editing.Id > 0;
    public bool IsReadMode => !IsCreateMode && !IsEditMode;
    public bool CanDelete => Editing.Id > 0;
    public bool HasValidationErrors => !string.IsNullOrWhiteSpace(SupplierNameError) || !string.IsNullOrWhiteSpace(PhoneError);
    public bool HasNoSuppliers => !IsBusy && Suppliers.Count == 0;

    public SuppliersViewModel(
        ISupplierAppService service,
        INotificationService notificationService,
        IDialogService dialogService,
        ISupplierImportExportService supplierImportExportService,
        ISupplierImportValidator supplierImportValidator,
        IWindowManagerService windowManagerService)
    {
        _service = service;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _supplierImportExportService = supplierImportExportService;
        _supplierImportValidator = supplierImportValidator;
        _windowManagerService = windowManagerService;

        Suppliers.CollectionChanged += OnSuppliersCollectionChanged;

        Editing = new SupplierDto { IsActive = true };
        IsCreatingNew = false;
        UpdateFormState();
        _ = LoadAsync();
    }

    partial void OnSelectedSupplierChanged(SupplierDto? value)
    {
        if (value is null)
        {
            return;
        }

        Editing = new SupplierDto
        {
            Id = value.Id,
            SupplierCode = value.SupplierCode,
            SupplierName = value.SupplierName,
            Phone = value.Phone,
            TaxCode = value.TaxCode,
            BankAccountNo = value.BankAccountNo,
            Address = value.Address,
            Note = value.Note,
            IsActive = value.IsActive
        };

        IsCreatingNew = false;
        ClearValidation();
        UpdateFormState();
    }

    partial void OnEditingChanged(SupplierDto value)
    {
        ClearValidation();
        UpdateFormState();
    }

    partial void OnIsCreatingNewChanged(bool value)
    {
        UpdateFormState();
    }

    partial void OnKeywordChanged(string value)
    {
        DebounceSearch();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            _pendingReload = true;
            return;
        }

        IsBusy = true;
        DataErrorMessage = string.Empty;

        try
        {
            var rows = await _service.SearchAsync(Keyword.Trim());
            Suppliers.Clear();
            foreach (var row in rows)
            {
                Suppliers.Add(row);
            }
            SuppliersPager.SetSource(Suppliers);

            if (SelectedSupplier is not null)
            {
                SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == SelectedSupplier.Id);
            }

            if (Suppliers.Count == 0)
            {
                IsCreatingNew = true;
            }
            else if (SelectedSupplier is null)
            {
                IsCreatingNew = false;
            }
        }
        catch (Exception ex)
        {
            DataErrorMessage = ex.Message;
            _dialogService.Error(ex.Message);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasNoSuppliers));

            if (_pendingReload)
            {
                _pendingReload = false;
                _ = LoadAsync();
            }
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadAsync();
    }

    private void DebounceSearch()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        _ = DebounceSearchAsync(token);
    }

    private async Task DebounceSearchAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(_searchDebounce, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            await LoadAsync();
        }
        catch (OperationCanceledException)
        {
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
                FileName = "supplier_template.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _supplierImportExportService.ExportTemplateAsync(saveDialog.FileName);
            _notificationService.Show("Đã xuất dữ liệu mẫu.");
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
                FileName = $"suppliers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _supplierImportExportService.ExportCurrentViewAsync(Suppliers, saveDialog.FileName);
            _notificationService.Show("Đã xuất bảng dữ liệu hiện tại.");
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
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
                CheckFileExists = true
            };

            if (openDialog.ShowDialog() != true)
            {
                return;
            }

            var importRows = await _supplierImportExportService.ReadImportRowsAsync(openDialog.FileName);
            if (importRows.Count == 0)
            {
                _dialogService.Info("File không có dữ liệu để nhập.");
                return;
            }

            var existingSuppliers = await _service.SearchAsync(string.Empty);
            var previewVm = new SupplierImportPreviewDialogViewModel(importRows, existingSuppliers, _supplierImportValidator);
            previewVm.RequestClose += OnImportPreviewRequestClose;

            CloseImportPreviewWindow();
            _windowManagerService.OpenOrActivate(ImportPreviewModuleInfo, () => previewVm);
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private async void OnImportPreviewRequestClose(bool accepted)
    {
        if (!accepted)
        {
            CloseImportPreviewWindow();
            return;
        }

        var previewVm = GetImportPreviewWindow()?.ContentViewModel as SupplierImportPreviewDialogViewModel;
        if (previewVm is null)
        {
            return;
        }

        var toImport = previewVm.BuildImportDtos();
        if (toImport.Count == 0)
        {
            _dialogService.Info("Không có dòng hợp lệ để nhập.");
            CloseImportPreviewWindow();
            return;
        }

        var result = new SupplierImportResult();
        foreach (var row in toImport)
        {
            try
            {
                await _service.SaveAsync(row);
                result.ImportedCount++;
            }
            catch
            {
                result.ErrorCount++;
            }
        }

        result.SkippedCount = previewVm.Rows.Count - result.ImportedCount - result.ErrorCount;
        CloseImportPreviewWindow();
        await LoadAsync();
        var supplierWindow = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == "suppliers");
        if (supplierWindow is not null)
        {
            _windowManagerService.Activate(supplierWindow);
        }

        _dialogService.Info($"Nhập hoàn tất.\nThành công: {result.ImportedCount}\nBỏ qua: {result.SkippedCount}\nLỗi: {result.ErrorCount}");
    }

    [RelayCommand]
    private void New()
    {
        Editing = new SupplierDto { IsActive = true };
        SelectedSupplier = null;
        IsCreatingNew = true;
        ClearValidation();
        UpdateFormState();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!ValidateForm())
        {
            return;
        }

        try
        {
            var saved = await _service.SaveAsync(Editing);
            _notificationService.Show("Đã lưu nhà cung cấp.");
            await LoadAsync();
            SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == saved.Id);
            if (SelectedSupplier is null)
            {
                New();
            }
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

        if (!_dialogService.Confirm("Bạn có chắc muốn xóa nhà cung cấp đang chọn?"))
        {
            return;
        }

        try
        {
            await _service.DeleteAsync(Editing.Id);
            _notificationService.Show("Đã xóa nhà cung cấp.");
            New();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private bool ValidateForm()
    {
        ClearValidation();

        if (string.IsNullOrWhiteSpace(Editing.SupplierName))
        {
            SupplierNameError = "Tên nhà cung cấp là bắt buộc.";
        }

        if (!string.IsNullOrWhiteSpace(Editing.Phone) && !PhoneRegex.IsMatch(Editing.Phone.Trim()))
        {
            PhoneError = "Số điện thoại không hợp lệ.";
        }

        OnPropertyChanged(nameof(HasValidationErrors));

        if (HasValidationErrors)
        {
            _dialogService.Error("Vui lòng kiểm tra lại thông tin trước khi lưu.");
            return false;
        }

        return true;
    }

    private void ClearValidation()
    {
        SupplierNameError = string.Empty;
        PhoneError = string.Empty;
        OnPropertyChanged(nameof(HasValidationErrors));
    }

    private void UpdateFormState()
    {
        FormModeText = IsEditMode
            ? "Chế độ chỉnh sửa"
            : IsCreateMode
                ? "Chế độ tạo mới"
                : "Chế độ xem";
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(IsReadMode));
        OnPropertyChanged(nameof(CanDelete));
    }

    private void OnSuppliersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasNoSuppliers));
    }

    private AppWindowViewModel? GetImportPreviewWindow()
    {
        return _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == ImportPreviewModuleKey);
    }

    private void CloseImportPreviewWindow()
    {
        var previewWindow = GetImportPreviewWindow();
        if (previewWindow?.ContentViewModel is SupplierImportPreviewDialogViewModel previewVm)
        {
            previewVm.RequestClose -= OnImportPreviewRequestClose;
        }

        if (previewWindow is not null)
        {
            _windowManagerService.Close(previewWindow);
        }
    }
}
