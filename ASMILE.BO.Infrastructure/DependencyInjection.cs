using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Repositories;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Infrastructure.Security.Store;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Infrastructure.SettingsEngine;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Events;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.ProcessManager;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.FrontOffice.ProcessManager;
using ASMILE.BO.Infrastructure.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.FrontOffice.HotelMasterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ASMILE.BO.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<TrainingDataOptions>(configuration.GetSection(TrainingDataOptions.SectionName));
        services.AddOptions<AccountingOptions>().Bind(configuration.GetSection(AccountingOptions.SectionName)).Validate(x => ValidateOptions(x), "Accounting configuration is invalid.");
        services.AddOptions<InventoryOptions>().Bind(configuration.GetSection(InventoryOptions.SectionName)).Validate(x => ValidateOptions(x), "Inventory configuration is invalid.");
        services.AddOptions<SecurityOptions>().Bind(configuration.GetSection(SecurityOptions.SectionName)).Validate(x => ValidateOptions(x), "Security configuration is invalid.");

        services.AddSingleton<InMemorySecurityStore>();
        services.AddSingleton<ISecretProtector>(_ => OperatingSystem.IsWindows()
            ? new DpapiSecretProtector()
            : new PlainTextSecretProtector());
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAuditCorrelationService, AuditCorrelationService>();
        services.AddSingleton<IPathSecurityService, PathSecurityService>();

        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ISecuritySeedService, SecuritySeedService>();

        var configuredConnectionString = configuration.GetSection(DatabaseOptions.SectionName).GetValue<string>("ConnectionString")
            ?? throw new InvalidOperationException("Database:ConnectionString is missing.");
        ISecretProtector secretProtector = OperatingSystem.IsWindows()
            ? new DpapiSecretProtector()
            : new PlainTextSecretProtector();
        var connectionString = secretProtector.IsProtected(configuredConnectionString)
            ? secretProtector.Unprotect(configuredConnectionString)
            : configuredConnectionString;

        services.AddSingleton<ICompanyContextAccessor, CompanyContextAccessor>();
        services.AddSingleton<ICurrentCompanyContext, CurrentCompanyContext>();
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));

        services.AddSingleton<IFrontOfficeExecutionContextAccessor, FrontOfficeExecutionContextAccessor>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IIntegrationEventMapper, PassthroughIntegrationEventMapper>();
        services.AddScoped<IOutboxMessageFactory, CanonicalOutboxMessageFactory>();
        services.AddScoped<IOutboxPublisher, NoOpOutboxPublisher>();
        services.AddScoped<FrontOfficeAuditWriter>();
        services.AddScoped<IFrontOfficeAuditWriter>(sp => sp.GetRequiredService<FrontOfficeAuditWriter>());
        services.AddScoped<IFrontOfficePersistenceSession, EfFrontOfficePersistenceSession>();
        services.AddScoped<IInboxStore, EfInboxStore>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<EfProcessManagerStore>();
        services.AddScoped<IProcessRepository>(sp => sp.GetRequiredService<EfProcessManagerStore>());
        services.AddScoped<IProcessMonitorQueries>(sp => sp.GetRequiredService<EfProcessManagerStore>());
        services.AddSingleton<IProcessFaultInjector, NoOpProcessFaultInjector>();
        services.AddSingleton(new ProcessWorkerOptions());
        services.AddSingleton<ProcessWorkerFailureClassifier>();
        services.AddSingleton<IProcessWorkerMetrics, NoOpProcessWorkerMetrics>();
        services.AddSingleton<IProcessTimeoutWorkExecutor, DurableProcessTimeoutWorkExecutor>();
        services.AddScoped<ProcessTimeoutDispatcher>();
        services.AddScoped<ProcessTransitionEngine>();
        services.AddScoped<ProcessSignalHandler>();
        services.AddScoped<EfReservationRepository>();
        services.AddScoped<IReservationRepository>(sp => sp.GetRequiredService<EfReservationRepository>());
        services.AddScoped<IRoomReservationConflictChecker>(sp => sp.GetRequiredService<EfReservationRepository>());
        services.AddScoped<IReservationNumberGenerator, SqlReservationNumberGenerator>();
        services.AddScoped<EfHotelRoomMasterDataRepository>();
        services.AddScoped<IHotelRoomMasterDataRepository>(sp => sp.GetRequiredService<EfHotelRoomMasterDataRepository>());
        services.AddScoped<IHotelReservationReferenceReader>(sp => sp.GetRequiredService<EfHotelRoomMasterDataRepository>());
        services.AddScoped<HotelRoomMasterDataApplicationService>();
        services.AddScoped<CreateHotelRoomTypeCommandHandler>(); services.AddScoped<UpdateHotelRoomTypeCommandHandler>(); services.AddScoped<ActivateHotelRoomTypeCommandHandler>(); services.AddScoped<DeactivateHotelRoomTypeCommandHandler>();
        services.AddScoped<CreateHotelRoomCommandHandler>(); services.AddScoped<UpdateHotelRoomCommandHandler>(); services.AddScoped<ChangeHotelRoomTypeCommandHandler>(); services.AddScoped<ChangeHotelRoomOperationalStatusCommandHandler>(); services.AddScoped<ActivateHotelRoomCommandHandler>(); services.AddScoped<DeactivateHotelRoomCommandHandler>();
        services.AddScoped<HotelRoomMasterDataQueryHandlers>();
        services.AddScoped<GetHotelRoomTypeByIdQueryHandler>(); services.AddScoped<GetHotelRoomTypeByCodeQueryHandler>(); services.AddScoped<SearchHotelRoomTypesQueryHandler>(); services.AddScoped<GetActiveHotelRoomTypesQueryHandler>();
        services.AddScoped<GetHotelRoomByIdQueryHandler>(); services.AddScoped<GetHotelRoomByNumberQueryHandler>(); services.AddScoped<SearchHotelRoomsQueryHandler>(); services.AddScoped<GetAssignableHotelRoomsQueryHandler>();
        services.AddScoped<ReservationApplicationService>();
        services.AddScoped<CreateReservationCommandHandler>(); services.AddScoped<UpdateReservationDetailsCommandHandler>(); services.AddScoped<ChangeReservationStayCommandHandler>(); services.AddScoped<ChangeReservationGuestCountCommandHandler>(); services.AddScoped<AssignReservationRoomCommandHandler>(); services.AddScoped<UnassignReservationRoomCommandHandler>(); services.AddScoped<MarkReservationTentativeCommandHandler>(); services.AddScoped<ConfirmReservationCommandHandler>(); services.AddScoped<CancelReservationCommandHandler>(); services.AddScoped<MarkReservationNoShowCommandHandler>();
        services.AddScoped<GetReservationByIdQueryHandler>(); services.AddScoped<GetReservationByNumberQueryHandler>(); services.AddScoped<SearchReservationsQueryHandler>(); services.AddScoped<CheckRoomReservationConflictQueryHandler>();
        services.AddHostedService<ProcessTimeoutHostedService>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IStockReceiptRepository, StockReceiptRepository>();
        services.AddScoped<IStockIssueRepository, StockIssueRepository>();
        services.AddScoped<IStockBalanceRepository, StockBalanceRepository>();

        services.AddScoped<IVirtualExplorerRepository, VirtualExplorerRepository>();
        services.AddScoped<IBusinessDocumentRepository, BusinessDocumentRepository>();

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditSessionService, AuditSessionService>();
        services.AddScoped<IAuditReplayService, AuditReplayService>();
        services.AddSingleton<RuntimeTelemetryService>();
        services.AddSingleton<IRuntimeTelemetryService>(sp => sp.GetRequiredService<RuntimeTelemetryService>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RuntimeTelemetryService>());
        services.AddScoped<IFailureRecoveryService, FailureRecoveryService>();
        services.AddScoped<IArchiveService, ArchiveService>();
        services.AddScoped<IRecoveryService, RecoveryService>();
        services.AddScoped<IReconciliationEngine, ReconciliationEngine>();
        services.AddScoped<IExportPipelineService, ExportPipelineService>();
        services.AddScoped<IDataIntegrityService, DataIntegrityService>();
        services.AddScoped<ICompliancePackagingService, CompliancePackagingService>();
        services.AddScoped<ITT99ComplianceService, TT99ComplianceService>();
        services.AddScoped<ITT200ComplianceService, TT200ComplianceService>();
        services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
        services.AddScoped<IWorkflowResilienceService, WorkflowResilienceService>();
        services.AddScoped<IEnterpriseConsistencyValidatorService, EnterpriseConsistencyValidatorService>();
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddScoped<DatabaseMigrationService>(sp => (DatabaseMigrationService)sp.GetRequiredService<IDatabaseMigrationService>());
        services.AddScoped<IInfrastructureHealthService, InfrastructureHealthService>();
        services.AddScoped<IBackgroundJobRunner, BackgroundJobRunner>();
        services.AddScoped<ISchedulerService, SchedulerService>();
        services.AddScoped<IBackgroundSchedulerService, BackgroundSchedulerService>();
        services.AddHostedService<BackgroundSchedulerHostedService>();
        services.AddScoped<INotificationTaskService, NotificationTaskService>();
        services.AddScoped<ISqlPerformanceScalingService, SqlPerformanceScalingService>();
        services.AddScoped<ISystemSettingsService, MockSystemSettingsService>();
        services.AddScoped<IDatabaseAdminService, MockDatabaseAdminService>();
        services.AddScoped<IBackupService, MockBackupService>();
        services.AddScoped<IServiceStatusService, MockServiceStatusService>();
        services.AddScoped<ISecuritySettingsService, MockSecuritySettingsService>();
        services.AddScoped<IPrinterSettingsService, MockPrinterSettingsService>();
        services.AddScoped<IReceivableService, EfReceivableService>();
        services.AddScoped<IPayableService, EfPayableService>();
        services.AddScoped<IInventoryMovementService, EfInventoryMovementService>();
        services.AddScoped<IGeneralLedgerService, EfGeneralLedgerService>();
        services.AddScoped<IAccountingPeriodService, AccountingPeriodService>();
        services.AddScoped<IDebtTransactionService, DebtTransactionService>();
        services.AddScoped<IInventoryCostingService, InventoryCostingService>();
        services.AddScoped<IImportEngine, ImportEngine>();
        services.AddScoped<IImportTemplateHandler, ItemImportTemplateHandler>();
        services.AddScoped<IImportTemplateHandler, BankStatementImportTemplateHandler>();
        services.AddScoped<IReportEngine, ReportEngine>();
        services.AddScoped<IAccountingStandardService, AccountingStandardService>();
        services.AddScoped<ICashFundService, CashFundService>();
        services.AddScoped<IBankingService, BankingService>();
        services.AddScoped<IFixedAssetService, FixedAssetService>();
        services.AddScoped<IToolAssetService, ToolAssetService>();
        services.AddScoped<ITaxService, TaxService>();
        services.AddScoped<IEInvoiceService, EInvoiceService>();
        services.AddScoped<IFinancialStatementMappingService, FinancialStatementMappingService>();
        services.AddScoped<IEInvoiceXmlGenerator, EInvoiceXmlGenerator>();
        services.AddScoped<IEInvoiceProvider>(_ => new StubEInvoiceProvider("VIETTEL"));
        services.AddScoped<IEInvoiceProvider>(_ => new StubEInvoiceProvider("VNPT"));
        services.AddScoped<IEInvoiceProvider>(_ => new StubEInvoiceProvider("MISA"));
        services.AddScoped<IFinancialStatementService, FinancialStatementService>();
        services.AddScoped<IClosingService, ClosingService>();
        services.AddScoped<IPostingRuleService, PostingRuleService>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<IPostingEngine, PostingEngine>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IEnterpriseSearchService, EnterpriseSearchService>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<IAppModuleRepository, AppModuleRepository>();
        services.AddSingleton<ISettingChangeLogRepository, SettingChangeLogRepository>();
        services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
        services.AddSingleton<ISettingsValidator, SettingsValidator>();
        services.AddSingleton<ISettingsAuditService, SettingsAuditService>();
        services.AddSingleton<ISettingsChangeNotifier, SettingsChangeNotifier>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAppModuleService, AppModuleService>();
        services.AddScoped<ITrainingDataService, TrainingDataService>();
        services.AddScoped<ITrainingDataCleaner, TrainingDataCleaner>();
        services.AddScoped<ITrainingDataSeeder, TrainingDataSeeder>();
        services.AddScoped<ITrainingDataValidator, TrainingDataValidator>();
        services.AddScoped<IMenuCatalogService, MenuCatalogService>();
        services.AddScoped<IMenuRecipeService, MenuRecipeService>();
        services.AddSingleton<ITableAllocationEngine, TableAllocationEngine>();
        services.AddSingleton<IMenuCalculationEngine, MenuCalculationEngine>();
        services.AddSingleton<IRoomOrderMenuPlanningEngine, RoomOrderMenuPlanningEngine>();
        services.AddScoped<IBreakfastCafeCostingService, BreakfastCafeCostingService>();
        services.AddScoped<IBreakfastCafeSalesService, BreakfastCafeSalesService>();
        services.AddScoped<IAlacarteRestaurantService, AlacarteRestaurantService>();
        services.AddScoped<IBanquetRestaurantService, BanquetRestaurantService>();
        services.AddScoped<IQuoteChangeService, QuoteChangeService>();

        return services;
    }

    private static bool ValidateOptions<TOptions>(TOptions options)
    {
        switch (options)
        {
            case AccountingOptions accounting:
                accounting.Validate();
                return true;
            case InventoryOptions inventory:
                inventory.Validate();
                return true;
            case SecurityOptions security:
                security.Validate();
                return true;
            default:
                return true;
        }
    }
}
