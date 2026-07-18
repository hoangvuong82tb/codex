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

public partial class CustomersViewModel : ViewModelBase
{
    private static readonly Regex PhoneRegex = new(@"^[0-9+()\-\s]{8,15}$", RegexOptions.Compiled);
    private readonly TimeSpan _searchDebounce = TimeSpan.FromMilliseconds(280);
    private CancellationTokenSource? _searchDebounceCts;
    private bool _pendingReload;

    private readonly ICustomerAppService _service;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ICustomerImportExportService _importExportService;
    private readonly ICustomerImportValidator _customerImportValidator;
    private readonly IWindowManagerService _windowManagerService;

    private const string ImportPreviewModuleKey = "customer-import-preview";
    private static readonly AppModuleInfo ImportPreviewModuleInfo = new(
        ImportPreviewModuleKey,
        "Xem trước dữ liệu nhập",
        "\uE8A7",
        "Nhập dữ liệu khách hàng",
        "#3B6EF5",
        isPinned: false,
        showOnDesktop: false,
        iconPath: @"D:\ASMILE-BO\Windows_Icon\Icons\applications\import.ico");

    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public TablePagerViewModel<CustomerDto> CustomersPager { get; } = new() { ItemName = "khách hàng" };

    [ObservableProperty] private CustomerDto editing = new();
    [ObservableProperty] private CustomerDto? selectedCustomer;
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private string formModeText = "Chế độ tạo mới khách hàng";
    [ObservableProperty] private string customerNameError = string.Empty;
    [ObservableProperty] private string phoneError = string.Empty;
    [ObservableProperty] private string dataErrorMessage = string.Empty;
    [ObservableProperty] private bool isCreatingNew;

    public bool IsCreateMode => Editing.Id <= 0 && IsCreatingNew;
    public bool IsEditMode => Editing.Id > 0;
    public bool IsReadMode => !IsCreateMode && !IsEditMode;
    public bool CanDelete => Editing.Id > 0;
    public bool HasValidationErrors => !string.IsNullOrWhiteSpace(CustomerNameError) || !string.IsNullOrWhiteSpace(PhoneError);
    public bool HasNoCustomers => !IsBusy && Customers.Count == 0;

    public CustomersViewModel(
        ICustomerAppService service,
        INotificationService notificationService,
        IDialogService dialogService,
        ICustomerImportExportService importExportService,
        ICustomerImportValidator customerImportValidator,
        IWindowManagerService windowManagerService)
    {
        _service = service;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _importExportService = importExportService;
        _customerImportValidator = customerImportValidator;
        _windowManagerService = windowManagerService;

        Customers.CollectionChanged += OnCustomersCollectionChanged;

        Editing = new CustomerDto { IsActive = true };
        IsCreatingNew = false;
        UpdateFormState();
        _ = LoadAsync();
    }

    partial void OnSelectedCustomerChanged(CustomerDto? value)
    {
        if (value is null)
        {
            return;
        }

        Editing = new CustomerDto
        {
            Id = value.Id,
            CustomerCode = value.CustomerCode,
            CustomerName = value.CustomerName,
            Phone = value.Phone,
            BankAccountNo = value.BankAccountNo,
            Address = value.Address,
            Note = value.Note,
            IsActive = value.IsActive
        };

        IsCreatingNew = false;
        ClearValidation();
        UpdateFormState();
    }

    partial void OnEditingChanged(CustomerDto value)
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
            Customers.Clear();
            foreach (var row in rows)
            {
                Customers.Add(row);
            }
            CustomersPager.SetSource(Customers);

            if (SelectedCustomer is not null)
            {
                SelectedCustomer = Customers.FirstOrDefault(x => x.Id == SelectedCustomer.Id);
            }

            if (Customers.Count == 0)
            {
                IsCreatingNew = true;
            }
            else if (SelectedCustomer is null)
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
            OnPropertyChanged(nameof(HasNoCustomers));

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
                FileName = "customer_template.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _importExportService.ExportTemplateAsync(saveDialog.FileName);
            _notificationService.Show("Đã xuất dữ liệu mẫu khách hàng.");
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
                FileName = $"customers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            var mapped = Customers.Select(x => new SupplierDto
            {
                SupplierCode = x.CustomerCode,
                SupplierName = x.CustomerName,
                Phone = x.Phone,
                IsActive = x.IsActive
            }).ToList();

            await _importExportService.ExportCurrentViewAsync(mapped, saveDialog.FileName);
            _notificationService.Show("Đã xuất bảng dữ liệu khách hàng.");
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

            var importRows = await _importExportService.ReadImportRowsAsync(openDialog.FileName);
            if (importRows.Count == 0)
            {
                _dialogService.Info("File không có dữ liệu để nhập.");
                return;
            }

            var existingCustomers = await _service.SearchAsync(string.Empty);
            var existingForValidation = existingCustomers.Select(x => new SupplierDto
            {
                Id = x.Id,
                SupplierCode = x.CustomerCode,
                SupplierName = x.CustomerName,
                Phone = x.Phone,
                BankAccountNo = x.BankAccountNo,
                Address = x.Address,
                Note = x.Note,
                IsActive = x.IsActive
            }).ToList();

            var previewVm = new SupplierImportPreviewDialogViewModel(
                importRows,
                existingForValidation,
                new SupplierImportValidatorAdapter(_customerImportValidator));

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
                await _service.SaveAsync(new CustomerDto
                {
                    CustomerCode = row.SupplierCode,
                    CustomerName = row.SupplierName,
                    Phone = row.Phone,
                    BankAccountNo = row.BankAccountNo,
                    Address = row.Address,
                    Note = row.Note,
                    IsActive = row.IsActive
                });
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
        var customerWindow = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == "customers");
        if (customerWindow is not null)
        {
            _windowManagerService.Activate(customerWindow);
        }

        _dialogService.Info($"Nhập hoàn tất.\nThành công: {result.ImportedCount}\nBỏ qua: {result.SkippedCount}\nLỗi: {result.ErrorCount}");
    }

    [RelayCommand]
    private void New()
    {
        Editing = new CustomerDto { IsActive = true };
        SelectedCustomer = null;
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
            _notificationService.Show("Đã lưu khách hàng.");
            await LoadAsync();
            SelectedCustomer = Customers.FirstOrDefault(x => x.Id == saved.Id);
            if (SelectedCustomer is null)
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

        if (!_dialogService.Confirm("Bạn có chắc muốn xóa khách hàng đang chọn?"))
        {
            return;
        }

        try
        {
            await _service.DeleteAsync(Editing.Id);
            _notificationService.Show("Đã xóa khách hàng.");
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

        if (string.IsNullOrWhiteSpace(Editing.CustomerName))
        {
            CustomerNameError = "Tên khách hàng là bắt buộc.";
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
        CustomerNameError = string.Empty;
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

    private void OnCustomersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasNoCustomers));
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

    private sealed class SupplierImportValidatorAdapter : ISupplierImportValidator
    {
        private readonly ICustomerImportValidator _inner;

        public SupplierImportValidatorAdapter(ICustomerImportValidator inner)
        {
            _inner = inner;
        }

        public IReadOnlyList<SupplierImportValidationIssue> ValidateRow(
            SupplierImportRow row,
            IReadOnlyList<SupplierImportRow> allRows,
            IReadOnlyList<SupplierDto> existingSuppliers)
        {
            return _inner.ValidateRow(row, allRows, existingSuppliers);
        }

        public void ValidateRows(
            IReadOnlyList<SupplierImportRow> rows,
            IReadOnlyList<SupplierDto> existingSuppliers,
            IDictionary<int, IReadOnlyList<SupplierImportValidationIssue>> target)
        {
            _inner.ValidateRows(rows, existingSuppliers, target);
        }
    }
}
