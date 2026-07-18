using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class BreakfastCafeSalesView : UserControl
{
    public BreakfastCafeSalesView()
    {
        InitializeComponent();
    }

    private async void MenuItemButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        await AddMenuItemFromElementAsync(sender as FrameworkElement);
    }

    private async void MenuItemCard_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        e.Handled = true;
        await AddMenuItemFromElementAsync(sender as FrameworkElement);
    }

    private async Task AddMenuItemFromElementAsync(FrameworkElement? element)
    {
        if (DataContext is not BreakfastCafeSalesViewModel viewModel)
        {
            return;
        }

        if (element?.DataContext is not PosMenuItemCard item)
        {
            viewModel.StatusText = "Không xác định được món cần thêm.";
            return;
        }

        await viewModel.AddMenuItemForSaleAsync(item);
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
            {
                return match;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }
}
