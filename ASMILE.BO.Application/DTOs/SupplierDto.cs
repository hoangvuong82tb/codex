namespace ASMILE.BO.Application.DTOs;

public sealed class SupplierDto
{
    public int Id { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BankAccountNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public string Code
    {
        get => SupplierCode;
        set => SupplierCode = value;
    }

    public string Name
    {
        get => SupplierName;
        set => SupplierName = value;
    }

    public string TaxCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status
    {
        get => IsActive ? "Active" : "Inactive";
        set => IsActive = !string.Equals(value, "Inactive", StringComparison.OrdinalIgnoreCase);
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
