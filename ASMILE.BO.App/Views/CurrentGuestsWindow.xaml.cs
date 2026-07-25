using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class CurrentGuestsWindow : Window
{
    private List<GuestRow> _allGuests;
    private int _currentPage;
    private const int PageSize = 20;

    public CurrentGuestsWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        LoadMockData();
        _currentPage = 0;
        ShowTab(0);
        UpdateMetrics();
    }

    private void LoadMockData()
    {
        _allGuests = new List<GuestRow>
        {
            new("Đang lưu trú","#DCFCE7","#16A34A","201","Nguyễn Văn An","Khách chính","24/07","27/07","3","Deluxe","0912-345-678","FOL-2401","",""),
            new("Đang lưu trú","#DCFCE7","#16A34A","202","Trần Thị Bình","Khách chính","24/07","28/07","4","Deluxe","0903-456-789","FOL-2402","",""),
            new("Trả hôm nay","#DBEAFE","#2563EB","301","Lê Hoàng Cường","Khách chính","23/07","27/07","4","Superior","0987-654-321","FOL-2403","",""),
            new("Trả hôm nay","#DBEAFE","#2563EB","105","Vũ Quang Phúc","Khách chính","20/07","27/07","7","Standard","0977-888-999","FOL-2406","!","#DC2626"),
            new("Đang lưu trú","#DCFCE7","#16A34A","302","Phạm Minh Đức","Khách chính","25/07","28/07","3","Superior","0918-222-333","FOL-2404","",""),
            new("Đang lưu trú","#DCFCE7","#16A34A","401","Hoàng Thị Em","Khách chính","24/07","26/07","2","Junior","0905-111-444","FOL-2405","",""),
            new("Quá hạn","#FEE2E2","#DC2626","210","Bùi Thanh Hà","Khách chính","15/07","26/07","11","Deluxe","0908-123-456","FOL-2408","!!","#DC2626"),
            new("Quá hạn","#FEE2E2","#DC2626","108","Đỗ Lan Khánh","Khách chính","14/07","25/07","12","Standard","0944-567-321","FOL-2410","!","#EA580C"),
            new("Đang lưu trú","#DCFCE7","#16A34A","402","Ngô Đức Inh","Khách chính","21/07","29/07","8","Junior","0965-432-109","FOL-2409","",""),
            new("Trả hôm nay","#DBEAFE","#2563EB","501","Lý Văn Long","Khách chính","19/07","27/07","8","Suite","0916-789-012","FOL-2411","",""),
            new("Gia hạn","#FFF7ED","#EA580C","305","Mai Thị Mỹ","Khách chính","18/07","29/07","11","Superior","0909-345-678","FOL-2412","",""),
            new("Đang lưu trú","#DCFCE7","#16A34A","403","Trịnh Hoàng Nam","Khách chính","26/07","30/07","4","Junior","0988-234-567","FOL-2413","",""),
            new("Trả hôm nay","#DBEAFE","#2563EB","203","Hồ Quang Ổn","Khách chính","22/07","27/07","5","Deluxe","0922-456-789","FOL-2414","!","#EA580C"),
            new("Đang lưu trú","#DCFCE7","#16A34A","201","Lê Thị Hoa","Đi cùng","24/07","27/07","3","Deluxe","0911-222-333","FOL-2401","",""),
            new("Đang lưu trú","#DCFCE7","#16A34A","202","Trần Văn Nam","Đi cùng","24/07","28/07","4","Deluxe","0909-888-777","FOL-2402","",""),
            new("Trả hôm nay","#DBEAFE","#2563EB","301","Phan Thị Lan","Đi cùng","23/07","27/07","4","Superior","0912-345-999","FOL-2403","",""),
            new("Đang lưu trú","#DCFCE7","#16A34A","401","Nguyễn Văn Bình","Đi cùng","24/07","26/07","2","Junior","0900-111-222","FOL-2405","",""),
            new("Đang lưu trú","#DCFCE7","#16A34A","503","Chu Văn Rạng","Khách chính","25/07","31/07","6","Suite","0977-333-444","FOL-2417","!","#EA580C"),
            new("Đang lưu trú","#DCFCE7","#16A34A","118","Hà Thị Sen","Khách chính","24/07","27/07","3","Standard","0911-456-789","FOL-2418","",""),
            new("Đang lưu trú","#DCFCE7","#16A34A","404","Vi Văn Tâm","Khách chính","27/07","30/07","3","Junior","0904-789-321","FOL-2419","",""),
            new("Trả hôm nay","#DBEAFE","#2563EB","106","Giàng Thị Út","Khách chính","21/07","27/07","6","Standard","0981-234-567","FOL-2420","!","#EA580C"),
            new("Đang lưu trú","#DCFCE7","#16A34A","204","Tô Văn Phú","Khách chính","26/07","28/07","2","Deluxe","0933-789-012","FOL-2421","",""),
            new("Gia hạn","#FFF7ED","#EA580C","405","Phan Thị Quỳnh","Khách chính","20/07","30/07","10","Junior","0955-111-222","FOL-2422","",""),
            new("Đang lưu trú","#DCFCE7","#16A34A","502","Đặng Kim Giang","Khách chính","27/07","29/07","2","Suite","0934-567-890","FOL-2423","",""),
            new("Trả hôm nay","#DBEAFE","#2563EB","303","Bùi Thanh Hải","Khách chính","23/07","27/07","4","Superior","0922-333-444","FOL-2424","",""),
            new("Đang lưu trú","#DCFCE7","#16A34A","119","Ngô Quang Hiếu","Khách chính","25/07","28/07","3","Standard","0912-999-888","FOL-2425","",""),
            new("Trả hôm nay","#DBEAFE","#2563EB","205","Lê Thị Kim","Khách chính","22/07","27/07","5","Deluxe","0905-666-555","FOL-2426","!","#EA580C"),
            new("Đang lưu trú","#DCFCE7","#16A34A","306","Trần Hoàng Long","Khách chính","26/07","29/07","3","Superior","0909-444-333","FOL-2427","",""),
        };
        ApplyFilters();
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
        _currentPage = 0;
        ApplyPage(list);
        UpdateMetrics(list);
    }

    private void ApplyPage(List<GuestRow> list)
    {
        var paged = list.Skip(_currentPage * PageSize).Take(PageSize).ToList();
        GuestGrid.ItemsSource = paged;
        ResultCount.Text = $"{list.Count} khách";
        FooterTotal.Text = $"Tổng: {list.Count} khách · {list.Select(r => r.RoomNumber).Distinct().Count()} phòng";
        var totalPages = (list.Count + PageSize - 1) / PageSize;
        if (totalPages == 0) totalPages = 1;
        PageInfo.Text = $"Trang {_currentPage + 1}/{totalPages}";
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
        DetailBalance.Text = r.Alert == "!!" ? "5.8M" : r.Alert == "!" ? "2.4M" : "0 VND";

        ShowTab(int.TryParse((TabContent.Tag as string) ?? "0", out var t) ? t : 0);
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
            Row("Họ và tên", r.FullName), Row("Danh xưng", "Ông"),
            Row("Giới tính", r.FullName.Contains("Thị") ? "Nữ" : "Nam"), Row("Ngày sinh", "15/03/1985"),
            Row("Quốc tịch", "Việt Nam"), Row("Điện thoại", r.Phone),
            Row("Email", $"{r.FullName.Split(' ').LastOrDefault()?.ToLower()}@email.com"),
            Row("Địa chỉ", "123 Đường Lê Lợi, Quận 1, TP. Hồ Chí Minh")));
        TabContent.Children.Add(Section("Lưu trú hiện tại",
            Row("Mã hồ sơ", r.FolioId.Replace("FOL-","STY-")), Row("Mã xác nhận", $"CFM-{r.FolioId[4..]}"),
            Row("Số phòng", r.RoomNumber), Row("Hạng phòng", r.RoomType),
            Row("Ngày đến", r.Arrival), Row("Ngày đi", r.Departure),
            Row("Số đêm", r.Nights), Row("Nguồn", "Booking.com"),
            Row("Company/TA", "Công ty ABC")));
    }

    private void ShowStay(GuestRow r)
    {
        TabContent.Children.Add(Section("Chi tiết lưu trú",
            Row("Ngày đến", r.Arrival), Row("Giờ đến", "14:00"),
            Row("Ngày đi", r.Departure), Row("Giờ đi", "12:00"),
            Row("Số đêm", r.Nights), Row("Book Status", "Confirmed"),
            Row("Meal Code", "BB"), Row("BF", "2 suất"),
            Row("Connecting Room", "Không"), Row("Complimentary", "Không"),
            Row("Giường thêm", "0"), Row("Ghi chú", "Khách yêu cầu phòng yên tĩnh"),
            Row("Yêu cầu đặc biệt", "Gối memory foam")));
        TabContent.Children.Add(new Button { Content = "Gia hạn lưu trú", Style = FindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6), Tag = "action" });
        TabContent.Children.Add(new Button { Content = "Đổi phòng", Style = FindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6), Tag = "action" });
    }

    private void ShowDocuments(GuestRow r)
    {
        TabContent.Children.Add(Section("Giấy tờ",
            Row("Loại giấy tờ", "CCCD"), Row("Số giấy tờ", $"0792020{new System.Random().Next(10000,99999)}"),
            Row("Ngày cấp", "15/03/2020"), Row("Nơi cấp", "Cục CSQLHC"),
            Row("Ngày hết hạn", "15/03/2035"), Row("Quốc gia cấp", "Việt Nam"),
            Row("Quê quán", "TP. Hồ Chí Minh"), Row("Thường trú", "Quận 1, TP. HCM"),
            Row("Trạng thái xác minh", "Đã xác minh")));
        TabContent.Children.Add(new Button { Content = "Cập nhật giấy tờ", Style = FindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6), Tag = "action" });
    }

    private void ShowRoomService(GuestRow r)
    {
        TabContent.Children.Add(Section("Thông tin phòng",
            Row("Số phòng", r.RoomNumber), Row("Hạng phòng", r.RoomType),
            Row("Tầng", r.RoomNumber[..1]), Row("Tình trạng phòng", "Đang có khách"),
            Row("Nhà buồng", "Ca sáng"), Row("Ngày dọn gần nhất", "26/07"),
            Row("Số khách", _allGuests.Count(g => g.RoomNumber == r.RoomNumber).ToString()), Row("Sức chứa", "2")));
        TabContent.Children.Add(Section("Dịch vụ",
            Row("Meal Code", "BB"), Row("Ăn sáng", "Bao gồm"),
            Row("Giường thêm", "0"), Row("Minibar", "Chưa sử dụng"),
            Row("Dịch vụ đã sử dụng", "Giặt ủi (25/07)"), Row("Yêu cầu nhà buồng", "Không")));
    }

    private void ShowFolio(GuestRow r)
    {
        TabContent.Children.Add(Section("Tài chính",
            Row("Mã Folio", r.FolioId), Row("Người thanh toán", r.FullName),
            Row("Tiền phòng", "3.600.000"), Row("Dịch vụ", "250.000"),
            Row("Thuế/phí", "385.000"), Row("Đặt cọc", "1.000.000"),
            Row("Đã thanh toán", r.Alert == "!!" ? "0" : "1.000.000"),
            Row("Còn phải thu", r.Alert == "!!" ? "5.835.000" : r.Alert == "!" ? "2.435.000" : "0"),
            Row("Hạn mức tín dụng", "10.000.000"), Row("Trạng thái Folio", r.Alert == "!!" ? "Vượt hạn mức" : r.Alert == "!" ? "Còn phải thu" : "Bình thường")));
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

    private void OnRefresh(object sender, RoutedEventArgs e) => ApplyFilters();
    private void OnPrevPage(object sender, RoutedEventArgs e) { if (_currentPage > 0) { _currentPage--; ApplyFilters(); } }
    private void OnNextPage(object sender, RoutedEventArgs e) { _currentPage++; ApplyFilters(); }

    private void OnViewProfile(object sender, RoutedEventArgs e) { ShowMockMsg("Xem hồ sơ"); }
    private void OnMoreActions(object sender, RoutedEventArgs e) { ShowMockMsg("Thao tác khác"); }
    private void OnExtendStay(object sender, RoutedEventArgs e) { ShowExtendStayPopup(); }
    private void OnChangeRoom(object sender, RoutedEventArgs e) { ShowChangeRoomPopup(); }
    private void OnAddGuest(object sender, RoutedEventArgs e) { ShowMockMsg("Thêm khách"); }
    private void OnHousekeeping(object sender, RoutedEventArgs e) { ShowMockMsg("Nhắn nhà buồng"); }
    private void OnViewFolio(object sender, RoutedEventArgs e) { ShowMockMsg("Xem Folio"); }
    private void OnPrepCheckout(object sender, RoutedEventArgs e) { ShowMockMsg("Chuẩn bị Check-out"); }

    private void ShowExtendStayPopup()
    {
        if (GuestGrid.SelectedItem is not GuestRow r) { ShowSelectGuestWarning(); return; }
        var msg = $"═══ Gia hạn lưu trú ═══\n\n" +
                  $"Khách: {r.FullName}\nPhòng: {r.RoomNumber}\n" +
                  $"Ngày đi hiện tại: {r.Departure}\n" +
                  $"Ngày đi mới: (chọn ngày)\n" +
                  $"Số đêm bổ sung: —\n" +
                  $"Tình trạng phòng: Khả dụng\n\n" +
                  $"Nút: [Kiểm tra khả dụng] [Xác nhận mockup] [Hủy]";
        MessageBox.Show(msg, "Gia hạn lưu trú", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowChangeRoomPopup()
    {
        if (GuestGrid.SelectedItem is not GuestRow r) { ShowSelectGuestWarning(); return; }
        var msg = $"═══ Đổi phòng ═══\n\n" +
                  $"Khách: {r.FullName}\nPhòng hiện tại: {r.RoomNumber} ({r.RoomType})\n" +
                  $"Hạng phòng mới: {r.RoomType}\n" +
                  $"Phòng khả dụng mockup: 206, 304, 407\n" +
                  $"Lý do chuyển: (chọn)\n\n" +
                  $"Nút: [Áp dụng mockup] [Hủy]";
        MessageBox.Show(msg, "Đổi phòng", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowMockMsg(string action)
    {
        if (GuestGrid.SelectedItem is not GuestRow r) { ShowSelectGuestWarning(); return; }
        MessageBox.Show($"Mockup: {action} cho khách {r.FullName} (Phòng {r.RoomNumber}). Chưa kết nối backend.", "Khách hiện tại", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowSelectGuestWarning() => MessageBox.Show("Vui lòng chọn một khách trước.", "Khách hiện tại", MessageBoxButton.OK, MessageBoxImage.Warning);
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

internal record GuestRow(
    string StatusText, string StatusBadgeBrush, string StatusColorBrush,
    string RoomNumber, string FullName, string Role,
    string Arrival, string Departure, string Nights,
    string RoomType, string Phone, string FolioId,
    string Alert, string AlertColor)
{
    public string FolioStatus => string.IsNullOrEmpty(Alert) ? "Bình thường" : Alert == "!!" ? "Vượt hạn mức" : "Còn phải thu";
}
