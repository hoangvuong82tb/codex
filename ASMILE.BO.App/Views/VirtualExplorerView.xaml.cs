using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class VirtualExplorerView : UserControl
{
    private double _lastListViewportWidth = -1;

    public VirtualExplorerView()
    {
        InitializeComponent();
        NavigationTreeView.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(NavigationTreeViewItem_Expanded));
    }

    private void CurrentItemsListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2
            || sender is not ListBox listBox
            || DataContext is not VirtualExplorerViewModel viewModel)
        {
            return;
        }

        // Handle the preview event so nested item templates/controls cannot consume
        // the mouse-down before WPF raises MouseDoubleClick. Resolve the clicked
        // container instead of relying on SelectedItem, which may still refer to
        // the previously selected row at preview-event time.
        var container = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (container?.DataContext is not VirtualExplorerNodeDto node)
        {
            return;
        }

        if (viewModel.OpenCommand.CanExecute(node))
        {
            viewModel.OpenCommand.Execute(node);
            e.Handled = true;
        }
    }

    private void CurrentItemsListView_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateVirtualPathColumnWidth(sender as ListView);
    }

    private void CurrentItemsListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateVirtualPathColumnWidth(sender as ListView);
    }

    private void CurrentItemsListView_LayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is not ListView listView)
        {
            return;
        }

        var scrollViewer = FindDescendant<ScrollViewer>(listView);
        var viewportWidth = scrollViewer?.ViewportWidth ?? listView.ActualWidth;
        if (viewportWidth <= 0)
        {
            return;
        }

        if (Math.Abs(viewportWidth - _lastListViewportWidth) > 0.5)
        {
            _lastListViewportWidth = viewportWidth;
            UpdateVirtualPathColumnWidth(listView);
        }
    }

    private void NavigationTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not VirtualExplorerViewModel viewModel)
        {
            return;
        }

        if (e.NewValue is not ExplorerNavNode node)
        {
            return;
        }

        if (viewModel.NavigateSidebarCommand.CanExecute(node))
        {
            viewModel.NavigateSidebarCommand.Execute(node);
        }
    }

    private async void NavigationTreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not VirtualExplorerViewModel viewModel)
        {
            return;
        }

        if (e.OriginalSource is not TreeViewItem treeViewItem || treeViewItem.DataContext is not ExplorerNavNode node)
        {
            return;
        }

        await viewModel.EnsureNavigationChildrenLoadedAsync(node);
    }

    private void DriveCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2
            || DataContext is not VirtualExplorerViewModel viewModel
            || (sender as FrameworkElement)?.DataContext is not ExplorerDriveItemModel drive)
        {
            return;
        }

        var node = CreateDriveNode(drive);
        if (viewModel.OpenCommand.CanExecute(node))
        {
            viewModel.OpenCommand.Execute(node);
            e.Handled = true;
        }
    }

    private void DriveCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1
            || DataContext is not VirtualExplorerViewModel viewModel
            || (sender as FrameworkElement)?.DataContext is not ExplorerDriveItemModel drive)
        {
            return;
        }

        var pseudoNode = CreateDriveNode(drive);
        viewModel.SelectedItem = pseudoNode;
    }

    private static VirtualExplorerNodeDto CreateDriveNode(ExplorerDriveItemModel drive) => new()
    {
        Id = drive.Id,
        Name = drive.DisplayName,
        DisplayName = drive.DisplayName,
        VirtualPath = drive.VirtualPath,
        NodeType = drive.IsNetworkLocation ? Core.Enums.VirtualNodeType.NetworkItem : Core.Enums.VirtualNodeType.Drive,
        Module = drive.IsNetworkLocation ? Core.Enums.BusinessModule.Network : Core.Enums.BusinessModule.System,
        PhysicalPath = drive.PhysicalPath,
        Description = drive.Description,
        Status = drive.Status,
        UsedPercent = drive.UsedPercent,
        HasUsageMetric = drive.HasUsageMetric,
        IsAlert = drive.IsAlert
    };

    private static void UpdateVirtualPathColumnWidth(ListView? listView)
    {
        if (listView?.View is not GridView gridView || gridView.Columns.Count == 0)
        {
            return;
        }

        // Keep fixed columns and let the last column (Virtual Path) always fill remainder.
        var fixedWidth = gridView.Columns.Take(gridView.Columns.Count - 1).Sum(column => column.Width > 0 ? column.Width : column.ActualWidth);
        var viewportWidth = listView.ActualWidth;
        var scrollViewer = FindDescendant<ScrollViewer>(listView);
        if (scrollViewer is not null && scrollViewer.ViewportWidth > 0)
        {
            viewportWidth = scrollViewer.ViewportWidth;
        }

        // Tiny offset only for header border; avoid leaving a visible blank column.
        var available = viewportWidth - fixedWidth - 1;
        var targetWidth = Math.Max(260, available);

        var lastColumn = gridView.Columns[^1];
        if (Math.Abs(lastColumn.Width - targetWidth) > 1)
        {
            lastColumn.Width = targetWidth;
        }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T typedRoot)
        {
            return typedRoot;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var found = FindDescendant<T>(child);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
