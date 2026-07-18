using System.ComponentModel.DataAnnotations;

namespace ASMILE.BO.Core.Entities;

public sealed class Warehouse
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string WarehouseCode { get; set; } = string.Empty;

    [MaxLength(255)]
    public string WarehouseName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    [MaxLength(255)]
    public string DepartmentName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string ResponsiblePersonName { get; set; } = string.Empty;

    public int? ParentWarehouseId { get; set; }

    public bool IsConsumptionWarehouse { get; set; }

    public bool IsActive { get; set; } = true;
}

