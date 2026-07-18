using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IBusinessDocumentRepository
{
    Task<IReadOnlyList<BusinessDocumentDto>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessDocumentDto>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessDocumentDto>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<BusinessDocumentDto?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
}
