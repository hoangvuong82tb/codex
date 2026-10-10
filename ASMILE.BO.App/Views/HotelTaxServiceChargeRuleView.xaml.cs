using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelTaxServiceChargeRuleView : UserControl
{
    public HotelTaxServiceChargeRuleView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is HotelTaxServiceChargeRuleViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
