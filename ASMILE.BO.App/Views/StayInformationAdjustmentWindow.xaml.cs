using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class StayInformationAdjustmentWindow : Window
{
    public StayInformationAdjustmentWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }
    private void OnTabClick(object s, RoutedEventArgs e) { } // mockup
    private void OnConfirm(object s, RoutedEventArgs e) => Close();
    private void OnClose(object s, RoutedEventArgs e) => Close();
}
