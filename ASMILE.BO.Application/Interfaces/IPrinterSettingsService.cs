using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IPrinterSettingsService
{
    Task<IReadOnlyList<PrinterDeviceDto>> GetAvailablePrintersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrinterPermissionDto>> GetPrinterPermissionsAsync(CancellationToken cancellationToken = default);
}
