using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelRoomStatusRuleView : UserControl
{
    public HotelRoomStatusRuleView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e)
    { if (DataContext is HotelRoomStatusRuleViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
