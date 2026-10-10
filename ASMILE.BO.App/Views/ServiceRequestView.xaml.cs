using System.Windows.Controls;
using System.Windows.Input;
namespace ASMILE.BO.App.Views;
public partial class ServiceRequestView : UserControl
{
    public ServiceRequestView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is ViewModels.ServiceRequestViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
    private void OnRequestClick(object sender, MouseButtonEventArgs e)
    { if (sender is Border b && b.Tag is ViewModels.ServiceRequestDto dto && DataContext is ViewModels.ServiceRequestViewModel vm) vm.SelectRequestCommand.Execute(dto); }
}
