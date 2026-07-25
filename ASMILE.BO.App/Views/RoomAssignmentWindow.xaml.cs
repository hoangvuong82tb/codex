using ASMILE.BO.App.ViewModels;
using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class RoomAssignmentWindow : Window
{
    private readonly ReservationWorkspaceViewModel _vm;

    public RoomAssignmentWindow(ReservationWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _vm = viewModel;
        DataContext = viewModel;
    }

    private async void OnLoadRoomsClick(object sender, RoutedEventArgs e)
    {
        await _vm.OpenRoomPanelAsync();
    }

    private async void OnAssignClick(object sender, RoutedEventArgs e)
    {
        await _vm.AssignRoomAsync();
        if (!_vm.HasError) Close();
    }

    private async void OnUnassignClick(object sender, RoutedEventArgs e)
    {
        await _vm.UnassignRoomAsync();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
