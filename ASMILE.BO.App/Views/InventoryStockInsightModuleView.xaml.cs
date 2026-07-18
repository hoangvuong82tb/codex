using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;
using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class InventoryStockInsightModuleView : UserControl
{
    public InventoryStockInsightModuleView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is not InventoryStockInsightModuleViewModel vm)
        {
            return;
        }

        var visibility = vm.HideIssueColumns ? Visibility.Collapsed : Visibility.Visible;
        OutQtyColumn.Visibility = visibility;
        OutAmountColumn.Visibility = visibility;

        var openingVisibility = vm.HideIssueColumns ? Visibility.Collapsed : Visibility.Visible;
        OpeningQtyColumn.Visibility = openingVisibility;
        OpeningAmountColumn.Visibility = openingVisibility;
        ClosingQtyColumn.Visibility = openingVisibility;
        ClosingAmountColumn.Visibility = openingVisibility;
    }
}
