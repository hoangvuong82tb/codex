using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IBusinessDocumentService
{
    Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsBySupplierAsync(Guid supplierId);
    Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByCustomerAsync(Guid customerId);
    Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByDateAsync(DateTime date);
    Task<BusinessDocumentDto?> GetDocumentAsync(Guid documentId);
    Task PrintAsync(Guid documentId);
    Task ExportPdfAsync(Guid documentId);
}
