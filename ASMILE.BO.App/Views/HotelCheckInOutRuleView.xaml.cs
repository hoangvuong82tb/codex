using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;
namespace ASMILE.BO.App.Views;
public partial class HotelCheckInOutRuleView : UserControl
{
    public HotelCheckInOutRuleView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e) { if (DataContext is HotelCheckInOutRuleViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
