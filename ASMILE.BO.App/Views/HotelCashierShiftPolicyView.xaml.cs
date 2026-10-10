using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelCashierShiftPolicyView : UserControl
{
    public HotelCashierShiftPolicyView() { InitializeComponent(); }
    private async void OnLoaded(object sender, RoutedEventArgs e) { if (DataContext is HotelCashierShiftPolicyViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
