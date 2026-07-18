using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.DTOs;

public sealed class TaxCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string? InputAccountCode { get; set; }
    public string? OutputAccountCode { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateTaxCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string? InputAccountCode { get; set; }
    public string? OutputAccountCode { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CreateVatTransactionRequest
{
    public Guid TaxCodeId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public Guid? JournalLineId { get; set; }
    public string? InvoiceNo { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? PartnerType { get; set; }
    public Guid? PartnerId { get; set; }
    public decimal TaxBaseAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string? DeclarationPeriod { get; set; }
    public string? Status { get; set; }
}

public sealed class VatTransactionDto
{
    public Guid Id { get; set; }
    public Guid TaxCodeId { get; set; }
    public string? TaxCode { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public Guid? JournalLineId { get; set; }
    public string? InvoiceNo { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? PartnerType { get; set; }
    public Guid? PartnerId { get; set; }
    public decimal TaxBaseAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string DeclarationPeriod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class TaxDeclarationDraftDto
{
    public Guid Id { get; set; }
    public string DeclarationPeriod { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public decimal InputVatAmount { get; set; }
    public decimal OutputVatAmount { get; set; }
    public decimal NetVatPayable { get; set; }
    public string Status { get; set; } = string.Empty;
}
