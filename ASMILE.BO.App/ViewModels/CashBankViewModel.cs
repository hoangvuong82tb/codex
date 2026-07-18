using ASMILE.BO.App.Services;
using ASMILE.BO.App.Models;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class CashBankViewModel : ViewModelBase
{
    private readonly ICashFundService _cashFundService;
    private readonly IBankingService _bankingService;
    private readonly IReportEngine _reportEngine;
    private readonly IImportEngine _importEngine;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ICurrentUserService _currentUserService;
    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _filterDebounceCts;
    private CancellationTokenSource? _reportCts;
    private CancellationTokenSource? _importCts;
    private bool _suppressFilterRefresh;

    public ObservableCollection<CashFundDto> CashFunds { get; } = new();
    public ObservableCollection<BankAccountDto> BankAccounts { get; } = new();
    public ObservableCollection<CashTransactionDto> CashTransactions { get; } = new();
    public ObservableCollection<BankTransactionDto> BankTransactions { get; } = new();
    public ObservableCollection<CashBookEntryDto> CashBookRows { get; } = new();
    public ObservableCollection<BankBookEntryDto> BankBookRows { get; } = new();
    public ObservableCollection<BankStatementDto> BankStatements { get; } = new();
    public ObservableCollection<BankStatementLineDto> StatementLines { get; } = new();
    public ObservableCollection<BankReconciliationDto> Reconciliations { get; } = new();
    public ObservableCollection<BankReconciliationLineDto> ReconciliationLines { get; } = new();
    public ObservableCollection<ImportRowDto> ImportPreviewRows { get; } = new();
    public ObservableCollection<CashBankValidationMessage> ValidationMessages { get; } = new();
    public ObservableCollection<CashBankReportRow> ReportRows { get; } = new();
    public ObservableCollection<ReportExecutionDto> ReportHistory { get; } = new();
    public TablePagerViewModel<CashTransactionDto> CashTransactionsPager { get; } = new() { ItemName = "giao dịch quỹ" };
    public TablePagerViewModel<BankTransactionDto> BankTransactionsPager { get; } = new() { ItemName = "giao dịch ngân hàng" };
    public TablePagerViewModel<ImportRowDto> ImportPreviewPager { get; } = new() { ItemName = "dòng sao kê" };
    public TablePagerViewModel<BankReconciliationDto> ReconciliationsPager { get; } = new() { ItemName = "phiếu đối chiếu" };
    public TablePagerViewModel<BankReconciliationLineDto> ReconciliationLinesPager { get; } = new() { ItemName = "dòng đối chiếu" };
    public TablePagerViewModel<CashBankReportRow> ReportRowsPager { get; } = new() { ItemName = "dòng báo cáo" };
    public TablePagerViewModel<ReportExecutionDto> ReportHistoryPager { get; } = new() { ItemName = "lần chạy báo cáo" };
    public ObservableCollection<string> CashEntryTypes { get; } = new() { "Thu tiền", "Chi tiền", "Chuyển nội bộ" };
    public ObservableCollection<string> BankEntryTypes { get; } = new() { "Thu tiền", "Chi tiền" };
    public ObservableCollection<EnterpriseReportOption> ReportCodes { get; } = new()
    {
        new("CASH_BOOK", "Sổ quỹ"),
        new("CASH_FLOW", "Lưu chuyển tiền"),
        new("BANK_BOOK", "Sổ ngân hàng"),
        new("BANK_RECONCILIATION", "Đối chiếu ngân hàng"),
        new("DAILY_CASHFLOW", "Dòng tiền ngày"),
        new("CASH_BALANCE", "Số dư tiền")
    };
    public ObservableCollection<string> ExportFormats { get; } = new() { "Excel", "PDF", "CSV" };
    public ObservableCollection<EnterpriseReportOption> FavoriteReports { get; } = new();

    [ObservableProperty] private CashFundDto? selectedCashFund;
    [ObservableProperty] private BankAccountDto? selectedBankAccount;
    [ObservableProperty] private CashTransactionDto? selectedCashTransaction;
    [ObservableProperty] private BankTransactionDto? selectedBankTransaction;
    [ObservableProperty] private BankStatementDto? selectedStatement;
    [ObservableProperty] private BankReconciliationDto? selectedReconciliation;
    [ObservableProperty] private DateTime? fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? toDate = DateTime.Today;
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private string selectedCashEntryType = "Thu tiền";
    [ObservableProperty] private string selectedBankEntryType = "Thu tiền";
    [ObservableProperty] private string selectedReportCode = "CASH_BOOK";
    [ObservableProperty] private string selectedExportFormat = "Excel";
    [ObservableProperty] private string importStatusText = "Chưa chọn file sao kê.";
    [ObservableProperty] private string importWarningSummary = string.Empty;
    [ObservableProperty] private bool isSummaryExpanded;
    [ObservableProperty] private bool isFilterExpanded;
    [ObservableProperty] private Guid? currentImportSessionId;
    [ObservableProperty] private bool isImportLoading;
    [ObservableProperty] private bool isReportLoading;
    [ObservableProperty] private int selectedTabIndex;
    [ObservableProperty] private decimal currentFundBalance;
    [ObservableProperty] private decimal dailyInflow;
    [ObservableProperty] private decimal dailyOutflow;
    [ObservableProperty] private decimal pendingCashAmount;
    [ObservableProperty] private int pendingCashTransactions;
    [ObservableProperty] private string lockedPeriodState = "Kiểm tra khi duyệt/ghi sổ";
    [ObservableProperty] private decimal bankCurrentBalance;
    [ObservableProperty] private decimal bankAvailableBalance;
    [ObservableProperty] private decimal bankPendingAmount;
    [ObservableProperty] private string lastReconciliationText = "Chưa có đối chiếu";
    [ObservableProperty] private decimal matchedAmount;
    [ObservableProperty] private decimal unmatchedAmount;
    [ObservableProperty] private decimal reconciliationDifference;
    [ObservableProperty] private string drilldownText = "Chọn giao dịch hoặc đối chiếu để xem bút toán và nguồn chứng từ.";
    [ObservableProperty] private QuickCashEntry quickCashEntry = new();
    [ObservableProperty] private QuickBankEntry quickBankEntry = new();
    [ObservableProperty] private Guid? editingCashTransactionId;
    [ObservableProperty] private Guid? editingBankTransactionId;
    [ObservableProperty] private DateTime lastRefreshAt = DateTime.Now;
    [ObservableProperty] private string statusText = "Quy trình quỹ và ngân hàng dùng dữ liệu đã lưu.";

    public string LastRefreshText => $"Cập nhật {LastRefreshAt:HH:mm:ss}";
    public string CashBalanceText => $"{CurrentFundBalance:N0}";
    public string BankBalanceText => $"{BankCurrentBalance:N0}";
    public string PendingText => $"{PendingCashTransactions:N0} giao dịch chờ";
    public string ImportTemplateText => "Mẫu nhập sao kê ngân hàng";
    public bool CanCommitImport => CurrentImportSessionId.HasValue && ImportPreviewRows.Count > 0;
    public string CashLedgerAccountCode => string.IsNullOrWhiteSpace(SelectedCashFund?.AccountCode) ? "111" : SelectedCashFund.AccountCode!.Trim();
    public string BankLedgerAccountCode => string.IsNullOrWhiteSpace(SelectedBankAccount?.AccountCode) ? "112" : SelectedBankAccount.AccountCode!.Trim();
    public bool CanCancelCashApproval => IsApprovedStatus(SelectedCashTransaction?.Status);
    public bool CanCancelBankApproval => IsApprovedStatus(SelectedBankTransaction?.Status);
    public bool CanEditCashTransaction => IsDraftStatus(SelectedCashTransaction?.Status);
    public bool CanDeleteCashTransaction => IsDraftStatus(SelectedCashTransaction?.Status);
    public bool CanEditBankTransaction => IsDraftStatus(SelectedBankTransaction?.Status);
    public bool CanDeleteBankTransaction => IsDraftStatus(SelectedBankTransaction?.Status);
    public string QuickCashSaveText => EditingCashTransactionId.HasValue ? "Cập nhật" : "Lưu";
    public string QuickBankSaveText => EditingBankTransactionId.HasValue ? "Cập nhật" : "Lưu";

    public string QuickCashDebitAccountCode
    {
        get => IsCashReceipt() ? CashLedgerAccountCode : QuickCashEntry.CounterAccountCode;
        set
        {
            if (!IsCashReceipt())
            {
                QuickCashEntry.CounterAccountCode = value;
                OnCashAccountDisplayChanged();
            }
        }
    }

    public string QuickCashCreditAccountCode
    {
        get => IsCashReceipt() ? QuickCashEntry.CounterAccountCode : CashLedgerAccountCode;
        set
        {
            if (IsCashReceipt())
            {
                QuickCashEntry.CounterAccountCode = value;
                OnCashAccountDisplayChanged();
            }
        }
    }

    public string QuickBankDebitAccountCode
    {
        get => IsBankReceipt() ? BankLedgerAccountCode : QuickBankEntry.CounterAccountCode;
        set
        {
            if (!IsBankReceipt())
            {
                QuickBankEntry.CounterAccountCode = value;
                OnBankAccountDisplayChanged();
            }
        }
    }

    public string QuickBankCreditAccountCode
    {
        get => IsBankReceipt() ? QuickBankEntry.CounterAccountCode : BankLedgerAccountCode;
        set
        {
            if (IsBankReceipt())
            {
                QuickBankEntry.CounterAccountCode = value;
                OnBankAccountDisplayChanged();
            }
        }
    }

    public CashBankViewModel(
        ICashFundService cashFundService,
        IBankingService bankingService,
        IReportEngine reportEngine,
        IImportEngine importEngine,
        INotificationService notificationService,
        IDialogService dialogService,
        ICurrentUserService currentUserService)
    {
        _cashFundService = cashFundService;
        _bankingService = bankingService;
        _reportEngine = reportEngine;
        _importEngine = importEngine;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _currentUserService = currentUserService;
        FavoriteReports.ResetWith(ReportCodes.Where(x => x.Code is "CASH_BOOK" or "BANK_RECONCILIATION" or "DAILY_CASHFLOW"));
        _ = RefreshAsync();
    }

    partial void OnKeywordChanged(string value) => QueueFilterRefresh();
    partial void OnFromDateChanged(DateTime? value) => QueueFilterRefresh();
    partial void OnToDateChanged(DateTime? value) => QueueFilterRefresh();
    partial void OnSelectedCashFundChanged(CashFundDto? value)
    {
        OnCashAccountDisplayChanged();
        QueueFilterRefresh();
    }

    partial void OnSelectedBankAccountChanged(BankAccountDto? value)
    {
        OnBankAccountDisplayChanged();
        QueueFilterRefresh();
    }

    partial void OnSelectedCashEntryTypeChanged(string value) => OnCashAccountDisplayChanged();
    partial void OnSelectedBankEntryTypeChanged(string value) => OnBankAccountDisplayChanged();
    partial void OnEditingCashTransactionIdChanged(Guid? value) => OnPropertyChanged(nameof(QuickCashSaveText));
    partial void OnEditingBankTransactionIdChanged(Guid? value) => OnPropertyChanged(nameof(QuickBankSaveText));

    partial void OnSelectedCashTransactionChanged(CashTransactionDto? value)
    {
        OnPropertyChanged(nameof(CanCancelCashApproval));
        OnPropertyChanged(nameof(CanEditCashTransaction));
        OnPropertyChanged(nameof(CanDeleteCashTransaction));
        if (value is null)
        {
            return;
        }

        DrilldownText = BuildCashDrilldown(value);
    }

    partial void OnSelectedBankTransactionChanged(BankTransactionDto? value)
    {
        OnPropertyChanged(nameof(CanCancelBankApproval));
        OnPropertyChanged(nameof(CanEditBankTransaction));
        OnPropertyChanged(nameof(CanDeleteBankTransaction));
        if (value is null)
        {
            return;
        }

        DrilldownText = BuildBankDrilldown(value);
    }

    partial void OnSelectedStatementChanged(BankStatementDto? value)
    {
        StatementLines.ResetWith(value?.Lines ?? Array.Empty<BankStatementLineDto>());
    }

    partial void OnSelectedReconciliationChanged(BankReconciliationDto? value)
    {
        if (value is null)
        {
            ReconciliationLines.Clear();
            RecalculateReconciliationTotals();
            return;
        }

        DrilldownText = $"Đối chiếu {value.ReconciliationNo} | tài khoản {value.BankAccountId} | trạng thái {value.Status} | chênh lệch {value.Difference:N0}";
        _ = LoadReconciliationLinesAsync(value.Id);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;
        IsBusy = true;
        ValidationMessages.Clear();

        try
        {
            _suppressFilterRefresh = true;
            var from = FromDate ?? DateTime.Today.AddMonths(-1);
            var to = ToDate ?? DateTime.Today;
            var cashFunds = await _cashFundService.GetFundsAsync(token);
            var bankAccounts = await _bankingService.GetAccountsAsync(token);

            CashFunds.ResetWith(cashFunds);
            BankAccounts.ResetWith(bankAccounts);
            SelectedCashFund ??= CashFunds.FirstOrDefault(x => x.IsDefault) ?? CashFunds.FirstOrDefault();
            SelectedBankAccount ??= BankAccounts.FirstOrDefault(x => x.IsDefault) ?? BankAccounts.FirstOrDefault();

            var cashFundId = SelectedCashFund?.Id;
            var bankAccountId = SelectedBankAccount?.Id;
            var cashTransactions = await _cashFundService.GetTransactionsAsync(cashFundId, from, to, Keyword, token);
            var bankTransactions = await _bankingService.GetTransactionsAsync(bankAccountId, from, to, Keyword, token);
            var cashBook = await _cashFundService.GetCashBookAsync(cashFundId, from, to, token);
            var bankBook = await _bankingService.GetBankBookAsync(bankAccountId, from, to, token);
            var statements = await _bankingService.GetStatementsAsync(bankAccountId, 50, token);
            var reconciliations = await _bankingService.GetReconciliationsAsync(bankAccountId, 50, token);

            CashTransactions.ResetWith(cashTransactions);
            BankTransactions.ResetWith(bankTransactions);
            CashBookRows.ResetWith(cashBook);
            BankBookRows.ResetWith(bankBook);
            BankStatements.ResetWith(statements);
            Reconciliations.ResetWith(reconciliations);
            CashTransactionsPager.SetSource(CashTransactions);
            BankTransactionsPager.SetSource(BankTransactions);
            ReconciliationsPager.SetSource(Reconciliations);
            SelectedCashTransaction ??= CashTransactions.FirstOrDefault();
            SelectedBankTransaction ??= BankTransactions.FirstOrDefault();
            SelectedStatement ??= BankStatements.FirstOrDefault();
            SelectedReconciliation ??= Reconciliations.FirstOrDefault();

            RecalculateBalances();
            BuildInlineValidation();
            await RefreshReportHistoryAsync(SelectedReportCode, token);
            LastRefreshAt = DateTime.Now;
            StatusText = $"Đã tải {CashTransactions.Count:N0} giao dịch quỹ, {BankTransactions.Count:N0} giao dịch ngân hàng.";
            OnPropertyChanged(nameof(LastRefreshText));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Làm mới", ex.Message);
            StatusText = "Không tải được quy trình quỹ và ngân hàng.";
        }
        finally
        {
            _suppressFilterRefresh = false;
            IsBusy = false;
            Interlocked.Exchange(ref _runWorkflowDepth, 0);
        }
    }

    [RelayCommand]
    private async Task SaveQuickCashAsync()
    {
        if (SelectedCashFund is null)
        {
            AddValidation("Error", "Quỹ", "Chọn quỹ trước khi nhập nhanh.");
            return;
        }

        if (QuickCashEntry.Amount <= 0)
        {
            AddValidation("Error", "Quỹ", "Số tiền phải lớn hơn 0.");
            return;
        }

        var request = new CashTransactionRequest
        {
            FundId = SelectedCashFund.Id,
            TransactionDate = QuickCashEntry.TransactionDate,
            Amount = QuickCashEntry.Amount,
            Description = BuildQuickDescription(QuickCashEntry.CounterpartyName, QuickCashEntry.Description),
            CounterAccountCode = IsCashReceipt() ? QuickCashCreditAccountCode : QuickCashDebitAccountCode,
            SourceType = SelectedCashEntryType == "Chuyển nội bộ" ? "InternalTransfer" : "CashQuickEntry",
            CreatedBy = CurrentUserName()
        };

        await RunWorkflowActionAsync("Nhập nhanh quỹ", async token =>
        {
            if (EditingCashTransactionId is Guid editingId)
            {
                await _cashFundService.UpdateTransactionAsync(editingId, request, token);
                EditingCashTransactionId = null;
            }
            else if (SelectedCashEntryType == "Thu tiền")
            {
                await _cashFundService.CreateReceiptAsync(request, token);
            }
            else
            {
                await _cashFundService.CreatePaymentAsync(request, token);
            }
        });
    }

    [RelayCommand]
    private async Task SaveQuickBankAsync()
    {
        if (SelectedBankAccount is null)
        {
            AddValidation("Error", "Ngân hàng", "Chọn tài khoản ngân hàng trước khi nhập nhanh.");
            return;
        }

        if (QuickBankEntry.Amount <= 0)
        {
            AddValidation("Error", "Ngân hàng", "Số tiền phải lớn hơn 0.");
            return;
        }

        var request = new BankTransactionRequest
        {
            BankAccountId = SelectedBankAccount.Id,
            TransactionDate = QuickBankEntry.TransactionDate,
            ValueDate = QuickBankEntry.ValueDate,
            Amount = QuickBankEntry.Amount,
            ReferenceNo = QuickBankEntry.ReferenceNo,
            Description = BuildQuickDescription(QuickBankEntry.CounterpartyName, QuickBankEntry.Description),
            CounterAccountCode = IsBankReceipt() ? QuickBankCreditAccountCode : QuickBankDebitAccountCode,
            SourceType = "BankQuickEntry",
            CreatedBy = CurrentUserName()
        };

        await RunWorkflowActionAsync("Nhập nhanh ngân hàng", async token =>
        {
            if (EditingBankTransactionId is Guid editingId)
            {
                await _bankingService.UpdateTransactionAsync(editingId, request, token);
                EditingBankTransactionId = null;
            }
            else if (SelectedBankEntryType == "Thu tiền")
            {
                await _bankingService.CreateReceiptAsync(request, token);
            }
            else
            {
                await _bankingService.CreatePaymentAsync(request, token);
            }
        });
    }

    [RelayCommand]
    private async Task ApproveCashAsync()
    {
        if (SelectedCashTransaction is null) return;
        if (Interlocked.CompareExchange(ref _runWorkflowDepth, 1, 0) != 0) return;
        var txId = SelectedCashTransaction.Id;
        var user = CurrentUserName();
        IsBusy = true;
        try
        {
            await Task.Run(() => _cashFundService.ApproveTransactionAsync(txId, user, CancellationToken.None));
            _notificationService.Show("Duyệt giao dịch quỹ hoàn tất.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Duyệt giao dịch quỹ", ex.Message);
        }
        finally { IsBusy = false; Interlocked.Exchange(ref _runWorkflowDepth, 0); }
    }

    [RelayCommand]
    private async Task PostCashAsync()
    {
        if (SelectedCashTransaction is null) return;
        if (Interlocked.CompareExchange(ref _runWorkflowDepth, 1, 0) != 0) return;
        var txId = SelectedCashTransaction.Id;
        var user = CurrentUserName();
        IsBusy = true;
        try
        {
            await Task.Run(() => _cashFundService.PostTransactionAsync(txId, user, CancellationToken.None));
            _notificationService.Show("Ghi sổ giao dịch quỹ hoàn tất.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Ghi sổ giao dịch quỹ", ex.Message);
        }
        finally { IsBusy = false; Interlocked.Exchange(ref _runWorkflowDepth, 0); }
    }

    [RelayCommand]
    private async Task CancelCashApprovalAsync()
    {
        if (SelectedCashTransaction is null)
        {
            AddValidation("Info", "Quỹ", "Chọn giao dịch quỹ trước khi hủy duyệt.");
            return;
        }

        if (!CanCancelCashApproval)
        {
            AddValidation("Info", "Quỹ", "Chỉ được hủy duyệt giao dịch quỹ ở trạng thái Approved.");
            return;
        }

        if (!_dialogService.Confirm("Hủy duyệt giao dịch quỹ đã chọn?", "Hủy duyệt quỹ"))
        {
            return;
        }

        var transactionId = SelectedCashTransaction.Id;
        await RunWorkflowActionAsync("Hủy duyệt giao dịch quỹ", token => _cashFundService.CancelApprovalAsync(transactionId, CurrentUserName(), token));
    }

    [RelayCommand]
    private void EditCashTransaction()
    {
        if (SelectedCashTransaction is null)
        {
            AddValidation("Info", "Quỹ", "Chọn giao dịch quỹ trước khi sửa.");
            return;
        }

        if (!CanEditCashTransaction)
        {
            AddValidation("Info", "Quỹ", "Chỉ được sửa giao dịch quỹ ở trạng thái Draft.");
            return;
        }

        SelectedCashEntryType = SelectedCashTransaction.TransactionType == "Receipt" ? "Thu tiền" : "Chi tiền";
        SelectedCashFund = CashFunds.FirstOrDefault(x => x.Id == SelectedCashTransaction.CashFundId) ?? SelectedCashFund;
        QuickCashEntry.Amount = Math.Abs(SelectedCashTransaction.DebitAmount != 0 ? SelectedCashTransaction.DebitAmount : SelectedCashTransaction.CreditAmount);
        QuickCashEntry.TransactionDate = SelectedCashTransaction.TransactionDate;
        QuickCashEntry.CounterAccountCode = SelectedCashTransaction.CounterAccountCode ?? string.Empty;
        (QuickCashEntry.CounterpartyName, QuickCashEntry.Description) = SplitQuickDescription(SelectedCashTransaction.Description);
        EditingCashTransactionId = SelectedCashTransaction.Id;
        OnCashAccountDisplayChanged();
        AddValidation("Info", "Quỹ", $"Đang sửa giao dịch {SelectedCashTransaction.TransactionNo}.");
    }

    [RelayCommand]
    private async Task DeleteCashTransactionAsync()
    {
        if (SelectedCashTransaction is null)
        {
            AddValidation("Info", "Quỹ", "Chọn giao dịch quỹ trước khi xóa.");
            return;
        }

        if (!CanDeleteCashTransaction)
        {
            AddValidation("Info", "Quỹ", "Chỉ được xóa giao dịch quỹ ở trạng thái Draft.");
            return;
        }

        var transactionId = SelectedCashTransaction.Id;
        var transactionNo = SelectedCashTransaction.TransactionNo;
        if (!_dialogService.Confirm($"Xóa giao dịch quỹ {transactionNo}?", "Xóa giao dịch quỹ"))
        {
            return;
        }

        await RunWorkflowActionAsync("Xóa giao dịch quỹ", token => _cashFundService.DeleteTransactionAsync(transactionId, CurrentUserName(), token));
        if (EditingCashTransactionId == transactionId)
        {
            EditingCashTransactionId = null;
        }
    }

    [RelayCommand]
    private async Task ReverseCashAsync()
    {
        if (SelectedCashTransaction is null)
        {
            return;
        }

        if (!_dialogService.Confirm("Đảo giao dịch quỹ đã chọn?", "Đảo giao dịch quỹ"))
        {
            return;
        }

        await RunWorkflowActionAsync("Đảo giao dịch quỹ", token => _cashFundService.ReverseTransactionAsync(SelectedCashTransaction.Id, "Reverse from Cash/Bank workflow", CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task BatchPostCashAsync()
    {
        var pending = CashTransactions.Where(x => x.Status is "Draft" or "Approved").Select(x => x.Id).ToList();
        if (pending.Count == 0)
        {
            AddValidation("Info", "Ghi sổ hàng loạt", "Không có giao dịch quỹ chờ ghi sổ.");
            return;
        }

        await RunWorkflowActionAsync("Ghi sổ quỹ hàng loạt", async token =>
        {
            foreach (var id in pending)
            {
                token.ThrowIfCancellationRequested();
                await _cashFundService.PostTransactionAsync(id, CurrentUserName(), token);
            }
        });
    }

    [RelayCommand]
    private async Task ApproveBankAsync()
    {
        if (SelectedBankTransaction is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Duyệt giao dịch ngân hàng", token => _bankingService.ApproveTransactionAsync(SelectedBankTransaction.Id, CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task CancelBankApprovalAsync()
    {
        if (SelectedBankTransaction is null)
        {
            AddValidation("Info", "Ngân hàng", "Chọn giao dịch ngân hàng trước khi hủy duyệt.");
            return;
        }

        if (!CanCancelBankApproval)
        {
            AddValidation("Info", "Ngân hàng", "Chỉ được hủy duyệt giao dịch ngân hàng ở trạng thái Approved.");
            return;
        }

        if (!_dialogService.Confirm("Hủy duyệt giao dịch ngân hàng đã chọn?", "Hủy duyệt ngân hàng"))
        {
            return;
        }

        var transactionId = SelectedBankTransaction.Id;
        await RunWorkflowActionAsync("Hủy duyệt giao dịch ngân hàng", token => _bankingService.CancelApprovalAsync(transactionId, CurrentUserName(), token));
    }

    [RelayCommand]
    private void EditBankTransaction()
    {
        if (SelectedBankTransaction is null)
        {
            AddValidation("Info", "Ngân hàng", "Chọn giao dịch ngân hàng trước khi sửa.");
            return;
        }

        if (!CanEditBankTransaction)
        {
            AddValidation("Info", "Ngân hàng", "Chỉ được sửa giao dịch ngân hàng ở trạng thái Draft.");
            return;
        }

        SelectedBankEntryType = SelectedBankTransaction.TransactionType == "Receipt" ? "Thu tiền" : "Chi tiền";
        SelectedBankAccount = BankAccounts.FirstOrDefault(x => x.Id == SelectedBankTransaction.BankAccountId) ?? SelectedBankAccount;
        QuickBankEntry.Amount = Math.Abs(SelectedBankTransaction.DebitAmount != 0 ? SelectedBankTransaction.DebitAmount : SelectedBankTransaction.CreditAmount);
        QuickBankEntry.TransactionDate = SelectedBankTransaction.TransactionDate;
        QuickBankEntry.ValueDate = SelectedBankTransaction.ValueDate;
        QuickBankEntry.ReferenceNo = SelectedBankTransaction.ReferenceNo ?? string.Empty;
        QuickBankEntry.CounterAccountCode = SelectedBankTransaction.CounterAccountCode ?? string.Empty;
        (QuickBankEntry.CounterpartyName, QuickBankEntry.Description) = SplitQuickDescription(SelectedBankTransaction.Description);
        EditingBankTransactionId = SelectedBankTransaction.Id;
        OnBankAccountDisplayChanged();
        AddValidation("Info", "Ngân hàng", $"Đang sửa giao dịch {SelectedBankTransaction.TransactionNo}.");
    }

    [RelayCommand]
    private async Task DeleteBankTransactionAsync()
    {
        if (SelectedBankTransaction is null)
        {
            AddValidation("Info", "Ngân hàng", "Chọn giao dịch ngân hàng trước khi xóa.");
            return;
        }

        if (!CanDeleteBankTransaction)
        {
            AddValidation("Info", "Ngân hàng", "Chỉ được xóa giao dịch ngân hàng ở trạng thái Draft.");
            return;
        }

        var transactionId = SelectedBankTransaction.Id;
        var transactionNo = SelectedBankTransaction.TransactionNo;
        if (!_dialogService.Confirm($"Xóa giao dịch ngân hàng {transactionNo}?", "Xóa giao dịch ngân hàng"))
        {
            return;
        }

        await RunWorkflowActionAsync("Xóa giao dịch ngân hàng", token => _bankingService.DeleteTransactionAsync(transactionId, CurrentUserName(), token));
        if (EditingBankTransactionId == transactionId)
        {
            EditingBankTransactionId = null;
        }
    }

    [RelayCommand]
    private async Task PostBankAsync()
    {
        if (SelectedBankTransaction is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Ghi sổ giao dịch ngân hàng", token => _bankingService.PostTransactionAsync(SelectedBankTransaction.Id, CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task ReverseBankAsync()
    {
        if (SelectedBankTransaction is null)
        {
            return;
        }

        if (!_dialogService.Confirm("Đảo giao dịch ngân hàng đã chọn?", "Đảo giao dịch ngân hàng"))
        {
            return;
        }

        await RunWorkflowActionAsync("Đảo giao dịch ngân hàng", token => _bankingService.ReverseTransactionAsync(SelectedBankTransaction.Id, "Reverse from Cash/Bank workflow", CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task BatchPostBankAsync()
    {
        var pending = BankTransactions.Where(x => x.Status is "Draft" or "Approved").Select(x => x.Id).ToList();
        if (pending.Count == 0)
        {
            AddValidation("Info", "Ghi sổ hàng loạt", "Không có giao dịch ngân hàng chờ ghi sổ.");
            return;
        }

        await RunWorkflowActionAsync("Ghi sổ ngân hàng hàng loạt", async token =>
        {
            foreach (var id in pending)
            {
                token.ThrowIfCancellationRequested();
                await _bankingService.PostTransactionAsync(id, CurrentUserName(), token);
            }
        });
    }

    [RelayCommand]
    private async Task ImportStatementAsync()
    {
        if (SelectedBankAccount is null)
        {
            AddValidation("Error", "Nhập sao kê", "Chọn tài khoản ngân hàng trước khi nhập sao kê.");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Chọn file sao kê ngân hàng",
            Filter = "File sao kê (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Tất cả tệp (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _importCts?.Cancel();
        _importCts = new CancellationTokenSource();
        var token = _importCts.Token;
        IsImportLoading = true;
        ImportPreviewRows.Clear();
        ImportWarningSummary = string.Empty;
        CurrentImportSessionId = null;
        OnPropertyChanged(nameof(CanCommitImport));

        try
        {
            var preparedFile = PrepareStatementImportFile(dialog.FileName, SelectedBankAccount.Id);
            var result = await _importEngine.PreviewAsync(new ImportPreviewRequest
            {
                TemplateCode = "BANK_STATEMENT",
                TargetEntity = "BankStatement",
                FilePath = preparedFile,
                CreatedBy = CurrentUserName()
            }, token);

            LoadImportResult(result, "Xem trước");
            if (result.IsValid)
            {
                _notificationService.Show("Dữ liệu xem trước hợp lệ. Kiểm tra trước khi ghi nhận.");
            }
        }
        catch (OperationCanceledException)
        {
            ImportStatusText = "Đã hủy nhập sao kê.";
        }
        catch (Exception ex)
        {
            ImportStatusText = "Không xem trước được file sao kê.";
            ImportWarningSummary = ex.Message;
        }
        finally
        {
            IsImportLoading = false;
        }
    }

    [RelayCommand]
    private async Task ValidateImportAsync()
    {
        if (!CurrentImportSessionId.HasValue)
        {
            AddValidation("Info", "Nhập sao kê", "Chưa có phiên nhập để kiểm tra.");
            return;
        }

        IsImportLoading = true;
        try
        {
            var result = await _importEngine.ValidateAsync(CurrentImportSessionId.Value);
            LoadImportResult(result, "Kiểm tra");
        }
        catch (Exception ex)
        {
            ImportWarningSummary = ex.Message;
        }
        finally
        {
            IsImportLoading = false;
        }
    }

    [RelayCommand]
    private async Task CommitImportAsync()
    {
        if (!CurrentImportSessionId.HasValue)
        {
            AddValidation("Info", "Nhập sao kê", "Chưa có phiên nhập để ghi nhận.");
            return;
        }

        if (!_dialogService.Confirm("Ghi nhận sao kê đã kiểm tra vào cơ sở dữ liệu?", "Ghi nhận sao kê"))
        {
            return;
        }

        IsImportLoading = true;
        try
        {
            var result = await _importEngine.CommitAsync(CurrentImportSessionId.Value, allowWarnings: true);
            ImportStatusText = result.Message;
            ImportWarningSummary = $"Đã ghi nhận {result.CommittedRows:N0} dòng. Lỗi {result.ErrorCount:N0}, cảnh báo {result.WarningCount:N0}.";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ImportWarningSummary = ex.Message;
        }
        finally
        {
            IsImportLoading = false;
        }
    }

    [RelayCommand]
    private async Task DownloadImportErrorsAsync()
    {
        if (!CurrentImportSessionId.HasValue)
        {
            AddValidation("Info", "Nhập sao kê", "Chưa có phiên nhập để xuất file lỗi.");
            return;
        }

        try
        {
            var bytes = await _importEngine.ExportErrorsAsync(CurrentImportSessionId.Value);
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ASMILE BO", "Imports");
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, $"bank_statement_errors_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            await File.WriteAllBytesAsync(path, bytes);
            _dialogService.Info($"Đã xuất file lỗi nhập sao kê: {path}", "Lỗi nhập");
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Import", ex.Message);
        }
    }

    [RelayCommand]
    private async Task RunReconciliationAsync()
    {
        if (SelectedBankAccount is null || SelectedStatement is null)
        {
            AddValidation("Error", "Đối chiếu", "Chọn tài khoản và sao kê trước khi đối chiếu.");
            return;
        }

        await RunWorkflowActionAsync("Chạy đối chiếu", token => _bankingService.ReconcileAsync(SelectedBankAccount.Id, SelectedStatement.Id, DateTime.Today, CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task ConfirmReconciliationAsync()
    {
        if (SelectedReconciliation is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Xác nhận đối chiếu", token => _bankingService.ConfirmReconciliationAsync(SelectedReconciliation.Id, CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task RunReportAsync()
    {
        _reportCts?.Cancel();
        _reportCts = new CancellationTokenSource();
        var token = _reportCts.Token;
        IsReportLoading = true;
        ReportRows.Clear();

        try
        {
            var result = await _reportEngine.ExecuteAsync(BuildReportRequest(), token);
            ReportRows.ResetWith(result.Rows.Select(x => new CashBankReportRow(result.Columns, x)).Take(1000));
            ReportRowsPager.SetSource(ReportRows);
            await RefreshReportHistoryAsync(SelectedReportCode, token);
            StatusText = $"Báo cáo {result.ReportName}: {result.RowCount:N0} dòng.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Đã hủy báo cáo.";
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Báo cáo", ex.Message);
        }
        finally
        {
            IsReportLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        _reportCts?.Cancel();
        _reportCts = new CancellationTokenSource();
        var token = _reportCts.Token;
        IsReportLoading = true;

        try
        {
            var request = BuildReportRequest();
            var format = SelectedExportFormat.ToLowerInvariant();
            var bytes = format switch
            {
                "excel" => await _reportEngine.ExportExcelAsync(request, token),
                "pdf" => await _reportEngine.ExportPdfAsync(request, token),
                "csv" => await _reportEngine.ExportCsvAsync(request, token),
                _ => await _reportEngine.ExportExcelAsync(request, token)
            };
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ASMILE BO", "Reports");
            Directory.CreateDirectory(folder);
            var extension = format == "excel" ? "xlsx" : format;
            var path = Path.Combine(folder, $"{SelectedReportCode}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");
            await File.WriteAllBytesAsync(path, bytes, token);
            _notificationService.Show($"Đã xuất báo cáo {Path.GetFileName(path)}.");
            await RefreshReportHistoryAsync(SelectedReportCode, token);
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Báo cáo", ex.Message);
        }
        finally
        {
            IsReportLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleFavoriteReport()
    {
        var option = ReportCodes.FirstOrDefault(x => x.Code == SelectedReportCode);
        if (option is null)
        {
            return;
        }

        var existing = FavoriteReports.FirstOrDefault(x => x.Code == SelectedReportCode);
        if (existing is not null)
        {
            FavoriteReports.Remove(existing);
        }
        else
        {
            FavoriteReports.Add(option);
        }
    }

    [RelayCommand]
    private void ToggleSummary()
    {
        IsSummaryExpanded = !IsSummaryExpanded;
        if (IsSummaryExpanded)
        {
            IsFilterExpanded = false;
        }
    }

    [RelayCommand]
    private void ToggleFilter()
    {
        IsFilterExpanded = !IsFilterExpanded;
        if (IsFilterExpanded)
        {
            IsSummaryExpanded = false;
        }
    }

    [RelayCommand]
    private void OpenSelectedDrilldown()
    {
        if (SelectedCashTransaction is not null && SelectedTabIndex == 1)
        {
            DrilldownText = BuildCashDrilldown(SelectedCashTransaction);
            return;
        }

        if (SelectedBankTransaction is not null && SelectedTabIndex == 2)
        {
            DrilldownText = BuildBankDrilldown(SelectedBankTransaction);
            return;
        }

        if (SelectedReconciliation is not null)
        {
            DrilldownText = $"Đối chiếu -> giao dịch đã khớp: {SelectedReconciliation.ReconciliationNo}, trạng thái {SelectedReconciliation.Status}, chênh lệch {SelectedReconciliation.Difference:N0}.";
        }
    }

    public void PasteCashRows(string clipboardText)
    {
        var rows = ParseClipboardRows(clipboardText).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var first = rows[0];
        if (first.Count > 0 && decimal.TryParse(first[0], NumberStyles.Any, CultureInfo.CurrentCulture, out var amount))
        {
            QuickCashEntry.Amount = amount;
        }
        if (first.Count > 1)
        {
            QuickCashEntry.CounterAccountCode = first[1];
            OnCashAccountDisplayChanged();
        }
        if (first.Count > 2)
        {
            QuickCashEntry.Description = first[2];
        }
        AddValidation("Info", "Dán dữ liệu", $"Đã đọc {rows.Count:N0} dòng Excel vào nhập nhanh quỹ.");
    }

    public void PasteBankRows(string clipboardText)
    {
        var rows = ParseClipboardRows(clipboardText).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var first = rows[0];
        if (first.Count > 0 && decimal.TryParse(first[0], NumberStyles.Any, CultureInfo.CurrentCulture, out var amount))
        {
            QuickBankEntry.Amount = amount;
        }
        if (first.Count > 1)
        {
            QuickBankEntry.ReferenceNo = first[1];
        }
        if (first.Count > 2)
        {
            QuickBankEntry.Description = first[2];
        }
        OnBankAccountDisplayChanged();
        AddValidation("Info", "Dán dữ liệu", $"Đã đọc {rows.Count:N0} dòng Excel vào nhập nhanh ngân hàng.");
    }

    private bool IsCashReceipt() => IsReceiptEntry(SelectedCashEntryType);

    private bool IsBankReceipt() => IsReceiptEntry(SelectedBankEntryType);

    private static bool IsReceiptEntry(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.TrimStart().StartsWith("Thu", StringComparison.OrdinalIgnoreCase);

    private static bool IsApprovedStatus(string? status)
        => string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);

    private static bool IsDraftStatus(string? status)
        => string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase);

    private static string BuildQuickDescription(string? counterpartyName, string? description)
    {
        var counterparty = counterpartyName?.Trim();
        var content = description?.Trim();
        if (string.IsNullOrWhiteSpace(counterparty))
        {
            return content ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Người nộp/nhận: {counterparty}";
        }

        return $"Người nộp/nhận: {counterparty} - {content}";
    }

    private static (string CounterpartyName, string Description) SplitQuickDescription(string? value)
    {
        const string prefix = "Người nộp/nhận: ";
        var text = value?.Trim() ?? string.Empty;
        if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return (string.Empty, text);
        }

        var rest = text[prefix.Length..];
        var separatorIndex = rest.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return (rest.Trim(), string.Empty);
        }

        return (rest[..separatorIndex].Trim(), rest[(separatorIndex + 3)..].Trim());
    }

    private void OnCashAccountDisplayChanged()
    {
        OnPropertyChanged(nameof(CashLedgerAccountCode));
        OnPropertyChanged(nameof(QuickCashDebitAccountCode));
        OnPropertyChanged(nameof(QuickCashCreditAccountCode));
    }

    private void OnBankAccountDisplayChanged()
    {
        OnPropertyChanged(nameof(BankLedgerAccountCode));
        OnPropertyChanged(nameof(QuickBankDebitAccountCode));
        OnPropertyChanged(nameof(QuickBankCreditAccountCode));
    }

    private void QueueFilterRefresh()
    {
        if (_suppressFilterRefresh)
        {
            return;
        }

        _filterDebounceCts?.Cancel();
        _filterDebounceCts = new CancellationTokenSource();
        var token = _filterDebounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, token);
                await App.Current.Dispatcher.InvokeAsync(async () => await RefreshAsync());
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private int _runWorkflowDepth;

    private async Task RunWorkflowActionAsync(string scope, Func<CancellationToken, Task> action)
    {
        if (Interlocked.CompareExchange(ref _runWorkflowDepth, 1, 0) != 0)
        {
            AddValidation("Warning", scope, "Đang có thao tác đang chạy, vui lòng chờ.");
            return;
        }

        IsBusy = true;
        try
        {
            // Run on threadpool to avoid WPF stack overflow
            await Task.Run(() => action(CancellationToken.None));
            _notificationService.Show($"{scope} hoàn tất.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddValidation("Error", scope, ex.Message);
        }
        finally
        {
            IsBusy = false;
            Interlocked.Exchange(ref _runWorkflowDepth, 0);
        }
    }

    private async Task RunWorkflowActionAsync<T>(string scope, Func<CancellationToken, Task<T>> action)
    {
        await RunWorkflowActionAsync(scope, async token => _ = await action(token));
    }

    private void RecalculateBalances()
    {
        CurrentFundBalance = SelectedCashFund?.CurrentBalance ?? 0m;
        DailyInflow = CashTransactions.Where(x => x.TransactionDate.Date == DateTime.Today).Sum(x => x.DebitAmount);
        DailyOutflow = CashTransactions.Where(x => x.TransactionDate.Date == DateTime.Today).Sum(x => x.CreditAmount);
        PendingCashTransactions = CashTransactions.Count(x => x.Status is "Draft" or "Approved");
        PendingCashAmount = CashTransactions.Where(x => x.Status is "Draft" or "Approved").Sum(x => x.DebitAmount - x.CreditAmount);
        BankCurrentBalance = SelectedBankAccount?.CurrentBalance ?? 0m;
        BankPendingAmount = BankTransactions.Where(x => x.Status is "Draft" or "Approved").Sum(x => x.DebitAmount - x.CreditAmount);
        BankAvailableBalance = BankCurrentBalance + BankPendingAmount;
        LastReconciliationText = Reconciliations.OrderByDescending(x => x.ReconciliationDate).FirstOrDefault() is { } last
            ? $"{last.ReconciliationDate:dd/MM/yyyy} - {last.Status}"
            : "Chưa có đối chiếu";
        RecalculateReconciliationTotals();

        OnPropertyChanged(nameof(CashBalanceText));
        OnPropertyChanged(nameof(BankBalanceText));
        OnPropertyChanged(nameof(PendingText));
    }

    private void RecalculateReconciliationTotals()
    {
        MatchedAmount = ReconciliationLines.Where(x => x.MatchStatus == "Matched").Sum(x => Math.Abs(x.DebitAmount - x.CreditAmount));
        UnmatchedAmount = ReconciliationLines.Where(x => x.MatchStatus != "Matched").Sum(x => Math.Abs(x.DebitAmount - x.CreditAmount));
        ReconciliationDifference = SelectedReconciliation?.Difference ?? ReconciliationLines.Sum(x => x.DifferenceAmount);
    }

    private void BuildInlineValidation()
    {
        ValidationMessages.Clear();
        if (CashTransactions.GroupBy(x => x.TransactionNo).Any(x => x.Count() > 1))
        {
            AddValidation("Warning", "Quỹ", "Có số giao dịch quỹ bị trùng trong dữ liệu lọc.");
        }
        if (BankTransactions.GroupBy(x => x.TransactionNo).Any(x => x.Count() > 1))
        {
            AddValidation("Warning", "Ngân hàng", "Có số giao dịch ngân hàng bị trùng trong dữ liệu lọc.");
        }
        if (SelectedCashFund is { IsActive: false })
        {
            AddValidation("Error", "Quỹ", "Quỹ đang tạm tắt.");
        }
        if (SelectedBankAccount is { IsActive: false })
        {
            AddValidation("Error", "Ngân hàng", "Tài khoản ngân hàng đang tạm tắt.");
        }
        if (SelectedCashFund is { AccountCode: null or "" })
        {
            AddValidation("Warning", "Quỹ", "Quỹ chưa có tài khoản kế toán mặc định.");
        }
        if (SelectedBankAccount is { AccountCode: null or "" })
        {
            AddValidation("Warning", "Ngân hàng", "Tài khoản ngân hàng chưa có tài khoản kế toán mặc định.");
        }
        if (CashTransactions.Any(x => x.CreditAmount > 0 && x.Status is "Draft" or "Approved" && SelectedCashFund is not null && x.CreditAmount > SelectedCashFund.CurrentBalance))
        {
            AddValidation("Warning", "Quỹ", "Có giao dịch chi có khả năng làm âm quỹ.");
        }
        if (BankTransactions.Any(x => x.CreditAmount > 0 && x.Status is "Draft" or "Approved" && SelectedBankAccount is not null && x.CreditAmount > SelectedBankAccount.CurrentBalance))
        {
            AddValidation("Warning", "Ngân hàng", "Có giao dịch chi có khả năng vượt số dư ngân hàng.");
        }
    }

    private async Task LoadReconciliationLinesAsync(Guid reconciliationId)
    {
        try
        {
            var lines = await _bankingService.GetReconciliationLinesAsync(reconciliationId);
            ReconciliationLines.ResetWith(lines);
            ReconciliationLinesPager.SetSource(ReconciliationLines);
            RecalculateReconciliationTotals();
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Đối chiếu", ex.Message);
        }
    }

    private void LoadImportResult(ImportPreviewResult result, string action)
    {
        CurrentImportSessionId = result.ImportSessionId;
        ImportPreviewRows.ResetWith(result.Rows.Take(1000));
        ImportPreviewPager.SetSource(ImportPreviewRows);
        ImportStatusText = $"{action}: {result.TotalRows:N0} dòng, hợp lệ {result.ValidRows:N0}, cảnh báo {result.WarningRows:N0}, lỗi {result.ErrorRows:N0}.";
        ImportWarningSummary = BuildImportSummary(result);
        OnPropertyChanged(nameof(CanCommitImport));
    }

    private static string BuildImportSummary(ImportPreviewResult result)
    {
        var messages = result.Messages.Concat(result.Rows.SelectMany(x => x.Errors.Select(e => $"Dòng {x.RowNumber}: {e.Severity} {e.ErrorCode} - {e.Message}"))).Take(10);
        return string.Join(Environment.NewLine, messages);
    }

    private async Task RefreshReportHistoryAsync(string? reportCode, CancellationToken cancellationToken)
    {
        var history = await _reportEngine.GetExecutionHistoryAsync(reportCode, 20, cancellationToken);
        ReportHistory.ResetWith(history);
        ReportHistoryPager.SetSource(ReportHistory);
    }

    private ReportRequestDto BuildReportRequest()
    {
        var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["fromDate"] = (FromDate ?? DateTime.Today.AddMonths(-1)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["toDate"] = (ToDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
        if (SelectedCashFund is not null)
        {
            parameters["cashFundId"] = SelectedCashFund.Id.ToString();
        }
        if (SelectedBankAccount is not null)
        {
            parameters["bankAccountId"] = SelectedBankAccount.Id.ToString();
        }

        return new ReportRequestDto
        {
            ReportCode = SelectedReportCode,
            ExecutedBy = CurrentUserName(),
            Parameters = parameters
        };
    }

    private string PrepareStatementImportFile(string sourcePath, Guid bankAccountId)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var root = Path.Combine(Path.GetTempPath(), "ASMILE-BO", "statement-import");
        Directory.CreateDirectory(root);
        var target = Path.Combine(root, $"bank-statement-{Guid.NewGuid():N}.xlsx");

        if (extension == ".csv")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Statement");
            var lines = File.ReadAllLines(sourcePath, Encoding.UTF8);
            for (var row = 0; row < lines.Length; row++)
            {
                var cells = SplitCsvLine(lines[row]).ToList();
                for (var col = 0; col < cells.Count; col++)
                {
                    worksheet.Cell(row + 1, col + 1).Value = cells[col];
                }
            }

            AddBankAccountColumn(worksheet, bankAccountId);
            workbook.SaveAs(target);
            return target;
        }

        using (var workbook = new XLWorkbook(sourcePath))
        {
            var worksheet = workbook.Worksheets.First();
            AddBankAccountColumn(worksheet, bankAccountId);
            workbook.SaveAs(target);
        }

        return target;
    }

    private static void AddBankAccountColumn(IXLWorksheet worksheet, Guid bankAccountId)
    {
        var headerRow = 1;
        var lastColumn = worksheet.Row(headerRow).LastCellUsed()?.Address.ColumnNumber ?? 0;
        var accountColumn = Enumerable.Range(1, Math.Max(1, lastColumn))
            .FirstOrDefault(col => string.Equals(worksheet.Cell(headerRow, col).GetString().Trim(), "BankAccountId", StringComparison.OrdinalIgnoreCase));
        if (accountColumn == 0)
        {
            accountColumn = lastColumn + 1;
            worksheet.Cell(headerRow, accountColumn).Value = "BankAccountId";
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            worksheet.Cell(row, accountColumn).Value = bankAccountId.ToString();
        }
    }

    private static IEnumerable<string> SplitCsvLine(string line)
    {
        var builder = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                yield return builder.ToString();
                builder.Clear();
                continue;
            }

            builder.Append(ch);
        }

        yield return builder.ToString();
    }

    private static IEnumerable<IReadOnlyList<string>> ParseClipboardRows(string clipboardText)
    {
        return clipboardText
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => (IReadOnlyList<string>)line.Split('\t').Select(x => x.Trim()).ToList());
    }

    private static string BuildCashDrilldown(CashTransactionDto value)
        => $"Sổ quỹ -> bút toán {FormatGuid(value.JournalHeaderId)} | nguồn {value.SourceType ?? "CashTransaction"} {FormatGuid(value.SourceId)} | chứng từ {value.TransactionNo} | ghi sổ {value.PostedAt:dd/MM/yyyy HH:mm} | trạng thái {value.Status}.";

    private static string BuildBankDrilldown(BankTransactionDto value)
        => $"Sổ ngân hàng -> bút toán {FormatGuid(value.JournalHeaderId)} | nguồn {value.SourceType ?? "BankTransaction"} {FormatGuid(value.SourceId)} | tham chiếu {value.ReferenceNo ?? value.TransactionNo} | ghi sổ {value.PostedAt:dd/MM/yyyy HH:mm} | trạng thái {value.Status}.";

    private static string FormatGuid(Guid? id) => id?.ToString() ?? "chưa có";

    private void AddValidation(string severity, string scope, string message)
    {
        ValidationMessages.Insert(0, new CashBankValidationMessage(LocalizeSeverity(severity), LocalizeScope(scope), message));
        while (ValidationMessages.Count > 20)
        {
            ValidationMessages.RemoveAt(ValidationMessages.Count - 1);
        }
    }

    private static string LocalizeSeverity(string severity)
        => severity switch
        {
            "Error" => "Lỗi",
            "Warning" => "Cảnh báo",
            "Info" => "Thông tin",
            _ => severity
        };

    private static string LocalizeScope(string scope)
        => scope switch
        {
            "Refresh" => "Làm mới",
            "Cash" => "Quỹ",
            "Bank" => "Ngân hàng",
            "Import" => "Nhập sao kê",
            "Reconciliation" => "Đối chiếu",
            "Report" => "Báo cáo",
            "Paste" => "Dán dữ liệu",
            "Batch posting" => "Ghi sổ hàng loạt",
            _ => scope
        };

    private string CurrentUserName()
        => _currentUserService.CurrentUser?.DisplayName ?? "cash-bank-ui";
}

public sealed partial class QuickCashEntry : ObservableObject
{
    [ObservableProperty] private DateTime transactionDate = DateTime.Today;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private string counterAccountCode = string.Empty;
    [ObservableProperty] private string counterpartyName = string.Empty;
    [ObservableProperty] private string description = string.Empty;
}

public sealed partial class QuickBankEntry : ObservableObject
{
    [ObservableProperty] private DateTime transactionDate = DateTime.Today;
    [ObservableProperty] private DateTime? valueDate = DateTime.Today;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private string referenceNo = string.Empty;
    [ObservableProperty] private string counterAccountCode = string.Empty;
    [ObservableProperty] private string counterpartyName = string.Empty;
    [ObservableProperty] private string description = string.Empty;
}

public sealed record CashBankValidationMessage(string Severity, string Scope, string Message);

public sealed class CashBankReportRow
{
    public CashBankReportRow(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row)
    {
        Date = Read(columns, row, "Date");
        DocumentNo = ReadAny(columns, row, "TransactionNo", "ReconciliationNo", "Code");
        Account = ReadAny(columns, row, "Account", "Name", "SourceType");
        Description = ReadAny(columns, row, "Description", "Status", "Currency");
        Debit = ReadAny(columns, row, "Debit", "Inflow", "CurrentBalance", "BookBalance");
        Credit = ReadAny(columns, row, "Credit", "Outflow", "AvailableBalance", "StatementBalance");
        Balance = ReadAny(columns, row, "Balance", "NetFlow", "Difference");
        Status = Read(columns, row, "Status");
    }

    public string Date { get; }
    public string DocumentNo { get; }
    public string Account { get; }
    public string Description { get; }
    public string Debit { get; }
    public string Credit { get; }
    public string Balance { get; }
    public string Status { get; }

    private static string ReadAny(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row, params string[] names)
    {
        foreach (var name in names)
        {
            var value = Read(columns, row, name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string Read(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row, string name)
    {
        var column = columns.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (column is null || !row.Values.TryGetValue(column.Name, out var value) || value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            DateTime date => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            decimal dec => dec.ToString("N0", CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("N2", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }
}

file static class CashBankCollectionExtensions
{
    public static void ResetWith<T>(this ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values)
        {
            collection.Add(value);
        }
    }
}
