namespace ASMILE.BO.App.Models;

public sealed class WarehouseImportRow
{
    public int RowNumber { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string ParentWarehouseCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string ResponsiblePersonName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string IsConsumptionWarehouseRaw { get; set; } = string.Empty;
    public string IsActiveRaw { get; set; } = string.Empty;
}
