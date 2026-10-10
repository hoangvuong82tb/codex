using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

using ASMILE.BO.App.Helpers;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class PaymentReceiptsViewModel : ViewModelBase
{
    private readonly IReceivableService _receivableService;
    private readonly ICustomerAppService _customerAppService;
    private readonly IPersonAppService _personService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly List<PaymentReceiptRow> _allRows = new();
    private readonly Task _initializationTask;

    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<PersonDto> Collectors { get; } = new();
    public ObservableCollection<PaymentReceiptRow> Receipts { get; } = new();
    public TablePagerViewModel<PaymentReceiptRow> ReceiptPager { get; } = new() { ItemName = "phiếu thu" };

    [ObservableProperty] private CustomerDto? selectedCustomer;
    [ObservableProperty] private PersonDto? selectedCollector;
    [ObservableProperty] private PaymentReceiptRow? selectedReceipt;
    [ObservableProperty] private string searchText = string.Empty;

    [ObservableProperty] private DateTime receiptDate = DateTime.Today;
    [ObservableProperty] private string receiptNo = string.Empty;
    [ObservableProperty] private DateTime filterFromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime filterToDate = DateTime.Today;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private string paymentMethod = "Tiền mặt";
    [ObservableProperty] private string collectorName = string.Empty;
    [ObservableProperty] private string debitAccountCode = "111";
    [ObservableProperty] private string creditAccountCode = "131";
    [ObservableProperty] private string bankAccount = string.Empty;
    [ObservableProperty] private string note = string.Empty;

    [ObservableProperty] private bool isCreateTabSelected = true;
    [ObservableProperty] private bool isPendingTabSelected;
    [ObservableProperty] private bool isApprovedTabSelected;

    public bool IsListTabSelected => !IsCreateTabSelected;
    public decimal TotalReceiptAmount => Receipts.Sum(x => x.Amount);

    public PaymentReceiptsViewModel(
        IReceivableService receivableService,
        ICustomerAppService customerAppService,
        IPersonAppService personService,
        INotificationService notificationService,
        IDialogService dialogService)
    {
        _receivableService = receivableService;
        _customerAppService = customerAppService;
        _personService = personService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _initializationTask = InitializeAsync();
    }

    /// <summary>
    /// Completes when the constructor-started real-data initialization has finished.
    /// Views must await this task before requesting another refresh.
    /// </summary>
    public Task WaitForInitializationAsync() => _initializationTask;

    /// <summary>
    /// Snapshot used by the legacy voucher shell. The shell must display the
    /// persisted rows, not only the currently selected workflow tab.
    /// </summary>
    public IReadOnlyList<PaymentReceiptRow> AllRows => _allRows;

    public bool SelectReceiptByDocumentNo(string documentNo)
    {
        var row = _allRows.FirstOrDefault(x =>
            string.Equals(x.DocumentNo?.Trim(), documentNo?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return false;
        }

        SelectedReceipt = row;
        return true;
    }

    public async Task CancelReceiptByDocumentNoAsync(string documentNo)
    {
        if (!SelectReceiptByDocumentNo(documentNo))
        {
            _dialogService.Error("Không tìm thấy phiếu thu thật để hủy. Vui lòng tải lại danh sách.");
            return;
        }

        await DeleteSelected();
    }

    private async Task InitializeAsync()
    {
        // Do not allocate a document number while the view model is being
        // constructed. The shell creates several real-data view models at
        // once, all sharing the application scope; starting a transactional
        // number allocation here races with their initial queries and can
        // leave an unobserved DbContext/SQL transaction exception. A number
        // is allocated only when the operator starts a new receipt.
        ResetEntryForm();
        await LoadAsync();
    }

    partial void OnSelectedCustomerChanged(CustomerDto? value) => ApplyCurrentTabFilter();
    partial void OnSelectedCollectorChanged(PersonDto? value) => CollectorName = value?.FullName ?? string.Empty;
    partial void OnSearchTextChanged(string value) => ApplyCurrentTabFilter();
    partial void OnFilterFromDateChanged(DateTime value) => ApplyCurrentTabFilter();
    partial void OnFilterToDateChanged(DateTime value) => ApplyCurrentTabFilter();
    partial void OnIsCreateTabSelectedChanged(bool value) => OnPropertyChanged(nameof(IsListTabSelected));

    [RelayCommand]
    private void ShowCreateTab()
    {
        IsCreateTabSelected = true;
        IsPendingTabSelected = false;
        IsApprovedTabSelected = false;
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private void ShowPendingTab()
    {
        IsCreateTabSelected = false;
        IsPendingTabSelected = true;
        IsApprovedTabSelected = false;
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private void ShowApprovedTab()
    {
        IsCreateTabSelected = false;
        IsPendingTabSelected = false;
        IsApprovedTabSelected = true;
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private async Task NewAsync()
    {
        await _initializationTask;
        ReceiptNo = await _receivableService.GetNextReceiptNoAsync();
        ResetEntryForm();
    }

    private void ResetEntryForm()
    {
        Amount = 0;
        Note = string.Empty;
        CollectorName = string.Empty;
        DebitAccountCode = "111";
        CreditAccountCode = "131";
        ReceiptDate = DateTime.Today;
        PaymentMethod = "Tiền mặt";
    }

    [RelayCommand]
    private async Task AddDraftAsync()
    {
        await _initializationTask;

        if (SelectedCustomer is null)
        {
            _dialogService.Error("Chọn khách hàng trước khi thêm dòng.");
            return;
        }

        if (Amount <= 0)
        {
            _dialogService.Error("Số tiền thu phải lớn hơn 0.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ReceiptNo))
        {
            ReceiptNo = await _receivableService.GetNextReceiptNoAsync();
        }

        var draft = new PaymentReceiptRow
        {
            Date = ReceiptDate,
            DocumentNo = ReceiptNo.Trim(),
            CustomerCode = SelectedCustomer.CustomerCode,
            CustomerName = SelectedCustomer.CustomerName,
            Amount = Amount,
            Method = PaymentMethod,
            Status = "Lập phiếu",
            WorkflowStatus = VoucherWorkflowStatus.Draft,
            CustomerId = SelectedCustomer.CustomerCode
        };

        _allRows.Insert(0, draft);
        IsCreateTabSelected = true;
        IsPendingTabSelected = false;
        IsApprovedTabSelected = false;
        SelectedReceipt = draft;
        ApplyCurrentTabFilter();
        OnPropertyChanged(nameof(TotalReceiptAmount));
        await NewAsync();
    }

    [RelayCommand]
    private async Task ApproveSelected()
    {
        if (SelectedReceipt is null)
        {
            _dialogService.Error("Chọn một phiếu ở danh sách chờ duyệt.");
            return;
        }

        if (SelectedReceipt.WorkflowStatus != VoucherWorkflowStatus.Pending)
        {
            _dialogService.Error("Chỉ duyệt được phiếu ở tab Chờ duyệt.");
            return;
        }

        if (!SelectedReceipt.IsPersisted)
        {
            _dialogService.Error("Phiếu chưa được lưu vào CSDL.");
            return;
        }

        var ok = await _receivableService.ApprovePaymentReceiptAsync(SelectedReceipt.ReceiptId, CancellationToken.None);
        if (!ok)
        {
            _dialogService.Error("Không duyệt được phiếu thu ở trạng thái hiện tại.");
            return;
        }

        _notificationService.Show("Đã duyệt phiếu thu và ghi nhận vào dữ liệu công nợ.");
        await LoadAsync();
        ShowApprovedTab();
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedReceipt is null)
        {
            _dialogService.Error("Chọn một phiếu ở danh sách chờ duyệt.");
            return;
        }

        if (SelectedReceipt.WorkflowStatus != VoucherWorkflowStatus.Pending)
        {
            _dialogService.Error("Chỉ sửa được phiếu ở tab Chờ duyệt.");
            return;
        }

        _dialogService.Error("Phiếu đã lưu không sửa trực tiếp; hãy hủy phiếu chờ duyệt rồi lập phiếu mới để giữ audit đầy đủ.");
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        if (SelectedReceipt is null)
        {
            _dialogService.Error("Chọn một phiếu ở danh sách chờ duyệt.");
            return;
        }

        if (SelectedReceipt.WorkflowStatus != VoucherWorkflowStatus.Pending)
        {
            _dialogService.Error("Chỉ xóa được phiếu ở tab Chờ duyệt.");
            return;
        }

        if (!SelectedReceipt.IsPersisted)
        {
            _allRows.Remove(SelectedReceipt);
            ApplyCurrentTabFilter();
            SelectedReceipt = Receipts.FirstOrDefault();
            return;
        }

        var ok = await _receivableService.CancelPaymentReceiptAsync(SelectedReceipt.ReceiptId, CancellationToken.None);
        if (!ok)
        {
            _dialogService.Error("Không hủy được phiếu thu ở trạng thái hiện tại.");
            return;
        }

        _notificationService.Show("Đã hủy phiếu thu trong CSDL.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Reapprove()
    {
        if (SelectedReceipt is null)
        {
            _dialogService.Error("Chọn một phiếu ở danh sách đã duyệt.");
            return;
        }

        if (!SelectedReceipt.IsPersisted)
        {
            _dialogService.Error("Phiếu chưa được lưu vào CSDL.");
            return;
        }

        var ok = await _receivableService.ReopenPaymentReceiptAsync(SelectedReceipt.ReceiptId, CancellationToken.None);
        if (!ok)
        {
            _dialogService.Error("Không chuyển lại phiếu thu về Chờ duyệt được.");
            return;
        }

        _notificationService.Show("Đã chuyển phiếu thu về Chờ duyệt trong CSDL.");
        await LoadAsync();
        ShowPendingTab();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var customers = await _customerAppService.SearchAsync(string.Empty);

            Customers.Clear();
            foreach (var c in customers.OrderBy(x => x.CustomerName))
            {
                Customers.Add(c);
            }

            var people = await _personService.SearchAsync(null, string.Empty, null);
            Collectors.Clear();
            foreach (var person in people.OrderBy(x => x.FullName))
            {
                Collectors.Add(person);
            }

            if (SelectedCustomer is not null && !Customers.Any(x => string.Equals(x.CustomerCode, SelectedCustomer.CustomerCode, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedCustomer = null;
            }
            if (SelectedCollector is not null && !Collectors.Any(x => x.Id == SelectedCollector.Id))
            {
                SelectedCollector = null;
            }

            var drafts = _allRows.Where(x => !x.IsPersisted).ToList();
            _allRows.Clear();
            _allRows.AddRange(drafts);

            var persisted = await _receivableService.GetPaymentReceiptsAsync(new ReceivableFilterDto
            {
                Keyword = SearchText.Trim(),
                FromDate = FilterFromDate,
                ToDate = FilterToDate
            });
            foreach (var row in persisted)
            {
                _allRows.Add(new PaymentReceiptRow
                {
                    ReceiptId = row.Id,
                    IsPersisted = true,
                    Date = row.ReceiptDate,
                    DocumentNo = row.DocumentNo,
                    CustomerCode = row.CustomerCode,
                    CustomerName = row.CustomerName,
                    Amount = row.Amount,
                    Method = row.PaymentMethod,
                    Note = row.Note,
                    Status = row.Status,
                    WorkflowStatus = row.Status switch
                    {
                        "Lập phiếu" => VoucherWorkflowStatus.Draft,
                        "Đã duyệt" => VoucherWorkflowStatus.Approved,
                        _ => VoucherWorkflowStatus.Pending
                    },
                    CustomerId = row.CustomerId
                });
            }

            ApplyCurrentTabFilter();
        }
        catch (Exception ex)
        {
            _dialogService.Error($"Không tải được dữ liệu phiếu thu từ CSDL: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyCurrentTabFilter()
    {
        var query = _allRows.AsEnumerable();

        if (SelectedCustomer is not null)
        {
            query = query.Where(x => x.CustomerCode == SelectedCustomer.CustomerCode);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var keyword = SearchText.Trim();
            query = query.Where(x => x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || x.CustomerCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || x.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (IsCreateTabSelected)
        {
            query = query.Where(x => x.WorkflowStatus == VoucherWorkflowStatus.Draft);
        }
        else if (IsPendingTabSelected)
        {
            query = query.Where(x => x.WorkflowStatus == VoucherWorkflowStatus.Pending);
        }
        else if (IsApprovedTabSelected)
        {
            query = query.Where(x => x.WorkflowStatus == VoucherWorkflowStatus.Approved);
        }

        if (!IsCreateTabSelected)
        {
            query = query.Where(x => x.Date.Date >= FilterFromDate.Date && x.Date.Date <= FilterToDate.Date);
        }

        Receipts.Clear();
        foreach (var row in query.OrderByDescending(x => x.Date))
        {
            Receipts.Add(row);
        }

        ReceiptPager.SetSource(Receipts);
        OnPropertyChanged(nameof(TotalReceiptAmount));
    }

    [RelayCommand]
    private async Task SaveReceiptAsync()
    {
        var target = SelectedReceipt;
        if (target is null || target.WorkflowStatus != VoucherWorkflowStatus.Draft)
        {
            _dialogService.Error("Chọn dòng ở tab Lập phiếu trước khi lưu.");
            return;
        }

        if (SelectedCustomer is null)
        {
            _dialogService.Error("Chọn khách hàng trước khi lưu phiếu thu.");
            return;
        }

        if (Amount <= 0)
        {
            _dialogService.Error("Số tiền thu phải lớn hơn 0.");
            return;
        }

        try
        {
            target.Date = ReceiptDate;
            target.Amount = Amount;
            target.Method = PaymentMethod;
            target.Note = Note;
            var documentNo = await _receivableService.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
            {
                CustomerId = target.CustomerId,
                ReceiptDate = target.Date,
                Amount = target.Amount,
                PaymentMethod = target.Method,
                BankAccount = BankAccount,
                Note = Note
            });
            if (string.IsNullOrWhiteSpace(documentNo))
            {
                _dialogService.Error("Không tạo được phiếu thu trong CSDL.");
                return;
            }

            _notificationService.Show("Đã lưu phiếu thu, chuyển sang danh sách Chờ duyệt.");
            target.IsPersisted = true;
            target.DocumentNo = documentNo;
            await LoadAsync();
            ShowPendingTab();
            SelectedReceipt = _allRows.FirstOrDefault(x => x.ReceiptId == target.ReceiptId)
                ?? _allRows.FirstOrDefault(x => x.DocumentNo == documentNo);
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    public async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void ExportCsv()
    {
        if (Receipts.Count == 0)
        {
            _notificationService.Show("Không có phiếu thu thật phù hợp bộ lọc để xuất.");
            return;
        }

        var rows = Receipts.Select(row => (IReadOnlyList<string>)new[]
        {
            row.Date.ToString("yyyy-MM-dd"),
            row.DocumentNo,
            row.CustomerCode,
            row.CustomerName,
            row.Amount.ToString("0.##"),
            row.Method,
            row.Status,
            row.Note
        }).ToList();

        var path = CsvExportHelper.Export(
            "phieu-thu",
            new[] { "Ngày thu", "Số phiếu", "Mã KH", "Khách hàng", "Số tiền", "Phương thức", "Trạng thái", "Ghi chú" },
            rows);
        _notificationService.Show($"Đã xuất {Receipts.Count:N0} phiếu thu thật: {path}");
    }
}

public enum VoucherWorkflowStatus
{
    Draft,
    Pending,
    Approved,
    Cancelled,
    Rejected
}

public sealed class PaymentReceiptRow
{
    public string ReceiptId { get; set; } = string.Empty;
    public bool IsPersisted { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Tiền mặt";
    public string Note { get; set; } = string.Empty;
    public string Status { get; set; } = "Chờ duyệt";
    public VoucherWorkflowStatus WorkflowStatus { get; set; } = VoucherWorkflowStatus.Pending;
}
