using System.ComponentModel.DataAnnotations;

namespace ASMILE.BO.Core.Entities;

public sealed class Item
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string ItemCode { get; set; } = string.Empty;

    [MaxLength(255)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string SupplierCode { get; set; } = string.Empty;

    [MaxLength(255)]
    public string SupplierName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

