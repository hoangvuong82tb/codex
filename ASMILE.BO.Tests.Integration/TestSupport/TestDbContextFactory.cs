using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Diagnostics.Internal;

namespace ASMILE.BO.Tests.Integration.TestSupport;

internal static class TestDbContextFactory
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"asmile-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new AppDbContext(options);
        SeedAccountingCore(context);
        return context;
    }

    public static AuditLogService AuditLogService(AppDbContext context) => new(context);
    public static AccountingPeriodService PeriodService(AppDbContext context) => new(context, AuditLogService(context));
    public static PostingRuleService RuleService(AppDbContext context) => new(context);
    public static AuditService AuditService(AppDbContext context) => new(context);

    public static JournalService JournalService(AppDbContext context)
        => new(context, PeriodService(context), AuditLogService(context), null, null, AuditService(context));

    public static void SeedAccountingCore(AppDbContext context)
    {
        if (!context.AccountingStandards.Any())
        {
            context.AccountingStandards.Add(new AccountingStandard
            {
                Id = Guid.NewGuid(), Code = "TT99", Name = "Thông tư 99", IsDefault = true, IsActive = true,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
        }

        if (!context.AccountingPeriods.Any(x => x.PeriodCode == "2026-05"))
        {
            context.AccountingPeriods.Add(new AccountingPeriod
            {
                Id = Guid.NewGuid(), PeriodCode = "2026-05", FromDate = new DateTime(2026, 5, 1), ToDate = new DateTime(2026, 5, 31),
                Status = AccountingPeriodStatus.Open, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
        }

        var today = DateTime.Today;
        var currentPeriodCode = $"{today:yyyy-MM}";
        if (!context.AccountingPeriods.Any(x => x.PeriodCode == currentPeriodCode))
        {
            var periodStart = new DateTime(today.Year, today.Month, 1);
            context.AccountingPeriods.Add(new AccountingPeriod
            {
                Id = Guid.NewGuid(),
                PeriodCode = currentPeriodCode,
                FromDate = periodStart,
                ToDate = periodStart.AddMonths(1).AddDays(-1),
                Status = AccountingPeriodStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        AddAccount(context, "1111", "Tiền mặt", "Asset", "Debit");
        AddAccount(context, "1121", "Tiền gửi ngân hàng", "Asset", "Debit");
        AddAccount(context, "1311", "Phải thu khách hàng", "Asset", "Debit");
        AddAccount(context, "1331", "Thuế GTGT được khấu trừ", "Asset", "Debit");
        AddAccount(context, "153", "Công cụ dụng cụ", "Asset", "Debit");
        AddAccount(context, "1561", "Hàng hóa", "Asset", "Debit");
        AddAccount(context, "211", "Tài sản cố định", "Asset", "Debit");
        AddAccount(context, "214", "Hao mòn tài sản cố định", "Liability", "Credit");
        AddAccount(context, "242", "Chi phí trả trước", "Asset", "Debit");
        AddAccount(context, "3311", "Phải trả nhà cung cấp", "Liability", "Credit");
        AddAccount(context, "3331", "Thuế GTGT phải nộp", "Liability", "Credit");
        AddAccount(context, "421", "Lợi nhuận sau thuế chưa phân phối", "Equity", "Credit");
        AddAccount(context, "5111", "Doanh thu bán hàng", "Revenue", "Credit");
        AddAccount(context, "6321", "Giá vốn hàng bán", "Expense", "Debit");
        AddAccount(context, "642", "Chi phí quản lý", "Expense", "Debit");
        AddAccount(context, "911", "Xác định kết quả kinh doanh", "Other", "Both");

        AddRole(context, "Cash", "1111");
        AddRole(context, "Bank", "1121");
        AddRole(context, "Receivable", "1311");
        AddRole(context, "VATInput", "1331");
        AddRole(context, "VATOutput", "3331");
        AddRole(context, "Inventory", "1561");
        AddRole(context, "Payable", "3311");
        AddRole(context, "Revenue", "5111");
        AddRole(context, "COGS", "6321");
        AddRole(context, "Expense", "642");
        AddRole(context, "IncomeSummary", "911");
        AddRole(context, "RetainedEarnings", "421");
        AddRole(context, "FixedAsset", "211");
        AddRole(context, "Depreciation", "214");
        AddRole(context, "ToolAndSupply", "153");
        AddRole(context, "PrepaidExpense", "242");

        context.SaveChanges();
    }

    private static void AddAccount(AppDbContext context, string code, string name, string category, string normalBalance)
    {
        if (context.LedgerAccounts.Any(x => x.AccountCode == code)) return;
        context.LedgerAccounts.Add(new LedgerAccount
        {
            Id = Guid.NewGuid(), AccountCode = code, AccountName = name, ReportName = name,
            AccountLevel = code.Length <= 3 ? 1 : 2, AccountKind = "D", AccountNature = normalBalance == "Credit" ? "C" : "D",
            AccountType = category, AccountingStandardCode = "TT99", AccountCategory = category, NormalBalance = normalBalance,
            AllowPosting = true, IsActive = true, IsEditable = true, IsDeletable = true, IsFinancialAccount = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
    }

    private static void AddRole(AppDbContext context, string role, string accountCode)
    {
        if (context.AccountRoleMappings.Any(x => x.AccountingStandardCode == "TT99" && x.RoleCode == role && x.AccountCode == accountCode)) return;
        context.AccountRoleMappings.Add(new AccountRoleMapping
        {
            Id = Guid.NewGuid(), AccountingStandardCode = "TT99", RoleCode = role, AccountCode = accountCode,
            IsDefault = true, IsActive = true
        });
    }
}


