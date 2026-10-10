using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Folio;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class SplitFolioWindow : Window
{
    private readonly IServiceScope? _dataScope;
    // ── Wizard State ──
    private int _step = 1;
    private SfFolio? _previewSource, _selectedSource;
    private readonly List<SfFolio> _allFolios;
    private readonly List<SfItem> _allItems;
    private readonly HashSet<int> _selectedItemIds = new();
    private SfItem? _previewItem;
    private readonly Dictionary<int, SfPartialSplit> _partialSplits = new();
    private readonly List<SfFolio> _targetFolios = new();
    private string _splitRuleType = "ByItem";
    private readonly Dictionary<int, int> _itemTargetMap = new(); // itemId → targetFolioId
    private double _splitRatio = 100;
    private int _splitFixedAmount;
    private readonly List<SfReconciliation> _reconciliation = new();
    private readonly List<SfWarning> _warnings = new();
    private readonly List<SfWarning> _blocking = new();
    private int _sourcePage, _sourcePageSize = 20;
    private int _itemPage, _itemPageSize = 20;
    private readonly HotelFolioApplicationService? _folioService;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private bool _persistedMode;

    // ── Constructor ──
    public SplitFolioWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner ? owner : null;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioService = services?.GetService<HotelFolioApplicationService>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _persistedMode = false;
        _allFolios = new List<SfFolio>();
        _allItems = new List<SfItem>();
        InitFilters();
        GoTo(1);
        Loaded += async (_, _) => await LoadPersistedFoliosAsync();
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async Task LoadPersistedFoliosAsync()
    {
        if (_folioService is null || _contextAccessor?.Current is not { } context)
        {
            _persistedMode = false;
            MessageBox.Show("Chưa cấu hình được Folio service và property context dữ liệu thật.", "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _persistedMode = true;

        try
        {
            var page = await _folioService!.SearchAsync(
                new SearchFoliosQuery(
                    new FolioSearchFilter(context.Property.PropertyId.Value, null, null, null, null, null, null, null),
                    new PageRequest(1, 500)),
                CancellationToken.None);
            if (page.IsFailure || page.Value is null)
            {
                _allFolios.Clear();
                _allItems.Clear();
                GoTo(1);
                MessageBox.Show(page.Error?.Message ?? "Không thể tải Folio thật.", "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _allFolios.Clear();
            _allItems.Clear();
            var currency = string.IsNullOrWhiteSpace(context.Property.CurrencyCode) ? "VND" : context.Property.CurrencyCode;
            var id = 0;
            foreach (var ledger in page.Value.Items)
            {
                var folioId = ++id;
                _allFolios.Add(MapPersistedFolio(folioId, ledger, currency));
                var itemId = 0;
                foreach (var entry in ledger.Entries)
                    _allItems.Add(MapPersistedItem(folioId, ++itemId, ledger, entry, currency));
            }

            _selectedSource = null;
            _previewSource = null;
            _selectedItemIds.Clear();
            _partialSplits.Clear();
            _targetFolios.Clear();
            GoTo(1);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải Folio thật: " + ex.Message, "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static SfFolio MapPersistedFolio(int id, FolioLedgerDto ledger, string currency)
    {
        var charges = ledger.Entries.Where(IsCharge).Sum(x => x.Amount);
        var deposits = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.Deposit).Sum(x => -x.Amount);
        var payments = ledger.Entries.Where(x => x.EntryType == HotelFolioEntryType.Payment).Sum(x => -x.Amount);
        var payer = !string.IsNullOrWhiteSpace(ledger.CompanyName) ? "Company" : !string.IsNullOrWhiteSpace(ledger.TravelAgentName) ? "TA" : "Guest";
        var billingInstruction = !string.IsNullOrWhiteSpace(ledger.CompanyName)
            ? "Route to Company"
            : !string.IsNullOrWhiteSpace(ledger.TravelAgentName) ? "Route to TA" : "Self-pay";
        return new SfFolio(
            Id: id, Code: ledger.FolioNumber, Type: "Folio", GuestName: ledger.GuestName, Room: ledger.RoomNumber, Reservation: ledger.ReservationNumber ?? "—", Stay: ledger.StayNumber,
            Arrival: ledger.ArrivalDate?.ToString("dd/MM/yyyy") ?? "—", Departure: ledger.DepartureDate?.ToString("dd/MM/yyyy") ?? "—", Total: ToInt(charges), Deposit: ToInt(deposits), Paid: ToInt(payments),
            Currency: string.IsNullOrWhiteSpace(currency) ? "—" : currency, Status: ledger.Status.ToString(), Payer: payer, Company: ledger.CompanyName, TA: ledger.TravelAgentName, CreditLimit: 0,
            ItemCount: ledger.Entries.Count, LockedCount: 0, DisputedCount: 0, BillingInstruction: billingInstruction, OpenDate: ledger.OpenedAtUtc?.ToLocalTime().ToString("dd/MM/yyyy") ?? "—",
            HasSettlement: ledger.Status is HotelFolioStatus.Closed or HotelFolioStatus.Settled,
            HasNightAuditLock: false, FolioGuid: ledger.HotelFolioId, RowVersion: ledger.RowVersion);
    }

    private static SfItem MapPersistedItem(int folioId, int itemId, FolioLedgerDto ledger, HotelFolioEntryDto entry, string currency)
    {
        var amount = ToInt(entry.Amount);
        var isLocked = entry.ReversesEntryId is not null || entry.ReversedByEntryId is not null;
        var isPayment = entry.EntryType is HotelFolioEntryType.Payment or HotelFolioEntryType.Deposit;
        var isRefund = entry.EntryType == HotelFolioEntryType.Refund;
        return new SfItem(
            itemId, entry.BusinessDate?.ToDateTime(TimeOnly.MinValue) ?? entry.CreatedAtUtc.ToLocalTime().Date,
            entry.CreatedAtUtc.ToLocalTime(), $"TXN-{entry.HotelFolioEntryId:N}"[..Math.Min(40, $"TXN-{entry.HotelFolioEntryId:N}".Length)],
            DisplayEntryType(entry.EntryType), entry.Description, entry.ReferenceType ?? "FO", ledger.RoomNumber, ledger.GuestName, "Guest",
            1, Math.Abs(amount), entry.EntryType == HotelFolioEntryType.Tax ? amount : 0,
            entry.EntryType == HotelFolioEntryType.ServiceCharge ? amount : 0, 0, amount,
            string.IsNullOrWhiteSpace(currency) ? "—" : currency, isLocked ? "Locked" : ledger.Status.ToString(),
            isLocked ? "Blocked" : IsCharge(entry) ? "Full" : isPayment || isRefund ? "Blocked" : "Full", folioId, null,
            isPayment, isLocked, false, IsCharge(entry) && amount > 0 && !isLocked, entry.HotelFolioEntryId);
    }

    private static string DisplayEntryType(HotelFolioEntryType type) => type switch
    {
        HotelFolioEntryType.RoomCharge => "Room Charge",
        HotelFolioEntryType.Restaurant => "Restaurant",
        HotelFolioEntryType.Laundry => "Laundry",
        HotelFolioEntryType.MiniBar => "Minibar",
        HotelFolioEntryType.Spa => "Spa",
        HotelFolioEntryType.Telephone => "Telephone",
        HotelFolioEntryType.Tax => "Tax",
        HotelFolioEntryType.ServiceCharge => "Service Charge",
        HotelFolioEntryType.Deposit => "Deposit",
        HotelFolioEntryType.Payment => "Payment",
        HotelFolioEntryType.Refund => "Refund",
        _ => type.ToString()
    };

    private static bool IsCharge(HotelFolioEntryDto entry) => entry.EntryType is
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Laundry or
        HotelFolioEntryType.MiniBar or HotelFolioEntryType.Spa or HotelFolioEntryType.Telephone or
        HotelFolioEntryType.OtherCharge or HotelFolioEntryType.Tax or HotelFolioEntryType.ServiceCharge or
        HotelFolioEntryType.Adjustment or HotelFolioEntryType.Correction or HotelFolioEntryType.ManualCharge;

    private static int ToInt(decimal value) => decimal.ToInt32(decimal.Round(value, 0, MidpointRounding.AwayFromZero));

    // ═══════════════════════════════════════════
    // Historical designer fixtures are retained below for reference only.
    // The production wizard loads Folios and entries from HotelFolioApplicationService.
#if false
    // ═══════════════════════════════════════════

    private static List<SfFolio> LoadFolios()
    {
        var names = new[] { "Nguyễn Văn An", "Trần Thị Bình", "Lê Hoàng Cường", "Phạm Minh Đức", "Hoàng Thị Em", "Vũ Quang Phúc", "Đặng Kim Giang", "Bùi Thanh Hà" };
        var types = new[] { "Master", "Room", "Restaurant", "Spa", "Laundry", "Extra", "Company", "Agent", "Guest", "City Ledger" };
        var currencies = new[] { "VND", "VND", "VND", "USD", "EUR" };
        var statuses = new[] { "Open", "Open", "Open", "Settled", "Locked", "Disputed", "Closed", "Pending" };
        var payers = new[] { "Guest", "Company", "TA", "House" };
        var companies = new[] { null, null, null, "Cty TNHH ABC", "Cty CP XYZ", "Tập đoàn DEF" };
        var tas = new[] { null, null, null, null, "TA Saigontourist", "TA Viettravel" };
        var r = new Random(42);
        var list = new List<SfFolio>(120);
        for (int i = 1; i <= 120; i++)
        {
            var total = r.Next(1000000, 20000000);
            var paid = r.Next(0, total);
            var deposit = r.Next(0, Math.Min(5000000, total - paid));
            list.Add(new SfFolio(
                Id: i, Code: $"FOL-{24000 + i}", Type: types[r.Next(10)], GuestName: names[r.Next(8)] + (i > 8 ? $" {i}" : ""),
                Room: $"{r.Next(1, 6)}{r.Next(1, 9):D2}", Reservation: $"RES-{24000 + r.Next(1, 20):D4}",
                Stay: $"STY-{24000 + r.Next(1, 20):D4}", Arrival: $"{(24 - r.Next(5)):D2}/07/2026", Departure: $"{(26 + r.Next(3)):D2}/07/2026",
                Total: total, Deposit: deposit, Paid: paid,
                Currency: currencies[r.Next(5)], Status: statuses[r.Next(8)],
                Payer: payers[r.Next(4)], Company: companies[r.Next(6)], TA: tas[r.Next(6)],
                CreditLimit: r.Next(5000000, 30000000), ItemCount: r.Next(0, 45),
                LockedCount: r.Next(0, 3), DisputedCount: r.Next(0, 2),
                BillingInstruction: r.Next(3) == 0 ? "Route to Company" : r.Next(3) == 0 ? "Self-pay" : "Standard",
                OpenDate: $"{(24 - r.Next(5)):D2}/07/2026", HasSettlement: r.Next(10) == 0, HasNightAuditLock: r.Next(15) == 0
            ));
        }
        return list;
    }

    private static List<SfItem> LoadItems()
    {
        var types = new[] { "Room Charge", "Room Charge", "Minibar", "Restaurant", "Room Service", "Laundry", "Spa", "Telephone", "Transport", "Package", "Tax", "Service Charge", "Discount", "Adjustment", "Deposit", "Payment", "Refund", "Voucher", "Other" };
        var depts = new[] { "FO", "F&B", "SPA", "LAUN", "MINI", "OTHER" };
        var guests = new[] { "Nguyễn Văn An", "Trần Thị Bình", "Lê Hoàng Cường" };
        var payers = new[] { "Guest", "Company", "TA" };
        var splitFlags = new[] { "Full", "Full", "Full", "Partial", "Blocked", "Disputed", "PackageParent", "PackageChild" };
        var r = new Random(99);
        var list = new List<SfItem>(350);
        var d = new DateTime(2026, 7, 24);
        for (int i = 1; i <= 350; i++)
        {
            var t = types[r.Next(19)];
            var qty = t is "Room Charge" ? 1 : r.Next(1, 6);
            var price = t switch
            {
                "Room Charge" => r.Next(800000, 3600000),
                "Minibar" => r.Next(20000, 200000),
                "Restaurant" => r.Next(80000, 800000),
                "Room Service" => r.Next(50000, 500000),
                "Laundry" => r.Next(30000, 300000),
                "Spa" => r.Next(100000, 1500000),
                "Transport" => r.Next(100000, 600000),
                "Package" => r.Next(500000, 3000000),
                _ => r.Next(50000, 1000000)
            };
            var amount = price * qty;
            var tax = t is "Room Charge" or "Restaurant" or "Spa" or "Minibar" ? (int)(amount * 0.08) : 0;
            var sc = t is "Room Charge" or "Restaurant" or "Spa" ? (int)(amount * 0.05) : 0;
            var disc = t is "Discount" ? -r.Next(20000, 200000) : 0;
            var net = amount + tax + sc + disc;
            var folioId = r.Next(1, 121);
            var splitFlag = splitFlags[r.Next(8)];

            list.Add(new SfItem(
                Id: i, Date: d.AddDays(r.Next(0, 5)), Time: d.AddHours(r.Next(6, 23)).AddMinutes(r.Next(0, 59)),
                Code: $"TXN-{27000 + i}", Type: t, Description: $"{t} #{i}",
                Dept: depts[r.Next(6)], Room: $"{r.Next(1, 6)}{r.Next(1, 9):D2}",
                Guest: guests[r.Next(3)], Payer: payers[r.Next(3)],
                Qty: qty, Price: price, Tax: tax, ServiceCharge: sc, Discount: disc, Net: net,
                Currency: folioId > 80 && r.Next(3) == 0 ? "USD" : "VND",
                Status: splitFlag == "Blocked" ? "Locked" : splitFlag == "Disputed" ? "Disputed" : r.Next(5) == 0 ? "Settled" : "Open",
                SplitFlag: splitFlag, FolioId: folioId,
                PackageGroupId: splitFlag is "PackageParent" or "PackageChild" ? r.Next(1, 20) : null,
                IsPaid: r.Next(6) == 0, IsLocked: splitFlag == "Blocked",
                IsDisputed: splitFlag == "Disputed", CanSplit: splitFlag is "Full" or "Partial"
            ));
        }
        return list;
    }

#endif

    private void InitFilters()
    {
        var statusItems = new[] { "Tất cả", "Open", "Settled", "Locked", "Disputed", "Closed" };
        FolioTypeFilter.ItemsSource = new[] { "Tất cả", "Master", "Room", "Restaurant", "Spa", "Laundry", "Extra", "Company", "Agent", "Guest", "City Ledger" };
        FolioTypeFilter.SelectedIndex = 0;
        FolioCurrFilter.ItemsSource = new[] { "Tất cả", "VND", "USD", "EUR" };
        FolioCurrFilter.SelectedIndex = 0;
        FolioStatusFilter.ItemsSource = statusItems;
        FolioStatusFilter.SelectedIndex = 0;
        SourcePageSizeBox.ItemsSource = new[] { "20 / trang", "50 / trang", "100 / trang" };
        SourcePageSizeBox.SelectedIndex = 0;

        ItemTypeFilter.ItemsSource = new[] { "Tất cả", "Room Charge", "Minibar", "Restaurant", "Room Service", "Laundry", "Spa", "Transport", "Package", "Tax", "Service Charge", "Discount", "Deposit", "Payment", "Refund" };
        ItemTypeFilter.SelectedIndex = 0;
        ItemDeptFilter.ItemsSource = new[] { "Tất cả", "FO", "F&B", "SPA", "LAUN", "MINI" };
        ItemDeptFilter.SelectedIndex = 0;
        ItemSplitFilter.ItemsSource = new[] { "Tất cả", "Có thể tách", "Tách một phần", "Bị khóa", "Đã thanh toán" };
        ItemSplitFilter.SelectedIndex = 0;
        ItemPageSizeBox.ItemsSource = new[] { "20 / trang", "50 / trang", "100 / trang" };
        ItemPageSizeBox.SelectedIndex = 0;
    }

    // ═══════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════

    private void GoTo(int s)
    {
        _step = Math.Clamp(s, 1, 6);
        BuildStepBar();
        HideAllSteps();
        switch (_step)
        {
            case 1: StepAContent.Visibility = Visibility.Visible; break;
            case 2: StepBContent.Visibility = Visibility.Visible; RenderStepB(); break;
            case 3: StepCContent.Visibility = Visibility.Visible; RenderStepC(); break;
            case 4: StepDContent.Visibility = Visibility.Visible; RenderStepD(); break;
            case 5: StepEContent.Visibility = Visibility.Visible; RenderStepE(); break;
            case 6: StepFContent.Visibility = Visibility.Visible; RenderStepF(); break;
        }
        UpdateNav();
        UpdateSummaryStrip();
    }

    private void HideAllSteps()
    {
        StepAContent.Visibility = Visibility.Collapsed; StepBContent.Visibility = Visibility.Collapsed;
        StepCContent.Visibility = Visibility.Collapsed; StepDContent.Visibility = Visibility.Collapsed;
        StepEContent.Visibility = Visibility.Collapsed; StepFContent.Visibility = Visibility.Collapsed;
    }

    private void BuildStepBar()
    {
        StepBar.Children.Clear();
        var steps = new[] { ("A", "Folio nguồn"), ("B", "Khoản mục"), ("C", "Folio đích"), ("D", "Quy tắc tách"), ("E", "Đối chiếu"), ("F", "Xác nhận") };
        for (int i = 0; i < 6; i++)
        {
            var idx = i + 1;
            var active = idx == _step;
            var bad = new Border
            {
                CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(2, 5, 2, 5),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = active ? Cb("#EFF6FF") : Cb("Transparent"),
                BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"),
                BorderThickness = new Thickness(1),
                Child = new StackPanel { Orientation = Orientation.Horizontal, Children = {
                    new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = active ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = active ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,0,0) }
                }}
            };
            bad.MouseLeftButtonDown += (_, _) => GoTo(idx);
            StepBar.Children.Add(bad);
            if (i < 5) StepBar.Children.Add(Sep());
        }
    }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = !HasBlocking(); }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        var stepNames = new[] { "Chọn Folio nguồn", "Chọn khoản mục", "Chọn Folio đích", "Quy tắc tách", "Đối chiếu trước/sau", "Xác nhận" };
        FooterStepLabel.Text = $"Bước {_step}/6 · {stepNames[_step - 1]} · Khoản: {_selectedItemIds.Count} · Tổng tách: {SplitTotal():N0} · Đích: {_targetFolios.Count}";
    }

    private bool HasBlocking()
    {
        if (!_persistedMode || _selectedSource is null) return true;
        if (_selectedItemIds.Count == 0 || _targetFolios.Count != 1) return true;
        if (_splitRuleType is not ("ByItem" or "ByRatio" or "ByAmount")) return true;
        if (_splitRuleType == "ByRatio" && (_splitRatio <= 0 || _splitRatio > 100)) return true;

        var source = _selectedSource;
        var target = _targetFolios[0];
        if (source.FolioGuid == Guid.Empty || source.RowVersion is not { Length: 8 }) return true;
        if (target.FolioGuid == Guid.Empty || target.RowVersion is not { Length: 8 }) return true;
        if (target.FolioGuid == source.FolioGuid) return true;
        if (string.IsNullOrWhiteSpace(source.Currency) || !string.Equals(source.Currency, target.Currency, StringComparison.OrdinalIgnoreCase)) return true;

        var selectedItems = SelectedItemsForSource();
        if (selectedItems.Count != _selectedItemIds.Count) return true;
        if (selectedItems.Any(item => item.EntryGuid == Guid.Empty || item.IsLocked || !item.CanSplit || item.Net <= 0)) return true;
        if (selectedItems.Any(item => _partialSplits.TryGetValue(item.Id, out var partial) && partial.Enabled &&
                                      (partial.SplitAmount <= 0 || partial.SplitAmount > item.Net))) return true;

        var total = SplitTotal();
        if (_splitRuleType == "ByAmount" && (_splitFixedAmount <= 0 || _splitFixedAmount > selectedItems.Sum(item => Math.Max(0, item.Net)))) return true;
        var sourceBalance = source.Total - source.Paid - source.Deposit;
        if (total <= 0 || total > Math.Max(0, sourceBalance)) return true;

        return _blocking.Any(w => w.Level == "block");
    }

    private List<SfItem> SelectedItemsForSource()
    {
        return _allItems
            .Where(item => _selectedSource is not null && item.FolioId == _selectedSource.Id && _selectedItemIds.Contains(item.Id))
            .ToList();
    }

    private void UpdateSummaryStrip()
    {
        SummaryStrip.Children.Clear();
        var src = _selectedSource;
        var sel = src != null;
        var total = SplitTotal();
        var targetCount = _targetFolios.Count;
        var diff = total - (_splitFixedAmount > 0 ? _splitFixedAmount : (int)(total * _splitRatio / 100.0));

        SummaryStrip.Children.Add(Strip("Khách", src?.GuestName ?? "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", src?.Room ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Nguồn", src?.Code ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Tổng nguồn", sel ? $"{src!.Total:N0}" : "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Khoản đã chọn", $"{_selectedItemIds.Count}", _selectedItemIds.Count > 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Tổng tách", total > 0 ? $"{total:N0}" : "—", total > 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Folio đích", $"{targetCount}", targetCount > 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Chênh lệch", sel && total > 0 ? $"{diff:N0}" : "—", diff == 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Currency", src?.Currency ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        var ready = sel && !HasBlocking();
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Sẵn sàng" : "Cần kiểm tra", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    private int SplitTotal()
    {
        return GetSplitAmounts().Sum(x => x.Amount);
    }

    private List<(SfItem Item, int Amount)> GetSplitAmounts()
    {
        var items = SelectedItemsForSource();
        if (_selectedItemIds.Count == 0) return new List<(SfItem, int)>();
        if (_splitRuleType == "ByRatio")
            return items.Select(item => (Item: item, Amount: (int)Math.Round(Math.Max(0, item.Net) * Math.Clamp(_splitRatio, 0, 100) / 100d, MidpointRounding.AwayFromZero))).ToList();
        if (_splitRuleType == "ByAmount")
        {
            var remaining = Math.Max(0, _splitFixedAmount);
            return items.Select(item =>
            {
                var amount = Math.Min(Math.Max(0, item.Net), remaining);
                remaining -= amount;
                return (Item: item, Amount: amount);
            }).ToList();
        }

        return items.Select(item =>
        {
            var amount = _partialSplits.TryGetValue(item.Id, out var partial) && partial.Enabled ? partial.SplitAmount : item.Net;
            return (Item: item, Amount: amount);
        }).ToList();
    }

    private List<FolioSplitAllocation> BuildAllocations()
    {
        return GetSplitAmounts()
            .Where(x => x.Amount > 0 && x.Item.EntryGuid != Guid.Empty)
            .Select(x => new FolioSplitAllocation(x.Item.EntryGuid, x.Amount))
            .ToList();
    }

    // ═══════════════════════════════════════════
    // STEP A — SOURCE FOLIO SELECTION
    // ═══════════════════════════════════════════

    private List<SfFolio> GetFilteredFolios()
    {
        var f = _allFolios.AsEnumerable();
        var kw = SourceSearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw))
            f = f.Where(x => x.GuestName.ToLower().Contains(kw) || x.Room.Contains(kw) || x.Code.ToLower().Contains(kw) || x.Reservation.ToLower().Contains(kw));
        var type = FolioTypeFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(type) && type != "Tất cả") f = f.Where(x => x.Type == type);
        var curr = FolioCurrFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(curr) && curr != "Tất cả") f = f.Where(x => x.Currency == curr);
        var status = FolioStatusFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(status) && status != "Tất cả") f = f.Where(x => x.Status == status);
        return f.ToList();
    }

    private void RefreshSourcePage(bool reset)
    {
        if (reset) _sourcePage = 0;
        var f = GetFilteredFolios();
        var tp = Math.Max(1, (f.Count + _sourcePageSize - 1) / _sourcePageSize);
        if (_sourcePage >= tp) _sourcePage = tp - 1;
        SourceGrid.ItemsSource = f.Skip(_sourcePage * _sourcePageSize).Take(_sourcePageSize).ToList();
        SourcePagerSummary.Text = $"Hiển thị {_sourcePage * _sourcePageSize + 1}–{Math.Min((_sourcePage + 1) * _sourcePageSize, f.Count)} / {f.Count} Folio";
        SourcePrevBtn.IsEnabled = _sourcePage > 0; SourceNextBtn.IsEnabled = _sourcePage < tp - 1;
        BuildSourcePageBtns(tp);
    }

    private void BuildSourcePageBtns(int tp)
    {
        SourcePageBtns.Children.Clear();
        for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = PagerBtn(i, _sourcePage, idx); nb.Click += (_, _) => { _sourcePage = idx; RefreshSourcePage(false); }; SourcePageBtns.Children.Add(nb); }
    }

    private void OnSourceSelected(object s, SelectionChangedEventArgs e)
    {
        if (SourceGrid.SelectedItem is not SfFolio f) return;
        _previewSource = f;
        _selectedSource = null;
        SourceEmptyState.Visibility = Visibility.Collapsed;
        SourceDetailPanel.Visibility = Visibility.Visible;
        SetDetail(SdCode, "Mã Folio", f.Code, "#17365D", true);
        SetDetail(SdType, "Loại", f.Type); SetDetail(SdGuest, "Khách", f.GuestName); SetDetail(SdRoom, "Phòng", f.Room);
        SetDetail(SdRes, "Hồ sơ", f.Reservation); SetDetail(SdOpen, "Ngày mở", f.OpenDate); SetDetail(SdClose, "Ngày đi", f.Departure);
        SetDetail(SdPayer, "Người thanh toán", f.Payer); SetDetail(SdCompany, "Công ty", f.Company ?? "—"); SetDetail(SdTA, "Đại lý du lịch", f.TA ?? "—");
        SetDetail(SdBilling, "Chỉ dẫn thanh toán", f.BillingInstruction); SetDetail(SdCurr, "Tiền tệ", f.Currency);
        SetDetail(SdTotal, "Tổng nợ", $"{f.Total:N0}", "#17365D", true);
        SetDetail(SdPaid, "Đã thanh toán", $"{f.Paid:N0}", "#16A34A", true);
        SetDetail(SdDeposit, "Đặt cọc", $"{f.Deposit:N0}", "#F59E0B", true);
        var bal = f.Total - f.Paid - f.Deposit;
        SetDetail(SdBalance, "Số dư", $"{bal:N0}", "#DC2626", true);
        SetDetail(SdItems, "Số khoản mục", $"{f.ItemCount}");
        SetDetail(SdLocked, "Khoản bị khóa", $"{f.LockedCount}", "#DC2626");
        SetDetail(SdDisputed, "Khoản tranh chấp", $"{f.DisputedCount}", "#F59E0B");
        SetDetail(SdStatus, "Trạng thái", f.Status, "#2F6BFF", true);
        BtnSelectSource.IsEnabled = f.Status is "Open" or "Disputed";
        BtnSelectSource.Content = "Chọn Folio này";
        UpdateSummaryStrip();
    }

    private void OnSelectSource(object s, RoutedEventArgs e)
    {
        if (_previewSource == null) return;
        _selectedSource = _previewSource;
        _selectedItemIds.Clear(); _partialSplits.Clear(); _targetFolios.Clear();
        _itemTargetMap.Clear(); _warnings.Clear(); _blocking.Clear();
        BtnSelectSource.Content = "✓ Đã chọn"; BtnSelectSource.IsEnabled = false;
        UpdateSummaryStrip(); UpdateNav();
    }

    private void OnApplySourceFilter(object s, RoutedEventArgs e) => RefreshSourcePage(true);
    private void OnClearSourceFilter(object s, RoutedEventArgs e) { SourceSearchBox.Text = ""; FolioTypeFilter.SelectedIndex = 0; FolioCurrFilter.SelectedIndex = 0; FolioStatusFilter.SelectedIndex = 0; RefreshSourcePage(true); }
    private void OnSourcePrevPage(object s, RoutedEventArgs e) { if (_sourcePage > 0) { _sourcePage--; RefreshSourcePage(false); } }
    private void OnSourceNextPage(object s, RoutedEventArgs e) { _sourcePage++; RefreshSourcePage(false); }
    private void OnSourcePageSizeChanged(object s, SelectionChangedEventArgs e) { _sourcePageSize = SourcePageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshSourcePage(true); }

    // ═══════════════════════════════════════════
    // STEP B — ITEM SELECTION
    // ═══════════════════════════════════════════

    private void RenderStepB() { RefreshItemPage(true); BuildItemSelectionSummary(); }

    private List<SfItem> GetFilteredItems()
    {
        var items = _selectedSource != null ? _allItems.Where(it => it.FolioId == _selectedSource.Id) : _allItems.AsEnumerable();
        var type = ItemTypeFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(type) && type != "Tất cả") items = items.Where(it => it.Type == type);
        var dept = ItemDeptFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(dept) && dept != "Tất cả") items = items.Where(it => it.Dept == dept);
        var sf = ItemSplitFilter.SelectedItem?.ToString();
        if (sf == "Có thể tách") items = items.Where(it => it.CanSplit);
        else if (sf == "Tách một phần") items = items.Where(it => it.SplitFlag == "Partial");
        else if (sf == "Bị khóa") items = items.Where(it => it.IsLocked);
        else if (sf == "Đã thanh toán") items = items.Where(it => it.IsPaid);
        return items.ToList();
    }

    private void RefreshItemPage(bool reset)
    {
        if (reset) _itemPage = 0;
        var f = GetFilteredItems();
        var tp = Math.Max(1, (f.Count + _itemPageSize - 1) / _itemPageSize);
        if (_itemPage >= tp) _itemPage = tp - 1;
        ItemGrid.ItemsSource = f.Skip(_itemPage * _itemPageSize).Take(_itemPageSize).ToList();
        ItemPagerSummary.Text = $"Hiển thị {_itemPage * _itemPageSize + 1}–{Math.Min((_itemPage + 1) * _itemPageSize, f.Count)} / {f.Count} khoản mục";
        ItemPrevBtn.IsEnabled = _itemPage > 0; ItemNextBtn.IsEnabled = _itemPage < tp - 1;
        BuildItemPageBtns(tp);
    }

    private void BuildItemPageBtns(int tp)
    {
        ItemPageBtns.Children.Clear();
        for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = PagerBtn(i, _itemPage, idx); nb.Click += (_, _) => { _itemPage = idx; RefreshItemPage(false); }; ItemPageBtns.Children.Add(nb); }
    }

    private Button PagerBtn(int i, int currentPage, int idx) => new()
    {
        Content = (i + 1).ToString(), Width = 28, Height = 26, FontSize = 11, Margin = new Thickness(1, 0, 1, 0),
        Background = i == currentPage ? Cb("#2F6BFF") : Cb("#F1F5F9"),
        Foreground = i == currentPage ? Cb("#FFF") : Cb("#475569"),
        BorderThickness = new Thickness(i == currentPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"),
        Cursor = System.Windows.Input.Cursors.Hand,
        Template = SourcePrevBtn.Template
    };

    private void OnItemSelected(object s, SelectionChangedEventArgs e)
    {
        if (ItemGrid.SelectedItem is not SfItem item) return;
        _previewItem = item;
        ItemEmptyState.Visibility = Visibility.Collapsed;
        ItemDetailPanel.Visibility = Visibility.Visible;
        SetDetail(IdCode, "Mã giao dịch", item.Code, "#17365D", true);
        SetDetail(IdType, "Loại", item.Type); SetDetail(IdDesc, "Diễn giải", item.Description);
        SetDetail(IdDate, "Thời gian", item.Date.ToString("dd/MM/yyyy HH:mm")); SetDetail(IdDept, "Bộ phận", item.Dept);
        SetDetail(IdRoom, "Phòng", item.Room); SetDetail(IdGuest, "Khách", item.Guest); SetDetail(IdPayer, "Người thanh toán", item.Payer);
        SetDetail(IdQty, "Số lượng", $"{item.Qty}"); SetDetail(IdPrice, "Đơn giá", $"{item.Price:N0}");
        SetDetail(IdTax, "Thuế", $"{item.Tax:N0}"); SetDetail(IdSc, "Phí dịch vụ", $"{item.ServiceCharge:N0}"); SetDetail(IdDisc, "Giảm giá", $"{item.Discount:N0}");
        SetDetail(IdNet, "Thành tiền", $"{item.Net:N0} {item.Currency}", "#17365D", true);
        SetDetail(IdStatus, "Trạng thái", item.Status);
        SetDetail(IdSplitFlag, "Khả năng tách", item.SplitFlag switch { "Full" => "Có thể tách", "Partial" => "Tách một phần", "Blocked" => "Bị khóa", "Disputed" => "Đang tranh chấp", "PackageParent" => "Package cha", "PackageChild" => "Package con", _ => item.SplitFlag }, "#2F6BFF", true);

        var isSelected = _selectedItemIds.Contains(item.Id);
        BtnToggleItem.Content = isSelected ? "✕ Bỏ chọn" : "✓ Chọn khoản này";
        BtnToggleItem.IsEnabled = item.CanSplit;

        PartialPanel.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
        if (isSelected && _partialSplits.TryGetValue(item.Id, out var ps))
        {
            PsOrig.Text = $"{item.Net:N0}"; PsSplit.Text = $"{ps.SplitAmount:N0}";
            PsRemain.Text = $"{item.Net - ps.SplitAmount:N0}"; PsRatio.Text = $"{(double)ps.SplitAmount / item.Net * 100:F0}%";
            PsReason.Text = ps.Reason;
        }
    }

    private void OnToggleItem(object s, RoutedEventArgs e)
    {
        if (_previewItem == null) return;
        var id = _previewItem.Id;
        if (_selectedItemIds.Contains(id)) { _selectedItemIds.Remove(id); _partialSplits.Remove(id); BtnToggleItem.Content = "✓ Chọn khoản này"; PartialPanel.Visibility = Visibility.Collapsed; }
        else { _selectedItemIds.Add(id); _partialSplits[id] = new SfPartialSplit { Enabled = false, SplitAmount = _previewItem.Net, RemainAmount = 0, Ratio = 100, Reason = "" }; BtnToggleItem.Content = "✕ Bỏ chọn"; PartialPanel.Visibility = Visibility.Visible; PsOrig.Text = $"{_previewItem.Net:N0}"; PsSplit.Text = $"{_previewItem.Net:N0}"; PsRemain.Text = "0"; PsRatio.Text = "100%"; }
        BuildItemSelectionSummary();
        UpdateSummaryStrip(); UpdateNav();
    }

    private void BuildItemSelectionSummary()
    {
        ItemSelPanel.Children.Clear();
        var total = SplitTotal();
        var tax = _allItems.Where(it => _selectedSource is not null && it.FolioId == _selectedSource.Id && _selectedItemIds.Contains(it.Id)).Sum(it => it.Tax);
        var sc = _allItems.Where(it => _selectedSource is not null && it.FolioId == _selectedSource.Id && _selectedItemIds.Contains(it.Id)).Sum(it => it.ServiceCharge);
        ItemSelPanel.Children.Add(Metric("Đã chọn", $"{_selectedItemIds.Count} khoản"));
        ItemSelPanel.Children.Add(Metric("Tổng tách", $"{total:N0}"));
        ItemSelPanel.Children.Add(Metric("Thuế", $"{tax:N0}"));
        ItemSelPanel.Children.Add(Metric("Phí DV", $"{sc:N0}"));
        var blocked = _allItems.Count(it => _selectedSource is not null && it.FolioId == _selectedSource.Id && _selectedItemIds.Contains(it.Id) && !it.CanSplit);
        if (blocked > 0) ItemSelPanel.Children.Add(Metric("Bị chặn", $"{blocked}"));
    }

    private void OnSelectAllItems(object s, RoutedEventArgs e) { foreach (var it in GetFilteredItems().Where(x => x.CanSplit)) _selectedItemIds.Add(it.Id); RefreshItemPage(false); BuildItemSelectionSummary(); UpdateSummaryStrip(); UpdateNav(); }
    private void OnDeselectAll(object s, RoutedEventArgs e) { _selectedItemIds.Clear(); _partialSplits.Clear(); RefreshItemPage(false); BuildItemSelectionSummary(); UpdateSummaryStrip(); UpdateNav(); }
    private void OnSelRoomOnly(object s, RoutedEventArgs e) { _selectedItemIds.Clear(); foreach (var it in GetFilteredItems().Where(x => x.Type == "Room Charge" && x.CanSplit)) _selectedItemIds.Add(it.Id); RefreshItemPage(false); BuildItemSelectionSummary(); UpdateSummaryStrip(); UpdateNav(); }
    private void OnApplyItemFilter(object s, RoutedEventArgs e) => RefreshItemPage(true);
    private void OnClearItemFilter(object s, RoutedEventArgs e) { ItemTypeFilter.SelectedIndex = 0; ItemDeptFilter.SelectedIndex = 0; ItemSplitFilter.SelectedIndex = 0; RefreshItemPage(true); }
    private void OnItemPrevPage(object s, RoutedEventArgs e) { if (_itemPage > 0) { _itemPage--; RefreshItemPage(false); } }
    private void OnItemNextPage(object s, RoutedEventArgs e) { _itemPage++; RefreshItemPage(false); }
    private void OnItemPageSizeChanged(object s, SelectionChangedEventArgs e) { _itemPageSize = ItemPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshItemPage(true); }

    // ═══════════════════════════════════════════
    // STEP C — TARGET FOLIO
    // ═══════════════════════════════════════════

    private void RenderStepC() { RefreshTargetList(); BuildTargetInfo(); }

    private void RefreshTargetList()
    {
        TargetListPanel.Children.Clear();
        foreach (var folio in _targetFolios)
        {
            var display = $"{folio.Code} · {folio.Type} · {folio.GuestName} · {folio.Currency}";
            var border = new Border
            {
                Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 0, 0, 4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Child = new StackPanel { Children = { new TextBlock { Text = display, FontSize = 11, Foreground = C("#17365D") }, new TextBlock { Text = $"Payer: {folio.Payer} · {folio.Currency}", FontSize = 10, Foreground = C("#94A3B8"), Margin = new Thickness(0, 2, 0, 0) } } }
            };
            border.MouseLeftButtonDown += (_, _) =>
            {
                _targetFolios.Clear();
                _targetFolios.Add(folio);
                RefreshTargetList(); UpdateSummaryStrip(); UpdateNav();
            };
            TargetListPanel.Children.Add(border);
        }

        // Add available folios from the stay
        if (_selectedSource != null)
        {
            // A persisted Folio is scoped to a stay, so a valid destination may belong
            // to another stay in the same property. Restricting by guest made the
            // real-data list empty whenever the property had no second Folio for the
            // same guest, even though the split use case supports another open Folio.
            var available = _allFolios
                .Where(f => f.Id != _selectedSource.Id && (f.Status is "Open" or "Suspended"))
                .Take(10);
            foreach (var f in available)
            {
                var code = f.Code;
                var border2 = new Border
                {
                    Background = Cb("White"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 0, 0, 4),
                    BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand,
                    Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "+ ", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = C("#16A34A"), VerticalAlignment = VerticalAlignment.Center }, new StackPanel { Children = { new TextBlock { Text = $"{f.Code} · {f.Type}", FontSize = 11, Foreground = C("#17365D") }, new TextBlock { Text = $"{f.GuestName} · {f.Currency} · {f.Payer}", FontSize = 10, Foreground = C("#94A3B8") } } } } }
                };
                var fid = f.Id;
                border2.MouseLeftButtonDown += (_, _) =>
                {
                    if (_persistedMode)
                    {
                        _targetFolios.Clear();
                    }
                    if (!_targetFolios.Any(t => t.Id == fid)) { _targetFolios.Add(f); RefreshTargetList(); UpdateSummaryStrip(); UpdateNav(); }
                };
                TargetListPanel.Children.Add(border2);
            }
        }
    }

    private void BuildTargetInfo()
    {
        TargetFolioInfoPanel.Children.Clear();
        if (!_persistedMode || _folioService is null || _contextAccessor?.Current is null)
        {
            MessageBox.Show("Không thể tách Folio khi chưa có dữ liệu Folio thật và property context.", "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TargetFolioInfoPanel.Children.Add(STitle("Folio đích"));
        TargetFolioInfoPanel.Children.Add(new TextBlock
        {
            Text = "Chọn một Folio đích đang mở trong dữ liệu thật. Không tạo Folio giả trong quy trình nghiệp vụ.",
            FontSize = 11, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap
        });
    }

    // ═══════════════════════════════════════════
    // STEP D — SPLIT RULES
    // ═══════════════════════════════════════════

    private void RenderStepD()
    {
        RuleLeftPanel.Children.Clear(); RuleRightPanel.Children.Clear();
        var rules = new[] { "Theo khoản mục", "Theo loại dịch vụ", "Theo ngày", "Theo khách", "Theo phòng", "Theo người TT", "Theo Company/TA", "Theo tỷ lệ", "Theo số tiền", "Chia đều", "Theo Billing" };

        foreach (var r in rules)
        {
            var active = _splitRuleType switch
            {
                "ByItem" => r == "Theo khoản mục", "ByService" => r == "Theo loại dịch vụ", "ByDate" => r == "Theo ngày",
                "ByGuest" => r == "Theo khách", "ByRoom" => r == "Theo phòng", "ByPayer" => r == "Theo người TT",
                "ByCompanyTA" => r == "Theo Company/TA", "ByRatio" => r == "Theo tỷ lệ", "ByAmount" => r == "Theo số tiền",
                "ByEqual" => r == "Chia đều", "ByBilling" => r == "Theo Billing", _ => false
            };
            var ruleKey = r switch
            {
                "Theo khoản mục" => "ByItem", "Theo loại dịch vụ" => "ByService", "Theo ngày" => "ByDate",
                "Theo khách" => "ByGuest", "Theo phòng" => "ByRoom", "Theo người TT" => "ByPayer",
                "Theo Company/TA" => "ByCompanyTA", "Theo tỷ lệ" => "ByRatio", "Theo số tiền" => "ByAmount",
                "Chia đều" => "ByEqual", "Theo Billing" => "ByBilling", _ => "ByItem"
            };
            var b = new Border
            {
                CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"),
                BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1),
                Child = new TextBlock { Text = r, FontSize = 12, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") }
            };
            b.MouseLeftButtonDown += (_, _) => { _splitRuleType = ruleKey; RenderStepD(); };
            RuleLeftPanel.Children.Add(b);
        }

        // Right side config
        RuleRightPanel.Children.Add(STitle("Cấu hình quy tắc"));
        if (_splitRuleType == "ByRatio")
        {
            RuleRightPanel.Children.Add(new TextBlock { Text = "Tỷ lệ (%)", Style = FindResource("FieldLabel") as Style });
            var ratioBox = new TextBox { Text = _splitRatio.ToString("0.##", CultureInfo.CurrentCulture), Style = FindResource("ModernTextBox") as Style, Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
            ratioBox.TextChanged += OnSplitRatioChanged;
            RuleRightPanel.Children.Add(ratioBox);
            RuleRightPanel.Children.Add(new TextBlock { Text = $"Tổng tỷ lệ = {_splitRatio}%", FontSize = 11, Foreground = _splitRatio == 100 ? C("#16A34A") : C("#DC2626"), Margin = new Thickness(0, 4, 0, 0) });
        }
        else if (_splitRuleType == "ByAmount")
        {
            RuleRightPanel.Children.Add(new TextBlock { Text = "Số tiền cố định", Style = FindResource("FieldLabel") as Style });
            var amountBox = new TextBox { Text = _splitFixedAmount > 0 ? $"{_splitFixedAmount:N0}" : "", Style = FindResource("ModernTextBox") as Style, Width = 160, HorizontalAlignment = HorizontalAlignment.Left };
            amountBox.TextChanged += OnSplitAmountChanged;
            RuleRightPanel.Children.Add(amountBox);
            RuleRightPanel.Children.Add(new TextBlock { Text = "Số tiền sẽ được phân bổ lần lượt theo các khoản đã chọn.", FontSize = 11, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
        }
        else if (_splitRuleType == "ByService")
        {
            var map = new (string, string)[] {
                ("Room Charge", "Company"), ("Minibar", "Guest"), ("Restaurant", "Guest"), ("Spa", "Guest"), ("Transport", "Master"), ("Tax/Phí", "Theo khoản gốc") };
            foreach (var (svc, target) in map)
                RuleRightPanel.Children.Add(Row(svc, target));
        }
        else if (_splitRuleType == "ByPayer")
        {
            RuleRightPanel.Children.Add(Row("Guest", "Guest Folio")); RuleRightPanel.Children.Add(Row("Company", "Company Folio"));
            RuleRightPanel.Children.Add(Row("TA", "Agent Folio")); RuleRightPanel.Children.Add(Row("House", "Master Folio"));
        }
        else
        {
            RuleRightPanel.Children.Add(new TextBlock { Text = "Chọn quy tắc tách phù hợp.", FontSize = 12, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap });
        }
    }

    // ═══════════════════════════════════════════
    // STEP E — RECONCILIATION
    // ═══════════════════════════════════════════

    private void RenderStepE()
    {
        ReconcileLeftPanel.Children.Clear(); ReconcileRightPanel.Children.Clear();
        var total = SplitTotal();
        var remain = (_selectedSource?.Total ?? 0) - (_selectedSource?.Paid ?? 0) - (_selectedSource?.Deposit ?? 0);
        var afterRemain = remain - total;

        ReconcileLeftPanel.Children.Add(STitle("Trước khi tách"));
        ReconcileLeftPanel.Children.Add(Row("Folio nguồn", _selectedSource?.Code ?? "—"));
        ReconcileLeftPanel.Children.Add(Row("Tổng Nợ", $"{_selectedSource?.Total ?? 0:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Tổng Có", $"{(_selectedSource?.Paid ?? 0) + (_selectedSource?.Deposit ?? 0):N0}"));
        ReconcileLeftPanel.Children.Add(Row("Balance", $"{remain:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Số khoản", $"{_selectedSource?.ItemCount ?? 0}"));
        ReconcileLeftPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 4) });
        ReconcileLeftPanel.Children.Add(Row("Thuế", $"{_allItems.Where(it => _selectedItemIds.Contains(it.Id)).Sum(it => it.Tax):N0}"));
        ReconcileLeftPanel.Children.Add(Row("Phí DV", $"{_allItems.Where(it => _selectedItemIds.Contains(it.Id)).Sum(it => it.ServiceCharge):N0}"));
        ReconcileLeftPanel.Children.Add(Row("Discount", $"{_allItems.Where(it => _selectedItemIds.Contains(it.Id)).Sum(it => it.Discount):N0}"));

        ReconcileLeftPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        ReconcileLeftPanel.Children.Add(STitle("Sau khi tách"));
        ReconcileLeftPanel.Children.Add(Row("Tổng tách", $"{total:N0}"));
        ReconcileLeftPanel.Children.Add(Row("Nguồn còn lại", $"{Math.Max(0, afterRemain):N0}"));
        ReconcileLeftPanel.Children.Add(Row("Số Folio đích", $"{_targetFolios.Count}"));
        var diff = remain - total - afterRemain;
        ReconcileLeftPanel.Children.Add(new Border { Background = diff == 0 ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = diff == 0 ? "✓ Tổng trước/sau cân" : $"⚠ Chênh lệch: {diff:N0}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = diff == 0 ? C("#16A34A") : C("#DC2626") } });

        // Warning Center
        ReconcileRightPanel.Children.Add(STitle("Warning Center"));
        _warnings.Clear(); _blocking.Clear();
        if (_selectedSource == null) _blocking.Add(new SfWarning("block", "Chưa chọn Folio nguồn", 1));
        if (_selectedItemIds.Count == 0) _blocking.Add(new SfWarning("block", "Chưa chọn khoản mục nào", 2));
        if (_targetFolios.Count == 0) _blocking.Add(new SfWarning("block", "Chưa chọn Folio đích", 3));
        if (!_persistedMode) _blocking.Add(new SfWarning("block", "Đường dữ liệu Folio thật chưa sẵn sàng; không thể xác nhận bằng dữ liệu mẫu.", 1));
        if (_persistedMode && _splitRuleType is not ("ByItem" or "ByRatio" or "ByAmount")) _blocking.Add(new SfWarning("block", "Dữ liệu thật hiện chỉ hỗ trợ tách theo khoản mục, theo tỷ lệ hoặc theo số tiền.", 4));
        if (_splitRuleType == "ByRatio" && (_splitRatio <= 0 || _splitRatio > 100)) _blocking.Add(new SfWarning("block", "Tỷ lệ phải lớn hơn 0% và không vượt quá 100%.", 4));
        if (_splitRuleType == "ByAmount" && _splitFixedAmount <= 0) _blocking.Add(new SfWarning("block", "Số tiền cố định phải lớn hơn 0.", 4));
        if (diff != 0) _warnings.Add(new SfWarning("warn", $"Chênh lệch trước/sau: {diff:N0}", 5));
        if (_selectedItemIds.Count > 0 && total == 0) _blocking.Add(new SfWarning("block", "Tổng tách = 0", 2));
        var selectedItems = SelectedItemsForSource();
        if (_persistedMode && selectedItems.Count != _selectedItemIds.Count)
            _blocking.Add(new SfWarning("block", "Một hoặc nhiều khoản mục không còn thuộc Folio nguồn hiện tại.", 2));
        if (_persistedMode && _selectedSource is { } source)
        {
            var sourceBalance = source.Total - source.Paid - source.Deposit;
            if (total > Math.Max(0, sourceBalance))
                _blocking.Add(new SfWarning("block", $"Tổng tách vượt balance Folio nguồn ({sourceBalance:N0}).", 2));
            if (_targetFolios.Count == 1 &&
                string.Equals(source.Currency, _targetFolios[0].Currency, StringComparison.OrdinalIgnoreCase) == false)
                _blocking.Add(new SfWarning("block", "Currency của Folio nguồn và Folio đích không khớp.", 3));
        }
        var disputed = _allItems.Count(it => _selectedSource is not null && it.FolioId == _selectedSource.Id && _selectedItemIds.Contains(it.Id) && it.IsDisputed);
        if (disputed > 0) _warnings.Add(new SfWarning("warn", $"{disputed} khoản đang tranh chấp", 2));
        var locked = _allItems.Count(it => _selectedSource is not null && it.FolioId == _selectedSource.Id && _selectedItemIds.Contains(it.Id) && it.IsLocked);
        if (locked > 0) _blocking.Add(new SfWarning("block", $"{locked} khoản bị khóa", 2));
        var unsplittable = selectedItems.Count(it => !it.CanSplit || it.EntryGuid == Guid.Empty || it.Net <= 0);
        if (_persistedMode && unsplittable > 0)
            _blocking.Add(new SfWarning("block", $"{unsplittable} khoản không có đủ dữ liệu giao dịch thật để tách.", 2));

        foreach (var w in _blocking.Concat(_warnings))
        {
            var isBlock = w.Level == "block";
            var b = new Border { Background = isBlock ? Cb("#FEE2E2") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 4), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = isBlock ? "✕ " : "⚠ ", FontSize = 12, Foreground = isBlock ? C("#DC2626") : C("#A16207"), VerticalAlignment = VerticalAlignment.Center }, new StackPanel { Children = { new TextBlock { Text = w.Message, FontSize = 11, Foreground = isBlock ? C("#DC2626") : C("#A16207") }, new TextBlock { Text = $"Bước {w.Step}", FontSize = 10, Foreground = C("#94A3B8"), Margin = new Thickness(0, 1, 0, 0) } } } } } };
            ReconcileRightPanel.Children.Add(b);
        }
        if (_warnings.Count == 0 && _blocking.Count == 0)
            ReconcileRightPanel.Children.Add(new TextBlock { Text = "✓ Không có warning hoặc blocking issue.", FontSize = 11, Foreground = C("#16A34A"), Margin = new Thickness(0, 0, 0, 6) });
        UpdateNav();
    }

    // ═══════════════════════════════════════════
    // STEP F — CONFIRMATION
    // ═══════════════════════════════════════════

    private void RenderStepF()
    {
        ConfirmLeftPanel.Children.Clear(); ConfirmRightPanel.Children.Clear();
        var src = _selectedSource;
        if (src == null) return;
        var total = SplitTotal();

        ConfirmLeftPanel.Children.Add(STitle("Tóm tắt tách Folio"));
        ConfirmLeftPanel.Children.Add(Row("Nguồn", $"{src.Code} · {src.GuestName} · Phòng {src.Room}"));
        ConfirmLeftPanel.Children.Add(Row("Payer", src.Payer)); ConfirmLeftPanel.Children.Add(Row("Currency", src.Currency));
        ConfirmLeftPanel.Children.Add(Row("Tổng trước", $"{src.Total:N0}"));
        ConfirmLeftPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 4) });
        ConfirmLeftPanel.Children.Add(Row("Số khoản tách", $"{_selectedItemIds.Count}"));
        ConfirmLeftPanel.Children.Add(Row("Tổng tách", $"{total:N0}"));
        ConfirmLeftPanel.Children.Add(Row("Số Folio đích", $"{_targetFolios.Count}"));
        ConfirmLeftPanel.Children.Add(Row("Quy tắc", _splitRuleType switch { "ByItem" => "Theo khoản mục", "ByRatio" => "Theo tỷ lệ", "ByAmount" => "Theo số tiền", _ => _splitRuleType }));
        var sourceBalance = src.Total - src.Paid - src.Deposit;
        ConfirmLeftPanel.Children.Add(Row("Balance nguồn", $"{sourceBalance:N0}"));

        // Readiness checklist
        ConfirmRightPanel.Children.Add(STitle("Readiness"));
        var selectedItems = SelectedItemsForSource();
        var sourceValid = _persistedMode && src.FolioGuid != Guid.Empty && src.RowVersion is { Length: 8 };
        var targetValid = _persistedMode && _targetFolios.Count == 1 && _targetFolios[0].FolioGuid != Guid.Empty &&
                          _targetFolios[0].RowVersion is { Length: 8 } && _targetFolios[0].FolioGuid != src.FolioGuid;
        var itemsValid = _persistedMode && selectedItems.Count == _selectedItemIds.Count &&
                         selectedItems.All(item => item.EntryGuid != Guid.Empty && item.Net > 0 && item.CanSplit && !item.IsLocked);
        var allocationsValid = itemsValid && selectedItems.All(item =>
            !_partialSplits.TryGetValue(item.Id, out var partial) || !partial.Enabled ||
            (partial.SplitAmount > 0 && partial.SplitAmount <= item.Net));
        var totalInBalance = total > 0 && total <= Math.Max(0, sourceBalance);
        var currencyValid = targetValid && !string.IsNullOrWhiteSpace(src.Currency) &&
                            string.Equals(src.Currency, _targetFolios[0].Currency, StringComparison.OrdinalIgnoreCase);
        var ruleValid = _persistedMode && (_splitRuleType is "ByItem" or "ByRatio" or "ByAmount") &&
                        (_splitRuleType != "ByRatio" || (_splitRatio > 0 && _splitRatio <= 100)) &&
                        (_splitRuleType != "ByAmount" || _splitFixedAmount > 0);
        var checks = new (string, bool)[] {
            ("Đã chọn Folio nguồn thật", sourceValid), ("Đã chọn khoản mục thật", _selectedItemIds.Count > 0 && itemsValid),
            ("Đã chọn Folio đích thật", targetValid), ("Quy tắc tách hợp lệ", ruleValid),
            ("Phân bổ khoản mục hợp lệ", allocationsValid), ("Không vượt balance nguồn", totalInBalance),
            ("Khoản mục không bị khóa", itemsValid), ("Currency nguồn/đích khớp", currencyValid),
            ("Không có blocking issue", !HasBlocking())
        };
        var allOk = true;
        foreach (var (label, ok) in checks)
        {
            ConfirmRightPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 12, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 11, Foreground = C("#334155") } } });
            if (!ok) allOk = false;
        }
        ConfirmRightPanel.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 8, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng tách Folio" : "Cần kiểm tra thêm", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
    }

    // ═══════════════════════════════════════════
    // CONFIRMATION DIALOG
    // ═══════════════════════════════════════════

    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (HasBlocking() || _selectedSource == null) return;
        var total = SplitTotal();
        if (_persistedMode)
        {
            if (_targetFolios.Count != 1 || _targetFolios[0].FolioGuid == Guid.Empty || _targetFolios[0].RowVersion is not { Length: 8 } ||
                _selectedSource.FolioGuid == Guid.Empty || _selectedSource.RowVersion is not { Length: 8 })
            {
                MessageBox.Show("Cần chọn đúng một Folio đích tồn tại trong dữ liệu thật, kèm rowversion hợp lệ.", "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItems = _allItems
                .Where(x => x.FolioId == _selectedSource.Id && _selectedItemIds.Contains(x.Id))
                .ToList();
            if (selectedItems.Any(x => x.EntryGuid == Guid.Empty))
            {
                MessageBox.Show("Khoản mục đã chọn chưa có định danh giao dịch thật để tách.", "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var allocations = BuildAllocations();
            if (allocations.Count == 0 || allocations.Sum(x => x.Amount) != total)
            {
                MessageBox.Show("Phân bổ số tiền tách không hợp lệ hoặc không khớp tổng tách.", "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = await _folioService!.SplitAsync(
                new SplitFolioCommand(
                    _selectedSource.FolioGuid,
                    _selectedSource.RowVersion,
                    _targetFolios[0].FolioGuid,
                    _targetFolios[0].RowVersion!,
                    total,
                    $"Tách Folio từ giao diện nghiệp vụ · Rule={_splitRuleType}",
                    allocations,
                    _splitRuleType),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể tách Folio.", "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Đã tách {total:N0} từ { _selectedSource.Code } sang {_targetFolios[0].Code }.", "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadPersistedFoliosAsync();
            return;
        }
        MessageBox.Show("Không thể tách Folio: đường dữ liệu thật chưa sẵn sàng.", "Tách Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // ═══════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════

    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private void OnRefresh(object s, RoutedEventArgs e) { _selectedSource = null; _previewSource = null; _selectedItemIds.Clear(); _partialSplits.Clear(); _targetFolios.Clear(); _itemTargetMap.Clear(); _warnings.Clear(); _blocking.Clear(); InitFilters(); GoTo(1); }
    private void OnClose(object s, RoutedEventArgs e) => Close();

    private void OnSplitRatioChanged(object s, TextChangedEventArgs e)
    {
        _splitRatio = double.TryParse((s as TextBox)?.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value) ? value : 0;
        UpdateSummaryStrip(); UpdateNav();
    }

    private void OnSplitAmountChanged(object s, TextChangedEventArgs e)
    {
        _splitFixedAmount = int.TryParse((s as TextBox)?.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : 0;
        UpdateSummaryStrip(); UpdateNav();
    }

    // ═══════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════

    private static FrameworkElement Metric(string l, string v) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 6, 4), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = C("#17365D") }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static void SetDetail(TextBlock target, string label, string value, string valueColor = "#334155", bool emphasize = false)
    {
        target.Inlines.Clear();
        target.FontSize = Math.Max(target.FontSize, 12);
        target.TextWrapping = TextWrapping.Wrap;
        target.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = C("#94A3B8") });
        target.Inlines.Add(new System.Windows.Documents.Run(value) { Foreground = C(valueColor), FontWeight = emphasize ? FontWeights.SemiBold : FontWeights.Normal });
    }
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsSf(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " › ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

// ═══════════════════════════════════════════
// DATA RECORDS
// ═══════════════════════════════════════════

internal record SfFolio(int Id, string Code, string Type, string GuestName, string Room, string Reservation, string Stay, string Arrival, string Departure, int Total, int Deposit, int Paid, string Currency, string Status, string Payer, string? Company, string? TA, int CreditLimit, int ItemCount, int LockedCount, int DisputedCount, string BillingInstruction, string OpenDate, bool HasSettlement, bool HasNightAuditLock, Guid FolioGuid = default, byte[]? RowVersion = null) { public int Balance => Total - Paid - Deposit; public string BalanceDisplay => $"{Balance:N0}"; public string TotalDisplay => $"{Total:N0}"; }

internal record SfItem(int Id, DateTime Date, DateTime Time, string Code, string Type, string Description, string Dept, string Room, string Guest, string Payer, int Qty, int Price, int Tax, int ServiceCharge, int Discount, int Net, string Currency, string Status, string SplitFlag, int FolioId, int? PackageGroupId, bool IsPaid, bool IsLocked, bool IsDisputed, bool CanSplit, Guid EntryGuid = default) { public string DateDisplay => Date.ToString("dd/MM HH:mm"); }


internal class SfPartialSplit { public bool Enabled { get; set; } public int SplitAmount { get; set; } public int RemainAmount { get; set; } public double Ratio { get; set; } public string Reason { get; set; } = ""; }

internal record SfReconciliation(string Label, int Before, int AfterSource, int Target1, int Target2, int TotalAfter, int Diff, string Status);

internal record SfWarning(string Level, string Message, int Step);

internal static class SfExt { public static TextBlock WithRunsSf(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
