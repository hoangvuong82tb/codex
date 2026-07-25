using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class StayProfileWindow : Window
{
    private List<StayRow> _allStays;
    private int _currentTab;

    public StayProfileWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        LoadMockData();
        ShowTabContent(0);
    }

    private void LoadMockData()
    {
        _allStays = new List<StayRow>
        {
            new("Đang lưu trú","STY-02401","Nguyễn Văn An","0912-345-678","CCCD 079202012345","201","24/07/2026","26/07/2026"),
            new("Đang lưu trú","STY-02402","Trần Thị Bình","0903-456-789","CCCD 001202067890","202","24/07/2026","27/07/2026"),
            new("Đang lưu trú","STY-02403","Lê Hoàng Cường","0987-654-321","HC 891234567","301","23/07/2026","28/07/2026"),
            new("Đang lưu trú","STY-02404","Phạm Minh Đức","0918-222-333","CCCD 030202011122","302","25/07/2026","27/07/2026"),
            new("Đang lưu trú","STY-02405","Hoàng Thị Em","0905-111-444","CCCD 048202055666","401","24/07/2026","26/07/2026"),
            new("Đã trả phòng","STY-02406","Vũ Quang Phúc","0977-888-999","CCCD 012202033444","105","20/07/2026","23/07/2026"),
            new("Đã trả phòng","STY-02407","Đặng Kim Giang","0934-567-890","HC 992345678","210","18/07/2026","22/07/2026"),
            new("Đã trả phòng","STY-02408","Bùi Thanh Hà","0908-123-456","CCCD 024202044555","305","15/07/2026","20/07/2026"),
            new("Đã trả phòng","STY-02409","Ngô Đức Inh","0965-432-109","CCCD 036202066777","402","21/07/2026","24/07/2026"),
            new("Đã trả phòng","STY-02410","Đỗ Lan Khánh","0944-567-321","HC 881234567","108","19/07/2026","21/07/2026"),
            new("Đã trả phòng","STY-02411","Lý Văn Long","0916-789-012","CCCD 020202022222","501","17/07/2026","19/07/2026"),
            new("Sắp đến","STY-02412","Mai Thị Mỹ","0909-345-678","HC 771234567","—","25/07/2026","28/07/2026"),
            new("Sắp đến","STY-02413","Trịnh Hoàng Nam","0988-234-567","CCCD 010202088999","—","26/07/2026","29/07/2026"),
            new("Sắp đến","STY-02414","Hồ Quang Ổn","0922-456-789","CCCD 044202077111","—","27/07/2026","30/07/2026"),
            new("Đã hủy","STY-02415","Phan Văn Phú","0933-789-012","CCCD 016202055888","303","22/07/2026","24/07/2026"),
            new("Đã hủy","STY-02416","Tô Thị Quỳnh","0955-111-222","HC 661234567","205","20/07/2026","23/07/2026"),
            new("Không đến","STY-02417","Chu Văn Rạng","0977-333-444","CCCD 028202099000","—","23/07/2026","25/07/2026"),
            new("Không đến","STY-02418","Hà Thị Sen","0911-456-789","CCCD 032202044333","—","24/07/2026","26/07/2026"),
            new("Đã trả phòng","STY-02419","Vi Văn Tâm","0904-789-321","HC 551234567","404","12/07/2026","15/07/2026"),
            new("Đã trả phòng","STY-02420","Giàng Thị Út","0981-234-567","CCCD 008202022444","502","10/07/2026","14/07/2026")
        };
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allStays.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            filtered = filtered.Where(r => r.Col3.Contains(SearchBox.Text, System.StringComparison.OrdinalIgnoreCase) || r.Col5.Contains(SearchBox.Text, System.StringComparison.OrdinalIgnoreCase));
        if (StatusFilter.SelectedItem is ComboBoxItem cbi && cbi.Content is string st && st != "Tất cả")
            filtered = filtered.Where(r => r.Col1 == st);
        var list = filtered.ToList();
        StayGrid.ItemsSource = list;
        ResultCount.Text = $"{list.Count} hồ sơ";
        FooterTotal.Text = $"Tổng: {list.Count} hồ sơ";
    }

    private void OnStaySelected(object sender, SelectionChangedEventArgs e)
    {
        if (StayGrid.SelectedItem is StayRow r)
        {
            DetailName.Text = r.Col3;
            DetailMeta.Text = $"{r.Col2} · Phòng {r.Col6} · {r.Col7} → {r.Col8}";
            MetricStatus.Text = r.Col1; MetricVisits.Text = "3"; MetricNights.Text = "8"; MetricLast.Text = "24/07";
            ShowTabContent(_currentTab);
        }
    }

    private void OnTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string t && int.TryParse(t, out var tab))
            ShowTabContent(tab);
    }

    private void ShowTabContent(int tab)
    {
        _currentTab = tab; TabContent.Children.Clear();
        var r = StayGrid.SelectedItem as StayRow;
        if (r is null) { TabContent.Children.Add(new TextBlock { Text = "Chọn một hồ sơ để xem thông tin.", FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), Margin = new Thickness(0, 20, 0, 0) }); return; }
        switch (tab)
        {
            case 0: ShowOverview(r); break; case 1: ShowDocuments(r); break; case 2: ShowHistory(r); break; case 3: ShowRoom(r); break; case 4: ShowFinance(r); break;
        }
    }

    private void ShowOverview(StayRow r)
    {
        TabContent.Children.Add(Section("Thông tin khách", Row("Họ và tên", r.Col3), Row("Điện thoại", r.Col4), Row("Giấy tờ", r.Col5)));
        TabContent.Children.Add(Section("Lưu trú hiện tại", Row("Mã HS", r.Col2), Row("Trạng thái", r.Col1), Row("Ngày đến", r.Col7), Row("Ngày đi", r.Col8), Row("Phòng", r.Col6)));
    }
    private void ShowDocuments(StayRow r) { TabContent.Children.Add(Section("Giấy tờ", Row("Loại", r.Col5.Contains("HC") ? "Hộ chiếu" : "CCCD"), Row("Số", r.Col5.Split(' ').LastOrDefault() ?? "—"), Row("Ngày cấp", "15/03/2020"), Row("Nơi cấp", "Cục CSQLHC"), Row("Quê quán", "TP. Hồ Chí Minh"))); }
    private void ShowHistory(StayRow r) { TabContent.Children.Add(Section("Lịch sử lưu trú", Row("Lần 1", "12/01/2026 → 14/01/2026 · Phòng 301"), Row("Lần 2", "05/05/2026 → 08/05/2026 · Phòng 202"), Row("Lần 3", $"{r.Col7} → {r.Col8} · Phòng {r.Col6}"))); }
    private void ShowRoom(StayRow r) { TabContent.Children.Add(Section("Phòng & dịch vụ", Row("Phòng", r.Col6), Row("Hạng phòng", r.Col6.StartsWith("1") ? "STD" : r.Col6.StartsWith("2") ? "DLX" : r.Col6.StartsWith("3") ? "SUP" : "JNR"), Row("Ngày ở", $"{r.Col7} → {r.Col8}"), Row("Meal Code", "RO"), Row("Ghi chú", "Không"))); }
    private void ShowFinance(StayRow r) { TabContent.Children.Add(Section("Tài chính", Row("Mã Folio", $"FOL-{r.Col2[4..]}"), Row("Giá phòng", "1.200.000 VND"), Row("Tiền phòng", "2.400.000 VND"), Row("Đã thanh toán", "0 VND"), Row("Còn phải thu", "2.400.000 VND"))); }

    private Border Section(string title, params FrameworkElement[] children)
    {
        var sp = new StackPanel();
        foreach (var c in children) sp.Children.Add(c);
        var header = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")), Margin = new Thickness(0, 0, 0, 8) };
        var inner = new StackPanel(); inner.Children.Add(header); inner.Children.Add(sp);
        return new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFBFD")),
            CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 10), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")), BorderThickness = new Thickness(1), Child = inner };
    }
    private TextBlock Row(string label, string value)
    {
        var tb = new TextBlock { FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F8198")), Margin = new Thickness(0, 3, 0, 0) };
        tb.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B8DA6")) });
        tb.Inlines.Add(new System.Windows.Documents.Run(value) { FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#17365D")) });
        return tb;
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object sender, RoutedEventArgs e) { SearchBox.Text = ""; StatusFilter.SelectedIndex = 0; ApplyFilters(); }
    private void OnEditToggle(object sender, RoutedEventArgs e) { System.Windows.MessageBox.Show("Mockup: chỉnh sửa chưa được kết nối với backend.", "Hồ sơ lưu trú", MessageBoxButton.OK, MessageBoxImage.Information); }
    private void OnClose(object sender, RoutedEventArgs e) => Close();
    private void OnRefresh(object sender, RoutedEventArgs e) => ApplyFilters();
}

internal record StayRow(string Col1, string Col2 = "", string Col3 = "", string Col4 = "", string Col5 = "", string Col6 = "", string Col7 = "", string Col8 = "");
