using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;
namespace ASMILE.BO.App.Views;
public partial class HotelRoomMoveUpgradeRuleView : UserControl
{
    public HotelRoomMoveUpgradeRuleView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e) { if (DataContext is HotelRoomMoveUpgradeRuleViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
