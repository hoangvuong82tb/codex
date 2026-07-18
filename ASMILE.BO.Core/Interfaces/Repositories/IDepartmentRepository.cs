using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Core.Interfaces.Repositories;

public interface IDepartmentRepository : IRepository<Department>
{
    Task<IReadOnlyList<Department>> SearchAsync(string keyword, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string departmentCode, int excludingId = 0, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<bool> HasPeopleAsync(int departmentId, CancellationToken cancellationToken = default);
}
