using ASMILE.BO.Application.DTOs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ASMILE.BO.App.Views;

public partial class StockReceiptDetailWindow : Window
{
    private readonly StockReceiptDto _receipt;
    private FixedDocument? _printDocument;

    public StockReceiptDetailWindow(StockReceiptDto receipt)
    {
        InitializeComponent();
        _receipt = receipt;
        DataContext = _receipt;
    }

    private void PreviewPrintButton_Click(object sender, RoutedEventArgs e)
    {
        _printDocument = BuildPreviewDocument();
        OverlayPreviewViewer.Document = _printDocument;
        PrintPreviewOverlay.Visibility = Visibility.Visible;
    }

    private void ClosePreviewOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        PrintPreviewOverlay.Visibility = Visibility.Collapsed;
    }

    private void OverlayPrintButton_Click(object sender, RoutedEventArgs e)
    {
        if (_printDocument is null)
        {
            _printDocument = BuildPreviewDocument();
        }

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
        {
            return;
        }

        printDialog.PrintDocument(_printDocument.DocumentPaginator, $"Phiếu nhập {_receipt.ReceiptNo}");
    }

    private FixedDocument BuildPreviewDocument()
    {
        return StockReceiptPrintDocumentFactory.Build(_receipt);
    }
}
