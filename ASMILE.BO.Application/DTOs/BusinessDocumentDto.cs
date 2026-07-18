using ASMILE.BO.Core.Enums;

namespace ASMILE.BO.Application.DTOs;

public sealed class BusinessDocumentDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public BusinessModule Module { get; set; }
    public BusinessEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public BusinessDocumentType DocumentType { get; set; }
    public DateTime DocumentDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string VirtualPath { get; set; } = string.Empty;
    public string PhysicalFilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DocumentStatus Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
