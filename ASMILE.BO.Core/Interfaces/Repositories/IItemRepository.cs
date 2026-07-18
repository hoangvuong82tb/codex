using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Core.Interfaces.Repositories;

public interface IItemRepository : IRepository<Item>
{
    Task<IReadOnlyList<Item>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<Item?> FindByCodeAsync(string itemCode, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string itemCode, int excludingId = 0, CancellationToken cancellationToken = default);
    Task<bool> ExistsNameAsync(string itemName, int excludingId = 0, CancellationToken cancellationToken = default);
    Task<bool> IsUsedInTransactionsAsync(int itemId, CancellationToken cancellationToken = default);
}

