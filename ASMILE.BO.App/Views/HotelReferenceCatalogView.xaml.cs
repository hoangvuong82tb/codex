using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelReferenceCatalogView : UserControl
{
    public HotelReferenceCatalogView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e)
    { if (DataContext is HotelReferenceCatalogViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
