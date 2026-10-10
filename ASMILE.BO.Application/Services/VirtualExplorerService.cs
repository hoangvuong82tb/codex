using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Enums;

namespace ASMILE.BO.Application.Services;

public sealed class VirtualExplorerService : IVirtualExplorerService
{
    private readonly IVirtualExplorerRepository _repository;
    private readonly IAuthorizationService _authorizationService;

    public VirtualExplorerService(IVirtualExplorerRepository repository, IAuthorizationService authorizationService)
    {
        _repository = repository;
        _authorizationService = authorizationService;
    }

    public async Task<IReadOnlyList<VirtualExplorerNodeDto>> GetRootAsync()
    {
        if (!CanUseVirtualFileCenter()) return Array.Empty<VirtualExplorerNodeDto>();
        return FilterVisibleNodes(await _repository.GetRootAsync());
    }

    public async Task<IReadOnlyList<VirtualExplorerNodeDto>> GetChildrenAsync(string virtualPath)
    {
        if (!CanAccessPath(virtualPath)) return Array.Empty<VirtualExplorerNodeDto>();
        return FilterVisibleNodes(await _repository.GetChildrenAsync(virtualPath));
    }

    public async Task<IReadOnlyList<VirtualExplorerNodeDto>> SearchAsync(string keyword)
    {
        if (!CanUseVirtualFileCenter()) return Array.Empty<VirtualExplorerNodeDto>();
        return FilterVisibleNodes(await _repository.SearchAsync(keyword));
    }

    public async Task<VirtualExplorerNodeDto?> GetNodeAsync(string virtualPath)
    {
        if (!CanAccessPath(virtualPath)) return null;
        var node = await _repository.GetNodeAsync(virtualPath);
        return node is not null && CanAccessNode(node) ? node : null;
    }

    public Task<VirtualExplorerRefreshResultDto> RefreshAsync(CancellationToken cancellationToken = default)
    {
        return CanUseVirtualFileCenter()
            ? _repository.RefreshAsync(cancellationToken)
            : Task.FromResult(new VirtualExplorerRefreshResultDto());
    }

    private bool CanUseVirtualFileCenter()
        => _authorizationService.HasPermission("VFC.View");

    private bool CanAccessPath(string? virtualPath)
    {
        if (!CanUseVirtualFileCenter()) return false;

        var path = NormalizePath(virtualPath);
        if (path.Equals("Desktop", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.Equals("This PC", StringComparison.OrdinalIgnoreCase)) return HasAnyDrivePermission();
        if (IsPathOrChild(path, "Network"))
        {
            return _authorizationService.HasPermission("System.View");
        }

        if (IsPathOrChild(path, @"This PC\C: SYSTEM"))
        {
            return _authorizationService.HasPermission("System.View");
        }

        if (IsPathOrChild(path, @"This PC\D: ACCOUNTING"))
        {
            return _authorizationService.HasPermission("Accounting.View");
        }

        if (IsPathOrChild(path, @"This PC\E: SALES"))
        {
            if (path.Equals(@"This PC\E: SALES", StringComparison.OrdinalIgnoreCase))
            {
                return HasSalesOrFrontOfficeViewPermission();
            }

            if (IsPathOrChild(path, @"This PC\E: SALES\FO"))
            {
                return HasFrontOfficeViewPermission(path);
            }

            return _authorizationService.HasPermission("Sales.View");
        }

        if (IsPathOrChild(path, @"This PC\F: HR\Lương"))
        {
            return _authorizationService.HasPermission("HR.View")
                && _authorizationService.HasPermission("HR.PayrollView");
        }

        if (IsPathOrChild(path, @"This PC\F: HR"))
        {
            return _authorizationService.HasPermission("HR.View");
        }

        if (IsPathOrChild(path, @"This PC\G: LOGS"))
        {
            return _authorizationService.HasPermission("Logs.View");
        }

        return false;
    }

    private IReadOnlyList<VirtualExplorerNodeDto> FilterVisibleNodes(
        IReadOnlyList<VirtualExplorerNodeDto> nodes)
        => nodes.Where(CanAccessNode).ToList();

    private bool CanAccessNode(VirtualExplorerNodeDto node)
    {
        if (!CanAccessPath(node.VirtualPath)) return false;
        if (node.VirtualPath.Equals("Desktop", StringComparison.OrdinalIgnoreCase)) return true;
        if (node.VirtualPath.Equals("This PC", StringComparison.OrdinalIgnoreCase)) return HasAnyDrivePermission();
        if (IsPathOrChild(NormalizePath(node.VirtualPath), "Network"))
        {
            return _authorizationService.HasPermission("System.View");
        }

        if (node.Module == BusinessModule.Sales
            && node.VirtualPath.StartsWith(@"This PC\E: SALES\FO", StringComparison.OrdinalIgnoreCase))
        {
            return HasFrontOfficeViewPermission(node.VirtualPath);
        }

        var modulePermission = node.Module switch
        {
            BusinessModule.System => "System.View",
            BusinessModule.Accounting => "Accounting.View",
            BusinessModule.Sales => "Sales.View",
            BusinessModule.HR => "HR.View",
            BusinessModule.Logs => "Logs.View",
            BusinessModule.Network => "System.View",
            BusinessModule.Warehouse => "Warehouse.View",
            _ => null
        };

        return modulePermission is null || _authorizationService.HasPermission(modulePermission);
    }

    private bool HasAnyDrivePermission()
        => _authorizationService.HasPermission("System.View")
            || _authorizationService.HasPermission("Accounting.View")
            || HasSalesOrFrontOfficeViewPermission()
            || _authorizationService.HasPermission("HR.View")
            || _authorizationService.HasPermission("Logs.View")
            || _authorizationService.HasPermission("Warehouse.View");

    private bool HasSalesOrFrontOfficeViewPermission()
        => _authorizationService.HasPermission("Sales.View") || HasFrontOfficeViewPermission(null);

    private bool HasFrontOfficeViewPermission(string? path)
    {
        if (path?.Contains("Reservation", StringComparison.OrdinalIgnoreCase) == true)
        {
            return _authorizationService.HasPermission("FrontOffice.Reservation.View");
        }

        if (path?.Contains("Stay", StringComparison.OrdinalIgnoreCase) == true)
        {
            return _authorizationService.HasPermission("FrontOffice.Stay.View");
        }

        if (path?.Contains("Folio", StringComparison.OrdinalIgnoreCase) == true)
        {
            return _authorizationService.HasPermission("FrontOffice.Folio.View");
        }

        return _authorizationService.HasPermission("FrontOffice.Reservation.View")
            || _authorizationService.HasPermission("FrontOffice.Stay.View")
            || _authorizationService.HasPermission("FrontOffice.Folio.View");
    }

    private static string NormalizePath(string? path)
        => (path ?? string.Empty).Replace('/', '\\').Trim().TrimEnd('\\');

    private static bool IsPathOrChild(string path, string root)
        => path.Equals(root, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
}
