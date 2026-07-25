using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class ExchangeRateWindow : Window
{
    private List<RateRow> _allRates;
    private int _currentPage;
    private const int PageSize = 20;

    public ExchangeRateWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _allRates = LoadMockData();
        _currentPage = 0;
        ShowTab(0);
        UpdateMetrics();
        ApplyFilters();
    }

    private static List<RateRow> LoadMockData() => new()
    {
        new("USD","US Dollar","$","25,460","25,680","26,120","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("EUR","Euro","€","27,890","28,450","28,900","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("GBP","British Pound","£","32,150","32,980","33,400","22/07/2026","28/07/2026","Sắp hết hạn","#FFF7ED","#EA580C","22/07"),
        new("JPY","Japanese Yen","¥","168","175","182","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("KRW","Korean Won","₩","18.5","19.2","20.0","20/07/2026","27/07/2026","Sắp hết hạn","#FFF7ED","#EA580C","20/07"),
        new("CNY","Chinese Yuan","¥","3,510","3,580","3,640","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("SGD","Singapore Dollar","$","19,120","19,450","19,780","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("AUD","Australian Dollar","$","16,890","17,240","17,520","22/07/2026","28/07/2026","Hiệu lực","#DCFCE7","#16A34A","22/07"),
        new("CAD","Canadian Dollar","$","18,760","19,120","19,450","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("CHF","Swiss Franc","Fr","28,340","28,900","29,350","20/07/2026","26/07/2026","Hết hạn","#FEE2E2","#DC2626","20/07"),
        new("THB","Thai Baht","฿","702","725","742","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("MYR","Malaysian Ringgit","RM","5,450","5,560","5,680","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("HKD","Hong Kong Dollar","$","3,260","3,320","3,380","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("TWD","Taiwan Dollar","NT$","812","835","855","22/07/2026","29/07/2026","Hiệu lực","#DCFCE7","#16A34A","22/07"),
        new("INR","Indian Rupee","₹","305","315","325","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("AED","UAE Dirham","د.إ","6,930","6,990","7,120","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("RUB","Russian Ruble","₽","278","290","305","18/07/2026","25/07/2026","Hết hạn","#FEE2E2","#DC2626","18/07"),
        new("NZD","New Zealand Dollar","$","15,450","15,780","16,050","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("PHP","Philippine Peso","₱","456","472","485","24/07/2026","31/07/2026","Hiệu lực","#DCFCE7","#16A34A","24/07"),
        new("VND","Vietnamese Dong","₫","1","1","1","01/01/2026","31/12/2026","Hiệu lực","#DCFCE7","#16A34A","01/01"),
    };

    private void ApplyFilters()
    {
        var f = _allRates.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text)) { var kw = SearchBox.Text.ToLower(); f = f.Where(r => r.CurrencyCode.ToLower().Contains(kw) || r.CurrencyName.ToLower().Contains(kw)); }
        if (StatusFilter.SelectedItem is ComboBoxItem st && st.Content is string sv && sv != "Tất cả") f = f.Where(r => r.Status == sv);
        var list = f.ToList(); _currentPage = 0; ApplyPage(list); UpdateMetrics(list);
    }

    private void ApplyPage(List<RateRow> list)
    {
        var paged = list.Skip(_currentPage * PageSize).Take(PageSize).ToList();
        RateGrid.ItemsSource = paged;
        ResultCount.Text = $"{list.Count} ngoại tệ"; FooterTotal.Text = $"Tổng: {list.Count} ngoại tệ";
    }

    private void UpdateMetrics(List<RateRow>? list = null)
    {
        list ??= _allRates;
        MetricActive.Text = list.Count.ToString();
        MetricEffective.Text = list.Count(r => r.Status == "Hiệu lực").ToString();
        MetricExpiring.Text = list.Count(r => r.Status == "Sắp hết hạn").ToString();
        MetricExpired.Text = list.Count(r => r.Status == "Hết hạn").ToString();
    }

    private void OnRateSelected(object sender, SelectionChangedEventArgs e)
    {
        if (RateGrid.SelectedItem is not RateRow r) { DetailEmpty.Visibility = Visibility.Visible; DetailContent.Visibility = Visibility.Collapsed; TabContent.Children.Clear(); return; }
        DetailEmpty.Visibility = Visibility.Collapsed; DetailContent.Visibility = Visibility.Visible;
        DetailCode.Text = r.CurrencyCode; DetailName.Text = r.CurrencyName;
        DetailStatus.Text = r.Status; DetailStatus.Foreground = C(r.StatusColor);
        DetailMeta.Text = $"1 {r.CurrencyCode} = {r.SellRate} VND · {r.EffectiveDate} → {r.ExpiredDate}";
        ShowTab(int.TryParse((TabContent.Tag as string) ?? "0", out var t) ? t : 0);
    }

    private void ShowTab(int tab) { TabContent.Tag = tab.ToString(); TabContent.Children.Clear(); if (RateGrid.SelectedItem is not RateRow r) return; switch (tab) { case 0: ShowOverview(r); break; case 1: ShowHistory(r); break; case 2: ShowApply(r); break; case 3: ShowNotes(r); break; } }

    private void ShowOverview(RateRow r)
    {
        TabContent.Children.Add(S("Thông tin", R("Mã", r.CurrencyCode), R("Tên", r.CurrencyName), R("ISO", r.CurrencyCode), R("Quốc gia", GetCountry(r.CurrencyCode)), R("Symbol", r.Symbol), R("Decimal", "2")));
        TabContent.Children.Add(S("Tỷ giá", R("Mua vào", r.BuyRate), R("Bán ra", r.SellRate), R("Hotel Rate", r.HotelRate)));
        TabContent.Children.Add(S("Hiệu lực", R("Ngày HL", r.EffectiveDate), R("Ngày HH", r.ExpiredDate), R("Cập nhật", r.LastUpdated), R("Trạng thái", r.Status)));
    }

    private void ShowHistory(RateRow r)
    {
        var sp = new StackPanel();
        sp.Children.Add(TimelineItem("24/07 09:00", "Lễ tân A", "Cập nhật tỷ giá", "Điều chỉnh Hotel Rate"));
        sp.Children.Add(TimelineItem("22/07 15:30", "Kế toán B", "Chỉnh sửa", "Thay đổi ngày hết hạn"));
        sp.Children.Add(TimelineItem("20/07 08:00", "Quản lý", "Tạo mới", "Thêm ngoại tệ vào hệ thống"));
        TabContent.Children.Add(S("Lịch sử thay đổi", sp));
    }

    private static FrameworkElement TimelineItem(string time, string who, string action, string detail)
    {
        var g = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        g.ColumnDefinitions.Add(new ColumnDefinition());
        g.Children.Add(new TextBlock { Text = time, FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 2, 0, 0) });
        var dot = new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = Cb("#059669"), Margin = new Thickness(0, 5, 0, 0), HorizontalAlignment = HorizontalAlignment.Center }; Grid.SetColumn(dot, 1); g.Children.Add(dot);
        var ds = new StackPanel(); Grid.SetColumn(ds, 2); ds.Children.Add(new TextBlock { Text = action, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }); ds.Children.Add(new TextBlock { Text = $"{who} · {detail}", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) }); g.Children.Add(ds);
        return g;
    }

    private void ShowApply(RateRow r)
    {
        TabContent.Children.Add(S("Áp dụng cho", R("Folio", "✓"), R("Cashiering", "✓"), R("Deposit", "✓"), R("Refund", "✓")));
    }

    private void ShowNotes(RateRow r)
    {
        TabContent.Children.Add(S("Ghi chú", R("Nội bộ", r.CurrencyCode == "VND" ? "Tiền tệ mặc định của hệ thống." : r.CurrencyCode == "USD" ? "Ngoại tệ chính, cập nhật hàng tuần." : "—")));
    }

    private static string GetCountry(string code) => code switch
    {
        "USD" => "Hoa Kỳ", "EUR" => "EU", "GBP" => "Anh", "JPY" => "Nhật Bản", "KRW" => "Hàn Quốc",
        "CNY" => "Trung Quốc", "SGD" => "Singapore", "AUD" => "Úc", "CAD" => "Canada", "CHF" => "Thụy Sĩ",
        "THB" => "Thái Lan", "MYR" => "Malaysia", "HKD" => "Hong Kong", "TWD" => "Đài Loan", "INR" => "Ấn Độ",
        "AED" => "UAE", "RUB" => "Nga", "NZD" => "New Zealand", "PHP" => "Philippines", "VND" => "Việt Nam", _ => "—"
    };

    private static SolidColorBrush C(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static SolidColorBrush Cb(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static FrameworkElement S(string title, params FrameworkElement[] children) { var sp = new StackPanel(); foreach (var c in children) sp.Children.Add(c); var h = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#0F172A"), Margin = new Thickness(0, 0, 0, 6) }; var i = new StackPanel(); i.Children.Add(h); i.Children.Add(sp); return new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8), BorderBrush = Cb("#F1F5F9"), BorderThickness = new Thickness(1), Child = i }; }
    private static TextBlock R(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.Inlines.Add(new System.Windows.Documents.Run(l + ": ") { Foreground = C("#94A3B8") }); tb.Inlines.Add(new System.Windows.Documents.Run(v) { FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }); return tb; }

    private void OnTabClick(object s, RoutedEventArgs e) { if (s is Button b && b.Tag is string t && int.TryParse(t, out var tab)) ShowTab(tab); }
    private void OnFilterChanged(object s, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = ""; StatusFilter.SelectedIndex = 0; ApplyFilters(); }
    private void OnRefresh(object s, RoutedEventArgs e) => ApplyFilters();
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_currentPage > 0) { _currentPage--; ApplyFilters(); } }
    private void OnNextPage(object s, RoutedEventArgs e) { _currentPage++; ApplyFilters(); }
    private void OnAddRate(object s, RoutedEventArgs e) => MessageBox.Show("═══ Thêm tỷ giá ═══\n\nCurrency: (chọn)\nBuy Rate: (nhập)\nSell Rate: (nhập)\nHotel Rate: (nhập)\nNgày HL: (chọn)\nNgày HH: (chọn)\n\nNút: [Hủy] [Lưu]\n\nMockup: chưa kết nối backend.", "Thêm tỷ giá", MessageBoxButton.OK, MessageBoxImage.Information);
    private void OnEdit(object s, RoutedEventArgs e) => Msg("Chỉnh sửa");
    private void OnCopy(object s, RoutedEventArgs e) => Msg("Sao chép");
    private void OnSetDefault(object s, RoutedEventArgs e) => Msg("Đặt mặc định");
    private void OnViewHistory(object s, RoutedEventArgs e) => Msg("Xem lịch sử");
    private void Msg(string a) { if (RateGrid.SelectedItem is RateRow r) MessageBox.Show($"Mockup: {a} tỷ giá {r.CurrencyCode}.", "Tỷ giá", MessageBoxButton.OK, MessageBoxImage.Information); else MessageBox.Show("Vui lòng chọn ngoại tệ.", "Tỷ giá", MessageBoxButton.OK, MessageBoxImage.Warning); }
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record RateRow(string CurrencyCode, string CurrencyName, string Symbol, string BuyRate, string SellRate, string HotelRate, string EffectiveDate, string ExpiredDate, string Status, string StatusBadge, string StatusColor, string LastUpdated);
