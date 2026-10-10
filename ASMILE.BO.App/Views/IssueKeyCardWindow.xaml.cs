using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Stay;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class IssueKeyCardWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private IkGuest? _activeGuest;
    private readonly List<IkGuest> _guests;
    private readonly List<IkCard> _cards;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IKeyCardService? _keyCardService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private int _guestPage, _guestPageSize = 20;
    private int _centerTab;
    private string _cardType = "Guest", _duration = "Checkout", _encoder = "Encoder 1";
    private int _cardQty = 1;
    private bool _revokeOld, _autoActivate = true, _requireSign, _printSlip;
    private readonly List<IkWarning> _warnings = new(), _blocking = new();

    public IssueKeyCardWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _guests = [];
        _cards = [];
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _keyCardService = services?.GetService<IKeyCardService>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        Loaded += async (_, _) => await LoadRealDataAsync();
        InitFilters();
        InitCenterTabs();
        RefreshGuestPage(true);
        UpdateSummaryStrip();
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async Task LoadRealDataAsync()
    {
        _guests.Clear();
        _cards.Clear();
        var context = _executionContext?.Current;
        if (_stayRepository is null || _keyCardService is null || context is null)
        {
            RefreshGuestPage(true);
            UpdateSummaryStrip();
            return;
        }

        var stays = await _stayRepository.GetCurrentInHouseAsync(context.Property.PropertyId.Value, default);
        var guestIndex = 0;
        var cardIndex = 0;
        foreach (var stay in stays)
        {
            guestIndex++;
            var keys = await _keyCardService.GetKeysForStayAsync(stay.HotelStayId, default);
            _guests.Add(new IkGuest(guestIndex, stay.GuestName, stay.RoomNumber, stay.RoomTypeCode,
                stay.ReservationNumber ?? "—", stay.StayNumber, stay.ScheduledArrivalDate.ToString("dd/MM/yyyy"),
                stay.ScheduledDepartureDate.ToString("dd/MM/yyyy"), "In-House", false, null, 0, keys.Count, 4,
                "Standard", "—", stay.HotelStayId, stay.GuestId, stay.RoomId));
            foreach (var key in keys)
            {
                cardIndex++;
                _cards.Add(new IkCard(cardIndex, key.KeyCode, key.Level.ToString(), key.IssuedAtUtc ?? DateTime.MinValue,
                    key.IssuedByName ?? "—", key.ValidUntilUtc ?? stay.ScheduledDepartureDate.ToDateTime(new TimeOnly(12, 0)),
                    key.Status.ToString(), key.EncoderId > 0 ? $"Encoder {key.EncoderId}" : "Chưa ghi nhận",
                    key.IssuedAtUtc ?? DateTime.MinValue, guestIndex));
            }
        }
        RefreshGuestPage(true);
        UpdateSummaryStrip();
        BuildInspector();
        UpdateBottomBar();
    }

    private void InitFilters()
    {
        GuestTypeFilter.ItemsSource = new[] { "Tất cả", "In-House", "Arrival", "Checked-Out" };
        GuestTypeFilter.SelectedIndex = 0;
        GuestPageSizeBox.ItemsSource = new[] { "20 / trang", "50 / trang", "100 / trang" };
        GuestPageSizeBox.SelectedIndex = 0;
    }

    // ═══ CENTER TABS ═══

    private void InitCenterTabs()
    {
        CenterTabs.Children.Clear();
        var tabs = new[] { "Thông tin khách", "Thẻ đang hoạt động", "Cấp thẻ mới", "Quyền truy cập", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _centerTab; var b = TabBtn(tabs[i], active); b.MouseLeftButtonDown += (_, _) => SwitchCenterTab(idx); CenterTabs.Children.Add(b); }
        ShowCenterContent(_centerTab);
    }

    private Border TabBtn(string text, bool active) => new() { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 4, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = text, FontSize = 12, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569") } };

    private void SwitchCenterTab(int idx) { _centerTab = idx; InitCenterTabs(); ShowCenterContent(idx); }

    private void ShowCenterContent(int tab)
    {
        Tab1.Visibility = tab == 0 ? Visibility.Visible : Visibility.Collapsed;
        Tab2.Visibility = tab == 1 ? Visibility.Visible : Visibility.Collapsed;
        Tab3.Visibility = tab == 2 ? Visibility.Visible : Visibility.Collapsed;
        Tab4.Visibility = tab == 3 ? Visibility.Visible : Visibility.Collapsed;
        Tab5.Visibility = tab == 4 ? Visibility.Visible : Visibility.Collapsed;
        switch (tab) { case 0: BuildTab1(); break; case 1: BuildTab2(); break; case 2: BuildTab3(); break; case 3: BuildTab4(); break; case 4: BuildTab5(); break; }
    }

    // ═══ GUEST BROWSER ═══

    private List<IkGuest> GetFilteredGuests()
    {
        var g = _guests.AsEnumerable();
        var kw = GuestSearchBox.Text?.ToLower();
        if (!string.IsNullOrWhiteSpace(kw)) g = g.Where(x => x.Name.ToLower().Contains(kw) || x.Room.Contains(kw) || x.Reservation.ToLower().Contains(kw));
        var type = GuestTypeFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(type) && type != "Tất cả") g = g.Where(x => x.Status == type);
        return g.ToList();
    }

    private void RefreshGuestPage(bool reset)
    {
        if (reset) _guestPage = 0;
        var f = GetFilteredGuests();
        var tp = Math.Max(1, (f.Count + _guestPageSize - 1) / _guestPageSize);
        if (_guestPage >= tp) _guestPage = tp - 1;
        GuestListPanel.Children.Clear();
        foreach (var g in f.Skip(_guestPage * _guestPageSize).Take(_guestPageSize))
        {
            var active = _activeGuest?.Id == g.Id;
            var cards = _cards.Count(c => c.GuestId == g.Id);
            var statusColor = g.Status switch { "In-House" => "#16A34A", "Arrival" => "#2F6BFF", _ => "#94A3B8" };
            var cardLimitColor = cards >= g.MaxCards ? "#DC2626" : cards >= g.MaxCards - 1 ? "#F59E0B" : "#16A34A";

            var sp = new StackPanel();

            // Row 1: Room + Status badge
            var row1 = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            var roomSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            roomSp.Children.Add(new TextBlock { Text = g.Room, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = active ? C("#1E40AF") : C("#102E5A") });
            roomSp.Children.Add(new TextBlock { Text = $"  {g.RoomType}", FontSize = 12, Foreground = C("#64748B"), VerticalAlignment = VerticalAlignment.Center });
            row1.Children.Add(roomSp);
            row1.Children.Add(new Border { Background = Cb(statusColor), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 2, 10, 2), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Child = new TextBlock { Text = g.Status, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
            sp.Children.Add(row1);

            // Row 2: Name + badges
            var row2 = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            var nameSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            nameSp.Children.Add(new TextBlock { Text = g.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#1E40AF") : C("#17365D") });
            if (g.IsVip) nameSp.Children.Add(new Border { Background = Cb("#FEF3C7"), CornerRadius = new CornerRadius(5), Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(6, 0, 0, 0), Child = new TextBlock { Text = "★ VIP", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#F59E0B") } });
            else if (g.Membership != "Standard") nameSp.Children.Add(new Border { Background = Cb("#F1F5F9"), CornerRadius = new CornerRadius(5), Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(6, 0, 0, 0), Child = new TextBlock { Text = g.Membership, FontSize = 10, Foreground = C("#64748B") } });
            row2.Children.Add(nameSp);
            sp.Children.Add(row2);

            // Row 3: Dates + Company
            var row3 = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            row3.Children.Add(new TextBlock { Text = $"{g.Arrival} → {g.Departure}", FontSize = 11, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Left });
            if (!string.IsNullOrEmpty(g.Company)) row3.Children.Add(new TextBlock { Text = g.Company, FontSize = 10, Foreground = C("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Right });
            sp.Children.Add(row3);

            // Row 4: Cards + Balance
            var row4 = new StackPanel { Orientation = Orientation.Horizontal };
            row4.Children.Add(new TextBlock { Text = "🪪", FontSize = 11, Margin = new Thickness(0, 0, 4, 0) });
            row4.Children.Add(new TextBlock { Text = "Thẻ: ", FontSize = 11, Foreground = C("#94A3B8") });
            row4.Children.Add(new TextBlock { Text = $"{cards}/{g.MaxCards}", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C(cardLimitColor) });
            if (cards >= g.MaxCards) row4.Children.Add(new TextBlock { Text = " (đã đạt tối đa)", FontSize = 10, Foreground = C("#DC2626"), Margin = new Thickness(2, 0, 0, 0) });
            if (g.Balance > 0) row4.Children.Add(new TextBlock { Text = $"  ·  Balance: {g.Balance:N0}đ", FontSize = 10, Foreground = C("#DC2626"), Margin = new Thickness(4, 0, 0, 0) });
            sp.Children.Add(row4);

            var b = new Border { Background = active ? Cb("#EFF6FF") : Cb("#FAFBFD"), BorderBrush = active ? Cb("#BFDBFE") : Cb("#E2E8F0"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(12, 10, 12, 10), Cursor = System.Windows.Input.Cursors.Hand, Child = sp };
            var gid = g.Id; b.MouseLeftButtonDown += (_, _) => SelectGuest(gid); GuestListPanel.Children.Add(b);
        }
        GuestPagerSummary.Text = $"Hiển thị {_guestPage * _guestPageSize + 1}–{Math.Min((_guestPage + 1) * _guestPageSize, f.Count)} / {f.Count} khách";
        GuestPrevBtn.IsEnabled = _guestPage > 0; GuestNextBtn.IsEnabled = _guestPage < tp - 1;
        BuildGuestPageBtns(tp);
    }

    private void BuildGuestPageBtns(int tp) { GuestPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = PagerBtn(i, _guestPage, idx); nb.Click += (_, _) => { _guestPage = idx; RefreshGuestPage(false); }; GuestPageBtns.Children.Add(nb); } }

    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 28, Height = 26, FontSize = 11, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = GuestPrevBtn.Template };

    private void SelectGuest(int id)
    {
        _activeGuest = _guests.First(g => g.Id == id);
        RefreshGuestPage(false);
        UpdateSummaryStrip();
        BuildTab1(); BuildTab2(); BuildTab3(); BuildInspector();
        UpdateBottomBar();
    }

    private void UpdateSummaryStrip()
    {
        SummaryStrip.Children.Clear();
        if (_activeGuest == null) { SummaryStrip.Children.Add(new TextBlock { Text = "Chưa chọn khách", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return; }
        var g = _activeGuest;
        var cards = _cards.Count(c => c.GuestId == g.Id);
        SummaryStrip.Children.Add(Strip("Khách", g.Name, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", g.Room, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Loại phòng", g.RoomType, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Res", g.Reservation, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đến", g.Arrival, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đi", g.Departure, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Trạng thái", g.Status, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Thẻ", $"{cards}/{g.MaxCards}", cards <= g.MaxCards)); SummaryStrip.Children.Add(Sep());
        if (g.IsVip) { SummaryStrip.Children.Add(new Border { Background = Cb("#FEF3C7"), CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 1, 6, 1), Child = new TextBlock { Text = "★ VIP", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#F59E0B") } }); }
    }

    // ═══ TAB 1 — GUEST INFO ═══

    private void BuildTab1() { Tab1Panel.Children.Clear(); if (_activeGuest == null) return; var g = _activeGuest; Tab1Panel.Children.Add(Row("Tên", g.Name)); Tab1Panel.Children.Add(Row("Reservation", g.Reservation)); Tab1Panel.Children.Add(Row("Stay", g.Stay)); Tab1Panel.Children.Add(Row("Phòng", $"{g.Room} ({g.RoomType})")); Tab1Panel.Children.Add(Row("Đến", g.Arrival)); Tab1Panel.Children.Add(Row("Đi", g.Departure)); Tab1Panel.Children.Add(Row("Công ty", g.Company ?? "—")); Tab1Panel.Children.Add(Row("Membership", g.Membership)); Tab1Panel.Children.Add(Row("Quốc tịch", g.Nationality)); Tab1Panel.Children.Add(Row("Balance", $"{g.Balance:N0} VND")); }

    // ═══ TAB 2 — ACTIVE CARDS ═══

    private void BuildTab2()
    {
        Tab2Panel.Children.Clear();
        if (_activeGuest == null) return;
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = _cards.Where(c => c.GuestId == _activeGuest.Id).ToList() };
        dg.Columns.Add(C("Số thẻ", "Code", 95)); dg.Columns.Add(C("Loại", "Type", 70));
        dg.Columns.Add(C("Ngày cấp", "IssuedDisplay", 110)); dg.Columns.Add(C("Người cấp", "IssuedBy", 85));
        dg.Columns.Add(C("Hết hạn", "ExpireDisplay", 110)); dg.Columns.Add(C("Trạng thái", "Status", 75));
        dg.Columns.Add(C("Encoder", "Encoder", 85)); dg.Columns.Add(C("SD cuối", "LastUsedDisplay", 110));
        Tab2Panel.Children.Add(dg);
    }

    // ═══ TAB 3 — ISSUE NEW CARD ═══

    private void BuildTab3()
    {
        Tab3Panel.Children.Clear();
        if (_activeGuest == null) { Tab3Panel.Children.Add(new TextBlock { Text = "Chọn khách trước khi cấp thẻ.", FontSize = 13, Foreground = C("#94A3B8") }); return; }

        var g = _activeGuest;
        // Section: Card Type
        Tab3Panel.Children.Add(STitle("Loại thẻ"));
        var types = new[] { "Guest", "Extra", "VIP", "Staff", "Housekeeping" };
        var typePanel = new WrapPanel();
        foreach (var t in types) { var active = _cardType == t; var b = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = t, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } }; b.MouseLeftButtonDown += (_, _) => { _cardType = t; BuildTab3(); UpdateBottomBar(); }; typePanel.Children.Add(b); }
        Tab3Panel.Children.Add(typePanel);

        // Section: Quantity
        Tab3Panel.Children.Add(STitle("Số lượng"));
        var qtyPanel = new StackPanel { Orientation = Orientation.Horizontal };
        for (int q = 1; q <= 4; q++) { var activeQ = _cardQty == q; var qb = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(0, 0, 6, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = activeQ ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = activeQ ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = q.ToString(), FontSize = 12, FontWeight = activeQ ? FontWeights.SemiBold : FontWeights.Normal, Foreground = activeQ ? C("#1E40AF") : C("#475569") } }; var qv = q; qb.MouseLeftButtonDown += (_, _) => { _cardQty = qv; BuildTab3(); UpdateBottomBar(); }; qtyPanel.Children.Add(qb); }
        Tab3Panel.Children.Add(qtyPanel);

        // Section: Duration
        Tab3Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        Tab3Panel.Children.Add(STitle("Thời hạn"));
        var durOptions = new[] { ("Theo checkout", "Checkout"), ("Tùy chỉnh", "Custom") };
        var durPanel = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (label, key) in durOptions) { var activeD = _duration == key; var db = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = activeD ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = activeD ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = activeD ? FontWeights.SemiBold : FontWeights.Normal, Foreground = activeD ? C("#1E40AF") : C("#475569") } }; db.MouseLeftButtonDown += (_, _) => { _duration = key; BuildTab3(); UpdateBottomBar(); }; durPanel.Children.Add(db); }
        Tab3Panel.Children.Add(durPanel);

        // Section: Encoder
        Tab3Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        Tab3Panel.Children.Add(STitle("Encoder"));
        var encs = new[] { "Encoder 1", "Encoder 2", "Encoder 3" };
        var encPanel = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var e in encs) { var activeE = _encoder == e; var eb = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = activeE ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = activeE ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = e, FontSize = 11, FontWeight = activeE ? FontWeights.SemiBold : FontWeights.Normal, Foreground = activeE ? C("#1E40AF") : C("#475569") } }; eb.MouseLeftButtonDown += (_, _) => { _encoder = e; BuildTab3(); UpdateBottomBar(); }; encPanel.Children.Add(eb); }
        Tab3Panel.Children.Add(encPanel);

        // Section: Options
        Tab3Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        Tab3Panel.Children.Add(STitle("Tùy chọn"));
        var opts = new (string, bool, Action<bool>)[] { ("Thu hồi thẻ cũ", _revokeOld, v => _revokeOld = v), ("Tự kích hoạt", _autoActivate, v => _autoActivate = v), ("Yêu cầu ký nhận", _requireSign, v => _requireSign = v), ("In phiếu bàn giao", _printSlip, v => _printSlip = v) };
        foreach (var (label, checked_, setter) in opts) { var cbPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0), Cursor = System.Windows.Input.Cursors.Hand }; cbPanel.Children.Add(new TextBlock { Text = checked_ ? "☑ " : "☐ ", FontSize = 13, Foreground = checked_ ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); cbPanel.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = C("#334155"), VerticalAlignment = VerticalAlignment.Center }); cbPanel.MouseLeftButtonDown += (_, _) => { setter(!checked_); BuildTab3(); UpdateBottomBar(); }; Tab3Panel.Children.Add(cbPanel); }
    }

    // ═══ TAB 4 — ACCESS RIGHTS ═══

    private void BuildTab4()
    {
        Tab4Panel.Children.Clear();
        if (_activeGuest == null) return;
        var areas = new[] { "Phòng " + _activeGuest.Room, "Thang máy", "Parking", "Gym", "Pool", "Spa", "Executive Lounge", "Meeting Room", "Restaurant" };
        foreach (var a in areas)
        {
            var hasAccess = a.StartsWith("Phòng") || a is "Thang máy" or "Parking" or "Gym" or "Pool";
            Tab4Panel.Children.Add(new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 3), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = a, FontSize = 11, Foreground = C("#17365D"), Width = 160 }, new TextBlock { Text = hasAccess ? "✓ Cho phép" : "○ Hạn chế", FontSize = 11, Foreground = hasAccess ? C("#16A34A") : C("#94A3B8") } } } });
        }
    }

    // ═══ TAB 5 — HISTORY ═══

    private void BuildTab5()
    {
        Tab5Panel.Children.Clear();
        if (_activeGuest == null) return;
        Tab5Panel.Children.Add(new TextBlock { Text = "Chưa có API đọc lịch sử key-card theo stay. Danh sách thẻ bên trên là dữ liệu thật hiện tại; không hiển thị timeline mẫu.", FontSize = 11, Foreground = C("#A16207"), TextWrapping = TextWrapping.Wrap });
    }

    // ═══ INSPECTOR ═══

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_activeGuest == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn khách để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        var g = _activeGuest; var cards = _cards.Count(c => c.GuestId == g.Id);
        InspectorPanel.Children.Add(STitle("Thông tin lưu trú"));
        InspectorPanel.Children.Add(Row("Khách", g.Name)); InspectorPanel.Children.Add(Row("Phòng", $"{g.Room} ({g.RoomType})"));
        InspectorPanel.Children.Add(Row("Reservation", g.Reservation)); InspectorPanel.Children.Add(Row("Stay", g.Stay));
        InspectorPanel.Children.Add(Row("Trạng thái", g.Status));

        InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        InspectorPanel.Children.Add(STitle("Khóa cửa"));
        InspectorPanel.Children.Add(Row("Trạng thái", "Dữ liệu thẻ từ cơ sở dữ liệu"));
        InspectorPanel.Children.Add(Row("Encoder", _encoder));

        InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        InspectorPanel.Children.Add(STitle("Thẻ"));
        InspectorPanel.Children.Add(Row("Đang hoạt động", $"{cards}"));
        InspectorPanel.Children.Add(Row("Tối đa", $"{g.MaxCards}"));
        var overLimit = cards >= g.MaxCards;
        if (overLimit) InspectorPanel.Children.Add(new TextBlock { Text = "⚠ Đã đạt số lượng thẻ tối đa", FontSize = 11, Foreground = C("#F59E0B"), Margin = new Thickness(0, 4, 0, 0) });

        // Warnings
        InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        InspectorPanel.Children.Add(STitle("Cảnh báo"));
        _warnings.Clear(); _blocking.Clear();
        if (g.Status == "Checked-Out") _blocking.Add(new IkWarning("block", "Khách đã check-out"));
        if (g.Status == "Arrival" && g.Arrival != "25/07/2026") _blocking.Add(new IkWarning("block", "Khách chưa đến ngày nhận phòng"));
        if (cards >= g.MaxCards) _blocking.Add(new IkWarning("block", "Đã đạt số lượng thẻ tối đa"));
        if (g.Balance > 5000000) _warnings.Add(new IkWarning("warn", $"Balance lớn: {g.Balance:N0} VND"));
        if (g.IsVip) _warnings.Add(new IkWarning("warn", "Khách VIP — yêu cầu thẻ VIP"));
        if (g.Departure == "26/07/2026") _warnings.Add(new IkWarning("warn", "Check-out hôm nay"));

        foreach (var w in _blocking.Concat(_warnings))
        {
            var isBlock = w.Level == "block";
            InspectorPanel.Children.Add(new Border { Background = isBlock ? Cb("#FEE2E2") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 3), Child = new TextBlock { Text = (isBlock ? "✕ " : "⚠ ") + w.Message, FontSize = 10, Foreground = isBlock ? C("#DC2626") : C("#A16207") } });
        }
        UpdateBottomBar();
    }

    // ═══ BOTTOM BAR ═══

    private void UpdateBottomBar()
    {
        BottomBar.Children.Clear();
        if (_activeGuest == null) { BottomBar.Children.Add(new TextBlock { Text = "Chọn khách để cấp thẻ.", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return; }
        var g = _activeGuest; var cards = _cards.Count(c => c.GuestId == g.Id);
        var expiry = _duration == "Checkout" ? $"{g.Departure} 12:00" : "Tùy chỉnh";

        BottomBar.Children.Add(BtmItem("Thẻ hiện có", $"{cards}/{g.MaxCards}", cards >= g.MaxCards ? "#DC2626" : "#64748B"));
        BottomBar.Children.Add(BtmItem("Loại", _cardType, "#2F6BFF"));
        BottomBar.Children.Add(BtmItem("Số lượng", $"{_cardQty}", "#475569"));
        BottomBar.Children.Add(BtmItem("Hết hạn", expiry, "#64748B"));
        BottomBar.Children.Add(BtmItem("Encoder", _encoder, "#64748B"));

        var hasBlocking = _blocking.Count > 0;
        if (hasBlocking) BottomBar.Children.Add(new Border { Background = Cb("#FEE2E2"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 3, 8, 3), Margin = new Thickness(8, 0, 0, 0), Child = new TextBlock { Text = $"✕ {_blocking.Count} chặn", FontSize = 11, Foreground = C("#DC2626") } });
        if (_warnings.Count > 0) BottomBar.Children.Add(new Border { Background = Cb("#FEF9C3"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 3, 8, 3), Margin = new Thickness(4, 0, 0, 0), Child = new TextBlock { Text = $"⚠ {_warnings.Count} cảnh báo", FontSize = 11, Foreground = C("#A16207") } });

        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 0, 0, 0) };
        var cancelBtn = new Button { Content = "Hủy", Style = FindResource("SecondaryButton") as Style, Height = 30, FontSize = 11 };
        cancelBtn.Click += (_, _) => Close();
        var issueBtn = new Button { Content = "Cấp thẻ", Style = FindResource("PrimaryButton") as Style, Height = 30, FontSize = 11, Margin = new Thickness(6, 0, 0, 0), IsEnabled = !hasBlocking };
        issueBtn.Click += (_, _) => IssueCard();
        sp.Children.Add(cancelBtn); sp.Children.Add(issueBtn);
        BottomBar.Children.Add(sp);
    }

    private FrameworkElement BtmItem(string label, string value, string color) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 12, 0), Children = { new TextBlock { Text = label + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = value, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C(color), VerticalAlignment = VerticalAlignment.Center } } };

    private async void IssueCard()
    {
        if (_activeGuest == null || _blocking.Count > 0) return;
        if (_keyCardService is null)
        {
            MessageBox.Show("Không tìm thấy dịch vụ cấp thẻ dữ liệu thật.", "Cấp thẻ phòng", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var level = _cardType switch
            {
                "Housekeeping" => AccessLevel.Housekeeping,
                "Staff" or "VIP" => AccessLevel.Supervisor,
                _ => AccessLevel.Guest
            };
            var encoderId = int.TryParse(_encoder.Replace("Encoder ", "", StringComparison.OrdinalIgnoreCase), out var parsedEncoder) ? parsedEncoder : 0;
            var validUntil = _duration == "Checkout" ? DateOnly.ParseExact(_activeGuest.Departure, "dd/MM/yyyy").ToDateTime(new TimeOnly(12, 0)) : (DateTime?)null;
            for (var i = 0; i < _cardQty; i++)
            {
                await _keyCardService.IssueKeyAsync(_activeGuest.GuestId, _activeGuest.StayId, _activeGuest.RoomId, level, validUntil, encoderId, default);
            }
            var selectedStay = _activeGuest.StayId;
            MessageBox.Show($"Đã lưu {_cardQty} thẻ vào dữ liệu thật cho {_activeGuest.Name}. Thiết bị encoder vật lý vẫn là điểm tích hợp riêng.", "Cấp thẻ phòng", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadRealDataAsync();
            _activeGuest = _guests.FirstOrDefault(x => x.StayId == selectedStay);
            RefreshGuestPage(false);
            UpdateSummaryStrip();
            BuildTab2();
            UpdateBottomBar();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể cấp thẻ: {ex.Message}", "Cấp thẻ phòng", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══ EVENT HANDLERS ═══

    private void OnApplyGuestFilter(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilter(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestTypeFilter.SelectedIndex = 0; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }
    private async void OnRefresh(object s, RoutedEventArgs e) { _activeGuest = null; _warnings.Clear(); _blocking.Clear(); InitFilters(); InitCenterTabs(); await LoadRealDataAsync(); }
    private void OnClose(object s, RoutedEventArgs e) => Close();

    // ═══ HELPERS ═══

    private static DataGridTextColumn C(string h, string b, double w) => new() { Header = h, Binding = new System.Windows.Data.Binding(b), Width = w };
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsIk(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record IkGuest(int Id, string Name, string Room, string RoomType, string Reservation, string Stay, string Arrival, string Departure, string Status, bool IsVip, string? Company, int Balance, int CardCount, int MaxCards, string Membership, string Nationality, Guid StayId, Guid? GuestId, Guid RoomId);
internal record IkCard(int Id, string Code, string Type, DateTime Issued, string IssuedBy, DateTime Expire, string Status, string Encoder, DateTime LastUsed, int GuestId) { public string IssuedDisplay => Issued.ToString("dd/MM/yyyy HH:mm"); public string ExpireDisplay => Expire.ToString("dd/MM/yyyy HH:mm"); public string LastUsedDisplay => LastUsed.ToString("dd/MM/yyyy HH:mm"); }
internal record IkWarning(string Level, string Message);
internal static class IkExt { public static TextBlock WithRunsIk(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
