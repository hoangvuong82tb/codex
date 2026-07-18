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
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace ASMILE.BO.Tests.Integration;

public sealed class TrainingDataSqlServerResetTests
{
    private const string SqlServerConnectionEnv = "TRAINING_DATA_SQLSERVER_TEST_CONNECTION";
    private readonly ITestOutputHelper _output;

    public TrainingDataSqlServerResetTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ResetToBlank_NonDryRun_ClearsBusinessData_ButPreservesSystemData_OnSqlServer()
    {
        var connectionString = Environment.GetEnvironmentVariable(SqlServerConnectionEnv);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _output.WriteLine($"SKIP: missing env var {SqlServerConnectionEnv}.");
            return;
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        var systemBefore = await SnapshotSystemCountsAsync(db);
        var runtime = CreateRuntime(db);
        var training = runtime.TrainingDataService;

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
        full.Success.Should().BeTrue();

        await AssertBusinessDataExistsAsync(db);

        var reset = await training.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetToBlank,
            ResetScope = TrainingDataResetScope.BusinessAndOperationalLogs,
            IsDryRun = false,
            ConfirmationText = "RESET ALL BUSINESS DATA"
        });
        reset.Success.Should().BeTrue();
        reset.Steps.Should().NotBeEmpty();
        reset.Steps.Where(x => x.StepCode.StartsWith("DEL_", StringComparison.OrdinalIgnoreCase)).Should().OnlyContain(x =>
            x.BeforeCount >= 0
            && x.DeletedCount >= 0
            && x.AfterCount >= 0
            && !string.IsNullOrWhiteSpace(x.EntityName)
            && !string.IsNullOrWhiteSpace(x.Status));

        await AssertBusinessDataClearedAsync(db);

        var systemAfter = await SnapshotSystemCountsAsync(db);
        systemAfter.Users.Should().BeGreaterThanOrEqualTo(systemBefore.Users);
        systemAfter.Roles.Should().BeGreaterThanOrEqualTo(systemBefore.Roles);
        systemAfter.Permissions.Should().BeGreaterThanOrEqualTo(systemBefore.Permissions);
        systemAfter.AccountingStandards.Should().BeGreaterThanOrEqualTo(systemBefore.AccountingStandards);
        systemAfter.AccountingTemplates.Should().BeGreaterThanOrEqualTo(systemBefore.AccountingTemplates);
        systemAfter.AppSettings.Should().BeGreaterThanOrEqualTo(systemBefore.AppSettings);
        systemAfter.Modules.Should().BeGreaterThanOrEqualTo(systemBefore.Modules);

        systemAfter.Users.Should().BeGreaterThan(0);
        systemAfter.Roles.Should().BeGreaterThan(0);
        systemAfter.Permissions.Should().BeGreaterThan(0);
        systemAfter.AccountingStandards.Should().BeGreaterThan(0);
        systemAfter.AppSettings.Should().BeGreaterThan(0);
    }

    private static async Task AssertBusinessDataExistsAsync(AppDbContext db)
    {
        (await db.JournalHeaders.CountAsync()).Should().BeGreaterThan(0);
        (await db.ReceivableTransactions.CountAsync()).Should().BeGreaterThan(0);
        (await db.PayableTransactions.CountAsync()).Should().BeGreaterThan(0);
        (await db.StockReceipts.CountAsync()).Should().BeGreaterThan(0);
        (await db.StockIssues.CountAsync()).Should().BeGreaterThan(0);
        (await db.InventoryCostTransactions.CountAsync()).Should().BeGreaterThan(0);
        (await db.VatTransactions.CountAsync()).Should().BeGreaterThan(0);
        (await db.CashTransactions.CountAsync()).Should().BeGreaterThan(0);
        (await db.BankTransactions.CountAsync()).Should().BeGreaterThan(0);
    }

    private static async Task AssertBusinessDataClearedAsync(AppDbContext db)
    {
        (await db.JournalHeaders.CountAsync()).Should().Be(0);
        (await db.JournalLines.CountAsync()).Should().Be(0);
        (await db.ReceivableTransactions.CountAsync()).Should().Be(0);
        (await db.PayableTransactions.CountAsync()).Should().Be(0);
        (await db.StockReceipts.CountAsync()).Should().Be(0);
        (await db.StockIssues.CountAsync()).Should().Be(0);
        (await db.InventoryCostTransactions.CountAsync()).Should().Be(0);
        (await db.VatTransactions.CountAsync()).Should().Be(0);
        (await db.CashTransactions.CountAsync()).Should().Be(0);
        (await db.BankTransactions.CountAsync()).Should().Be(0);
        (await db.Customers.CountAsync()).Should().Be(0);
        (await db.Suppliers.CountAsync()).Should().Be(0);
        (await db.Items.CountAsync()).Should().Be(0);
        (await db.Warehouses.CountAsync()).Should().Be(0);
    }

    private static async Task<SystemSnapshot> SnapshotSystemCountsAsync(AppDbContext db)
    {
        return new SystemSnapshot(
            await db.AppUsers.CountAsync(),
            await db.AppRoles.CountAsync(),
            await db.AppPermissions.CountAsync(),
            await db.AccountingStandards.CountAsync(),
            await db.AccountingStandardTemplates.CountAsync(),
            await db.AppSettings.CountAsync(),
            await db.AppModules.CountAsync());
    }

    private static RuntimeServices CreateRuntime(AppDbContext db)
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
                AllowResetWithRealDataRisk = true,
                RequireConfirmationText = "RESET DATA"
            }));

        return new RuntimeServices(trainingDataService);
    }

    private sealed record RuntimeServices(ITrainingDataService TrainingDataService);
    private sealed record SystemSnapshot(int Users, int Roles, int Permissions, int AccountingStandards, int AccountingTemplates, int AppSettings, int Modules);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser { get; private set; } = new() { Id = Guid.NewGuid(), UserName = "sqlserver-test" };
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
        public string ApplicationName { get; set; } = "ASMILE.BO.Tests.Integration.SqlServer";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
