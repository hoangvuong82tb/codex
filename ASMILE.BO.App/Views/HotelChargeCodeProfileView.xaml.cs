using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;
namespace ASMILE.BO.App.Views;
public partial class HotelChargeCodeProfileView : UserControl { public HotelChargeCodeProfileView() => InitializeComponent(); private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelChargeCodeProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); } }
