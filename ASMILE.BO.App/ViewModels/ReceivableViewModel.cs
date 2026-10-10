using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class ReceivableViewModel : ViewModelBase
{
    private readonly IReceivableService _service;
    private readonly IEmailDispatchService _emailDispatchService;
    private readonly INotificationService _notification;
    private readonly IDialogService _dialog;
    private bool _isRefreshing;

    public ObservableCollection<ReceivableSummaryDto> CustomerOptions { get; } = new();
    public ObservableCollection<ReceivableSummaryDto> Summaries { get; } = new();
    public ObservableCollection<ReceivableTransactionDto> AllTransactions { get; } = new();
    public ObservableCollection<ReceivableTransactionDto> Transactions { get; } = new();
    public ObservableCollection<MetricPoint> CollectionByDay { get; } = new();
    public ObservableCollection<ReceivableDailyFlowPoint> ReceivableFlowByDay { get; } = new();
    public ObservableCollection<ReceivableSummaryDto> OverdueRows { get; } = new();
    public ObservableCollection<ReceivableTransactionDto> RecentCollectionRows { get; } = new();
    public TablePagerViewModel<ReceivableSummaryDto> SummaryPager { get; } = new() { ItemName = "khách hàng" };
    public TablePagerViewModel<ReceivableTransactionDto> TransactionPager { get; } = new() { ItemName = "phát sinh" };
    public TablePagerViewModel<ReceivableSummaryDto> OverduePager { get; } = new() { ItemName = "khách hàng" };
    public TablePagerViewModel<ReceivableTransactionDto> CollectionPager { get; } = new() { ItemName = "giao dịch thu" };

    public ObservableCollection<DebtStatus?> StatusOptions { get; } = new() { null, DebtStatus.Open, DebtStatus.PartiallyPaid, DebtStatus.Paid, DebtStatus.Overdue, DebtStatus.Cancelled };
    public ObservableCollection<string> GroupOptions { get; } = new() { "Tất cả nhóm khách" };
    public ObservableCollection<string> OwnerOptions { get; } = new() { "Tất cả nhân viên" };
    public ObservableCollection<string> ReportTemplates { get; } = new() { "Tổng hợp phải thu", "Chi tiết phải thu", "Tuổi nợ", "Thu tiền theo ngày", "Khách hàng quá hạn", "Đối chiếu công nợ" };

    [ObservableProperty] private ReceivableSummaryDto? selectedSummary;
    [ObservableProperty] private ReceivableTransactionDto? selectedTransaction;
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private DateTime? fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? toDate = DateTime.Today;
    [ObservableProperty] private DebtStatus? selectedStatus;
    [ObservableProperty] private string selectedGroup = "Tất cả nhóm khách";
    [ObservableProperty] private string selectedOwner = "Tất cả nhân viên";
    [ObservableProperty] private bool overdueOnly;
    [ObservableProperty] private bool remainingOnly = true;
    [ObservableProperty] private string selectedReportTemplate = "Tổng hợp phải thu";

    [ObservableProperty] private decimal totalReceivable;
    [ObservableProperty] private decimal collectedAmount;
    [ObservableProperty] private decimal remainingAmount;
    [ObservableProperty] private decimal overdueAmount;

    [ObservableProperty] private decimal bucket0To30;
    [ObservableProperty] private decimal bucket31To60;
    [ObservableProperty] private decimal bucket61To90;
    [ObservableProperty] private decimal bucket90Plus;
    [ObservableProperty] private DateTime lastRefreshAt = DateTime.Now;
    [ObservableProperty] private int selectedTabIndex;
    [ObservableProperty] private bool isSummaryExpanded;
    [ObservableProperty] private bool isFilterExpanded;

    public string ModuleStatus => "Đang hoạt động";
    public int DebtorCustomerCount => Summaries.Count(x => x.ClosingBalance > 0);
    public int TotalCustomerCount => Summaries.Count;
    public decimal DueIn7DaysAmount => DashboardTransactions.Where(x => x.BalanceAmount > 0 && x.DueDate.HasValue && x.DueDate.Value.Date >= DateTime.Today && x.DueDate.Value.Date <= DateTime.Today.AddDays(7)).Sum(x => x.BalanceAmount);
    public int CollectionTransactionCount => DashboardTransactions.Count(x => x.CreditAmount > 0);
    public decimal RecoveryRate => TotalReceivable <= 0 ? 0 : (CollectedAmount / TotalReceivable) * 100m;
    public IEnumerable<ReceivableSummaryDto> TopDebtors => Summaries.OrderByDescending(x => x.ClosingBalance).Take(5);
    public IEnumerable<ReceivableSummaryDto> OverdueCustomers => Summaries.Where(x => x.OverdueAmount > 0).OrderByDescending(x => x.OverdueAmount);
    public IEnumerable<ReceivableTransactionDto> RecentCollections => DashboardTransactions.Where(x => x.CreditAmount > 0).OrderByDescending(x => x.TransactionDate).Take(7);
    public string ReconciliationPeriodText => FromDate.HasValue && ToDate.HasValue
        ? $"{FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}"
        : "Theo bộ lọc hiện tại";
    public string StatusBarText => $"Khách hàng: {TotalCustomerCount} • Tổng còn nợ: {RemainingAmount:N0} • Quá hạn: {OverdueAmount:N0} • Cập nhật: {LastRefreshAt:HH:mm:ss} • Nguồn: SQL Server";
    private IEnumerable<ReceivableTransactionDto> DashboardTransactions => AllTransactions.Count > 0 ? AllTransactions : Transactions;

    public ReceivableViewModel(
        IReceivableService service,
        IEmailDispatchService emailDispatchService,
        INotificationService notification,
        IDialogService dialog)
    {
        _service = service;
        _emailDispatchService = emailDispatchService;
        _notification = notification;
        _dialog = dialog;
        _ = RefreshAsync();
    }

    partial void OnSelectedSummaryChanged(ReceivableSummaryDto? value)
    {
        if (value is null)
        {
            Transactions.Clear();
            TransactionPager.SetSource(Transactions);
            return;
        }

        if (!_isRefreshing)
        {
            _ = LoadTransactionsSafeAsync(value.CustomerId);
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsReconciliationTabActive));
    }

    public bool IsReconciliationTabActive => SelectedTabIndex == 4;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        _isRefreshing = true;
        try
        {
            await LoadLookupOptionsAsync();

            var filter = BuildFilter();
            var data = await _service.GetDebtSummariesAsync(filter);

            Summaries.Clear();
            foreach (var item in data.Where(x => !RemainingOnly || x.ClosingBalance > 0))
            {
                Summaries.Add(item);
            }
            SummaryPager.SetSource(Summaries);

            await LoadAllTransactionsAsync();

            TotalReceivable = Summaries.Sum(x => x.TotalDebit);
            CollectedAmount = Summaries.Sum(x => x.TotalCredit);
            RemainingAmount = Summaries.Sum(x => x.ClosingBalance);
            OverdueAmount = Summaries.Sum(x => x.OverdueAmount);
            LastRefreshAt = DateTime.Now;

            OnPropertyChanged(nameof(ModuleStatus));
            OnPropertyChanged(nameof(TotalCustomerCount));
            OnPropertyChanged(nameof(DebtorCustomerCount));
            OnPropertyChanged(nameof(DueIn7DaysAmount));
            OnPropertyChanged(nameof(CollectionTransactionCount));
            OnPropertyChanged(nameof(RecoveryRate));
            OnPropertyChanged(nameof(TopDebtors));
            OnPropertyChanged(nameof(OverdueCustomers));
            OnPropertyChanged(nameof(ReconciliationPeriodText));
            OnPropertyChanged(nameof(StatusBarText));

            if (SelectedSummary is null || CustomerOptions.All(x => x.CustomerId != SelectedSummary.CustomerId))
            {
                SelectedSummary = Summaries.FirstOrDefault() ?? CustomerOptions.FirstOrDefault();
            }
            else
            {
                SelectedSummary = Summaries.FirstOrDefault(x => x.CustomerId == SelectedSummary.CustomerId)
                    ?? CustomerOptions.FirstOrDefault(x => x.CustomerId == SelectedSummary.CustomerId);
            }

            if (SelectedSummary is not null)
            {
                await LoadTransactionsAsync(SelectedSummary.CustomerId);
            }

            RebuildAgingBuckets();
            RebuildCollectionByDay();
            RebuildDashboardTables();
        }
        catch (Exception ex)
        {
            _dialog.Error(ex.Message);
        }
        finally
        {
            _isRefreshing = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        Keyword = string.Empty;
        FromDate = DateTime.Today.AddMonths(-1);
        ToDate = DateTime.Today;
        SelectedStatus = null;
        SelectedGroup = "Tất cả nhóm khách";
        SelectedOwner = "Tất cả nhân viên";
        OverdueOnly = false;
        RemainingOnly = true;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CollectAsync()
    {
        if (Summaries.Count == 0)
        {
            _dialog.Info("Không có khách hàng để thu tiền.");
            return;
        }

        var dialog = new ReceivablePaymentDialog(Summaries)
        {
            Owner = App.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        await _service.CreatePaymentReceiptAsync(dialog.Result);
        _notification.Show("Đã ghi nhận phiếu thu và cập nhật công nợ.");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (SelectedSummary is null)
        {
            _dialog.Info("Chọn khách hàng cần in đối chiếu.");
            return;
        }

        await _service.PrintDebtStatementAsync(SelectedSummary.CustomerId);
        var preview = new PrintPreviewDialog(
            $"In đối chiếu công nợ - {SelectedSummary.CustomerCode}",
            $"Khách hàng: {SelectedSummary.CustomerName}\nMã: {SelectedSummary.CustomerCode}\nCòn nợ: {SelectedSummary.ClosingBalance:N0}\nQuá hạn: {SelectedSummary.OverdueAmount:N0}\nKỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}");
        preview.Owner = App.Current.MainWindow;
        preview.ShowDialog();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (Summaries.Count == 0)
        {
            _dialog.Info("Không có dữ liệu để xuất.");
            return;
        }

        foreach (var summary in Summaries.Take(1))
        {
            await _service.ExportDebtStatementAsync(summary.CustomerId);
        }

        var file = CsvExportHelper.Export(
            "receivable",
            ["Mã KH", "Tên khách hàng", "Đầu kỳ", "Phát sinh nợ", "Đã thu", "Còn nợ", "Quá hạn"],
            Summaries.Select(x => (IReadOnlyList<string>)
            [
                x.CustomerCode,
                x.CustomerName,
                CsvExportHelper.F(x.OpeningBalance),
                CsvExportHelper.F(x.TotalDebit),
                CsvExportHelper.F(x.TotalCredit),
                CsvExportHelper.F(x.ClosingBalance),
                CsvExportHelper.F(x.OverdueAmount)
            ]).ToList());

        _dialog.Info($"Đã xuất báo cáo: {file}", "Xuất dữ liệu");
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        var preview = new PrintPreviewDialog(
            $"Báo cáo: {SelectedReportTemplate}",
            $"Mẫu báo cáo: {SelectedReportTemplate}\nKỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}\nKhách hàng còn nợ: {DebtorCustomerCount}\nTổng còn nợ: {RemainingAmount:N0}");
        preview.Owner = App.Current.MainWindow;
        preview.ShowDialog();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenOverdueDrilldownAsync()
    {
        SelectedTabIndex = 5;
        OverdueOnly = true;
        await RefreshAsync();
    }

    [RelayCommand]
    private void OpenSummaryTab() => SelectedTabIndex = 1;

    [RelayCommand]
    private void OpenTransactionTab() => SelectedTabIndex = 2;

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
    private async Task FilterByCollectionPointAsync(MetricPoint? point)
    {
        if (point is null || string.IsNullOrWhiteSpace(point.Label))
        {
            return;
        }

        if (DateTime.TryParseExact(point.Label, "dd/MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            var day = new DateTime(DateTime.Today.Year, parsed.Month, parsed.Day);
            FromDate = day;
            ToDate = day;
            SelectedTabIndex = 2;
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task ReceivePaymentAsync() => await CollectAsync();

    [RelayCommand]
    private async Task CreateReconciliationAsync()
    {
        if (SelectedSummary is null)
        {
            _dialog.Info("Vui lòng chọn khách hàng cần đối chiếu.", "Đối chiếu công nợ");
            return;
        }

        // The current data contract only provides the persisted receivable
        // transactions and summary. Do not pretend to create a customer
        // confirmation record that has no backend storage yet; open the real
        // detail workspace instead.
        await OpenReconciliationDetailModalAsync();
    }

    [RelayCommand]
    private async Task SendReminderAsync()
    {
        var overdue = OverdueCustomers.ToList();
        if (overdue.Count == 0)
        {
            _dialog.Info("Không có khách hàng quá hạn cần gửi nhắc nợ.", "Gửi nhắc nợ");
            return;
        }

        var recipients = overdue
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .ToList();
        var missingEmailCount = overdue.Count - recipients.Count;
        var queuedCount = 0;
        var failedCount = 0;

        foreach (var customer in recipients)
        {
            try
            {
                await _emailDispatchService.QueueAsync(new EmailDispatchRequestDto
                {
                    Recipient = customer.Email.Trim(),
                    Subject = $"Nhắc thanh toán công nợ - {customer.CustomerName}",
                    Body = $"Kính gửi {customer.CustomerName},\n\n" +
                           $"Số tiền quá hạn hiện tại: {customer.OverdueAmount:N0} VND.\n" +
                           "Vui lòng liên hệ bộ phận kế toán để đối chiếu và thanh toán.\n\nTrân trọng.",
                    SourceType = "ReceivableReminder"
                }, CancellationToken.None);
                queuedCount++;
            }
            catch
            {
                failedCount++;
            }
        }

        var suffix = missingEmailCount > 0
            ? $" Bỏ qua {missingEmailCount} khách chưa có email."
            : string.Empty;
        _dialog.Info(
            $"Đã xếp hàng {queuedCount} email nhắc nợ vào outbox." +
            (failedCount > 0 ? $" {failedCount} email lỗi." : string.Empty) + suffix,
            "Gửi nhắc nợ");
    }

    [RelayCommand]
    private async Task PrintStatementAsync() => await PrintAsync();

    [RelayCommand]
    private async Task MoreAsync()
    {
        _dialog.Info("Tùy chọn nâng cao:\n- Mở vị trí VFC\n- Khóa kỳ đối chiếu\n- Gửi thông báo email", "Khác");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenReconciliationDetailModalAsync()
    {
        var dialog = new ReconciliationDetailDialog(SelectedSummary, Transactions, FromDate, ToDate)
        {
            Owner = App.Current.MainWindow
        };
        dialog.ShowDialog();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenReconciliationSendModalAsync()
    {
        var dialog = new ReconciliationListDialog(Summaries, SelectedSummary)
        {
            Owner = App.Current.MainWindow
        };
        dialog.ShowDialog();
        await Task.CompletedTask;
    }

    private async Task LoadTransactionsAsync(string customerId)
    {
        var rows = await _service.GetTransactionsByCustomerAsync(customerId, FromDate, ToDate);
        Transactions.Clear();
        foreach (var row in rows) Transactions.Add(row);
        TransactionPager.SetSource(Transactions);
        SelectedTransaction = Transactions.FirstOrDefault();
        OnPropertyChanged(nameof(RecentCollections));
        OnPropertyChanged(nameof(DueIn7DaysAmount));
        OnPropertyChanged(nameof(CollectionTransactionCount));
        RebuildAgingBuckets();
        RebuildCollectionByDay();
        RebuildDashboardTables();
    }

    private async Task LoadTransactionsSafeAsync(string customerId)
    {
        try
        {
            await LoadTransactionsAsync(customerId);
        }
        catch (Exception ex)
        {
            _dialog.Error($"Không tải được phát sinh công nợ phải thu: {ex.Message}");
        }
    }

    private async Task LoadAllTransactionsAsync()
    {
        AllTransactions.Clear();
        foreach (var summary in Summaries)
        {
            var rows = await _service.GetTransactionsByCustomerAsync(summary.CustomerId, FromDate, ToDate);
            foreach (var row in rows)
            {
                if (SelectedStatus.HasValue && row.Status != SelectedStatus.Value)
                {
                    continue;
                }

                if (OverdueOnly && row.Status != DebtStatus.Overdue)
                {
                    continue;
                }

                if (SelectedOwner != "Tất cả nhân viên" && !string.Equals(row.CreatedBy, SelectedOwner, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AllTransactions.Add(row);
            }
        }
    }

    private void RebuildAgingBuckets()
    {
        var today = DateTime.Today;
        Bucket0To30 = 0;
        Bucket31To60 = 0;
        Bucket61To90 = 0;
        Bucket90Plus = 0;

        foreach (var tx in DashboardTransactions.Where(x => x.BalanceAmount > 0 && x.DueDate.HasValue))
        {
            var dueDate = tx.DueDate ?? today;
            var days = (today - dueDate.Date).Days;
            if (days <= 30) Bucket0To30 += tx.BalanceAmount;
            else if (days <= 60) Bucket31To60 += tx.BalanceAmount;
            else if (days <= 90) Bucket61To90 += tx.BalanceAmount;
            else Bucket90Plus += tx.BalanceAmount;
        }
    }

    private void RebuildCollectionByDay()
    {
        CollectionByDay.Clear();
        ReceivableFlowByDay.Clear();

        var baseDate = DateTime.Today.AddDays(-6);
        var flowRows = new List<ReceivableDailyFlowPoint>();

        for (var i = 0; i < 7; i++)
        {
            var day = baseDate.AddDays(i).Date;
            var credit = DashboardTransactions.Where(x => x.TransactionDate.Date == day).Sum(x => x.CreditAmount);
            var debit = DashboardTransactions.Where(x => x.TransactionDate.Date == day).Sum(x => x.DebitAmount);
            flowRows.Add(new ReceivableDailyFlowPoint
            {
                DayLabel = day.ToString("dd/MM"),
                Collected = credit,
                Incurred = debit
            });

            CollectionByDay.Add(new MetricPoint
            {
                Label = day.ToString("dd/MM"),
                Value = credit
            });
        }

        var max = Math.Max(1m, flowRows.Max(x => Math.Max(x.Collected, x.Incurred)));
        foreach (var row in flowRows)
        {
            row.CollectedHeight = (double)(row.Collected / max * 110m);
            row.IncurredHeight = (double)(row.Incurred / max * 110m);
            ReceivableFlowByDay.Add(row);
        }
    }

    private void RebuildDashboardTables()
    {
        OverdueRows.Clear();
        foreach (var item in OverdueCustomers.Take(6))
        {
            OverdueRows.Add(item);
        }
        OverduePager.SetSource(OverdueRows);

        RecentCollectionRows.Clear();
        foreach (var tx in DashboardTransactions.OrderByDescending(x => x.TransactionDate).Take(8))
        {
            RecentCollectionRows.Add(tx);
        }
        CollectionPager.SetSource(RecentCollectionRows);
    }

    private ReceivableFilterDto BuildFilter() => new()
    {
        Keyword = Keyword,
        FromDate = FromDate,
        ToDate = ToDate,
        Status = SelectedStatus,
        Owner = SelectedOwner == "Tất cả nhân viên" ? string.Empty : SelectedOwner,
        OverdueOnly = OverdueOnly
    };

    private async Task LoadLookupOptionsAsync()
    {
        var currentCustomerId = SelectedSummary?.CustomerId;
        var current = SelectedOwner;

        var customers = await _service.GetCustomerOptionsAsync();
        CustomerOptions.Clear();
        foreach (var customer in customers)
        {
            CustomerOptions.Add(customer);
        }

        if (!string.IsNullOrWhiteSpace(currentCustomerId))
        {
            SelectedSummary = CustomerOptions.FirstOrDefault(x => x.CustomerId == currentCustomerId) ?? SelectedSummary;
        }

        var owners = await _service.GetOwnerOptionsAsync();
        OwnerOptions.Clear();
        OwnerOptions.Add("Tất cả nhân viên");
        foreach (var owner in owners)
        {
            OwnerOptions.Add(owner);
        }

        SelectedOwner = OwnerOptions.Contains(current) ? current : "Tất cả nhân viên";
    }
}

public sealed class ReceivableDailyFlowPoint
{
    public string DayLabel { get; set; } = string.Empty;
    public decimal Collected { get; set; }
    public decimal Incurred { get; set; }
    public double CollectedHeight { get; set; }
    public double IncurredHeight { get; set; }
}
