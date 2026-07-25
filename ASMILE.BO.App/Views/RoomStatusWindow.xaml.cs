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
        try
        {
            var result = await _hotelQueries.SearchRooms(null, null, null, null, null, true, new(1, 500), "number", false, default);
            if (result.IsSuccess && result.Value is not null)
            {
                _allRooms = result.Value.Items.ToList();
                PopulateFilters();
            }
        }
        catch { }
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
    private void OnClearFilter(object sender, RoutedEventArgs e) { RoomTypeFilter.SelectedIndex = -1; FloorFilter.SelectedIndex = -1; StatusFilter.SelectedIndex = -1; ApplyFilters(); }
    private void OnRoomClick(object sender, MouseButtonEventArgs e) { }
    private void OnRefreshClick(object sender, RoutedEventArgs e) => _ = LoadRoomsAsync();
}
