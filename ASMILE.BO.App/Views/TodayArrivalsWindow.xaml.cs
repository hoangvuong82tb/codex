using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.App.Controls;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.Message;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Stay;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class TodayArrivalsWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private List<ArrivalRow> _allArrivals;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IHousekeepingBoardService? _housekeepingBoardService;
    private readonly IGuestProfileRepository? _guestProfileRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IReceptionUiService? _receptionUiService;
    private readonly IGuestMessageService? _guestMessageService;
    private readonly TablePagerViewModel<ArrivalRow> _pager = new() { ItemName = "khách", SelectedPageSize = "20 / trang" };
    private readonly Dictionary<Guid, string> _guestDocumentsByProfile = new();

    public TodayArrivalsWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _housekeepingBoardService = services?.GetService<IHousekeepingBoardService>();
        _guestProfileRepository = services?.GetService<IGuestProfileRepository>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _receptionUiService = services?.GetService<IReceptionUiService>();
        _guestMessageService = services?.GetService<IGuestMessageService>();
        _allArrivals = new List<ArrivalRow>();
        AttachPager();
        ShowTab(0);
        UpdateMetrics();
        ApplyFilters();
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e) => await LoadPersistedAsync();

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
            var stays = await _stayRepository.GetExpectedArrivalsAsync(context.Property.PropertyId.Value, context.BusinessDate.BusinessDate, CancellationToken.None);
            var profiles = new Dictionary<Guid, GuestProfileDto>();
            var folios = new Dictionary<Guid, HotelFolioDto>();
            var roomSnapshots = new Dictionary<Guid, HkRoomSnapshot>();
            if (_guestProfileRepository is not null)
            {
                _guestDocumentsByProfile.Clear();
                foreach (var stay in stays)
                {
                    if (stay.GuestId is not Guid guestId) continue;
                    try
                    {
                        var profile = await _guestProfileRepository.GetDtoAsync(context.Property.PropertyId.Value, guestId, CancellationToken.None);
                        if (profile is not null) profiles[guestId] = profile;

                        var fullProfile = await _guestProfileRepository.GetAsync(context.Property.PropertyId.Value, guestId, CancellationToken.None);
                        _guestDocumentsByProfile[guestId] = fullProfile?.Documents.Count > 0
                            ? string.Join(" · ", fullProfile.Documents.Select(x => $"{x.DocumentType}: {x.DocumentNumber}"))
                            : "Chưa có giấy tờ thật";
                    }
                    catch
                    {
                        // Keep the arrival visible when the optional guest profile query fails.
                    }
                }
            }

            if (_folioRepository is not null)
            {
                foreach (var stay in stays)
                {
                    try
                    {
                        var folio = await _folioRepository.GetByStayAsync(context.Property.PropertyId.Value, stay.HotelStayId, CancellationToken.None);
                        if (folio is not null) folios[stay.HotelStayId] = folio;
                    }
                    catch
                    {
                        // Keep the arrival visible when the optional Folio query fails.
                    }
                }
            }

            if (_housekeepingBoardService is not null)
            {
                try
                {
                    roomSnapshots = (await _housekeepingBoardService.GetRoomSnapshotAsync(
                        context.Property.PropertyId.Value,
                        CancellationToken.None)).ToDictionary(x => x.RoomId);
                }
                catch
                {
                    // Keep the arrival list available when housekeeping is unavailable.
                }
            }

            _allArrivals = stays.Select(stay =>
            {
                var profile = stay.GuestId is Guid guestId && profiles.TryGetValue(guestId, out var value) ? value : null;
                var folio = folios.TryGetValue(stay.HotelStayId, out var persistedFolio) ? persistedFolio : null;
                var room = roomSnapshots.TryGetValue(stay.RoomId, out var snapshot) ? snapshot : null;
                var documents = stay.GuestId is Guid guestIdValue && _guestDocumentsByProfile.TryGetValue(guestIdValue, out var summary)
                    ? summary
                    : "Chưa tải được hồ sơ giấy tờ thật";
                return MapPersistedStay(stay, profile, folio, room, documents);
            }).ToList();
            ApplyFilters();
            FooterTotal.Text = $"Đã tải {_allArrivals.Count:N0} khách dự kiến đến từ dữ liệu Stay thật.";
        }
        catch (Exception ex)
        {
            _allArrivals = new List<ArrivalRow>();
            ApplyFilters();
            FooterTotal.Text = $"Không tải được khách đến thật: {ex.Message}";
        }
    }

    private static ArrivalRow MapPersistedStay(HotelStayDto stay, GuestProfileDto? profile, HotelFolioDto? folio, HkRoomSnapshot? room, string documentSummary)
    {
        var checkedIn = stay.ActualCheckInAtUtc.HasValue || stay.Status is HotelStayStatus.CheckedIn or HotelStayStatus.InHouse or HotelStayStatus.RoomMoved;
        var badge = checkedIn ? ("#DCFCE7", "#16A34A") : ("#DBEAFE", "#2563EB");
        var nights = Math.Max(1, stay.ScheduledDepartureDate.DayNumber - stay.ScheduledArrivalDate.DayNumber);
        return new ArrivalRow(
            "—",
            stay.GuestName,
            stay.ReservationNumber ?? stay.StayNumber,
            string.IsNullOrWhiteSpace(stay.Source) ? "—" : stay.Source,
            stay.RoomTypeCode,
            stay.RoomNumber,
            nights.ToString(),
            (stay.Adults + stay.Children).ToString(),
            checkedIn ? "Đã Check-in" : "Đã xác nhận",
            badge.Item1,
            badge.Item2,
            profile?.VipLevel is not null and not VipLevel.Normal ? "★" : "",
            profile?.Notes ?? "",
            stay.HotelStayId,
            stay.RowVersion,
            checkedIn,
            stay.GuestId,
            profile?.Phone,
            profile?.Email,
            profile?.Nationality,
            profile?.CompanyName,
            profile?.VipLevel.ToString(),
            profile?.Notes,
            folio?.FolioNumber,
            folio?.Status.ToString(),
            folio?.OutstandingBalance,
            room?.OperationalStatus,
            room?.HousekeepingStatus,
            documentSummary);
    }

    private void ApplyFilters()
    {
        var filtered = _allArrivals.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            var kw = SearchBox.Text.ToLower();
            filtered = filtered.Where(r => r.GuestName.ToLower().Contains(kw) || r.BookingRef.ToLower().Contains(kw) || r.RoomNumber.Contains(kw));
        }
        if (SourceFilter.SelectedItem is ComboBoxItem sc && sc.Content is string sv && sv != "Tất cả")
            filtered = filtered.Where(r => r.Source == sv);
        if (StatusFilter.SelectedItem is ComboBoxItem ss && ss.Content is string stv && stv != "Tất cả")
            filtered = filtered.Where(r => r.StatusText == stv);
        if (VipFilter.SelectedItem is ComboBoxItem vc && vc.Content is string vv && vv != "Tất cả")
            filtered = filtered.Where(r => (vv == "Có") == (r.Vip == "★"));
        if (EtaFilter.SelectedItem is ComboBoxItem ec && ec.Content is string ev && ev != "Tất cả")
            filtered = filtered.Where(r => GetEtaSlot(r.ETA) == ev);

        var list = filtered.ToList();
        ApplyPage(list);
        UpdateMetrics(list);
    }

    private static string GetEtaSlot(string eta)
    {
        if (eta.Length >= 2 && int.TryParse(eta[..2], out var h))
            return h switch { >= 6 and < 12 => "Sáng (6-12h)", >= 12 and < 18 => "Chiều (12-18h)", >= 18 and < 24 => "Tối (18-24h)", _ => "Late (0-6h)" };
        return "—";
    }

    private void ApplyPage(List<ArrivalRow> list)
    {
        _pager.SetSource(list);
        ArrivalGrid.ItemsSource = _pager.Items;
        ResultCount.Text = $"{list.Count} khách";
        FooterTotal.Text = $"Tổng: {list.Count} khách đến";
    }

    private void UpdateMetrics(List<ArrivalRow>? list = null)
    {
        list ??= _allArrivals;
        MetricTotal.Text = list.Count.ToString();
        MetricCheckedIn.Text = list.Count(r => r.StatusText == "Đã Check-in").ToString();
        MetricPending.Text = list.Count(r => r.StatusText != "Đã Check-in").ToString();
        MetricVip.Text = list.Count(r => r.Vip == "★").ToString();
        MetricOta.Text = list.Count(r => r.Source == "OTA").ToString();
        MetricRoomPrep.Text = list.Count(r => r.Remark.Contains("dơ") || r.Remark.Contains("assign") || r.Remark.Contains("Phòng")).ToString();
    }

    private void OnArrivalSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ArrivalGrid.SelectedItem is not ArrivalRow r)
        {
            DetailEmpty.Visibility = Visibility.Visible;
            DetailContent.Visibility = Visibility.Collapsed;
            TabContent.Children.Clear();
            return;
        }
        DetailEmpty.Visibility = Visibility.Collapsed;
        DetailContent.Visibility = Visibility.Visible;

        var parts = r.GuestName.Split(' ');
        DetailInitials.Text = parts.Length > 0 ? parts[^1][..1].ToUpper() : "?";
        DetailName.Text = r.GuestName;
        DetailBookingMeta.Text = $"Booking #{r.BookingRef}";
        DetailEtaMeta.Text = r.ETA;
        DetailVipBadge.Visibility = r.Vip == "★" ? Visibility.Visible : Visibility.Collapsed;
        DetailNights.Text = r.Nights;
        DetailGuests.Text = r.GuestCount;
        DetailRoom.Text = r.RoomNumber;
        DetailRate.Text = "Chưa tải rate từ Reservation/Folio";

        ShowTab(int.TryParse((TabContent.Tag as string) ?? "0", out var t) ? t : 0);
    }

    private void ShowTab(int tab)
    {
        TabContent.Tag = tab.ToString();
        TabContent.Children.Clear();
        if (ArrivalGrid.SelectedItem is not ArrivalRow r) return;

        switch (tab)
        {
            case 0: ShowOverview(r); break;
            case 1: ShowGuest(r); break;
            case 2: ShowRoom(r); break;
            case 3: ShowDocs(r); break;
            case 4: ShowFinance(r); break;
        }
    }

    private void ShowOverview(ArrivalRow r)
    {
        TabContent.Children.Add(Section("Booking",
            Row("Confirmation", r.BookingRef), Row("Nguồn", r.Source),
            Row("Company/TA", r.Company ?? "Chưa có dữ liệu"),
            Row("Guarantee", "Chưa tải từ Reservation"),
            Row("Ngày tạo", "Chưa có trong Stay DTO"), Row("Tạo bởi", "Theo hồ sơ Stay")));
        TabContent.Children.Add(Section("Lưu trú",
            Row("Ngày đến", "Theo business date"), Row("Ngày đi", "Theo hồ sơ Stay"),
            Row("Số đêm", r.Nights), Row("Số khách", r.GuestCount),
            Row("Ghi chú", string.IsNullOrEmpty(r.Remark) ? "—" : r.Remark)));
    }

    private void ShowGuest(ArrivalRow r)
    {
        TabContent.Children.Add(Section("Thông tin khách",
            Row("Họ tên", r.GuestName), Row("Điện thoại", r.Phone ?? "Chưa có dữ liệu"),
            Row("Email", r.Email ?? "Chưa có dữ liệu"), Row("Quốc tịch", r.Nationality ?? "Chưa có dữ liệu"),
            Row("Giấy tờ", r.DocumentSummary ?? "Chưa có giấy tờ thật"),
            Row("VIP/Membership", r.VipLevel ?? "Normal"),
            Row("Ghi chú hồ sơ", r.Notes ?? "Chưa có dữ liệu"),
            Row("Special Request", "Chưa tải từ Reservation/Guest")));
    }

    private void ShowRoom(ArrivalRow r)
    {
        TabContent.Children.Add(Section("Phòng",
            Row("Loại phòng", r.RoomType), Row("Phòng assigned", r.RoomNumber),
            Row("Tầng", r.RoomNumber.Length > 0 ? r.RoomNumber[..1] : "—"),
            Row("Trạng thái phòng", r.RoomOperationalStatus ?? "Chưa có dữ liệu"),
            Row("Trạng thái buồng", r.HousekeepingStatus ?? "Chưa có dữ liệu"),
            Row("HSK Note", "Chưa có task note trong snapshot"), Row("ETA Ready", "Theo trạng thái buồng")));
    }

    private void ShowDocs(ArrivalRow r)
    {
        TabContent.Children.Add(Section("Giấy tờ",
            Row("Trạng thái", r.DocumentSummary ?? "Chưa có giấy tờ thật"),
            Row("Nguồn", "GuestProfile thật"), Row("Online Check-in", "Không có trường dữ liệu thật"),
            Row("OCR", "Không có trường dữ liệu thật"), Row("Thiếu giấy tờ", "Đối chiếu theo hồ sơ GuestProfile")));
    }

    private void ShowFinance(ArrivalRow r)
    {
        TabContent.Children.Add(Section("Tài chính",
            Row("Rate Code", "Chưa tải Reservation rate"), Row("Room Rate", "Chưa tải"),
            Row("Folio", r.FolioNumber ?? "Chưa có Folio"),
            Row("Trạng thái Folio", r.FolioStatus ?? "Chưa có dữ liệu"),
            Row("Còn phải thu", r.FolioBalance.HasValue ? r.FolioBalance.Value.ToString("N0") : "Chưa có dữ liệu"),
            Row("Currency", "Theo property/Folio"), Row("Payment", "Xem chi tiết trong Folio"), Row("VAT", "Chưa tải")));
    }

    private static FrameworkElement Section(string title, params FrameworkElement[] children)
    {
        var sp = new StackPanel(); foreach (var c in children) sp.Children.Add(c);
        var h = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")), Margin = new Thickness(0, 0, 0, 6) };
        var inner = new StackPanel(); inner.Children.Add(h); inner.Children.Add(sp);
        return new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFBFD")), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")), BorderThickness = new Thickness(1), Child = inner };
    }

    private static TextBlock Row(string label, string value)
    {
        var tb = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F8198")), Margin = new Thickness(0, 2, 0, 0) };
        tb.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) });
        tb.Inlines.Add(new System.Windows.Documents.Run(value) { FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#17365D")) });
        return tb;
    }

    private void OnTabClick(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is string t && int.TryParse(t, out var tab)) ShowTab(tab); }
    private void OnFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object sender, RoutedEventArgs e) { SearchBox.Text = ""; SourceFilter.SelectedIndex = 0; StatusFilter.SelectedIndex = 0; VipFilter.SelectedIndex = 0; EtaFilter.SelectedIndex = 0; DateFilter.SelectedDate = null; ApplyFilters(); }
    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadPersistedAsync();
    private void OnPrevPage(object sender, RoutedEventArgs e) { _pager.PreviousPageCommand.Execute(null); }
    private void OnNextPage(object sender, RoutedEventArgs e) { _pager.NextPageCommand.Execute(null); }

    private void AttachPager()
    {
        if (FooterTotal.Parent is not Grid footer || footer.Children.Count < 2 || footer.Children[1] is not StackPanel actions)
            return;

        foreach (var child in actions.Children.OfType<Button>().Take(2))
            child.Visibility = Visibility.Collapsed;

        var pager = new ServerPagerControl { DataContext = _pager, Margin = new Thickness(0, 0, 8, 0) };
        actions.Children.Insert(0, pager);
    }
    private async void OnCheckIn(object sender, RoutedEventArgs e)
    {
        if (ArrivalGrid.SelectedItem is not ArrivalRow row)
        {
            ShowSelectArrivalWarning();
            return;
        }

        if (row.StayId == Guid.Empty || row.IsCheckedIn || _receptionUiService is null)
        {
            MessageBox.Show("Lưu trú này đã check-in hoặc chưa có đủ thông tin để thực hiện thao tác thật.", "Khách đến hôm nay", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show($"Xác nhận check-in cho {row.GuestName} - phòng {row.RoomNumber}?", "Check-in", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
            return;

        try
        {
            var result = await _receptionUiService.CheckInAsync(row.StayId, row.RowVersion ?? [], CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể check-in lưu trú.", "Check-in", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Đã check-in {result.Value!.GuestName} - phòng {result.Value.RoomNumber}.", "Check-in", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadPersistedAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể check-in: {ex.Message}", "Check-in", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void OnChangeRoom(object sender, RoutedEventArgs e)
    {
        if (ArrivalGrid.SelectedItem is not ArrivalRow)
        {
            ShowSelectArrivalWarning();
            return;
        }

        if (ArrivalGrid.SelectedItem is ArrivalRow row && row.StayId != Guid.Empty)
            new RoomMoveWindow(row.StayId).ShowDialog();
        else
            ShowUnavailable("Stay đang chọn chưa có định danh thật để đổi phòng.");
    }

    private void OnViewBooking(object sender, RoutedEventArgs e)
    {
        if (ArrivalGrid.SelectedItem is not ArrivalRow row)
        {
            ShowSelectArrivalWarning();
            return;
        }

        // Open the real profile query with the selected Stay context.
        new StayProfileWindow(row.StayId == Guid.Empty ? null : row.StayId).ShowDialog();
    }

    private void OnPrintReg(object sender, RoutedEventArgs e)
    {
        if (ArrivalGrid.SelectedItem is not ArrivalRow row)
        {
            ShowSelectArrivalWarning();
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
            dialog.PrintVisual(ArrivalGrid, $"Registration Card - {row.GuestName}");
    }

    private void OnSendHsk(object sender, RoutedEventArgs e)
    {
        if (ArrivalGrid.SelectedItem is not ArrivalRow row)
        {
            ShowSelectArrivalWarning();
            return;
        }

        // Filter the persisted Housekeeping board to the selected room; no synthetic request is created here.
        new HousekeepingMessageWindow(row.RoomNumber).ShowDialog();
    }
    private async void OnSendBell(object sender, RoutedEventArgs e)
    {
        if (ArrivalGrid.SelectedItem is not ArrivalRow row)
        {
            ShowSelectArrivalWarning();
            return;
        }
        if (row.StayId == Guid.Empty || _guestMessageService is null)
        {
            ShowUnavailable("Stay hoặc service Bellman chưa sẵn sàng trong dữ liệu thật.");
            return;
        }

        try
        {
            await _guestMessageService.CreateAndSendAsync(new CreateGuestMessageRequest(
                GuestId: row.GuestId,
                ReservationId: null,
                StayId: row.StayId,
                RoomId: null,
                GuestName: row.GuestName,
                RoomNumber: row.RoomNumber,
                MessageType: "Bellman",
                Channel: "FrontDesk",
                Priority: "Normal",
                Subject: $"Yêu cầu Bellman - phòng {row.RoomNumber}",
                Content: $"Vui lòng hỗ trợ Bellman cho khách {row.GuestName}, phòng {row.RoomNumber}.",
                RequiresResponse: false,
                Department: "Bellman"), CancellationToken.None);
            MessageBox.Show($"Đã tạo yêu cầu Bellman thật cho phòng {row.RoomNumber}.", "Khách đến hôm nay", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tạo yêu cầu Bellman: {ex.Message}", "Khách đến hôm nay", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowUnavailable(string message)
    {
        MessageBox.Show(message, "Khách đến hôm nay", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowSelectArrivalWarning() => MessageBox.Show("Vui lòng chọn một booking trước.", "Khách đến hôm nay", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

internal record ArrivalRow(
    string ETA, string GuestName, string BookingRef, string Source,
    string RoomType, string RoomNumber, string Nights, string GuestCount,
    string StatusText, string StatusBadgeBrush, string StatusColorBrush,
    string Vip, string Remark, Guid StayId = default, byte[]? RowVersion = null, bool IsCheckedIn = false, Guid? GuestId = null,
    string? Phone = null, string? Email = null, string? Nationality = null, string? Company = null,
    string? VipLevel = null, string? Notes = null, string? FolioNumber = null, string? FolioStatus = null, decimal? FolioBalance = null,
    string? RoomOperationalStatus = null, string? HousekeepingStatus = null, string? DocumentSummary = null);
