using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Services;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Repositories;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Integration.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace ASMILE.BO.Tests.Integration;

public sealed class TrainingDataE2ETests
{
    private readonly ITestOutputHelper _output;

    public TrainingDataE2ETests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ResetFoundationFullTraining_ProvidesDemoReadyDataAndReports()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var services = CreateRuntime(db);
        var training = services.TrainingDataService;

        var reset = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetToBlank,
            ResetScope = TrainingDataResetScope.BusinessAndOperationalLogs,
            IsDryRun = true,
            ConfirmationText = "RESET ALL BUSINESS DATA"
        });
        reset.Success.Should().BeTrue();

        var foundation = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFoundation,
            SeedProfile = TrainingDataSeedProfile.FoundationOnly,
            ConfirmationText = "RESET DATA"
        });
        foundation.Success.Should().BeTrue();

        var full = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFullTraining,
            SeedProfile = TrainingDataSeedProfile.FullTraining,
            ConfirmationText = "RESET DATA"
        });
        if (!full.Success)
        {
            _output.WriteLine($"FullTraining failed: {full.ErrorMessage}");
            foreach (var step in full.Steps)
            {
                _output.WriteLine($"[{step.StepOrder}] {step.StepCode} | Success={step.Success} | {step.Message} | Error={step.ErrorMessage}");
            }
        }
        full.Success.Should().BeTrue();
        full.ValidationWarnings.Should().NotContain(x => x.Contains("chua", StringComparison.OrdinalIgnoreCase));
        (await db.TrainingDataMarkers.CountAsync()).Should().BeGreaterThan(0);
        var markerEntities = await db.TrainingDataMarkers.Select(x => x.EntityName).Distinct().ToListAsync();
        markerEntities.Should().Contain(new[]
        {
            "Customers", "Suppliers", "Items", "Warehouses", "CashFunds", "BankAccounts",
            "StockReceipts", "StockIssues", "JournalHeaders", "JournalLines",
            "VatTransactions", "CashTransactions", "BankTransactions", "ReceivableTransactions", "PayableTransactions"
        });

        await AssertDemoDataAsync(db, services.ReportEngine);
        await AssertReportsAsync(services.ReportEngine);

        var preflight = await training.GetPreflightAsync();
        preflight.Should().NotBeNull();
        preflight.BusinessDocumentCount.Should().BeGreaterThan(0);

        var resetTrainingOnly = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetTrainingOnly,
            SeedProfile = TrainingDataSeedProfile.FullTraining,
            ConfirmationText = "RESET TRAINING DATA"
        });
        resetTrainingOnly.Success.Should().BeTrue();
    }

    [Fact]
    public async Task FullReset_ShouldBeBlocked_WhenRealDataRiskAndOptionDisabled()
    {
        await using var db = TestDbContextFactory.CreateContext();
        db.JournalHeaders.Add(new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = "JV-RISK-INT-1",
            JournalDate = DateTime.UtcNow.Date,
            PostingDate = DateTime.UtcNow.Date,
            PeriodCode = "2026-05",
            SourceType = "Manual",
            SourceNo = "RISK-1",
            Status = Core.Enums.JournalStatus.Posted
        });
        await db.SaveChangesAsync();

        var services = CreateRuntime(db, allowResetWithRealDataRisk: false);
        var training = services.TrainingDataService;

        var act = () => training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetToBlank,
            ResetScope = TrainingDataResetScope.BusinessAndOperationalLogs,
            ConfirmationText = "RESET ALL BUSINESS DATA"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RestaurantAndHotelProfiles_ShouldProduceHospitalityData()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var services = CreateRuntime(db);
        var training = services.TrainingDataService;

        var restaurant = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFullTraining,
            SeedProfile = TrainingDataSeedProfile.RestaurantDemo,
            ConfirmationText = "RESET DATA"
        });
        restaurant.Success.Should().BeTrue();
        (await db.Warehouses.CountAsync(x => x.WarehouseCode == "KHO-BEP" || x.WarehouseCode == "KHO-BAR")).Should().BeGreaterThan(0);
        (await db.VatTransactions.CountAsync()).Should().BeGreaterThan(0);
        (await db.InventoryCostTransactions.CountAsync()).Should().BeGreaterThan(0);

        var resetTraining = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetTrainingOnly,
            SeedProfile = TrainingDataSeedProfile.RestaurantDemo,
            ConfirmationText = "RESET TRAINING DATA"
        });
        resetTraining.Success.Should().BeTrue();

        var hotel = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFullTraining,
            SeedProfile = TrainingDataSeedProfile.HotelDemo,
            ConfirmationText = "RESET DATA"
        });
        hotel.Success.Should().BeTrue();
        (await db.Items.CountAsync(x => x.ItemCode.StartsWith("HOTEL-"))).Should().BeGreaterThan(0);
        (await db.JournalHeaders.CountAsync(x => x.SourceType == "TrainingDataHotel")).Should().BeGreaterThan(0);
        (await db.ReceivableTransactions.CountAsync()).Should().BeGreaterThan(0);
        (await db.HotelRoomTypes.CountAsync(x => x.Code == "STD")).Should().Be(1);
        (await db.HotelRooms.CountAsync()).Should().Be(6);
        var seededRooms = await db.HotelRooms.AsNoTracking().ToListAsync();
        seededRooms.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.RoomCondition));
        seededRooms.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.View));
        seededRooms.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.BedType));
        seededRooms.Single(x => x.RoomNumber == "102").IsConnecting.Should().BeTrue();
        seededRooms.Single(x => x.RoomNumber == "202").IsSmoking.Should().BeTrue();
        (await db.GuestProfiles.CountAsync()).Should().Be(3);
        (await db.FrontOfficeReservations.CountAsync()).Should().Be(2);
        (await db.HotelStays.CountAsync()).Should().Be(2);
        (await db.HotelFolios.CountAsync()).Should().Be(1);
        (await db.HotelFolioEntries.CountAsync()).Should().Be(4);

        var roomCount = await db.HotelRooms.CountAsync();
        var secondHotel = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFullTraining,
            SeedProfile = TrainingDataSeedProfile.HotelDemo,
            ConfirmationText = "RESET DATA"
        });
        secondHotel.Success.Should().BeTrue();
        (await db.HotelRooms.CountAsync()).Should().Be(roomCount);
    }

    [Fact]
    public async Task FoundationSeed_ShouldCreateTrainingPerson_WithValidDepartment()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var services = CreateRuntime(db);
        var training = services.TrainingDataService;

        var result = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFoundation,
            SeedProfile = TrainingDataSeedProfile.FoundationOnly,
            ConfirmationText = "RESET DATA"
        });

        result.Success.Should().BeTrue();
        var person = await db.People.SingleAsync(x => x.PersonCode == "NV001");
        (await db.Departments.AnyAsync(x => x.Id == person.DepartmentId)).Should().BeTrue();
        person.DepartmentId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FoundationSeed_ShouldRepairInvalidTrainingPeopleDepartmentReferences()
    {
        await using var db = TestDbContextFactory.CreateContext();
        db.People.Add(new Person
        {
            PersonCode = "NV009",
            FullName = "Nhân sự training lỗi",
            DepartmentId = 0,
            Position = "Test",
            Phone = "0909999999",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var services = CreateRuntime(db);
        var training = services.TrainingDataService;
        var result = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFoundation,
            SeedProfile = TrainingDataSeedProfile.FoundationOnly,
            ConfirmationText = "RESET DATA"
        });

        result.Success.Should().BeTrue();
        var person = await db.People.SingleAsync(x => x.PersonCode == "NV009");
        (await db.Departments.AnyAsync(x => x.Id == person.DepartmentId)).Should().BeTrue();
        person.DepartmentId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FoundationSeed_ShouldBeIdempotent_ForTrainingPersonAndDepartment()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var services = CreateRuntime(db);
        var training = services.TrainingDataService;

        var first = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFoundation,
            SeedProfile = TrainingDataSeedProfile.FoundationOnly,
            ConfirmationText = "RESET DATA"
        });
        var second = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFoundation,
            SeedProfile = TrainingDataSeedProfile.FoundationOnly,
            ConfirmationText = "RESET DATA"
        });

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
        (await db.People.CountAsync(x => x.PersonCode == "NV001")).Should().Be(1);
        (await db.Departments.CountAsync(x => x.DepartmentCode == "HCKT")).Should().BeGreaterThanOrEqualTo(1);
    }

    private static async Task AssertDemoDataAsync(AppDbContext db, IReportEngine reportEngine)
    {
        var postedJournals = await db.JournalHeaders.CountAsync(x => x.Status == Core.Enums.JournalStatus.Posted);
        postedJournals.Should().BeGreaterThan(0);

        var trialBalance = await reportEngine.ExecuteAsync(new ReportRequestDto
        {
            ReportCode = "TRIAL_BALANCE",
            Parameters = new Dictionary<string, string?>()
        });
        trialBalance.RowCount.Should().BeGreaterThan(0);
        var totalDebit = trialBalance.Rows.Sum(x => ReadDecimal(x, "PeriodDebit"));
        var totalCredit = trialBalance.Rows.Sum(x => ReadDecimal(x, "PeriodCredit"));
        totalDebit.Should().BeGreaterThan(0);
        totalCredit.Should().BeGreaterThan(0);
        Math.Abs(totalDebit - totalCredit).Should().BeLessThanOrEqualTo(0.01m);

        var arCount = await db.ReceivableTransactions.CountAsync(x => x.CustomerId != Guid.Empty);
        arCount.Should().BeGreaterThan(0);
        var apCount = await db.PayableTransactions.CountAsync(x => x.SupplierId != Guid.Empty);
        apCount.Should().BeGreaterThan(0);

        var stockBalances = await db.StockBalances.ToListAsync();
        stockBalances.Should().NotBeEmpty();
        stockBalances.Any(x => x.InQty > 0).Should().BeTrue();
        stockBalances.Any(x => x.OutQty > 0).Should().BeTrue();
        stockBalances.Any(x => x.ClosingQty >= 0).Should().BeTrue();
        stockBalances.Any(x => x.ClosingQty < 0).Should().BeFalse();

        var issueCount = await db.StockIssues.CountAsync();
        issueCount.Should().BeGreaterThan(0);
        var issueCostCount = await db.InventoryCostTransactions.CountAsync(x => x.SourceType == "StockIssue");
        issueCostCount.Should().BeGreaterThan(0);

        var vatInput = await db.VatTransactions.CountAsync(x => x.Direction == "Input");
        var vatOutput = await db.VatTransactions.CountAsync(x => x.Direction == "Output");
        vatInput.Should().BeGreaterThan(0);
        vatOutput.Should().BeGreaterThan(0);

        var cashPosted = await db.CashTransactions.CountAsync(x => x.Status == "Posted");
        var bankPosted = await db.BankTransactions.CountAsync(x => x.Status == "Posted");
        cashPosted.Should().BeGreaterThan(0);
        bankPosted.Should().BeGreaterThan(0);
    }

    private async Task AssertReportsAsync(IReportEngine reportEngine)
    {
        var requestedCodes = new[]
        {
            "GENERAL_LEDGER",
            "TRIAL_BALANCE",
            "INVENTORY_SUMMARY",
            "AR_AGING",
            "AP_AGING",
            "VAT_INPUT",
            "VAT_OUTPUT"
        };

        var templateCodes = (await reportEngine.GetTemplatesAsync())
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var reportCode in requestedCodes)
        {
            if (!templateCodes.Contains(reportCode))
            {
                _output.WriteLine($"SKIP report {reportCode}: code not found in current catalog.");
                continue;
            }

            var result = await reportEngine.ExecuteAsync(new ReportRequestDto
            {
                ReportCode = reportCode,
                Parameters = new Dictionary<string, string?>()
            });

            result.ReportCode.Should().Be(reportCode);
            result.RowCount.Should().BeGreaterThan(0, $"report {reportCode} must provide demo-visible data");
        }
    }

    private static decimal ReadDecimal(ReportRowDto row, string key)
    {
        if (!row.Values.TryGetValue(key, out var raw) || raw is null)
        {
            return 0m;
        }

        return raw switch
        {
            decimal d => d,
            double d => (decimal)d,
            float f => (decimal)f,
            int i => i,
            long l => l,
            _ when decimal.TryParse(raw.ToString(), out var parsed) => parsed,
            _ => 0m
        };
    }

    private static RuntimeServices CreateRuntime(AppDbContext db, bool allowResetWithRealDataRisk = true)
    {
        var auditLog = new AuditLogService(db);
        var auditService = new AuditService(db);
        var periodService = new AccountingPeriodService(db, auditLog);
        var postingRuleService = new PostingRuleService(db);
        var debtService = new DebtTransactionService(db, auditLog);
        var taxService = new TaxService(db, auditLog, auditService, null);
        var journalService = new JournalService(db, periodService, auditLog, debtService, taxService, auditService);
        var postingEngine = new PostingEngine(db, journalService, postingRuleService, periodService);
        var inventoryCostingService = new InventoryCostingService(db, auditLog);

        IStockReceiptRepository receiptRepository = new StockReceiptRepository(db);
        IStockIssueRepository issueRepository = new StockIssueRepository(db);
        IStockBalanceRepository balanceRepository = new StockBalanceRepository(db);
        IItemRepository itemRepository = new ItemRepository(db);
        IWarehouseRepository warehouseRepository = new WarehouseRepository(db);
        ISupplierRepository supplierRepository = new SupplierRepository(db);
        IUnitOfWork unitOfWork = new EfUnitOfWork(db);

        var inventoryAppService = new InventoryAppService(
            receiptRepository,
            issueRepository,
            balanceRepository,
            itemRepository,
            warehouseRepository,
            supplierRepository,
            unitOfWork,
            auditLog,
            postingEngine,
            periodService,
            inventoryCostingService);

        var cashFundService = new CashFundService(db, journalService, periodService, postingRuleService, auditLog);
        var bankingService = new BankingService(db, journalService, periodService, postingRuleService, auditLog);
        var accountingStandardService = new AccountingStandardService(db, auditLog);
        var cleaner = new TrainingDataCleaner(db);
        var seeder = new TrainingDataSeeder(db, accountingStandardService, inventoryAppService, journalService, cashFundService, bankingService, taxService, debtService);
        var validator = new TrainingDataValidator(db);
        var reportEngine = new ReportEngine(db, auditService);

        var trainingDataService = new TrainingDataService(
            db,
            cleaner,
            seeder,
            validator,
            new FakeCurrentUserService(),
            auditService,
            new FakeHostEnvironment(),
            Options.Create(new TrainingDataOptions
            {
                AllowDestructiveReset = true,
                AllowInProduction = false,
                AllowResetWithRealDataRisk = allowResetWithRealDataRisk,
                RequireConfirmationText = "RESET DATA"
            }));

        return new RuntimeServices(trainingDataService, reportEngine);
    }

    private sealed record RuntimeServices(ITrainingDataService TrainingDataService, IReportEngine ReportEngine);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser { get; private set; } = new() { Id = Guid.NewGuid(), UserName = "integration-tester" };
        public Guid? CurrentSessionId { get; private set; } = Guid.NewGuid();
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => true;
        public void SetCurrentUser(LoginResultDto login)
            => CurrentUser = new UserAccountDto { Id = login.UserId, UserName = login.UserName, DisplayName = login.DisplayName, Roles = login.Roles.ToList() };
        public void Clear() => CurrentUser = null;
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ASMILE.BO.Tests.Integration";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
