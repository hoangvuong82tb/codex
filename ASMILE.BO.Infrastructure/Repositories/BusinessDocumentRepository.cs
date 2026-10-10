using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class BusinessDocumentRepository(AppDbContext db) : IBusinessDocumentRepository
{
    public Task<IReadOnlyList<BusinessDocumentDto>> GetByEntityAsync(BusinessEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
        => QueryAsync(
            x => x.EntityType == entityType && x.EntityId == entityId,
            x => x.OrderByDescending(row => row.DocumentDate).ThenByDescending(row => row.Code),
            cancellationToken);

    public Task<IReadOnlyList<BusinessDocumentDto>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => QueryAsync(
            x => x.EntityType == Core.Enums.BusinessEntityType.Supplier && x.EntityId == supplierId,
            x => x.OrderByDescending(row => row.DocumentDate).ThenByDescending(row => row.Code),
            cancellationToken);

    public Task<IReadOnlyList<BusinessDocumentDto>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        => QueryAsync(
            x => x.EntityType == Core.Enums.BusinessEntityType.Customer && x.EntityId == customerId,
            x => x.OrderByDescending(row => row.DocumentDate).ThenByDescending(row => row.Code),
            cancellationToken);

    public Task<IReadOnlyList<BusinessDocumentDto>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
        => QueryAsync(
            x => x.DocumentDate.Date == date.Date,
            x => x.OrderBy(row => row.Module).ThenBy(row => row.DocumentType).ThenBy(row => row.Code),
            cancellationToken);

    public async Task<BusinessDocumentDto?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var row = await db.BusinessDocuments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<BusinessDocumentDto> AddAsync(Core.Entities.BusinessDocument document, CancellationToken cancellationToken = default)
    {
        db.BusinessDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        return Map(document);
    }

    private async Task<IReadOnlyList<BusinessDocumentDto>> QueryAsync(
        Expression<Func<Core.Entities.BusinessDocument, bool>> predicate,
        Func<IQueryable<Core.Entities.BusinessDocument>, IOrderedQueryable<Core.Entities.BusinessDocument>> order,
        CancellationToken cancellationToken)
    {
        var rows = await order(db.BusinessDocuments.AsNoTracking().Where(predicate))
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    private static BusinessDocumentDto Map(Core.Entities.BusinessDocument row) => new()
    {
        Id = row.Id,
        Code = row.Code,
        Title = row.Title,
        Module = row.Module,
        EntityType = row.EntityType,
        EntityId = row.EntityId,
        DocumentType = row.DocumentType,
        DocumentDate = row.DocumentDate,
        Amount = row.Amount,
        Currency = row.Currency,
        VirtualPath = row.VirtualPath,
        PhysicalFilePath = string.IsNullOrWhiteSpace(row.PhysicalFilePath) ? $"dbdoc://{row.Id:N}" : row.PhysicalFilePath,
        FileName = row.FileName,
        FileExtension = row.FileExtension,
        FileSize = row.FileSize,
        Status = row.Status,
        CreatedBy = row.CreatedBy,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt
    };
}
