using ASMILE.BO.App.ViewModels;
using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class SearchReservationWindow : Window
{
    public SearchReservationWindow(ReservationWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var view = new SearchReservationView();
        view.DataContext = viewModel;
        Host.Content = view;
    }
}
