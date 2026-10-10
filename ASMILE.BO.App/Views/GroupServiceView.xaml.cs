using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class GroupServiceView : UserControl
{
    public GroupServiceView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.GroupServiceViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnServiceSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.GroupServiceViewModel vm || sender is not DataGrid grid)
            return;

        vm.UpdateSelectedServices(grid.SelectedItems.OfType<ViewModels.GroupServiceDto>());
        if (grid.SelectedItem is ViewModels.GroupServiceDto svc)
            vm.SelectServiceCommand.Execute(svc);
        else
            vm.CloseDetailCommand.Execute(null);
    }

    private void OnServiceDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Service details are already rendered in the real bound inspector.
        e.Handled = true;
    }

    private void OnInspectorTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tagStr || !int.TryParse(tagStr, out var idx)) return;

        var tabs = new[] { TabOverview, TabSchedule, TabCharge, TabGuest, TabHistory, TabRemark };
        var panels = new[] { InspectorTab0, InspectorTab1, InspectorTab2, InspectorTab3, InspectorTab4, InspectorTab5 };

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

        if (DataContext is ViewModels.GroupServiceViewModel vm)
            vm.SelectedTabIndex = idx;

        // Toggle category-specific fields in Overview tab based on selected service
        UpdateCategoryFields();
    }

    private void UpdateCategoryFields()
    {
        if (DataContext is not ViewModels.GroupServiceViewModel vm || vm.SelectedService is null) return;

        RestaurantFields.Visibility = vm.SelectedService.Category == "Restaurant" ? Visibility.Visible : Visibility.Collapsed;
        MeetingFields.Visibility = vm.SelectedService.Category == "Meeting" ? Visibility.Visible : Visibility.Collapsed;
        TransportFields.Visibility = vm.SelectedService.Category == "Transport" ? Visibility.Visible : Visibility.Collapsed;
        SpaFields.Visibility = vm.SelectedService.Category == "Spa" ? Visibility.Visible : Visibility.Collapsed;
        LaundryFields.Visibility = vm.SelectedService.Category == "Laundry" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnDismissInfo(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.GroupServiceViewModel vm)
            vm.InfoMessage = string.Empty;
    }
}
