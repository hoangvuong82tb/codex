using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HousekeepingSupplyView : UserControl
{
    public HousekeepingSupplyView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HousekeepingSupplyViewModel vm)
            await vm.InitializeAsync(CancellationToken.None);
    }
}
