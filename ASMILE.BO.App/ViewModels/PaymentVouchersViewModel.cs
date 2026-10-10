using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class PaymentVouchersViewModel : ViewModelBase
{
    private readonly IPayableService _payableService;
    private readonly ISupplierAppService _supplierAppService;
    private readonly IPersonAppService _personService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IWindowManagerService _windowManagerService;
    private readonly Task _initializationTask;
    private readonly List<PaymentVoucherRow> _allRows = new();
    private readonly List<PayableSummaryDto> _allDebtSummaries = new();

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();
    public ObservableCollection<PersonDto> Receivers { get; } = new();
    public ObservableCollection<PaymentVoucherRow> Vouchers { get; } = new();
    public ObservableCollection<PayableSummaryDto> SupplierDebts { get; } = new();
    public TablePagerViewModel<PaymentVoucherRow> VoucherPager { get; } = new() { ItemName = "phiếu chi" };
    public TablePagerViewModel<PayableSummaryDto> DebtPager { get; } = new() { ItemName = "nhà cung cấp" };

    [ObservableProperty] private SupplierDto? selectedSupplier;
    [ObservableProperty] private PersonDto? selectedReceiver;
    [ObservableProperty] private PaymentVoucherRow? selectedVoucher;
    [ObservableProperty] private PayableSummaryDto? selectedDebtSummary;
    [ObservableProperty] private string searchText = string.Empty;

    [ObservableProperty] private DateTime paymentDate = DateTime.Today;
    [ObservableProperty] private string voucherNo = string.Empty;
    [ObservableProperty] private DateTime filterFromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime filterToDate = DateTime.Today;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private string paymentMethod = "Chuyển khoản";
    [ObservableProperty] private string payerName = string.Empty;
    [ObservableProperty] private string debitAccountCode = "331";
    [ObservableProperty] private string creditAccountCode = "111";
    [ObservableProperty] private string bankAccount = string.Empty;
    [ObservableProperty] private string note = string.Empty;

    [ObservableProperty] private bool isCreateTabSelected = true;
    [ObservableProperty] private bool isPendingTabSelected;
    [ObservableProperty] private bool isApprovedTabSelected;
    [ObservableProperty] private bool isDebtTabSelected;
    [ObservableProperty] private bool showAllDebts = true;
    [ObservableProperty] private bool showRemainingDebts;
    [ObservableProperty] private bool showOverdueDebts;

    public bool IsListTabSelected => !IsCreateTabSelected;
    public bool IsVoucherGridVisible => !IsDebtTabSelected;
    public bool IsDebtGridVisible => IsDebtTabSelected;
    public bool ShowFooterActions => !IsDebtTabSelected;
    public decimal TotalPaymentAmount => Vouchers.Sum(x => x.Amount);
    public decimal TotalDebtAmount => SupplierDebts.Sum(x => x.ClosingBalance);

    public PaymentVouchersViewModel(
        IPayableService payableService,
        ISupplierAppService supplierAppService,
        IPersonAppService personService,
        INotificationService notificationService,
        IDialogService dialogService,
        IWindowManagerService windowManagerService)
    {
        _payableService = payableService;
        _supplierAppService = supplierAppService;
        _personService = personService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _windowManagerService = windowManagerService;
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
    public IReadOnlyList<PaymentVoucherRow> AllRows => _allRows;

    public bool SelectVoucherByDocumentNo(string documentNo)
    {
        var row = _allRows.FirstOrDefault(x =>
            string.Equals(x.DocumentNo?.Trim(), documentNo?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return false;
        }

        SelectedVoucher = row;
        return true;
    }

    public async Task ApproveVoucherByDocumentNoAsync(string documentNo)
    {
        if (!SelectVoucherByDocumentNo(documentNo))
        {
            _dialogService.Error("Không tìm thấy phiếu chi thật để duyệt. Vui lòng tải lại danh sách.");
            return;
        }

        await ApproveSelected();
    }

    public async Task RejectVoucherByDocumentNoAsync(string documentNo, string reason)
    {
        if (!SelectVoucherByDocumentNo(documentNo))
        {
            _dialogService.Error("Không tìm thấy phiếu chi thật để từ chối. Vui lòng tải lại danh sách.");
            return;
        }

        if (SelectedVoucher?.WorkflowStatus != VoucherWorkflowStatus.Pending)
        {
            _dialogService.Error("Chỉ từ chối được phiếu chi đang chờ duyệt.");
            return;
        }

        var rejected = await _payableService.RejectPaymentVoucherAsync(documentNo, reason);
        if (!rejected)
        {
            _dialogService.Error("Không từ chối được phiếu chi ở trạng thái hiện tại.");
            return;
        }

        SelectedVoucher.Status = "Từ chối";
        SelectedVoucher.WorkflowStatus = VoucherWorkflowStatus.Rejected;
        _notificationService.Show("Đã từ chối phiếu chi trong CSDL và ghi audit.");
        ApplyCurrentTabFilter();
    }

    private async Task InitializeAsync()
    {
        // Number allocation is a transactional database operation. Do not run
        // it from constructor-started initialization while other module view
        // models are querying through the shared application scope.
        ResetEntryForm();
        await LoadAsync();
    }

    partial void OnSelectedSupplierChanged(SupplierDto? value) => ApplyCurrentTabFilter();

    partial void OnSelectedDebtSummaryChanged(PayableSummaryDto? value)
    {
        if (value is null)
        {
            return;
        }

        var supplier = Suppliers.FirstOrDefault(x =>
            string.Equals(x.SupplierCode?.Trim(), value.SupplierCode?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (supplier is not null && !ReferenceEquals(SelectedSupplier, supplier))
        {
            SelectedSupplier = supplier;
        }
    }

    partial void OnSelectedReceiverChanged(PersonDto? value) => PayerName = value?.FullName ?? string.Empty;
    partial void OnSelectedVoucherChanged(PaymentVoucherRow? value)
    {
        if (value is null)
        {
            return;
        }

        VoucherNo = value.DocumentNo;
        PaymentDate = value.Date;
        Amount = value.Amount;
        PaymentMethod = value.Method;
        DebitAccountCode = string.IsNullOrWhiteSpace(value.DebitAccountCode) ? "331" : value.DebitAccountCode;
        CreditAccountCode = string.IsNullOrWhiteSpace(value.CreditAccountCode) ? "111" : value.CreditAccountCode;
        Note = value.Note ?? string.Empty;

        var supplier = Suppliers.FirstOrDefault(x =>
            string.Equals(x.SupplierCode?.Trim(), value.SupplierCode?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (supplier is not null)
        {
            SelectedSupplier = supplier;
        }
    }
    partial void OnSearchTextChanged(string value) => ApplyCurrentTabFilter();
    partial void OnFilterFromDateChanged(DateTime value) => ApplyCurrentTabFilter();
    partial void OnFilterToDateChanged(DateTime value) => ApplyCurrentTabFilter();
    partial void OnIsCreateTabSelectedChanged(bool value) => OnPropertyChanged(nameof(IsListTabSelected));
    partial void OnShowAllDebtsChanged(bool value)
    {
        if (!value)
        {
            if (!ShowRemainingDebts && !ShowOverdueDebts) ShowAllDebts = true;
            return;
        }
        if (ShowRemainingDebts) ShowRemainingDebts = false;
        if (ShowOverdueDebts) ShowOverdueDebts = false;
        ApplyCurrentTabFilter();
    }
    partial void OnShowRemainingDebtsChanged(bool value)
    {
        if (!value)
        {
            if (!ShowAllDebts && !ShowOverdueDebts) ShowRemainingDebts = true;
            return;
        }
        if (ShowAllDebts) ShowAllDebts = false;
        if (ShowOverdueDebts) ShowOverdueDebts = false;
        ApplyCurrentTabFilter();
    }
    partial void OnShowOverdueDebtsChanged(bool value)
    {
        if (!value)
        {
            if (!ShowAllDebts && !ShowRemainingDebts) ShowOverdueDebts = true;
            return;
        }
        if (ShowAllDebts) ShowAllDebts = false;
        if (ShowRemainingDebts) ShowRemainingDebts = false;
        ApplyCurrentTabFilter();
    }
    partial void OnIsDebtTabSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsVoucherGridVisible));
        OnPropertyChanged(nameof(IsDebtGridVisible));
        OnPropertyChanged(nameof(ShowFooterActions));
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private void ShowCreateTab()
    {
        IsCreateTabSelected = true;
        IsPendingTabSelected = false;
        IsApprovedTabSelected = false;
        IsDebtTabSelected = false;
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private void ShowPendingTab()
    {
        IsCreateTabSelected = false;
        IsPendingTabSelected = true;
        IsApprovedTabSelected = false;
        IsDebtTabSelected = false;
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private void ShowApprovedTab()
    {
        IsCreateTabSelected = false;
        IsPendingTabSelected = false;
        IsApprovedTabSelected = true;
        IsDebtTabSelected = false;
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private void ShowDebtTab()
    {
        IsCreateTabSelected = false;
        IsPendingTabSelected = false;
        IsApprovedTabSelected = false;
        IsDebtTabSelected = true;
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private async Task NewAsync()
    {
        await _initializationTask;
        VoucherNo = await _payableService.GetNextPaymentVoucherNoAsync();
        ResetEntryForm();
    }

    private void ResetEntryForm()
    {
        Amount = 0;
        Note = string.Empty;
        PayerName = string.Empty;
        DebitAccountCode = "331";
        CreditAccountCode = "111";
        PaymentDate = DateTime.Today;
        PaymentMethod = "Chuyển khoản";
    }

    [RelayCommand]
    private async Task AddDraftAsync()
    {
        await _initializationTask;

        if (SelectedSupplier is null)
        {
            _dialogService.Error("Chọn nhà cung cấp trước khi thêm dòng.");
            return;
        }

        if (Amount <= 0)
        {
            _dialogService.Error("Số tiền chi phải lớn hơn 0.");
            return;
        }

        if (string.IsNullOrWhiteSpace(VoucherNo))
        {
            VoucherNo = await _payableService.GetNextPaymentVoucherNoAsync();
        }

        var draft = new PaymentVoucherRow
        {
            Date = PaymentDate,
            DocumentNo = VoucherNo.Trim(),
            SupplierCode = SelectedSupplier.SupplierCode,
            SupplierName = SelectedSupplier.SupplierName,
            Amount = Amount,
            Method = PaymentMethod,
            DebitAccountCode = string.IsNullOrWhiteSpace(DebitAccountCode) ? "331" : DebitAccountCode,
            CreditAccountCode = string.IsNullOrWhiteSpace(CreditAccountCode) ? "111" : CreditAccountCode,
            Note = Note?.Trim() ?? string.Empty,
            Status = "Lập phiếu",
            WorkflowStatus = VoucherWorkflowStatus.Draft,
            SupplierId = SelectedSupplier.Id.ToString()
        };

        _allRows.Insert(0, draft);
        IsCreateTabSelected = true;
        IsPendingTabSelected = false;
        IsApprovedTabSelected = false;
        ApplyCurrentTabFilter();
        SelectedVoucher = Vouchers.FirstOrDefault(x => x.DocumentNo == draft.DocumentNo) ?? draft;
        OnPropertyChanged(nameof(TotalPaymentAmount));
    }

    [RelayCommand]
    private void DeleteDraft()
    {
        if (SelectedVoucher is null)
        {
            _dialogService.Error("Chọn một phiếu ở tab Lập phiếu để xóa.");
            return;
        }

        if (SelectedVoucher.WorkflowStatus != VoucherWorkflowStatus.Draft)
        {
            _dialogService.Error("Chỉ xóa được phiếu ở tab Lập phiếu.");
            return;
        }

        _allRows.Remove(SelectedVoucher);
        ApplyCurrentTabFilter();
        SelectedVoucher = Vouchers.FirstOrDefault();
        OnPropertyChanged(nameof(TotalPaymentAmount));
        if (SelectedVoucher is null)
        {
            _ = NewAsync();
        }
    }

    [RelayCommand]
    private async Task ApproveSelected()
    {
        if (SelectedVoucher is null)
        {
            _dialogService.Error("Chọn một phiếu ở danh sách chờ duyệt.");
            return;
        }

        if (SelectedVoucher.WorkflowStatus != VoucherWorkflowStatus.Pending)
        {
            _dialogService.Error("Chỉ duyệt được phiếu ở tab Chờ duyệt.");
            return;
        }

        await _payableService.SetPaymentVoucherApprovedAsync(SelectedVoucher.DocumentNo, true);
        var isApprovedInDb = await _payableService.GetPaymentVoucherApprovedAsync(SelectedVoucher.DocumentNo);
        if (isApprovedInDb != true)
        {
            _dialogService.Error("Không tìm thấy phiếu trong CSDL để duyệt. Vui lòng tải lại danh sách và thử lại.");
            return;
        }

        SelectedVoucher.WorkflowStatus = VoucherWorkflowStatus.Approved;
        SelectedVoucher.Status = "Đã duyệt";
        await ReloadDebtSummariesAsync();
        ShowApprovedTab();
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedVoucher is null)
        {
            _dialogService.Error("Chọn một phiếu ở danh sách chờ duyệt.");
            return;
        }

        if (SelectedVoucher.WorkflowStatus != VoucherWorkflowStatus.Pending)
        {
            _dialogService.Error("Chỉ sửa được phiếu ở tab Chờ duyệt.");
            return;
        }

        SelectedVoucher.WorkflowStatus = VoucherWorkflowStatus.Draft;
        SelectedVoucher.Status = "Lập phiếu";
        VoucherNo = SelectedVoucher.DocumentNo;
        PaymentDate = SelectedVoucher.Date;
        Amount = SelectedVoucher.Amount;
        PaymentMethod = SelectedVoucher.Method;
        SelectedSupplier = Suppliers.FirstOrDefault(x => string.Equals(x.SupplierCode, SelectedVoucher.SupplierCode, StringComparison.OrdinalIgnoreCase)) ?? SelectedSupplier;
        ShowCreateTab();
        ApplyCurrentTabFilter();
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        if (SelectedVoucher is null)
        {
            _dialogService.Error("Chọn một phiếu ở danh sách chờ duyệt.");
            return;
        }

        if (SelectedVoucher.WorkflowStatus != VoucherWorkflowStatus.Pending)
        {
            _dialogService.Error("Chỉ xóa được phiếu ở tab Chờ duyệt.");
            return;
        }

        if (SelectedVoucher.IsPersisted)
        {
            var cancelled = await _payableService.CancelPaymentVoucherAsync(SelectedVoucher.DocumentNo);
            if (!cancelled)
            {
                _dialogService.Error("Không hủy được phiếu chi. Chỉ phiếu chờ duyệt mới được hủy và phải còn nguyên trạng thái trong CSDL.");
                return;
            }

            SelectedVoucher.Status = "Đã hủy";
            SelectedVoucher.WorkflowStatus = VoucherWorkflowStatus.Cancelled;
            _notificationService.Show("Đã hủy phiếu chi trong CSDL và ghi audit.");
        }
        else
        {
            _allRows.Remove(SelectedVoucher);
        }

        ApplyCurrentTabFilter();
        SelectedVoucher = Vouchers.FirstOrDefault();
        OnPropertyChanged(nameof(TotalPaymentAmount));
    }

    [RelayCommand]
    private async Task Reapprove()
    {
        if (SelectedVoucher is null)
        {
            _dialogService.Error("Chọn một phiếu ở danh sách đã duyệt.");
            return;
        }

        await _payableService.SetPaymentVoucherApprovedAsync(SelectedVoucher.DocumentNo, false);
        var isApprovedInDb = await _payableService.GetPaymentVoucherApprovedAsync(SelectedVoucher.DocumentNo);
        if (isApprovedInDb is null)
        {
            _dialogService.Error("Không tìm thấy phiếu trong CSDL để chuyển trạng thái. Vui lòng tải lại danh sách.");
            return;
        }

        SelectedVoucher.WorkflowStatus = VoucherWorkflowStatus.Pending;
        SelectedVoucher.Status = "Chờ duyệt";
        await ReloadDebtSummariesAsync();
        ShowPendingTab();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var suppliers = await _supplierAppService.SearchAsync(string.Empty);
            Suppliers.Clear();
            foreach (var s in suppliers.OrderBy(x => x.SupplierName))
            {
                Suppliers.Add(s);
            }

            var people = await _personService.SearchAsync(null, string.Empty, null);
            Receivers.Clear();
            foreach (var person in people.OrderBy(x => x.FullName))
            {
                Receivers.Add(person);
            }

            if (SelectedSupplier is not null && !Suppliers.Any(x => string.Equals(x.SupplierCode, SelectedSupplier.SupplierCode, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedSupplier = null;
            }
            if (SelectedReceiver is not null && !Receivers.Any(x => x.Id == SelectedReceiver.Id))
            {
                SelectedReceiver = null;
            }

            var summaries = await ReloadDebtSummariesAsync();

            if (_allRows.Count == 0 && Suppliers.Count > 0)
            {
                foreach (var supplier in Suppliers)
                {
                    var summary = summaries.FirstOrDefault(s => string.Equals(s.SupplierCode, supplier.SupplierCode, StringComparison.OrdinalIgnoreCase));
                    if (summary is null)
                    {
                        continue;
                    }

                    var tx = await _payableService.GetTransactionsBySupplierAsync(summary.SupplierId, DateTime.Today.AddMonths(-12), DateTime.Today);
                    foreach (var row in tx.Where(x => x.DocumentType == PayableDocumentType.PaymentVoucher || x.DocumentType == PayableDocumentType.AdvancePayment).OrderByDescending(x => x.TransactionDate))
                    {
                        var isApproved = row.DocumentType == PayableDocumentType.PaymentVoucher
                            ? await _payableService.GetPaymentVoucherApprovedAsync(row.DocumentNo)
                            : true;
                        var isCancelled = row.Status == DebtStatus.Cancelled;
                        var isRejected = row.Status == DebtStatus.Rejected;
                        _allRows.Add(new PaymentVoucherRow
                        {
                            IsPersisted = true,
                            Date = row.TransactionDate,
                            DocumentNo = row.DocumentNo,
                            SupplierCode = row.SupplierCode,
                            SupplierName = row.SupplierName,
                            Amount = row.DebitAmount,
                            Method = string.IsNullOrWhiteSpace(PaymentMethod) ? "Chuyển khoản" : PaymentMethod,
                            DebitAccountCode = "331",
                            CreditAccountCode = "111",
                            Note = row.Description ?? string.Empty,
                            Status = isRejected ? "Từ chối" : isCancelled ? "Đã hủy" : (isApproved ?? false) ? "Đã duyệt" : "Chờ duyệt",
                            WorkflowStatus = isRejected ? VoucherWorkflowStatus.Rejected : isCancelled ? VoucherWorkflowStatus.Cancelled : (isApproved ?? false) ? VoucherWorkflowStatus.Approved : VoucherWorkflowStatus.Pending,
                            SupplierId = row.SupplierId
                        });
                    }
                }
            }

            ApplyCurrentTabFilter();
        }
        catch (Exception ex)
        {
            _dialogService.Error($"Không tải được dữ liệu phiếu chi từ CSDL: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyCurrentTabFilter()
    {
        var query = _allRows.AsEnumerable();
        var debtQuery = _allDebtSummaries.AsEnumerable();

        // Ở tab công nợ, không lọc theo nhà cung cấp đang chọn trên form lập phiếu
        // để tránh che mất các khoản công nợ còn tồn.
        if (SelectedSupplier is not null)
        {
            query = query.Where(x => x.SupplierCode == SelectedSupplier.SupplierCode);
            if (!IsDebtTabSelected)
            {
                debtQuery = debtQuery.Where(x => x.SupplierCode == SelectedSupplier.SupplierCode);
            }
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var keyword = SearchText.Trim();
            query = query.Where(x => x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || x.SupplierCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || x.SupplierName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            debtQuery = debtQuery.Where(x => x.SupplierCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || x.SupplierName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
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

        if (!IsCreateTabSelected && !IsDebtTabSelected)
        {
            query = query.Where(x => x.Date.Date >= FilterFromDate.Date && x.Date.Date <= FilterToDate.Date);
        }

        Vouchers.Clear();
        foreach (var row in query.OrderByDescending(x => x.Date))
        {
            Vouchers.Add(row);
        }
        VoucherPager.SetSource(Vouchers);

        SupplierDebts.Clear();
        if (ShowRemainingDebts)
        {
            debtQuery = debtQuery.Where(x => x.ClosingBalance > 0);
        }
        else if (ShowOverdueDebts)
        {
            debtQuery = debtQuery.Where(x => x.OverdueAmount > 0);
        }
        foreach (var debt in debtQuery.OrderByDescending(x => x.ClosingBalance))
        {
            SupplierDebts.Add(debt);
        }
        DebtPager.SetSource(SupplierDebts);

        OnPropertyChanged(nameof(TotalPaymentAmount));
        OnPropertyChanged(nameof(TotalDebtAmount));
    }

    [RelayCommand]
    private async Task SaveVoucherAsync()
    {
        var target = SelectedVoucher;
        if (target is null || target.WorkflowStatus != VoucherWorkflowStatus.Draft)
        {
            target = _allRows.FirstOrDefault(x =>
                x.WorkflowStatus == VoucherWorkflowStatus.Draft &&
                (string.Equals(x.DocumentNo, VoucherNo, StringComparison.OrdinalIgnoreCase) ||
                 (SelectedSupplier is not null && string.Equals(x.SupplierCode, SelectedSupplier.SupplierCode, StringComparison.OrdinalIgnoreCase))));
        }

        if (target is null || target.WorkflowStatus != VoucherWorkflowStatus.Draft)
        {
            _dialogService.Error("Chọn dòng ở tab Lập phiếu trước khi lưu.");
            return;
        }

        if (SelectedSupplier is null)
        {
            _dialogService.Error("Chọn nhà cung cấp trước khi lưu phiếu chi.");
            return;
        }

        if (Amount <= 0)
        {
            _dialogService.Error("Số tiền chi phải lớn hơn 0.");
            return;
        }

        // Đồng bộ dữ liệu form vào dòng draft hiện tại trước khi lưu xuống CSDL.
        target.Date = PaymentDate;
        target.SupplierCode = SelectedSupplier.SupplierCode;
        target.SupplierName = SelectedSupplier.SupplierName;
        target.SupplierId = SelectedSupplier.Id.ToString();
        target.Amount = Amount;
        target.Method = string.IsNullOrWhiteSpace(PaymentMethod) ? "Chuyển khoản" : PaymentMethod;
        target.DebitAccountCode = string.IsNullOrWhiteSpace(DebitAccountCode) ? "331" : DebitAccountCode.Trim();
        target.CreditAccountCode = string.IsNullOrWhiteSpace(CreditAccountCode) ? "111" : CreditAccountCode.Trim();
        target.Note = Note?.Trim() ?? string.Empty;
        target.DocumentNo = string.IsNullOrWhiteSpace(VoucherNo) ? target.DocumentNo : VoucherNo.Trim();

        try
        {
            var documentNo = await _payableService.CreatePaymentVoucherAsync(new CreatePaymentVoucherRequest
            {
                SupplierId = target.SupplierId,
                PaymentDate = target.Date,
                Amount = target.Amount,
                PaymentMethod = target.Method,
                BankAccount = BankAccount,
                Note = target.Note
            });
            if (string.IsNullOrWhiteSpace(documentNo))
            {
                _dialogService.Error("Không tạo được phiếu chi trong CSDL. Kiểm tra lại dữ liệu nhà cung cấp.");
                return;
            }
            target.Status = "Chờ duyệt";
            target.WorkflowStatus = VoucherWorkflowStatus.Pending;
            target.DocumentNo = documentNo;
            await _payableService.SetPaymentVoucherApprovedAsync(target.DocumentNo, false);
            await ReloadDebtSummariesAsync();

            _notificationService.Show("Đã lưu phiếu chi, chuyển sang danh sách Chờ duyệt.");
            ApplyCurrentTabFilter();
            await NewAsync();
            ShowPendingTab();
            SelectedVoucher = target;
            OnPropertyChanged(nameof(TotalPaymentAmount));
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    public async Task RefreshAsync() => await LoadAsync();

    private async Task<IReadOnlyList<PayableSummaryDto>> ReloadDebtSummariesAsync()
    {
        var summaries = await _payableService.GetDebtSummariesAsync(new PayableFilterDto
        {
            // Công nợ phải trả cần phản ánh toàn bộ số dư còn lại, không bó hẹp theo ngày lọc của tab phiếu.
            Keyword = SearchText.Trim()
        });

        _allDebtSummaries.Clear();
        _allDebtSummaries.AddRange(summaries);
        return summaries;
    }
    [RelayCommand]
    private async Task OpenDebtDetailAsync(PayableSummaryDto? summary)
    {
        var target = summary ?? SelectedDebtSummary;
        if (target is null)
        {
            return;
        }

        try
        {
            var transactions = await _payableService.GetTransactionsBySupplierAsync(
                target.SupplierId,
                FilterFromDate,
                FilterToDate);

            var moduleKey = $"payable-debt-detail-{target.SupplierCode?.Trim().ToLowerInvariant()}";
            var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
            if (existing is not null)
            {
                _windowManagerService.Close(existing);
            }

            var module = new AppModuleInfo(
                moduleKey,
                $"Chi tiết công nợ {target.SupplierCode}",
                "\uE8C8",
                "Chi tiết công nợ phải trả",
                "#8B5CF6",
                isPinned: false,
                showOnDesktop: false,
                iconPath: ApplicationAssetPaths.Icon(@"Icons\applications\payment-voucher.ico"));

            var vm = new PayableDebtDetailModuleViewModel(
                target.SupplierCode ?? string.Empty,
                target.SupplierName ?? string.Empty,
                transactions,
                FilterFromDate,
                FilterToDate,
                _windowManagerService,
                moduleKey,
                ApplyDebtPaymentSelection);

            vm.RequestClose += () =>
            {
                var opened = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
                if (opened is not null)
                {
                    _windowManagerService.Close(opened);
                }
            };

            _windowManagerService.OpenOrActivate(module, () => vm);
        }
        catch (Exception ex)
        {
            _dialogService.Error($"Không mở được chi tiết công nợ: {ex.Message}");
        }
    }

    private void ApplyDebtPaymentSelection(PayableDebtPaymentSelection selection)
    {
        var supplier = Suppliers.FirstOrDefault(x =>
            string.Equals(x.SupplierCode?.Trim(), selection.SupplierCode?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? Suppliers.FirstOrDefault(x =>
                string.Equals(x.SupplierName?.Trim(), selection.SupplierName?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (supplier is not null)
        {
            SelectedSupplier = supplier;
        }

        Amount = selection.TotalAmount;
        PaymentDate = DateTime.Today;
        ShowCreateTab();
        Note = $"Ghi chú: thanh toán cho phiếu nhập {string.Join(", ", selection.DocumentNos)}";
    }

    [RelayCommand]
    private void OpenVoucherDetail(PaymentVoucherRow? row)
    {
        var target = row ?? SelectedVoucher;
        if (target is null || target.WorkflowStatus == VoucherWorkflowStatus.Draft)
        {
            return;
        }

        var safeDocNo = string.IsNullOrWhiteSpace(target.DocumentNo)
            ? Guid.NewGuid().ToString("N")
            : target.DocumentNo.Replace(" ", string.Empty);
        var moduleKey = $"payment-voucher-detail-{safeDocNo.ToLowerInvariant()}";

        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            moduleKey,
            $"Chi tiết {target.DocumentNo}",
            "\uE8C8",
            "Chi tiết phiếu chi",
            "#8B5CF6",
            isPinned: false,
            showOnDesktop: false,
            iconPath: ApplicationAssetPaths.Icon(@"Icons\applications\payment-voucher.ico"));

        var vm = new PaymentVoucherDetailModuleViewModel(target, _windowManagerService, moduleKey);
        vm.RequestClose += () =>
        {
            var opened = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
            if (opened is not null)
            {
                _windowManagerService.Close(opened);
            }
        };

        _windowManagerService.OpenOrActivate(module, () => vm);
    }
}

public sealed class PaymentVoucherRow
{
    public bool IsPersisted { get; set; }
    public string SupplierId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Chuyển khoản";
    public string DebitAccountCode { get; set; } = "331";
    public string CreditAccountCode { get; set; } = "111";
    public string Note { get; set; } = string.Empty;
    public string Status { get; set; } = "Chờ duyệt";
    public VoucherWorkflowStatus WorkflowStatus { get; set; } = VoucherWorkflowStatus.Pending;
}











