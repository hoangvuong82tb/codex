using ASMILE.BO.App.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Controls;

public partial class ExplorerBreadcrumb : UserControl
{
    public ExplorerBreadcrumb()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
        nameof(Segments),
        typeof(IEnumerable<ExplorerBreadcrumbSegment>),
        typeof(ExplorerBreadcrumb),
        new PropertyMetadata(null));

    public static readonly DependencyProperty NavigateSegmentCommandProperty = DependencyProperty.Register(
        nameof(NavigateSegmentCommand),
        typeof(ICommand),
        typeof(ExplorerBreadcrumb),
        new PropertyMetadata(null));

    public IEnumerable<ExplorerBreadcrumbSegment>? Segments
    {
        get => (IEnumerable<ExplorerBreadcrumbSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public ICommand? NavigateSegmentCommand
    {
        get => (ICommand?)GetValue(NavigateSegmentCommandProperty);
        set => SetValue(NavigateSegmentCommandProperty, value);
    }
}

