using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class HotelGuestPreferenceProfileView : UserControl
{
    public HotelGuestPreferenceProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelGuestPreferenceProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
