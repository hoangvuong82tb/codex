using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelExchangeRateView : UserControl
{
    public HotelExchangeRateView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e)
    { if (DataContext is HotelExchangeRateViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
