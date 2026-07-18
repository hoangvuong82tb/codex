using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Core.Interfaces.Repositories;

public interface IPersonRepository : IRepository<Person>
{
    Task<IReadOnlyList<Person>> SearchAsync(int? departmentId, string keyword, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string personCode, int excludingId = 0, CancellationToken cancellationToken = default);
}
