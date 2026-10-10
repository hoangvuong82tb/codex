using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class ArCompanyDto
{
    public string CustomerId { get; set; } = "";
    public string CompanyCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string? ShortName { get; set; }
    public string? EnglishName { get; set; }
    public string CompanyType { get; set; } = "Corporate";
    public string TypeLabel => CompanyType switch
    {
        "Corporate" => "Doanh nghiệp", "TravelAgent" => "Travel Agent", "Company" => "Công ty",
        "Government" => "Chính phủ", "Education" => "Giáo dục", "Medical" => "Y tế",
        "Supplier" => "Nhà cung cấp", _ => CompanyType
    };
    public string TypeColor => CompanyType switch
    {
        "Corporate" => "#2563EB", "TravelAgent" => "#0891B2", "Company" => "#D97706",
        "Government" => "#7C3AED", "Education" => "#16A34A", "Medical" => "#E32F70",
        "Supplier" => "#F59E0B", _ => "#64748B"
    };
    public string TypeBg => CompanyType switch
    {
        "Corporate" => "#DBEAFE", "TravelAgent" => "#ECFEFF", "Company" => "#FFF7ED",
        "Government" => "#F3E8FF", "Education" => "#DCFCE7", "Medical" => "#FFF0F5",
        "Supplier" => "#FFFBEB", _ => "#F8FAFC"
    };

    public string? TaxCode { get; set; }
    public string? BusinessLicense { get; set; }
    public DateTime? FoundedDate { get; set; }
    public int? EmployeeCount { get; set; }

    public string? Address { get; set; }
    public string? BillingAddress { get; set; }
    public string? InvoiceAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public string? Market { get; set; }
    public string? Source { get; set; }
    public string? Segment { get; set; }
    public string? SalesPerson { get; set; }
    public string? SalesManager { get; set; }
    public string? SalesRegion { get; set; }
    public string? Priority { get; set; }

    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; } = 30;
    public string? PaymentTerms { get; set; }
    public string Currency { get; set; } = "VND";
    public bool VatRequired { get; set; }
    public string? InvoiceType { get; set; }
    public bool RequireDeposit { get; set; }
    public bool NeedPo { get; set; }
    public bool NeedContract { get; set; }
    public bool AllowCredit { get; set; }
    public bool AutoInvoice { get; set; }
    public bool AutoEmailInvoice { get; set; }

    public decimal Outstanding { get; set; }
    public decimal CurrentDebt => Outstanding;
    public decimal Debt1To30 { get; set; }
    public decimal Debt31To60 { get; set; }
    public decimal Debt61To90 { get; set; }
    public decimal DebtOver90 { get; set; }
    public decimal AvailableCredit => Math.Max(0, CreditLimit - Outstanding);
    public bool IsOverCredit => CreditLimit > 0 && Outstanding > CreditLimit;

    public int ReservationCount { get; set; }
    public int GroupCount { get; set; }
    public int OpenInvoiceCount { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal TotalRefund { get; set; }
    public int StayCount { get; set; }
    public int RoomNightCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime? LastStayDate { get; set; }
    public DateTime? LastBookingDate { get; set; }
    public DateTime? LastInvoiceDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public string Status { get; set; } = "Active";
    public string StatusLabel => Status switch
    {
        "Active" => "Hoạt động", "Blocked" => "Đã khóa", "Inactive" => "Không HĐ",
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

    public string? CrmStatus { get; set; }
    public string CrmLabel => CrmStatus switch
    {
        "Lead" => "Lead", "Prospect" => "Tiềm năng", "Customer" => "KH", "Lost" => "Mất", "Inactive" => "Không HĐ", _ => CrmStatus ?? "—"
    };
    public string CrmColor => CrmStatus switch
    {
        "Lead" => "#F59E0B", "Prospect" => "#2563EB", "Customer" => "#16A34A", "Lost" => "#DC2626", "Inactive" => "#64748B", _ => "#64748B"
    };

    public bool HasWarning => Warnings.Count > 0;
    public int WarningCount => Warnings.Count;
    public List<string> Warnings { get; set; } = new();

    public List<ArCompanyContactDto> Contacts { get; set; } = new();
    public List<ArCompanyContractDto> Contracts { get; set; } = new();
    public List<ArCompanyRateDto> Rates { get; set; } = new();
    public List<ArCompanyReservationDto> Reservations { get; set; } = new();
    public List<ArCompanyGroupDto> Groups { get; set; } = new();
    public List<ArCompanyFolioDto> Folios { get; set; } = new();
    public List<ArCompanyInvoiceDto> Invoices { get; set; } = new();
    public List<ArCompanyPaymentDto> Payments { get; set; } = new();
    public List<ArCompanyDocumentDto> Documents { get; set; } = new();
    public List<ArCompanyNoteDto> Notes { get; set; } = new();
    public List<ArCompanyHistoryDto> History { get; set; } = new();
    public List<ArCompanyAuditEntry> AuditLog { get; set; } = new();
}

public sealed class ArCompanyContactDto
{
    public string Name { get; set; } = "";
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? Birthday { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsAccounting { get; set; }
    public bool IsReservation { get; set; }
    public bool IsManager { get; set; }
    public bool IsDecisionMaker { get; set; }
    public bool IsSales { get; set; }
}

public sealed class ArCompanyContractDto
{
    public string ContractNo { get; set; } = "";
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal Discount { get; set; }
    public decimal Commission { get; set; }
    public string? BlackoutDates { get; set; }
    public string? Guarantee { get; set; }
    public string? Cancellation { get; set; }
    public string? SpecialCondition { get; set; }
    public string Status { get; set; } = "Active";
    public string StatusLabel => Status switch { "Active" => "Hiệu lực", "Expired" => "Hết hạn", "Pending" => "Chờ ký", "Cancelled" => "Đã hủy", _ => Status };
    public string StatusColor => Status switch { "Active" => "#16A34A", "Expired" => "#DC2626", "Pending" => "#F59E0B", "Cancelled" => "#64748B", _ => "#64748B" };
}

public sealed class ArCompanyRateDto
{
    public string RoomType { get; set; } = "";
    public string Season { get; set; } = "";
    public decimal Rate { get; set; }
    public string? MealPlan { get; set; }
    public decimal WeekendRate { get; set; }
    public string? Promotion { get; set; }
    public string RateType { get; set; } = "Corporate";
}

public sealed class ArCompanyReservationDto
{
    public string ReservationNo { get; set; } = "";
    public DateTime BookingDate { get; set; }
    public DateTime Arrival { get; set; }
    public DateTime Departure { get; set; }
    public int Nights { get; set; }
    public string? RoomType { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Confirmed";
}

public sealed class ArCompanyGroupDto
{
    public string GroupName { get; set; } = "";
    public DateTime Arrival { get; set; }
    public DateTime Departure { get; set; }
    public int Rooms { get; set; }
    public int Guests { get; set; }
    public decimal Revenue { get; set; }
    public string Status { get; set; } = "Confirmed";
}

public sealed class ArCompanyFolioDto
{
    public string FolioNo { get; set; } = "";
    public string Type { get; set; } = "Master";
    public decimal Outstanding { get; set; }
    public string? InvoiceNo { get; set; }
    public string Status { get; set; } = "Open";
}

public sealed class ArCompanyInvoiceDto
{
    public string InvoiceNo { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Vat { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = "Open";
    public string StatusLabel => Status switch { "Open" => "Mở", "Paid" => "Đã TT", "PartiallyPaid" => "Một phần", "Overdue" => "Quá hạn", "Cancelled" => "Đã hủy", _ => Status };
    public string StatusColor => Status switch { "Open" => "#F59E0B", "Paid" => "#16A34A", "PartiallyPaid" => "#2563EB", "Overdue" => "#DC2626", "Cancelled" => "#64748B", _ => "#64748B" };
}

public sealed class ArCompanyPaymentDto
{
    public string ReceiptNo { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Tiền mặt";
    public string? Collector { get; set; }
    public string? Reference { get; set; }
    public string Type { get; set; } = "Payment";
    public string TypeLabel => Type switch { "Payment" => "Thanh toán", "Deposit" => "Đặt cọc", "Refund" => "Hoàn tiền", "Transfer" => "Chuyển khoản", "Adjustment" => "Điều chỉnh", _ => Type };
}

public sealed class ArCompanyDocumentDto
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string TypeLabel => Type switch
    {
        "BusinessLicense" => "GPKD", "TaxCertificate" => "MST", "Contract" => "Hợp đồng",
        "Quotation" => "Báo giá", "Proposal" => "Đề xuất", "Image" => "Ảnh",
        "PDF" => "PDF", "Word" => "Word", "Excel" => "Excel", _ => Type
    };
    public DateTime UploadDate { get; set; }
    public string? FileName { get; set; }
}

public sealed class ArCompanyNoteDto
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "Internal";
    public string TypeLabel => Type switch { "Reminder" => "Nhắc", "PhoneCall" => "Gọi", "Email" => "Email", "Internal" => "Nội bộ", "Meeting" => "Họp", "Visit" => "Thăm", _ => Type };
    public string Content { get; set; } = "";
    public string Author { get; set; } = "";
}

public sealed class ArCompanyHistoryDto
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "";
    public string TypeLabel => Type switch
    {
        "Reservation" => "Đặt phòng", "Group" => "Đoàn", "Invoice" => "Hóa đơn",
        "Payment" => "Thanh toán", "Contract" => "Hợp đồng", "Email" => "Email",
        "Call" => "Cuộc gọi", "Visit" => "Thăm", "Remark" => "Ghi chú", _ => Type
    };
    public string Detail { get; set; } = "";
    public string? Reference { get; set; }
}

public sealed class ArCompanyAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? Device { get; set; }
}

public sealed class ArCompanyKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class ArCompanyPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }
    public ArCompanyPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    { Label = label; PageNumber = pageNumber; IsCurrent = isCurrent; IsEllipsis = isEllipsis; }
}

// ═══ VIEW MODEL ═══

public sealed partial class ArCompanyViewModel : ViewModelBase
{
    private readonly IReceivableService? _receivableService;
    private readonly ICustomerAppService? _customerAppService;
    private readonly IEmailDispatchService? _emailDispatchService;

    public ArCompanyViewModel(IReceivableService? receivableService = null, ICustomerAppService? customerAppService = null, IEmailDispatchService? emailDispatchService = null)
    {
        _receivableService = receivableService;
        _customerAppService = customerAppService;
        _emailDispatchService = emailDispatchService;
    }

    public ObservableCollection<ArCompanyDto> Items { get; } = new();
    public ObservableCollection<ArCompanyDto> FilteredItems { get; } = new();
    public ObservableCollection<ArCompanyKpi> DashboardKpis { get; } = new();
    public ObservableCollection<ArCompanyPaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private ArCompanyDto? _selectedItem;
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
    [ObservableProperty] private bool _filterHasContract;
    [ObservableProperty] private bool _filterExpiredContract;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    [ObservableProperty] private bool _isRightPanelOpen = true;

    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formMode = string.Empty;
    [ObservableProperty] private string _formTitle = string.Empty;
    [ObservableProperty] private string _formErrorMessage = string.Empty;

    [ObservableProperty] private string _editCompanyName = "";
    [ObservableProperty] private string _editShortName = "";
    [ObservableProperty] private string _editCompanyType = "Corporate";
    [ObservableProperty] private string _editTaxCode = "";
    [ObservableProperty] private string _editAddress = "";
    [ObservableProperty] private string _editPhone = "";
    [ObservableProperty] private string _editEmail = "";
    [ObservableProperty] private string _editSalesPerson = "";
    [ObservableProperty] private decimal _editCreditLimit;
    [ObservableProperty] private int _editCreditDays = 30;
    [ObservableProperty] private string _editPaymentTerms = "Net 30";

    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private string _paymentMethod = "Tiền mặt";
    [ObservableProperty] private string _paymentReference = "";

    [ObservableProperty] private string _emailTo = "";
    [ObservableProperty] private string _emailType = "Statement";
    [ObservableProperty] private string _emailBody = "";

    public bool IsEditForm => FormMode == "Edit";
    public bool IsPaymentForm => FormMode == "Payment";
    public bool IsEmailForm => FormMode == "Email";
    public bool IsFormClosed => !IsFormOpen;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0 doanh nghiệp" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} doanh nghiệp";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedItem is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool IsGridView => ViewMode == "Grid";
    public bool IsCardView => ViewMode == "Card";

    public int TotalItems => Items.Count;
    public int ActiveCount => Items.Count(i => i.Status == "Active");
    public int BlockedCount => Items.Count(i => i.Status == "Blocked");
    public int HasDebtCount => Items.Count(i => i.Outstanding > 0);
    public int OverdueCount => Items.Count(i => i.DebtOver90 > 0);
    public int HasContractCount => Items.Count(i => i.Contracts.Any(c => c.Status == "Active"));
    public int ExpiredContractCount => Items.Count(i => i.Contracts.Any(c => c.Status == "Expired"));
    public decimal TotalOutstanding => Items.Sum(i => i.Outstanding);
    public decimal TotalRevenue => Items.Sum(i => i.TotalRevenue);

    public static string[] TypeFilters { get; } = ["Tất cả", "Corporate", "TravelAgent", "Company", "Government", "Education", "Medical", "Supplier"];
    public static string[] MarketFilters { get; } = ["Tất cả", "Nội địa", "Quốc tế", "Đông Nam Á", "Châu Âu", "Châu Mỹ", "Đông Bắc Á"];
    public static string[] SourceFilters { get; } = ["Tất cả", "Trực tiếp", "Booking.com", "Agoda", "Expedia", "Traveloka", "Website", "Phone", "Email", "Sales"];
    public static string[] CountryFilters { get; } = ["Tất cả", "Việt Nam", "Hàn Quốc", "Nhật Bản", "Trung Quốc", "Mỹ", "Anh", "Pháp", "Đức", "Úc", "Singapore", "Thái Lan"];
    public static string[] SalesFilters { get; } = ["Tất cả", "Nguyễn Văn Kinh Doanh", "Trần Thị Sales", "Lê Văn MKT", "Phạm Thị Business", "Hoàng Văn Corp"];
    public static string[] StatusFilters { get; } = ["Tất cả", "Active", "Blocked", "Inactive", "Pending", "Suspended"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];
    public static string[] PaymentMethods { get; } = ["Tiền mặt", "Chuyển khoản", "Thẻ tín dụng", "Thẻ ghi nợ", "Ví điện tử", "Séc"];

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

            var summaries = await _receivableService.GetDebtSummariesAsync(new ReceivableFilterDto(), ct);
            foreach (var summary in summaries)
                Items.Add(MapPersistedCompany(summary));

            BuildDashboard();
            ApplyFilters();
            InfoMessage = $"Đã tải {Items.Count:N0} đối tác từ dữ liệu công nợ thật. Chi tiết CRM chỉ hiển thị khi đã có nguồn dữ liệu tương ứng.";
            return;

#if false // Legacy designer fixture retained for historical reference only.
#pragma warning disable CS0162
            await Task.Delay(100, ct);
            var rng = new Random(88);

            var types = new[] { "Corporate", "Corporate", "Corporate", "TravelAgent", "TravelAgent", "Company", "Government", "Education", "Medical", "Supplier" };
            var statuses = new[] { "Active", "Active", "Active", "Active", "Active", "Blocked", "Inactive", "Pending" };
            var crmStatuses = new[] { "Customer", "Customer", "Customer", "Prospect", "Lead", "Lost", "Inactive" };
            var salesPeople = new[] { "Nguyễn Văn Kinh Doanh", "Trần Thị Sales", "Lê Văn MKT", "Phạm Thị Business", "Hoàng Văn Corp" };
            var salesManagers = new[] { "Nguyễn Văn Giám Đốc KD", "Trần Thị Director", "Lê Văn SM" };
            var markets = new[] { "Nội địa", "Quốc tế", "Đông Nam Á", "Châu Âu", "Châu Mỹ", "Đông Bắc Á" };
            var sources = new[] { "Trực tiếp", "Booking.com", "Agoda", "Expedia", "Traveloka", "Website", "Phone", "Email", "Sales" };
            var cities = new[] { "Hà Nội", "TP.HCM", "Đà Nẵng", "Nha Trang", "Phú Quốc", "Hải Phòng", "Cần Thơ", "Huế", "Đà Lạt", "Vũng Tàu" };
            var countries = new[] { "Việt Nam", "Hàn Quốc", "Nhật Bản", "Trung Quốc", "Mỹ", "Anh", "Pháp", "Đức", "Úc", "Singapore", "Thái Lan" };
            var positions = new[] { "Giám đốc", "Trưởng phòng KD", "Kế toán trưởng", "Trợ lý GĐ", "TP Đặt phòng", "TP Marketing", "Sales Manager", "Reservation Manager" };
            var contactNames = new[] { "Nguyễn Văn A", "Trần Thị B", "Lê Văn C", "Phạm Thị D", "Hoàng Văn E", "Đỗ Thị F", "Vũ Văn G", "Bùi Thị H" };
            var paymentTerms = new[] { "Net 15", "Net 30", "Net 45", "Net 60", "Net 90", "COD", "Prepayment 50%", "Monthly" };
            var roomTypes = new[] { "Superior", "Deluxe", "Suite", "Premium", "Standard", "Executive" };
            var seasons = new[] { "Low", "Shoulder", "High", "Peak", "Holiday" };
            var mealPlans = new[] { "RO", "BB", "HB", "FB", "AI" };
            var segments = new[] { "Leisure", "Corporate", "Government", "Group", "MICE" };

            var companyPrefixes = new[] { "Công ty TNHH", "Công ty CP", "Tập đoàn", "Tổng công ty", "CT TNHH", "CTCP" };
            var companySuffixes = new[] { "Thương mại & Dịch vụ", "Du lịch", "Xây dựng", "Vận tải", "Công nghệ", "Sản xuất", "Xuất nhập khẩu", "Đầu tư", "Bất động sản", "Tài chính", "Dầu khí", "Hàng không", "Dược phẩm", "May mặc" };
            var companyNames = new[] { "Hòa Bình", "Sao Mai", "Phương Nam", "Đại Việt", "Thăng Long", "Sông Hồng", "Biển Đông", "Trường Sơn", "Tây Nguyên", "Mê Kông", "Hoàng Gia", "Kim Cương", "Ngọc Trai", "Bạch Kim", "Hồng Hà", "Lam Sơn", "Phú Quốc", "Bà Nà", "Hạ Long", "Sơn Trà" };
            var taList = new[] { "Saigon Tourist", "Hanoi Travel", "Vietravel", "BenThanh Tourist", "Fiditour", "Buffalo Tours", "APT Travel", "Exotissimo", "Diethelm Travel", "Asian Trails" };
            var govList = new[] { "UBND TP Hà Nội", "UBND TP.HCM", "Bộ Ngoại giao", "Bộ Quốc phòng", "Tổng cục Du lịch", "Sở VHTTDL Hà Nội", "Sở VHTTDL TP.HCM", "Bộ Tài chính" };
            var corpList = new[] { "Vingroup", "FPT", "Vietcombank", "VietinBank", "Petrolimex", "VNPT", "Viettel", "Masan", "TH True Milk", "Vinamilk", "Techcombank", "BIDV", "Agribank", "MB Bank", "ACB" };
            var eduList = new[] { "Đại học Quốc gia Hà Nội", "Đại học Quốc gia TP.HCM", "Đại học Bách Khoa", "Đại học Kinh tế", "Đại học Ngoại thương", "RMIT Việt Nam" };
            var medList = new[] { "Bệnh viện Bạch Mai", "Bệnh viện Chợ Rẫy", "Bệnh viện Đại học Y Dược", "Bệnh viện FV", "Bệnh viện Vinmec", "Bệnh viện Hồng Ngọc" };

            for (int i = 0; i < 280; i++)
            {
                var type = types[rng.Next(types.Length)];
                var status = statuses[rng.Next(statuses.Length)];
                var city = cities[rng.Next(cities.Length)];
                var country = type == "Government" ? "Việt Nam" : countries[rng.Next(countries.Length)];

                string name, code;
                switch (type)
                {
                    case "TravelAgent":
                        name = taList[rng.Next(taList.Length)]; code = $"TA{rng.Next(100, 999)}"; break;
                    case "Government":
                        name = govList[rng.Next(govList.Length)]; code = $"GOV{rng.Next(100, 999)}"; break;
                    case "Corporate":
                        name = corpList[rng.Next(corpList.Length)]; code = $"CORP{rng.Next(100, 999)}"; break;
                    case "Education":
                        name = eduList[rng.Next(eduList.Length)]; code = $"EDU{rng.Next(100, 999)}"; break;
                    case "Medical":
                        name = medList[rng.Next(medList.Length)]; code = $"MED{rng.Next(100, 999)}"; break;
                    default:
                        var prefix = companyPrefixes[rng.Next(companyPrefixes.Length)];
                        var suffix = companySuffixes[rng.Next(companySuffixes.Length)];
                        var cname = companyNames[rng.Next(companyNames.Length)];
                        name = $"{prefix} {cname} {suffix}"; code = $"DN{rng.Next(10000, 99999)}"; break;
                }

                var creditLimit = type switch
                {
                    "Government" => (decimal)rng.Next(200000000, 2000000000),
                    "Corporate" => (decimal)rng.Next(100000000, 1000000000),
                    "TravelAgent" => (decimal)rng.Next(50000000, 500000000),
                    _ => (decimal)rng.Next(20000000, 200000000)
                };
                var outstanding = rng.Next(4) switch
                {
                    0 => 0, 1 => creditLimit * (decimal)(rng.Next(10, 45) / 100.0), _ => creditLimit * (decimal)(rng.Next(45, 90) / 100.0)
                };
                if (status == "Blocked") outstanding = creditLimit * (decimal)(rng.Next(95, 130) / 100.0);

                var createdDate = DateTime.Now.AddDays(-rng.Next(30, 1825));
                var item = new ArCompanyDto
                {
                    CompanyCode = code, CompanyName = name,
                    ShortName = name.Split(' ').LastOrDefault(),
                    EnglishName = $"{name} Co., Ltd.",
                    CompanyType = type,
                    TaxCode = rng.Next(4) > 0 ? $"MST{1000000000 + rng.Next(0, 999999999):D10}" : null,
                    BusinessLicense = $"GPKD{rng.Next(100000, 999999)}",
                    FoundedDate = createdDate.AddYears(-rng.Next(1, 25)),
                    EmployeeCount = rng.Next(10, 5000),
                    Address = $"{rng.Next(1, 999)} Đường {rng.Next(3) switch { 0 => "Nguyễn Huệ", 1 => "Lê Lợi", _ => "Trần Hưng Đạo" }}, {city}",
                    BillingAddress = $"{rng.Next(1, 999)} {rng.Next(3) switch { 0 => "Nguyễn Huệ", 1 => "Lê Lợi", _ => "Hai Bà Trưng" }}, {city}",
                    InvoiceAddress = $"{rng.Next(1, 999)} {rng.Next(3) switch { 0 => "Lý Thường Kiệt", 1 => "Phạm Ngũ Lão", _ => "Bùi Viện" }}, {city}",
                    City = city, Province = city, Country = country,
                    Phone = $"0{rng.Next(100000000, 999999999)}",
                    Email = $"{name.Split(' ').LastOrDefault()?.ToLower()}@{rng.Next(3) switch { 0 => "gmail.com", 1 => "yahoo.com", _ => "company.com.vn" }}",
                    Website = $"www.{name.Split(' ').LastOrDefault()?.ToLower()}.com.vn",
                    Market = markets[rng.Next(markets.Length)], Source = sources[rng.Next(sources.Length)],
                    Segment = segments[rng.Next(segments.Length)],
                    SalesPerson = salesPeople[rng.Next(salesPeople.Length)],
                    SalesManager = salesManagers[rng.Next(salesManagers.Length)],
                    SalesRegion = rng.Next(3) switch { 0 => "Miền Bắc", 1 => "Miền Trung", _ => "Miền Nam" },
                    Priority = rng.Next(3) switch { 0 => "Cao", 1 => "Trung bình", _ => "VIP" },
                    CreditLimit = creditLimit, CreditDays = rng.Next(4) switch { 0 => 15, 1 => 30, 2 => 45, _ => 60 },
                    PaymentTerms = paymentTerms[rng.Next(paymentTerms.Length)],
                    Currency = rng.Next(5) > 0 ? "VND" : "USD",
                    VatRequired = rng.Next(3) > 0,
                    InvoiceType = rng.Next(3) switch { 0 => "GTGT", 1 => "Bán hàng", _ => "Điện tử" },
                    RequireDeposit = rng.Next(3) == 1, NeedPo = rng.Next(5) == 1,
                    NeedContract = type is "Corporate" or "Government" or "TravelAgent",
                    AllowCredit = rng.Next(4) > 0, AutoInvoice = rng.Next(3) > 0, AutoEmailInvoice = rng.Next(3) == 1,
                    Outstanding = outstanding,
                    Debt1To30 = outstanding * (decimal)(rng.Next(20, 60) / 100.0),
                    Debt31To60 = outstanding * (decimal)(rng.Next(10, 30) / 100.0),
                    Debt61To90 = outstanding * (decimal)(rng.Next(5, 15) / 100.0),
                    DebtOver90 = outstanding * (decimal)(rng.Next(0, 10) / 100.0),
                    ReservationCount = rng.Next(0, 50),
                    GroupCount = rng.Next(0, 10),
                    OpenInvoiceCount = rng.Next(0, 12),
                    TotalDeposit = rng.Next(0, 100000000),
                    TotalRevenue = (decimal)rng.Next(10000000, 2000000000),
                    StayCount = rng.Next(0, 200),
                    RoomNightCount = rng.Next(0, 500),
                    LastStayDate = rng.Next(2) == 1 ? DateTime.Now.AddDays(-rng.Next(1, 90)) : null,
                    LastBookingDate = rng.Next(3) > 0 ? DateTime.Now.AddDays(-rng.Next(1, 30)) : null,
                    LastInvoiceDate = rng.Next(3) > 0 ? DateTime.Now.AddDays(-rng.Next(1, 45)) : null,
                    CreatedDate = createdDate, UpdatedDate = DateTime.Now.AddDays(-rng.Next(0, 30)),
                    Status = status, CrmStatus = crmStatuses[rng.Next(crmStatuses.Length)],
                };

                if (item.IsOverCredit) item.Warnings.Add("Credit Limit Exceeded");
                if (item.DebtOver90 > 0) item.Warnings.Add("Có công nợ quá hạn > 90 ngày");
                if (item.OpenInvoiceCount > 5) item.Warnings.Add($"{item.OpenInvoiceCount} hóa đơn mở");
                if (string.IsNullOrEmpty(item.TaxCode)) item.Warnings.Add("Thiếu MST");
                if (string.IsNullOrEmpty(item.Email)) item.Warnings.Add("Thiếu Email");
                if (string.IsNullOrEmpty(item.SalesPerson)) item.Warnings.Add("Thiếu Sales");
                if (!item.NeedContract) item.Warnings.Add("Không có hợp đồng");
                if (string.IsNullOrEmpty(item.BillingAddress)) item.Warnings.Add("Thiếu Billing Address");

                // Contacts
                var contactCnt = rng.Next(1, 5);
                for (int c = 0; c < contactCnt; c++)
                    item.Contacts.Add(new ArCompanyContactDto
                    {
                        Name = contactNames[rng.Next(contactNames.Length)],
                        Position = positions[rng.Next(positions.Length)],
                        Phone = $"0{rng.Next(100000000, 999999999)}",
                        Email = $"contact{c}@{(name.Split(' ').LastOrDefault()?.ToLower() ?? "company")}.com",
                        IsPrimary = c == 0, IsDecisionMaker = c == 0,
                        IsAccounting = c == 1, IsReservation = c == 2, IsManager = c == 1, IsSales = c == 2
                    });

                // Contracts
                if (type is "Corporate" or "TravelAgent" or "Government" || rng.Next(3) == 1)
                {
                    var contractCnt = rng.Next(1, 3);
                    for (int cIdx = 0; cIdx < contractCnt; cIdx++)
                        item.Contracts.Add(new ArCompanyContractDto
                        {
                            ContractNo = $"HD{code}-{cIdx + 1:D2}",
                            EffectiveDate = createdDate.AddDays(cIdx * 365),
                            ExpiryDate = createdDate.AddDays((cIdx + 1) * 365).AddDays(rng.Next(-30, 90)),
                            Discount = rng.Next(5, 30),
                            Commission = rng.Next(5, 15),
                            BlackoutDates = rng.Next(3) == 1 ? "Tết Nguyên Đán, 30/4-1/5, 2/9" : null,
                            Guarantee = rng.Next(3) switch { 0 => "Credit Card", 1 => "Deposit", _ => "PO" },
                            Cancellation = rng.Next(3) switch { 0 => "Free 24h", 1 => "Free 48h", _ => "Non-refundable" },
                            SpecialCondition = rng.Next(2) == 1 ? "Giá áp dụng tất cả mùa trừ Peak" : null,
                            Status = rng.Next(4) switch { 0 => "Active", 1 => "Active", 2 => "Expired", _ => "Pending" }
                        });
                }

                // Rates
                var rateCnt = rng.Next(2, 6);
                for (int r = 0; r < rateCnt; r++)
                    item.Rates.Add(new ArCompanyRateDto
                    {
                        RoomType = roomTypes[rng.Next(roomTypes.Length)],
                        Season = seasons[rng.Next(seasons.Length)],
                        Rate = rng.Next(800000, 8000000),
                        MealPlan = mealPlans[rng.Next(mealPlans.Length)],
                        WeekendRate = rng.Next(1000000, 9000000),
                        Promotion = rng.Next(3) == 1 ? $"PROMO{rng.Next(1, 5)}" : null,
                        RateType = rng.Next(3) switch { 0 => "Corporate", 1 => "Corporate", _ => "Dynamic" }
                    });

                // Reservations
                for (int res = 0; res < rng.Next(0, 8); res++)
                    item.Reservations.Add(new ArCompanyReservationDto
                    {
                        ReservationNo = $"RES{rng.Next(10000, 99999)}",
                        BookingDate = DateTime.Now.AddDays(-rng.Next(1, 365)),
                        Arrival = DateTime.Now.AddDays(-rng.Next(1, 180)),
                        Departure = DateTime.Now.AddDays(-rng.Next(1, 180)).AddDays(rng.Next(1, 7)),
                        Nights = rng.Next(1, 7),
                        RoomType = roomTypes[rng.Next(roomTypes.Length)],
                        Amount = rng.Next(1000000, 20000000),
                        Status = rng.Next(4) switch { 0 => "Confirmed", 1 => "Confirmed", 2 => "CheckedIn", _ => "Completed" }
                    });

                // Groups
                for (int g = 0; g < rng.Next(0, 3); g++)
                    item.Groups.Add(new ArCompanyGroupDto
                    {
                        GroupName = $"Đoàn {name.Split(' ').LastOrDefault()} {g + 1}",
                        Arrival = DateTime.Now.AddDays(-rng.Next(1, 180)),
                        Departure = DateTime.Now.AddDays(-rng.Next(1, 180)).AddDays(rng.Next(1, 5)),
                        Rooms = rng.Next(5, 30),
                        Guests = rng.Next(10, 60),
                        Revenue = rng.Next(20000000, 200000000),
                        Status = rng.Next(3) switch { 0 => "Confirmed", 1 => "InHouse", _ => "Completed" }
                    });

                // Folios
                for (int f = 0; f < rng.Next(1, 6); f++)
                    item.Folios.Add(new ArCompanyFolioDto
                    {
                        FolioNo = $"FOL{rng.Next(1000, 9999)}",
                        Type = rng.Next(3) switch { 0 => "Master", 1 => "Master", _ => "Sub" },
                        Outstanding = rng.Next(0, 20000000),
                        InvoiceNo = rng.Next(2) == 1 ? $"INV{code}-{f + 1:D3}" : null,
                        Status = rng.Next(3) switch { 0 => "Open", 1 => "Closed", _ => "Transferred" }
                    });

                // Invoices
                for (int inv = 0; inv < rng.Next(0, 10); inv++)
                {
                    var invAmt = (decimal)rng.Next(1000000, 50000000);
                    var invPaid = rng.Next(3) > 0 ? invAmt * (decimal)(rng.Next(50, 100) / 100.0) : 0;
                    item.Invoices.Add(new ArCompanyInvoiceDto
                    {
                        InvoiceNo = $"INV{code}-{inv + 1:D3}",
                        IssueDate = DateTime.Now.AddDays(-rng.Next(1, 365)),
                        Amount = invAmt, Vat = invAmt * 0.08m, Paid = invPaid,
                        Balance = invAmt * 1.08m - invPaid,
                        Status = invPaid >= (invAmt * 1.08m) ? "Paid" : invPaid > 0 ? "PartiallyPaid" : "Open"
                    });
                }

                // Payments
                for (int p = 0; p < rng.Next(0, 8); p++)
                    item.Payments.Add(new ArCompanyPaymentDto
                    {
                        ReceiptNo = $"PT{code}-{p + 1:D3}",
                        PaymentDate = DateTime.Now.AddDays(-rng.Next(1, 180)),
                        Amount = rng.Next(1000000, 20000000),
                        Method = PaymentMethods[rng.Next(PaymentMethods.Length)],
                        Collector = salesPeople[rng.Next(salesPeople.Length)],
                        Reference = $"REF{rng.Next(10000, 99999)}",
                        Type = rng.Next(5) switch { 0 => "Payment", 1 => "Payment", 2 => "Deposit", 3 => "Refund", _ => "Adjustment" }
                    });

                // Documents
                if (rng.Next(3) == 1)
                    item.Documents.Add(new ArCompanyDocumentDto { Name = $"GPKD_{code}", Type = "BusinessLicense", UploadDate = createdDate, FileName = $"gpdk_{code}.pdf" });
                if (item.Contracts.Count > 0)
                    item.Documents.Add(new ArCompanyDocumentDto { Name = $"HĐ_{code}", Type = "Contract", UploadDate = createdDate, FileName = $"contract_{code}.pdf" });
                if (!string.IsNullOrEmpty(item.TaxCode))
                    item.Documents.Add(new ArCompanyDocumentDto { Name = $"MST_{code}", Type = "TaxCertificate", UploadDate = createdDate, FileName = $"mst_{code}.pdf" });

                // Notes
                if (rng.Next(4) == 1)
                    item.Notes.Add(new ArCompanyNoteDto { Timestamp = createdDate.AddDays(rng.Next(1, 60)), Type = "Internal", Content = $"Khách hàng VIP — {item.Market}, {item.Segment}", Author = item.SalesPerson ?? "System" });
                if (rng.Next(3) == 1)
                    item.Notes.Add(new ArCompanyNoteDto { Timestamp = DateTime.Now.AddDays(-rng.Next(0, 30)), Type = "Meeting", Content = "Đã họp bàn về hợp đồng năm 2026", Author = item.SalesPerson ?? "System" });

                // History
                for (int h = 0; h < rng.Next(8, 20); h++)
                    item.History.Add(new ArCompanyHistoryDto
                    {
                        Timestamp = createdDate.AddDays(rng.Next(1, 365)),
                        Type = rng.Next(8) switch { 0 => "Reservation", 1 => "Group", 2 => "Invoice", 3 => "Payment", 4 => "Contract", 5 => "Email", 6 => "Call", _ => "Remark" },
                        Detail = $"Giao dịch #{h + 1}",
                        Reference = rng.Next(2) == 1 ? $"REF{rng.Next(10000, 99999)}" : null
                    });

                item.AuditLog.Add(new ArCompanyAuditEntry { Timestamp = createdDate, Action = "Created", Actor = item.SalesPerson ?? "System", Detail = $"Tạo doanh nghiệp {item.CompanyCode}", Device = "Front Office" });

                Items.Add(item);
            }

            BuildDashboard(); ApplyFilters();
#pragma warning restore CS0162
#endif
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static ArCompanyDto MapPersistedCompany(ReceivableSummaryDto summary)
    {
        var status = summary.Status == DebtStatus.Cancelled ? "Inactive" : "Active";
        return new ArCompanyDto
        {
            CustomerId = summary.CustomerId,
            CompanyCode = summary.CustomerCode,
            CompanyName = summary.CustomerName,
            ShortName = summary.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault(),
            CompanyType = "Company",
            Market = "Chưa phân loại",
            Source = "Dữ liệu công nợ",
            Currency = "VND",
            Outstanding = Math.Max(0, summary.ClosingBalance),
            DebtOver90 = Math.Max(0, summary.OverdueAmount),
            OpenInvoiceCount = summary.ClosingBalance > 0 ? 1 : 0,
            LastInvoiceDate = summary.LastTransactionDate,
            CreatedDate = summary.LastTransactionDate ?? DateTime.MinValue,
            UpdatedDate = summary.LastTransactionDate,
            Status = status
        };
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new ArCompanyKpi { Label = "Tổng DN", Value = TotalItems.ToString(), Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new ArCompanyKpi { Label = "Hoạt động", Value = ActiveCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4", FilterKey = "Status", FilterValue = "Active" });
        DashboardKpis.Add(new ArCompanyKpi { Label = "Bị khóa", Value = BlockedCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Status", FilterValue = "Blocked" });
        DashboardKpis.Add(new ArCompanyKpi { Label = "Có công nợ", Value = HasDebtCount.ToString(), Color = "#F59E0B", BgColor = "#FFFBEB", FilterKey = "HasDebt", FilterValue = "true" });
        DashboardKpis.Add(new ArCompanyKpi { Label = "Quá hạn", Value = OverdueCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2" });
        DashboardKpis.Add(new ArCompanyKpi { Label = "Có hợp đồng", Value = HasContractCount.ToString(), Color = "#2563EB", BgColor = "#EFF6FF" });
        DashboardKpis.Add(new ArCompanyKpi { Label = "Hết HĐ", Value = ExpiredContractCount.ToString(), Color = "#E32F70", BgColor = "#FFF0F5" });
        DashboardKpis.Add(new ArCompanyKpi { Label = "Tổng dư nợ", Value = $"{TotalOutstanding:N0} ₫", Color = "#DC2626", BgColor = "#FEF2F2" });
        DashboardKpis.Add(new ArCompanyKpi { Label = "Doanh thu", Value = $"{TotalRevenue:N0} ₫", Color = "#16A34A", BgColor = "#F0FDF4" });
    }

    [RelayCommand] private void ApplyFilters()
    {
        var q = Items.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s))
            q = q.Where(r => r.CompanyCode.ToLower().Contains(s) || r.CompanyName.ToLower().Contains(s) || (r.TaxCode?.Contains(s) ?? false) || (r.Phone?.Contains(s) ?? false) || (r.Email?.ToLower().Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterType) && FilterType != "Tất cả") q = q.Where(r => r.CompanyType == FilterType);
        if (!string.IsNullOrEmpty(FilterMarket) && FilterMarket != "Tất cả") q = q.Where(r => r.Market == FilterMarket);
        if (!string.IsNullOrEmpty(FilterSource) && FilterSource != "Tất cả") q = q.Where(r => r.Source == FilterSource);
        if (!string.IsNullOrEmpty(FilterCountry) && FilterCountry != "Tất cả") q = q.Where(r => r.Country == FilterCountry);
        if (!string.IsNullOrEmpty(FilterSales) && FilterSales != "Tất cả") q = q.Where(r => r.SalesPerson == FilterSales);
        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.Status == FilterStatus);
        if (FilterHasDebt) q = q.Where(r => r.Outstanding > 0);
        if (FilterOverdue) q = q.Where(r => r.DebtOver90 > 0);
        if (FilterHasContract) q = q.Where(r => r.Contracts.Any(c => c.Status == "Active"));
        if (FilterExpiredContract) q = q.Where(r => r.Contracts.Any(c => c.Status == "Expired"));

        var list = q.OrderByDescending(r => r.Outstanding).ThenBy(r => r.CompanyCode).ToList();
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
        FilterHasDebt = false; FilterOverdue = false; FilterHasContract = false; FilterExpiredContract = false;
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
    partial void OnFilterHasContractChanged(bool value) => ApplyFilters();
    partial void OnFilterExpiredContractChanged(bool value) => ApplyFilters();

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
            PageButtons.Add(p is null ? new ArCompanyPaginationButton("…", null, false, true)
                : new ArCompanyPaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == current));
    }

    [RelayCommand] private void SelectItem(ArCompanyDto? item) => SelectedItem = item;
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedItem = null; }
    [RelayCommand] private void ToggleRightPanel() => IsRightPanelOpen = !IsRightPanelOpen;

    partial void OnSelectedItemChanged(ArCompanyDto? value)
    { OnPropertyChanged(nameof(HasSelection)); if (value is not null) { IsDetailOpen = true; SelectedDetailTab = 0; } }
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsOverviewTab)); OnPropertyChanged(nameof(IsContactsTab));
        OnPropertyChanged(nameof(IsPaymentTermsTab)); OnPropertyChanged(nameof(IsContractsTab));
        OnPropertyChanged(nameof(IsRatesTab)); OnPropertyChanged(nameof(IsSalesTab));
        OnPropertyChanged(nameof(IsReservationsTab)); OnPropertyChanged(nameof(IsGroupsTab));
        OnPropertyChanged(nameof(IsFoliosTab)); OnPropertyChanged(nameof(IsInvoicesTab));
        OnPropertyChanged(nameof(IsPaymentsTab)); OnPropertyChanged(nameof(IsArTab));
        OnPropertyChanged(nameof(IsFilesTab)); OnPropertyChanged(nameof(IsHistoryTab));
        OnPropertyChanged(nameof(IsCrmTab));
    }
    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsContactsTab => SelectedDetailTab == 1;
    public bool IsPaymentTermsTab => SelectedDetailTab == 2;
    public bool IsContractsTab => SelectedDetailTab == 3;
    public bool IsRatesTab => SelectedDetailTab == 4;
    public bool IsSalesTab => SelectedDetailTab == 5;
    public bool IsReservationsTab => SelectedDetailTab == 6;
    public bool IsGroupsTab => SelectedDetailTab == 7;
    public bool IsFoliosTab => SelectedDetailTab == 8;
    public bool IsInvoicesTab => SelectedDetailTab == 9;
    public bool IsPaymentsTab => SelectedDetailTab == 10;
    public bool IsArTab => SelectedDetailTab == 11;
    public bool IsFilesTab => SelectedDetailTab == 12;
    public bool IsHistoryTab => SelectedDetailTab == 13;
    public bool IsCrmTab => SelectedDetailTab == 14;

    partial void OnFormModeChanged(string value) { OnPropertyChanged(nameof(IsEditForm)); OnPropertyChanged(nameof(IsPaymentForm)); OnPropertyChanged(nameof(IsEmailForm)); }
    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(IsFormClosed));
    partial void OnViewModeChanged(string value) { OnPropertyChanged(nameof(IsGridView)); OnPropertyChanged(nameof(IsCardView)); }

    [RelayCommand] private void SetGridView() { ViewMode = "Grid"; }
    [RelayCommand] private void SetCardView() { ViewMode = "Card"; }
    [RelayCommand] private void CloseForm() { IsFormOpen = false; FormErrorMessage = string.Empty; }

    [RelayCommand] private void OpenEditForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Edit"; FormTitle = $"Chỉnh sửa — {SelectedItem.CompanyName}"; FormErrorMessage = string.Empty;
        EditCompanyName = SelectedItem.CompanyName; EditShortName = SelectedItem.ShortName ?? "";
        EditCompanyType = SelectedItem.CompanyType; EditTaxCode = SelectedItem.TaxCode ?? "";
        EditAddress = SelectedItem.Address ?? ""; EditPhone = SelectedItem.Phone ?? "";
        EditEmail = SelectedItem.Email ?? ""; EditSalesPerson = SelectedItem.SalesPerson ?? "";
        EditCreditLimit = SelectedItem.CreditLimit; EditCreditDays = SelectedItem.CreditDays;
        EditPaymentTerms = SelectedItem.PaymentTerms ?? "Net 30";
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitEditForm()
    {
        if (SelectedItem is null) return;
        if (string.IsNullOrWhiteSpace(EditCompanyName)) { FormErrorMessage = "Vui lòng nhập tên doanh nghiệp."; return; }
        if (_customerAppService is null || !int.TryParse(SelectedItem.CustomerId, out var customerId))
        {
            FormErrorMessage = "Đối tác chưa có mã dữ liệu thật hoặc Company Master service chưa sẵn sàng.";
            return;
        }

        var customer = await _customerAppService.GetAsync(customerId);
        if (customer is null)
        {
            FormErrorMessage = "Không tìm thấy đối tác trong CSDL.";
            return;
        }

        customer.CustomerName = EditCompanyName.Trim();
        customer.Phone = EditPhone?.Trim() ?? string.Empty;
        customer.Address = EditAddress?.Trim() ?? string.Empty;
        await _customerAppService.SaveAsync(customer);
        IsFormOpen = false;
        await LoadAsync(CancellationToken.None);
        InfoMessage = $"Đã cập nhật đối tác {customer.CustomerName} trong CSDL.";
    }

    [RelayCommand] private async Task ToggleLock()
    {
        if (SelectedItem is null) return;
        if (_customerAppService is null || !int.TryParse(SelectedItem.CustomerId, out var customerId))
        {
            InfoMessage = "Đối tác chưa có mã dữ liệu thật hoặc Company Master service chưa sẵn sàng.";
            return;
        }

        var customer = await _customerAppService.GetAsync(customerId);
        if (customer is null)
        {
            InfoMessage = "Không tìm thấy đối tác trong CSDL.";
            return;
        }

        customer.IsActive = !customer.IsActive;
        await _customerAppService.SaveAsync(customer);
        var action = customer.IsActive ? "mở khóa" : "khóa";
        await LoadAsync(CancellationToken.None);
        InfoMessage = $"Đã {action} đối tác {customer.CustomerName} trong CSDL.";
    }

    [RelayCommand] private void OpenPaymentForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Payment"; FormTitle = $"Thu tiền — {SelectedItem.CompanyName}"; FormErrorMessage = string.Empty;
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
            FormErrorMessage = "Chưa thể thu tiền: đối tác chưa có mã dữ liệu thật hoặc dịch vụ công nợ chưa sẵn sàng.";
            return;
        }

        var companyName = SelectedItem.CompanyName;
        var amount = PaymentAmount;
        var receiptNo = await _receivableService.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
        {
            CustomerId = SelectedItem.CustomerId,
            ReceiptDate = DateTime.Now,
            Amount = amount,
            PaymentMethod = PaymentMethod,
            Note = string.IsNullOrWhiteSpace(PaymentReference) ? $"Thu công nợ {SelectedItem.CompanyCode}" : PaymentReference
        }, CancellationToken.None);

        if (string.IsNullOrWhiteSpace(receiptNo))
        {
            FormErrorMessage = "Không tạo được phiếu thu trên dữ liệu thật.";
            return;
        }

        IsFormOpen = false;
        await LoadAsync(CancellationToken.None);
        InfoMessage = $"Đã tạo phiếu thu {receiptNo} — {amount:N0} ₫ từ {companyName}.";
    }

    [RelayCommand] private void OpenEmailForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Email"; FormTitle = $"Gửi Email — {SelectedItem.CompanyName}"; FormErrorMessage = string.Empty;
        EmailType = "Statement"; EmailTo = SelectedItem.Email ?? "";
        EmailBody = $"Kính gửi {SelectedItem.CompanyName},\n\nVui lòng xem sao kê công nợ đính kèm.\n\nTrân trọng,\nASMILE BO Accounting";
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
                Subject = $"Sao kê công nợ - {SelectedItem.CompanyName}",
                Body = EmailBody,
                SourceType = "ArCompany",
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
            InfoMessage = "Không có doanh nghiệp công nợ thật để xuất.";
            return;
        }

        var path = CsvExportHelper.Export(
            "ar_company_master",
            ["Mã DN", "Tên doanh nghiệp", "Loại", "MST", "Điện thoại", "Email", "Hạn mức", "Công nợ", "Doanh thu", "Đặt cọc", "Hoàn cọc", "Tiền tệ", "Trạng thái"],
            Items.Select(item => (IReadOnlyList<string>)[
                item.CompanyCode,
                item.CompanyName,
                item.TypeLabel,
                item.TaxCode ?? string.Empty,
                item.Phone ?? string.Empty,
                item.Email ?? string.Empty,
                CsvExportHelper.F(item.CreditLimit),
                CsvExportHelper.F(item.Outstanding),
                CsvExportHelper.F(item.TotalRevenue),
                CsvExportHelper.F(item.TotalDeposit),
                CsvExportHelper.F(item.TotalRefund),
                item.Currency,
                item.StatusLabel
            ]).ToList());

        InfoMessage = $"Đã xuất dữ liệu doanh nghiệp thật dạng CSV (mở bằng Excel): {path}";
    }
    [RelayCommand]
    private void ExportPdf()
    {
        IEnumerable<ArCompanyDto> source = SelectedItem is not null ? new[] { SelectedItem } : Items;
        var rows = source.ToList();
        if (rows.Count == 0) { InfoMessage = "Không có doanh nghiệp công nợ thật để xuất PDF."; return; }
        var path = PdfExportHelper.Export(
            "Doanh nghiệp công nợ",
            "ar_company_master",
            ["Mã doanh nghiệp", "Tên doanh nghiệp", "Loại", "Còn nợ", "Tổng doanh thu", "Hóa đơn mở", "Trạng thái"],
            rows.Select(item => (IReadOnlyList<string>)[
                item.CompanyCode, item.CompanyName, item.TypeLabel,
                CsvExportHelper.F(item.Outstanding), CsvExportHelper.F(item.TotalRevenue),
                item.OpenInvoiceCount.ToString(), item.StatusLabel]).ToList());
        if (!string.IsNullOrWhiteSpace(path)) InfoMessage = $"Đã xuất PDF doanh nghiệp công nợ thật: {path}";
    }
    [RelayCommand] private void RefreshCommand() { _ = LoadAsync(CancellationToken.None); }
    [RelayCommand] private void PrintStatement()
    {
        IEnumerable<ArCompanyDto> rowsSource = SelectedItem is not null ? new[] { SelectedItem } : Items;
        var rows = rowsSource.ToList();
        if (rows.Count == 0) { InfoMessage = "Không có doanh nghiệp công nợ thật để in."; return; }
        var content = new StringBuilder().AppendLine("DANH SÁCH DOANH NGHIỆP CÔNG NỢ").AppendLine(new string('-', 100));
        foreach (var item in rows)
            content.AppendLine($"{item.CompanyCode,-16} | {item.CompanyName,-30} | Công nợ {item.Outstanding,15:N0} {item.Currency} | {item.StatusLabel}");
        var preview = new PrintPreviewDialog("Doanh nghiệp công nợ", content.ToString()) { Owner = System.Windows.Application.Current.MainWindow };
        preview.ShowDialog();
        InfoMessage = "Đã mở bản xem trước từ dữ liệu doanh nghiệp công nợ thật.";
    }
}
