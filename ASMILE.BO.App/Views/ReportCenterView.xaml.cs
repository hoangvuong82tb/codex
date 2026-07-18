using ASMILE.BO.App.ViewModels;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Data;

namespace ASMILE.BO.App.Views;

public partial class ReportCenterView : UserControl
{
    private ReportCenterViewModel? _viewModel;

    public ReportCenterView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.ResultColumns.CollectionChanged -= OnResultColumnsChanged;
        }

        _viewModel = e.NewValue as ReportCenterViewModel;
        if (_viewModel is not null)
        {
            _viewModel.ResultColumns.CollectionChanged += OnResultColumnsChanged;
            RebuildResultColumns();
        }
    }

    private void OnResultColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildResultColumns();
    }

    private void RebuildResultColumns()
    {
        ResultGrid.Columns.Clear();
        if (_viewModel is null)
        {
            return;
        }

        foreach (var column in _viewModel.ResultColumns)
        {
            ResultGrid.Columns.Add(new DataGridTextColumn
            {
                Header = column.DisplayName,
                Binding = new Binding($"Values[{column.Name}]"),
                MinWidth = 110,
                Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells)
            });
        }
    }
}
