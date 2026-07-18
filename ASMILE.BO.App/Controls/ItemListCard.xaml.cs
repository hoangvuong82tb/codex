using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Controls;

public partial class ItemListCard : UserControl
{
    public ItemListCard()
    {
        InitializeComponent();
    }

    private async void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ItemsViewModel vm)
        {
            return;
        }

        await vm.OpenSelectedStockInsightAsync();
    }
}
