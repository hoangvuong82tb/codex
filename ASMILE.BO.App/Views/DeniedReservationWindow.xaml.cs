using System.Windows;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class DeniedReservationWindow : Window
{
    public DeniedReservationWindow(DeniedReservationViewModel viewModel)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        DataContext = viewModel;
    }

}
