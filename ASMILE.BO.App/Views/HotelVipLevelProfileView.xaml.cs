using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelVipLevelProfileView : UserControl
{
    public HotelVipLevelProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelVipLevelProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
