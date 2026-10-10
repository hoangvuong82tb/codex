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

public partial class ReissueKeyCardWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private RkGuest? _activeGuest;
    private RkCard? _selectedCard;
    private readonly List<RkGuest> _guests;
    private readonly List<RkCard> _cards;
    private readonly List<RkAudit> _audit = new();
    private int _guestPage, _guestPageSize = 20;
    private int _centerTab;
    private string _reason = "Lost", _mode = "Replace", _oldCardRule = "DisableNow";
    private int _cardQty = 1;
    private string _expiry = "Keep", _encoder = "Encoder 1";
    private readonly List<RkWarning> _warnings = new(), _blocking = new();
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IKeyCardService? _keyCardService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;

    public ReissueKeyCardWindow()
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

    private async Task LoadRealDataAsync(Guid? preserveStayId = null)
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
            _guests.Add(new RkGuest(guestIndex, stay.GuestName, stay.RoomNumber, stay.RoomTypeCode,
                stay.ReservationNumber ?? "—", stay.StayNumber, stay.ScheduledArrivalDate.ToString("dd/MM/yyyy"),
                stay.ScheduledDepartureDate.ToString("dd/MM/yyyy"), "In-House", false, null, 0, 4, "Standard",
                stay.HotelStayId, stay.GuestId, stay.RoomId));
            foreach (var key in keys)
            {
                cardIndex++;
                _cards.Add(new RkCard(cardIndex, key.KeyCode, key.Level.ToString(), key.IssuedAtUtc ?? DateTime.MinValue,
                    key.IssuedByName ?? "—", key.ValidUntilUtc ?? stay.ScheduledDepartureDate.ToDateTime(new TimeOnly(12, 0)),
                    key.Status.ToString(), key.EncoderId > 0 ? $"Encoder {key.EncoderId}" : "Chưa ghi nhận",
                    key.IssuedAtUtc ?? DateTime.MinValue, guestIndex, key.KeyId));
            }
        }

        _activeGuest = preserveStayId is { } stayId ? _guests.FirstOrDefault(x => x.StayId == stayId) : null;
        _selectedCard = null;
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

    // ═══ TABS ═══

    private void InitCenterTabs()
    {
        CenterTabs.Children.Clear();
        var tabs = new[] { "Thông tin khách", "Thẻ hiện tại", "Cấp lại", "Xử lý thẻ cũ", "Lịch sử" };
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

    private List<RkGuest> GetFilteredGuests()
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
            var needsReissue = _cards.Any(c => c.GuestId == g.Id && c.NeedsReissue);
            var statusColor = g.Status switch { "In-House" => "#16A34A", "Arrival" => "#2F6BFF", _ => "#94A3B8" };

            var sp = new StackPanel();
            var row1 = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            var roomSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            roomSp.Children.Add(new TextBlock { Text = g.Room, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = active ? C("#1E40AF") : C("#102E5A") });
            roomSp.Children.Add(new TextBlock { Text = $"  {g.RoomType}", FontSize = 12, Foreground = C("#64748B"), VerticalAlignment = VerticalAlignment.Center });
            row1.Children.Add(roomSp);
            var badgeStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            badgeStack.Children.Add(new Border { Background = Cb(statusColor), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 2, 10, 2), Child = new TextBlock { Text = g.Status, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
            if (needsReissue) badgeStack.Children.Add(new Border { Background = Cb("#F59E0B"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(4, 0, 0, 0), Child = new TextBlock { Text = "Cần cấp lại", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
            row1.Children.Add(badgeStack);
            sp.Children.Add(row1);

            var row2 = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            var nameSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            nameSp.Children.Add(new TextBlock { Text = g.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#1E40AF") : C("#17365D") });
            if (g.IsVip) nameSp.Children.Add(new Border { Background = Cb("#FEF3C7"), CornerRadius = new CornerRadius(5), Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(6, 0, 0, 0), Child = new TextBlock { Text = "★ VIP", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#F59E0B") } });
            row2.Children.Add(nameSp);
            sp.Children.Add(row2);

            var row3 = new StackPanel { Orientation = Orientation.Horizontal };
            row3.Children.Add(new TextBlock { Text = "🪪", FontSize = 11, Margin = new Thickness(0, 0, 4, 0) });
            row3.Children.Add(new TextBlock { Text = $"Thẻ: {cards}  ·  {g.Arrival} → {g.Departure}", FontSize = 11, Foreground = C("#94A3B8") });
            if (g.Balance > 0) row3.Children.Add(new TextBlock { Text = $"  ·  Balance: {g.Balance:N0}đ", FontSize = 10, Foreground = C("#DC2626"), Margin = new Thickness(4, 0, 0, 0) });
            sp.Children.Add(row3);

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
        _activeGuest = _guests.First(g => g.Id == id); _selectedCard = null;
        RefreshGuestPage(false); UpdateSummaryStrip();
        BuildTab1(); BuildTab2(); BuildTab3(); BuildTab4(); BuildInspector(); UpdateBottomBar();
    }

    private void UpdateSummaryStrip()
    {
        SummaryStrip.Children.Clear();
        if (_activeGuest == null) { SummaryStrip.Children.Add(new TextBlock { Text = "Chưa chọn khách", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return; }
        var g = _activeGuest; var cards = _cards.Count(c => c.GuestId == g.Id);
        SummaryStrip.Children.Add(Strip("Khách", g.Name, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", $"{g.Room} ({g.RoomType})", true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Res", g.Reservation, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đến", g.Arrival, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đi", g.Departure, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Thẻ", $"{cards}", true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Trạng thái", g.Status, true));
        if (g.IsVip) { SummaryStrip.Children.Add(Sep()); SummaryStrip.Children.Add(new Border { Background = Cb("#FEF3C7"), CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 1, 6, 1), Child = new TextBlock { Text = "★ VIP", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#F59E0B") } }); }
    }

    // ═══ TAB 1 ═══

    private void BuildTab1() { Tab1Panel.Children.Clear(); if (_activeGuest == null) return; var g = _activeGuest; Tab1Panel.Children.Add(Row("Tên", g.Name)); Tab1Panel.Children.Add(Row("Reservation", g.Reservation)); Tab1Panel.Children.Add(Row("Stay", g.Stay)); Tab1Panel.Children.Add(Row("Phòng", $"{g.Room} ({g.RoomType})")); Tab1Panel.Children.Add(Row("Đến", g.Arrival)); Tab1Panel.Children.Add(Row("Đi", g.Departure)); Tab1Panel.Children.Add(Row("Công ty", g.Company ?? "—")); Tab1Panel.Children.Add(Row("Membership", g.Membership)); if (g.IsVip) Tab1Panel.Children.Add(Row("VIP", "Có")); }

    // ═══ TAB 2 — CURRENT CARDS ═══

    private void BuildTab2()
    {
        Tab2Panel.Children.Clear();
        if (_activeGuest == null) return;
        var guestCards = _cards.Where(c => c.GuestId == _activeGuest.Id).ToList();
        Tab2Panel.Children.Add(STitle($"Thẻ của {_activeGuest.Name} ({guestCards.Count} thẻ)"));
        foreach (var c in guestCards)
        {
            var active = _selectedCard?.Id == c.Id;
            var statusColor = c.Status switch { "Active" => "#16A34A", "Lost" => "#DC2626", "Broken" => "#F59E0B", "Expired" => "#94A3B8", "Returned" => "#64748B", "Disabled" => "#DC2626", _ => "#64748B" };
            var b = new Border { Background = active ? Cb("#EFF6FF") : Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 4), BorderBrush = active ? Cb("#2F6BFF") : Cb("#E2E8F0"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand, Child = new StackPanel { Children = { new Grid { Margin = new Thickness(0, 0, 0, 3), Children = { new TextBlock { Text = c.Code, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#1E40AF") : C("#17365D"), HorizontalAlignment = HorizontalAlignment.Left }, new Border { Background = Cb(statusColor), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 1, 8, 1), HorizontalAlignment = HorizontalAlignment.Right, Child = new TextBlock { Text = c.Status, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } } } }, new TextBlock { Text = $"Loại: {c.Type} · Cấp: {c.Issued:dd/MM HH:mm} · Hết hạn: {c.Expire:dd/MM HH:mm} · Encoder: {c.Encoder}", FontSize = 11, Foreground = C("#64748B") }, new TextBlock { Text = $"SD cuối: {c.LastUsed:dd/MM HH:mm} · Người cấp: {c.IssuedBy}", FontSize = 10, Foreground = C("#94A3B8"), Margin = new Thickness(0, 1, 0, 0) } } } };
            var cid = c.Id; b.MouseLeftButtonDown += (_, _) => { _selectedCard = guestCards.First(x => x.Id == cid); BuildTab2(); BuildTab3(); UpdateBottomBar(); };
            Tab2Panel.Children.Add(b);
        }
    }

    // ═══ TAB 3 — REISSUE ═══

    private void BuildTab3()
    {
        Tab3Panel.Children.Clear();
        if (_activeGuest == null) { Tab3Panel.Children.Add(new TextBlock { Text = "Chọn khách trước khi cấp lại thẻ.", FontSize = 13, Foreground = C("#94A3B8") }); return; }
        if (_selectedCard == null) { Tab3Panel.Children.Add(new TextBlock { Text = "Chọn thẻ cần cấp lại ở Tab 2.", FontSize = 13, Foreground = C("#94A3B8") }); return; }

        var c = _selectedCard;
        Tab3Panel.Children.Add(new Border { Background = Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 10), Child = new StackPanel { Children = { new TextBlock { Text = $"Thẻ cần cấp lại: {c.Code}", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#DC2626") }, new TextBlock { Text = $"Trạng thái: {c.Status} · Loại: {c.Type} · Cấp lúc: {c.Issued:dd/MM HH:mm}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) } } } });

        // Reason
        Tab3Panel.Children.Add(STitle("Lý do cấp lại"));
        var reasons = new[] { "Lost", "Broken", "Demagnetized", "Forgot Inside Room", "Damaged", "Security", "Guest Request", "Other" };
        var reasonLabels = new[] { "Mất thẻ", "Hỏng", "Khử từ", "Quên trong phòng", "Hư hại", "An ninh", "Khách yêu cầu", "Khác" };
        var wp = new WrapPanel();
        for (int i = 0; i < reasons.Length; i++) { var active = _reason == reasons[i]; var label = reasonLabels[i]; var key = reasons[i]; var rb = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } }; rb.MouseLeftButtonDown += (_, _) => { _reason = key; BuildTab3(); UpdateBottomBar(); }; wp.Children.Add(rb); }
        Tab3Panel.Children.Add(wp);

        // Replacement Mode
        Tab3Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 8) });
        Tab3Panel.Children.Add(STitle("Chế độ thay thế"));
        var modes = new[] { ("Thay thế ngay", "Replace"), ("Cấp thêm", "Additional"), ("Thẻ tạm", "Temp"), ("Thẻ khẩn cấp", "Emergency") };
        var modePanel = new WrapPanel();
        foreach (var (label, key) in modes) { var active = _mode == key; var mb = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } }; mb.MouseLeftButtonDown += (_, _) => { _mode = key; BuildTab3(); UpdateBottomBar(); }; modePanel.Children.Add(mb); }
        Tab3Panel.Children.Add(modePanel);

        // Expiry + Encoder
        Tab3Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 8) });
        Tab3Panel.Children.Add(STitle("Thời hạn & Encoder"));
        var expOpts = new[] { ("Giữ nguyên hạn cũ", "Keep"), ("Tùy chỉnh", "Custom") };
        var expPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        foreach (var (label, key) in expOpts) { var active = _expiry == key; var eb = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } }; eb.MouseLeftButtonDown += (_, _) => { _expiry = key; BuildTab3(); UpdateBottomBar(); }; expPanel.Children.Add(eb); }
        Tab3Panel.Children.Add(expPanel);

        var encs = new[] { "Encoder 1", "Encoder 2", "Encoder 3" };
        var encPanel = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var e in encs) { var active = _encoder == e; var eb2 = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = e, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } }; eb2.MouseLeftButtonDown += (_, _) => { _encoder = e; BuildTab3(); UpdateBottomBar(); }; encPanel.Children.Add(eb2); }
        Tab3Panel.Children.Add(encPanel);
    }

    // ═══ TAB 4 — OLD CARD HANDLING ═══

    private void BuildTab4()
    {
        Tab4Panel.Children.Clear();
        if (_activeGuest == null) return;
        var rules = new (string, string, string[])[] {
            ("DisableNow", "Vô hiệu hóa ngay", new[]{"Thẻ cũ sẽ bị vô hiệu hóa ngay lập tức khi thẻ mới được cấp.", "Phù hợp với thẻ bị mất hoặc vấn đề an ninh."}),
            ("KeepActive", "Giữ thẻ cũ hoạt động", new[]{"Thẻ cũ vẫn hoạt động song song với thẻ mới.", "Phù hợp khi cần thêm thẻ phụ."}),
            ("Disable30Min", "Vô hiệu hóa sau 30 phút", new[]{"Thẻ cũ tự động vô hiệu hóa sau 30 phút.", "Cho phép khách có thời gian chuyển đổi."}),
            ("DisableCheckout", "Vô hiệu hóa khi check-out", new[]{"Thẻ cũ hoạt động đến khi khách check-out.", "Phù hợp với thẻ tạm."}),
            ("ReturnRequired", "Yêu cầu trả thẻ", new[]{"Khách phải trả lại thẻ cũ tại lễ tân.", "Cần chữ ký xác nhận."}),
            ("NeedApproval", "Cần phê duyệt", new[]{"Yêu cầu supervisor phê duyệt trước khi cấp lại.", "Áp dụng cho trường hợp an ninh cao."})
        };

        foreach (var (key, title, descs) in rules)
        {
            var active = _oldCardRule == key;
            var b = new Border { Background = active ? Cb("#EFF6FF") : Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 6), BorderBrush = active ? Cb("#2F6BFF") : Cb("#E2E8F0"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand, Child = new StackPanel { Children = { new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#1E40AF") : C("#17365D") }, new TextBlock { Text = descs[0], FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) }, new TextBlock { Text = descs[1], FontSize = 10, Foreground = C("#94A3B8"), Margin = new Thickness(0, 1, 0, 0) } } } };
            b.MouseLeftButtonDown += (_, _) => { _oldCardRule = key; BuildTab4(); UpdateBottomBar(); };
            Tab4Panel.Children.Add(b);
        }
    }

    // ═══ TAB 5 — HISTORY ═══

    private void BuildTab5()
    {
        Tab5Panel.Children.Clear();
        if (_activeGuest == null) return;
        Tab5Panel.Children.Add(new TextBlock { Text = "Chưa có API đọc lịch sử key-card theo stay. Không hiển thị timeline mẫu.", FontSize = 11, Foreground = C("#A16207"), TextWrapping = TextWrapping.Wrap });

        if (_audit.Count > 0)
        {
            Tab5Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
            Tab5Panel.Children.Add(STitle("Audit thao tác trong phiên hiện tại"));
            foreach (var a in _audit)
                Tab5Panel.Children.Add(new TextBlock { Text = $"{a.Time:dd/MM HH:mm} · {a.Action} · {a.Detail} · {a.Operator}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) });
        }
    }

    // ═══ INSPECTOR ═══

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        if (_activeGuest == null) { InspectorPanel.Children.Add(new TextBlock { Text = "Chọn khách để xem chi tiết.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        var g = _activeGuest; var cards = _cards.Count(c => c.GuestId == g.Id);
        var needsReissue = _cards.Where(c => c.GuestId == g.Id && c.NeedsReissue).ToList();

        InspectorPanel.Children.Add(STitle("Thông tin lưu trú"));
        InspectorPanel.Children.Add(Row("Khách", g.Name)); InspectorPanel.Children.Add(Row("Phòng", $"{g.Room} ({g.RoomType})"));
        InspectorPanel.Children.Add(Row("Reservation", g.Reservation)); InspectorPanel.Children.Add(Row("Trạng thái", g.Status));

        InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        InspectorPanel.Children.Add(STitle("Thẻ"));
        InspectorPanel.Children.Add(Row("Tổng thẻ", $"{cards}"));
        InspectorPanel.Children.Add(Row("Cần cấp lại", $"{needsReissue.Count}"));
        if (_selectedCard != null) { InspectorPanel.Children.Add(Row("Thẻ đang chọn", _selectedCard.Code)); InspectorPanel.Children.Add(Row("Trạng thái", _selectedCard.Status)); }

        InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        InspectorPanel.Children.Add(STitle("Cảnh báo"));
        _warnings.Clear(); _blocking.Clear();
        if (g.Status == "Checked-Out") _blocking.Add(new RkWarning("block", "Khách đã check-out"));
        if (g.Status == "Arrival" && g.Arrival != "25/07/2026") _blocking.Add(new RkWarning("block", "Khách chưa đến ngày nhận phòng"));
        if (needsReissue.Count > 0) _warnings.Add(new RkWarning("warn", $"{needsReissue.Count} thẻ cần cấp lại"));
        if (g.IsVip) _warnings.Add(new RkWarning("warn", "Khách VIP — xử lý ưu tiên"));
        if (_selectedCard?.Status == "Lost") _warnings.Add(new RkWarning("warn", "Thẻ bị mất — rủi ro an ninh"));
        if (cards >= 3) _warnings.Add(new RkWarning("warn", "Nhiều thẻ đang hoạt động"));

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
        if (_activeGuest == null) { BottomBar.Children.Add(new TextBlock { Text = "Chọn khách và thẻ cần cấp lại.", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return; }

        BottomBar.Children.Add(BtmItem("Thẻ cũ", _selectedCard?.Code ?? "Chưa chọn", _selectedCard != null ? "#DC2626" : "#94A3B8"));
        BottomBar.Children.Add(BtmItem("Lý do", _reason, "#F59E0B"));
        BottomBar.Children.Add(BtmItem("Chế độ", _mode, "#2F6BFF"));
        BottomBar.Children.Add(BtmItem("Xử lý thẻ cũ", _oldCardRule, "#64748B"));
        BottomBar.Children.Add(BtmItem("Encoder", _encoder, "#64748B"));

        var hasBlocking = _blocking.Count > 0 || _selectedCard == null;
        if (hasBlocking) BottomBar.Children.Add(new Border { Background = Cb("#FEE2E2"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 3, 8, 3), Margin = new Thickness(8, 0, 0, 0), Child = new TextBlock { Text = $"✕ {(_selectedCard == null ? 1 : _blocking.Count)} chặn", FontSize = 11, Foreground = C("#DC2626") } });

        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 0, 0, 0) };
        var cancelBtn = new Button { Content = "Hủy", Style = FindResource("SecondaryButton") as Style, Height = 30, FontSize = 11 }; cancelBtn.Click += (_, _) => Close();
        var reissueBtn = new Button { Content = "Cấp lại thẻ", Style = FindResource("PrimaryButton") as Style, Height = 30, FontSize = 11, Margin = new Thickness(6, 0, 0, 0), IsEnabled = !hasBlocking };
        reissueBtn.Click += (_, _) => ReissueCard();
        sp.Children.Add(cancelBtn); sp.Children.Add(reissueBtn);
        BottomBar.Children.Add(sp);
    }

    private FrameworkElement BtmItem(string label, string value, string color) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 12, 0), Children = { new TextBlock { Text = label + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = value, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C(color), VerticalAlignment = VerticalAlignment.Center } } };

    private async void ReissueCard()
    {
        if (_activeGuest == null || _selectedCard == null || _blocking.Count > 0) return;
        if (_keyCardService is null || _executionContext?.Current is null) return;
        try
        {
            var quantity = Math.Max(1, _cardQty);
            for (var i = 0; i < quantity; i++) await _keyCardService.IssueDuplicateAsync(_selectedCard.KeyId, default);
            if (_oldCardRule == "DisableNow")
            {
                var actorId = _executionContext.Current.Actor.ActorId?.Value ?? Guid.Empty;
                await _keyCardService.RevokeKeyAsync(_selectedCard.KeyId, $"Reissue: {_reason}", actorId, default);
            }
            var stayId = _activeGuest.StayId;
            _audit.Add(new RkAudit(DateTime.Now, "Cấp lại", $"{quantity} thẻ mới ← {_selectedCard.Code} · Lý do: {_reason}", "Operator hiện tại"));
            await LoadRealDataAsync(stayId);
            MessageBox.Show("Đã cấp lại thẻ và lưu nhật ký vào cơ sở dữ liệu. Thiết bị encoder vẫn là bước tích hợp phần cứng riêng.", "Cấp lại thẻ", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể cấp lại thẻ: {ex.Message}", "Cấp lại thẻ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══ EVENT HANDLERS ═══

    private void OnApplyGuestFilter(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilter(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestTypeFilter.SelectedIndex = 0; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }
    private async void OnRefresh(object s, RoutedEventArgs e) { _activeGuest = null; _selectedCard = null; _warnings.Clear(); _blocking.Clear(); _audit.Clear(); InitFilters(); InitCenterTabs(); await LoadRealDataAsync(); }
    private void OnClose(object s, RoutedEventArgs e) => Close();

    // ═══ HELPERS ═══

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsRk(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record RkGuest(int Id, string Name, string Room, string RoomType, string Reservation, string Stay, string Arrival, string Departure, string Status, bool IsVip, string? Company, int Balance, int MaxCards, string Membership, Guid StayId, Guid? GuestId, Guid RoomId);
internal record RkCard(int Id, string Code, string Type, DateTime Issued, string IssuedBy, DateTime Expire, string Status, string Encoder, DateTime LastUsed, int GuestId, Guid KeyId, bool NeedsReissue = false);
internal record RkAudit(DateTime Time, string Action, string Detail, string Operator);
internal record RkWarning(string Level, string Message);
internal static class RkExt { public static TextBlock WithRunsRk(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
