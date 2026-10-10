using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public partial class FolioWorkspaceWindow : Window
{
    private readonly IServiceScope? _dataScope;
    // ── State ──
    private readonly List<FwFolio> _folios;
    private readonly List<FwTransaction> _allTxns;
    private readonly List<FwPayment> _payments;
    private readonly List<FwDeposit> _deposits;
    private readonly List<FwRefund> _refunds;
    private readonly List<FwRoutingRule> _routings;
    private readonly Dictionary<Guid, DepositSummaryDto> _depositSummaries;
    private string? _depositSummaryLoadWarning;
    private readonly IBusinessDocumentService? _businessDocumentService;
    private IReadOnlyList<BusinessDocumentDto> _folioDocuments = Array.Empty<BusinessDocumentDto>();
    private string? _documentLoadWarning;
    private FwFolio? _activeFolio;
    private FwTransaction? _activeTxn;
    private int _txnPage, _txnPageSize = 20;
    private string _ribbonFilter = "ALL";
    private int _sideTab;
    private int _inspectorSubTab;
    private readonly List<FwFolio> _filteredFolios;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IHotelPaymentRepository? _paymentRepository;
    private readonly HotelFolioApplicationService? _folioService;
    private readonly IFolioRoutingQueryService? _routingQuery;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly Guid? _initialStayId;

    public FolioWorkspaceWindow() : this(null)
    {
    }

    public FolioWorkspaceWindow(Guid? initialStayId)
    {
        _initialStayId = initialStayId;
        InitializeComponent();
        Owner = System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner ? owner : null;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _paymentRepository = services?.GetService<IHotelPaymentRepository>();
        _businessDocumentService = services?.GetService<IBusinessDocumentService>();
        _folioService = services?.GetService<HotelFolioApplicationService>();
        _routingQuery = services?.GetService<IFolioRoutingQueryService>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _folios = new List<FwFolio>();
        _filteredFolios = new List<FwFolio>(_folios);
        _allTxns = new List<FwTransaction>();
        _payments = new List<FwPayment>();
        _deposits = new List<FwDeposit>();
        _refunds = new List<FwRefund>();
        _routings = new List<FwRoutingRule>();
        _depositSummaries = new Dictionary<Guid, DepositSummaryDto>();
        InitFilters();
        InitSideTabs();
        RefreshFolioList();
        RefreshTransactions(true);
        BuildRibbon();
        BuildInspectorTabs();
        UpdateSummaryStrip();
        UpdateBottomSummary();
        FooterText.Text = "Sẵn sàng · Chọn Folio để xem chi tiết tài chính";
        Loaded += async (_, _) => await LoadPersistedFoliosAsync();
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async Task LoadPersistedFoliosAsync()
    {
        if (_folioRepository is null || _contextAccessor?.Current is not { } context)
        {
            FooterText.Text = "Chưa cấu hình được Folio service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var page = await _folioRepository!.SearchAsync(
                new FolioSearchFilter(context.Property.PropertyId.Value, null, null, null, null, null, null, null),
                new PageRequest(1, 500),
                CancellationToken.None);
            var ledgers = page.Items.ToList();

            _folios.Clear();
            _allTxns.Clear();
            _payments.Clear();
            _deposits.Clear();
            _refunds.Clear();
            _routings.Clear();
            _depositSummaries.Clear();
            _depositSummaryLoadWarning = null;
            _folioDocuments = Array.Empty<BusinessDocumentDto>();
            _documentLoadWarning = null;
            _activeTxn = null;

            if (_routingQuery is not null)
            {
                try
                {
                    var routingRules = await _routingQuery.SearchAsync(
                        new FolioRoutingSearchRequest(context.Property.PropertyId.Value),
                        CancellationToken.None);
                    _routings.AddRange(routingRules.Select(x => new FwRoutingRule(
                        x.Name, x.SourceFolioNumber, x.TargetFolioNumber, x.EntryType, x.Percent, x.IsActive)));
                }
                catch (Exception routingError)
                {
                    FooterText.Text = "Folio đã tải; không tải được routing thật: " + routingError.Message;
                }
            }

            for (var index = 0; index < ledgers.Count; index++)
            {
                var ledger = ledgers[index];
                var id = index + 1;
                var paymentLookup = new Dictionary<Guid, HotelPaymentDto>();
                try
                {
                    if (_paymentRepository is not null)
                    {
                        if (ledger.Entries.Any(x => x.EntryType == HotelFolioEntryType.Payment))
                        {
                            var persistedPayments = await _paymentRepository.GetForFolioAsync(
                                context.Property.PropertyId.Value,
                                ledger.HotelFolioId,
                                CancellationToken.None);
                            foreach (var payment in persistedPayments)
                            {
                                if (payment.FolioEntryId is { } folioEntryId)
                                    paymentLookup[folioEntryId] = payment;
                            }
                        }

                        _depositSummaries[ledger.HotelFolioId] = await _paymentRepository.GetDepositSummaryAsync(
                            context.Property.PropertyId.Value,
                            ledger.HotelFolioId,
                            CancellationToken.None);
                    }
                }
                catch (Exception paymentError)
                {
                    _depositSummaryLoadWarning ??= "Không đọc đủ số liệu thanh toán/đặt cọc thật: " + paymentError.Message;
                }

                _folios.Add(MapPersistedFolio(
                    id,
                    ledger,
                    context.Property.CurrencyCode ?? "VND",
                    _depositSummaries.TryGetValue(ledger.HotelFolioId, out var depositSummary) ? depositSummary : null));
                foreach (var entry in ledger.Entries)
                    AddPersistedEntry(_allTxns, _payments, _deposits, _refunds, id, ledger, entry, context.Property.CurrencyCode ?? "VND", paymentLookup);
            }

            _filteredFolios.Clear();
            _filteredFolios.AddRange(_folios);
            _activeFolio = _initialStayId is { } stayId
                ? _folios.FirstOrDefault(f => f.StayId == stayId)
                : null;
            RefreshFolioList();
            RefreshTransactions(true);
            UpdateSummaryStrip();
            UpdateInspector();
            UpdateBottomSummary();
            FooterText.Text = $"Đã tải {ledgers.Count} Folio từ cơ sở dữ liệu";
            if (!string.IsNullOrWhiteSpace(_depositSummaryLoadWarning))
                FooterText.Text += $" · {_depositSummaryLoadWarning}";
            if (_activeFolio is not null)
                FooterText.Text += $" · Đã chọn {_activeFolio.Code}";
        }
        catch (Exception ex)
        {
            FooterText.Text = "Không thể tải Folio thật: " + ex.Message;
        }
    }

    private static FwFolio MapPersistedFolio(int id, FolioLedgerDto ledger, string currency, DepositSummaryDto? depositSummary = null)
    {
        // Payments and deposits are negative ledger entries. The workspace
        // displays their applied amounts as positive values.
        var paid = ledger.Entries
            .Where(x => x.EntryType == HotelFolioEntryType.Payment)
            .Sum(x => -x.Amount);
        var deposit = ledger.Entries
            .Where(x => x.EntryType == HotelFolioEntryType.Deposit)
            .Sum(x => -x.Amount);
        // Keep the legacy list model aligned with the domain ledger. The old
        // fixture-based view reconstructed balance from a partial charge list,
        // which omitted refunds and newer charge types. RunningBalance is the
        // authoritative signed result; derive the display total from it.
        var total = ledger.RunningBalance + paid + deposit;
        var status = ledger.Status.ToString();
        return new FwFolio(
            id,
            "Folio",
            ledger.GuestName,
            ledger.FolioNumber,
            currency,
            ToInt(total),
            ToInt(deposit),
            ToInt(paid),
            status,
            ledger.RoomNumber,
            ledger.ReservationNumber ?? "—",
            ledger.StayNumber,
            ledger.ArrivalDate?.ToString("dd/MM/yyyy") ?? "—",
            ledger.DepartureDate?.ToString("dd/MM/yyyy") ?? "—",
            ledger.CompanyName,
            ledger.TravelAgentName,
            0,
            0,
            ledger.HotelStayId,
            ledger.HotelFolioId,
            ledger.RowVersion,
            ToInt(depositSummary?.AppliedDeposits ?? 0),
            ToInt(depositSummary?.RemainingDeposits ?? 0),
            depositSummary is not null,
            ledger.GuestProfileId);
    }

    private static void AddPersistedEntry(
        List<FwTransaction> transactions,
        List<FwPayment> payments,
        List<FwDeposit> deposits,
        List<FwRefund> refunds,
        int folioId,
        FolioLedgerDto ledger,
        HotelFolioEntryDto entry,
        string currency,
        IReadOnlyDictionary<Guid, HotelPaymentDto>? paymentLookup = null)
    {
        var amount = ToInt(entry.Amount);
        var displayAmount = entry.EntryType is HotelFolioEntryType.Payment or HotelFolioEntryType.Deposit or HotelFolioEntryType.Refund
            ? Math.Abs(amount)
            : amount;
        var type = MapEntryType(entry.EntryType);
        var time = entry.CreatedAtUtc.ToLocalTime();
        transactions.Add(new FwTransaction(
            time,
            $"{ledger.FolioNumber}-{entry.SequenceNumber:D4}",
            entry.ReferenceId?.ToString() ?? string.Empty,
            type,
            entry.Description,
            "FO",
            "Front Office",
            1,
            displayAmount,
            displayAmount,
            entry.EntryType == HotelFolioEntryType.Tax ? amount : 0,
            entry.EntryType == HotelFolioEntryType.Discount ? amount : 0,
            entry.EntryType == HotelFolioEntryType.ServiceCharge ? amount : 0,
            amount,
            currency,
            1m,
            !string.IsNullOrWhiteSpace(ledger.CompanyName) ? "Company" : !string.IsNullOrWhiteSpace(ledger.TravelAgentName) ? "TA" : "Guest",
            entry.CreatedBy?.ToString() ?? "—",
            entry.ReversedByEntryId.HasValue ? "Reversed" : "Posted",
            0,
            string.Empty,
            folioId,
            ledger.GuestName,
            ledger.RoomNumber,
            ledger.ReservationNumber ?? "—",
            ledger.FolioNumber,
            entry.HotelFolioEntryId,
            ledger.RowVersion));

        if (entry.EntryType == HotelFolioEntryType.Payment)
        {
            var payment = paymentLookup is not null && paymentLookup.TryGetValue(entry.HotelFolioEntryId, out var persistedPayment)
                ? persistedPayment
                : null;
            payments.Add(new FwPayment(
                payment?.PaymentNumber ?? $"PAY-{entry.HotelFolioEntryId}",
                payment?.PaymentNumber ?? $"RCP-{entry.SequenceNumber}",
                payment?.ExternalReference ?? entry.ReferenceId?.ToString() ?? string.Empty,
                payment?.PaymentMethod.ToString() ?? "Không xác định",
                ToInt(Math.Abs(payment?.Amount ?? entry.Amount)),
                payment?.Currency.ToString() ?? currency,
                payment?.Status.ToString() ?? (entry.ReversedByEntryId.HasValue ? "Reversed" : "Posted"),
                folioId,
                payment?.CreatedAtUtc.ToLocalTime() ?? time,
                entry.CreatedBy?.ToString() ?? "—"));
        }
        else if (entry.EntryType == HotelFolioEntryType.Deposit)
            deposits.Add(new FwDeposit($"DEP-{entry.HotelFolioEntryId}", Math.Abs(amount), currency, folioId, time));
        else if (entry.EntryType == HotelFolioEntryType.Refund)
            refunds.Add(new FwRefund($"REF-{entry.HotelFolioEntryId}", Math.Abs(amount), entry.Description, true, "Posted", folioId, time, entry.CreatedBy?.ToString() ?? "—"));
    }

    private static bool IsCharge(HotelFolioEntryDto entry) => entry.EntryType is
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Laundry or
        HotelFolioEntryType.MiniBar or HotelFolioEntryType.Spa or HotelFolioEntryType.Telephone or
        HotelFolioEntryType.OtherCharge or HotelFolioEntryType.Tax or HotelFolioEntryType.ServiceCharge or
        HotelFolioEntryType.Discount or HotelFolioEntryType.Package or HotelFolioEntryType.Adjustment or
        HotelFolioEntryType.Correction or HotelFolioEntryType.ManualCharge or HotelFolioEntryType.ManualCredit or
        HotelFolioEntryType.OpeningBalance;

    private static string MapEntryType(HotelFolioEntryType type) => type switch
    {
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Laundry or
        HotelFolioEntryType.MiniBar or HotelFolioEntryType.Spa or HotelFolioEntryType.Telephone or
        HotelFolioEntryType.OtherCharge or HotelFolioEntryType.ManualCharge => "Charge",
        HotelFolioEntryType.Payment => "Payment",
        HotelFolioEntryType.Deposit => "Deposit",
        HotelFolioEntryType.Refund => "Refund",
        HotelFolioEntryType.Tax => "Tax",
        HotelFolioEntryType.ServiceCharge => "Service Charge",
        HotelFolioEntryType.Discount => "Discount",
        HotelFolioEntryType.Transfer => "Transfer",
        _ => "Adjustment"
    };

    private static int ToInt(decimal value) => decimal.ToInt32(decimal.Round(value, 0, MidpointRounding.AwayFromZero));

    // ═══════════════════════════════════════════
    // Historical designer fixtures are retained for reference only. Production data
    // is loaded from IHotelFolioRepository and the current property context.
#if false
    // ═══════════════════════════════════════════

    private static List<FwFolio> LoadFolios()
    {
        var names = new[] { "Nguyễn Văn An", "Trần Thị Bình" };
        var types = new[] { "Master", "Room", "Restaurant", "Spa", "Laundry", "Extra", "Company", "Agent", "Event", "Voucher", "City Ledger" };
        var currencies = new[] { "VND", "VND", "VND", "USD", "EUR" };
        var statuses = new[] { "Open", "Open", "Open", "Settled", "Pending", "Transferred" };
        var r = new Random(42);
        var list = new List<FwFolio>();

        // Guest 1: Nguyễn Văn An — 8 folios
        list.Add(new FwFolio(1, "Master", names[0], "FOL-26001", "VND", 15200000, 5000000, 12000000, "Open",
            "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", "Cty TNHH ABC", "TA Saigontourist", 20000000, 5000000));
        list.Add(new FwFolio(2, "Room", names[0], "FOL-26002", "VND", 7200000, 0, 5000000, "Open",
            "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", "Cty TNHH ABC", "TA Saigontourist", 0, 0));
        list.Add(new FwFolio(3, "Restaurant", names[0], "FOL-26003", "VND", 3200000, 0, 2800000, "Open",
            "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", "Cty TNHH ABC", "TA Saigontourist", 0, 0));
        list.Add(new FwFolio(4, "Spa", names[0], "FOL-26004", "VND", 1800000, 0, 1500000, "Settled",
            "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", null, null, 0, 0));
        list.Add(new FwFolio(5, "Laundry", names[0], "FOL-26005", "VND", 450000, 0, 450000, "Settled",
            "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", null, null, 0, 0));
        list.Add(new FwFolio(6, "Extra", names[0], "FOL-26006", "VND", 600000, 0, 0, "Open",
            "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", "Cty TNHH ABC", null, 0, 0));
        list.Add(new FwFolio(7, "Company", names[0], "FOL-26007", "VND", 10400000, 0, 8000000, "Open",
            "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", "Cty TNHH ABC", null, 15000000, 0));
        list.Add(new FwFolio(8, "Voucher", names[0], "FOL-26008", "VND", -500000, 0, -500000, "Pending",
            "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", null, null, 0, 0));

        // Guest 2: Trần Thị Bình — 5 folios
        list.Add(new FwFolio(9, "Master", names[1], "FOL-26009", "USD", 850, 200, 600, "Open",
            "205", "RES-24002", "STY-002", "25/07/2026", "28/07/2026", null, "TA Viettravel", 1000, 200));
        list.Add(new FwFolio(10, "Room", names[1], "FOL-26010", "USD", 450, 0, 300, "Open",
            "205", "RES-24002", "STY-002", "25/07/2026", "28/07/2026", null, "TA Viettravel", 0, 0));
        list.Add(new FwFolio(11, "Restaurant", names[1], "FOL-26011", "USD", 120, 0, 110, "Open",
            "205", "RES-24002", "STY-002", "25/07/2026", "28/07/2026", null, null, 0, 0));
        list.Add(new FwFolio(12, "Spa", names[1], "FOL-26012", "USD", 80, 0, 80, "Settled",
            "205", "RES-24002", "STY-002", "25/07/2026", "28/07/2026", null, null, 0, 0));
        list.Add(new FwFolio(13, "City Ledger", names[1], "FOL-26013", "USD", 650, 0, 0, "Pending",
            "205", "RES-24002", "STY-002", "25/07/2026", "28/07/2026", null, "TA Viettravel", 1000, 0));

        return list;
    }

    private static List<FwTransaction> LoadTransactions()
    {
        var types = new[] { "Charge", "Charge", "Charge", "Charge", "Payment", "Payment", "Deposit", "Deposit", "Refund", "Adjustment", "Transfer", "Tax", "Discount", "Service Charge" };
        var depts = new[] { "FO", "F&B", "SPA", "LAUN", "MINI", "EVENT", "OTHER" };
        var outlets = new[] { "Reception", "Main Restaurant", "Lobby Bar", "Spa Center", "Laundry", "Room Service", "Banquet", "Pool Bar" };
        var payers = new[] { "Guest", "Guest", "Company", "TA", "House" };
        var operators = new[] { "Lễ tân A", "Lễ tân B", "Thu ngân A", "Thu ngân B", "FO Sup", "GSM" };
        var statuses = new[] { "Posted", "Posted", "Posted", "Pending", "Reversed" };
        var currencies = new[] { "VND", "VND", "VND", "USD" };
        var r = new Random(99);
        var list = new List<FwTransaction>();
        var d = new DateTime(2026, 7, 24, 6, 0, 0);

        for (int i = 1; i <= 320; i++)
        {
            var t = types[r.Next(14)];
            var isCredit = t is "Payment" or "Deposit" or "Refund";
            var amount = t switch
            {
                "Charge" => r.Next(80000, 3600000),
                "Payment" => r.Next(500000, 5000000),
                "Deposit" => r.Next(1000000, 8000000),
                "Refund" => r.Next(200000, 2000000),
                "Adjustment" => r.Next(-500000, 500000),
                "Transfer" => r.Next(100000, 2000000),
                "Tax" => r.Next(50000, 500000),
                "Discount" => r.Next(-300000, -20000),
                "Service Charge" => r.Next(30000, 300000),
                _ => r.Next(100000, 1000000)
            };
            var discount = t is "Charge" or "Payment" ? r.Next(0, (int)(Math.Abs(amount) * 0.15)) : 0;
            var tax = t is "Charge" ? (int)((amount - discount) * 0.08) : 0;
            var sc = t is "Charge" && r.Next(3) == 0 ? (int)(amount * 0.05) : 0;
            var net = amount - discount + tax + sc;
            var folioId = r.Next(1, 14);
            var curr = folioId >= 9 ? currencies[r.Next(2, 4)] : currencies[r.Next(3)];
            var folioIdx = folioId - 1;
            var folioName = folioIdx < 13 ? new[] { "Nguyễn Văn An", "Nguyễn Văn An", "Nguyễn Văn An", "Nguyễn Văn An", "Nguyễn Văn An", "Nguyễn Văn An", "Nguyễn Văn An", "Nguyễn Văn An", "Trần Thị Bình", "Trần Thị Bình", "Trần Thị Bình", "Trần Thị Bình", "Trần Thị Bình" }[folioIdx] : "—";

            list.Add(new FwTransaction(
                Time: d.AddMinutes(r.Next(0, 2880)),
                Id: $"TXN-{27000 + i}",
                Reference: t is "Payment" ? $"RCP-{25000 + r.Next(1, 50):D4}" : t is "Deposit" ? $"DEP-{24000 + r.Next(1, 30):D4}" : $"",
                Type: t,
                Description: t switch
                {
                    "Charge" => new[] { "Tiền phòng", "Ăn sáng buffet", "Giặt ủi", "Massage Thái", "Mini bar", "Room service", "Thuê xe", "Vé máy bay", "Tour City", "Internet Premium" }[r.Next(10)],
                    "Payment" => new[] { "Thanh toán tiền mặt", "Thanh toán Visa", "Thanh toán chuyển khoản", "Thanh toán Mastercard", "Thanh toán QR Pay" }[r.Next(5)],
                    "Deposit" => "Đặt cọc đảm bảo",
                    "Refund" => "Hoàn tiền đặt cọc",
                    "Adjustment" => "Điều chỉnh sai lệch",
                    "Transfer" => "Chuyển sang Folio khác",
                    "Tax" => "Thuế VAT 8%",
                    "Discount" => new[] { "Giảm giá thành viên", "Giảm giá đoàn", "Chiết khấu đại lý", "Khuyến mãi mùa hè" }[r.Next(4)],
                    "Service Charge" => "Phí dịch vụ 5%",
                    _ => "Khác"
                },
                Dept: depts[r.Next(7)],
                Outlet: outlets[r.Next(8)],
                Qty: t is "Charge" ? r.Next(1, 5) : 1,
                Price: t is "Charge" ? amount / Math.Max(1, r.Next(1, 5)) : amount,
                Amount: amount,
                Tax: tax,
                Discount: discount,
                ServiceCharge: sc,
                Net: net,
                Currency: curr,
                ExchangeRate: curr == "USD" ? 25450m : curr == "EUR" ? 27800m : 1m,
                Payer: payers[r.Next(5)],
                Operator: operators[r.Next(6)],
                Status: statuses[r.Next(5)],
                BalanceAfter: 0,
                Remark: r.Next(5) == 0 ? "Đã xác nhận" : "",
                FolioId: folioId,
                GuestName: folioName,
                Room: folioIdx < 13 ? new[] { "101", "101", "101", "101", "101", "101", "101", "101", "205", "205", "205", "205", "205" }[folioIdx] : "—",
                Reservation: folioIdx < 13 ? new[] { "RES-24001", "RES-24001", "RES-24001", "RES-24001", "RES-24001", "RES-24001", "RES-24001", "RES-24001", "RES-24002", "RES-24002", "RES-24002", "RES-24002", "RES-24002" }[folioIdx] : "—",
                FolioCode: folioId <= 13 ? $"FOL-{26000 + folioId}" : "FOL-????"
            ));
        }

        // Sort by time descending
        return list.OrderByDescending(tx => tx.Time).ToList();
    }

    private static List<FwPayment> LoadPayments()
    {
        var methods = new[] { "Tiền mặt", "Visa", "Mastercard", "JCB", "AMEX", "QR Pay", "Chuyển khoản", "Ví điện tử", "Công ty", "Đại lý", "Voucher", "Gift Card" };
        var r = new Random(123);
        var list = new List<FwPayment>();
        for (int i = 1; i <= 80; i++)
        {
            var folioId = r.Next(1, 14);
            list.Add(new FwPayment(
                Id: $"PAY-{26000 + i}",
                Receipt: $"RCP-{25000 + i:D4}",
                Reference: $"REF-{24000 + i:D4}",
                Method: methods[r.Next(12)],
                Amount: r.Next(200000, 5000000),
                Currency: folioId >= 9 ? (r.Next(2) == 0 ? "USD" : "VND") : "VND",
                Approval: r.Next(5) != 0 ? "Approved" : "Pending",
                FolioId: folioId,
                Time: new DateTime(2026, 7, 24 + r.Next(3), r.Next(6, 22), r.Next(0, 59), 0),
                Operator: new[] { "Thu ngân A", "Thu ngân B", "Lễ tân A" }[r.Next(3)]
            ));
        }
        return list.OrderByDescending(p => p.Time).ToList();
    }

    private static List<FwDeposit> LoadDeposits()
    {
        var r = new Random(456);
        var list = new List<FwDeposit>();
        for (int i = 1; i <= 30; i++)
        {
            var amount = r.Next(1000000, 8000000);
            var applied = r.Next(0, amount);
            list.Add(new FwDeposit(
                Id: $"DEP-{24000 + i}",
                Amount: amount,
                Applied: applied,
                Remaining: amount - applied,
                Currency: i <= 20 ? "VND" : "USD",
                Status: applied >= amount ? "Fully Applied" : applied > 0 ? "Partial" : "Unapplied",
                FolioId: r.Next(1, 14),
                Time: new DateTime(2026, 7, 23 + r.Next(3), r.Next(8, 20), 0, 0)
            ));
        }
        return list.OrderByDescending(d => d.Time).ToList();
    }

    private static List<FwRefund> LoadRefunds()
    {
        var reasons = new[] { "Check-out sớm", "Hủy dịch vụ", "Sai lệch giá", "Khiếu nại", "Đặt cọc dư", "Đổi phòng", "Hoàn tiền đại lý" };
        var r = new Random(789);
        var list = new List<FwRefund>();
        for (int i = 1; i <= 25; i++)
        {
            list.Add(new FwRefund(
                Id: $"REF-{23000 + i}",
                Amount: r.Next(100000, 3000000),
                Reason: reasons[r.Next(7)],
                Approved: r.Next(4) != 0,
                Status: r.Next(4) != 0 ? "Approved" : "Pending",
                FolioId: r.Next(1, 14),
                Time: new DateTime(2026, 7, 24 + r.Next(3), r.Next(8, 22), 0, 0),
                Operator: new[] { "GSM", "FO Sup", "Thu ngân A" }[r.Next(3)]
            ));
        }
        return list.OrderByDescending(rf => rf.Time).ToList();
    }

    private static List<FwRoutingRule> LoadRoutings()
    {
        return new List<FwRoutingRule>
        {
            new("Room Charge → Master", "Room", "Master", "%", 100, true),
            new("Restaurant → Master", "Restaurant", "Master", "%", 100, true),
            new("Spa → Master", "Spa", "Master", "%", 100, true),
            new("Laundry → Master", "Laundry", "Master", "%", 100, true),
            new("Room Charge → Company", "Room", "Company", "%", 100, true),
            new("Restaurant → Company", "Restaurant", "Company", "%", 50, true),
            new("Room Charge → TA", "Room", "Agent", "%", 10, false),
            new("Extra → Master", "Extra", "Master", "%", 100, true),
        };
    }

    // ═══════════════════════════════════════════
#endif

    // FILTERS
    // ═══════════════════════════════════════════

    private void InitFilters()
    {
        FwTypeFilter.ItemsSource = new[] { "Tất cả", "Charge", "Payment", "Deposit", "Refund", "Adjustment", "Transfer", "Tax", "Discount", "Service Charge" };
        FwTypeFilter.SelectedIndex = 0;
        FwCurrFilter.ItemsSource = new[] { "Tất cả", "VND", "USD", "EUR" };
        FwCurrFilter.SelectedIndex = 0;
        FwDeptFilter.ItemsSource = new[] { "Tất cả", "FO", "F&B", "SPA", "LAUN", "MINI", "EVENT" };
        FwDeptFilter.SelectedIndex = 0;
        FwStatusFilter.ItemsSource = new[] { "Tất cả", "Posted", "Pending", "Reversed" };
        FwStatusFilter.SelectedIndex = 0;
        TxnPageSizeBox.ItemsSource = new[] { "20 dòng / trang", "50 dòng / trang", "100 dòng / trang" };
        TxnPageSizeBox.SelectedIndex = 0;
    }

    // ═══════════════════════════════════════════
    // SIDE TABS
    // ═══════════════════════════════════════════

    private void InitSideTabs()
    {
        var tabs = new[] { "Tổng quan", "Giao dịch", "Thanh toán", "Đặt cọc", "Hoàn tiền", "Routing", "Lịch sử", "Tài liệu" };
        SideTabPanel.Children.Clear();
        for (int i = 0; i < tabs.Length; i++)
        {
            var idx = i;
            var active = i == 1; // default to Transactions
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 2),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = active ? Cb("#EFF6FF") : Cb("Transparent"),
                Child = new TextBlock { Text = tabs[i], FontSize = 12, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569") }
            };
            if (active) SetSideTabActive(border);
            border.MouseLeftButtonDown += (_, _) => SwitchSideTab(idx);
            SideTabPanel.Children.Add(border);
        }
        _sideTab = 1;
        ShowSideTabContent(1);
    }

    private void SwitchSideTab(int idx)
    {
        _sideTab = idx;
        foreach (var child in SideTabPanel.Children)
        {
            if (child is Border b && b.Child is TextBlock tb)
            {
                b.Background = Cb("Transparent");
                tb.Foreground = C("#475569");
                tb.FontWeight = FontWeights.Normal;
            }
        }
        if (SideTabPanel.Children[idx] is Border ab && ab.Child is TextBlock atb)
        {
            SetSideTabActive(ab);
        }
        ShowSideTabContent(idx);
    }

    private static void SetSideTabActive(Border b)
    {
        b.Background = Cb("#EFF6FF");
        if (b.Child is TextBlock tb) { tb.Foreground = C("#2F6BFF"); tb.FontWeight = FontWeights.SemiBold; }
    }

    private void ShowSideTabContent(int tab)
    {
        TxnContentArea.Visibility = tab == 1 ? Visibility.Visible : Visibility.Collapsed;
        PaymentContentArea.Visibility = tab == 2 ? Visibility.Visible : Visibility.Collapsed;
        DepositContentArea.Visibility = tab == 3 ? Visibility.Visible : Visibility.Collapsed;
        RefundContentArea.Visibility = tab == 4 ? Visibility.Visible : Visibility.Collapsed;
        RoutingContentArea.Visibility = tab == 5 ? Visibility.Visible : Visibility.Collapsed;
        HistoryContentArea.Visibility = tab == 6 ? Visibility.Visible : Visibility.Collapsed;
        DocumentContentArea.Visibility = tab == 7 ? Visibility.Visible : Visibility.Collapsed;
        OverviewContentArea.Visibility = tab == 0 ? Visibility.Visible : Visibility.Collapsed;

        switch (tab)
        {
            case 0: BuildOverview(); break;
            case 2: BuildPaymentsTab(); break;
            case 3: BuildDepositsTab(); break;
            case 4: BuildRefundsTab(); break;
            case 5: BuildRoutingTab(); break;
            case 6: BuildHistoryTab(); break;
            case 7:
                BuildDocumentsTab();
                if (_activeFolio is not null)
                    _ = LoadFolioDocumentsAsync(_activeFolio);
                break;
        }
    }

    // ═══════════════════════════════════════════
    // FOLIO LIST
    // ═══════════════════════════════════════════

    private void RefreshFolioList()
    {
        FolioListPanel.Children.Clear();
        foreach (var f in _filteredFolios)
        {
            var isActive = _activeFolio?.Id == f.Id;
            var balance = f.Total - f.Paid - f.Deposit;
            var badgeColor = f.Status switch
            {
                "Open" => balance > 0 ? "#F59E0B" : "#16A34A",
                "Settled" => "#16A34A",
                "Pending" => "#8B5CF6",
                "Transferred" => "#06B6D4",
                _ => "#64748B"
            };
            var badgeText = f.Status switch
            {
                "Open" => balance > 0 ? "Còn nợ" : "Đã cân",
                "Settled" => "Đã tất toán",
                "Pending" => "Chờ xử lý",
                "Transferred" => "Đã chuyển",
                _ => f.Status
            };

            var border = new Border
            {
                Style = FindResource("FolioListItem") as Style,
                Background = isActive ? Cb("#EFF6FF") : Cb("Transparent"),
                BorderBrush = isActive ? Cb("#BFDBFE") : Cb("#E2E8F0"),
                BorderThickness = new Thickness(isActive ? 0 : 0, 0, 0, 1),
                Child = new StackPanel
                {
                    Children =
                    {
                        new Grid { Margin = new Thickness(0,0,0,4), Children = {
                            new TextBlock { Text = f.Type, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = isActive ? C("#1E40AF") : C("#17365D"), HorizontalAlignment = HorizontalAlignment.Left },
                            new Border { Background = Cb(badgeColor), CornerRadius = new CornerRadius(6), Padding = new Thickness(6,1,6,1), HorizontalAlignment = HorizontalAlignment.Right,
                                Child = new TextBlock { Text = badgeText, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } }
                        }},
                        new TextBlock { Text = $"{f.Code} · {f.Currency}", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0,0,0,2) },
                        new TextBlock { Text = $"Balance: {balance:N0} {f.Currency}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = balance > 0 ? C("#DC2626") : C("#16A34A"), Margin = new Thickness(0,0,0,1) },
                        new TextBlock { Text = $"Payer: {f.PayerDisplay}", FontSize = 11, Foreground = C("#64748B") }
                    }
                }
            };
            var fid = f.Id;
            border.MouseLeftButtonDown += (_, _) => SelectFolio(fid);
            FolioListPanel.Children.Add(border);
        }
    }

    private void SelectFolio(int id)
    {
        _activeFolio = _filteredFolios.FirstOrDefault(f => f.Id == id);
        _activeTxn = null;
        RefreshFolioList();
        RefreshTransactions(true);
        UpdateSummaryStrip();
        UpdateInspector();
        UpdateBottomSummary();
        if (_activeFolio is not null)
            _ = LoadFolioDocumentsAsync(_activeFolio);
        var balance = (_activeFolio?.Total ?? 0) - (_activeFolio?.Paid ?? 0) - (_activeFolio?.Deposit ?? 0);
        FooterText.Text = _activeFolio != null
            ? $"{_activeFolio.Type} · {_activeFolio.Code} · {_activeFolio.GuestName} · Balance: {balance:N0} {_activeFolio.Currency}"
            : "Sẵn sàng · Chọn Folio để xem chi tiết tài chính";
    }

    // ═══════════════════════════════════════════
    // TRANSACTIONS
    // ═══════════════════════════════════════════

    private List<FwTransaction> GetFilteredTxns()
    {
        var t = _activeFolio != null ? _allTxns.Where(tx => tx.FolioId == _activeFolio.Id) : _allTxns.AsEnumerable();
        var kw = FwSearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw))
            t = t.Where(tx => tx.Description.ToLower().Contains(kw) || tx.Id.ToLower().Contains(kw) || tx.Reference.ToLower().Contains(kw) || tx.GuestName.ToLower().Contains(kw));
        if (_ribbonFilter != "ALL")
            t = t.Where(tx => string.Equals(tx.Type, _ribbonFilter, StringComparison.OrdinalIgnoreCase));
        var typeFilter = FwTypeFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "Tất cả" && _ribbonFilter == "ALL")
            t = t.Where(tx => tx.Type == typeFilter);
        var currFilter = FwCurrFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(currFilter) && currFilter != "Tất cả")
            t = t.Where(tx => tx.Currency == currFilter);
        var deptFilter = FwDeptFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(deptFilter) && deptFilter != "Tất cả")
            t = t.Where(tx => tx.Dept == deptFilter);
        var statusFilter = FwStatusFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Tất cả")
            t = t.Where(tx => tx.Status == statusFilter);
        return t.ToList();
    }

    private void RefreshTransactions(bool reset)
    {
        if (reset) _txnPage = 0;
        var f = GetFilteredTxns();
        var tp = Math.Max(1, (f.Count + _txnPageSize - 1) / _txnPageSize);
        if (_txnPage >= tp) _txnPage = tp - 1;
        TxnGrid.ItemsSource = f.Skip(_txnPage * _txnPageSize).Take(_txnPageSize).ToList();
        TxnPagerSummary.Text = $"{_txnPage * _txnPageSize + 1}–{Math.Min((_txnPage + 1) * _txnPageSize, f.Count)} / {f.Count} giao dịch";
        TxnPrevBtn.IsEnabled = _txnPage > 0;
        TxnNextBtn.IsEnabled = _txnPage < tp - 1;
        BuildTxnPageButtons(tp);
        UpdateBottomSummary();
    }

    private void BuildTxnPageButtons(int tp)
    {
        TxnPageBtns.Children.Clear();
        for (int i = 0; i < tp && i < 7; i++)
        {
            var idx = i;
            var nb = new Button
            {
                Content = (i + 1).ToString(), Width = 30, Height = 28, FontSize = 11, Margin = new Thickness(1, 0, 1, 0),
                Background = i == _txnPage ? Cb("#2F6BFF") : Cb("#F1F5F9"),
                Foreground = i == _txnPage ? Cb("#FFF") : Cb("#475569"),
                BorderThickness = new Thickness(i == _txnPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            nb.Click += (_, _) => { _txnPage = idx; RefreshTransactions(false); };
            nb.Template = TxnPrevBtn.Template;
            TxnPageBtns.Children.Add(nb);
        }
    }

    // ═══════════════════════════════════════════
    // RIBBON
    // ═══════════════════════════════════════════

    private void BuildRibbon()
    {
        RibbonPanel.Children.Clear();
        var filters = new[] { "ALL", "Charge", "Payment", "Deposit", "Refund", "Adjustment", "Transfer", "Tax", "Discount", "Service Charge" };
        var labels = new[] { "Tất cả", "Charge", "Payment", "Deposit", "Refund", "Điều chỉnh", "Chuyển", "Thuế", "Discount", "Phí DV" };
        var colors = new[] { "#475569", "#DC2626", "#16A34A", "#F59E0B", "#8B5CF6", "#06B6D4", "#EC4899", "#6366F1", "#F97316", "#14B8A6" };
        for (int i = 0; i < filters.Length; i++)
        {
            var filter = filters[i];
            var active = _ribbonFilter == filter;
            var btn = new Button
            {
                Content = labels[i], Height = 28, FontSize = 11, FontWeight = FontWeights.SemiBold,
                Background = active ? Cb(colors[i]) : Cb("#F1F5F9"),
                Foreground = active ? Cb("White") : C("#475569"),
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 4, 0),
                Template = FindResource("RibbonBtn") as ControlTemplate
            };
            btn.Click += (_, _) => { _ribbonFilter = filter; BuildRibbon(); RefreshTransactions(true); };
            RibbonPanel.Children.Add(btn);
        }
    }

    // ═══════════════════════════════════════════
    // INSPECTOR
    // ═══════════════════════════════════════════

    private void BuildInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Tài chính", "Thuế", "Audit", "Routing", "Phê duyệt", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++)
        {
            var idx = i;
            var isActive = i == _inspectorSubTab;
            var border = new Border
            {
                CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 1, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = isActive ? Cb("White") : Cb("Transparent"),
                BorderBrush = isActive ? Cb("#E2E8F0") : Cb("Transparent"),
                BorderThickness = new Thickness(1, 1, 1, 0),
                Child = new TextBlock { Text = tabs[i], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = isActive ? C("#2F6BFF") : C("#64748B") }
            };
            border.MouseLeftButtonDown += (_, _) =>
            {
                _inspectorSubTab = idx;
                BuildInspectorTabs();
                UpdateInspector();
            };
            InspectorTabs.Children.Add(border);
        }
    }

    private void UpdateInspector()
    {
        InspectorPanel.Children.Clear();
        if (_activeTxn == null)
        {
            InspectorPanel.Children.Add(new TextBlock { Text = "Chọn một giao dịch để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap });
            return;
        }
        var tx = _activeTxn;
        switch (_inspectorSubTab)
        {
            case 0: // General
                InspectorPanel.Children.Add(STitle("Thông tin chung"));
                InspectorPanel.Children.Add(Row("Mã GD", tx.Id)); InspectorPanel.Children.Add(Row("Tham chiếu", tx.Reference));
                InspectorPanel.Children.Add(Row("Thời gian", tx.Time.ToString("dd/MM/yyyy HH:mm")));
                InspectorPanel.Children.Add(Row("Khách", tx.GuestName)); InspectorPanel.Children.Add(Row("Phòng", tx.Room));
                InspectorPanel.Children.Add(Row("Reservation", tx.Reservation)); InspectorPanel.Children.Add(Row("Folio", tx.FolioCode));
                InspectorPanel.Children.Add(Row("Bộ phận", tx.Dept)); InspectorPanel.Children.Add(Row("Outlet", tx.Outlet));
                InspectorPanel.Children.Add(Row("Operator", tx.Operator)); InspectorPanel.Children.Add(Row("Trạng thái", tx.Status));
                break;
            case 1: // Financial
                InspectorPanel.Children.Add(STitle("Tài chính"));
                InspectorPanel.Children.Add(Row("Tiền gốc", $"{tx.Amount:N0} {tx.Currency}"));
                InspectorPanel.Children.Add(Row("Discount", $"{tx.Discount:N0}"));
                InspectorPanel.Children.Add(Row("Thuế", $"{tx.Tax:N0}"));
                InspectorPanel.Children.Add(Row("Phí DV", $"{tx.ServiceCharge:N0}"));
                InspectorPanel.Children.Add(Row("Net", $"{tx.Net:N0} {tx.Currency}"));
                InspectorPanel.Children.Add(Row("Tệ", tx.Currency));
                InspectorPanel.Children.Add(Row("Tỷ giá", tx.ExchangeRate.ToString("N0")));
                InspectorPanel.Children.Add(Row("Số dư sau", $"{tx.BalanceAfter:N0}"));
                break;
            case 2: // Tax
                InspectorPanel.Children.Add(STitle("Thuế"));
                InspectorPanel.Children.Add(Row("VAT 8%", $"{tx.Tax:N0}"));
                InspectorPanel.Children.Add(Row("Phí DV 5%", $"{tx.ServiceCharge:N0}"));
                InspectorPanel.Children.Add(Row("Tổng thuế/phí", $"{tx.Tax + tx.ServiceCharge:N0}"));
                break;
            case 3: // Audit
                InspectorPanel.Children.Add(STitle("Audit Trail"));
                InspectorPanel.Children.Add(Row("Tạo lúc", tx.Time.ToString("dd/MM/yyyy HH:mm")));
                InspectorPanel.Children.Add(Row("Người tạo", tx.Operator));
                InspectorPanel.Children.Add(Row("Sửa lần cuối", "—"));
                InspectorPanel.Children.Add(Row("Duyệt", tx.Status == "Posted" ? "Đã duyệt" : "Chưa duyệt"));
                InspectorPanel.Children.Add(Row("In", "Chưa in"));
                InspectorPanel.Children.Add(Row("Chuyển", "—"));
                InspectorPanel.Children.Add(Row("Đảo ngược", tx.Status == "Reversed" ? "Đã đảo" : "—"));
                if (tx.Status == "Reversed")
                {
                    InspectorPanel.Children.Add(new TextBlock
                    {
                        Text = "Giao dịch này đã được đảo ngược, không thể thực hiện lại.",
                        FontSize = 11,
                        Foreground = C("#16A34A"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 10, 0, 0)
                    });
                }
                else if (CanReverse(tx))
                {
                    var reverseButton = new Button
                    {
                        Content = "Đảo ngược giao dịch",
                        Padding = new Thickness(10, 6, 10, 6),
                        Margin = new Thickness(0, 10, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Background = Cb("#DC2626"),
                        Foreground = Cb("White"),
                        BorderBrush = Cb("#B91C1C"),
                        BorderThickness = new Thickness(1),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    reverseButton.Click += async (_, _) => await OnReverseTransactionAsync(tx);
                    InspectorPanel.Children.Add(reverseButton);
                    InspectorPanel.Children.Add(new TextBlock
                    {
                        Text = "Tạo bút toán correction và giữ nguyên lịch sử giao dịch gốc.",
                        FontSize = 11,
                        Foreground = C("#64748B"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 5, 0, 0)
                    });
                }
                break;
            case 4: // Routing
                InspectorPanel.Children.Add(STitle("Routing"));
                var rules = _routings.Where(r => r.SourceFolio == GetFolioCode(tx.FolioId)).ToList();
                if (rules.Count == 0)
                    InspectorPanel.Children.Add(new TextBlock { Text = "Không có routing rule.", FontSize = 12, Foreground = C("#94A3B8") });
                else
                    foreach (var rule in rules)
                        InspectorPanel.Children.Add(Row(rule.Name, $"{rule.Percent}% → {rule.TargetFolio}"));
                break;
            case 5: // Approval
                InspectorPanel.Children.Add(STitle("Phê duyệt"));
                InspectorPanel.Children.Add(Row("Trạng thái", tx.Status == "Posted" ? "Đã duyệt" : "Chờ duyệt"));
                InspectorPanel.Children.Add(Row("Người duyệt", tx.Status == "Posted" ? tx.Operator : "—"));
                break;
            case 6: // History
                InspectorPanel.Children.Add(STitle("Lịch sử"));
                InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ {tx.Time:dd/MM/yyyy HH:mm} — Tạo bởi {tx.Operator}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
                if (tx.Status == "Reversed")
                    InspectorPanel.Children.Add(new TextBlock { Text = $"⏱ — Đảo ngược", FontSize = 11, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) });
                break;
        }
    }

    private string GetFolioCode(int folioId)
    {
        var f = _folios.FirstOrDefault(x => x.Id == folioId);
        return f?.Code ?? string.Empty;
    }

    private bool CanReverse(FwTransaction tx)
    {
        var folio = _folios.FirstOrDefault(x => x.Id == tx.FolioId);
        return _folioService is not null
            && tx.EntryId != Guid.Empty
            && folio is not null
            && folio.FolioGuid != Guid.Empty
            && folio.RowVersion is { Length: 8 };
    }

    private async Task OnReverseTransactionAsync(FwTransaction tx)
    {
        var folio = _folios.FirstOrDefault(x => x.Id == tx.FolioId);
        if (_folioService is null || folio is null || folio.FolioGuid == Guid.Empty || folio.RowVersion is not { Length: 8 } || tx.EntryId == Guid.Empty)
        {
            MessageBox.Show("Giao dịch hiện tại chưa có định danh Folio/rowversion thật để đảo ngược.", "Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn đảo ngược giao dịch {tx.Id} với số tiền {tx.Amount:N0} {tx.Currency}?\nThao tác sẽ tạo bút toán correction, không xóa lịch sử.",
            "Xác nhận đảo ngược Folio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var reason = PromptForReversalReason();
        if (string.IsNullOrWhiteSpace(reason)) return;

        try
        {
            var result = await _folioService.ReverseEntryAsync(
                new ReverseFolioEntryCommand(folio.FolioGuid, folio.RowVersion, tx.EntryId, reason),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể đảo ngược giao dịch.", "Folio", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _activeTxn = null;
            await LoadPersistedFoliosAsync();
            MessageBox.Show("Đã đảo ngược giao dịch và tải lại Folio từ cơ sở dữ liệu.", "Folio", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể đảo ngược giao dịch thật: " + ex.Message, "Folio", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string? PromptForReversalReason()
    {
        var dialog = new Window
        {
            Title = "Lý do đảo ngược",
            Width = 430,
            Height = 185,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        var input = new TextBox
        {
            Margin = new Thickness(0, 6, 0, 10),
            MinHeight = 52,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var ok = new Button { Content = "Xác nhận", Padding = new Thickness(12, 5, 12, 5), IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                MessageBox.Show("Vui lòng nhập lý do đảo ngược.", "Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
                input.Focus();
                return;
            }
            dialog.DialogResult = true;
        };
        dialog.Content = new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Lý do nghiệp vụ", FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") },
                    input,
                    ok
                }
            }
        };
        return dialog.ShowDialog() == true ? input.Text.Trim() : null;
    }

    // ═══════════════════════════════════════════
    // SIDE TAB CONTENT BUILDERS
    // ═══════════════════════════════════════════

    private void BuildOverview()
    {
        OverviewContent.Children.Clear();
        if (_activeFolio == null)
        {
            OverviewContent.Children.Add(new TextBlock { Text = "Chọn Folio để xem tổng quan.", FontSize = 13, Foreground = C("#94A3B8") });
            return;
        }
        var chargeTotal = _allTxns.Where(tx => tx.FolioId == _activeFolio.Id && tx.Type == "Charge").Sum(tx => (long)tx.Net);
        var payTotal = _allTxns.Where(tx => tx.FolioId == _activeFolio.Id && tx.Type == "Payment").Sum(tx => (long)tx.Net);
        var depTotal = _allTxns.Where(tx => tx.FolioId == _activeFolio.Id && tx.Type == "Deposit").Sum(tx => (long)tx.Net);
        var refTotal = _allTxns.Where(tx => tx.FolioId == _activeFolio.Id && tx.Type == "Refund").Sum(tx => (long)tx.Net);
        var balance = _activeFolio.Total - _activeFolio.Paid - _activeFolio.Deposit;

        var kpis = new (string, string, string)[] {
            ("Charge hôm nay", $"{chargeTotal:N0}", "#DC2626"),
            ("Thanh toán", $"{payTotal:N0}", "#16A34A"),
            ("Còn nợ", $"{Math.Max(0, balance):N0}", "#F59E0B"),
            ("Đặt cọc", $"{depTotal:N0}", "#8B5CF6"),
            ("Hoàn tiền", $"{refTotal:N0}", "#EC4899"),
            ("Hạn mức TD", $"{_activeFolio.CreditLimit:N0}", "#06B6D4")
        };
        var wp = new WrapPanel();
        foreach (var (label, value, color) in kpis)
            wp.Children.Add(Kpi(label, value, color));
        OverviewContent.Children.Add(wp);
    }

    private void BuildPaymentsTab()
    {
        PaymentContent.Children.Clear();
        var payments = _activeFolio != null ? _payments.Where(p => p.FolioId == _activeFolio.Id).ToList() : _payments;
        var dg = BuildPaymentGrid(payments);
        PaymentContent.Children.Add(dg);
    }

    private void BuildDepositsTab()
    {
        DepositContent.Children.Clear();
        var deposits = _activeFolio != null ? _deposits.Where(d => d.FolioId == _activeFolio.Id).ToList() : _deposits;

        if (_activeFolio is { } folio)
        {
            DepositContent.Children.Add(new TextBlock
            {
                Text = "Tổng hợp đặt cọc theo Folio",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = C("#17365D"),
                Margin = new Thickness(0, 0, 0, 6)
            });
            if (folio.DepositSummaryLoaded)
            {
                var summary = new WrapPanel();
                summary.Children.Add(Kpi("Tổng đặt cọc", $"{folio.Deposit:N0} {folio.Currency}", "#8B5CF6"));
                summary.Children.Add(Kpi("Đã phân bổ", $"{folio.DepositApplied:N0} {folio.Currency}", "#16A34A"));
                summary.Children.Add(Kpi("Chưa phân bổ", $"{folio.DepositRemaining:N0} {folio.Currency}", "#F59E0B"));
                DepositContent.Children.Add(summary);
            }
            else
            {
                DepositContent.Children.Add(new TextBlock
                {
                    Text = _depositSummaryLoadWarning ?? "Chưa tải được deposit summary thật; không suy diễn trạng thái phân bổ.",
                    FontSize = 11,
                    Foreground = C("#92400E"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }
            DepositContent.Children.Add(new TextBlock
            {
                Text = "Các khoản ghi nhận trên ledger",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = C("#17365D"),
                Margin = new Thickness(0, 6, 0, 6)
            });
        }

        var dg = BuildDepositGrid(deposits);
        DepositContent.Children.Add(dg);
    }

    private void BuildRefundsTab()
    {
        RefundContent.Children.Clear();
        var refunds = _activeFolio != null ? _refunds.Where(r => r.FolioId == _activeFolio.Id).ToList() : _refunds;
        var dg = BuildRefundGrid(refunds);
        RefundContent.Children.Add(dg);
    }

    private void BuildRoutingTab()
    {
        RoutingContent.Children.Clear();
        if (_routings.Count == 0)
        {
            RoutingContent.Children.Add(new TextBlock
            {
                Text = "Không có routing rule trong dữ liệu thật của property hiện tại.",
                FontSize = 12,
                Foreground = C("#64748B"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            });
            return;
        }
        foreach (var rule in _routings)
        {
            RoutingContent.Children.Add(new Border
            {
                Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 0, 4),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = rule.Name, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") },
                        new TextBlock { Text = $"{rule.SourceFolio} → {rule.TargetFolio} ({rule.Percent}%) {(rule.IsActive ? "✓ Đang hoạt động" : "○ Tắt")}", FontSize = 11, Foreground = rule.IsActive ? C("#16A34A") : C("#94A3B8"), Margin = new Thickness(0,2,0,0) }
                    }
                }
            });
        }
    }

    private void BuildHistoryTab()
    {
        HistoryContent.Children.Clear();
        var events = (_activeFolio is null ? _allTxns : _allTxns.Where(x => x.FolioId == _activeFolio.Id))
            .OrderByDescending(x => x.Time)
            .Take(20)
            .Select(x => (x.Time.ToLocalTime().ToString("dd/MM HH:mm"), x.Type, $"{x.Description} · {x.Amount:N0} {x.Currency}", x.Type == "Payment" ? "#16A34A" : x.Type == "Deposit" ? "#F59E0B" : "#DC2626"))
            .ToList();
        foreach (var (time, type, desc, color) in events)
        {
            HistoryContent.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8), Children = {
                new Border { Width = 3, Background = Cb(color), CornerRadius = new CornerRadius(2), Margin = new Thickness(0,0,8,0) },
                new StackPanel { Children = {
                    new TextBlock { Text = $"{time} · {type}", FontSize = 11, Foreground = C("#94A3B8") },
                    new TextBlock { Text = desc, FontSize = 12, Foreground = C("#17365D"), Margin = new Thickness(0,1,0,0) }
                }}
            }});
        }
    }

    private void BuildDocumentsTab()
    {
        DocumentContent.Children.Clear();
        if (_activeFolio is null)
        {
            DocumentContent.Children.Add(new TextBlock { Text = "Chọn một Folio để xem tài liệu thật.", FontSize = 12, Foreground = C("#94A3B8"), Margin = new Thickness(10), TextWrapping = TextWrapping.Wrap });
            return;
        }

        if (_businessDocumentService is null || _activeFolio.GuestProfileId is not Guid)
        {
            DocumentContent.Children.Add(new TextBlock
            {
                Text = "Folio chưa có GuestProfileId thật hoặc chưa đăng ký document service.",
                FontSize = 12,
                Foreground = C("#B45309"),
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        if (_folioDocuments.Count == 0)
        {
            DocumentContent.Children.Add(new TextBlock
            {
                Text = _documentLoadWarning ?? "Chưa có tài liệu thật được lưu cho khách của Folio này.",
                FontSize = 12,
                Foreground = C(_documentLoadWarning is null ? "#64748B" : "#B45309"),
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        foreach (var document in _folioDocuments)
        {
            var pathAvailable = !string.IsNullOrWhiteSpace(document.PhysicalFilePath) && File.Exists(document.PhysicalFilePath);
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var details = new StackPanel();
            details.Children.Add(new TextBlock { Text = document.Title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") });
            details.Children.Add(new TextBlock { Text = $"{document.FileName} · {document.FileSize:N0} bytes · {document.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}", FontSize = 10, Foreground = C("#64748B") });
            Grid.SetColumn(details, 0);
            row.Children.Add(details);
            var open = new Button { Content = pathAvailable ? "Mở" : "Không khả dụng", IsEnabled = pathAvailable, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(8, 0, 0, 0) };
            if (pathAvailable) open.Click += (_, _) => OpenDocument(document);
            Grid.SetColumn(open, 1);
            row.Children.Add(open);
            DocumentContent.Children.Add(new Border { Background = Cb("#FAFBFD"), BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Padding = new Thickness(9, 7, 9, 7), Child = row });
        }
    }

    private async Task LoadFolioDocumentsAsync(FwFolio folio)
    {
        _folioDocuments = Array.Empty<BusinessDocumentDto>();
        _documentLoadWarning = null;
        if (_businessDocumentService is null || folio.GuestProfileId is not Guid guestId)
        {
            if (_sideTab == 7) BuildDocumentsTab();
            return;
        }

        try
        {
            var documents = await _businessDocumentService.GetDocumentsByEntityAsync(BusinessEntityType.Customer, guestId);
            // A user can switch Folio while the database read is in flight.
            // Never paint the previous guest's documents into the newly selected Folio.
            if (_activeFolio?.GuestProfileId != guestId)
                return;
            _folioDocuments = documents;
        }
        catch (Exception ex)
        {
            if (_activeFolio?.GuestProfileId != guestId)
                return;
            _documentLoadWarning = "Không thể tải tài liệu Folio thật: " + ex.Message;
        }

        if (_sideTab == 7) BuildDocumentsTab();
    }

    private static void OpenDocument(BusinessDocumentDto document)
    {
        if (string.IsNullOrWhiteSpace(document.PhysicalFilePath) || !File.Exists(document.PhysicalFilePath))
            return;
        try
        {
            Process.Start(new ProcessStartInfo(document.PhysicalFilePath) { UseShellExecute = true });
        }
        catch
        {
            // The row remains visible as unavailable if the shell cannot open it.
        }
    }

    private DataGrid BuildPaymentGrid(List<FwPayment> payments)
    {
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = payments, MaxHeight = 400 };
        dg.Columns.Add(C("Receipt", "Receipt", 100));
        dg.Columns.Add(C("Reference", "Reference", 100));
        dg.Columns.Add(C("Method", "Method", 100));
        dg.Columns.Add(C("Amount", "AmountDisplay", 100));
        dg.Columns.Add(C("Currency", "Currency", 60));
        dg.Columns.Add(C("Approval", "Approval", 80));
        dg.Columns.Add(C("Time", "TimeDisplay", 120));
        dg.Columns.Add(C("Operator", "Operator", 90));
        return dg;
    }

    private DataGrid BuildDepositGrid(List<FwDeposit> deposits)
    {
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = deposits, MaxHeight = 400 };
        dg.Columns.Add(C("ID", "Id", 90));
        dg.Columns.Add(C("Amount ghi nhận", "AmountDisplay", 120));
        dg.Columns.Add(C("Currency", "Currency", 60));
        dg.Columns.Add(C("Time", "TimeDisplay", 120));
        return dg;
    }

    private DataGrid BuildRefundGrid(List<FwRefund> refunds)
    {
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = refunds, MaxHeight = 400 };
        dg.Columns.Add(C("ID", "Id", 90));
        dg.Columns.Add(C("Amount", "AmountDisplay", 110));
        dg.Columns.Add(C("Reason", "Reason", 150));
        dg.Columns.Add(C("Approved", "ApprovedDisplay", 70));
        dg.Columns.Add(C("Status", "Status", 80));
        dg.Columns.Add(C("Time", "TimeDisplay", 120));
        dg.Columns.Add(C("Operator", "Operator", 90));
        return dg;
    }

    private static DataGridTextColumn C(string h, string b, double w)
    {
        return new DataGridTextColumn { Header = h, Binding = new System.Windows.Data.Binding(b), Width = w };
    }

    // ═══════════════════════════════════════════
    // SUMMARY STRIPS
    // ═══════════════════════════════════════════

    private void UpdateSummaryStrip()
    {
        SummaryStrip.Children.Clear();
        if (_activeFolio == null)
        {
            SummaryStrip.Children.Add(new TextBlock { Text = "Chưa chọn Folio", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center });
            return;
        }
        var f = _activeFolio;
        var balance = f.Total - f.Paid - f.Deposit;
        SummaryStrip.Children.Add(Strip("Khách", f.GuestName, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", f.Room, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Res", f.Reservation, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Stay", f.Stay, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đến", f.Arrival, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đi", f.Departure, true)); SummaryStrip.Children.Add(Sep());
        if (!string.IsNullOrEmpty(f.Company)) { SummaryStrip.Children.Add(Strip("Cty", f.Company, true)); SummaryStrip.Children.Add(Sep()); }
        if (!string.IsNullOrEmpty(f.TA)) { SummaryStrip.Children.Add(Strip("TA", f.TA, true)); SummaryStrip.Children.Add(Sep()); }
        SummaryStrip.Children.Add(Strip("Folio", f.Code, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Balance", $"{balance:N0} {f.Currency}", balance <= 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đặt cọc", $"{f.Deposit:N0}", f.Deposit > 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Hạn mức", $"{f.CreditLimit:N0}", balance <= f.CreditLimit)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Tệ", f.Currency, true)); SummaryStrip.Children.Add(Sep());
        var ready = balance <= 0;
        SummaryStrip.Children.Add(new Border
        {
            Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"),
            CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2),
            Child = new TextBlock { Text = ready ? "Đã cân" : "Còn nợ", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") }
        });
    }

    private void UpdateBottomSummary()
    {
        BottomSummary.Children.Clear();
        var f = GetFilteredTxns();
        if (f.Count == 0) { BottomSummary.Children.Add(new TextBlock { Text = "Không có giao dịch.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        var charge = f.Where(tx => tx.Type == "Charge").Sum(tx => (long)tx.Net);
        var payment = -f.Where(tx => tx.Type == "Payment").Sum(tx => (long)tx.Net);
        var deposit = -f.Where(tx => tx.Type == "Deposit").Sum(tx => (long)tx.Net);
        var refund = f.Where(tx => tx.Type == "Refund").Sum(tx => (long)tx.Net);
        var tax = f.Sum(tx => (long)tx.Tax);
        var discount = f.Sum(tx => (long)tx.Discount);
        var sc = f.Sum(tx => (long)tx.ServiceCharge);
        var balance = charge + refund + tax + discount + sc - payment - deposit;

        BottomSummary.Children.Add(Btm("Charge", $"{charge:N0}", "#DC2626")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Payment", $"{payment:N0}", "#16A34A")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Deposit", $"{deposit:N0}", "#F59E0B")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Refund", $"{refund:N0}", "#8B5CF6")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Thuế", $"{tax:N0}", "#6366F1")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Discount", $"{discount:N0}", "#F97316")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Phí DV", $"{sc:N0}", "#14B8A6")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Balance", $"{balance:N0}", balance >= 0 ? "#16A34A" : "#DC2626"));
    }

    // ═══════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════

    private void OnApplyFilter(object s, RoutedEventArgs e)
    {
        var kw = FwSearchBox.Text?.ToLower();
        _filteredFolios.Clear();
        if (string.IsNullOrWhiteSpace(kw))
            _filteredFolios.AddRange(_folios);
        else
            _filteredFolios.AddRange(_folios.Where(f =>
                f.GuestName.ToLower().Contains(kw) ||
                f.Room.Contains(kw) ||
                f.Code.ToLower().Contains(kw) ||
                (f.Company?.ToLower().Contains(kw) ?? false) ||
                (f.TA?.ToLower().Contains(kw) ?? false)));
        RefreshFolioList();
        RefreshTransactions(true);
    }

    private void OnClearFilter(object s, RoutedEventArgs e)
    {
        FwSearchBox.Text = "";
        FwTypeFilter.SelectedIndex = 0;
        FwCurrFilter.SelectedIndex = 0;
        FwDeptFilter.SelectedIndex = 0;
        FwStatusFilter.SelectedIndex = 0;
        _ribbonFilter = "ALL";
        _filteredFolios.Clear();
        _filteredFolios.AddRange(_folios);
        _activeFolio = null;
        _activeTxn = null;
        RefreshFolioList();
        RefreshTransactions(true);
        BuildRibbon();
        UpdateSummaryStrip();
        UpdateInspector();
        UpdateBottomSummary();
        FooterText.Text = "Sẵn sàng · Chọn Folio để xem chi tiết tài chính";
    }

    private void OnTxnSelected(object s, SelectionChangedEventArgs e)
    {
        if (TxnGrid.SelectedItem is not FwTransaction txn) return;
        _activeTxn = txn;
        UpdateInspector();
    }

    private void OnTxnMouseDoubleClick(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TxnGrid.SelectedItem is not FwTransaction txn) return;
        // Switch inspector to General tab
        _inspectorSubTab = 0;
        BuildInspectorTabs();
        UpdateInspector();
    }

    private void OnTxnPrevPage(object s, RoutedEventArgs e) { if (_txnPage > 0) { _txnPage--; RefreshTransactions(false); } }
    private void OnTxnNextPage(object s, RoutedEventArgs e) { _txnPage++; RefreshTransactions(false); }
    private void OnTxnPageSizeChanged(object s, SelectionChangedEventArgs e)
    {
        _txnPageSize = TxnPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 };
        RefreshTransactions(true);
    }

    private async void OnRefresh(object s, RoutedEventArgs e)
    {
        _activeFolio = null;
        _activeTxn = null;
        _ribbonFilter = "ALL";
        _filteredFolios.Clear();
        _filteredFolios.AddRange(_folios);
        FwSearchBox.Text = "";
        FwTypeFilter.SelectedIndex = 0;
        FwCurrFilter.SelectedIndex = 0;
        FwDeptFilter.SelectedIndex = 0;
        FwStatusFilter.SelectedIndex = 0;
        RefreshFolioList();
        RefreshTransactions(true);
        BuildRibbon();
        UpdateSummaryStrip();
        UpdateInspector();
        UpdateBottomSummary();
        await LoadPersistedFoliosAsync();
    }

    private void OnPrint(object s, RoutedEventArgs e)
    {
        var rows = GetFilteredTxns();
        if (rows.Count == 0)
        {
            ShowDataAvailability("In Folio / Statement");
            return;
        }

        try
        {
            var dialog = new System.Windows.Controls.PrintDialog();
            if (dialog.ShowDialog() != true) return;

            var document = new FlowDocument
            {
                PagePadding = new Thickness(36),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 9
            };
            var title = _activeFolio is null
                ? "Folio statement — toàn bộ Folio"
                : $"Folio statement — {_activeFolio.Code} · {_activeFolio.GuestName}";
            document.Blocks.Add(new Paragraph(new Run(title)) { FontSize = 16, FontWeight = FontWeights.Bold });
            document.Blocks.Add(new Paragraph(new Run($"In lúc {DateTime.Now:dd/MM/yyyy HH:mm} · {rows.Count:N0} giao dịch")));

            var table = new Table { CellSpacing = 0 };
            var widths = new[] { 82d, 76d, 92d, 210d, 92d, 70d };
            foreach (var width in widths) table.Columns.Add(new TableColumn { Width = new GridLength(width) });
            var header = new TableRow();
            foreach (var text in new[] { "Thời gian", "Mã GD", "Loại", "Diễn giải", "Số tiền", "Tệ" })
                header.Cells.Add(PrintCell(text, true));
            table.RowGroups.Add(new TableRowGroup { Rows = { header } });
            var body = new TableRowGroup();
            foreach (var row in rows)
            {
                var printRow = new TableRow();
                printRow.Cells.Add(PrintCell(row.Time.ToString("dd/MM HH:mm")));
                printRow.Cells.Add(PrintCell(row.Id));
                printRow.Cells.Add(PrintCell(row.Type));
                printRow.Cells.Add(PrintCell(row.Description));
                printRow.Cells.Add(PrintCell(row.Net.ToString("N0")));
                printRow.Cells.Add(PrintCell(row.Currency));
                body.Rows.Add(printRow);
            }
            table.RowGroups.Add(body);
            document.Blocks.Add(table);
            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Folio statement");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể in Folio thật: " + ex.Message, "Folio", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExport(object s, RoutedEventArgs e)
    {
        var rows = GetFilteredTxns();
        if (rows.Count == 0)
        {
            ShowDataAvailability("Xuất CSV");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            DefaultExt = ".csv",
            AddExtension = true,
            FileName = $"folio-{_activeFolio?.Code ?? "all"}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Title = "Xuất giao dịch Folio"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Thoi gian,Ma GD,Tham chieu,Loai,Dien giai,Bo phan,Outlet,So luong,Don gia,So tien,Thue,Giam gia,Phi dich vu,Net,Tien te,Ty gia,Nguoi tra,Operator,Trang thai,Folio,Khach,Phong");
            foreach (var row in rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(row.Time.ToString("yyyy-MM-dd HH:mm:ss")), Csv(row.Id), Csv(row.Reference), Csv(row.Type),
                    Csv(row.Description), Csv(row.Dept), Csv(row.Outlet), row.Qty.ToString(), row.Price.ToString(),
                    row.Amount.ToString(), row.Tax.ToString(), row.Discount.ToString(), row.ServiceCharge.ToString(),
                    row.Net.ToString(), Csv(row.Currency), row.ExchangeRate.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Csv(row.Payer), Csv(row.Operator), Csv(row.Status), Csv(row.FolioCode), Csv(row.GuestName), Csv(row.Room)
                }));
            }
            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            FooterText.Text = $"Đã xuất {rows.Count:N0} giao dịch Folio thật: {Path.GetFileName(dialog.FileName)}";
            MessageBox.Show("Đã xuất dữ liệu Folio thật ra CSV.", "Folio", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xuất Folio thật: " + ex.Message, "Folio", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static TableCell PrintCell(string value, bool header = false) => new(new Paragraph(new Run(value ?? string.Empty)))
    {
        Padding = new Thickness(4),
        BorderBrush = Brushes.LightGray,
        BorderThickness = new Thickness(0, 0, 0.5, 0.5),
        FontWeight = header ? FontWeights.Bold : FontWeights.Normal
    };

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains(',', StringComparison.Ordinal) || text.Contains('"', StringComparison.Ordinal) || text.Contains('\n', StringComparison.Ordinal)
            ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : text;
    }

    private void OnClose(object s, RoutedEventArgs e) => Close();

    private void ShowDataAvailability(string action) => MessageBox.Show(
        $"{action} đã có dữ liệu Folio thật để xử lý: {_folios.Count} Folio, {_allTxns.Count} khoản mục. Bộ dựng file/máy in sẽ là bước xuất bản tiếp theo.",
        "Folio", MessageBoxButton.OK, MessageBoxImage.Information);

    // ═══════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════

    private static FrameworkElement Kpi(string l, string v, string color) => new Border
    {
        Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 8, 6),
        Child = new StackPanel
        {
            Children = { new TextBlock { Text = v, FontSize = 18, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } }
        }
    };

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRunsFw(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) });

    private static FrameworkElement Strip(string l, string v, bool ok)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0) };
        sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center });
        sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center });
        return sp;
    }

    private static FrameworkElement Btm(string l, string v, string color) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 12, Foreground = C("#64748B"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = v, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C(color), VerticalAlignment = VerticalAlignment.Center } } };

    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static TextBlock BtmSep() => new() { Text = "  ·  ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

// ═══════════════════════════════════════════
// DATA RECORDS
// ═══════════════════════════════════════════

internal record FwFolio(
    int Id, string Type, string GuestName, string Code, string Currency,
    int Total, int Deposit, int Paid, string Status,
    string Room, string Reservation, string Stay, string Arrival, string Departure,
    string? Company, string? TA, int CreditLimit, int HmLimit, Guid StayId = default,
    Guid FolioGuid = default, byte[]? RowVersion = null,
    int DepositApplied = 0, int DepositRemaining = 0, bool DepositSummaryLoaded = false,
    Guid? GuestProfileId = null
)
{
    public string PayerDisplay => !string.IsNullOrEmpty(Company) ? $"Cty: {Company}" : !string.IsNullOrEmpty(TA) ? $"TA: {TA}" : "Guest";
}

internal record FwTransaction(
    DateTime Time, string Id, string Reference, string Type, string Description,
    string Dept, string Outlet, int Qty, int Price, int Amount,
    int Tax, int Discount, int ServiceCharge, int Net, string Currency, decimal ExchangeRate,
    string Payer, string Operator, string Status, int BalanceAfter, string Remark,
    int FolioId, string GuestName, string Room, string Reservation, string FolioCode,
    Guid EntryId = default, byte[]? FolioRowVersion = null
)
{
    public string AmountDisplay => $"{Amount:N0}";
    public string NetDisplay => $"{Net:N0}";
    public string TimeDisplay => Time.ToString("dd/MM HH:mm");
    public string BalanceDisplay => $"{BalanceAfter:N0}";
    public string TaxDisplay => $"{Tax:N0}";
    public string DiscountDisplay => $"{Discount:N0}";
    public string ScDisplay => $"{ServiceCharge:N0}";
}

internal record FwPayment(string Id, string Receipt, string Reference, string Method, int Amount, string Currency, string Approval, int FolioId, DateTime Time, string Operator)
{
    public string AmountDisplay => $"{Amount:N0}";
    public string TimeDisplay => Time.ToString("dd/MM/yyyy HH:mm");
}

internal record FwDeposit(string Id, int Amount, string Currency, int FolioId, DateTime Time)
{
    public string AmountDisplay => $"{Amount:N0}";
    public string TimeDisplay => Time.ToString("dd/MM/yyyy HH:mm");
}

internal record FwRefund(string Id, int Amount, string Reason, bool Approved, string Status, int FolioId, DateTime Time, string Operator)
{
    public string AmountDisplay => $"{Amount:N0}";
    public string TimeDisplay => Time.ToString("dd/MM/yyyy HH:mm");
    public string ApprovedDisplay => Approved ? "✓" : "○";
}

internal record FwRoutingRule(string Name, string SourceFolio, string TargetFolio, string Unit, decimal Percent, bool IsActive);

internal static class FwExt2
{
    public static TextBlock WithRunsFw(this TextBlock tb, (SolidColorBrush, string, bool)[] runs)
    {
        foreach (var (b, t, w) in runs)
            tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal });
        return tb;
    }
}
