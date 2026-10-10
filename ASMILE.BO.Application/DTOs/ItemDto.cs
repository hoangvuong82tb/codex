namespace ASMILE.BO.Application.DTOs;

public sealed class ItemDto
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal RetailPrice { get; set; }
    public decimal? VatRate { get; set; }
    public bool IsMinibarItem { get; set; }
    public string MinibarCategory { get; set; } = string.Empty;
    public decimal MinibarStandardQuantity { get; set; }
    public decimal MinibarMinStock { get; set; }
    public decimal MinibarMaxStock { get; set; }
    public bool IsActive { get; set; } = true;
}

