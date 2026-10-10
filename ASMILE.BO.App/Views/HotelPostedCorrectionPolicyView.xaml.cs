using System.Windows; using System.Windows.Controls; using ASMILE.BO.App.ViewModels;
namespace ASMILE.BO.App.Views;
public partial class HotelPostedCorrectionPolicyView : UserControl { public HotelPostedCorrectionPolicyView()=>InitializeComponent(); private async void OnLoaded(object s,RoutedEventArgs e){if(DataContext is HotelPostedCorrectionPolicyViewModel vm)await vm.InitializeAsync(CancellationToken.None);} }
