using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.Services;

public interface IItemImportExportService
{
    Task ExportTemplateAsync(string filePath, CancellationToken cancellationToken = default);
    Task ExportCurrentViewAsync(IEnumerable<ItemDto> items, string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemImportRow>> ReadImportRowsAsync(string filePath, CancellationToken cancellationToken = default);
}
