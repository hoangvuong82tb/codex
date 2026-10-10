using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ASMILE.BO.App.ViewModels;

// ═══════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════

public sealed class ArAgingItemDto
{
    public string PartnerId { get; set; } = "";
    public string PartnerCode { get; set; } = "";
    public string PartnerName { get; set; } = "";
    public string PartnerType { get; set; } = "Company";
    public string PartnerTypeLabel => PartnerType switch
    {
        "Company" => "Company", "TravelAgent" => "Travel Agent", "OTA" => "OTA",
        "Government" => "Government", "Corporate" => "Corporate", "Guest" => "Guest",
        _ => PartnerType
    };
    public string? GuestName { get; set; }
    public string? InvoiceNo { get; set; }
    public string? ReservationNo { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Overdue1To30 { get; set; }
    public decimal Overdue31To60 { get; set; }
    public decimal Overdue61To90 { get; set; }
    public decimal Overdue91To120 { get; set; }
    public decimal Overdue121To180 { get; set; }
    public decimal Overdue181To365 { get; set; }
    public decimal OverdueOver365 { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal AvailableCredit => CreditLimit - OutstandingAmount;
    public string? CollectorName { get; set; }
    public string AgingStatus { get; set; } = "Current";
    public string AgingStatusLabel => AgingStatus switch
    {
        "Current" => "Current", "Due" => "Đến hạn", "Overdue" => "Quá hạn",
        "Blocked" => "Bị khóa", "Collection" => "Đang thu", "Legal" => "Pháp lý",
        "Closed" => "Đã đóng", _ => AgingStatus
    };
    public string AgingStatusColor => AgingStatus switch
    {
        "Current" => "#16A34A", "Due" => "#2563EB", "Overdue" => "#DC2626",
        "Blocked" => "#7C3AED", "Collection" => "#D97706", "Legal" => "#E32F70",
        "Closed" => "#64748B", _ => "#64748B"
    };
    public string AgingStatusBg => AgingStatus switch
    {
        "Current" => "#DCFCE7", "Due" => "#DBEAFE", "Overdue" => "#FEE2E2",
        "Blocked" => "#F3E8FF", "Collection" => "#FFF7ED", "Legal" => "#FCE7F3",
        "Closed" => "#F1F5F9", _ => "#F8FAFC"
    };
    public string RiskLevel { get; set; } = "Low";
    public string RiskLevelColor => RiskLevel switch
    {
        "Low" => "#16A34A", "Medium" => "#D97706", "High" => "#DC2626", "Critical" => "#991B1B", _ => "#64748B"
    };
    public string RiskLevelBg => RiskLevel switch
    {
        "Low" => "#DCFCE7", "Medium" => "#FFF7ED", "High" => "#FEE2E2", "Critical" => "#FEF2F2", _ => "#F1F5F9"
    };
    public string? MarketCode { get; set; }
    public string? SourceCode { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? CountryCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxCode { get; set; }
    public int? AgeDays { get; set; }
    public DateTime? OldestInvoiceDate { get; set; }
    public DateTime? NewestInvoiceDate { get; set; }
    public decimal? AverageAgeDays { get; set; }
    public decimal? LastPaymentAmount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public string? Remark { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsOverCredit => CreditLimit > 0 && OutstandingAmount > CreditLimit;

    public List<ArAgingInvoiceDto> Invoices { get; set; } = new();
    public List<ArAgingCollectionDto> Collections { get; set; } = new();
    public List<ArAgingPaymentDto> Payments { get; set; } = new();
    public List<ArAgingTimelineEntry> Timeline { get; set; } = new();
    public List<ArAgingDocumentDto> Documents { get; set; } = new();
    public List<ArAgingAuditEntry> AuditLog { get; set; } = new();
}

public sealed class ArAgingInvoiceDto
{
    public string InvoiceNo { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public int AgeDays { get; set; }
    public decimal Amount { get; set; }
    public decimal Outstanding { get; set; }
    public string Status { get; set; } = "Open";
    public string StatusColor => Status switch { "Paid" => "#16A34A", "Open" => "#F59E0B", "Overdue" => "#DC2626", "Cancelled" => "#64748B", _ => "#64748B" };
    public string StatusBg => Status switch { "Paid" => "#DCFCE7", "Open" => "#FFF7ED", "Overdue" => "#FEE2E2", "Cancelled" => "#F1F5F9", _ => "#F8FAFC" };
}

public sealed class ArAgingCollectionDto
{
    public string CollectorName { get; set; } = "";
    public DateTime? ReminderDate { get; set; }
    public DateTime? PhoneCallDate { get; set; }
    public DateTime? EmailDate { get; set; }
    public DateTime? MeetingDate { get; set; }
    public DateTime? PromiseToPayDate { get; set; }
    public decimal? PromiseAmount { get; set; }
    public string CollectionStatus { get; set; } = "Pending";
    public string CollectionStatusColor => CollectionStatus switch
    {
        "Pending" => "#F59E0B", "InProgress" => "#2563EB", "Promised" => "#7C3AED",
        "PartialPaid" => "#D97706", "Completed" => "#16A34A", "Failed" => "#DC2626", _ => "#64748B"
    };
    public string CollectionStatusLabel => CollectionStatus switch
    {
        "Pending" => "Chờ xử lý", "InProgress" => "Đang thu", "Promised" => "Đã hứa trả",
        "PartialPaid" => "Trả 1 phần", "Completed" => "Hoàn thành", "Failed" => "Thất bại", _ => CollectionStatus
    };
    public string? Remark { get; set; }
}

public sealed class ArAgingPaymentDto
{
    public string PaymentNo { get; set; } = "";
    public string ReceiptNo { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = "";
    public string Status { get; set; } = "Completed";
}

public sealed class ArAgingTimelineEntry
{
    public string Event { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#2563EB";
}

public sealed class ArAgingDocumentDto
{
    public string DocType { get; set; } = "";
    public string DocLabel { get; set; } = "";
    public DateTime? CreatedDate { get; set; }
    public string? Url { get; set; }
    public string Icon => DocType switch
    {
        "InvoicePdf" => "📄", "Statement" => "📊", "Contract" => "📝",
        "ReminderLetter" => "📨", "Email" => "✉️", "Receipt" => "🧾", _ => "📎"
    };
}

public sealed class ArAgingAuditEntry
{
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Device { get; set; } = "";
}

public sealed class ArAgingKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#16A34A";
    public string BgColor { get; set; } = "#DCFCE7";
    public string? Tooltip { get; set; }
}

public sealed class ArAgingTreeItem
{
    public string Label { get; set; } = "";
    public string Count { get; set; } = "";
    public string BucketKey { get; set; } = "";
    public bool IsSelected { get; set; }
}

public sealed class ArAgingBucketConfig
{
    public string Label { get; set; } = "";
    public int FromDay { get; set; }
    public int ToDay { get; set; }
    public string Color { get; set; } = "#16A34A";
    public string BgColor { get; set; } = "#DCFCE7";
    public bool IsEnabled { get; set; } = true;
}

public sealed class ArAgingChartData
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public string Color { get; set; } = "#2563EB";
}

public sealed class CollectorPerformanceDto
{
    public string CollectorName { get; set; } = "";
    public int TotalAccounts { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal RecoveryRate { get; set; }
    public int OverdueUnresolved { get; set; }
    public string PerformanceColor => RecoveryRate >= 80 ? "#16A34A" : RecoveryRate >= 50 ? "#D97706" : "#DC2626";
}

// ═══════════════════════════════════════════════════════════════
// VIEWMODEL
// ═══════════════════════════════════════════════════════════════

public sealed partial class ArAgingViewModel : ObservableObject
{
    private readonly IReceivableService? _receivableService;
    private readonly IEmailDispatchService? _emailDispatchService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IReportEngine? _reportEngine;
    private IReadOnlyList<ReceivableTransactionDto> _transactions = Array.Empty<ReceivableTransactionDto>();
    private bool _isInitialized;

    #region Observable Collections

    public ObservableCollection<ArAgingKpi> DashboardKpis { get; } = new();
    public ObservableCollection<ArAgingTreeItem> TreeItems { get; } = new();
    public ObservableCollection<ArAgingItemDto> AllItems { get; } = new();
    public ObservableCollection<ArAgingItemDto> FilteredItems { get; } = new();
    public ObservableCollection<ArAgingBucketConfig> BucketConfigs { get; } = new();
    public ObservableCollection<CollectorPerformanceDto> CollectorPerformances { get; } = new();
    public ObservableCollection<ArAgingChartData> AgingDistribution { get; } = new();
    public ObservableCollection<ArAgingChartData> TopDebtors { get; } = new();
    public ObservableCollection<ArAgingChartData> OutstandingTrend { get; } = new();
    public ObservableCollection<ArAgingChartData> CollectionTrend { get; } = new();
    public ObservableCollection<string> StatusFilters { get; } = new();
    public ObservableCollection<string> PartnerTypeFilters { get; } = new();
    public ObservableCollection<string> MarketFilters { get; } = new();
    public ObservableCollection<string> CollectorFilters { get; } = new();
    public ObservableCollection<string> CurrencyFilters { get; } = new();
    public ObservableCollection<string> CountryFilters { get; } = new();
    public ObservableCollection<string> BucketFilters { get; } = new();
    public ObservableCollection<string> RiskFilters { get; } = new();

    #endregion

    #region Selected Properties

    [ObservableProperty] private ArAgingItemDto? _selectedItem;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterPartnerType;
    [ObservableProperty] private string? _filterMarket;
    [ObservableProperty] private string? _filterCollector;
    [ObservableProperty] private string? _filterCurrency;
    [ObservableProperty] private string? _filterCountry;
    [ObservableProperty] private string? _filterBucket;
    [ObservableProperty] private string? _filterRisk;
    [ObservableProperty] private string? _filterAsOfDate;
    [ObservableProperty] private bool _filterOverdueOnly;
    [ObservableProperty] private bool _filterOverCredit;
    [ObservableProperty] private bool _isGridView = true;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _infoMessage;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 50;
    [ObservableProperty] private int _totalItems;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _pagerSummary = "";

    #endregion

    #region Inspector Tab Visibility

    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsInvoicesTab => SelectedDetailTab == 1;
    public bool IsCollectionTab => SelectedDetailTab == 2;
    public bool IsPaymentTab => SelectedDetailTab == 3;
    public bool IsHistoryTab => SelectedDetailTab == 4;
    public bool IsDocumentsTab => SelectedDetailTab == 5;

    #endregion

    public ArAgingViewModel()
    {
        var services = (System.Windows.Application.Current as ASMILE.BO.App.App)?.Services;
        _receivableService = services?.GetService(typeof(IReceivableService)) as IReceivableService;
        _emailDispatchService = services?.GetService(typeof(IEmailDispatchService)) as IEmailDispatchService;
        _executionContext = services?.GetService(typeof(IFrontOfficeExecutionContextAccessor)) as IFrontOfficeExecutionContextAccessor;
        _reportEngine = services?.GetService(typeof(IReportEngine)) as IReportEngine;
        InitializeEmptyState();
    }

    public ArAgingViewModel(
        IReceivableService? receivableService,
        IFrontOfficeExecutionContextAccessor? executionContext = null,
        IEmailDispatchService? emailDispatchService = null,
        IReportEngine? reportEngine = null)
    {
        _receivableService = receivableService;
        _executionContext = executionContext;
        _emailDispatchService = emailDispatchService;
        _reportEngine = reportEngine;
        InitializeEmptyState();
    }

    private void InitializeEmptyState()
    {
        BuildFilters();
        BuildTreeItems();
        BuildDashboardKpis();
        BuildBucketConfigs();
        BuildCollectorPerformances();
        BuildChartData();
        ApplyFilters();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        IsLoading = true;
        ErrorMessage = null;
        InfoMessage = null;

        try
        {
            if (_receivableService is null)
            {
                SetDataUnavailable("Chưa kết nối được dịch vụ công nợ thật. Màn hình không dùng dữ liệu mẫu.");
                return;
            }

            var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
            var filter = new ReceivableFilterDto { PropertyId = propertyId };
            var summaries = await _receivableService.GetDebtSummariesAsync(filter, cancellationToken);
            _transactions = await _receivableService.GetTransactionsAsync(filter, cancellationToken);

            AllItems.Clear();
            foreach (var summary in summaries)
            {
                var customerTransactions = _transactions
                    .Where(x => string.Equals(x.CustomerId, summary.CustomerId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.TransactionDate)
                    .ToList();
                AllItems.Add(MapRealItem(summary, customerTransactions));
            }

            BuildFilters();
            BuildBucketConfigs();
            BuildCollectorPerformances();
            BuildChartData();
            ApplyFilters();

            if (AllItems.Count == 0)
                InfoMessage = "Chưa có dữ liệu công nợ thật trong phạm vi hiện tại.";
        }
        catch (OperationCanceledException)
        {
            _isInitialized = false;
            throw;
        }
        catch (Exception ex)
        {
            SetDataUnavailable($"Không tải được dữ liệu công nợ thật: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetDataUnavailable(string message)
    {
        AllItems.Clear();
        FilteredItems.Clear();
        _transactions = Array.Empty<ReceivableTransactionDto>();
        ErrorMessage = message;
        BuildFilters();
        BuildTreeItems();
        BuildDashboardKpis();
        BuildBucketConfigs();
        BuildCollectorPerformances();
        BuildChartData();
        ApplyFilters();
    }

    private static ArAgingItemDto MapRealItem(
        ReceivableSummaryDto summary,
        IReadOnlyList<ReceivableTransactionDto> transactions)
    {
        var today = DateTime.Today;
        var outstanding = Math.Max(0m, summary.ClosingBalance);
        var openTransactions = transactions.Where(x => x.BalanceAmount > 0).ToList();
        var current = openTransactions
            .Where(x => !x.DueDate.HasValue || x.DueDate.Value.Date >= today)
            .Sum(x => x.BalanceAmount);
        var overdue = openTransactions
            .Where(x => x.DueDate.HasValue && x.DueDate.Value.Date < today)
            .GroupBy(x => Math.Max(1, (today - x.DueDate!.Value.Date).Days))
            .ToDictionary(x => x.Key, x => x.Sum(t => t.BalanceAmount));

        decimal Bucket(int from, int to) => overdue
            .Where(x => x.Key >= from && x.Key <= to)
            .Sum(x => x.Value);

        var invoices = transactions
            .Where(x => x.DebitAmount > 0)
            .Select(x => new ArAgingInvoiceDto
            {
                InvoiceNo = x.DocumentNo,
                IssueDate = x.TransactionDate,
                DueDate = x.DueDate ?? x.TransactionDate,
                AgeDays = x.DueDate.HasValue ? Math.Max(0, (today - x.DueDate.Value.Date).Days) : 0,
                Amount = x.DebitAmount,
                Outstanding = Math.Max(0m, x.BalanceAmount),
                Status = x.Status == DebtStatus.Paid ? "Paid" : x.Status == DebtStatus.Overdue ? "Overdue" : "Open"
            })
            .ToList();

        var payments = transactions
            .Where(x => x.CreditAmount > 0)
            .Select(x => new ArAgingPaymentDto
            {
                PaymentNo = x.DocumentNo,
                ReceiptNo = x.DocumentNo,
                PaymentDate = x.TransactionDate,
                Amount = x.CreditAmount,
                Reference = x.Description,
                Status = x.Status == DebtStatus.Cancelled ? "Cancelled" : "Completed"
            })
            .ToList();

        var first = transactions.FirstOrDefault();
        var oldest = transactions.Count == 0 ? (DateTime?)null : transactions.Min(x => x.TransactionDate);
        var newest = transactions.Count == 0 ? (DateTime?)null : transactions.Max(x => x.TransactionDate);
        var status = summary.Status switch
        {
            DebtStatus.Paid => "Closed",
            DebtStatus.Overdue => "Overdue",
            _ when summary.OverdueAmount > 0 => "Overdue",
            _ when openTransactions.Any(x => x.DueDate.HasValue && x.DueDate.Value.Date <= today.AddDays(7)) => "Due",
            _ => "Current"
        };

        return new ArAgingItemDto
        {
            PartnerId = summary.CustomerId,
            PartnerCode = summary.CustomerCode,
            PartnerName = summary.CustomerName,
            PartnerType = "Company",
            InvoiceNo = invoices.FirstOrDefault()?.InvoiceNo,
            IssueDate = oldest,
            DueDate = openTransactions.OrderBy(x => x.DueDate ?? DateTime.MaxValue).FirstOrDefault()?.DueDate,
            CurrentAmount = current > 0 ? current : Math.Max(0m, outstanding - summary.OverdueAmount),
            Overdue1To30 = Bucket(1, 30),
            Overdue31To60 = Bucket(31, 60),
            Overdue61To90 = Bucket(61, 90),
            Overdue91To120 = Bucket(91, 120),
            Overdue121To180 = Bucket(121, 180),
            Overdue181To365 = Bucket(181, 365),
            OverdueOver365 = Bucket(366, int.MaxValue),
            OutstandingAmount = outstanding,
            AgingStatus = status,
            RiskLevel = outstanding <= 0 ? "Low" : summary.OverdueAmount > 0 ? "High" : "Low",
            CurrencyCode = first?.Currency ?? "VND",
            AgeDays = openTransactions.Count == 0 ? 0 : openTransactions.Max(x => x.DueDate.HasValue ? Math.Max(0, (today - x.DueDate.Value.Date).Days) : 0),
            OldestInvoiceDate = oldest,
            NewestInvoiceDate = newest,
            AverageAgeDays = openTransactions.Count == 0 ? 0 : (decimal)openTransactions.Average(x => x.DueDate.HasValue ? Math.Max(0, (today - x.DueDate.Value.Date).Days) : 0),
            LastPaymentAmount = payments.LastOrDefault()?.Amount,
            LastPaymentDate = payments.LastOrDefault()?.PaymentDate,
            Invoices = invoices,
            Payments = payments,
            Timeline = transactions.Select(x => new ArAgingTimelineEntry
            {
                Event = x.CreditAmount > 0 ? "Thanh toán" : "Ghi nhận công nợ",
                Timestamp = x.TransactionDate,
                Detail = string.IsNullOrWhiteSpace(x.Description) ? x.DocumentNo : x.Description,
                Color = x.CreditAmount > 0 ? "#16A34A" : "#2563EB"
            }).ToList()
        };
    }

    #region Filters

    private void BuildFilters()
    {
        StatusFilters.Clear(); foreach (var s in new[] { "Tất cả", "Current", "Đến hạn", "Quá hạn", "Bị khóa", "Đang thu", "Pháp lý", "Đã đóng" }) StatusFilters.Add(s);
        PartnerTypeFilters.Clear(); foreach (var s in new[] { "Tất cả", "Company", "Corporate", "Travel Agent", "OTA", "Government", "Guest" }) PartnerTypeFilters.Add(s);
        MarketFilters.Clear(); foreach (var s in new[] { "Tất cả" }.Concat(AllItems.Select(x => x.MarketCode).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct().OrderBy(x => x))) MarketFilters.Add(s);
        CollectorFilters.Clear(); foreach (var s in new[] { "Tất cả" }.Concat(AllItems.Select(x => x.CollectorName).Where(x => x != null).Cast<string>().Distinct().OrderBy(x => x))) CollectorFilters.Add(s);
        CurrencyFilters.Clear(); foreach (var s in new[] { "Tất cả", "VND", "USD", "EUR", "KRW", "JPY", "CNY" }) CurrencyFilters.Add(s);
        CountryFilters.Clear(); foreach (var s in new[] { "Tất cả" }.Concat(AllItems.Select(x => x.CountryCode).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct().OrderBy(x => x))) CountryFilters.Add(s);
        BucketFilters.Clear(); foreach (var s in new[] { "Tất cả", "Current", "1-30", "31-60", "61-90", "91-120", "121-180", "181-365", ">365" }) BucketFilters.Add(s);
        RiskFilters.Clear(); foreach (var s in new[] { "Tất cả", "Low", "Medium", "High", "Critical" }) RiskFilters.Add(s);
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        var query = AllItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var kw = SearchText.Trim().ToLower();
            query = query.Where(x =>
                x.PartnerName.ToLower().Contains(kw) ||
                x.PartnerCode.ToLower().Contains(kw) ||
                (x.GuestName?.ToLower().Contains(kw) ?? false) ||
                (x.InvoiceNo?.ToLower().Contains(kw) ?? false) ||
                (x.ReservationNo?.ToLower().Contains(kw) ?? false) ||
                (x.Phone?.Contains(kw) ?? false) ||
                (x.Email?.ToLower().Contains(kw) ?? false) ||
                (x.CollectorName?.ToLower().Contains(kw) ?? false) ||
                (x.TaxCode?.Contains(kw) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(FilterStatus) && FilterStatus != "Tất cả")
        {
            var statusMap = new Dictionary<string, string>
            {
                ["Current"] = "Current", ["Đến hạn"] = "Due", ["Quá hạn"] = "Overdue",
                ["Bị khóa"] = "Blocked", ["Đang thu"] = "Collection", ["Pháp lý"] = "Legal", ["Đã đóng"] = "Closed"
            };
            if (statusMap.TryGetValue(FilterStatus, out var mapped))
                query = query.Where(x => x.AgingStatus == mapped);
        }

        if (!string.IsNullOrWhiteSpace(FilterPartnerType) && FilterPartnerType != "Tất cả")
        {
            var typeMap = new Dictionary<string, string>
            {
                ["Travel Agent"] = "TravelAgent"
            };
            var type = typeMap.TryGetValue(FilterPartnerType, out var t) ? t : FilterPartnerType;
            query = query.Where(x => x.PartnerType == type);
        }

        if (!string.IsNullOrWhiteSpace(FilterMarket) && FilterMarket != "Tất cả")
            query = query.Where(x => x.MarketCode == FilterMarket);
        if (!string.IsNullOrWhiteSpace(FilterCollector) && FilterCollector != "Tất cả")
            query = query.Where(x => x.CollectorName == FilterCollector);
        if (!string.IsNullOrWhiteSpace(FilterCurrency) && FilterCurrency != "Tất cả")
            query = query.Where(x => x.CurrencyCode == FilterCurrency);
        if (!string.IsNullOrWhiteSpace(FilterCountry) && FilterCountry != "Tất cả")
            query = query.Where(x => x.CountryCode == FilterCountry);
        if (FilterOverdueOnly)
            query = query.Where(x => x.AgingStatus == "Overdue" || x.AgingStatus == "Blocked");
        if (FilterOverCredit)
            query = query.Where(x => x.IsOverCredit);

        if (!string.IsNullOrWhiteSpace(FilterBucket) && FilterBucket != "Tất cả")
        {
            query = FilterBucket switch
            {
                "Current" => query.Where(x => x.CurrentAmount > 0),
                "1-30" => query.Where(x => x.Overdue1To30 > 0),
                "31-60" => query.Where(x => x.Overdue31To60 > 0),
                "61-90" => query.Where(x => x.Overdue61To90 > 0),
                "91-120" => query.Where(x => x.Overdue91To120 > 0),
                "121-180" => query.Where(x => x.Overdue121To180 > 0),
                "181-365" => query.Where(x => x.Overdue181To365 > 0),
                ">365" => query.Where(x => x.OverdueOver365 > 0),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(FilterRisk) && FilterRisk != "Tất cả")
            query = query.Where(x => x.RiskLevel == FilterRisk);

        var filtered = query.OrderByDescending(x => x.OutstandingAmount).ToList();
        TotalItems = filtered.Count;
        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        if (CurrentPage > TotalPages) CurrentPage = Math.Max(1, TotalPages);

        // Rebuild the shared pager whenever the result set changes.  Building
        // it only from the view's Loaded event races the initial data load and
        // leaves the footer without page buttons after filtering.
        BuildPagerItems();

        var paged = filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        FilteredItems.Clear();
        foreach (var item in paged)
            FilteredItems.Add(item);

        UpdatePagerSummary();
        BuildDashboardKpis();
        BuildTreeItems();
    }

    partial void OnSearchTextChanged(string value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterStatusChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterPartnerTypeChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterMarketChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterCollectorChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterCurrencyChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterCountryChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterBucketChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterRiskChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterOverdueOnlyChanged(bool value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterOverCreditChanged(bool value) => ApplyFiltersCommand.Execute(null);
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFiltersCommand.Execute(null); }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = "";
        FilterStatus = "Tất cả";
        FilterPartnerType = "Tất cả";
        FilterMarket = "Tất cả";
        FilterCollector = "Tất cả";
        FilterCurrency = "Tất cả";
        FilterCountry = "Tất cả";
        FilterBucket = "Tất cả";
        FilterRisk = "Tất cả";
        FilterOverdueOnly = false;
        FilterOverCredit = false;
        CurrentPage = 1;
        ApplyFiltersCommand.Execute(null);
    }

    private void UpdatePagerSummary()
    {
        var start = TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
        var end = Math.Min(CurrentPage * PageSize, TotalItems);
        PagerSummary = $"Hiển thị {start:N0}–{end:N0} / {TotalItems:N0}";
    }

    #endregion

    #region KPI Dashboard

    private void BuildDashboardKpis()
    {
        DashboardKpis.Clear();
        var total = AllItems.Sum(x => x.OutstandingAmount);
        var current = AllItems.Sum(x => x.CurrentAmount);
        var d1to30 = AllItems.Sum(x => x.Overdue1To30);
        var d31to60 = AllItems.Sum(x => x.Overdue31To60);
        var d61to90 = AllItems.Sum(x => x.Overdue61To90);
        var d91to120 = AllItems.Sum(x => x.Overdue91To120);
        var d121to180 = AllItems.Sum(x => x.Overdue121To180);
        var d181plus = AllItems.Sum(x => x.Overdue181To365 + x.OverdueOver365);
        var overdue = d1to30 + d31to60 + d61to90 + d91to120 + d121to180 + d181plus;
        var due = AllItems.Where(x => x.AgingStatus == "Due").Sum(x => x.OutstandingAmount);
        var collectedToday = AllItems.Sum(x => x.Payments?.Where(p => p.PaymentDate == DateTime.Today).Sum(p => p.Amount) ?? 0);
        var recoveryRate = total > 0 ? (total - overdue) / total * 100 : 100;

        DashboardKpis.Add(new ArAgingKpi { Label = "Tổng công nợ", Value = $"{total:N0} ₫", Color = "#0F172A", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new ArAgingKpi { Label = "Current", Value = $"{current:N0} ₫", Color = "#16A34A", BgColor = "#DCFCE7" });
        DashboardKpis.Add(new ArAgingKpi { Label = "1–30 ngày", Value = $"{d1to30:N0} ₫", Color = "#2563EB", BgColor = "#DBEAFE" });
        DashboardKpis.Add(new ArAgingKpi { Label = "31–60 ngày", Value = $"{d31to60:N0} ₫", Color = "#D97706", BgColor = "#FFF7ED" });
        DashboardKpis.Add(new ArAgingKpi { Label = "61–90 ngày", Value = $"{d61to90:N0} ₫", Color = "#DC2626", BgColor = "#FEE2E2" });
        DashboardKpis.Add(new ArAgingKpi { Label = "91–120 ngày", Value = $"{d91to120:N0} ₫", Color = "#991B1B", BgColor = "#FEF2F2" });
        DashboardKpis.Add(new ArAgingKpi { Label = "121–180 ngày", Value = $"{d121to180:N0} ₫", Color = "#7C3AED", BgColor = "#F3E8FF" });
        DashboardKpis.Add(new ArAgingKpi { Label = ">180 ngày", Value = $"{d181plus:N0} ₫", Color = "#E32F70", BgColor = "#FCE7F3" });
        DashboardKpis.Add(new ArAgingKpi { Label = "Quá hạn", Value = $"{overdue:N0} ₫", Color = "#DC2626", BgColor = "#FEE2E2" });
        DashboardKpis.Add(new ArAgingKpi { Label = "Đến hạn", Value = $"{due:N0} ₫", Color = "#2563EB", BgColor = "#DBEAFE" });
        DashboardKpis.Add(new ArAgingKpi { Label = "Đã thu hôm nay", Value = $"{collectedToday:N0} ₫", Color = "#16A34A", BgColor = "#DCFCE7" });
        DashboardKpis.Add(new ArAgingKpi { Label = "Tỷ lệ thu hồi", Value = $"{recoveryRate:F1}%", Color = "#2563EB", BgColor = "#DBEAFE" });
    }

    #endregion

    #region Tree Navigation

    private void BuildTreeItems()
    {
        TreeItems.Clear();
        var total = AllItems.Count;
        var current = AllItems.Count(x => x.CurrentAmount > 0);
        var d1to30 = AllItems.Count(x => x.Overdue1To30 > 0);
        var d31to60 = AllItems.Count(x => x.Overdue31To60 > 0);
        var d61to90 = AllItems.Count(x => x.Overdue61To90 > 0);
        var d91to120 = AllItems.Count(x => x.Overdue91To120 > 0);
        var d121to180 = AllItems.Count(x => x.Overdue121To180 > 0);
        var d181plus = AllItems.Count(x => x.Overdue181To365 > 0 || x.OverdueOver365 > 0);
        var overCredit = AllItems.Count(x => x.IsOverCredit);
        var blocked = AllItems.Count(x => x.IsBlocked);

        TreeItems.Add(new ArAgingTreeItem { Label = "Tất cả", Count = $"{total}", BucketKey = "All", IsSelected = true });
        TreeItems.Add(new ArAgingTreeItem { Label = "Current", Count = $"{current}", BucketKey = "Current" });
        TreeItems.Add(new ArAgingTreeItem { Label = "1–30 ngày", Count = $"{d1to30}", BucketKey = "1-30" });
        TreeItems.Add(new ArAgingTreeItem { Label = "31–60 ngày", Count = $"{d31to60}", BucketKey = "31-60" });
        TreeItems.Add(new ArAgingTreeItem { Label = "61–90 ngày", Count = $"{d61to90}", BucketKey = "61-90" });
        TreeItems.Add(new ArAgingTreeItem { Label = "91–120 ngày", Count = $"{d91to120}", BucketKey = "91-120" });
        TreeItems.Add(new ArAgingTreeItem { Label = "121–180 ngày", Count = $"{d121to180}", BucketKey = "121-180" });
        TreeItems.Add(new ArAgingTreeItem { Label = ">180 ngày", Count = $"{d181plus}", BucketKey = ">365" });
        TreeItems.Add(new ArAgingTreeItem { Label = "Vượt Credit", Count = $"{overCredit}", BucketKey = "OverCredit" });
        TreeItems.Add(new ArAgingTreeItem { Label = "Bị khóa", Count = $"{blocked}", BucketKey = "Blocked" });
    }

    [RelayCommand]
    private void SelectTreeNode(ArAgingTreeItem? item)
    {
        if (item == null) return;
        foreach (var t in TreeItems) t.IsSelected = false;
        item.IsSelected = true;

        if (item.BucketKey == "All")
        {
            FilterBucket = "Tất cả";
            FilterOverdueOnly = false;
            FilterOverCredit = false;
        }
        else if (item.BucketKey == "OverCredit")
        {
            FilterBucket = "Tất cả";
            FilterOverdueOnly = false;
            FilterOverCredit = true;
        }
        else if (item.BucketKey == "Blocked")
        {
            FilterBucket = "Tất cả";
            FilterOverdueOnly = false;
            FilterOverCredit = false;
            FilterStatus = "Bị khóa";
        }
        else
        {
            FilterBucket = item.BucketKey;
            FilterOverdueOnly = false;
            FilterOverCredit = false;
        }
        CurrentPage = 1;
        ApplyFiltersCommand.Execute(null);
    }

    #endregion

    #region Bucket Configuration

    private void BuildBucketConfigs()
    {
        BucketConfigs.Clear();
        BucketConfigs.Add(new ArAgingBucketConfig { Label = "Current", FromDay = 0, ToDay = 0, Color = "#16A34A", BgColor = "#DCFCE7" });
        BucketConfigs.Add(new ArAgingBucketConfig { Label = "1–30 ngày", FromDay = 1, ToDay = 30, Color = "#2563EB", BgColor = "#DBEAFE" });
        BucketConfigs.Add(new ArAgingBucketConfig { Label = "31–60 ngày", FromDay = 31, ToDay = 60, Color = "#D97706", BgColor = "#FFF7ED" });
        BucketConfigs.Add(new ArAgingBucketConfig { Label = "61–90 ngày", FromDay = 61, ToDay = 90, Color = "#DC2626", BgColor = "#FEE2E2" });
        BucketConfigs.Add(new ArAgingBucketConfig { Label = "91–120 ngày", FromDay = 91, ToDay = 120, Color = "#991B1B", BgColor = "#FEF2F2" });
        BucketConfigs.Add(new ArAgingBucketConfig { Label = "121–180 ngày", FromDay = 121, ToDay = 180, Color = "#7C3AED", BgColor = "#F3E8FF" });
        BucketConfigs.Add(new ArAgingBucketConfig { Label = "181–365 ngày", FromDay = 181, ToDay = 365, Color = "#E32F70", BgColor = "#FCE7F3" });
        BucketConfigs.Add(new ArAgingBucketConfig { Label = ">365 ngày", FromDay = 366, ToDay = 9999, Color = "#000000", BgColor = "#F1F5F9" });
    }

    #endregion

    #region Collector Performance

    private void BuildCollectorPerformances()
    {
        CollectorPerformances.Clear();
        var collectors = AllItems.Select(x => x.CollectorName).Where(x => x != null).Cast<string>().Distinct().ToList();
        foreach (var c in collectors)
        {
            var items = AllItems.Where(x => x.CollectorName == c).ToList();
            var collected = items.Sum(x => x.Payments?.Where(p => p.PaymentDate >= DateTime.Today.AddDays(-30)).Sum(p => p.Amount) ?? 0);
            var recoveryBase = collected + items.Sum(x => x.OutstandingAmount);
            CollectorPerformances.Add(new CollectorPerformanceDto
            {
                CollectorName = c,
                TotalAccounts = items.Count,
                TotalOutstanding = items.Sum(x => x.OutstandingAmount),
                CollectedThisMonth = collected,
                RecoveryRate = recoveryBase <= 0 ? 0 : collected / recoveryBase * 100,
                OverdueUnresolved = items.Count(x => x.AgingStatus == "Overdue")
            });
        }
    }

    #endregion

    #region Chart Data

    private void BuildChartData()
    {
        AgingDistribution.Clear();
        AgingDistribution.Add(new ArAgingChartData { Label = "Current", Value = AllItems.Sum(x => x.CurrentAmount), Color = "#16A34A" });
        AgingDistribution.Add(new ArAgingChartData { Label = "1–30", Value = AllItems.Sum(x => x.Overdue1To30), Color = "#2563EB" });
        AgingDistribution.Add(new ArAgingChartData { Label = "31–60", Value = AllItems.Sum(x => x.Overdue31To60), Color = "#D97706" });
        AgingDistribution.Add(new ArAgingChartData { Label = "61–90", Value = AllItems.Sum(x => x.Overdue61To90), Color = "#DC2626" });
        AgingDistribution.Add(new ArAgingChartData { Label = "91–120", Value = AllItems.Sum(x => x.Overdue91To120), Color = "#991B1B" });
        AgingDistribution.Add(new ArAgingChartData { Label = "121–180", Value = AllItems.Sum(x => x.Overdue121To180), Color = "#7C3AED" });
        AgingDistribution.Add(new ArAgingChartData { Label = "181+", Value = AllItems.Sum(x => x.Overdue181To365 + x.OverdueOver365), Color = "#E32F70" });

        TopDebtors.Clear();
        foreach (var item in AllItems.OrderByDescending(x => x.OutstandingAmount).Take(10))
            TopDebtors.Add(new ArAgingChartData { Label = item.PartnerCode, Value = item.OutstandingAmount, Color = item.RiskLevelColor });

        OutstandingTrend.Clear();
        var latestOutstanding = AllItems.Sum(x => x.OutstandingAmount);
        if (AllItems.Count > 0)
            OutstandingTrend.Add(new ArAgingChartData { Label = DateTime.Today.ToString("MM/yy"), Value = latestOutstanding, Color = "#2563EB" });

        CollectionTrend.Clear();
        foreach (var month in _transactions
                     .Where(x => x.CreditAmount > 0)
                     .GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month })
                     .OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month)
                     .TakeLast(12))
        {
            var date = new DateTime(month.Key.Year, month.Key.Month, 1);
            CollectionTrend.Add(new ArAgingChartData
            {
                Label = date.ToString("MM/yy"),
                Value = month.Sum(x => x.CreditAmount),
                Color = "#16A34A"
            });
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Refresh() => ApplyFiltersCommand.Execute(null);

    [RelayCommand]
    private void SetGridView() { IsGridView = true; IsCardView = false; }

    [RelayCommand]
    private void SetCardView() { IsCardView = true; IsGridView = false; }

    [RelayCommand]
    private void SelectItem(ArAgingItemDto? item)
    {
        SelectedItem = item;
        SelectedDetailTab = 0;
        OnPropertyChanged(nameof(IsOverviewTab));
        OnPropertyChanged(nameof(IsInvoicesTab));
        OnPropertyChanged(nameof(IsCollectionTab));
        OnPropertyChanged(nameof(IsPaymentTab));
        OnPropertyChanged(nameof(IsHistoryTab));
        OnPropertyChanged(nameof(IsDocumentsTab));
    }

    [RelayCommand]
    private void CloseDetail()
    {
        SelectedItem = null;
        SelectedDetailTab = 0;
    }

    [RelayCommand]
    private void SelectDetailTab(int tabIndex)
    {
        SelectedDetailTab = tabIndex;
        OnPropertyChanged(nameof(IsOverviewTab));
        OnPropertyChanged(nameof(IsInvoicesTab));
        OnPropertyChanged(nameof(IsCollectionTab));
        OnPropertyChanged(nameof(IsPaymentTab));
        OnPropertyChanged(nameof(IsHistoryTab));
        OnPropertyChanged(nameof(IsDocumentsTab));
    }

    [RelayCommand]
    private void GoToPage(int page)
    {
        if (page < 1 || page > TotalPages) return;
        CurrentPage = page;
        ApplyFiltersCommand.Execute(null);
    }

    [RelayCommand]
    private void NextPage() => GoToPage(CurrentPage + 1);

    [RelayCommand]
    private void PrevPage() => GoToPage(CurrentPage - 1);

    [RelayCommand]
    private void Analyze()
    {
        var overdue = FilteredItems.Where(x => x.OutstandingAmount > 0 && x.AgeDays.GetValueOrDefault() > 0).ToList();
        var total = overdue.Sum(x => x.OutstandingAmount);
        InfoMessage = $"Đã phân tích {FilteredItems.Count:N0} tài khoản thật; {overdue.Count:N0} tài khoản quá hạn, tổng {total:N0}.";
    }

    [RelayCommand]
    private async Task CollectPayment()
    {
        if (SelectedItem is null)
        {
            InfoMessage = "Hãy chọn một khoản công nợ cần thu.";
            return;
        }

        if (SelectedItem.OutstandingAmount <= 0)
        {
            InfoMessage = "Khoản công nợ đã hết số dư, không thể tạo phiếu thu.";
            return;
        }

        if (_receivableService is null)
        {
            InfoMessage = "Chưa kết nối được dịch vụ công nợ thật.";
            return;
        }

        var answer = MessageBox.Show(
            $"Tạo phiếu thu toàn bộ số dư {SelectedItem.OutstandingAmount:N0} {SelectedItem.CurrencyCode} cho {SelectedItem.PartnerName}?\n\nPhương thức mặc định: Tiền mặt.",
            "Xác nhận thu công nợ",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            var receiptNo = await _receivableService.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
            {
                CustomerId = SelectedItem.PartnerId,
                ReceiptDate = DateTime.Today,
                Amount = SelectedItem.OutstandingAmount,
                PaymentMethod = "Tiền mặt",
                Note = $"Thu công nợ từ màn Tuổi nợ - {SelectedItem.PartnerName}"
            }, CancellationToken.None);

            if (string.IsNullOrWhiteSpace(receiptNo))
            {
                InfoMessage = "Không tạo được phiếu thu. Kiểm tra mã khách hàng và quyền ghi dữ liệu.";
                return;
            }

            InfoMessage = $"Đã tạo phiếu thu thật {receiptNo} cho {SelectedItem.PartnerName}.";
            _isInitialized = false;
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể tạo phiếu thu công nợ: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SendReminder()
    {
        if (SelectedItem is null) { InfoMessage = "Hãy chọn một khoản công nợ cần nhắc."; return; }
        if (string.IsNullOrWhiteSpace(SelectedItem.Email)) { InfoMessage = "Đối tác chưa có email nhận nhắc nợ."; return; }
        if (_emailDispatchService is null) { InfoMessage = "Chưa đăng ký email outbox."; return; }
        try
        {
            var queued = await _emailDispatchService.QueueAsync(new EmailDispatchRequestDto
            {
                Recipient = SelectedItem.Email.Trim(),
                Subject = $"Nhắc thanh toán công nợ - {SelectedItem.PartnerName}",
                Body = $"Kính gửi {SelectedItem.PartnerName},\n\nTheo dữ liệu công nợ hiện tại, số tiền còn phải thu là {SelectedItem.OutstandingAmount:N0} {SelectedItem.CurrencyCode}. Vui lòng kiểm tra và thanh toán theo thỏa thuận.\n\nTrân trọng,\nASMILE BO Accounting",
                SourceType = "ArAgingReminder",
                SourceId = null
            }, CancellationToken.None);
            InfoMessage = $"Đã đưa nhắc nợ vào email outbox thật ({queued.Id:N}); gửi khi SMTP được bật.";
        }
        catch (Exception ex) { InfoMessage = $"Không thể lưu nhắc nợ: {ex.Message}"; }
    }

    [RelayCommand]
    private void GenerateStatement() => ExportAgingCsv("ar_aging_statement", "Đã xuất bảng kê công nợ thật dạng CSV");

    [RelayCommand]
    private async Task SendEmail()
    {
        if (SelectedItem is null) { InfoMessage = "Hãy chọn một khoản công nợ."; return; }
        if (string.IsNullOrWhiteSpace(SelectedItem.Email)) { InfoMessage = "Đối tác chưa có email nhận sao kê."; return; }
        if (_emailDispatchService is null) { InfoMessage = "Chưa đăng ký email outbox."; return; }
        try
        {
            var queued = await _emailDispatchService.QueueAsync(new EmailDispatchRequestDto
            {
                Recipient = SelectedItem.Email.Trim(),
                Subject = $"Sao kê công nợ - {SelectedItem.PartnerName}",
                Body = $"Kính gửi {SelectedItem.PartnerName},\n\nSố dư công nợ hiện tại: {SelectedItem.OutstandingAmount:N0} {SelectedItem.CurrencyCode}.\n\nTrân trọng,\nASMILE BO Accounting",
                SourceType = "ArAging",
                SourceId = null
            }, CancellationToken.None);
            InfoMessage = $"Đã lưu sao kê vào hàng đợi ({queued.Id:N}); gửi khi SMTP được bật.";
        }
        catch (Exception ex) { InfoMessage = $"Không thể lưu email: {ex.Message}"; }
    }

    [RelayCommand]
    private void ExportExcel() => ExportAgingCsv("ar_aging_report", "Đã xuất báo cáo Aging thật dạng CSV (mở bằng Excel)");

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (_reportEngine is null)
        {
            InfoMessage = "Chưa đăng ký report engine thật; không tạo PDF giả.";
            return;
        }

        try
        {
            var bytes = await _reportEngine.ExportPdfAsync(BuildAgingReportRequest(), CancellationToken.None);
            var dialog = new SaveFileDialog
            {
                FileName = $"ar_aging_{DateTime.Today:yyyyMMdd}.pdf",
                Filter = "PDF (*.pdf)|*.pdf",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true) return;
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            InfoMessage = $"Đã xuất PDF Aging từ báo cáo thật: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể xuất PDF Aging thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Print()
    {
        IEnumerable<ArAgingItemDto> rowsSource = SelectedItem is not null ? new[] { SelectedItem } : FilteredItems;
        var rows = rowsSource.ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Không có dữ liệu Aging thật để in.";
            return;
        }

        var content = new StringBuilder()
            .AppendLine("BÁO CÁO TUỔI NỢ PHẢI THU")
            .AppendLine($"Ngày chốt: {FilterAsOfDate ?? DateTime.Today.ToString("dd/MM/yyyy")} · Số dòng: {rows.Count:N0}")
            .AppendLine(new string('-', 100));
        foreach (var row in rows)
            content.AppendLine($"{row.PartnerCode,-18} | {row.PartnerName,-30} | {row.InvoiceNo ?? "—",-16} | Còn nợ {row.OutstandingAmount,15:N0} {row.CurrencyCode} | {row.AgingStatusLabel}");

        var preview = new PrintPreviewDialog("Báo cáo tuổi nợ phải thu", content.ToString()) { Owner = System.Windows.Application.Current.MainWindow };
        preview.ShowDialog();
        InfoMessage = "Đã mở bản xem trước từ dữ liệu Aging thật.";
    }

    private ReportRequestDto BuildAgingReportRequest() => new()
    {
        ReportCode = "AR_AGING",
        Parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["asOfDate"] = FilterAsOfDate
        }
    };

    private void ExportAgingCsv(string fileName, string successMessage)
    {
        IEnumerable<ArAgingItemDto> rows = SelectedItem is not null ? new[] { SelectedItem } : FilteredItems;
        var rowList = rows.ToList();
        if (rowList.Count == 0)
        {
            InfoMessage = "Không có dữ liệu Aging thật để xuất.";
            return;
        }

        var path = CsvExportHelper.Export(
            fileName,
            new[] { "Đối tác", "Loại", "Invoice", "Ngày đến hạn", "Số ngày quá hạn", "Hiện tại", "Quá hạn 1-30", "Quá hạn 31-60", "Quá hạn 61-90", "Còn phải thu", "Tiền tệ", "Trạng thái" },
            rowList.Select(item => (IReadOnlyList<string>)new[]
            {
                item.PartnerName,
                item.PartnerTypeLabel,
                item.InvoiceNo ?? string.Empty,
                item.DueDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                item.AgeDays?.ToString() ?? string.Empty,
                CsvExportHelper.F(item.CurrentAmount),
                CsvExportHelper.F(item.Overdue1To30),
                CsvExportHelper.F(item.Overdue31To60),
                CsvExportHelper.F(item.Overdue61To90),
                CsvExportHelper.F(item.OutstandingAmount),
                item.CurrencyCode,
                item.AgingStatusLabel
            }).ToList());
        InfoMessage = $"{successMessage}: {path}";
    }

    [RelayCommand]
    private void ShowHistory()
    {
        SelectDetailTab(4);
        InfoMessage = SelectedItem is null ? "Hãy chọn một khoản công nợ để xem lịch sử." : "Đã mở lịch sử công nợ của đối tác.";
    }

    [RelayCommand]
    private void OpenCompany()
    {
        SelectDetailTab(0);
        InfoMessage = SelectedItem is null ? "Hãy chọn một khoản công nợ để xem hồ sơ đối tác." : "Đã mở tổng quan hồ sơ đối tác.";
    }

    [RelayCommand]
    private void OpenGuest()
    {
        SelectDetailTab(0);
        InfoMessage = SelectedItem is null ? "Hãy chọn một khoản công nợ để xem hồ sơ khách." : "Đã mở tổng quan hồ sơ khách/đối tác.";
    }

    [RelayCommand]
    private void OpenInvoice()
    {
        SelectDetailTab(1);
        InfoMessage = SelectedItem is null ? "Hãy chọn một khoản công nợ để xem hóa đơn." : "Đã mở danh sách hóa đơn của đối tác.";
    }

    [RelayCommand]
    private void OpenPayment()
    {
        SelectDetailTab(3);
        InfoMessage = SelectedItem is null ? "Hãy chọn một khoản công nợ để xem thanh toán." : "Đã mở các khoản thanh toán của đối tác.";
    }

    [RelayCommand]
    private void OpenCollection()
    {
        SelectDetailTab(2);
        InfoMessage = SelectedItem is null ? "Hãy chọn một khoản công nợ để xem thu hồi." : "Đã mở thông tin thu hồi công nợ.";
    }

    [RelayCommand]
    private void DismissInfo() => InfoMessage = null;

    #endregion

    #region Pagination

    public ObservableCollection<ArAgingPagerItem> PagerItems { get; } = new();

    public void BuildPagerItems()
    {
        PagerItems.Clear();
        if (TotalPages <= 7)
        {
            for (int i = 1; i <= TotalPages; i++)
                PagerItems.Add(new ArAgingPagerItem { Label = i.ToString(), Page = i, IsCurrent = i == CurrentPage });
        }
        else
        {
            PagerItems.Add(new ArAgingPagerItem { Label = "1", Page = 1, IsCurrent = 1 == CurrentPage });
            if (CurrentPage > 3)
                PagerItems.Add(new ArAgingPagerItem { Label = "...", IsEllipsis = true });
            for (int i = Math.Max(2, CurrentPage - 1); i <= Math.Min(TotalPages - 1, CurrentPage + 1); i++)
                PagerItems.Add(new ArAgingPagerItem { Label = i.ToString(), Page = i, IsCurrent = i == CurrentPage });
            if (CurrentPage < TotalPages - 2)
                PagerItems.Add(new ArAgingPagerItem { Label = "...", IsEllipsis = true });
            PagerItems.Add(new ArAgingPagerItem { Label = TotalPages.ToString(), Page = TotalPages, IsCurrent = TotalPages == CurrentPage });
        }
    }

    #endregion
}

public sealed class ArAgingPagerItem
{
    public string Label { get; set; } = "";
    public int Page { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsEllipsis { get; set; }
}
