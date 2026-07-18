using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Infrastructure.Persistence;

internal static class AccountingStandardSeedData
{
    private static readonly DateTime SeededAt = new(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<AccountingStandard> Standards { get; } =
    [
        new()
        {
            Id = StableGuid("standard:TT99"),
            Code = "TT99",
            Name = "Thông tư 99",
            IsDefault = true,
            IsActive = true,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        },
        new()
        {
            Id = StableGuid("standard:TT200"),
            Code = "TT200",
            Name = "Thông tư 200",
            IsDefault = false,
            IsActive = true,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        },
        new()
        {
            Id = StableGuid("standard:CUSTOM"),
            Code = "CUSTOM",
            Name = "Tùy chỉnh",
            IsDefault = false,
            IsActive = true,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        }
    ];

    public static IReadOnlyList<AccountTemplate> AccountTemplates { get; } =
        BuildAccountTemplates("TT99").Concat(BuildAccountTemplates("TT200")).ToList();

    public static IReadOnlyList<AccountRoleMapping> RoleMappings { get; } =
        BuildRoleMappings("TT99").Concat(BuildRoleMappings("TT200")).ToList();

    public static IReadOnlyList<FinancialStatementMapping> FinancialMappings { get; } =
        BuildFinancialMappings("TT99").Concat(BuildFinancialMappings("TT200")).ToList();

    public static IReadOnlyList<AccountingStandardTemplate> StandardTemplates { get; } =
        BuildStandardTemplates("TT99").Concat(BuildStandardTemplates("TT200")).Concat(BuildStandardTemplates("CUSTOM")).ToList();

    private static IEnumerable<AccountTemplate> BuildAccountTemplates(string standardCode)
    {
        var rows = new[]
        {
            Account("111", "Tiền mặt", null, 1, "Asset", "Debit", false),
            Account("1111", "Tiền Việt Nam", "111", 2, "Asset", "Debit", true),
            Account("112", "Tiền gửi ngân hàng", null, 1, "Asset", "Debit", false),
            Account("1121", "Tiền Việt Nam gửi ngân hàng", "112", 2, "Asset", "Debit", true),
            Account("131", "Phải thu khách hàng", null, 1, "Asset", "Debit", false),
            Account("1311", "Phải thu khách hàng chi tiết", "131", 2, "Asset", "Debit", true, requireCustomer: true),
            Account("133", "Thuế GTGT được khấu trừ", null, 1, "Asset", "Debit", false),
            Account("1331", "Thuế GTGT đầu vào", "133", 2, "Asset", "Debit", true),
            Account("152", "Nguyên liệu, vật liệu", null, 1, "Asset", "Debit", true, requireWarehouse: true, requireItem: true),
            Account("153", "Công cụ, dụng cụ", null, 1, "Asset", "Debit", true, requireWarehouse: true, requireItem: true),
            Account("156", "Hàng hóa", null, 1, "Asset", "Debit", false),
            Account("1561", "Giá mua hàng hóa", "156", 2, "Asset", "Debit", true, requireWarehouse: true, requireItem: true),
            Account("211", "Tài sản cố định hữu hình", null, 1, "Asset", "Debit", true),
            Account("214", "Hao mòn tài sản cố định", null, 1, "Asset", "Credit", true),
            Account("242", "Chi phí trả trước", null, 1, "Asset", "Debit", true),
            Account("331", "Phải trả nhà cung cấp", null, 1, "Liability", "Credit", false),
            Account("3311", "Phải trả nhà cung cấp chi tiết", "331", 2, "Liability", "Credit", true, requireSupplier: true),
            Account("333", "Thuế và các khoản phải nộp Nhà nước", null, 1, "Liability", "Credit", false),
            Account("3331", "Thuế GTGT đầu ra", "333", 2, "Liability", "Credit", true),
            Account("334", "Phải trả người lao động", null, 1, "Liability", "Credit", true),
            Account("338", "Phải trả, phải nộp khác", null, 1, "Liability", "Credit", true),
            Account("411", "Vốn góp của chủ sở hữu", null, 1, "Equity", "Credit", true),
            Account("421", "Lợi nhuận sau thuế chưa phân phối", null, 1, "Equity", "Credit", true),
            Account("511", "Doanh thu bán hàng và cung cấp dịch vụ", null, 1, "Revenue", "Credit", false),
            Account("5111", "Doanh thu bán hàng hóa", "511", 2, "Revenue", "Credit", true),
            Account("632", "Giá vốn hàng bán", null, 1, "Expense", "Debit", false),
            Account("6321", "Giá vốn hàng hóa", "632", 2, "Expense", "Debit", true),
            Account("641", "Chi phí bán hàng", null, 1, "Expense", "Debit", true),
            Account("642", "Chi phí quản lý doanh nghiệp", null, 1, "Expense", "Debit", true),
            Account("711", "Thu nhập khác", null, 1, "Revenue", "Credit", true),
            Account("811", "Chi phí khác", null, 1, "Expense", "Debit", true),
            Account("911", "Xác định kết quả kinh doanh", null, 1, "Other", "Both", true)
        };

        var sortOrder = 10;
        foreach (var row in rows)
        {
            yield return new AccountTemplate
            {
                Id = StableGuid($"account-template:{standardCode}:{row.Code}"),
                StandardCode = standardCode,
                AccountCode = row.Code,
                AccountName = row.Name,
                ParentCode = row.Parent,
                AccountLevel = row.Level,
                AccountKind = row.AllowPosting ? "D" : "H",
                NormalBalance = row.NormalBalance,
                AccountCategory = row.Category,
                AllowPosting = row.AllowPosting,
                IsSystemAccount = true,
                IsFinancialAccount = row.Category != "Other",
                IsManagementAccount = row.Category == "Other",
                RequireCustomer = row.RequireCustomer,
                RequireSupplier = row.RequireSupplier,
                RequireWarehouse = row.RequireWarehouse,
                RequireItem = row.RequireItem,
                SortOrder = sortOrder,
                IsActive = true
            };
            sortOrder += 10;
        }
    }

    private static IEnumerable<AccountRoleMapping> BuildRoleMappings(string standardCode)
    {
        var rows = new[]
        {
            ("Cash", "1111"),
            ("Bank", "1121"),
            ("Receivable", "1311"),
            ("Inventory", "1561"),
            ("Payable", "3311"),
            ("VATInput", "1331"),
            ("VATOutput", "3331"),
            ("Revenue", "5111"),
            ("COGS", "6321"),
            ("Expense", "642"),
            ("FixedAsset", "211"),
            ("Depreciation", "214"),
            ("ToolAndSupply", "153"),
            ("PrepaidExpense", "242"),
            ("IncomeSummary", "911"),
            ("RetainedEarnings", "421")
        };

        foreach (var (role, account) in rows)
        {
            yield return new AccountRoleMapping
            {
                Id = StableGuid($"role-mapping:{standardCode}:{role}:{account}"),
                AccountingStandardCode = standardCode,
                RoleCode = role,
                AccountCode = account,
                IsDefault = true,
                IsActive = true
            };
        }
    }

    private static IEnumerable<FinancialStatementMapping> BuildFinancialMappings(string standardCode)
    {
        var rows = new[]
        {
            Mapping("BALANCE_SHEET", "CASH_AND_CASH_EQUIVALENTS", "Tiền và tương đương tiền", "111*,112*", "Cash,Bank", null, 10),
            Mapping("BALANCE_SHEET", "RECEIVABLES", "Phải thu khách hàng", "131*", "Receivable", null, 20),
            Mapping("BALANCE_SHEET", "INVENTORY", "Hàng tồn kho", "152*,153*,156*", "Inventory,ToolAndSupply", null, 30),
            Mapping("BALANCE_SHEET", "FIXED_ASSETS", "Tài sản cố định", "211*,214*", "FixedAsset,Depreciation", null, 40),
            Mapping("BALANCE_SHEET", "PAYABLES", "Phải trả nhà cung cấp", "331*", "Payable", null, 50),
            Mapping("BALANCE_SHEET", "TAX_PAYABLES", "Thuế và các khoản phải nộp", "333*", "VATOutput", null, 60),
            Mapping("BALANCE_SHEET", "EQUITY", "Vốn chủ sở hữu", "411*,421*", null, null, 70),
            Mapping("INCOME_STATEMENT", "REVENUE", "Doanh thu", "511*", "Revenue", null, 10),
            Mapping("INCOME_STATEMENT", "COGS", "Giá vốn hàng bán", "632*", "COGS", null, 20),
            Mapping("INCOME_STATEMENT", "GROSS_PROFIT", "Lợi nhuận gộp", null, null, "REVENUE - COGS", 30),
            Mapping("INCOME_STATEMENT", "SELLING_EXPENSE", "Chi phí bán hàng", "641*", null, null, 40),
            Mapping("INCOME_STATEMENT", "ADMIN_EXPENSE", "Chi phí quản lý doanh nghiệp", "642*", "Expense", null, 50),
            Mapping("INCOME_STATEMENT", "OTHER_INCOME", "Thu nhập khác", "711*", null, null, 60),
            Mapping("INCOME_STATEMENT", "OTHER_EXPENSE", "Chi phí khác", "811*", null, null, 70),
            Mapping("INCOME_STATEMENT", "PROFIT_BEFORE_TAX", "Lợi nhuận trước thuế", null, null, "GROSS_PROFIT - SELLING_EXPENSE - ADMIN_EXPENSE + OTHER_INCOME - OTHER_EXPENSE", 80)
        };

        foreach (var row in rows)
        {
            yield return new FinancialStatementMapping
            {
                Id = StableGuid($"financial-mapping:{standardCode}:{row.ReportCode}:{row.LineCode}"),
                StandardCode = standardCode,
                ReportCode = row.ReportCode,
                LineCode = row.LineCode,
                LineName = row.LineName,
                AccountFilter = row.AccountFilter,
                AccountRoleFilter = row.AccountRoleFilter,
                Formula = row.Formula,
                SortOrder = row.SortOrder,
                IsActive = true
            };
        }
    }

    private static IEnumerable<AccountingStandardTemplate> BuildStandardTemplates(string standardCode)
    {
        var templates = new[]
        {
            ("Account", $"{standardCode}_ACCOUNTS", "Mẫu hệ thống tài khoản"),
            ("RoleMapping", $"{standardCode}_ROLE_MAPPING", "Mẫu mapping vai trò tài khoản"),
            ("ReportMapping", $"{standardCode}_REPORT_MAPPING", "Mẫu mapping báo cáo tài chính"),
            ("PostingRule", $"{standardCode}_POSTING_RULE", "Mẫu quy tắc hạch toán")
        };

        foreach (var (type, code, name) in templates)
        {
            yield return new AccountingStandardTemplate
            {
                Id = StableGuid($"standard-template:{code}"),
                StandardCode = standardCode,
                TemplateType = type,
                TemplateCode = code,
                Name = name,
                DefinitionJson = JsonSerializer.Serialize(new { standardCode, type, version = 1 }),
                IsSystem = true,
                IsActive = true,
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            };
        }
    }

    private static (string Code, string Name, string? Parent, int Level, string Category, string NormalBalance, bool AllowPosting, bool RequireCustomer, bool RequireSupplier, bool RequireWarehouse, bool RequireItem) Account(
        string code,
        string name,
        string? parent,
        int level,
        string category,
        string normalBalance,
        bool allowPosting,
        bool requireCustomer = false,
        bool requireSupplier = false,
        bool requireWarehouse = false,
        bool requireItem = false)
        => (code, name, parent, level, category, normalBalance, allowPosting, requireCustomer, requireSupplier, requireWarehouse, requireItem);

    private static (string ReportCode, string LineCode, string LineName, string? AccountFilter, string? AccountRoleFilter, string? Formula, int SortOrder) Mapping(
        string reportCode,
        string lineCode,
        string lineName,
        string? accountFilter,
        string? roleFilter,
        string? formula,
        int sortOrder)
        => (reportCode, lineCode, lineName, accountFilter, roleFilter, formula, sortOrder);

    private static Guid StableGuid(string value)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash);
    }
}
