using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class StockReceiptPrintPreviewModuleView : UserControl
{
    public StockReceiptPrintPreviewModuleView()
    {
        InitializeComponent();
    }

    private void PrintButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not StockReceiptPrintPreviewModuleViewModel vm)
        {
            return;
        }

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
        {
            return;
        }

        printDialog.PrintDocument(vm.PreviewDocument.DocumentPaginator, vm.WindowTitle);
    }
}
