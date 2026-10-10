using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class ArReconcileView : UserControl
{
    public ArReconcileView() => InitializeComponent();

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ArReconcileViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ArReconcileViewModel vm && vm.SelectedItem is not null)
            vm.IsDetailOpen = true;
    }

    private void OnDismissInfo(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ArReconcileViewModel vm)
            vm.InfoMessage = string.Empty;
    }
}