namespace ASMILE.BO.App.Models;

public sealed class SupplierImportRow
{
    public int RowNumber { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BankAccountNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string IsActiveRaw { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

