using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface ICustomerAppService
{
    Task<IReadOnlyList<CustomerDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<CustomerDto> SaveAsync(CustomerDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default);
}
