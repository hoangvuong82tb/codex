using System.Windows;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class KeyManagementWindow : Window
{
    public KeyManagementWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnIssueKey(object s, MouseButtonEventArgs e) => Msg("Cấp thẻ");
    private void OnReissueKey(object s, MouseButtonEventArgs e) => Msg("Cấp lại");
    private void OnRevokeKey(object s, MouseButtonEventArgs e) => Msg("Thu hồi");
    private void OnLockKey(object s, MouseButtonEventArgs e) => Msg("Khóa thẻ");
    private void OnKeyLog(object s, MouseButtonEventArgs e) => Msg("Nhật ký thẻ");
    private void Msg(string a) => MessageBox.Show($"Mockup: {a}. Chưa kết nối backend.", "Quản lý chìa khóa", MessageBoxButton.OK, MessageBoxImage.Information);
    private void OnClose(object s, RoutedEventArgs e) => Close();
}
