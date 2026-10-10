using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;

namespace ASMILE.BO.App.Views;

/// <summary>
/// Minimal data-entry dialog for creating a hotel room. The room type list is
/// supplied by the real property-scoped query; this dialog never invents a
/// type or writes directly to the database.
/// </summary>
public sealed class CreateHotelRoomDialog : Window
{
    private readonly TextBox _roomNumber = new();
    private readonly TextBox _displayName = new();
    private readonly TextBox _floor = new();
    private readonly TextBox _building = new();
    private readonly TextBox _zone = new();
    private readonly TextBox _roomCondition = new();
    private readonly TextBox _view = new();
    private readonly TextBox _bedType = new();
    private readonly CheckBox _smoking = new() { Content = "Cho phép hút thuốc", VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _connecting = new() { Content = "Phòng connecting", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(18, 0, 0, 0) };
    private readonly ComboBox _roomType = new();

    public CreateHotelRoomDialog(IEnumerable<HotelRoomTypeDto> roomTypes)
    {
        Title = "Thêm phòng";
        Width = 520;
        Height = 650;
        MinWidth = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var types = roomTypes.ToList();
        _roomType.ItemsSource = types;
        _roomType.DisplayMemberPath = nameof(HotelRoomTypeDto.Name);
        _roomType.SelectedIndex = 0;

        var grid = new Grid { Margin = new Thickness(24) };
        for (var i = 0; i < 11; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = i == 10 ? GridLength.Auto : new GridLength(52) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        AddField(grid, 0, "Số phòng *", _roomNumber);
        AddField(grid, 1, "Tên hiển thị", _displayName);
        AddField(grid, 2, "Loại phòng *", _roomType);
        AddField(grid, 3, "Tầng", _floor);
        AddField(grid, 4, "Tòa nhà", _building);
        AddField(grid, 5, "Khu vực", _zone);
        AddField(grid, 6, "Tình trạng", _roomCondition);
        AddField(grid, 7, "Hướng nhìn", _view);
        AddField(grid, 8, "Loại giường", _bedType);
        var flags = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        flags.Children.Add(_smoking);
        flags.Children.Add(_connecting);
        AddField(grid, 9, "Tùy chọn", flags);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };
        var cancel = new Button { Content = "Hủy", MinWidth = 90, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
        cancel.Click += (_, _) => { DialogResult = false; };
        var save = new Button { Content = "Lưu phòng", MinWidth = 110, Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
        save.Click += OnSaveClick;
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        Grid.SetRow(buttons, 10);
        Grid.SetColumn(buttons, 1);
        grid.Children.Add(buttons);

        Content = grid;
        Loaded += (_, _) => _roomNumber.Focus();
    }

    public string RoomNumber => _roomNumber.Text.Trim();
    public string? DisplayName => NullIfEmpty(_displayName.Text);
    public string? Floor => NullIfEmpty(_floor.Text);
    public string? Building => NullIfEmpty(_building.Text);
    public string? Zone => NullIfEmpty(_zone.Text);
    public string? RoomCondition => NullIfEmpty(_roomCondition.Text);
    public string? View => NullIfEmpty(_view.Text);
    public string? BedType => NullIfEmpty(_bedType.Text);
    public bool IsSmoking => _smoking.IsChecked == true;
    public bool IsConnecting => _connecting.IsChecked == true;
    public HotelRoomTypeDto? SelectedRoomType => _roomType.SelectedItem as HotelRoomTypeDto;

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RoomNumber))
        {
            MessageBox.Show("Vui lòng nhập số phòng.", "Thêm phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            _roomNumber.Focus();
            return;
        }

        if (SelectedRoomType is null)
        {
            MessageBox.Show("Chưa có loại phòng hoạt động để chọn.", "Thêm phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddField(Grid grid, int row, string label, FrameworkElement control)
    {
        var labelBlock = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.DarkSlateGray
        };
        Grid.SetRow(labelBlock, row);
        Grid.SetColumn(labelBlock, 0);
        grid.Children.Add(labelBlock);

        control.Margin = new Thickness(0, 4, 0, 4);
        if (control is TextBox textBox)
        {
            textBox.Padding = new Thickness(8, 6, 8, 6);
            textBox.VerticalContentAlignment = VerticalAlignment.Center;
        }
        Grid.SetRow(control, row);
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
    }
}
