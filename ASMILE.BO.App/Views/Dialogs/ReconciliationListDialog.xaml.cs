using ASMILE.BO.Application.DTOs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class ReconciliationListDialog : Window
{
    private readonly List<ReconciliationRowItem> _allRows = [];
    public ObservableCollection<ReconciliationRowItem> PagedRows { get; } = [];
    private bool _isReady;

    private int _page = 1;
    private int _pageSize = 10;
    private string _keyword = string.Empty;
    private int _totalPages = 1;

    public ReconciliationListDialog(IEnumerable<ReceivableSummaryDto> rows, ReceivableSummaryDto? selectedCustomer = null)
    {
        InitializeComponent();
        DataContext = this;

        _allRows.AddRange(BuildRealRows(rows, selectedCustomer));
        _isReady = true;
        RefreshGrid();
    }

    public static IReadOnlyList<ReconciliationRowItem> BuildRealRows(
        IEnumerable<ReceivableSummaryDto> rows,
        ReceivableSummaryDto? selectedCustomer = null)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var source = selectedCustomer is null
            ? rows
            : rows.Where(x => x.CustomerId == selectedCustomer.CustomerId);

        return source
            .OrderBy(x => x.CustomerName)
            .Select((summary, index) =>
            {
                var lastTransactionDate = summary.LastTransactionDate;
                var period = lastTransactionDate.HasValue
                    ? $"Lũy kế đến {lastTransactionDate.Value:MM/yyyy}"
                    : "Theo dữ liệu hiện có";

                return new ReconciliationRowItem(
                    index + 1,
                    period,
                    summary.CustomerName,
                    lastTransactionDate,
                    summary.ClosingBalance,
                    customerConfirmedBalance: null,
                    difference: null,
                    status: "Chưa đối chiếu",
                    reviewer: "Chưa có xác nhận khách hàng");
            })
            .ToList();
    }

    private void RefreshGrid()
    {
        if (!_isReady) return;

        var filtered = _allRows
            .Where(x => string.IsNullOrWhiteSpace(_keyword)
                || x.Period.Contains(_keyword, StringComparison.OrdinalIgnoreCase)
                || x.CustomerName.Contains(_keyword, StringComparison.OrdinalIgnoreCase)
                || x.Reviewer.Contains(_keyword, StringComparison.OrdinalIgnoreCase)
                || x.Status.Contains(_keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)_pageSize));
        _page = Math.Clamp(_page, 1, _totalPages);

        var pageRows = filtered.Skip((_page - 1) * _pageSize).Take(_pageSize).ToList();
        PagedRows.Clear();
        foreach (var r in pageRows)
        {
            PagedRows.Add(r);
        }

        RecordCountText.Text = $"trên tổng số {filtered.Count} bản ghi";
        ApplyPageButtonState();
    }

    private void ApplyPageButtonState()
    {
        if (Page1Button is null || Page2Button is null || Page3Button is null) return;

        Page1Button.Visibility = _totalPages >= 1 ? Visibility.Visible : Visibility.Collapsed;
        Page2Button.Visibility = _totalPages >= 2 ? Visibility.Visible : Visibility.Collapsed;
        Page3Button.Visibility = _totalPages >= 3 ? Visibility.Visible : Visibility.Collapsed;

        SetPageButtonStyle(Page1Button, _page == 1);
        SetPageButtonStyle(Page2Button, _page == 2);
        SetPageButtonStyle(Page3Button, _page == 3);
    }

    private static void SetPageButtonStyle(Button button, bool active)
    {
        if (active)
        {
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            button.BorderBrush = button.Background;
            button.Foreground = Brushes.White;
        }
        else
        {
            button.Background = Brushes.White;
            button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
            button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
        }
    }

    private void OnPageSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady) return;

        if (PageSizeComboBox.SelectedItem is ComboBoxItem cbi && int.TryParse(cbi.Content?.ToString(), out var size))
        {
            _pageSize = size;
            _page = 1;
            RefreshGrid();
        }
    }

    private void OnFirstPageClicked(object sender, RoutedEventArgs e)
    {
        _page = 1;
        RefreshGrid();
    }

    private void OnPrevPageClicked(object sender, RoutedEventArgs e)
    {
        _page = Math.Max(1, _page - 1);
        RefreshGrid();
    }

    private void OnNextPageClicked(object sender, RoutedEventArgs e)
    {
        _page = Math.Min(_totalPages, _page + 1);
        RefreshGrid();
    }

    private void OnLastPageClicked(object sender, RoutedEventArgs e)
    {
        _page = _totalPages;
        RefreshGrid();
    }

    private void OnPage1Clicked(object sender, RoutedEventArgs e)
    {
        _page = 1;
        RefreshGrid();
    }

    private void OnPage2Clicked(object sender, RoutedEventArgs e)
    {
        _page = 2;
        RefreshGrid();
    }

    private void OnPage3Clicked(object sender, RoutedEventArgs e)
    {
        _page = 3;
        RefreshGrid();
    }

    private void OnFilterClicked(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Bộ lọc nâng cao đang được mở rộng.", "Bộ lọc", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnMoreClicked(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Tùy chọn:\n- Xuất Excel\n- In danh sách\n- Làm mới", "Thêm", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

public sealed class ReconciliationRowItem
{
    public int Index { get; }
    public string Period { get; }
    public string CustomerName { get; }
    public DateTime? ReconcileDate { get; }
    public decimal DebtA { get; }
    public decimal? DebtB { get; }
    public decimal? Diff { get; }
    public string Status { get; }
    public string Reviewer { get; }
    public bool IsMatched => DebtB.HasValue && Diff == 0;

    public ReconciliationRowItem(
        int index,
        string period,
        string customerName,
        DateTime? reconcileDate,
        decimal debtA,
        decimal? customerConfirmedBalance,
        decimal? difference,
        string status,
        string reviewer)
    {
        Index = index;
        Period = period;
        CustomerName = customerName;
        ReconcileDate = reconcileDate;
        DebtA = debtA;
        DebtB = customerConfirmedBalance;
        Diff = difference;
        Status = status;
        Reviewer = reviewer;
    }
}
