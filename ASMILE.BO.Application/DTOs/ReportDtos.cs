namespace ASMILE.BO.Application.DTOs;

public sealed class ReportRequestDto
{
    public string ReportCode { get; set; } = string.Empty;
    public Dictionary<string, string?> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? ExecutedBy { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed class ReportResultDto
{
    public string ReportCode { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public IReadOnlyList<ReportColumnDto> Columns { get; set; } = [];
    public IReadOnlyList<ReportRowDto> Rows { get; set; } = [];
    public Dictionary<string, object?> Summary { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime GeneratedAt { get; set; }
    public int RowCount { get; set; }
}

public sealed class ReportColumnDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "Text";
}

public sealed class ReportRowDto
{
    public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ReportExecutionDto
{
    public Guid Id { get; set; }
    public string ReportCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RowCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class ReportParameterDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "Text";
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}

public sealed class JournalLinePerformanceDto
{
    public Guid JournalHeaderId { get; set; }
    public Guid JournalLineId { get; set; }
    public DateTime PostingDate { get; set; }
    public string JournalNo { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceNo { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

public sealed class DataScaleRequestDto
{
    public int JournalLineCount { get; set; } = 100_000;
    public int BatchSize { get; set; } = 5_000;
    public DateTime StartDate { get; set; } = new(2026, 1, 1);
    public string CreatedBy { get; set; } = "dev-scale-helper";
}

public sealed class PerformanceTimingDto
{
    public string Operation { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
    public int RowCount { get; set; }
    public DateTime MeasuredAt { get; set; }
    public string? Notes { get; set; }
}
