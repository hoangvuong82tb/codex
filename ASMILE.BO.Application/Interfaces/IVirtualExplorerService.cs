using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IVirtualExplorerService
{
    Task<IReadOnlyList<VirtualExplorerNodeDto>> GetRootAsync();
    Task<IReadOnlyList<VirtualExplorerNodeDto>> GetChildrenAsync(string virtualPath);
    Task<IReadOnlyList<VirtualExplorerNodeDto>> SearchAsync(string keyword);
    Task<VirtualExplorerNodeDto?> GetNodeAsync(string virtualPath);
}
