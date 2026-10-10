using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;
namespace ASMILE.BO.App.Views;
public partial class HotelWakeupRuleProfileView : UserControl { public HotelWakeupRuleProfileView() => InitializeComponent(); private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelWakeupRuleProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); } }
