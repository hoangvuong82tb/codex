namespace ASMILE.BO.Application.DTOs;

public sealed class StatementLineDto
{
    public string LineCode { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public sealed class BalanceSheetDto
{
    public int FiscalYear { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public IReadOnlyList<StatementLineDto> Assets { get; set; } = [];
    public IReadOnlyList<StatementLineDto> Liabilities { get; set; } = [];
    public IReadOnlyList<StatementLineDto> Equity { get; set; } = [];
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilitiesAndEquity { get; set; }
    public decimal Difference { get; set; }
    public bool IsBalanced { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = [];
}

public sealed class IncomeStatementDto
{
    public int FiscalYear { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public decimal Revenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal OtherExpense { get; set; }
    public decimal ProfitBeforeTax { get; set; }
    public decimal ProfitAfterTax { get; set; }
    public IReadOnlyList<StatementLineDto> Lines { get; set; } = [];
    public IReadOnlyList<string> Warnings { get; set; } = [];
}

public sealed class CashFlowDto
{
    public int FiscalYear { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public decimal OperatingCashFlow { get; set; }
    public decimal InvestingCashFlow { get; set; }
    public decimal FinancingCashFlow { get; set; }
    public decimal NetCashFlow { get; set; }
    public IReadOnlyList<StatementLineDto> Lines { get; set; } = [];
    public IReadOnlyList<string> Warnings { get; set; } = [];
}

public sealed class AccountBalanceSnapshotDto
{
    public int FiscalYear { get; set; }
    public string Period { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
    public DateTime SnapshotDate { get; set; }
}

public sealed class ClosingEntryDto
{
    public Guid Id { get; set; }
    public int FiscalYear { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime ClosingDate { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public IReadOnlyList<ClosingEntryLineDto> Lines { get; set; } = [];
}

public sealed class ClosingEntryLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

public sealed class FinancialStatementSummaryDto
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilitiesAndEquity { get; set; }
    public decimal ProfitBeforeTax { get; set; }
    public bool IsBalanced { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = [];
}
