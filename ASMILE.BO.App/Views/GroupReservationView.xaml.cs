using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class GroupReservationView : UserControl
{
    public GroupReservationView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.GroupReservationViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnGroupSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.GroupReservationViewModel vm || sender is not DataGrid grid)
            return;

        if (grid.SelectedItem is ViewModels.GroupListDto group)
            vm.SelectGroupCommand.Execute(group);
        else
            vm.CloseDetailCommand.Execute(null);
    }

    private void OnGroupDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // The selected group is already displayed in the real workspace.
        // Do not present a fabricated workspace-success dialog.
        e.Handled = true;
    }

    private void OnTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tagStr || !int.TryParse(tagStr, out var idx)) return;

        // Update tab button styles
        var tabs = new[] { TabOverview, TabRooms, TabGuests, TabFinance, TabNotes };
        var panels = new[] { PreviewTab0, PreviewTab1, PreviewTab2, PreviewTab3, PreviewTab4 };

        for (int i = 0; i < tabs.Length; i++)
        {
            if (i == idx)
            {
                tabs[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F3FF"));
                tabs[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED"));
                tabs[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D28D9"));
                panels[i].Visibility = Visibility.Visible;
            }
            else
            {
                tabs[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                tabs[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                tabs[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                panels[i].Visibility = Visibility.Collapsed;
            }
        }

        if (DataContext is ViewModels.GroupReservationViewModel vm)
            vm.SelectedTabIndex = idx;
    }
}
