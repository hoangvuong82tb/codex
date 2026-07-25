using System.Windows;
namespace ASMILE.BO.App.Views;
public partial class FutureAvailabilityWindow : Window
{
    public FutureAvailabilityWindow() { InitializeComponent(); Owner = System.Windows.Application.Current.MainWindow; }
    private void OnRefreshClick(object sender, RoutedEventArgs e) { }
}
