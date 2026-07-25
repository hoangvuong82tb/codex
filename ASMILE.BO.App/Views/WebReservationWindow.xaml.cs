using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class WebReservationWindow : Window
{
    public WebReservationWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        // No backend data source available
    }
}
