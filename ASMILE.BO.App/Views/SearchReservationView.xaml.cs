using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class SearchReservationView : UserControl
{
    public SearchReservationView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        if (DataContext is ReservationWorkspaceViewModel vm)
            _ = vm.RefreshAsync();
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Detail panel updates via SelectedReservation binding
    }

    private void OnViewDetailClick(object sender, RoutedEventArgs e)
    {
        // Placeholder for future detail view
    }

    private void OnSelectClick(object sender, RoutedEventArgs e)
    {
        // Placeholder — returns selected reservation context
    }
}
