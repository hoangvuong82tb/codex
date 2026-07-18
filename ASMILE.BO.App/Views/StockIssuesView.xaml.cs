using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.App.Views;

public partial class StockIssuesView : UserControl
{
    public StockIssuesView()
    {
        InitializeComponent();
        HookEditableCombo(DetailProductComboBox);
        HookEditableCombo(ReceiverComboBox);
    }

    private static void HookEditableCombo(ComboBox comboBox)
    {
        comboBox.Loaded += (_, _) =>
        {
            if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is not TextBox textBox)
            {
                return;
            }

            textBox.GotKeyboardFocus -= EditableTextBox_GotKeyboardFocus;
            textBox.PreviewKeyDown -= EditableTextBox_PreviewKeyDown;
            textBox.PreviewTextInput -= EditableTextBox_PreviewTextInput;

            textBox.GotKeyboardFocus += EditableTextBox_GotKeyboardFocus;
            textBox.PreviewKeyDown += EditableTextBox_PreviewKeyDown;
            textBox.PreviewTextInput += EditableTextBox_PreviewTextInput;
        };
    }

    private static void EditableTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        textBox.CaretIndex = textBox.Text?.Length ?? 0;
        textBox.SelectionLength = 0;
    }

    private static void EditableTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        if (textBox.SelectionLength > 0)
        {
            var caret = textBox.CaretIndex;
            textBox.SelectionLength = 0;
            textBox.CaretIndex = Math.Max(caret, textBox.Text?.Length ?? 0);
        }
    }

    private static void EditableTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        if (textBox.SelectionLength <= 0)
        {
            return;
        }

        var isNavigationKey = e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Tab or Key.Enter or Key.Escape;
        if (isNavigationKey)
        {
            return;
        }

        var caret = textBox.CaretIndex;
        textBox.SelectionLength = 0;
        textBox.CaretIndex = Math.Max(caret, textBox.Text?.Length ?? 0);
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not StockIssuesViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedIssue is not StockIssueDto selectedIssue)
        {
            return;
        }

        viewModel.OpenIssueDetailModule(selectedIssue);
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

        if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && DataContext is StockIssuesViewModel viewModel)
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
}
