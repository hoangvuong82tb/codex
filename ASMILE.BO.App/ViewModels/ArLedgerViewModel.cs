using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class ArLedgerItemDto
{
    public string CustomerId { get; set; } = "";
    public string ArNo { get; set; } = "";
    public string InvoiceNo { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string CompanyCode { get; set; } = "";
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? GuestName { get; set; }
    public string? ReservationNo { get; set; }
    public string? FolioNo { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Outstanding { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal CreditLimit { get; set; }
    public int OverdueDays => DateTime.Today > DueDate ? (DateTime.Today - DueDate).Days : 0;
    public bool IsOverdue => OverdueDays > 0;
    public bool IsCreditLimitExceeded => CreditLimit > 0 && Balance > CreditLimit;

    public string ArType { get; set; } = "Doanh nghiệp";
    public string? SalesPerson { get; set; }
    public string? Market { get; set; }
    public string? Source { get; set; }

    public string Status { get; set; } = "Open";
    public string StatusLabel => Status switch
    {
        "Open" => "Mở",
        "PartiallyPaid" => "Thanh toán một phần",
        "Paid" => "Đã thanh toán",
        "Overdue" => "Quá hạn",
        "Closed" => "Đã khóa",
        "Disputed" => "Đang tranh chấp",
        _ => Status
    };
    public string StatusColor => Status switch
    {
        "Open" => "#F59E0B",
        "PartiallyPaid" => "#2563EB",
        "Paid" => "#16A34A",
        "Overdue" => "#DC2626",
        "Closed" => "#64748B",
        "Disputed" => "#7C3AED",
        _ => "#64748B"
    };
    public string StatusBg => Status switch
    {
        "Open" => "#FFFBEB",
        "PartiallyPaid" => "#DBEAFE",
        "Paid" => "#DCFCE7",
        "Overdue" => "#FEE2E2",
        "Closed" => "#F1F5F9",
        "Disputed" => "#F3E8FF",
        _ => "#F8FAFC"
    };
    public string ArTypeBadge => ArType switch
    {
        "Doanh nghiệp" => "DN",
        "OTA" => "OTA",
        "Travel Agent" => "TA",
        "Government" => "GOV",
        "Corporate" => "CORP",
        "City Ledger" => "CL",
        "Credit Company" => "CC",
        _ => ArType
    };
    public string ArTypeColor => ArType switch
    {
        "Doanh nghiệp" => "#2563EB",
        "OTA" => "#D97706",
        "Travel Agent" => "#0891B2",
        "Government" => "#7C3AED",
        "Corporate" => "#16A34A",
        "City Ledger" => "#DC2626",
        "Credit Company" => "#E32F70",
        _ => "#64748B"
    };
    public string ArTypeBg => ArType switch
    {
        "Doanh nghiệp" => "#DBEAFE",
        "OTA" => "#FFF7ED",
        "Travel Agent" => "#ECFEFF",
        "Government" => "#F3E8FF",
        "Corporate" => "#DCFCE7",
        "City Ledger" => "#FEE2E2",
        "Credit Company" => "#FFF0F5",
        _ => "#F8FAFC"
    };

    public string? WarningText => IsCreditLimitExceeded ? "Credit Limit Exceeded" : IsOverdue ? $"Quá hạn {OverdueDays} ngày" : null;
    public string? WarningColor => IsCreditLimitExceeded ? "#DC2626" : IsOverdue ? "#F59E0B" : null;

    public List<ArInvoiceDto> Invoices { get; set; } = new();
    public List<ArPaymentDto> Payments { get; set; } = new();
    public List<ArTimelineEntry> Timeline { get; set; } = new();
    public List<ArDocumentDto> Documents { get; set; } = new();
    public List<ArNoteDto> Notes { get; set; } = new();
    public List<ArAuditEntry> AuditLog { get; set; } = new();
}

public sealed class ArInvoiceDto
{
    public string InvoiceNo { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Vat { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = "Open";
    public string StatusLabel => Status switch
    {
        "Open" => "Mở", "Paid" => "Đã thanh toán", "PartiallyPaid" => "Một phần",
        "Overdue" => "Quá hạn", "Cancelled" => "Đã hủy", _ => Status
    };
    public string StatusColor => Status switch
    {
        "Open" => "#F59E0B", "Paid" => "#16A34A", "PartiallyPaid" => "#2563EB",
        "Overdue" => "#DC2626", "Cancelled" => "#64748B", _ => "#64748B"
    };
    public string StatusBg => Status switch
    {
        "Open" => "#FFFBEB", "Paid" => "#DCFCE7", "PartiallyPaid" => "#DBEAFE",
        "Overdue" => "#FEE2E2", "Cancelled" => "#F1F5F9", _ => "#F8FAFC"
    };
}

public sealed class ArPaymentDto
{
    public string ReceiptNo { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public string Cashier { get; set; } = "";
    public string Method { get; set; } = "Tiền mặt";
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Remark { get; set; }
}

public sealed class ArTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public sealed class ArDocumentDto
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string TypeLabel => Type switch
    {
        "Invoice" => "Hóa đơn", "Contract" => "Hợp đồng", "PO" => "Đơn đặt hàng",
        "Statement" => "Sao kê", "CreditNote" => "Ghi có", "DebitNote" => "Ghi nợ",
        "Reminder" => "Nhắc nợ", _ => Type
    };
    public DateTime UploadDate { get; set; }
    public string? FileName { get; set; }
}

public sealed class ArNoteDto
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "Internal";
    public string TypeLabel => Type switch
    {
        "Reminder" => "Nhắc nợ", "PhoneCall" => "Gọi điện", "Email" => "Email",
        "Internal" => "Nội bộ", "Accounting" => "Kế toán", _ => Type
    };
    public string Content { get; set; } = "";
    public string Author { get; set; } = "";
}

public sealed class ArAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? Device { get; set; }
}

public sealed class ArKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class ArTreeItem : ObservableObject
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsSelected { get; set; }
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
    public ObservableCollection<ArTreeItem> Children { get; } = new();
}

public sealed class ArPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public ArPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label; PageNumber = pageNumber; IsCurrent = isCurrent; IsEllipsis = isEllipsis;
    }
}

// ═══ VIEW MODEL ═══

public sealed partial class ArLedgerViewModel : ViewModelBase
{
    private readonly IReceivableService _receivableService;
    private readonly IEmailDispatchService? _emailDispatchService;

    public ArLedgerViewModel(IReceivableService receivableService, IEmailDispatchService? emailDispatchService = null)
    {
        _receivableService = receivableService ?? throw new ArgumentNullException(nameof(receivableService));
        _emailDispatchService = emailDispatchService;
    }

    public ObservableCollection<ArLedgerItemDto> Items { get; } = new();
    public ObservableCollection<ArLedgerItemDto> FilteredItems { get; } = new();
    public ObservableCollection<ArKpi> DashboardKpis { get; } = new();
    public ObservableCollection<ArTimelineEntry> ItemTimeline { get; } = new();
    public ObservableCollection<ArAuditEntry> ItemAuditLog { get; } = new();
    public ObservableCollection<ArTreeItem> TreeItems { get; } = new();
    public ObservableCollection<ArPaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private ArLedgerItemDto? _selectedItem;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private ArTreeItem? _selectedTreeItem;
    [ObservableProperty] private string _viewMode = "Grid";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterArType;
    [ObservableProperty] private string? _filterCurrency;
    [ObservableProperty] private string? _filterOverdue;
    [ObservableProperty] private string? _filterMarket;
    [ObservableProperty] private string? _filterSource;
    [ObservableProperty] private string? _filterSalesman;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;

    // ═══ CHART STATE ═══
    [ObservableProperty] private bool _isChartOpen;
    [ObservableProperty] private string _chartTitle = string.Empty;
    [ObservableProperty] private string _chartType = "Bar";

    // ═══ FORM STATE ═══
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formMode = string.Empty;
    [ObservableProperty] private string _formTitle = string.Empty;
    [ObservableProperty] private string _formErrorMessage = string.Empty;

    // Payment form
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private string _paymentMethod = "Tiền mặt";
    [ObservableProperty] private string _paymentCashier = "";
    [ObservableProperty] private string _paymentReference = "";
    [ObservableProperty] private string _paymentRemark = "";
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;

    // Email form
    [ObservableProperty] private string _emailType = "Statement";
    [ObservableProperty] private string _emailTo = "";
    [ObservableProperty] private string _emailSubject = "";
    [ObservableProperty] private string _emailBody = "";

    // Note form
    [ObservableProperty] private string _noteType = "Internal";
    [ObservableProperty] private string _noteContent = "";

    public bool IsRecordPaymentForm => FormMode == "Payment";
    public bool IsEmailForm => FormMode == "Email";
    public bool IsNoteForm => FormMode == "Note";
    public bool IsFormClosed => !IsFormOpen;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0 công nợ" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} công nợ";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedItem is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool IsGridView => ViewMode == "Grid";
    public bool IsCardView => ViewMode == "Card";

    public int TotalItems => Items.Count;
    public int OverdueCount => Items.Count(i => i.Status == "Overdue");
    public int OpenCount => Items.Count(i => i.Status == "Open");
    public int PartiallyPaidCount => Items.Count(i => i.Status == "PartiallyPaid");
    public int PaidCount => Items.Count(i => i.Status == "Paid");
    public int ClosedCount => Items.Count(i => i.Status == "Closed");
    public int DisputedCount => Items.Count(i => i.Status == "Disputed");
    public int DueTodayCount => Items.Count(i => i.DueDate.Date == DateTime.Today && i.Status != "Paid" && i.Status != "Closed");
    public int BusinessCount => Items.Count(i => i.ArType == "Doanh nghiệp");
    public int OtaCount => Items.Count(i => i.ArType == "OTA");
    public int TravelAgentCount => Items.Count(i => i.ArType == "Travel Agent");
    public int CityLedgerCount => Items.Count(i => i.ArType == "City Ledger");
    public int CreditLimitExceededCount => Items.Count(i => i.IsCreditLimitExceeded);

    public decimal TotalOutstanding => Items.Sum(i => i.Outstanding);
    public decimal TotalPaid => Items.Sum(i => i.Paid);
    public decimal TotalBalance => Items.Sum(i => i.Balance);
    public decimal CollectedToday => Items.Sum(i => i.Payments.Where(p => p.PaymentDate.Date == DateTime.Today).Sum(p => p.Amount));
    public decimal CollectedMonth => Items.Sum(i => i.Payments.Where(p => p.PaymentDate.Year == DateTime.Today.Year && p.PaymentDate.Month == DateTime.Today.Month).Sum(p => p.Amount));

    public static string[] StatusFilters { get; } = ["Tất cả", "Open", "PartiallyPaid", "Paid", "Overdue", "Closed", "Disputed"];
    public static string[] ArTypeFilters { get; } = ["Tất cả", "Doanh nghiệp", "OTA", "Travel Agent", "Government", "Corporate", "City Ledger", "Credit Company"];
    public static string[] CurrencyFilters { get; } = ["Tất cả", "VND", "USD", "EUR"];
    public static string[] OverdueFilters { get; } = ["Tất cả", "Quá hạn", "Đến hạn hôm nay", "Chưa đến hạn", "7 ngày", "15 ngày", "30 ngày", "60 ngày", "90+ ngày"];
    public static string[] MarketFilters { get; } = ["Tất cả", "Nội địa", "Quốc tế", "Đông Nam Á", "Châu Âu", "Châu Mỹ", "Đông Bắc Á"];
    public static string[] SourceFilters { get; } = ["Tất cả", "Trực tiếp", "Booking.com", "Agoda", "Expedia", "Traveloka", "Website", "Phone", "Email", "Sales"];
    public static string[] SalesmanFilters { get; } = ["Tất cả", "Nguyễn Văn Kinh Doanh", "Trần Thị Sales", "Lê Văn MKT", "Phạm Thị Business", "Hoàng Văn Corp"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    public static string[] Cashiers { get; } = ["Nguyễn Văn Thu Ngân", "Trần Thị Cashier", "Lê Văn Teller", "Phạm Thị Payment"];
    public static string[] PaymentMethods { get; } = ["Tiền mặt", "Chuyển khoản", "Thẻ tín dụng", "Thẻ ghi nợ", "Ví điện tử", "Séc"];
    public static string[] EmailTypes { get; } = ["Statement", "Invoice", "Reminder", "BalanceConfirmation"];

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Items.Clear();

            // The AR ledger is backed by the receivable service.  Keep the existing
            // presentation model (including its inspector tabs), but never fabricate
            // accounting balances when the database returns no rows.
            var summaries = await _receivableService.GetDebtSummariesAsync(
                new ReceivableFilterDto(), ct);
            foreach (var summary in summaries)
                Items.Add(MapReceivableSummary(summary));

            BuildDashboard(); BuildTree(); ApplyFilters();
            SelectTreeItem(TreeItems.FirstOrDefault());
            return;

#if false // Legacy designer fixture retained for historical reference only.
#pragma warning disable CS0162
            var rng = new Random(42);

            var arTypes = new[] { "Doanh nghiệp", "OTA", "Travel Agent", "Government", "Corporate", "City Ledger", "Credit Company" };
            var statuses = new[] { "Open", "Open", "PartiallyPaid", "PartiallyPaid", "Paid", "Paid", "Paid", "Overdue", "Overdue", "Closed", "Disputed" };
            var salesPeople = new[] { "Nguyễn Văn Kinh Doanh", "Trần Thị Sales", "Lê Văn MKT", "Phạm Thị Business", "Hoàng Văn Corp" };
            var markets = new[] { "Nội địa", "Quốc tế", "Đông Nam Á", "Châu Âu", "Châu Mỹ", "Đông Bắc Á" };
            var sources = new[] { "Trực tiếp", "Booking.com", "Agoda", "Expedia", "Traveloka", "Website", "Phone", "Email", "Sales" };
            var currencies = new[] { "VND", "VND", "VND", "USD", "EUR" };
            var cities = new[] { "Hà Nội", "TP.HCM", "Đà Nẵng", "Nha Trang", "Phú Quốc", "Hải Phòng", "Cần Thơ", "Huế", "Đà Lạt", "Vũng Tàu" };
            var streets = new[] { "Nguyễn Huệ", "Lê Lợi", "Trần Hưng Đạo", "Hai Bà Trưng", "Lý Thường Kiệt", "Phạm Ngũ Lão", "Bùi Viện", "Đồng Khởi" };

            // Company name pools
            var companyPrefixes = new[] { "Công ty TNHH", "Công ty CP", "Tập đoàn", "Tổng công ty", "CT TNHH", "CTCP", "Doanh nghiệp tư nhân" };
            var companySuffixes = new[] { "Thương mại & Dịch vụ", "Du lịch", "Xây dựng", "Vận tải", "Công nghệ", "Sản xuất", "Xuất nhập khẩu", "Đầu tư", "Bất động sản", "Tài chính", "Dầu khí", "Hàng không", "Viễn thông", "Dược phẩm", "May mặc" };
            var companyNames = new[] { "Hòa Bình", "Sao Mai", "Phương Nam", "Đại Việt", "Thăng Long", "Sông Hồng", "Biển Đông", "Trường Sơn", "Tây Nguyên", "Mê Kông", "Hoàng Gia", "Kim Cương", "Ngọc Trai", "Bạch Kim", "Hồng Hà", "Lam Sơn", "Phú Quốc", "Bà Nà", "Hạ Long", "Sơn Trà" };

            // OTA names
            var otaNames = new[] { "Booking.com", "Agoda", "Expedia", "Traveloka", "Trip.com", "Hotels.com", "Priceline", "Kayak" };
            var taNames = new[] { "Saigon Tourist", "Hanoi Travel", "Vietravel", "BenThanh Tourist", "Fiditour", "Buffalo Tours", "APT Travel", "Exotissimo", "Diethelm Travel", "Asian Trails" };
            var govNames = new[] { "UBND TP Hà Nội", "UBND TP.HCM", "Bộ Ngoại giao", "Bộ Quốc phòng", "Bộ Công an", "Tổng cục Du lịch", "Sở VHTTDL Hà Nội", "Sở VHTTDL TP.HCM" };
            var corpNames = new[] { "Vingroup", "FPT", "Vietcombank", "VietinBank", "Petrolimex", "VNPT", "Viettel", "Masan", "TH True Milk", "Vinamilk" };

            var arNo = 10000;

            for (int i = 0; i < 400; i++)
            {
                arNo++;
                var arType = i < 200 ? arTypes[rng.Next(3)] : arTypes[rng.Next(arTypes.Length)];
                var status = statuses[rng.Next(statuses.Length)];
                var currency = currencies[rng.Next(currencies.Length)];

                string companyName, companyCode;
                switch (arType)
                {
                    case "OTA":
                        companyName = otaNames[rng.Next(otaNames.Length)];
                        companyCode = $"OTA{arNo - 10000:D3}";
                        break;
                    case "Travel Agent":
                        companyName = taNames[rng.Next(taNames.Length)];
                        companyCode = $"TA{arNo - 10000:D3}";
                        break;
                    case "Government":
                        companyName = govNames[rng.Next(govNames.Length)];
                        companyCode = $"GOV{arNo - 10000:D3}";
                        break;
                    case "Corporate":
                        companyName = corpNames[rng.Next(corpNames.Length)];
                        companyCode = $"CORP{arNo - 10000:D3}";
                        break;
                    default:
                        var prefix = companyPrefixes[rng.Next(companyPrefixes.Length)];
                        var suffix = companySuffixes[rng.Next(companySuffixes.Length)];
                        var cname = companyNames[rng.Next(companyNames.Length)];
                        companyName = $"{prefix} {cname} {suffix}";
                        companyCode = $"DN{arNo - 10000:D3}";
                        break;
                }

                var issueDate = DateTime.Now.AddDays(-rng.Next(0, 365));
                var dueDate = issueDate.AddDays(rng.Next(15, 90));
                var creditLimit = arType switch
                {
                    "Doanh nghiệp" => rng.Next(50000000, 500000000),
                    "OTA" => rng.Next(100000000, 1000000000),
                    "Travel Agent" => rng.Next(30000000, 300000000),
                    "Government" => rng.Next(200000000, 2000000000),
                    "Corporate" => rng.Next(100000000, 1000000000),
                    "City Ledger" => rng.Next(20000000, 200000000),
                    _ => rng.Next(50000000, 500000000)
                };

                var outstanding = arType switch
                {
                    "Doanh nghiệp" => (decimal)rng.Next(5000000, 200000000),
                    "OTA" => (decimal)rng.Next(10000000, 500000000),
                    "Travel Agent" => (decimal)rng.Next(3000000, 150000000),
                    "Government" => (decimal)rng.Next(20000000, 1000000000),
                    "Corporate" => (decimal)rng.Next(10000000, 500000000),
                    "City Ledger" => (decimal)rng.Next(2000000, 100000000),
                    _ => (decimal)rng.Next(5000000, 200000000)
                };

                decimal paid = 0;
                switch (status)
                {
                    case "Paid":
                        paid = outstanding;
                        outstanding = 0;
                        break;
                    case "PartiallyPaid":
                        paid = outstanding * (decimal)(rng.Next(20, 80) / 100.0);
                        outstanding -= paid;
                        break;
                    case "Overdue":
                        paid = outstanding * (decimal)(rng.Next(0, 30) / 100.0);
                        outstanding -= paid;
                        dueDate = DateTime.Now.AddDays(-rng.Next(1, 120));
                        break;
                }

                if (status == "Overdue" && !arType.StartsWith("City")) { }
                if (status == "Closed" || status == "Paid") { outstanding = 0; paid = outstanding + paid; }

                var item = new ArLedgerItemDto
                {
                    ArNo = $"AR{arNo:D6}",
                    InvoiceNo = $"INV{arNo:D6}",
                    CompanyName = companyName,
                    CompanyCode = companyCode,
                    TaxCode = rng.Next(3) > 0 ? $"MST{1000000000 + rng.Next(0, 999999999):D10}" : null,
                    Address = $"{rng.Next(1, 999)} {streets[rng.Next(streets.Length)]}, {cities[rng.Next(cities.Length)]}",
                    Phone = $"0{rng.Next(100000000, 999999999)}",
                    Email = $"{companyName.Split(' ').LastOrDefault()?.ToLower().Replace(" ", "")}@{rng.Next(3) switch { 0 => "gmail.com", 1 => "yahoo.com", _ => "company.com.vn" }}",
                    GuestName = rng.Next(2) == 1 ? $"Khách {companyName.Split(' ').LastOrDefault() ?? "VIP"}" : null,
                    ReservationNo = rng.Next(2) == 1 ? $"RES{rng.Next(10000, 99999)}" : null,
                    FolioNo = rng.Next(2) == 1 ? $"FOL{rng.Next(1000, 9999)}" : null,
                    IssueDate = issueDate,
                    DueDate = dueDate,
                    Outstanding = outstanding,
                    Paid = paid,
                    Balance = outstanding + paid,
                    Currency = currency,
                    CreditLimit = creditLimit,
                    ArType = arType,
                    SalesPerson = salesPeople[rng.Next(salesPeople.Length)],
                    Market = markets[rng.Next(markets.Length)],
                    Source = sources[rng.Next(sources.Length)],
                    Status = status,
                };

                // Generate invoices for this AR account
                var invCount = rng.Next(3, 18);
                for (int j = 0; j < invCount; j++)
                {
                    var invDate = issueDate.AddDays(rng.Next(-30, 60));
                    var invDue = invDate.AddDays(rng.Next(15, 60));
                    var invAmt = (decimal)rng.Next(1000000, 50000000);
                    var invVat = invAmt * 0.08m;
                    var invPaid = status switch
                    {
                        "Paid" => invAmt + invVat,
                        "PartiallyPaid" => (invAmt + invVat) * (decimal)(rng.Next(20, 80) / 100.0),
                        "Overdue" => (invAmt + invVat) * (decimal)(rng.Next(0, 30) / 100.0),
                        _ => 0
                    };
                    var invStatus = invPaid >= (invAmt + invVat) ? "Paid"
                        : invPaid > 0 ? "PartiallyPaid"
                        : DateTime.Today > invDue ? "Overdue"
                        : "Open";

                    item.Invoices.Add(new ArInvoiceDto
                    {
                        InvoiceNo = $"INV{arNo:D6}-{j + 1:D2}",
                        IssueDate = invDate,
                        DueDate = invDue,
                        Amount = invAmt,
                        Vat = invVat,
                        Paid = invPaid,
                        Balance = invAmt + invVat - invPaid,
                        Status = invStatus,
                    });
                }

                // Generate payments for this AR account
                var pmtCount = rng.Next(0, status switch { "Paid" => 15, "PartiallyPaid" => 10, "Overdue" => 5, _ => 3 });
                for (int k = 0; k < pmtCount; k++)
                {
                    item.Payments.Add(new ArPaymentDto
                    {
                        ReceiptNo = $"PT{arNo:D6}-{k + 1:D3}",
                        PaymentDate = issueDate.AddDays(rng.Next(1, 180)),
                        Cashier = Cashiers[rng.Next(Cashiers.Length)],
                        Method = PaymentMethods[rng.Next(PaymentMethods.Length)],
                        Amount = (decimal)rng.Next(500000, 20000000),
                        Reference = rng.Next(2) == 1 ? $"REF{rng.Next(10000, 99999)}" : null,
                        Remark = rng.Next(3) == 1 ? $"Thanh toán đợt {k + 1}" : null,
                    });
                }

                // Generate timeline
                item.Timeline.Add(new ArTimelineEntry { Timestamp = issueDate, Event = "Tạo Invoice", Detail = $"Invoice {item.InvoiceNo} — {item.Outstanding + item.Paid:N0} {currency}", Color = "#2563EB" });
                if (item.Invoices.Count > 0)
                    item.Timeline.Add(new ArTimelineEntry { Timestamp = issueDate.AddDays(1), Event = "Invoice Created", Detail = $"{item.Invoices.Count} hóa đơn được tạo", Color = "#0891B2" });
                if (status != "Open" && item.Payments.Count > 0)
                    item.Timeline.Add(new ArTimelineEntry { Timestamp = issueDate.AddDays(rng.Next(5, 30)), Event = "Payment Received", Detail = $"Đã nhận {item.Paid:N0} {currency}", Color = "#16A34A" });
                if (status == "Overdue")
                    item.Timeline.Add(new ArTimelineEntry { Timestamp = dueDate.AddDays(1), Event = "Overdue", Detail = $"Quá hạn — {item.OverdueDays} ngày", Color = "#DC2626" });
                if (status == "Closed")
                    item.Timeline.Add(new ArTimelineEntry { Timestamp = issueDate.AddDays(rng.Next(60, 120)), Event = "Closed", Detail = "Công nợ đã được khóa", Color = "#64748B" });

                // Generate audit log
                item.AuditLog.Add(new ArAuditEntry { Timestamp = issueDate, Action = "Created", Actor = item.SalesPerson ?? "System", Detail = $"Tạo công nợ {item.ArNo}", Device = "Front Office" });
                if (item.Payments.Count > 0)
                    item.AuditLog.Add(new ArAuditEntry { Timestamp = issueDate.AddDays(rng.Next(5, 30)), Action = "Payment", Actor = Cashiers[rng.Next(Cashiers.Length)], Detail = $"Thu {item.Paid:N0} {currency}", Device = "Cashiering" });

                // Generate documents
                if (rng.Next(2) == 1)
                    item.Documents.Add(new ArDocumentDto { Name = $"Invoice_{item.InvoiceNo}", Type = "Invoice", UploadDate = issueDate, FileName = $"invoice_{item.InvoiceNo}.pdf" });
                if (rng.Next(3) == 1)
                    item.Documents.Add(new ArDocumentDto { Name = $"Contract_{item.CompanyCode}", Type = "Contract", UploadDate = issueDate.AddDays(-30), FileName = $"contract_{item.CompanyCode}.pdf" });
                if (rng.Next(4) == 1)
                    item.Documents.Add(new ArDocumentDto { Name = $"Statement_{item.ArNo}_{DateTime.Now:MM/yyyy}", Type = "Statement", UploadDate = DateTime.Now.AddDays(-rng.Next(0, 60)), FileName = $"statement_{item.ArNo}.pdf" });
                if (status == "Overdue" && rng.Next(2) == 1)
                    item.Documents.Add(new ArDocumentDto { Name = $"Reminder_{item.ArNo}", Type = "Reminder", UploadDate = DateTime.Now.AddDays(-rng.Next(0, 30)), FileName = $"reminder_{item.ArNo}.pdf" });

                // Generate notes
                if (rng.Next(3) == 1)
                    item.Notes.Add(new ArNoteDto { Timestamp = issueDate.AddDays(rng.Next(1, 30)), Type = "Internal", Content = $"Ghi chú nội bộ: Theo dõi thanh toán từ {companyName}", Author = item.SalesPerson ?? "Admin" });
                if (status == "Overdue" && rng.Next(2) == 1)
                    item.Notes.Add(new ArNoteDto { Timestamp = DateTime.Now.AddDays(-rng.Next(0, 15)), Type = "Reminder", Content = $"Đã gửi nhắc nợ lần {rng.Next(1, 4)} — Quá hạn {item.OverdueDays} ngày", Author = "Hệ thống" });
                if (rng.Next(4) == 1)
                    item.Notes.Add(new ArNoteDto { Timestamp = issueDate.AddDays(rng.Next(10, 60)), Type = "PhoneCall", Content = $"Đã gọi điện trao đổi với {companyName} về lịch thanh toán", Author = item.SalesPerson ?? "Admin" });

                Items.Add(item);
            }

            BuildDashboard(); BuildTree(); ApplyFilters();
            SelectTreeItem(TreeItems.FirstOrDefault());
#pragma warning restore CS0162
#endif
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static ArLedgerItemDto MapReceivableSummary(ReceivableSummaryDto summary)
    {
        var lastDate = summary.LastTransactionDate?.Date ?? DateTime.Today;
        var status = summary.Status switch
        {
            DebtStatus.PartiallyPaid => "PartiallyPaid",
            DebtStatus.Paid => "Paid",
            DebtStatus.Overdue => "Overdue",
            DebtStatus.Cancelled => "Closed",
            _ => "Open"
        };

        var code = summary.CustomerCode ?? string.Empty;
        var arType = code.StartsWith("CORP", StringComparison.OrdinalIgnoreCase)
            ? "Doanh nghiệp"
            : "City Ledger";

        return new ArLedgerItemDto
        {
            CustomerId = summary.CustomerId,
            ArNo = string.IsNullOrWhiteSpace(code) ? summary.CustomerId : code,
            CompanyCode = code,
            CompanyName = summary.CustomerName,
            GuestName = summary.CustomerName,
            IssueDate = lastDate,
            DueDate = lastDate,
            Outstanding = Math.Max(0, summary.ClosingBalance),
            Paid = summary.TotalCredit,
            Balance = summary.ClosingBalance,
            Currency = "VND",
            ArType = arType,
            Status = status
        };
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new ArKpi { Label = "Tổng công nợ", Value = $"{TotalOutstanding:N0} ₫", Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new ArKpi { Label = "Quá hạn", Value = OverdueCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Status", FilterValue = "Overdue" });
        DashboardKpis.Add(new ArKpi { Label = "Chưa đến hạn", Value = OpenCount.ToString(), Color = "#F59E0B", BgColor = "#FFFBEB", FilterKey = "Status", FilterValue = "Open" });
        DashboardKpis.Add(new ArKpi { Label = "Đã thu hôm nay", Value = $"{CollectedToday:N0} ₫", Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new ArKpi { Label = "Đã thu tháng", Value = $"{CollectedMonth:N0} ₫", Color = "#0891B2", BgColor = "#ECFEFF" });
        DashboardKpis.Add(new ArKpi { Label = "Invoice mở", Value = Items.Sum(i => i.Invoices.Count(inv => inv.Status == "Open")).ToString(), Color = "#2563EB", BgColor = "#EFF6FF" });
        DashboardKpis.Add(new ArKpi { Label = "Invoice quá hạn", Value = Items.Sum(i => i.Invoices.Count(inv => inv.Status == "Overdue")).ToString(), Color = "#E32F70", BgColor = "#FFF0F5" });
        DashboardKpis.Add(new ArKpi { Label = "Doanh nghiệp", Value = BusinessCount.ToString(), Color = "#2563EB", BgColor = "#DBEAFE", FilterKey = "ArType", FilterValue = "Doanh nghiệp" });
        DashboardKpis.Add(new ArKpi { Label = "OTA", Value = OtaCount.ToString(), Color = "#D97706", BgColor = "#FFF7ED", FilterKey = "ArType", FilterValue = "OTA" });
        DashboardKpis.Add(new ArKpi { Label = "Travel Agent", Value = TravelAgentCount.ToString(), Color = "#0891B2", BgColor = "#ECFEFF", FilterKey = "ArType", FilterValue = "Travel Agent" });
        DashboardKpis.Add(new ArKpi { Label = "City Ledger", Value = CityLedgerCount.ToString(), Color = "#DC2626", BgColor = "#FEE2E2", FilterKey = "ArType", FilterValue = "City Ledger" });
        DashboardKpis.Add(new ArKpi { Label = "Vượt Credit Limit", Value = CreditLimitExceededCount.ToString(), Color = "#E32F70", BgColor = "#FFF0F5" });
    }

    private void BuildTree()
    {
        TreeItems.Clear();
        TreeItems.Add(new ArTreeItem { Label = "Tất cả", Count = TotalItems, IsSelected = true });
        TreeItems.Add(new ArTreeItem { Label = "Doanh nghiệp", Count = BusinessCount, FilterKey = "ArType", FilterValue = "Doanh nghiệp" });
        TreeItems.Add(new ArTreeItem { Label = "OTA", Count = OtaCount, FilterKey = "ArType", FilterValue = "OTA" });
        TreeItems.Add(new ArTreeItem { Label = "Travel Agent", Count = TravelAgentCount, FilterKey = "ArType", FilterValue = "Travel Agent" });
        TreeItems.Add(new ArTreeItem { Label = "Government", Count = Items.Count(i => i.ArType == "Government"), FilterKey = "ArType", FilterValue = "Government" });
        TreeItems.Add(new ArTreeItem { Label = "Corporate", Count = Items.Count(i => i.ArType == "Corporate"), FilterKey = "ArType", FilterValue = "Corporate" });
        TreeItems.Add(new ArTreeItem { Label = "City Ledger", Count = CityLedgerCount, FilterKey = "ArType", FilterValue = "City Ledger" });
        TreeItems.Add(new ArTreeItem { Label = "Quá hạn", Count = OverdueCount, FilterKey = "Status", FilterValue = "Overdue" });
        TreeItems.Add(new ArTreeItem { Label = "Đến hạn", Count = DueTodayCount, FilterKey = "DueToday", FilterValue = "true" });
        TreeItems.Add(new ArTreeItem { Label = "Đã khóa", Count = ClosedCount, FilterKey = "Status", FilterValue = "Closed" });
    }

    private void BuildTimeline(ArLedgerItemDto item) { ItemTimeline.Clear(); foreach (var e in item.Timeline.OrderBy(t => t.Timestamp)) ItemTimeline.Add(e); }
    private void BuildAuditLog(ArLedgerItemDto item) { ItemAuditLog.Clear(); foreach (var e in item.AuditLog.OrderBy(t => t.Timestamp)) ItemAuditLog.Add(e); }

    // ═══ FILTERS ═══

    [RelayCommand] private void ApplyFilters()
    {
        var q = Items.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s))
            q = q.Where(r => r.ArNo.ToLower().Contains(s) || r.CompanyName.ToLower().Contains(s) || r.InvoiceNo.ToLower().Contains(s) || (r.GuestName?.ToLower().Contains(s) ?? false) || (r.FolioNo?.ToLower().Contains(s) ?? false) || (r.ReservationNo?.ToLower().Contains(s) ?? false) || (r.TaxCode?.ToLower().Contains(s) ?? false) || (r.Phone?.Contains(s) ?? false) || (r.Email?.ToLower().Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.Status == FilterStatus);
        if (!string.IsNullOrEmpty(FilterArType) && FilterArType != "Tất cả") q = q.Where(r => r.ArType == FilterArType);
        if (!string.IsNullOrEmpty(FilterCurrency) && FilterCurrency != "Tất cả") q = q.Where(r => r.Currency == FilterCurrency);
        if (!string.IsNullOrEmpty(FilterMarket) && FilterMarket != "Tất cả") q = q.Where(r => r.Market == FilterMarket);
        if (!string.IsNullOrEmpty(FilterSource) && FilterSource != "Tất cả") q = q.Where(r => r.Source == FilterSource);
        if (!string.IsNullOrEmpty(FilterSalesman) && FilterSalesman != "Tất cả") q = q.Where(r => r.SalesPerson == FilterSalesman);
        if (!string.IsNullOrEmpty(FilterOverdue) && FilterOverdue != "Tất cả")
            q = FilterOverdue switch
            {
                "Quá hạn" => q.Where(r => r.IsOverdue),
                "Đến hạn hôm nay" => q.Where(r => r.DueDate.Date == DateTime.Today && r.Status != "Paid" && r.Status != "Closed"),
                "Chưa đến hạn" => q.Where(r => r.DueDate.Date > DateTime.Today && r.Status != "Paid" && r.Status != "Closed"),
                "7 ngày" => q.Where(r => r.OverdueDays >= 1 && r.OverdueDays <= 7),
                "15 ngày" => q.Where(r => r.OverdueDays >= 8 && r.OverdueDays <= 15),
                "30 ngày" => q.Where(r => r.OverdueDays >= 16 && r.OverdueDays <= 30),
                "60 ngày" => q.Where(r => r.OverdueDays >= 31 && r.OverdueDays <= 60),
                "90+ ngày" => q.Where(r => r.OverdueDays > 60),
                _ => q
            };
        if (SelectedTreeItem?.FilterKey is not null)
        {
            var k = SelectedTreeItem.FilterKey; var v = SelectedTreeItem.FilterValue;
            q = k switch
            {
                "Status" => q.Where(r => r.Status == v),
                "ArType" => q.Where(r => r.ArType == v),
                "DueToday" => q.Where(r => r.DueDate.Date == DateTime.Today && r.Status != "Paid" && r.Status != "Closed"),
                _ => q
            };
        }
        var list = q.OrderByDescending(r => r.OverdueDays).ThenByDescending(r => r.Outstanding).ThenBy(r => r.ArNo).ToList();
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredItems.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredItems.Add(r);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void ClearFilters()
    {
        SearchText = string.Empty; FilterStatus = null; FilterArType = null; FilterCurrency = null;
        FilterOverdue = null; FilterMarket = null; FilterSource = null; FilterSalesman = null;
        SelectTreeItem(TreeItems.FirstOrDefault()); ApplyFilters();
    }

    [RelayCommand] private void SelectTreeItem(ArTreeItem? item)
    {
        if (item is null) return;
        foreach (var ti in TreeItems) DeselectTree(ti);
        item.IsSelected = true; SelectedTreeItem = item; CurrentPage = 1; ApplyFilters();
    }
    private static void DeselectTree(ArTreeItem item) { item.IsSelected = false; foreach (var c in item.Children) DeselectTree(c); }

    [RelayCommand] private void ApplyKpiFilter(ArKpi? kpi)
    {
        if (kpi?.FilterKey is null) return; ClearFilters();
        if (kpi.FilterKey == "Status") FilterStatus = kpi.FilterValue;
        else if (kpi.FilterKey == "ArType") FilterArType = kpi.FilterValue;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterArTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterCurrencyChanged(string? value) => ApplyFilters();
    partial void OnFilterOverdueChanged(string? value) => ApplyFilters();
    partial void OnFilterMarketChanged(string? value) => ApplyFilters();
    partial void OnFilterSourceChanged(string? value) => ApplyFilters();
    partial void OnFilterSalesmanChanged(string? value) => ApplyFilters();

    // ═══ PAGINATION ═══

    [RelayCommand] private void FirstPage() { CurrentPage = 1; ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (HasPrevPage) { CurrentPage--; ApplyFilters(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand] private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }
    [RelayCommand] private void SetPage(int? page) { if (page is null) return; CurrentPage = Math.Clamp(page.Value, 1, TotalPages); ApplyFilters(); }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFilters(); }
    partial void OnViewModeChanged(string value) { OnPropertyChanged(nameof(IsGridView)); OnPropertyChanged(nameof(IsCardView)); }

    private void RebuildPageButtons()
    {
        PageButtons.Clear();
        var total = TotalPages; var current = Math.Clamp(CurrentPage, 1, total);
        IEnumerable<int?> pages = total <= 5 ? Enumerable.Range(1, total).Cast<int?>()
            : current <= 3 ? new int?[] { 1, 2, 3, 4, 5, null, total }
            : current >= total - 2 ? new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total }
            : new int?[] { 1, null, current - 1, current, current + 1, null, total };
        foreach (var p in pages)
            PageButtons.Add(p is null ? new ArPaginationButton("…", null, false, true)
                : new ArPaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == current));
    }

    // ═══ SELECTION ═══

    [RelayCommand] private void SelectItem(ArLedgerItemDto? item) => SelectedItem = item;
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedItem = null; ItemTimeline.Clear(); ItemAuditLog.Clear(); }
    partial void OnSelectedItemChanged(ArLedgerItemDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        if (value is not null)
        {
            IsDetailOpen = true; SelectedDetailTab = 0;
            BuildTimeline(value); BuildAuditLog(value);
        }
    }
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsOverviewTab)); OnPropertyChanged(nameof(IsInvoicesTab));
        OnPropertyChanged(nameof(IsPaymentsTab)); OnPropertyChanged(nameof(IsHistoryTab));
        OnPropertyChanged(nameof(IsDocumentsTab)); OnPropertyChanged(nameof(IsNotesTab));
    }
    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsInvoicesTab => SelectedDetailTab == 1;
    public bool IsPaymentsTab => SelectedDetailTab == 2;
    public bool IsHistoryTab => SelectedDetailTab == 3;
    public bool IsDocumentsTab => SelectedDetailTab == 4;
    public bool IsNotesTab => SelectedDetailTab == 5;
    partial void OnFormModeChanged(string value) { OnPropertyChanged(nameof(IsRecordPaymentForm)); OnPropertyChanged(nameof(IsEmailForm)); OnPropertyChanged(nameof(IsNoteForm)); }
    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(IsFormClosed));

    [RelayCommand] private void CloseForm() { IsFormOpen = false; FormErrorMessage = string.Empty; }
    [RelayCommand] private void OpenDetail() { if (SelectedItem is not null) IsDetailOpen = true; }

    // ═══ WORKFLOW ═══

    [RelayCommand] private void OpenPaymentForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Payment"; FormTitle = $"Thu tiền — {SelectedItem.CompanyName}"; FormErrorMessage = string.Empty;
        PaymentAmount = SelectedItem.Outstanding > 0 ? Math.Min(SelectedItem.Outstanding, 10000000) : 0;
        PaymentMethod = "Tiền mặt"; PaymentCashier = Cashiers[0]; PaymentReference = ""; PaymentRemark = "";
        PaymentDate = DateTime.Today;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitPaymentForm()
    {
        if (SelectedItem is null) return;
        if (PaymentAmount <= 0) { FormErrorMessage = "Vui lòng nhập số tiền thu."; return; }
        if (PaymentAmount > SelectedItem.Outstanding) { FormErrorMessage = "Không thể thu lớn hơn số dư công nợ."; return; }
        if (string.IsNullOrWhiteSpace(SelectedItem.CustomerId))
        {
            FormErrorMessage = "Chưa thể thu tiền: bản ghi công nợ chưa có mã khách hàng thật.";
            return;
        }

        var customerName = SelectedItem.CompanyName;
        var amount = PaymentAmount;
        var receiptNo = await _receivableService.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
        {
            CustomerId = SelectedItem.CustomerId,
            ReceiptDate = PaymentDate,
            Amount = amount,
            PaymentMethod = PaymentMethod,
            Note = string.IsNullOrWhiteSpace(PaymentRemark) ? $"Thu công nợ {SelectedItem.ArNo}" : PaymentRemark
        }, CancellationToken.None);

        if (string.IsNullOrWhiteSpace(receiptNo))
        {
            FormErrorMessage = "Không tạo được phiếu thu trên dữ liệu thật.";
            return;
        }

        IsFormOpen = false;
        await LoadAsync(CancellationToken.None);
        InfoMessage = $"Đã tạo phiếu thu {receiptNo} — {amount:N0} ₫ từ {customerName}.";
    }

    [RelayCommand] private async Task CloseAr()
    {
        if (SelectedItem is null) return;
        if (SelectedItem.Invoices.Any(i => i.Status == "Open" || i.Status == "Overdue")) { InfoMessage = "Không thể đóng công nợ còn Invoice mở hoặc quá hạn."; return; }

        var customerId = SelectedItem.CustomerId;
        var companyName = SelectedItem.CompanyName;
        var closed = await _receivableService.CloseReceivableAsync(customerId, CancellationToken.None);
        if (!closed)
        {
            InfoMessage = "Không thể đóng công nợ: chưa thanh toán hết hoặc chưa có giao dịch công nợ thật.";
            return;
        }

        IsFormOpen = false;
        await LoadAsync(CancellationToken.None);
        InfoMessage = $"Đã đóng công nợ {companyName} trên dữ liệu thật.";
    }

    [RelayCommand] private void OpenEmailForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Email"; FormTitle = $"Gửi Email — {SelectedItem.CompanyName}"; FormErrorMessage = string.Empty;
        EmailType = "Statement";
        EmailTo = SelectedItem.Email ?? "";
        EmailSubject = $"Sao kê công nợ — {SelectedItem.CompanyName}";
        EmailBody = $"Kính gửi {SelectedItem.CompanyName},\n\nVui lòng xem sao kê công nợ đính kèm.\nSố dư hiện tại: {SelectedItem.Outstanding:N0} {SelectedItem.Currency}\n\nTrân trọng,\nASMILE BO Accounting";
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitEmailForm()
    {
        if (SelectedItem is null) return;
        if (string.IsNullOrWhiteSpace(EmailTo)) { FormErrorMessage = "Vui lòng nhập email người nhận."; return; }
        if (_emailDispatchService is null) { FormErrorMessage = "Chưa đăng ký email outbox."; return; }
        try
        {
            var queued = await _emailDispatchService.QueueAsync(new EmailDispatchRequestDto
            {
                Recipient = EmailTo.Trim(),
                Subject = EmailSubject,
                Body = EmailBody,
                SourceType = "ArLedger",
                SourceId = null
            }, CancellationToken.None);
            IsFormOpen = false;
            InfoMessage = $"Đã lưu email sao kê vào hàng đợi ({queued.Id:N}); gửi khi SMTP được bật.";
        }
        catch (Exception ex) { FormErrorMessage = $"Không thể lưu email: {ex.Message}"; }
    }

    [RelayCommand] private void OpenNoteForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Note"; FormTitle = $"Thêm ghi chú — {SelectedItem.CompanyName}"; FormErrorMessage = string.Empty;
        NoteType = "Internal"; NoteContent = "";
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitNoteForm()
    {
        if (SelectedItem is null) return;
        if (string.IsNullOrWhiteSpace(NoteContent)) { FormErrorMessage = "Vui lòng nhập nội dung ghi chú."; return; }

        var saved = await _receivableService.AddReceivableNoteAsync(
            SelectedItem.CustomerId,
            NoteType,
            NoteContent,
            CancellationToken.None);
        if (!saved)
        {
            FormErrorMessage = "Không thể lưu ghi chú: công nợ chưa có mã khách hàng thật.";
            return;
        }

        SelectedItem.Notes.Add(new ArNoteDto
        {
            Timestamp = DateTime.Now,
            Type = NoteType,
            Content = NoteContent.Trim(),
            Author = "system"
        });
        IsFormOpen = false;
        InfoMessage = "Đã lưu ghi chú công nợ vào AuditLog.";
    }

    // ═══ EXPORT ═══

    [RelayCommand] private void ExportExcel()
    {
        if (Items.Count == 0)
        {
            InfoMessage = "Không có công nợ thật để xuất.";
            return;
        }

        var path = CsvExportHelper.Export(
            "ar_ledger",
            ["Mã AR", "Hóa đơn", "Doanh nghiệp", "MST", "Khách", "Reservation", "Folio", "Ngày phát hành", "Hạn thanh toán", "Công nợ", "Đã thu", "Số dư", "Tiền tệ", "Trạng thái", "Quá hạn (ngày)"],
            Items.Select(item => (IReadOnlyList<string>)[
                item.ArNo,
                item.InvoiceNo,
                item.CompanyName,
                item.TaxCode ?? string.Empty,
                item.GuestName ?? string.Empty,
                item.ReservationNo ?? string.Empty,
                item.FolioNo ?? string.Empty,
                item.IssueDate.ToString("dd/MM/yyyy"),
                item.DueDate.ToString("dd/MM/yyyy"),
                CsvExportHelper.F(item.Outstanding),
                CsvExportHelper.F(item.Paid),
                CsvExportHelper.F(item.Balance),
                item.Currency,
                item.StatusLabel,
                item.OverdueDays.ToString()
            ]).ToList());

        InfoMessage = $"Đã xuất sổ công nợ thật dạng CSV (mở bằng Excel): {path}";
    }

    [RelayCommand] private void ExportPdf()
    {
        if (Items.Count == 0) { InfoMessage = "Không có công nợ thật để xuất PDF."; return; }
        var path = PdfExportHelper.Export(
            "Sổ công nợ phải thu",
            "ar_ledger",
            ["Mã AR", "Invoice", "Đối tác", "Số dư", "Tiền tệ", "Trạng thái"],
            Items.Select(item => (IReadOnlyList<string>)[
                item.ArNo, item.InvoiceNo, item.CompanyName,
                CsvExportHelper.F(item.Balance), item.Currency, item.StatusLabel]).ToList());
        if (!string.IsNullOrWhiteSpace(path)) InfoMessage = $"Đã xuất PDF sổ công nợ thật: {path}";
    }

    [RelayCommand] private void PrintStatement()
    {
        if (Items.Count == 0) { InfoMessage = "Không có công nợ thật để in."; return; }
        var content = new StringBuilder().AppendLine("SỔ CÔNG NỢ PHẢI THU").AppendLine(new string('-', 110));
        foreach (var item in Items)
            content.AppendLine($"{item.ArNo,-16} | {item.InvoiceNo,-16} | {item.CompanyName,-28} | Số dư {item.Balance,15:N0} {item.Currency} | {item.StatusLabel}");
        var preview = new PrintPreviewDialog("Sổ công nợ phải thu", content.ToString()) { Owner = System.Windows.Application.Current.MainWindow };
        preview.ShowDialog();
        InfoMessage = "Đã mở bản xem trước từ dữ liệu AR Ledger thật.";
    }

    [RelayCommand] private void RefreshCommand()
    {
        _ = LoadAsync(CancellationToken.None);
    }

    [RelayCommand] private void SetGridView() { ViewMode = "Grid"; }
    [RelayCommand] private void SetCardView() { ViewMode = "Card"; }
}
