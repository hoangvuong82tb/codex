using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class ExtendStayWindow : Window
{
    public ExtendStayWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }
    private void OnConfirm(object s, RoutedEventArgs e) => Close();
    private void OnClose(object s, RoutedEventArgs e) => Close();
}
