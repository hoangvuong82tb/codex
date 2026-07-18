namespace ASMILE.BO.Application.DTOs;

public sealed class ImportPreviewRequest
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public int HeaderRowNumber { get; set; } = 1;
    public string? SheetName { get; set; }
}

public sealed class ImportPreviewResult
{
    public Guid ImportSessionId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int WarningRows { get; set; }
    public int ErrorRows { get; set; }
    public bool IsValid => ErrorRows == 0;
    public IReadOnlyList<ImportRowDto> Rows { get; set; } = [];
    public IReadOnlyList<string> Messages { get; set; } = [];
}

public sealed class ImportCommitResult
{
    public Guid ImportSessionId { get; set; }
    public bool Success { get; set; }
    public int CommittedRows { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public bool RequiresConfirmation { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ImportRowDto
{
    public Guid Id { get; set; }
    public int RowNumber { get; set; }
    public IReadOnlyDictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    public string Status { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public IReadOnlyList<ImportCellErrorDto> Errors { get; set; } = [];
}

public sealed class ImportCellErrorDto
{
    public Guid Id { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string CellAddress { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SuggestedValue { get; set; }
}

public sealed class ImportColumnMapping
{
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetProperty { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsKey { get; set; }
}

public sealed class ImportValidationResult
{
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public bool IsValid => ErrorCount == 0;
    public IReadOnlyList<string> Messages { get; set; } = [];
}
