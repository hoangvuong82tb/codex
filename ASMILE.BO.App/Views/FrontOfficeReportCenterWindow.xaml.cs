using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class FrontOfficeReportCenterWindow : Window
{
    private List<ReportItem> _allReports;
    private readonly string _categoryFilter;
    private readonly string _sourceModule;
    private int _currentPage;
    private const int PageSize = 20;

    public FrontOfficeReportCenterWindow(string categoryFilter = "All", string sourceModule = "")
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _categoryFilter = categoryFilter;
        _sourceModule = sourceModule;
        _allReports = LoadMockData();
        _currentPage = 0;
        ApplyFilters();
    }

    private static List<ReportItem> LoadMockData()
    {
        var r = new List<ReportItem>();
        var FO = "FrontOffice"; var CS = "Cashiering"; var GP = "Group";
        var HK = "Housekeeping"; var TL = "Telephone"; var AR = "AccountsReceivable";
        var MK = "Marketing"; var NA = "NightAudit"; var RV = "Reservation";
        // Khách — shared: FO, GP, RV, MK, TL
        r.AddRange(new ReportItem[] {
            new("★","Khách đang lưu trú","Khách","Danh sách khách đang lưu trú","PDF/Excel","24/07 08:00",$"{FO},{GP}"),
            new("★","Danh sách khách đến","Khách","Khách đến hôm nay","PDF/Excel","24/07 07:30",FO),
            new("","Danh sách khách đi","Khách","Khách đi hôm nay","PDF/Excel","24/07 07:30",FO),
            new("★","Danh sách VIP","Khách","Danh sách khách VIP","PDF","24/07 08:00",$"{FO},{MK}"),
            new("","Báo cáo quốc tịch","Khách","Thống kê theo quốc tịch","Excel","23/07 18:00",$"{FO},{MK}"),
            new("","Lịch sử khách","Khách","Lịch sử lưu trú của khách","PDF","23/07 16:00",FO),
            new("","Khách dự kiến đến","Khách","Khách dự kiến đến","PDF/Excel","24/07 07:30",$"{FO},{GP}"),
            new("","Khách dự kiến đi","Khách","Khách dự kiến đi","PDF/Excel","24/07 07:30",$"{FO},{GP}"),
        });
        // Phòng — shared: FO, HK, RV
        r.AddRange(new ReportItem[] {
            new("★","Tình trạng phòng","Phòng","Trạng thái phòng hiện tại","PDF/Excel","24/07 08:00",$"{FO},{HK},{RV}"),
            new("","Phòng trống","Phòng","Phòng còn trống","PDF","24/07 08:00",$"{FO},{HK},{RV}"),
            new("","Phòng có khách","Phòng","Phòng đang có khách","PDF","24/07 08:00",$"{FO},{HK}"),
            new("","Phòng ngừng sử dụng","Phòng","Phòng OOO","PDF","23/07 14:00",$"{FO},{HK}"),
            new("","Phòng tạm ngừng","Phòng","Phòng OOS","PDF","23/07 14:00",$"{FO},{HK}"),
            new("","Phòng bẩn","Phòng","Phòng chưa dọn","PDF","24/07 08:00",HK),
            new("","Phòng sạch","Phòng","Phòng đã dọn","PDF","24/07 08:00",HK),
            new("","Phòng đã kiểm tra","Phòng","Phòng đã kiểm tra","PDF","24/07 08:00",HK),
        });
        // Doanh thu — shared: FO, CS, NA, AR, GP
        r.AddRange(new ReportItem[] {
            new("★","Doanh thu phòng","Doanh thu","Doanh thu phòng","PDF/Excel","24/07 09:00",$"{FO},{CS},{NA},{AR}"),
            new("","Dự báo doanh thu","Doanh thu","Dự báo doanh thu","Excel","23/07 17:00",$"{FO},{NA}"),
            new("","Doanh thu ngày","Doanh thu","Doanh thu theo ngày","PDF/Excel","24/07 09:00",$"{FO},{CS},{NA}"),
            new("","Doanh thu tháng","Doanh thu","Doanh thu theo tháng","Excel","01/07 10:00",$"{FO},{CS},{NA},{AR}"),
            new("","Báo cáo đặt cọc","Doanh thu","Báo cáo tiền đặt cọc","PDF","23/07 16:00",$"{CS},{AR}"),
            new("","Báo cáo số dư","Doanh thu","Báo cáo công nợ","PDF/Excel","24/07 08:00",$"{CS},{AR},{NA}"),
        });
        // Thu ngân — shared: CS, FO, AR, NA
        r.AddRange(new ReportItem[] {
            new("★","Folio đang mở","Thu ngân","Folio đang mở","PDF/Excel","24/07 08:00",$"{FO},{CS},{NA}"),
            new("","Folio đã đóng","Thu ngân","Folio đã đóng","PDF","23/07 23:00",$"{CS},{NA}"),
            new("","Công nợ còn lại","Thu ngân","Công nợ chưa thu","PDF/Excel","24/07 08:00",$"{CS},{AR},{NA}"),
            new("","Đặt cọc trước","Thu ngân","Tiền đặt cọc trước","PDF","23/07 18:00",$"{CS},{AR}"),
            new("","Báo cáo hoàn tiền","Thu ngân","Báo cáo hoàn tiền","PDF","23/07 15:00",CS),
            new("","Chuyển khoản mục","Thu ngân","Chuyển khoản mục Folio","PDF","22/07 11:00",CS),
            new("","Phương thức thanh toán","Thu ngân","Theo phương thức TT","Excel","24/07 08:00",$"{CS},{AR}"),
        });
        // Vận hành — shared: FO, HK, NA, TL
        r.AddRange(new ReportItem[] {
            new("","Nhật ký nhắn nhà buồng","Vận hành","Nhật ký nhắn nhà buồng","PDF","24/07 08:00",$"{FO},{HK}"),
            new("","Lịch sử đổi phòng","Vận hành","Lịch sử đổi phòng","PDF","23/07 20:00",FO),
            new("","Lịch sử gia hạn","Vận hành","Lịch sử gia hạn","PDF","23/07 20:00",FO),
            new("","Khách không đến","Vận hành","Khách không đến","PDF","24/07 08:00",$"{FO},{NA}"),
            new("","Hủy đặt phòng","Vận hành","Hủy đặt phòng","PDF/Excel","24/07 08:00",$"{FO},{RV}"),
            new("","Trả phòng muộn","Vận hành","Khách trả phòng muộn","PDF","24/07 08:00",$"{FO},{NA}"),
            new("","Bàn giao ca","Vận hành","Bàn giao ca","PDF","24/07 06:00",FO),
            new("","Nhật ký FO","Vận hành","Nhật ký FO","PDF","24/07 23:00",FO),
            new("","Báo thức","Vận hành","Báo thức","PDF","24/07 06:00",$"{FO},{TL}"),
            new("","Sử dụng amenities","Vận hành","Sử dụng amenities","Excel","23/07 18:00",$"{FO},{HK}"),
        });
        return r;
    }

    private void ApplyFilters()
    {
        var f = _allReports.AsEnumerable();
        if (_categoryFilter != "All")
            f = f.Where(r => r.Modules.Contains(_categoryFilter));
        var list = f.ToList(); _currentPage = 0; ApplyPage(list);
    }

    private void ApplyPage(List<ReportItem> list)
    {
        var paged = list.Skip(_currentPage * PageSize).Take(PageSize).ToList();
        ReportGrid.ItemsSource = paged;
        ResultCount.Text = $"{list.Count} báo cáo";
    }

    private void OnReportSelected(object s, SelectionChangedEventArgs e)
    {
        if (ReportGrid.SelectedItem is not ReportItem r) { PreviewContent.Children.Clear(); PreviewContent.Children.Add(new TextBlock { Text = "Chọn một báo cáo để xem preview.", FontSize = 13, Foreground = C("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) }); return; }
        PreviewContent.Children.Clear();
        PreviewContent.Children.Add(new Border { Width = 260, Height = 180, Background = Cb("#F1F5F9"), CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 0, 10), HorizontalAlignment = HorizontalAlignment.Center, Child = new TextBlock { Text = "Preview", FontSize = 14, Foreground = C("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        PreviewContent.Children.Add(new TextBlock { Text = r.ReportName, FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 4) });
        PreviewContent.Children.Add(new TextBlock { Text = r.Description, FontSize = 12, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
        PreviewContent.Children.Add(new TextBlock { Text = $"Danh mục: {r.Category} · Định dạng: {r.Format} · Chạy gần nhất: {r.LastRun}", FontSize = 11, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap });
    }

    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));

    private void OnQuickReport(object s, MouseButtonEventArgs e)
    {
        var name = (s is Border b && b.Child is TextBlock t) ? t.Text : "Quick Report";
        MessageBox.Show($"Mockup: In nhanh '{name}'. Chưa kết nối backend.", "Báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnPreview(object s, RoutedEventArgs e) => RptMsg("Preview");
    private void OnPrint(object s, RoutedEventArgs e) => RptMsg("In");
    private void OnExportPdf(object s, RoutedEventArgs e) => RptMsg("Export PDF");
    private void OnExportExcel(object s, RoutedEventArgs e) => RptMsg("Export Excel");
    private void OnFavorite(object s, RoutedEventArgs e) => RptMsg("Yêu thích");
    private void OnSchedule(object s, RoutedEventArgs e) => RptMsg("Đặt lịch");
    private void RptMsg(string a) { if (ReportGrid.SelectedItem is ReportItem r) MessageBox.Show($"Mockup: {a} báo cáo '{r.ReportName}'. Chưa kết nối backend.", "Báo cáo", MessageBoxButton.OK, MessageBoxImage.Information); else MessageBox.Show("Vui lòng chọn báo cáo.", "Báo cáo", MessageBoxButton.OK, MessageBoxImage.Warning); }
    private void OnRefresh(object s, RoutedEventArgs e) => ApplyFilters();
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_currentPage > 0) { _currentPage--; ApplyFilters(); } }
    private void OnNextPage(object s, RoutedEventArgs e) { _currentPage++; ApplyFilters(); }
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record ReportItem(string Favorite, string ReportName, string Category, string Description, string Format, string LastRun, string Modules = "All");
