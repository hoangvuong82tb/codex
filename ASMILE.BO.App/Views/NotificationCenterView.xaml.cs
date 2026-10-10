using System.Windows.Controls; using System.Windows.Input;
namespace ASMILE.BO.App.Views;
public partial class NotificationCenterView : UserControl
{
    public NotificationCenterView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is ViewModels.NotificationCenterViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
    private void OnClick(object sender, MouseButtonEventArgs e) { if (sender is Border b && b.Tag is ViewModels.AlertNotificationDto dto && DataContext is ViewModels.NotificationCenterViewModel vm) vm.SelectCommand.Execute(dto); }
}
