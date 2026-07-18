using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;
namespace ASMILE.BO.Infrastructure.Services;

public sealed class ImportEngine : IImportEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const long MaxImportFileBytes = 50L * 1024 * 1024;

    private readonly AppDbContext _dbContext;
    private readonly IReadOnlyDictionary<string, IImportTemplateHandler> _handlersByTarget;
    private readonly IRuntimeTelemetryService? _telemetryService;
    private readonly IAuditLogService? _auditLogService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _auditCorrelationService;

    public ImportEngine(AppDbContext dbContext, IEnumerable<IImportTemplateHandler> handlers, IAuditLogService? auditLogService = null, IAuditService? auditService = null, IAuditCorrelationService? auditCorrelationService = null, IRuntimeTelemetryService? telemetryService = null)
    {
        _dbContext = dbContext;
        _handlersByTarget = handlers.ToDictionary(x => x.TargetEntity, StringComparer.OrdinalIgnoreCase);
        _telemetryService = telemetryService;
        _auditLogService = auditLogService;
        _auditService = auditService;
        _auditCorrelationService = auditCorrelationService;
    }

    public async Task<ImportPreviewResult> PreviewAsync(ImportPreviewRequest request, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var sw = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(request.FilePath) || !File.Exists(request.FilePath))
        {
            throw new FileNotFoundException("Không tìm thấy file Excel cần nhập.", request.FilePath);
        }

        ValidateImportFile(request.FilePath);
        CleanupImportTempFiles();

        var template = await ResolveTemplateAsync(request, cancellationToken);
        var handler = ResolveHandler(template.TargetEntity);
        var tempFile = CopyToTemp(request.FilePath);
        var session = new ImportSession
        {
            Id = Guid.NewGuid(),
            TemplateCode = template.Code,
            FileName = Path.GetFileName(request.FilePath),
            OriginalFilePath = request.FilePath,
            TempFilePath = tempFile,
            TargetEntity = template.TargetEntity,
            Status = "Draft",
            StartedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        try
        {
            using var workbook = new XLWorkbook(tempFile);
            var worksheet = ResolveWorksheet(workbook, request.SheetName);
            var headers = ReadHeaders(worksheet, request.HeaderRowNumber);
            ValidateRequiredHeaders(handler, headers, session);

            if (!string.IsNullOrWhiteSpace(session.ErrorMessage))
            {
                session.Status = "Failed";
                session.CompletedAt = DateTime.UtcNow;
                _dbContext.ImportSessions.Add(session);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return ToPreviewResult(session, []);
            }

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? request.HeaderRowNumber;
            for (var rowNumber = request.HeaderRowNumber + 1; rowNumber <= lastRow; rowNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = ReadRow(worksheet, rowNumber, headers);
                if (values.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                session.Rows.Add(new ImportRow
                {
                    Id = Guid.NewGuid(),
                    ImportSessionId = session.Id,
                    RowNumber = rowNumber,
                    RawJson = JsonSerializer.Serialize(values, JsonOptions),
                    Status = "Valid"
                });
            }

            session.TotalRows = session.Rows.Count;
            _dbContext.ImportSessions.Add(session);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await ValidateSessionAsync(session, cancellationToken);
            await LogAsync("Import.Preview", $"Tạo phiên import {session.Id} cho {session.TargetEntity}.", session.CreatedBy ?? "system", cancellationToken);
            await AuditAsync("ImportSession", session.Id.ToString(), "Preview", null, new { session.TemplateCode, session.TargetEntity, session.Status, session.TotalRows }, session.CreatedBy, cancellationToken);
            if (_telemetryService is not null)
            {
                sw.Stop();
                await _telemetryService.TrackSlowOperationAsync("import", "preview", sw.ElapsedMilliseconds, "Information", "Import preview completed.", session.Id.ToString(), session.Id, new { session.TargetEntity, session.TotalRows }, cancellationToken);
            }
            return ToPreviewResult(session, session.Rows.OrderBy(x => x.RowNumber).ToList());
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            session.Status = "Failed";
            session.ErrorMessage = ex.Message;
            session.CompletedAt = DateTime.UtcNow;
            if (_dbContext.Entry(session).State == EntityState.Detached)
            {
            _dbContext.ImportSessions.Add(session);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("Import.PreviewFailed", ex.Message, session.CreatedBy ?? "system", cancellationToken);
        await AuditAsync("ImportSession", session.Id.ToString(), "PreviewFailed", null, new { session.Status, session.ErrorMessage }, session.CreatedBy, cancellationToken);
        if (_telemetryService is not null)
        {
            sw.Stop();
            await _telemetryService.TrackExceptionAsync(ex, "import", "preview", ex.Message, session.Id.ToString(), session.Id, new { session.TargetEntity, session.Status }, cancellationToken);
            await _telemetryService.TrackSlowOperationAsync("import", "preview", sw.ElapsedMilliseconds, "Warning", "Import preview failed.", session.Id.ToString(), session.Id, new { session.TargetEntity, session.Status }, cancellationToken);
        }
        return ToPreviewResult(session, []);
    }
    }

    public async Task<ImportPreviewResult> ValidateAsync(Guid importSessionId, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(importSessionId, cancellationToken);
        await ValidateSessionAsync(session, cancellationToken);
        return ToPreviewResult(session, session.Rows.OrderBy(x => x.RowNumber).ToList());
    }

    public async Task<ImportCommitResult> CommitAsync(Guid importSessionId, bool allowWarnings = false, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var sw = Stopwatch.StartNew();
        var session = await LoadSessionAsync(importSessionId, cancellationToken);
        await ValidateSessionAsync(session, cancellationToken);

        if (session.ErrorRows > 0)
        {
            return new ImportCommitResult
            {
                ImportSessionId = session.Id,
                Success = false,
                ErrorCount = session.ErrorRows,
                WarningCount = session.WarningRows,
                Message = "Không thể import khi còn dòng lỗi."
            };
        }

        if (session.WarningRows > 0 && !allowWarnings)
        {
            return new ImportCommitResult
            {
                ImportSessionId = session.Id,
                Success = false,
                WarningCount = session.WarningRows,
                RequiresConfirmation = true,
                Message = "File còn cảnh báo. Cần xác nhận trước khi commit."
            };
        }

        var handler = ResolveHandler(session.TargetEntity);
        var committedRows = await handler.CommitAsync(session, session.Rows.OrderBy(x => x.RowNumber).ToList(), cancellationToken);
        session.Status = "Committed";
        session.CompletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("Import.Commit", $"Import thành công {committedRows} dòng cho phiên {session.Id}.", session.CreatedBy ?? "system", cancellationToken);
        await AuditAsync("ImportSession", session.Id.ToString(), "Commit", new { session.Status }, new { session.Status, session.CompletedAt }, session.CreatedBy, cancellationToken);
        if (_telemetryService is not null)
        {
            sw.Stop();
            await _telemetryService.TrackSlowOperationAsync("import", "commit", sw.ElapsedMilliseconds, "Information", "Import commit completed.", session.Id.ToString(), session.Id, new { session.TargetEntity, committedRows }, cancellationToken);
        }

        return new ImportCommitResult
        {
            ImportSessionId = session.Id,
            Success = true,
            CommittedRows = committedRows,
            Message = $"Đã import {committedRows} dòng."
        };
    }

    public async Task<ImportSession?> GetSessionAsync(Guid importSessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ImportSessions
            .Include(x => x.Rows.OrderBy(row => row.RowNumber))
            .ThenInclude(x => x.CellErrors)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == importSessionId, cancellationToken);
    }

    public async Task<byte[]> ExportErrorsAsync(Guid importSessionId, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.ImportSessions
            .Include(x => x.Rows.OrderBy(row => row.RowNumber))
            .ThenInclude(x => x.CellErrors)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == importSessionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiên import.");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Lỗi import");
        var headers = new[] { "RowNumber", "ColumnName", "CellAddress", "Severity", "ErrorCode", "Message", "SuggestedValue" };
        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var rowIndex = 2;
        foreach (var row in session.Rows.OrderBy(x => x.RowNumber))
        {
            foreach (var error in row.CellErrors.OrderBy(x => x.Severity).ThenBy(x => x.ColumnName))
            {
                worksheet.Cell(rowIndex, 1).Value = row.RowNumber;
                worksheet.Cell(rowIndex, 2).Value = error.ColumnName;
                worksheet.Cell(rowIndex, 3).Value = error.CellAddress;
                worksheet.Cell(rowIndex, 4).Value = error.Severity;
                worksheet.Cell(rowIndex, 5).Value = error.ErrorCode;
                worksheet.Cell(rowIndex, 6).Value = error.Message;
                worksheet.Cell(rowIndex, 7).Value = error.SuggestedValue;
                rowIndex++;
            }
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task ValidateSessionAsync(ImportSession session, CancellationToken cancellationToken)
    {
        var handler = ResolveHandler(session.TargetEntity);
        var rows = session.Rows.OrderBy(x => x.RowNumber).ToList();

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (row.CellErrors.Count > 0)
            {
                _dbContext.ImportCellErrors.RemoveRange(row.CellErrors);
                row.CellErrors.Clear();
            }

            row.ErrorCount = 0;
            row.WarningCount = 0;
            row.Status = "Valid";
            await handler.ValidateRowAsync(row, ParseValues(row.RawJson), rows, cancellationToken);
            row.ErrorCount = row.CellErrors.Count(x => x.Severity == "Error");
            row.WarningCount = row.CellErrors.Count(x => x.Severity == "Warning");
            row.Status = row.ErrorCount > 0 ? "Error" : row.WarningCount > 0 ? "Warning" : "Valid";
        }

        session.TotalRows = rows.Count;
        session.ErrorRows = rows.Count(x => x.ErrorCount > 0);
        session.WarningRows = rows.Count(x => x.ErrorCount == 0 && x.WarningCount > 0);
        session.ValidRows = rows.Count(x => x.ErrorCount == 0 && x.WarningCount == 0);
        session.Status = session.ErrorRows > 0 || session.WarningRows > 0 ? "Validated" : "ReadyToCommit";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ImportSession> LoadSessionAsync(Guid importSessionId, CancellationToken cancellationToken)
    {
        return await _dbContext.ImportSessions
            .Include(x => x.Rows.OrderBy(row => row.RowNumber))
            .ThenInclude(x => x.CellErrors)
            .FirstOrDefaultAsync(x => x.Id == importSessionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiên import.");
    }

    private async Task<ImportTemplate> ResolveTemplateAsync(ImportPreviewRequest request, CancellationToken cancellationToken)
    {
        var query = _dbContext.ImportTemplates.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(request.TemplateCode))
        {
            query = query.Where(x => x.Code == request.TemplateCode);
        }
        else if (!string.IsNullOrWhiteSpace(request.TargetEntity))
        {
            query = query.Where(x => x.TargetEntity == request.TargetEntity);
        }

        var template = await query.OrderBy(x => x.Code).FirstOrDefaultAsync(cancellationToken);
        if (template is not null)
        {
            return template;
        }

        if (string.Equals(request.TemplateCode, "BANK_STATEMENT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.TargetEntity, "BankStatement", StringComparison.OrdinalIgnoreCase))
        {
            var now = DateTime.UtcNow;
            return new ImportTemplate
            {
                Id = Guid.NewGuid(),
                Code = "BANK_STATEMENT",
                Name = "Bank statement import",
                TargetEntity = "BankStatement",
                Description = "Preview, validate and commit bank statement rows.",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        throw new InvalidOperationException("Không tìm thấy template import phù hợp.");
    }

    private IImportTemplateHandler ResolveHandler(string targetEntity)
        => _handlersByTarget.TryGetValue(targetEntity, out var handler)
            ? handler
            : throw new InvalidOperationException($"Chưa có import handler cho {targetEntity}.");

    private static string CopyToTemp(string sourcePath)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ASMILE-BO", "imports");
        Directory.CreateDirectory(directory);
        var extension = Path.GetExtension(sourcePath);
        var target = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(sourcePath)}-{Guid.NewGuid():N}{extension}");
        File.Copy(sourcePath, target, overwrite: true);
        return target;
    }

    private static void ValidateImportFile(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".xlsm", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Định dạng import không được hỗ trợ.");
        }

        var fileInfo = new FileInfo(sourcePath);
        if (fileInfo.Length > MaxImportFileBytes)
        {
            throw new InvalidOperationException("File import quá lớn. Vui lòng tách file trước khi import.");
        }
    }

    private static void CleanupImportTempFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ASMILE-BO", "imports");
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            try
            {
                var info = new FileInfo(file);
                if (info.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-2))
                {
                    info.Delete();
                }
            }
            catch
            {
            }
        }
    }

    private static IXLWorksheet ResolveWorksheet(XLWorkbook workbook, string? sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            return workbook.Worksheets.First();
        }

        return workbook.Worksheets.FirstOrDefault(x => string.Equals(x.Name, sheetName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Không tìm thấy sheet {sheetName}.");
    }

    private static Dictionary<int, string> ReadHeaders(IXLWorksheet worksheet, int headerRowNumber)
    {
        var lastCell = worksheet.Row(headerRowNumber).LastCellUsed()?.Address.ColumnNumber ?? 0;
        var headers = new Dictionary<int, string>();
        for (var col = 1; col <= lastCell; col++)
        {
            var header = worksheet.Cell(headerRowNumber, col).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(header))
            {
                headers[col] = header;
            }
        }

        return headers;
    }

    private static Dictionary<string, string> ReadRow(IXLWorksheet worksheet, int rowNumber, IReadOnlyDictionary<int, string> headers)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (columnNumber, header) in headers)
        {
            values[header] = worksheet.Cell(rowNumber, columnNumber).GetFormattedString().Trim();
        }

        return values;
    }

    private static void ValidateRequiredHeaders(IImportTemplateHandler handler, IReadOnlyDictionary<int, string> headers, ImportSession session)
    {
        var existing = headers.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = handler.RequiredColumns.Where(x => !existing.Contains(x)).ToList();
        if (missing.Count > 0)
        {
            session.ErrorMessage = $"File thiếu cột bắt buộc: {string.Join(", ", missing)}.";
        }
    }

    private static Dictionary<string, string> ParseValues(string json)
        => string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static ImportPreviewResult ToPreviewResult(ImportSession session, IReadOnlyList<ImportRow> rows)
        => new()
        {
            ImportSessionId = session.Id,
            TemplateCode = session.TemplateCode,
            TargetEntity = session.TargetEntity,
            Status = session.Status,
            TotalRows = session.TotalRows,
            ValidRows = session.ValidRows,
            WarningRows = session.WarningRows,
            ErrorRows = session.ErrorRows,
            Messages = string.IsNullOrWhiteSpace(session.ErrorMessage) ? [] : [session.ErrorMessage],
            Rows = rows.Select(ToDto).ToList()
        };

    private static ImportRowDto ToDto(ImportRow row)
        => new()
        {
            Id = row.Id,
            RowNumber = row.RowNumber,
            Values = ParseValues(row.NormalizedJson ?? row.RawJson),
            Status = row.Status,
            ErrorCount = row.ErrorCount,
            WarningCount = row.WarningCount,
            Errors = row.CellErrors.Select(x => new ImportCellErrorDto
            {
                Id = x.Id,
                ColumnName = x.ColumnName,
                CellAddress = x.CellAddress,
                Severity = x.Severity,
                ErrorCode = x.ErrorCode,
                Message = x.Message,
                SuggestedValue = x.SuggestedValue
            }).ToList()
        };

    private async Task LogAsync(string action, string detail, string username, CancellationToken cancellationToken)
    {
        if (_auditLogService is not null)
        {
            await _auditLogService.LogAsync(action, detail, username, cancellationToken);
        }
    }

    private async Task AuditAsync(string entityName, string entityId, string action, object? oldValues, object? newValues, string? userName, CancellationToken cancellationToken)
    {
        if (_auditService is not null)
        {
            await _auditService.LogAsync(entityName, entityId, action, oldValues, newValues, userName, correlationId: _auditCorrelationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
        }
    }
}
