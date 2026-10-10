using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelPaymentMethodProfileView : UserControl
{
    public HotelPaymentMethodProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is HotelPaymentMethodProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
