using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.App.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using AuditLogEntity = ASMILE.BO.Core.Entities.AuditLog;

namespace ASMILE.BO.App.Views;

public partial class ProformaFolioWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private PfFolio? _activeFolio;
    private readonly List<PfFolio> _folios;
    private readonly List<PfPosting> _postings;
    private readonly List<PfPayment> _payments;
    private readonly List<PfDeposit> _deposits;
    private readonly List<PfRefund> _refunds;
    private readonly Dictionary<Guid, DepositSummaryDto> _depositSummaries = new();
    private string _template = "Standard", _language = "Vi", _currency = "Original";
    private readonly List<PfPreviewRecord> _previewHistory = new();
    private readonly List<PfWarning> _warnings = new();
    private readonly List<PfWarning> _blocking = new();
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IHotelPaymentRepository? _paymentRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IEmailDispatchService? _emailDispatchService;
    private readonly IFolioRoutingQueryService? _routingQueryService;
    private readonly IAuditService? _auditService;
    private readonly IBusinessDocumentService? _businessDocumentService;
    private IReadOnlyList<FolioRoutingRuleDto> _routingRules = Array.Empty<FolioRoutingRuleDto>();
    private IReadOnlyList<AuditLogEntity> _folioAudit = Array.Empty<AuditLogEntity>();
    private string? _routingLoadWarning;
    private string? _auditLoadWarning;
    private IReadOnlyList<BusinessDocumentDto> _folioDocuments = Array.Empty<BusinessDocumentDto>();
    private bool _loadedRealData;
    private bool _paymentDetailsLoaded;
    private string? _paymentLoadWarning;
    private string _propertyCurrencyCode = "—";
    private int _inspectorTab;
    private int _detailsLoadVersion;

    public ProformaFolioWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner ? owner : null;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _paymentRepository = services?.GetService<IHotelPaymentRepository>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _emailDispatchService = services?.GetService<IEmailDispatchService>();
        _routingQueryService = services?.GetService<IFolioRoutingQueryService>();
        _auditService = services?.GetService<IAuditService>();
        _businessDocumentService = services?.GetService<IBusinessDocumentService>();
        _folios = new List<PfFolio>();
        _postings = new List<PfPosting>();
        _payments = new List<PfPayment>();
        _deposits = new List<PfDeposit>();
        _refunds = new List<PfRefund>();
        InitSideTabs();
        ShowSideContent(0);
        InitInspectorTabs();
        BuildTemplateBtns();
        BuildPreviewOptions();
        RefreshFolioList();
        UpdateSummaryStrip();
        FooterText.Text = "Đang tải Folio và payment detail từ dữ liệu thật...";
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
            _propertyCurrencyCode = string.IsNullOrWhiteSpace(context.Property.CurrencyCode)
                ? "—"
                : context.Property.CurrencyCode;
            _paymentDetailsLoaded = false;
            _paymentLoadWarning = null;
            var page = await _folioRepository.SearchAsync(
                new FolioSearchFilter(context.Property.PropertyId.Value, null, null, null, null, null, null, null),
                new PageRequest(1, 500), CancellationToken.None);
            var ledgers = page.Items.ToList();

            _folios.Clear();
            _postings.Clear();
            _payments.Clear();
            _deposits.Clear();
            _refunds.Clear();
            _depositSummaries.Clear();

            var paymentsByFolio = new Dictionary<Guid, IReadOnlyList<HotelPaymentDto>>();
            if (_paymentRepository is null)
            {
                _paymentLoadWarning = "Chưa đăng ký payment repository; chỉ hiển thị ledger Folio, không dựng payment detail.";
            }
            else
            {
                try
                {
                    // The repository uses the scoped EF DbContext. Keep these
                    // reads sequential so multiple Folio payment queries do
                    // not overlap on the same context.
                    foreach (var ledger in ledgers)
                    {
                        var payments = await _paymentRepository.GetForFolioAsync(
                            context.Property.PropertyId.Value,
                            ledger.HotelFolioId,
                            CancellationToken.None);
                        paymentsByFolio[ledger.HotelFolioId] = payments;
                        _depositSummaries[ledger.HotelFolioId] = await _paymentRepository.GetDepositSummaryAsync(
                            context.Property.PropertyId.Value,
                            ledger.HotelFolioId,
                            CancellationToken.None);
                    }
                    _paymentDetailsLoaded = true;
                }
                catch (Exception ex)
                {
                    _paymentLoadWarning = "Không thể tải payment detail thật: " + ex.Message;
                }
            }

            for (var index = 0; index < ledgers.Count; index++)
            {
                var ledger = ledgers[index];
                var id = index + 1;
                _folios.Add(MapPersistedFolio(
                    id,
                    ledger,
                    _propertyCurrencyCode,
                    _paymentDetailsLoaded && _depositSummaries.TryGetValue(ledger.HotelFolioId, out var depositSummary) ? depositSummary : null));
                foreach (var entry in ledger.Entries)
                    AddPersistedEntry(id, ledger, entry);
                if (paymentsByFolio.TryGetValue(ledger.HotelFolioId, out var payments))
                    AddPersistedPayments(id, payments);
            }

            _activeFolio = null;
            _loadedRealData = true;
            RefreshFolioList();
            UpdateSummaryStrip();
            UpdateFooter();
            BuildInspector();
            FooterText.Text = string.IsNullOrWhiteSpace(_paymentLoadWarning)
                ? $"Đã tải {ledgers.Count:N0} Folio và payment detail thật từ cơ sở dữ liệu"
                : $"Đã tải {ledgers.Count:N0} Folio thật. {_paymentLoadWarning}";
        }
        catch (Exception ex)
        {
            _loadedRealData = false;
            _paymentDetailsLoaded = false;
            _paymentLoadWarning = null;
            _folios.Clear();
            _postings.Clear();
            _payments.Clear();
            _deposits.Clear();
            _refunds.Clear();
            _depositSummaries.Clear();
            RefreshFolioList();
            UpdateSummaryStrip();
            FooterText.Text = "Không thể tải Folio thật: " + ex.Message;
        }
    }

    private static PfFolio MapPersistedFolio(int id, FolioLedgerDto ledger, string currency, DepositSummaryDto? depositSummary = null)
    {
        var total = ledger.Entries.Where(IsCharge).Sum(x => x.Amount);
        var deposit = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.Deposit).Sum(x => Math.Abs(x.Amount));
        var paid = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.Payment).Sum(x => Math.Abs(x.Amount));
        var refund = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.Refund).Sum(x => Math.Abs(x.Amount));
        return new PfFolio(
            id,
            ledger.FolioNumber,
            "Folio",
            ledger.GuestName,
            ledger.RoomNumber,
            ledger.ReservationNumber ?? "—",
            ledger.StayNumber,
            ledger.ArrivalDate?.ToString("dd/MM/yyyy") ?? "—",
            ledger.DepartureDate?.ToString("dd/MM/yyyy") ?? "—",
            ledger.CompanyName,
            ledger.TravelAgentName,
            currency,
            ToInt(total),
            ToInt(deposit),
            ToInt(paid),
            ToInt(refund),
            ledger.Status.ToString(),
            ledger.Entries.Count,
            false,
            ledger.HotelFolioId,
            ledger.GuestProfileId,
            ToInt(depositSummary?.AppliedDeposits ?? 0),
            ToInt(depositSummary?.RemainingDeposits ?? 0),
            depositSummary is not null);
    }

    private void AddPersistedEntry(int folioId, FolioLedgerDto ledger, HotelFolioEntryDto entry)
    {
        var amount = ToInt(entry.Amount);
        _postings.Add(new PfPosting(
            entry.SequenceNumber,
            $"{ledger.FolioNumber}-{entry.SequenceNumber:D4}",
            entry.CreatedAtUtc.ToLocalTime(),
            MapEntryType(entry.EntryType),
            entry.Description,
            "FO",
            1,
            amount,
            entry.EntryType == HotelFolioEntryType.Tax ? amount : 0,
            entry.EntryType == HotelFolioEntryType.ServiceCharge ? amount : 0,
            entry.EntryType == HotelFolioEntryType.Discount ? amount : 0,
            amount,
            _propertyCurrencyCode,
            folioId));

        var time = entry.CreatedAtUtc.ToLocalTime();
        if (entry.EntryType == HotelFolioEntryType.Deposit)
            _deposits.Add(new PfDeposit(entry.SequenceNumber, $"Ledger #{entry.SequenceNumber}", Math.Abs(amount), _propertyCurrencyCode, folioId, time));
        else if (entry.EntryType == HotelFolioEntryType.Refund)
            _refunds.Add(new PfRefund(entry.SequenceNumber, $"Ledger #{entry.SequenceNumber}", Math.Abs(amount), entry.Description, true, _propertyCurrencyCode, folioId, time));
    }

    private void AddPersistedPayments(int folioId, IReadOnlyList<HotelPaymentDto> payments)
    {
        foreach (var payment in payments)
        {
            _payments.Add(new PfPayment(
                _payments.Count + 1,
                payment.PaymentNumber,
                payment.PaymentMethod.ToString(),
                ToInt(payment.Amount),
                payment.Currency.ToString(),
                payment.Status.ToString(),
                folioId,
                payment.CreatedAtUtc.ToLocalTime(),
                "—"));
        }
    }

    private static bool IsCharge(HotelFolioEntryDto entry) => entry.EntryType is
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Tax or
        HotelFolioEntryType.ServiceCharge or HotelFolioEntryType.Discount or HotelFolioEntryType.Adjustment or
        HotelFolioEntryType.Correction or HotelFolioEntryType.ManualCharge or HotelFolioEntryType.ManualCredit;

    private static string MapEntryType(HotelFolioEntryType type) => type switch
    {
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.ManualCharge => "Charge",
        HotelFolioEntryType.Payment => "Payment",
        HotelFolioEntryType.Deposit => "Deposit",
        HotelFolioEntryType.Refund => "Refund",
        HotelFolioEntryType.Tax => "Tax",
        HotelFolioEntryType.ServiceCharge => "Service Charge",
        HotelFolioEntryType.Discount => "Discount",
        HotelFolioEntryType.Transfer => "Transfer",
        _ => "Adjustment"
    };

    private static int ToInt(decimal value) => value <= int.MinValue ? int.MinValue : value >= int.MaxValue ? int.MaxValue : (int)Math.Round(value, MidpointRounding.AwayFromZero);
    private static int Balance(PfFolio folio) => folio.Total - folio.Paid - folio.Deposit + folio.Refund;

    // Historical designer fixtures are retained for reference only. Production data
    // is loaded from IHotelFolioRepository and the current property context.
#if false

    private static List<PfFolio> LoadFolios()
    {
        var r = new Random(42);
        var names = new[] { "Nguyễn Văn An", "Trần Thị Bình" };
        var list = new List<PfFolio>();
        list.Add(new(1, "FOL-26001", "Master", names[0], "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", "Cty TNHH ABC", "TA Saigontourist", "VND", 15200000, 5000000, 12000000, 500000, "Open", 42, true));
        list.Add(new(2, "FOL-26002", "Room", names[0], "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", "Cty TNHH ABC", null, "VND", 7200000, 0, 5000000, 0, "Open", 18, true));
        list.Add(new(3, "FOL-26003", "Restaurant", names[0], "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", null, null, "VND", 3200000, 0, 2800000, 0, "Open", 12, true));
        list.Add(new(4, "FOL-26004", "Spa", names[0], "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", null, null, "VND", 1800000, 0, 1500000, 0, "Settled", 8, true));
        list.Add(new(5, "FOL-26007", "Company", names[0], "101", "RES-24001", "STY-001", "24/07/2026", "26/07/2026", "Cty TNHH ABC", null, "VND", 10400000, 0, 8000000, 0, "Open", 22, true));
        list.Add(new(6, "FOL-26009", "Master", names[1], "205", "RES-24002", "STY-002", "25/07/2026", "28/07/2026", null, "TA Viettravel", "USD", 850, 200, 600, 0, "Open", 15, true));
        list.Add(new(7, "FOL-26010", "Room", names[1], "205", "RES-24002", "STY-002", "25/07/2026", "28/07/2026", null, "TA Viettravel", "USD", 450, 0, 300, 0, "Open", 10, true));
        list.Add(new(8, "FOL-26013", "Incidentals", names[1], "205", "RES-24002", "STY-002", "25/07/2026", "28/07/2026", null, null, "USD", 120, 0, 0, 0, "Open", 6, false));
        return list;
    }

    private static List<PfPosting> LoadPostings()
    {
        var types = new[] { "Room Charge", "Room Charge", "Restaurant", "Spa", "Laundry", "Minibar", "Transport", "Telephone", "Tax", "Service Charge", "Discount", "Package", "Misc" };
        var depts = new[] { "FO", "F&B", "SPA", "LAUN", "MINI", "OTHER" };
        var r = new Random(99); var list = new List<PfPosting>(320);
        var d = new DateTime(2026, 7, 24);
        for (int i = 1; i <= 320; i++)
        {
            var t = types[r.Next(13)];
            var qty = r.Next(1, 5); var price = r.Next(50000, 3000000);
            var amount = price * qty;
            var tax = t is "Room Charge" or "Restaurant" or "Spa" ? (int)(amount * 0.08) : 0;
            var sc = t is "Room Charge" or "Restaurant" or "Spa" ? (int)(amount * 0.05) : 0;
            var disc = t is "Discount" ? -r.Next(20000, 200000) : 0;
            var net = amount + tax + sc + disc;
            var folioId = r.Next(1, 9);
            list.Add(new PfPosting(i, $"TXN-{29000 + i}", d.AddDays(r.Next(0, 4)), t, $"{t} #{i}", depts[r.Next(6)], r.Next(1, 6), price, tax, sc, disc, net, folioId <= 5 ? "VND" : "USD", folioId));
        }
        return list.OrderBy(p => p.Date).ToList();
    }

    private static List<PfPayment> LoadPayments()
    {
        var methods = new[] { "Tiền mặt", "Visa", "Mastercard", "Chuyển khoản", "QR Pay", "Ví điện tử" };
        var r = new Random(123); var list = new List<PfPayment>(80);
        for (int i = 1; i <= 80; i++) { var fid = r.Next(1, 9); list.Add(new(i, $"RCP-{26000 + i:D4}", methods[r.Next(6)], r.Next(200000, 5000000), fid <= 5 ? "VND" : "USD", r.Next(4) != 0 ? "Approved" : "Pending", fid, new DateTime(2026, 7, 24 + r.Next(4), r.Next(6, 22), 0, 0), new[] { "Thu ngân A", "Lễ tân A", "Lễ tân B" }[r.Next(3)])); }
        return list;
    }

    private static List<PfDeposit> LoadDeposits()
    {
        var r = new Random(456); var list = new List<PfDeposit>(30);
        for (int i = 1; i <= 30; i++) { var amt = r.Next(1000000, 8000000); var applied = r.Next(0, amt); var fid = r.Next(1, 9); list.Add(new(i, $"DEP-{25000 + i:D4}", amt, applied, amt - applied, fid <= 5 ? "VND" : "USD", fid, new DateTime(2026, 7, 23 + r.Next(3), r.Next(8, 20), 0, 0))); }
        return list;
    }

    private static List<PfRefund> LoadRefunds()
    {
        var reasons = new[] { "Check-out sớm", "Hủy dịch vụ", "Sai lệch giá", "Khiếu nại", "Đặt cọc dư" };
        var r = new Random(789); var list = new List<PfRefund>(20);
        for (int i = 1; i <= 20; i++) { var fid = r.Next(1, 9); list.Add(new(i, $"REF-{24000 + i:D4}", r.Next(100000, 2000000), reasons[r.Next(5)], r.Next(3) != 0, fid <= 5 ? "VND" : "USD", fid, new DateTime(2026, 7, 25 + r.Next(2), r.Next(8, 20), 0, 0))); }
        return list;
    }

    // ═══ SIDE TABS ═══

#endif

    private void InitSideTabs()
    {
        SideTabPanel.Children.Clear();
        var tabs = new[] { "Tổng quan", "Posting", "Thanh toán", "Đặt cọc", "Hoàn tiền", "Routing", "Tài liệu", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _sideTab; var b = SideTabBtn(tabs[i], active); b.MouseLeftButtonDown += (_, _) => SwitchSideTab(idx); SideTabPanel.Children.Add(b); }
    }

    private int _sideTab;
    private void SwitchSideTab(int idx)
    {
        _sideTab = idx; InitSideTabs();
        ShowSideContent(idx);
        if (idx == 5 && _activeFolio is not null) _ = LoadRoutingAsync(_activeFolio, _detailsLoadVersion);
        if (idx == 7 && _activeFolio is not null) _ = LoadFolioAuditAsync(_activeFolio, _detailsLoadVersion);
    }

    private Border SideTabBtn(string text, bool active) => new() { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 0, 0, 2), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), Child = new TextBlock { Text = text, FontSize = 12, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569") } };

    private void ShowSideContent(int tab)
    {
        OverviewArea.Visibility = tab == 0 ? Visibility.Visible : Visibility.Collapsed;
        PostingArea.Visibility = tab == 1 ? Visibility.Visible : Visibility.Collapsed;
        PaymentArea.Visibility = tab == 2 ? Visibility.Visible : Visibility.Collapsed;
        DepositArea.Visibility = tab == 3 ? Visibility.Visible : Visibility.Collapsed;
        RefundArea.Visibility = tab == 4 ? Visibility.Visible : Visibility.Collapsed;
        RoutingArea.Visibility = tab == 5 ? Visibility.Visible : Visibility.Collapsed;
        DocumentArea.Visibility = tab == 6 ? Visibility.Visible : Visibility.Collapsed;
        HistoryArea.Visibility = tab == 7 ? Visibility.Visible : Visibility.Collapsed;

        switch (tab)
        {
            case 0: BuildOverview(); break;
            case 1: BuildPostingTab(); break;
            case 2: BuildPaymentTab(); break;
            case 3: BuildDepositTab(); break;
            case 4: BuildRefundTab(); break;
            case 5: BuildRoutingTab(); break;
            case 6: BuildDocumentTab(); break;
            case 7: BuildHistoryTab(); break;
        }
    }

    // ═══ FOLIO BROWSER ═══

    private void RefreshFolioList()
    {
        FolioListPanel.Children.Clear();
        foreach (var f in _folios)
        {
            var active = _activeFolio?.Id == f.Id;
            var balance = Balance(f);
            var b = new Border { Background = active ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = active ? Cb("#BFDBFE") : Cb("#E2E8F0"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(10, 7, 10, 7), Cursor = System.Windows.Input.Cursors.Hand, Child = new StackPanel { Children = { new Grid { Children = { new TextBlock { Text = f.Type, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#1E40AF") : C("#17365D"), HorizontalAlignment = HorizontalAlignment.Left }, new TextBlock { Text = balance > 0 ? $"Còn nợ: {balance:N0}" : "Đã cân", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = balance > 0 ? C("#DC2626") : C("#16A34A"), HorizontalAlignment = HorizontalAlignment.Right } } }, new TextBlock { Text = $"{f.Code} · {f.Currency} · {f.PostingCount} posting", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 2, 0, 0) } } } };
            var fid = f.Id; b.MouseLeftButtonDown += (_, _) =>
            {
                _activeFolio = _folios.First(x => x.Id == fid);
                var loadVersion = ++_detailsLoadVersion;
                _routingRules = Array.Empty<FolioRoutingRuleDto>();
                _folioAudit = Array.Empty<AuditLogEntity>();
                _folioDocuments = Array.Empty<BusinessDocumentDto>();
                RefreshFolioList();
                UpdateSummaryStrip();
                BuildBillPreview();
                UpdateFooter();
                if (_sideTab == 6) BuildDocumentTab();
                _ = LoadSelectedFolioDetailsAsync(_activeFolio, loadVersion);
            };
            FolioListPanel.Children.Add(b);
        }
    }

    private void UpdateSummaryStrip()
    {
        SummaryStrip.Children.Clear();
        if (_activeFolio == null) { SummaryStrip.Children.Add(new TextBlock { Text = "Chưa chọn Folio", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return; }
        var f = _activeFolio; var bal = Balance(f);
        SummaryStrip.Children.Add(Strip("Khách", f.Guest, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", f.Room, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Res", f.Reservation, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đến", f.Arrival, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đi", f.Departure, true)); SummaryStrip.Children.Add(Sep());
        if (!string.IsNullOrEmpty(f.Company)) { SummaryStrip.Children.Add(Strip("Cty", f.Company, true)); SummaryStrip.Children.Add(Sep()); }
        if (!string.IsNullOrEmpty(f.TA)) { SummaryStrip.Children.Add(Strip("TA", f.TA, true)); SummaryStrip.Children.Add(Sep()); }
        SummaryStrip.Children.Add(Strip("Folio", f.Code, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Currency", f.Currency, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Balance", $"{bal:N0}", bal <= 0)); SummaryStrip.Children.Add(Sep());
        var ready = bal <= 0;
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Đã cân" : "Còn nợ", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    private void UpdateFooter()
    {
        if (_activeFolio == null) { FooterText.Text = "Sẵn sàng · Chọn Folio để xem trước hóa đơn"; return; }
        var f = _activeFolio; var posts = _postings.Where(p => p.FolioId == f.Id).ToList();
        var charge = posts.Where(p => p.Type != "Payment" && p.Type != "Deposit" && p.Type != "Refund").Sum(p => p.Net);
        var pay = _payments.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var dep = _deposits.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var refAmt = _refunds.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var bal = Balance(f);

        BottomSummary.Children.Clear();
        BottomSummary.Children.Add(Btm("Charge", $"{charge:N0}", "#DC2626")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Payment", _paymentDetailsLoaded ? $"{pay:N0}" : "Chưa đọc được", "#16A34A")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Deposit", $"{dep:N0}", "#F59E0B")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Refund", $"{refAmt:N0}", "#8B5CF6")); BottomSummary.Children.Add(BtmSep());
        BottomSummary.Children.Add(Btm("Balance", $"{bal:N0}", bal <= 0 ? "#16A34A" : "#DC2626"));
        FooterText.Text = $"Charge: {charge:N0} · Payment: {(_paymentDetailsLoaded ? $"{pay:N0}" : "Chưa đọc được")} · Deposit: {dep:N0} · Balance: {bal:N0} {f.Currency}";
    }

    // ═══ BILL PREVIEW ═══

    private void BuildTemplateBtns()
    {
        TemplatePanel.Children.Clear();
        var temps = new[] { "Standard", "Compact", "Detailed", "Corporate", "TA", "Group" };
        foreach (var t in temps) { var active = _template == t; var b = new Button { Content = t, Height = 28, FontSize = 11, FontWeight = FontWeights.SemiBold, Background = active ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = active ? Cb("White") : C("#475569"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 4, 0), Template = FindResource("RibbonBtn") as ControlTemplate }; b.Click += (_, _) => { _template = t; BuildTemplateBtns(); BuildBillPreview(); }; TemplatePanel.Children.Add(b); }
    }

    private void BuildPreviewOptions()
    {
        LangPanel.Children.Clear();
        var langs = new[] { ("Tiếng Việt", "Vi"), ("English", "En"), ("Song ngữ", "Bi") };
        foreach (var (label, key) in langs) { var active = _language == key; var b = new Button { Content = label, Height = 28, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Background = active ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = active ? Cb("White") : C("#475569"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(8, 0, 8, 0), Margin = new Thickness(0, 0, 4, 0), Template = FindResource("RibbonBtn") as ControlTemplate }; b.Click += (_, _) => { _language = key; BuildPreviewOptions(); BuildBillPreview(); }; LangPanel.Children.Add(b); }

        CurrPanel.Children.Clear();
        var currs = new[] { ("Gốc", "Original"), ("VND", "VND"), ("USD", "USD") };
        foreach (var (label, key) in currs) { var active = _currency == key; var b2 = new Button { Content = label, Height = 28, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Background = active ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = active ? Cb("White") : C("#475569"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(8, 0, 8, 0), Margin = new Thickness(0, 0, 4, 0), Template = FindResource("RibbonBtn") as ControlTemplate }; b2.Click += (_, _) => { _currency = key; BuildPreviewOptions(); BuildBillPreview(); }; CurrPanel.Children.Add(b2); }
    }

    private void BuildBillPreview()
    {
        BillPanel.Children.Clear();
        if (_activeFolio == null) { BillPanel.Children.Add(new TextBlock { Text = "Chọn Folio để xem trước hóa đơn.", FontSize = 14, Foreground = C("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0) }); return; }
        var f = _activeFolio; var posts = _postings.Where(p => p.FolioId == f.Id).ToList();

        // Hotel header
        BillPanel.Children.Add(new TextBlock { Text = "Thông tin khách sạn", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 2) });
        BillPanel.Children.Add(new TextBlock { Text = "Địa chỉ/MST lấy từ cấu hình property khi có provider thật.", FontSize = 11, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center });
        BillPanel.Children.Add(new TextBlock { Text = "IN TẠM TÍNH / PROFORMA FOLIO", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = C("#2F6BFF"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 10) });

        // Guest info grid
        var gi = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        gi.ColumnDefinitions.Add(new ColumnDefinition()); gi.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < 6; i++) { gi.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); }
        AddGi(gi, 0, 0, "Khách:", f.Guest); AddGi(gi, 1, 0, "Phòng:", f.Room);
        AddGi(gi, 2, 0, "Reservation:", f.Reservation); AddGi(gi, 3, 0, "Stay:", f.Stay);
        AddGi(gi, 0, 1, "Đến:", f.Arrival); AddGi(gi, 1, 1, "Đi:", f.Departure);
        AddGi(gi, 2, 1, "Công ty:", f.Company ?? "—"); AddGi(gi, 3, 1, "Đại lý:", f.TA ?? "—");
        AddGi(gi, 4, 0, "Folio:", f.Code); AddGi(gi, 4, 1, "Currency:", f.Currency);
        AddGi(gi, 5, 0, "Template:", _template); AddGi(gi, 5, 1, "Ngôn ngữ:", _language);
        BillPanel.Children.Add(gi);

        BillPanel.Children.Add(new Border { Height = 2, Background = Cb("#2F6BFF"), Margin = new Thickness(0, 4, 0, 8) });

        // Posting table header
        var tbl = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        tbl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        tbl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        tbl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        tbl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        tbl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        tbl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        var thStyle = new TextBlock { FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(2, 3, 2, 3) };
        AddTbl(tbl, 0, "Ngày", true); AddTbl(tbl, 1, "Diễn giải", true); AddTbl(tbl, 2, "SL", true); AddTbl(tbl, 3, "Đơn giá", true); AddTbl(tbl, 4, "Thuế/Phí", true); AddTbl(tbl, 5, "Thành tiền", true);
        BillPanel.Children.Add(tbl);

        BillPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 0, 0, 4) });

        // Posting rows
        foreach (var p in posts.Take(20))
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            AddTbl(row, 0, p.Date.ToString("dd/MM"), false);
            AddTbl(row, 1, p.Description, false);
            AddTbl(row, 2, $"{p.Qty}", false);
            AddTbl(row, 3, $"{p.Price:N0}", false);
            AddTbl(row, 4, $"{p.Tax + p.Sc:N0}", false);
            AddTbl(row, 5, $"{p.Net:N0}", false);
            BillPanel.Children.Add(row);
            if (posts.IndexOf(p) == 19 && posts.Count > 20) BillPanel.Children.Add(new TextBlock { Text = $"... và {posts.Count - 20} posting khác", FontSize = 10, Foreground = C("#94A3B8"), FontStyle = FontStyles.Italic, Margin = new Thickness(0, 2, 0, 4) });
        }

        BillPanel.Children.Add(new Border { Height = 1, Background = Cb("#2F6BFF"), Margin = new Thickness(0, 6, 0, 8) });

        // Totals
        var charge = posts.Where(p => p.Type is not "Payment" and not "Deposit" and not "Refund").Sum(p => p.Net);
        var tax = posts.Sum(p => p.Tax); var sc = posts.Sum(p => p.Sc); var disc = posts.Sum(p => p.Disc);
        var pay = _payments.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var dep = _deposits.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var refAmt = _refunds.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var balance = Balance(f);

        BillPanel.Children.Add(TotalRow("Tổng Charge", $"{charge:N0}", false));
        BillPanel.Children.Add(TotalRow("Thuế", $"{tax:N0}", false));
        BillPanel.Children.Add(TotalRow("Phí dịch vụ", $"{sc:N0}", false));
        BillPanel.Children.Add(TotalRow("Giảm giá", $"{disc:N0}", disc < 0));
        BillPanel.Children.Add(TotalRow("Tổng thanh toán", _paymentDetailsLoaded ? $"{pay:N0}" : "Chưa đọc được", false));
        BillPanel.Children.Add(TotalRow("Đặt cọc", $"{dep:N0}", false));
        BillPanel.Children.Add(TotalRow("Hoàn tiền", $"{refAmt:N0}", false));
        BillPanel.Children.Add(new Border { Height = 1, Background = Cb("#2F6BFF"), Margin = new Thickness(0, 4, 0, 6) });
        BillPanel.Children.Add(TotalRow("CÒN LẠI", $"{balance:N0} {f.Currency}", true));
        if (balance <= 0) BillPanel.Children.Add(new TextBlock { Text = "✓ Đã thanh toán đầy đủ", FontSize = 11, Foreground = C("#16A34A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });

        UpdateFooter();
    }

    private static void AddGi(Grid g, int row, int col, string label, string value)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        sp.Children.Add(new TextBlock { Text = label + " ", FontSize = 11, Foreground = C("#94A3B8") });
        sp.Children.Add(new TextBlock { Text = value, FontSize = 11, Foreground = C("#17365D"), FontWeight = FontWeights.SemiBold });
        Grid.SetRow(sp, row); Grid.SetColumn(sp, col); g.Children.Add(sp);
    }

    private static void AddTbl(Grid g, int col, string text, bool isHeader)
    {
        var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal, Foreground = isHeader ? C("#102E5A") : C("#334155"), Margin = new Thickness(2, 2, 2, 2) };
        Grid.SetColumn(tb, col); g.Children.Add(tb);
    }

    private FrameworkElement TotalRow(string label, string value, bool bold)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 2, 0, 0) };
        sp.Children.Add(new TextBlock { Text = label + ": ", FontSize = 12, Foreground = bold ? C("#102E5A") : C("#64748B"), FontWeight = bold ? FontWeights.Bold : FontWeights.Normal });
        sp.Children.Add(new TextBlock { Text = value, FontSize = 12, Foreground = bold ? C("#102E5A") : C("#17365D"), FontWeight = bold ? FontWeights.Bold : FontWeights.SemiBold });
        return sp;
    }

    // ═══ INSPECTOR ═══

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Posting", "TT toán", "Đặt cọc", "Hoàn tiền", "Routing", "Tài liệu", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _inspectorTab; var b = new Border { CornerRadius = new CornerRadius(7, 7, 0, 0), Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 1, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("White") : Cb("Transparent"), BorderBrush = active ? Cb("#E2E8F0") : Cb("Transparent"), BorderThickness = new Thickness(1, 1, 1, 0), Child = new TextBlock { Text = tabs[i], FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#2F6BFF") : C("#64748B") } }; b.MouseLeftButtonDown += (_, _) => { _inspectorTab = idx; InitInspectorTabs(); BuildInspector(); }; InspectorTabs.Children.Add(b); }
        BuildInspector();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_activeFolio == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn Folio để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        var f = _activeFolio; var charge = _postings.Where(p => p.FolioId == f.Id && p.Type != "Payment" && p.Type != "Deposit" && p.Type != "Refund").Sum(p => p.Net);
        var pay = _payments.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var dep = _deposits.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var refAmt = _refunds.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);

        switch (_inspectorTab)
        {
            case 0:
                InspectorPanel.Children.Add(STitle("Tổng quan tài chính"));
                InspectorPanel.Children.Add(Row("Charge", $"{charge:N0}"));
                InspectorPanel.Children.Add(Row("Payment", _paymentDetailsLoaded ? $"{pay:N0}" : "Chưa đọc được"));
                InspectorPanel.Children.Add(Row("Deposit", $"{dep:N0}"));
                InspectorPanel.Children.Add(Row("Refund", $"{refAmt:N0}"));
                var bal = Balance(f);
                InspectorPanel.Children.Add(new Border { Background = bal <= 0 ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = bal <= 0 ? $"✓ Đã cân (dư: {Math.Abs(bal):N0})" : $"Còn nợ: {bal:N0} {f.Currency}", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = bal <= 0 ? C("#16A34A") : C("#DC2626") } });
                break;
            case 1:
                InspectorPanel.Children.Add(STitle("Posting"));
                foreach (var p in _postings.Where(p => p.FolioId == f.Id).Take(15))
                    InspectorPanel.Children.Add(new TextBlock { Text = $"{p.Date:dd/MM} · {p.Description} · {p.Net:N0}", FontSize = 11, Foreground = C("#334155"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 2:
                InspectorPanel.Children.Add(STitle("Thanh toán"));
                foreach (var p in _payments.Where(p => p.FolioId == f.Id).Take(10))
                    InspectorPanel.Children.Add(new TextBlock { Text = $"{p.Receipt} · {p.Method} · {p.Amount:N0} · {p.Approval}", FontSize = 11, Foreground = C("#334155"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 3:
                InspectorPanel.Children.Add(STitle("Đặt cọc"));
                if (f.DepositSummaryLoaded)
                {
                    InspectorPanel.Children.Add(Row("Tổng đặt cọc", $"{f.Deposit:N0} {f.Currency}"));
                    InspectorPanel.Children.Add(Row("Đã phân bổ", $"{f.DepositApplied:N0} {f.Currency}"));
                    InspectorPanel.Children.Add(Row("Chưa phân bổ", $"{f.DepositRemaining:N0} {f.Currency}"));
                }
                else
                {
                    InspectorPanel.Children.Add(new TextBlock { Text = "Chưa tải được deposit summary thật; không suy diễn trạng thái phân bổ.", FontSize = 11, Foreground = C("#B45309"), TextWrapping = TextWrapping.Wrap });
                }
                foreach (var d in _deposits.Where(p => p.FolioId == f.Id).Take(8))
                    InspectorPanel.Children.Add(new TextBlock { Text = $"{d.Code} · {d.Amount:N0} {d.Currency} · Khoản ghi nhận trên ledger", FontSize = 11, Foreground = C("#334155"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 4:
                InspectorPanel.Children.Add(STitle("Hoàn tiền"));
                foreach (var rf in _refunds.Where(p => p.FolioId == f.Id).Take(5))
                    InspectorPanel.Children.Add(new TextBlock { Text = $"{rf.Code} · {rf.Reason} · {rf.Amount:N0} · {(rf.Approved ? "✓" : "○")}", FontSize = 11, Foreground = C("#334155"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 5:
                InspectorPanel.Children.Add(STitle("Routing"));
                InspectorPanel.Children.Add(Row("Company", f.Company ?? "—"));
                InspectorPanel.Children.Add(Row("TA", f.TA ?? "—"));
                InspectorPanel.Children.Add(Row("Master", f.Type == "Master" ? "Có" : "Chưa có dữ liệu routing"));
                break;
            case 6:
                InspectorPanel.Children.Add(STitle("Tài liệu"));
                InspectorPanel.Children.Add(new TextBlock { Text = "Chưa có chứng từ proforma thật được lưu.", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 2, 0, 0) });
                break;
            case 7:
                InspectorPanel.Children.Add(STitle("Lịch sử xem trước"));
                if (_previewHistory.Count == 0)
                    InspectorPanel.Children.Add(new TextBlock { Text = "Chưa có lịch sử in tạm tính.", FontSize = 11, Foreground = C("#94A3B8") });
                else foreach (var h in _previewHistory)
                        InspectorPanel.Children.Add(new TextBlock { Text = $"{h.Time:dd/MM HH:mm} · {h.Template} · {h.Lang} · {h.Operator}", FontSize = 11, Foreground = C("#334155"), Margin = new Thickness(0, 2, 0, 0) });
                break;
        }
    }

    // ═══ SIDE TAB CONTENT ═══

    private void BuildOverview()
    {
        OverviewPanel.Children.Clear();
        if (_activeFolio == null) return;
        var f = _activeFolio; var charge = _postings.Where(p => p.FolioId == f.Id && p.Type != "Payment" && p.Type != "Deposit" && p.Type != "Refund").Sum(p => p.Net);
        var pay = _payments.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var dep = _deposits.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var refAmt = _refunds.Where(p => p.FolioId == f.Id).Sum(p => p.Amount);
        var bal = Balance(f);
        var kpis = new (string, string, string)[] { ("Charge", $"{charge:N0}", "#DC2626"), ("Payment", _paymentDetailsLoaded ? $"{pay:N0}" : "Chưa đọc được", "#16A34A"), ("Deposit", $"{dep:N0}", "#F59E0B"), ("Refund", $"{refAmt:N0}", "#8B5CF6"), ("Balance", $"{bal:N0}", bal <= 0 ? "#16A34A" : "#DC2626") };
        foreach (var (l, v, c) in kpis) OverviewPanel.Children.Add(Kpi(l, v, c));
    }

    private void BuildPostingTab()
    {
        PostingTabPanel.Children.Clear();
        if (_activeFolio == null) return;
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = _postings.Where(p => p.FolioId == _activeFolio.Id).Take(50).ToList() };
        dg.Columns.Add(C("Ngày", "Date", 70)); dg.Columns.Add(C("Diễn giải", "Description", 120));
        dg.Columns.Add(C("SL", "Qty", 35)); dg.Columns.Add(C("ĐG", "Price", 70));
        dg.Columns.Add(C("Thuế", "Tax", 55)); dg.Columns.Add(C("Phí", "Sc", 50)); dg.Columns.Add(C("Net", "Net", 85));
        PostingTabPanel.Children.Add(dg);
    }

    private void BuildPaymentTab()
    {
        PaymentTabPanel.Children.Clear();
        if (_activeFolio == null) return;
        if (!_paymentDetailsLoaded)
        {
            PaymentTabPanel.Children.Add(new TextBlock
            {
                Text = _paymentLoadWarning ?? "Chưa đọc được payment detail thật; không hiển thị dữ liệu thay thế.",
                FontSize = 11,
                Foreground = C("#92400E"),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = _payments.Where(p => p.FolioId == _activeFolio.Id).ToList() };
        dg.Columns.Add(C("Receipt", "Receipt", 90)); dg.Columns.Add(C("Phương thức", "Method", 100));
        dg.Columns.Add(C("Số tiền", "Amount", 90)); dg.Columns.Add(C("Duyệt", "Approval", 70)); dg.Columns.Add(C("Operator", "Operator", 80));
        PaymentTabPanel.Children.Add(dg);
    }

    private void BuildDepositTab()
    {
        DepositTabPanel.Children.Clear();
        if (_activeFolio == null) return;

        var folio = _activeFolio;
        DepositTabPanel.Children.Add(STitle("Tổng hợp đặt cọc theo Folio"));
        if (folio.DepositSummaryLoaded)
        {
            DepositTabPanel.Children.Add(Row("Tổng đặt cọc", $"{folio.Deposit:N0} {folio.Currency}"));
            DepositTabPanel.Children.Add(Row("Đã phân bổ", $"{folio.DepositApplied:N0} {folio.Currency}"));
            DepositTabPanel.Children.Add(Row("Chưa phân bổ", $"{folio.DepositRemaining:N0} {folio.Currency}"));
        }
        else
        {
            DepositTabPanel.Children.Add(new TextBlock
            {
                Text = _paymentLoadWarning ?? "Chưa tải được deposit summary thật; không suy diễn trạng thái phân bổ.",
                FontSize = 11,
                Foreground = C("#92400E"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        DepositTabPanel.Children.Add(STitle("Các khoản ghi nhận trên ledger"));
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = _deposits.Where(p => p.FolioId == _activeFolio.Id).ToList() };
        dg.Columns.Add(C("Mã", "Code", 120));
        dg.Columns.Add(C("Thời gian", "Time", 125));
        dg.Columns.Add(C("Số tiền ghi nhận", "Amount", 110));
        dg.Columns.Add(C("Tiền tệ", "Currency", 70));
        DepositTabPanel.Children.Add(dg);
    }

    private void BuildRefundTab()
    {
        RefundTabPanel.Children.Clear();
        if (_activeFolio == null) return;
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = _refunds.Where(p => p.FolioId == _activeFolio.Id).ToList() };
        dg.Columns.Add(C("Mã", "Code", 85)); dg.Columns.Add(C("Lý do", "Reason", 120)); dg.Columns.Add(C("Số tiền", "Amount", 90)); dg.Columns.Add(C("Duyệt", "Approved", 50));
        RefundTabPanel.Children.Add(dg);
    }

    private void BuildRoutingTab()
    {
        RoutingTabPanel.Children.Clear();
        if (_activeFolio == null) return;
        RoutingTabPanel.Children.Add(new TextBlock { Text = "Routing", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 6) });
        if (_routingQueryService is null)
        {
            RoutingTabPanel.Children.Add(new TextBlock { Text = "Chưa đăng ký Folio routing service trong phiên hiện tại.", FontSize = 11, Foreground = C("#B45309"), TextWrapping = TextWrapping.Wrap });
            return;
        }
        if (_routingRules.Count == 0)
        {
            RoutingTabPanel.Children.Add(new TextBlock
            {
                Text = _routingLoadWarning ?? "Không có routing rule đang hoạt động cho Folio này trong dữ liệu thật.",
                FontSize = 11,
                Foreground = C(_routingLoadWarning is null ? "#64748B" : "#B45309"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
            return;
        }
        foreach (var rule in _routingRules)
        {
            RoutingTabPanel.Children.Add(new Border
            {
                Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(7), Padding = new Thickness(9, 7, 9, 7), Margin = new Thickness(0, 0, 0, 5),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = rule.Name, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") },
                        new TextBlock { Text = $"{rule.SourceFolioNumber} → {rule.TargetFolioNumber} · {rule.EntryType} · {rule.Percent:N2}% · Ưu tiên {rule.Priority}", FontSize = 10, Foreground = C("#475569"), Margin = new Thickness(0, 2, 0, 0) },
                        new TextBlock { Text = string.IsNullOrWhiteSpace(rule.Notes) ? "Đang hoạt động" : $"Đang hoạt động · {rule.Notes}", FontSize = 10, Foreground = C("#16A34A"), Margin = new Thickness(0, 2, 0, 0), TextWrapping = TextWrapping.Wrap }
                    }
                }
            });
        }
    }

    private void BuildDocumentTab()
    {
        DocumentTabPanel.Children.Clear();
        if (_activeFolio is null)
        {
            DocumentTabPanel.Children.Add(new TextBlock { Text = "Chọn một Folio để xem tài liệu thật.", FontSize = 11, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap });
            return;
        }

        if (_businessDocumentService is null || _activeFolio.GuestProfileId is null)
        {
            DocumentTabPanel.Children.Add(new TextBlock { Text = "Folio chưa có GuestProfileId thật hoặc chưa đăng ký document service.", FontSize = 11, Foreground = C("#B45309"), TextWrapping = TextWrapping.Wrap });
            return;
        }

        if (_folioDocuments.Count == 0)
        {
            DocumentTabPanel.Children.Add(new TextBlock { Text = "Chưa có tài liệu thật được lưu cho khách của Folio này.", FontSize = 11, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap });
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
            DocumentTabPanel.Children.Add(new Border { Background = Cb("#F8FAFC"), BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Padding = new Thickness(9, 7, 9, 7), Child = row });
        }
    }

    private void BuildHistoryTab()
    {
        HistoryTabPanel.Children.Clear();
        foreach (var h in _folioAudit)
            HistoryTabPanel.Children.Add(new TextBlock { Text = $"{h.CreatedAt:dd/MM/yyyy HH:mm} · {h.Action} · {h.UserName ?? h.Username ?? "—"}", FontSize = 11, Foreground = C("#17365D"), Margin = new Thickness(0, 2, 0, 0) });
        foreach (var h in _previewHistory)
            HistoryTabPanel.Children.Add(new TextBlock { Text = $"{h.Time:dd/MM HH:mm} · {h.Template} · {h.Lang} · {h.Currency} · {h.Operator}", FontSize = 11, Foreground = C("#334155"), Margin = new Thickness(0, 2, 0, 0) });
        if (_folioAudit.Count == 0 && _previewHistory.Count == 0)
            HistoryTabPanel.Children.Add(new TextBlock { Text = _auditLoadWarning ?? "Chưa có lịch sử.", FontSize = 11, Foreground = C(_auditLoadWarning is null ? "#94A3B8" : "#B45309"), TextWrapping = TextWrapping.Wrap });
    }

    private async Task LoadSelectedFolioDetailsAsync(PfFolio folio, int loadVersion)
    {
        await LoadRoutingAsync(folio, loadVersion);
        if (!IsCurrentDetailLoad(folio, loadVersion)) return;
        await LoadFolioAuditAsync(folio, loadVersion);
        if (!IsCurrentDetailLoad(folio, loadVersion)) return;
        await LoadFolioDocumentsAsync(folio, loadVersion);
    }

    private bool IsCurrentDetailLoad(PfFolio folio, int loadVersion) =>
        loadVersion == _detailsLoadVersion && _activeFolio?.Id == folio.Id;

    private async Task LoadFolioDocumentsAsync(PfFolio folio, int loadVersion)
    {
        _folioDocuments = Array.Empty<BusinessDocumentDto>();
        if (_businessDocumentService is null || folio.GuestProfileId is not Guid guestId)
        {
            if (_sideTab == 6) BuildDocumentTab();
            return;
        }

        try
        {
            var documents = await _businessDocumentService.GetDocumentsByEntityAsync(BusinessEntityType.Customer, guestId);
            // A user can switch Folio while the database read is in flight.
            // Do not render the previous guest's documents into the new Folio.
            if (!IsCurrentDetailLoad(folio, loadVersion) || _activeFolio?.GuestProfileId != guestId)
                return;
            _folioDocuments = documents;
        }
        catch (Exception ex)
        {
            if (!IsCurrentDetailLoad(folio, loadVersion) || _activeFolio?.GuestProfileId != guestId)
                return;
            FooterText.Text = "Không thể tải tài liệu Folio thật: " + ex.Message;
        }
        if (_sideTab == 6) BuildDocumentTab();
    }

    private void OpenDocument(BusinessDocumentDto document)
    {
        if (string.IsNullOrWhiteSpace(document.PhysicalFilePath) || !File.Exists(document.PhysicalFilePath)) return;
        try
        {
            Process.Start(new ProcessStartInfo(document.PhysicalFilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            FooterText.Text = "Không thể mở tài liệu Folio: " + ex.Message;
        }
    }

    private async Task LoadRoutingAsync(PfFolio folio, int loadVersion)
    {
        _routingRules = Array.Empty<FolioRoutingRuleDto>();
        _routingLoadWarning = null;
        if (_routingQueryService is null || folio.HotelFolioId is not Guid sourceFolioId || _contextAccessor?.Current is not { } context)
        {
            _routingLoadWarning = "Chưa đủ context để đọc routing rule Folio thật.";
            if (_sideTab == 5) BuildRoutingTab();
            return;
        }

        try
        {
            var rules = await _routingQueryService.SearchAsync(
                new FolioRoutingSearchRequest(context.Property.PropertyId.Value, sourceFolioId, ActiveOnly: true),
                CancellationToken.None);
            if (!IsCurrentDetailLoad(folio, loadVersion)) return;
            _routingRules = rules;
        }
        catch (Exception ex)
        {
            if (!IsCurrentDetailLoad(folio, loadVersion)) return;
            _routingLoadWarning = "Không thể tải routing rule thật: " + ex.Message;
        }
        if (_sideTab == 5) BuildRoutingTab();
    }

    private async Task LoadFolioAuditAsync(PfFolio folio, int loadVersion)
    {
        _folioAudit = Array.Empty<AuditLogEntity>();
        _auditLoadWarning = null;
        if (_auditService is null || folio.HotelFolioId is not Guid folioId)
        {
            _auditLoadWarning = "Chưa đăng ký audit service hoặc Folio chưa có định danh thật.";
            if (_sideTab == 7) BuildHistoryTab();
            return;
        }

        try
        {
            var audit = await _auditService.QueryAsync(
                entityName: "HotelFolio",
                entityId: folioId.ToString("D"),
                fromDate: DateTime.UtcNow.AddDays(-90),
                toDate: DateTime.UtcNow,
                cancellationToken: CancellationToken.None);
            if (!IsCurrentDetailLoad(folio, loadVersion)) return;
            _folioAudit = audit;
        }
        catch (Exception ex)
        {
            if (!IsCurrentDetailLoad(folio, loadVersion)) return;
            _auditLoadWarning = "Không thể tải lịch sử Folio thật: " + ex.Message;
        }
        if (_sideTab == 7) BuildHistoryTab();
    }

    private static DataGridTextColumn C(string h, string b, double w) => new() { Header = h, Binding = new System.Windows.Data.Binding(b), Width = w };

    private static FrameworkElement Kpi(string l, string v, string color) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 6, 4), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };

    // ═══ ACTIONS ═══

    private void OnPreview(object s, RoutedEventArgs e)
    {
        if (_activeFolio is null)
        {
            ShowToast("Hãy chọn một Folio thật trước khi tạo preview.");
            return;
        }

        BuildBillPreview();
        _previewHistory.Add(new PfPreviewRecord(DateTime.Now, _template, _language, _currency, "Lễ tân A"));
        BuildHistoryTab();
        BuildInspector();
        ShowToast(_loadedRealData
            ? "Đã tạo preview từ dữ liệu Folio thật. Preview chưa được lưu thành chứng từ."
            : "Chưa có Folio thật để tạo preview.");
    }

    private void OnPrint(object s, RoutedEventArgs e)
    {
        if (_activeFolio is null)
        {
            ShowToast("Hãy chọn một Folio thật trước khi in.");
            return;
        }

        var content = new StringBuilder()
            .AppendLine("PHIẾU FOLIO TẠM TÍNH")
            .AppendLine($"Folio: {_activeFolio.Code} | Khách: {_activeFolio.Guest} | Phòng: {_activeFolio.Room}")
            .AppendLine($"Đặt phòng: {_activeFolio.Reservation} | Lưu trú: {_activeFolio.Stay}")
            .AppendLine($"Tổng: {_activeFolio.Total:N0} {_activeFolio.Currency} | Đặt cọc: {_activeFolio.Deposit:N0} | Đã phân bổ: {(_activeFolio.DepositSummaryLoaded ? _activeFolio.DepositApplied.ToString("N0") : "Chưa xác định")} | Chưa phân bổ: {(_activeFolio.DepositSummaryLoaded ? _activeFolio.DepositRemaining.ToString("N0") : "Chưa xác định")} | Đã thu: {_activeFolio.Paid:N0} | Hoàn: {_activeFolio.Refund:N0}")
            .AppendLine(new string('-', 100));
        foreach (var posting in _postings.Where(x => x.FolioId == _activeFolio.Id))
            content.AppendLine($"POSTING | {posting.Date:dd/MM/yyyy HH:mm} | {posting.Code} | {posting.Description} | SL {posting.Qty} | Thành tiền {posting.Net:N0} {posting.Currency}");
        foreach (var payment in _payments.Where(x => x.FolioId == _activeFolio.Id))
            content.AppendLine($"THU | {payment.Time:dd/MM/yyyy HH:mm} | {payment.Receipt} | {payment.Method} | {payment.Amount:N0} {payment.Currency}");
        foreach (var deposit in _deposits.Where(x => x.FolioId == _activeFolio.Id))
            content.AppendLine($"CỌC | {deposit.Time:dd/MM/yyyy HH:mm} | {deposit.Code} | Số tiền ghi nhận {deposit.Amount:N0} {deposit.Currency}");
        foreach (var refund in _refunds.Where(x => x.FolioId == _activeFolio.Id))
            content.AppendLine($"HOÀN | {refund.Time:dd/MM/yyyy HH:mm} | {refund.Code} | {refund.Amount:N0} {refund.Currency} | {refund.Reason}");

        new PrintPreviewDialog("Phiếu Folio tạm tính", content.ToString()).ShowDialog();
    }

    private void OnExport(object s, RoutedEventArgs e)
    {
        if (_activeFolio is null)
        {
            ShowToast("Hãy chọn một Folio thật trước khi xuất.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Xuất Folio tạm tính",
            Filter = "CSV UTF-8 (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            FileName = $"folio-tam-tinh-{_activeFolio.Code}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            AddExtension = true,
            DefaultExt = ".csv",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Nhóm,Mã,Ngày,Diễn giải,Số lượng,Thành tiền,Tiền tệ,Trạng thái");
            foreach (var posting in _postings.Where(x => x.FolioId == _activeFolio.Id))
                csv.AppendLine(string.Join(",", Csv("Posting"), Csv(posting.Code), Csv(posting.Date.ToString("yyyy-MM-dd HH:mm:ss")), Csv(posting.Description), Csv(posting.Qty.ToString()), Csv(posting.Net.ToString()), Csv(posting.Currency), Csv(posting.Type)));
            foreach (var payment in _payments.Where(x => x.FolioId == _activeFolio.Id))
                csv.AppendLine(string.Join(",", Csv("Payment"), Csv(payment.Receipt), Csv(payment.Time.ToString("yyyy-MM-dd HH:mm:ss")), Csv(payment.Method), Csv("1"), Csv(payment.Amount.ToString()), Csv(payment.Currency), Csv(payment.Approval)));
            foreach (var deposit in _deposits.Where(x => x.FolioId == _activeFolio.Id))
                csv.AppendLine(string.Join(",", Csv("Deposit"), Csv(deposit.Code), Csv(deposit.Time.ToString("yyyy-MM-dd HH:mm:ss")), Csv("Đặt cọc ghi nhận trên ledger"), Csv("1"), Csv(deposit.Amount.ToString()), Csv(deposit.Currency), Csv("Chưa phân bổ theo từng khoản")));
            if (_activeFolio.DepositSummaryLoaded)
                csv.AppendLine(string.Join(",", Csv("DepositSummary"), Csv("SUMMARY"), Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), Csv("Tổng hợp theo Folio"), Csv("1"), Csv(_activeFolio.Deposit.ToString()), Csv(_activeFolio.Currency), Csv($"Đã phân bổ {_activeFolio.DepositApplied:N0}; Chưa phân bổ {_activeFolio.DepositRemaining:N0}")));
            foreach (var refund in _refunds.Where(x => x.FolioId == _activeFolio.Id))
                csv.AppendLine(string.Join(",", Csv("Refund"), Csv(refund.Code), Csv(refund.Time.ToString("yyyy-MM-dd HH:mm:ss")), Csv(refund.Reason), Csv("1"), Csv(refund.Amount.ToString()), Csv(refund.Currency), Csv(refund.Approved ? "Đã duyệt" : "Chờ duyệt")));
            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ShowToast($"Đã xuất dữ liệu Folio thật: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            ShowToast("Không thể xuất Folio thật: " + ex.Message);
        }
    }
    private async void OnEmail(object s, RoutedEventArgs e)
    {
        if (_activeFolio is null)
        {
            ShowToast("Hãy chọn một Folio thật trước khi gửi email.");
            return;
        }

        if (_emailDispatchService is null)
        {
            ShowToast("Chưa cấu hình email outbox trong phiên hiện tại.");
            return;
        }

        var input = PromptForEmail();
        if (input is null) return;

        try
        {
            var result = await _emailDispatchService.QueueAsync(new EmailDispatchRequestDto
            {
                Recipient = input.Value.Recipient,
                Subject = input.Value.Subject,
                Body = input.Value.Body,
                SourceType = "ProformaFolio",
                SourceId = null
            }, CancellationToken.None);
            MessageBox.Show($"Đã lưu email Folio vào hàng đợi thật ({result.Id:N}). Email sẽ được gửi khi SMTP được bật.", "Email Folio", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể lưu email Folio: " + ex.Message, "Email Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private (string Recipient, string Subject, string Body)? PromptForEmail()
    {
        var recipient = new TextBox { Margin = new Thickness(0, 4, 0, 10) };
        var subject = new TextBox { Text = $"Folio {_activeFolio?.Code}", Margin = new Thickness(0, 4, 0, 10) };
        var body = new TextBox { Text = $"Kính gửi {_activeFolio?.Guest},\n\nFolio: {_activeFolio?.Code}\nPhòng: {_activeFolio?.Room}\nTổng tiền: {_activeFolio?.Total:N0} {_activeFolio?.Currency}\n\nTrân trọng,\nASMILE BO Front Office", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 130, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 4, 0, 12) };
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "Email người nhận" }); panel.Children.Add(recipient);
        panel.Children.Add(new TextBlock { Text = "Tiêu đề" }); panel.Children.Add(subject);
        panel.Children.Add(new TextBlock { Text = "Nội dung" }); panel.Children.Add(body);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Hủy", IsCancel = true, Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 8, 0) };
        var send = new Button { Content = "Đưa vào hàng đợi", IsDefault = true, Padding = new Thickness(14, 6, 14, 6) };
        buttons.Children.Add(cancel); buttons.Children.Add(send); panel.Children.Add(buttons);
        var dialog = new Window { Title = "Gửi email Folio", Width = 560, Height = 430, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, Content = panel, ResizeMode = ResizeMode.NoResize };
        var accepted = false;
        send.Click += (_, _) => { accepted = true; dialog.Close(); };
        dialog.ShowDialog();
        if (!accepted || string.IsNullOrWhiteSpace(recipient.Text)) return null;
        return (recipient.Text.Trim(), subject.Text.Trim(), body.Text);
    }
    private async void OnRefresh(object s, RoutedEventArgs e)
    {
        _activeFolio = null;
        _previewHistory.Clear();
        RefreshFolioList();
        UpdateSummaryStrip();
        UpdateFooter();
        BillPanel.Children.Clear();
        BuildInspector();
        await LoadPersistedFoliosAsync();
    }
    private void OnClose(object s, RoutedEventArgs e) => Close();

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private void ShowToast(string msg) => MessageBox.Show($"═══ IN TẠM TÍNH ═══\n\n{msg}", "In tạm tính", MessageBoxButton.OK, MessageBoxImage.Information);

    // ═══ HELPERS ═══

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsPf(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static FrameworkElement Btm(string l, string v, string c) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 12, Foreground = C("#64748B"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = v, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C(c), VerticalAlignment = VerticalAlignment.Center } } };
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static TextBlock BtmSep() => new() { Text = "  ·  ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

// ═══ DATA RECORDS ═══

internal record PfFolio(int Id, string Code, string Type, string Guest, string Room, string Reservation, string Stay, string Arrival, string Departure, string? Company, string? TA, string Currency, int Total, int Deposit, int Paid, int Refund, string Status, int PostingCount, bool CanPrint, Guid? HotelFolioId = null, Guid? GuestProfileId = null, int DepositApplied = 0, int DepositRemaining = 0, bool DepositSummaryLoaded = false);
internal record PfPosting(int Id, string Code, DateTime Date, string Type, string Description, string Dept, int Qty, int Price, int Tax, int Sc, int Disc, int Net, string Currency, int FolioId);
internal record PfPayment(int Id, string Receipt, string Method, int Amount, string Currency, string Approval, int FolioId, DateTime Time, string Operator);
internal record PfDeposit(int Id, string Code, int Amount, string Currency, int FolioId, DateTime Time);
internal record PfRefund(int Id, string Code, int Amount, string Reason, bool Approved, string Currency, int FolioId, DateTime Time);
internal record PfPreviewRecord(DateTime Time, string Template, string Lang, string Currency, string Operator);
internal record PfWarning(string Level, string Message);

internal static class PfExt { public static TextBlock WithRunsPf(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
