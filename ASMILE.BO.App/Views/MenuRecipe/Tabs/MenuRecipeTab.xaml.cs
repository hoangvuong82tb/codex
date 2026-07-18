using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views.MenuRecipe.Tabs;

public partial class MenuRecipeTab : UserControl
{
    public MenuRecipeTab()
    {
        InitializeComponent();
    }

    private void RecipeGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindVisualParent<ButtonBase>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        var cell = FindVisualParent<DataGridCell>(e.OriginalSource as DependencyObject);
        if (cell is null || cell.IsEditing || cell.IsReadOnly)
        {
            return;
        }

        cell.Focus();

        if (sender is DataGrid grid)
        {
            grid.CurrentCell = new DataGridCellInfo(cell.DataContext, cell.Column);
            grid.BeginEdit(e);
        }
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T target)
            {
                return target;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
