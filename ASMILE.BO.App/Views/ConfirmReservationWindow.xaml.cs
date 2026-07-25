using ASMILE.BO.App.ViewModels;
using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class ConfirmReservationWindow : Window
{
    private readonly ReservationWorkspaceViewModel _vm;

    public ConfirmReservationWindow(ReservationWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _vm = viewModel;
        DataContext = viewModel;
    }

    private async void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        await _vm.ConfirmAsync();
    }

    private async void OnTentativeClick(object sender, RoutedEventArgs e)
    {
        await _vm.TentativeAsync();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
