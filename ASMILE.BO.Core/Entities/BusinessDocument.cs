using ASMILE.BO.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ASMILE.BO.Core.Entities;

public sealed class BusinessDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    public BusinessModule Module { get; set; } = BusinessModule.Accounting;
    public BusinessEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public BusinessDocumentType DocumentType { get; set; }
    public DateTime DocumentDate { get; set; }
    public decimal Amount { get; set; }

    [MaxLength(32)]
    public string PaymentMethod { get; set; } = "Chưa xác định";

    [MaxLength(16)]
    public string Currency { get; set; } = "VND";

    [MaxLength(512)]
    public string VirtualPath { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string PhysicalFilePath { get; set; } = string.Empty;

    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(32)]
    public string FileExtension { get; set; } = string.Empty;

    public long FileSize { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Active;

    [MaxLength(128)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
