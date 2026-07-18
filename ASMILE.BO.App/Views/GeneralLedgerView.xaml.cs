using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class GeneralLedgerView : UserControl
{
    public GeneralLedgerView()
    {
        InitializeComponent();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
    {

    }

    private void ComboBox_SelectionChanged_2(object sender, SelectionChangedEventArgs e)
    {

    }

    private void JournalEntriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is GeneralLedgerViewModel viewModel && sender is DataGrid grid)
        {
            viewModel.ReplaceSelectedEntries(grid.SelectedItems.OfType<LedgerEntryDto>());
        }
    }

    private void EnterpriseGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            CopySelectedRows(grid);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
        }
    }

    private static void CopySelectedRows(DataGrid grid)
    {
        var rows = grid.SelectedItems.OfType<LedgerEntryDto>().ToList();
        if (rows.Count == 0 && grid.SelectedItem is LedgerEntryDto current)
        {
            rows.Add(current);
        }

        if (rows.Count == 0)
        {
            return;
        }

        var lines = rows.Select(x => string.Join('\t',
            x.EntryDate.ToString("dd/MM/yyyy"),
            x.DocumentNo,
            x.Description,
            x.DebitAccount,
            x.CreditAccount,
            x.Amount.ToString("N0"),
            x.Status));
        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }
}
