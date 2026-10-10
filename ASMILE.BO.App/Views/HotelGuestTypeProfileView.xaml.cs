using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelGuestTypeProfileView : UserControl
{
    public HotelGuestTypeProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelGuestTypeProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
