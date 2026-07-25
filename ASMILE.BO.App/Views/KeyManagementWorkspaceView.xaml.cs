using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class KeyManagementWorkspaceView : UserControl
{
    public KeyManagementWorkspaceView() => InitializeComponent();
    private void OnIssue(object s, MouseButtonEventArgs e) => M("Cấp thẻ");
    private void OnReissue(object s, MouseButtonEventArgs e) => M("Cấp lại");
    private void OnRevoke(object s, MouseButtonEventArgs e) => M("Thu hồi");
    private void OnLock(object s, MouseButtonEventArgs e) => M("Khóa thẻ");
    private void OnLog(object s, MouseButtonEventArgs e) => M("Nhật ký thẻ");
    private static void M(string a) => MessageBox.Show($"Mockup: {a}. Chưa kết nối backend.", "Quản lý chìa khóa", MessageBoxButton.OK, MessageBoxImage.Information);
}
