using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;
namespace ASMILE.BO.App.Views;
public partial class HotelComplaintProfileView : UserControl { public HotelComplaintProfileView() => InitializeComponent(); private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelComplaintProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); } }
