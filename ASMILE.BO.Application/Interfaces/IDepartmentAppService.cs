using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IDepartmentAppService
{
    Task<IReadOnlyList<DepartmentDto>> SearchAsync(string keyword, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<DepartmentDto> SaveAsync(DepartmentDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default);
}
