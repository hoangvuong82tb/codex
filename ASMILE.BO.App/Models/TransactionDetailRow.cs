using ASMILE.BO.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.Models;

public sealed partial class TransactionDetailRow : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private int lineNo;

    [ObservableProperty]
    private int itemId;

    [ObservableProperty]
    private string itemCode = string.Empty;

    [ObservableProperty]
    private string itemName = string.Empty;

    [ObservableProperty]
    private string unit = string.Empty;

    [ObservableProperty]
    private int? warehouseId;

    [ObservableProperty]
    private string warehouseCode = string.Empty;

    [ObservableProperty]
    private string toWarehouseCode = string.Empty;

    [ObservableProperty]
    private string departmentCode = string.Empty;

    [ObservableProperty]
    private decimal quantity;

    [ObservableProperty]
    private decimal unitPrice;

    [ObservableProperty]
    private decimal vatRate;

    public decimal Amount => Quantity * UnitPrice;
    public decimal VatAmount => Math.Round(Amount * VatRate / 100m, 2, MidpointRounding.AwayFromZero);
    public decimal TotalWithVat => Amount + VatAmount;

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(Amount));
        OnPropertyChanged(nameof(VatAmount));
        OnPropertyChanged(nameof(TotalWithVat));
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(Amount));
        OnPropertyChanged(nameof(VatAmount));
        OnPropertyChanged(nameof(TotalWithVat));
    }

    partial void OnVatRateChanged(decimal value)
    {
        OnPropertyChanged(nameof(VatAmount));
        OnPropertyChanged(nameof(TotalWithVat));
    }

    public StockTransactionDetailDto ToDto() => new()
    {
        Id = Id,
        LineNo = LineNo,
        ItemId = ItemId,
        ItemCode = ItemCode,
        ItemName = ItemName,
        Unit = Unit,
        WarehouseId = WarehouseId,
        WarehouseCode = WarehouseCode,
        Quantity = Quantity,
        UnitPrice = UnitPrice,
        Amount = Amount,
        VatRate = VatRate,
        VatAmount = VatAmount
    };
}

