using System.Globalization;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;
namespace ASMILE.BO.Infrastructure.Services;

public sealed class ReportEngine : IReportEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxInteractiveRows = 50_000;
    private readonly AppDbContext _dbContext;
    private readonly IRuntimeTelemetryService? _telemetryService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _auditCorrelationService;
    private readonly ISettingsService? _settingsService;

    public ReportEngine(AppDbContext dbContext, IAuditService? auditService = null, IAuditCorrelationService? auditCorrelationService = null, IRuntimeTelemetryService? telemetryService = null, ISettingsService? settingsService = null)
    {
        _dbContext = dbContext;
        _telemetryService = telemetryService;
        _auditService = auditService;
        _auditCorrelationService = auditCorrelationService;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<ReportTemplate>> GetTemplatesAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ReportTemplates.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        var templates = await query.OrderBy(x => x.Category).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return MergeBuiltInInventoryTemplates(templates, category);
    }

    public async Task<IReadOnlyList<ReportParameterDto>> GetParametersAsync(string reportCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportCode))
        {
            return [];
        }

        var normalizedCode = reportCode.Trim().ToUpperInvariant();
        var persistedParameters = await _dbContext.ReportParameters
            .AsNoTracking()
            .Where(x => x.ReportTemplate != null
                        && x.ReportTemplate.Code == normalizedCode
                        && x.ReportTemplate.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ReportParameterDto
            {
                Name = x.Name,
                DisplayName = x.DisplayName,
                DataType = x.DataType,
                DefaultValue = x.DefaultValue,
                IsRequired = x.IsRequired,
                SortOrder = x.SortOrder
            })
            .ToListAsync(cancellationToken);

        if (persistedParameters.Count > 0)
        {
            return persistedParameters;
        }

        return BuildDefaultParameters(normalizedCode);
    }

    private static IReadOnlyList<ReportParameterDto> BuildDefaultParameters(string reportCode)
    {
        return reportCode switch
        {
            "GENERAL_LEDGER" => CreateParameters(
                ("fromDate", "Từ ngày", "Date", DateTime.Today.AddMonths(-1).ToString("dd/MM/yyyy"), true),
                ("toDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), true),
                ("accountCode", "Tài khoản", "Text", string.Empty, false),
                ("keyword", "Từ khóa / chứng từ", "Text", string.Empty, false)),

            "TRIAL_BALANCE" => CreateParameters(
                ("fromDate", "Từ ngày", "Date", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd/MM/yyyy"), true),
                ("toDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), true)),

            "INVENTORY_SUMMARY" or "INVENTORY_MOVEMENT" or "STOCK_CARD" or "COST_ANALYSIS" => CreateParameters(
                ("fromDate", "Từ ngày", "Date", DateTime.Today.AddMonths(-1).ToString("dd/MM/yyyy"), true),
                ("toDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), true),
                ("itemCode", "Mã hàng / từ khóa", "Text", string.Empty, false),
                ("warehouse", "Kho", "Text", string.Empty, false)),

            "LOW_STOCK" => CreateParameters(
                ("minQty", "Ngưỡng tồn tối thiểu", "Number", "10", true),
                ("itemCode", "Mã hàng / từ khóa", "Text", string.Empty, false),
                ("warehouse", "Kho", "Text", string.Empty, false)),

            "NEGATIVE_STOCK" => CreateParameters(
                ("itemCode", "Mã hàng / từ khóa", "Text", string.Empty, false),
                ("warehouse", "Kho", "Text", string.Empty, false)),

            "AP_AGING" or "AR_AGING" => CreateParameters(
                ("asOfDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), true)),

            "VAT_INPUT" or "VAT_OUTPUT" => CreateParameters(
                ("period", "Kỳ kê khai", "Text", DateTime.Today.ToString("yyyy-MM"), false),
                ("fromDate", "Từ ngày", "Date", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd/MM/yyyy"), true),
                ("toDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), true)),

            "BALANCE_SHEET" or "INCOME_STATEMENT" or "CASH_FLOW"
                or "TT99_BALANCE_SHEET" or "TT99_INCOME_STATEMENT" or "TT99_CASH_FLOW"
                or "TT200_BALANCE_SHEET" or "TT200_INCOME_STATEMENT" or "TT200_CASH_FLOW"
                or "TT99_ACCOUNT_MAPPING" or "TT99_COMPLIANCE_ISSUES" or "TT99_DECLARATION_SUMMARY"
                or "TT200_ACCOUNT_MAPPING" or "TT200_COMPLIANCE_ISSUES" or "TT200_DECLARATION_SUMMARY"
                or "TT200_CLOSING_SUMMARY" => CreateParameters(
                ("period", "Kỳ báo cáo", "Text", DateTime.Today.ToString("yyyy-MM"), true)),

            "CASH_BOOK" or "BANK_BOOK" or "BANK_RECONCILIATION" => CreateParameters(
                ("fromDate", "Từ ngày", "Date", DateTime.Today.AddMonths(-1).ToString("dd/MM/yyyy"), true),
                ("toDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), true),
                ("accountCode", "Quỹ / tài khoản", "Text", string.Empty, false)),

            "DAILY_CASHFLOW" or "CASH_BALANCE" => CreateParameters(
                ("fromDate", "Từ ngày", "Date", DateTime.Today.AddDays(-7).ToString("dd/MM/yyyy"), true),
                ("toDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), true)),

            "FIXED_ASSET_REGISTER" or "DEPRECIATION_SCHEDULE" or "ASSET_INCREASE_DECREASE"
                or "TOOL_ASSET_REGISTER" or "TOOL_ASSET_ALLOCATION" or "PREPAID_EXPENSE_SCHEDULE" => CreateParameters(
                ("fromDate", "Từ ngày", "Date", DateTime.Today.AddYears(-1).ToString("dd/MM/yyyy"), true),
                ("toDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), true)),

            "BREAKFAST_CAFE_DAILY_SALES" or "BREAKFAST_CAFE_PAYMENT_SUMMARY" or "BREAKFAST_CAFE_ITEM_SALES" or "BREAKFAST_CAFE_KITCHEN_TICKET"
                or "ALACARTE_ORDER_SUMMARY" or "ALACARTE_QUOTE_SUMMARY" or "ALACARTE_RESERVATION_SCHEDULE" or "ALACARTE_DEPOSIT_SUMMARY"
                or "BANQUET_EVENT_SUMMARY" or "BANQUET_CONTRACT_SUMMARY" or "BANQUET_DEPOSIT_SUMMARY" or "BANQUET_SERVICE_DETAIL" => CreateParameters(
                ("fromDate", "Từ ngày", "Date", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd/MM/yyyy"), true),
                ("toDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), true)),

            _ => CreateParameters(
                ("fromDate", "Từ ngày", "Date", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd/MM/yyyy"), false),
                ("toDate", "Đến ngày", "Date", DateTime.Today.ToString("dd/MM/yyyy"), false))
        };
    }

    private static IReadOnlyList<ReportParameterDto> CreateParameters(params (string Name, string DisplayName, string DataType, string DefaultValue, bool IsRequired)[] items)
        => items
            .Select((item, index) => new ReportParameterDto
            {
                Name = item.Name,
                DisplayName = item.DisplayName,
                DataType = item.DataType,
                DefaultValue = item.DefaultValue,
                IsRequired = item.IsRequired,
                SortOrder = index + 1
            })
            .ToList();

    private static IReadOnlyList<ReportTemplate> MergeBuiltInInventoryTemplates(IReadOnlyList<ReportTemplate> templates, string? category)
    {
        var merged = templates.ToList();
        var existingCodes = merged.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        foreach (var template in BuiltInInventoryTemplates(now)
                     .Concat(BuiltInCashBankTemplates(now))
                     .Concat(BuiltInAssetTemplates(now))
                     .Concat(BuiltInRestaurantTemplates(now)))
        {
            if (existingCodes.Contains(template.Code))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(template.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            merged.Add(template);
        }

        return merged.OrderBy(x => x.Category).ThenBy(x => x.Name).ToList();
    }

    private static IEnumerable<ReportTemplate> BuiltInInventoryTemplates(DateTime timestamp)
    {
        yield return BuiltInInventoryTemplate("INVENTORY_SUMMARY", "Tổng hợp tồn kho", "Tổng hợp tồn, giá trị tồn theo hàng và kho.", timestamp);
        yield return BuiltInInventoryTemplate("INVENTORY_MOVEMENT", "Nhập xuất tồn", "Chi tiết phát sinh nhập xuất theo kỳ.", timestamp);
        yield return BuiltInInventoryTemplate("STOCK_CARD", "Thẻ kho", "Thẻ kho theo hàng hóa và kho.", timestamp);
        yield return BuiltInInventoryTemplate("LOW_STOCK", "Cảnh báo tồn thấp", "Danh sách hàng dưới ngưỡng tồn tối thiểu.", timestamp);
        yield return BuiltInInventoryTemplate("NEGATIVE_STOCK", "Tồn âm", "Danh sách hàng có tồn âm.", timestamp);
        yield return BuiltInInventoryTemplate("COST_ANALYSIS", "Phân tích giá vốn", "Giá bình quân, tồn giá vốn và lịch sử giao dịch giá vốn.", timestamp);
    }

    private static ReportTemplate BuiltInInventoryTemplate(string code, string name, string description, DateTime timestamp) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name,
        Category = "Inventory",
        Description = description,
        DataSource = "StockBalance,StockReceipt,StockIssue,InventoryCostLayer,InventoryCostTransaction",
        IsSystem = true,
        IsActive = true,
        CreatedAt = timestamp,
        UpdatedAt = timestamp
    };

    private static IEnumerable<ReportTemplate> BuiltInCashBankTemplates(DateTime timestamp)
    {
        yield return BuiltInCashBankTemplate("CASH_BOOK", "Sổ quỹ tiền mặt", "Sổ quỹ theo quỹ tiền mặt và kỳ báo cáo.", timestamp);
        yield return BuiltInCashBankTemplate("BANK_BOOK", "Sổ tiền gửi ngân hàng", "Sổ ngân hàng theo tài khoản và kỳ báo cáo.", timestamp);
        yield return BuiltInCashBankTemplate("BANK_RECONCILIATION", "Đối chiếu ngân hàng", "Tổng hợp đối chiếu, matched/unmatched và chênh lệch.", timestamp);
        yield return BuiltInCashBankTemplate("DAILY_CASHFLOW", "Dòng tiền ngày", "Thu chi tiền mặt/ngân hàng theo ngày.", timestamp);
        yield return BuiltInCashBankTemplate("CASH_BALANCE", "Số dư tiền", "Số dư hiện hành theo quỹ và tài khoản ngân hàng.", timestamp);
    }

    private static ReportTemplate BuiltInCashBankTemplate(string code, string name, string description, DateTime timestamp) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name,
        Category = "CashBank",
        Description = description,
        DataSource = "CashFund,CashTransaction,BankAccount,BankTransaction,BankStatement,BankReconciliation",
        IsSystem = true,
        IsActive = true,
        CreatedAt = timestamp,
        UpdatedAt = timestamp
    };

    private static IEnumerable<ReportTemplate> BuiltInAssetTemplates(DateTime timestamp)
    {
        yield return BuiltInAssetTemplate("FIXED_ASSET_REGISTER", "Sổ tài sản cố định", "Danh sách TSCĐ, nguyên giá, khấu hao lũy kế, giá trị còn lại và trạng thái ghi sổ.", timestamp);
        yield return BuiltInAssetTemplate("DEPRECIATION_SCHEDULE", "Lịch khấu hao TSCĐ", "Lịch và lịch sử khấu hao TSCĐ theo kỳ.", timestamp);
        yield return BuiltInAssetTemplate("ASSET_INCREASE_DECREASE", "Tăng giảm TSCĐ", "Biến động tăng giảm và điều chỉnh TSCĐ.", timestamp);
        yield return BuiltInAssetTemplate("TOOL_ASSET_REGISTER", "Sổ công cụ dụng cụ", "Danh sách CCDC, phân bổ lũy kế, còn lại và trạng thái ghi sổ.", timestamp);
        yield return BuiltInAssetTemplate("TOOL_ASSET_ALLOCATION", "Phân bổ CCDC", "Lịch và lịch sử phân bổ công cụ dụng cụ.", timestamp);
        yield return BuiltInAssetTemplate("PREPAID_EXPENSE_SCHEDULE", "Lịch chi phí trả trước", "Theo dõi CCDC phân bổ nhiều kỳ qua tài khoản chi phí trả trước.", timestamp);
    }

    private static ReportTemplate BuiltInAssetTemplate(string code, string name, string description, DateTime timestamp) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name,
        Category = "Asset",
        Description = description,
        DataSource = "FixedAsset,FixedAssetDepreciation,FixedAssetAdjustment,ToolAsset,ToolAllocationSchedule",
        IsSystem = true,
        IsActive = true,
        CreatedAt = timestamp,
        UpdatedAt = timestamp
    };

    private static IEnumerable<ReportTemplate> BuiltInRestaurantTemplates(DateTime timestamp)
    {
        yield return BuiltInRestaurantTemplate(
            "BREAKFAST_CAFE_DAILY_SALES",
            "Tổng hợp bán hàng ăn sáng và cà phê",
            "Theo dõi đơn bán hàng, bàn, khách và doanh thu của module Ăn sáng - Cà phê.",
            "BreakfastCafe",
            "BreakfastCafeOrder,BreakfastCafePayment,PosTable",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "ALACARTE_ORDER_SUMMARY",
            "Tổng hợp đơn À La Carte",
            "Theo dõi đơn mở, khách, khu vực/phòng và doanh thu thực tế của module À La Carte.",
            "Alacarte",
            "AlacarteOrder,RestaurantQuote,DiningRoom,RestaurantDeposit",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "BREAKFAST_CAFE_PAYMENT_SUMMARY",
            "Chi tiết thanh toán ăn sáng và cà phê",
            "Theo dõi chứng từ thanh toán, phương thức thu và người thu của module Ăn sáng - Cà phê.",
            "BreakfastCafe",
            "BreakfastCafePayment,BreakfastCafeOrder",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "BREAKFAST_CAFE_ITEM_SALES",
            "Top món bán ăn sáng và cà phê",
            "Thống kê món bán, số lượng và doanh thu từ các order line của module Ăn sáng - Cà phê.",
            "BreakfastCafe",
            "BreakfastCafeOrderLine,BreakfastCafeOrder",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "BREAKFAST_CAFE_KITCHEN_TICKET",
            "Phiếu bếp/bar ăn sáng và cà phê",
            "Theo dõi trạng thái gửi bếp/bar của module Ăn sáng - Cà phê.",
            "BreakfastCafe",
            "BreakfastCafeKitchenTicket,BreakfastCafeOrder",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "BANQUET_EVENT_SUMMARY",
            "Tổng hợp sự kiện tiệc",
            "Theo dõi lịch tiệc, khách hàng, sảnh và tình trạng hợp đồng/đặt cọc của module Tiệc.",
            "Banquet",
            "BanquetEvent,BanquetRoom,BanquetContract,BanquetDeposit",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "ALACARTE_QUOTE_SUMMARY",
            "Tổng hợp báo giá À La Carte",
            "Theo dõi báo giá, khách hàng, ngày phục vụ và trạng thái phê duyệt của module À La Carte.",
            "Alacarte",
            "RestaurantQuote,RestaurantQuoteLine,DiningRoom",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "ALACARTE_RESERVATION_SCHEDULE",
            "Lịch đặt bàn À La Carte",
            "Theo dõi booking, khách, phòng/bàn và trạng thái đặt chỗ của module À La Carte.",
            "Alacarte",
            "TableReservation,DiningRoom,DiningTable",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "ALACARTE_DEPOSIT_SUMMARY",
            "Tổng hợp đặt cọc À La Carte",
            "Theo dõi phiếu đặt cọc liên quan đơn, hợp đồng và booking của module À La Carte.",
            "Alacarte",
            "RestaurantDeposit,AlacarteOrder,RestaurantContract,TableReservation",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "BANQUET_CONTRACT_SUMMARY",
            "Tổng hợp hợp đồng tiệc",
            "Theo dõi giá trị hợp đồng, đặt cọc và số dư còn lại của module Tiệc.",
            "Banquet",
            "BanquetContract,BanquetEvent,BanquetDeposit",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "BANQUET_DEPOSIT_SUMMARY",
            "Chi tiết đặt cọc tiệc",
            "Theo dõi phiếu thu đặt cọc, phương thức thanh toán và người nhận của module Tiệc.",
            "Banquet",
            "BanquetDeposit,BanquetContract,BanquetEvent",
            timestamp);
        yield return BuiltInRestaurantTemplate(
            "BANQUET_SERVICE_DETAIL",
            "Chi tiết dịch vụ tiệc",
            "Theo dõi từng dịch vụ phát sinh theo sự kiện tiệc, nhà cung cấp và giá trị.",
            "Banquet",
            "BanquetServiceLine,BanquetEvent,Supplier",
            timestamp);
    }

    private static ReportTemplate BuiltInRestaurantTemplate(string code, string name, string description, string category, string dataSource, DateTime timestamp) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name,
        Category = category,
        Description = description,
        DataSource = dataSource,
        IsSystem = true,
        IsActive = true,
        CreatedAt = timestamp,
        UpdatedAt = timestamp
    };

    public async Task<IReadOnlyList<ReportExecutionDto>> GetExecutionHistoryAsync(string? reportCode = null, int take = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ReportExecutions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(reportCode))
        {
            var normalizedCode = reportCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.ReportCode == normalizedCode);
        }

        return await query
            .OrderByDescending(x => x.StartedAt)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new ReportExecutionDto
            {
                Id = x.Id,
                ReportCode = x.ReportCode,
                Status = x.Status,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                RowCount = x.RowCount,
                ErrorMessage = x.ErrorMessage
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ReportResultDto> ExecuteAsync(ReportRequestDto request, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var sw = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(request.ReportCode))
        {
            throw new ArgumentException("Mã báo cáo là bắt buộc.", nameof(request));
        }

        var execution = new ReportExecution
        {
            Id = Guid.NewGuid(),
            ReportCode = request.ReportCode.Trim().ToUpperInvariant(),
            ParametersJson = JsonSerializer.Serialize(request.Parameters, JsonOptions),
            Status = "Running",
            StartedAt = DateTime.UtcNow,
            ExecutedBy = request.ExecutedBy
        };
        _dbContext.ReportExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var result = execution.ReportCode switch
            {
                "GENERAL_LEDGER" => await BuildGeneralLedgerAsync(request, cancellationToken),
                "TRIAL_BALANCE" => await BuildTrialBalanceAsync(request, cancellationToken),
                "INVENTORY_SUMMARY" => await BuildInventorySummaryAsync(request, cancellationToken),
                "INVENTORY_MOVEMENT" => await BuildInventoryMovementAsync(request, cancellationToken),
                "STOCK_CARD" => await BuildStockCardAsync(request, cancellationToken),
                "LOW_STOCK" => await BuildLowStockAsync(request, cancellationToken),
                "NEGATIVE_STOCK" => await BuildNegativeStockAsync(request, cancellationToken),
                "COST_ANALYSIS" => await BuildCostAnalysisAsync(request, cancellationToken),
                "AP_AGING" => await BuildPayableAgingAsync(request, cancellationToken),
                "AR_AGING" => await BuildReceivableAgingAsync(request, cancellationToken),
                "VAT_INPUT" => await BuildVatReportAsync(request, "Input", "VAT_INPUT", "Bảng kê VAT đầu vào", cancellationToken),
                "VAT_OUTPUT" => await BuildVatReportAsync(request, "Output", "VAT_OUTPUT", "Bảng kê VAT đầu ra", cancellationToken),
                "BALANCE_SHEET" => await BuildBalanceSheetAsync(request, cancellationToken),
                "INCOME_STATEMENT" => await BuildIncomeStatementAsync(request, cancellationToken),
                "CASH_FLOW" => await BuildCashFlowAsync(request, cancellationToken),
                "CASH_BOOK" => await BuildCashBookAsync(request, cancellationToken),
                "BANK_BOOK" => await BuildBankBookAsync(request, cancellationToken),
                "BANK_RECONCILIATION" => await BuildBankReconciliationAsync(request, cancellationToken),
                "DAILY_CASHFLOW" => await BuildDailyCashflowAsync(request, cancellationToken),
                "CASH_BALANCE" => await BuildCashBalanceAsync(request, cancellationToken),
                "FIXED_ASSET_REGISTER" => await BuildFixedAssetRegisterAsync(request, cancellationToken),
                "DEPRECIATION_SCHEDULE" => await BuildDepreciationScheduleReportAsync(request, cancellationToken),
                "ASSET_INCREASE_DECREASE" => await BuildAssetIncreaseDecreaseAsync(request, cancellationToken),
                "TOOL_ASSET_REGISTER" => await BuildToolAssetRegisterAsync(request, cancellationToken),
                "TOOL_ASSET_ALLOCATION" => await BuildToolAssetAllocationAsync(request, cancellationToken),
                "PREPAID_EXPENSE_SCHEDULE" => await BuildPrepaidExpenseScheduleAsync(request, cancellationToken),
                "BREAKFAST_CAFE_DAILY_SALES" => await BuildBreakfastCafeDailySalesAsync(request, cancellationToken),
                "BREAKFAST_CAFE_PAYMENT_SUMMARY" => await BuildBreakfastCafePaymentSummaryAsync(request, cancellationToken),
                "BREAKFAST_CAFE_ITEM_SALES" => await BuildBreakfastCafeItemSalesAsync(request, cancellationToken),
                "BREAKFAST_CAFE_KITCHEN_TICKET" => await BuildBreakfastCafeKitchenTicketAsync(request, cancellationToken),
                "ALACARTE_ORDER_SUMMARY" => await BuildAlacarteOrderSummaryAsync(request, cancellationToken),
                "ALACARTE_QUOTE_SUMMARY" => await BuildAlacarteQuoteSummaryAsync(request, cancellationToken),
                "ALACARTE_RESERVATION_SCHEDULE" => await BuildAlacarteReservationScheduleAsync(request, cancellationToken),
                "ALACARTE_DEPOSIT_SUMMARY" => await BuildAlacarteDepositSummaryAsync(request, cancellationToken),
                "BANQUET_EVENT_SUMMARY" => await BuildBanquetEventSummaryAsync(request, cancellationToken),
                "BANQUET_CONTRACT_SUMMARY" => await BuildBanquetContractSummaryAsync(request, cancellationToken),
                "BANQUET_DEPOSIT_SUMMARY" => await BuildBanquetDepositSummaryAsync(request, cancellationToken),
                "BANQUET_SERVICE_DETAIL" => await BuildBanquetServiceDetailAsync(request, cancellationToken),
                "TT99_BALANCE_SHEET" => await BuildBalanceSheetAsync(request, cancellationToken),
                "TT99_INCOME_STATEMENT" => await BuildIncomeStatementAsync(request, cancellationToken),
                "TT99_CASH_FLOW" => await BuildCashFlowAsync(request, cancellationToken),
                "TT99_ACCOUNT_MAPPING" => await BuildTt99AccountMappingAsync(cancellationToken),
                "TT99_COMPLIANCE_ISSUES" => await BuildTt99ComplianceIssuesAsync(cancellationToken),
                "TT99_DECLARATION_SUMMARY" => await BuildTt99DeclarationSummaryAsync(cancellationToken),
                "TT200_BALANCE_SHEET" => await BuildBalanceSheetAsync(request, cancellationToken),
                "TT200_INCOME_STATEMENT" => await BuildIncomeStatementAsync(request, cancellationToken),
                "TT200_CASH_FLOW" => await BuildCashFlowAsync(request, cancellationToken),
                "TT200_CLOSING_SUMMARY" => await BuildTt200ClosingSummaryAsync(cancellationToken),
                "TT200_ACCOUNT_MAPPING" => await BuildTt200AccountMappingAsync(cancellationToken),
                "TT200_COMPLIANCE_ISSUES" => await BuildTt200ComplianceIssuesAsync(cancellationToken),
                "TT200_DECLARATION_SUMMARY" => await BuildTt200DeclarationSummaryAsync(cancellationToken),
                _ => throw new InvalidOperationException($"Chưa hỗ trợ mã báo cáo {request.ReportCode}.")
            };

            execution.Status = "Completed";
            execution.CompletedAt = DateTime.UtcNow;
            execution.RowCount = result.RowCount;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await AuditAsync("ReportExecution", execution.Id.ToString(), "Completed", null, new { execution.ReportCode, execution.Status, execution.RowCount }, request.ExecutedBy, cancellationToken);
            if (_telemetryService is not null)
            {
                sw.Stop();
                await _telemetryService.TrackSlowOperationAsync("report", execution.ReportCode, sw.ElapsedMilliseconds, "Information", "Report execution completed.", _auditCorrelationService?.CurrentCorrelationId, null, new { execution.RowCount }, cancellationToken);
            }
            return result;
        }
        catch (Exception ex)
        {
            execution.Status = "Failed";
            execution.CompletedAt = DateTime.UtcNow;
            execution.ErrorMessage = ex.Message;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await AuditAsync("ReportExecution", execution.Id.ToString(), "Failed", null, new { execution.ReportCode, execution.Status, execution.ErrorMessage }, request.ExecutedBy, cancellationToken);
            if (_telemetryService is not null)
            {
                sw.Stop();
                await _telemetryService.TrackExceptionAsync(ex, "report", execution.ReportCode, ex.Message, _auditCorrelationService?.CurrentCorrelationId, null, new { execution.ReportCode, execution.Status }, cancellationToken);
                await _telemetryService.TrackSlowOperationAsync("report", execution.ReportCode, sw.ElapsedMilliseconds, "Warning", "Report execution failed.", _auditCorrelationService?.CurrentCorrelationId, null, new { execution.Status }, cancellationToken);
            }
            throw;
        }
    }

    public async Task<byte[]> ExportExcelAsync(ReportRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(request, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet(SafeSheetName(result.ReportCode));
        worksheet.Cell(1, 1).Value = result.ReportName;
        worksheet.Range(1, 1, 1, Math.Max(1, result.Columns.Count)).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        worksheet.Cell(2, 1).Value = $"Ngày lập: {result.GeneratedAt:dd/MM/yyyy HH:mm}";

        var headerRow = 4;
        for (var i = 0; i < result.Columns.Count; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = result.Columns[i].DisplayName;
            cell.Style.Font.SetBold();
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EAF2FF"));
        }

        var rowIndex = headerRow + 1;
        foreach (var row in result.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var i = 0; i < result.Columns.Count; i++)
            {
                var column = result.Columns[i];
                var value = row.Values.TryGetValue(column.Name, out var raw) ? raw : null;
                worksheet.Cell(rowIndex, i + 1).Value = ToCellValue(value);
            }

            rowIndex++;
        }

        var used = worksheet.RangeUsed();
        if (used is not null)
        {
            used.SetAutoFilter();
            used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            used.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        worksheet.SheetView.FreezeRows(headerRow);
        if (result.RowCount <= 5000)
        {
            worksheet.Columns().AdjustToContents();
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportCsvAsync(ReportRequestDto request, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await ExportCsvStreamAsync(request, stream, cancellationToken);
        return stream.ToArray();
    }

    public async Task ExportCsvStreamAsync(ReportRequestDto request, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var result = await ExecuteAsync(request, cancellationToken);
        await using var writer = new StreamWriter(destination, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
        await writer.WriteLineAsync(string.Join(",", result.Columns.Select(x => EscapeCsv(x.DisplayName))).AsMemory(), cancellationToken);
        foreach (var row in result.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = string.Join(",", result.Columns.Select(c => EscapeCsv(FormatValue(row.Values.TryGetValue(c.Name, out var value) ? value : null))));
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    public async Task<byte[]> ExportPdfAsync(ReportRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(request, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var presentation = await GetPdfPresentationAsync(result.ReportCode, cancellationToken);
        return SimplePdfWriter.Write(result, presentation);
    }

    private async Task<LedgerPdfPresentation?> GetPdfPresentationAsync(string reportCode, CancellationToken cancellationToken)
    {
        if (_settingsService is null
            || !new[] { "GENERAL_LEDGER", "TRIAL_BALANCE" }.Contains(reportCode, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var footer = await _settingsService.GetValueAsync("GeneralLedger.ReportFooterNote", SettingScope.App, "general-ledger");
            var accountant = await _settingsService.GetValueAsync("GeneralLedger.ShowAccountantSignature", SettingScope.App, "general-ledger");
            var director = await _settingsService.GetValueAsync("GeneralLedger.ShowDirectorSignature", SettingScope.App, "general-ledger");
            cancellationToken.ThrowIfCancellationRequested();

            return new LedgerPdfPresentation(
                footer?.Value ?? "Báo cáo được lập từ dữ liệu sổ cái.",
                !bool.TryParse(accountant?.Value, out var showAccountant) || showAccountant,
                !bool.TryParse(director?.Value, out var showDirector) || showDirector);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // PDF export remains available with documented defaults if the optional settings provider is unavailable.
            return new LedgerPdfPresentation("Báo cáo được lập từ dữ liệu sổ cái.", true, true);
        }
    }

    private sealed record LedgerPdfPresentation(string FooterNote, bool ShowAccountantSignature, bool ShowDirectorSignature);

    private async Task<ReportResultDto> BuildGeneralLedgerAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddMonths(-1).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var accountCode = GetString(request, "accountCode");
        var keyword = GetString(request, "keyword") ?? GetString(request, "sourceNo");

        var query = _dbContext.JournalLines
            .AsNoTracking()
            .Where(x => x.JournalHeader != null
                && x.JournalHeader.Status == JournalStatus.Posted
                && x.JournalHeader.PostingDate >= fromDate
                && x.JournalHeader.PostingDate <= toDate);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.JournalHeader!.JournalNo.Contains(keyword)
                || x.JournalHeader.SourceNo.Contains(keyword)
                || x.JournalHeader.Description.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(accountCode))
        {
            query = query.Where(x => x.AccountCode == accountCode);
        }

        var rowSources = await query
            .OrderBy(x => x.JournalHeader!.PostingDate)
            .ThenBy(x => x.JournalHeader!.JournalNo)
            .ThenBy(x => x.LineNo)
            .Select(x => new
            {
                Date = x.JournalHeader!.PostingDate,
                x.JournalHeader.JournalNo,
                x.JournalHeader.SourceType,
                x.JournalHeader.SourceNo,
                HeaderDescription = x.JournalHeader.Description,
                x.Description,
                x.AccountCode,
                x.AccountName,
                Debit = x.DebitAmount,
                Credit = x.CreditAmount
            })
            .Take(GetEffectiveTake(request))
            .ToListAsync(cancellationToken);

        var rows = rowSources
            .Select(x => Row(
                ("Date", x.Date),
                ("JournalNo", x.JournalNo),
                ("SourceType", x.SourceType),
                ("SourceNo", x.SourceNo),
                ("Description", x.Description ?? x.HeaderDescription),
                ("AccountCode", x.AccountCode),
                ("AccountName", x.AccountName),
                ("Debit", x.Debit),
                ("Credit", x.Credit)))
            .ToList();

        return Result("GENERAL_LEDGER", "Sổ cái", LedgerColumns(), rows,
            Summary(("TotalDebit", rows.Sum(x => GetDecimal(x, "Debit"))), ("TotalCredit", rows.Sum(x => GetDecimal(x, "Credit")))));
    }

    private async Task<ReportResultDto> BuildTrialBalanceAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);

        var lines = await _dbContext.JournalHeaders
            .AsNoTracking()
            .Where(x => x.Status == JournalStatus.Posted && x.PostingDate <= toDate)
            .SelectMany(x => x.Lines.Select(line => new
            {
                x.PostingDate,
                line.AccountCode,
                line.AccountName,
                line.DebitAmount,
                line.CreditAmount
            }))
            .ToListAsync(cancellationToken);

        var rows = lines
            .GroupBy(x => x.AccountCode)
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var openingDebit = group.Where(x => x.PostingDate < fromDate).Sum(x => x.DebitAmount);
                var openingCredit = group.Where(x => x.PostingDate < fromDate).Sum(x => x.CreditAmount);
                var periodDebit = group.Where(x => x.PostingDate >= fromDate).Sum(x => x.DebitAmount);
                var periodCredit = group.Where(x => x.PostingDate >= fromDate).Sum(x => x.CreditAmount);
                var netClosing = openingDebit - openingCredit + periodDebit - periodCredit;
                return Row(
                    ("AccountCode", group.Key),
                    ("AccountName", group.Select(x => x.AccountName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty),
                    ("OpeningDebit", Math.Max(netClosing >= 0 ? openingDebit - openingCredit : 0, 0)),
                    ("OpeningCredit", Math.Max(netClosing < 0 ? openingCredit - openingDebit : 0, 0)),
                    ("PeriodDebit", periodDebit),
                    ("PeriodCredit", periodCredit),
                    ("ClosingDebit", Math.Max(netClosing, 0)),
                    ("ClosingCredit", Math.Max(-netClosing, 0)));
            })
            .ToList();

        return Result("TRIAL_BALANCE", "Cân đối phát sinh", TrialBalanceColumns(), rows,
            Summary(("PeriodDebit", rows.Sum(x => GetDecimal(x, "PeriodDebit"))), ("PeriodCredit", rows.Sum(x => GetDecimal(x, "PeriodCredit")))));
    }

    private async Task<ReportResultDto> BuildInventorySummaryAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var balances = await _dbContext.StockBalances.AsNoTracking().ToListAsync(cancellationToken);
        var items = await _dbContext.Items.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var warehouses = await _dbContext.Warehouses.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);

        var rows = balances
            .OrderBy(x => items.TryGetValue(x.ItemId, out var item) ? item.ItemCode : x.ItemId.ToString(CultureInfo.InvariantCulture))
            .Select(x =>
            {
                items.TryGetValue(x.ItemId, out var item);
                warehouses.TryGetValue(x.WarehouseId, out var warehouse);
                return Row(
                    ("ItemCode", item?.ItemCode ?? x.ItemId.ToString(CultureInfo.InvariantCulture)),
                    ("ItemName", item?.ItemName ?? string.Empty),
                    ("Warehouse", warehouse?.WarehouseName ?? x.WarehouseId.ToString(CultureInfo.InvariantCulture)),
                    ("OpeningQty", x.OpeningQty),
                    ("InQty", x.InQty),
                    ("OutQty", x.OutQty),
                    ("ClosingQty", x.ClosingQty),
                    ("ClosingCost", x.ClosingAmount));
            })
            .ToList();

        return Result("INVENTORY_SUMMARY", "Tổng hợp tồn kho", InventorySummaryColumns(), rows,
            Summary(("ClosingQty", rows.Sum(x => GetDecimal(x, "ClosingQty"))), ("ClosingCost", rows.Sum(x => GetDecimal(x, "ClosingCost")))));
    }

    private async Task<ReportResultDto> BuildInventoryMovementAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddMonths(-1).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var items = await _dbContext.Items.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var warehouses = await _dbContext.Warehouses.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);

        var receiptRows = await _dbContext.StockReceipts
            .AsNoTracking()
            .Include(x => x.Details)
            .Where(x => x.PostingStatus != "Reversed" && x.ReceiptDate >= fromDate && x.ReceiptDate <= toDate)
            .SelectMany(x => x.Details.Select(d => new { Date = x.ReceiptDate, VoucherNo = x.ReceiptNo, Type = "Nhập", d.ItemId, WarehouseId = d.WarehouseId ?? x.WarehouseId, QtyIn = d.Quantity, QtyOut = 0m, UnitCost = d.UnitPrice, TotalCost = d.Amount }))
            .ToListAsync(cancellationToken);

        var issueCosts = await _dbContext.InventoryCostTransactions
            .AsNoTracking()
            .Where(x => x.SourceType == "StockIssue" && x.SourceDetailId.HasValue)
            .ToDictionaryAsync(x => x.SourceDetailId!.Value, x => x, cancellationToken);

        var issueRows = await _dbContext.StockIssues
            .AsNoTracking()
            .Include(x => x.Details)
            .Where(x => x.PostingStatus != "Reversed" && x.IssueDate >= fromDate && x.IssueDate <= toDate)
            .SelectMany(x => x.Details.Select(d => new { Date = x.IssueDate, VoucherNo = x.IssueNo, Type = "Xuất", d.Id, d.ItemId, WarehouseId = d.WarehouseId ?? x.WarehouseId, QtyIn = 0m, QtyOut = d.Quantity, d.UnitPrice, d.Amount }))
            .ToListAsync(cancellationToken);

        var rows = receiptRows.Select(x => MovementRow(x.Date, x.VoucherNo, x.Type, x.ItemId, x.WarehouseId, x.QtyIn, x.QtyOut, x.UnitCost, x.TotalCost, items, warehouses))
            .Concat(issueRows.Select(x =>
            {
                var detailId = PostingSourceIds.FromInt32(x.Id);
                var hasCost = issueCosts.TryGetValue(detailId, out var cost);
                return MovementRow(x.Date, x.VoucherNo, x.Type, x.ItemId, x.WarehouseId, x.QtyIn, x.QtyOut, hasCost ? cost!.UnitCost : x.UnitPrice, hasCost ? cost!.TotalCost : x.Amount, items, warehouses);
            }))
            .OrderBy(x => x.Values["Date"])
            .ThenBy(x => x.Values["VoucherNo"])
            .ToList();

        return Result("INVENTORY_MOVEMENT", "Nhập xuất tồn", InventoryMovementColumns(), rows,
            Summary(("QtyIn", rows.Sum(x => GetDecimal(x, "QtyIn"))), ("QtyOut", rows.Sum(x => GetDecimal(x, "QtyOut"))), ("TotalCost", rows.Sum(x => GetDecimal(x, "TotalCost")))));
    }

    private async Task AuditAsync(string entityName, string entityId, string action, object? oldValues, object? newValues, string? userName, CancellationToken cancellationToken)
    {
        if (_auditService is not null)
        {
            await _auditService.LogAsync(entityName, entityId, action, oldValues, newValues, userName, correlationId: _auditCorrelationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
        }
    }

    private async Task<ReportResultDto> BuildStockCardAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var movement = await BuildInventoryMovementAsync(request, cancellationToken);
        var itemKeyword = GetString(request, "itemCode") ?? GetString(request, "keyword");
        var warehouseKeyword = GetString(request, "warehouse");
        var query = movement.Rows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(itemKeyword))
        {
            query = query.Where(x => Convert.ToString(x.Values["Item"], CultureInfo.InvariantCulture)?.Contains(itemKeyword, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrWhiteSpace(warehouseKeyword) && warehouseKeyword != "Tất cả kho")
        {
            query = query.Where(x => Convert.ToString(x.Values["Warehouse"], CultureInfo.InvariantCulture)?.Contains(warehouseKeyword, StringComparison.OrdinalIgnoreCase) == true);
        }

        var rows = new List<ReportRowDto>();
        foreach (var group in query.GroupBy(x => new { Item = Convert.ToString(x.Values["Item"], CultureInfo.InvariantCulture), Warehouse = Convert.ToString(x.Values["Warehouse"], CultureInfo.InvariantCulture) }))
        {
            decimal balance = 0;
            foreach (var row in group.OrderBy(x => x.Values["Date"]).ThenBy(x => x.Values["VoucherNo"]))
            {
                balance += GetDecimal(row, "QtyIn") - GetDecimal(row, "QtyOut");
                rows.Add(Row(
                    ("Date", row.Values["Date"]),
                    ("VoucherNo", row.Values["VoucherNo"]),
                    ("Type", row.Values["Type"]),
                    ("Item", row.Values["Item"]),
                    ("Warehouse", row.Values["Warehouse"]),
                    ("QtyIn", GetDecimal(row, "QtyIn")),
                    ("QtyOut", GetDecimal(row, "QtyOut")),
                    ("BalanceQty", balance),
                    ("UnitCost", GetDecimal(row, "UnitCost")),
                    ("TotalCost", GetDecimal(row, "TotalCost"))));
            }
        }

        return Result("STOCK_CARD", "Thẻ kho", StockCardColumns(), rows,
            Summary(("QtyIn", rows.Sum(x => GetDecimal(x, "QtyIn"))), ("QtyOut", rows.Sum(x => GetDecimal(x, "QtyOut"))), ("TotalCost", rows.Sum(x => GetDecimal(x, "TotalCost")))));
    }

    private async Task<ReportResultDto> BuildLowStockAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var threshold = GetDecimalParameter(request, "minQty", 10m);
        var rows = (await BuildStockLevelRowsAsync(cancellationToken))
            .Where(x => GetDecimal(x, "ClosingQty") >= 0 && GetDecimal(x, "ClosingQty") <= threshold)
            .ToList();

        return Result("LOW_STOCK", "Cảnh báo tồn thấp", StockLevelColumns(), rows,
            Summary(("ItemCount", rows.Count), ("ClosingCost", rows.Sum(x => GetDecimal(x, "ClosingCost")))));
    }

    private async Task<ReportResultDto> BuildNegativeStockAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var rows = (await BuildStockLevelRowsAsync(cancellationToken))
            .Where(x => GetDecimal(x, "ClosingQty") < 0)
            .ToList();

        return Result("NEGATIVE_STOCK", "Tồn âm", StockLevelColumns(), rows,
            Summary(("ItemCount", rows.Count), ("ClosingCost", rows.Sum(x => GetDecimal(x, "ClosingCost")))));
    }

    private async Task<IReadOnlyList<ReportRowDto>> BuildStockLevelRowsAsync(CancellationToken cancellationToken)
    {
        var balances = await _dbContext.StockBalances.AsNoTracking().ToListAsync(cancellationToken);
        var items = await _dbContext.Items.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var warehouses = await _dbContext.Warehouses.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);

        return balances
            .OrderBy(x => items.TryGetValue(x.ItemId, out var item) ? item.ItemCode : x.ItemId.ToString(CultureInfo.InvariantCulture))
            .ThenBy(x => warehouses.TryGetValue(x.WarehouseId, out var warehouse) ? warehouse.WarehouseCode : x.WarehouseId.ToString(CultureInfo.InvariantCulture))
            .Select(x =>
            {
                items.TryGetValue(x.ItemId, out var item);
                warehouses.TryGetValue(x.WarehouseId, out var warehouse);
                return Row(
                    ("ItemCode", item?.ItemCode ?? x.ItemId.ToString(CultureInfo.InvariantCulture)),
                    ("ItemName", item?.ItemName ?? string.Empty),
                    ("Warehouse", warehouse?.WarehouseName ?? x.WarehouseId.ToString(CultureInfo.InvariantCulture)),
                    ("ClosingQty", x.ClosingQty),
                    ("AverageCost", x.AvgPrice),
                    ("ClosingCost", x.ClosingAmount),
                    ("Status", x.ClosingQty < 0 ? "Âm kho" : x.ClosingQty <= 10 ? "Tồn thấp" : "Bình thường"));
            })
            .ToList();
    }

    private async Task<ReportResultDto> BuildCostAnalysisAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var items = await _dbContext.Items.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var warehouses = await _dbContext.Warehouses.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var reversedReceiptSources = (await _dbContext.StockReceipts
                .AsNoTracking()
                .Where(x => x.PostingStatus == "Reversed")
                .Select(x => x.Id)
                .ToListAsync(cancellationToken))
            .Select(PostingSourceIds.FromInt32)
            .ToHashSet();
        var reversedIssueSources = (await _dbContext.StockIssues
                .AsNoTracking()
                .Where(x => x.PostingStatus == "Reversed")
                .Select(x => x.Id)
                .ToListAsync(cancellationToken))
            .Select(PostingSourceIds.FromInt32)
            .ToHashSet();

        var issueCosts = await _dbContext.InventoryCostTransactions
            .AsNoTracking()
            .Where(x => !x.SourceId.HasValue || !reversedIssueSources.Contains(x.SourceId.Value))
            .GroupBy(x => new { x.ItemId, x.WarehouseId })
            .Select(x => new
            {
                x.Key.ItemId,
                x.Key.WarehouseId,
                IssueCost = x.Sum(y => y.TotalCost),
                TransactionCount = x.Count()
            })
            .ToListAsync(cancellationToken);

        var issueCostMap = issueCosts.ToDictionary(x => (x.ItemId, x.WarehouseId));
        var rows = await _dbContext.InventoryCostLayers
            .AsNoTracking()
            .Where(x => x.Status != "Cancelled" && (!x.SourceId.HasValue || !reversedReceiptSources.Contains(x.SourceId.Value)))
            .GroupBy(x => new { x.ItemId, x.WarehouseId })
            .Select(x => new
            {
                x.Key.ItemId,
                x.Key.WarehouseId,
                RemainingQty = x.Sum(y => y.RemainingQuantity),
                RemainingCost = x.Sum(y => y.RemainingCost)
            })
            .ToListAsync(cancellationToken);

        var resultRows = rows
            .OrderBy(x => items.TryGetValue(x.ItemId, out var item) ? item.ItemCode : x.ItemId.ToString(CultureInfo.InvariantCulture))
            .Select(x =>
            {
                items.TryGetValue(x.ItemId, out var item);
                warehouses.TryGetValue(x.WarehouseId, out var warehouse);
                issueCostMap.TryGetValue((x.ItemId, x.WarehouseId), out var issue);
                return Row(
                    ("ItemCode", item?.ItemCode ?? x.ItemId.ToString(CultureInfo.InvariantCulture)),
                    ("ItemName", item?.ItemName ?? string.Empty),
                    ("Warehouse", warehouse?.WarehouseName ?? x.WarehouseId.ToString(CultureInfo.InvariantCulture)),
                    ("RemainingQty", x.RemainingQty),
                    ("MovingAverageCost", x.RemainingQty == 0 ? 0 : x.RemainingCost / x.RemainingQty),
                    ("RemainingCost", x.RemainingCost),
                    ("IssueCost", issue?.IssueCost ?? 0m),
                    ("TransactionCount", issue?.TransactionCount ?? 0));
            })
            .ToList();

        return Result("COST_ANALYSIS", "Phân tích giá vốn", CostAnalysisColumns(), resultRows,
            Summary(("RemainingCost", resultRows.Sum(x => GetDecimal(x, "RemainingCost"))), ("IssueCost", resultRows.Sum(x => GetDecimal(x, "IssueCost")))));
    }

    private async Task<ReportResultDto> BuildPayableAgingAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var asOfDate = GetDate(request, "asOfDate", DateTime.Today.Date);
        var suppliers = await _dbContext.Suppliers.AsNoTracking().ToDictionaryAsync(x => PostingSourceIds.FromInt32(x.Id), cancellationToken);
        var transactions = await _dbContext.PayableTransactions.AsNoTracking()
            .Where(x => x.Status != "Cancelled" && x.BalanceAmount != 0)
            .ToListAsync(cancellationToken);

        return BuildAgingResult("AP_AGING", "Tuổi nợ phải trả", "SupplierCode", "SupplierName", transactions.Select(x =>
        {
            suppliers.TryGetValue(x.SupplierId, out var supplier);
            return new AgingSource(supplier?.SupplierCode ?? x.SupplierId.ToString(), supplier?.SupplierName ?? string.Empty, x.DueDate ?? x.DocumentDate, x.BalanceAmount);
        }), asOfDate);
    }

    private async Task<ReportResultDto> BuildReceivableAgingAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var asOfDate = GetDate(request, "asOfDate", DateTime.Today.Date);
        var customers = await _dbContext.Customers.AsNoTracking().ToDictionaryAsync(x => PostingSourceIds.FromInt32(x.Id), cancellationToken);
        var transactions = await _dbContext.ReceivableTransactions.AsNoTracking()
            .Where(x => x.Status != "Cancelled" && x.BalanceAmount != 0)
            .ToListAsync(cancellationToken);

        return BuildAgingResult("AR_AGING", "Tuổi nợ phải thu", "CustomerCode", "CustomerName", transactions.Select(x =>
        {
            customers.TryGetValue(x.CustomerId, out var customer);
            return new AgingSource(customer?.CustomerCode ?? x.CustomerId.ToString(), customer?.CustomerName ?? string.Empty, x.DueDate ?? x.DocumentDate, x.BalanceAmount);
        }), asOfDate);
    }

    private async Task<ReportResultDto> BuildVatReportAsync(ReportRequestDto request, string direction, string reportCode, string reportName, CancellationToken cancellationToken)
    {
        var period = GetString(request, "period") ?? GetString(request, "declarationPeriod");
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);

        var query = _dbContext.VatTransactions
            .AsNoTracking()
            .Include(x => x.TaxCode)
            .Where(x => x.Direction == direction && x.Status != "Cancelled");

        if (!string.IsNullOrWhiteSpace(period))
        {
            query = query.Where(x => x.DeclarationPeriod == NormalizeReportPeriod(period));
        }
        else
        {
            query = query.Where(x => (x.InvoiceDate ?? x.CreatedAt) >= fromDate && (x.InvoiceDate ?? x.CreatedAt) <= toDate);
        }

        var rows = (await query.OrderBy(x => x.InvoiceDate ?? x.CreatedAt).ThenBy(x => x.InvoiceNo).ToListAsync(cancellationToken))
            .Select(x => Row(
                ("DeclarationPeriod", x.DeclarationPeriod),
                ("InvoiceDate", x.InvoiceDate),
                ("InvoiceNo", x.InvoiceNo ?? string.Empty),
                ("PartnerType", x.PartnerType ?? string.Empty),
                ("TaxCode", x.TaxCode?.Code ?? string.Empty),
                ("TaxRate", x.TaxRate),
                ("TaxBaseAmount", x.TaxBaseAmount),
                ("TaxAmount", x.TaxAmount),
                ("Status", x.Status)))
            .ToList();

        return Result(reportCode, reportName, VatColumns(), rows,
            Summary(("TaxBaseAmount", rows.Sum(x => GetDecimal(x, "TaxBaseAmount"))), ("TaxAmount", rows.Sum(x => GetDecimal(x, "TaxAmount")))));
    }

    private async Task<ReportResultDto> BuildBalanceSheetAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var period = NormalizeReportPeriod(GetString(request, "period") ?? DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture));
        var toDate = DateTime.ParseExact($"{period}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture).AddMonths(1).AddDays(-1).Date.AddDays(1).AddTicks(-1);
        var accounts = await _dbContext.LedgerAccounts.AsNoTracking().ToDictionaryAsync(x => x.AccountCode, cancellationToken);
        var balances = await LoadBalancesUntilAsync(toDate, cancellationToken);

        var rows = balances.Select(x =>
            {
                accounts.TryGetValue(x.AccountCode, out var account);
                var category = ResolveStatementCategory(x.AccountCode, account?.AccountCategory);
                var amount = category is "Liability" or "Equity" ? x.Credit - x.Debit : x.Debit - x.Credit;
                return Row(
                    ("Section", category),
                    ("AccountCode", x.AccountCode),
                    ("AccountName", account?.AccountName ?? x.AccountName),
                    ("Amount", amount));
            })
            .Where(x => GetDecimal(x, "Amount") != 0)
            .OrderBy(x => x.Values["Section"])
            .ThenBy(x => x.Values["AccountCode"])
            .ToList();

        var totalAssets = rows.Where(x => (string)x.Values["Section"]! == "Asset").Sum(x => GetDecimal(x, "Amount"));
        var totalLiabilities = rows.Where(x => (string)x.Values["Section"]! == "Liability").Sum(x => GetDecimal(x, "Amount"));
        var totalEquity = rows.Where(x => (string)x.Values["Section"]! == "Equity").Sum(x => GetDecimal(x, "Amount"));
        return Result("BALANCE_SHEET", "Bảng cân đối kế toán", FinancialStatementColumns(), rows,
            Summary(("TotalAssets", totalAssets), ("TotalLiabilitiesAndEquity", totalLiabilities + totalEquity), ("Difference", totalAssets - totalLiabilities - totalEquity)));
    }

    private async Task<ReportResultDto> BuildIncomeStatementAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var period = NormalizeReportPeriod(GetString(request, "period") ?? DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture));
        var fromDate = DateTime.ParseExact($"{period}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toDate = fromDate.AddMonths(1).AddDays(-1).Date.AddDays(1).AddTicks(-1);
        var accounts = await _dbContext.LedgerAccounts.AsNoTracking().ToDictionaryAsync(x => x.AccountCode, cancellationToken);
        var lines = await LoadLinesBetweenAsync(fromDate, toDate, cancellationToken);

        decimal revenue = 0m, cogs = 0m, expenses = 0m, otherIncome = 0m, otherExpense = 0m;
        foreach (var line in lines)
        {
            accounts.TryGetValue(line.AccountCode, out var account);
            var category = ResolveStatementCategory(line.AccountCode, account?.AccountCategory);
            if (category == "Revenue" || line.AccountCode.StartsWith("5", StringComparison.Ordinal) || line.AccountCode.StartsWith("7", StringComparison.Ordinal))
            {
                var amount = line.Credit - line.Debit;
                if (line.AccountCode.StartsWith("7", StringComparison.Ordinal)) otherIncome += amount;
                else revenue += amount;
            }
            else if (category == "Expense" || line.AccountCode.StartsWith("6", StringComparison.Ordinal) || line.AccountCode.StartsWith("8", StringComparison.Ordinal))
            {
                var amount = line.Debit - line.Credit;
                if (line.AccountCode.StartsWith("632", StringComparison.Ordinal)) cogs += amount;
                else if (line.AccountCode.StartsWith("8", StringComparison.Ordinal)) otherExpense += amount;
                else expenses += amount;
            }
        }

        var grossProfit = revenue - cogs;
        var profitBeforeTax = grossProfit - expenses + otherIncome - otherExpense;
        var rows = new List<ReportRowDto>
        {
            Row(("Section", "IncomeStatement"), ("AccountCode", "REVENUE"), ("AccountName", "Doanh thu"), ("Amount", revenue)),
            Row(("Section", "IncomeStatement"), ("AccountCode", "COGS"), ("AccountName", "Giá vốn hàng bán"), ("Amount", cogs)),
            Row(("Section", "IncomeStatement"), ("AccountCode", "GROSS_PROFIT"), ("AccountName", "Lợi nhuận gộp"), ("Amount", grossProfit)),
            Row(("Section", "IncomeStatement"), ("AccountCode", "OPERATING_EXPENSES"), ("AccountName", "Chi phí hoạt động"), ("Amount", expenses)),
            Row(("Section", "IncomeStatement"), ("AccountCode", "OTHER_INCOME"), ("AccountName", "Thu nhập khác"), ("Amount", otherIncome)),
            Row(("Section", "IncomeStatement"), ("AccountCode", "OTHER_EXPENSE"), ("AccountName", "Chi phí khác"), ("Amount", otherExpense)),
            Row(("Section", "IncomeStatement"), ("AccountCode", "PROFIT_BEFORE_TAX"), ("AccountName", "Lợi nhuận trước thuế"), ("Amount", profitBeforeTax))
        };

        return Result("INCOME_STATEMENT", "Báo cáo kết quả kinh doanh", FinancialStatementColumns(), rows,
            Summary(("ProfitBeforeTax", profitBeforeTax)));
    }

    private async Task<ReportResultDto> BuildCashFlowAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var period = NormalizeReportPeriod(GetString(request, "period") ?? DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture));
        var fromDate = DateTime.ParseExact($"{period}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toDate = fromDate.AddMonths(1).AddDays(-1).Date.AddDays(1).AddTicks(-1);
        var cashAccounts = await _dbContext.AccountRoleMappings
            .AsNoTracking()
            .Where(x => x.IsActive && (x.RoleCode == "Cash" || x.RoleCode == "Bank"))
            .Select(x => x.AccountCode)
            .Distinct()
            .ToListAsync(cancellationToken);
        var lines = await LoadLinesBetweenAsync(fromDate, toDate, cancellationToken);
        var operating = lines.Where(x => cashAccounts.Contains(x.AccountCode)).Sum(x => x.Debit - x.Credit);
        var rows = new List<ReportRowDto>
        {
            Row(("Section", "CashFlow"), ("AccountCode", "OPERATING"), ("AccountName", "Lưu chuyển tiền từ hoạt động kinh doanh"), ("Amount", operating)),
            Row(("Section", "CashFlow"), ("AccountCode", "INVESTING"), ("AccountName", "Lưu chuyển tiền từ hoạt động đầu tư"), ("Amount", 0m)),
            Row(("Section", "CashFlow"), ("AccountCode", "FINANCING"), ("AccountName", "Lưu chuyển tiền từ hoạt động tài chính"), ("Amount", 0m))
        };

        return Result("CASH_FLOW", "Báo cáo lưu chuyển tiền tệ", FinancialStatementColumns(), rows,
            Summary(("NetCashFlow", operating), ("Warning", "Cash Flow mới là skeleton.")));
    }

    private async Task<ReportResultDto> BuildCashBookAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddMonths(-1).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date);
        var funds = await _dbContext.CashFunds.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var rows = (await _dbContext.CashTransactions.AsNoTracking()
                .Where(x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate)
                .OrderBy(x => x.TransactionDate).ThenBy(x => x.TransactionNo)
                .ToListAsync(cancellationToken))
            .Select(x =>
            {
                funds.TryGetValue(x.CashFundId, out var fund);
                return Row(("Date", x.TransactionDate), ("Account", fund?.FundCode ?? string.Empty), ("TransactionNo", x.TransactionNo),
                    ("ReferenceNo", x.SourceType ?? string.Empty), ("Description", x.Description ?? string.Empty), ("Debit", x.DebitAmount),
                    ("Credit", x.CreditAmount), ("Balance", x.BalanceAfter), ("Status", x.Status), ("JournalStatus", x.JournalHeaderId.HasValue ? x.Status : "Unposted"));
            })
            .ToList();

        return Result("CASH_BOOK", "Sổ quỹ tiền mặt", CashBankBookColumns(), rows,
            Summary(("Debit", rows.Sum(x => GetDecimal(x, "Debit"))), ("Credit", rows.Sum(x => GetDecimal(x, "Credit")))));
    }

    private async Task<ReportResultDto> BuildBankBookAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddMonths(-1).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date);
        var accounts = await _dbContext.BankAccounts.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var rows = (await _dbContext.BankTransactions.AsNoTracking()
                .Where(x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate)
                .OrderBy(x => x.TransactionDate).ThenBy(x => x.TransactionNo)
                .ToListAsync(cancellationToken))
            .Select(x =>
            {
                accounts.TryGetValue(x.BankAccountId, out var account);
                return Row(("Date", x.TransactionDate), ("Account", account?.AccountNumber ?? string.Empty), ("TransactionNo", x.TransactionNo),
                    ("ReferenceNo", x.ReferenceNo ?? string.Empty), ("Description", x.Description ?? string.Empty), ("Debit", x.DebitAmount),
                    ("Credit", x.CreditAmount), ("Balance", x.BalanceAfter), ("Status", x.Status), ("JournalStatus", x.JournalHeaderId.HasValue ? x.Status : "Unposted"));
            })
            .ToList();

        return Result("BANK_BOOK", "Sổ tiền gửi ngân hàng", CashBankBookColumns(), rows,
            Summary(("Debit", rows.Sum(x => GetDecimal(x, "Debit"))), ("Credit", rows.Sum(x => GetDecimal(x, "Credit")))));
    }

    private async Task<ReportResultDto> BuildBankReconciliationAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddMonths(-1).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date);
        var accounts = await _dbContext.BankAccounts.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var rows = (await _dbContext.BankReconciliations.AsNoTracking()
                .Where(x => x.ReconciliationDate >= fromDate && x.ReconciliationDate <= toDate)
                .OrderByDescending(x => x.ReconciliationDate)
                .ToListAsync(cancellationToken))
            .Select(x =>
            {
                accounts.TryGetValue(x.BankAccountId, out var account);
                return Row(("Date", x.ReconciliationDate), ("Account", account?.AccountNumber ?? string.Empty), ("ReconciliationNo", x.ReconciliationNo),
                    ("BookBalance", x.BookBalance), ("StatementBalance", x.StatementBalance), ("Difference", x.Difference),
                    ("MatchedCount", x.Status == "Confirmed" ? 1 : 0), ("UnmatchedCount", x.Difference == 0 ? 0 : 1), ("Status", x.Status));
            })
            .ToList();

        return Result("BANK_RECONCILIATION", "Đối chiếu ngân hàng", BankReconciliationColumns(), rows,
            Summary(("Difference", rows.Sum(x => GetDecimal(x, "Difference")))));
    }

    private async Task<ReportResultDto> BuildDailyCashflowAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddDays(-7).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date);
        var cash = await _dbContext.CashTransactions.AsNoTracking()
            .Where(x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate && x.Status != "Reversed")
            .Select(x => new { x.TransactionDate, x.DebitAmount, x.CreditAmount })
            .ToListAsync(cancellationToken);
        var bank = await _dbContext.BankTransactions.AsNoTracking()
            .Where(x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate && x.Status != "Reversed")
            .Select(x => new { x.TransactionDate, x.DebitAmount, x.CreditAmount })
            .ToListAsync(cancellationToken);
        var rows = cash.Concat(bank).GroupBy(x => x.TransactionDate.Date).OrderBy(x => x.Key)
            .Select(x => Row(("Date", x.Key), ("Inflow", x.Sum(y => y.DebitAmount)), ("Outflow", x.Sum(y => y.CreditAmount)), ("NetFlow", x.Sum(y => y.DebitAmount - y.CreditAmount))))
            .ToList();

        return Result("DAILY_CASHFLOW", "Dòng tiền ngày", DailyCashflowColumns(), rows,
            Summary(("Inflow", rows.Sum(x => GetDecimal(x, "Inflow"))), ("Outflow", rows.Sum(x => GetDecimal(x, "Outflow"))), ("NetFlow", rows.Sum(x => GetDecimal(x, "NetFlow")))));
    }

    private async Task<ReportResultDto> BuildCashBalanceAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var cashSources = await _dbContext.CashFunds.AsNoTracking()
            .Select(x => new { x.FundCode, x.FundName, x.CurrencyCode, x.CurrentBalance, x.IsActive })
            .ToListAsync(cancellationToken);
        var bankSources = await _dbContext.BankAccounts.AsNoTracking()
            .Select(x => new { x.AccountNumber, x.BankName, x.CurrencyCode, x.CurrentBalance, x.IsActive })
            .ToListAsync(cancellationToken);
        var cashRows = cashSources
            .Select(x => Row(("Type", "Cash"), ("Code", x.FundCode), ("Name", x.FundName), ("Currency", x.CurrencyCode), ("CurrentBalance", x.CurrentBalance), ("AvailableBalance", x.CurrentBalance), ("Status", x.IsActive ? "Active" : "Inactive")));
        var bankRows = bankSources
            .Select(x => Row(("Type", "Bank"), ("Code", x.AccountNumber), ("Name", x.BankName), ("Currency", x.CurrencyCode), ("CurrentBalance", x.CurrentBalance), ("AvailableBalance", x.CurrentBalance), ("Status", x.IsActive ? "Active" : "Inactive")));
        var rows = cashRows.Concat(bankRows).ToList();

        return Result("CASH_BALANCE", "Số dư tiền", CashBalanceColumns(), rows,
            Summary(("CurrentBalance", rows.Sum(x => GetDecimal(x, "CurrentBalance")))));
    }

    private async Task<ReportResultDto> BuildFixedAssetRegisterAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var rows = (await _dbContext.FixedAssets.AsNoTracking()
                .Include(x => x.Category)
                .OrderBy(x => x.AssetCode)
                .ToListAsync(cancellationToken))
            .Select(x => Row(
                ("AssetCode", x.AssetCode),
                ("AssetName", x.AssetName),
                ("Category", x.Category?.CategoryName ?? string.Empty),
                ("PurchaseDate", x.PurchaseDate),
                ("OriginalCost", x.OriginalCost),
                ("AccumulatedDepreciation", x.AccumulatedDepreciation),
                ("RemainingValue", x.RemainingValue),
                ("Method", x.DepreciationMethod),
                ("UsefulLifeMonths", x.UsefulLifeMonths),
                ("Status", x.Status),
                ("JournalStatus", x.JournalHeaderId.HasValue ? "Posted" : "Unposted"),
                ("JournalId", x.JournalHeaderId?.ToString() ?? string.Empty)))
            .ToList();

        return Result("FIXED_ASSET_REGISTER", "Sổ tài sản cố định", FixedAssetRegisterColumns(), rows,
            Summary(("OriginalCost", rows.Sum(x => GetDecimal(x, "OriginalCost"))),
                ("AccumulatedDepreciation", rows.Sum(x => GetDecimal(x, "AccumulatedDepreciation"))),
                ("RemainingValue", rows.Sum(x => GetDecimal(x, "RemainingValue")))));
    }

    private async Task<ReportResultDto> BuildDepreciationScheduleReportAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddYears(-1).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date);
        var rows = (await _dbContext.FixedAssetDepreciations.AsNoTracking()
                .Include(x => x.FixedAsset)
                .Where(x => x.DepreciationDate >= fromDate && x.DepreciationDate <= toDate)
                .OrderBy(x => x.DepreciationDate)
                .ThenBy(x => x.FixedAsset!.AssetCode)
                .ToListAsync(cancellationToken))
            .Select(x => Row(
                ("Period", x.DepreciationPeriod),
                ("Date", x.DepreciationDate),
                ("AssetCode", x.FixedAsset?.AssetCode ?? string.Empty),
                ("AssetName", x.FixedAsset?.AssetName ?? string.Empty),
                ("Amount", x.DepreciationAmount),
                ("AccumulatedAfter", x.AccumulatedAfter),
                ("RemainingAfter", x.RemainingValueAfter),
                ("Status", x.Status),
                ("JournalStatus", x.JournalHeaderId.HasValue ? "Posted" : "Unposted"),
                ("JournalId", x.JournalHeaderId?.ToString() ?? string.Empty)))
            .ToList();

        return Result("DEPRECIATION_SCHEDULE", "Lịch khấu hao TSCĐ", DepreciationScheduleColumns(), rows,
            Summary(("Amount", rows.Sum(x => GetDecimal(x, "Amount")))));
    }

    private async Task<ReportResultDto> BuildAssetIncreaseDecreaseAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddYears(-1).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date);
        var increases = (await _dbContext.FixedAssets.AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.PurchaseDate >= fromDate && x.PurchaseDate <= toDate)
                .ToListAsync(cancellationToken))
            .Select(x => Row(
                ("Date", x.PurchaseDate),
                ("AssetCode", x.AssetCode),
                ("AssetName", x.AssetName),
                ("Type", "Increase"),
                ("Amount", x.OriginalCost),
                ("Description", x.Category?.CategoryName ?? string.Empty),
                ("Status", x.Status),
                ("JournalStatus", x.JournalHeaderId.HasValue ? "Posted" : "Unposted")));
        var adjustments = (await _dbContext.FixedAssetAdjustments.AsNoTracking()
                .Include(x => x.FixedAsset)
                .Where(x => x.AdjustmentDate >= fromDate && x.AdjustmentDate <= toDate)
                .ToListAsync(cancellationToken))
            .Select(x => Row(
                ("Date", x.AdjustmentDate),
                ("AssetCode", x.FixedAsset?.AssetCode ?? string.Empty),
                ("AssetName", x.FixedAsset?.AssetName ?? string.Empty),
                ("Type", x.AdjustmentType),
                ("Amount", x.Amount),
                ("Description", x.Description ?? string.Empty),
                ("Status", "Posted"),
                ("JournalStatus", x.JournalHeaderId.HasValue ? "Posted" : "Unposted")));
        var disposals = (await _dbContext.FixedAssetDisposals.AsNoTracking()
                .Include(x => x.FixedAsset)
                .Where(x => x.DisposalDate >= fromDate && x.DisposalDate <= toDate)
                .ToListAsync(cancellationToken))
            .Select(x => Row(
                ("Date", x.DisposalDate),
                ("AssetCode", x.FixedAsset?.AssetCode ?? string.Empty),
                ("AssetName", x.FixedAsset?.AssetName ?? string.Empty),
                ("Type", x.DisposalType),
                ("Amount", x.DisposalAmount),
                ("Description", x.Notes ?? string.Empty),
                ("Status", "Disposed"),
                ("JournalStatus", x.JournalHeaderId.HasValue ? "Posted" : "Unposted")));

        var rows = increases.Concat(adjustments).Concat(disposals)
            .OrderBy(x => x.Values.TryGetValue("Date", out var date) ? date : null)
            .ToList();

        return Result("ASSET_INCREASE_DECREASE", "Tăng giảm TSCĐ", AssetIncreaseDecreaseColumns(), rows,
            Summary(("Amount", rows.Sum(x => GetDecimal(x, "Amount")))));
    }

    private async Task<ReportResultDto> BuildToolAssetRegisterAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var rows = (await _dbContext.ToolAssets.AsNoTracking()
                .Include(x => x.Category)
                .OrderBy(x => x.ToolCode)
                .ToListAsync(cancellationToken))
            .Select(x => Row(
                ("ToolCode", x.ToolCode),
                ("ToolName", x.ToolName),
                ("Category", x.Category?.CategoryName ?? string.Empty),
                ("PurchaseDate", x.PurchaseDate),
                ("OriginalCost", x.OriginalCost),
                ("AllocatedAmount", x.AllocatedAmount),
                ("RemainingAmount", x.RemainingAmount),
                ("AllocationMonths", x.AllocationMonths),
                ("MonthlyAllocation", x.AllocationMonths <= 0 ? 0m : Math.Round(x.OriginalCost / x.AllocationMonths, 2, MidpointRounding.AwayFromZero)),
                ("Status", x.Status),
                ("JournalStatus", x.JournalHeaderId.HasValue ? "Posted" : "Unposted"),
                ("JournalId", x.JournalHeaderId?.ToString() ?? string.Empty)))
            .ToList();

        return Result("TOOL_ASSET_REGISTER", "Sổ công cụ dụng cụ", ToolAssetRegisterColumns(), rows,
            Summary(("OriginalCost", rows.Sum(x => GetDecimal(x, "OriginalCost"))),
                ("AllocatedAmount", rows.Sum(x => GetDecimal(x, "AllocatedAmount"))),
                ("RemainingAmount", rows.Sum(x => GetDecimal(x, "RemainingAmount")))));
    }

    private async Task<ReportResultDto> BuildToolAssetAllocationAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddYears(-1).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date);
        var rows = (await _dbContext.ToolAllocationSchedules.AsNoTracking()
                .Include(x => x.ToolAsset)
                .Where(x => x.AllocationDate >= fromDate && x.AllocationDate <= toDate)
                .OrderBy(x => x.AllocationDate)
                .ThenBy(x => x.ToolAsset!.ToolCode)
                .ToListAsync(cancellationToken))
            .Select(x => Row(
                ("Period", x.AllocationPeriod),
                ("Date", x.AllocationDate),
                ("ToolCode", x.ToolAsset?.ToolCode ?? string.Empty),
                ("ToolName", x.ToolAsset?.ToolName ?? string.Empty),
                ("Amount", x.AllocationAmount),
                ("AccumulatedAfter", x.AccumulatedAfter),
                ("RemainingAfter", x.RemainingAfter),
                ("Status", x.Status),
                ("JournalStatus", x.JournalHeaderId.HasValue ? "Posted" : "Unposted"),
                ("JournalId", x.JournalHeaderId?.ToString() ?? string.Empty)))
            .ToList();

        return Result("TOOL_ASSET_ALLOCATION", "Phân bổ CCDC", ToolAllocationColumns(), rows,
            Summary(("Amount", rows.Sum(x => GetDecimal(x, "Amount")))));
    }

    private async Task<ReportResultDto> BuildPrepaidExpenseScheduleAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        var fromDate = GetDate(request, "fromDate", DateTime.Today.AddYears(-1).Date);
        var toDate = GetDate(request, "toDate", DateTime.Today.Date);
        var rows = (await _dbContext.ToolAllocationSchedules.AsNoTracking()
                .Include(x => x.ToolAsset)
                .ThenInclude(x => x!.Category)
                .Where(x => x.AllocationDate >= fromDate && x.AllocationDate <= toDate && x.ToolAsset != null && x.ToolAsset.AllocationMonths > 1)
                .OrderBy(x => x.AllocationDate)
                .ThenBy(x => x.ToolAsset!.ToolCode)
                .ToListAsync(cancellationToken))
            .Select(x => Row(
                ("Period", x.AllocationPeriod),
                ("Date", x.AllocationDate),
                ("SourceNo", x.ToolAsset?.ToolCode ?? string.Empty),
                ("SourceName", x.ToolAsset?.ToolName ?? string.Empty),
                ("PrepaidAccount", x.ToolAsset?.Category?.PrepaidExpenseAccountCode ?? string.Empty),
                ("ExpenseAccount", x.ToolAsset?.Category?.ExpenseAccountCode ?? string.Empty),
                ("Amount", x.AllocationAmount),
                ("RemainingAfter", x.RemainingAfter),
                ("Status", x.Status),
                ("JournalStatus", x.JournalHeaderId.HasValue ? "Posted" : "Unposted")))
            .ToList();

        return Result("PREPAID_EXPENSE_SCHEDULE", "Lịch chi phí trả trước", PrepaidExpenseScheduleColumns(), rows,
            Summary(("Amount", rows.Sum(x => GetDecimal(x, "Amount"))), ("RemainingAfter", rows.Sum(x => GetDecimal(x, "RemainingAfter")))));
    }

    private async Task<ReportResultDto> BuildBreakfastCafeDailySalesAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "BREAKFAST_CAFE_DAILY_SALES";
        const string name = "Tổng hợp bán hàng ăn sáng và cà phê";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.BreakfastCafeOrders.AsNoTracking()
            .Where(x => x.OrderDate >= fromDate && x.OrderDate <= toDate)
            .OrderByDescending(x => x.OrderDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new
            {
                x.OrderNumber,
                x.OrderDate,
                x.AreaName,
                x.TableName,
                x.CustomerReference,
                x.OrderType,
                x.Status,
                x.PaymentStatus,
                x.ServiceStatus,
                x.TotalAmount,
                x.PaidAmount,
                x.RemainingAmount
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("OrderNumber", x.OrderNumber),
            ("OrderDate", x.OrderDate),
            ("AreaName", x.AreaName ?? string.Empty),
            ("TableName", x.TableName ?? string.Empty),
            ("CustomerName", x.CustomerReference ?? string.Empty),
            ("OrderType", x.OrderType),
            ("Status", x.Status),
            ("PaymentStatus", x.PaymentStatus),
            ("ServiceStatus", x.ServiceStatus),
            ("TotalAmount", x.TotalAmount),
            ("PaidAmount", x.PaidAmount),
            ("RemainingAmount", x.RemainingAmount)))
            .ToList();

        return Result(code, name, BreakfastCafeSalesColumns(), rows,
            Summary(
                ("TotalAmount", rows.Sum(x => GetDecimal(x, "TotalAmount"))),
                ("PaidAmount", rows.Sum(x => GetDecimal(x, "PaidAmount"))),
                ("RemainingAmount", rows.Sum(x => GetDecimal(x, "RemainingAmount")))));
    }

    private async Task<ReportResultDto> BuildBreakfastCafePaymentSummaryAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "BREAKFAST_CAFE_PAYMENT_SUMMARY";
        const string name = "Chi tiết thanh toán ăn sáng và cà phê";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.BreakfastCafePayments.AsNoTracking()
            .Where(x => x.PaymentDate >= fromDate && x.PaymentDate <= toDate)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new
            {
                x.PaymentDate,
                x.PaymentMethod,
                x.ReferenceNo,
                x.Amount,
                x.Status,
                x.CreatedBy,
                OrderNumber = x.Order != null ? x.Order.OrderNumber : null,
                TableName = x.Order != null ? x.Order.TableName : null,
                CustomerName = x.Order != null ? x.Order.CustomerReference : null
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("PaymentDate", x.PaymentDate),
            ("OrderNumber", x.OrderNumber ?? string.Empty),
            ("TableName", x.TableName ?? string.Empty),
            ("CustomerName", x.CustomerName ?? string.Empty),
            ("PaymentMethod", x.PaymentMethod),
            ("ReferenceNo", x.ReferenceNo ?? string.Empty),
            ("Amount", x.Amount),
            ("Status", x.Status),
            ("CreatedBy", x.CreatedBy)))
            .ToList();

        return Result(code, name, BreakfastCafePaymentColumns(), rows,
            Summary(("Amount", rows.Sum(x => GetDecimal(x, "Amount")))));
    }

    private async Task<ReportResultDto> BuildBreakfastCafeItemSalesAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "BREAKFAST_CAFE_ITEM_SALES";
        const string name = "Top món bán ăn sáng và cà phê";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.BreakfastCafeOrderLines.AsNoTracking()
            .Where(x => x.Order != null && x.Order.OrderDate >= fromDate && x.Order.OrderDate <= toDate)
            .GroupBy(x => new { x.MenuItemCode, x.MenuItemName, x.UnitName, x.VatRate })
            .Select(g => new
            {
                g.Key.MenuItemCode,
                g.Key.MenuItemName,
                g.Key.UnitName,
                g.Key.VatRate,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotalAmount),
                OrderCount = g.Select(x => x.BreakfastCafeOrderId).Distinct().Count()
            })
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.MenuItemName)
            .Take(take)
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("MenuItemCode", x.MenuItemCode),
            ("MenuItemName", x.MenuItemName),
            ("UnitName", x.UnitName),
            ("VatRate", x.VatRate),
            ("Quantity", x.Quantity),
            ("OrderCount", x.OrderCount),
            ("Revenue", x.Revenue)))
            .ToList();

        return Result(code, name, BreakfastCafeItemSalesColumns(), rows,
            Summary(
                ("Quantity", rows.Sum(x => GetDecimal(x, "Quantity"))),
                ("Revenue", rows.Sum(x => GetDecimal(x, "Revenue")))));
    }

    private async Task<ReportResultDto> BuildBreakfastCafeKitchenTicketAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "BREAKFAST_CAFE_KITCHEN_TICKET";
        const string name = "Phiếu bếp/bar ăn sáng và cà phê";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.BreakfastCafeKitchenTickets.AsNoTracking()
            .Where(x => x.SentAt >= fromDate && x.SentAt <= toDate)
            .OrderByDescending(x => x.SentAt)
            .Take(take)
            .Select(x => new
            {
                x.TicketNumber,
                x.Station,
                x.Status,
                x.SentAt,
                x.SentBy,
                x.Note,
                OrderNumber = x.Order != null ? x.Order.OrderNumber : null,
                TableName = x.Order != null ? x.Order.TableName : null
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("TicketNumber", x.TicketNumber),
            ("SentAt", x.SentAt),
            ("OrderNumber", x.OrderNumber ?? string.Empty),
            ("TableName", x.TableName ?? string.Empty),
            ("Station", x.Station),
            ("Status", x.Status),
            ("SentBy", x.SentBy ?? string.Empty),
            ("Note", x.Note ?? string.Empty)))
            .ToList();

        return Result(code, name, BreakfastCafeKitchenTicketColumns(), rows);
    }

    private async Task<ReportResultDto> BuildAlacarteOrderSummaryAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "ALACARTE_ORDER_SUMMARY";
        const string name = "Tổng hợp đơn À La Carte";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.AlacarteOrders.AsNoTracking()
            .Where(x => x.OrderDate >= fromDate && x.OrderDate <= toDate)
            .OrderByDescending(x => x.OrderDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new
            {
                x.OrderNo,
                x.OrderDate,
                RoomName = x.Room != null ? x.Room.Name : null,
                CustomerName = x.Customer != null
                    ? x.Customer.CustomerName
                    : x.Quote != null
                        ? x.Quote.CustomerName
                        : x.Reservation != null
                            ? x.Reservation.CustomerName
                            : null,
                GuestCount = x.ActualGuestCount,
                x.OrderType,
                x.Status,
                x.PaymentStatus,
                x.TotalAmount,
                x.PaidAmount,
                x.RemainingAmount
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("OrderNo", x.OrderNo),
            ("OrderDate", x.OrderDate),
            ("RoomName", x.RoomName ?? string.Empty),
            ("CustomerName", x.CustomerName ?? string.Empty),
            ("GuestCount", x.GuestCount),
            ("OrderType", x.OrderType),
            ("Status", x.Status),
            ("PaymentStatus", x.PaymentStatus),
            ("TotalAmount", x.TotalAmount),
            ("PaidAmount", x.PaidAmount),
            ("RemainingAmount", x.RemainingAmount)))
            .ToList();

        return Result(code, name, AlacarteOrderSummaryColumns(), rows,
            Summary(
                ("TotalAmount", rows.Sum(x => GetDecimal(x, "TotalAmount"))),
                ("PaidAmount", rows.Sum(x => GetDecimal(x, "PaidAmount"))),
                ("RemainingAmount", rows.Sum(x => GetDecimal(x, "RemainingAmount"))),
                ("GuestCount", rows.Sum(x => GetDecimal(x, "GuestCount")))));
    }

    private async Task<ReportResultDto> BuildAlacarteQuoteSummaryAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "ALACARTE_QUOTE_SUMMARY";
        const string name = "Tổng hợp báo giá À La Carte";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.RestaurantQuotes.AsNoTracking()
            .Where(x => x.EventDate >= fromDate && x.EventDate <= toDate)
            .OrderByDescending(x => x.EventDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new
            {
                x.QuoteNo,
                x.EventDate,
                x.StartTime,
                x.EndTime,
                x.CustomerName,
                x.Phone,
                x.RoomName,
                x.TableCount,
                x.GuestCount,
                x.TotalAmount,
                x.DepositAmount,
                x.Status,
                x.PreparedBy
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("QuoteNo", x.QuoteNo),
            ("EventDate", x.EventDate),
            ("TimeRange", $"{x.StartTime} - {x.EndTime}"),
            ("CustomerName", x.CustomerName),
            ("Phone", x.Phone),
            ("RoomName", x.RoomName),
            ("TableCount", x.TableCount),
            ("GuestCount", x.GuestCount),
            ("TotalAmount", x.TotalAmount),
            ("DepositAmount", x.DepositAmount),
            ("Status", x.Status),
            ("PreparedBy", x.PreparedBy)))
            .ToList();

        return Result(code, name, AlacarteQuoteSummaryColumns(), rows,
            Summary(
                ("TotalAmount", rows.Sum(x => GetDecimal(x, "TotalAmount"))),
                ("DepositAmount", rows.Sum(x => GetDecimal(x, "DepositAmount"))),
                ("GuestCount", rows.Sum(x => GetDecimal(x, "GuestCount")))));
    }

    private async Task<ReportResultDto> BuildAlacarteReservationScheduleAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "ALACARTE_RESERVATION_SCHEDULE";
        const string name = "Lịch đặt bàn À La Carte";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.TableReservations.AsNoTracking()
            .Where(x => x.ReservationDateTime >= fromDate && x.ReservationDateTime <= toDate)
            .OrderByDescending(x => x.ReservationDateTime)
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new
            {
                x.ReservationNo,
                x.ReservationDateTime,
                x.CustomerName,
                x.Phone,
                RoomName = x.Room != null ? x.Room.Name : null,
                TableName = x.Table != null ? x.Table.Name : null,
                x.GuestCount,
                x.Status,
                x.Note
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("ReservationNo", x.ReservationNo),
            ("ReservationDateTime", x.ReservationDateTime),
            ("CustomerName", x.CustomerName ?? string.Empty),
            ("Phone", x.Phone),
            ("RoomName", x.RoomName ?? string.Empty),
            ("TableName", x.TableName ?? string.Empty),
            ("GuestCount", x.GuestCount),
            ("Status", x.Status),
            ("Note", x.Note ?? string.Empty)))
            .ToList();

        return Result(code, name, AlacarteReservationColumns(), rows,
            Summary(("GuestCount", rows.Sum(x => GetDecimal(x, "GuestCount")))));
    }

    private async Task<ReportResultDto> BuildAlacarteDepositSummaryAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "ALACARTE_DEPOSIT_SUMMARY";
        const string name = "Tổng hợp đặt cọc À La Carte";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.RestaurantDeposits.AsNoTracking()
            .Where(x => x.Date >= fromDate && x.Date <= toDate)
            .OrderByDescending(x => x.Date)
            .Take(take)
            .Select(x => new
            {
                x.DepositNo,
                x.Date,
                x.PaymentMethod,
                x.Amount,
                x.Receiver,
                x.Status,
                OrderNo = x.Order != null ? x.Order.OrderNo : null,
                ContractNo = x.Contract != null ? x.Contract.ContractNo : null,
                ReservationNo = x.Reservation != null ? x.Reservation.ReservationNo : null
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("DepositNo", x.DepositNo),
            ("Date", x.Date),
            ("OrderNo", x.OrderNo ?? string.Empty),
            ("ContractNo", x.ContractNo ?? string.Empty),
            ("ReservationNo", x.ReservationNo ?? string.Empty),
            ("PaymentMethod", x.PaymentMethod),
            ("Amount", x.Amount),
            ("Receiver", x.Receiver ?? string.Empty),
            ("Status", x.Status)))
            .ToList();

        return Result(code, name, AlacarteDepositColumns(), rows,
            Summary(("Amount", rows.Sum(x => GetDecimal(x, "Amount")))));
    }

    private async Task<ReportResultDto> BuildBanquetEventSummaryAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "BANQUET_EVENT_SUMMARY";
        const string name = "Tổng hợp sự kiện tiệc";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.BanquetEvents.AsNoTracking()
            .Where(x => x.EventDate >= fromDate && x.EventDate <= toDate)
            .OrderByDescending(x => x.EventDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new
            {
                x.EventNo,
                x.EventDate,
                x.StartTime,
                x.EndTime,
                x.EventType,
                x.CustomerName,
                RoomName = x.Room != null ? x.Room.Name : null,
                x.GuestCount,
                x.TableCount,
                x.Status,
                ContractTotal = x.Contracts.Sum(c => c.TotalAmount),
                DepositTotal = x.Deposits.Sum(d => d.Amount)
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("EventNo", x.EventNo),
            ("EventDate", x.EventDate),
            ("TimeRange", $"{x.StartTime:hh\\:mm} - {x.EndTime:hh\\:mm}"),
            ("EventType", x.EventType),
            ("CustomerName", x.CustomerName ?? string.Empty),
            ("RoomName", x.RoomName ?? string.Empty),
            ("GuestCount", x.GuestCount),
            ("TableCount", x.TableCount),
            ("Status", x.Status),
            ("ContractTotal", x.ContractTotal),
            ("DepositTotal", x.DepositTotal)))
            .ToList();

        return Result(code, name, BanquetEventSummaryColumns(), rows,
            Summary(
                ("ContractTotal", rows.Sum(x => GetDecimal(x, "ContractTotal"))),
                ("DepositTotal", rows.Sum(x => GetDecimal(x, "DepositTotal"))),
                ("GuestCount", rows.Sum(x => GetDecimal(x, "GuestCount")))));
    }

    private async Task<ReportResultDto> BuildBanquetContractSummaryAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "BANQUET_CONTRACT_SUMMARY";
        const string name = "Tổng hợp hợp đồng tiệc";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.BanquetContracts.AsNoTracking()
            .Where(x => (x.SignDate ?? x.Event!.EventDate) >= fromDate && (x.SignDate ?? x.Event!.EventDate) <= toDate)
            .OrderByDescending(x => x.SignDate ?? x.Event!.EventDate)
            .Take(take)
            .Select(x => new
            {
                x.ContractNo,
                SignDate = x.SignDate ?? x.Event!.EventDate,
                EventNo = x.Event != null ? x.Event.EventNo : null,
                EventDate = x.Event != null ? x.Event.EventDate : (DateTime?)null,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : x.Event != null ? x.Event.CustomerName : null,
                TotalAmount = x.TotalAmount,
                DepositAmount = x.DepositAmount,
                RemainingAmount = x.RemainingAmount,
                x.Status
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("ContractNo", x.ContractNo),
            ("SignDate", x.SignDate),
            ("EventNo", x.EventNo ?? string.Empty),
            ("EventDate", x.EventDate),
            ("CustomerName", x.CustomerName ?? string.Empty),
            ("TotalAmount", x.TotalAmount),
            ("DepositAmount", x.DepositAmount),
            ("RemainingAmount", x.RemainingAmount),
            ("Status", x.Status)))
            .ToList();

        return Result(code, name, BanquetContractColumns(), rows,
            Summary(
                ("TotalAmount", rows.Sum(x => GetDecimal(x, "TotalAmount"))),
                ("DepositAmount", rows.Sum(x => GetDecimal(x, "DepositAmount"))),
                ("RemainingAmount", rows.Sum(x => GetDecimal(x, "RemainingAmount")))));
    }

    private async Task<ReportResultDto> BuildBanquetDepositSummaryAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "BANQUET_DEPOSIT_SUMMARY";
        const string name = "Chi tiết đặt cọc tiệc";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.BanquetDeposits.AsNoTracking()
            .Where(x => x.DepositDate >= fromDate && x.DepositDate <= toDate)
            .OrderByDescending(x => x.DepositDate)
            .Take(take)
            .Select(x => new
            {
                x.DepositNo,
                x.DepositDate,
                EventNo = x.Event != null ? x.Event.EventNo : null,
                ContractNo = x.Contract != null ? x.Contract.ContractNo : null,
                CustomerName = x.Event != null ? x.Event.CustomerName : null,
                x.PaymentMethod,
                x.Amount,
                x.Receiver,
                x.Status
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("DepositNo", x.DepositNo),
            ("DepositDate", x.DepositDate),
            ("EventNo", x.EventNo ?? string.Empty),
            ("ContractNo", x.ContractNo ?? string.Empty),
            ("CustomerName", x.CustomerName ?? string.Empty),
            ("PaymentMethod", x.PaymentMethod),
            ("Amount", x.Amount),
            ("Receiver", x.Receiver ?? string.Empty),
            ("Status", x.Status)))
            .ToList();

        return Result(code, name, BanquetDepositColumns(), rows,
            Summary(("Amount", rows.Sum(x => GetDecimal(x, "Amount")))));
    }

    private async Task<ReportResultDto> BuildBanquetServiceDetailAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        const string code = "BANQUET_SERVICE_DETAIL";
        const string name = "Chi tiết dịch vụ tiệc";
        var fromDate = GetDate(request, "fromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var toDate = GetDate(request, "toDate", DateTime.Today.Date).Date.AddDays(1).AddTicks(-1);
        var take = GetEffectiveTake(request);

        var source = await _dbContext.BanquetServiceLines.AsNoTracking()
            .Where(x => x.Event != null && x.Event.EventDate >= fromDate && x.Event.EventDate <= toDate)
            .OrderByDescending(x => x.Event!.EventDate)
            .Take(take)
            .Select(x => new
            {
                EventNo = x.Event != null ? x.Event.EventNo : null,
                EventDate = x.Event != null ? x.Event.EventDate : (DateTime?)null,
                CustomerName = x.Event != null ? x.Event.CustomerName : null,
                x.ServiceType,
                x.Description,
                x.Qty,
                x.UnitPrice,
                x.VatRate,
                x.Amount,
                SupplierName = x.Supplier != null ? x.Supplier.SupplierName : null
            })
            .ToListAsync(cancellationToken);

        var rows = source.Select(x => Row(
            ("EventNo", x.EventNo ?? string.Empty),
            ("EventDate", x.EventDate),
            ("CustomerName", x.CustomerName ?? string.Empty),
            ("ServiceType", x.ServiceType),
            ("Description", x.Description),
            ("Qty", x.Qty),
            ("UnitPrice", x.UnitPrice),
            ("VatRate", x.VatRate),
            ("Amount", x.Amount),
            ("SupplierName", x.SupplierName ?? string.Empty)))
            .ToList();

        return Result(code, name, BanquetServiceColumns(), rows,
            Summary(
                ("Qty", rows.Sum(x => GetDecimal(x, "Qty"))),
                ("Amount", rows.Sum(x => GetDecimal(x, "Amount")))));
    }

    private async Task<List<AccountBalanceSource>> LoadBalancesUntilAsync(DateTime toDate, CancellationToken cancellationToken)
    {
        var lines = await LoadLinesUntilAsync(toDate, cancellationToken);
        return lines.GroupBy(x => x.AccountCode)
            .Select(x => new AccountBalanceSource(x.Key, x.Select(line => line.AccountName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty, x.Sum(line => line.Debit), x.Sum(line => line.Credit)))
            .ToList();
    }

    private async Task<List<LineSource>> LoadLinesUntilAsync(DateTime toDate, CancellationToken cancellationToken)
        => await LoadLinesAsync(null, toDate, cancellationToken);

    private async Task<List<LineSource>> LoadLinesBetweenAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
        => await LoadLinesAsync(fromDate, toDate, cancellationToken);

    private async Task<List<LineSource>> LoadLinesAsync(DateTime? fromDate, DateTime toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.JournalHeaders
            .AsNoTracking()
            .Where(x => x.Status == JournalStatus.Posted && x.PostingDate <= toDate);
        if (fromDate.HasValue)
        {
            query = query.Where(x => x.PostingDate >= fromDate.Value);
        }

        return await query
            .SelectMany(header => header.Lines.Select(line => new LineSource(line.AccountCode, line.AccountName, line.DebitAmount, line.CreditAmount)))
            .ToListAsync(cancellationToken);
    }

    private static ReportResultDto BuildAgingResult(string code, string name, string codeColumn, string nameColumn, IEnumerable<AgingSource> sources, DateTime asOfDate)
    {
        var rows = sources
            .GroupBy(x => new { x.Code, x.Name })
            .OrderBy(x => x.Key.Code)
            .Select(group =>
            {
                decimal current = 0, d1 = 0, d31 = 0, d61 = 0, d90 = 0;
                foreach (var item in group)
                {
                    var days = (asOfDate.Date - item.DueDate.Date).Days;
                    if (days <= 0) current += item.Amount;
                    else if (days <= 30) d1 += item.Amount;
                    else if (days <= 60) d31 += item.Amount;
                    else if (days <= 90) d61 += item.Amount;
                    else d90 += item.Amount;
                }

                return Row(
                    (codeColumn, group.Key.Code),
                    (nameColumn, group.Key.Name),
                    ("Current", current),
                    ("Overdue1To30", d1),
                    ("Overdue31To60", d31),
                    ("Overdue61To90", d61),
                    ("OverdueOver90", d90),
                    ("Total", current + d1 + d31 + d61 + d90));
            })
            .ToList();

        return Result(code, name, AgingColumns(codeColumn, nameColumn), rows, Summary(("Total", rows.Sum(x => GetDecimal(x, "Total")))));
    }

    private async Task<ReportResultDto> BuildTt99AccountMappingAsync(CancellationToken cancellationToken)
    {
        const string code = "TT99_ACCOUNT_MAPPING";
        const string name = "Đối chiếu mapping tài khoản TT99";
        var source = await _dbContext.TT99AccountMappings.AsNoTracking()
            .OrderBy(x => x.AccountCode)
            .ToListAsync(cancellationToken);
        var rows = source.Select(x => Row(
            ("AccountCode", x.AccountCode),
            ("Category", x.TT99Category),
            ("ReportSection", x.TT99ReportSection),
            ("DeclarationGroup", x.TT99DeclarationGroup),
            ("IsActive", x.IsActive))).ToList();
        return Result(code, name, Tt99AccountMappingColumns(), rows, Summary(("Total", rows.Count)));
    }

    private async Task<ReportResultDto> BuildTt99ComplianceIssuesAsync(CancellationToken cancellationToken)
    {
        const string code = "TT99_COMPLIANCE_ISSUES";
        const string name = "Danh sách lỗi tuân thủ TT99";
        var source = await _dbContext.TT99ComplianceIssues.AsNoTracking()
            .OrderByDescending(x => x.DetectedAtUtc)
            .Take(10_000)
            .ToListAsync(cancellationToken);
        var rows = source.Select(x => Row(
            ("DetectedAtUtc", x.DetectedAtUtc),
            ("RuleCode", x.RuleCode),
            ("Severity", x.Severity),
            ("Module", x.Module),
            ("Message", x.Message),
            ("AffectedEntity", x.AffectedEntity))).ToList();
        return Result(code, name, Tt99ComplianceIssueColumns(), rows, Summary(("Total", rows.Count)));
    }

    private async Task<ReportResultDto> BuildTt99DeclarationSummaryAsync(CancellationToken cancellationToken)
    {
        const string code = "TT99_DECLARATION_SUMMARY";
        const string name = "Tổng hợp kỳ kê khai TT99";
        var source = await _dbContext.TT99DeclarationPeriods.AsNoTracking()
            .OrderByDescending(x => x.PeriodCode)
            .Take(1_000)
            .ToListAsync(cancellationToken);
        var rows = source.Select(x => Row(
            ("PeriodCode", x.PeriodCode),
            ("FromDateUtc", x.FromDateUtc),
            ("ToDateUtc", x.ToDateUtc),
            ("Status", x.Status),
            ("UpdatedAtUtc", x.UpdatedAtUtc))).ToList();
        return Result(code, name, Tt99DeclarationSummaryColumns(), rows, Summary(("Total", rows.Count)));
    }

    private async Task<ReportResultDto> BuildTt200AccountMappingAsync(CancellationToken cancellationToken)
    {
        const string code = "TT200_ACCOUNT_MAPPING";
        const string name = "Đối chiếu mapping tài khoản TT200";
        var source = await _dbContext.TT200AccountMappings.AsNoTracking()
            .OrderBy(x => x.AccountCode)
            .ToListAsync(cancellationToken);
        var rows = source.Select(x => Row(
            ("AccountCode", x.AccountCode),
            ("Category", x.TT200Category),
            ("ReportSection", x.TT200ReportSection),
            ("ClosingGroup", x.TT200ClosingGroup),
            ("IsActive", x.IsActive))).ToList();
        return Result(code, name, Tt200AccountMappingColumns(), rows, Summary(("Total", rows.Count)));
    }

    private async Task<ReportResultDto> BuildTt200ComplianceIssuesAsync(CancellationToken cancellationToken)
    {
        const string code = "TT200_COMPLIANCE_ISSUES";
        const string name = "Danh sách lỗi tuân thủ TT200";
        var source = await _dbContext.TT200ComplianceIssues.AsNoTracking()
            .OrderByDescending(x => x.DetectedAtUtc)
            .Take(10_000)
            .ToListAsync(cancellationToken);
        var rows = source.Select(x => Row(
            ("DetectedAtUtc", x.DetectedAtUtc),
            ("RuleCode", x.RuleCode),
            ("Severity", x.Severity),
            ("Module", x.Module),
            ("Message", x.Message),
            ("AffectedEntity", x.AffectedEntity))).ToList();
        return Result(code, name, Tt200ComplianceIssueColumns(), rows, Summary(("Total", rows.Count)));
    }

    private async Task<ReportResultDto> BuildTt200DeclarationSummaryAsync(CancellationToken cancellationToken)
    {
        const string code = "TT200_DECLARATION_SUMMARY";
        const string name = "Tổng hợp kỳ kê khai TT200";
        var source = await _dbContext.TT200DeclarationPeriods.AsNoTracking()
            .OrderByDescending(x => x.PeriodCode)
            .Take(1_000)
            .ToListAsync(cancellationToken);
        var rows = source.Select(x => Row(
            ("PeriodCode", x.PeriodCode),
            ("FromDateUtc", x.FromDateUtc),
            ("ToDateUtc", x.ToDateUtc),
            ("Status", x.Status),
            ("UpdatedAtUtc", x.UpdatedAtUtc))).ToList();
        return Result(code, name, Tt200DeclarationSummaryColumns(), rows, Summary(("Total", rows.Count)));
    }

    private async Task<ReportResultDto> BuildTt200ClosingSummaryAsync(CancellationToken cancellationToken)
    {
        const string code = "TT200_CLOSING_SUMMARY";
        const string name = "Tổng hợp khóa sổ TT200";
        var source = await _dbContext.ClosingEntries.AsNoTracking()
            .Include(x => x.Lines)
            .OrderByDescending(x => x.ClosingDate)
            .Take(1_000)
            .ToListAsync(cancellationToken);
        var rows = source.Select(x => Row(
            ("Period", x.Period),
            ("Status", x.Status),
            ("ClosingDate", x.ClosingDate),
            ("Debit", x.DebitTotal()),
            ("Credit", x.CreditTotal()),
            ("Difference", x.DebitTotal() - x.CreditTotal()))).ToList();
        return Result(code, name, Tt200ClosingSummaryColumns(), rows, Summary(("Total", rows.Count)));
    }

    private static ReportRowDto MovementRow(DateTime date, string voucherNo, string type, int itemId, int warehouseId, decimal qtyIn, decimal qtyOut, decimal unitCost, decimal totalCost, IReadOnlyDictionary<int, Item> items, IReadOnlyDictionary<int, Warehouse> warehouses)
    {
        items.TryGetValue(itemId, out var item);
        warehouses.TryGetValue(warehouseId, out var warehouse);
        return Row(
            ("Date", date),
            ("VoucherNo", voucherNo),
            ("Type", type),
            ("Item", item is null ? itemId.ToString(CultureInfo.InvariantCulture) : $"{item.ItemCode} - {item.ItemName}"),
            ("Warehouse", warehouse?.WarehouseName ?? warehouseId.ToString(CultureInfo.InvariantCulture)),
            ("QtyIn", qtyIn),
            ("QtyOut", qtyOut),
            ("UnitCost", unitCost),
            ("TotalCost", totalCost));
    }

    private static IReadOnlyList<ReportColumnDto> LedgerColumns() => Columns(
        ("Date", "Ngày", "Date"), ("JournalNo", "Số bút toán", "Text"), ("SourceType", "Nguồn", "Text"),
        ("SourceNo", "Số chứng từ", "Text"), ("Description", "Diễn giải", "Text"), ("AccountCode", "Tài khoản", "Text"),
        ("AccountName", "Tên tài khoản", "Text"), ("Debit", "Nợ", "Money"), ("Credit", "Có", "Money"));

    private static IReadOnlyList<ReportColumnDto> TrialBalanceColumns() => Columns(
        ("AccountCode", "Tài khoản", "Text"), ("AccountName", "Tên tài khoản", "Text"),
        ("OpeningDebit", "Dư đầu Nợ", "Money"), ("OpeningCredit", "Dư đầu Có", "Money"),
        ("PeriodDebit", "Phát sinh Nợ", "Money"), ("PeriodCredit", "Phát sinh Có", "Money"),
        ("ClosingDebit", "Dư cuối Nợ", "Money"), ("ClosingCredit", "Dư cuối Có", "Money"));

    private static IReadOnlyList<ReportColumnDto> InventorySummaryColumns() => Columns(
        ("ItemCode", "Mã hàng", "Text"), ("ItemName", "Tên hàng", "Text"), ("Warehouse", "Kho", "Text"),
        ("OpeningQty", "SL đầu", "Number"), ("InQty", "SL nhập", "Number"), ("OutQty", "SL xuất", "Number"),
        ("ClosingQty", "SL cuối", "Number"), ("ClosingCost", "Giá trị cuối", "Money"));

    private static IReadOnlyList<ReportColumnDto> InventoryMovementColumns() => Columns(
        ("Date", "Ngày", "Date"), ("VoucherNo", "Số phiếu", "Text"), ("Type", "Loại", "Text"),
        ("Item", "Hàng hóa", "Text"), ("Warehouse", "Kho", "Text"), ("QtyIn", "SL nhập", "Number"),
        ("QtyOut", "SL xuất", "Number"), ("UnitCost", "Đơn giá vốn", "Money"), ("TotalCost", "Thành tiền", "Money"));

    private static IReadOnlyList<ReportColumnDto> StockCardColumns() => Columns(
        ("Date", "Ngày", "Date"), ("VoucherNo", "Số phiếu", "Text"), ("Type", "Loại", "Text"),
        ("Item", "Hàng hóa", "Text"), ("Warehouse", "Kho", "Text"), ("QtyIn", "Nhập", "Number"),
        ("QtyOut", "Xuất", "Number"), ("BalanceQty", "Tồn", "Number"), ("UnitCost", "Giá vốn", "Money"),
        ("TotalCost", "Thành tiền", "Money"));

    private static IReadOnlyList<ReportColumnDto> StockLevelColumns() => Columns(
        ("ItemCode", "Mã hàng", "Text"), ("ItemName", "Tên hàng", "Text"), ("Warehouse", "Kho", "Text"),
        ("ClosingQty", "Tồn cuối", "Number"), ("AverageCost", "Giá vốn TB", "Money"),
        ("ClosingCost", "Giá trị tồn", "Money"), ("Status", "Trạng thái", "Text"));

    private static IReadOnlyList<ReportColumnDto> CostAnalysisColumns() => Columns(
        ("ItemCode", "Mã hàng", "Text"), ("ItemName", "Tên hàng", "Text"), ("Warehouse", "Kho", "Text"),
        ("RemainingQty", "SL còn", "Number"), ("MovingAverageCost", "Giá BQ", "Money"),
        ("RemainingCost", "Giá trị còn", "Money"), ("IssueCost", "Giá vốn xuất", "Money"),
        ("TransactionCount", "Số giao dịch", "Number"));

    private static IReadOnlyList<ReportColumnDto> CashBankBookColumns() => Columns(
        ("Date", "Ngày", "Date"), ("Account", "Tài khoản/quỹ", "Text"), ("TransactionNo", "Số giao dịch", "Text"),
        ("ReferenceNo", "Tham chiếu", "Text"), ("Description", "Diễn giải", "Text"), ("Debit", "Thu", "Money"),
        ("Credit", "Chi", "Money"), ("Balance", "Số dư", "Money"), ("Status", "Trạng thái", "Text"), ("JournalStatus", "Ghi sổ", "Text"));

    private static IReadOnlyList<ReportColumnDto> BankReconciliationColumns() => Columns(
        ("Date", "Ngày", "Date"), ("Account", "Tài khoản", "Text"), ("ReconciliationNo", "Số đối chiếu", "Text"),
        ("BookBalance", "Số dư sổ", "Money"), ("StatementBalance", "Số dư sao kê", "Money"), ("Difference", "Chênh lệch", "Money"),
        ("MatchedCount", "Đã khớp", "Number"), ("UnmatchedCount", "Chưa khớp", "Number"), ("Status", "Trạng thái", "Text"));

    private static IReadOnlyList<ReportColumnDto> DailyCashflowColumns() => Columns(
        ("Date", "Ngày", "Date"), ("Inflow", "Thu", "Money"), ("Outflow", "Chi", "Money"), ("NetFlow", "Dòng tiền ròng", "Money"));

    private static IReadOnlyList<ReportColumnDto> CashBalanceColumns() => Columns(
        ("Type", "Loại", "Text"), ("Code", "Mã/Tài khoản", "Text"), ("Name", "Tên", "Text"),
        ("Currency", "Tiền tệ", "Text"), ("CurrentBalance", "Số dư hiện tại", "Money"), ("AvailableBalance", "Số dư khả dụng", "Money"), ("Status", "Trạng thái", "Text"));

    private static IReadOnlyList<ReportColumnDto> BreakfastCafeSalesColumns() => Columns(
        ("OrderNumber", "Mã đơn", "Text"), ("OrderDate", "Ngày giờ", "Date"), ("AreaName", "Khu vực", "Text"),
        ("TableName", "Bàn", "Text"), ("CustomerName", "Khách hàng", "Text"), ("OrderType", "Loại đơn", "Text"),
        ("Status", "Trạng thái đơn", "Text"), ("PaymentStatus", "Thanh toán", "Text"), ("ServiceStatus", "Phục vụ", "Text"),
        ("TotalAmount", "Tổng tiền", "Money"), ("PaidAmount", "Đã thu", "Money"), ("RemainingAmount", "Còn lại", "Money"));

    private static IReadOnlyList<ReportColumnDto> BreakfastCafePaymentColumns() => Columns(
        ("PaymentDate", "Ngày thu", "Date"), ("OrderNumber", "Mã đơn", "Text"), ("TableName", "Bàn", "Text"),
        ("CustomerName", "Khách hàng", "Text"), ("PaymentMethod", "Phương thức", "Text"), ("ReferenceNo", "Tham chiếu", "Text"),
        ("Amount", "Số tiền", "Money"), ("Status", "Trạng thái", "Text"), ("CreatedBy", "Người thu", "Text"));

    private static IReadOnlyList<ReportColumnDto> BreakfastCafeItemSalesColumns() => Columns(
        ("MenuItemCode", "Mã món", "Text"), ("MenuItemName", "Tên món", "Text"), ("UnitName", "ĐVT", "Text"),
        ("VatRate", "VAT %", "Number"), ("Quantity", "Số lượng", "Number"), ("OrderCount", "Số đơn", "Number"),
        ("Revenue", "Doanh thu", "Money"));

    private static IReadOnlyList<ReportColumnDto> BreakfastCafeKitchenTicketColumns() => Columns(
        ("TicketNumber", "Mã phiếu", "Text"), ("SentAt", "Giờ gửi", "Date"), ("OrderNumber", "Mã đơn", "Text"),
        ("TableName", "Bàn", "Text"), ("Station", "Trạm", "Text"), ("Status", "Trạng thái", "Text"),
        ("SentBy", "Người gửi", "Text"), ("Note", "Ghi chú", "Text"));

    private static IReadOnlyList<ReportColumnDto> AlacarteOrderSummaryColumns() => Columns(
        ("OrderNo", "Mã đơn", "Text"), ("OrderDate", "Ngày giờ", "Date"), ("RoomName", "Phòng/Khu vực", "Text"),
        ("CustomerName", "Khách hàng", "Text"), ("GuestCount", "Số khách", "Number"), ("OrderType", "Loại đơn", "Text"),
        ("Status", "Trạng thái đơn", "Text"), ("PaymentStatus", "Thanh toán", "Text"),
        ("TotalAmount", "Tổng tiền", "Money"), ("PaidAmount", "Đã thu", "Money"), ("RemainingAmount", "Còn lại", "Money"));

    private static IReadOnlyList<ReportColumnDto> AlacarteQuoteSummaryColumns() => Columns(
        ("QuoteNo", "Mã báo giá", "Text"), ("EventDate", "Ngày phục vụ", "Date"), ("TimeRange", "Khung giờ", "Text"),
        ("CustomerName", "Khách hàng", "Text"), ("Phone", "SĐT", "Text"), ("RoomName", "Phòng/Khu vực", "Text"),
        ("TableCount", "Số bàn", "Number"), ("GuestCount", "Số khách", "Number"), ("TotalAmount", "Tổng tiền", "Money"),
        ("DepositAmount", "Đặt cọc", "Money"), ("Status", "Trạng thái", "Text"), ("PreparedBy", "Người lập", "Text"));

    private static IReadOnlyList<ReportColumnDto> AlacarteReservationColumns() => Columns(
        ("ReservationNo", "Mã đặt bàn", "Text"), ("ReservationDateTime", "Giờ đến", "Date"), ("CustomerName", "Khách hàng", "Text"),
        ("Phone", "SĐT", "Text"), ("RoomName", "Phòng", "Text"), ("TableName", "Bàn", "Text"),
        ("GuestCount", "Số khách", "Number"), ("Status", "Trạng thái", "Text"), ("Note", "Ghi chú", "Text"));

    private static IReadOnlyList<ReportColumnDto> AlacarteDepositColumns() => Columns(
        ("DepositNo", "Mã đặt cọc", "Text"), ("Date", "Ngày thu", "Date"), ("OrderNo", "Mã đơn", "Text"),
        ("ContractNo", "Mã hợp đồng", "Text"), ("ReservationNo", "Mã đặt bàn", "Text"), ("PaymentMethod", "Phương thức", "Text"),
        ("Amount", "Số tiền", "Money"), ("Receiver", "Người nhận", "Text"), ("Status", "Trạng thái", "Text"));

    private static IReadOnlyList<ReportColumnDto> BanquetEventSummaryColumns() => Columns(
        ("EventNo", "Mã sự kiện", "Text"), ("EventDate", "Ngày tiệc", "Date"), ("TimeRange", "Khung giờ", "Text"),
        ("EventType", "Loại tiệc", "Text"), ("CustomerName", "Khách hàng", "Text"), ("RoomName", "Sảnh/Phòng", "Text"),
        ("GuestCount", "Số khách", "Number"), ("TableCount", "Số bàn", "Number"), ("Status", "Trạng thái", "Text"),
        ("ContractTotal", "Giá trị hợp đồng", "Money"), ("DepositTotal", "Đặt cọc", "Money"));

    private static IReadOnlyList<ReportColumnDto> BanquetContractColumns() => Columns(
        ("ContractNo", "Mã hợp đồng", "Text"), ("SignDate", "Ngày ký", "Date"), ("EventNo", "Mã sự kiện", "Text"),
        ("EventDate", "Ngày tiệc", "Date"), ("CustomerName", "Khách hàng", "Text"),
        ("TotalAmount", "Giá trị HĐ", "Money"), ("DepositAmount", "Đặt cọc", "Money"), ("RemainingAmount", "Còn lại", "Money"),
        ("Status", "Trạng thái", "Text"));

    private static IReadOnlyList<ReportColumnDto> BanquetDepositColumns() => Columns(
        ("DepositNo", "Mã đặt cọc", "Text"), ("DepositDate", "Ngày thu", "Date"), ("EventNo", "Mã sự kiện", "Text"),
        ("ContractNo", "Mã hợp đồng", "Text"), ("CustomerName", "Khách hàng", "Text"), ("PaymentMethod", "Phương thức", "Text"),
        ("Amount", "Số tiền", "Money"), ("Receiver", "Người nhận", "Text"), ("Status", "Trạng thái", "Text"));

    private static IReadOnlyList<ReportColumnDto> BanquetServiceColumns() => Columns(
        ("EventNo", "Mã sự kiện", "Text"), ("EventDate", "Ngày tiệc", "Date"), ("CustomerName", "Khách hàng", "Text"),
        ("ServiceType", "Loại DV", "Text"), ("Description", "Diễn giải", "Text"), ("Qty", "Số lượng", "Number"),
        ("UnitPrice", "Đơn giá", "Money"), ("VatRate", "VAT %", "Number"), ("Amount", "Thành tiền", "Money"),
        ("SupplierName", "Nhà cung cấp", "Text"));

    private static IReadOnlyList<ReportColumnDto> FixedAssetRegisterColumns() => Columns(
        ("AssetCode", "Mã TSCĐ", "Text"), ("AssetName", "Tên TSCĐ", "Text"), ("Category", "Nhóm", "Text"),
        ("PurchaseDate", "Ngày mua", "Date"), ("OriginalCost", "Nguyên giá", "Money"),
        ("AccumulatedDepreciation", "Khấu hao lũy kế", "Money"), ("RemainingValue", "Giá trị còn lại", "Money"),
        ("Method", "Phương pháp", "Text"), ("UsefulLifeMonths", "Thời gian KH", "Number"),
        ("Status", "Trạng thái", "Text"), ("JournalStatus", "Ghi sổ", "Text"), ("JournalId", "Journal", "Text"));

    private static IReadOnlyList<ReportColumnDto> DepreciationScheduleColumns() => Columns(
        ("Period", "Kỳ", "Text"), ("Date", "Ngày KH", "Date"), ("AssetCode", "Mã TSCĐ", "Text"),
        ("AssetName", "Tên TSCĐ", "Text"), ("Amount", "Khấu hao", "Money"),
        ("AccumulatedAfter", "Lũy kế sau KH", "Money"), ("RemainingAfter", "Còn lại sau KH", "Money"),
        ("Status", "Trạng thái", "Text"), ("JournalStatus", "Ghi sổ", "Text"), ("JournalId", "Journal", "Text"));

    private static IReadOnlyList<ReportColumnDto> AssetIncreaseDecreaseColumns() => Columns(
        ("Date", "Ngày", "Date"), ("AssetCode", "Mã TSCĐ", "Text"), ("AssetName", "Tên TSCĐ", "Text"),
        ("Type", "Loại biến động", "Text"), ("Amount", "Số tiền", "Money"), ("Description", "Diễn giải", "Text"),
        ("Status", "Trạng thái", "Text"), ("JournalStatus", "Ghi sổ", "Text"));

    private static IReadOnlyList<ReportColumnDto> ToolAssetRegisterColumns() => Columns(
        ("ToolCode", "Mã CCDC", "Text"), ("ToolName", "Tên CCDC", "Text"), ("Category", "Nhóm", "Text"),
        ("PurchaseDate", "Ngày mua", "Date"), ("OriginalCost", "Nguyên giá", "Money"),
        ("AllocatedAmount", "Đã phân bổ", "Money"), ("RemainingAmount", "Còn lại", "Money"),
        ("AllocationMonths", "Số kỳ PB", "Number"), ("MonthlyAllocation", "PB tháng", "Money"),
        ("Status", "Trạng thái", "Text"), ("JournalStatus", "Ghi sổ", "Text"), ("JournalId", "Journal", "Text"));

    private static IReadOnlyList<ReportColumnDto> ToolAllocationColumns() => Columns(
        ("Period", "Kỳ", "Text"), ("Date", "Ngày PB", "Date"), ("ToolCode", "Mã CCDC", "Text"),
        ("ToolName", "Tên CCDC", "Text"), ("Amount", "Phân bổ", "Money"),
        ("AccumulatedAfter", "Lũy kế sau PB", "Money"), ("RemainingAfter", "Còn lại sau PB", "Money"),
        ("Status", "Trạng thái", "Text"), ("JournalStatus", "Ghi sổ", "Text"), ("JournalId", "Journal", "Text"));

    private static IReadOnlyList<ReportColumnDto> PrepaidExpenseScheduleColumns() => Columns(
        ("Period", "Kỳ", "Text"), ("Date", "Ngày PB", "Date"), ("SourceNo", "Nguồn", "Text"),
        ("SourceName", "Tên", "Text"), ("PrepaidAccount", "TK trả trước", "Text"), ("ExpenseAccount", "TK chi phí", "Text"),
        ("Amount", "Phân bổ", "Money"), ("RemainingAfter", "Còn lại", "Money"),
        ("Status", "Trạng thái", "Text"), ("JournalStatus", "Ghi sổ", "Text"));

    private static IReadOnlyList<ReportColumnDto> AgingColumns(string codeColumn, string nameColumn) => Columns(
        (codeColumn, "Mã đối tượng", "Text"), (nameColumn, "Tên đối tượng", "Text"),
        ("Current", "Chưa đến hạn", "Money"), ("Overdue1To30", "1-30 ngày", "Money"),
        ("Overdue31To60", "31-60 ngày", "Money"), ("Overdue61To90", "61-90 ngày", "Money"),
        ("OverdueOver90", "> 90 ngày", "Money"), ("Total", "Tổng cộng", "Money"));

    private static IReadOnlyList<ReportColumnDto> VatColumns() => Columns(
        ("DeclarationPeriod", "Kỳ kê khai", "Text"), ("InvoiceDate", "Ngày hóa đơn", "Date"),
        ("InvoiceNo", "Số hóa đơn", "Text"), ("PartnerType", "Đối tượng", "Text"),
        ("TaxCode", "Mã thuế", "Text"), ("TaxRate", "Thuế suất", "Number"),
        ("TaxBaseAmount", "Giá trị tính thuế", "Money"), ("TaxAmount", "Tiền thuế", "Money"),
        ("Status", "Trạng thái", "Text"));

    private static IReadOnlyList<ReportColumnDto> FinancialStatementColumns() => Columns(
        ("Section", "Nhóm", "Text"), ("AccountCode", "Mã dòng/TK", "Text"),
        ("AccountName", "Tên dòng/Tài khoản", "Text"), ("Amount", "Số tiền", "Money"));
    private static IReadOnlyList<ReportColumnDto> Tt99AccountMappingColumns() => Columns(
        ("AccountCode", "Tài khoản", "Text"), ("Category", "Nhóm TT99", "Text"),
        ("ReportSection", "Phần báo cáo", "Text"), ("DeclarationGroup", "Nhóm kê khai", "Text"),
        ("IsActive", "Kích hoạt", "Text"));
    private static IReadOnlyList<ReportColumnDto> Tt99ComplianceIssueColumns() => Columns(
        ("DetectedAtUtc", "Thời điểm", "Date"), ("RuleCode", "Mã rule", "Text"),
        ("Severity", "Mức độ", "Text"), ("Module", "Phân hệ", "Text"),
        ("Message", "Nội dung", "Text"), ("AffectedEntity", "Đối tượng", "Text"));
    private static IReadOnlyList<ReportColumnDto> Tt99DeclarationSummaryColumns() => Columns(
        ("PeriodCode", "Kỳ", "Text"), ("FromDateUtc", "Từ ngày", "Date"),
        ("ToDateUtc", "Đến ngày", "Date"), ("Status", "Trạng thái", "Text"),
        ("UpdatedAtUtc", "Cập nhật", "Date"));

    private static IReadOnlyList<ReportColumnDto> Tt200AccountMappingColumns() => Columns(
        ("AccountCode", "Tài khoản", "Text"), ("Category", "Nhóm TT200", "Text"),
        ("ReportSection", "Phần báo cáo", "Text"), ("ClosingGroup", "Nhóm khóa sổ", "Text"),
        ("IsActive", "Kích hoạt", "Text"));
    private static IReadOnlyList<ReportColumnDto> Tt200ComplianceIssueColumns() => Columns(
        ("DetectedAtUtc", "Thời điểm", "Date"), ("RuleCode", "Mã rule", "Text"),
        ("Severity", "Mức độ", "Text"), ("Module", "Phân hệ", "Text"),
        ("Message", "Nội dung", "Text"), ("AffectedEntity", "Đối tượng", "Text"));
    private static IReadOnlyList<ReportColumnDto> Tt200DeclarationSummaryColumns() => Columns(
        ("PeriodCode", "Kỳ", "Text"), ("FromDateUtc", "Từ ngày", "Date"),
        ("ToDateUtc", "Đến ngày", "Date"), ("Status", "Trạng thái", "Text"),
        ("UpdatedAtUtc", "Cập nhật", "Date"));
    private static IReadOnlyList<ReportColumnDto> Tt200ClosingSummaryColumns() => Columns(
        ("Period", "Kỳ", "Text"), ("Status", "Trạng thái", "Text"),
        ("ClosingDate", "Ngày khóa sổ", "Date"), ("Debit", "Nợ", "Money"),
        ("Credit", "Có", "Money"), ("Difference", "Chênh lệch", "Money"));

    private static string ResolveStatementCategory(string accountCode, string? category)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            return category;
        }

        if (accountCode.StartsWith("3", StringComparison.Ordinal)) return "Liability";
        if (accountCode.StartsWith("4", StringComparison.Ordinal)) return "Equity";
        if (accountCode.StartsWith("5", StringComparison.Ordinal) || accountCode.StartsWith("7", StringComparison.Ordinal)) return "Revenue";
        if (accountCode.StartsWith("6", StringComparison.Ordinal) || accountCode.StartsWith("8", StringComparison.Ordinal)) return "Expense";
        return "Asset";
    }

    private static IReadOnlyList<ReportColumnDto> Columns(params (string Name, string DisplayName, string DataType)[] columns)
        => columns.Select(x => new ReportColumnDto { Name = x.Name, DisplayName = x.DisplayName, DataType = x.DataType }).ToList();

    private static ReportResultDto Result(string code, string name, IReadOnlyList<ReportColumnDto> columns, IReadOnlyList<ReportRowDto> rows, Dictionary<string, object?>? summary = null)
        => new()
        {
            ReportCode = code,
            ReportName = name,
            Columns = columns,
            Rows = rows,
            Summary = summary ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
            GeneratedAt = DateTime.Now,
            RowCount = rows.Count
        };

    private static ReportRowDto Row(params (string Name, object? Value)[] values)
        => new() { Values = values.ToDictionary(x => x.Name, x => x.Value, StringComparer.OrdinalIgnoreCase) };

    private static Dictionary<string, object?> Summary(params (string Name, object? Value)[] values)
        => values.ToDictionary(x => x.Name, x => x.Value, StringComparer.OrdinalIgnoreCase);

    private static DateTime GetDate(ReportRequestDto request, string key, DateTime fallback)
        => request.Parameters.TryGetValue(key, out var raw) && DateTime.TryParse(raw, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var value)
            ? value
            : fallback;

    private static string? GetString(ReportRequestDto request, string key)
        => request.Parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static int GetEffectiveTake(ReportRequestDto request)
    {
        var pageSize = request.PageSize.HasValue
            ? Math.Clamp(request.PageSize.Value, 1, MaxInteractiveRows)
            : MaxInteractiveRows;
        return pageSize;
    }

    private static decimal GetDecimalParameter(ReportRequestDto request, string key, decimal fallback)
        => request.Parameters.TryGetValue(key, out var value)
           && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static string NormalizeReportPeriod(string period)
    {
        var trimmed = period.Trim();
        if (DateTime.TryParse($"01/{trimmed}", CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var monthYear))
        {
            return monthYear.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse($"{trimmed}-01", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yearMonth))
        {
            return yearMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        return trimmed;
    }

    private static decimal GetDecimal(ReportRowDto row, string key)
        => row.Values.TryGetValue(key, out var value) && value is decimal amount ? amount : 0m;

    private static string SafeSheetName(string name)
        => new string(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).Take(31).ToArray());

    private static XLCellValue ToCellValue(object? value)
        => value switch
        {
            null => string.Empty,
            DateTime date => date,
            decimal amount => amount,
            int number => number,
            long number => number,
            double number => number,
            _ => value.ToString() ?? string.Empty
        };

    private static string FormatValue(object? value)
        => value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            decimal amount => amount.ToString("0.##", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        return value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private sealed record AgingSource(string Code, string Name, DateTime DueDate, decimal Amount);
    private sealed record LineSource(string AccountCode, string AccountName, decimal Debit, decimal Credit);
    private sealed record AccountBalanceSource(string AccountCode, string AccountName, decimal Debit, decimal Credit);

    private static class SimplePdfWriter
    {
        private const int LinesPerPage = 42;

        public static byte[] Write(ReportResultDto report, LedgerPdfPresentation? presentation)
        {
            var lines = new List<string>
            {
                report.ReportName,
                $"Ngay lap: {report.GeneratedAt:dd/MM/yyyy HH:mm}",
                $"So dong: {report.RowCount}",
                string.Join(" | ", report.Columns.Select(x => StripPdfText(x.DisplayName)))
            };

            lines.AddRange(report.Rows.Take(40).Select(row => string.Join(" | ", report.Columns.Select(c => StripPdfText(FormatValue(row.Values.TryGetValue(c.Name, out var value) ? value : null))))));
            if (report.Rows.Count > 40)
            {
                lines.Add($"... con {report.Rows.Count - 40} dong. Dung Excel/CSV de xem day du.");
            }

            if (presentation is not null)
            {
                if (!string.IsNullOrWhiteSpace(presentation.FooterNote))
                {
                    lines.Add(string.Empty);
                    var normalizedFooter = string.Join(" ", presentation.FooterNote.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                    lines.Add($"Ghi chu: {StripPdfText(normalizedFooter)}");
                }
                if (presentation.ShowAccountantSignature)
                {
                    lines.Add("Ke toan");
                }
                if (presentation.ShowDirectorSignature)
                {
                    lines.Add("Giam doc");
                }
            }

            var pages = lines
                .Chunk(LinesPerPage)
                .Select(page => (IReadOnlyList<string>)page)
                .ToArray();
            return BuildPdf(pages);
        }

        private static byte[] BuildPdf(IReadOnlyList<IReadOnlyList<string>> pages)
        {
            var objects = new List<string>
            {
                string.Empty,
                string.Empty,
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
            };

            var pageObjectIds = new List<int>();
            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                var pageObjectId = objects.Count + 1;
                var contentObjectId = pageObjectId + 1;
                pageObjectIds.Add(pageObjectId);
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectId} 0 R >>");

                var content = new StringBuilder();
                content.AppendLine("BT /F1 10 Tf 40 555 Td 12 TL");
                foreach (var line in pages[pageIndex])
                {
                    content.Append('(').Append(EscapePdf(line)).AppendLine(") Tj T*");
                }
                content.Append("ET");
                var streamText = content.ToString();
                var streamBytes = Encoding.ASCII.GetBytes(streamText);
                objects.Add($"<< /Length {streamBytes.Length} >>\nstream\n{streamText}\nendstream");
            }

            objects[0] = "<< /Type /Catalog /Pages 2 0 R >>";
            objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>";

            using var memory = new MemoryStream();
            using var writer = new StreamWriter(memory, Encoding.ASCII, leaveOpen: true);
            writer.WriteLine("%PDF-1.4");
            var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Count; i++)
            {
                writer.Flush();
                offsets.Add(memory.Position);
                writer.WriteLine($"{i + 1} 0 obj");
                writer.WriteLine(objects[i]);
                writer.WriteLine("endobj");
            }

            writer.Flush();
            var xrefOffset = memory.Position;
            writer.WriteLine("xref");
            writer.WriteLine($"0 {objects.Count + 1}");
            writer.WriteLine("0000000000 65535 f ");
            foreach (var offset in offsets.Skip(1))
            {
                writer.WriteLine($"{offset:0000000000} 00000 n ");
            }

            writer.WriteLine("trailer");
            writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefOffset);
            writer.WriteLine("%%EOF");
            writer.Flush();
            return memory.ToArray();
        }

        private static string StripPdfText(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark && c <= 127)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static string EscapePdf(string value)
            => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
