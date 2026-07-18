using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class GeneralLedgerViewModel : ViewModelBase
{
    private readonly IGeneralLedgerService _service;
    private readonly IJournalService _journalService;
    private readonly INotificationService _notification;
    private readonly IDialogService _dialog;
    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _debounceCts;
    private bool _suppressAutoRefresh;

    public ObservableCollection<LedgerEntryDto> Entries { get; } = new();
    public ObservableCollection<LedgerBalanceDto> Balances { get; } = new();
    public ObservableCollection<LedgerAccountDto> Accounts { get; } = new();
    public ObservableCollection<LedgerReportRow> ReportRows { get; } = new();
    public ObservableCollection<LedgerAuditLogRow> AuditRows { get; } = new();
    public ObservableCollection<LedgerEntryDto> SelectedEntries { get; } = new();
    public TablePagerViewModel<LedgerEntryDto> EntryPager { get; } = new() { ItemName = "bút toán" };
    public TablePagerViewModel<LedgerEntryDto> PostingPager { get; } = new() { ItemName = "chứng từ" };
    public TablePagerViewModel<LedgerBalanceDto> BalancePager { get; } = new() { ItemName = "tài khoản" };
    public TablePagerViewModel<LedgerAccountDto> AccountPager { get; } = new() { ItemName = "tài khoản" };
    public TablePagerViewModel<LedgerReportRow> ReportPager { get; } = new() { ItemName = "báo cáo" };
    public TablePagerViewModel<LedgerAuditLogRow> AuditPager { get; } = new() { ItemName = "nhật ký" };
    public ObservableCollection<LedgerDocumentType?> DocumentTypeOptions { get; } = new() { null, LedgerDocumentType.ImportVoucher, LedgerDocumentType.ExportVoucher, LedgerDocumentType.PaymentReceipt, LedgerDocumentType.PaymentVoucher, LedgerDocumentType.AdvancePayment, LedgerDocumentType.Adjustment, LedgerDocumentType.OpeningBalance };
    public ObservableCollection<LedgerFilterOption> SourceTypeOptions { get; } = new()
    {
        new("Tất cả", string.Empty),
        new("Bút toán thủ công", "ManualJournal"),
        new("Phiếu nhập", "StockReceipt"),
        new("Phiếu xuất", "StockIssue"),
        new("Phiếu thu", "PaymentReceipt"),
        new("Phiếu chi", "PaymentVoucher"),
        new("Bút toán đảo", "Reversal")
    };
    public ObservableCollection<LedgerFilterOption> StatusOptions { get; } = new()
    {
        new("Tất cả", string.Empty),
        new("Nháp", "Draft"),
        new("Đã ghi sổ", "Posted"),
        new("Đã đảo", "Reversed")
    };
    public ObservableCollection<string> ReportTemplates { get; } = new() { "Sổ cái theo tài khoản", "Nhật ký chung", "Bảng cân đối phát sinh" };

    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private DateTime? fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? toDate = DateTime.Today;
    [ObservableProperty] private string selectedAccountCode = string.Empty;
    [ObservableProperty] private LedgerDocumentType? selectedDocumentType;
    [ObservableProperty] private string selectedSourceType = string.Empty;
    [ObservableProperty] private string selectedStatus = string.Empty;
    [ObservableProperty] private LedgerEntryDto? selectedEntry;
    [ObservableProperty] private LedgerAccountDto? selectedAccount;
    [ObservableProperty] private string selectedReportTemplate = "Sổ cái theo tài khoản";

    [ObservableProperty] private decimal totalDebit;
    [ObservableProperty] private decimal totalCredit;
    [ObservableProperty] private int totalEntries;
    [ObservableProperty] private decimal difference;
    [ObservableProperty] private DateTime lastRefreshAt = DateTime.Now;
    [ObservableProperty] private int selectedTabIndex;
    [ObservableProperty] private bool isSummaryExpanded;
    [ObservableProperty] private bool isFilterExpanded;

    public bool IsBalanced => Difference == 0;
    public string BalanceStatus => IsBalanced ? "Cân đối" : "Lệch số";
    public int ActiveAccountCount => Accounts.Count(x => x.IsActive);
    public int UnpostedVoucherCount => Entries.Count(x => string.Equals(x.Status, "Draft", StringComparison.OrdinalIgnoreCase));
    public int ErrorEntryCount => Entries.Count(IsEntryInvalid);
    public IEnumerable<LedgerEntryDto> ErrorEntries => Entries.Where(IsEntryInvalid);
    public IEnumerable<LedgerEntryDto> RecentEntries => Entries.OrderByDescending(x => x.EntryDate).Take(8);
    public IEnumerable<LedgerBalanceDto> TopDebitAccounts => Balances.OrderByDescending(x => x.PeriodDebit).Take(6);
    public LedgerBalanceDto? SelectedAccountBalance => SelectedAccount is null ? null : Balances.FirstOrDefault(x => x.AccountCode == SelectedAccount.AccountCode);
    public decimal SelectedAccountDebitBalance => SelectedAccountBalance?.ClosingDebit ?? 0m;
    public decimal SelectedAccountCreditBalance => SelectedAccountBalance?.ClosingCredit ?? 0m;
    public string StatusBarText => $"Bút toán: {TotalEntries} • Chênh lệch: {Difference:N0} • Cập nhật: {LastRefreshAt:HH:mm:ss} • Nguồn: SQL Server";

    public GeneralLedgerViewModel(IGeneralLedgerService service, IJournalService journalService, INotificationService notification, IDialogService dialog)
    {
        _service = service;
        _journalService = journalService;
        _notification = notification;
        _dialog = dialog;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
        => await RefreshNowAsync();

    private async Task RefreshNowAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = new CancellationTokenSource();
        var cancellationToken = _refreshCts.Token;
        IsBusy = true;
        try
        {
            var accounts = await _service.GetAccountsAsync(cancellationToken);
            Accounts.Clear();
            foreach (var account in accounts) Accounts.Add(account);

            var filter = BuildFilter();
            var entries = await _service.GetLedgerEntriesAsync(filter, cancellationToken);
            Entries.Clear();
            foreach (var entry in entries) Entries.Add(entry);
            EntryPager.SetSource(Entries);
            PostingPager.SetSource(Entries);

            var balances = await _service.GetLedgerBalancesAsync(filter, cancellationToken);
            Balances.Clear();
            foreach (var balance in balances) Balances.Add(balance);
            BalancePager.SetSource(Balances);
            AccountPager.SetSource(Accounts);

            TotalDebit = Entries.Sum(GetDebitForTotal);
            TotalCredit = Entries.Sum(GetCreditForTotal);
            TotalEntries = Entries.Count;
            Difference = TotalDebit - TotalCredit;
            LastRefreshAt = DateTime.Now;
            SelectedEntry = Entries.FirstOrDefault();
            if (SelectedAccount is null || Accounts.All(x => x.AccountCode != SelectedAccount.AccountCode))
            {
                SelectedAccount = Accounts.FirstOrDefault();
            }
            OnPropertyChanged(nameof(IsBalanced));
            OnPropertyChanged(nameof(BalanceStatus));
            OnPropertyChanged(nameof(ActiveAccountCount));
            OnPropertyChanged(nameof(UnpostedVoucherCount));
            OnPropertyChanged(nameof(ErrorEntryCount));
            OnPropertyChanged(nameof(ErrorEntries));
            OnPropertyChanged(nameof(RecentEntries));
            OnPropertyChanged(nameof(TopDebitAccounts));
            OnPropertyChanged(nameof(SelectedAccountBalance));
            OnPropertyChanged(nameof(SelectedAccountDebitBalance));
            OnPropertyChanged(nameof(SelectedAccountCreditBalance));
            OnPropertyChanged(nameof(StatusBarText));
            RebuildReportRows();
            RebuildAuditRows();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _dialog.Error(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        _suppressAutoRefresh = true;
        try
        {
            Keyword = string.Empty;
            FromDate = DateTime.Today.AddMonths(-1);
            ToDate = DateTime.Today;
            SelectedAccountCode = string.Empty;
            SelectedDocumentType = null;
            SelectedSourceType = string.Empty;
            SelectedStatus = string.Empty;
        }
        finally
        {
            _suppressAutoRefresh = false;
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        if (Accounts.Count == 0)
        {
            _dialog.Info("Không có danh mục tài khoản.");
            return;
        }

        var dialog = new LedgerEntryDialog(Accounts)
        {
            Owner = App.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        await _service.CreateManualEntryAsync(dialog.Result);
        _notification.Show("Đã thêm bút toán và cập nhật sổ cái.");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task PostFromDocumentAsync()
    {
        await PostSelectedJournalAsync();
    }

    [RelayCommand]
    private async Task PostSelectedJournalAsync()
    {
        var journalId = SelectedEntry?.JournalId;
        if (journalId is null)
        {
            _dialog.Info("Dòng đang chọn chưa liên kết bút toán persisted.", "Ghi sổ");
            return;
        }

        try
        {
            await _journalService.PostDraftAsync(journalId.Value, "system");
            _notification.Show("Đã ghi sổ bút toán đang chọn.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _dialog.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ReverseSelectedJournalAsync()
    {
        var journalId = SelectedEntry?.JournalId;
        if (journalId is null)
        {
            _dialog.Info("Dòng đang chọn chưa liên kết bút toán persisted.", "Đảo bút toán");
            return;
        }

        try
        {
            await _journalService.ReverseAsync(journalId.Value, "Đảo bút toán từ màn Sổ cái", "system");
            _notification.Show("Đã tạo bút toán đảo.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _dialog.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task BatchPostAsync()
    {
        var journalIds = GetWorkflowSelection()
            .Where(x => string.Equals(x.Status, "Draft", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.JournalId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (journalIds.Count == 0)
        {
            _dialog.Info("Không có bút toán nháp để ghi sổ trong vùng đang chọn.", "Ghi sổ hàng loạt");
            return;
        }

        await RunBatchAsync(journalIds, id => _journalService.PostDraftAsync(id, "system"), "Đã ghi sổ hàng loạt");
    }

    [RelayCommand]
    private async Task BatchReverseAsync()
    {
        var journalIds = GetWorkflowSelection()
            .Where(x => string.Equals(x.Status, "Posted", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.JournalId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (journalIds.Count == 0)
        {
            _dialog.Info("Không có bút toán đã ghi sổ để đảo trong vùng đang chọn.", "Đảo hàng loạt");
            return;
        }

        await RunBatchAsync(journalIds, id => _journalService.ReverseAsync(id, "Đảo bút toán hàng loạt từ màn Sổ cái", "system"), "Đã đảo bút toán hàng loạt");
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        var accountCode = string.IsNullOrWhiteSpace(SelectedAccountCode) ? "131" : SelectedAccountCode;
        await _service.PrintLedgerAsync(accountCode, FromDate, ToDate);
        var preview = new PrintPreviewDialog(
            $"Sổ cái tài khoản {accountCode}",
            $"Tài khoản: {accountCode}\nKỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}\nTổng phát sinh Nợ: {TotalDebit:N0}\nTổng phát sinh Có: {TotalCredit:N0}\nChênh lệch: {Difference:N0}\nSố bút toán: {TotalEntries}");
        preview.Owner = App.Current.MainWindow;
        preview.ShowDialog();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        await _service.ExportLedgerAsync(BuildFilter());

        var file = CsvExportHelper.Export(
            "general_ledger",
            ["Ngày", "Số CT", "Diễn giải", "TK Nợ", "TK Có", "Số tiền", "Loại CT"],
            Entries.Select(x => (IReadOnlyList<string>)
            [
                x.EntryDate.ToString("dd/MM/yyyy"),
                x.DocumentNo,
                x.Description,
                x.DebitAccount,
                x.CreditAccount,
                CsvExportHelper.F(x.Amount),
                ToVietnameseDocumentType(x.DocumentType)
            ]).ToList());

        _dialog.Info($"Đã xuất CSV: {file}", "Xuất dữ liệu");
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        var preview = new PrintPreviewDialog(
            $"Báo cáo: {SelectedReportTemplate}",
            $"Mẫu báo cáo: {SelectedReportTemplate}\nKỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}\nSố bút toán: {TotalEntries}\nTổng Nợ: {TotalDebit:N0}\nTổng Có: {TotalCredit:N0}");
        preview.Owner = App.Current.MainWindow;
        preview.ShowDialog();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void OpenEntriesTab() => SelectedTabIndex = 1;

    [RelayCommand]
    private void OpenBalancesTab() => SelectedTabIndex = 2;

    [RelayCommand]
    private void OpenBalanceCheckTab() => SelectedTabIndex = 5;

    [RelayCommand]
    private async Task OpenSelectedAccountJournalsAsync()
    {
        var accountCode = SelectedAccount?.AccountCode;
        if (string.IsNullOrWhiteSpace(accountCode))
        {
            return;
        }

        SelectedAccountCode = accountCode;
        SelectedTabIndex = 1;
        await RefreshAsync();
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

    private static decimal GetDebitForTotal(LedgerEntryDto entry)
        => entry.DebitAmount != 0 || entry.CreditAmount != 0 ? entry.DebitAmount : entry.Amount;

    private static decimal GetCreditForTotal(LedgerEntryDto entry)
        => entry.DebitAmount != 0 || entry.CreditAmount != 0 ? entry.CreditAmount : entry.Amount;

    private static bool IsEntryInvalid(LedgerEntryDto entry)
    {
        if (entry.DebitAmount != 0 || entry.CreditAmount != 0)
        {
            return entry.DebitAmount < 0
                || entry.CreditAmount < 0
                || (entry.DebitAmount == 0 && entry.CreditAmount == 0)
                || (entry.DebitAmount > 0 && entry.CreditAmount > 0);
        }

        return entry.Amount <= 0 || entry.DebitAccount == entry.CreditAccount;
    }

    private LedgerFilterDto BuildFilter() => new()
    {
        Keyword = Keyword,
        FromDate = FromDate,
        ToDate = ToDate,
        AccountCode = SelectedAccountCode,
        DocumentType = SelectedDocumentType
        ,
        SourceType = SelectedSourceType,
        Status = SelectedStatus
    };

    public void ReplaceSelectedEntries(IEnumerable<LedgerEntryDto> rows)
    {
        SelectedEntries.Clear();
        foreach (var row in rows)
        {
            SelectedEntries.Add(row);
        }
    }

    private IReadOnlyList<LedgerEntryDto> GetWorkflowSelection()
    {
        if (SelectedEntries.Count > 0)
        {
            return SelectedEntries.ToList();
        }

        if (SelectedEntry is not null)
        {
            return [SelectedEntry];
        }

        return EntryPager.Items.ToList();
    }

    private async Task RunBatchAsync<T>(IReadOnlyCollection<Guid> journalIds, Func<Guid, Task<T>> action, string successMessage)
    {
        IsBusy = true;
        var success = 0;
        var errors = new List<string>();
        try
        {
            foreach (var journalId in journalIds)
            {
                try
                {
                    await action(journalId);
                    success++;
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
            }

            if (errors.Count > 0)
            {
                _dialog.Error($"{successMessage}: {success}/{journalIds.Count}. Lỗi đầu tiên: {errors[0]}");
            }
            else
            {
                _notification.Show($"{successMessage}: {success} bút toán.");
            }

            await RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void QueueRefresh()
    {
        if (_suppressAutoRefresh)
        {
            return;
        }

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, token);
                if (!token.IsCancellationRequested)
                {
                    await App.Current.Dispatcher.InvokeAsync(async () => await RefreshAsync());
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private void RebuildReportRows()
    {
        ReportRows.Clear();
        ReportRows.Add(new("Sổ cái chi tiết tài khoản", "Sổ cái và chi tiết", "Chi tiết theo từng tài khoản", "Excel, PDF", "Theo yêu cầu"));
        ReportRows.Add(new("Sổ cái tổng hợp", "Báo cáo tổng hợp", "Tổng hợp phát sinh tài khoản trong kỳ", "Excel, PDF", "Theo tháng"));
        ReportRows.Add(new("Bảng cân đối phát sinh", "Báo cáo cân đối", "Cân đối phát sinh Nợ - Có theo tài khoản", "Excel, PDF", "Theo tháng"));
        ReportRows.Add(new("Nhật ký chung", "Báo cáo tổng hợp", "Danh sách bút toán theo trình tự thời gian", "Excel, PDF", "Theo tháng"));
        ReportRows.Add(new("Báo cáo kiểm tra cân đối", "Báo cáo kiểm soát", "Kiểm tra chênh lệch phát sinh và số dư", "Excel", "Theo yêu cầu"));
        ReportPager.SetSource(ReportRows);
    }

    private void RebuildAuditRows()
    {
        AuditRows.Clear();
        foreach (var entry in Entries.OrderByDescending(x => x.EntryDate))
        {
            AuditRows.Add(new(
                entry.EntryDate,
                "Hệ thống",
                "Sổ cái",
                "Ghi nhận",
                entry.Description,
                entry.DocumentNo,
                "Thành công",
                "127.0.0.1"));
        }

        AuditPager.SetSource(AuditRows);
    }

    partial void OnSelectedAccountChanged(LedgerAccountDto? value)
    {
        OnPropertyChanged(nameof(SelectedAccountBalance));
        OnPropertyChanged(nameof(SelectedAccountDebitBalance));
        OnPropertyChanged(nameof(SelectedAccountCreditBalance));
    }

    partial void OnKeywordChanged(string value) => QueueRefresh();
    partial void OnFromDateChanged(DateTime? value) => QueueRefresh();
    partial void OnToDateChanged(DateTime? value) => QueueRefresh();
    partial void OnSelectedAccountCodeChanged(string value) => QueueRefresh();
    partial void OnSelectedDocumentTypeChanged(LedgerDocumentType? value) => QueueRefresh();
    partial void OnSelectedSourceTypeChanged(string value) => QueueRefresh();
    partial void OnSelectedStatusChanged(string value) => QueueRefresh();

    private static string ToVietnameseDocumentType(LedgerDocumentType documentType) => documentType switch
    {
        LedgerDocumentType.ImportVoucher => "Phiếu nhập",
        LedgerDocumentType.ExportVoucher => "Phiếu xuất",
        LedgerDocumentType.PaymentReceipt => "Phiếu thu",
        LedgerDocumentType.PaymentVoucher => "Phiếu chi",
        LedgerDocumentType.AdvancePayment => "Tạm ứng",
        LedgerDocumentType.Adjustment => "Điều chỉnh",
        LedgerDocumentType.OpeningBalance => "Số dư đầu kỳ",
        _ => "Khác"
    };
}

public sealed record LedgerReportRow(string ReportName, string ReportType, string Description, string ExportFormat, string Frequency);

public sealed record LedgerFilterOption(string Label, string Value);

public sealed record LedgerAuditLogRow(
    DateTime Time,
    string UserName,
    string FunctionName,
    string ActionName,
    string ChangeContent,
    string Target,
    string Result,
    string IpAddress);

