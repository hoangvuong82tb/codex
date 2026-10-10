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

public partial class DisableKeyCardWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private DkGuest? _activeGuest;
    private readonly List<DkGuest> _guests;
    private readonly List<DkCard> _cards;
    private readonly HashSet<int> _selectedCardIds = new();
    private readonly List<DkAudit> _audit = new();
    private int _guestPage, _guestPageSize = 20;
    private int _centerTab;
    private string _disableReason = "Lost", _disableMode = "Immediate", _approval = "None";
    private string _riskLevel = "Low";
    private readonly List<DkWarning> _warnings = new(), _blocking = new();
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IKeyCardService? _keyCardService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;

    public DisableKeyCardWindow()
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
            _guests.Add(new DkGuest(guestIndex, stay.GuestName, stay.RoomNumber, stay.RoomTypeCode,
                stay.ReservationNumber ?? "—", stay.StayNumber, stay.ScheduledArrivalDate.ToString("dd/MM/yyyy"),
                stay.ScheduledDepartureDate.ToString("dd/MM/yyyy"), "In-House", false, null, 0, "Standard", false,
                stay.HotelStayId, stay.GuestId, stay.RoomId));
            foreach (var key in keys)
            {
                cardIndex++;
                _cards.Add(new DkCard(cardIndex, key.KeyCode, key.Level.ToString(), key.IssuedAtUtc ?? DateTime.MinValue,
                    key.IssuedByName ?? "—", key.ValidUntilUtc ?? stay.ScheduledDepartureDate.ToDateTime(new TimeOnly(12, 0)),
                    key.Status.ToString(), key.EncoderId > 0 ? $"Encoder {key.EncoderId}" : "Chưa ghi nhận",
                    key.IssuedAtUtc ?? DateTime.MinValue, guestIndex, key.KeyId,
                    key.IsMaster || key.Level is AccessLevel.Emergency or AccessLevel.Master));
            }
        }

        _activeGuest = preserveStayId is { } stayId ? _guests.FirstOrDefault(x => x.StayId == stayId) : null;
        _selectedCardIds.Clear();
        RefreshGuestPage(true);
        UpdateSummaryStrip();
        BuildInspector();
        UpdateBottomBar();
    }

    private void InitFilters()
    {
        GuestTypeFilter.ItemsSource = new[] { "Tất cả", "In-House", "Checked-Out", "Arrival" };
        GuestTypeFilter.SelectedIndex = 0;
        GuestPageSizeBox.ItemsSource = new[] { "20 / trang", "50 / trang", "100 / trang" };
        GuestPageSizeBox.SelectedIndex = 0;
    }

    private void InitCenterTabs()
    {
        CenterTabs.Children.Clear();
        var tabs = new[] { "Thông tin khách", "Danh sách thẻ", "Khóa thẻ", "Kiểm tra bảo mật", "Lịch sử" };
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

    private List<DkGuest> GetFilteredGuests()
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
            var total = _cards.Count(c => c.GuestId == g.Id);
            var activeCards = _cards.Count(c => c.GuestId == g.Id && c.Status == "Active");
            var statusColor = g.Status switch { "In-House" => "#16A34A", "Arrival" => "#2F6BFF", _ => "#94A3B8" };

            var sp = new StackPanel();
            var row1 = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            var roomSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            roomSp.Children.Add(new TextBlock { Text = g.Room, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = active ? C("#1E40AF") : C("#102E5A") });
            roomSp.Children.Add(new TextBlock { Text = $"  {g.RoomType}", FontSize = 12, Foreground = C("#64748B"), VerticalAlignment = VerticalAlignment.Center });
            row1.Children.Add(roomSp);
            var badges = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            badges.Children.Add(new Border { Background = Cb(statusColor), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 2, 10, 2), Child = new TextBlock { Text = g.Status, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
            if (g.SecurityFlag) badges.Children.Add(new Border { Background = Cb("#DC2626"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(4, 0, 0, 0), Child = new TextBlock { Text = "⚠", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Cb("White") } });
            row1.Children.Add(badges);
            sp.Children.Add(row1);

            var row2 = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            var nameSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            nameSp.Children.Add(new TextBlock { Text = g.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = active ? C("#1E40AF") : C("#17365D") });
            if (g.IsVip) nameSp.Children.Add(new Border { Background = Cb("#FEF3C7"), CornerRadius = new CornerRadius(5), Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(6, 0, 0, 0), Child = new TextBlock { Text = "★ VIP", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#F59E0B") } });
            row2.Children.Add(nameSp);
            sp.Children.Add(row2);

            var row3 = new StackPanel { Orientation = Orientation.Horizontal };
            row3.Children.Add(new TextBlock { Text = $"🪪 {total} thẻ ({activeCards} hoạt động)  ·  {g.Arrival} → {g.Departure}", FontSize = 11, Foreground = C("#94A3B8") });
            if (g.Balance > 0) row3.Children.Add(new TextBlock { Text = $"  ·  Balance: {g.Balance:N0}đ", FontSize = 10, Foreground = C("#DC2626"), Margin = new Thickness(4, 0, 0, 0) });
            sp.Children.Add(row3);

            var b = new Border { Background = active ? Cb("#EFF6FF") : Cb("#FAFBFD"), BorderBrush = active ? Cb("#BFDBFE") : Cb("#E2E8F0"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(12, 10, 12, 10), Cursor = System.Windows.Input.Cursors.Hand, Child = sp };
            var gid = g.Id; b.MouseLeftButtonDown += (_, _) => SelectGuest(gid); GuestListPanel.Children.Add(b);
        }
        GuestPagerSummary.Text = $"{_guestPage * _guestPageSize + 1}–{Math.Min((_guestPage + 1) * _guestPageSize, f.Count)} / {f.Count} khách";
        GuestPrevBtn.IsEnabled = _guestPage > 0; GuestNextBtn.IsEnabled = _guestPage < tp - 1;
        BuildGuestPageBtns(tp);
    }

    private void BuildGuestPageBtns(int tp) { GuestPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = PagerBtn(i, _guestPage, idx); nb.Click += (_, _) => { _guestPage = idx; RefreshGuestPage(false); }; GuestPageBtns.Children.Add(nb); } }
    private Button PagerBtn(int i, int cp, int idx) => new() { Content = (i + 1).ToString(), Width = 28, Height = 26, FontSize = 11, Margin = new Thickness(1, 0, 1, 0), Background = i == cp ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == cp ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == cp ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand, Template = GuestPrevBtn.Template };

    private void SelectGuest(int id)
    {
        _activeGuest = _guests.First(g => g.Id == id); _selectedCardIds.Clear();
        RefreshGuestPage(false); UpdateSummaryStrip();
        BuildTab1(); BuildTab2(); BuildTab3(); BuildTab4(); BuildInspector(); UpdateBottomBar();
    }

    private void UpdateSummaryStrip()
    {
        SummaryStrip.Children.Clear();
        if (_activeGuest == null) { SummaryStrip.Children.Add(new TextBlock { Text = "Chưa chọn khách", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return; }
        var g = _activeGuest; var total = _cards.Count(c => c.GuestId == g.Id);
        var active = _cards.Count(c => c.GuestId == g.Id && c.Status == "Active");
        var disabled = _cards.Count(c => c.GuestId == g.Id && c.Status == "Disabled");
        SummaryStrip.Children.Add(Strip("Khách", g.Name, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", $"{g.Room} ({g.RoomType})", true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Res", g.Reservation, true)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đang hoạt động", $"{active}", active > 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đã khóa", $"{disabled}", disabled > 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Tổng", $"{total}", true));
        if (g.SecurityFlag) { SummaryStrip.Children.Add(Sep()); SummaryStrip.Children.Add(new Border { Background = Cb("#FEE2E2"), CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 1, 6, 1), Child = new TextBlock { Text = "⚠ Bảo mật", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C("#DC2626") } }); }
    }

    // ═══ TAB 1 ═══

    private void BuildTab1() { Tab1Panel.Children.Clear(); if (_activeGuest == null) return; var g = _activeGuest; Tab1Panel.Children.Add(Row("Tên", g.Name)); Tab1Panel.Children.Add(Row("Reservation", g.Reservation)); Tab1Panel.Children.Add(Row("Stay", g.Stay)); Tab1Panel.Children.Add(Row("Phòng", $"{g.Room} ({g.RoomType})")); Tab1Panel.Children.Add(Row("Đến", g.Arrival)); Tab1Panel.Children.Add(Row("Đi", g.Departure)); Tab1Panel.Children.Add(Row("Công ty", g.Company ?? "—")); Tab1Panel.Children.Add(Row("Membership", g.Membership)); if (g.IsVip) Tab1Panel.Children.Add(Row("VIP", "Có")); if (g.SecurityFlag) Tab1Panel.Children.Add(Row("Security", "⚠ Có cảnh báo")); }

    // ═══ TAB 2 ═══

    private void BuildTab2()
    {
        Tab2Panel.Children.Clear();
        if (_activeGuest == null) return;
        var guestCards = _cards.Where(c => c.GuestId == _activeGuest.Id).ToList();
        Tab2Panel.Children.Add(STitle($"Thẻ của {_activeGuest.Name} ({guestCards.Count} thẻ)"));
        var header = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
        AddHdr(header, 1, "Số thẻ"); AddHdr(header, 2, "Loại"); AddHdr(header, 3, "Ngày cấp"); AddHdr(header, 4, "Encoder"); AddHdr(header, 5, "Trạng thái");
        Tab2Panel.Children.Add(header);

        foreach (var c in guestCards)
        {
            var selected = _selectedCardIds.Contains(c.Id);
            var canDisable = c.Status == "Active" && c.Status != "Disabled" && c.Status != "Returned";
            var row = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });

            var cb = new CheckBox { IsChecked = selected, IsEnabled = canDisable, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            var cid = c.Id; cb.Checked += (_, _) => { _selectedCardIds.Add(cid); UpdateBottomBar(); UpdateRiskLevel(); };
            cb.Unchecked += (_, _) => { _selectedCardIds.Remove(cid); UpdateBottomBar(); UpdateRiskLevel(); };
            Grid.SetColumn(cb, 0); row.Children.Add(cb);

            var statusColor = c.Status switch { "Active" => "#16A34A", "Disabled" => "#DC2626", "Returned" => "#64748B", "Lost" => "#F59E0B", _ => "#94A3B8" };
            AddCell(row, 1, c.Code, "#17365D", false); AddCell(row, 2, c.Type, "#64748B", false);
            AddCell(row, 3, c.Issued.ToString("dd/MM HH:mm"), "#64748B", false);
            AddCell(row, 4, c.Encoder, "#64748B", false);
            AddCell(row, 5, c.Status, statusColor, true);
            Tab2Panel.Children.Add(row);
        }
    }

    private static void AddHdr(Grid g, int col, string text) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }
    private static void AddCell(Grid g, int col, string text, string color, bool bold) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }

    // ═══ TAB 3 — DISABLE ═══

    private void BuildTab3()
    {
        Tab3Panel.Children.Clear();
        if (_activeGuest == null) { Tab3Panel.Children.Add(new TextBlock { Text = "Chọn khách và thẻ cần khóa ở Tab 2.", FontSize = 13, Foreground = C("#94A3B8") }); return; }
        var selected = _cards.Where(c => _selectedCardIds.Contains(c.Id)).ToList();
        if (selected.Count == 0) { Tab3Panel.Children.Add(new TextBlock { Text = "Chọn ít nhất một thẻ để khóa ở Tab 2.", FontSize = 13, Foreground = C("#F59E0B") }); return; }

        Tab3Panel.Children.Add(STitle($"Khóa {selected.Count} thẻ"));
        foreach (var c in selected)
            Tab3Panel.Children.Add(new Border { Background = Cb("#FEE2E2"), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 0, 3), Child = new TextBlock { Text = $"{c.Code} · {c.Type} · {c.Status} · {c.Encoder}", FontSize = 11, Foreground = C("#DC2626") } });

        // Reason
        Tab3Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        Tab3Panel.Children.Add(STitle("Lý do khóa"));
        var reasons = new[] { ("Lost", "Mất thẻ"), ("Security", "Rủi ro an ninh"), ("GuestRequest", "Khách yêu cầu"), ("StaffLeave", "Nhân viên nghỉ"), ("MasterDisabled", "Khóa thẻ master"), ("EmergencyDisabled", "Khóa thẻ khẩn cấp"), ("UnpaidBill", "Chưa thanh toán"), ("DuplicateCard", "Thẻ trùng lặp"), ("Other", "Khác") };
        var wp = new WrapPanel();
        foreach (var (key, label) in reasons) { var active = _disableReason == key; var rb = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } }; rb.MouseLeftButtonDown += (_, _) => { _disableReason = key; BuildTab3(); UpdateBottomBar(); }; wp.Children.Add(rb); }
        Tab3Panel.Children.Add(wp);

        // Mode
        Tab3Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 8) });
        Tab3Panel.Children.Add(STitle("Chế độ khóa"));
        var modes = new[] { ("Immediate", "Ngay lập tức"), ("Temporary", "Tạm thời"), ("Scheduled", "Lên lịch"), ("UntilCheckout", "Đến khi check-out"), ("Permanent", "Vĩnh viễn") };
        var wp2 = new WrapPanel();
        foreach (var (key, label) in modes) { var active = _disableMode == key; var mb = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } }; mb.MouseLeftButtonDown += (_, _) => { _disableMode = key; BuildTab3(); UpdateBottomBar(); }; wp2.Children.Add(mb); }
        Tab3Panel.Children.Add(wp2);

        // Approval
        Tab3Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 8) });
        Tab3Panel.Children.Add(STitle("Phê duyệt"));
        var approvals = new[] { ("None", "Không cần"), ("Supervisor", "Supervisor"), ("Manager", "Manager"), ("Security", "Security") };
        var wp3 = new WrapPanel();
        foreach (var (key, label) in approvals) { var active = _approval == key; var ab = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("#F3F4F6"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#1E40AF") : C("#475569") } }; ab.MouseLeftButtonDown += (_, _) => { _approval = key; BuildTab3(); UpdateBottomBar(); }; wp3.Children.Add(ab); }
        Tab3Panel.Children.Add(wp3);
    }

    // ═══ TAB 4 — SECURITY VALIDATION ═══

    private void UpdateRiskLevel()
    {
        var selected = _cards.Where(c => _selectedCardIds.Contains(c.Id)).ToList();
        if (selected.Any(c => c.IsSensitive)) _riskLevel = "Critical";
        else if (selected.Any(c => c.Type is "Master" or "Emergency")) _riskLevel = "High";
        else if (_activeGuest is { IsVip: true } || _activeGuest is { SecurityFlag: true }) _riskLevel = "Medium";
        else _riskLevel = "Low";
    }

    private void BuildTab4()
    {
        Tab4Panel.Children.Clear();
        if (_activeGuest == null) return;
        var selected = _cards.Where(c => _selectedCardIds.Contains(c.Id)).ToList();
        _warnings.Clear(); _blocking.Clear();

        if (_selectedCardIds.Count == 0) _blocking.Add(new DkWarning("block", "Chưa chọn thẻ cần khóa"));
        if (selected.Any(c => c.Status == "Disabled")) _blocking.Add(new DkWarning("block", "Một số thẻ đã bị khóa"));
        if (selected.Any(c => c.Status == "Returned")) _blocking.Add(new DkWarning("block", "Một số thẻ đã được thu hồi"));
        if (selected.Any(c => c.IsSensitive)) _warnings.Add(new DkWarning("warn", "Có thẻ nhạy cảm (Master/Emergency)"));
        if (_activeGuest.IsVip) _warnings.Add(new DkWarning("warn", "Khách VIP — xác nhận kỹ trước khi khóa"));
        if (_activeGuest.Balance > 5000000) _warnings.Add(new DkWarning("warn", $"Balance lớn: {_activeGuest.Balance:N0}đ"));
        if (_disableReason == "Security") _warnings.Add(new DkWarning("warn", "Lý do an ninh — ghi nhận audit"));

        // Risk level display
        var riskColor = _riskLevel switch { "Critical" => "#DC2626", "High" => "#F59E0B", "Medium" => "#2F6BFF", _ => "#16A34A" };
        Tab4Panel.Children.Add(new Border { Background = Cb(riskColor), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 10), Child = new StackPanel { Children = { new TextBlock { Text = $"Mức độ rủi ro: {_riskLevel}", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Cb("White") }, new TextBlock { Text = _riskLevel switch { "Critical" => "Thẻ nhạy cảm — cần Security phê duyệt", "High" => "Thẻ Master/Emergency — cần Manager phê duyệt", "Medium" => "Cần kiểm tra kỹ", _ => "Rủi ro thấp" }, FontSize = 11, Foreground = Cb("White"), Margin = new Thickness(0, 2, 0, 0), Opacity = 0.85 } } } });

        Tab4Panel.Children.Add(STitle("Checklist bảo mật"));
        var checks = new (string, bool)[] { ("Đã chọn thẻ", _selectedCardIds.Count > 0), ("Thẻ đang hoạt động", selected.All(c => c.Status == "Active")), ("Không bị khóa trước đó", !selected.Any(c => c.Status == "Disabled")), ("Không bị thu hồi", !selected.Any(c => c.Status == "Returned")) };
        foreach (var (label, ok) in checks)
            Tab4Panel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 12, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 11, Foreground = C("#334155") } } });

        Tab4Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        Tab4Panel.Children.Add(STitle("Warning & Blocking"));
        foreach (var w in _blocking.Concat(_warnings))
        {
            var isBlock = w.Level == "block";
            Tab4Panel.Children.Add(new Border { Background = isBlock ? Cb("#FEE2E2") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 4), Child = new TextBlock { Text = (isBlock ? "✕ " : "⚠ ") + w.Message, FontSize = 11, Foreground = isBlock ? C("#DC2626") : C("#A16207") } });
        }
        if (_warnings.Count == 0 && _blocking.Count == 0) Tab4Panel.Children.Add(new TextBlock { Text = "✓ Sẵn sàng khóa thẻ.", FontSize = 11, Foreground = C("#16A34A") });
        UpdateBottomBar();
    }

    // ═══ TAB 5 ═══

    private void BuildTab5()
    {
        Tab5Panel.Children.Clear();
        if (_activeGuest == null) return;
        Tab5Panel.Children.Add(new TextBlock { Text = "Chưa có API đọc lịch sử key-card theo stay. Không hiển thị timeline mẫu; audit thao tác trong phiên hiện tại nằm bên dưới.", FontSize = 11, Foreground = C("#A16207"), TextWrapping = TextWrapping.Wrap });
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
        var g = _activeGuest; var active = _cards.Count(c => c.GuestId == g.Id && c.Status == "Active");
        var disabled = _cards.Count(c => c.GuestId == g.Id && c.Status == "Disabled");
        var selected = _cards.Where(c => _selectedCardIds.Contains(c.Id)).ToList();

        InspectorPanel.Children.Add(STitle("Thông tin lưu trú"));
        InspectorPanel.Children.Add(Row("Khách", g.Name)); InspectorPanel.Children.Add(Row("Phòng", $"{g.Room} ({g.RoomType})"));
        InspectorPanel.Children.Add(Row("Reservation", g.Reservation)); InspectorPanel.Children.Add(Row("Trạng thái", g.Status));
        InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        InspectorPanel.Children.Add(STitle("An ninh"));
        InspectorPanel.Children.Add(Row("Đang hoạt động", $"{active}")); InspectorPanel.Children.Add(Row("Đã khóa", $"{disabled}"));
        InspectorPanel.Children.Add(Row("Đã chọn khóa", $"{selected.Count}"));
        InspectorPanel.Children.Add(Row("Mức độ rủi ro", _riskLevel));
        if (selected.Count > 0) { InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) }); InspectorPanel.Children.Add(STitle("Thẻ đã chọn")); foreach (var c in selected) InspectorPanel.Children.Add(new TextBlock { Text = $"{c.Code} · {c.Type} · {c.Status}", FontSize = 10, Foreground = C("#DC2626"), Margin = new Thickness(0, 1, 0, 0) }); }
    }

    // ═══ BOTTOM BAR ═══

    private void UpdateBottomBar()
    {
        BottomBar.Children.Clear();
        if (_activeGuest == null) { BottomBar.Children.Add(new TextBlock { Text = "Chọn khách và thẻ cần khóa.", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return; }
        var selected = _cards.Where(c => _selectedCardIds.Contains(c.Id)).ToList();

        BottomBar.Children.Add(BtmItem("Đã chọn", $"{selected.Count} thẻ", selected.Count > 0 ? "#DC2626" : "#94A3B8"));
        BottomBar.Children.Add(BtmItem("Lý do", _disableReason, "#64748B"));
        BottomBar.Children.Add(BtmItem("Chế độ", _disableMode, "#64748B"));
        BottomBar.Children.Add(BtmItem("Phê duyệt", _approval, _approval != "None" ? "#F59E0B" : "#64748B"));
        BottomBar.Children.Add(BtmItem("Rủi ro", _riskLevel, _riskLevel is "Critical" or "High" ? "#DC2626" : "#16A34A"));

        var hasBlocking = _blocking.Count > 0 || _selectedCardIds.Count == 0;
        if (hasBlocking) BottomBar.Children.Add(new Border { Background = Cb("#FEE2E2"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 3, 8, 3), Margin = new Thickness(8, 0, 0, 0), Child = new TextBlock { Text = $"✕ {(_selectedCardIds.Count == 0 ? 1 : _blocking.Count)} chặn", FontSize = 11, Foreground = C("#DC2626") } });

        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 0, 0, 0) };
        var cancelBtn = new Button { Content = "Hủy", Style = FindResource("SecondaryButton") as Style, Height = 30, FontSize = 11 }; cancelBtn.Click += (_, _) => Close();
        var disableBtn = new Button { Content = "Xác nhận khóa thẻ", Style = FindResource("PrimaryButton") as Style, Height = 30, FontSize = 11, Margin = new Thickness(6, 0, 0, 0), IsEnabled = !hasBlocking };
        disableBtn.Click += (_, _) => DisableCards();
        sp.Children.Add(cancelBtn); sp.Children.Add(disableBtn);
        BottomBar.Children.Add(sp);
    }

    private FrameworkElement BtmItem(string label, string value, string color) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 12, 0), Children = { new TextBlock { Text = label + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = value, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C(color), VerticalAlignment = VerticalAlignment.Center } } };

    private async void DisableCards()
    {
        if (_activeGuest == null || _selectedCardIds.Count == 0 || _blocking.Count > 0) return;
        var selected = _cards.Where(c => _selectedCardIds.Contains(c.Id)).ToList();
        if (_keyCardService is null || _executionContext?.Current is null) return;
        try
        {
            var actorId = _executionContext.Current.Actor.ActorId?.Value ?? Guid.Empty;
            foreach (var card in selected) await _keyCardService.RevokeKeyAsync(card.KeyId, $"{_disableReason}; Mode={_disableMode}; Approval={_approval}", actorId, default);
            var stayId = _activeGuest.StayId;
            _audit.Add(new DkAudit(DateTime.Now, $"Khóa {selected.Count} thẻ", $"Lý do: {_disableReason} · Chế độ: {_disableMode} · Phê duyệt: {_approval} · Rủi ro: {_riskLevel}", "Operator hiện tại"));
            await LoadRealDataAsync(stayId);
            MessageBox.Show("Đã khóa thẻ và lưu trạng thái Revoked vào cơ sở dữ liệu.", "Khóa thẻ", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể khóa thẻ: {ex.Message}", "Khóa thẻ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnApplyGuestFilter(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilter(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestTypeFilter.SelectedIndex = 0; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }
    private async void OnRefresh(object s, RoutedEventArgs e) { _activeGuest = null; _selectedCardIds.Clear(); _warnings.Clear(); _blocking.Clear(); _audit.Clear(); InitFilters(); InitCenterTabs(); await LoadRealDataAsync(); }
    private void OnClose(object s, RoutedEventArgs e) => Close();

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsDk(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 3, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record DkGuest(int Id, string Name, string Room, string RoomType, string Reservation, string Stay, string Arrival, string Departure, string Status, bool IsVip, string? Company, int Balance, string Membership, bool SecurityFlag, Guid StayId, Guid? GuestId, Guid RoomId);
internal record DkCard(int Id, string Code, string Type, DateTime Issued, string IssuedBy, DateTime Expire, string Status, string Encoder, DateTime LastUsed, int GuestId, Guid KeyId, bool IsSensitive);
internal record DkAudit(DateTime Time, string Action, string Detail, string Operator);
internal record DkWarning(string Level, string Message);
internal static class DkExt { public static TextBlock WithRunsDk(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
