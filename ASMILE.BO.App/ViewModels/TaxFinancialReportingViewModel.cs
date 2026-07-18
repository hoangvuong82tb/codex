using ASMILE.BO.App.Services;
using ASMILE.BO.App.Models;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class TaxFinancialReportingViewModel : ViewModelBase
{
    private readonly ITaxService _taxService;
    private readonly IFinancialStatementService _financialStatementService;
    private readonly IClosingService _closingService;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly IGeneralLedgerService _generalLedgerService;
    private readonly IReportEngine _reportEngine;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _filterDebounceCts;
    private CancellationTokenSource? _reportCts;

    public ObservableCollection<TaxCodeDto> TaxCodes { get; } = new();
    public ObservableCollection<VatTransactionDto> VatTransactions { get; } = new();
    public ObservableCollection<TaxDeclarationDraftDto> Declarations { get; } = new();
    public ObservableCollection<StatementLineDto> StatementLines { get; } = new();
    public ObservableCollection<AccountBalanceSnapshotDto> SnapshotBalances { get; } = new();
    public ObservableCollection<LedgerEntryDto> JournalDrilldownRows { get; } = new();
    public ObservableCollection<AccountingPeriod> AccountingPeriods { get; } = new();
    public ObservableCollection<ClosingEntryDto> ClosingEntries { get; } = new();
    public ObservableCollection<ClosingEntryLineDto> ClosingLines { get; } = new();
    public ObservableCollection<TaxFinancialValidationMessage> ValidationMessages { get; } = new();
    public ObservableCollection<TaxFinancialReportRow> ReportRows { get; } = new();
    public ObservableCollection<ReportExecutionDto> ReportHistory { get; } = new();
    private static readonly TaxVatDirectionOption AllVatDirection = new("Tất cả", null);
    private static readonly TaxVatDirectionOption InputVatDirection = new("Đầu vào", "Input");
    private static readonly TaxVatDirectionOption OutputVatDirection = new("Đầu ra", "Output");

    public ObservableCollection<TaxVatDirectionOption> VatDirections { get; } = new()
    {
        AllVatDirection,
        InputVatDirection,
        OutputVatDirection
    };

    public ObservableCollection<TaxVatDirectionOption> VatEntryDirections { get; } = new()
    {
        InputVatDirection,
        OutputVatDirection
    };
    public ObservableCollection<EnterpriseReportOption> ReportCodes { get; } = new()
    {
        new("BALANCE_SHEET", "Bảng cân đối kế toán"),
        new("INCOME_STATEMENT", "Kết quả kinh doanh"),
        new("CASH_FLOW", "Lưu chuyển tiền tệ"),
        new("VAT_INPUT", "Bảng kê VAT đầu vào"),
        new("VAT_OUTPUT", "Bảng kê VAT đầu ra"),
        new("GENERAL_LEDGER", "Sổ cái"),
        new("TRIAL_BALANCE", "Bảng cân đối phát sinh")
    };
    public ObservableCollection<string> ExportFormats { get; } = new() { "Excel", "PDF", "CSV" };
    public ObservableCollection<EnterpriseReportOption> FavoriteReports { get; } = new();

    [ObservableProperty] private TaxCodeDto? selectedTaxCode;
    [ObservableProperty] private VatTransactionDto? selectedVatTransaction;
    [ObservableProperty] private TaxDeclarationDraftDto? selectedDeclaration;
    [ObservableProperty] private StatementLineDto? selectedStatementLine;
    [ObservableProperty] private AccountBalanceSnapshotDto? selectedSnapshotBalance;
    [ObservableProperty] private LedgerEntryDto? selectedJournalDrilldown;
    [ObservableProperty] private AccountingPeriod? selectedAccountingPeriod;
    [ObservableProperty] private ClosingEntryDto? selectedClosingEntry;
    [ObservableProperty] private ClosingEntryLineDto? selectedClosingLine;
    [ObservableProperty] private DateTime? fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? toDate = DateTime.Today;
    [ObservableProperty] private string periodCode = DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private TaxVatDirectionOption selectedVatDirection = AllVatDirection;
    [ObservableProperty] private string selectedReportCode = "BALANCE_SHEET";
    [ObservableProperty] private string selectedExportFormat = "Excel";
    [ObservableProperty] private int selectedTabIndex;
    [ObservableProperty] private bool isReportLoading;
    [ObservableProperty] private decimal inputVatAmount;
    [ObservableProperty] private decimal outputVatAmount;
    [ObservableProperty] private decimal vatPayable;
    [ObservableProperty] private decimal vatReceivable;
    [ObservableProperty] private decimal totalAssets;
    [ObservableProperty] private decimal totalLiabilitiesAndEquity;
    [ObservableProperty] private decimal profitBeforeTax;
    [ObservableProperty] private bool isBalanced;
    [ObservableProperty] private string closingStateText = "Chưa tải trạng thái khóa kỳ.";
    [ObservableProperty] private string postingVisibilityText = "Chọn dòng VAT/BCTC/kết chuyển để xem bút toán, nguồn và trạng thái khóa kỳ.";
    [ObservableProperty] private string statusText = "Quy trình thuế và báo cáo tài chính dùng dữ liệu đã lưu.";
    [ObservableProperty] private QuickVatEntry quickVat = new();
    [ObservableProperty] private DateTime lastRefreshAt = DateTime.Now;

    public string LastRefreshText => $"Cập nhật {LastRefreshAt:HH:mm:ss}";
    public string VatPayableText => $"{VatPayable:N0}";
    public string VatReceivableText => $"{VatReceivable:N0}";
    public string BalanceStatusText => IsBalanced ? "Cân đối" : "Lệch";

    public TaxFinancialReportingViewModel(
        ITaxService taxService,
        IFinancialStatementService financialStatementService,
        IClosingService closingService,
        IAccountingPeriodService accountingPeriodService,
        IGeneralLedgerService generalLedgerService,
        IReportEngine reportEngine,
        INotificationService notificationService,
        IDialogService dialogService,
        ICurrentUserService currentUserService)
    {
        _taxService = taxService;
        _financialStatementService = financialStatementService;
        _closingService = closingService;
        _accountingPeriodService = accountingPeriodService;
        _generalLedgerService = generalLedgerService;
        _reportEngine = reportEngine;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _currentUserService = currentUserService;
        FavoriteReports.ResetWith(ReportCodes.Where(x => x.Code is "BALANCE_SHEET" or "INCOME_STATEMENT" or "VAT_OUTPUT"));
        _ = RefreshAsync();
    }

    partial void OnKeywordChanged(string value) => QueueFilterRefresh();
    partial void OnFromDateChanged(DateTime? value) => QueueFilterRefresh();
    partial void OnToDateChanged(DateTime? value) => QueueFilterRefresh();
    partial void OnPeriodCodeChanged(string value) => QueueFilterRefresh();
    partial void OnSelectedVatDirectionChanged(TaxVatDirectionOption value) => QueueFilterRefresh();
    partial void OnSelectedReportCodeChanged(string value) => _ = RefreshReportHistoryAsync(value, CancellationToken.None);

    partial void OnSelectedVatTransactionChanged(VatTransactionDto? value)
    {
        if (value is null)
        {
            return;
        }

        PostingVisibilityText = $"VAT -> bút toán {FormatGuid(value.JournalHeaderId)} | nguồn {value.SourceType} {FormatGuid(value.SourceId)} | hóa đơn {value.InvoiceNo ?? "chưa có"} | trạng thái {value.Status}.";
    }

    partial void OnSelectedDeclarationChanged(TaxDeclarationDraftDto? value)
    {
        if (value is null)
        {
            return;
        }

        PostingVisibilityText = $"Tờ khai {value.TaxType} kỳ {value.DeclarationPeriod} | trạng thái {value.Status} | phải nộp {value.NetVatPayable:N0}.";
    }

    partial void OnSelectedStatementLineChanged(StatementLineDto? value)
    {
        if (value is null)
        {
            return;
        }

        PostingVisibilityText = $"Chỉ tiêu BCTC -> số dư tài khoản {value.LineCode} | số tiền {value.Amount:N0} | thuyết minh {value.Note ?? "chưa có"}.";
        _ = LoadJournalDrilldownAsync(value.LineCode);
    }

    partial void OnSelectedSnapshotBalanceChanged(AccountBalanceSnapshotDto? value)
    {
        if (value is null)
        {
            return;
        }

        PostingVisibilityText = $"Tài khoản -> bút toán {value.AccountCode} | cuối kỳ Nợ/Có {value.ClosingDebit:N0}/{value.ClosingCredit:N0}.";
        _ = LoadJournalDrilldownAsync(value.AccountCode);
    }

    partial void OnSelectedClosingEntryChanged(ClosingEntryDto? value)
    {
        ClosingLines.ResetWith(value?.Lines ?? Array.Empty<ClosingEntryLineDto>());
        if (value is not null)
        {
            PostingVisibilityText = $"Kết chuyển kỳ {value.Period} -> bút toán {FormatGuid(value.JournalHeaderId)} | trạng thái {value.Status} | {value.Lines.Count:N0} dòng.";
        }
    }

    partial void OnSelectedClosingLineChanged(ClosingEntryLineDto? value)
    {
        if (value is not null)
        {
            _ = LoadJournalDrilldownAsync(value.AccountCode);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;
        await _operationGate.WaitAsync(token);
        IsBusy = true;
        try
        {
            await LoadCoreAsync(token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Làm mới", ex.Message);
            StatusText = "Không tải được quy trình thuế và báo cáo tài chính.";
        }
        finally
        {
            IsBusy = false;
            _operationGate.Release();
        }
    }

    [RelayCommand]
    private async Task CreateVatTransactionAsync()
    {
        if (SelectedTaxCode is null)
        {
            AddValidation("Error", "VAT", "Chọn mã thuế trước khi tạo giao dịch VAT.");
            return;
        }

        await RunWorkflowActionAsync("Tạo giao dịch VAT", token => _taxService.CreateVatTransactionAsync(new CreateVatTransactionRequest
        {
            TaxCodeId = SelectedTaxCode.Id,
            SourceType = string.IsNullOrWhiteSpace(QuickVat.SourceType) ? "ManualVAT" : QuickVat.SourceType,
            InvoiceNo = QuickVat.InvoiceNo,
            InvoiceDate = QuickVat.InvoiceDate,
            PartnerType = QuickVat.PartnerType,
            TaxBaseAmount = QuickVat.TaxBaseAmount,
            TaxAmount = QuickVat.TaxAmount,
            Direction = QuickVat.Direction,
            DeclarationPeriod = PeriodCode,
            Status = "Posted"
        }, token));
    }

    [RelayCommand]
    private async Task GenerateDeclarationAsync()
    {
        await RunWorkflowActionAsync("Lập tờ khai VAT", token => _taxService.BuildVatDeclarationDraftAsync(PeriodCode, token));
    }

    [RelayCommand]
    private async Task LockDeclarationAsync()
    {
        if (SelectedDeclaration is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Khóa tờ khai VAT", token => _taxService.MarkDeclaredAsync(SelectedDeclaration.Id, CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task ReopenDeclarationAsync()
    {
        if (SelectedDeclaration is null || !_dialogService.Confirm("Mở lại tờ khai VAT đã chọn?", "Mở lại tờ khai"))
        {
            return;
        }

        await RunWorkflowActionAsync("Mở lại tờ khai VAT", token => _taxService.ReopenDeclarationAsync(SelectedDeclaration.Id, "Re-open from Tax/FS workflow", CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task ExportDeclarationAsync()
    {
        await ExportReportWithCodeAsync("VAT_OUTPUT");
        await ExportReportWithCodeAsync("VAT_INPUT");
    }

    [RelayCommand]
    private async Task GenerateFinancialStatementsAsync()
    {
        await RunWorkflowActionAsync("Lập báo cáo tài chính", async token =>
        {
            await _financialStatementService.BuildSnapshotsAsync(PeriodCode, true, CurrentUserName(), token);
            var balance = await _financialStatementService.GenerateBalanceSheetAsync(PeriodCode, CurrentUserName(), token);
            var income = await _financialStatementService.GenerateIncomeStatementAsync(PeriodCode, CurrentUserName(), token);
            var cashflow = await _financialStatementService.GenerateCashFlowAsync(PeriodCode, CurrentUserName(), token);
            LoadStatementLines(balance, income, cashflow);
        });
    }

    [RelayCommand]
    private async Task PreviewClosingAsync()
    {
        await RunWorkflowActionAsync("Xem trước kết chuyển", async token =>
        {
            var preview = await _closingService.PreviewClosingAsync(PeriodCode, token);
            ClosingLines.ResetWith(preview.Lines);
            PostingVisibilityText = $"Xem trước kết chuyển kỳ {preview.Period}: {preview.Lines.Count:N0} dòng.";
        });
    }

    [RelayCommand]
    private async Task BatchClosingAsync()
    {
        if (!_dialogService.Confirm($"Kết chuyển kỳ {PeriodCode}?", "Kết chuyển hàng loạt"))
        {
            return;
        }

        await RunWorkflowActionAsync("Kết chuyển hàng loạt", token => _closingService.ClosePeriodAsync(PeriodCode, CurrentUserName(), "Close from Tax/FS workflow", token));
    }

    [RelayCommand]
    private async Task ReverseClosingAsync()
    {
        if (SelectedClosingEntry is null || !_dialogService.Confirm("Đảo bút toán kết chuyển đã chọn?", "Đảo kết chuyển"))
        {
            return;
        }

        await RunWorkflowActionAsync("Đảo kết chuyển", token => _closingService.ReverseClosingAsync(SelectedClosingEntry.Id, "Reverse from Tax/FS workflow", CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task LockPeriodAsync()
    {
        await RunWorkflowActionAsync("Khóa kỳ kế toán", token => _accountingPeriodService.LockPeriodAsync(PeriodCode, CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task UnlockPeriodAsync()
    {
        if (!_dialogService.Confirm($"Mở khóa kỳ {PeriodCode}?", "Mở khóa kỳ"))
        {
            return;
        }

        await RunWorkflowActionAsync("Mở khóa kỳ kế toán", token => _accountingPeriodService.UnlockPeriodAsync(PeriodCode, CurrentUserName(), "Unlock from Tax/FS workflow", token));
    }

    [RelayCommand]
    private async Task RunReportAsync()
    {
        _reportCts?.Cancel();
        _reportCts = new CancellationTokenSource();
        var token = _reportCts.Token;
        var gateAcquired = false;
        IsReportLoading = true;
        try
        {
            await _operationGate.WaitAsync(token);
            gateAcquired = true;
            var result = await _reportEngine.ExecuteAsync(BuildReportRequest(SelectedReportCode), token);
            ReportRows.ResetWith(result.Rows.Select(x => new TaxFinancialReportRow(result.Columns, x)).Take(2500));
            await RefreshReportHistoryAsync(SelectedReportCode, token);
            StatusText = $"Báo cáo {result.ReportName}: {result.RowCount:N0} dòng.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Đã hủy báo cáo.";
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Report", ex.Message);
        }
        finally
        {
            if (gateAcquired)
            {
                _operationGate.Release();
            }
            IsReportLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        await ExportReportWithCodeAsync(SelectedReportCode);
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
    private void OpenSelectedDrilldown()
    {
        if (SelectedVatTransaction is not null && SelectedTabIndex == 0)
        {
            PostingVisibilityText = $"VAT -> bút toán {FormatGuid(SelectedVatTransaction.JournalHeaderId)} -> nguồn {SelectedVatTransaction.SourceType} {FormatGuid(SelectedVatTransaction.SourceId)}.";
            return;
        }

        if (SelectedStatementLine is not null && SelectedTabIndex == 1)
        {
            PostingVisibilityText = $"Chỉ tiêu BCTC -> số dư tài khoản -> bút toán | {SelectedStatementLine.LineCode} | số tiền {SelectedStatementLine.Amount:N0}.";
            _ = LoadJournalDrilldownAsync(SelectedStatementLine.LineCode);
            return;
        }

        if (SelectedClosingEntry is not null)
        {
            PostingVisibilityText = $"Kết chuyển -> bút toán {FormatGuid(SelectedClosingEntry.JournalHeaderId)} -> nguồn kết chuyển CLOSE-{SelectedClosingEntry.Period} | trạng thái {SelectedClosingEntry.Status}.";
        }
    }

    private async Task LoadCoreAsync(CancellationToken token)
    {
        ValidationMessages.Clear();
        var taxes = await _taxService.GetTaxCodesAsync(token);
        var vatDirection = SelectedVatDirection.Value;
        var vat = await _taxService.GetVatTransactionsAsync(PeriodCode, vatDirection, token);
        var declarations = await _taxService.GetDeclarationsAsync(null, token);
        var snapshots = await _financialStatementService.BuildSnapshotsAsync(PeriodCode, true, CurrentUserName(), token);
        var balance = await _financialStatementService.GenerateBalanceSheetAsync(PeriodCode, CurrentUserName(), token);
        var income = await _financialStatementService.GenerateIncomeStatementAsync(PeriodCode, CurrentUserName(), token);
        var cashflow = await _financialStatementService.GenerateCashFlowAsync(PeriodCode, CurrentUserName(), token);
        var periods = await _accountingPeriodService.GetPeriodsAsync(ParseYear(PeriodCode), token);
        var closings = await _closingService.GetClosingEntriesAsync(null, 100, token);

        TaxCodes.ResetWith(taxes);
        VatTransactions.ResetWith(ApplyVatFilter(vat));
        Declarations.ResetWith(declarations);
        SnapshotBalances.ResetWith(snapshots.OrderBy(x => x.AccountCode));
        AccountingPeriods.ResetWith(periods.OrderByDescending(x => x.PeriodCode));
        ClosingEntries.ResetWith(closings);
        LoadStatementLines(balance, income, cashflow);

        SelectedTaxCode ??= TaxCodes.FirstOrDefault(x => x.IsActive) ?? TaxCodes.FirstOrDefault();
        SelectedVatTransaction ??= VatTransactions.FirstOrDefault();
        SelectedDeclaration ??= Declarations.FirstOrDefault(x => x.DeclarationPeriod == PeriodCode) ?? Declarations.FirstOrDefault();
        SelectedAccountingPeriod = AccountingPeriods.FirstOrDefault(x => x.PeriodCode == PeriodCode) ?? SelectedAccountingPeriod;
        SelectedClosingEntry ??= ClosingEntries.FirstOrDefault();

        InputVatAmount = VatTransactions.Where(x => x.Direction == "Input").Sum(x => x.TaxAmount);
        OutputVatAmount = VatTransactions.Where(x => x.Direction == "Output").Sum(x => x.TaxAmount);
        VatPayable = Math.Max(0m, OutputVatAmount - InputVatAmount);
        VatReceivable = Math.Max(0m, InputVatAmount - OutputVatAmount);
        TotalAssets = balance.TotalAssets;
        TotalLiabilitiesAndEquity = balance.TotalLiabilitiesAndEquity;
        ProfitBeforeTax = income.ProfitBeforeTax;
        IsBalanced = balance.IsBalanced;
        ClosingStateText = SelectedAccountingPeriod is null
            ? $"Kỳ {PeriodCode} chưa tồn tại."
            : $"{SelectedAccountingPeriod.PeriodCode}: {SelectedAccountingPeriod.Status}, khóa bởi {SelectedAccountingPeriod.LockedBy ?? "chưa có"}";

        BuildInlineValidation();
        await RefreshReportHistoryAsync(SelectedReportCode, token);
        LastRefreshAt = DateTime.Now;
        StatusText = $"Đã tải {VatTransactions.Count:N0} dòng VAT, {StatementLines.Count:N0} dòng BCTC, {ClosingEntries.Count:N0} bút toán kết chuyển.";
        OnPropertyChanged(nameof(LastRefreshText));
        OnPropertyChanged(nameof(VatPayableText));
        OnPropertyChanged(nameof(VatReceivableText));
        OnPropertyChanged(nameof(BalanceStatusText));
    }

    private void LoadStatementLines(BalanceSheetDto balance, IncomeStatementDto income, CashFlowDto cashflow)
    {
        var rows = balance.Assets.Select(x => WithNote(x, "Bảng cân đối - Tài sản"))
            .Concat(balance.Liabilities.Select(x => WithNote(x, "Bảng cân đối - Nợ phải trả")))
            .Concat(balance.Equity.Select(x => WithNote(x, "Bảng cân đối - Vốn chủ sở hữu")))
            .Concat(income.Lines.Select(x => WithNote(x, "Báo cáo kết quả kinh doanh")))
            .Concat(cashflow.Lines.Select(x => WithNote(x, "Lưu chuyển tiền tệ")))
            .ToList();
        StatementLines.ResetWith(rows);
        SelectedStatementLine ??= StatementLines.FirstOrDefault();
    }

    private static StatementLineDto WithNote(StatementLineDto source, string note)
        => new() { LineCode = source.LineCode, LineName = source.LineName, Amount = source.Amount, Note = source.Note ?? note };

    private async Task LoadJournalDrilldownAsync(string accountCode)
    {
        await _operationGate.WaitAsync();
        try
        {
            var rows = await _generalLedgerService.GetLedgerEntriesAsync(new LedgerFilterDto
            {
                AccountCode = accountCode,
                FromDate = FromDate,
                ToDate = ToDate,
                Keyword = Keyword
            });
            JournalDrilldownRows.ResetWith(rows.Take(500));
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Truy vết", ex.Message);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private IEnumerable<VatTransactionDto> ApplyVatFilter(IEnumerable<VatTransactionDto> values)
    {
        var query = values;
        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            query = query.Where(x => (x.InvoiceNo?.Contains(Keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                                     || x.SourceType.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
                                     || (x.TaxCode?.Contains(Keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return query.OrderByDescending(x => x.InvoiceDate ?? DateTime.MinValue).ThenByDescending(x => x.InvoiceNo);
    }

    private void BuildInlineValidation()
    {
        if (VatTransactions.Any(x => string.IsNullOrWhiteSpace(x.TaxCode)))
        {
            AddValidation("Error", "VAT", "Có dòng VAT thiếu mã thuế.");
        }
        if (VatTransactions.Any(x => x.TaxRate < 0 || x.TaxRate > 100))
        {
            AddValidation("Error", "VAT", "Có dòng VAT có thuế suất không hợp lệ.");
        }
        if (VatTransactions.Where(x => !string.IsNullOrWhiteSpace(x.InvoiceNo)).GroupBy(x => new { x.InvoiceNo, x.Direction }).Any(x => x.Count() > 1))
        {
            AddValidation("Warning", "VAT", "Có hóa đơn VAT trùng số trong kỳ/chiều đang lọc.");
        }
        if (VatTransactions.Any(x => !x.JournalHeaderId.HasValue))
        {
            AddValidation("Warning", "VAT", "Có dòng VAT chưa liên kết bút toán.");
        }
        if (!IsBalanced)
        {
            AddValidation("Warning", "BCTC", "Bảng cân đối đang lệch.");
        }
        if (SnapshotBalances.Count == 0)
        {
            AddValidation("Info", "Số dư", "Chưa có ảnh chụp số dư cho kỳ.");
        }
        if (SelectedAccountingPeriod is { Status: not AccountingPeriodStatus.Open })
        {
            AddValidation("Info", "Kỳ kế toán", "Kỳ hiện tại đang khóa/đóng.");
        }
    }

    private void QueueFilterRefresh()
    {
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

    private async Task RunWorkflowActionAsync<T>(string scope, Func<CancellationToken, Task<T>> action)
        => await RunWorkflowActionAsync(scope, async token => _ = await action(token));

    private async Task RunWorkflowActionAsync(string scope, Func<CancellationToken, Task> action)
    {
        _refreshCts?.Cancel();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        await _operationGate.WaitAsync(token);
        IsBusy = true;
        try
        {
            await action(token);
            _notificationService.Show($"{scope} hoàn tất.");
            await LoadCoreAsync(token);
        }
        catch (Exception ex)
        {
            AddValidation("Error", scope, ex.Message);
        }
        finally
        {
            IsBusy = false;
            _operationGate.Release();
        }
    }

    private async Task ExportReportWithCodeAsync(string reportCode)
    {
        _reportCts?.Cancel();
        _reportCts = new CancellationTokenSource();
        var token = _reportCts.Token;
        var gateAcquired = false;
        IsReportLoading = true;
        try
        {
            await _operationGate.WaitAsync(token);
            gateAcquired = true;
            var request = BuildReportRequest(reportCode);
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
            var path = Path.Combine(folder, $"{reportCode}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");
            await File.WriteAllBytesAsync(path, bytes, token);
            _notificationService.Show($"Đã xuất báo cáo {Path.GetFileName(path)}.");
            await RefreshReportHistoryAsync(reportCode, token);
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Xuất báo cáo", ex.Message);
        }
        finally
        {
            if (gateAcquired)
            {
                _operationGate.Release();
            }
            IsReportLoading = false;
        }
    }

    private async Task RefreshReportHistoryAsync(string? reportCode, CancellationToken cancellationToken)
    {
        var history = await _reportEngine.GetExecutionHistoryAsync(reportCode, 20, cancellationToken);
        ReportHistory.ResetWith(history);
    }

    private ReportRequestDto BuildReportRequest(string reportCode)
    {
        var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["fromDate"] = (FromDate ?? DateTime.Today.AddMonths(-1)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["toDate"] = (ToDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["period"] = PeriodCode,
            ["declarationPeriod"] = PeriodCode
        };

        return new ReportRequestDto
        {
            ReportCode = reportCode,
            ExecutedBy = CurrentUserName(),
            Parameters = parameters
        };
    }

    private static int ParseYear(string period)
    {
        return DateTime.TryParseExact($"{period}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Year
            : DateTime.Today.Year;
    }

    private static string FormatGuid(Guid? id) => id?.ToString() ?? "chưa có";

    private void AddValidation(string severity, string scope, string message)
    {
        ValidationMessages.Insert(0, new TaxFinancialValidationMessage(LocalizeSeverity(severity), LocalizeScope(scope), message));
        while (ValidationMessages.Count > 24)
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
            "Report" => "Báo cáo",
            "Drilldown" => "Truy vết",
            "Export" => "Xuất báo cáo",
            "Financial Statement" => "BCTC",
            "Snapshot" => "Số dư",
            "Period" => "Kỳ kế toán",
            _ => scope
        };

    private string CurrentUserName()
        => _currentUserService.CurrentUser?.DisplayName ?? "tax-fs-ui";
}

public sealed record TaxFinancialValidationMessage(string Severity, string Scope, string Message);

public sealed record TaxVatDirectionOption(string Label, string? Value);

public sealed partial class QuickVatEntry : ObservableObject
{
    [ObservableProperty] private string invoiceNo = string.Empty;
    [ObservableProperty] private DateTime? invoiceDate = DateTime.Today;
    [ObservableProperty] private string partnerType = "Supplier";
    [ObservableProperty] private decimal taxBaseAmount;
    [ObservableProperty] private decimal taxAmount;
    [ObservableProperty] private string direction = "Input";
    [ObservableProperty] private string sourceType = "ManualVAT";
}

public sealed class TaxFinancialReportRow
{
    public TaxFinancialReportRow(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row)
    {
        Column1 = Read(columns, row, 0);
        Column2 = Read(columns, row, 1);
        Column3 = Read(columns, row, 2);
        Column4 = Read(columns, row, 3);
        Debit = ReadAny(columns, row, "Debit", "OpeningDebit", "PeriodDebit", "TaxBaseAmount");
        Credit = ReadAny(columns, row, "Credit", "OpeningCredit", "PeriodCredit", "TaxAmount");
        Amount = ReadAny(columns, row, "Amount", "ClosingDebit", "ClosingCredit", "NetFlow");
        Status = ReadAny(columns, row, "Status", "JournalStatus", "Section");
    }

    public string Column1 { get; }
    public string Column2 { get; }
    public string Column3 { get; }
    public string Column4 { get; }
    public string Debit { get; }
    public string Credit { get; }
    public string Amount { get; }
    public string Status { get; }

    private static string Read(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row, int index)
        => index >= 0 && index < columns.Count ? Read(columns, row, columns[index].Name) : string.Empty;

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

file static class TaxFinancialCollectionExtensions
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
