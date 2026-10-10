using System.Windows;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class WebReservationWindow : Window
{
    public WebReservationWindow(ReservationWorkspaceViewModel workspace)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        DataContext = workspace;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ReservationWorkspaceViewModel workspace)
            _ = workspace.RefreshAsync();
    }
}
