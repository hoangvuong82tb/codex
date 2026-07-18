using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IVirtualExplorerRepository
{
    Task<IReadOnlyList<VirtualExplorerNodeDto>> GetRootAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VirtualExplorerNodeDto>> GetChildrenAsync(string virtualPath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VirtualExplorerNodeDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<VirtualExplorerNodeDto?> GetNodeAsync(string virtualPath, CancellationToken cancellationToken = default);
}
