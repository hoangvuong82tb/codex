using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.Services;

public interface IDepartmentPeopleImportExportService
{
    Task ExportDepartmentTemplateAsync(string filePath, CancellationToken cancellationToken = default);
    Task ExportPersonTemplateAsync(string filePath, CancellationToken cancellationToken = default);
    Task ExportCurrentViewAsync(IEnumerable<PersonDto> people, string filePath, CancellationToken cancellationToken = default);
    Task<DepartmentPeopleImportPayload> ReadImportPayloadAsync(string filePath, CancellationToken cancellationToken = default);
}
