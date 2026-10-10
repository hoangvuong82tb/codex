using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class HotelGuestRequestProfileView : UserControl
{
    public HotelGuestRequestProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelGuestRequestProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
