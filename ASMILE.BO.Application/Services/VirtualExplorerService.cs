using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Application.Services;

public sealed class VirtualExplorerService : IVirtualExplorerService
{
    private readonly IVirtualExplorerRepository _repository;

    public VirtualExplorerService(IVirtualExplorerRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<VirtualExplorerNodeDto>> GetRootAsync()
    {
        return _repository.GetRootAsync();
    }

    public Task<IReadOnlyList<VirtualExplorerNodeDto>> GetChildrenAsync(string virtualPath)
    {
        return _repository.GetChildrenAsync(virtualPath);
    }

    public Task<IReadOnlyList<VirtualExplorerNodeDto>> SearchAsync(string keyword)
    {
        return _repository.SearchAsync(keyword);
    }

    public Task<VirtualExplorerNodeDto?> GetNodeAsync(string virtualPath)
    {
        return _repository.GetNodeAsync(virtualPath);
    }
}
