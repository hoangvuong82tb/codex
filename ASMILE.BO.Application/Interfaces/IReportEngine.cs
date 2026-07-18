using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface IReportEngine
{
    Task<ReportResultDto> ExecuteAsync(ReportRequestDto request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportExcelAsync(ReportRequestDto request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(ReportRequestDto request, CancellationToken cancellationToken = default);
    Task ExportCsvStreamAsync(ReportRequestDto request, Stream destination, CancellationToken cancellationToken = default);
    Task<byte[]> ExportPdfAsync(ReportRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportTemplate>> GetTemplatesAsync(string? category = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportParameterDto>> GetParametersAsync(string reportCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportExecutionDto>> GetExecutionHistoryAsync(string? reportCode = null, int take = 50, CancellationToken cancellationToken = default);
}
