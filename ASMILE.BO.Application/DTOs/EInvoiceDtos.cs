namespace ASMILE.BO.Application.DTOs;

public sealed class InvoiceDraftDto
{
    public string InvoiceNo { get; set; } = string.Empty;
    public string InvoiceSeries { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerTaxCode { get; set; }
    public string? BuyerAddress { get; set; }
    public string ProviderCode { get; set; } = "MISA";
    public Guid? JournalHeaderId { get; set; }
    public Guid? ReceivableTransactionId { get; set; }
    public Guid? PayableTransactionId { get; set; }
    public IReadOnlyList<InvoiceLineDraftDto> Lines { get; set; } = [];
}

public sealed class InvoiceLineDraftDto
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public string? JournalAccountCode { get; set; }
}

public sealed class InvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
}

public sealed class InvoiceIssueResultDto
{
    public Guid InvoiceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public string? ProviderInvoiceId { get; set; }
    public string Message { get; set; } = string.Empty;
}
