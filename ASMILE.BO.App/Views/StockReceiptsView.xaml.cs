using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;
using System.Text;

namespace ASMILE.BO.App.Views;

public partial class StockReceiptsView : UserControl
{
    private StockReceiptDto? _currentDetailReceipt;
    private FixedDocument? _currentPreviewDocument;
    private bool _isDraggingReceiptWindow;
    private Point _dragStartPoint;
    private double _startTransformX;
    private double _startTransformY;

    public StockReceiptsView()
    {
        InitializeComponent();
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not StockReceiptsViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedReceipt is not StockReceiptDto selectedReceipt)
        {
            return;
        }

        var itemNameById = viewModel.ItemLookup
            .Where(x => x.Id > 0)
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First().Name);
        var itemCodeById = viewModel.ItemLookup
            .Where(x => x.Id > 0)
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First().Code);
        var warehouseCodeById = viewModel.WarehouseLookup
            .Where(x => x.Id > 0)
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First().Code);

        var popupReceipt = new StockReceiptDto
        {
            Id = selectedReceipt.Id,
            ReceiptNo = selectedReceipt.ReceiptNo,
            ReceiptDate = selectedReceipt.ReceiptDate,
            SupplierCode = selectedReceipt.SupplierCode,
            SupplierName = selectedReceipt.SupplierName,
            ReceiverName = selectedReceipt.ReceiverName,
            Description = selectedReceipt.Description,
            WarehouseId = selectedReceipt.WarehouseId,
            WarehouseCode = selectedReceipt.WarehouseCode,
            DebitAccountCode = selectedReceipt.DebitAccountCode,
            CreditAccountCode = selectedReceipt.CreditAccountCode,
            TotalAmount = selectedReceipt.TotalAmount,
            IsApproved = selectedReceipt.IsApproved,
            Details = selectedReceipt.Details.Select(d => new StockTransactionDetailDto
            {
                Id = d.Id,
                LineNo = d.LineNo,
                ItemId = d.ItemId,
                ItemCode = string.IsNullOrWhiteSpace(d.ItemCode) && itemCodeById.TryGetValue(d.ItemId, out var itemCode)
                    ? itemCode
                    : d.ItemCode,
                ItemName = string.IsNullOrWhiteSpace(d.ItemName) && itemNameById.TryGetValue(d.ItemId, out var itemName)
                    ? itemName
                    : d.ItemName,
                Unit = d.Unit,
                WarehouseId = d.WarehouseId,
                WarehouseCode = d.WarehouseId.HasValue && warehouseCodeById.TryGetValue(d.WarehouseId.Value, out var warehouseCode)
                    ? warehouseCode
                    : string.Empty,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Amount = d.Amount,
                VatRate = d.VatRate,
                VatAmount = d.VatAmount
            }).ToList()
        };

        if (string.IsNullOrWhiteSpace(popupReceipt.WarehouseCode))
        {
            popupReceipt.WarehouseCode = popupReceipt.Details.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.WarehouseCode))?.WarehouseCode ?? string.Empty;
        }

        viewModel.OpenReceiptDetailModule(popupReceipt);
    }

    private void ShowInAppReceiptDetail(StockReceiptDto receipt)
    {
        _currentDetailReceipt = receipt;
        ReceiptDetailGrid.ItemsSource = receipt.Details;
        BuildReceiptDetailHeader(receipt);
        ReceiptDetailWindowTransform.X = 0;
        ReceiptDetailWindowTransform.Y = 0;
        ReceiptPrintPreviewModal.Visibility = Visibility.Collapsed;
        ReceiptDetailModal.Visibility = Visibility.Visible;
    }

    private void BuildReceiptDetailHeader(StockReceiptDto receipt)
    {
        ReceiptDetailHeaderPanel.Children.Clear();

        AddHeaderText($"Nhà cung cấp: {receipt.SupplierName}", 0, 0, true, 2, "#1E3352");
        AddHeaderText($"Số phiếu: {receipt.ReceiptNo}", 1, 0, false, 1, "#334D70");
        AddHeaderText($"Ngày chứng từ: {receipt.ReceiptDate:dd/MM/yyyy}", 1, 1, false, 1, "#334D70");
        AddHeaderText($"Người nhận hàng: {receipt.ReceiverName}", 2, 0, false, 1, "#334D70");
        AddHeaderText($"Tổng tiền: {receipt.TotalAmount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))}", 2, 1, true, 1, "#2F65D9");
    }

    private void AddHeaderText(string text, int row, int column, bool bold, int span, string colorHex)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = row == 0 ? 15 : 14,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!,
            Margin = new Thickness(0, row == 0 ? 0 : 8, 10, 0)
        };
        Grid.SetRow(block, row);
        Grid.SetColumn(block, column);
        Grid.SetColumnSpan(block, span);
        ReceiptDetailHeaderPanel.Children.Add(block);
    }

    private void CloseReceiptDetailModal_Click(object sender, RoutedEventArgs e)
    {
        ReceiptPrintPreviewModal.Visibility = Visibility.Collapsed;
        ReceiptDetailModal.Visibility = Visibility.Collapsed;
    }

    private void PreviewInAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentDetailReceipt is null)
        {
            return;
        }

        _currentPreviewDocument = StockReceiptPrintDocumentFactory.Build(_currentDetailReceipt);
        ReceiptPreviewViewer.Document = _currentPreviewDocument;
        ReceiptPrintPreviewModal.Visibility = Visibility.Visible;
    }

    private void CloseInAppPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        ReceiptPrintPreviewModal.Visibility = Visibility.Collapsed;
    }

    private void PrintFromInAppPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPreviewDocument is null)
        {
            return;
        }

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
        {
            return;
        }

        printDialog.PrintDocument(_currentPreviewDocument.DocumentPaginator, $"Phiếu nhập {_currentDetailReceipt?.ReceiptNo}");
    }

    private void InventoryGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            CopyGridSelection(grid);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && DataContext is StockReceiptsViewModel viewModel)
        {
            viewModel.PasteExcelRows(Clipboard.GetText());
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Enter or Key.Tab)
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
        }
    }

    private static void CopyGridSelection(DataGrid grid)
    {
        var rows = grid.SelectedItems.Cast<object>().ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var columns = grid.Columns
            .Where(x => x.Visibility == Visibility.Visible)
            .OrderBy(x => x.DisplayIndex)
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine(string.Join('\t', columns.Select(x => x.Header?.ToString() ?? string.Empty)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join('\t', columns.Select(column => ReadCellValue(column, row))));
        }

        Clipboard.SetText(builder.ToString());
    }

    private static string ReadCellValue(DataGridColumn column, object row)
    {
        if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is System.Windows.Data.Binding binding)
        {
            var propertyName = binding.Path?.Path;
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                var value = row.GetType().GetProperty(propertyName)?.GetValue(row);
                return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private void ReceiptWindowHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source)
        {
            var parentButton = FindAncestor<Button>(source);
            if (parentButton is not null)
            {
                return;
            }
        }

        _isDraggingReceiptWindow = true;
        _dragStartPoint = e.GetPosition(ReceiptDetailModalHost);
        _startTransformX = ReceiptDetailWindowTransform.X;
        _startTransformY = ReceiptDetailWindowTransform.Y;
        Mouse.Capture((UIElement)sender);
    }

    private void ReceiptWindowHeader_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingReceiptWindow)
        {
            return;
        }

        var current = e.GetPosition(ReceiptDetailModalHost);
        var deltaX = current.X - _dragStartPoint.X;
        var deltaY = current.Y - _dragStartPoint.Y;

        var newX = _startTransformX + deltaX;
        var newY = _startTransformY + deltaY;

        var maxX = Math.Max(0, (ReceiptDetailModalHost.ActualWidth - ReceiptDetailFloatingWindow.ActualWidth) / 2);
        var maxY = Math.Max(0, (ReceiptDetailModalHost.ActualHeight - ReceiptDetailFloatingWindow.ActualHeight) / 2);

        ReceiptDetailWindowTransform.X = Math.Clamp(newX, -maxX, maxX);
        ReceiptDetailWindowTransform.Y = Math.Clamp(newY, -maxY, maxY);
    }

    private void ReceiptWindowHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingReceiptWindow)
        {
            return;
        }

        _isDraggingReceiptWindow = false;
        Mouse.Capture(null);
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
