using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;
namespace ASMILE.BO.App.Views;
public partial class HotelDiscountPolicyView : UserControl { public HotelDiscountPolicyView() => InitializeComponent(); private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelDiscountPolicyViewModel vm) await vm.InitializeAsync(CancellationToken.None); } }
