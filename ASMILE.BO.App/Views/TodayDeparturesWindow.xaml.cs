using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.App.Controls;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Stay;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class TodayDeparturesWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private List<DepartureRow> _allDepartures;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IGuestProfileRepository? _guestProfileRepository;
    private readonly IHousekeepingBoardService? _housekeepingBoardService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly TablePagerViewModel<DepartureRow> _pager = new() { ItemName = "khách", SelectedPageSize = "20 / trang" };
    private readonly Dictionary<Guid, string> _guestDocumentsByProfile = new();

    public TodayDeparturesWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        BusinessDatePicker.SelectedDate = System.DateTime.Today;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _guestProfileRepository = services?.GetService<IGuestProfileRepository>();
        _housekeepingBoardService = services?.GetService<IHousekeepingBoardService>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _allDepartures = new List<DepartureRow>();
        AttachPager();
        ShowTab(0);
        UpdateMetrics();
        ApplyFilters();
        Loaded += async (_, _) => await LoadPersistedAsync();
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async Task LoadPersistedAsync()
    {
        if (_stayRepository is null || _executionContext?.Current is null)
        {
            ApplyFilters();
            FooterTotal.Text = "Chưa cấu hình được Stay service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var context = _executionContext.Current;
            IReadOnlyList<HkRoomSnapshot> roomSnapshots = Array.Empty<HkRoomSnapshot>();
            if (_housekeepingBoardService is not null)
            {
                try
                {
                    roomSnapshots = await _housekeepingBoardService.GetRoomSnapshotAsync(
                        context.Property.PropertyId.Value,
                        CancellationToken.None);
                }
                catch
                {
                    // Keep departures visible when the optional room snapshot is unavailable.
                }
            }

            var roomByNumber = roomSnapshots
                .Where(x => !string.IsNullOrWhiteSpace(x.RoomNumber))
                .GroupBy(x => x.RoomNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var stays = await _stayRepository.GetExpectedDeparturesAsync(context.Property.PropertyId.Value, context.BusinessDate.BusinessDate, CancellationToken.None);
            _allDepartures = new List<DepartureRow>();
            _guestDocumentsByProfile.Clear();
            foreach (var stay in stays)
            {
                HotelFolioDto? folio = null;
                FolioLedgerDto? ledger = null;
                if (_folioRepository is not null)
                {
                    try
                    {
                        folio = await _folioRepository.GetByStayAsync(context.Property.PropertyId.Value, stay.HotelStayId, CancellationToken.None);
                        if (folio is not null)
                            ledger = await _folioRepository.GetLedgerAsync(context.Property.PropertyId.Value, folio.HotelFolioId, CancellationToken.None);
                    }
                    catch { /* Keep the departure visible without inventing a balance. */ }
                }
                GuestProfileDto? profile = null;
                if (_guestProfileRepository is not null && stay.GuestId is Guid guestId)
                {
                    try
                    {
                        profile = await _guestProfileRepository.GetDtoAsync(context.Property.PropertyId.Value, guestId, CancellationToken.None);
                        var fullProfile = await _guestProfileRepository.GetAsync(context.Property.PropertyId.Value, guestId, CancellationToken.None);
                        _guestDocumentsByProfile[guestId] = fullProfile?.Documents.Count > 0
                            ? string.Join(" · ", fullProfile.Documents.Select(x => $"{x.DocumentType}: {x.DocumentNumber}"))
                            : "Chưa có giấy tờ thật";
                    }
                    catch { /* Keep the departure visible when the optional profile query fails. */ }
                }
                roomByNumber.TryGetValue(stay.RoomNumber, out var roomSnapshot);
                var documents = stay.GuestId is Guid guestIdValue && _guestDocumentsByProfile.TryGetValue(guestIdValue, out var summary)
                    ? summary
                    : "Chưa tải được hồ sơ giấy tờ thật";
                _allDepartures.Add(MapPersistedStay(stay, folio, ledger, profile, roomSnapshot, documents));
            }
            ApplyFilters();
            FooterTotal.Text = $"Đã tải {_allDepartures.Count:N0} khách dự kiến đi từ dữ liệu Stay thật.";
        }
        catch (Exception ex)
        {
            _allDepartures = new List<DepartureRow>();
            ApplyFilters();
            FooterTotal.Text = $"Không tải được khách đi thật: {ex.Message}";
        }
    }

    private static DepartureRow MapPersistedStay(
        HotelStayDto stay,
        HotelFolioDto? folio,
        FolioLedgerDto? ledger,
        GuestProfileDto? profile,
        HkRoomSnapshot? roomSnapshot,
        string documentSummary)
    {
        var checkedOut = stay.ActualCheckOutAtUtc.HasValue || stay.Status is HotelStayStatus.CheckedOut or HotelStayStatus.Completed;
        var status = checkedOut ? ("Đã Check-out", "#DCFCE7", "#16A34A") : ("Dự kiến đi", "#DBEAFE", "#2563EB");
        var nights = Math.Max(1, stay.ScheduledDepartureDate.DayNumber - stay.ScheduledArrivalDate.DayNumber);
        var balance = folio is null ? "Chưa kiểm tra" : folio.OutstandingBalance <= 0 ? "0" : folio.OutstandingBalance.ToString("N0");
        var folioStatus = folio is null ? "Chưa kiểm tra" : folio.OutstandingBalance > 0 ? "Còn phải thu" : "Bình thường";
        var roomAlert = roomSnapshot is null
            ? string.Empty
            : IsRoomReady(roomSnapshot)
                ? string.Empty
                : "⚡";
        var ledgerAlert = ledger is null && folio is not null ? "⚠" : string.Empty;
        var alerts = roomAlert + ledgerAlert + (folio is not null && folio.OutstandingBalance > 0 ? "⚠" : string.Empty);
        return new DepartureRow(
            status.Item1,
            status.Item2,
            status.Item3,
            folioStatus,
            "#FEF9C3",
            "#A16207",
            stay.RoomNumber,
            stay.GuestName,
            stay.ScheduledArrivalDate.ToString("dd/MM"),
            "—",
            nights.ToString(),
            stay.RoomTypeCode,
            balance,
            profile is not null && profile.VipLevel != VipLevel.Normal ? "★" : "",
            alerts,
            stay.HotelStayId,
            stay.RowVersion,
            folio?.FolioNumber,
            profile?.Phone,
            profile?.Email,
            profile?.Nationality,
            profile?.CompanyName,
            profile?.VipLevel.ToString(),
            profile?.Notes,
            stay.Adults + stay.Children,
            roomSnapshot?.OperationalStatus,
            roomSnapshot?.HousekeepingStatus,
            folio?.TotalCharges,
            ledger?.Entries is null ? null : SummarizeLedger(ledger.Entries),
            documentSummary);
    }

    private static bool IsRoomReady(HkRoomSnapshot snapshot)
        => !IsProblemStatus(snapshot.OperationalStatus)
           && !IsProblemStatus(snapshot.HousekeepingStatus);

    private static bool IsProblemStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
           || status.Contains("Dirty", StringComparison.OrdinalIgnoreCase)
           || status.Contains("OutOf", StringComparison.OrdinalIgnoreCase)
           || status.Contains("Maintenance", StringComparison.OrdinalIgnoreCase)
           || status.Contains("Blocked", StringComparison.OrdinalIgnoreCase);

    private static string SummarizeLedger(IReadOnlyList<HotelFolioEntryDto> entries)
    {
        if (entries.Count == 0) return "Không có giao dịch";
        return string.Join(" · ", entries
            .GroupBy(x => x.EntryType)
            .OrderBy(x => x.Key.ToString())
            .Select(x => $"{x.Key}: {x.Sum(y => y.Amount):N0}"));
    }

    private void ApplyFilters()
    {
        var f = _allDepartures.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text)) { var kw = SearchBox.Text.ToLower(); f = f.Where(r => r.FullName.ToLower().Contains(kw) || r.RoomNumber.Contains(kw)); }
        if (FloorFilter.SelectedItem is ComboBoxItem fl && fl.Content is string fv && fv != "Tất cả") f = f.Where(r => r.RoomNumber.StartsWith(fv));
        if (RoomTypeFilter.SelectedItem is ComboBoxItem rt && rt.Content is string rv && rv != "Tất cả") f = f.Where(r => r.RoomType == rv);
        if (StatusFilter.SelectedItem is ComboBoxItem st && st.Content is string sv && sv != "Tất cả") f = f.Where(r => r.StatusText == sv);
        if (ChkUnpaid.IsChecked == true) f = f.Where(r => r.BalanceDue != "0");
        if (ChkLate.IsChecked == true) f = f.Where(r => r.StatusText == "Trả muộn");
        if (ChkVip.IsChecked == true) f = f.Where(r => r.Vip == "★");
        if (ChkNeedCheck.IsChecked == true) f = f.Where(r => !string.IsNullOrEmpty(r.Alerts));

        var list = f.ToList(); ApplyPage(list); UpdateMetrics(list);
    }

    private void ApplyPage(List<DepartureRow> list)
    {
        _pager.SetSource(list);
        DepartureGrid.ItemsSource = _pager.Items;
        ResultCount.Text = $"{list.Count} khách · {list.Select(r => r.RoomNumber).Distinct().Count()} phòng";
        FooterTotal.Text = $"Tổng: {list.Count} khách · {list.Select(r => r.RoomNumber).Distinct().Count()} phòng";
    }

    private void UpdateMetrics(List<DepartureRow>? list = null)
    {
        list ??= _allDepartures;
        MetricTotal.Text = list.Count.ToString();
        MetricCheckedOut.Text = list.Count(r => r.StatusText == "Đã Check-out").ToString();
        MetricUnpaid.Text = list.Count(r => r.BalanceDue != "0").ToString();
        MetricLate.Text = list.Count(r => r.StatusText == "Trả muộn").ToString();
        MetricFolioAlerts.Text = list.Count(r => r.FolioStatus is "Vượt hạn mức" or "Còn phải thu" or "Chưa kiểm tra").ToString();
        MetricHsk.Text = list.Count(r => r.Alerts.Contains("⚡")).ToString();
    }

    private void OnDepartureSelected(object sender, SelectionChangedEventArgs e)
    {
        if (DepartureGrid.SelectedItem is not DepartureRow r)
        {
            DetailEmpty.Visibility = Visibility.Visible; DetailContent.Visibility = Visibility.Collapsed;
            ReadinessPanel.Visibility = Visibility.Collapsed; TabContent.Children.Clear(); return;
        }
        DetailEmpty.Visibility = Visibility.Collapsed; DetailContent.Visibility = Visibility.Visible;
        var parts = r.FullName.Split(' '); DetailInitials.Text = parts.Length > 0 ? parts[^1][..1].ToUpper() : "?";
        DetailName.Text = r.FullName; DetailRoomMeta.Text = $"Phòng {r.RoomNumber} · {r.RoomType}"; DetailEtdMeta.Text = r.Etd;
        DetailVipBadge.Visibility = r.Vip == "★" ? Visibility.Visible : Visibility.Collapsed;
        DetailNights.Text = r.Nights; DetailGuests.Text = r.GuestCount.ToString();
        DetailBalance.Text = r.BalanceDue == "0" ? "0 VND" : r.BalanceDue;
        DetailEtd.Text = r.Etd;

        // Readiness
        ReadinessPanel.Visibility = Visibility.Visible; ReadinessContent.Children.Clear();
        var hasDebt = r.BalanceDue != "0"; var hasAlerts = r.Alerts.Length > 1;
        if (!hasDebt && !hasAlerts) { ReadinessPanel.Style = FindResource("ReadyGreen") as Style; ReadinessContent.Children.Add(new TextBlock { Text = "✓ Sẵn sàng Check-out", FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A")), FontSize = 13 }); ReadinessContent.Children.Add(new TextBlock { Text = "Folio đã cân · Dịch vụ đã chốt · Không cảnh báo", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")), Margin = new Thickness(0, 2, 0, 0) }); }
        else if (hasAlerts && !hasDebt) { ReadinessPanel.Style = FindResource("ReadyYellow") as Style; ReadinessContent.Children.Add(new TextBlock { Text = "⚠ Cần kiểm tra", FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A16207")), FontSize = 13 }); ReadinessContent.Children.Add(new TextBlock { Text = "Minibar chưa chốt · Cần xác nhận dịch vụ", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")), Margin = new Thickness(0, 2, 0, 0) }); }
        else { ReadinessPanel.Style = FindResource("ReadyRed") as Style; ReadinessContent.Children.Add(new TextBlock { Text = "✗ Chưa thể Check-out", FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")), FontSize = 13 }); ReadinessContent.Children.Add(new TextBlock { Text = $"Còn phải thu {r.BalanceDue} · Cần thanh toán trước khi Check-out", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")), Margin = new Thickness(0, 2, 0, 0) }); }

        ShowTab(int.TryParse((TabContent.Tag as string) ?? "0", out var t) ? t : 0);
    }

    private void ShowTab(int tab) { TabContent.Tag = tab.ToString(); TabContent.Children.Clear(); if (DepartureGrid.SelectedItem is not DepartureRow r) return; switch (tab) { case 0: ShowOverview(r); break; case 1: ShowFolio(r); break; case 2: ShowServices(r); break; case 3: ShowHandover(r); break; case 4: ShowRequests(r); break; } }

    private void ShowOverview(DepartureRow r)
    {
        TabContent.Children.Add(S("Khách", R("Họ tên", r.FullName), R("Điện thoại", r.Phone ?? "Chưa có dữ liệu"), R("Email", r.Email ?? "Chưa có dữ liệu"), R("Quốc tịch", r.Nationality ?? "Chưa có dữ liệu"), R("Giấy tờ", r.DocumentSummary ?? "Chưa có giấy tờ thật"), R("VIP", r.VipLevel ?? "Normal"), R("Ghi chú hồ sơ", r.Notes ?? "Chưa có dữ liệu")));
        TabContent.Children.Add(S("Lưu trú",
            R("Phòng", r.RoomNumber),
            R("Hạng", r.RoomType),
            R("Ngày đến", r.Arrival),
            R("Ngày đi", "Theo business date"),
            R("Giờ đi", r.Etd),
            R("Đêm", r.Nights),
            R("Số khách", r.GuestCount.ToString()),
            R("Trạng thái vận hành phòng", r.RoomOperationalStatus ?? "Chưa có snapshot phòng"),
            R("Trạng thái buồng phòng", r.HousekeepingStatus ?? "Chưa có snapshot buồng phòng"),
            R("Nguồn", "Theo hồ sơ Stay + Room snapshot thật")));
        TabContent.Children.Add(S("Kế hoạch rời", R("Giờ rời", r.Etd), R("Di chuyển", "Chưa tải yêu cầu khách"), R("Hành lý", "Chưa tải"), R("Gửi HL sau C/O", "Chưa tải"), R("Ghi chú", "Chưa tải")));
    }

    private void ShowFolio(DepartureRow r)
    {
        TabContent.Children.Add(S("Folio",
            R("Mã Folio", string.IsNullOrWhiteSpace(r.FolioNumber) ? "Chưa có Folio" : r.FolioNumber),
            R("Trạng thái", r.FolioStatus),
            R("Người thanh toán", r.FullName),
            R("Tổng phát sinh", r.TotalCharges?.ToString("N0") ?? "Chưa có dữ liệu"),
            R("Chi tiết giao dịch", r.LedgerSummary ?? "Chưa có ledger giao dịch"),
            R("Còn phải thu", r.BalanceDue),
            R("Tiền tệ", "Theo Folio"),
            R("Phương thức", "Theo Payment/Folio ledger")));
        var folioButton = new Button { Content = "Xem Folio", Style = TryFindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6) };
        folioButton.Click += OnViewFolio;
        TabContent.Children.Add(folioButton);
    }

    private void ShowServices(DepartureRow r)
    {
        TabContent.Children.Add(S("Dịch vụ đã ghi nhận trong Folio", R("Các khoản thực tế", r.LedgerSummary ?? "Chưa có ledger giao dịch để phân loại"), R("Tổng phát sinh", r.TotalCharges?.ToString("N0") ?? "Chưa có dữ liệu"), R("Cảnh báo", string.IsNullOrWhiteSpace(r.Alerts) ? "Không có cảnh báo từ nguồn đã tải" : r.Alerts)));
        var minibarButton = new Button { Content = "Yêu cầu chốt Minibar", Style = TryFindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6) };
        minibarButton.Click += OnSendHsk;
        TabContent.Children.Add(minibarButton);
    }

    private void ShowHandover(DepartureRow r)
    {
        var items = new[]
        {
            ("Trạng thái phòng", r.RoomOperationalStatus ?? "Chưa có snapshot phòng", "Buồng phòng", "Dữ liệu thật"),
            ("Trạng thái buồng phòng", r.HousekeepingStatus ?? "Chưa có snapshot buồng phòng", "Buồng phòng", "Dữ liệu thật"),
            ("Xác nhận Folio", r.BalanceDue == "Chưa kiểm tra" ? "Chưa kiểm tra Folio" : r.BalanceDue == "0" ? "Đã cân bằng" : "Còn phải thu", "Thu ngân", "Theo Folio thật"),
            ("Bàn giao chìa khóa/thẻ", "Chưa có nguồn task bàn giao", "Lễ tân", "Không giả trạng thái")
        };
        var sp = new StackPanel();
        foreach (var (label, status, who, when) in items)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) }; row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")) });
            var sc = status.Contains("Chưa") ? "#A16207" : "#94A3B8";
            row.Children.Add(new TextBlock { Text = status, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(sc)), HorizontalAlignment = HorizontalAlignment.Center }); Grid.SetColumn(row.Children[^1], 1);
            row.Children.Add(new TextBlock { Text = who, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")) }); Grid.SetColumn(row.Children[^1], 2);
            row.Children.Add(new TextBlock { Text = when, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) }); Grid.SetColumn(row.Children[^1], 3);
            sp.Children.Add(row);
        }
        TabContent.Children.Add(S("Bàn giao phòng", sp));
    }

    private void ShowRequests(DepartureRow r)
    {
        var reqs = new List<string>
        {
            string.IsNullOrWhiteSpace(r.Notes) ? "Hồ sơ khách không có ghi chú" : $"Ghi chú hồ sơ: {r.Notes}",
            string.IsNullOrWhiteSpace(r.Company) ? "Hồ sơ khách không có công ty" : $"Công ty: {r.Company}",
            "Chưa có nguồn dữ liệu yêu cầu vận hành riêng cho Stay này"
        };
        var reqSp = new StackPanel();
        foreach (var req in reqs)
            reqSp.Children.Add(new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFBFD")), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 0, 4), Child = new TextBlock { Text = req, FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")) } });
        TabContent.Children.Add(S("Yêu cầu / Ghi chú", reqSp));
    }

    private static FrameworkElement S(string title, params FrameworkElement[] children) { var sp = new StackPanel(); foreach (var c in children) sp.Children.Add(c); var h = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")), Margin = new Thickness(0, 0, 0, 6) }; var i = new StackPanel(); i.Children.Add(h); i.Children.Add(sp); return new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFBFD")), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")), BorderThickness = new Thickness(1), Child = i }; }
    private static TextBlock R(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F8198")), Margin = new Thickness(0, 2, 0, 0) }; tb.Inlines.Add(new System.Windows.Documents.Run(l + ": ") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) }); tb.Inlines.Add(new System.Windows.Documents.Run(v) { FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#17365D")) }); return tb; }
    private static FrameworkElement S(string title, StackPanel content) { var h = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")), Margin = new Thickness(0, 0, 0, 6) }; var i = new StackPanel(); i.Children.Add(h); i.Children.Add(content); return new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFBFD")), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")), BorderThickness = new Thickness(1), Child = i }; }

    private void OnTabClick(object s, RoutedEventArgs e) { if (s is Button b && b.Tag is string t && int.TryParse(t, out var tab)) ShowTab(tab); }
    private void OnFilterChanged(object s, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnQuickFilterChanged(object s, RoutedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = ""; FloorFilter.SelectedIndex = 0; RoomTypeFilter.SelectedIndex = 0; EtdFilter.SelectedIndex = 0; StatusFilter.SelectedIndex = 0; ChkUnpaid.IsChecked = false; ChkLate.IsChecked = false; ChkVip.IsChecked = false; ChkNeedCheck.IsChecked = false; ApplyFilters(); }
    private async void OnRefresh(object s, RoutedEventArgs e) => await LoadPersistedAsync();
    private void OnPrevPage(object s, RoutedEventArgs e) { _pager.PreviousPageCommand.Execute(null); }
    private void OnNextPage(object s, RoutedEventArgs e) { _pager.NextPageCommand.Execute(null); }

    private void AttachPager()
    {
        if (FooterTotal.Parent is not Grid footer || footer.Children.Count < 2 || footer.Children[1] is not StackPanel actions)
            return;

        foreach (var child in actions.Children.OfType<Button>().Take(2))
            child.Visibility = Visibility.Collapsed;

        var pager = new ServerPagerControl { DataContext = _pager, Margin = new Thickness(0, 0, 8, 0) };
        actions.Children.Insert(0, pager);
    }
    private void OnPrepCheckout(object s, RoutedEventArgs e)
    {
        if (DepartureGrid.SelectedItem is not DepartureRow r)
        {
            MessageBox.Show("Vui lòng chọn một khách.", "Khách đi hôm nay", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (r.StayId == Guid.Empty || r.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ Stay chưa có định danh/row version hợp lệ từ dữ liệu thật.", "Chuẩn bị Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new CheckoutPreparationWindow(r.StayId).ShowDialog();
    }
    private void OnExtendStay(object s, RoutedEventArgs e)
    {
        if (DepartureGrid.SelectedItem is not DepartureRow r)
        {
            ShowSelectDepartureWarning();
            return;
        }

        // ExtendStayWindow owns the real Stay/Rate/Reservation checks and mutation workflow.
        if (r.StayId == Guid.Empty || r.RowVersion is not { Length: 8 })
        {
            ShowUnavailable("Stay chưa có định danh/row version hợp lệ để gia hạn");
            return;
        }

        new ExtendStayWindow(r.StayId).ShowDialog();
    }

    private void OnViewFolio(object s, RoutedEventArgs e)
    {
        if (DepartureGrid.SelectedItem is not DepartureRow row || row.StayId == Guid.Empty)
        {
            ShowSelectDepartureWarning();
            return;
        }

        new FolioWorkspaceWindow(row.StayId).ShowDialog();
    }

    private void OnSendHsk(object s, RoutedEventArgs e)
    {
        if (DepartureGrid.SelectedItem is not DepartureRow row)
        {
            ShowSelectDepartureWarning();
            return;
        }

        new HousekeepingMessageWindow(row.RoomNumber).ShowDialog();
    }

    private void OnPrintFolio(object s, RoutedEventArgs e)
    {
        if (DepartureGrid.SelectedItem is not DepartureRow row)
        {
            ShowSelectDepartureWarning();
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
            dialog.PrintVisual(DepartureGrid, $"Folio tạm tính - {row.FullName}");
    }

    private void OnViewProfile(object s, RoutedEventArgs e)
    {
        if (DepartureGrid.SelectedItem is not DepartureRow)
        {
            ShowSelectDepartureWarning();
            return;
        }

        if (DepartureGrid.SelectedItem is DepartureRow row && row.StayId != Guid.Empty)
            new StayProfileWindow(row.StayId).ShowDialog();
        else
            ShowSelectDepartureWarning();
    }

    private void ShowSelectDepartureWarning() => MessageBox.Show("Vui lòng chọn một khách trước.", "Khách đi hôm nay", MessageBoxButton.OK, MessageBoxImage.Warning);
    private void ShowUnavailable(string message) => MessageBox.Show(message, "Khách đi hôm nay", MessageBoxButton.OK, MessageBoxImage.Information);
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record DepartureRow(string StatusText, string StatusBadgeBrush, string StatusColorBrush, string FolioStatus, string FolioBadgeBrush, string FolioColorBrush, string RoomNumber, string FullName, string Arrival, string Etd, string Nights, string RoomType, string BalanceDue, string Vip, string Alerts, Guid StayId = default, byte[]? RowVersion = null, string? FolioNumber = null, string? Phone = null, string? Email = null, string? Nationality = null, string? Company = null, string? VipLevel = null, string? Notes = null, int GuestCount = 1, string? RoomOperationalStatus = null, string? HousekeepingStatus = null, decimal? TotalCharges = null, string? LedgerSummary = null, string? DocumentSummary = null) { public string AlertColor => Alerts.Length > 1 ? "#DC2626" : string.IsNullOrEmpty(Alerts) ? "Transparent" : "#EA580C"; }
