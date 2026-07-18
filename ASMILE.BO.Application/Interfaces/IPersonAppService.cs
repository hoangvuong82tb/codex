using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IPersonAppService
{
    Task<IReadOnlyList<PersonDto>> SearchAsync(int? departmentId, string keyword, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<PersonDto> SaveAsync(PersonDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
