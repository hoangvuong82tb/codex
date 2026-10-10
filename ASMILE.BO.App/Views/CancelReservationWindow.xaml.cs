using ASMILE.BO.App.ViewModels;
using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class CancelReservationWindow : Window
{
    private readonly ReservationWorkspaceViewModel _vm;

    public CancelReservationWindow(ReservationWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _vm = viewModel;
        DataContext = viewModel;
    }

    private async void OnCancelClick(object sender, RoutedEventArgs e)
    {
        var reason = CancelReason.Text?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            _vm.ErrorMessage = "Vui lòng nhập lý do hủy.";
            return;
        }
        await _vm.CancelReservationAsync();
    }
}
