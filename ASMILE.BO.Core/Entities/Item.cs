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

    /// <summary>Giá bán chuẩn dùng khi hàng được bán hoặc ghi vào Folio.</summary>
    public decimal RetailPrice { get; set; }

    /// <summary>Thuế suất mặc định của hàng hóa, lưu theo phần trăm.</summary>
    public decimal VatRate { get; set; }

    /// <summary>Đánh dấu hàng thuộc danh mục minibar của cơ sở.</summary>
    public bool IsMinibarItem { get; set; }

    [MaxLength(100)]
    public string MinibarCategory { get; set; } = string.Empty;

    public decimal MinibarStandardQuantity { get; set; }
    public decimal MinibarMinStock { get; set; }
    public decimal MinibarMaxStock { get; set; }

    public bool IsActive { get; set; } = true;
}

