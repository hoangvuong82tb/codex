using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is ListBox listBox && listBox.SelectedItem is SettingsCategoryItem item)
        {
            vm.SelectCategoryCommand.Execute(item);
        }
    }

    private void OnCategoryMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is ListBox listBox && listBox.SelectedItem is SettingsCategoryItem item)
        {
            vm.SelectCategoryCommand.Execute(item);
        }
    }

    private void OnTopSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is SettingsViewModel vm)
        {
            vm.SearchCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void OnSettingRowClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm || sender is not Button { DataContext: SettingItemViewModel item })
        {
            return;
        }

        await vm.NavigateToSettingsDetail(item);
        e.Handled = true;
    }
}
