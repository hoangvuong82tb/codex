using ASMILE.BO.Application.DTOs;

using ASMILE.BO.Core.Enums;

namespace ASMILE.BO.Application.Interfaces;

public interface IBusinessDocumentService
{
    Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByEntityAsync(BusinessEntityType entityType, Guid entityId);
    Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsBySupplierAsync(Guid supplierId);
    Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByCustomerAsync(Guid customerId);
    Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByDateAsync(DateTime date);
    Task<BusinessDocumentDto?> GetDocumentAsync(Guid documentId);
    Task<string?> ResolvePhysicalPathAsync(Guid documentId);
    Task<BusinessDocumentDto> UploadAttachmentAsync(BusinessEntityType entityType, Guid entityId, string title, string sourceFilePath, string createdBy, CancellationToken cancellationToken = default);
    Task PrintAsync(Guid documentId);
    Task ExportPdfAsync(Guid documentId);
}
