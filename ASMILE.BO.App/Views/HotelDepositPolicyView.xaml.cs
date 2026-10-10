using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;
namespace ASMILE.BO.App.Views;
public partial class HotelDepositPolicyView : UserControl { public HotelDepositPolicyView() => InitializeComponent(); private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelDepositPolicyViewModel vm) await vm.InitializeAsync(CancellationToken.None); } }
