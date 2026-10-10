using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using System.Text;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class ArInvRecordDto
{
    public Guid InvoiceId { get; set; }
    public string? ReceivableCustomerId { get; set; }
    public string InvoiceNo { get; set; } = "";
    public string InvoiceType { get; set; } = "Standard";
    public string TypeLabel => InvoiceType switch
    {
        "Standard" => "Hóa đơn", "VAT" => "HĐ VAT", "Receipt" => "Phiếu thu",
        "CreditNote" => "Ghi có", "DebitNote" => "Ghi nợ", "Proforma" => "Tạm tính",
        "Advance" => "Tạm ứng", "Group" => "Đoàn", "Company" => "Doanh nghiệp",
        "Agency" => "Đại lý", "Cash" => "Tiền mặt", "AR" => "Công nợ", _ => InvoiceType
    };
    public string TypeColor => InvoiceType switch
    {
        "Standard" => "#2563EB", "VAT" => "#7C3AED", "Receipt" => "#16A34A",
        "CreditNote" => "#0891B2", "DebitNote" => "#D97706", "Proforma" => "#F59E0B",
        "Advance" => "#E32F70", "Group" => "#7C3AED", "Company" => "#2563EB",
        "Agency" => "#0891B2", "Cash" => "#16A34A", "AR" => "#DC2626", _ => "#64748B"
    };

    public DateTime InvoiceDate { get; set; }
    public DateTime PostingDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? ReservationNo { get; set; }
    public string? RoomNo { get; set; }
    public string? GuestName { get; set; }
    public string? CompanyName { get; set; }
    public string? GroupName { get; set; }
    public string? Market { get; set; }
    public string? Source { get; set; }
    public string? SalesPerson { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal ExchangeRate { get; set; } = 1;
    public string? Cashier { get; set; }

    public string? BuyerName { get; set; }
    public string? BuyerAddress { get; set; }
    public string? BuyerTaxCode { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }

    public string? VatNumber { get; set; }
    public string? VatTemplate { get; set; }
    public string? VatSymbol { get; set; }
    public string? VatStatus { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DepositUsed { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal Adjustment { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }

    public string Status { get; set; } = "Open";
    public string StatusLabel => Status switch
    {
        "Open" => "Mở", "Paid" => "Đã TT", "PartiallyPaid" => "Một phần",
        "Overdue" => "Quá hạn", "Cancelled" => "Đã hủy", "Printed" => "Đã in",
        "Emailed" => "Đã gửi", "Closed" => "Đã đóng", _ => Status
    };
    public string StatusColor => Status switch
    {
        "Open" => "#F59E0B", "Paid" => "#16A34A", "PartiallyPaid" => "#2563EB",
        "Overdue" => "#DC2626", "Cancelled" => "#64748B", "Printed" => "#0891B2",
        "Emailed" => "#7C3AED", "Closed" => "#64748B", _ => "#64748B"
    };
    public string StatusBg => Status switch
    {
        "Open" => "#FFFBEB", "Paid" => "#DCFCE7", "PartiallyPaid" => "#DBEAFE",
        "Overdue" => "#FEE2E2", "Cancelled" => "#F1F5F9", _ => "#F8FAFC"
    };

    public bool IsPrinted { get; set; }
    public bool IsEmailed { get; set; }
    public bool HasVat { get; set; }
    public int PrintCount { get; set; }

    public string? WarningText => Outstanding > 0 && Status == "Overdue" ? "Quá hạn thanh toán"
        : string.IsNullOrEmpty(BuyerTaxCode) && HasVat ? "Thiếu MST" : null;

    public List<ArInvoiceLineDto> Lines { get; set; } = new();
    public List<ArInvoicePaymentDto> Payments { get; set; } = new();
    public List<ArInvoiceFolioDto> Folios { get; set; } = new();
    public List<ArInvoiceEmailDto> Emails { get; set; } = new();
    public List<ArInvoicePrintDto> Prints { get; set; } = new();
    public List<ArInvoiceAuditDto> Audits { get; set; } = new();
}

public sealed class ArInvoiceLineDto
{
    public DateTime PostingDate { get; set; }
    public string Description { get; set; } = "";
    public string Department { get; set; } = "";
    public string? RoomNo { get; set; }
    public decimal Qty { get; set; } = 1;
    public string? Unit { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Amount { get; set; }
    public string? FolioNo { get; set; }
    public string? TransferFrom { get; set; }
    public string? TransferTo { get; set; }
    public string? Remark { get; set; }
}

public sealed class ArInvoicePaymentDto
{
    public string ReceiptNo { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Tiền mặt";
    public string? Reference { get; set; }
    public string? Collector { get; set; }
    public string Type { get; set; } = "Payment";
    public string TypeLabel => Type switch { "Payment" => "Thanh toán", "Deposit" => "Đặt cọc", "Refund" => "Hoàn tiền", "Transfer" => "Chuyển khoản", "Adjustment" => "Điều chỉnh", _ => Type };
}

public sealed class ArInvoiceFolioDto
{
    public string FolioNo { get; set; } = "";
    public string Type { get; set; } = "Master";
    public decimal Balance { get; set; }
    public string Status { get; set; } = "Open";
}

public sealed class ArInvoiceEmailDto
{
    public DateTime SentDate { get; set; }
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Status { get; set; } = "Sent";
    public string? Attachment { get; set; }
}

public sealed class ArInvoicePrintDto
{
    public DateTime PrintedDate { get; set; }
    public string PrintedBy { get; set; } = "";
    public string? Printer { get; set; }
    public int ReprintCount { get; set; }
}

public sealed class ArInvoiceAuditDto
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string ActionLabel => Action switch
    {
        "Create" => "Tạo", "Modify" => "Sửa", "Cancel" => "Hủy", "Approve" => "Duyệt",
        "Print" => "In", "Email" => "Gửi", "Export" => "Xuất", "Payment" => "Thanh toán",
        "Refund" => "Hoàn tiền", "Adjustment" => "Điều chỉnh", "Close" => "Đóng", _ => Action
    };
    public string Actor { get; set; } = "";
    public string? Detail { get; set; }
}

public sealed class ArInvoiceKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
}

public sealed class ArInvoicePaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }
    public ArInvoicePaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    { Label = label; PageNumber = pageNumber; IsCurrent = isCurrent; IsEllipsis = isEllipsis; }
}

// ═══ VIEW MODEL ═══

public sealed partial class ArInvoiceViewModel : ViewModelBase
{
    private readonly IEInvoiceService? _invoiceService;
    private readonly IReceivableService? _receivableService;
    private readonly IEmailDispatchService? _emailDispatchService;

    public ArInvoiceViewModel(IEInvoiceService? invoiceService = null, IEmailDispatchService? emailDispatchService = null, IReceivableService? receivableService = null)
    {
        _invoiceService = invoiceService;
        _emailDispatchService = emailDispatchService;
        _receivableService = receivableService;
    }

    public ObservableCollection<ArInvRecordDto> Items { get; } = new();
    public ObservableCollection<ArInvRecordDto> FilteredItems { get; } = new();
    public ObservableCollection<ArInvoiceKpi> DashboardKpis { get; } = new();
    public ObservableCollection<ArInvoicePaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private ArInvRecordDto? _selectedItem;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private string _viewMode = "Grid";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterType;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterCurrency;
    [ObservableProperty] private string? _filterMarket;
    [ObservableProperty] private string? _filterCashier;
    [ObservableProperty] private bool _filterOverdue;
    [ObservableProperty] private bool _filterHasVat;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    [ObservableProperty] private bool _isRightPanelOpen = true;

    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formMode = string.Empty;
    [ObservableProperty] private string _formTitle = string.Empty;
    [ObservableProperty] private string _formErrorMessage = string.Empty;

    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private string _paymentMethod = "Tiền mặt";
    [ObservableProperty] private string _paymentReference = "";

    [ObservableProperty] private string _emailTo = "";
    [ObservableProperty] private string _emailSubject = "";
    [ObservableProperty] private string _emailBody = "";

    public bool IsPaymentForm => FormMode == "Payment";
    public bool IsEmailForm => FormMode == "Email";
    public bool IsFormClosed => !IsFormOpen;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0 hóa đơn" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} hóa đơn";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedItem is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool IsGridView => ViewMode == "Grid";
    public bool IsCardView => ViewMode == "Card";

    public int TotalItems => Items.Count;
    public int OpenCount => Items.Count(i => i.Status == "Open");
    public int PaidCount => Items.Count(i => i.Status == "Paid");
    public int OverdueCount => Items.Count(i => i.Status == "Overdue");
    public int CancelledCount => Items.Count(i => i.Status == "Cancelled");
    public int TodayCount => Items.Count(i => i.InvoiceDate.Date == DateTime.Today);
    public int MonthCount => Items.Count(i => i.InvoiceDate.Year == DateTime.Today.Year && i.InvoiceDate.Month == DateTime.Today.Month);
    public decimal TotalRevenue => Items.Sum(i => i.GrandTotal);
    public decimal TotalOutstanding => Items.Sum(i => i.Outstanding);
    public decimal TotalPaid => Items.Sum(i => i.Paid);
    public decimal TotalVat => Items.Sum(i => i.VatAmount);

    public static string[] TypeFilters { get; } = ["Tất cả", "Standard", "VAT", "Receipt", "CreditNote", "DebitNote", "Proforma", "Group", "Company", "Agency", "Cash", "AR"];
    public static string[] StatusFilters { get; } = ["Tất cả", "Open", "Paid", "PartiallyPaid", "Overdue", "Cancelled", "Printed", "Emailed", "Closed"];
    public static string[] CurrencyFilters { get; } = ["Tất cả", "VND", "USD", "EUR"];
    public static string[] MarketFilters { get; } = ["Tất cả", "Nội địa", "Quốc tế", "Đông Nam Á", "Châu Âu", "Châu Mỹ"];
    public static string[] CashierFilters { get; } = ["Tất cả", "Nguyễn Văn Thu Ngân", "Trần Thị Cashier", "Lê Văn Teller", "Phạm Thị Payment"];
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

            if (_invoiceService is null)
            {
                ErrorMessage = "Chưa có E-Invoice query service; không hiển thị dữ liệu mẫu.";
                BuildDashboard();
                ApplyFilters();
                return;
            }

            var invoices = await _invoiceService.GetInvoicesAsync(take: 1_000, cancellationToken: ct);
            foreach (var invoice in invoices)
                Items.Add(MapPersistedInvoice(invoice));

            BuildDashboard();
            ApplyFilters();
            InfoMessage = $"Đã tải {Items.Count:N0} hóa đơn từ dữ liệu E-Invoice thật. Dòng hàng, thanh toán và Folio chỉ hiện khi backend có projection tương ứng.";
            return;

#if false // Legacy designer fixture retained for historical reference only.
#pragma warning disable CS0162
            await Task.Delay(100, ct);
            var rng = new Random(99);

            var types = new[] { "Standard", "Standard", "Standard", "VAT", "VAT", "Receipt", "Proforma", "Group", "Company", "Agency", "Cash", "AR" };
            var statuses = new[] { "Open", "Open", "Paid", "Paid", "Paid", "PartiallyPaid", "Overdue", "Cancelled", "Printed", "Emailed", "Closed" };
            var guests = new[] { "Nguyễn Văn An", "Trần Thị Bình", "John Smith", "Kim Min-Ji", "Tanaka Hiroshi", "Chen Wei", "Sophie Martin", "Hans Mueller", "Anna Rossi", "Park Ji-Sung", "David Chen", "Maria Garcia" };
            var companies = new[] { "Vingroup", "FPT", "Vietcombank", "Saigon Tourist", "Hanoi Travel", "Petrolimex", "VNPT", "Viettel", "Masan", "TH True Milk" };
            var groups = new[] { "Đoàn FPT 2025", "Đoàn Samsung Q1", "Đoàn Vietcombank", "Đoàn Chính phủ Nhật", "Đoàn MICE Singapore" };
            var markets = new[] { "Nội địa", "Quốc tế", "Đông Nam Á", "Châu Âu", "Châu Mỹ" };
            var sources = new[] { "Trực tiếp", "Booking.com", "Agoda", "Expedia", "Traveloka", "Website", "Phone", "Email", "Sales" };
            var salesPeople = new[] { "Nguyễn Văn Kinh Doanh", "Trần Thị Sales", "Lê Văn MKT", "Phạm Thị Business", "Hoàng Văn Corp" };
            var cashiers = new[] { "Nguyễn Văn Thu Ngân", "Trần Thị Cashier", "Lê Văn Teller", "Phạm Thị Payment" };
            var currencies = new[] { "VND", "VND", "VND", "USD", "EUR" };
            var departments = new[] { "Accommodation", "Restaurant", "Banquet", "Laundry", "Minibar", "Spa", "Telephone", "Other" };
            var deptLabels = new Dictionary<string, string> { ["Accommodation"] = "Phòng", ["Restaurant"] = "Nhà hàng", ["Banquet"] = "Tiệc", ["Laundry"] = "Giặt ủi", ["Minibar"] = "Minibar", ["Spa"] = "Spa", ["Telephone"] = "Điện thoại", ["Other"] = "Khác" };
            var descriptions = new Dictionary<string, string[]> {
                ["Accommodation"] = new[] { "Room Charge", "Extra Bed", "Late Check-out", "Early Check-in", "Upgrade Fee" },
                ["Restaurant"] = new[] { "Buffet sáng", "Ăn trưa", "Ăn tối", "Room Service", "Đồ uống" },
                ["Banquet"] = new[] { "Tiệc cưới", "Hội nghị", "Hội thảo", "Tiệc tất niên", "Gala dinner" },
                ["Laundry"] = new[] { "Giặt khô", "Giặt ướt", "Ủi", "Giặt Express", "Giặt thường" },
                ["Minibar"] = new[] { "Bia", "Nước ngọt", "Snack", "Rượu vang", "Nước suối" },
                ["Spa"] = new[] { "Massage Thái", "Massage đá nóng", "Facial", "Body Scrub", "Sauna" },
                ["Telephone"] = new[] { "Gọi nội địa", "Gọi quốc tế", "IDD", "Fax", "Internet" },
                ["Other"] = new[] { "Transfer", "Vận chuyển", "Vé tham quan", "Hướng dẫn viên", "Dịch vụ khác" }
            };

            for (int i = 0; i < 600; i++)
            {
                var type = types[rng.Next(types.Length)];
                var status = statuses[rng.Next(statuses.Length)];
                var currency = currencies[rng.Next(currencies.Length)];
                var invDate = DateTime.Now.AddDays(-rng.Next(0, 365));
                var dueDate = invDate.AddDays(rng.Next(15, 60));
                var exchangeRate = currency == "VND" ? 1 : rng.Next(23000, 26000);

                var subtotal = (decimal)rng.Next(500000, 50000000);
                var discount = subtotal * (decimal)(rng.Next(0, 15) / 100.0);
                var svcCharge = (subtotal - discount) * (decimal)(rng.Next(0, 5) / 100.0);
                var vatAmount = (subtotal - discount + svcCharge) * 0.08m;
                var grandTotal = subtotal - discount + svcCharge + vatAmount;
                var paid = status switch
                {
                    "Paid" => grandTotal,
                    "PartiallyPaid" => grandTotal * (decimal)(rng.Next(40, 85) / 100.0),
                    "Overdue" => grandTotal * (decimal)(rng.Next(0, 20) / 100.0),
                    _ => 0
                };
                var outstanding = grandTotal - paid;
                var hasVat = type == "VAT" || rng.Next(3) == 1;

                var item = new ArInvRecordDto
                {
                    InvoiceNo = $"INV{rng.Next(100000, 999999)}",
                    InvoiceType = type,
                    InvoiceDate = invDate, PostingDate = invDate, DueDate = dueDate,
                    ReservationNo = rng.Next(2) == 1 ? $"RES{rng.Next(10000, 99999)}" : null,
                    RoomNo = rng.Next(2) == 1 ? $"{rng.Next(1, 10)}{rng.Next(1, 16):D2}" : null,
                    GuestName = guests[rng.Next(guests.Length)],
                    CompanyName = rng.Next(3) == 1 ? companies[rng.Next(companies.Length)] : null,
                    GroupName = rng.Next(5) == 1 ? groups[rng.Next(groups.Length)] : null,
                    Market = markets[rng.Next(markets.Length)], Source = sources[rng.Next(sources.Length)],
                    SalesPerson = salesPeople[rng.Next(salesPeople.Length)],
                    Currency = currency, ExchangeRate = exchangeRate,
                    Cashier = cashiers[rng.Next(cashiers.Length)],
                    BuyerName = rng.Next(3) > 0 ? guests[rng.Next(guests.Length)] : companies[rng.Next(companies.Length)],
                    BuyerAddress = $"{rng.Next(1, 999)} Đường " + (rng.Next(3) switch { 0 => "Nguyễn Huệ", 1 => "Lê Lợi", _ => "Trần Hưng Đạo" }),
                    BuyerTaxCode = hasVat ? $"MST{1000000000 + rng.Next(0, 999999999):D10}" : null,
                    BuyerEmail = $"{guests[rng.Next(guests.Length)].Split(' ').LastOrDefault()?.ToLower()}@email.com",
                    BuyerPhone = $"0{rng.Next(100000000, 999999999)}",
                    VatNumber = hasVat ? $"VAT{rng.Next(100000, 999999)}" : null,
                    VatTemplate = hasVat ? "01GTKT0/001" : null,
                    VatSymbol = hasVat ? $"AA/19E{rng.Next(100000, 999999)}" : null,
                    VatStatus = hasVat ? (rng.Next(3) switch { 0 => "Issued", 1 => "Pending", _ => "Cancelled" }) : null,
                    Subtotal = subtotal, Discount = discount, ServiceCharge = svcCharge,
                    VatAmount = hasVat ? vatAmount : 0, GrandTotal = grandTotal,
                    Paid = paid, Outstanding = outstanding,
                    Status = status, IsPrinted = rng.Next(3) > 0, IsEmailed = rng.Next(3) == 1,
                    HasVat = hasVat, PrintCount = rng.Next(0, 3),
                };

                // Line items
                var deptCount = rng.Next(1, 5);
                foreach (var dept in departments.OrderBy(_ => rng.Next()).Take(deptCount))
                {
                    var lineCount = rng.Next(1, 4);
                    for (int l = 0; l < lineCount; l++)
                    {
                        var descs = descriptions[dept];
                        var price = (decimal)rng.Next(50000, 5000000);
                        var qty = rng.Next(1, 5);
                        var lineDiscount = price * qty * (decimal)(rng.Next(0, 10) / 100.0);
                        item.Lines.Add(new ArInvoiceLineDto
                        {
                            PostingDate = invDate.AddDays(-rng.Next(0, 3)),
                            Description = descs[rng.Next(descs.Length)] + (l > 0 ? $" #{l + 1}" : ""),
                            Department = dept, RoomNo = item.RoomNo,
                            Qty = qty, Unit = dept == "Accommodation" ? "Đêm" : dept == "Restaurant" ? "Phần" : "Lần",
                            Price = price, Discount = lineDiscount,
                            ServiceCharge = (price * qty - lineDiscount) * 0.05m,
                            VatAmount = (price * qty - lineDiscount) * 0.08m,
                            Amount = price * qty - lineDiscount,
                            FolioNo = $"FOL{rng.Next(1000, 9999)}",
                            TransferFrom = rng.Next(4) == 1 ? $"FOL{rng.Next(1000, 9999)}" : null,
                            Remark = rng.Next(4) == 1 ? "Theo yêu cầu" : null
                        });
                    }
                }

                // Payments
                if (paid > 0)
                {
                    var pmtCount = rng.Next(1, 4);
                    var remaining = paid;
                    for (int p = 0; p < pmtCount && remaining > 0; p++)
                    {
                        var amt = p == pmtCount - 1 ? remaining : (decimal)rng.Next(100000, (int)Math.Max(100001, remaining));
                        item.Payments.Add(new ArInvoicePaymentDto
                        {
                            ReceiptNo = $"PT{rng.Next(10000, 99999)}",
                            PaymentDate = invDate.AddDays(rng.Next(0, 30)),
                            Amount = Math.Min(amt, remaining),
                            Method = PaymentMethods[rng.Next(PaymentMethods.Length)],
                            Reference = $"REF{rng.Next(10000, 99999)}",
                            Collector = cashiers[rng.Next(cashiers.Length)],
                            Type = rng.Next(4) switch { 0 => "Payment", 1 => "Payment", 2 => "Deposit", _ => "Transfer" }
                        });
                        remaining -= amt;
                    }
                }

                // Folios
                var folioCnt = rng.Next(1, 4);
                for (int f = 0; f < folioCnt; f++)
                    item.Folios.Add(new ArInvoiceFolioDto
                    {
                        FolioNo = $"FOL{rng.Next(1000, 9999)}",
                        Type = rng.Next(3) switch { 0 => "Master", 1 => "Sub", _ => "Sub" },
                        Balance = rng.Next(0, 20000000),
                        Status = rng.Next(3) switch { 0 => "Open", 1 => "Closed", _ => "Transferred" }
                    });

                // Emails
                if (item.IsEmailed)
                    item.Emails.Add(new ArInvoiceEmailDto
                    {
                        SentDate = invDate.AddDays(rng.Next(0, 5)),
                        Recipient = item.BuyerEmail ?? "guest@email.com",
                        Subject = $"Invoice {item.InvoiceNo}",
                        Status = "Sent",
                        Attachment = $"invoice_{item.InvoiceNo}.pdf"
                    });

                // Prints
                if (item.IsPrinted)
                    item.Prints.Add(new ArInvoicePrintDto
                    {
                        PrintedDate = invDate.AddDays(rng.Next(0, 3)),
                        PrintedBy = item.Cashier ?? "System",
                        Printer = "Printer-Lobby",
                        ReprintCount = item.PrintCount
                    });

                // Audits
                item.Audits.Add(new ArInvoiceAuditDto { Timestamp = invDate, Action = "Create", Actor = item.Cashier ?? "System", Detail = $"Tạo hóa đơn {item.InvoiceNo}" });
                if (item.IsPrinted) item.Audits.Add(new ArInvoiceAuditDto { Timestamp = invDate.AddDays(rng.Next(0, 3)), Action = "Print", Actor = item.Cashier ?? "System", Detail = $"In hóa đơn lần {item.PrintCount + 1}" });
                if (item.IsEmailed) item.Audits.Add(new ArInvoiceAuditDto { Timestamp = invDate.AddDays(rng.Next(0, 5)), Action = "Email", Actor = "System", Detail = $"Gửi email đến {item.BuyerEmail}" });
                if (paid > 0) item.Audits.Add(new ArInvoiceAuditDto { Timestamp = invDate.AddDays(rng.Next(0, 30)), Action = "Payment", Actor = item.Cashier ?? "System", Detail = $"Thanh toán {paid:N0} {currency}" });
                if (status == "Cancelled") item.Audits.Add(new ArInvoiceAuditDto { Timestamp = invDate.AddDays(rng.Next(1, 10)), Action = "Cancel", Actor = "Manager", Detail = "Hủy hóa đơn" });

                Items.Add(item);
            }

            BuildDashboard(); ApplyFilters();
#pragma warning restore CS0162
#endif
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static ArInvRecordDto MapPersistedInvoice(InvoiceSummaryDto invoice)
    {
        var isCancelled = invoice.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                          || invoice.Status.Equals("Canceled", StringComparison.OrdinalIgnoreCase);
        var status = isCancelled ? "Cancelled" : "Open";
        var total = invoice.TotalAmount;

        return new ArInvRecordDto
        {
            InvoiceId = invoice.Id,
            ReceivableCustomerId = invoice.ReceivableCustomerId,
            InvoiceNo = invoice.InvoiceNo,
            InvoiceType = string.IsNullOrWhiteSpace(invoice.BuyerTaxCode) ? "Standard" : "VAT",
            InvoiceDate = invoice.InvoiceDate,
            PostingDate = invoice.InvoiceDate,
            DueDate = invoice.InvoiceDate,
            BuyerName = invoice.BuyerName,
            CompanyName = invoice.BuyerName,
            BuyerAddress = invoice.BuyerAddress,
            BuyerTaxCode = invoice.BuyerTaxCode,
            Currency = invoice.CurrencyCode,
            ExchangeRate = invoice.ExchangeRate,
            Subtotal = invoice.SubtotalAmount,
            VatAmount = invoice.TaxAmount,
            GrandTotal = total,
            NetAmount = total,
            Outstanding = isCancelled ? 0 : total,
            Paid = 0,
            Status = status,
            HasVat = invoice.TaxAmount > 0 || !string.IsNullOrWhiteSpace(invoice.BuyerTaxCode),
            VatStatus = invoice.Status,
            Audits =
            {
                new ArInvoiceAuditDto
                {
                    Timestamp = invoice.IssuedAt ?? invoice.InvoiceDate,
                    Action = "Create",
                    Actor = invoice.ProviderCode,
                    Detail = $"Trạng thái E-Invoice: {invoice.Status}"
                }
            }
        };
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new ArInvoiceKpi { Label = "Tổng HĐ", Value = TotalItems.ToString(), Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new ArInvoiceKpi { Label = "Hôm nay", Value = TodayCount.ToString(), Color = "#0891B2", BgColor = "#ECFEFF" });
        DashboardKpis.Add(new ArInvoiceKpi { Label = "Tháng này", Value = MonthCount.ToString(), Color = "#2563EB", BgColor = "#EFF6FF" });
        DashboardKpis.Add(new ArInvoiceKpi { Label = "Đã thanh toán", Value = PaidCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new ArInvoiceKpi { Label = "Quá hạn", Value = OverdueCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2" });
        DashboardKpis.Add(new ArInvoiceKpi { Label = "Đã hủy", Value = CancelledCount.ToString(), Color = "#64748B", BgColor = "#F1F5F9" });
        DashboardKpis.Add(new ArInvoiceKpi { Label = "Doanh thu", Value = $"{TotalRevenue:N0} ₫", Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new ArInvoiceKpi { Label = "Còn nợ", Value = $"{TotalOutstanding:N0} ₫", Color = "#DC2626", BgColor = "#FEF2F2" });
        DashboardKpis.Add(new ArInvoiceKpi { Label = "Tổng VAT", Value = $"{TotalVat:N0} ₫", Color = "#7C3AED", BgColor = "#F3E8FF" });
    }

    [RelayCommand] private void ApplyFilters()
    {
        var q = Items.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s))
            q = q.Where(r => r.InvoiceNo.ToLower().Contains(s) || (r.GuestName?.ToLower().Contains(s) ?? false) || (r.CompanyName?.ToLower().Contains(s) ?? false) || (r.ReservationNo?.ToLower().Contains(s) ?? false) || (r.BuyerTaxCode?.Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterType) && FilterType != "Tất cả") q = q.Where(r => r.InvoiceType == FilterType);
        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.Status == FilterStatus);
        if (!string.IsNullOrEmpty(FilterCurrency) && FilterCurrency != "Tất cả") q = q.Where(r => r.Currency == FilterCurrency);
        if (!string.IsNullOrEmpty(FilterMarket) && FilterMarket != "Tất cả") q = q.Where(r => r.Market == FilterMarket);
        if (!string.IsNullOrEmpty(FilterCashier) && FilterCashier != "Tất cả") q = q.Where(r => r.Cashier == FilterCashier);
        if (FilterOverdue) q = q.Where(r => r.Status == "Overdue");
        if (FilterHasVat) q = q.Where(r => r.HasVat);

        var list = q.OrderByDescending(r => r.InvoiceDate).ThenBy(r => r.InvoiceNo).ToList();
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredItems.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredItems.Add(r);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void ClearFilters()
    {
        SearchText = string.Empty; FilterType = null; FilterStatus = null; FilterCurrency = null;
        FilterMarket = null; FilterCashier = null; FilterOverdue = false; FilterHasVat = false;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterCurrencyChanged(string? value) => ApplyFilters();
    partial void OnFilterMarketChanged(string? value) => ApplyFilters();
    partial void OnFilterCashierChanged(string? value) => ApplyFilters();
    partial void OnFilterOverdueChanged(bool value) => ApplyFilters();
    partial void OnFilterHasVatChanged(bool value) => ApplyFilters();

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
            PageButtons.Add(p is null ? new ArInvoicePaginationButton("…", null, false, true)
                : new ArInvoicePaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == current));
    }

    [RelayCommand] private void SelectItem(ArInvRecordDto? item) => SelectedItem = item;
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedItem = null; }
    [RelayCommand] private void ToggleRightPanel() => IsRightPanelOpen = !IsRightPanelOpen;

    partial void OnSelectedItemChanged(ArInvRecordDto? value)
    { OnPropertyChanged(nameof(HasSelection)); if (value is not null) { IsDetailOpen = true; SelectedDetailTab = 0; } }
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsOverviewTab)); OnPropertyChanged(nameof(IsItemsTab));
        OnPropertyChanged(nameof(IsPaymentTab)); OnPropertyChanged(nameof(IsFolioTab));
        OnPropertyChanged(nameof(IsVatTab)); OnPropertyChanged(nameof(IsEmailTab));
        OnPropertyChanged(nameof(IsPrintTab)); OnPropertyChanged(nameof(IsAuditTab));
    }
    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsItemsTab => SelectedDetailTab == 1;
    public bool IsPaymentTab => SelectedDetailTab == 2;
    public bool IsFolioTab => SelectedDetailTab == 3;
    public bool IsVatTab => SelectedDetailTab == 4;
    public bool IsEmailTab => SelectedDetailTab == 5;
    public bool IsPrintTab => SelectedDetailTab == 6;
    public bool IsAuditTab => SelectedDetailTab == 7;

    partial void OnFormModeChanged(string value) { OnPropertyChanged(nameof(IsPaymentForm)); OnPropertyChanged(nameof(IsEmailForm)); }
    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(IsFormClosed));
    partial void OnViewModeChanged(string value) { OnPropertyChanged(nameof(IsGridView)); OnPropertyChanged(nameof(IsCardView)); }

    [RelayCommand] private void SetGridView() { ViewMode = "Grid"; }
    [RelayCommand] private void SetCardView() { ViewMode = "Card"; }
    [RelayCommand] private void CloseForm() { IsFormOpen = false; FormErrorMessage = string.Empty; }

    [RelayCommand] private void OpenPaymentForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Payment"; FormTitle = $"Thu tiền — {SelectedItem.InvoiceNo}"; FormErrorMessage = string.Empty;
        PaymentAmount = SelectedItem.Outstanding > 0 ? Math.Min(SelectedItem.Outstanding, 10000000) : 0;
        PaymentMethod = "Tiền mặt"; PaymentReference = "";
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitPaymentForm()
    {
        if (SelectedItem is null)
        {
            FormErrorMessage = "Hãy chọn một hóa đơn trước khi thu tiền.";
            return;
        }
        if (string.IsNullOrWhiteSpace(SelectedItem.ReceivableCustomerId))
        {
            FormErrorMessage = "Hóa đơn chưa có liên kết công nợ với Customer; không thể ghi nhận thanh toán tự động.";
            return;
        }
        if (_receivableService is null)
        {
            FormErrorMessage = "Chưa đăng ký dịch vụ công nợ thật.";
            return;
        }
        if (PaymentAmount <= 0 || PaymentAmount > SelectedItem.Outstanding)
        {
            FormErrorMessage = $"Số tiền phải lớn hơn 0 và không vượt quá số còn nợ {SelectedItem.Outstanding:N0} {SelectedItem.Currency}.";
            return;
        }

        try
        {
            var receiptNo = await _receivableService.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
            {
                CustomerId = SelectedItem.ReceivableCustomerId,
                ReceiptDate = DateTime.Today,
                Amount = PaymentAmount,
                PaymentMethod = PaymentMethod,
                Note = $"Thu hóa đơn {SelectedItem.InvoiceNo}{(string.IsNullOrWhiteSpace(PaymentReference) ? string.Empty : $" - {PaymentReference.Trim()}")}"
            }, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(receiptNo))
            {
                FormErrorMessage = "Không tạo được phiếu thu. Kiểm tra mã Customer và quyền ghi dữ liệu.";
                return;
            }

            IsFormOpen = false;
            InfoMessage = $"Đã tạo phiếu thu thật {receiptNo} cho hóa đơn {SelectedItem.InvoiceNo}.";
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Không thể ghi nhận thanh toán: {ex.Message}";
        }
    }

    [RelayCommand] private void OpenEmailForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Email"; FormTitle = $"Gửi Email — {SelectedItem.InvoiceNo}"; FormErrorMessage = string.Empty;
        EmailTo = SelectedItem.BuyerEmail ?? ""; EmailSubject = $"Invoice {SelectedItem.InvoiceNo}";
        EmailBody = $"Kính gửi Quý khách,\n\nVui lòng xem hóa đơn {SelectedItem.InvoiceNo} đính kèm.\nTổng: {SelectedItem.GrandTotal:N0} {SelectedItem.Currency}\n\nTrân trọng,\nASMILE BO";
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
                Subject = EmailSubject,
                Body = EmailBody,
                SourceType = "ArInvoice",
                SourceId = null
            }, CancellationToken.None);
            IsFormOpen = false;
            InfoMessage = $"Đã lưu email hóa đơn vào hàng đợi ({queued.Id:N}); gửi khi SMTP được bật.";
        }
        catch (Exception ex) { FormErrorMessage = $"Không thể lưu email: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task CancelInvoice()
    {
        if (SelectedItem is null)
        {
            InfoMessage = "Hãy chọn một hóa đơn thật trước khi hủy.";
            return;
        }

        if (SelectedItem.HasVat && SelectedItem.VatStatus == "Issued")
        {
            InfoMessage = "Không thể hủy hóa đơn đã xuất VAT; cần lập nghiệp vụ điều chỉnh/hủy với nhà cung cấp.";
            return;
        }

        if (_invoiceService is null || SelectedItem.InvoiceId == Guid.Empty)
        {
            InfoMessage = "Hóa đơn đang chọn chưa có ID backend hoặc E-Invoice service; không cập nhật trạng thái giả.";
            return;
        }

        var reason = PromptForCancelReason();
        if (string.IsNullOrWhiteSpace(reason))
            return;

        if (System.Windows.MessageBox.Show(
                $"Hủy hóa đơn nháp {SelectedItem.InvoiceNo}?\n\nLý do: {reason}",
                "Xác nhận hủy hóa đơn",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var result = await _invoiceService.CancelAsync(SelectedItem.InvoiceId, reason, CancellationToken.None);
            InfoMessage = result.Message;
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể hủy hóa đơn thật: {ex.Message}";
        }
    }

    private static string? PromptForCancelReason()
    {
        var dialog = new System.Windows.Window
        {
            Title = "Lý do hủy hóa đơn",
            Width = 460,
            Height = 205,
            Owner = System.Windows.Application.Current?.MainWindow,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            ResizeMode = System.Windows.ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        var input = new System.Windows.Controls.TextBox
        {
            Margin = new System.Windows.Thickness(0, 6, 0, 10),
            MinHeight = 58,
            AcceptsReturn = true,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
        };
        var ok = new System.Windows.Controls.Button
        {
            Content = "Xác nhận",
            Padding = new System.Windows.Thickness(12, 5, 12, 5),
            IsDefault = true,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                System.Windows.MessageBox.Show("Vui lòng nhập lý do hủy hóa đơn.", "Hóa đơn", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                input.Focus();
                return;
            }

            dialog.DialogResult = true;
        };
        dialog.Content = new System.Windows.Controls.Border
        {
            Padding = new System.Windows.Thickness(16),
            Child = new System.Windows.Controls.StackPanel
            {
                Children =
                {
                    new System.Windows.Controls.TextBlock { Text = "Lý do nghiệp vụ *", FontWeight = System.Windows.FontWeights.SemiBold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkBlue) },
                    input,
                    ok
                }
            }
        };
        return dialog.ShowDialog() == true ? input.Text.Trim() : null;
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (Items.Count == 0)
        {
            InfoMessage = "Không có hóa đơn thật để xuất.";
            return;
        }

        var rows = Items.Select(item => (IReadOnlyList<string>)new[]
        {
            item.InvoiceNo,
            item.InvoiceDate.ToString("dd/MM/yyyy"),
            item.BuyerName ?? item.GuestName ?? string.Empty,
            item.Currency,
            CsvExportHelper.F(item.GrandTotal),
            CsvExportHelper.F(item.Paid),
            CsvExportHelper.F(item.Outstanding),
            item.StatusLabel
        }).ToList();

        var path = CsvExportHelper.Export(
            "invoice_report",
            new[] { "Invoice No", "Ngày", "Người mua", "Tiền tệ", "Tổng tiền", "Đã thu", "Còn nợ", "Trạng thái" },
            rows);
        InfoMessage = $"Đã xuất dữ liệu thật dạng CSV (mở bằng Excel): {path}";
    }

    [RelayCommand]
    private void ExportPdf()
    {
        if (Items.Count == 0) { InfoMessage = "Không có hóa đơn thật để xuất PDF."; return; }
        var path = PdfExportHelper.Export(
            "Danh sách hóa đơn",
            "ar_invoice_report",
            ["Invoice No", "Ngày", "Người mua", "Tiền tệ", "Tổng tiền", "Đã thu", "Còn nợ", "Trạng thái"],
            Items.Select(item => (IReadOnlyList<string>)[
                item.InvoiceNo,
                item.InvoiceDate.ToString("dd/MM/yyyy"),
                item.BuyerName ?? item.GuestName ?? string.Empty,
                item.Currency,
                CsvExportHelper.F(item.GrandTotal),
                CsvExportHelper.F(item.Paid),
                CsvExportHelper.F(item.Outstanding),
                item.StatusLabel]).ToList());
        if (!string.IsNullOrWhiteSpace(path)) InfoMessage = $"Đã xuất PDF danh sách hóa đơn thật: {path}";
    }

    [RelayCommand]
    private async Task PrintInvoice()
    {
        if (SelectedItem is null)
        {
            InfoMessage = "Hãy chọn một hóa đơn thật để xem trước.";
            return;
        }

        if (_invoiceService is null || SelectedItem.InvoiceId == Guid.Empty)
        {
            InfoMessage = "Hóa đơn đang chọn chưa có ID backend hoặc E-Invoice service; không in dữ liệu giả.";
            return;
        }

        try
        {
            var previewBytes = await _invoiceService.GeneratePdfPreviewAsync(SelectedItem.InvoiceId);
            var preview = new PrintPreviewDialog(
                $"Xem trước hóa đơn {SelectedItem.InvoiceNo}",
                Encoding.UTF8.GetString(previewBytes))
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            preview.ShowDialog();
            InfoMessage = $"Đã mở xem trước hóa đơn {SelectedItem.InvoiceNo} từ dữ liệu thật. Chưa đánh dấu đã in vì chưa có command ghi nhận máy in.";
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể xem trước hóa đơn thật: {ex.Message}";
        }
    }
    [RelayCommand] private void RefreshCommand() { _ = LoadAsync(CancellationToken.None); }
}
