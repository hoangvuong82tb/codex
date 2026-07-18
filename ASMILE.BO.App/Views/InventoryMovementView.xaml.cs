using ASMILE.BO.App.ViewModels;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class InventoryMovementView : UserControl
{
    private bool isDraggingRowDetail;
    private Point rowDetailDragStart;
    private double rowDetailStartX;
    private double rowDetailStartY;

    public InventoryMovementView()
    {
        InitializeComponent();
    }

    private void InventoryDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid || DataContext is not InventoryMovementViewModel viewModel)
        {
            return;
        }

        var row = FindAncestor<DataGridRow>((DependencyObject)e.OriginalSource);
        if (row is null || row.Item is null)
        {
            return;
        }

        grid.SelectedItem = row.Item;
        if (viewModel.OpenRowDetailCommand.CanExecute(row.Item))
        {
            RowDetailOverlayTransform.X = 0;
            RowDetailOverlayTransform.Y = 0;
            viewModel.OpenRowDetailCommand.Execute(row.Item);
        }
    }

    private void RowDetailPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsMouseOverDragHeader(e.OriginalSource as DependencyObject))
        {
            return;
        }

        isDraggingRowDetail = true;
        rowDetailDragStart = e.GetPosition(this);
        rowDetailStartX = RowDetailOverlayTransform.X;
        rowDetailStartY = RowDetailOverlayTransform.Y;
        RowDetailOverlayPanel.CaptureMouse();
        e.Handled = true;
    }

    private void RowDetailPanel_MouseMove(object sender, MouseEventArgs e)
    {
        if (!isDraggingRowDetail)
        {
            return;
        }

        var current = e.GetPosition(this);
        RowDetailOverlayTransform.X = rowDetailStartX + current.X - rowDetailDragStart.X;
        RowDetailOverlayTransform.Y = rowDetailStartY + current.Y - rowDetailDragStart.Y;
        e.Handled = true;
    }

    private void RowDetailPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isDraggingRowDetail)
        {
            return;
        }

        isDraggingRowDetail = false;
        RowDetailOverlayPanel.ReleaseMouseCapture();
        e.Handled = true;
    }

    private bool IsMouseOverDragHeader(DependencyObject? source)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, RowDetailDragHeader))
            {
                return true;
            }

            if (ReferenceEquals(source, RowDetailOverlayPanel))
            {
                return false;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }


    private void InventoryDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            CopySelectedRowsToClipboard(grid);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
            var request = new TraversalRequest(FocusNavigationDirection.Next);
            if (Keyboard.FocusedElement is UIElement element)
            {
                element.MoveFocus(request);
            }

            e.Handled = true;
        }
    }

    private static void CopySelectedRowsToClipboard(DataGrid grid)
    {
        var rows = grid.SelectedItems.Cast<object>().Where(x => x is not null).ToList();
        if (rows.Count == 0 && grid.SelectedItem is not null)
        {
            rows.Add(grid.SelectedItem);
        }

        if (rows.Count == 0)
        {
            return;
        }

        var columns = grid.Columns
            .Where(x => x.Visibility == Visibility.Visible)
            .OrderBy(x => x.DisplayIndex)
            .ToList();
        var builder = new StringBuilder();
        builder.AppendLine(string.Join('	', columns.Select(x => x.Header?.ToString() ?? string.Empty)));

        foreach (var row in rows)
        {
            var values = columns.Select(column => ReadCellValue(row, column));
            builder.AppendLine(string.Join('	', values));
        }

        Clipboard.SetText(builder.ToString());
    }

    private static string ReadCellValue(object row, DataGridColumn column)
    {
        if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is System.Windows.Data.Binding binding && binding.Path is not null)
        {
            var source = row;
            foreach (var segment in binding.Path.Path.Split('.'))
            {
                var property = source.GetType().GetProperty(segment, BindingFlags.Instance | BindingFlags.Public);
                if (property is null)
                {
                    return string.Empty;
                }

                source = property.GetValue(source);
                if (source is null)
                {
                    return string.Empty;
                }
            }

            return source is DateTime date ? date.ToString("dd/MM/yyyy") : source.ToString() ?? string.Empty;
        }

        return row.ToString() ?? string.Empty;
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

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void DataGrid_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
    {
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
    }
}
