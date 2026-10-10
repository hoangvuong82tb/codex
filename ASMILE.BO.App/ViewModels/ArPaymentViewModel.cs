using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class ArPmtDto
{
    public string TransactionId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string PaymentNo { get; set; } = "";
    public string? ReceiptNo { get; set; }
    public string? CompanyName { get; set; }
    public string? GuestName { get; set; }
    public string? InvoiceNo { get; set; }
    public string? ReservationNo { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Method { get; set; } = "Tiền mặt";
    public string? Bank { get; set; }
    public decimal Amount { get; set; }
    public decimal Allocated { get; set; }
    public decimal Unallocated => Amount - Allocated;
    public decimal Remaining => Unallocated;
    public string Currency { get; set; } = "VND";
    public decimal ExchangeRate { get; set; } = 1;
    public string? Reference { get; set; }
    public string? Collector { get; set; }
    public string? Cashier { get; set; }

    public string Status { get; set; } = "Pending";
    public string StatusLabel => Status switch
    {
        "Pending" => "Chờ xử lý", "Completed" => "Hoàn thành", "Partial" => "Một phần",
        "Rejected" => "Từ chối", "Reversed" => "Đã hoàn", "Allocated" => "Đã phân bổ",
        "Unallocated" => "Chưa phân bổ", _ => Status
    };
    public string StatusColor => Status switch
    {
        "Pending" => "#F59E0B", "Completed" => "#16A34A", "Partial" => "#2563EB",
        "Rejected" => "#DC2626", "Reversed" => "#64748B", "Allocated" => "#0891B2",
        "Unallocated" => "#D97706", _ => "#64748B"
    };
    public string StatusBg => Status switch
    {
        "Pending" => "#FFFBEB", "Completed" => "#DCFCE7", "Partial" => "#DBEAFE",
        "Rejected" => "#FEE2E2", "Reversed" => "#F1F5F9", "Allocated" => "#ECFEFF",
        "Unallocated" => "#FFF7ED", _ => "#F8FAFC"
    };

    public bool IsOverPayment => Amount > 0 && Allocated > 0 && Amount > Allocated;
    public bool IsPartial => Amount > 0 && Allocated > 0 && Amount < Allocated;
    public double ProgressPercent => Amount > 0 ? (double)Allocated / (double)Amount * 100 : 0;

    public List<ArPaymentAllocationDto> Allocations { get; set; } = new();
    public ObservableCollection<ArPaymentInvoiceDto> Invoices { get; set; } = new();
    public List<ArPaymentReceiptDto> Receipts { get; set; } = new();
    public List<ArPaymentTimelineDto> Timeline { get; set; } = new();
    public List<ArPaymentDocumentDto> Documents { get; set; } = new();
    public List<ArPaymentNoteDto> Notes { get; set; } = new();
    public List<ArPaymentAuditDto> Audits { get; set; } = new();
}

public sealed class ArPaymentAllocationDto
{
    public string InvoiceNo { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Outstanding { get; set; }
    public decimal Allocated { get; set; }
    public decimal Remaining { get; set; }
    public double ProgressPercent => Outstanding > 0 ? (double)Allocated / (double)Outstanding * 100 : 0;
}

public sealed class ArPaymentInvoiceDto
{
    public string TransactionId { get; set; } = "";
    public string InvoiceNo { get; set; } = "";
    public decimal Outstanding { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Vat { get; set; }
    public string Status { get; set; } = "Open";
    public decimal Balance { get; set; }
}

public sealed class ArPaymentReceiptDto
{
    public string ReceiptNo { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
    public string? Cashier { get; set; }
    public string? Printer { get; set; }
    public bool IsPrinted { get; set; }
    public int ReprintCount { get; set; }
    public bool IsEmailed { get; set; }
}

public sealed class ArPaymentTimelineDto
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public sealed class ArPaymentDocumentDto
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string TypeLabel => Type switch
    {
        "BankTransfer" => "Ủy nhiệm chi", "Receipt" => "Biên lai", "BankCredit" => "Giấy báo có",
        "Statement" => "Sao kê", "Slip" => "Slip", "PDF" => "PDF", "Image" => "Ảnh", "Excel" => "Excel", _ => Type
    };
    public DateTime UploadDate { get; set; }
}

public sealed class ArPaymentNoteDto
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "Internal";
    public string TypeLabel => Type switch { "Remark" => "Ghi chú", "Accounting" => "Kế toán", "Collector" => "Thu ngân", "Internal" => "Nội bộ", _ => Type };
    public string Content { get; set; } = "";
    public string Author { get; set; } = "";
}

public sealed class ArPaymentAuditDto
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? Device { get; set; }
}

// ═══ Chart DTOs ═══

public sealed class ArPaymentChartDataPoint
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public decimal MaxValue { get; set; }
    public double BarWidth => MaxValue > 0 ? (double)(Value / MaxValue) * 100 : 0;
    public string Color { get; set; } = "#2563EB";
    public string FormattedValue => Value >= 1_000_000_000 ? $"{Value / 1_000_000_000:N1}B" : Value >= 1_000_000 ? $"{Value / 1_000_000:N1}M" : Value >= 1_000 ? $"{Value / 1_000:N1}K" : $"{Value:N0}";
}

public sealed class ArPaymentMethodPieSlice
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public double Percentage { get; set; }
    public string Color { get; set; } = "#2563EB";
}

public sealed class ArPaymentTopCompany
{
    public string Name { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int PaymentCount { get; set; }
    public double Percentage { get; set; }
    public string Color { get; set; } = "#2563EB";
}

public sealed class ArPaymentCollectorStat
{
    public string Name { get; set; } = "";
    public int TotalPayments { get; set; }
    public decimal TotalCollected { get; set; }
    public int OnTimeCount { get; set; }
    public int LateCount { get; set; }
    public double CollectionRate { get; set; }
    public string Color { get; set; } = "#2563EB";
}

public sealed class ArPaymentBankImportRow
{
    public Guid BankStatementId { get; set; }
    public Guid StatementLineId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Reference { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public bool IsMatched { get; set; }
    public string? MatchedPaymentNo { get; set; }
    public ObservableCollection<ArPaymentBankCandidate> CandidatePayments { get; } = new();
    public string? SelectedPaymentId { get; set; }
}

public sealed class ArPaymentBankCandidate
{
    public string TransactionId { get; init; } = string.Empty;
    public string PaymentNo { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

public sealed class ArPaymentReceiptCenterItem
{
    public string ReceiptNo { get; set; } = "";
    public string PaymentNo { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
    public string? Cashier { get; set; }
    public string? Printer { get; set; }
    public bool IsPrinted { get; set; }
    public bool IsEmailed { get; set; }
    public int ReprintCount { get; set; }
    public string StatusLabel => IsPrinted ? "Đã in" : "Chưa in";
    public string StatusColor => IsPrinted ? "#16A34A" : "#F59E0B";
    public string StatusBg => IsPrinted ? "#DCFCE7" : "#FFFBEB";
}

public sealed class ArPaymentKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class ArPaymentTreeItem : ObservableObject
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsSelected { get; set; }
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class ArPaymentPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }
    public ArPaymentPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    { Label = label; PageNumber = pageNumber; IsCurrent = isCurrent; IsEllipsis = isEllipsis; }
}

public sealed class ArPaymentSplitItem : ObservableObject
{
    public string Method { get; set; } = "Tiền mặt";
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Bank { get; set; }
}

// ═══ VIEW MODEL ═══

public sealed partial class ArPaymentViewModel : ViewModelBase
{
    private readonly IReceivableService? _receivableService;
    private readonly IDebtTransactionService? _debtTransactionService;
    private readonly IBankingService? _bankingService;
    private readonly IReconciliationEngine? _reconciliationEngine;
    private readonly ICustomerAppService? _customerAppService;
    private readonly IEmailDispatchService? _emailDispatchService;

    public ArPaymentViewModel(
        IReceivableService? receivableService = null,
        IDebtTransactionService? debtTransactionService = null,
        IBankingService? bankingService = null,
        IReconciliationEngine? reconciliationEngine = null,
        ICustomerAppService? customerAppService = null,
        IEmailDispatchService? emailDispatchService = null)
    {
        _receivableService = receivableService;
        _debtTransactionService = debtTransactionService;
        _bankingService = bankingService;
        _reconciliationEngine = reconciliationEngine;
        _customerAppService = customerAppService;
        _emailDispatchService = emailDispatchService;
    }

    public ObservableCollection<ArPmtDto> Items { get; } = new();
    public ObservableCollection<ArPmtDto> FilteredItems { get; } = new();
    public ObservableCollection<ArPaymentKpi> DashboardKpis { get; } = new();
    public ObservableCollection<ArPaymentTreeItem> TreeItems { get; } = new();
    public ObservableCollection<ArPaymentPaginationButton> PageButtons { get; } = new();
    public ObservableCollection<ReceivableSummaryDto> PaymentCustomers { get; } = new();

    [ObservableProperty] private ArPmtDto? _selectedItem;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private ArPaymentTreeItem? _selectedTreeItem;
    [ObservableProperty] private string _viewMode = "Grid";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterMethod;
    [ObservableProperty] private string? _filterCurrency;
    [ObservableProperty] private string? _filterCollector;
    [ObservableProperty] private string? _filterBank;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    [ObservableProperty] private bool _isRightPanelOpen = true;

    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formMode = string.Empty;
    [ObservableProperty] private string _formTitle = string.Empty;
    [ObservableProperty] private string _formErrorMessage = string.Empty;

    [ObservableProperty] private string _payCompanyName = "";
    [ObservableProperty] private string _payInvoiceNo = "";
    [ObservableProperty] private decimal _payOutstanding;
    [ObservableProperty] private string _payMethod = "Tiền mặt";
    [ObservableProperty] private decimal _payAmount;
    [ObservableProperty] private string _payReference = "";
    [ObservableProperty] private string _payRemark = "";
    [ObservableProperty] private ReceivableSummaryDto? _selectedPaymentCustomer;

    [ObservableProperty] private string _allocInvoiceNo = "";
    [ObservableProperty] private decimal _allocAmount;

    // ═══ Chart data ═══
    public ObservableCollection<ArPaymentChartDataPoint> PaymentByMonthData { get; } = new();
    public ObservableCollection<ArPaymentMethodPieSlice> PaymentMethodData { get; } = new();
    public ObservableCollection<ArPaymentTopCompany> TopCompanyData { get; } = new();
    public ObservableCollection<ArPaymentChartDataPoint> OutstandingTrendData { get; } = new();
    public ObservableCollection<ArPaymentChartDataPoint> CollectionTrendData { get; } = new();
    public ObservableCollection<ArPaymentCollectorStat> CollectorPerformanceData { get; } = new();

    // ═══ Bank import ═══
    public ObservableCollection<ArPaymentBankImportRow> BankImportRows { get; } = new();
    [ObservableProperty] private bool _isBankImportOpen;
    [ObservableProperty] private string _bankImportFile = "";
    [ObservableProperty] private string _bankImportStatus = "";

    // ═══ Receipt Center ═══
    public ObservableCollection<ArPaymentReceiptCenterItem> ReceiptCenterItems { get; } = new();
    [ObservableProperty] private bool _isReceiptCenterOpen;
    [ObservableProperty] private string _receiptCenterFilter = "Tất cả";

    // ═══ Collection Dashboard ═══
    [ObservableProperty] private bool _isCollectionDashboardOpen;
    public decimal OverallCollectionRate => TotalItems > 0 ? (decimal)CompletedCount / TotalItems * 100 : 0;
    public decimal AvgCollectionDays => Items.Where(i => i.Status == "Completed")
        .Select(i => i.PaymentDate == default ? 0 : (decimal)((i.PaymentDate - DateTime.Now.AddDays(-365)).TotalDays))
        .DefaultIfEmpty().Average();
    public int OverdueCount => Items.Count(i => i.Status is "Pending" or "Partial" && i.PaymentDate < DateTime.Now.AddDays(-30));

    // ═══ Additional KPIs ═══
    public int QrCount => Items.Count(i => i.Method == "QR");
    public int VirtualCardCount => Items.Count(i => i.Method == "Virtual Card");
    public int ChequeCount => Items.Count(i => i.Method == "Séc");
    public int VoucherCount => Items.Count(i => i.Method == "Voucher");
    public int YearCount => Items.Count(i => i.PaymentDate.Year == DateTime.Today.Year);
    public int OutstandingCount => Items.Count(i => i.Unallocated > 0);
    public int CorporateCount => Items.Count(i => i.CompanyName != null && i.Status != "Rejected");
    public int TravelAgentCount => Items.Count(i => i.CompanyName != null && i.Method == "Chuyển khoản");
    public int OtaCount => Items.Count(i => i.CompanyName != null && i.Method == "Virtual Card");
    public decimal TotalOverPaymentAmount => Items.Where(i => i.IsOverPayment).Sum(i => i.Amount - i.Allocated);
    public decimal TotalPendingAmount => Items.Where(i => i.Status == "Pending").Sum(i => i.Amount);

    // ═══ Additional filter options ═══
    public static string[] PaymentDateFilters { get; } = ["Tất cả", "Hôm nay", "Hôm qua", "Tuần này", "Tháng này", "Tháng trước", "Quý này", "Năm nay"];
    public static string[] CashierFilters { get; } = ["Tất cả", "Nguyễn Văn Thu Ngân", "Trần Thị Cashier", "Lê Văn Teller", "Phạm Thị Payment"];

    [ObservableProperty] private string? _filterPaymentDate;
    [ObservableProperty] private string? _filterCashier;
    [ObservableProperty] private string? _filterCompany;
    [ObservableProperty] private string? _filterInvoice;

    // ═══ Over Payment state ═══
    [ObservableProperty] private bool _isOverPaymentDialogOpen;
    [ObservableProperty] private string _overPaymentAction = "Credit";
    [ObservableProperty] private string _overPaymentMessage = "";

    // ═══ Mixed payment ═══
    public ObservableCollection<ArPaymentSplitItem> PaymentSplits { get; } = new();
    [ObservableProperty] private bool _isMixedPayment;

    // ═══ Dashboard tab ═══
    [ObservableProperty] private int _selectedDashboardTab; // 0=charts, 1=collection, 2=receipts
    public bool IsChartsTab => SelectedDashboardTab == 0;
    public bool IsCollectionTab => SelectedDashboardTab == 1;
    public bool IsReceiptCenterTab => SelectedDashboardTab == 2;

    // ═══ Tab visibility ═══
    partial void OnSelectedDashboardTabChanged(int value) { OnPropertyChanged(nameof(IsChartsTab)); OnPropertyChanged(nameof(IsCollectionTab)); OnPropertyChanged(nameof(IsReceiptCenterTab)); }

    public bool IsPaymentForm => FormMode == "Payment";
    public bool IsAllocForm => FormMode == "Alloc";
    public bool IsFormClosed => !IsFormOpen;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0 thanh toán" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} thanh toán";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedItem is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool IsGridView => ViewMode == "Grid";

    public int TotalItems => Items.Count;
    public int PendingCount => Items.Count(i => i.Status == "Pending");
    public int CompletedCount => Items.Count(i => i.Status == "Completed");
    public int PartialCount => Items.Count(i => i.Status == "Partial");
    public int RejectedCount => Items.Count(i => i.Status == "Rejected");
    public int OverPaymentCount => Items.Count(i => i.IsOverPayment);
    public int TodayCount => Items.Count(i => i.PaymentDate.Date == DateTime.Today);
    public int MonthCount => Items.Count(i => i.PaymentDate.Year == DateTime.Today.Year && i.PaymentDate.Month == DateTime.Today.Month);
    public int CashCount => Items.Count(i => i.Method == "Tiền mặt");
    public int TransferCount => Items.Count(i => i.Method == "Chuyển khoản");
    public int CardCount => Items.Count(i => i.Method is "Thẻ tín dụng" or "Thẻ ghi nợ");
    public decimal TotalAmount => Items.Sum(i => i.Amount);
    public decimal TotalAllocated => Items.Sum(i => i.Allocated);
    public decimal TotalUnallocated => Items.Sum(i => i.Unallocated);

    public static string[] StatusFilters { get; } = ["Tất cả", "Pending", "Completed", "Partial", "Rejected", "Reversed", "Allocated", "Unallocated"];
    public static string[] MethodFilters { get; } = ["Tất cả", "Tiền mặt", "Chuyển khoản", "Thẻ tín dụng", "Thẻ ghi nợ", "QR", "Virtual Card", "Séc", "Voucher"];
    public static string[] CurrencyFilters { get; } = ["Tất cả", "VND", "USD", "EUR"];
    public static string[] CollectorFilters { get; } = ["Tất cả", "Nguyễn Văn Thu Ngân", "Trần Thị Cashier", "Lê Văn Teller", "Phạm Thị Payment"];
    public static string[] BankFilters { get; } = ["Tất cả", "VCB", "BIDV", "VietinBank", "Agribank", "Techcombank", "MB Bank", "ACB", "Sacombank"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Items.Clear();

            if (_receivableService is null)
            {
                ErrorMessage = "Chưa có Receivable query service; không hiển thị dữ liệu mẫu.";
                BuildDashboard();
                ApplyFilters();
                return;
            }

            var transactions = await _receivableService.GetTransactionsAsync(new ReceivableFilterDto(), ct);
            foreach (var transaction in transactions.Where(x => x.CreditAmount > 0))
                Items.Add(MapPersistedPayment(transaction));

            BuildDashboard();
            ApplyFilters();
            InfoMessage = $"Đã tải {Items.Count:N0} khoản thu từ dữ liệu công nợ thật. Allocation chỉ hiện khi backend có dữ liệu phân bổ.";
            return;

#if false // Legacy designer fixture retained for historical reference only.
#pragma warning disable CS0162
            await Task.Delay(100, ct);
            var rng = new Random(202);

            var methods = new[] { "Tiền mặt", "Chuyển khoản", "Chuyển khoản", "Thẻ tín dụng", "Thẻ ghi nợ", "QR", "Virtual Card", "Séc", "Voucher" };
            var statuses = new[] { "Completed", "Completed", "Completed", "Pending", "Pending", "Partial", "Rejected", "Allocated", "Unallocated" };
            var currencies = new[] { "VND", "VND", "VND", "USD", "EUR" };
            var banks = new[] { "VCB", "BIDV", "VietinBank", "Agribank", "Techcombank", "MB Bank", "ACB", "Sacombank" };
            var guests = new[] { "Nguyễn Văn An", "Trần Thị Bình", "John Smith", "Kim Min-Ji", "Tanaka Hiroshi", "Chen Wei", "Sophie Martin", "Hans Mueller", "Anna Rossi", "Park Ji-Sung", "David Chen", "Maria Garcia" };
            var companies = new[] { "Vingroup", "FPT", "Vietcombank", "Saigon Tourist", "Hanoi Travel", "Petrolimex", "VNPT", "Viettel", "Masan", "TH True Milk", "Techcombank", "BIDV" };
            var collectors = new[] { "Nguyễn Văn Thu Ngân", "Trần Thị Cashier", "Lê Văn Teller", "Phạm Thị Payment" };
            var cashiers = new[] { "Nguyễn Văn Thu Ngân", "Trần Thị Cashier", "Lê Văn Teller", "Phạm Thị Payment" };

            for (int i = 0; i < 8000; i++)
            {
                var method = methods[rng.Next(methods.Length)];
                var status = statuses[rng.Next(statuses.Length)];
                var currency = currencies[rng.Next(currencies.Length)];
                var bank = method == "Chuyển khoản" ? banks[rng.Next(banks.Length)] : null;
                var date = DateTime.Now.AddDays(-rng.Next(0, 365));
                var amount = (decimal)rng.Next(500000, 100000000);
                var allocated = status switch
                {
                    "Completed" => amount,
                    "Partial" => amount * (decimal)(rng.Next(25, 75) / 100.0),
                    "Allocated" => amount,
                    "Unallocated" => 0,
                    _ => 0
                };

                var item = new ArPmtDto
                {
                    PaymentNo = $"PMT{rng.Next(100000, 999999)}",
                    ReceiptNo = rng.Next(3) > 0 ? $"PT{rng.Next(10000, 99999)}" : null,
                    CompanyName = rng.Next(3) == 1 ? companies[rng.Next(companies.Length)] : null,
                    GuestName = guests[rng.Next(guests.Length)],
                    InvoiceNo = rng.Next(3) > 0 ? $"INV{rng.Next(100000, 999999)}" : null,
                    ReservationNo = rng.Next(2) == 1 ? $"RES{rng.Next(10000, 99999)}" : null,
                    PaymentDate = date,
                    Method = method, Bank = bank,
                    Amount = amount, Allocated = allocated,
                    Currency = currency, ExchangeRate = currency == "VND" ? 1 : rng.Next(23000, 26000),
                    Reference = method == "Chuyển khoản" ? $"REF{rng.Next(10000, 99999)}" : rng.Next(3) == 1 ? $"REF{rng.Next(10000, 99999)}" : null,
                    Collector = collectors[rng.Next(collectors.Length)],
                    Cashier = cashiers[rng.Next(cashiers.Length)],
                    Status = status,
                };

                // Allocations
                if (allocated > 0)
                {
                    var invCount = rng.Next(1, 4);
                    var remaining = allocated;
                    for (int a = 0; a < invCount && remaining > 0; a++)
                    {
                        var invAmt = a == invCount - 1 ? remaining : (decimal)rng.Next(100000, (int)Math.Max(100001, remaining));
                        item.Allocations.Add(new ArPaymentAllocationDto
                        {
                            InvoiceNo = $"INV{rng.Next(100000, 999999)}",
                            InvoiceDate = date.AddDays(-rng.Next(1, 60)),
                            DueDate = date.AddDays(rng.Next(15, 60)),
                            Outstanding = invAmt * 2,
                            Allocated = Math.Min(invAmt, remaining),
                            Remaining = (invAmt * 2) - Math.Min(invAmt, remaining)
                        });
                        remaining -= invAmt;
                    }
                }

                // Linked invoices
                var invCount2 = rng.Next(0, 5);
                for (int inv = 0; inv < invCount2; inv++)
                    item.Invoices.Add(new ArPaymentInvoiceDto
                    {
                        InvoiceNo = $"INV{rng.Next(100000, 999999)}",
                        Outstanding = (decimal)rng.Next(1000000, 30000000),
                        DueDate = date.AddDays(rng.Next(15, 60)),
                        Vat = (decimal)rng.Next(100000, 3000000),
                        Status = rng.Next(3) switch { 0 => "Open", 1 => "PartiallyPaid", _ => "Paid" },
                        Balance = (decimal)rng.Next(0, 20000000)
                    });

                // Receipts
                if (item.ReceiptNo is not null)
                    item.Receipts.Add(new ArPaymentReceiptDto
                    {
                        ReceiptNo = item.ReceiptNo,
                        ReceiptDate = date,
                        Cashier = item.Cashier,
                        Printer = "Printer-Lobby",
                        IsPrinted = rng.Next(3) > 0,
                        ReprintCount = rng.Next(0, 3),
                        IsEmailed = rng.Next(4) == 1
                    });

                // Timeline
                item.Timeline.Add(new ArPaymentTimelineDto { Timestamp = date, Event = "Payment Created", Detail = $"Tạo thanh toán {item.PaymentNo} — {amount:N0} {currency}", Color = "#2563EB" });
                if (allocated > 0)
                    item.Timeline.Add(new ArPaymentTimelineDto { Timestamp = date.AddMinutes(rng.Next(10, 60)), Event = "Allocated", Detail = $"Phân bổ {allocated:N0} {currency}", Color = "#0891B2" });
                if (status == "Completed")
                    item.Timeline.Add(new ArPaymentTimelineDto { Timestamp = date.AddHours(rng.Next(1, 24)), Event = "Completed", Detail = "Hoàn thành thanh toán", Color = "#16A34A" });

                // Documents
                if (method == "Chuyển khoản")
                    item.Documents.Add(new ArPaymentDocumentDto { Name = $"UNC_{item.PaymentNo}", Type = "BankTransfer", UploadDate = date });
                if (item.ReceiptNo is not null)
                    item.Documents.Add(new ArPaymentDocumentDto { Name = $"Receipt_{item.ReceiptNo}", Type = "Receipt", UploadDate = date });

                // Notes
                if (rng.Next(4) == 1)
                    item.Notes.Add(new ArPaymentNoteDto { Timestamp = date, Type = "Internal", Content = $"Thanh toán từ {item.CompanyName ?? item.GuestName}", Author = item.Collector ?? "System" });

                // Audits
                item.Audits.Add(new ArPaymentAuditDto { Timestamp = date, Action = "Created", Actor = item.Collector ?? "System", Detail = $"Tạo thanh toán {item.PaymentNo}", Device = "AR" });
                if (allocated > 0)
                    item.Audits.Add(new ArPaymentAuditDto { Timestamp = date.AddMinutes(30), Action = "Allocated", Actor = item.Collector ?? "System", Detail = $"Phân bổ {allocated:N0} {currency}", Device = "AR" });

                Items.Add(item);
            }

            BuildDashboard(); BuildTree(); BuildChartData(); ApplyFilters();
            SelectTreeItem(TreeItems.FirstOrDefault());
#pragma warning restore CS0162
#endif
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new ArPaymentKpi { Label = "Tổng TT", Value = TotalItems.ToString(), Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Hôm nay", Value = TodayCount.ToString(), Color = "#0891B2", BgColor = "#ECFEFF" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Tháng", Value = MonthCount.ToString(), Color = "#2563EB", BgColor = "#EFF6FF" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Năm", Value = YearCount.ToString(), Color = "#7C3AED", BgColor = "#F3E8FF" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Tiền mặt", Value = CashCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4", FilterKey = "Method", FilterValue = "Tiền mặt" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Chuyển khoản", Value = TransferCount.ToString(), Color = "#2563EB", BgColor = "#DBEAFE", FilterKey = "Method", FilterValue = "Chuyển khoản" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Thẻ", Value = CardCount.ToString(), Color = "#7C3AED", BgColor = "#F3E8FF", FilterKey = "Method", FilterValue = "Thẻ tín dụng" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "QR", Value = QrCount.ToString(), Color = "#059669", BgColor = "#ECFDF5", FilterKey = "Method", FilterValue = "QR" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Virtual Card", Value = VirtualCardCount.ToString(), Color = "#0D9488", BgColor = "#F0FDFA", FilterKey = "Method", FilterValue = "Virtual Card" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Pending", Value = PendingCount.ToString(), Color = "#F59E0B", BgColor = "#FFFBEB", FilterKey = "Status", FilterValue = "Pending" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Partial", Value = PartialCount.ToString(), Color = "#2563EB", BgColor = "#DBEAFE", FilterKey = "Status", FilterValue = "Partial" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Rejected", Value = RejectedCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Status", FilterValue = "Rejected" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Over Pmt", Value = OverPaymentCount.ToString(), Color = "#E32F70", BgColor = "#FFF0F5" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Chưa phân bổ", Value = OutstandingCount.ToString(), Color = "#D97706", BgColor = "#FFF7ED" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Tổng tiền", Value = $"{TotalAmount:N0} ₫", Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Chưa phân bổ", Value = $"{TotalUnallocated:N0} ₫", Color = "#DC2626", BgColor = "#FEF2F2" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Pending Amount", Value = $"{TotalPendingAmount:N0} ₫", Color = "#F59E0B", BgColor = "#FFFBEB" });
        DashboardKpis.Add(new ArPaymentKpi { Label = "Over Amount", Value = $"{TotalOverPaymentAmount:N0} ₫", Color = "#E32F70", BgColor = "#FFF0F5" });
    }

    private void BuildTree()
    {
        TreeItems.Clear();
        TreeItems.Add(new ArPaymentTreeItem { Label = "Tất cả", Count = TotalItems, IsSelected = true });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Pending", Count = PendingCount, FilterKey = "Status", FilterValue = "Pending" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Completed", Count = CompletedCount, FilterKey = "Status", FilterValue = "Completed" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Partial", Count = PartialCount, FilterKey = "Status", FilterValue = "Partial" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Over Payment", Count = OverPaymentCount, FilterKey = "OverPayment", FilterValue = "true" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Rejected", Count = RejectedCount, FilterKey = "Status", FilterValue = "Rejected" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Hôm nay", Count = TodayCount, FilterKey = "Today", FilterValue = "true" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Tháng này", Count = MonthCount, FilterKey = "Month", FilterValue = "true" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Corporate", Count = CorporateCount, FilterKey = "Method", FilterValue = "Chuyển khoản" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Travel Agent", Count = TravelAgentCount, FilterKey = "Method", FilterValue = "Thẻ tín dụng" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "OTA", Count = OtaCount, FilterKey = "Method", FilterValue = "Virtual Card" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Tiền mặt", Count = CashCount, FilterKey = "Method", FilterValue = "Tiền mặt" });
        TreeItems.Add(new ArPaymentTreeItem { Label = "Chuyển khoản", Count = TransferCount, FilterKey = "Method", FilterValue = "Chuyển khoản" });
    }

    private static ArPmtDto MapPersistedPayment(ReceivableTransactionDto transaction)
    {
        return new ArPmtDto
        {
            TransactionId = transaction.Id,
            CustomerId = transaction.CustomerId,
            PaymentNo = string.IsNullOrWhiteSpace(transaction.DocumentNo) ? transaction.Id : transaction.DocumentNo,
            ReceiptNo = transaction.DocumentType == ReceivableDocumentType.PaymentReceipt ? transaction.DocumentNo : null,
            CompanyName = transaction.CustomerName,
            PaymentDate = transaction.TransactionDate,
            Method = transaction.DocumentType == ReceivableDocumentType.PaymentReceipt ? "Chưa phân loại" : "Bút toán công nợ",
            Amount = transaction.CreditAmount,
            Allocated = transaction.AllocatedAmount,
            Currency = transaction.Currency,
            Reference = transaction.Description,
            Collector = transaction.CreatedBy,
            Cashier = transaction.CreatedBy,
            Status = transaction.Status == DebtStatus.Cancelled
                ? "Reversed"
                : transaction.AllocatedAmount <= 0
                    ? "Unallocated"
                    : transaction.AllocatedAmount >= transaction.CreditAmount ? "Allocated" : "Partial",
            Timeline =
            {
                new ArPaymentTimelineDto
                {
                    Timestamp = transaction.TransactionDate,
                    Event = "Posted",
                    Detail = "Khoản thu từ sổ công nợ",
                    Color = "#2563EB"
                }
            }
        };
    }

    private void BuildChartData()
    {
        PaymentByMonthData.Clear();
        var months = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
        var maxVal = 0m;
        var rawValues = new decimal[12];
        for (int i = 0; i < 12; i++)
        {
            rawValues[i] = Items.Where(p => p.PaymentDate.Month == i + 1 && p.PaymentDate.Year == DateTime.Today.Year)
                .Sum(p => p.Amount);
            if (rawValues[i] > maxVal) maxVal = rawValues[i];
        }
        for (int i = 0; i < 12; i++)
            PaymentByMonthData.Add(new ArPaymentChartDataPoint { Label = months[i], Value = rawValues[i], MaxValue = maxVal, Color = "#2563EB" });

        PaymentMethodData.Clear();
        var methodColors = new Dictionary<string, string> { ["Tiền mặt"] = "#16A34A", ["Chuyển khoản"] = "#2563EB", ["Thẻ tín dụng"] = "#7C3AED", ["Thẻ ghi nợ"] = "#0891B2", ["QR"] = "#059669", ["Virtual Card"] = "#0D9488", ["Séc"] = "#F59E0B", ["Voucher"] = "#DC2626" };
        var totalMethod = Items.Count;
        foreach (var kv in methodColors)
        {
            var count = Items.Count(i => i.Method == kv.Key);
            PaymentMethodData.Add(new ArPaymentMethodPieSlice { Label = kv.Key, Value = count, Percentage = totalMethod > 0 ? (double)count / totalMethod * 100 : 0, Color = kv.Value });
        }

        TopCompanyData.Clear();
        var companies = new[] { "Vingroup", "FPT", "Vietcombank", "Petrolimex", "VNPT", "Viettel", "Masan", "TH True Milk", "Techcombank", "BIDV" };
        var compColors = new[] { "#2563EB", "#7C3AED", "#16A34A", "#F59E0B", "#DC2626", "#0891B2", "#059669", "#0D9488", "#E32F70", "#D97706" };
        var totalComp = Items.Count(i => i.CompanyName is not null);
        for (int i = 0; i < companies.Length; i++)
        {
            var count = Items.Count(p => p.CompanyName == companies[i]);
            TopCompanyData.Add(new ArPaymentTopCompany { Name = companies[i], TotalAmount = Items.Where(p => p.CompanyName == companies[i]).Sum(p => p.Amount), PaymentCount = count, Percentage = totalComp > 0 ? (double)count / totalComp * 100 : 0, Color = compColors[i] });
        }

        OutstandingTrendData.Clear();
        for (int i = 0; i < 12; i++)
        {
            var val = Items.Where(p => p.PaymentDate.Month == i + 1 && p.PaymentDate.Year == DateTime.Today.Year).Sum(p => p.Unallocated);
            OutstandingTrendData.Add(new ArPaymentChartDataPoint { Label = months[i], Value = val, MaxValue = Math.Max(val, maxVal), Color = "#DC2626" });
        }

        CollectionTrendData.Clear();
        for (int i = 0; i < 12; i++)
        {
            var val = Items.Where(p => p.PaymentDate.Month == i + 1 && p.PaymentDate.Year == DateTime.Today.Year && p.Status == "Completed").Sum(p => p.Amount);
            CollectionTrendData.Add(new ArPaymentChartDataPoint { Label = months[i], Value = val, MaxValue = Math.Max(val, maxVal), Color = "#16A34A" });
        }

        CollectorPerformanceData.Clear();
        var collectors = new[] { "Nguyễn Văn Thu Ngân", "Trần Thị Cashier", "Lê Văn Teller", "Phạm Thị Payment" };
        var collColors = new[] { "#2563EB", "#16A34A", "#F59E0B", "#7C3AED" };
        for (int i = 0; i < collectors.Length; i++)
        {
            var name = collectors[i];
            var total = Items.Count(p => p.Collector == name);
            var completed = Items.Count(p => p.Collector == name && p.Status == "Completed");
            CollectorPerformanceData.Add(new ArPaymentCollectorStat { Name = name, TotalPayments = total, TotalCollected = Items.Where(p => p.Collector == name).Sum(p => p.Amount), OnTimeCount = completed, LateCount = Items.Count(p => p.Collector == name && p.Status is "Pending" or "Partial"), CollectionRate = total > 0 ? (double)completed / total * 100 : 0, Color = collColors[i] });
        }
    }

    [RelayCommand] private void ApplyFilters()
    {
        var q = Items.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s))
            q = q.Where(r => r.PaymentNo.ToLower().Contains(s) || (r.GuestName?.ToLower().Contains(s) ?? false) || (r.CompanyName?.ToLower().Contains(s) ?? false) || (r.InvoiceNo?.ToLower().Contains(s) ?? false) || (r.Reference?.ToLower().Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.Status == FilterStatus);
        if (!string.IsNullOrEmpty(FilterMethod) && FilterMethod != "Tất cả") q = q.Where(r => r.Method == FilterMethod);
        if (!string.IsNullOrEmpty(FilterCurrency) && FilterCurrency != "Tất cả") q = q.Where(r => r.Currency == FilterCurrency);
        if (!string.IsNullOrEmpty(FilterCollector) && FilterCollector != "Tất cả") q = q.Where(r => r.Collector == FilterCollector);
        if (!string.IsNullOrEmpty(FilterBank) && FilterBank != "Tất cả") q = q.Where(r => r.Bank == FilterBank);
        if (!string.IsNullOrEmpty(FilterCashier) && FilterCashier != "Tất cả") q = q.Where(r => r.Cashier == FilterCashier);
        if (!string.IsNullOrEmpty(FilterCompany) && FilterCompany != "Tất cả") q = q.Where(r => r.CompanyName != null && r.CompanyName.Contains(FilterCompany));
        if (!string.IsNullOrEmpty(FilterInvoice) && FilterInvoice != "Tất cả") q = q.Where(r => r.InvoiceNo != null && r.InvoiceNo.Contains(FilterInvoice));
        if (!string.IsNullOrEmpty(FilterPaymentDate) && FilterPaymentDate != "Tất cả")
        {
            q = FilterPaymentDate switch
            {
                "Hôm nay" => q.Where(r => r.PaymentDate.Date == DateTime.Today),
                "Hôm qua" => q.Where(r => r.PaymentDate.Date == DateTime.Today.AddDays(-1)),
                "Tuần này" => q.Where(r => r.PaymentDate >= DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek)),
                "Tháng này" => q.Where(r => r.PaymentDate.Year == DateTime.Today.Year && r.PaymentDate.Month == DateTime.Today.Month),
                "Tháng trước" => q.Where(r => r.PaymentDate.Year == DateTime.Today.AddMonths(-1).Year && r.PaymentDate.Month == DateTime.Today.AddMonths(-1).Month),
                "Quý này" => q.Where(r => r.PaymentDate >= new DateTime(DateTime.Today.Year, ((DateTime.Today.Month - 1) / 3) * 3 + 1, 1)),
                "Năm nay" => q.Where(r => r.PaymentDate.Year == DateTime.Today.Year),
                _ => q
            };
        }
        if (SelectedTreeItem?.FilterKey is not null)
        {
            var k = SelectedTreeItem.FilterKey; var v = SelectedTreeItem.FilterValue;
            q = k switch
            {
                "Status" => q.Where(r => r.Status == v), "Method" => q.Where(r => r.Method == v),
                "OverPayment" => q.Where(r => r.IsOverPayment),
                "Today" => q.Where(r => r.PaymentDate.Date == DateTime.Today),
                "Month" => q.Where(r => r.PaymentDate.Year == DateTime.Today.Year && r.PaymentDate.Month == DateTime.Today.Month),
                _ => q
            };
        }
        var list = q.OrderByDescending(r => r.PaymentDate).ThenBy(r => r.PaymentNo).ToList();
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredItems.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredItems.Add(r);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void ClearFilters() { SearchText = string.Empty; FilterStatus = null; FilterMethod = null; FilterCurrency = null; FilterCollector = null; FilterBank = null; FilterPaymentDate = null; FilterCashier = null; FilterCompany = null; FilterInvoice = null; SelectTreeItem(TreeItems.FirstOrDefault()); ApplyFilters(); }
    [RelayCommand] private void SelectTreeItem(ArPaymentTreeItem? item) { if (item is null) return; foreach (var ti in TreeItems) ti.IsSelected = false; item.IsSelected = true; SelectedTreeItem = item; CurrentPage = 1; ApplyFilters(); }

    // ═══ Enhanced Commands ═══
    [RelayCommand]
    private async Task EmailReceipt()
    {
        if (SelectedItem is null) return;
        await QueueReceiptEmailAsync(SelectedItem);
    }
    [RelayCommand]
    private async Task UndoAllocate()
    {
        if (SelectedItem is null) return;
        if (_debtTransactionService is null)
        {
            InfoMessage = "Chưa có Debt transaction service thật; không hoàn tác phân bổ.";
            return;
        }

        if (!Guid.TryParse(SelectedItem.TransactionId, out var paymentId))
        {
            InfoMessage = "Khoản thu không có mã giao dịch thật để hoàn tác phân bổ.";
            return;
        }

        try
        {
            var count = await _debtTransactionService.UndoPaymentAllocationsAsync(paymentId, SelectedItem.Cashier);
            InfoMessage = count > 0
                ? $"Đã hoàn tác {count} phân bổ trên dữ liệu thật."
                : "Khoản thu chưa có phân bổ để hoàn tác.";
            if (count > 0)
                await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể hoàn tác phân bổ: {ex.Message}";
        }
    }
    [RelayCommand]
    private async Task ReversePayment()
    {
        if (SelectedItem is null) return;
        if (_receivableService is null)
        {
            InfoMessage = "Chưa có Receivable service thật; không đảo ngược giao dịch.";
            return;
        }

        if (SelectedItem.Allocated > 0)
        {
            InfoMessage = "Không thể đảo ngược khoản thu đã phân bổ. Hãy hoàn tác phân bổ bằng command thật trước.";
            return;
        }

        if (!Guid.TryParse(SelectedItem.TransactionId, out _))
        {
            InfoMessage = "Khoản thu không có mã giao dịch thật để đảo ngược.";
            return;
        }

        var cancelled = await _receivableService.CancelPaymentReceiptAsync(SelectedItem.TransactionId);
        if (!cancelled)
        {
            var reopened = await _receivableService.ReopenPaymentReceiptAsync(SelectedItem.TransactionId);
            cancelled = reopened && await _receivableService.CancelPaymentReceiptAsync(SelectedItem.TransactionId);
        }

        InfoMessage = cancelled
            ? $"Đã đảo ngược khoản thu {SelectedItem.PaymentNo} trên dữ liệu thật."
            : "Không thể đảo ngược khoản thu. Kiểm tra trạng thái, quyền hoặc dữ liệu phân bổ.";
        if (cancelled)
            await LoadAsync(CancellationToken.None);
    }
    [RelayCommand]
    private async Task AutoAllocate()
    {
        if (SelectedItem is null) return;
        if (_debtTransactionService is null)
        {
            InfoMessage = "Chưa có Debt transaction service thật; không tự động phân bổ.";
            return;
        }

        if (!Guid.TryParse(SelectedItem.TransactionId, out var paymentId))
        {
            InfoMessage = "Khoản thu không có mã giao dịch thật để tự động phân bổ.";
            return;
        }

        try
        {
            var count = await _debtTransactionService.AutoAllocatePaymentAsync(paymentId, SelectedItem.Cashier);
            InfoMessage = count > 0
                ? $"Đã tự động phân bổ vào {count} khoản công nợ cũ nhất trên dữ liệu thật."
                : "Không có khoản công nợ cùng khách hàng còn số dư để phân bổ.";
            if (count > 0)
                await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể tự động phân bổ: {ex.Message}";
        }
    }
    [RelayCommand] private void HandleOverPayment(string action)
    {
        if (SelectedItem is null) return;
        OverPaymentAction = action;
        OverPaymentMessage = action switch { "Credit" => $"Giữ {SelectedItem.Amount - SelectedItem.Allocated:N0} ₫ làm Credit cho {SelectedItem.CompanyName ?? SelectedItem.GuestName}.", "Refund" => $"Hoàn trả {SelectedItem.Amount - SelectedItem.Allocated:N0} ₫.", "Advance" => $"Chuyển {SelectedItem.Amount - SelectedItem.Allocated:N0} ₫ thành Advance Deposit.", _ => "" };
        IsOverPaymentDialogOpen = false;
        InfoMessage = OverPaymentMessage;
    }
    [RelayCommand] private void OpenOverPaymentDialog() { if (SelectedItem?.IsOverPayment == true) IsOverPaymentDialogOpen = true; }
    [RelayCommand] private void CloseOverPaymentDialog() { IsOverPaymentDialogOpen = false; }

    // ═══ Bank Import ═══
    [RelayCommand]
    private async Task OpenBankImport()
    {
        BankImportRows.Clear();
        BankImportFile = "";
        BankImportStatus = "Đang tải sao kê thật...";
        IsBankImportOpen = true;
        await LoadRealBankStatementsAsync();
    }
    [RelayCommand] private void CloseBankImport() { IsBankImportOpen = false; }

    [RelayCommand]
    private async Task ReloadBankStatements()
    {
        BankImportRows.Clear();
        BankImportFile = string.Empty;
        await LoadRealBankStatementsAsync();
    }

    [RelayCommand]
    private async Task MatchBankRow(ArPaymentBankImportRow? row)
    {
        if (row is null)
        {
            BankImportStatus = "Chưa chọn dòng sao kê.";
            return;
        }
        if (_reconciliationEngine is null)
        {
            BankImportStatus = "Chưa cấu hình reconciliation service thật.";
            return;
        }

        var candidates = Items
            .Where(x => x.Status != "Reversed"
                && decimal.Round(x.Amount, 2) == decimal.Round(row.Amount, 2)
                && Math.Abs((x.PaymentDate.Date - row.TransactionDate.Date).TotalDays) <= 3)
            .ToList();
        ArPmtDto? target = null;
        if (!string.IsNullOrWhiteSpace(row.SelectedPaymentId))
        {
            target = candidates.FirstOrDefault(x => string.Equals(x.TransactionId, row.SelectedPaymentId, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            target = null;
        }
        var referenced = candidates
            .Where(x => HasBankReference(x, row.Reference, row.Description))
            .ToList();
        if (target is null && string.IsNullOrWhiteSpace(row.SelectedPaymentId))
        {
            target = referenced.Count == 1
                ? referenced[0]
                : referenced.Count == 0 && candidates.Count == 1 ? candidates[0] : null;
        }
        if (target is null || !Guid.TryParse(target.TransactionId, out var transactionId))
        {
            BankImportStatus = candidates.Count > 1
                ? "Có nhiều phiếu thu cùng số tiền/ngày; không tự chọn đích. Hãy dùng Auto Match hoặc lọc lại dữ liệu."
                : "Không tìm thấy duy nhất một phiếu thu thật phù hợp để ghép.";
            return;
        }

        try
        {
            var session = await _reconciliationEngine.StartSessionAsync(
                "AR-BankStatement",
                JsonSerializer.Serialize(new { bankStatementId = row.BankStatementId }));
            await _reconciliationEngine.MatchBankStatementLineAsync(
                session.Id,
                row.StatementLineId,
                transactionId,
                target.Cashier ?? target.Collector ?? "system");
            await LoadRealBankStatementsAsync();
            BankImportStatus = $"Đã ghép dòng sao kê với phiếu thu {target.PaymentNo} bằng dữ liệu thật.";
        }
        catch (Exception ex)
        {
            BankImportStatus = $"Không thể ghép dòng sao kê thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AutoMatchBank()
    {
        if (_bankingService is null || _reconciliationEngine is null)
        {
            BankImportStatus = "Chưa cấu hình banking/reconciliation service thật.";
            return;
        }

        try
        {
            var statements = await _bankingService.GetStatementsAsync(null, 100, CancellationToken.None);
            if (statements.Count == 0)
            {
                BankImportStatus = "Chưa có sao kê thật để đối chiếu.";
                return;
            }

            var matched = 0;
            foreach (var statement in statements)
            {
                var session = await _reconciliationEngine.StartSessionAsync(
                    "AR-BankStatement",
                    JsonSerializer.Serialize(new { bankStatementId = statement.Id }));
                matched += await _reconciliationEngine.RunAutoMatchAsync(session.Id);
            }

            await LoadRealBankStatementsAsync();
            BankImportStatus = $"Đã đối chiếu dữ liệu thật: {matched} dòng được khớp; các dòng chưa đủ điều kiện được giữ nguyên để kiểm tra.";
        }
        catch (Exception ex)
        {
            BankImportStatus = $"Không thể đối chiếu sao kê thật: {ex.Message}";
        }
    }

    private async Task LoadRealBankStatementsAsync()
    {
        if (_bankingService is null)
        {
            BankImportStatus = "Chưa cấu hình banking service thật.";
            return;
        }

        try
        {
            var statements = await _bankingService.GetStatementsAsync(null, 100, CancellationToken.None);
            BankImportRows.Clear();
            foreach (var statement in statements)
            {
                foreach (var line in statement.Lines)
                {
                    var amount = line.CreditAmount != 0m ? line.CreditAmount : line.DebitAmount;
                    BankImportRows.Add(new ArPaymentBankImportRow
                    {
                        BankStatementId = statement.Id,
                        StatementLineId = line.Id,
                        TransactionDate = line.TransactionDate,
                        Reference = line.ReferenceNo ?? string.Empty,
                        Description = line.Description ?? string.Empty,
                        Amount = Math.Abs(amount),
                        IsMatched = string.Equals(line.MatchStatus, "Matched", StringComparison.OrdinalIgnoreCase),
                        Currency = "VND"
                    });
                    var row = BankImportRows[^1];
                    foreach (var candidate in FindBankPaymentCandidates(row))
                    {
                        row.CandidatePayments.Add(new ArPaymentBankCandidate
                        {
                            TransactionId = candidate.TransactionId,
                            PaymentNo = candidate.PaymentNo,
                            Label = $"{candidate.PaymentNo} · {candidate.Amount:N0} · {candidate.PaymentDate:dd/MM}"
                        });
                    }
                    if (row.CandidatePayments.Count == 1 && !row.IsMatched)
                    {
                        row.SelectedPaymentId = row.CandidatePayments[0].TransactionId;
                    }
                }
            }

            BankImportStatus = statements.Count == 0
                ? "Chưa có sao kê thật trong hệ thống."
                : $"Đã tải {statements.Count} sao kê thật, {BankImportRows.Count} dòng giao dịch.";
        }
        catch (Exception ex)
        {
            BankImportRows.Clear();
            BankImportStatus = $"Không thể tải sao kê thật: {ex.Message}";
        }
    }

    private static bool HasBankReference(ArPmtDto payment, string? reference, string? description)
    {
        if (string.IsNullOrWhiteSpace(reference) && string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        var values = new[] { payment.PaymentNo, payment.ReceiptNo, payment.Reference }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();
        return values.Any(value =>
            (!string.IsNullOrWhiteSpace(reference) && reference.Contains(value, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(description) && description.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    private List<ArPmtDto> FindBankPaymentCandidates(ArPaymentBankImportRow row)
        => Items
            .Where(x => x.Status != "Reversed"
                && decimal.Round(x.Amount, 2) == decimal.Round(row.Amount, 2)
                && Math.Abs((x.PaymentDate.Date - row.TransactionDate.Date).TotalDays) <= 3)
            .ToList();

    // ═══ Receipt Center ═══
    [RelayCommand] private void OpenReceiptCenter()
    {
        ReceiptCenterItems.Clear();
        foreach (var item in Items.Where(i => i.ReceiptNo is not null).Take(500))
            ReceiptCenterItems.Add(new ArPaymentReceiptCenterItem { ReceiptNo = item.ReceiptNo!, PaymentNo = item.PaymentNo, ReceiptDate = item.PaymentDate, Cashier = item.Cashier, Printer = null, IsPrinted = false, IsEmailed = false, ReprintCount = 0 });
        IsReceiptCenterOpen = true;
    }
    [RelayCommand] private void CloseReceiptCenter() { IsReceiptCenterOpen = false; }
    [RelayCommand]
    private void ReprintReceipt(string? receiptNo)
    {
        var payment = Items.FirstOrDefault(x => string.Equals(x.ReceiptNo, receiptNo, StringComparison.OrdinalIgnoreCase));
        if (payment is null) { InfoMessage = "Không tìm thấy phiếu thu thật để in lại."; return; }
        PrintReceipt(payment);
    }
    [RelayCommand]
    private async Task EmailReceiptCenter(string? receiptNo)
    {
        var payment = Items.FirstOrDefault(x => string.Equals(x.ReceiptNo, receiptNo, StringComparison.OrdinalIgnoreCase));
        if (payment is null)
        {
            InfoMessage = "Không tìm thấy phiếu thu thật để gửi email.";
            return;
        }

        await QueueReceiptEmailAsync(payment);
    }

    private async Task QueueReceiptEmailAsync(ArPmtDto payment)
    {
        if (_customerAppService is null)
        {
            InfoMessage = "Chưa đăng ký dịch vụ khách hàng để lấy email người nhận.";
            return;
        }

        if (!int.TryParse(payment.CustomerId, out var customerId) || customerId <= 0)
        {
            InfoMessage = "Phiếu thu chưa có mã khách hàng thật để tra email.";
            return;
        }

        try
        {
            var customer = await _customerAppService.GetAsync(customerId, CancellationToken.None);
            if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
            {
                InfoMessage = "Khách hàng chưa có email nhận phiếu thu.";
                return;
            }

            if (_emailDispatchService is null)
            {
                InfoMessage = "Chưa đăng ký email outbox.";
                return;
            }

            var queued = await _emailDispatchService.QueueAsync(new EmailDispatchRequestDto
            {
                Recipient = customer.Email.Trim(),
                Subject = $"Phiếu thu {payment.ReceiptNo ?? payment.PaymentNo}",
                Body = $"Kính gửi {customer.CustomerName},\n\nPhiếu thu: {payment.ReceiptNo ?? payment.PaymentNo}\nSố tiền: {payment.Amount:N0} {payment.Currency}\nNgày thu: {payment.PaymentDate:dd/MM/yyyy}\nPhương thức: {payment.Method}\n\nTrân trọng,\nASMILE BO Cashiering",
                SourceType = "ArPayment",
                SourceId = Guid.TryParse(payment.TransactionId, out var transactionId) ? transactionId : null
            }, CancellationToken.None);
            InfoMessage = $"Đã lưu phiếu thu vào hàng đợi email ({queued.Id:N}); gửi khi SMTP được bật.";
        }
        catch (Exception ex) { InfoMessage = $"Không thể lưu email phiếu thu: {ex.Message}"; }
    }

    // ═══ Collection Dashboard ═══
    [RelayCommand] private void OpenCollectionDashboard() { IsCollectionDashboardOpen = true; }
    [RelayCommand] private void CloseCollectionDashboard() { IsCollectionDashboardOpen = false; }

    // ═══ Dashboard Tab ═══
    [RelayCommand] private void SetDashboardTab(int tab) { SelectedDashboardTab = tab; }

    // ═══ Mixed Payment ═══
    [RelayCommand] private void AddPaymentSplit() { PaymentSplits.Add(new ArPaymentSplitItem { Method = "Tiền mặt", Amount = 0 }); IsMixedPayment = true; }
    [RelayCommand] private void RemovePaymentSplit(ArPaymentSplitItem? item) { if (item is not null) { PaymentSplits.Remove(item); IsMixedPayment = PaymentSplits.Count > 0; } }

    // ═══ View Navigation ═══
    [RelayCommand]
    private void ViewInvoice(string? invoiceNo)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo))
        {
            InfoMessage = "Khoản thu đang chọn chưa có số hóa đơn thật.";
            return;
        }

        IsDetailOpen = true;
        SelectedDetailTab = 2;
        InfoMessage = $"Đang xem hóa đơn {invoiceNo} trong chi tiết khoản thu.";
    }

    [RelayCommand]
    private void ViewReceipt(string? receiptNo)
    {
        if (string.IsNullOrWhiteSpace(receiptNo))
        {
            InfoMessage = "Khoản thu đang chọn chưa có số phiếu thu thật.";
            return;
        }

        IsDetailOpen = true;
        SelectedDetailTab = 3;
        InfoMessage = $"Đang xem phiếu thu {receiptNo} trong chi tiết khoản thu.";
    }
    [RelayCommand] private void ViewHistory() { if (SelectedItem is not null) { SelectedDetailTab = 4; InfoMessage = $"Xem lịch sử {SelectedItem.PaymentNo}."; } }
    [RelayCommand]
    private void DownloadPdf()
    {
        var payment = SelectedItem;
        if (payment?.ReceiptNo is not { Length: > 0 } receiptNo)
        {
            InfoMessage = "Khoản thu đang chọn chưa có số phiếu thu thật để xuất PDF.";
            return;
        }

        var path = PdfExportHelper.Export(
            $"Phiếu thu {receiptNo}",
            $"receipt_{receiptNo}",
            ["Trường", "Giá trị"],
            [
                (IReadOnlyList<string>)["Số phiếu", receiptNo],
                ["Mã thanh toán", payment.PaymentNo],
                ["Khách hàng", payment.CompanyName ?? payment.GuestName ?? "—"],
                ["Ngày thu", payment.PaymentDate.ToString("dd/MM/yyyy")],
                ["Phương thức", payment.Method],
                ["Số tiền", $"{payment.Amount:N0} {payment.Currency}"],
                ["Trạng thái", payment.StatusLabel]
            ]);
        if (!string.IsNullOrWhiteSpace(path)) InfoMessage = $"Đã xuất PDF phiếu thu thật: {path}";
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterMethodChanged(string? value) => ApplyFilters();
    partial void OnFilterCurrencyChanged(string? value) => ApplyFilters();
    partial void OnFilterCollectorChanged(string? value) => ApplyFilters();
    partial void OnFilterBankChanged(string? value) => ApplyFilters();
    partial void OnFilterPaymentDateChanged(string? value) => ApplyFilters();
    partial void OnFilterCashierChanged(string? value) => ApplyFilters();
    partial void OnFilterCompanyChanged(string? value) => ApplyFilters();
    partial void OnFilterInvoiceChanged(string? value) => ApplyFilters();

    [RelayCommand] private void FirstPage() { CurrentPage = 1; ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (HasPrevPage) { CurrentPage--; ApplyFilters(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand] private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }
    [RelayCommand] private void SetPage(int? page) { if (page is null) return; CurrentPage = Math.Clamp(page.Value, 1, TotalPages); ApplyFilters(); }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFilters(); }

    private void RebuildPageButtons()
    {
        PageButtons.Clear(); var total = TotalPages; var current = Math.Clamp(CurrentPage, 1, total);
        IEnumerable<int?> pages = total <= 5 ? Enumerable.Range(1, total).Cast<int?>()
            : current <= 3 ? new int?[] { 1, 2, 3, 4, 5, null, total }
            : current >= total - 2 ? new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total }
            : new int?[] { 1, null, current - 1, current, current + 1, null, total };
        foreach (var p in pages) PageButtons.Add(p is null ? new ArPaymentPaginationButton("…", null, false, true) : new ArPaymentPaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == current));
    }

    [RelayCommand] private void SelectItem(ArPmtDto? item) => SelectedItem = item;
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedItem = null; }
    [RelayCommand] private void ToggleRightPanel() => IsRightPanelOpen = !IsRightPanelOpen;

    partial void OnSelectedItemChanged(ArPmtDto? value) { OnPropertyChanged(nameof(HasSelection)); if (value is not null) { IsDetailOpen = true; SelectedDetailTab = 0; } }
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsOverviewTab)); OnPropertyChanged(nameof(IsAllocationTab)); OnPropertyChanged(nameof(IsInvoiceTab));
        OnPropertyChanged(nameof(IsReceiptTab)); OnPropertyChanged(nameof(IsHistoryTab)); OnPropertyChanged(nameof(IsDocumentsTab)); OnPropertyChanged(nameof(IsNotesTab));
        OnPropertyChanged(nameof(IsAuditTab));
    }
    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsAllocationTab => SelectedDetailTab == 1;
    public bool IsInvoiceTab => SelectedDetailTab == 2;
    public bool IsReceiptTab => SelectedDetailTab == 3;
    public bool IsHistoryTab => SelectedDetailTab == 4;
    public bool IsDocumentsTab => SelectedDetailTab == 5;
    public bool IsNotesTab => SelectedDetailTab == 6;
    public bool IsAuditTab => SelectedDetailTab == 7;

    partial void OnFormModeChanged(string value) { OnPropertyChanged(nameof(IsPaymentForm)); OnPropertyChanged(nameof(IsAllocForm)); }
    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(IsFormClosed));
    partial void OnViewModeChanged(string value) => OnPropertyChanged(nameof(IsGridView));

    [RelayCommand] private void SetGridView() { ViewMode = "Grid"; }
    [RelayCommand] private void CloseForm() { IsFormOpen = false; FormErrorMessage = string.Empty; }

    [RelayCommand]
    private async Task OpenPaymentForm()
    {
        FormMode = "Payment"; FormTitle = "Nhận thanh toán mới"; FormErrorMessage = string.Empty;
        PayCompanyName = ""; PayInvoiceNo = ""; PayOutstanding = 0; PayMethod = "Tiền mặt"; PayAmount = 0; PayReference = ""; PayRemark = "";
        SelectedPaymentCustomer = null;
        PaymentSplits.Clear();
        IsMixedPayment = false;
        PaymentCustomers.Clear();
        IsFormOpen = true;

        if (_receivableService is null)
        {
            FormErrorMessage = "Chưa kết nối được dịch vụ khách hàng/công nợ thật.";
            return;
        }

        try
        {
            foreach (var customer in await _receivableService.GetCustomerOptionsAsync())
                PaymentCustomers.Add(customer);
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Không tải được danh sách khách hàng thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SubmitPaymentForm()
    {
        FormErrorMessage = string.Empty;
        if (_receivableService is null)
        {
            FormErrorMessage = "Chưa có Receivable service thật; không tạo giao dịch.";
            return;
        }

        if (SelectedPaymentCustomer is null)
        {
            FormErrorMessage = "Hãy chọn khách hàng từ danh sách dữ liệu thật.";
            return;
        }

        if (PayAmount <= 0)
        {
            FormErrorMessage = "Số tiền phải lớn hơn 0.";
            return;
        }

        var splitRows = PaymentSplits
            .Where(split => split.Amount > 0)
            .ToList();
        if (IsMixedPayment)
        {
            if (splitRows.Count < 2)
            {
                FormErrorMessage = "Thanh toán hỗn hợp phải có ít nhất hai phương thức có số tiền lớn hơn 0.";
                return;
            }

            if (splitRows.Any(split => string.IsNullOrWhiteSpace(split.Method)))
            {
                FormErrorMessage = "Mỗi phương thức thanh toán hỗn hợp phải được chọn đầy đủ.";
                return;
            }

            var splitTotal = splitRows.Sum(split => split.Amount);
            if (Math.Abs(splitTotal - PayAmount) > 0.01m)
            {
                FormErrorMessage = $"Tổng thanh toán ({PayAmount:N2}) phải bằng tổng các phương thức ({splitTotal:N2}).";
                return;
            }
        }

        var persistedMethod = splitRows.Count > 0 ? "Hỗn hợp" : PayMethod;
        var persistedNote = string.IsNullOrWhiteSpace(PayRemark) ? "Phiếu thu công nợ" : PayRemark.Trim();
        if (splitRows.Count > 0)
        {
            var splitDescription = string.Join("; ", splitRows.Select(split => $"{split.Method.Trim()}: {split.Amount:N2}"));
            persistedNote = $"{persistedNote} · Phương thức: {splitDescription}";
        }

        var receiptNo = await _receivableService.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
        {
            CustomerId = SelectedPaymentCustomer.CustomerId,
            ReceiptDate = DateTime.Today,
            Amount = PayAmount,
            PaymentMethod = persistedMethod,
            BankAccount = PayReference,
            Note = persistedNote
        });
        if (string.IsNullOrWhiteSpace(receiptNo))
        {
            FormErrorMessage = "Không tạo được phiếu thu thật. Kiểm tra khách hàng, quyền và cơ sở dữ liệu.";
            return;
        }

        IsFormOpen = false;
        InfoMessage = $"Đã tạo phiếu thu {receiptNo} ở trạng thái chờ duyệt.";
        await LoadAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task OpenAllocForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Alloc"; FormTitle = $"Phân bổ — {SelectedItem.PaymentNo}"; FormErrorMessage = string.Empty;
        SelectedItem.Invoices.Clear();
        if (_receivableService is null)
        {
            FormErrorMessage = "Chưa có Receivable service thật; không tải công nợ để phân bổ.";
        }
        else
        {
            try
            {
                var transactions = await _receivableService.GetTransactionsByCustomerAsync(SelectedItem.CustomerId, null, null);
                foreach (var transaction in transactions.Where(x => x.DebitAmount > 0 && x.BalanceAmount > 0))
                {
                    SelectedItem.Invoices.Add(new ArPaymentInvoiceDto
                    {
                        TransactionId = transaction.Id,
                        InvoiceNo = transaction.DocumentNo,
                        Outstanding = transaction.BalanceAmount,
                        Balance = transaction.BalanceAmount,
                        DueDate = transaction.DueDate ?? transaction.TransactionDate.AddDays(30),
                        Status = transaction.Status.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                FormErrorMessage = $"Không tải được công nợ thật: {ex.Message}";
            }
        }

        AllocInvoiceNo = SelectedItem.Invoices.FirstOrDefault()?.InvoiceNo ?? "";
        AllocAmount = Math.Min(SelectedItem.Unallocated, SelectedItem.Invoices.FirstOrDefault()?.Balance ?? 0);
        IsFormOpen = true;
    }

    [RelayCommand]
    private async Task SubmitAllocForm()
    {
        FormErrorMessage = string.Empty;
        if (SelectedItem is null || _debtTransactionService is null)
        {
            FormErrorMessage = "Chưa cấu hình service phân bổ công nợ thật.";
            return;
        }

        var target = SelectedItem.Invoices.FirstOrDefault(x => string.Equals(x.InvoiceNo, AllocInvoiceNo?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (target is null || !Guid.TryParse(SelectedItem.TransactionId, out var sourceId) || !Guid.TryParse(target.TransactionId, out var targetId))
        {
            FormErrorMessage = "Phải chọn đúng khoản thu nguồn và hóa đơn công nợ tồn tại trong dữ liệu thật.";
            return;
        }

        if (AllocAmount <= 0 || AllocAmount > SelectedItem.Unallocated || AllocAmount > target.Balance)
        {
            FormErrorMessage = "Số tiền phân bổ không hợp lệ hoặc vượt số dư còn lại.";
            return;
        }

        try
        {
            await _debtTransactionService.AllocatePaymentAsync(
                "Receivable", sourceId, null, "Receivable", targetId, AllocAmount, DateTime.UtcNow, "system");
            IsFormOpen = false;
            InfoMessage = $"Đã phân bổ {AllocAmount:N0} từ {SelectedItem.PaymentNo} vào {target.InvoiceNo}.";
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Không thể ghi phân bổ thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (Items.Count == 0)
        {
            InfoMessage = "Không có dữ liệu khoản thu thật để xuất.";
            return;
        }

        var path = CsvExportHelper.Export(
            "ar_payment_report",
            ["Mã thanh toán", "Số phiếu thu", "Khách hàng", "Ngày thu", "Phương thức", "Số tiền", "Đã phân bổ", "Còn lại", "Tiền tệ", "Trạng thái"],
            Items.Select(item => (IReadOnlyList<string>)[
                item.PaymentNo,
                item.ReceiptNo ?? string.Empty,
                item.CompanyName ?? item.GuestName ?? string.Empty,
                item.PaymentDate.ToString("dd/MM/yyyy"),
                item.Method,
                CsvExportHelper.F(item.Amount),
                CsvExportHelper.F(item.Allocated),
                CsvExportHelper.F(item.Unallocated),
                item.Currency,
                item.StatusLabel
            ]).ToList());

        InfoMessage = $"Đã xuất dữ liệu thật dạng CSV (mở bằng Excel): {path}";
    }

    [RelayCommand]
    private void ExportPdf()
    {
        var rows = Items.Where(x => !string.IsNullOrWhiteSpace(x.ReceiptNo)).ToList();
        if (rows.Count == 0) { InfoMessage = "Không có phiếu thu thật để xuất PDF."; return; }
        var path = PdfExportHelper.Export(
            "Báo cáo phiếu thu AR",
            "ar_payment_report",
            ["Số phiếu", "Ngày", "Khách/Đối tác", "Phương thức", "Số tiền", "Đã phân bổ", "Trạng thái"],
            rows.Select(item => (IReadOnlyList<string>)[
                item.ReceiptNo ?? item.PaymentNo,
                item.PaymentDate.ToString("dd/MM/yyyy"),
                item.CompanyName ?? item.GuestName ?? string.Empty,
                item.Method,
                CsvExportHelper.F(item.Amount),
                CsvExportHelper.F(item.Allocated),
                item.StatusLabel]).ToList());
        if (!string.IsNullOrWhiteSpace(path)) InfoMessage = $"Đã xuất PDF báo cáo phiếu thu thật: {path}";
    }

    [RelayCommand]
    private void PrintReceipt()
    {
        if (SelectedItem is null)
        {
            InfoMessage = "Khoản thu đang chọn chưa có số phiếu thu thật để xem trước.";
            return;
        }

        PrintReceipt(SelectedItem);
    }

    private void PrintReceipt(ArPmtDto payment)
    {
        if (payment.ReceiptNo is not { Length: > 0 } receiptNo)
        {
            InfoMessage = "Khoản thu đang chọn chưa có số phiếu thu thật để xem trước.";
            return;
        }

        var preview = new PrintPreviewDialog(
            $"Phiếu thu {receiptNo}",
            $"Số phiếu: {receiptNo}\n" +
            $"Mã thanh toán: {payment.PaymentNo}\n" +
            $"Khách hàng: {payment.CompanyName ?? payment.GuestName ?? "—"}\n" +
            $"Ngày thu: {payment.PaymentDate:dd/MM/yyyy}\n" +
            $"Phương thức: {payment.Method}\n" +
            $"Số tiền: {payment.Amount:N0} {payment.Currency}\n" +
            $"Trạng thái: {payment.StatusLabel}")
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        preview.ShowDialog();
    }
    [RelayCommand] private void RefreshCommand() { _ = LoadAsync(CancellationToken.None); }
}
