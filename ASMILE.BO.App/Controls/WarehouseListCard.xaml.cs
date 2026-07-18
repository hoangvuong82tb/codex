using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Controls;

public partial class WarehouseListCard : UserControl
{
    public WarehouseListCard()
    {
        InitializeComponent();
    }

    private async void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not WarehousesViewModel vm)
        {
            return;
        }

        await vm.OpenSelectedStockInsightAsync();
    }
}
