namespace ASMILE.BO.Application.DTOs;

public sealed class CustomerDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BankAccountNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public string Code
    {
        get => CustomerCode;
        set => CustomerCode = value;
    }

    public string Name
    {
        get => CustomerName;
        set => CustomerName = value;
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
