using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class DeniedReservationWindow : Window
{
    public DeniedReservationWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e) { }
}
