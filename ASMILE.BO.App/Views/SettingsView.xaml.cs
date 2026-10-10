using System.Windows.Controls;
using System.Windows;
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

    private void OnDatabaseConnectionPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.DatabaseConfigPassword = passwordBox.Password;
        }
    }

    private async void OnSaveDatabaseConfigurationClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.SaveDatabaseConfigurationCommand.ExecuteAsync(null);
            DatabaseConnectionPasswordBox.Clear();
        }
    }

    private void OnDatabaseConnectionEditorVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is SettingsViewModel vm)
        {
            DatabaseConnectionPasswordBox.Clear();
            vm.DatabaseConfigPassword = string.Empty;
        }
    }
}
