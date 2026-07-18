using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IWarehouseAppService
{
    Task<IReadOnlyList<WarehouseDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<WarehouseDto> SaveAsync(WarehouseDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default);
}

