using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class PaymentReceiptsViewModel : ViewModelBase
{
    private readonly IReceivableService _receivableService;
    private readonly ICustomerAppService _customerAppService;
    private readonly IPersonAppService _personService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly List<PaymentReceiptRow> _allRows = new();

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
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await NewAsync();
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
        ReceiptNo = await _receivableService.GetNextReceiptNoAsync();
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
    private void ApproveSelected()
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

        SelectedReceipt.WorkflowStatus = VoucherWorkflowStatus.Approved;
        SelectedReceipt.Status = "Đã duyệt";
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

        SelectedReceipt.WorkflowStatus = VoucherWorkflowStatus.Draft;
        SelectedReceipt.Status = "Lập phiếu";
        ReceiptNo = SelectedReceipt.DocumentNo;
        ReceiptDate = SelectedReceipt.Date;
        Amount = SelectedReceipt.Amount;
        PaymentMethod = SelectedReceipt.Method;
        SelectedCustomer = Customers.FirstOrDefault(x => string.Equals(x.CustomerCode, SelectedReceipt.CustomerCode, StringComparison.OrdinalIgnoreCase)) ?? SelectedCustomer;
        ShowCreateTab();
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private void DeleteSelected()
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

        _allRows.Remove(SelectedReceipt);
        ApplyCurrentTabFilter();
        SelectedReceipt = Receipts.FirstOrDefault();
        OnPropertyChanged(nameof(TotalReceiptAmount));
    }

    [RelayCommand]
    private void Reapprove()
    {
        if (SelectedReceipt is null)
        {
            _dialogService.Error("Chọn một phiếu ở danh sách đã duyệt.");
            return;
        }

        SelectedReceipt.WorkflowStatus = VoucherWorkflowStatus.Pending;
        SelectedReceipt.Status = "Chờ duyệt";
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

            var summaries = await _receivableService.GetDebtSummariesAsync(new ReceivableFilterDto
            {
                Keyword = SearchText.Trim(),
                FromDate = FilterFromDate,
                ToDate = FilterToDate
            });

            if (_allRows.Count == 0 && Customers.Count > 0)
            {
                foreach (var customer in Customers)
                {
                    var summary = summaries.FirstOrDefault(s => string.Equals(s.CustomerCode, customer.CustomerCode, StringComparison.OrdinalIgnoreCase));
                    if (summary is null)
                    {
                        continue;
                    }

                    var tx = await _receivableService.GetTransactionsByCustomerAsync(summary.CustomerId, DateTime.Today.AddMonths(-12), DateTime.Today);
                    foreach (var row in tx.Where(x => x.DocumentType == ReceivableDocumentType.PaymentReceipt).OrderByDescending(x => x.TransactionDate))
                    {
                        _allRows.Add(new PaymentReceiptRow
                        {
                            Date = row.TransactionDate,
                            DocumentNo = row.DocumentNo,
                            CustomerCode = row.CustomerCode,
                            CustomerName = row.CustomerName,
                            Amount = row.CreditAmount,
                            Method = string.IsNullOrWhiteSpace(PaymentMethod) ? "Tiền mặt" : PaymentMethod,
                            Status = row.Status == DebtStatus.Paid ? "Đã duyệt" : "Chờ duyệt",
                            WorkflowStatus = row.Status == DebtStatus.Paid ? VoucherWorkflowStatus.Approved : VoucherWorkflowStatus.Pending,
                            CustomerId = row.CustomerId
                        });
                    }
                }
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
            var documentNo = await _receivableService.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
            {
                CustomerId = target.CustomerId,
                ReceiptDate = target.Date,
                Amount = target.Amount,
                PaymentMethod = target.Method,
                BankAccount = BankAccount,
                Note = Note
            });
            target.Status = "Chờ duyệt";
            target.WorkflowStatus = VoucherWorkflowStatus.Pending;
            target.DocumentNo = string.IsNullOrWhiteSpace(documentNo) ? target.DocumentNo : documentNo;

            _notificationService.Show("Đã lưu phiếu thu, chuyển sang danh sách Chờ duyệt.");
            await NewAsync();
            ShowPendingTab();
            SelectedReceipt = target;
            OnPropertyChanged(nameof(TotalReceiptAmount));
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public enum VoucherWorkflowStatus
{
    Draft,
    Pending,
    Approved
}

public sealed class PaymentReceiptRow
{
    public string CustomerId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Tiền mặt";
    public string Status { get; set; } = "Chờ duyệt";
    public VoucherWorkflowStatus WorkflowStatus { get; set; } = VoucherWorkflowStatus.Pending;
}
