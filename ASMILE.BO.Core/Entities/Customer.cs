using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASMILE.BO.Core.Entities;

public sealed class Customer
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string CustomerCode { get; set; } = string.Empty;

    [MaxLength(255)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string TaxCode { get; set; } = string.Empty;

    [MaxLength(80)]
    public string BankAccountNo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [NotMapped]
    public string Code
    {
        get => CustomerCode;
        set => CustomerCode = value;
    }

    [NotMapped]
    public string Name
    {
        get => CustomerName;
        set => CustomerName = value;
    }

    [NotMapped]
    public string Status
    {
        get => IsActive ? "Active" : "Inactive";
        set => IsActive = !string.Equals(value, "Inactive", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(value, "Ngưng", StringComparison.OrdinalIgnoreCase);
    }

    [NotMapped]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
