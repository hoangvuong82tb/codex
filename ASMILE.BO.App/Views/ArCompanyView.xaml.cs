using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class ArCompanyView : UserControl
{
    public ArCompanyView() => InitializeComponent();

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ArCompanyViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is ArCompanyDto dto
            && DataContext is ArCompanyViewModel vm)
            vm.SelectItemCommand.Execute(dto);
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ArCompanyViewModel vm && vm.SelectedItem is not null)
            vm.IsDetailOpen = true;
    }

    private void OnDetailTabClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is string tag
            && int.TryParse(tag, out var tab) && DataContext is ArCompanyViewModel vm)
            vm.SelectedDetailTab = tab;
    }

    private void OnDismissInfo(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ArCompanyViewModel vm)
            vm.InfoMessage = string.Empty;
    }
}