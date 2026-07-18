using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.Services;

public interface IWarehouseImportExportService
{
    Task ExportTemplateAsync(string filePath, CancellationToken cancellationToken = default);
    Task ExportCurrentViewAsync(IEnumerable<WarehouseDto> warehouses, string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseImportRow>> ReadImportRowsAsync(string filePath, CancellationToken cancellationToken = default);
}
