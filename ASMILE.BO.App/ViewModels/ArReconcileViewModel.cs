using System.Text;
using System.Collections.ObjectModel;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class ArReconcileTxnDto
{
    public string TxnId { get; set; } = "";
    public string? ReservationNo { get; set; }
    public string? RoomNo { get; set; }
    public string? GuestName { get; set; }
    public string? CompanyName { get; set; }
    public string? InvoiceNo { get; set; }
    public string? ReceiptNo { get; set; }
    public string? FolioNo { get; set; }
    public string? Department { get; set; }
    public string Description { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Payment { get; set; }
    public decimal Outstanding { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime PostingDate { get; set; }
    public string Type { get; set; } = "Invoice";
    public string TypeLabel => Type switch
    {
        "Invoice" => "Hóa đơn", "Payment" => "Thanh toán", "Deposit" => "Đặt cọc",
        "Refund" => "Hoàn tiền", "Transfer" => "Chuyển khoản", "Adjustment" => "Điều chỉnh",
        "Credit" => "Ghi có", "Debit" => "Ghi nợ", _ => Type
    };
    public string TypeColor => Type switch
    {
        "Invoice" => "#2563EB", "Payment" => "#16A34A", "Deposit" => "#D97706",
        "Refund" => "#DC2626", "Transfer" => "#0891B2", "Adjustment" => "#7C3AED",
        "Credit" => "#F59E0B", "Debit" => "#E32F70", _ => "#64748B"
    };

    public string MatchStatus { get; set; } = "Unmatched";
    public string MatchLabel => MatchStatus switch
    {
        "Matched" => "Đã khớp", "Partial" => "Khớp 1 phần", "Unmatched" => "Chưa khớp",
        "Duplicate" => "Trùng", "Overpaid" => "Trả thừa", "Underpaid" => "Trả thiếu",
        "Cancelled" => "Đã hủy", _ => MatchStatus
    };
    public string MatchColor => MatchStatus switch
    {
        "Matched" => "#16A34A", "Partial" => "#F59E0B", "Unmatched" => "#DC2626",
        "Duplicate" => "#7C3AED", "Overpaid" => "#D97706", "Underpaid" => "#E32F70",
        "Cancelled" => "#64748B", _ => "#64748B"
    };
    public string MatchBg => MatchStatus switch
    {
        "Matched" => "#DCFCE7", "Partial" => "#FFFBEB", "Unmatched" => "#FEE2E2",
        "Duplicate" => "#F3E8FF", "Overpaid" => "#FFF7ED", "Underpaid" => "#FFF0F5",
        "Cancelled" => "#F1F5F9", _ => "#F8FAFC"
    };

    public DateTime? MatchedDate { get; set; }
    public string? MatchedBy { get; set; }
    public string? Reference { get; set; }
    public string? MatchReason { get; set; }

    public List<ArReconcileMatchDto> Matches { get; set; } = new();
    public List<ArReconcileAuditDto> Audits { get; set; } = new();
}

public sealed class ArReconcileMatchDto
{
    public string MatchId { get; set; } = "";
    public string SourceTxnId { get; set; } = "";
    public string TargetTxnId { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime MatchedDate { get; set; }
    public string? MatchedBy { get; set; }
    public string? Reason { get; set; }
}

public sealed class ArReconcileStatementDto
{
    public DateTime StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal Adjustment { get; set; }
    public decimal ClosingBalance { get; set; }
}

public sealed class ArReconcileAuditDto
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string ActionLabel => Action switch
    {
        "Match" => "Ghép", "Unmatch" => "Hủy ghép", "AutoMatch" => "Ghép tự động",
        "ManualMatch" => "Ghép thủ công", "Confirm" => "Xác nhận", "Unconfirm" => "Hủy XN",
        "Export" => "Xuất", "Import" => "Nhập", _ => Action
    };
    public string Actor { get; set; } = "";
    public string? Detail { get; set; }
}

public sealed class ArReconcileDiffDto
{
    public string Type { get; set; } = "";
    public string TypeLabel => Type switch
    {
        "MissingInvoice" => "Thiếu hóa đơn", "MissingPayment" => "Thiếu thanh toán",
        "DuplicatePayment" => "Trùng thanh toán", "WrongAmount" => "Sai số tiền",
        "WrongCurrency" => "Sai tiền tệ", "WrongGuest" => "Sai khách",
        "WrongCompany" => "Sai DN", "WrongFolio" => "Sai Folio",
        "WrongReservation" => "Sai Reservation", "TransferPending" => "Transfer chưa HT", _ => Type
    };
    public string Detail { get; set; } = "";
    public decimal Amount { get; set; }
    public string Severity { get; set; } = "Low";
    public string SeverityColor => Severity switch { "High" => "#DC2626", "Medium" => "#D97706", "Low" => "#F59E0B", _ => "#64748B" };
}

public sealed class ArReconcileKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
}

public sealed class ArReconcilePaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }
    public ArReconcilePaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    { Label = label; PageNumber = pageNumber; IsCurrent = isCurrent; IsEllipsis = isEllipsis; }
}

// ═══ VIEW MODEL ═══

public sealed partial class ArReconcileViewModel : ViewModelBase
{
    private readonly IReceivableService? _receivableService;
    private readonly IDebtTransactionService? _debtTransactionService;

    public ArReconcileViewModel(
        IReceivableService? receivableService = null,
        IDebtTransactionService? debtTransactionService = null)
    {
        _receivableService = receivableService;
        _debtTransactionService = debtTransactionService;
    }

    public ObservableCollection<ArReconcileTxnDto> Items { get; } = new();
    public ObservableCollection<ArReconcileTxnDto> FilteredItems { get; } = new();
    public ObservableCollection<ArReconcileKpi> DashboardKpis { get; } = new();
    public ObservableCollection<ArReconcileDiffDto> Differences { get; } = new();
    public ObservableCollection<ArReconcileDiffDto> SelectedDifferences { get; } = new();
    public ObservableCollection<ArReconcilePaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private ArReconcileTxnDto? _selectedItem;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterType;
    [ObservableProperty] private string? _filterMatchStatus;
    [ObservableProperty] private string? _filterCurrency;
    [ObservableProperty] private string? _filterDepartment;
    [ObservableProperty] private string? _filterGuest;
    [ObservableProperty] private DateTime? _filterFromDate;
    [ObservableProperty] private DateTime? _filterToDate;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    [ObservableProperty] private bool _isRightPanelOpen = true;

    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formMode = string.Empty;
    [ObservableProperty] private string _formTitle = string.Empty;
    [ObservableProperty] private string _formErrorMessage = string.Empty;

    [ObservableProperty] private string _matchSourceTxnId = "";
    [ObservableProperty] private string _matchTargetTxnId = "";
    [ObservableProperty] private decimal _matchAmount;
    [ObservableProperty] private string _matchReason = "";

    public bool IsMatchForm => FormMode == "Match";
    public bool IsFormClosed => !IsFormOpen;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0 giao dịch" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} giao dịch";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedItem is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool HasSelectedDifferences => SelectedDifferences.Count > 0;
    public bool ShowMatchedState => SelectedItem?.MatchStatus == "Matched" && !HasSelectedDifferences;

    // Totals shown in the left "Kết quả nhanh" card must follow the active
    // filters, while the KPI strip remains a dashboard for the full dataset.
    private decimal _quickTotalDebit;
    private decimal _quickTotalCredit;
    private decimal _quickTotalPayment;
    private decimal _quickTotalOutstanding;
    private decimal _quickTotalDifference;
    public decimal QuickTotalDebit => _quickTotalDebit;
    public decimal QuickTotalCredit => _quickTotalCredit;
    public decimal QuickTotalPayment => _quickTotalPayment;
    public decimal QuickTotalOutstanding => _quickTotalOutstanding;
    public decimal QuickTotalDifference => _quickTotalDifference;

    public int TotalItems => Items.Count;
    public int MatchedCount => Items.Count(i => i.MatchStatus == "Matched");
    public int PartialCount => Items.Count(i => i.MatchStatus == "Partial");
    public int UnmatchedCount => Items.Count(i => i.MatchStatus == "Unmatched");
    public int DuplicateCount => Items.Count(i => i.MatchStatus == "Duplicate");
    // The quick-result card follows the active filter. Keep full-dataset
    // totals separate for the KPI strip so filtering never changes its cards.
    public decimal TotalDebit => _quickTotalDebit;
    public decimal TotalCredit => _quickTotalCredit;
    public decimal TotalPayment => _quickTotalPayment;
    public decimal TotalOutstanding => _quickTotalOutstanding;
    public decimal TotalDifference => _quickTotalDifference;
    private decimal FullTotalDebit => Items.Sum(i => i.Debit);
    private decimal FullTotalCredit => Items.Sum(i => i.Credit);
    private decimal FullTotalPayment => Items.Sum(i => i.Payment);
    private decimal FullTotalOutstanding => Items.Sum(i => i.Outstanding);
    private decimal FullTotalDifference => FullTotalDebit - FullTotalCredit - FullTotalPayment;

    public double MatchPercent => TotalItems > 0 ? (double)MatchedCount / TotalItems * 100 : 0;
    public double UnmatchPercent => TotalItems > 0 ? (double)UnmatchedCount / TotalItems * 100 : 0;

    public static string[] TypeFilters { get; } = ["Tất cả", "Invoice", "Payment", "Deposit", "Refund", "Transfer", "Adjustment", "Credit", "Debit"];
    public static string[] MatchStatusFilters { get; } = ["Tất cả", "Matched", "Partial", "Unmatched", "Duplicate", "Overpaid", "Underpaid", "Cancelled"];
    public static string[] CurrencyFilters { get; } = ["Tất cả", "VND", "USD", "EUR"];
    public static string[] DepartmentFilters { get; } = ["Tất cả", "Accommodation", "Restaurant", "Banquet", "Spa", "Minibar", "Laundry", "Telephone", "Other"];
    // Guest filter values must come from the persisted transactions.  Keeping
    // sample names here made the filter look populated even when the database
    // returned no real rows.
    public ObservableCollection<string> GuestFilters { get; } = new() { "Tất cả" };
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Items.Clear(); Differences.Clear();

            if (_receivableService is null)
            {
                ErrorMessage = "Chưa có Receivable query service; không hiển thị dữ liệu mẫu.";
                BuildDashboard();
                ApplyFilters();
                return;
            }

            var transactions = await _receivableService.GetTransactionsAsync(
                new ReceivableFilterDto { FromDate = FilterFromDate, ToDate = FilterToDate }, ct);
            foreach (var transaction in transactions)
                Items.Add(MapPersistedTransaction(transaction));

            RefreshGuestFilters();
            BuildDashboard();
            ApplyFilters();
            InfoMessage = $"Đã tải {Items.Count:N0} giao dịch công nợ từ dữ liệu thật. Đối chiếu chỉ đánh dấu khi backend có allocation/match thực tế.";
            return;

#if false // Legacy designer fixture retained for historical reference only.
#pragma warning disable CS0162
            await Task.Delay(100, ct);
            var rng = new Random(123);

            var guests = new[] { "Nguyễn Văn An", "Trần Thị Bình", "John Smith", "Kim Min-Ji", "Tanaka Hiroshi", "Chen Wei", "Sophie Martin", "Hans Mueller", "Anna Rossi", "Park Ji-Sung", "David Chen", "Maria Garcia" };
            var companies = new[] { "Vingroup", "FPT", "Vietcombank", "Saigon Tourist", "Hanoi Travel", "Petrolimex", "VNPT", "Viettel", "Masan", "TH True Milk" };
            var departments = new[] { "Accommodation", "Restaurant", "Banquet", "Spa", "Minibar", "Laundry", "Telephone", "Other" };
            var deptLabels = new Dictionary<string, string> { ["Accommodation"] = "Phòng", ["Restaurant"] = "Nhà hàng", ["Banquet"] = "Tiệc", ["Spa"] = "Spa", ["Minibar"] = "Minibar", ["Laundry"] = "Giặt ủi", ["Telephone"] = "Điện thoại", ["Other"] = "Khác" };
            var types = new[] { "Invoice", "Invoice", "Invoice", "Payment", "Payment", "Deposit", "Refund", "Transfer", "Adjustment", "Credit", "Debit" };
            var matchStatuses = new[] { "Matched", "Matched", "Matched", "Partial", "Partial", "Unmatched", "Unmatched", "Unmatched", "Duplicate", "Overpaid", "Underpaid", "Cancelled" };
            var currencies = new[] { "VND", "VND", "VND", "USD", "EUR" };
            var operators = new[] { "Nguyễn Văn KT", "Trần Thị TN", "Lê Văn ĐC", "Phạm Thị AR", "Hệ thống" };

            for (int i = 0; i < 550; i++)
            {
                var type = types[rng.Next(types.Length)];
                var status = matchStatuses[rng.Next(matchStatuses.Length)];
                var currency = currencies[rng.Next(currencies.Length)];
                var date = DateTime.Now.AddDays(-rng.Next(0, 180));
                var dept = departments[rng.Next(departments.Length)];
                var debit = type is "Invoice" or "Debit" ? (decimal)rng.Next(500000, 50000000) : 0;
                var credit = type is "Payment" or "Refund" or "Credit" ? (decimal)rng.Next(500000, 50000000) : 0;
                var payment = type is "Payment" or "Deposit" ? (decimal)rng.Next(500000, 30000000) : 0;
                var outstanding = debit - credit - payment;
                if (outstanding < 0) outstanding = 0;

                var item = new ArReconcileTxnDto
                {
                    TxnId = $"TXN{rng.Next(100000, 999999)}",
                    ReservationNo = rng.Next(2) == 1 ? $"RES{rng.Next(10000, 99999)}" : null,
                    RoomNo = rng.Next(2) == 1 ? $"{rng.Next(1, 10)}{rng.Next(1, 16):D2}" : null,
                    GuestName = guests[rng.Next(guests.Length)],
                    CompanyName = rng.Next(3) == 1 ? companies[rng.Next(companies.Length)] : null,
                    InvoiceNo = type is "Invoice" or "Debit" ? $"INV{rng.Next(100000, 999999)}" : null,
                    ReceiptNo = type is "Payment" or "Deposit" ? $"PT{rng.Next(10000, 99999)}" : null,
                    FolioNo = rng.Next(2) == 1 ? $"FOL{rng.Next(1000, 9999)}" : null,
                    Department = dept,
                    Description = $"{deptLabels[dept]} — {type} #{i + 1}",
                    Debit = debit, Credit = credit, Payment = payment, Outstanding = outstanding,
                    Currency = currency, PostingDate = date,
                    Type = type, MatchStatus = status,
                    MatchedDate = status == "Matched" || status == "Partial" ? date.AddDays(rng.Next(0, 10)) : null,
                    MatchedBy = status == "Matched" || status == "Partial" ? operators[rng.Next(operators.Length)] : null,
                    Reference = rng.Next(2) == 1 ? $"REF{rng.Next(10000, 99999)}" : null,
                    MatchReason = status == "Partial" ? "Chênh lệch số tiền" : null,
                };

                if (status == "Matched" || status == "Partial")
                {
                    item.Matches.Add(new ArReconcileMatchDto
                    {
                        MatchId = $"MATCH{rng.Next(10000, 99999)}",
                        SourceTxnId = item.TxnId,
                        TargetTxnId = $"TXN{rng.Next(100000, 999999)}",
                        Amount = Math.Min(debit > 0 ? debit : credit, payment > 0 ? payment : credit),
                        MatchedDate = date.AddDays(rng.Next(0, 10)),
                        MatchedBy = operators[rng.Next(operators.Length)],
                        Reason = status == "Partial" ? "Khớp một phần" : "Khớp hoàn toàn"
                    });
                }

                item.Audits.Add(new ArReconcileAuditDto { Timestamp = date, Action = "AutoMatch", Actor = "Hệ thống", Detail = status == "Matched" ? "Ghép tự động thành công" : "Chưa khớp — chờ xử lý" });

                Items.Add(item);
            }

            // Generate differences
            Differences.Add(new ArReconcileDiffDto { Type = "MissingInvoice", Detail = "Hóa đơn INV234567 chưa có trong hệ thống", Amount = 15000000, Severity = "High" });
            Differences.Add(new ArReconcileDiffDto { Type = "MissingPayment", Detail = "Thanh toán PT12345 chưa được ghi nhận", Amount = 8500000, Severity = "High" });
            Differences.Add(new ArReconcileDiffDto { Type = "DuplicatePayment", Detail = "Thanh toán PT23456 bị trùng lặp", Amount = 5200000, Severity = "Medium" });
            Differences.Add(new ArReconcileDiffDto { Type = "WrongAmount", Detail = "Số tiền hóa đơn INV345678 chênh lệch 200,000 ₫", Amount = 200000, Severity = "Low" });
            Differences.Add(new ArReconcileDiffDto { Type = "WrongCurrency", Detail = "Hóa đơn INV456789 ghi USD nhưng thanh toán VND", Amount = 23000000, Severity = "High" });
            Differences.Add(new ArReconcileDiffDto { Type = "WrongGuest", Detail = "Payment PT34567 gán nhầm khách", Amount = 3100000, Severity = "Medium" });
            Differences.Add(new ArReconcileDiffDto { Type = "WrongCompany", Detail = "Invoice INV567890 gán sai doanh nghiệp", Amount = 12000000, Severity = "Medium" });
            Differences.Add(new ArReconcileDiffDto { Type = "TransferPending", Detail = "Transfer từ FOL1234 chưa hoàn thành", Amount = 4500000, Severity = "Low" });

            BuildDashboard(); ApplyFilters();
#pragma warning restore CS0162
#endif
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static ArReconcileTxnDto MapPersistedTransaction(ReceivableTransactionDto transaction)
    {
        var isPayment = transaction.DocumentType == ReceivableDocumentType.PaymentReceipt;
        var allocated = Math.Max(0, transaction.AllocatedAmount);
        var sourceAmount = isPayment ? Math.Max(0, transaction.CreditAmount) : Math.Max(0, transaction.DebitAmount);
        var matchStatus = allocated <= 0
            ? "Unmatched"
            : sourceAmount > 0 && allocated >= sourceAmount ? "Matched" : "Partial";
        return new ArReconcileTxnDto
        {
            TxnId = transaction.Id,
            InvoiceNo = isPayment ? null : transaction.DocumentNo,
            ReceiptNo = isPayment ? transaction.DocumentNo : null,
            CompanyName = transaction.CustomerName,
            Description = transaction.Description,
            Debit = transaction.DebitAmount,
            Credit = transaction.CreditAmount,
            Payment = isPayment ? transaction.CreditAmount : 0,
            Outstanding = transaction.BalanceAmount,
            Currency = transaction.Currency,
            PostingDate = transaction.TransactionDate,
            Type = isPayment ? "Payment" : transaction.DocumentType switch
            {
                ReceivableDocumentType.SalesInvoice => "Invoice",
                ReceivableDocumentType.ExportVoucher => "Debit",
                _ => "Adjustment"
            },
            MatchStatus = matchStatus,
            MatchedBy = allocated > 0 ? transaction.CreatedBy : null,
            MatchReason = allocated > 0
                ? $"Đã phân bổ {allocated:N0} trên dữ liệu thật."
                : "Chưa có allocation đối ứng trên dữ liệu thật."
        };
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new ArReconcileKpi { Label = "Tổng GD", Value = TotalItems.ToString(), Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new ArReconcileKpi { Label = "Đã khớp", Value = MatchedCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new ArReconcileKpi { Label = "Khớp 1 phần", Value = PartialCount.ToString(), Color = "#F59E0B", BgColor = "#FFFBEB" });
        DashboardKpis.Add(new ArReconcileKpi { Label = "Chưa khớp", Value = UnmatchedCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2" });
        DashboardKpis.Add(new ArReconcileKpi { Label = "Trùng", Value = DuplicateCount.ToString(), Color = "#7C3AED", BgColor = "#F3E8FF" });
        DashboardKpis.Add(new ArReconcileKpi { Label = "Tỷ lệ khớp", Value = $"{MatchPercent:F1}%", Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new ArReconcileKpi { Label = "Tổng Debit", Value = $"{FullTotalDebit:N0} ₫", Color = "#2563EB", BgColor = "#EFF6FF" });
        DashboardKpis.Add(new ArReconcileKpi { Label = "Tổng Credit", Value = $"{FullTotalCredit:N0} ₫", Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new ArReconcileKpi { Label = "Chênh lệch", Value = $"{FullTotalDifference:N0} ₫", Color = "#DC2626", BgColor = "#FEF2F2" });
    }

    private void RefreshGuestFilters()
    {
        var selected = FilterGuest;
        GuestFilters.Clear();
        GuestFilters.Add("Tất cả");

        foreach (var guest in Items
            .Select(item => item.GuestName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase))
        {
            GuestFilters.Add(guest!);
        }

        if (!string.IsNullOrWhiteSpace(selected)
            && !GuestFilters.Any(name => string.Equals(name, selected, StringComparison.CurrentCultureIgnoreCase)))
        {
            FilterGuest = null;
        }
    }

    [RelayCommand] private void ApplyFilters()
    {
        var q = Items.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s))
            q = q.Where(r => r.TxnId.ToLower().Contains(s) || (r.GuestName?.ToLower().Contains(s) ?? false) || (r.CompanyName?.ToLower().Contains(s) ?? false) || (r.InvoiceNo?.ToLower().Contains(s) ?? false) || (r.Reference?.ToLower().Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterType) && FilterType != "Tất cả") q = q.Where(r => r.Type == FilterType);
        if (!string.IsNullOrEmpty(FilterMatchStatus) && FilterMatchStatus != "Tất cả") q = q.Where(r => r.MatchStatus == FilterMatchStatus);
        if (!string.IsNullOrEmpty(FilterCurrency) && FilterCurrency != "Tất cả") q = q.Where(r => r.Currency == FilterCurrency);
        if (!string.IsNullOrEmpty(FilterDepartment) && FilterDepartment != "Tất cả") q = q.Where(r => r.Department == FilterDepartment);
        if (!string.IsNullOrEmpty(FilterGuest) && FilterGuest != "Tất cả") q = q.Where(r => r.GuestName == FilterGuest);
        // Date filters are date-based, so include the complete end date
        // instead of truncating it at midnight.
        if (FilterFromDate.HasValue)
        {
            var from = FilterFromDate.Value.Date;
            q = q.Where(r => r.PostingDate >= from);
        }
        if (FilterToDate.HasValue)
        {
            var untilExclusive = FilterToDate.Value.Date.AddDays(1);
            q = q.Where(r => r.PostingDate < untilExclusive);
        }

        var list = q.OrderByDescending(r => r.PostingDate).ThenBy(r => r.TxnId).ToList();
        _quickTotalDebit = list.Sum(r => r.Debit);
        _quickTotalCredit = list.Sum(r => r.Credit);
        _quickTotalPayment = list.Sum(r => r.Payment);
        _quickTotalOutstanding = list.Sum(r => r.Outstanding);
        _quickTotalDifference = _quickTotalDebit - _quickTotalCredit - _quickTotalPayment;
        OnPropertyChanged(nameof(QuickTotalDebit));
        OnPropertyChanged(nameof(QuickTotalCredit));
        OnPropertyChanged(nameof(QuickTotalPayment));
        OnPropertyChanged(nameof(QuickTotalOutstanding));
        OnPropertyChanged(nameof(QuickTotalDifference));
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredItems.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredItems.Add(r);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void ClearFilters()
    {
        SearchText = string.Empty; FilterType = null; FilterMatchStatus = null; FilterCurrency = null;
        FilterDepartment = null; FilterGuest = null; FilterFromDate = null; FilterToDate = null;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterMatchStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterCurrencyChanged(string? value) => ApplyFilters();
    partial void OnFilterDepartmentChanged(string? value) => ApplyFilters();
    partial void OnFilterGuestChanged(string? value) => ApplyFilters();
    partial void OnFilterFromDateChanged(DateTime? value) => ApplyFilters();
    partial void OnFilterToDateChanged(DateTime? value) => ApplyFilters();

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
            PageButtons.Add(p is null ? new ArReconcilePaginationButton("…", null, false, true)
                : new ArReconcilePaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == current));
    }

    [RelayCommand] private void SelectItem(ArReconcileTxnDto? item) => SelectedItem = item;

    private void RefreshSelectedDifferences()
    {
        SelectedDifferences.Clear();
        if (SelectedItem is null)
        {
            OnPropertyChanged(nameof(HasSelectedDifferences));
            OnPropertyChanged(nameof(ShowMatchedState));
            return;
        }

        // A matched transaction has no discrepancy.  The previous UI showed
        // the static demo list for every selection, which made a green
        // "Đã khớp" row appear to have unresolved errors.
        switch (SelectedItem.MatchStatus)
        {
            case "Partial":
                SelectedDifferences.Add(new ArReconcileDiffDto
                {
                    Type = "WrongAmount", Detail = SelectedItem.MatchReason ?? "Giao dịch mới chỉ khớp một phần.",
                    Amount = SelectedItem.Outstanding, Severity = "Medium"
                });
                break;
            case "Duplicate":
                SelectedDifferences.Add(new ArReconcileDiffDto
                {
                    Type = "DuplicatePayment", Detail = "Giao dịch có dấu hiệu bị trùng.",
                    Amount = SelectedItem.Payment, Severity = "High"
                });
                break;
            case "Overpaid":
                SelectedDifferences.Add(new ArReconcileDiffDto
                {
                    Type = "WrongAmount", Detail = "Số tiền thanh toán lớn hơn số phải thu.",
                    Amount = SelectedItem.Payment - SelectedItem.Debit, Severity = "Low"
                });
                break;
            case "Underpaid":
                SelectedDifferences.Add(new ArReconcileDiffDto
                {
                    Type = "MissingPayment", Detail = "Giao dịch còn thiếu thanh toán.",
                    Amount = SelectedItem.Outstanding, Severity = "Medium"
                });
                break;
            case "Unmatched":
                SelectedDifferences.Add(new ArReconcileDiffDto
                {
                    Type = SelectedItem.InvoiceNo is null ? "MissingInvoice" : "MissingPayment",
                    Detail = "Chưa tìm thấy giao dịch đối ứng phù hợp.",
                    Amount = SelectedItem.Outstanding, Severity = "High"
                });
                break;
        }

        OnPropertyChanged(nameof(HasSelectedDifferences));
        OnPropertyChanged(nameof(ShowMatchedState));
    }
    // XAML literal CommandParameter values arrive as strings.  Accept both
    // string literals and numeric values so the window can be loaded without
    // CommunityToolkit throwing while it evaluates CanExecute.
    [RelayCommand] private void SelectDetailTab(object? tab)
    {
        if (tab is null) return;
        var value = tab is int number
            ? number
            : int.TryParse(tab.ToString(), out var parsed) ? parsed : -1;
        if (value >= 0) SelectedDetailTab = Math.Clamp(value, 0, 5);
    }
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedItem = null; }
    [RelayCommand] private void ToggleRightPanel() => IsRightPanelOpen = !IsRightPanelOpen;

    partial void OnSelectedItemChanged(ArReconcileTxnDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        RefreshSelectedDifferences();
        if (value is not null) IsDetailOpen = true;
    }
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsMatchingTab)); OnPropertyChanged(nameof(IsStatementTab));
        OnPropertyChanged(nameof(IsHistoryTab)); OnPropertyChanged(nameof(IsPaymentTab));
        OnPropertyChanged(nameof(IsInvoiceTab)); OnPropertyChanged(nameof(IsDiffTab));
    }
    public bool IsMatchingTab => SelectedDetailTab == 0;
    public bool IsStatementTab => SelectedDetailTab == 1;
    public bool IsHistoryTab => SelectedDetailTab == 2;
    public bool IsPaymentTab => SelectedDetailTab == 3;
    public bool IsInvoiceTab => SelectedDetailTab == 4;
    public bool IsDiffTab => SelectedDetailTab == 5;

    partial void OnFormModeChanged(string value) { OnPropertyChanged(nameof(IsMatchForm)); }
    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(IsFormClosed));

    [RelayCommand] private void CloseForm() { IsFormOpen = false; FormErrorMessage = string.Empty; }

    [RelayCommand] private void OpenMatchForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Match"; FormTitle = $"Ghép giao dịch — {SelectedItem.TxnId}"; FormErrorMessage = string.Empty;
        MatchSourceTxnId = SelectedItem.TxnId; MatchTargetTxnId = "";
        MatchAmount = SelectedItem.Type == "Payment" ? SelectedItem.Credit : SelectedItem.Debit;
        MatchReason = "";
        IsFormOpen = true;
    }

    [RelayCommand] private Task SubmitMatchForm() => SubmitMatchFormAsync();

    [RelayCommand] private Task AutoMatch() => AutoMatchAsync();

    [RelayCommand] private Task UndoMatch() => UndoMatchAsync();

    private async Task SubmitMatchFormAsync()
    {
        FormErrorMessage = string.Empty;
        if (SelectedItem is null)
        {
            FormErrorMessage = "Hãy chọn giao dịch cần đối chiếu.";
            return;
        }
        if (_debtTransactionService is null)
        {
            FormErrorMessage = "Chưa cấu hình Debt transaction service thật.";
            return;
        }
        if (!Guid.TryParse(MatchTargetTxnId.Trim(), out var enteredTargetId))
        {
            FormErrorMessage = "Mã giao dịch đích phải là GUID của giao dịch công nợ thật.";
            return;
        }
        if (MatchAmount <= 0)
        {
            FormErrorMessage = "Số tiền phân bổ phải lớn hơn 0.";
            return;
        }

        Guid sourceId;
        Guid targetId;
        if (SelectedItem.Type == "Payment")
        {
            if (!Guid.TryParse(SelectedItem.TxnId, out sourceId))
            {
                FormErrorMessage = "Khoản thu đang chọn không có mã giao dịch thật.";
                return;
            }
            targetId = enteredTargetId;
        }
        else
        {
            if (!Guid.TryParse(enteredTargetId.ToString(), out sourceId)
                || !Guid.TryParse(SelectedItem.TxnId, out targetId))
            {
                FormErrorMessage = "Không xác định được giao dịch nguồn/đích thật.";
                return;
            }
        }

        try
        {
            await _debtTransactionService.AllocatePaymentAsync(
                "Receivable",
                sourceId,
                null,
                "Receivable",
                targetId,
                MatchAmount,
                DateTime.UtcNow,
                string.IsNullOrWhiteSpace(MatchReason) ? "AR Reconciliation" : MatchReason.Trim(),
                CancellationToken.None);
            IsFormOpen = false;
            InfoMessage = $"Đã ghi allocation đối soát thật: {MatchAmount:N0}.";
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Không thể ghi allocation thật: {ex.Message}";
        }
    }

    private async Task AutoMatchAsync()
    {
        if (SelectedItem is null)
        {
            InfoMessage = "Hãy chọn một khoản thu thật để tự động phân bổ.";
            return;
        }
        if (_debtTransactionService is null)
        {
            InfoMessage = "Chưa cấu hình Debt transaction service thật.";
            return;
        }
        if (SelectedItem.Type != "Payment" || !Guid.TryParse(SelectedItem.TxnId, out var paymentId))
        {
            InfoMessage = "Tự động phân bổ chỉ áp dụng cho giao dịch Payment có ID thật.";
            return;
        }

        try
        {
            var count = await _debtTransactionService.AutoAllocatePaymentAsync(paymentId, "AR Reconciliation", CancellationToken.None);
            InfoMessage = count > 0
                ? $"Đã tự động phân bổ vào {count} khoản công nợ thật."
                : "Không tìm thấy công nợ cùng khách hàng còn số dư để phân bổ.";
            if (count > 0)
                await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể tự động đối soát thật: {ex.Message}";
        }
    }

    private async Task UndoMatchAsync()
    {
        if (SelectedItem is null)
        {
            InfoMessage = "Hãy chọn một giao dịch thật để hủy ghép.";
            return;
        }
        if (_debtTransactionService is null)
        {
            InfoMessage = "Chưa cấu hình Debt transaction service thật.";
            return;
        }
        if (SelectedItem.Type != "Payment" || !Guid.TryParse(SelectedItem.TxnId, out var paymentId))
        {
            InfoMessage = "Hủy ghép chỉ áp dụng cho giao dịch Payment có ID thật.";
            return;
        }

        try
        {
            var count = await _debtTransactionService.UndoPaymentAllocationsAsync(paymentId, "AR Reconciliation", CancellationToken.None);
            InfoMessage = count > 0
                ? $"Đã hủy {count} allocation đối soát trên dữ liệu thật."
                : "Khoản thu này chưa có allocation để hủy.";
            if (count > 0)
                await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể hủy ghép thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (Items.Count == 0)
        {
            InfoMessage = "Không có giao dịch công nợ thật để xuất.";
            return;
        }

        var path = CsvExportHelper.Export(
            "ar_reconciliation",
            ["Mã giao dịch", "Invoice", "Phiếu thu", "Khách", "Doanh nghiệp", "Ngày hạch toán", "Loại", "Debit", "Credit", "Thanh toán", "Còn nợ", "Tiền tệ", "Trạng thái khớp", "Lý do"],
            Items.Select(item => (IReadOnlyList<string>)[
                item.TxnId,
                item.InvoiceNo ?? string.Empty,
                item.ReceiptNo ?? string.Empty,
                item.GuestName ?? string.Empty,
                item.CompanyName ?? string.Empty,
                item.PostingDate.ToString("dd/MM/yyyy"),
                item.TypeLabel,
                CsvExportHelper.F(item.Debit),
                CsvExportHelper.F(item.Credit),
                CsvExportHelper.F(item.Payment),
                CsvExportHelper.F(item.Outstanding),
                item.Currency,
                item.MatchStatus,
                item.MatchReason ?? string.Empty
            ]).ToList());

        InfoMessage = $"Đã xuất đối soát thật dạng CSV (mở bằng Excel): {path}";
    }
    [RelayCommand]
    private void ExportPdf()
    {
        if (Items.Count == 0) { InfoMessage = "Không có giao dịch đối soát thật để xuất PDF."; return; }
        var path = PdfExportHelper.Export(
            "Biên bản đối soát công nợ",
            "ar_reconciliation",
            ["Mã giao dịch", "Khách/Đối tác", "Mô tả", "Nợ", "Có", "Thanh toán", "Còn nợ", "Kết quả"],
            Items.Select(item => (IReadOnlyList<string>)[
                item.TxnId, item.CompanyName ?? item.GuestName ?? string.Empty, item.Description,
                CsvExportHelper.F(item.Debit), CsvExportHelper.F(item.Credit),
                CsvExportHelper.F(item.Payment), CsvExportHelper.F(item.Outstanding),
                item.MatchStatus]).ToList());
        if (!string.IsNullOrWhiteSpace(path)) InfoMessage = $"Đã xuất PDF biên bản đối soát thật: {path}";
    }

    [RelayCommand]
    private void PrintStatement()
    {
        if (Items.Count == 0) { InfoMessage = "Không có giao dịch đối soát thật để in."; return; }
        var content = new StringBuilder()
            .AppendLine("BIÊN BẢN ĐỐI SOÁT CÔNG NỢ")
            .AppendLine(new string('-', 110));
        foreach (var item in Items)
            content.AppendLine($"{item.TxnId,-18} | {item.CompanyName ?? item.GuestName ?? "—",-28} | {item.Description,-30} | Nợ {item.Debit,15:N0} | Có {item.Credit,15:N0} | {item.MatchStatus}");
        new PrintPreviewDialog("Biên bản đối soát công nợ", content.ToString())
        {
            Owner = System.Windows.Application.Current?.MainWindow
        }.ShowDialog();
        InfoMessage = "Đã mở bản xem trước đối soát từ dữ liệu thật.";
    }
    [RelayCommand] private void RefreshCommand() { _ = LoadAsync(CancellationToken.None); }
}
