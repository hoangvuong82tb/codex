namespace ASMILE.BO.Infrastructure.Options;

public sealed class AccountingOptions
{
    public const string SectionName = "Accounting";

    public string DefaultStandardCode { get; set; } = "TT99";
    public bool EnforceBalancedJournal { get; set; } = true;
    public bool AllowPostingToLockedPeriod { get; set; }
    public bool AutoSyncDebtAndVat { get; set; } = true;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultStandardCode))
        {
            throw new InvalidOperationException("Accounting:DefaultStandardCode is required.");
        }
    }
}

public sealed class InventoryOptions
{
    public const string SectionName = "Inventory";

    public string CostMethod { get; set; } = "MovingAverage";
    public bool AllowNegativeStock { get; set; }
    public int ReportPageSize { get; set; } = 1000;

    public void Validate()
    {
        if (ReportPageSize <= 0)
        {
            throw new InvalidOperationException("Inventory:ReportPageSize must be greater than 0.");
        }
    }
}

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public bool AllowPlainTextSecretsOnNonWindows { get; set; } = true;
    public bool EnableAuditTrail { get; set; } = true;
    public long MaxImportFileBytes { get; set; } = 25 * 1024 * 1024;

    public void Validate()
    {
        if (MaxImportFileBytes <= 0)
        {
            throw new InvalidOperationException("Security:MaxImportFileBytes must be greater than 0.");
        }
    }
}
