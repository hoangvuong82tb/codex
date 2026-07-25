using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class TodayDeparturesWindow : Window
{
    private List<DepartureRow> _allDepartures;
    private int _currentPage;
    private const int PageSize = 20;

    public TodayDeparturesWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        BusinessDatePicker.SelectedDate = System.DateTime.Today;
        _allDepartures = LoadMockData();
        _currentPage = 0;
        ShowTab(0);
        UpdateMetrics();
        ApplyFilters();
    }

    private static List<DepartureRow> LoadMockData() => new()
    {
        new("Dự kiến đi","#DBEAFE","#2563EB","Đã cân","#DCFCE7","#16A34A","201","Nguyễn Văn An","24/07","12:00","3","Deluxe","0","★","⚡"),
        new("Đã Check-out","#DCFCE7","#16A34A","Đã cân","#DCFCE7","#16A34A","202","Trần Thị Bình","24/07","10:00","4","Deluxe","0","",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Còn phải thu","#FEF9C3","#A16207","301","Lê Hoàng Cường","23/07","12:00","4","Superior","1.2M","","⚡"),
        new("Dự kiến đi","#DBEAFE","#2563EB","Vượt hạn mức","#FEE2E2","#DC2626","105","Vũ Quang Phúc","20/07","14:00","7","Standard","3.6M","","⚡⚡"),
        new("Trả muộn","#FEE2E2","#DC2626","Còn phải thu","#FEF9C3","#A16207","210","Bùi Thanh Hà","15/07","18:00","11","Deluxe","5.8M","★","⚡⚡"),
        new("Đã Check-out","#DCFCE7","#16A34A","Có tiền dư","#F3E8FF","#7C3AED","108","Đỗ Lan Khánh","14/07","09:00","12","Standard","-500K","",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Đã cân","#DCFCE7","#16A34A","302","Phạm Minh Đức","25/07","12:00","3","Superior","0","",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Chưa kiểm tra","#FFF7ED","#EA580C","401","Hoàng Thị Em","24/07","15:00","2","Junior","0","","⚡"),
        new("Đã Check-out","#DCFCE7","#16A34A","Đã cân","#DCFCE7","#16A34A","501","Lý Văn Long","19/07","08:00","8","Suite","0","★",""),
        new("Trả muộn","#FEE2E2","#DC2626","Còn phải thu","#FEF9C3","#A16207","305","Mai Thị Mỹ","18/07","20:00","9","Superior","4.2M","","⚡⚡"),
        new("Dự kiến đi","#DBEAFE","#2563EB","Đã cân","#DCFCE7","#16A34A","403","Trịnh Hoàng Nam","26/07","12:00","4","Junior","0","",""),
        new("Gia hạn","#FFF7ED","#EA580C","Đã cân","#DCFCE7","#16A34A","203","Hồ Quang Ổn","22/07","12:00","8","Deluxe","0","",""),
        new("Đã Check-out","#DCFCE7","#16A34A","Đã cân","#DCFCE7","#16A34A","402","Ngô Đức Inh","21/07","11:00","8","Junior","0","",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Còn phải thu","#FEF9C3","#A16207","118","Hà Thị Sen","24/07","12:00","3","Standard","800K","","⚡"),
        new("Dự kiến đi","#DBEAFE","#2563EB","Chờ Cty TT","#DBEAFE","#2563EB","503","Chu Văn Rạng","25/07","12:00","6","Suite","2.4M","★","⚡"),
        new("Trả muộn","#FEE2E2","#DC2626","Vượt hạn mức","#FEE2E2","#DC2626","404","Vi Văn Tâm","12/07","22:00","16","Junior","8.1M","","⚡⚡⚡"),
        new("Đã Check-out","#DCFCE7","#16A34A","Đã cân","#DCFCE7","#16A34A","106","Giàng Thị Út","21/07","07:00","6","Standard","0","",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Đã cân","#DCFCE7","#16A34A","204","Tô Văn Phú","26/07","14:00","2","Deluxe","0","",""),
        new("Đã Check-out","#DCFCE7","#16A34A","Có tiền dư","#F3E8FF","#7C3AED","405","Phan Thị Quỳnh","20/07","10:00","10","Junior","-1.2M","★",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Đã cân","#DCFCE7","#16A34A","502","Đặng Kim Giang","27/07","12:00","2","Suite","0","",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Chưa kiểm tra","#FFF7ED","#EA580C","303","Bùi Thanh Hải","23/07","12:00","4","Superior","0","","⚡"),
        new("Đã Check-out","#DCFCE7","#16A34A","Đã cân","#DCFCE7","#16A34A","119","Ngô Quang Hiếu","25/07","08:00","3","Standard","0","",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Còn phải thu","#FEF9C3","#A16207","205","Lê Thị Kim","22/07","12:00","5","Deluxe","1.8M","","⚡"),
        new("Dự kiến đi","#DBEAFE","#2563EB","Đã cân","#DCFCE7","#16A34A","306","Trần Hoàng Long","26/07","12:00","3","Superior","0","",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Còn phải thu","#FEF9C3","#A16207","120","Đỗ Quang Khải","26/07","15:00","2","Standard","600K","","⚡"),
        new("Đã Check-out","#DCFCE7","#16A34A","Đã cân","#DCFCE7","#16A34A","307","Phan Văn Hùng","25/07","09:00","4","Superior","0","",""),
        new("Dự kiến đi","#DBEAFE","#2563EB","Vượt hạn mức","#FEE2E2","#DC2626","504","Lê Thanh Sơn","21/07","12:00","6","Suite","4.5M","","⚡⚡"),
        new("Dự kiến đi","#DBEAFE","#2563EB","Đã cân","#DCFCE7","#16A34A","407","Trịnh Thị Mai","26/07","12:00","3","Junior","0","",""),
        new("Đã Check-out","#DCFCE7","#16A34A","Đã cân","#DCFCE7","#16A34A","308","Nguyễn Đức Thắng","24/07","10:00","2","Superior","0","",""),
        new("Trả muộn","#FEE2E2","#DC2626","Còn phải thu","#FEF9C3","#A16207","209","Bùi Quốc Anh","20/07","21:00","7","Deluxe","2.1M","★","⚡⚡"),
    };

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

        var list = f.ToList(); _currentPage = 0; ApplyPage(list); UpdateMetrics(list);
    }

    private void ApplyPage(List<DepartureRow> list)
    {
        var paged = list.Skip(_currentPage * PageSize).Take(PageSize).ToList();
        DepartureGrid.ItemsSource = paged;
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
        DetailNights.Text = r.Nights; DetailGuests.Text = r.Vip == "★" ? "2" : "1";
        DetailBalance.Text = r.BalanceDue == "0" ? "0 VND" : r.BalanceDue + " VND";
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
        TabContent.Children.Add(S("Khách", R("Họ tên", r.FullName), R("Điện thoại", $"09{r.GetHashCode() % 90000000 + 10000000}"), R("Email", $"{r.FullName.Split(' ').LastOrDefault()?.ToLower()}@email.com"), R("Quốc tịch", "Việt Nam"), R("Số giấy tờ", "CCCD 079202012345"), R("VIP", r.Vip == "★" ? "Gold" : "—")));
        TabContent.Children.Add(S("Lưu trú", R("Mã HS", $"STY-{24000 + _allDepartures.IndexOf(r)}"), R("Phòng", r.RoomNumber), R("Hạng", r.RoomType), R("Ngày đến", r.Arrival), R("Ngày đi", "27/07"), R("Giờ đi", r.Etd), R("Đêm", r.Nights), R("Nguồn", "Booking.com"), R("Company", "—")));
        TabContent.Children.Add(S("Kế hoạch rời", R("Giờ rời", r.Etd), R("Di chuyển", r.Vip == "★" ? "Xe đưa đón" : "Tự túc"), R("Hành lý", r.Vip == "★" ? "2 kiện" : "1 kiện"), R("Gửi HL sau C/O", "Không"), R("Ghi chú", r.Alerts.Contains("⚡") ? "Cần hỗ trợ hành lý" : "—")));
    }

    private void ShowFolio(DepartureRow r)
    {
        TabContent.Children.Add(S("Folio", R("Mã Folio", $"FOL-{24000 + _allDepartures.IndexOf(r)}"), R("Trạng thái", r.FolioStatus), R("Người TT", r.FullName), R("Tiền phòng", $"{int.Parse(r.Nights) * 1200000:N0}"), R("Minibar", "150.000"), R("Dịch vụ", "200.000"), R("Thuế/phí", "185.000"), R("Đặt cọc", "1.000.000"), R("Đã TT", r.BalanceDue == "0" ? "Đủ" : "1.000.000"), R("Còn phải thu", r.BalanceDue == "0" ? "0" : r.BalanceDue), R("Tiền tệ", "VND"), R("Phương thức", "Tiền mặt / Thẻ")));
        TabContent.Children.Add(new Button { Content = "Xem Folio", Style = FindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6) });
    }

    private void ShowServices(DepartureRow r)
    {
        TabContent.Children.Add(S("Dịch vụ cần chốt", R("Tiền phòng", $"{int.Parse(r.Nights) * 1200000:N0} VND"), R("Minibar", r.Alerts.Contains("⚡") ? "Chưa chốt" : "Đã chốt"), R("Nhà hàng", "Đã chốt"), R("Giặt là", r.Vip == "★" ? "350.000 — Chưa chốt" : "—"), R("Spa", "—"), R("Điện thoại", "—"), R("Vận chuyển", r.Vip == "★" ? "Đã chốt" : "—")));
        TabContent.Children.Add(new Button { Content = "Yêu cầu chốt Minibar", Style = FindResource("OutlineBtn") as Style, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 6) });
    }

    private void ShowHandover(DepartureRow r)
    {
        var items = new[] { ("Trả chìa khóa/thẻ", "✓", "Lễ tân", "10:30"), ("Kiểm tra Minibar", r.Alerts.Contains("⚡") ? "○" : "✓", "Buồng phòng", "—"), ("Kiểm tra tài sản", "○", "Buồng phòng", "—"), ("Nhận hành lý", r.Vip == "★" ? "○" : "✓", "Bellman", "11:00"), ("Trả tài sản gửi", "—", "—", "—"), ("Thông báo buồng phòng", "○", "Lễ tân", "—"), ("Xác nhận hóa đơn", r.BalanceDue == "0" ? "○" : "✗", "Thu ngân", "—"), ("Lấy ý kiến khách", "○", "Lễ tân", "—") };
        var sp = new StackPanel();
        foreach (var (label, status, who, when) in items)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) }; row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")) });
            var sc = status == "✓" ? "#16A34A" : status == "✗" ? "#DC2626" : "#94A3B8";
            row.Children.Add(new TextBlock { Text = status, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(sc)), HorizontalAlignment = HorizontalAlignment.Center }); Grid.SetColumn(row.Children[^1], 1);
            row.Children.Add(new TextBlock { Text = who, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")) }); Grid.SetColumn(row.Children[^1], 2);
            row.Children.Add(new TextBlock { Text = when, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) }); Grid.SetColumn(row.Children[^1], 3);
            sp.Children.Add(row);
        }
        TabContent.Children.Add(S("Bàn giao phòng", sp));
    }

    private void ShowRequests(DepartureRow r)
    {
        var reqs = new List<string>();
        if (r.StatusText == "Trả muộn") reqs.Add("Late Check-out");
        if (r.Vip == "★") { reqs.Add("Xe đưa đón sân bay"); reqs.Add("Gửi hành lý sau C/O"); }
        if (r.Alerts.Length > 1) reqs.Add("In hóa đơn VAT");
        if (r.Vip == "★") reqs.Add("Hóa đơn công ty");
        if (r.Vip != "★") reqs.Add("Đánh thức 6:00");
        reqs.Add("Ghi chú: Khách hài lòng" + (r.Alerts.Contains("⚡") ? ", cần hỗ trợ vali" : ""));
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
    private void OnRefresh(object s, RoutedEventArgs e) => ApplyFilters();
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_currentPage > 0) { _currentPage--; ApplyFilters(); } }
    private void OnNextPage(object s, RoutedEventArgs e) { _currentPage++; ApplyFilters(); }
    private void OnPrepCheckout(object s, RoutedEventArgs e) { if (DepartureGrid.SelectedItem is DepartureRow r) MessageBox.Show($"═══ Chuẩn bị Check-out ═══\n\nKhách: {r.FullName}\nPhòng: {r.RoomNumber}\nNgày đi: 27/07\nGiờ đi: {r.Etd}\n\nTài chính: {(r.BalanceDue == "0" ? "Đã cân" : "Còn " + r.BalanceDue + " VND")}\nDịch vụ: {(r.Alerts.Contains("⚡") ? "Minibar chưa chốt" : "Đã chốt")}\n\nNút: [Hủy] [Xem Folio] [Xác nhận chuẩn bị]", "Chuẩn bị Check-out", MessageBoxButton.OK, MessageBoxImage.Information); else MessageBox.Show("Vui lòng chọn một khách.", "Khách đi hôm nay", MessageBoxButton.OK, MessageBoxImage.Warning); }
    private void OnExtendStay(object s, RoutedEventArgs e) { if (DepartureGrid.SelectedItem is DepartureRow r) MessageBox.Show($"═══ Gia hạn lưu trú ═══\n\nKhách: {r.FullName}\nPhòng: {r.RoomNumber}\nNgày đi hiện tại: 27/07\nNgày đi mới: (chọn)\nSố đêm thêm: —\n\nNút: [Kiểm tra] [Xác nhận mockup] [Hủy]", "Gia hạn lưu trú", MessageBoxButton.OK, MessageBoxImage.Information); else MessageBox.Show("Vui lòng chọn một khách.", "Khách đi hôm nay", MessageBoxButton.OK, MessageBoxImage.Warning); }
    private void OnViewFolio(object s, RoutedEventArgs e) => Msg("Xem Folio");
    private void OnSendHsk(object s, RoutedEventArgs e) => Msg("Gửi nhà buồng");
    private void OnPrintFolio(object s, RoutedEventArgs e) => Msg("In tạm tính");
    private void OnViewProfile(object s, RoutedEventArgs e) => Msg("Xem hồ sơ");
    private void Msg(string a) { if (DepartureGrid.SelectedItem is DepartureRow r) MessageBox.Show($"Mockup: {a} cho {r.FullName}.", "Khách đi hôm nay", MessageBoxButton.OK, MessageBoxImage.Information); else MessageBox.Show("Vui lòng chọn khách.", "Khách đi hôm nay", MessageBoxButton.OK, MessageBoxImage.Warning); }
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record DepartureRow(string StatusText, string StatusBadgeBrush, string StatusColorBrush, string FolioStatus, string FolioBadgeBrush, string FolioColorBrush, string RoomNumber, string FullName, string Arrival, string Etd, string Nights, string RoomType, string BalanceDue, string Vip, string Alerts) { public string AlertColor => Alerts.Length > 1 ? "#DC2626" : string.IsNullOrEmpty(Alerts) ? "Transparent" : "#EA580C"; }
