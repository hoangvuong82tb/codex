using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Core.Interfaces.Repositories;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<IReadOnlyList<Supplier>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetCodesStartingWithAsync(string prefix, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string supplierCode, int excludingId = 0, CancellationToken cancellationToken = default);
    Task<bool> ExistsTaxCodeAsync(string taxCode, int excludingId = 0, CancellationToken cancellationToken = default);
    Task<bool> IsUsedInReceiptsAsync(string supplierCode, CancellationToken cancellationToken = default);
}

