namespace ASMILE.BO.App.Models;

public enum SupplierImportIssueSeverity
{
    Warning = 1,
    Error = 2
}

public sealed class SupplierImportValidationIssue
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public SupplierImportIssueSeverity Severity { get; init; }
}

