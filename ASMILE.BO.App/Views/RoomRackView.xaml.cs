using System.Windows.Controls;
namespace ASMILE.BO.App.Views;
public partial class RoomRackView : UserControl
{
    public RoomRackView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.RoomRackViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }
    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (DataContext is ViewModels.RoomRackViewModel vm) vm.HandleKey(e.Key);
    }
}
