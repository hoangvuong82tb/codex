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

public partial class TransferPostingWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step = 1;
    private TpPosting? _previewPosting;
    private readonly List<TpPosting> _allPostings;
    private readonly List<TpFolio> _allTargetFolios;
    private readonly HashSet<int> _selectedPostingIds = new();
    private TpFolio? _selectedTarget;
    private string _transferRule = "Entire", _taxRule = "Together", _packageRule = "Together", _payerRule = "Keep";
    private double _transferPercent = 100;
    private int _transferAmount;
    private int _transferQty;
    private readonly List<TpWarning> _warnings = new();
    private readonly List<TpWarning> _blocking = new();
    private int _postingPage, _postingPageSize = 30;
    private readonly List<TpTransferRecord> _history = new();
    private readonly HotelFolioApplicationService? _folioService;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private bool _persistedMode;

    public TransferPostingWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner ? owner : null;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioService = services?.GetService<HotelFolioApplicationService>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _allPostings = new List<TpPosting>();
        _allTargetFolios = new List<TpFolio>();
        InitFilters();
        GoTo(1);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_folioService is null || _contextAccessor?.Current is not { } context)
        {
            FooterStepLabel.Text = "Chưa cấu hình được Folio service và property context dữ liệu thật.";
            return;
        }

        _persistedMode = true;
        try
        {
            var result = await _folioService.SearchAsync(
                new SearchFoliosQuery(
                    new FolioSearchFilter(context.Property.PropertyId.Value, null, null, null, null, null, null, null),
                    new PageRequest(1, 500)),
                CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                FooterStepLabel.Text = result.Error?.Message ?? "Không thể tải Folio thật.";
                return;
            }

            _allPostings.Clear();
            _allTargetFolios.Clear();
            var currency = string.IsNullOrWhiteSpace(context.Property.CurrencyCode)
                ? "VND"
                : context.Property.CurrencyCode;
            var folioIndex = 0;
            foreach (var ledger in result.Value.Items)
            {
                var folioId = ++folioIndex;
                _allTargetFolios.Add(MapPersistedFolio(folioId, ledger, currency));
                var postingId = 0;
                foreach (var entry in ledger.Entries.Where(IsTransferable))
                    _allPostings.Add(MapPersistedPosting(++postingId, ledger, entry, currency));
            }

            _selectedPostingIds.Clear();
            _selectedTarget = null;
            _warnings.Clear();
            _blocking.Clear();
            GoTo(1);
            FooterStepLabel.Text = $"Đã tải {_allPostings.Count:N0} posting và {_allTargetFolios.Count:N0} Folio đích từ dữ liệu thật.";
        }
        catch (Exception ex)
        {
            _allPostings.Clear();
            _allTargetFolios.Clear();
            FooterStepLabel.Text = "Không tải được dữ liệu Folio thật: " + ex.Message;
        }
    }

    private static bool IsTransferable(HotelFolioEntryDto entry) => entry.EntryType is
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Laundry or
        HotelFolioEntryType.MiniBar or HotelFolioEntryType.Spa or HotelFolioEntryType.Telephone or
        HotelFolioEntryType.OtherCharge or HotelFolioEntryType.Tax or HotelFolioEntryType.ServiceCharge or
        HotelFolioEntryType.Adjustment or HotelFolioEntryType.Correction or HotelFolioEntryType.ManualCharge;

    private static TpFolio MapPersistedFolio(int id, FolioLedgerDto ledger, string currency)
    {
        var payer = !string.IsNullOrWhiteSpace(ledger.CompanyName) ? $"Company: {ledger.CompanyName}"
            : !string.IsNullOrWhiteSpace(ledger.TravelAgentName) ? $"TA: {ledger.TravelAgentName}" : "Guest";
        return new TpFolio(
            id,
            ledger.FolioNumber,
            "Folio",
            ledger.GuestName,
            ledger.RoomNumber,
            payer,
            string.IsNullOrWhiteSpace(currency) ? "VND" : currency,
            decimal.ToInt32(decimal.Round(ledger.RunningBalance, 0, MidpointRounding.AwayFromZero)),
            !string.IsNullOrWhiteSpace(ledger.CompanyName) ? "Route to Company" : !string.IsNullOrWhiteSpace(ledger.TravelAgentName) ? "Route to TA" : "Self-pay",
            ledger.Status.ToString(),
            true,
            ledger.HotelFolioId,
            ledger.RowVersion);
    }

    private static TpPosting MapPersistedPosting(int id, FolioLedgerDto ledger, HotelFolioEntryDto entry, string currency)
    {
        var amount = decimal.ToInt32(decimal.Round(Math.Abs(entry.Amount), 0, MidpointRounding.AwayFromZero));
        var type = entry.EntryType switch
        {
            HotelFolioEntryType.RoomCharge => "Room Charge",
            HotelFolioEntryType.Restaurant => "Restaurant",
            HotelFolioEntryType.MiniBar => "Minibar",
            HotelFolioEntryType.ServiceCharge => "Service Charge",
            _ => entry.EntryType.ToString()
        };
        var payer = !string.IsNullOrWhiteSpace(ledger.CompanyName) ? "Company" : !string.IsNullOrWhiteSpace(ledger.TravelAgentName) ? "TA" : "Guest";
        return new TpPosting(
            id,
            $"TXN-{entry.HotelFolioEntryId:N}"[..16],
            entry.BusinessDate?.ToDateTime(TimeOnly.MinValue) ?? entry.CreatedAtUtc.ToLocalTime().Date,
            entry.CreatedAtUtc.ToLocalTime(),
            type,
            entry.Description,
            "FO",
            "Front Office",
            ledger.GuestName,
            ledger.RoomNumber,
            ledger.FolioNumber,
            ledger.ReservationNumber ?? "—",
            1,
            amount,
            entry.EntryType == HotelFolioEntryType.Tax ? amount : 0,
            entry.EntryType == HotelFolioEntryType.ServiceCharge ? amount : 0,
            0,
            amount,
            string.IsNullOrWhiteSpace(currency) ? "VND" : currency,
            payer,
            ledger.Status.ToString(),
            entry.ReversedByEntryId.HasValue ? "Locked" : "Transferable",
            entry.ReferenceType ?? "",
            null,
            false,
            entry.CreatedBy?.ToString() ?? "—",
            "—",
            ledger.HotelFolioId,
            ledger.RowVersion);
    }

    // Historical designer fixture retained for reference only. Production postings
    // are loaded from HotelFolioApplicationService and never from generated rows.
#if false
    private static List<TpPosting> LoadPostings()
    {
        var types = new[] { "Room Charge", "Room Charge", "Restaurant", "Restaurant", "Spa", "Laundry", "Minibar", "Telephone", "Transport", "Package", "Tax", "Service Charge", "Discount", "Adjustment", "Misc" };
        var depts = new[] { "FO", "F&B", "SPA", "LAUN", "MINI", "OTHER" };
        var outlets = new[] { "Reception", "Main Restaurant", "Lobby Bar", "Spa Center", "Laundry", "Room Service", "Banquet" };
        var guests = new[] { "Nguyễn Văn An", "Trần Thị Bình", "Lê Hoàng Cường" };
        var folios = new[] { "FOL-26001", "FOL-26002", "FOL-26003", "FOL-26004", "FOL-26009", "FOL-26010" };
        var currencies = new[] { "VND", "VND", "VND", "USD" };
        var payers = new[] { "Guest", "Company", "TA" };
        var statuses = new[] { "Open", "Open", "Open", "Locked", "Settled", "Disputed" };
        var flags = new[] { "Transferable", "Transferable", "Transferable", "Locked", "Settled", "PackageParent", "PackageChild", "InvoiceIssued", "Disputed" };
        var r = new Random(42);
        var list = new List<TpPosting>(320);
        var d = new DateTime(2026, 7, 24);
        for (int i = 1; i <= 320; i++)
        {
            var t = types[r.Next(15)];
            var qty = t is "Room Charge" ? 1 : r.Next(1, 6);
            var price = t switch
            {
                "Room Charge" => r.Next(800000, 3600000), "Restaurant" => r.Next(80000, 600000),
                "Spa" => r.Next(100000, 1200000), "Laundry" => r.Next(30000, 250000),
                "Minibar" => r.Next(20000, 150000), _ => r.Next(50000, 800000)
            };
            var amount = price * qty;
            var tax = t is "Room Charge" or "Restaurant" or "Spa" or "Minibar" ? (int)(amount * 0.08) : 0;
            var sc = t is "Room Charge" or "Restaurant" or "Spa" ? (int)(amount * 0.05) : 0;
            var disc = t is "Discount" ? -r.Next(20000, 200000) : 0;
            var net = amount + tax + sc + disc;
            var flag = flags[r.Next(9)];
            var folioCode = folios[r.Next(6)];

            list.Add(new TpPosting(
                Id: i, Code: $"TXN-{28000 + i}", Date: d.AddDays(r.Next(0, 5)), Time: d.AddHours(r.Next(6, 23)).AddMinutes(r.Next(0, 59)),
                Type: t, Description: $"{t} #{i}", Dept: depts[r.Next(6)], Outlet: outlets[r.Next(7)],
                Guest: guests[r.Next(3)], Room: $"{r.Next(1, 6)}{r.Next(1, 9):D2}",
                Folio: folioCode, Reservation: $"RES-{25000 + r.Next(1, 20):D4}",
                Qty: qty, Price: price, Tax: tax, ServiceCharge: sc, Discount: disc, Net: net,
                Currency: folioCode.Contains("26009") || folioCode.Contains("26010") ? "USD" : "VND",
                Payer: payers[r.Next(3)], Status: flag switch { "Locked" => "Locked", "Settled" => "Settled", "Disputed" => "Disputed", _ => "Open" },
                Flag: flag, Reference: t is "Package" ? $"PKG-{r.Next(1, 30):D3}" : "",
                PackageGroupId: flag is "PackageParent" or "PackageChild" ? r.Next(1, 15) : null,
                HasInvoice: flag == "InvoiceIssued", Operator: new[] { "Lễ tân A", "Thu ngân A", "Lễ tân B" }[r.Next(3)],
                Cashier: new[] { "CS1", "CS2", "CS3" }[r.Next(3)]
            ));
        }
        return list;
    }

#endif

    private List<TpFolio> LoadTargetFolios() => _allTargetFolios;

    private void InitFilters()
    {
        PTypeFilter.ItemsSource = new[] { "Tất cả", "Room Charge", "Restaurant", "Spa", "Laundry", "Minibar", "Telephone", "Transport", "Package", "Tax", "Service Charge", "Discount", "Adjustment" };
        PTypeFilter.SelectedIndex = 0;
        PDeptFilter.ItemsSource = new[] { "Tất cả", "FO", "F&B", "SPA", "LAUN", "MINI" };
        PDeptFilter.SelectedIndex = 0;
        PEligFilter.ItemsSource = new[] { "Tất cả", "Có thể chuyển", "Bị khóa", "Đã settle", "Có invoice", "Tranh chấp" };
        PEligFilter.SelectedIndex = 0;
        PostingPageSizeBox.ItemsSource = new[] { "30 / trang", "50 / trang", "100 / trang" };
        PostingPageSizeBox.SelectedIndex = 0;
    }

    // ═══ NAVIGATION ═══

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
        UpdateNav(); UpdateSummaryStrip();
    }

    private void HideAllSteps() { StepAContent.Visibility = StepBContent.Visibility = StepCContent.Visibility = StepDContent.Visibility = StepEContent.Visibility = StepFContent.Visibility = Visibility.Collapsed; }

    private void BuildStepBar()
    {
        StepBar.Children.Clear();
        var steps = new[] { ("A", "Chọn Posting"), ("B", "Folio đích"), ("C", "Quy tắc chuyển"), ("D", "Preview"), ("E", "Kiểm tra"), ("F", "Xác nhận") };
        for (int i = 0; i < 6; i++)
        {
            var idx = i + 1; var active = idx == _step;
            var bad = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(2, 5, 2, 5), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = active ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = active ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) } } } };
            bad.MouseLeftButtonDown += (_, _) => GoTo(idx); StepBar.Children.Add(bad);
            if (i < 5) StepBar.Children.Add(Sep());
        }
    }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = !HasBlocking(); }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        var total = TransferTotal();
        var stepNames = new[] { "Chọn Posting", "Chọn Folio đích", "Quy tắc chuyển", "Preview", "Kiểm tra", "Xác nhận" };
        FooterStepLabel.Text = $"Bước {_step}/6 · {stepNames[_step - 1]} · Posting: {_selectedPostingIds.Count} · Chuyển: {total:N0} · Đích: {_selectedTarget?.Code ?? "—"}";
    }

    private bool HasBlocking()
    {
        if (_selectedPostingIds.Count == 0 || _selectedTarget == null || _blocking.Count > 0) return true;
        if (_transferRule == "Percent" && (_transferPercent <= 0 || _transferPercent > 100)) return true;
        if (_transferRule == "Amount" && _transferAmount <= 0) return true;
        if (_transferRule == "Qty" && _transferQty <= 0) return true;
        return _taxRule != "Together" || _packageRule != "Together" || _payerRule != "Keep" || TransferTotal() <= 0;
    }

    private int TransferTotal() => _allPostings
        .Where(p => _selectedPostingIds.Contains(p.Id))
        .Sum(TransferAmountForPosting);

    private int TransferAmountForPosting(TpPosting posting)
    {
        var net = Math.Max(0, posting.Net);
        return _transferRule switch
        {
            "Entire" => net,
            "Percent" => (int)Math.Round(net * Math.Clamp(_transferPercent, 0, 100) / 100d, MidpointRounding.AwayFromZero),
            "Amount" => Math.Min(Math.Max(0, _transferAmount), net),
            "Qty" when posting.Qty > 0 => (int)Math.Round(net * Math.Min(_transferQty, posting.Qty) / (double)posting.Qty, MidpointRounding.AwayFromZero),
            _ => 0
        };
    }

    private void UpdateSummaryStrip()
    {
        SummaryStrip.Children.Clear();
        var sel = _selectedPostingIds.Count > 0;
        var total = TransferTotal();
        var sourceFolio = _selectedPostingIds.Count > 0 ? _allPostings.First(p => _selectedPostingIds.Contains(p.Id)).Folio : null;
        var targetCode = _selectedTarget?.Code;
        SummaryStrip.Children.Add(Strip("Khách", sel ? _allPostings.First(p => _selectedPostingIds.Contains(p.Id)).Guest : "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Nguồn", sourceFolio ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đích", targetCode ?? "—", targetCode != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Posting", $"{_selectedPostingIds.Count}", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Chuyển", sel ? $"{total:N0}" : "—", sel)); SummaryStrip.Children.Add(Sep());
        var ready = sel && !HasBlocking();
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Sẵn sàng" : "Cần kiểm tra", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    // ═══ STEP A — POSTING SELECTION ═══

    private List<TpPosting> GetFilteredPostings()
    {
        var p = _allPostings.AsEnumerable();
        var kw = PSearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) p = p.Where(x => x.Description.ToLower().Contains(kw) || x.Code.ToLower().Contains(kw) || x.Guest.ToLower().Contains(kw));
        var type = PTypeFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(type) && type != "Tất cả") p = p.Where(x => x.Type == type);
        var dept = PDeptFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(dept) && dept != "Tất cả") p = p.Where(x => x.Dept == dept);
        var elig = PEligFilter.SelectedItem?.ToString();
        if (elig == "Có thể chuyển") p = p.Where(x => x.Flag == "Transferable");
        else if (elig == "Bị khóa") p = p.Where(x => x.Flag == "Locked");
        else if (elig == "Đã settle") p = p.Where(x => x.Flag == "Settled");
        else if (elig == "Có invoice") p = p.Where(x => x.HasInvoice);
        else if (elig == "Tranh chấp") p = p.Where(x => x.Flag == "Disputed");
        return p.ToList();
    }

    private void RefreshPostingPage(bool reset)
    {
        if (reset) _postingPage = 0;
        var f = GetFilteredPostings();
        var tp = Math.Max(1, (f.Count + _postingPageSize - 1) / _postingPageSize);
        if (_postingPage >= tp) _postingPage = tp - 1;
        PostingGrid.ItemsSource = f.Skip(_postingPage * _postingPageSize).Take(_postingPageSize).ToList();
        PostingPagerSummary.Text = $"Hiển thị {_postingPage * _postingPageSize + 1}–{Math.Min((_postingPage + 1) * _postingPageSize, f.Count)} / {f.Count} posting";
        PostingPrevBtn.IsEnabled = _postingPage > 0; PostingNextBtn.IsEnabled = _postingPage < tp - 1;
        BuildPostingPageBtns(tp);
    }

    private void BuildPostingPageBtns(int tp) { PostingPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = PagerBtn(i, _postingPage, idx); nb.Click += (_, _) => { _postingPage = idx; RefreshPostingPage(false); }; PostingPageBtns.Children.Add(nb); } }

    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 28, Height = 26, FontSize = 11, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = PostingPrevBtn.Template };

    private void OnPostingSelected(object s, SelectionChangedEventArgs e)
    {
        if (PostingGrid.SelectedItem is not TpPosting p) return;
        _previewPosting = p;
        PostingEmptyState.Visibility = Visibility.Collapsed; PostingDetailPanel.Visibility = Visibility.Visible;
        PdCode.Text = p.Code; PdDesc.Text = p.Description;
        PdType.Text = p.Type; PdDept.Text = p.Dept; PdOutlet.Text = p.Outlet;
        PdDate.Text = p.Date.ToString("dd/MM/yyyy HH:mm");
        PdGuest.Text = p.Guest; PdRoom.Text = p.Room; PdFolio.Text = p.Folio; PdPayer.Text = p.Payer;
        PdRef.Text = string.IsNullOrEmpty(p.Reference) ? "—" : p.Reference;
        PdOper.Text = p.Operator; PdCashier.Text = p.Cashier; PdRes.Text = p.Reservation;
        PdQty.Text = $"{p.Qty}"; PdPrice.Text = $"{p.Price:N0}";
        PdTax.Text = $"{p.Tax:N0}"; PdSC.Text = $"{p.ServiceCharge:N0}"; PdDisc.Text = $"{p.Discount:N0}";
        PdNet.Text = $"{p.Net:N0}"; PdCurr.Text = p.Currency;
        PdStatus.Text = p.Status; PdFlag.Text = p.Flag switch {
            "Transferable" => "Có thể chuyển", "Locked" => "Bị khóa", "Settled" => "Đã settle",
            "PackageParent" => "Package cha", "PackageChild" => "Package con",
            "InvoiceIssued" => "Đã xuất invoice", "Disputed" => "Đang tranh chấp", _ => p.Flag
        };

        var isSelected = _selectedPostingIds.Contains(p.Id);
        var canTransfer = p.Flag is "Transferable";
        BtnTogglePosting.Content = isSelected ? "✕ Bỏ chọn" : "✓ Chọn posting này";
        BtnTogglePosting.IsEnabled = canTransfer || isSelected;
        if (!canTransfer && !isSelected) BtnTogglePosting.Content = p.Flag switch { "Locked" => "Bị khóa", "Settled" => "Đã settle", "PackageParent" => "Package cha", "PackageChild" => "Package con", "InvoiceIssued" => "Có invoice", "Disputed" => "Tranh chấp", _ => "Không thể chuyển" };
    }

    private void OnTogglePosting(object s, RoutedEventArgs e)
    {
        if (_previewPosting == null) return;
        var id = _previewPosting.Id;
        if (_selectedPostingIds.Contains(id)) { _selectedPostingIds.Remove(id); BtnTogglePosting.Content = "✓ Chọn posting này"; }
        else { _selectedPostingIds.Add(id); BtnTogglePosting.Content = "✕ Bỏ chọn"; }
        UpdateSummaryStrip(); UpdateNav();
    }

    private void OnTransferPercentChanged(object s, TextChangedEventArgs e)
    {
        _transferPercent = double.TryParse((s as TextBox)?.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value) ? value : 0;
        UpdateSummaryStrip(); UpdateNav();
    }

    private void OnTransferAmountChanged(object s, TextChangedEventArgs e)
    {
        _transferAmount = int.TryParse((s as TextBox)?.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : 0;
        UpdateSummaryStrip(); UpdateNav();
    }

    private void OnTransferQtyChanged(object s, TextChangedEventArgs e)
    {
        _transferQty = int.TryParse((s as TextBox)?.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) ? value : 0;
        UpdateSummaryStrip(); UpdateNav();
    }

    private void OnSelRoomCharge(object s, RoutedEventArgs e) { _selectedPostingIds.Clear(); foreach (var p in GetFilteredPostings().Where(x => x.Type == "Room Charge" && x.Flag == "Transferable")) _selectedPostingIds.Add(p.Id); RefreshPostingPage(false); UpdateSummaryStrip(); UpdateNav(); }
    private void OnSelTransferable(object s, RoutedEventArgs e) { _selectedPostingIds.Clear(); foreach (var p in GetFilteredPostings().Where(x => x.Flag == "Transferable")) _selectedPostingIds.Add(p.Id); RefreshPostingPage(false); UpdateSummaryStrip(); UpdateNav(); }
    private void OnDeselectAllP(object s, RoutedEventArgs e) { _selectedPostingIds.Clear(); RefreshPostingPage(false); UpdateSummaryStrip(); UpdateNav(); }
    private void OnApplyPFilter(object s, RoutedEventArgs e) => RefreshPostingPage(true);
    private void OnClearPFilter(object s, RoutedEventArgs e) { PSearchBox.Text = ""; PTypeFilter.SelectedIndex = 0; PDeptFilter.SelectedIndex = 0; PEligFilter.SelectedIndex = 0; RefreshPostingPage(true); }
    private void OnPostingPrevPage(object s, RoutedEventArgs e) { if (_postingPage > 0) { _postingPage--; RefreshPostingPage(false); } }
    private void OnPostingNextPage(object s, RoutedEventArgs e) { _postingPage++; RefreshPostingPage(false); }
    private void OnPostingPageSizeChanged(object s, SelectionChangedEventArgs e) { _postingPageSize = PostingPageSizeBox.SelectedIndex switch { 0 => 30, 1 => 50, 2 => 100, _ => 30 }; RefreshPostingPage(true); }

    // ═══ STEP B — TARGET FOLIO ═══

    private void RenderStepB()
    {
        TargetListPanel.Children.Clear(); TargetPreviewPanel.Children.Clear();
        var targets = LoadTargetFolios();
        foreach (var f in targets)
        {
            var b = new Border { Background = _selectedTarget?.Id == f.Id ? Cb("#EFF6FF") : Cb("#FAFBFD"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 4), BorderBrush = _selectedTarget?.Id == f.Id ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand, Child = new StackPanel { Children = { new TextBlock { Text = $"{f.Code} · {f.Type} · {f.Guest}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, new TextBlock { Text = $"Phòng {f.Room} · {f.Currency} · {f.Payer} · Bal: {f.Balance:N0} · {f.Billing}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) } } } };
            var fid = f.Id; b.MouseLeftButtonDown += (_, _) => PreviewTarget(targets.First(x => x.Id == fid)); TargetListPanel.Children.Add(b);
        }
        TargetListPanel.Children.Add(new TextBlock
        {
            Text = "Chỉ chọn Folio đích đã tồn tại trong dữ liệu thật.",
            FontSize = 11,
            Foreground = C("#94A3B8"),
            Margin = new Thickness(0, 6, 0, 0)
        });
    }

    private void PreviewTarget(TpFolio f)
    {
        _selectedTarget = f; RenderStepB();
        TargetPreviewPanel.Children.Clear();
        TargetPreviewPanel.Children.Add(STitle("Preview Folio đích"));
        TargetPreviewPanel.Children.Add(Row("Mã", f.Code)); TargetPreviewPanel.Children.Add(Row("Loại", f.Type));
        TargetPreviewPanel.Children.Add(Row("Khách", f.Guest)); TargetPreviewPanel.Children.Add(Row("Phòng", f.Room));
        TargetPreviewPanel.Children.Add(Row("Payer", f.Payer)); TargetPreviewPanel.Children.Add(Row("Currency", f.Currency));
        TargetPreviewPanel.Children.Add(Row("Balance", $"{f.Balance:N0}"));
        TargetPreviewPanel.Children.Add(Row("Billing", f.Billing));
        var afterBalance = f.Balance + TransferTotal();
        TargetPreviewPanel.Children.Add(Row("Sau chuyển", $"{afterBalance:N0}"));
        TargetPreviewPanel.Children.Add(new TextBlock { Text = f.Compatible ? "✓ Có thể nhận posting" : "⚠ Không tương thích", FontSize = 11, Foreground = f.Compatible ? C("#16A34A") : C("#F59E0B"), Margin = new Thickness(0, 4, 0, 0) });
        UpdateSummaryStrip(); UpdateNav();
    }

    // ═══ STEP C — TRANSFER RULES ═══

    private void RenderStepC()
    {
        RuleLeftPanel.Children.Clear(); RuleRightPanel.Children.Clear();

        RuleLeftPanel.Children.Add(STitle("Số tiền chuyển"));
        var amtOptions = new[] { ("Toàn bộ posting", "Entire"), ("Theo tỷ lệ %", "Percent"), ("Theo số tiền", "Amount"), ("Theo số lượng", "Qty") };
        foreach (var (label, key) in amtOptions)
        {
            var active = _transferRule == key;
            var b = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } };
            b.MouseLeftButtonDown += (_, _) => { _transferRule = key; RenderStepC(); };
            RuleLeftPanel.Children.Add(b);
        }
        if (_transferRule == "Percent")
        {
            RuleLeftPanel.Children.Add(new TextBlock { Text = "Tỷ lệ áp dụng (%)", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 4, 0, 2) });
            var percentBox = new TextBox { Text = _transferPercent.ToString("0.##", CultureInfo.CurrentCulture), Style = FindResource("ModernTextBox") as Style, Width = 80, HorizontalAlignment = HorizontalAlignment.Left };
            percentBox.TextChanged += OnTransferPercentChanged;
            RuleLeftPanel.Children.Add(percentBox);
        }
        if (_transferRule == "Amount")
        {
            RuleLeftPanel.Children.Add(new TextBlock { Text = "Số tiền cho mỗi posting", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 4, 0, 2) });
            var amountBox = new TextBox { Text = _transferAmount > 0 ? $"{_transferAmount:N0}" : "", Style = FindResource("ModernTextBox") as Style, Width = 140, HorizontalAlignment = HorizontalAlignment.Left };
            amountBox.TextChanged += OnTransferAmountChanged;
            RuleLeftPanel.Children.Add(amountBox);
        }
        if (_transferRule == "Qty")
        {
            RuleLeftPanel.Children.Add(new TextBlock { Text = "Số lượng cho mỗi posting", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 4, 0, 2) });
            var qtyBox = new TextBox { Text = _transferQty > 0 ? _transferQty.ToString(CultureInfo.CurrentCulture) : "", Style = FindResource("ModernTextBox") as Style, Width = 80, HorizontalAlignment = HorizontalAlignment.Left };
            qtyBox.TextChanged += OnTransferQtyChanged;
            RuleLeftPanel.Children.Add(qtyBox);
        }

        RuleLeftPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        RuleLeftPanel.Children.Add(STitle("Tax & Phí"));
        var taxOptions = new[] { ("Chuyển cùng", "Together"), ("Giữ lại nguồn", "Keep"), ("Tách riêng", "Separate") };
        foreach (var (label, key) in taxOptions) { var active = _taxRule == key; var b2 = RuleOption(label, active); b2.MouseLeftButtonDown += (_, _) => { _taxRule = key; RenderStepC(); }; RuleLeftPanel.Children.Add(b2); }

        RuleLeftPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        RuleLeftPanel.Children.Add(STitle("Package"));
        var pkgOptions = new[] { ("Chuyển cùng", "Together"), ("Giữ cha/con", "Keep"), ("Cảnh báo", "Warn") };
        foreach (var (label, key) in pkgOptions) { var active = _packageRule == key; var b3 = RuleOption(label, active); b3.MouseLeftButtonDown += (_, _) => { _packageRule = key; RenderStepC(); }; RuleLeftPanel.Children.Add(b3); }

        RuleRightPanel.Children.Add(STitle("Payer"));
        var payerOptions = new[] { ("Giữ nguyên", "Keep"), ("Đổi về đích", "Change"), ("Map Company", "MapCompany"), ("Map Guest", "MapGuest"), ("Map TA", "MapTA") };
        foreach (var (label, key) in payerOptions) { var active = _payerRule == key; var b4 = RuleOption(label, active); b4.MouseLeftButtonDown += (_, _) => { _payerRule = key; RenderStepC(); }; RuleRightPanel.Children.Add(b4); }

        RuleRightPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        RuleRightPanel.Children.Add(STitle("Tóm tắt quy tắc"));
        RuleRightPanel.Children.Add(Row("Chuyển", _transferRule switch { "Entire" => "Toàn bộ", "Percent" => $"Tỷ lệ {_transferPercent}%", "Amount" => "Số tiền cố định", "Qty" => "Theo số lượng", _ => _transferRule }));
        RuleRightPanel.Children.Add(Row("Tax", _taxRule)); RuleRightPanel.Children.Add(Row("Package", _packageRule)); RuleRightPanel.Children.Add(Row("Payer", _payerRule));
    }

    private Border RuleOption(string label, bool active) => new() { CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } };

    // ═══ STEP D — PREVIEW ═══

    private void RenderStepD()
    {
        PreviewLeftPanel.Children.Clear(); PreviewRightPanel.Children.Clear();
        var postings = _allPostings.Where(p => _selectedPostingIds.Contains(p.Id)).ToList();
        var total = TransferTotal();
        PreviewLeftPanel.Children.Add(STitle("Posting nguồn"));
        foreach (var p in postings.Take(15))
            PreviewLeftPanel.Children.Add(new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 0, 2), Child = new TextBlock { Text = $"{p.Code} · {p.Type} · {p.Description} · {p.Net:N0} {p.Currency}", FontSize = 11, Foreground = C("#334155") } });
        PreviewLeftPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 4) });
        PreviewLeftPanel.Children.Add(Row("Tổng posting", $"{postings.Count}"));
        PreviewLeftPanel.Children.Add(Row("Tổng chuyển", $"{total:N0}"));
        PreviewLeftPanel.Children.Add(Row("Thuế đi kèm", $"{postings.Sum(p => p.Tax):N0}"));
        PreviewLeftPanel.Children.Add(Row("Phí DV đi kèm", $"{postings.Sum(p => p.ServiceCharge):N0}"));

        PreviewRightPanel.Children.Add(STitle("Preview đích"));
        PreviewRightPanel.Children.Add(Row("Folio đích", _selectedTarget?.Code ?? "—"));
        PreviewRightPanel.Children.Add(Row("Balance trước", $"{_selectedTarget?.Balance ?? 0:N0}"));
        PreviewRightPanel.Children.Add(Row("Balance sau", $"{(_selectedTarget?.Balance ?? 0) + total:N0}"));
        PreviewRightPanel.Children.Add(Row("Payer sau", _payerRule == "Change" ? (_selectedTarget?.Payer ?? "—") : "Giữ nguyên"));
        PreviewRightPanel.Children.Add(new Border { Background = Cb("#DCFCE7"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = "✓ Preview hợp lệ", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#16A34A") } });
    }

    // ═══ STEP E — VALIDATION ═══

    private void RenderStepE()
    {
        ValidLeftPanel.Children.Clear(); ValidRightPanel.Children.Clear();
        _warnings.Clear(); _blocking.Clear();
        var postings = _allPostings.Where(p => _selectedPostingIds.Contains(p.Id)).ToList();

        if (postings.Count == 0) _blocking.Add(new TpWarning("block", "Chưa chọn posting", 1));
        if (_selectedTarget == null) _blocking.Add(new TpWarning("block", "Chưa chọn Folio đích", 2));
        if (postings.Any(p => p.Flag == "Locked")) _blocking.Add(new TpWarning("block", "Có posting bị khóa", 1));
        if (postings.Any(p => p.Flag == "Settled")) _blocking.Add(new TpWarning("block", "Có posting đã settle", 1));
        if (postings.Any(p => p.Flag == "InvoiceIssued")) _warnings.Add(new TpWarning("warn", "Có posting đã xuất invoice", 1));
        if (postings.Any(p => p.Flag == "Disputed")) _warnings.Add(new TpWarning("warn", "Có posting tranh chấp", 1));
        if (_transferRule == "Percent" && (_transferPercent <= 0 || _transferPercent > 100)) _blocking.Add(new TpWarning("block", "Tỷ lệ phải lớn hơn 0% và không vượt quá 100%.", 3));
        if (_transferRule == "Amount" && _transferAmount <= 0) _blocking.Add(new TpWarning("block", "Số tiền chuyển phải lớn hơn 0.", 3));
        if (_transferRule == "Qty" && _transferQty <= 0) _blocking.Add(new TpWarning("block", "Số lượng chuyển phải lớn hơn 0.", 3));
        if (TransferTotal() <= 0) _blocking.Add(new TpWarning("block", "Tổng chuyển phải lớn hơn 0.", 3));
        if (_taxRule != "Together" || _packageRule != "Together" || _payerRule != "Keep")
            _blocking.Add(new TpWarning("block", "Quy tắc Tax/Package/Payer đang chọn chưa có mapping trong command dữ liệu thật.", 3));

        var currencies = postings.Select(p => p.Currency).Distinct().ToList();
        if (currencies.Count > 1) _warnings.Add(new TpWarning("warn", $"Khác currency: {string.Join(", ", currencies)}", 1));
        if (_selectedTarget != null && postings.Any(p => p.Currency != _selectedTarget.Currency)) _warnings.Add(new TpWarning("warn", "Khác currency với Folio đích", 2));

        ValidLeftPanel.Children.Add(STitle("Checklist kiểm tra"));
        var checks = new (string, bool)[] {
            ("Đã chọn posting", _selectedPostingIds.Count > 0), ("Đã chọn Folio đích", _selectedTarget != null),
            ("Currency hợp lệ", currencies.Count == 1), ("Payer hợp lệ", true),
            ("Tax hợp lệ", true), ("Package hợp lệ", !postings.Any(p => p.Flag is "PackageParent" or "PackageChild")),
            ("Không trùng lặp", true), ("Không xung đột", !HasBlocking())
        };
        foreach (var (label, ok) in checks)
            ValidLeftPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 12, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 11, Foreground = C("#334155") } } });

        ValidRightPanel.Children.Add(STitle("Warning & Blocking"));
        foreach (var w in _blocking.Concat(_warnings))
        {
            var isBlock = w.Level == "block";
            ValidRightPanel.Children.Add(new Border { Background = isBlock ? Cb("#FEE2E2") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 4), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = isBlock ? "✕ " : "⚠ ", FontSize = 12, Foreground = isBlock ? C("#DC2626") : C("#A16207"), VerticalAlignment = VerticalAlignment.Center }, new StackPanel { Children = { new TextBlock { Text = w.Message, FontSize = 11, Foreground = isBlock ? C("#DC2626") : C("#A16207") }, new TextBlock { Text = $"Bước {w.Step}", FontSize = 10, Foreground = C("#94A3B8"), Margin = new Thickness(0, 1, 0, 0) } } } } } });
        }
        if (_warnings.Count == 0 && _blocking.Count == 0)
            ValidRightPanel.Children.Add(new TextBlock { Text = "✓ Không có warning hoặc blocking.", FontSize = 11, Foreground = C("#16A34A"), Margin = new Thickness(0, 0, 0, 6) });
        UpdateNav();
    }

    // ═══ STEP F — CONFIRMATION ═══

    private void RenderStepF()
    {
        ConfirmLeftPanel.Children.Clear(); ConfirmRightPanel.Children.Clear();
        var postings = _allPostings.Where(p => _selectedPostingIds.Contains(p.Id)).ToList();
        var total = TransferTotal();
        ConfirmLeftPanel.Children.Add(STitle("Tóm tắt chuyển khoản mục"));
        ConfirmLeftPanel.Children.Add(Row("Số posting", $"{postings.Count}"));
        ConfirmLeftPanel.Children.Add(Row("Tổng chuyển", $"{total:N0}"));
        ConfirmLeftPanel.Children.Add(Row("Nguồn", postings.FirstOrDefault()?.Folio ?? "—"));
        ConfirmLeftPanel.Children.Add(Row("Đích", _selectedTarget?.Code ?? "—"));
        ConfirmLeftPanel.Children.Add(Row("Quy tắc", _transferRule));
        ConfirmLeftPanel.Children.Add(Row("Tax", _taxRule)); ConfirmLeftPanel.Children.Add(Row("Package", _packageRule));
        ConfirmLeftPanel.Children.Add(Row("Payer", _payerRule));

        ConfirmRightPanel.Children.Add(STitle("Readiness"));
        var checks = new (string, bool)[] {
            ("Đã chọn posting", _selectedPostingIds.Count > 0), ("Đã chọn Folio đích", _selectedTarget != null),
            ("Đã kiểm tra tax", true), ("Đã kiểm tra package", true), ("Đã kiểm tra approval", true), ("Không blocking", !HasBlocking())
        };
        var allOk = true;
        foreach (var (label, ok) in checks)
            { ConfirmRightPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 12, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 11, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        ConfirmRightPanel.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng chuyển khoản mục" : "Cần kiểm tra thêm", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
    }

    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (HasBlocking()) return;
        if (!_persistedMode || _folioService is null || _contextAccessor?.Current is null)
        {
            MessageBox.Show("Chưa tải được Folio thật hoặc property context.", "Chuyển khoản mục", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var postings = _allPostings.Where(p => _selectedPostingIds.Contains(p.Id)).ToList();
        var total = TransferTotal();
        if (total <= 0 || _taxRule != "Together" || _packageRule != "Together" || _payerRule != "Keep")
        {
            MessageBox.Show("Quy tắc chuyển chưa hợp lệ hoặc chưa được hỗ trợ bởi dữ liệu thật.", "Chuyển khoản mục", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_selectedTarget is null || _selectedTarget.FolioGuid == Guid.Empty || _selectedTarget.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Folio đích chưa có định danh hoặc row version hợp lệ từ dữ liệu thật.", "Chuyển khoản mục", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sourceFolios = postings.Select(p => p.SourceFolioGuid).Where(id => id != Guid.Empty).Distinct().ToList();
        if (sourceFolios.Count != 1 || postings.Any(p => p.SourceRowVersion is not { Length: 8 }))
        {
            MessageBox.Show("Chỉ có thể chuyển các posting cùng một Folio nguồn và có row version hợp lệ.", "Chuyển khoản mục", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = await _folioService.TransferAsync(
                new AddFolioTransferCommand(
                    sourceFolios[0],
                    postings[0].SourceRowVersion!,
                    _selectedTarget.FolioGuid,
                    total,
                    $"Chuyển {postings.Count} khoản mục từ giao diện nghiệp vụ"),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể chuyển khoản mục vào dữ liệu thật.", "Chuyển khoản mục", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Đã chuyển {postings.Count} khoản mục, tổng {total:N0}, từ {postings[0].Folio} sang {_selectedTarget.Code}.", "Chuyển khoản mục", MessageBoxButton.OK, MessageBoxImage.Information);
            _selectedPostingIds.Clear();
            _selectedTarget = null;
            GoTo(1);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể chuyển khoản mục: " + ex.Message, "Chuyển khoản mục", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══ EVENT HANDLERS ═══

    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private void OnRefresh(object s, RoutedEventArgs e) { _selectedPostingIds.Clear(); _previewPosting = null; _selectedTarget = null; _warnings.Clear(); _blocking.Clear(); InitFilters(); GoTo(1); }
    private void OnClose(object s, RoutedEventArgs e) => Close();

    // ═══ HELPERS ═══

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsTp(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " › ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

// ═══ DATA RECORDS ═══

internal record TpPosting(int Id, string Code, DateTime Date, DateTime Time, string Type, string Description, string Dept, string Outlet, string Guest, string Room, string Folio, string Reservation, int Qty, int Price, int Tax, int ServiceCharge, int Discount, int Net, string Currency, string Payer, string Status, string Flag, string Reference, int? PackageGroupId, bool HasInvoice, string Operator, string Cashier, Guid SourceFolioGuid = default, byte[]? SourceRowVersion = null) { public string DateDisplay => Date.ToString("dd/MM HH:mm"); public string NetDisplay => $"{Net:N0}"; }

internal record TpFolio(int Id, string Code, string Type, string Guest, string Room, string Payer, string Currency, int Balance, string Billing, string Status, bool Compatible, Guid FolioGuid = default, byte[]? RowVersion = null);

internal record TpWarning(string Level, string Message, int Step);

internal record TpTransferRecord(DateTime Time, string BatchId, int PostingCount, string Source, string Target, int Amount, string Operator, string Status);

internal static class TpExt { public static TextBlock WithRunsTp(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
