using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class HotelBookingSourceProfileView : UserControl
{
    public HotelBookingSourceProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelBookingSourceProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
