using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class BusinessDocumentRepository : IBusinessDocumentRepository
{
    private readonly List<BusinessDocumentDto> _documents;

    public BusinessDocumentRepository()
    {
        _documents = VirtualFileCenterSeed.DefaultDocuments();
    }

    public Task<IReadOnlyList<BusinessDocumentDto>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var rows = _documents
            .Where(x => x.EntityType == Core.Enums.BusinessEntityType.Supplier && x.EntityId == supplierId)
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.Code)
            .ToList();

        return Task.FromResult<IReadOnlyList<BusinessDocumentDto>>(rows);
    }

    public Task<IReadOnlyList<BusinessDocumentDto>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var rows = _documents
            .Where(x => x.EntityType == Core.Enums.BusinessEntityType.Customer && x.EntityId == customerId)
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.Code)
            .ToList();

        return Task.FromResult<IReadOnlyList<BusinessDocumentDto>>(rows);
    }

    public Task<IReadOnlyList<BusinessDocumentDto>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var target = date.Date;
        var rows = _documents
            .Where(x => x.DocumentDate.Date == target)
            .OrderBy(x => x.Module)
            .ThenBy(x => x.DocumentType)
            .ThenBy(x => x.Code)
            .ToList();

        return Task.FromResult<IReadOnlyList<BusinessDocumentDto>>(rows);
    }

    public Task<BusinessDocumentDto?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var row = _documents.FirstOrDefault(x => x.Id == documentId);
        return Task.FromResult(row);
    }
}
