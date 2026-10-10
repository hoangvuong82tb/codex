using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class HotelHousekeepingSupplyCatalogView : UserControl
{
    public HotelHousekeepingSupplyCatalogView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.HotelHousekeepingSupplyCatalogViewModel vm)
            await vm.InitializeAsync(CancellationToken.None);
    }
}
