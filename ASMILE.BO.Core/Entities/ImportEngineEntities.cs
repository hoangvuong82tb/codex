namespace ASMILE.BO.Core.Entities;

public sealed class ImportTemplate
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ImportSession
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? OriginalFilePath { get; set; }
    public string? TempFilePath { get; set; }
    public string TargetEntity { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int WarningRows { get; set; }
    public int ErrorRows { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ErrorMessage { get; set; }
    public ICollection<ImportRow> Rows { get; set; } = new List<ImportRow>();
}

public sealed class ImportRow
{
    public Guid Id { get; set; }
    public Guid ImportSessionId { get; set; }
    public int RowNumber { get; set; }
    public string RawJson { get; set; } = "{}";
    public string? NormalizedJson { get; set; }
    public string Status { get; set; } = "Valid";
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public ImportSession? ImportSession { get; set; }
    public ICollection<ImportCellError> CellErrors { get; set; } = new List<ImportCellError>();
}

public sealed class ImportCellError
{
    public Guid Id { get; set; }
    public Guid ImportRowId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string CellAddress { get; set; } = string.Empty;
    public string Severity { get; set; } = "Error";
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SuggestedValue { get; set; }
    public ImportRow? ImportRow { get; set; }
}
