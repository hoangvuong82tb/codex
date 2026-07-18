using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IItemAppService
{
    Task<IReadOnlyList<ItemDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<ItemDto> SaveAsync(ItemDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default);
}

