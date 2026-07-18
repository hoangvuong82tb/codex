using ASMILE.BO.App.ViewModels;
using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class BanquetMenuModal : Window
{
    private readonly BanquetRestaurantViewModel _parent;

    public BanquetMenuModal(BanquetMenuSelectionViewModel vm, BanquetRestaurantViewModel parent)
    {
        InitializeComponent();
        _parent = parent;
        DataContext = vm;
        Owner = System.Windows.Application.Current.MainWindow;
        Closed += (_, _) => _parent.BackToMenuCardsCommand.Execute(null);
    }
}