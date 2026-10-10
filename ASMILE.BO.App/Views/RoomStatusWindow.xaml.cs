using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class RoomStatusWindow : Window
{
    private List<HotelRoomDto> _allRooms = new();
    private readonly HotelRoomMasterDataQueryHandlers _hotelQueries;

    public RoomStatusWindow(HotelRoomMasterDataQueryHandlers hotelQueries)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _hotelQueries = hotelQueries;
        Loaded += async (_, _) => await LoadRoomsAsync();
    }

    private async System.Threading.Tasks.Task LoadRoomsAsync()
    {
        LoadError.Text = string.Empty;
        LoadError.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _hotelQueries.SearchRooms(null, null, null, null, null, true, new(1, 500), "number", false, default);
            if (!result.IsSuccess || result.Value is null)
            {
                _allRooms.Clear();
                ResetFilters();
                LoadError.Text = "Không thể tải tình trạng phòng từ dữ liệu thật: " + (result.Error?.Message ?? "kết quả rỗng.");
                LoadError.Visibility = Visibility.Visible;
                ApplyFilters();
                return;
            }

            _allRooms = result.Value.Items.ToList();
            PopulateFilters();
        }
        catch (Exception ex)
        {
            _allRooms.Clear();
            ResetFilters();
            LoadError.Text = "Không thể tải tình trạng phòng từ dữ liệu thật: " + ex.Message;
            LoadError.Visibility = Visibility.Visible;
        }
        ApplyFilters();
    }

    private void PopulateFilters()
    {
        RoomTypeFilter.ItemsSource = _allRooms.Select(r => r.HotelRoomTypeName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().OrderBy(n => n).ToList();
        FloorFilter.ItemsSource = _allRooms.Select(r => r.Floor).Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().OrderBy(f => f).ToList();
        StatusFilter.ItemsSource = _allRooms.Select(r => r.OperationalStatus.ToString()).Distinct().OrderBy(s => s).ToList();
    }

    private void ApplyFilters()
    {
        var filtered = _allRooms.AsEnumerable();
        if (RoomTypeFilter.SelectedItem is string rt) filtered = filtered.Where(r => r.HotelRoomTypeName == rt);
        if (FloorFilter.SelectedItem is string fl) filtered = filtered.Where(r => r.Floor == fl);
        if (StatusFilter.SelectedItem is string st) filtered = filtered.Where(r => r.OperationalStatus.ToString() == st);
        var list = filtered.ToList();
        RoomList.ItemsSource = list;
        RoomCount.Text = list.Count.ToString();
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object sender, RoutedEventArgs e) { ResetFilters(); ApplyFilters(); }

    private void ResetFilters()
    {
        RoomTypeFilter.SelectedIndex = -1;
        FloorFilter.SelectedIndex = -1;
        StatusFilter.SelectedIndex = -1;
        RoomTypeFilter.ItemsSource = null;
        FloorFilter.ItemsSource = null;
        StatusFilter.ItemsSource = null;
        PopulateFilters();
    }

    private void OnRoomClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: HotelRoomDto room })
            return;

        var details = string.Join(Environment.NewLine,
            $"Loại phòng: {room.HotelRoomTypeName}",
            $"Tầng: {room.Floor ?? "-"}",
            $"Khu: {room.Zone ?? "-"}",
            $"Trạng thái vận hành: {room.OperationalStatus}",
            $"Tình trạng: {room.RoomCondition ?? "-"}",
            $"View: {room.View ?? "-"}",
            $"Giường: {room.BedType ?? "-"}",
            $"Hút thuốc: {(room.IsSmoking ? "Có" : "Không")}",
            $"Phòng liên thông: {(room.IsConnecting ? "Có" : "Không")}",
            $"Đang hoạt động: {(room.IsActive ? "Có" : "Không")} ");

        MessageBox.Show(details, $"Chi tiết phòng {room.RoomNumber}", MessageBoxButton.OK, MessageBoxImage.Information);
        e.Handled = true;
    }
    private void OnRefreshClick(object sender, RoutedEventArgs e) => _ = LoadRoomsAsync();
}
