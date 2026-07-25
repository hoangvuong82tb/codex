using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class TodayArrivalsWindow : Window
{
    private List<ArrivalRow> _allArrivals;
    private int _currentPage;
    private const int PageSize = 20;

    public TodayArrivalsWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _allArrivals = LoadMockData();
        _currentPage = 0;
        ShowTab(0);
        UpdateMetrics();
        ApplyFilters();
    }

    private static List<ArrivalRow> LoadMockData()
    {
        return new List<ArrivalRow>
        {
            new("08:30","Nguyễn Văn An","CFM-24301","OTA","Deluxe","201","3","2","Đã xác nhận","#DBEAFE","#2563EB","★",""),
            new("09:00","Trần Thị Bình","CFM-24302","OTA","Deluxe","202","4","1","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("10:00","Lê Hoàng Cường","CFM-24303","Corporate","Superior","301","2","1","Đã Check-in","#DCFCE7","#16A34A","★","Late check-in"),
            new("10:30","Phạm Minh Đức","CFM-24304","Travel Agent","Superior","302","3","2","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("11:00","Hoàng Thị Em","CFM-24305","FIT","Junior","401","2","1","Đã Check-in","#DCFCE7","#16A34A","",""),
            new("11:15","Vũ Quang Phúc","CFM-24306","Group","Standard","105","5","2","Đã xác nhận","#DBEAFE","#2563EB","","Đoàn 15p"),
            new("11:45","Đặng Kim Giang","CFM-24307","OTA","Suite","502","3","1","Đã Check-in","#DCFCE7","#16A34A","★",""),
            new("12:00","Bùi Thanh Hà","CFM-24308","OTA","Deluxe","203","2","2","Chờ","#FFF7ED","#EA580C","","Chưa assign"),
            new("12:15","Ngô Đức Inh","CFM-24309","Corporate","Junior","402","4","1","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("12:30","Đỗ Lan Khánh","CFM-24310","Walk-in","Standard","108","1","1","Đã Check-in","#DCFCE7","#16A34A","","Walk-in"),
            new("13:00","Lý Văn Long","CFM-24311","OTA","Suite","501","6","2","Đã xác nhận","#DBEAFE","#2563EB","★",""),
            new("13:15","Mai Thị Mỹ","CFM-24312","FIT","Superior","305","5","1","Đã Check-in","#DCFCE7","#16A34A","",""),
            new("13:30","Trịnh Hoàng Nam","CFM-24313","OTA","Junior","403","3","2","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("13:45","Hồ Quang Ổn","CFM-24314","Travel Agent","Deluxe","204","4","1","Chờ","#FFF7ED","#EA580C","","Thiếu ĐC"),
            new("14:00","Chu Văn Rạng","CFM-24315","Corporate","Suite","503","5","1","Guarantee","#DBEAFE","#2563EB","",""),
            new("14:00","Hà Thị Sen","CFM-24316","OTA","Standard","118","3","2","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("14:15","Vi Văn Tâm","CFM-24317","Walk-in","Junior","404","2","1","Đã Check-in","#DCFCE7","#16A34A","",""),
            new("14:30","Tô Văn Phú","CFM-24318","FIT","Deluxe","205","2","1","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("14:45","Phan Thị Quỳnh","CFM-24319","OTA","Junior","405","7","2","Guarantee","#DBEAFE","#2563EB","",""),
            new("15:00","Giàng Thị Út","CFM-24320","Group","Standard","106","4","2","Đã xác nhận","#DBEAFE","#2563EB","","Đoàn 20p"),
            new("15:00","Bùi Thanh Hải","CFM-24321","Corporate","Superior","303","3","1","Đã Check-in","#DCFCE7","#16A34A","",""),
            new("15:15","Ngô Quang Hiếu","CFM-24322","OTA","Standard","119","2","1","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("15:30","Lê Thị Kim","CFM-24323","Travel Agent","Deluxe","206","4","2","Đã Check-in","#DCFCE7","#16A34A","★",""),
            new("15:45","Trần Hoàng Long","CFM-24324","FIT","Superior","306","3","1","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("16:00","Vũ Minh Tuấn","CFM-24325","OTA","Deluxe","207","2","1","Chờ","#FFF7ED","#EA580C","","Phòng dơ"),
            new("16:15","Đỗ Quang Khải","CFM-24326","Corporate","Junior","406","3","2","Guarantee","#DBEAFE","#2563EB","",""),
            new("16:30","Ngô Thị Lan","CFM-24327","OTA","Standard","120","2","1","Đã Check-in","#DCFCE7","#16A34A","",""),
            new("17:00","Hoàng Văn Nam","CFM-24328","Travel Agent","Superior","307","4","1","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("17:15","Phan Văn Hùng","CFM-24329","FIT","Deluxe","208","1","2","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("17:30","Lê Thanh Sơn","CFM-24330","Group","Suite","504","5","3","Guarantee","#DBEAFE","#2563EB","",""),
            new("18:00","Trịnh Thị Mai","CFM-24331","OTA","Junior","407","3","1","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("18:30","Nguyễn Đức Thắng","CFM-24332","Corporate","Superior","308","2","1","Đã Check-in","#DCFCE7","#16A34A","",""),
            new("19:00","Bùi Quốc Anh","CFM-24333","OTA","Deluxe","209","3","2","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("19:30","Hồ Thị Ngọc","CFM-24334","Walk-in","Standard","121","1","1","Chờ","#FFF7ED","#EA580C","",""),
            new("20:00","Đặng Minh Hoàng","CFM-24335","Travel Agent","Suite","505","4","1","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("20:30","Võ Thanh Phương","CFM-24336","OTA","Superior","309","2","1","Đã Check-in","#DCFCE7","#16A34A","",""),
            new("21:00","Trương Văn Bình","CFM-24337","FIT","Junior","408","3","2","Đã xác nhận","#DBEAFE","#2563EB","",""),
            new("21:30","Phạm Ngọc Ánh","CFM-24338","OTA","Deluxe","210","2","1","Guarantee","#DBEAFE","#2563EB","","Early tmr"),
            new("22:00","Lý Hoàng Phúc","CFM-24339","Corporate","Standard","122","1","1","Đã xác nhận","#DBEAFE","#2563EB","","Late 22h"),
            new("23:00","Mai Quốc Đạt","CFM-24340","OTA","Suite","506","3","1","Chờ","#FFF7ED","#EA580C","","Late arrival"),
        };
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
        _currentPage = 0;
        ApplyPage(list);
        UpdateMetrics(list);
    }

    private static string GetEtaSlot(string eta)
    {
        if (int.TryParse(eta[..2], out var h))
            return h switch { >= 6 and < 12 => "Sáng (6-12h)", >= 12 and < 18 => "Chiều (12-18h)", >= 18 and < 24 => "Tối (18-24h)", _ => "Late (0-6h)" };
        return "—";
    }

    private void ApplyPage(List<ArrivalRow> list)
    {
        var paged = list.Skip(_currentPage * PageSize).Take(PageSize).ToList();
        ArrivalGrid.ItemsSource = paged;
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
        DetailRate.Text = r.Source == "OTA" ? "1.2M" : r.Source == "Corporate" ? "950K" : r.RoomType == "Suite" ? "1.8M" : "1.0M";

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
            Row("Company", r.Source == "Corporate" ? "Công ty ABC" : "—"),
            Row("Travel Agent", r.Source == "Travel Agent" ? "TA Việt Nam" : "—"),
            Row("Guarantee", r.StatusText == "Guarantee" ? "Credit Card" : "Không"),
            Row("Ngày tạo", "25/07/2026"), Row("Tạo bởi", "Reservation")));
        TabContent.Children.Add(Section("Lưu trú",
            Row("Ngày đến", "27/07/2026"), Row("Ngày đi", $"{int.Parse(r.Nights) + 27:00}/07/2026"),
            Row("Số đêm", r.Nights), Row("Số khách", r.GuestCount),
            Row("Ghi chú", string.IsNullOrEmpty(r.Remark) ? "—" : r.Remark)));
    }

    private void ShowGuest(ArrivalRow r)
    {
        TabContent.Children.Add(Section("Thông tin khách",
            Row("Họ tên", r.GuestName), Row("Điện thoại", $"09{r.BookingRef[4..]}"),
            Row("Email", $"{r.GuestName.Split(' ').LastOrDefault()?.ToLower()}@email.com"),
            Row("Quốc tịch", "Việt Nam"), Row("Loại giấy tờ", "CCCD"),
            Row("Số giấy tờ", $"079{r.BookingRef[4..]}"), Row("Ngày sinh", "01/01/1990"),
            Row("VIP Level", r.Vip == "★" ? "Gold" : "Standard"),
            Row("Membership", r.Vip == "★" ? "M-0042" : "—"),
            Row("Special Request", r.Vip == "★" ? "Phòng cao tầng, welcome fruit" : "Không")));
    }

    private void ShowRoom(ArrivalRow r)
    {
        TabContent.Children.Add(Section("Phòng",
            Row("Loại phòng", r.RoomType), Row("Phòng assigned", r.RoomNumber),
            Row("Tầng", r.RoomNumber[..1]), Row("Trạng thái phòng",
                r.Remark.Contains("dơ") ? "Dirty" : r.Remark.Contains("assign") ? "Chưa assign" : "Ready"),
            Row("HSK Note", r.Remark.Contains("dơ") ? "Cần dọn gấp" : "—"),
            Row("ETA Ready", r.Remark.Contains("dơ") ? "Cần kiểm tra" : "Sẵn sàng")));
    }

    private void ShowDocs(ArrivalRow r)
    {
        TabContent.Children.Add(Section("Giấy tờ",
            Row("Trạng thái", r.StatusText == "Đã Check-in" ? "Đã scan" : "Chưa scan"),
            Row("Online Check-in", r.StatusText == "Đã Check-in" ? "Đã hoàn thành" : "Chưa"),
            Row("OCR", "—"), Row("Thiếu giấy tờ", r.Vip != "★" ? "Không" : "CCCD cần cập nhật")));
        TabContent.Children.Add(new Button { Content = "Quét giấy tờ", Style = FindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6) });
    }

    private void ShowFinance(ArrivalRow r)
    {
        TabContent.Children.Add(Section("Tài chính",
            Row("Rate Code", r.Source == "OTA" ? "OTA-10" : r.Source == "Corporate" ? "CORP-15" : "RACK"),
            Row("Room Rate", $"{DetailRate.Text}/đêm"),
            Row("Deposit", r.StatusText == "Guarantee" ? "1.200.000" : "0"),
            Row("Balance", "0"), Row("Currency", "VND"),
            Row("Payment", r.StatusText == "Guarantee" ? "Credit Card" : "Tiền mặt"),
            Row("VAT", "Đã bao gồm")));
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
    private void OnRefresh(object sender, RoutedEventArgs e) => ApplyFilters();
    private void OnPrevPage(object sender, RoutedEventArgs e) { if (_currentPage > 0) { _currentPage--; ApplyFilters(); } }
    private void OnNextPage(object sender, RoutedEventArgs e) { _currentPage++; ApplyFilters(); }
    private void OnCheckIn(object sender, RoutedEventArgs e) => ShowMockMsg("Check-in");
    private void OnChangeRoom(object sender, RoutedEventArgs e) => ShowMockMsg("Đổi phòng");
    private void OnViewBooking(object sender, RoutedEventArgs e) => ShowMockMsg("Xem Booking");
    private void OnPrintReg(object sender, RoutedEventArgs e) => ShowMockMsg("In Registration Card");
    private void OnSendHsk(object sender, RoutedEventArgs e) => ShowMockMsg("Gửi Housekeeping");
    private void OnSendBell(object sender, RoutedEventArgs e) => ShowMockMsg("Gửi Bellman");

    private void ShowMockMsg(string action)
    {
        if (ArrivalGrid.SelectedItem is ArrivalRow r)
            MessageBox.Show($"Mockup: {action} cho {r.GuestName} (Booking {r.BookingRef}). Chưa kết nối backend.", "Khách đến hôm nay", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show("Vui lòng chọn một booking trước.", "Khách đến hôm nay", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

internal record ArrivalRow(
    string ETA, string GuestName, string BookingRef, string Source,
    string RoomType, string RoomNumber, string Nights, string GuestCount,
    string StatusText, string StatusBadgeBrush, string StatusColorBrush,
    string Vip, string Remark);
