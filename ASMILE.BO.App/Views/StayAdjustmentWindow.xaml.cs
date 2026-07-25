using System.Windows;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class StayAdjustmentWindow : Window
{
    public StayAdjustmentWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnRoomMove(object sender, MouseButtonEventArgs e) => Msg("Đổi phòng");
    private void OnExtendStay(object sender, MouseButtonEventArgs e) => Msg("Gia hạn lưu trú");
    private void OnMoveAndExtend(object sender, MouseButtonEventArgs e) => Msg("Đổi phòng + Gia hạn");
    private void OnModifyInfo(object sender, MouseButtonEventArgs e) => Msg("Điều chỉnh thông tin lưu trú");
    private void Msg(string action) => MessageBox.Show($"Mockup: {action}. Chưa kết nối backend.", "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Information);
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
