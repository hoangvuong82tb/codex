using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public sealed class RoomLockSelectionDialog : Window
{
    private readonly ComboBox _room = new();

    public HotelRoomDto? SelectedRoom => _room.SelectedItem as HotelRoomDto;

    public RoomLockSelectionDialog(IReadOnlyList<HotelRoomDto> rooms)
    {
        Title = "Chọn phòng cần khóa";
        Width = 460;
        Height = 190;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        _room.ItemsSource = rooms;
        _room.DisplayMemberPath = nameof(HotelRoomDto.RoomNumber);
        _room.SelectedIndex = 0;
        _room.Margin = new Thickness(0, 8, 0, 16);

        var ok = new Button { Content = "Tiếp tục", Width = 86, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        ok.Click += (_, _) => { DialogResult = true; Close(); };
        var cancel = new Button { Content = "Hủy", Width = 86, IsCancel = true };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = "Phòng đang khai thác", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(_room);
        panel.Children.Add(buttons);
        Content = panel;
    }
}
