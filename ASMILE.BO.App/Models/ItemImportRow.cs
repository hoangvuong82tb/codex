namespace ASMILE.BO.App.Models;

public sealed class ItemImportRow
{
    public int RowNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string IsActiveRaw { get; set; } = string.Empty;
}
