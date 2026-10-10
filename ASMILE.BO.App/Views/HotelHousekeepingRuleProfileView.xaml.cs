using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;
namespace ASMILE.BO.App.Views;
public partial class HotelHousekeepingRuleProfileView : UserControl { public HotelHousekeepingRuleProfileView() => InitializeComponent(); private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelHousekeepingRuleProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); } }
