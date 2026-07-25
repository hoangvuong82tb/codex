using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class ReservationWorkspaceView : UserControl
{
    public ReservationWorkspaceView() => InitializeComponent();

    private bool _loaded;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded || DataContext is not ReservationWorkspaceViewModel viewModel) return;
        _loaded = true;
        await viewModel.LoadAsync();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && DataContext is ReservationWorkspaceViewModel viewModel)
        {
            viewModel.CancelEditCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ReservationWorkspaceViewModel viewModel && viewModel.SelectedReservation is not null && viewModel.CanEditDetails)
            viewModel.NewCommand.Execute(null);
    }
}
