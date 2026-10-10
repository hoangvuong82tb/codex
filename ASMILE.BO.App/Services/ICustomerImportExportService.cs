using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.Services;

public interface ICustomerImportExportService
{
    Task ExportTemplateAsync(string filePath, CancellationToken cancellationToken = default);
    Task ExportCurrentViewAsync(IEnumerable<CustomerDto> customers, string filePath, SupplierExportOptions? options = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierImportRow>> ReadImportRowsAsync(string filePath, CancellationToken cancellationToken = default);
}
