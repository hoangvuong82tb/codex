using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.Services;

public interface ISupplierImportExportService
{
    Task ExportTemplateAsync(string filePath, CancellationToken cancellationToken = default);
    Task ExportCurrentViewAsync(IEnumerable<SupplierDto> suppliers, string filePath, SupplierExportOptions? options = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierImportRow>> ReadImportRowsAsync(string filePath, CancellationToken cancellationToken = default);
}

