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
using System.Text.RegularExpressions;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class PayableViewModel : ViewModelBase
{
    private readonly IPayableService _service;
    private readonly INotificationService _notification;
    private readonly IDialogService _dialog;

    public ObservableCollection<PayableSummaryDto> Summaries { get; } = new();
    public ObservableCollection<PayableTransactionDto> Transactions { get; } = new();
    public TablePagerViewModel<PayableSummaryDto> SummaryPager { get; } = new() { ItemName = "nhà cung cấp" };
    public TablePagerViewModel<PayableSummaryDto> DueSoonPager { get; } = new() { ItemName = "nhà cung cấp" };
    public TablePagerViewModel<PayableTransactionDto> TransactionPager { get; } = new() { ItemName = "phát sinh" };
    public ObservableCollection<MetricPoint> PaymentByDay { get; } = new();
    public ObservableCollection<DebtStatus?> StatusOptions { get; } = new() { null, DebtStatus.Open, DebtStatus.PartiallyPaid, DebtStatus.Paid, DebtStatus.Overdue, DebtStatus.Cancelled };
    public ObservableCollection<string> ReportTemplates { get; } = new() { "Tổng hợp phải trả", "Chi tiết phải trả", "Lịch thanh toán", "Tạm ứng NCC" };
    public ObservableCollection<string> ReconciliationSupplierOptions { get; } = new() { "Tất cả" };
    public ObservableCollection<string> ReconciliationStatusOptions { get; } = new() { "Tất cả", "Đã đối chiếu", "Chưa đối chiếu" };
    public ObservableCollection<string> ReconciliationResultOptions { get; } = new() { "Tất cả", "Khớp", "Chênh lệch" };
    public ObservableCollection<string> ReconciliationPageSizeOptions { get; } = new() { "10 / trang", "15 / trang", "20 / trang", "50 / trang", "100 / trang" };
    private readonly List<PayableReconciliationRow> _allReconciliationRows = new();
    public ObservableCollection<PayableReconciliationRow> ReconciliationRows { get; } = new();
    public ObservableCollection<PayablePaginationButton> ReconciliationPageButtons { get; } = new();
    public ObservableCollection<PayableReportCard> ReportCards { get; } = new()
    {
        new("Báo cáo tổng hợp phải trả", "Tổng hợp phải trả theo kỳ", "\uE9D2", "#EAF2FF", "#2563EB", "Kỳ báo cáo", "Tháng 05/2026", "Cập nhật cuối", "08/05/2026 09:20"),
        new("Báo cáo chi tiết phải trả", "Chi tiết phải trả theo nhà cung cấp, chứng từ", "\uE8A5", "#EAFBF1", "#16A34A", "Kỳ báo cáo", "Tháng 05/2026", "Cập nhật cuối", "08/05/2026 09:15"),
        new("Báo cáo tuổi nợ", "Phân tích tuổi nợ phải trả", "\uE9D9", "#FFF4E8", "#F97316", "Tính đến ngày", "08/05/2026", "Cập nhật cuối", "08/05/2026 08:45"),
        new("Báo cáo thanh toán nhà cung cấp", "Thống kê thanh toán theo khoảng thời gian", "\uE823", "#ECFEFF", "#06B6D4", "Kỳ báo cáo", "Tháng 05/2026", "Cập nhật cuối", "07/05/2026 16:30"),
        new("Báo cáo quá hạn phải trả", "Danh sách khoản phải trả quá hạn", "\uE7BA", "#FFF1F2", "#EF4444", "Tính đến ngày", "08/05/2026", "Cập nhật cuối", "08/05/2026 09:05"),
        new("Báo cáo đối chiếu công nợ", "Đối chiếu công nợ với nhà cung cấp", "\uE8FB", "#F3E8FF", "#8B5CF6", "Kỳ báo cáo", "Tháng 05/2026", "Cập nhật cuối", "07/05/2026 14:10"),
        new("Báo cáo theo nhà cung cấp", "Tổng hợp phải trả theo từng nhà cung cấp", "\uE716", "#EEF4FF", "#3B82F6", "Kỳ báo cáo", "Tháng 05/2026", "Cập nhật cuối", "08/05/2026 08:30"),
        new("Báo cáo theo chứng từ", "Tổng hợp phải trả theo chứng từ", "\uE8A5", "#FFF7ED", "#F59E0B", "Kỳ báo cáo", "Tháng 05/2026", "Cập nhật cuối", "07/05/2026 17:40"),
        new("Báo cáo tạm ứng nhà cung cấp", "Tình hình tạm ứng và thanh toán tạm ứng", "\uE8F9", "#ECFDF5", "#10B981", "Kỳ báo cáo", "Tháng 05/2026", "Cập nhật cuối", "07/05/2026 09:05")
    };

    [ObservableProperty] private PayableSummaryDto? selectedSummary;
    [ObservableProperty] private PayableTransactionDto? selectedTransaction;
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private DateTime? fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? toDate = DateTime.Today;
    [ObservableProperty] private DebtStatus? selectedStatus;
    [ObservableProperty] private bool overdueOnly;
    [ObservableProperty] private string selectedReportTemplate = "Tổng hợp phải trả";
    [ObservableProperty] private DateTime? reconciliationFromDate = new DateTime(2026, 5, 1);
    [ObservableProperty] private DateTime? reconciliationToDate = new DateTime(2026, 5, 31);
    [ObservableProperty] private string selectedReconciliationSupplier = "Tất cả";
    [ObservableProperty] private string selectedReconciliationStatus = "Tất cả";
    [ObservableProperty] private string selectedReconciliationResult = "Tất cả";
    [ObservableProperty] private string selectedReconciliationPageSize = "15 / trang";
    [ObservableProperty] private int reconciliationCurrentPage = 1;
    [ObservableProperty] private PayableReconciliationRow? selectedReconciliationRow;

    [ObservableProperty] private decimal totalPayable;
    [ObservableProperty] private decimal paidAmount;
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

    public string ModuleStatus => $"Đang hoạt động • {Summaries.Count} nhà cung cấp";
    public int DebtSupplierCount => Summaries.Count(x => x.ClosingBalance > 0);
    public decimal PaymentRate => TotalPayable <= 0 ? 0 : (PaidAmount / TotalPayable) * 100m;
    public IEnumerable<PayableSummaryDto> TopPayables => Summaries.OrderByDescending(x => x.ClosingBalance).Take(5);
    public IEnumerable<PayableSummaryDto> DueSoonSuppliers => Summaries.Where(x => x.ClosingBalance > 0).OrderByDescending(x => x.OverdueAmount).Take(8);
    public IEnumerable<PayableTransactionDto> RecentPayments => Transactions.Where(x => x.DebitAmount > 0).OrderByDescending(x => x.TransactionDate).Take(7);
    public decimal SelectedTransactionPayable => SelectedTransaction?.CreditAmount ?? 0m;
    public decimal SelectedTransactionPaid => SelectedTransaction is null
        ? 0m
        : SelectedTransaction.DebitAmount > 0
            ? SelectedTransaction.DebitAmount
            : CalculatePaidForDocument(SelectedTransaction.DocumentNo);
    public decimal SelectedTransactionRemaining => SelectedTransaction is null
        ? 0m
        : SelectedTransaction.CreditAmount > 0
            ? Math.Max(0, SelectedTransaction.CreditAmount - SelectedTransactionPaid)
            : 0m;
    public decimal SelectedTransactionSupplierBalance => SelectedTransaction?.BalanceAmount ?? 0m;
    public string StatusBarText => $"Bản ghi: {Summaries.Count} • Tổng phải trả: {RemainingAmount:N0} • Cập nhật: {LastRefreshAt:HH:mm:ss} • Nguồn: SQL Server";
    public int ReconciliationTotalCount => _allReconciliationRows.Count;
    public int ReconciliationMatchedCount => _allReconciliationRows.Count(x => x.Result == "Khớp");
    public int ReconciliationDifferenceCount => _allReconciliationRows.Count(x => x.Result == "Chênh lệch");
    public int ReconciliationPendingCount => _allReconciliationRows.Count(x => x.Status == "Chưa đối chiếu");
    public int ReconciliationPageSize => ParsePageSize(SelectedReconciliationPageSize);
    public int ReconciliationTotalPages => Math.Max(1, (int)Math.Ceiling(ReconciliationTotalCount / (double)Math.Max(1, ReconciliationPageSize)));
    public string ReconciliationPageText
    {
        get
        {
            if (ReconciliationTotalCount == 0)
            {
                return "Hiển thị 0 của 0 nhà cung cấp";
            }

            var start = ((ReconciliationCurrentPage - 1) * ReconciliationPageSize) + 1;
            var end = Math.Min(ReconciliationCurrentPage * ReconciliationPageSize, ReconciliationTotalCount);
            return $"Hiển thị {start} - {end} của {ReconciliationTotalCount} nhà cung cấp";
        }
    }
    public bool IsReconciliationTabSelected => SelectedTabIndex == 4;
    public bool IsReportTabSelected => SelectedTabIndex == 6;
    public bool IsSupplierDetailVisible => !IsReportTabSelected && !IsReconciliationTabSelected;

    public PayableViewModel(IPayableService service, INotificationService notification, IDialogService dialog)
    {
        _service = service;
        _notification = notification;
        _dialog = dialog;
        _ = RefreshAsync();
    }

    partial void OnSelectedSummaryChanged(PayableSummaryDto? value)
    {
        if (value is null)
        {
            Transactions.Clear();
            TransactionPager.SetSource(Transactions);
            return;
        }

        _ = LoadTransactionsAsync(value.SupplierId);
    }

    partial void OnSelectedTransactionChanged(PayableTransactionDto? value)
    {
        OnPropertyChanged(nameof(SelectedTransactionPayable));
        OnPropertyChanged(nameof(SelectedTransactionPaid));
        OnPropertyChanged(nameof(SelectedTransactionRemaining));
        OnPropertyChanged(nameof(SelectedTransactionSupplierBalance));
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsReconciliationTabSelected));
        OnPropertyChanged(nameof(IsReportTabSelected));
        OnPropertyChanged(nameof(IsSupplierDetailVisible));
    }

    partial void OnSelectedReconciliationPageSizeChanged(string value)
    {
        ReconciliationCurrentPage = 1;
        UpdateReconciliationPage();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _service.GetDebtSummariesAsync(new PayableFilterDto
            {
                Keyword = Keyword,
                FromDate = FromDate,
                ToDate = ToDate,
                Status = SelectedStatus,
                OverdueOnly = OverdueOnly
            });

            Summaries.Clear();
            foreach (var item in data) Summaries.Add(item);
            SummaryPager.SetSource(Summaries);
            DueSoonPager.SetSource(DueSoonSuppliers);

            TotalPayable = Summaries.Sum(x => x.TotalPayable);
            PaidAmount = Summaries.Sum(x => x.TotalPaid);
            RemainingAmount = Summaries.Sum(x => x.ClosingBalance);
            OverdueAmount = Summaries.Sum(x => x.OverdueAmount);
            LastRefreshAt = DateTime.Now;

            OnPropertyChanged(nameof(ModuleStatus));
            OnPropertyChanged(nameof(DebtSupplierCount));
            OnPropertyChanged(nameof(PaymentRate));
            OnPropertyChanged(nameof(TopPayables));
            OnPropertyChanged(nameof(DueSoonSuppliers));
            OnPropertyChanged(nameof(StatusBarText));

            RebuildReconciliationOptions();
            RebuildReconciliationRows();

            if (SelectedSummary is null || Summaries.All(x => x.SupplierId != SelectedSummary.SupplierId))
            {
                SelectedSummary = Summaries.FirstOrDefault();
            }
            else
            {
                await LoadTransactionsAsync(SelectedSummary.SupplierId);
            }

            RebuildAgingBuckets();
            RebuildPaymentByDay();
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
        Keyword = string.Empty;
        FromDate = DateTime.Today.AddMonths(-1);
        ToDate = DateTime.Today;
        SelectedStatus = null;
        OverdueOnly = false;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        if (Summaries.Count == 0)
        {
            _dialog.Info("Không có nhà cung cấp để thanh toán.");
            return;
        }

        var dialog = new PayablePaymentDialog(Summaries)
        {
            Owner = App.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        await _service.CreatePaymentVoucherAsync(dialog.Result);
        _notification.Show("Đã ghi nhận thanh toán công nợ.");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AdvanceAsync()
    {
        if (Summaries.Count == 0)
        {
            _dialog.Info("Không có nhà cung cấp để tạm ứng.");
            return;
        }

        var dialog = new PayableAdvanceDialog(Summaries)
        {
            Owner = App.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        await _service.CreateAdvancePaymentAsync(dialog.Result);
        _notification.Show("Đã ghi nhận tạm ứng nhà cung cấp.");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (SelectedSummary is null)
        {
            _dialog.Info("Chọn nhà cung cấp để in đối chiếu.");
            return;
        }

        await _service.PrintDebtStatementAsync(SelectedSummary.SupplierId);
        var preview = new PrintPreviewDialog(
            $"Đối chiếu phải trả - {SelectedSummary.SupplierCode}",
            $"Nhà cung cấp: {SelectedSummary.SupplierName}\nMã: {SelectedSummary.SupplierCode}\nCòn phải trả: {SelectedSummary.ClosingBalance:N0}\nQuá hạn: {SelectedSummary.OverdueAmount:N0}\nKỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}");
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
            await _service.ExportDebtStatementAsync(summary.SupplierId);
        }

        var file = CsvExportHelper.Export(
            "payable",
            ["Mã NCC", "Tên nhà cung cấp", "Đầu kỳ", "Phát sinh", "Đã trả", "Còn phải trả", "Quá hạn"],
            Summaries.Select(x => (IReadOnlyList<string>)
            [
                x.SupplierCode,
                x.SupplierName,
                CsvExportHelper.F(x.OpeningBalance),
                CsvExportHelper.F(x.TotalPayable),
                CsvExportHelper.F(x.TotalPaid),
                CsvExportHelper.F(x.ClosingBalance),
                CsvExportHelper.F(x.OverdueAmount)
            ]).ToList());

        _dialog.Info($"Đã xuất CSV: {file}", "Xuất dữ liệu");
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        var preview = new PrintPreviewDialog(
            $"Báo cáo: {SelectedReportTemplate}",
            $"Mẫu báo cáo: {SelectedReportTemplate}\nKỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}\nNCC còn nợ: {DebtSupplierCount}\nTổng phải trả: {RemainingAmount:N0}");
        preview.Owner = App.Current.MainWindow;
        preview.ShowDialog();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task PreviewReportCardAsync(PayableReportCard? card)
    {
        if (card is not null)
        {
            SelectedReportTemplate = card.Title;
        }
        await GenerateReportAsync();
    }

    [RelayCommand]
    private async Task ExportReportCardAsync(PayableReportCard? card)
    {
        if (card is not null)
        {
            SelectedReportTemplate = card.Title;
        }
        await ExportAsync();
    }

    [RelayCommand]
    private async Task PrintReportCardAsync(PayableReportCard? card)
    {
        if (card is not null)
        {
            SelectedReportTemplate = card.Title;
        }

        var preview = new PrintPreviewDialog(
            $"In báo cáo: {SelectedReportTemplate}",
            $"Báo cáo: {SelectedReportTemplate}\nKỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}\nTổng phải trả: {RemainingAmount:N0}\nĐã thanh toán: {PaidAmount:N0}\nQuá hạn: {OverdueAmount:N0}");
        preview.Owner = App.Current.MainWindow;
        preview.ShowDialog();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void SearchReconciliation()
    {
        ReconciliationCurrentPage = 1;
        RebuildReconciliationRows();
        _notification.Show("Đã lọc danh sách đối chiếu công nợ.");
    }

    [RelayCommand]
    private void ClearReconciliationFilter()
    {
        ReconciliationFromDate = new DateTime(2026, 5, 1);
        ReconciliationToDate = new DateTime(2026, 5, 31);
        SelectedReconciliationSupplier = "Tất cả";
        SelectedReconciliationStatus = "Tất cả";
        SelectedReconciliationResult = "Tất cả";
        ReconciliationCurrentPage = 1;
        RebuildReconciliationRows();
    }

    [RelayCommand]
    private void GoToReconciliationPage(PayablePaginationButton? page)
    {
        if (page is null || page.IsEllipsis || page.PageNumber is null)
        {
            return;
        }

        ReconciliationCurrentPage = Math.Clamp(page.PageNumber.Value, 1, ReconciliationTotalPages);
        UpdateReconciliationPage();
    }

    [RelayCommand]
    private void PreviousReconciliationPage()
    {
        ReconciliationCurrentPage = Math.Max(1, ReconciliationCurrentPage - 1);
        UpdateReconciliationPage();
    }

    [RelayCommand]
    private void NextReconciliationPage()
    {
        ReconciliationCurrentPage = Math.Min(ReconciliationTotalPages, ReconciliationCurrentPage + 1);
        UpdateReconciliationPage();
    }

    [RelayCommand]
    private void ViewReconciliationDetail(PayableReconciliationRow? row)
    {
        if (row is not null)
        {
            SelectedReconciliationRow = row;
        }
    }

    [RelayCommand]
    private void AttachReconciliationFile()
    {
        _notification.Show("Chọn file xác nhận đối chiếu của nhà cung cấp.");
    }

    [RelayCommand]
    private void ConfirmSupplierReconciliation()
    {
        if (SelectedReconciliationRow is null)
        {
            _dialog.Info("Chọn nhà cung cấp cần ghi nhận xác nhận.");
            return;
        }

        _notification.Show($"Đã ghi nhận xác nhận đối chiếu cho {SelectedReconciliationRow.SupplierCode}.");
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
    private async Task FilterByPaymentPointAsync(MetricPoint? point)
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

    private async Task LoadTransactionsAsync(string supplierId)
    {
        var rows = await _service.GetTransactionsBySupplierAsync(supplierId, FromDate, ToDate);
        Transactions.Clear();
        foreach (var row in rows) Transactions.Add(row);
        TransactionPager.SetSource(Transactions);
        SelectedTransaction = Transactions.FirstOrDefault();
        OnPropertyChanged(nameof(RecentPayments));
        RebuildAgingBuckets();
        RebuildPaymentByDay();
    }

    private void RebuildAgingBuckets()
    {
        var today = DateTime.Today;
        Bucket0To30 = 0;
        Bucket31To60 = 0;
        Bucket61To90 = 0;
        Bucket90Plus = 0;

        foreach (var tx in Transactions.Where(x => x.BalanceAmount > 0 && x.DueDate.HasValue))
        {
            var dueDate = tx.DueDate ?? today;
            var days = (today - dueDate.Date).Days;
            if (days <= 30) Bucket0To30 += tx.BalanceAmount;
            else if (days <= 60) Bucket31To60 += tx.BalanceAmount;
            else if (days <= 90) Bucket61To90 += tx.BalanceAmount;
            else Bucket90Plus += tx.BalanceAmount;
        }
    }

    private void RebuildPaymentByDay()
    {
        PaymentByDay.Clear();
        var baseDate = DateTime.Today.AddDays(-6);
        var temp = new List<MetricPoint>();
        for (var i = 0; i < 7; i++)
        {
            var day = baseDate.AddDays(i).Date;
            var value = Transactions.Where(x => x.TransactionDate.Date == day).Sum(x => x.DebitAmount);
            temp.Add(new MetricPoint { Label = day.ToString("dd/MM"), Value = value });
        }

        var max = temp.Count == 0 ? 1m : Math.Max(1m, temp.Max(x => x.Value));
        foreach (var point in temp)
        {
            point.Percent = (double)(point.Value / max * 220m);
            PaymentByDay.Add(point);
        }
    }

    private void RebuildReconciliationOptions()
    {
        ReconciliationSupplierOptions.Clear();
        ReconciliationSupplierOptions.Add("Tất cả");
        foreach (var supplier in Summaries.OrderBy(x => x.SupplierName))
        {
            var display = $"{supplier.SupplierCode} - {supplier.SupplierName}";
            if (!ReconciliationSupplierOptions.Contains(display))
            {
                ReconciliationSupplierOptions.Add(display);
            }
        }

        if (!ReconciliationSupplierOptions.Contains(SelectedReconciliationSupplier))
        {
            SelectedReconciliationSupplier = "Tất cả";
        }
    }

    private void RebuildReconciliationRows()
    {
        SelectedReconciliationRow = null;
        ReconciliationRows.Clear();

        var rows = Summaries
            .OrderByDescending(x => x.ClosingBalance)
            .ThenBy(x => x.SupplierCode)
            .Select((summary, index) => BuildReconciliationRow(summary, index + 1))
            .Where(MatchesReconciliationFilter)
            .ToList();

        _allReconciliationRows.Clear();
        foreach (var row in rows)
        {
            _allReconciliationRows.Add(row);
        }

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Index = i + 1;
        }

        ReconciliationCurrentPage = Math.Clamp(ReconciliationCurrentPage, 1, ReconciliationTotalPages);
        UpdateReconciliationPage();
        OnPropertyChanged(nameof(ReconciliationTotalCount));
        OnPropertyChanged(nameof(ReconciliationMatchedCount));
        OnPropertyChanged(nameof(ReconciliationDifferenceCount));
        OnPropertyChanged(nameof(ReconciliationPendingCount));
        OnPropertyChanged(nameof(ReconciliationTotalPages));
    }

    private void UpdateReconciliationPage()
    {
        SelectedReconciliationRow = null;
        ReconciliationRows.Clear();

        ReconciliationCurrentPage = Math.Clamp(ReconciliationCurrentPage, 1, ReconciliationTotalPages);
        var startIndex = (ReconciliationCurrentPage - 1) * ReconciliationPageSize;
        foreach (var row in _allReconciliationRows.Skip(startIndex).Take(ReconciliationPageSize))
        {
            ReconciliationRows.Add(row);
        }

        SelectedReconciliationRow = ReconciliationRows.FirstOrDefault();
        OnPropertyChanged(nameof(ReconciliationPageSize));
        OnPropertyChanged(nameof(ReconciliationTotalPages));
        OnPropertyChanged(nameof(ReconciliationPageText));
        RebuildReconciliationPageButtons();
    }

    private static int ParsePageSize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 10;
        }

        var numberPart = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(numberPart, out var result) && result > 0 ? result : 10;
    }

    private void RebuildReconciliationPageButtons()
    {
        ReconciliationPageButtons.Clear();

        var total = ReconciliationTotalPages;
        var current = ReconciliationCurrentPage;
        var pages = new List<int?>();

        if (total <= 8)
        {
            pages.AddRange(Enumerable.Range(1, total).Select(x => (int?)x));
        }
        else if (current <= 5)
        {
            pages.AddRange(new int?[] { 1, 2, 3, 4, 5, null, total });
        }
        else if (current >= total - 4)
        {
            pages.AddRange(new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total });
        }
        else
        {
            pages.AddRange(new int?[] { 1, null, current - 1, current, current + 1, null, total });
        }

        foreach (var page in pages)
        {
            ReconciliationPageButtons.Add(page is null
                ? new PayablePaginationButton("...", null, false, true)
                : new PayablePaginationButton(page.Value.ToString(CultureInfo.InvariantCulture), page.Value, page.Value == current, false));
        }
    }

    private static PayableReconciliationRow BuildReconciliationRow(PayableSummaryDto summary, int index)
    {
        var status = summary.LastTransactionDate.HasValue ? "Đã đối chiếu" : "Chưa đối chiếu";
        return new PayableReconciliationRow
        {
            Index = index,
            SupplierCode = summary.SupplierCode,
            SupplierName = summary.SupplierName,
            SystemBalance = Math.Max(0, summary.ClosingBalance),
            SupplierConfirmedBalance = Math.Max(0, summary.ClosingBalance),
            Status = status,
            Period = "Tháng 05/2026",
            ReconciledAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            ReconciledBy = "Administrator"
        };
    }

    private bool MatchesReconciliationFilter(PayableReconciliationRow row)
    {
        if (SelectedReconciliationSupplier != "Tất cả" &&
            !SelectedReconciliationSupplier.StartsWith(row.SupplierCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (SelectedReconciliationStatus != "Tất cả" &&
            !string.Equals(row.Status, SelectedReconciliationStatus, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (SelectedReconciliationResult != "Tất cả" &&
            !string.Equals(row.Result, SelectedReconciliationResult, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private decimal CalculatePaidForDocument(string documentNo)
    {
        if (string.IsNullOrWhiteSpace(documentNo))
        {
            return 0m;
        }

        var payableDocs = Transactions
            .Where(x => x.CreditAmount > 0)
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.DocumentNo)
            .ToList();
        var remainingByDoc = payableDocs.ToDictionary(x => x.DocumentNo, x => Math.Max(0, x.CreditAmount), StringComparer.OrdinalIgnoreCase);
        var paidByDoc = payableDocs.ToDictionary(x => x.DocumentNo, _ => 0m, StringComparer.OrdinalIgnoreCase);

        foreach (var payment in Transactions.Where(x => x.DebitAmount > 0).OrderBy(x => x.TransactionDate).ThenBy(x => x.DocumentNo))
        {
            var remainingPayment = Math.Max(0, payment.DebitAmount);
            foreach (var refDocNo in ExtractReferencedDocumentNos(payment.Description))
            {
                if (remainingPayment <= 0)
                {
                    break;
                }

                if (!remainingByDoc.TryGetValue(refDocNo, out var outstanding) || outstanding <= 0)
                {
                    continue;
                }

                var allocated = Math.Min(outstanding, remainingPayment);
                remainingByDoc[refDocNo] = outstanding - allocated;
                paidByDoc[refDocNo] += allocated;
                remainingPayment -= allocated;
            }

            foreach (var doc in payableDocs)
            {
                if (remainingPayment <= 0)
                {
                    break;
                }

                var outstanding = remainingByDoc[doc.DocumentNo];
                if (outstanding <= 0)
                {
                    continue;
                }

                var allocated = Math.Min(outstanding, remainingPayment);
                remainingByDoc[doc.DocumentNo] = outstanding - allocated;
                paidByDoc[doc.DocumentNo] += allocated;
                remainingPayment -= allocated;
            }
        }

        return paidByDoc.TryGetValue(documentNo, out var paid) ? paid : 0m;
    }

    private static IReadOnlyList<string> ExtractReferencedDocumentNos(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return [];
        }

        return Regex.Matches(description, @"\b[A-Z]{2,6}-[A-Z0-9]+\b", RegexOptions.IgnoreCase)
            .Where(x => x.Success)
            .Select(x => x.Value.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public sealed record PayableReportCard(
    string Title,
    string Description,
    string IconGlyph,
    string IconBackground,
    string AccentBrush,
    string PrimaryLabel,
    string PrimaryValue,
    string SecondaryLabel,
    string SecondaryValue);

public sealed class PayableReconciliationRow
{
    public int Index { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal SystemBalance { get; set; }
    public decimal SupplierConfirmedBalance { get; set; }
    public decimal Difference => Math.Abs(SystemBalance - SupplierConfirmedBalance);
    public string Result => Difference == 0 ? "Khớp" : "Chênh lệch";
    public string Status { get; set; } = "Chưa đối chiếu";
    public string Period { get; set; } = "Tháng 05/2026";
    public string ReconciledAt { get; set; } = string.Empty;
    public string ReconciledBy { get; set; } = string.Empty;
    public string AvatarText => string.IsNullOrWhiteSpace(SupplierCode) ? "N" : SupplierCode[..1].ToUpperInvariant();
    public string Note => Result == "Khớp"
        ? "Số dư hệ thống khớp với số dư nhà cung cấp xác nhận."
        : "Chênh lệch do hóa đơn nhập hàng chưa được nhà cung cấp ghi nhận.";
}

public sealed record PayablePaginationButton(
    string Label,
    int? PageNumber,
    bool IsCurrent,
    bool IsEllipsis);
