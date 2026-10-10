namespace ASMILE.BO.Application.DTOs;

public sealed class InvoiceProviderConfigurationDto
{
    public Guid Id { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string? TenantCode { get; set; }
    public string? Username { get; set; }
    public bool HasSecret { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class InvoiceProviderConfigurationRequestDto
{
    public Guid? Id { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string? TenantCode { get; set; }
    public string? Username { get; set; }
    /// <summary>Plain secret supplied only on save; never returned by a query.</summary>
    public string? Secret { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
}

public sealed class InvoiceProviderReadinessDto
{
    public string ProviderCode { get; set; } = string.Empty;
    public bool HasRealAdapter { get; set; }
    public bool HasActiveConfiguration { get; set; }
    public bool HasValidEndpoint { get; set; }
    public bool HasTenant { get; set; }
    public bool HasUsername { get; set; }
    public bool HasSecret { get; set; }
    public bool IsReady { get; set; }
    public string Status { get; set; } = "Unavailable";
    public string Message { get; set; } = string.Empty;
}

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

public sealed class InvoiceAdjustmentDraftDto
{
    public Guid SourceInvoiceId { get; set; }
    public string AdjustmentType { get; set; } = "Increase";
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemCode { get; set; } = "ADJUSTMENT";
    public string UnitName { get; set; } = "Lần";
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
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
    public Guid? ReceivableTransactionId { get; set; }
    public string? ReceivableCustomerId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerTaxCode { get; set; }
    public string? BuyerAddress { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivableDebitAmount { get; set; }
    public decimal ReceivableCreditAmount { get; set; }
    public decimal ReceivableBalanceAmount { get; set; }
    public string? ReceivableStatus { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public string? ProviderInvoiceId { get; set; }
    public DateTime? IssuedAt { get; set; }
    public Guid? AdjustsInvoiceId { get; set; }
    public string? AdjustmentType { get; set; }
    public string? AdjustmentReason { get; set; }
    public IReadOnlyList<InvoiceLineSummaryDto> Lines { get; set; } = [];
}

public sealed class InvoiceLineSummaryDto
{
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
}

public sealed class InvoiceIssueResultDto
{
    public Guid InvoiceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public string? ProviderInvoiceId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class InvoiceIssueFailureDto
{
    public Guid LogId { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public int AttemptNo { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
