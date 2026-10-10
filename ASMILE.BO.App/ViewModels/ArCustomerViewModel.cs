using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class ArCustomerDto
{
    public string CustomerId { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? ShortName { get; set; }
    public string? EnglishName { get; set; }
    public string CustomerType { get; set; } = "Corporate";
    public string TypeLabel => CustomerType switch
    {
        "Corporate" => "Doanh nghiệp", "TravelAgent" => "Travel Agent", "Company" => "Công ty",
        "Government" => "Chính phủ", "VIP" => "VIP", "Individual" => "Cá nhân",
        "OTA" => "OTA", "WalkIn" => "Walk-in", _ => CustomerType
    };
    public string TypeColor => CustomerType switch
    {
        "Corporate" => "#2563EB", "TravelAgent" => "#0891B2", "Company" => "#D97706",
        "Government" => "#7C3AED", "VIP" => "#E32F70", "Individual" => "#16A34A",
        "OTA" => "#F59E0B", "WalkIn" => "#64748B", _ => "#64748B"
    };
    public string TypeBg => CustomerType switch
    {
        "Corporate" => "#DBEAFE", "TravelAgent" => "#ECFEFF", "Company" => "#FFF7ED",
        "Government" => "#F3E8FF", "VIP" => "#FFF0F5", "Individual" => "#DCFCE7",
        "OTA" => "#FFFBEB", "WalkIn" => "#F1F5F9", _ => "#F8FAFC"
    };
    public string TypeIcon => CustomerType switch
    {
        "Corporate" => "🏢", "TravelAgent" => "✈", "Company" => "🏭",
        "Government" => "🏛", "VIP" => "👑", "Individual" => "👤",
        "OTA" => "🌐", "WalkIn" => "🚶", _ => "📋"
    };

    public string? TaxCode { get; set; }
    public string? BusinessLicense { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public string? Market { get; set; }
    public string? Source { get; set; }
    public string? Segment { get; set; }
    public string? SalesPerson { get; set; }
    public string? Priority { get; set; }

    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; } = 30;
    public string? PaymentTerms { get; set; }
    public string Currency { get; set; } = "VND";
    public bool VatRequired { get; set; }
    public string? VatType { get; set; }
    public bool DepositRequired { get; set; }
    public bool RequirePo { get; set; }
    public bool RequireContract { get; set; }
    public bool AutoApprove { get; set; }
    public bool NeedCreditApproval { get; set; }

    public string? ArAccount { get; set; }
    public string? RevenueAccount { get; set; }
    public string? TaxGroup { get; set; }
    public string? CommissionGroup { get; set; }
    public string? PriceLevel { get; set; }
    public string? DiscountLevel { get; set; }

    public decimal Outstanding { get; set; }
    public decimal CurrentDebt => Outstanding;
    public decimal Debt1To30 { get; set; }
    public decimal Debt31To60 { get; set; }
    public decimal Debt61To90 { get; set; }
    public decimal DebtOver90 { get; set; }
    public decimal AvailableCredit => Math.Max(0, CreditLimit - Outstanding);
    public bool IsOverCreditLimit => CreditLimit > 0 && Outstanding > CreditLimit;
    public bool IsOverdue => DebtOver90 > 0;

    public int OpenInvoiceCount { get; set; }
    public decimal PendingPayment { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal TotalRefund { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public DateTime? LastStayDate { get; set; }
    public DateTime? LastReservationDate { get; set; }
    public DateTime? LastInvoiceDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public string Status { get; set; } = "Active";
    public string StatusLabel => Status switch
    {
        "Active" => "Hoạt động", "Blocked" => "Đã khóa", "Inactive" => "Không hoạt động",
        "Pending" => "Chờ duyệt", "Suspended" => "Tạm ngưng", _ => Status
    };
    public string StatusColor => Status switch
    {
        "Active" => "#16A34A", "Blocked" => "#DC2626", "Inactive" => "#64748B",
        "Pending" => "#F59E0B", "Suspended" => "#D97706", _ => "#64748B"
    };
    public string StatusBg => Status switch
    {
        "Active" => "#DCFCE7", "Blocked" => "#FEE2E2", "Inactive" => "#F1F5F9",
        "Pending" => "#FFFBEB", "Suspended" => "#FFF7ED", _ => "#F8FAFC"
    };

    public bool HasWarning => Warnings.Count > 0;
    public int WarningCount => Warnings.Count;
    public List<string> Warnings { get; set; } = new();

    // Sub-collections
    public List<ArCustomerContactDto> Contacts { get; set; } = new();
    public List<ArCustomerContractDto> Contracts { get; set; } = new();
    public List<ArCustomerInvoiceDto> Invoices { get; set; } = new();
    public List<ArCustomerPaymentDto> Payments { get; set; } = new();
    public List<ArCustomerFolioDto> Folios { get; set; } = new();
    public List<ArCustomerRateDto> Rates { get; set; } = new();
    public List<ArCustomerHistoryDto> History { get; set; } = new();
    public List<ArCustomerDocumentDto> Documents { get; set; } = new();
    public List<ArCustomerNoteDto> Notes { get; set; } = new();
    public List<ArCustomerAuditEntry> AuditLog { get; set; } = new();
}

public sealed class ArCustomerContactDto
{
    public string Name { get; set; } = "";
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? Birthday { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsDecisionMaker { get; set; }
    public bool IsAccounting { get; set; }
    public bool IsBooking { get; set; }
}

public sealed class ArCustomerContractDto
{
    public string ContractNo { get; set; } = "";
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal RoomRate { get; set; }
    public decimal Discount { get; set; }
    public decimal Credit { get; set; }
    public decimal Commission { get; set; }
    public string? SpecialCondition { get; set; }
    public string? BlackoutDates { get; set; }
    public string Status { get; set; } = "Active";
    public string StatusLabel => Status switch { "Active" => "Hiệu lực", "Expired" => "Hết hạn", "Pending" => "Chờ ký", "Cancelled" => "Đã hủy", _ => Status };
    public string StatusColor => Status switch { "Active" => "#16A34A", "Expired" => "#DC2626", "Pending" => "#F59E0B", "Cancelled" => "#64748B", _ => "#64748B" };
}

public sealed class ArCustomerInvoiceDto
{
    public string InvoiceNo { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Vat { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = "Open";
    public string StatusLabel => Status switch { "Open" => "Mở", "Paid" => "Đã TT", "PartiallyPaid" => "Một phần", "Overdue" => "Quá hạn", "Cancelled" => "Đã hủy", _ => Status };
    public string StatusColor => Status switch { "Open" => "#F59E0B", "Paid" => "#16A34A", "PartiallyPaid" => "#2563EB", "Overdue" => "#DC2626", "Cancelled" => "#64748B", _ => "#64748B" };
}

public sealed class ArCustomerPaymentDto
{
    public string ReceiptNo { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Tiền mặt";
    public string? Bank { get; set; }
    public string? Reference { get; set; }
    public string? Collector { get; set; }
    public string Status { get; set; } = "Completed";
}

public sealed class ArCustomerFolioDto
{
    public string FolioNo { get; set; } = "";
    public string? ReservationNo { get; set; }
    public DateTime StayDate { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = "Open";
}

public sealed class ArCustomerRateDto
{
    public string RateCode { get; set; } = "";
    public string RoomType { get; set; } = "";
    public decimal Rate { get; set; }
    public string? Season { get; set; }
    public string? Promotion { get; set; }
    public string? MealPlan { get; set; }
    public string? Cancellation { get; set; }
    public string? Guarantee { get; set; }
}

public sealed class ArCustomerHistoryDto
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "";
    public string TypeLabel => Type switch
    {
        "Reservation" => "Đặt phòng", "Stay" => "Lưu trú", "Invoice" => "Hóa đơn",
        "Payment" => "Thanh toán", "Deposit" => "Đặt cọc", "Refund" => "Hoàn tiền",
        "CreditAdjustment" => "Điều chỉnh", "Contract" => "Hợp đồng",
        "PriceAgreement" => "Thỏa thuận giá", "Email" => "Email", "Call" => "Cuộc gọi",
        "Remark" => "Ghi chú", _ => Type
    };
    public string Detail { get; set; } = "";
    public string? Reference { get; set; }
    public decimal? Amount { get; set; }
}

public sealed class ArCustomerDocumentDto
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string TypeLabel => Type switch
    {
        "BusinessLicense" => "GPKD", "Contract" => "Hợp đồng", "TaxCertificate" => "MST",
        "ID" => "CMND/CCCD", "Logo" => "Logo", "PDF" => "PDF", "Word" => "Word", "Excel" => "Excel",
        _ => Type
    };
    public DateTime UploadDate { get; set; }
    public string? FileName { get; set; }
}

public sealed class ArCustomerNoteDto
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "Internal";
    public string TypeLabel => Type switch { "Reminder" => "Nhắc", "PhoneCall" => "Gọi", "Email" => "Email", "Internal" => "Nội bộ", "Accounting" => "Kế toán", _ => Type };
    public string Content { get; set; } = "";
    public string Author { get; set; } = "";
}

public sealed class ArCustomerAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? Device { get; set; }
}

public sealed class ArCustomerKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class ArCustomerPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public ArCustomerPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label; PageNumber = pageNumber; IsCurrent = isCurrent; IsEllipsis = isEllipsis;
    }
}

// ═══ VIEW MODEL ═══

public sealed partial class ArCustomerViewModel : ViewModelBase
{
    private readonly IReceivableService? _receivableService;
    private readonly ICustomerAppService? _customerAppService;
    private readonly IEmailDispatchService? _emailDispatchService;

    public ArCustomerViewModel(IReceivableService? receivableService = null, ICustomerAppService? customerAppService = null, IEmailDispatchService? emailDispatchService = null)
    {
        _receivableService = receivableService;
        _customerAppService = customerAppService;
        _emailDispatchService = emailDispatchService;
    }

    public ObservableCollection<ArCustomerDto> Items { get; } = new();
    public ObservableCollection<ArCustomerDto> FilteredItems { get; } = new();
    public ObservableCollection<ArCustomerKpi> DashboardKpis { get; } = new();
    public ObservableCollection<ArCustomerPaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private ArCustomerDto? _selectedItem;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private string _viewMode = "Grid";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterType;
    [ObservableProperty] private string? _filterMarket;
    [ObservableProperty] private string? _filterSource;
    [ObservableProperty] private string? _filterCountry;
    [ObservableProperty] private string? _filterSales;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private bool _filterHasDebt;
    [ObservableProperty] private bool _filterOverdue;
    [ObservableProperty] private bool _filterOverCredit;
    [ObservableProperty] private bool _filterBlocked;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;

    [ObservableProperty] private bool _isRightPanelOpen = true;

    // Form state
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formMode = string.Empty;
    [ObservableProperty] private string _formTitle = string.Empty;
    [ObservableProperty] private string _formErrorMessage = string.Empty;

    // Edit form fields
    [ObservableProperty] private string _editCustomerName = "";
    [ObservableProperty] private string _editShortName = "";
    [ObservableProperty] private string _editEnglishName = "";
    [ObservableProperty] private string _editCustomerType = "Corporate";
    [ObservableProperty] private string _editTaxCode = "";
    [ObservableProperty] private string _editAddress = "";
    [ObservableProperty] private string _editCity = "";
    [ObservableProperty] private string _editPhone = "";
    [ObservableProperty] private string _editEmail = "";
    [ObservableProperty] private string _editWebsite = "";
    [ObservableProperty] private string _editMarket = "Nội địa";
    [ObservableProperty] private string _editSource = "Trực tiếp";
    [ObservableProperty] private string _editSalesPerson = "";
    [ObservableProperty] private decimal _editCreditLimit;
    [ObservableProperty] private int _editCreditDays = 30;
    [ObservableProperty] private string _editCurrency = "VND";
    [ObservableProperty] private string _editPaymentTerms = "Net 30";

    // Quick action form
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private string _paymentMethod = "Tiền mặt";
    [ObservableProperty] private string _paymentReference = "";

    // Email form
    [ObservableProperty] private string _emailType = "Statement";
    [ObservableProperty] private string _emailTo = "";
    [ObservableProperty] private string _emailBody = "";

    public bool IsEditForm => FormMode == "Edit";
    public bool IsPaymentForm => FormMode == "Payment";
    public bool IsEmailForm => FormMode == "Email";
    public bool IsFormClosed => !IsFormOpen;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0 khách hàng" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} khách hàng";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedItem is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool IsGridView => ViewMode == "Grid";
    public bool IsCardView => ViewMode == "Card";

    public int TotalItems => Items.Count;
    public int ActiveCount => Items.Count(i => i.Status == "Active");
    public int BlockedCount => Items.Count(i => i.Status == "Blocked");
    public int OverCreditCount => Items.Count(i => i.IsOverCreditLimit);
    public int OverdueCount => Items.Count(i => i.IsOverdue);
    public int HasDebtCount => Items.Count(i => i.Outstanding > 0);
    public int CorporateCount => Items.Count(i => i.CustomerType == "Corporate");
    public int TravelAgentCount => Items.Count(i => i.CustomerType == "TravelAgent");
    public int GovernmentCount => Items.Count(i => i.CustomerType == "Government");
    public int VipCount => Items.Count(i => i.CustomerType == "VIP");

    public decimal TotalOutstanding => Items.Sum(i => i.Outstanding);
    public decimal TotalCreditLimit => Items.Sum(i => i.CreditLimit);
    public decimal TotalOpenInvoices => Items.Sum(i => i.Invoices.Count(inv => inv.Status is "Open" or "PartiallyPaid"));

    public static string[] TypeFilters { get; } = ["Tất cả", "Corporate", "TravelAgent", "Company", "Government", "VIP", "Individual", "OTA"];
    public static string[] MarketFilters { get; } = ["Tất cả", "Nội địa", "Quốc tế", "Đông Nam Á", "Châu Âu", "Châu Mỹ", "Đông Bắc Á"];
    public static string[] SourceFilters { get; } = ["Tất cả", "Trực tiếp", "Booking.com", "Agoda", "Expedia", "Traveloka", "Website", "Phone", "Email", "Sales"];
    public static string[] CountryFilters { get; } = ["Tất cả", "Việt Nam", "Hàn Quốc", "Nhật Bản", "Trung Quốc", "Mỹ", "Anh", "Pháp", "Đức", "Úc", "Singapore", "Thái Lan"];
    public static string[] SalesFilters { get; } = ["Tất cả", "Nguyễn Văn Kinh Doanh", "Trần Thị Sales", "Lê Văn MKT", "Phạm Thị Business", "Hoàng Văn Corp"];
    public static string[] StatusFilters { get; } = ["Tất cả", "Active", "Blocked", "Inactive", "Pending", "Suspended"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];
    public static string[] PaymentMethods { get; } = ["Tiền mặt", "Chuyển khoản", "Thẻ tín dụng", "Thẻ ghi nợ", "Ví điện tử", "Séc"];
    public static string[] EmailTypes { get; } = ["Statement", "Invoice", "Reminder", "BalanceConfirmation"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Items.Clear();

            if (_receivableService is not null)
            {
                var summaries = await _receivableService.GetDebtSummariesAsync(new ReceivableFilterDto(), ct);
                foreach (var summary in summaries)
                    Items.Add(MapPersistedCustomer(summary));

                BuildDashboard(); ApplyFilters();
                InfoMessage = $"Đã tải {Items.Count:N0} khách hàng công nợ từ dữ liệu thật.";
                return;
            }

            ErrorMessage = "Chưa có Receivable query service; không hiển thị dữ liệu mẫu.";
            BuildDashboard();
            ApplyFilters();
            return;

#if false // Legacy designer fixture retained for historical reference only.
#pragma warning disable CS0162
            await Task.Delay(100, ct);
            var rng = new Random(77);

            var types = new[] { "Corporate", "Corporate", "Corporate", "TravelAgent", "TravelAgent", "Company", "Government", "VIP", "Individual", "OTA" };
            var statuses = new[] { "Active", "Active", "Active", "Active", "Active", "Blocked", "Inactive", "Pending" };
            var salesPeople = new[] { "Nguyễn Văn Kinh Doanh", "Trần Thị Sales", "Lê Văn MKT", "Phạm Thị Business", "Hoàng Văn Corp" };
            var markets = new[] { "Nội địa", "Quốc tế", "Đông Nam Á", "Châu Âu", "Châu Mỹ", "Đông Bắc Á" };
            var sources = new[] { "Trực tiếp", "Booking.com", "Agoda", "Expedia", "Traveloka", "Website", "Phone", "Email", "Sales" };
            var cities = new[] { "Hà Nội", "TP.HCM", "Đà Nẵng", "Nha Trang", "Phú Quốc", "Hải Phòng", "Cần Thơ", "Huế", "Đà Lạt", "Vũng Tàu" };
            var countries = new[] { "Việt Nam", "Hàn Quốc", "Nhật Bản", "Trung Quốc", "Mỹ", "Anh", "Pháp", "Đức", "Úc", "Singapore", "Thái Lan" };
            var positions = new[] { "Giám đốc", "Trưởng phòng", "Kế toán trưởng", "Trợ lý", "Thư ký", "Sales Manager", "Reservation Manager" };
            var contactNames = new[] { "Nguyễn Văn A", "Trần Thị B", "Lê Văn C", "Phạm Thị D", "Hoàng Văn E", "Đỗ Thị F", "Vũ Văn G", "Bùi Thị H" };
            var paymentTerms = new[] { "Net 15", "Net 30", "Net 45", "Net 60", "Net 90", "COD", "Prepayment 50%", "Monthly" };
            var currencies = new[] { "VND", "VND", "VND", "USD", "EUR" };
            var priorityLevels = new[] { "Cao", "Trung bình", "Thấp", "VIP" };
            var segments = new[] { "Leisure", "Corporate", "Government", "Group", "MICE" };
            var priceLevels = new[] { "BAR", "Corporate", "Government", "Wholesale", "Promo" };
            var rateCodes = new[] { "CORP2025", "GOV2025", "TA2025", "OTA2025", "VIP2025", "PROMO25" };
            var roomTypes = new[] { "Superior", "Deluxe", "Suite", "Premium", "Standard", "Executive" };
            var seasons = new[] { "Low", "Shoulder", "High", "Peak", "Holiday" };
            var mealPlans = new[] { "RO", "BB", "HB", "FB", "AI" };

            // Company name parts
            var companyPrefixes = new[] { "Công ty TNHH", "Công ty CP", "Tập đoàn", "Tổng công ty", "CT TNHH", "CTCP" };
            var companySuffixes = new[] { "Thương mại & Dịch vụ", "Du lịch", "Xây dựng", "Vận tải", "Công nghệ", "Sản xuất", "Xuất nhập khẩu", "Đầu tư", "Bất động sản", "Tài chính", "Dầu khí", "Hàng không", "Dược phẩm" };
            var companyNames = new[] { "Hòa Bình", "Sao Mai", "Phương Nam", "Đại Việt", "Thăng Long", "Sông Hồng", "Biển Đông", "Trường Sơn", "Tây Nguyên", "Mê Kông", "Hoàng Gia", "Kim Cương", "Ngọc Trai", "Bạch Kim", "Hồng Hà" };
            var otaList = new[] { "Booking.com", "Agoda", "Expedia", "Traveloka", "Trip.com", "Hotels.com" };
            var taList = new[] { "Saigon Tourist", "Hanoi Travel", "Vietravel", "BenThanh Tourist", "Fiditour", "Buffalo Tours", "APT Travel", "Exotissimo" };
            var govList = new[] { "UBND TP Hà Nội", "UBND TP.HCM", "Bộ Ngoại giao", "Bộ Quốc phòng", "Tổng cục Du lịch", "Sở VHTTDL Hà Nội" };
            var corpList = new[] { "Vingroup", "FPT", "Vietcombank", "VietinBank", "Petrolimex", "VNPT", "Viettel", "Masan", "TH True Milk", "Vinamilk" };

            for (int i = 0; i < 220; i++)
            {
                var type = i < 80 ? types[rng.Next(4)] : types[rng.Next(types.Length)];
                var status = statuses[rng.Next(statuses.Length)];
                var city = cities[rng.Next(cities.Length)];
                var country = type == "Government" ? "Việt Nam" : countries[rng.Next(countries.Length)];

                string name, code;
                switch (type)
                {
                    case "OTA":
                        name = otaList[rng.Next(otaList.Length)];
                        code = $"OTA{rng.Next(100, 999)}";
                        break;
                    case "TravelAgent":
                        name = taList[rng.Next(taList.Length)];
                        code = $"TA{rng.Next(100, 999)}";
                        break;
                    case "Government":
                        name = govList[rng.Next(govList.Length)];
                        code = $"GOV{rng.Next(100, 999)}";
                        break;
                    case "Corporate":
                        name = corpList[rng.Next(corpList.Length)];
                        code = $"CORP{rng.Next(100, 999)}";
                        break;
                    default:
                        var prefix = companyPrefixes[rng.Next(companyPrefixes.Length)];
                        var suffix = companySuffixes[rng.Next(companySuffixes.Length)];
                        var cname = companyNames[rng.Next(companyNames.Length)];
                        name = $"{prefix} {cname} {suffix}";
                        code = $"KH{rng.Next(10000, 99999)}";
                        break;
                }

                var creditLimit = type switch
                {
                    "Government" => rng.Next(200000000, 2000000000),
                    "Corporate" => rng.Next(100000000, 1000000000),
                    "TravelAgent" => rng.Next(50000000, 500000000),
                    "OTA" => rng.Next(50000000, 500000000),
                    "VIP" => rng.Next(100000000, 1000000000),
                    _ => rng.Next(10000000, 200000000)
                };
                var outstanding = rng.Next(4) switch
                {
                    0 => 0,
                    1 => creditLimit * (decimal)(rng.Next(10, 50) / 100.0),
                    _ => creditLimit * (decimal)(rng.Next(50, 95) / 100.0)
                };

                if (status == "Blocked") outstanding = creditLimit * (decimal)(rng.Next(90, 130) / 100.0);
                if (status == "Inactive") outstanding = 0;

                var createdDate = DateTime.Now.AddDays(-rng.Next(30, 1095));
                var item = new ArCustomerDto
                {
                    CustomerCode = code, CustomerName = name,
                    ShortName = name.Split(' ').LastOrDefault(),
                    EnglishName = type is "Corporate" or "TravelAgent" ? $"{name} Co., Ltd." : null,
                    CustomerType = type,
                    TaxCode = rng.Next(4) > 0 ? $"MST{1000000000 + rng.Next(0, 999999999):D10}" : null,
                    BusinessLicense = type is "Corporate" or "Company" ? $"GPKD{rng.Next(100000, 999999)}" : null,
                    Address = $"{rng.Next(1, 999)} Đường {rng.Next(3) switch { 0 => "Nguyễn Huệ", 1 => "Lê Lợi", _ => "Trần Hưng Đạo" }}, {city}",
                    City = city, Province = city, Country = country,
                    Phone = $"0{rng.Next(100000000, 999999999)}",
                    Email = $"{name.Split(' ').LastOrDefault()?.ToLower()}@{rng.Next(3) switch { 0 => "gmail.com", 1 => "yahoo.com", _ => "company.com.vn" }}",
                    Website = rng.Next(3) == 1 ? $"www.{name.Split(' ').LastOrDefault()?.ToLower()}.com.vn" : null,
                    Market = markets[rng.Next(markets.Length)], Source = sources[rng.Next(sources.Length)],
                    Segment = segments[rng.Next(segments.Length)], SalesPerson = salesPeople[rng.Next(salesPeople.Length)],
                    Priority = priorityLevels[rng.Next(priorityLevels.Length)],
                    CreditLimit = creditLimit, CreditDays = rng.Next(4) switch { 0 => 15, 1 => 30, 2 => 45, _ => 60 },
                    PaymentTerms = paymentTerms[rng.Next(paymentTerms.Length)],
                    Currency = currencies[rng.Next(currencies.Length)],
                    VatRequired = rng.Next(3) > 0, VatType = rng.Next(3) > 0 ? "10%" : "8%",
                    DepositRequired = rng.Next(3) == 1, RequirePo = rng.Next(5) == 1,
                    RequireContract = type is "Corporate" or "Government" or "TravelAgent",
                    AutoApprove = rng.Next(3) > 0, NeedCreditApproval = creditLimit > 500000000,
                    ArAccount = $"131.{code}", RevenueAccount = $"511.{code}",
                    TaxGroup = rng.Next(3) switch { 0 => "VAT10", 1 => "VAT8", _ => "VAT0" },
                    CommissionGroup = rng.Next(3) switch { 0 => "Standard", 1 => "Premium", _ => "Travel" },
                    PriceLevel = priceLevels[rng.Next(priceLevels.Length)],
                    DiscountLevel = rng.Next(5) switch { 0 => "0%", 1 => "5%", 2 => "10%", 3 => "15%", _ => "20%" },
                    Outstanding = outstanding,
                    Debt1To30 = outstanding * (decimal)(rng.Next(20, 60) / 100.0),
                    Debt31To60 = outstanding * (decimal)(rng.Next(10, 30) / 100.0),
                    Debt61To90 = outstanding * (decimal)(rng.Next(5, 15) / 100.0),
                    DebtOver90 = outstanding * (decimal)(rng.Next(0, 10) / 100.0),
                    OpenInvoiceCount = rng.Next(0, 15),
                    PendingPayment = outstanding * (decimal)(rng.Next(0, 20) / 100.0),
                    TotalDeposit = rng.Next(0, 50000000),
                    LastPaymentDate = rng.Next(3) > 0 ? DateTime.Now.AddDays(-rng.Next(1, 90)) : null,
                    LastStayDate = rng.Next(2) == 1 ? DateTime.Now.AddDays(-rng.Next(1, 180)) : null,
                    LastReservationDate = rng.Next(2) == 1 ? DateTime.Now.AddDays(-rng.Next(1, 60)) : null,
                    LastInvoiceDate = rng.Next(3) > 0 ? DateTime.Now.AddDays(-rng.Next(1, 45)) : null,
                    CreatedDate = createdDate, UpdatedDate = DateTime.Now.AddDays(-rng.Next(0, 30)),
                    Status = status,
                };

                // Generate warnings
                if (item.IsOverCreditLimit) item.Warnings.Add("Credit Limit Exceeded");
                if (item.IsOverdue) item.Warnings.Add("Có công nợ quá hạn > 90 ngày");
                if (item.OpenInvoiceCount > 5) item.Warnings.Add($"{item.OpenInvoiceCount} hóa đơn mở");
                if (string.IsNullOrEmpty(item.TaxCode) && item.VatRequired) item.Warnings.Add("Thiếu MST");
                if (string.IsNullOrEmpty(item.Email)) item.Warnings.Add("Thiếu Email");
                if (string.IsNullOrEmpty(item.Phone)) item.Warnings.Add("Thiếu Phone");

                // Contacts
                var contactCount = rng.Next(1, 4);
                for (int c = 0; c < contactCount; c++)
                    item.Contacts.Add(new ArCustomerContactDto
                    {
                        Name = contactNames[rng.Next(contactNames.Length)],
                        Position = positions[rng.Next(positions.Length)],
                        Phone = $"0{rng.Next(100000000, 999999999)}",
                        Email = $"contact{c}@{(name.Split(' ').LastOrDefault()?.ToLower() ?? "company")}.com",
                        Birthday = rng.Next(2) == 1 ? DateTime.Now.AddYears(-rng.Next(25, 55)) : null,
                        IsPrimary = c == 0, IsDecisionMaker = c == 0 && rng.Next(2) == 1,
                        IsAccounting = c == 1, IsBooking = c == 2
                    });

                // Contracts
                if (type is "Corporate" or "TravelAgent" or "Government" || rng.Next(3) == 1)
                {
                    var contractCnt = rng.Next(1, 3);
                    for (int cIdx = 0; cIdx < contractCnt; cIdx++)
                        item.Contracts.Add(new ArCustomerContractDto
                        {
                            ContractNo = $"HD{code}-{cIdx + 1:D2}",
                            EffectiveDate = createdDate.AddDays(cIdx * 365),
                            ExpiryDate = createdDate.AddDays((cIdx + 1) * 365).AddDays(rng.Next(-30, 60)),
                            RoomRate = rng.Next(500000, 5000000),
                            Discount = rng.Next(5, 25),
                            Credit = rng.Next(10000000, 200000000),
                            Commission = rng.Next(5, 15),
                            SpecialCondition = rng.Next(2) == 1 ? "Giá áp dụng cho tất cả các mùa trừ Peak" : null,
                            Status = rng.Next(4) switch { 0 => "Active", 1 => "Active", 2 => "Expired", _ => "Pending" }
                        });
                }

                // Rates
                var rateCount = rng.Next(2, 5);
                for (int rt = 0; rt < rateCount; rt++)
                    item.Rates.Add(new ArCustomerRateDto
                    {
                        RateCode = $"{rateCodes[rng.Next(rateCodes.Length)]}_{rt + 1}",
                        RoomType = roomTypes[rng.Next(roomTypes.Length)],
                        Rate = rng.Next(800000, 8000000),
                        Season = seasons[rng.Next(seasons.Length)],
                        MealPlan = mealPlans[rng.Next(mealPlans.Length)],
                        Cancellation = rng.Next(3) switch { 0 => "Free 24h", 1 => "Free 48h", _ => "Non-refundable" },
                        Guarantee = rng.Next(3) switch { 0 => "Credit Card", 1 => "Deposit", _ => "PO" }
                    });

                // Invoices
                var invCount = rng.Next(0, 12);
                for (int inv = 0; inv < invCount; inv++)
                {
                    var invAmt = (decimal)rng.Next(1000000, 50000000);
                    var invPaid = rng.Next(3) > 0 ? invAmt * (decimal)(rng.Next(50, 100) / 100.0) : 0;
                    item.Invoices.Add(new ArCustomerInvoiceDto
                    {
                        InvoiceNo = $"INV{code}-{inv + 1:D3}",
                        IssueDate = DateTime.Now.AddDays(-rng.Next(1, 365)),
                        DueDate = DateTime.Now.AddDays(-rng.Next(0, 180)),
                        Amount = invAmt, Vat = invAmt * 0.08m, Paid = invPaid,
                        Balance = invAmt * 1.08m - invPaid,
                        Status = invPaid >= (invAmt * 1.08m) ? "Paid" : invPaid > 0 ? "PartiallyPaid" : "Open"
                    });
                }

                // Payments
                var pmtCount = rng.Next(0, 8);
                for (int p = 0; p < pmtCount; p++)
                    item.Payments.Add(new ArCustomerPaymentDto
                    {
                        ReceiptNo = $"PT{code}-{p + 1:D3}",
                        PaymentDate = DateTime.Now.AddDays(-rng.Next(1, 180)),
                        Amount = rng.Next(1000000, 20000000),
                        Method = PaymentMethods[rng.Next(PaymentMethods.Length)],
                        Bank = rng.Next(3) == 1 ? $"VCB{rng.Next(1000, 9999)}" : null,
                        Reference = $"REF{rng.Next(10000, 99999)}",
                        Collector = salesPeople[rng.Next(salesPeople.Length)],
                        Status = "Completed"
                    });

                // Folios
                var folioCount = rng.Next(1, 8);
                for (int f = 0; f < folioCount; f++)
                    item.Folios.Add(new ArCustomerFolioDto
                    {
                        FolioNo = $"FOL{rng.Next(1000, 9999)}",
                        ReservationNo = $"RES{rng.Next(10000, 99999)}",
                        StayDate = DateTime.Now.AddDays(-rng.Next(1, 180)),
                        InvoiceNo = rng.Next(2) == 1 ? $"INV{code}-{f + 1:D3}" : null,
                        Balance = rng.Next(0, 20000000),
                        Status = rng.Next(3) switch { 0 => "Open", 1 => "Closed", _ => "Transferred" }
                    });

                // History
                for (int h = 0; h < rng.Next(5, 15); h++)
                    item.History.Add(new ArCustomerHistoryDto
                    {
                        Timestamp = createdDate.AddDays(rng.Next(1, 365)),
                        Type = rng.Next(8) switch { 0 => "Reservation", 1 => "Stay", 2 => "Invoice", 3 => "Payment", 4 => "Contract", 5 => "Email", 6 => "Call", _ => "Remark" },
                        Detail = $"Giao dịch #{h + 1}",
                        Reference = rng.Next(2) == 1 ? $"REF{rng.Next(10000, 99999)}" : null,
                        Amount = rng.Next(2) == 1 ? rng.Next(1000000, 10000000) : null
                    });

                // Documents
                if (rng.Next(3) == 1)
                    item.Documents.Add(new ArCustomerDocumentDto { Name = $"GPKD_{code}", Type = "BusinessLicense", UploadDate = createdDate, FileName = $"gpdk_{code}.pdf" });
                if (item.Contracts.Count > 0)
                    item.Documents.Add(new ArCustomerDocumentDto { Name = $"HĐ_{code}", Type = "Contract", UploadDate = createdDate, FileName = $"contract_{code}.pdf" });
                if (!string.IsNullOrEmpty(item.TaxCode))
                    item.Documents.Add(new ArCustomerDocumentDto { Name = $"MST_{code}", Type = "TaxCertificate", UploadDate = createdDate, FileName = $"mst_{code}.pdf" });

                // Notes
                if (rng.Next(4) == 1)
                    item.Notes.Add(new ArCustomerNoteDto { Timestamp = createdDate.AddDays(rng.Next(1, 60)), Type = "Internal", Content = $"Khách hàng {item.Priority} — {item.Market}", Author = item.SalesPerson ?? "System" });
                if (item.IsOverdue)
                    item.Notes.Add(new ArCustomerNoteDto { Timestamp = DateTime.Now.AddDays(-rng.Next(0, 10)), Type = "Reminder", Content = "Cần liên hệ thu hồi công nợ quá hạn", Author = "Hệ thống" });

                // Audit
                item.AuditLog.Add(new ArCustomerAuditEntry { Timestamp = createdDate, Action = "Created", Actor = item.SalesPerson ?? "System", Detail = $"Tạo khách hàng {item.CustomerCode}", Device = "Front Office" });

                Items.Add(item);
            }

            BuildDashboard(); ApplyFilters();
#pragma warning restore CS0162
#endif
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static ArCustomerDto MapPersistedCustomer(ReceivableSummaryDto summary)
    {
        var status = summary.Status switch
        {
            DebtStatus.Cancelled => "Inactive",
            _ => "Active"
        };

        return new ArCustomerDto
        {
            CustomerId = summary.CustomerId,
            CustomerCode = summary.CustomerCode,
            CustomerName = summary.CustomerName,
            ShortName = summary.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault(),
            CustomerType = "Company",
            Market = "Chưa phân loại",
            Source = "Dữ liệu công nợ",
            Currency = "VND",
            Outstanding = Math.Max(0, summary.ClosingBalance),
            DebtOver90 = Math.Max(0, summary.OverdueAmount),
            OpenInvoiceCount = summary.ClosingBalance > 0 ? 1 : 0,
            LastInvoiceDate = summary.LastTransactionDate,
            LastPaymentDate = summary.TotalCredit > 0 ? summary.LastTransactionDate : null,
            CreatedDate = summary.LastTransactionDate ?? DateTime.MinValue,
            UpdatedDate = summary.LastTransactionDate,
            Status = status
        };
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new ArCustomerKpi { Label = "Tổng KH", Value = TotalItems.ToString(), Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Đang hoạt động", Value = ActiveCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4", FilterKey = "Status", FilterValue = "Active" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Bị khóa", Value = BlockedCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Status", FilterValue = "Blocked" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Có công nợ", Value = HasDebtCount.ToString(), Color = "#F59E0B", BgColor = "#FFFBEB", FilterKey = "HasDebt", FilterValue = "true" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Quá hạn", Value = OverdueCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Overdue", FilterValue = "true" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Vượt Credit", Value = OverCreditCount.ToString(), Color = "#E32F70", BgColor = "#FFF0F5", FilterKey = "OverCredit", FilterValue = "true" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Doanh nghiệp", Value = CorporateCount.ToString(), Color = "#2563EB", BgColor = "#DBEAFE", FilterKey = "Type", FilterValue = "Corporate" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Travel Agent", Value = TravelAgentCount.ToString(), Color = "#0891B2", BgColor = "#ECFEFF", FilterKey = "Type", FilterValue = "TravelAgent" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Government", Value = GovernmentCount.ToString(), Color = "#7C3AED", BgColor = "#F3E8FF", FilterKey = "Type", FilterValue = "Government" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "VIP", Value = VipCount.ToString(), Color = "#E32F70", BgColor = "#FFF0F5", FilterKey = "Type", FilterValue = "VIP" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Tổng dư nợ", Value = $"{TotalOutstanding:N0} ₫", Color = "#DC2626", BgColor = "#FEF2F2" });
        DashboardKpis.Add(new ArCustomerKpi { Label = "Tổng Credit", Value = $"{TotalCreditLimit:N0} ₫", Color = "#2563EB", BgColor = "#EFF6FF" });
    }

    [RelayCommand] private void ApplyFilters()
    {
        var q = Items.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s))
            q = q.Where(r => r.CustomerCode.ToLower().Contains(s) || r.CustomerName.ToLower().Contains(s) || (r.TaxCode?.Contains(s) ?? false) || (r.Phone?.Contains(s) ?? false) || (r.Email?.ToLower().Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterType) && FilterType != "Tất cả") q = q.Where(r => r.CustomerType == FilterType);
        if (!string.IsNullOrEmpty(FilterMarket) && FilterMarket != "Tất cả") q = q.Where(r => r.Market == FilterMarket);
        if (!string.IsNullOrEmpty(FilterSource) && FilterSource != "Tất cả") q = q.Where(r => r.Source == FilterSource);
        if (!string.IsNullOrEmpty(FilterCountry) && FilterCountry != "Tất cả") q = q.Where(r => r.Country == FilterCountry);
        if (!string.IsNullOrEmpty(FilterSales) && FilterSales != "Tất cả") q = q.Where(r => r.SalesPerson == FilterSales);
        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.Status == FilterStatus);
        if (FilterHasDebt) q = q.Where(r => r.Outstanding > 0);
        if (FilterOverdue) q = q.Where(r => r.IsOverdue);
        if (FilterOverCredit) q = q.Where(r => r.IsOverCreditLimit);
        if (FilterBlocked) q = q.Where(r => r.Status == "Blocked");

        var list = q.OrderByDescending(r => r.Outstanding).ThenBy(r => r.CustomerCode).ToList();
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredItems.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredItems.Add(r);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void ClearFilters()
    {
        SearchText = string.Empty; FilterType = null; FilterMarket = null; FilterSource = null;
        FilterCountry = null; FilterSales = null; FilterStatus = null;
        FilterHasDebt = false; FilterOverdue = false; FilterOverCredit = false; FilterBlocked = false;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterMarketChanged(string? value) => ApplyFilters();
    partial void OnFilterSourceChanged(string? value) => ApplyFilters();
    partial void OnFilterCountryChanged(string? value) => ApplyFilters();
    partial void OnFilterSalesChanged(string? value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterHasDebtChanged(bool value) => ApplyFilters();
    partial void OnFilterOverdueChanged(bool value) => ApplyFilters();
    partial void OnFilterOverCreditChanged(bool value) => ApplyFilters();
    partial void OnFilterBlockedChanged(bool value) => ApplyFilters();

    [RelayCommand] private void FirstPage() { CurrentPage = 1; ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (HasPrevPage) { CurrentPage--; ApplyFilters(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand] private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }
    [RelayCommand] private void SetPage(int? page) { if (page is null) return; CurrentPage = Math.Clamp(page.Value, 1, TotalPages); ApplyFilters(); }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFilters(); }

    private void RebuildPageButtons()
    {
        PageButtons.Clear();
        var total = TotalPages; var current = Math.Clamp(CurrentPage, 1, total);
        IEnumerable<int?> pages = total <= 5 ? Enumerable.Range(1, total).Cast<int?>()
            : current <= 3 ? new int?[] { 1, 2, 3, 4, 5, null, total }
            : current >= total - 2 ? new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total }
            : new int?[] { 1, null, current - 1, current, current + 1, null, total };
        foreach (var p in pages)
            PageButtons.Add(p is null ? new ArCustomerPaginationButton("…", null, false, true)
                : new ArCustomerPaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == current));
    }

    [RelayCommand] private void SelectItem(ArCustomerDto? item) => SelectedItem = item;
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedItem = null; }
    [RelayCommand] private void ToggleRightPanel() => IsRightPanelOpen = !IsRightPanelOpen;

    partial void OnSelectedItemChanged(ArCustomerDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        if (value is not null) { IsDetailOpen = true; SelectedDetailTab = 0; }
    }
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsOverviewTab)); OnPropertyChanged(nameof(IsContactsTab));
        OnPropertyChanged(nameof(IsPaymentTermsTab)); OnPropertyChanged(nameof(IsAccountingTab));
        OnPropertyChanged(nameof(IsContractsTab)); OnPropertyChanged(nameof(IsRatesTab));
        OnPropertyChanged(nameof(IsInvoicesTab)); OnPropertyChanged(nameof(IsPaymentsTab));
        OnPropertyChanged(nameof(IsHistoryTab)); OnPropertyChanged(nameof(IsFoliosTab));
        OnPropertyChanged(nameof(IsDocumentsTab)); OnPropertyChanged(nameof(IsNotesTab));
    }
    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsContactsTab => SelectedDetailTab == 1;
    public bool IsPaymentTermsTab => SelectedDetailTab == 2;
    public bool IsAccountingTab => SelectedDetailTab == 3;
    public bool IsContractsTab => SelectedDetailTab == 4;
    public bool IsRatesTab => SelectedDetailTab == 5;
    public bool IsInvoicesTab => SelectedDetailTab == 6;
    public bool IsPaymentsTab => SelectedDetailTab == 7;
    public bool IsHistoryTab => SelectedDetailTab == 8;
    public bool IsFoliosTab => SelectedDetailTab == 9;
    public bool IsDocumentsTab => SelectedDetailTab == 10;
    public bool IsNotesTab => SelectedDetailTab == 11;

    partial void OnFormModeChanged(string value) { OnPropertyChanged(nameof(IsEditForm)); OnPropertyChanged(nameof(IsPaymentForm)); OnPropertyChanged(nameof(IsEmailForm)); }
    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(IsFormClosed));
    partial void OnViewModeChanged(string value) { OnPropertyChanged(nameof(IsGridView)); OnPropertyChanged(nameof(IsCardView)); }

    [RelayCommand] private void SetGridView() { ViewMode = "Grid"; }
    [RelayCommand] private void SetCardView() { ViewMode = "Card"; }
    [RelayCommand] private void CloseForm() { IsFormOpen = false; FormErrorMessage = string.Empty; }

    [RelayCommand] private void OpenEditForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Edit"; FormTitle = $"Chỉnh sửa — {SelectedItem.CustomerName}"; FormErrorMessage = string.Empty;
        EditCustomerName = SelectedItem.CustomerName; EditShortName = SelectedItem.ShortName ?? "";
        EditEnglishName = SelectedItem.EnglishName ?? ""; EditCustomerType = SelectedItem.CustomerType;
        EditTaxCode = SelectedItem.TaxCode ?? ""; EditAddress = SelectedItem.Address ?? "";
        EditCity = SelectedItem.City ?? ""; EditPhone = SelectedItem.Phone ?? "";
        EditEmail = SelectedItem.Email ?? ""; EditWebsite = SelectedItem.Website ?? "";
        EditMarket = SelectedItem.Market ?? "Nội địa"; EditSource = SelectedItem.Source ?? "Trực tiếp";
        EditSalesPerson = SelectedItem.SalesPerson ?? "";
        EditCreditLimit = SelectedItem.CreditLimit; EditCreditDays = SelectedItem.CreditDays;
        EditCurrency = SelectedItem.Currency; EditPaymentTerms = SelectedItem.PaymentTerms ?? "Net 30";
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitEditForm()
    {
        if (SelectedItem is null) return;
        if (string.IsNullOrWhiteSpace(EditCustomerName)) { FormErrorMessage = "Vui lòng nhập tên khách hàng."; return; }
        if (_customerAppService is null || !int.TryParse(SelectedItem.CustomerId, out var customerId))
        {
            FormErrorMessage = "Khách hàng chưa có mã dữ liệu thật hoặc Customer Master service chưa sẵn sàng.";
            return;
        }

        var customer = await _customerAppService.GetAsync(customerId);
        if (customer is null)
        {
            FormErrorMessage = "Không tìm thấy khách hàng trong CSDL.";
            return;
        }

        customer.CustomerName = EditCustomerName.Trim();
        customer.Phone = EditPhone?.Trim() ?? string.Empty;
        customer.Address = EditAddress?.Trim() ?? string.Empty;
        await _customerAppService.SaveAsync(customer);
        IsFormOpen = false;
        await LoadAsync(CancellationToken.None);
        InfoMessage = $"Đã cập nhật thông tin khách hàng {customer.CustomerName} trong CSDL.";
    }

    [RelayCommand] private async Task ToggleLock()
    {
        if (SelectedItem is null) return;
        if (_customerAppService is null || !int.TryParse(SelectedItem.CustomerId, out var customerId))
        {
            InfoMessage = "Khách hàng chưa có mã dữ liệu thật hoặc Customer Master service chưa sẵn sàng.";
            return;
        }

        var customer = await _customerAppService.GetAsync(customerId);
        if (customer is null)
        {
            InfoMessage = "Không tìm thấy khách hàng trong CSDL.";
            return;
        }

        customer.IsActive = !customer.IsActive;
        await _customerAppService.SaveAsync(customer);
        var action = customer.IsActive ? "mở khóa" : "khóa";
        await LoadAsync(CancellationToken.None);
        InfoMessage = $"Đã {action} khách hàng {customer.CustomerName} trong CSDL.";
    }

    [RelayCommand] private void OpenPaymentForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Payment"; FormTitle = $"Thu tiền — {SelectedItem.CustomerName}"; FormErrorMessage = string.Empty;
        PaymentAmount = SelectedItem.Outstanding > 0 ? Math.Min(SelectedItem.Outstanding, 10000000) : 0;
        PaymentMethod = "Tiền mặt"; PaymentReference = "";
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitPaymentForm()
    {
        if (SelectedItem is null) return;
        if (PaymentAmount <= 0 || PaymentAmount > SelectedItem.Outstanding) { FormErrorMessage = "Số tiền không hợp lệ."; return; }
        if (_receivableService is null || string.IsNullOrWhiteSpace(SelectedItem.CustomerId))
        {
            FormErrorMessage = "Chưa thể thu tiền: khách hàng chưa có mã dữ liệu thật hoặc dịch vụ công nợ chưa sẵn sàng.";
            return;
        }

        var customerName = SelectedItem.CustomerName;
        var amount = PaymentAmount;
        var receiptNo = await _receivableService.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
        {
            CustomerId = SelectedItem.CustomerId,
            ReceiptDate = DateTime.Now,
            Amount = amount,
            PaymentMethod = PaymentMethod,
            Note = string.IsNullOrWhiteSpace(PaymentReference) ? $"Thu công nợ {SelectedItem.CustomerCode}" : PaymentReference
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

    [RelayCommand] private void OpenEmailForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Email"; FormTitle = $"Gửi Email — {SelectedItem.CustomerName}"; FormErrorMessage = string.Empty;
        EmailType = "Statement"; EmailTo = SelectedItem.Email ?? ""; EmailBody = $"Kính gửi {SelectedItem.CustomerName},\n\nVui lòng xem sao kê công nợ đính kèm.\nSố dư: {SelectedItem.Outstanding:N0} {SelectedItem.Currency}\n\nTrân trọng,\nASMILE BO Accounting";
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitEmailForm()
    {
        if (SelectedItem is null) return;
        if (string.IsNullOrWhiteSpace(EmailTo)) { FormErrorMessage = "Vui lòng nhập email."; return; }
        if (_emailDispatchService is null) { FormErrorMessage = "Chưa đăng ký email outbox."; return; }
        try
        {
            var queued = await _emailDispatchService.QueueAsync(new EmailDispatchRequestDto
            {
                Recipient = EmailTo.Trim(),
                Subject = $"Sao kê công nợ - {SelectedItem.CustomerName}",
                Body = EmailBody,
                SourceType = "ArCustomer",
                SourceId = null
            }, CancellationToken.None);
            IsFormOpen = false;
            InfoMessage = $"Đã lưu email sao kê vào hàng đợi ({queued.Id:N}); gửi khi SMTP được bật.";
        }
        catch (Exception ex) { FormErrorMessage = $"Không thể lưu email: {ex.Message}"; }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (Items.Count == 0)
        {
            InfoMessage = "Không có khách hàng công nợ thật để xuất.";
            return;
        }

        var path = CsvExportHelper.Export(
            "ar_customer_master",
            ["Mã KH", "Tên khách hàng", "Loại", "MST", "Điện thoại", "Email", "Hạn mức", "Công nợ", "Đặt cọc", "Hoàn cọc", "Tiền tệ", "Trạng thái"],
            Items.Select(item => (IReadOnlyList<string>)[
                item.CustomerCode,
                item.CustomerName,
                item.TypeLabel,
                item.TaxCode ?? string.Empty,
                item.Phone ?? string.Empty,
                item.Email ?? string.Empty,
                CsvExportHelper.F(item.CreditLimit),
                CsvExportHelper.F(item.Outstanding),
                CsvExportHelper.F(item.TotalDeposit),
                CsvExportHelper.F(item.TotalRefund),
                item.Currency,
                item.StatusLabel
            ]).ToList());

        InfoMessage = $"Đã xuất dữ liệu khách hàng thật dạng CSV (mở bằng Excel): {path}";
    }
    [RelayCommand]
    private void ExportPdf()
    {
        IEnumerable<ArCustomerDto> source = SelectedItem is not null ? new[] { SelectedItem } : Items;
        var rows = source.ToList();
        if (rows.Count == 0) { InfoMessage = "Không có khách hàng công nợ thật để xuất PDF."; return; }
        var path = PdfExportHelper.Export(
            "Khách hàng công nợ",
            "ar_customer_master",
            ["Mã khách", "Tên khách", "Loại", "Còn nợ", "Chờ thu", "Hóa đơn mở", "Trạng thái"],
            rows.Select(item => (IReadOnlyList<string>)[
                item.CustomerCode, item.CustomerName, item.TypeLabel,
                CsvExportHelper.F(item.Outstanding), CsvExportHelper.F(item.PendingPayment),
                item.OpenInvoiceCount.ToString(), item.StatusLabel]).ToList());
        if (!string.IsNullOrWhiteSpace(path)) InfoMessage = $"Đã xuất PDF khách hàng công nợ thật: {path}";
    }
    [RelayCommand] private void RefreshCommand() { _ = LoadAsync(CancellationToken.None); }
    [RelayCommand] private void PrintStatement()
    {
        IEnumerable<ArCustomerDto> rowsSource = SelectedItem is not null ? new[] { SelectedItem } : Items;
        var rows = rowsSource.ToList();
        if (rows.Count == 0) { InfoMessage = "Không có khách hàng công nợ thật để in."; return; }
        var content = new StringBuilder().AppendLine("DANH SÁCH KHÁCH HÀNG CÔNG NỢ").AppendLine(new string('-', 100));
        foreach (var item in rows)
            content.AppendLine($"{item.CustomerCode,-16} | {item.CustomerName,-30} | Công nợ {item.Outstanding,15:N0} {item.Currency} | {item.StatusLabel}");
        var preview = new PrintPreviewDialog("Khách hàng công nợ", content.ToString()) { Owner = System.Windows.Application.Current.MainWindow };
        preview.ShowDialog();
        InfoMessage = "Đã mở bản xem trước từ dữ liệu khách hàng công nợ thật.";
    }
}
