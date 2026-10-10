using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;

namespace ASMILE.BO.App.Views;

/// <summary>Property-scoped profile editor for an existing room.</summary>
public sealed class UpdateHotelRoomDialog : Window
{
    private readonly TextBox _displayName = new();
    private readonly TextBox _floor = new();
    private readonly TextBox _building = new();
    private readonly TextBox _zone = new();
    private readonly TextBox _roomCondition = new();
    private readonly TextBox _view = new();
    private readonly TextBox _bedType = new();
    private readonly CheckBox _smoking = new() { Content = "Cho phép hút thuốc", VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _connecting = new() { Content = "Phòng connecting", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(18, 0, 0, 0) };

    public UpdateHotelRoomDialog(HotelRoomDto room)
    {
        Title = $"Sửa hồ sơ phòng {room.RoomNumber}";
        Width = 500;
        Height = 560;
        MinWidth = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        _displayName.Text = room.DisplayName ?? string.Empty;
        _floor.Text = room.Floor ?? string.Empty;
        _building.Text = room.Building ?? string.Empty;
        _zone.Text = room.Zone ?? string.Empty;
        _roomCondition.Text = room.RoomCondition ?? string.Empty;
        _view.Text = room.View ?? string.Empty;
        _bedType.Text = room.BedType ?? string.Empty;
        _smoking.IsChecked = room.IsSmoking;
        _connecting.IsChecked = room.IsConnecting;

        var grid = new Grid { Margin = new Thickness(24) };
        for (var i = 0; i < 11; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = i == 10 ? GridLength.Auto : new GridLength(52) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var roomNumber = new TextBlock
        {
            Text = $"Số phòng: {room.RoomNumber}",
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.DarkSlateGray,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(roomNumber, 0);
        Grid.SetColumn(roomNumber, 0);
        grid.Children.Add(roomNumber);
        var type = new TextBlock
        {
            Text = $"{room.HotelRoomTypeCode} · {room.HotelRoomTypeName}",
            Foreground = System.Windows.Media.Brushes.SlateGray,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(type, 0);
        Grid.SetColumn(type, 1);
        grid.Children.Add(type);

        AddField(grid, 1, "Tên hiển thị", _displayName);
        AddField(grid, 2, "Tầng", _floor);
        AddField(grid, 3, "Tòa nhà", _building);
        AddField(grid, 4, "Khu vực", _zone);
        AddField(grid, 5, "Tình trạng", _roomCondition);
        AddField(grid, 6, "Hướng nhìn", _view);
        AddField(grid, 7, "Loại giường", _bedType);
        var flags = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        flags.Children.Add(_smoking);
        flags.Children.Add(_connecting);
        AddField(grid, 8, "Tùy chọn", flags);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };
        var cancel = new Button { Content = "Hủy", MinWidth = 90, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
        cancel.Click += (_, _) => DialogResult = false;
        var save = new Button { Content = "Lưu thay đổi", MinWidth = 110, Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
        save.Click += (_, _) => DialogResult = true;
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        Grid.SetRow(buttons, 10);
        Grid.SetColumn(buttons, 1);
        grid.Children.Add(buttons);

        Content = grid;
        Loaded += (_, _) => _displayName.Focus();
    }

    public string? DisplayName => NullIfEmpty(_displayName.Text);
    public string? Floor => NullIfEmpty(_floor.Text);
    public string? Building => NullIfEmpty(_building.Text);
    public string? Zone => NullIfEmpty(_zone.Text);
    public string? RoomCondition => NullIfEmpty(_roomCondition.Text);
    public string? View => NullIfEmpty(_view.Text);
    public string? BedType => NullIfEmpty(_bedType.Text);
    public bool IsSmoking => _smoking.IsChecked == true;
    public bool IsConnecting => _connecting.IsChecked == true;

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
