namespace ASMILE.BO.Core.Entities;

public sealed class Invoice : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string InvoiceSeries { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerTaxCode { get; set; }
    public string? BuyerAddress { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public string ProviderCode { get; set; } = string.Empty;
    public string? ProviderInvoiceId { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public Guid? ReceivableTransactionId { get; set; }
    public Guid? PayableTransactionId { get; set; }
    public Guid? VatDeclarationDraftId { get; set; }
    public string? XmlPayload { get; set; }
    public string? PdfPreviewPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? IssuedAt { get; set; }
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<InvoiceTaxBreakdown> TaxBreakdowns { get; set; } = new List<InvoiceTaxBreakdown>();
    public ICollection<InvoiceIssueLog> IssueLogs { get; set; } = new List<InvoiceIssueLog>();
}

public sealed class InvoiceLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid InvoiceId { get; set; }
    public int LineNo { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public string? JournalAccountCode { get; set; }
    public Invoice? Invoice { get; set; }
}

public sealed class InvoiceTaxBreakdown : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxBaseAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string TaxCode { get; set; } = string.Empty;
    public Invoice? Invoice { get; set; }
}

public sealed class InvoiceIssueLog : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid InvoiceId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public int AttemptNo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? RequestXml { get; set; }
    public string? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public Invoice? Invoice { get; set; }
}

public sealed class InvoiceProviderConfiguration : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string? TenantCode { get; set; }
    public string? Username { get; set; }
    public string? ProtectedSecret { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
