using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class HousekeepingMessageWindow : Window
{
    private List<HskRequest> _allRequests;
    private int _currentPage;
    private const int PageSize = 20;

    public HousekeepingMessageWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _allRequests = LoadMockData();
        _currentPage = 0;
        ShowTab(0);
        UpdateMetrics();
        ApplyFilters();
    }

    private static List<HskRequest> LoadMockData() => new()
    {
        new("Khẩn cấp","#FEE2E2","#DC2626","HSK-001","201","Dọn phòng ngay","Khách VIP vừa check-in, cần dọn phòng cấp tốc.","Lễ tân A","TB Hiền","10:15","12:00","Mới tạo","#DBEAFE","#2563EB","⚡"),
        new("Cao","#FFF7ED","#EA580C","HSK-002","305","Kiểm tra Minibar","Kiểm tra Minibar trước khi khách trả phòng 12:00.","Lễ tân A","TB Hoa","10:20","11:30","Đang xử lý","#FFF7ED","#EA580C","⚠"),
        new("Bình thường","#DBEAFE","#2563EB","HSK-003","108","Bổ sung đồ dùng","Khách yêu cầu thêm 2 khăn tắm, 1 chai nước.","Lễ tân B","TB Lan","10:30","11:00","Hoàn thành","#DCFCE7","#16A34A",""),
        new("Khẩn cấp","#FEE2E2","#DC2626","HSK-004","402","Sửa thiết bị","Điều hòa phòng 402 không mát, khách phàn nàn.","Lễ tân C","KT Tuấn","10:35","11:00","Đang xử lý","#FFF7ED","#EA580C","⚡"),
        new("Cao","#FFF7ED","#EA580C","HSK-005","210","Kiểm tra phòng","Kiểm tra phòng trước khi khách VIP check-in lúc 14:00.","Lễ tân A","GS Mai","10:40","13:30","Chờ xác nhận","#FEF9C3","#A16207","⚠"),
        new("Bình thường","#DBEAFE","#2563EB","HSK-006","503","Thay ga/gối","Khách yêu cầu thay toàn bộ ga gối phòng Suite.","Lễ tân B","TB Hà","10:45","12:00","Hoàn thành","#DCFCE7","#16A34A",""),
        new("Khẩn cấp","#FEE2E2","#DC2626","HSK-007","115","Lost & Found","Khách báo mất đồng hồ trong phòng, cần kiểm tra gấp.","Lễ tân A","GS Mai","11:00","11:30","Đang xử lý","#FFF7ED","#EA580C","⚡"),
        new("Thấp","#F3F4F6","#64748B","HSK-008","301","Khác","Khách hỏi về dịch vụ giặt là, cần tư vấn thêm.","Lễ tân C","TB Hiền","11:05","14:00","Mới tạo","#DBEAFE","#2563EB",""),
        new("Cao","#FFF7ED","#EA580C","HSK-009","220","Dọn phòng ngay","Phòng cần tổng vệ sinh sau khi khách check-out.","Lễ tân B","TB Lan","11:10","13:00","Chờ xác nhận","#FEF9C3","#A16207",""),
        new("Bình thường","#DBEAFE","#2563EB","HSK-010","118","Bổ sung đồ dùng","Cần bổ sung amenities: bàn chải, kem đánh răng, dầu gội.","Lễ tân A","TB Hà","11:15","12:00","Hoàn thành","#DCFCE7","#16A34A",""),
        new("Cao","#FFF7ED","#EA580C","HSK-011","405","Kiểm tra Minibar","Kiểm tra và chốt Minibar cho khách chuẩn bị check-out.","Lễ tân C","TB Hoa","11:20","12:00","Đang xử lý","#FFF7ED","#EA580C","⚠"),
        new("Khẩn cấp","#FEE2E2","#DC2626","HSK-012","502","Sửa thiết bị","TV phòng Suite không bật được, khách VIP phàn nàn.","Lễ tân A","KT Tuấn","11:25","11:45","Quá hạn","#FEE2E2","#DC2626","⚡"),
        new("Bình thường","#DBEAFE","#2563EB","HSK-013","303","Thay ga/gối","Thay ga gối định kỳ cho phòng Superior.","Lễ tân B","TB Lan","11:30","14:00","Mới tạo","#DBEAFE","#2563EB",""),
        new("Cao","#FFF7ED","#EA580C","HSK-014","106","Dọn phòng ngay","Khách sắp check-in lúc 13:00, phòng cần dọn gấp.","Lễ tân A","TB Hiền","11:35","12:30","Đang xử lý","#FFF7ED","#EA580C","⚠"),
        new("Bình thường","#DBEAFE","#2563EB","HSK-015","401","Kiểm tra phòng","Kiểm tra định kỳ phòng Junior trước cuối ca.","Lễ tân C","GS Mai","11:40","16:00","Chờ xác nhận","#FEF9C3","#A16207",""),
        new("Thấp","#F3F4F6","#64748B","HSK-016","205","Khác","Ghi nhận phản hồi khách về mùi phòng, cần kiểm tra.","Lễ tân B","TB Hà","11:45","15:00","Mới tạo","#DBEAFE","#2563EB",""),
        new("Khẩn cấp","#FEE2E2","#DC2626","HSK-017","504","Lost & Found","Phát hiện ví khách bỏ quên trong tủ, cần ghi nhận.","Lễ tân A","GS Mai","11:50","12:00","Quá hạn","#FEE2E2","#DC2626","⚡"),
        new("Cao","#FFF7ED","#EA580C","HSK-018","209","Dọn phòng ngay","Phòng VIP cần kiểm tra và setup welcome amenities.","Lễ tân A","TB Hiền","11:55","14:00","Đang xử lý","#FFF7ED","#EA580C","⚠"),
        new("Bình thường","#DBEAFE","#2563EB","HSK-019","310","Bổ sung đồ dùng","Thêm nước uống cho phòng Superior.","Lễ tân C","TB Hoa","12:00","13:00","Hoàn thành","#DCFCE7","#16A34A",""),
        new("Cao","#FFF7ED","#EA580C","HSK-020","123","Sửa thiết bị","Vòi sen phòng Standard bị rò rỉ nước.","Lễ tân B","KT Tuấn","12:05","14:00","Chờ xác nhận","#FEF9C3","#A16207","⚠"),
    };

    private void ApplyFilters()
    {
        var f = _allRequests.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text)) { var kw = SearchBox.Text.ToLower(); f = f.Where(r => r.Content.ToLower().Contains(kw) || r.RoomNumber.Contains(kw) || r.RequestId.ToLower().Contains(kw)); }
        if (FloorFilter.SelectedItem is ComboBoxItem fl && fl.Content is string fv && fv != "Tất cả") f = f.Where(r => r.RoomNumber.StartsWith(fv));
        if (TypeFilter.SelectedItem is ComboBoxItem tp && tp.Content is string tv && tv != "Tất cả") f = f.Where(r => r.RequestType == tv);
        if (PriorityFilter.SelectedItem is ComboBoxItem pr && pr.Content is string pv && pv != "Tất cả") f = f.Where(r => r.Priority == pv);
        if (StatusFilter.SelectedItem is ComboBoxItem st && st.Content is string sv && sv != "Tất cả") f = f.Where(r => r.Status == sv);
        if (ChkUrgent.IsChecked == true) f = f.Where(r => r.Priority == "Khẩn cấp");
        if (ChkOverdue.IsChecked == true) f = f.Where(r => r.Status == "Quá hạn");
        var list = f.ToList(); _currentPage = 0; ApplyPage(list); UpdateMetrics(list);
    }

    private void ApplyPage(List<HskRequest> list)
    {
        var paged = list.Skip(_currentPage * PageSize).Take(PageSize).ToList();
        RequestGrid.ItemsSource = paged;
        ResultCount.Text = $"{list.Count} yêu cầu";
        FooterTotal.Text = $"Tổng: {list.Count} yêu cầu · {list.Count(r => r.Status != "Hoàn thành")} đang mở";
    }

    private void UpdateMetrics(List<HskRequest>? list = null)
    {
        list ??= _allRequests;
        MetricOpen.Text = list.Count(r => r.Status != "Hoàn thành").ToString();
        MetricUrgent.Text = list.Count(r => r.Priority == "Khẩn cấp").ToString();
        MetricProcessing.Text = list.Count(r => r.Status == "Đang xử lý").ToString();
        MetricPendingConfirm.Text = list.Count(r => r.Status == "Chờ xác nhận").ToString();
        MetricOverdue.Text = list.Count(r => r.Status == "Quá hạn").ToString();
        MetricCompleted.Text = list.Count(r => r.Status == "Hoàn thành").ToString();
    }

    private void OnRequestSelected(object sender, SelectionChangedEventArgs e)
    {
        if (RequestGrid.SelectedItem is not HskRequest r) { DetailEmpty.Visibility = Visibility.Visible; DetailContent.Visibility = Visibility.Collapsed; return; }
        DetailEmpty.Visibility = Visibility.Collapsed; DetailContent.Visibility = Visibility.Visible;
        DetailInitials.Text = r.RequestId[^3..]; DetailReqId.Text = r.RequestId;
        DetailPriority.Text = r.Priority; DetailPriority.Foreground = C(r.PriorityColor);
        DetailStatus.Text = r.Status; DetailStatus.Foreground = C(r.StatusColor);
        DetailRoomMeta.Text = $"Phòng {r.RoomNumber} · {r.RequestType}"; DetailCreated.Text = r.CreatedAt;
        ShowTab(int.TryParse((TabContent.Tag as string) ?? "0", out var t) ? t : 0);
    }

    private void ShowTab(int tab) { TabContent.Tag = tab.ToString(); TabContent.Children.Clear(); if (RequestGrid.SelectedItem is not HskRequest r) return; switch (tab) { case 0: ShowContent(r); break; case 1: ShowChat(r); break; case 2: ShowHistory(r); break; case 3: ShowRoom(r); break; } }

    private void ShowContent(HskRequest r)
    {
        TabContent.Children.Add(S("Yêu cầu", R("Loại", r.RequestType), R("Nội dung", r.Content), R("Ưu tiên", r.Priority), R("Hạn xử lý", r.Deadline), R("Người phụ trách", r.Assignee), R("Trạng thái", r.Status)));
        TabContent.Children.Add(S("Ghi chú", R("Lễ tân", r.Content.Length > 60 ? "Đã trao đổi với khách." : "—")));
    }

    private void ShowChat(HskRequest r)
    {
        var msgs = new[] {
            (r.Assignee, "Nhà buồng", "10:35", "Đã nhận yêu cầu, đang kiểm tra."),
            ("Lễ tân A", "Lễ tân", "10:38", "Cảm ơn, khách đang đợi."),
            (r.Assignee, "Nhà buồng", "10:42", r.Priority == "Khẩn cấp" ? "Đã xong, vui lòng kiểm tra lại." : "Đang thực hiện, khoảng 15 phút nữa.")
        };
        var sp = new StackPanel();
        foreach (var (who, dept, time, text) in msgs)
        {
            var isSelf = who.StartsWith("Lễ"); var align = isSelf ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            var bg = isSelf ? "#DBEAFE" : "#F3F4F6"; var fg = isSelf ? "#1E40AF" : "#1F2937";
            sp.Children.Add(new Border { Background = Cb(bg), CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(isSelf ? 40 : 0, 0, isSelf ? 0 : 40, 6), HorizontalAlignment = align, Child = new StackPanel { Children = { T11($"{who} · {dept}", "#64748B", true), T12(text, fg, true), T10(time, "#94A3B8", false) } } });
        }
        TabContent.Children.Add(S("Trao đổi", sp));
    }

    private void ShowHistory(HskRequest r)
    {
        var events = new List<(string, string, string, string, string)> {
            ("10:15","Lễ tân A","Lễ tân","Tạo yêu cầu",$"Yêu cầu {r.RequestType} cho phòng {r.RoomNumber}"),
            ("10:20","GS Mai","Nhà buồng","Phân công",$"Phân công {r.Assignee} xử lý"),
            ("10:25",r.Assignee,"Nhà buồng","Tiếp nhận","Đã nhận và bắt đầu xử lý"),
            (r.Priority=="Khẩn cấp"?"10:35":"10:45",r.Assignee,"Nhà buồng","Cập nhật","Đang thực hiện"+(r.Status=="Hoàn thành"?", sắp xong":"...")),
        };
        if (r.Status == "Hoàn thành") events.Add(("10:50", r.Assignee, "Nhà buồng", "Hoàn thành", "Đã xử lý xong, chờ xác nhận."));
        var sp = new StackPanel();
        foreach (var (time, who, dept, action, detail) in events)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.Children.Add(new TextBlock { Text = time, FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 0, 0) });
            var dot = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = Cb("#2F6BFF"), Margin = new Thickness(0, 4, 0, 0), HorizontalAlignment = HorizontalAlignment.Center }; Grid.SetColumn(dot, 1); grid.Children.Add(dot);
            var ds = new StackPanel(); Grid.SetColumn(ds, 2); ds.Children.Add(T12(action, "#17365D", true)); ds.Children.Add(T11($"{who} · {dept}", "#64748B", false)); ds.Children.Add(T11(detail, "#6F8198", false)); grid.Children.Add(ds);
            sp.Children.Add(grid);
        }
        TabContent.Children.Add(S("Lịch sử", sp));
    }

    private void ShowRoom(HskRequest r)
    {
        var rt = r.RoomNumber.StartsWith("5") ? "Suite" : r.RoomNumber.StartsWith("4") ? "Junior" : r.RoomNumber.StartsWith("3") ? "Superior" : r.RoomNumber.StartsWith("2") ? "Deluxe" : "Standard";
        var guest = r.RoomNumber switch { "201" => "Nguyễn Văn An", "305" => "Mai Thị Mỹ", "402" => "Phạm Minh Đức", _ => "—" };
        TabContent.Children.Add(S("Phòng", R("Số phòng", r.RoomNumber), R("Hạng phòng", rt), R("Tầng", r.RoomNumber[..1]), R("Trạng thái NB", r.Status == "Hoàn thành" ? "Sạch" : "Đang xử lý")));
        TabContent.Children.Add(S("Khách", R("Tên", guest), R("VIP", r.RoomNumber is "201" or "502" ? "★ Có" : "Không")));
    }

    // Helpers
    private static SolidColorBrush C(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static SolidColorBrush Cb(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static TextBlock T11(string text, string color, bool bold) => new() { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(0, 1, 0, 0) };
    private static TextBlock T12(string text, string color, bool top) => new() { Text = text, FontSize = 12, FontWeight = top ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), TextWrapping = TextWrapping.Wrap, Margin = top ? new Thickness(0) : new Thickness(0, 2, 0, 0) };
    private static TextBlock T10(string text, string color, bool right) => new() { Text = text, FontSize = 10, Foreground = C(color), HorizontalAlignment = right ? HorizontalAlignment.Right : HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0) };
    private static FrameworkElement S(string title, params FrameworkElement[] children) { var sp = new StackPanel(); foreach (var c in children) sp.Children.Add(c); var h = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#0F172A"), Margin = new Thickness(0, 0, 0, 6) }; var i = new StackPanel(); i.Children.Add(h); i.Children.Add(sp); return new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8), BorderBrush = Cb("#F1F5F9"), BorderThickness = new Thickness(1), Child = i }; }
    private static TextBlock R(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.Inlines.Add(new System.Windows.Documents.Run(l + ": ") { Foreground = C("#94A3B8") }); tb.Inlines.Add(new System.Windows.Documents.Run(v) { FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }); return tb; }

    private void OnTabClick(object s, RoutedEventArgs e) { if (s is Button b && b.Tag is string t && int.TryParse(t, out var tab)) ShowTab(tab); }
    private void OnFilterChanged(object s, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnQuickFilterChanged(object s, RoutedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = ""; FloorFilter.SelectedIndex = 0; TypeFilter.SelectedIndex = 0; PriorityFilter.SelectedIndex = 0; StatusFilter.SelectedIndex = 0; ChkUrgent.IsChecked = false; ChkOverdue.IsChecked = false; ApplyFilters(); }
    private void OnRefresh(object s, RoutedEventArgs e) => ApplyFilters();
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_currentPage > 0) { _currentPage--; ApplyFilters(); } }
    private void OnNextPage(object s, RoutedEventArgs e) { _currentPage++; ApplyFilters(); }
    private void OnCreateRequest(object s, RoutedEventArgs e) => MessageBox.Show("═══ Tạo yêu cầu nhà buồng ═══\n\nChọn phòng: (click để chọn nhiều)\nLoại: (chọn)\nƯu tiên: (chọn)\nNội dung: (nhập)\n\nNút: [Hủy] [Lưu nháp] [Gửi yêu cầu]\n\nMockup: yêu cầu sẽ được thêm vào danh sách.", "Tạo yêu cầu", MessageBoxButton.OK, MessageBoxImage.Information);
    private void OnUpdateProgress(object s, RoutedEventArgs e) => Msg("Cập nhật tiến độ");
    private void OnAssign(object s, RoutedEventArgs e) => Msg("Phân công");
    private void OnComplete(object s, RoutedEventArgs e) => Msg("Hoàn thành");
    private void OnCancelRequest(object s, RoutedEventArgs e) => Msg("Hủy yêu cầu");
    private void OnMoreActions(object s, RoutedEventArgs e) => Msg("Thao tác khác");
    private void Msg(string a) { if (RequestGrid.SelectedItem is HskRequest r) MessageBox.Show($"Mockup: {a} cho {r.RequestId}. Chưa kết nối backend.", "Nhắn nhà buồng", MessageBoxButton.OK, MessageBoxImage.Information); else MessageBox.Show("Vui lòng chọn yêu cầu.", "Nhắn nhà buồng", MessageBoxButton.OK, MessageBoxImage.Warning); }
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record HskRequest(string Priority, string PriorityBadge, string PriorityColor, string RequestId, string RoomNumber, string RequestType, string Content, string Creator, string Assignee, string CreatedAt, string Deadline, string Status, string StatusBadge, string StatusColor, string Alert);
