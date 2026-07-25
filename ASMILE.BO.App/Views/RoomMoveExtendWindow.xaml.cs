using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class RoomMoveExtendWindow : Window
{
    public RoomMoveExtendWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }
    private void OnConfirm(object s, RoutedEventArgs e) => Close();
    private void OnClose(object s, RoutedEventArgs e) => Close();
}
