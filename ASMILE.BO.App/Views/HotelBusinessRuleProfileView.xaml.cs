using System.Windows; using System.Windows.Controls; using ASMILE.BO.App.ViewModels;
namespace ASMILE.BO.App.Views;
public partial class HotelBusinessRuleProfileView : UserControl { public HotelBusinessRuleProfileView()=>InitializeComponent(); private async void OnLoaded(object s,RoutedEventArgs e){if(DataContext is HotelBusinessRuleProfileViewModel vm)await vm.InitializeAsync(CancellationToken.None);} }
