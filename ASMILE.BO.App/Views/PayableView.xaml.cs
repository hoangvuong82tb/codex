using ASMILE.BO.App.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ASMILE.BO.App.Views;

public partial class PayableView : UserControl
{
    public PayableView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is PayableViewModel oldViewModel)
        {
            oldViewModel.ReconciliationRows.CollectionChanged -= OnReconciliationRowsChanged;
        }

        if (e.NewValue is PayableViewModel newViewModel)
        {
            newViewModel.ReconciliationRows.CollectionChanged += OnReconciliationRowsChanged;
        }
    }

    private void OnReconciliationRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(ScrollReconciliationGridToTop, DispatcherPriority.Loaded);
    }

    private void ScrollReconciliationGridToTop()
    {
        var scrollViewer = FindVisualChild<ScrollViewer>(ReconciliationDataGrid);
        scrollViewer?.ScrollToTop();
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
