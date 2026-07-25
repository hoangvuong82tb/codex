using System.Windows;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class FrontOfficeLogWindow : Window
{
    public FrontOfficeLogWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnShiftLog(object s, MouseButtonEventArgs e) => Msg("Nhật ký ca");
    private void OnIncidentLog(object s, MouseButtonEventArgs e) => Msg("Nhật ký sự cố");
    private void OnLostFound(object s, MouseButtonEventArgs e) => Msg("Lost & Found");
    private void OnVipNote(object s, MouseButtonEventArgs e) => Msg("VIP Note");
    private void OnShiftHandover(object s, MouseButtonEventArgs e) => Msg("Bàn giao ca");
    private void Msg(string a) => MessageBox.Show($"Mockup: {a}. Chưa kết nối backend.", "Nhật ký FO", MessageBoxButton.OK, MessageBoxImage.Information);
    private void OnClose(object s, RoutedEventArgs e) => Close();
}
