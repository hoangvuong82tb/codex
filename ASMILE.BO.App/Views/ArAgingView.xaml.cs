using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class ArAgingView : UserControl
{
    private ArAgingViewModel VM => (ArAgingViewModel)DataContext;

    public ArAgingView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await VM.InitializeAsync();
        VM.BuildPagerItems();
    }

    private void OnTreeItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is ArAgingTreeItem item)
        {
            VM.SelectTreeNodeCommand.Execute(item);
        }
    }

    private void OnDetailTabClick(object sender, RoutedEventArgs e)
    {
        // XAML may materialize Tag as either a string or a boxed numeric value;
        // normalize through ToString so every inspector tab remains clickable.
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out var tabIndex))
        {
            VM.SelectDetailTabCommand.Execute(tabIndex);
        }
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (VM.SelectedItem != null)
        {
            VM.SelectDetailTabCommand.Execute(0);
        }
    }

    private void OnItemCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is ArAgingItemDto item)
        {
            VM.SelectItemCommand.Execute(item);
        }
    }

    private void OnDismissInfo(object sender, RoutedEventArgs e)
    {
        VM.DismissInfoCommand.Execute(null);
    }
}
