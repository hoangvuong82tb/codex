using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class LockedRoomWindow : Window
{
    private List<LockedRoomRow> _allRooms;

    public LockedRoomWindow()
    {
        InitializeComponent();
        LoadMockData();
    }

    private void LoadMockData()
    {
        _allRooms = new List<LockedRoomRow>
        {
            new("201","Deluxe","2","Trống","Đang khóa","Bảo trì","Lễ tân A","24/07/2026","27/07/2026","Sửa điều hòa","#FEE2E2","#DC2626"),
            new("305","Superior","3","Trống","Đang khóa","Vệ sinh","Buồng phòng","24/07/2026","25/07/2026","Tổng vệ sinh","#FEE2E2","#DC2626"),
            new("108","Standard","1","Trống","Sắp mở","Sửa chữa","Kỹ thuật","22/07/2026","24/07/2026","Thay ống nước","#FFF7ED","#EA580C"),
            new("402","Junior","4","Trống","Đang khóa","Bảo trì","Lễ tân B","23/07/2026","30/07/2026","Bảo trì định kỳ","#FEE2E2","#DC2626"),
            new("210","Deluxe","2","Có khách","Đang khóa","Sửa chữa","Kỹ thuật","23/07/2026","26/07/2026","Sửa cửa sổ","#FEE2E2","#DC2626"),
            new("503","Suite","5","Trống","Sắp mở","Kiểm kê","Quản lý","20/07/2026","25/07/2026","Kiểm kê tài sản","#FFF7ED","#EA580C"),
            new("115","Standard","1","Trống","Đã hết hạn","Khác","Lễ tân A","15/07/2026","20/07/2026","Chờ quyết định","#F3F4F6","#64748B"),
            new("301","Superior","3","Trống","Đã hết hạn","Bảo trì","Kỹ thuật","10/07/2026","18/07/2026","Sơn lại tường","#F3F4F6","#64748B"),
            new("205","Deluxe","2","Trống","Đang khóa","Vệ sinh","Buồng phòng","25/07/2026","25/07/2026","Dọn khẩn","#FEE2E2","#DC2626"),
            new("410","Junior","4","Trống","Đang khóa","Bảo trì","Kỹ thuật","24/07/2026","28/07/2026","Thay thảm","#FEE2E2","#DC2626"),
            new("220","Deluxe","2","Có khách","Sắp mở","Sửa chữa","Lễ tân C","22/07/2026","24/07/2026","Sửa TV","#FFF7ED","#EA580C"),
            new("118","Standard","1","Trống","Đang khóa","Bảo trì","Kỹ thuật","24/07/2026","26/07/2026","Kiểm tra điện","#FEE2E2","#DC2626"),
        };
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allRooms.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            filtered = filtered.Where(r =>
                r.RoomNumber.Contains(SearchBox.Text, System.StringComparison.OrdinalIgnoreCase) ||
                r.Reason.Contains(SearchBox.Text, System.StringComparison.OrdinalIgnoreCase));
        if (FloorFilter.SelectedItem is ComboBoxItem cbiFloor && cbiFloor.Content is string floor && floor != "Tất cả")
            filtered = filtered.Where(r => r.Floor == floor);
        if (StatusFilter.SelectedItem is ComboBoxItem cbiStatus && cbiStatus.Content is string status && status != "Tất cả")
            filtered = filtered.Where(r => r.LockStatus == status);
        if (TypeFilter.SelectedItem is ComboBoxItem cbiType && cbiType.Content is string type && type != "Tất cả")
            filtered = filtered.Where(r => r.Reason == type);
        var list = filtered.ToList();
        LockedGrid.ItemsSource = list;
        ResultCount.Text = $"{list.Count} phòng";
        FooterTotal.Text = $"Tổng: {list.Count} phòng";
    }

    private void OnRoomSelected(object sender, SelectionChangedEventArgs e)
    {
        if (LockedGrid.SelectedItem is not LockedRoomRow r)
        {
            DetailPanel.Children.Clear();
            DetailPanel.Children.Add(new TextBlock { Text = "Chọn một phòng để xem chi tiết.", FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) });
            return;
        }

        DetailPanel.Children.Clear();

        DetailPanel.Children.Add(DetailRow("Phòng", $"{r.RoomNumber} · {r.RoomType}"));
        DetailPanel.Children.Add(DetailRow("Loại khóa", r.Reason));
        DetailPanel.Children.Add(DetailRow("Thời gian", $"{r.StartDate}  →  {r.EndDate}"));
        DetailPanel.Children.Add(DetailRow("Người thực hiện", r.LockedBy));
        DetailPanel.Children.Add(DetailRow("Trạng thái phòng", r.RoomStatus));
        DetailPanel.Children.Add(DetailRow("Ghi chú", string.IsNullOrWhiteSpace(r.Note) ? "—" : r.Note));
    }

    private static FrameworkElement DetailRow(string label, string value)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        sp.Children.Add(new TextBlock
        {
            Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
            Margin = new Thickness(0, 0, 0, 2)
        });
        sp.Children.Add(new TextBlock
        {
            Text = value, FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#17365D")),
            TextWrapping = TextWrapping.Wrap
        });
        return sp;
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
        FloorFilter.SelectedIndex = 0;
        StatusFilter.SelectedIndex = 0;
        TypeFilter.SelectedIndex = 0;
        ApplyFilters();
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => ApplyFilters();

    private void OnLockRoom(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Mockup: Mở modal Khóa phòng mới chưa được kết nối với backend.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnUnlockRoom(object sender, RoutedEventArgs e)
    {
        if (LockedGrid.SelectedItem is LockedRoomRow r)
            MessageBox.Show($"Mockup: Mở khóa phòng {r.RoomNumber}. Chưa kết nối backend.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show("Vui lòng chọn một phòng trước.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnExtendLock(object sender, RoutedEventArgs e)
    {
        if (LockedGrid.SelectedItem is LockedRoomRow r)
            MessageBox.Show($"Mockup: Gia hạn khóa phòng {r.RoomNumber}. Chưa kết nối backend.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show("Vui lòng chọn một phòng trước.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnViewHistory(object sender, RoutedEventArgs e)
    {
        if (LockedGrid.SelectedItem is LockedRoomRow r)
            MessageBox.Show($"Mockup: Xem lịch sử phòng {r.RoomNumber}. Chưa kết nối backend.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show("Vui lòng chọn một phòng trước.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

internal record LockedRoomRow(
    string RoomNumber, string RoomType, string Floor, string RoomStatus,
    string LockStatus, string Reason, string LockedBy,
    string StartDate, string EndDate, string Note,
    string LockStatusBadge, string LockStatusColor);
