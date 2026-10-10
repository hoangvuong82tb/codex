using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelPartnerProfileView : UserControl
{
    public HotelPartnerProfileView() { InitializeComponent(); }
    private async void OnLoaded(object sender, RoutedEventArgs e) { if (DataContext is HotelPartnerProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
