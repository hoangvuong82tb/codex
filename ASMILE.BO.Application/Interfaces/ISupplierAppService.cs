using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface ISupplierAppService
{
    Task<IReadOnlyList<SupplierDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<SupplierDto> SaveAsync(SupplierDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default);
}

