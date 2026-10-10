using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class HotelMarketSegmentProfileView : UserControl
{
    public HotelMarketSegmentProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelMarketSegmentProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
