using ASMILE.BO.Application.DTOs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class InventoryAdjustmentDialog : Window
{
    public ObservableCollection<InventoryStockBalanceDto> StockOptions { get; }
    public InventoryStockBalanceDto? SelectedStock { get; set; }
    public string ActualQuantityText { get; set; } = string.Empty;
    public string Reason { get; set; } = "Kiểm kê định kỳ";
    public string Note { get; set; } = string.Empty;
    public DateTime? AdjustmentDate { get; set; } = DateTime.Today;
    public string DifferenceText
    {
        get
        {
            if (SelectedStock is null)
            {
                return "0";
            }

            if (!TryGetActualQuantity(out var actual))
            {
                return "0";
            }

            return (actual - SelectedStock.ClosingQuantity).ToString("N2", CultureInfo.GetCultureInfo("vi-VN"));
        }
    }

    public (InventoryStockBalanceDto Stock, decimal ActualQuantity, string Reason)? Result { get; private set; }

    public InventoryAdjustmentDialog(
        IEnumerable<InventoryStockBalanceDto> stocks,
        InventoryStockBalanceDto? initialStock = null,
        decimal? actualQuantity = null,
        string? reason = null)
    {
        StockOptions = new ObservableCollection<InventoryStockBalanceDto>(stocks);
        SelectedStock = initialStock is null
            ? StockOptions.FirstOrDefault()
            : StockOptions.FirstOrDefault(x => x.ProductId == initialStock.ProductId && x.WarehouseId == initialStock.WarehouseId)
                ?? StockOptions.FirstOrDefault();
        if (SelectedStock is not null)
        {
            ActualQuantityText = (actualQuantity ?? SelectedStock.ClosingQuantity).ToString("N2", CultureInfo.GetCultureInfo("vi-VN"));
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Reason = reason.Trim();
        }

        InitializeComponent();
        DataContext = this;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (SelectedStock is null)
        {
            MessageBox.Show("Vui lòng chọn hàng hóa.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetActualQuantity(out var actual))
        {
            MessageBox.Show("Tồn thực tế không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (actual < 0)
        {
            MessageBox.Show("Tồn thực tế không được âm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var reason = string.IsNullOrWhiteSpace(Reason) ? "Điều chỉnh kiểm kê" : Reason.Trim();
        if (!string.IsNullOrWhiteSpace(Note))
        {
            reason = $"{reason} | {Note.Trim()}";
        }

        Result = (SelectedStock, actual, reason);
        DialogResult = true;
    }

    private bool TryGetActualQuantity(out decimal actual)
    {
        return decimal.TryParse(ActualQuantityText, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out actual)
            || decimal.TryParse(ActualQuantityText, NumberStyles.Number, CultureInfo.InvariantCulture, out actual);
    }
}
