using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class FrontOfficeLogWorkspaceView : UserControl
{
    public FrontOfficeLogWorkspaceView() => InitializeComponent();
    private void OnShiftLog(object s, MouseButtonEventArgs e) => M("Nhật ký ca");
    private void OnIncident(object s, MouseButtonEventArgs e) => M("Nhật ký sự cố");
    private void OnLostFound(object s, MouseButtonEventArgs e) => M("Đồ thất lạc / Tìm thấy");
    private void OnVipNote(object s, MouseButtonEventArgs e) => M("Ghi chú VIP");
    private void OnHandover(object s, MouseButtonEventArgs e) => M("Bàn giao ca");
    private static void M(string a) => MessageBox.Show($"Mockup: {a}. Chưa kết nối backend.", "Nhật ký FO", MessageBoxButton.OK, MessageBoxImage.Information);
}
