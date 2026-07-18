using ASMILE.BO.App.ViewModels;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class CashBankView : UserControl
{
    public CashBankView()
    {
        InitializeComponent();
    }

    private void CashGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (HandleGridCopyPaste(grid, e, pasteText =>
            {
                if (DataContext is CashBankViewModel viewModel)
                {
                    viewModel.PasteCashRows(pasteText);
                }
            }))
        {
            return;
        }
    }

    private void BankGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (HandleGridCopyPaste(grid, e, pasteText =>
            {
                if (DataContext is CashBankViewModel viewModel)
                {
                    viewModel.PasteBankRows(pasteText);
                }
            }))
        {
            return;
        }
    }

    private static bool HandleGridCopyPaste(DataGrid grid, KeyEventArgs e, Action<string> paste)
    {
        if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            CopyGridSelection(grid);
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            paste(Clipboard.GetText());
            e.Handled = true;
            return true;
        }

        if (e.Key is Key.Enter or Key.Tab)
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        return false;
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
            if (!string.IsNullOrWhiteSpace(propertyName) && !propertyName.Contains('[', StringComparison.Ordinal))
            {
                var value = row.GetType().GetProperty(propertyName)?.GetValue(row);
                return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
