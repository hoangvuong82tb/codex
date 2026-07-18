namespace ASMILE.BO.App.Models;

public sealed class SupplierImportResult
{
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
}

