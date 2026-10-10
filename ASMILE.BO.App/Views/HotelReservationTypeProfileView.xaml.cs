using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class HotelReservationTypeProfileView : UserControl
{
    public HotelReservationTypeProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelReservationTypeProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
