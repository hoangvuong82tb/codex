using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

/// <summary>Small real-data status selector for Room Master mutation.</summary>
public sealed class RoomOperationalStatusDialog : Window
{
    private readonly ComboBox _status = new();

    public HotelRoomOperationalStatus? SelectedStatus
        => _status.SelectedItem is HotelRoomOperationalStatus value ? value : null;

    public RoomOperationalStatusDialog(string currentFoStatus)
    {
        Title = "Đổi trạng thái phòng";
        Width = 360;
        Height = 190;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        _status.ItemsSource = Enum.GetValues<HotelRoomOperationalStatus>();
        _status.SelectedItem = currentFoStatus switch
        {
            "OOO" => HotelRoomOperationalStatus.OutOfOrder,
            "OOS" => HotelRoomOperationalStatus.OutOfService,
            _ => HotelRoomOperationalStatus.Available
        };
        _status.Margin = new Thickness(0, 8, 0, 16);

        var ok = new Button { Content = "Lưu", Width = 86, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        ok.Click += (_, _) => { DialogResult = true; Close(); };
        var cancel = new Button { Content = "Hủy", Width = 86, IsCancel = true };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = "Trạng thái vận hành", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(_status);
        panel.Children.Add(buttons);
        Content = panel;
    }
}
