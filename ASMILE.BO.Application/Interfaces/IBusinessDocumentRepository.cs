using ASMILE.BO.Application.DTOs;

using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;

namespace ASMILE.BO.Application.Interfaces;

public interface IBusinessDocumentRepository
{
    Task<IReadOnlyList<BusinessDocumentDto>> GetByEntityAsync(BusinessEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessDocumentDto>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessDocumentDto>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessDocumentDto>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<BusinessDocumentDto?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<BusinessDocumentDto> AddAsync(BusinessDocument document, CancellationToken cancellationToken = default);
}
