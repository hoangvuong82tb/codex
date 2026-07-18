using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Core.Interfaces.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<IReadOnlyList<Customer>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string customerCode, int excludingId = 0, CancellationToken cancellationToken = default);
}
