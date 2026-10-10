using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ASMILE.BO.App.Controls;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Stay;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class CurrentGuestsWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private List<GuestRow> _allGuests;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelStayOccupantRepository? _occupantRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IBusinessDocumentService? _businessDocumentService;
    private readonly HotelStayApplicationService? _stayService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly TablePagerViewModel<GuestRow> _pager = new() { ItemName = "khách", SelectedPageSize = "20 / trang" };
    private IReadOnlyList<BusinessDocumentDto> _selectedDocuments = Array.Empty<BusinessDocumentDto>();
    private FolioLedgerDto? _selectedLedger;
    private int _detailsLoadVersion;

    public CurrentGuestsWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _occupantRepository = services?.GetService<IHotelStayOccupantRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _businessDocumentService = services?.GetService<IBusinessDocumentService>();
        _stayService = services?.GetService<HotelStayApplicationService>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _allGuests = new List<GuestRow>();
        AttachPager();
        ShowTab(0);
        UpdateMetrics();
        Loaded += async (_, _) => await LoadPersistedAsync();
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async Task LoadPersistedAsync()
    {
        if (_stayRepository is null || _occupantRepository is null || _executionContext?.Current is null)
        {
            ApplyFilters();
            FooterTotal.Text = "Chưa cấu hình được Stay/occupant service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var propertyId = _executionContext.Current.Property.PropertyId.Value;
            var stays = await _stayRepository.GetCurrentInHouseAsync(propertyId, CancellationToken.None);
            _allGuests.Clear();
            foreach (var stay in stays)
            {
                var primary = MapPersistedStay(stay);
                if (_folioRepository is not null)
                {
                    try
                    {
                        var folio = await _folioRepository.GetByStayAsync(propertyId, stay.HotelStayId, CancellationToken.None);
                        primary = primary with { FolioBalance = folio?.OutstandingBalance };
                    }
                    catch
                    {
                        // Keep the Stay row visible, but do not invent a financial value when Folio is unavailable.
                    }
                }
                _allGuests.Add(primary);
                var occupants = await _occupantRepository.GetActiveAsync(propertyId, stay.HotelStayId, CancellationToken.None);
                _allGuests.AddRange(occupants.Select(occupant => primary with
                {
                    FullName = occupant.FullName,
                    Role = occupant.OccupantType == HotelStayOccupantType.Child ? "Trẻ em đi cùng" : "Khách đi cùng",
                    Phone = occupant.Phone ?? "—",
                    OccupantId = occupant.OccupantId,
                    OccupantRowVersion = occupant.RowVersion,
                    GuestProfileId = occupant.GuestProfileId
                }));
            }
            ApplyFilters();
            FooterTotal.Text = $"Đã tải {_allGuests.Count:N0} khách lưu trú từ dữ liệu Stay thật.";
        }
        catch (Exception ex)
        {
            _allGuests.Clear();
            ApplyFilters();
            FooterTotal.Text = $"Không tải được khách lưu trú thật: {ex.Message}";
        }
    }

    private static GuestRow MapPersistedStay(HotelStayDto stay)
    {
        var status = stay.Status switch
        {
            HotelStayStatus.ExpectedDeparture => ("Trả hôm nay", "#DBEAFE", "#2563EB"),
            HotelStayStatus.Extended => ("Gia hạn", "#FFF7ED", "#EA580C"),
            HotelStayStatus.RoomMoved or HotelStayStatus.CheckedIn or HotelStayStatus.InHouse => ("Đang lưu trú", "#DCFCE7", "#16A34A"),
            _ => (stay.Status.ToString(), "#F1F5F9", "#64748B")
        };
        var nights = Math.Max(1, stay.ScheduledDepartureDate.DayNumber - stay.ScheduledArrivalDate.DayNumber);
        return new GuestRow(
            status.Item1,
            status.Item2,
            status.Item3,
            stay.RoomNumber,
            stay.GuestName,
            "Khách chính",
            stay.ScheduledArrivalDate.ToString("dd/MM"),
            stay.ScheduledDepartureDate.ToString("dd/MM"),
            nights.ToString(),
            stay.RoomTypeCode,
            "—",
            stay.StayNumber,
            "",
            "",
            stay.HotelStayId,
            stay.RowVersion,
            GuestProfileId: stay.GuestId);
    }

    private void ApplyFilters()
    {
        var filtered = _allGuests.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            var kw = SearchBox.Text.ToLower();
            filtered = filtered.Where(r =>
                r.FullName.ToLower().Contains(kw) || r.RoomNumber.Contains(kw) ||
                r.Phone.Contains(kw) || r.FolioId.ToLower().Contains(kw));
        }
        if (FloorFilter.SelectedItem is ComboBoxItem f && f.Content is string fl && fl != "Tất cả")
            filtered = filtered.Where(r => r.RoomNumber.StartsWith(fl));
        if (RoomTypeFilter.SelectedItem is ComboBoxItem rt && rt.Content is string rtv && rtv != "Tất cả")
            filtered = filtered.Where(r => r.RoomType == rtv);
        if (StatusFilter.SelectedItem is ComboBoxItem s && s.Content is string st && st != "Tất cả")
            filtered = filtered.Where(r => r.StatusText == st);
        if (FolioFilter.SelectedItem is ComboBoxItem ff && ff.Content is string fv && fv != "Tất cả")
            filtered = filtered.Where(r => r.FolioStatus == fv);
        if (ChkDeparting.IsChecked == true)
            filtered = filtered.Where(r => r.StatusText == "Trả hôm nay");
        if (ChkAlert.IsChecked == true)
            filtered = filtered.Where(r => !string.IsNullOrEmpty(r.Alert));

        var list = filtered.ToList();
        ApplyPage(list);
        UpdateMetrics(list);
    }

    private void ApplyPage(List<GuestRow> list)
    {
        _pager.SetSource(list);
        GuestGrid.ItemsSource = _pager.Items;
        ResultCount.Text = $"{list.Count} khách";
        FooterTotal.Text = $"Tổng: {list.Count} khách · {list.Select(r => r.RoomNumber).Distinct().Count()} phòng";
        PageInfo.Text = _pager.PageText;
    }

    private void UpdateMetrics(List<GuestRow>? list = null)
    {
        list ??= _allGuests;
        MetricTotalGuests.Text = list.Count.ToString();
        MetricOccupiedRooms.Text = list.Select(r => r.RoomNumber).Distinct().Count().ToString();
        MetricDeparting.Text = list.Count(r => r.StatusText == "Trả hôm nay").ToString();
        MetricOverstay.Text = list.Count(r => r.StatusText == "Quá hạn").ToString();
        MetricFolioAlert.Text = list.Count(r => !string.IsNullOrEmpty(r.Alert)).ToString();
    }

    private void OnGuestSelected(object sender, SelectionChangedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow r)
        {
            DetailEmpty.Visibility = Visibility.Visible;
            DetailContent.Visibility = Visibility.Collapsed;
            TabContent.Children.Clear();
            return;
        }
        DetailEmpty.Visibility = Visibility.Collapsed;
        DetailContent.Visibility = Visibility.Visible;

        var parts = r.FullName.Split(' ');
        DetailInitials.Text = parts.Length > 0 ? parts[^1][..1].ToUpper() : "?";
        DetailName.Text = r.FullName;
        DetailRoleText.Text = r.Role;
        DetailBadgeRole.Background = r.Role == "Khách chính" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
        DetailRoleText.Foreground = r.Role == "Khách chính" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
        DetailRoomMeta.Text = $"Phòng {r.RoomNumber} · {r.RoomType}";
        DetailStatusMeta.Text = r.StatusText;
        DetailNights.Text = r.Nights;
        DetailDeparture.Text = r.Departure;
        DetailGuestCount.Text = _allGuests.Count(g => g.RoomNumber == r.RoomNumber).ToString();
        DetailBalance.Text = r.FolioBalance is decimal balance
            ? $"{balance:N0} · {(balance > 0 ? "Còn phải thu" : "Đã cân bằng")}"
            : "Chưa tải được Folio thật";

        ShowTab(int.TryParse((TabContent.Tag as string) ?? "0", out var t) ? t : 0);
        _ = LoadSelectedDetailsAsync(r);
    }

    private void ShowTab(int tab)
    {
        TabContent.Tag = tab.ToString();
        TabContent.Children.Clear();
        if (GuestGrid.SelectedItem is not GuestRow r) return;

        switch (tab)
        {
            case 0: ShowOverview(r); break;
            case 1: ShowStay(r); break;
            case 2: ShowDocuments(r); break;
            case 3: ShowRoomService(r); break;
            case 4: ShowFolio(r); break;
        }
    }

    private void ShowOverview(GuestRow r)
    {
        TabContent.Children.Add(Section("Thông tin khách",
            Row("Họ và tên", r.FullName), Row("Vai trò", r.Role),
            Row("Điện thoại", r.Phone), Row("Guest profile", "Đang dùng dữ liệu Stay; thông tin hồ sơ chi tiết tải ở module Khách hàng")));
        TabContent.Children.Add(Section("Lưu trú hiện tại",
            Row("Mã lưu trú", r.FolioId),
            Row("Số phòng", r.RoomNumber), Row("Hạng phòng", r.RoomType),
            Row("Ngày đến", r.Arrival), Row("Ngày đi", r.Departure),
            Row("Số đêm", r.Nights), Row("Nguồn", "Theo hồ sơ Stay")));
    }

    private void ShowStay(GuestRow r)
    {
        TabContent.Children.Add(Section("Chi tiết lưu trú",
            Row("Ngày đến", r.Arrival), Row("Giờ đến", "Chưa có dữ liệu"),
            Row("Ngày đi", r.Departure), Row("Giờ đi", "Chưa có dữ liệu"),
            Row("Số đêm", r.Nights), Row("Book Status", r.StatusText),
            Row("Trạng thái Stay", r.StatusText), Row("Nguồn", "Theo hồ sơ Stay")));
        var extend = new Button { Content = "Gia hạn lưu trú", Style = TryFindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6), Tag = "action" };
        extend.Click += OnExtendStay;
        TabContent.Children.Add(extend);
        var move = new Button { Content = "Đổi phòng", Style = TryFindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6), Tag = "action" };
        move.Click += OnChangeRoom;
        TabContent.Children.Add(move);
    }

    private void ShowDocuments(GuestRow r)
    {
        var children = new List<FrameworkElement>();
        if (_selectedDocuments.Count == 0)
        {
            var hasGuestProfile = r.GuestProfileId is { } guestId && guestId != Guid.Empty;
            children.Add(Row("Trạng thái", hasGuestProfile
                ? "Chưa có tài liệu giấy tờ thật"
                : "Khách chưa có GuestProfileId thật"));
        }
        else
        {
            foreach (var document in _selectedDocuments)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
                row.Children.Add(new TextBlock
                {
                    Text = $"{document.FileName} · {document.CreatedAt:dd/MM/yyyy HH:mm}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#17365D")),
                    VerticalAlignment = VerticalAlignment.Center
                });
                var open = new Button
                {
                    Content = "Mở",
                    Tag = document,
                    Style = TryFindResource("OutlineBtn") as Style,
                    Height = 26,
                    Padding = new Thickness(10, 0, 10, 0),
                    Margin = new Thickness(8, 0, 0, 0)
                };
                open.Click += OnOpenGuestDocument;
                row.Children.Add(open);
                children.Add(row);
            }
        }

        children.Add(Row("Nguồn", "BusinessDocuments / hồ sơ khách thật"));
        TabContent.Children.Add(Section("Giấy tờ", children.ToArray()));
    }

    private void ShowRoomService(GuestRow r)
    {
        TabContent.Children.Add(Section("Thông tin phòng",
            Row("Số phòng", r.RoomNumber), Row("Hạng phòng", r.RoomType),
            Row("Tầng", r.RoomNumber[..1]), Row("Tình trạng phòng", "Đang có khách"),
            Row("Số khách", _allGuests.Count(g => g.RoomNumber == r.RoomNumber).ToString()), Row("Sức chứa", "Theo master phòng")));
        var serviceRows = new List<FrameworkElement>();
        var entries = _selectedLedger?.Entries ?? Array.Empty<HotelFolioEntryDto>();
        if (entries.Count == 0)
        {
            serviceRows.Add(Row("Phát sinh", "Chưa có phát sinh Folio thật"));
        }
        else
        {
            foreach (var entry in entries.OrderByDescending(x => x.CreatedAtUtc).Take(20))
            {
                serviceRows.Add(Row(
                    entry.BusinessDate?.ToString("dd/MM/yyyy") ?? entry.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy"),
                    $"{entry.EntryType}: {entry.Description} · {entry.Amount:N0}"));
            }
        }

        serviceRows.Add(Row("Nguồn", "Folio ledger thật của Stay"));
        TabContent.Children.Add(Section("Dịch vụ & phát sinh", serviceRows.ToArray()));
    }

    private async Task LoadSelectedDetailsAsync(GuestRow row)
    {
        var loadVersion = ++_detailsLoadVersion;
        _selectedDocuments = Array.Empty<BusinessDocumentDto>();
        _selectedLedger = null;

        try
        {
            if (_businessDocumentService is not null && row.GuestProfileId is { } guestId && guestId != Guid.Empty)
                _selectedDocuments = await _businessDocumentService.GetDocumentsByCustomerAsync(guestId);

            if (_folioRepository is not null && _executionContext?.Current is { } context && row.StayId != Guid.Empty)
            {
                var folio = await _folioRepository.GetByStayAsync(context.Property.PropertyId.Value, row.StayId, CancellationToken.None);
                if (folio is not null)
                    _selectedLedger = await _folioRepository.GetLedgerAsync(context.Property.PropertyId.Value, folio.HotelFolioId, CancellationToken.None);
            }
        }
        catch
        {
            if (loadVersion != _detailsLoadVersion)
                return;
            _selectedDocuments = Array.Empty<BusinessDocumentDto>();
            _selectedLedger = null;
        }

        if (loadVersion != _detailsLoadVersion)
            return;

        if (GuestGrid.SelectedItem is GuestRow selected && selected.StayId == row.StayId && selected.GuestProfileId == row.GuestProfileId)
        {
            var tab = int.TryParse(TabContent.Tag as string, out var currentTab) ? currentTab : 0;
            if (tab is 2 or 3)
                ShowTab(tab);
        }
    }

    private void OnOpenGuestDocument(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button
            || button.Tag is not BusinessDocumentDto document
            || string.IsNullOrWhiteSpace(document.PhysicalFilePath)
            || !File.Exists(document.PhysicalFilePath))
        {
            MessageBox.Show("Tài liệu chưa có file thật trên vùng lưu trữ.", "Giấy tờ", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(document.PhysicalFilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể mở tài liệu: {ex.Message}", "Giấy tờ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowFolio(GuestRow r)
    {
        TabContent.Children.Add(Section("Tài chính",
            Row("Mã lưu trú", r.FolioId), Row("Người thanh toán", r.FullName),
            Row("Số dư Folio", r.FolioBalance is decimal balance ? balance.ToString("N0") : "Chưa tải được Folio thật"),
            Row("Trạng thái", r.FolioBalance is decimal currentBalance
                ? currentBalance > 0 ? "Còn phải thu" : "Đã cân bằng"
                : "Mở Folio để kiểm tra dữ liệu thật")));
    }

    private static FrameworkElement Section(string title, params FrameworkElement[] children)
    {
        var sp = new StackPanel();
        foreach (var c in children) sp.Children.Add(c);
        var header = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")), Margin = new Thickness(0, 0, 0, 6) };
        var inner = new StackPanel(); inner.Children.Add(header); inner.Children.Add(sp);
        return new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFBFD")), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")), BorderThickness = new Thickness(1), Child = inner };
    }

    private static TextBlock Row(string label, string value)
    {
        var tb = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F8198")), Margin = new Thickness(0, 2, 0, 0) };
        tb.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) });
        tb.Inlines.Add(new System.Windows.Documents.Run(value) { FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#17365D")) });
        return tb;
    }

    private void OnTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string t && int.TryParse(t, out var tab))
            ShowTab(tab);
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnSearchKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) ApplyFilters(); }
    private void OnQuickFilterChanged(object sender, RoutedEventArgs e) => ApplyFilters();

    private void OnClearFilter(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = ""; FloorFilter.SelectedIndex = 0; RoomTypeFilter.SelectedIndex = 0;
        StatusFilter.SelectedIndex = 0; FolioFilter.SelectedIndex = 0;
        DepartureFilter.SelectedDate = null; ChkDeparting.IsChecked = false; ChkAlert.IsChecked = false;
        ApplyFilters();
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadPersistedAsync();
    private void OnPrevPage(object sender, RoutedEventArgs e) { _pager.PreviousPageCommand.Execute(null); }
    private void OnNextPage(object sender, RoutedEventArgs e) { _pager.NextPageCommand.Execute(null); }

    private void AttachPager()
    {
        if (FooterTotal.Parent is not Grid footer || footer.Children.Count < 2 || footer.Children[1] is not StackPanel actions)
            return;

        foreach (var child in actions.Children.OfType<Button>().Take(2))
            child.Visibility = Visibility.Collapsed;

        PageInfo.Visibility = Visibility.Collapsed;
        var pager = new ServerPagerControl { DataContext = _pager, Margin = new Thickness(0, 0, 8, 0) };
        actions.Children.Insert(0, pager);
        _pager.PropertyChanged += (_, _) => PageInfo.Text = _pager.PageText;
    }

    private void OnViewProfile(object sender, RoutedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow row) { ShowSelectGuestWarning(); return; }
        new StayProfileWindow(row.StayId == Guid.Empty ? null : row.StayId)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();
    }
    private void OnMoreActions(object sender, RoutedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow row)
        {
            ShowSelectGuestWarning();
            return;
        }

        var menu = new ContextMenu
        {
            PlacementTarget = sender as UIElement,
            StaysOpen = false
        };

        void Add(string header, RoutedEventHandler handler)
        {
            var item = new MenuItem { Header = header };
            item.Click += handler;
            menu.Items.Add(item);
        }

        Add("Xem Folio", OnViewFolio);
        Add("Gia hạn lưu trú", OnExtendStay);
        Add("Đổi phòng", OnChangeRoom);
        Add("Housekeeping", OnHousekeeping);
        Add("Chuẩn bị Check-out", OnPrepCheckout);
        if (row.OccupantId != Guid.Empty)
            Add("Xóa khách đi cùng", OnRemoveGuest);
        menu.IsOpen = true;
    }
    private void OnExtendStay(object sender, RoutedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow row) { ShowSelectGuestWarning(); return; }
        if (row.StayId == Guid.Empty || row.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ Stay chưa có định danh/row version hợp lệ từ dữ liệu thật.", "Gia hạn lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new ExtendStayWindow(row.StayId)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();
    }
    private void OnChangeRoom(object sender, RoutedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow row) { ShowSelectGuestWarning(); return; }
        if (row.StayId == Guid.Empty || row.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ Stay chưa có định danh/row version hợp lệ từ dữ liệu thật.", "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new RoomMoveExtendWindow(row.StayId)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();
    }
    private async void OnAddGuest(object sender, RoutedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow row)
        {
            ShowSelectGuestWarning();
            return;
        }
        if (_stayService is null || row.StayId == Guid.Empty || row.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ Stay chưa có định danh/row version hợp lệ hoặc chưa đăng ký service dữ liệu thật.", "Thêm khách đi cùng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new AddStayOccupantWindow { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (dialog.ShowDialog() != true || !dialog.WasConfirmed)
            return;

        var result = await _stayService.AddOccupantAsync(
            new AddStayOccupantCommand(row.StayId, dialog.SelectedGuestId, dialog.SelectedOccupantType, row.RowVersion),
            CancellationToken.None);
        if (result.IsFailure)
        {
            MessageBox.Show(result.Error?.Message ?? "Không thể thêm khách đi cùng vào Stay thật.", "Thêm khách đi cùng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadPersistedAsync();
        FooterTotal.Text = $"Đã thêm {result.Value?.FullName ?? "khách đi cùng"} vào Stay thật.";
    }
    private async void OnRemoveGuest(object sender, RoutedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow row || row.OccupantId == Guid.Empty)
        {
            ShowSelectGuestWarning();
            return;
        }
        if (_stayService is null || row.StayId == Guid.Empty || row.RowVersion is not { Length: 8 } || row.OccupantRowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ khách đi cùng chưa có đủ định danh/row version từ dữ liệu thật.", "Xóa khách đi cùng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Xóa {row.FullName} khỏi danh sách khách đang lưu trú? Dữ liệu sẽ được lưu lịch sử, không bị xóa vật lý.",
            "Xóa khách đi cùng",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        var result = await _stayService.RemoveOccupantAsync(
            new RemoveStayOccupantCommand(row.StayId, row.OccupantId, row.RowVersion, row.OccupantRowVersion),
            CancellationToken.None);
        if (result.IsFailure)
        {
            MessageBox.Show(result.Error?.Message ?? "Không thể xóa khách đi cùng khỏi Stay thật.", "Xóa khách đi cùng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadPersistedAsync();
        FooterTotal.Text = $"Đã xóa {row.FullName} khỏi danh sách khách đang lưu trú.";
    }
    private void OnHousekeeping(object sender, RoutedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow row)
        {
            ShowSelectGuestWarning();
            return;
        }

        // Filter the persisted Housekeeping board to the selected room; do not create an in-memory request here.
        new HousekeepingMessageWindow(row.RoomNumber) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
    }
    private void OnViewFolio(object sender, RoutedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow r)
        {
            ShowSelectGuestWarning();
            return;
        }

        if (r.StayId == Guid.Empty)
        {
            MessageBox.Show("Hồ sơ Stay chưa có định danh hợp lệ từ dữ liệu thật.", "Xem Folio", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new FolioWorkspaceWindow(r.StayId).ShowDialog();
    }
    private void OnPrepCheckout(object sender, RoutedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not GuestRow r)
        {
            ShowSelectGuestWarning();
            return;
        }

        if (r.StayId == Guid.Empty || r.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ Stay chưa có định danh/row version hợp lệ từ dữ liệu thật.", "Chuẩn bị Check-out", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new CheckoutPreparationWindow(r.StayId).ShowDialog();
    }

    private void ShowExtendStayPopup()
    {
        if (GuestGrid.SelectedItem is not GuestRow r) { ShowSelectGuestWarning(); return; }
        if (r.StayId == Guid.Empty || r.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ Stay chưa có định danh/row version hợp lệ từ dữ liệu thật.", "Gia hạn lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new ExtendStayWindow(r.StayId) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
    }

    private void ShowChangeRoomPopup()
    {
        if (GuestGrid.SelectedItem is not GuestRow r) { ShowSelectGuestWarning(); return; }
        if (r.StayId == Guid.Empty || r.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ Stay chưa có định danh/row version hợp lệ từ dữ liệu thật.", "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new RoomMoveExtendWindow(r.StayId) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
    }

    private void ShowUnavailable(string action)
    {
        if (GuestGrid.SelectedItem is not GuestRow r) { ShowSelectGuestWarning(); return; }
        MessageBox.Show($"{action} cho khách {r.FullName} (Phòng {r.RoomNumber}) chưa có nghiệp vụ backend thật. Không có dữ liệu nào được tạo.", "Khách hiện tại", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ShowSelectGuestWarning() => MessageBox.Show("Vui lòng chọn một khách trước.", "Khách hiện tại", MessageBoxButton.OK, MessageBoxImage.Warning);
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

internal record GuestRow(
    string StatusText, string StatusBadgeBrush, string StatusColorBrush,
    string RoomNumber, string FullName, string Role,
    string Arrival, string Departure, string Nights,
    string RoomType, string Phone, string FolioId,
    string Alert, string AlertColor, Guid StayId = default, byte[]? RowVersion = null,
    Guid OccupantId = default, byte[]? OccupantRowVersion = null, decimal? FolioBalance = null,
    Guid? GuestProfileId = null)
{
    public string FolioStatus => FolioBalance is null
        ? "Chưa tải"
        : FolioBalance > 0 ? "Còn phải thu" : "Bình thường";
}
