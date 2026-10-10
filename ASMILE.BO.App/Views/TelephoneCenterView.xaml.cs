using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class TelephoneCenterView : UserControl
{
    public TelephoneCenterView() => InitializeComponent();

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is TelephoneCenterViewModel vm) await vm.InitializeAsync();
    }
}
