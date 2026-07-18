namespace ASMILE.BO.Application.DTOs;

public sealed class WarehouseDto
{
    public int Id { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string ResponsiblePersonName { get; set; } = string.Empty;
    public int? ParentWarehouseId { get; set; }
    public bool IsConsumptionWarehouse { get; set; }
    public bool IsActive { get; set; } = true;
    public string ParentWarehouseCode { get; set; } = string.Empty;
}

