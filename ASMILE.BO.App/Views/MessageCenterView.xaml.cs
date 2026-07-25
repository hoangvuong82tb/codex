using System.Windows.Controls; using System.Windows.Input;
namespace ASMILE.BO.App.Views;
public partial class MessageCenterView : UserControl
{
    public MessageCenterView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is ViewModels.MessageCenterViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
    private void OnClick(object sender, MouseButtonEventArgs e) { if (sender is Border b && b.Tag is ViewModels.MessageDto dto && DataContext is ViewModels.MessageCenterViewModel vm) vm.SelectMessageCommand.Execute(dto); }
}
