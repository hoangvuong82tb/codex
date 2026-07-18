using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ASMILE.BO.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ICurrentCompanyContext? _companyContext;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentCompanyContext companyContext) : base(options)
    {
        _companyContext = companyContext;
    }

    public Guid? CurrentCompanyId
    {
        get
        {
            var cid = _companyContext?.CompanyId;
            return cid.HasValue && cid.Value != Guid.Empty ? cid : CurrentCompanyContext.DefaultCompanyId;
        }
    }
    public Guid? CurrentBranchId => _companyContext?.BranchId;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyCompanyScope();
        ApplyProviderSafeRowVersions();
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
                ChangeTracker.Clear();
                throw new AppConcurrencyException("Dữ liệu đã được người dùng khác thay đổi. Vui lòng tải lại trước khi lưu.", ex);
        }
        catch
        {
                ChangeTracker.Clear();
                throw;
        }
    }

    private void ApplyCompanyScope()
    {
        var companyId = CurrentCompanyId;
        var branchId = CurrentBranchId;
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity is ICompanyScoped companyScoped && companyScoped.CompanyId is null)
            {
                companyScoped.CompanyId = companyId;
            }

            if (branchId.HasValue && entry.Entity is IBranchScoped branchScoped && branchScoped.BranchId is null)
            {
                branchScoped.BranchId = branchId;
            }
        }
    }

    private void ApplyProviderSafeRowVersions()
    {
        if (Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            var property = entry.Metadata.FindProperty("RowVersion");
            if (property is null)
            {
                continue;
            }

            entry.Property("RowVersion").CurrentValue = Guid.NewGuid().ToByteArray();
        }
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<BranchSettings> BranchSettings => Set<BranchSettings>();
    public DbSet<CompanyFiscalPeriod> CompanyFiscalPeriods => Set<CompanyFiscalPeriod>();
    public DbSet<CompanyFeatureFlag> CompanyFeatureFlags => Set<CompanyFeatureFlag>();
    public DbSet<UserCompanyAccess> UserCompanyAccesses => Set<UserCompanyAccess>();
    public DbSet<UserBranchAccess> UserBranchAccesses => Set<UserBranchAccess>();
    public DbSet<ActiveCompanySession> ActiveCompanySessions => Set<ActiveCompanySession>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockReceiptDetail> StockReceiptDetails => Set<StockReceiptDetail>();
    public DbSet<StockIssue> StockIssues => Set<StockIssue>();
    public DbSet<StockIssueDetail> StockIssueDetails => Set<StockIssueDetail>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditSession> AuditSessions => Set<AuditSession>();
    public DbSet<AuditCorrelation> AuditCorrelations => Set<AuditCorrelation>();
    public DbSet<AuditEntitySnapshot> AuditEntitySnapshots => Set<AuditEntitySnapshot>();
    public DbSet<AuditReplayEntry> AuditReplayEntries => Set<AuditReplayEntry>();
    public DbSet<AuditExportHistory> AuditExportHistory => Set<AuditExportHistory>();
    public DbSet<RuntimeMetricSnapshot> RuntimeMetricSnapshots => Set<RuntimeMetricSnapshot>();
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();
    public DbSet<SlowOperationLog> SlowOperationLogs => Set<SlowOperationLog>();
    public DbSet<RuntimeExceptionLog> RuntimeExceptionLogs => Set<RuntimeExceptionLog>();
    public DbSet<UiResponsivenessSnapshot> UiResponsivenessSnapshots => Set<UiResponsivenessSnapshot>();
    public DbSet<RecoveryRecommendation> RecoveryRecommendations => Set<RecoveryRecommendation>();
    public DbSet<SystemResourceSnapshot> SystemResourceSnapshots => Set<SystemResourceSnapshot>();
    public DbSet<TT99AccountMapping> TT99AccountMappings => Set<TT99AccountMapping>();
    public DbSet<TT99ReportTemplate> TT99ReportTemplates => Set<TT99ReportTemplate>();
    public DbSet<TT99ReportLine> TT99ReportLines => Set<TT99ReportLine>();
    public DbSet<TT99ValidationRule> TT99ValidationRules => Set<TT99ValidationRule>();
    public DbSet<TT99ComplianceIssue> TT99ComplianceIssues => Set<TT99ComplianceIssue>();
    public DbSet<TT99DeclarationPeriod> TT99DeclarationPeriods => Set<TT99DeclarationPeriod>();
    public DbSet<TT99ComplianceSnapshot> TT99ComplianceSnapshots => Set<TT99ComplianceSnapshot>();
    public DbSet<TT99ExportManifest> TT99ExportManifests => Set<TT99ExportManifest>();
    public DbSet<TT200AccountMapping> TT200AccountMappings => Set<TT200AccountMapping>();
    public DbSet<TT200ReportTemplate> TT200ReportTemplates => Set<TT200ReportTemplate>();
    public DbSet<TT200ReportLine> TT200ReportLines => Set<TT200ReportLine>();
    public DbSet<TT200ValidationRule> TT200ValidationRules => Set<TT200ValidationRule>();
    public DbSet<TT200ComplianceIssue> TT200ComplianceIssues => Set<TT200ComplianceIssue>();
    public DbSet<TT200DeclarationPeriod> TT200DeclarationPeriods => Set<TT200DeclarationPeriod>();
    public DbSet<TT200ComplianceSnapshot> TT200ComplianceSnapshots => Set<TT200ComplianceSnapshot>();
    public DbSet<TT200ExportManifest> TT200ExportManifests => Set<TT200ExportManifest>();
    public DbSet<DataRetentionPolicy> DataRetentionPolicies => Set<DataRetentionPolicy>();
    public DbSet<ArchiveBatch> ArchiveBatches => Set<ArchiveBatch>();
    public DbSet<ArchiveBatchItem> ArchiveBatchItems => Set<ArchiveBatchItem>();
    public DbSet<RecoveryCheckpoint> RecoveryCheckpoints => Set<RecoveryCheckpoint>();
    public DbSet<RecoveryOperation> RecoveryOperations => Set<RecoveryOperation>();
    public DbSet<ArchiveExportManifest> ArchiveExportManifests => Set<ArchiveExportManifest>();
    public DbSet<WorkflowExecution> WorkflowExecutions => Set<WorkflowExecution>();
    public DbSet<WorkflowExecutionStep> WorkflowExecutionSteps => Set<WorkflowExecutionStep>();
    public DbSet<WorkflowCompensationAction> WorkflowCompensationActions => Set<WorkflowCompensationAction>();
    public DbSet<WorkflowRetryHistory> WorkflowRetryHistories => Set<WorkflowRetryHistory>();
    public DbSet<WorkflowConsistencyViolation> WorkflowConsistencyViolations => Set<WorkflowConsistencyViolation>();
    public DbSet<DistributedOperationMarker> DistributedOperationMarkers => Set<DistributedOperationMarker>();
    public DbSet<IdempotencyKeyEntry> IdempotencyKeyEntries => Set<IdempotencyKeyEntry>();
    public DbSet<WorkflowLease> WorkflowLeases => Set<WorkflowLease>();
    public DbSet<WorkflowDistributedLock> WorkflowDistributedLocks => Set<WorkflowDistributedLock>();
    public DbSet<WorkflowDeadLetter> WorkflowDeadLetters => Set<WorkflowDeadLetter>();
    public DbSet<WorkflowRecoveryCheckpoint> WorkflowRecoveryCheckpoints => Set<WorkflowRecoveryCheckpoint>();
    public DbSet<WorkflowExecutionMetric> WorkflowExecutionMetrics => Set<WorkflowExecutionMetric>();
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ApprovalAssignment> ApprovalAssignments => Set<ApprovalAssignment>();
    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();
    public DbSet<ApprovalEscalation> ApprovalEscalations => Set<ApprovalEscalation>();
    public DbSet<ApprovalHistory> ApprovalHistory => Set<ApprovalHistory>();
    public DbSet<ApprovalSnapshot> ApprovalSnapshots => Set<ApprovalSnapshot>();
    public DbSet<ReconciliationSession> ReconciliationSessions => Set<ReconciliationSession>();
    public DbSet<ReconciliationMatch> ReconciliationMatches => Set<ReconciliationMatch>();
    public DbSet<ReconciliationRule> ReconciliationRules => Set<ReconciliationRule>();
    public DbSet<ReconciliationSnapshot> ReconciliationSnapshots => Set<ReconciliationSnapshot>();
    public DbSet<ReconciliationAnomaly> ReconciliationAnomalies => Set<ReconciliationAnomaly>();
    public DbSet<ReportSnapshot> ReportSnapshots => Set<ReportSnapshot>();
    public DbSet<ExportExecution> ExportExecutions => Set<ExportExecution>();
    public DbSet<ExportRetentionPolicy> ExportRetentionPolicies => Set<ExportRetentionPolicy>();
    public DbSet<ExportArtifact> ExportArtifacts => Set<ExportArtifact>();
    public DbSet<DataIntegrityCheckpoint> DataIntegrityCheckpoints => Set<DataIntegrityCheckpoint>();
    public DbSet<DataIntegrityScanResult> DataIntegrityScanResults => Set<DataIntegrityScanResult>();
    public DbSet<DataRepairOperation> DataRepairOperations => Set<DataRepairOperation>();
    public DbSet<CompliancePackage> CompliancePackages => Set<CompliancePackage>();
    public DbSet<ComplianceManifest> ComplianceManifests => Set<ComplianceManifest>();
    public DbSet<ComplianceSubmission> ComplianceSubmissions => Set<ComplianceSubmission>();
    public DbSet<ComplianceArchive> ComplianceArchives => Set<ComplianceArchive>();
    public DbSet<ProductionCertificationReport> ProductionCertificationReports => Set<ProductionCertificationReport>();
    public DbSet<EnterpriseConsistencyViolation> EnterpriseConsistencyViolations => Set<EnterpriseConsistencyViolation>();
    public DbSet<VirtualDrive> VirtualDrives => Set<VirtualDrive>();
    public DbSet<VirtualFolder> VirtualFolders => Set<VirtualFolder>();
    public DbSet<BusinessDocument> BusinessDocuments => Set<BusinessDocument>();
    public DbSet<DocumentLink> DocumentLinks => Set<DocumentLink>();
    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
    public DbSet<SettingDefinition> SettingDefinitions => Set<SettingDefinition>();
    public DbSet<SettingValue> SettingValues => Set<SettingValue>();
    public DbSet<AppModule> AppModules => Set<AppModule>();
    public DbSet<SettingChangeLog> SettingChangeLogs => Set<SettingChangeLog>();
    public DbSet<AccountingStandard> AccountingStandards => Set<AccountingStandard>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<PeriodLock> PeriodLocks => Set<PeriodLock>();
    public DbSet<CompanyBranchPermission> CompanyBranchPermissions => Set<CompanyBranchPermission>();
    public DbSet<JournalHeader> JournalHeaders => Set<JournalHeader>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<PostingBatch> PostingBatches => Set<PostingBatch>();
    public DbSet<PostingSource> PostingSources => Set<PostingSource>();
    public DbSet<PostingRule> PostingRules => Set<PostingRule>();
    public DbSet<PostingRuleLine> PostingRuleLines => Set<PostingRuleLine>();
    public DbSet<AccountRoleMapping> AccountRoleMappings => Set<AccountRoleMapping>();
    public DbSet<AccountTagMapping> AccountTagMappings => Set<AccountTagMapping>();
    public DbSet<ReceivableTransaction> ReceivableTransactions => Set<ReceivableTransaction>();
    public DbSet<PayableTransaction> PayableTransactions => Set<PayableTransaction>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<DebtReconciliation> DebtReconciliations => Set<DebtReconciliation>();
    public DbSet<DebtReconciliationLine> DebtReconciliationLines => Set<DebtReconciliationLine>();
    public DbSet<AgingSnapshot> AgingSnapshots => Set<AgingSnapshot>();
    public DbSet<ImportTemplate> ImportTemplates => Set<ImportTemplate>();
    public DbSet<ImportSession> ImportSessions => Set<ImportSession>();
    public DbSet<ImportRow> ImportRows => Set<ImportRow>();
    public DbSet<ImportCellError> ImportCellErrors => Set<ImportCellError>();
    public DbSet<InventoryCostLayer> InventoryCostLayers => Set<InventoryCostLayer>();
    public DbSet<InventoryCostTransaction> InventoryCostTransactions => Set<InventoryCostTransaction>();
    public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportParameter> ReportParameters => Set<ReportParameter>();
    public DbSet<ReportExecution> ReportExecutions => Set<ReportExecution>();
    public DbSet<AccountingStandardTemplate> AccountingStandardTemplates => Set<AccountingStandardTemplate>();
    public DbSet<AccountTemplate> AccountTemplates => Set<AccountTemplate>();
    public DbSet<FinancialStatementMapping> FinancialStatementMappings => Set<FinancialStatementMapping>();
    public DbSet<CompanyAccountingProfile> CompanyAccountingProfiles => Set<CompanyAccountingProfile>();
    public DbSet<CashFund> CashFunds => Set<CashFund>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<CashCount> CashCounts => Set<CashCount>();
    public DbSet<CashCountLine> CashCountLines => Set<CashCountLine>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();
    public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();
    public DbSet<BankReconciliationLine> BankReconciliationLines => Set<BankReconciliationLine>();
    public DbSet<FixedAssetCategory> FixedAssetCategories => Set<FixedAssetCategory>();
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<FixedAssetDepreciation> FixedAssetDepreciations => Set<FixedAssetDepreciation>();
    public DbSet<FixedAssetAdjustment> FixedAssetAdjustments => Set<FixedAssetAdjustment>();
    public DbSet<FixedAssetDisposal> FixedAssetDisposals => Set<FixedAssetDisposal>();
    public DbSet<ToolAssetCategory> ToolAssetCategories => Set<ToolAssetCategory>();
    public DbSet<ToolAsset> ToolAssets => Set<ToolAsset>();
    public DbSet<ToolIssue> ToolIssues => Set<ToolIssue>();
    public DbSet<ToolAllocationSchedule> ToolAllocationSchedules => Set<ToolAllocationSchedule>();
    public DbSet<ToolAllocationRun> ToolAllocationRuns => Set<ToolAllocationRun>();
    public DbSet<ToolRecovery> ToolRecoveries => Set<ToolRecovery>();
    public DbSet<ToolDisposal> ToolDisposals => Set<ToolDisposal>();
    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<VatTransaction> VatTransactions => Set<VatTransaction>();
    public DbSet<TaxDeclarationDraft> TaxDeclarationDrafts => Set<TaxDeclarationDraft>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<InvoiceTaxBreakdown> InvoiceTaxBreakdowns => Set<InvoiceTaxBreakdown>();
    public DbSet<InvoiceIssueLog> InvoiceIssueLogs => Set<InvoiceIssueLog>();
    public DbSet<InvoiceProviderConfiguration> InvoiceProviderConfigurations => Set<InvoiceProviderConfiguration>();
    public DbSet<AccountBalanceSnapshot> AccountBalanceSnapshots => Set<AccountBalanceSnapshot>();
    public DbSet<FinancialStatementExecution> FinancialStatementExecutions => Set<FinancialStatementExecution>();
    public DbSet<FinancialTemplate> FinancialTemplates => Set<FinancialTemplate>();
    public DbSet<FinancialTemplateVersion> FinancialTemplateVersions => Set<FinancialTemplateVersion>();
    public DbSet<FinancialStatementFormula> FinancialStatementFormulas => Set<FinancialStatementFormula>();
    public DbSet<FinancialStatementSnapshot> FinancialStatementSnapshots => Set<FinancialStatementSnapshot>();
    public DbSet<ClosingEntry> ClosingEntries => Set<ClosingEntry>();
    public DbSet<ClosingEntryLine> ClosingEntryLines => Set<ClosingEntryLine>();
    public DbSet<AppPermission> AppPermissions => Set<AppPermission>();
    public DbSet<AppUserPermission> AppUserPermissions => Set<AppUserPermission>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<AppRolePermission> AppRolePermissions => Set<AppRolePermission>();
    public DbSet<AppRoleInheritance> AppRoleInheritances => Set<AppRoleInheritance>();
    public DbSet<AppDataPermission> AppDataPermissions => Set<AppDataPermission>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();
    public DbSet<UserWorkspacePreference> UserWorkspacePreferences => Set<UserWorkspacePreference>();
    public DbSet<BackgroundJobDefinition> BackgroundJobDefinitions => Set<BackgroundJobDefinition>();
    public DbSet<BackgroundJobSchedule> BackgroundJobSchedules => Set<BackgroundJobSchedule>();
    public DbSet<BackgroundJobExecution> BackgroundJobExecutions => Set<BackgroundJobExecution>();
    public DbSet<BackgroundJobLog> BackgroundJobLogs => Set<BackgroundJobLog>();
    public DbSet<JobDefinition> JobDefinitions => Set<JobDefinition>();
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
    public DbSet<JobRetry> JobRetries => Set<JobRetry>();
    public DbSet<JobLog> JobLogs => Set<JobLog>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<TaskInbox> TaskInboxItems => Set<TaskInbox>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskAttachmentMetadata> TaskAttachmentMetadata => Set<TaskAttachmentMetadata>();
    public DbSet<SearchIndexEntry> SearchIndexEntries => Set<SearchIndexEntry>();
    public DbSet<RecentSearch> RecentSearches => Set<RecentSearch>();
    public DbSet<TrainingDataRun> TrainingDataRuns => Set<TrainingDataRun>();
    public DbSet<TrainingDataRunStep> TrainingDataRunSteps => Set<TrainingDataRunStep>();
    public DbSet<TrainingDataMarker> TrainingDataMarkers => Set<TrainingDataMarker>();
    public DbSet<BreakfastCafeOrder> BreakfastCafeOrders => Set<BreakfastCafeOrder>();
    public DbSet<BreakfastCafeOrderLine> BreakfastCafeOrderLines => Set<BreakfastCafeOrderLine>();
    public DbSet<BreakfastCafePayment> BreakfastCafePayments => Set<BreakfastCafePayment>();
    public DbSet<PosTable> PosTables => Set<PosTable>();
    public DbSet<BreakfastCafeKitchenTicket> BreakfastCafeKitchenTickets => Set<BreakfastCafeKitchenTicket>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemRecipe> MenuItemRecipes => Set<MenuItemRecipe>();
    public DbSet<MenuItemRecipeLine> MenuItemRecipeLines => Set<MenuItemRecipeLine>();
    public DbSet<RecipeIngredientSubstitute> RecipeIngredientSubstitutes => Set<RecipeIngredientSubstitute>();
    public DbSet<DiningRoom> DiningRooms => Set<DiningRoom>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<TableReservation> TableReservations => Set<TableReservation>();
    public DbSet<AlacarteOrder> AlacarteOrders => Set<AlacarteOrder>();
    public DbSet<AlacarteOrderLine> AlacarteOrderLines => Set<AlacarteOrderLine>();
    public DbSet<RestaurantOrderTable> RestaurantOrderTables => Set<RestaurantOrderTable>();
    public DbSet<RestaurantServingGroup> RestaurantServingGroups => Set<RestaurantServingGroup>();
    public DbSet<RestaurantServingGroupTable> RestaurantServingGroupTables => Set<RestaurantServingGroupTable>();
    public DbSet<RestaurantExtraService> RestaurantExtraServices => Set<RestaurantExtraService>();
    public DbSet<RestaurantOrderExtraService> RestaurantOrderExtraServices => Set<RestaurantOrderExtraService>();
    public DbSet<RestaurantContract> RestaurantContracts => Set<RestaurantContract>();
    public DbSet<RestaurantDeposit> RestaurantDeposits => Set<RestaurantDeposit>();
    public DbSet<RestaurantQuote> RestaurantQuotes => Set<RestaurantQuote>();
    public DbSet<RestaurantQuoteLine> RestaurantQuoteLines => Set<RestaurantQuoteLine>();
    public DbSet<RestaurantQuoteTable> RestaurantQuoteTables => Set<RestaurantQuoteTable>();
    public DbSet<RestaurantPreparationTicket> RestaurantPreparationTickets => Set<RestaurantPreparationTicket>();
    public DbSet<RestaurantPreparationTicketLine> RestaurantPreparationTicketLines => Set<RestaurantPreparationTicketLine>();
    public DbSet<BanquetRoom> BanquetRooms => Set<BanquetRoom>();
    public DbSet<BanquetEvent> BanquetEvents => Set<BanquetEvent>();
    public DbSet<BanquetMenuPackage> BanquetMenuPackages => Set<BanquetMenuPackage>();
    public DbSet<BanquetMenuGroup> BanquetMenuGroups => Set<BanquetMenuGroup>();
    public DbSet<BanquetMenuPackageLine> BanquetMenuPackageLines => Set<BanquetMenuPackageLine>();
    public DbSet<BanquetEventMenu> BanquetEventMenus => Set<BanquetEventMenu>();
    public DbSet<BanquetServiceLine> BanquetServiceLines => Set<BanquetServiceLine>();
    public DbSet<BanquetContract> BanquetContracts => Set<BanquetContract>();
    public DbSet<BanquetDeposit> BanquetDeposits => Set<BanquetDeposit>();
    public DbSet<BanquetChecklist> BanquetChecklists => Set<BanquetChecklist>();
    public DbSet<BanquetFloorPlan> BanquetFloorPlans => Set<BanquetFloorPlan>();
    public DbSet<BanquetFloorPlanTable> BanquetFloorPlanTables => Set<BanquetFloorPlanTable>();
    public DbSet<BanquetTimeline> BanquetTimelines => Set<BanquetTimeline>();
    public DbSet<BanquetQuantificationBatch> BanquetQuantificationBatches => Set<BanquetQuantificationBatch>();
    public DbSet<BanquetQuantificationLine> BanquetQuantificationLines => Set<BanquetQuantificationLine>();
    public DbSet<BanquetPurchaseRequest> BanquetPurchaseRequests => Set<BanquetPurchaseRequest>();
    public DbSet<BanquetPurchaseRequestLine> BanquetPurchaseRequestLines => Set<BanquetPurchaseRequestLine>();
    public DbSet<BanquetModuleSetting> BanquetModuleSettings => Set<BanquetModuleSetting>();
    public DbSet<QuoteChangeRequest> QuoteChangeRequests => Set<QuoteChangeRequest>();
    public DbSet<QuoteChangeRequestDetail> QuoteChangeRequestDetails => Set<QuoteChangeRequestDetail>();
    public DbSet<QuoteAuditLog> QuoteAuditLogs => Set<QuoteAuditLog>();
    public DbSet<FrontOfficeOutboxMessage> FrontOfficeOutboxMessages => Set<FrontOfficeOutboxMessage>();
    public DbSet<FrontOfficeInboxMessage> FrontOfficeInboxMessages => Set<FrontOfficeInboxMessage>();
    public DbSet<FrontOfficeIdempotencyRecord> FrontOfficeIdempotencyRecords => Set<FrontOfficeIdempotencyRecord>();
    public DbSet<ProcessInstance> FrontOfficeProcesses => Set<ProcessInstance>();
    public DbSet<ProcessStep> FrontOfficeProcessSteps => Set<ProcessStep>();
    public DbSet<ProcessStepAttempt> FrontOfficeProcessStepAttempts => Set<ProcessStepAttempt>();
    public DbSet<ProcessSignal> FrontOfficeProcessSignals => Set<ProcessSignal>();
    public DbSet<ProcessTimeout> FrontOfficeProcessTimeouts => Set<ProcessTimeout>();
    public DbSet<ProcessDeadLetter> FrontOfficeProcessDeadLetters => Set<ProcessDeadLetter>();
    public DbSet<ProcessCompensation> FrontOfficeProcessCompensations => Set<ProcessCompensation>();
    public DbSet<ProcessManualAction> FrontOfficeProcessManualActions => Set<ProcessManualAction>();
    public DbSet<ProcessHistory> FrontOfficeProcessHistory => Set<ProcessHistory>();
    public DbSet<Reservation> FrontOfficeReservations => Set<Reservation>();
    public DbSet<ReservationHistory> FrontOfficeReservationHistory => Set<ReservationHistory>();
    public DbSet<HotelRoomType> HotelRoomTypes => Set<HotelRoomType>();
    public DbSet<HotelRoom> HotelRooms => Set<HotelRoom>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var useSqlServerRowVersion = Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
        if (useSqlServerRowVersion) modelBuilder.HasSequence<long>("TrinhTuSoDatPhong", "fo").StartsAt(1).IncrementsBy(1);

        void ConfigureRowVersion(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<byte[]?> property)
        {
            if (useSqlServerRowVersion)
            {
                property.IsRowVersion();
                return;
            }

            property.IsConcurrencyToken(false).ValueGeneratedNever();
        }

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.IsArchived);
            entity.Property(x => x.Code).HasMaxLength(40);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.TaxCode).HasMaxLength(50);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.DefaultCurrencyCode).HasMaxLength(12);
            entity.Property(x => x.AccountingStandardProfile).HasMaxLength(80);
            entity.Property(x => x.TimeZoneId).HasMaxLength(80);
            entity.Property(x => x.Locale).HasMaxLength(20);
            entity.Property(x => x.FiscalConfigJson);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasData(new Company
            {
                Id = CurrentCompanyContext.DefaultCompanyId,
                Code = "DEFAULT",
                Name = "Default Company",
                IsDefault = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc),
                ModifiedAtUtc = new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasIndex(x => x.ParentBranchId);
            entity.HasIndex(x => x.IsArchived);
            entity.Property(x => x.Code).HasMaxLength(40);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.HasOne(x => x.Company).WithMany(x => x.Branches).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ParentBranch).WithMany(x => x.ChildBranches).HasForeignKey(x => x.ParentBranchId).OnDelete(DeleteBehavior.Restrict);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasData(new Branch
            {
                Id = CurrentCompanyContext.DefaultBranchId,
                CompanyId = CurrentCompanyContext.DefaultCompanyId,
                Code = "MAIN",
                Name = "Main Branch",
                IsDefault = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc),
                ModifiedAtUtc = new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        modelBuilder.Entity<CompanySettings>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.SettingKey }).IsUnique();
            entity.Property(x => x.SettingKey).HasMaxLength(120);
            entity.Property(x => x.ValueJson);
            entity.Property(x => x.MetadataJson);
            entity.HasIndex(x => x.IsArchived);
            entity.HasOne<Company>().WithMany(x => x.Settings).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });

        modelBuilder.Entity<BranchSettings>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.SettingKey }).IsUnique();
            entity.Property(x => x.SettingKey).HasMaxLength(120);
            entity.Property(x => x.ValueJson);
            entity.Property(x => x.MetadataJson);
            entity.HasIndex(x => x.IsArchived);
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Branch>().WithMany(x => x.Settings).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });

        modelBuilder.Entity<CompanyFiscalPeriod>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.FiscalYearCode, x.PeriodNo }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.PeriodCode });
            entity.HasIndex(x => x.IsArchived);
            entity.Property(x => x.FiscalYearCode).HasMaxLength(40);
            entity.Property(x => x.PeriodCode).HasMaxLength(40);
            entity.HasOne<Company>().WithMany(x => x.FiscalPeriods).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });

        modelBuilder.Entity<CompanyFeatureFlag>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.FeatureKey }).IsUnique();
            entity.HasIndex(x => x.IsArchived);
            entity.Property(x => x.FeatureKey).HasMaxLength(120);
            entity.Property(x => x.ValueJson);
            entity.Property(x => x.MetadataJson);
            entity.HasOne<Company>().WithMany(x => x.FeatureFlags).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });

        modelBuilder.Entity<UserCompanyAccess>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.CompanyId }).IsUnique();
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.IsArchived);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });

        modelBuilder.Entity<UserBranchAccess>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.BranchId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.BranchId });
            entity.HasIndex(x => x.IsArchived);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });

        modelBuilder.Entity<ActiveCompanySession>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.SessionId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.IsActive });
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasIndex(x => x.IsArchived);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Source).HasMaxLength(120);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.AppVersion).HasMaxLength(40);
            entity.Property(x => x.SessionMetadataJson);
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.NoAction);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.CreatedAt });
            entity.HasIndex(x => x.NotificationType);
            entity.Property(x => x.NotificationType).HasMaxLength(80);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.Title).HasMaxLength(255);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.SourceType).HasMaxLength(80);
            entity.Property(x => x.TargetRoute).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
        });

        modelBuilder.Entity<TaskInbox>(entity =>
        {
            entity.HasIndex(x => new { x.AssignedToUserId, x.Status, x.UpdatedAt });
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status });
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            entity.Property(x => x.Title).HasMaxLength(255);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Priority).HasMaxLength(30);
            entity.Property(x => x.SourceType).HasMaxLength(80);
            entity.Property(x => x.TargetRoute).HasMaxLength(120);
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.HasIndex(x => x.TaskInboxId);
            entity.Property(x => x.CommentText).HasMaxLength(4000);
            entity.Property(x => x.MentionUserIdsJson);
            entity.HasOne(x => x.Task).WithMany(x => x.Comments).HasForeignKey(x => x.TaskInboxId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskAttachmentMetadata>(entity =>
        {
            entity.HasIndex(x => x.TaskInboxId);
            entity.Property(x => x.FileName).HasMaxLength(260);
            entity.Property(x => x.ContentType).HasMaxLength(120);
            entity.Property(x => x.StorageProvider).HasMaxLength(40);
            entity.Property(x => x.StorageKey).HasMaxLength(500);
            entity.HasOne(x => x.Task).WithMany(x => x.Attachments).HasForeignKey(x => x.TaskInboxId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SearchIndexEntry>(entity =>
        {
            entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CompanyId, x.BranchId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Category });
            entity.HasIndex(x => x.TargetRoute);
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.IsArchived);
            entity.Property(x => x.EntityType).HasMaxLength(80);
            entity.Property(x => x.EntityId).HasMaxLength(120);
            entity.Property(x => x.Title).HasMaxLength(255);
            entity.Property(x => x.Subtitle).HasMaxLength(500);
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.TargetType).HasMaxLength(40);
            entity.Property(x => x.TargetRoute).HasMaxLength(120);
            entity.Property(x => x.SourceType).HasMaxLength(80);
            entity.Property(x => x.SearchText).HasMaxLength(2000);
        });

        modelBuilder.Entity<RecentSearch>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.CompanyId, x.BranchId, x.SearchedAt });
            entity.HasIndex(x => x.IsArchived);
            entity.Property(x => x.Query).HasMaxLength(255);
            entity.Property(x => x.SelectedEntityType).HasMaxLength(80);
            entity.Property(x => x.SelectedEntityId).HasMaxLength(120);
        });

        modelBuilder.Entity<TrainingDataRun>(entity =>
        {
            entity.HasIndex(x => x.StartedAtUtc);
            entity.HasIndex(x => new { x.OperationType, x.StartedAtUtc });
            entity.Property(x => x.OperationType).HasMaxLength(60);
            entity.Property(x => x.SeedProfile).HasMaxLength(60);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.SummaryJson);
            entity.HasMany(x => x.Steps).WithOne(x => x.TrainingDataRun).HasForeignKey(x => x.TrainingDataRunId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrainingDataRunStep>(entity =>
        {
            entity.HasIndex(x => new { x.TrainingDataRunId, x.StepOrder }).IsUnique();
            entity.Property(x => x.StepCode).HasMaxLength(80);
            entity.Property(x => x.StepName).HasMaxLength(255);
            entity.Property(x => x.EntityName).HasMaxLength(160);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
        });

        modelBuilder.Entity<TrainingDataMarker>(entity =>
        {
            entity.HasIndex(x => new { x.TrainingDataRunId, x.EntityName, x.RecordId });
            entity.HasIndex(x => new { x.Profile, x.EntityName });
            entity.Property(x => x.Profile).HasMaxLength(60);
            entity.Property(x => x.EntityName).HasMaxLength(160);
            entity.Property(x => x.RecordId).HasMaxLength(128);
        });

        modelBuilder.Entity<MenuCategory>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.ParentCategoryId);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.HasOne<MenuCategory>().WithMany().HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.MenuCategoryId, x.SortOrder });
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.UnitName).HasMaxLength(30);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.VatRate).HasPrecision(9, 4);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.ImagePath).HasMaxLength(500);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.HasOne(x => x.MenuCategory).WithMany(x => x.MenuItems).HasForeignKey(x => x.MenuCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MenuItemRecipe>(entity =>
        {
            entity.HasIndex(x => new { x.MenuItemId, x.IsDefault });
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.HasOne(x => x.MenuItem).WithMany(x => x.Recipes).HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuItemRecipeLine>(entity =>
        {
            entity.HasIndex(x => new { x.MenuItemRecipeId, x.LineNo }).IsUnique();
            entity.HasIndex(x => x.ItemId);
            entity.Property(x => x.ItemCode).HasMaxLength(50);
            entity.Property(x => x.ItemName).HasMaxLength(255);
            entity.Property(x => x.UnitName).HasMaxLength(30);
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.LossRate).HasPrecision(9, 4);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasOne(x => x.Recipe).WithMany(x => x.Lines).HasForeignKey(x => x.MenuItemRecipeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<MenuItem>().WithMany().HasForeignKey(x => x.ChildMenuItemId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.DefaultWarehouseId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RecipeIngredientSubstitute>(entity =>
        {
            entity.HasIndex(x => x.RecipeLineId);
            entity.HasIndex(x => x.InventoryItemId);
            entity.HasIndex(x => x.SubstituteInventoryItemId);
            entity.HasIndex(x => new { x.RecipeLineId, x.SubstituteInventoryItemId, x.IsDeleted });
            entity.Property(x => x.ConversionRate).HasPrecision(18, 4);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.ModifiedBy).HasMaxLength(120);
            entity.HasOne(x => x.RecipeLine).WithMany(x => x.Substitutes).HasForeignKey(x => x.RecipeLineId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.InventoryItem).WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.SubstituteInventoryItem).WithMany().HasForeignKey(x => x.SubstituteInventoryItemId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BreakfastCafeOrder>(entity =>
        {
            entity.HasIndex(x => x.OrderDate);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.PaymentStatus);
            entity.HasIndex(x => x.PosTableId);
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.Property(x => x.OrderNumber).HasMaxLength(60);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.PaymentStatus).HasMaxLength(30);
            entity.Property(x => x.OrderType).HasMaxLength(30);
            entity.Property(x => x.ServiceStatus).HasMaxLength(30);
            entity.Property(x => x.AreaName).HasMaxLength(120);
            entity.Property(x => x.TableCode).HasMaxLength(50);
            entity.Property(x => x.TableName).HasMaxLength(120);
            entity.Property(x => x.CustomerReference).HasMaxLength(255);
            entity.Property(x => x.RoomReference).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.VatAmount).HasPrecision(18, 2);
            entity.Property(x => x.ServiceChargeAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.PaidAmount).HasPrecision(18, 2);
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            entity.HasMany(x => x.Lines).WithOne(x => x.Order).HasForeignKey(x => x.BreakfastCafeOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Payments).WithOne(x => x.Order).HasForeignKey(x => x.BreakfastCafeOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.KitchenTickets).WithOne(x => x.Order).HasForeignKey(x => x.BreakfastCafeOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PosTable).WithMany(x => x.Orders).HasForeignKey(x => x.PosTableId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BreakfastCafeOrderLine>(entity =>
        {
            entity.HasIndex(x => x.MenuItemCode);
            entity.HasIndex(x => new { x.BreakfastCafeOrderId, x.LineNo }).IsUnique();
            entity.Property(x => x.MenuItemCode).HasMaxLength(50);
            entity.Property(x => x.MenuItemName).HasMaxLength(255);
            entity.Property(x => x.UnitName).HasMaxLength(30);
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.LineDiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.VatRate).HasPrecision(9, 4);
            entity.Property(x => x.VatAmount).HasPrecision(18, 2);
            entity.Property(x => x.LineTotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.KitchenStatus).HasMaxLength(30);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BreakfastCafePayment>(entity =>
        {
            entity.HasIndex(x => new { x.BreakfastCafeOrderId, x.PaymentDate });
            entity.HasIndex(x => x.PaymentMethod);
            entity.Property(x => x.PaymentMethod).HasMaxLength(40);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.ReferenceNo).HasMaxLength(120);
            entity.Property(x => x.RoomReference).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.HasOne<CashFund>().WithMany().HasForeignKey(x => x.CashFundId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<BankAccount>().WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PosTable>(entity =>
        {
            entity.HasIndex(x => x.AreaCode);
            entity.HasIndex(x => x.TableCode).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.AreaCode).HasMaxLength(50);
            entity.Property(x => x.AreaName).HasMaxLength(120);
            entity.Property(x => x.TableCode).HasMaxLength(50);
            entity.Property(x => x.TableName).HasMaxLength(120);
            entity.Property(x => x.OrderType).HasMaxLength(30);
            entity.Property(x => x.Status).HasMaxLength(30);
        });

        modelBuilder.Entity<BreakfastCafeKitchenTicket>(entity =>
        {
            entity.HasIndex(x => x.BreakfastCafeOrderId);
            entity.HasIndex(x => x.TicketNumber).IsUnique();
            entity.Property(x => x.TicketNumber).HasMaxLength(60);
            entity.Property(x => x.Station).HasMaxLength(50);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.SentBy).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasOne(x => x.Order).WithMany(x => x.KitchenTickets).HasForeignKey(x => x.BreakfastCafeOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FrontOfficeOutboxMessage>(entity =>
        {
            entity.ToTable("fo_hop_thu_di", "fo");
            entity.HasKey(x => x.MessageId).HasName("PK_fo_hop_thu_di");
            var messageId = entity.Property(x => x.MessageId).HasColumnName("ma_thong_diep");
            messageId.HasConversion(v => v.Value, v => new MessageId(v));
            entity.Property(x => x.EventName).HasColumnName("ten_su_kien").HasMaxLength(200);
            entity.Property(x => x.SchemaVersion).HasColumnName("phien_ban_schema");
            entity.Property(x => x.OccurredAtUtc).HasColumnName("xay_ra_luc_utc");
            entity.Property(x => x.PublishedAtUtc).HasColumnName("xuat_ban_luc_utc");
            entity.Property(x => x.PropertyId).HasColumnName("ma_co_so").HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<PropertyId?, Guid?>(v => v.HasValue ? v.Value.Value : null, v => v.HasValue ? PropertyId.From(v.Value) : null));
            entity.Property(x => x.BusinessDate).HasColumnName("ngay_nghiep_vu");
            entity.Property(x => x.CorrelationId).HasColumnName("ma_tuong_quan").HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<CorrelationId?, Guid?>(v => v.HasValue ? v.Value.Value : null, v => v.HasValue ? CorrelationId.From(v.Value) : null));
            entity.Property(x => x.CausationId).HasColumnName("ma_nguyen_nhan").HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<EventId?, Guid?>(v => v.HasValue ? v.Value.Value : null, v => v.HasValue ? EventId.From(v.Value) : null));
            entity.Property(x => x.AggregateType).HasColumnName("loai_tong_hop").HasMaxLength(160);
            entity.Property(x => x.AggregateId).HasColumnName("ma_tong_hop").HasMaxLength(100);
            entity.Property(x => x.AggregateVersion).HasColumnName("phien_ban_tong_hop");
            entity.Property(x => x.PartitionKey).HasColumnName("khoa_phan_vung").HasMaxLength(200);
            entity.Property(x => x.Sensitivity).HasColumnName("do_nhay_cam").HasMaxLength(30);
            entity.Property(x => x.Payload).HasColumnName("noi_dung");
            entity.Property(x => x.PayloadHash).HasColumnName("ma_bam_noi_dung").HasMaxLength(64);
            entity.Property(x => x.Status).HasColumnName("trang_thai").HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.RetryCount).HasColumnName("so_lan_thu_lai");
            entity.Property(x => x.NextRetryAtUtc).HasColumnName("thu_lai_luc_utc");
            entity.Property(x => x.LeaseOwner).HasColumnName("chu_so_huu_thue").HasMaxLength(120);
            entity.Property(x => x.LeaseExpiresAtUtc).HasColumnName("het_han_thue_utc");
            entity.Property(x => x.LastErrorCode).HasColumnName("ma_loi_cuoi").HasMaxLength(100);
            entity.Property(x => x.CreatedAtUtc).HasColumnName("tao_luc_utc");
            ConfigureRowVersion(entity.Property(x => x.RowVersion).HasColumnName("phien_ban_dong"));
            entity.HasIndex(x => new { x.Status, x.NextRetryAtUtc }).HasDatabaseName("IX_fo_hop_thu_di_trang_thai_thu_lai");
            entity.HasIndex(x => x.LeaseExpiresAtUtc).HasDatabaseName("IX_fo_hop_thu_di_het_han_thue");
            entity.HasIndex(x => x.PayloadHash).HasDatabaseName("IX_fo_hop_thu_di_ma_bam");
        });

        modelBuilder.Entity<FrontOfficeInboxMessage>(entity =>
        {
            entity.ToTable("fo_hop_thu_den", "fo");
            entity.HasKey(x => new { x.ConsumerName, x.MessageId }).HasName("PK_fo_hop_thu_den");
            entity.Property(x => x.ConsumerName).HasColumnName("ten_bo_tieu_thu").HasMaxLength(160);
            var messageId = entity.Property(x => x.MessageId).HasColumnName("ma_thong_diep");
            messageId.HasConversion(v => v.Value, v => new MessageId(v));
            entity.Property(x => x.PayloadHash).HasColumnName("ma_bam_noi_dung").HasMaxLength(64);
            entity.Property(x => x.ReceivedAtUtc).HasColumnName("nhan_luc_utc");
            entity.Property(x => x.CompletedAtUtc).HasColumnName("hoan_tat_luc_utc");
            entity.Property(x => x.Status).HasColumnName("trang_thai").HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.RetryCount).HasColumnName("so_lan_thu_lai");
            entity.Property(x => x.LastErrorCode).HasColumnName("ma_loi_cuoi").HasMaxLength(100);
            entity.Property(x => x.LeaseOwner).HasColumnName("chu_so_huu_thue").HasMaxLength(120);
            entity.Property(x => x.LeaseExpiresAtUtc).HasColumnName("het_han_thue_utc");
            ConfigureRowVersion(entity.Property(x => x.RowVersion).HasColumnName("phien_ban_dong"));
        });

        modelBuilder.Entity<FrontOfficeIdempotencyRecord>(entity =>
        {
            entity.ToTable("fo_khoa_idempotency", "fo");
            entity.HasKey(x => x.Id).HasName("PK_fo_khoa_idempotency");
            entity.Property(x => x.Id).HasColumnName("ma");
            entity.Property(x => x.Namespace).HasColumnName("khong_gian_ten").HasMaxLength(120);
            entity.Property(x => x.Key).HasColumnName("khoa").HasConversion(v => v.Value, v => IdempotencyKey.From(v)).HasMaxLength(160);
            var commandId = entity.Property(x => x.CommandId).HasColumnName("ma_lenh");
            commandId.HasConversion(v => v.Value, v => new CommandId(v));
            entity.Property(x => x.RequestHash).HasColumnName("ma_bam_yeu_cau").HasMaxLength(64);
            entity.Property(x => x.Status).HasColumnName("trang_thai").HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.ResultType).HasColumnName("loai_ket_qua").HasMaxLength(100);
            entity.Property(x => x.ResultReference).HasColumnName("tham_chieu_ket_qua").HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).HasColumnName("tao_luc_utc");
            entity.Property(x => x.CompletedAtUtc).HasColumnName("hoan_tat_luc_utc");
            entity.Property(x => x.ExpiresAtUtc).HasColumnName("het_han_luc_utc");
            entity.Property(x => x.FailureClassification).HasColumnName("phan_loai_that_bai").HasMaxLength(80);
            ConfigureRowVersion(entity.Property(x => x.RowVersion).HasColumnName("phien_ban_dong"));
            entity.HasIndex(x => new { x.Namespace, x.Key }).IsUnique().HasDatabaseName("UQ_fo_khoa_idempotency_khong_gian_khoa");
            entity.HasIndex(x => new { x.Status, x.ExpiresAtUtc }).HasDatabaseName("IX_fo_khoa_idempotency_trang_thai_het_han");
        });

        modelBuilder.Entity<ProcessInstance>(entity =>
        {
            entity.ToTable("QuyTrinhVanHanh", "fo_process");
            entity.HasKey(x => x.ProcessId).HasName("PK_fo_process_QuyTrinhVanHanh");
            entity.Property(x => x.ProcessKey).HasMaxLength(200);
            entity.Property(x => x.ProcessType).HasMaxLength(120);
            entity.Property(x => x.RootSubjectType).HasMaxLength(100);
            entity.Property(x => x.RootSubjectId).HasMaxLength(120);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.CurrentState).HasMaxLength(80);
            entity.Property(x => x.CurrentStep).HasMaxLength(120);
            entity.Property(x => x.LastErrorCode).HasMaxLength(100);
            entity.Property(x => x.ResumeToken).HasMaxLength(160);
            entity.Property(x => x.CompensationStatus).HasConversion<string>().HasMaxLength(40);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => x.ProcessKey).IsUnique().HasDatabaseName("UQ_fo_process_QuyTrinhVanHanh_ProcessKey");
            entity.HasIndex(x => new { x.Status, x.ExpiresAtUtc }).HasDatabaseName("IX_fo_process_QuyTrinhVanHanh_Status_Expiry");
            entity.HasIndex(x => new { x.PropertyId, x.RootSubjectType, x.RootSubjectId }).HasDatabaseName("IX_fo_process_QuyTrinhVanHanh_Subject");
            entity.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_fo_process_QuyTrinhVanHanh_Correlation");
        });
        modelBuilder.Entity<ProcessStep>(entity =>
        {
            entity.ToTable("BuocQuyTrinh", "fo_process"); entity.HasKey(x => x.StepId).HasName("PK_fo_process_BuocQuyTrinh");
            entity.Property(x => x.StepName).HasMaxLength(120); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); entity.Property(x => x.ExpectedSignalType).HasMaxLength(120); entity.Property(x => x.CommandReference).HasMaxLength(500); entity.Property(x => x.ResultReference).HasMaxLength(500); entity.Property(x => x.LastErrorCode).HasMaxLength(100); ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.ProcessId, x.Sequence }).IsUnique().HasDatabaseName("UQ_fo_process_BuocQuyTrinh_Process_Sequence"); entity.HasOne<ProcessInstance>().WithMany().HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProcessStepAttempt>(entity =>
        {
            entity.ToTable("LanThucHienBuocQuyTrinh", "fo_process"); entity.HasKey(x => x.AttemptId).HasName("PK_fo_process_LanThucHienBuocQuyTrinh");
            entity.Property(x => x.Outcome).HasMaxLength(40); entity.Property(x => x.ErrorCode).HasMaxLength(100); entity.Property(x => x.RetryClassification).HasMaxLength(60); entity.Property(x => x.WorkerId).HasMaxLength(120); entity.Property(x => x.PayloadHash).HasMaxLength(64); entity.Property(x => x.SanitizedMetadata).HasMaxLength(2000);
            entity.HasIndex(x => new { x.StepId, x.AttemptNumber }).IsUnique().HasDatabaseName("UQ_fo_process_LanThucHienBuocQuyTrinh_Step_Attempt"); entity.HasOne<ProcessInstance>().WithMany().HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<ProcessStep>().WithMany().HasForeignKey(x => x.StepId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProcessSignal>(entity =>
        {
            entity.ToTable("TinHieuQuyTrinh", "fo_process"); entity.HasKey(x => new { x.ProcessId, x.SignalId }).HasName("PK_fo_process_TinHieuQuyTrinh");
            entity.Property(x => x.SignalType).HasMaxLength(120); entity.Property(x => x.PayloadHash).HasMaxLength(64); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); entity.Property(x => x.ErrorCode).HasMaxLength(100);
            entity.HasIndex(x => new { x.ProcessId, x.SignalId }).IsUnique().HasDatabaseName("UQ_fo_process_TinHieuQuyTrinh_Process_Signal"); entity.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_fo_process_TinHieuQuyTrinh_Correlation"); entity.HasOne<ProcessInstance>().WithMany().HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProcessTimeout>(entity =>
        {
            entity.ToTable("HenGioQuyTrinh", "fo_process"); entity.HasKey(x => x.TimeoutId).HasName("PK_fo_process_HenGioQuyTrinh"); entity.Property(x => x.TimeoutType).HasMaxLength(120); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); entity.Property(x => x.ClaimedBy).HasMaxLength(120); ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.Status, x.DueAtUtc, x.LeaseExpiresAtUtc }).HasDatabaseName("IX_fo_process_HenGioQuyTrinh_Claim"); entity.HasOne<ProcessInstance>().WithMany().HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<ProcessStep>().WithMany().HasForeignKey(x => x.StepId).OnDelete(DeleteBehavior.Restrict);
          });
        modelBuilder.Entity<ProcessDeadLetter>(entity =>
        {
            entity.ToTable("ThuDaChetQuyTrinh", "fo_process");
            entity.HasKey(x => x.DeadLetterId).HasName("PK_fo_process_ThuDaChetQuyTrinh");
            entity.Property(x => x.Reason).HasMaxLength(200);
            entity.Property(x => x.ErrorCode).HasMaxLength(100);
            entity.Property(x => x.WorkerId).HasMaxLength(120);
            entity.HasIndex(x => x.TimeoutId).IsUnique().HasDatabaseName("UQ_fo_process_ThuDaChetQuyTrinh_Timeout");
            entity.HasIndex(x => new { x.ProcessId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_process_ThuDaChetQuyTrinh_Process_Occurred");
            entity.HasIndex(x => new { x.BusinessDate, x.OccurredAtUtc }).HasDatabaseName("IX_fo_process_ThuDaChetQuyTrinh_BusinessDate_Occurred");
            entity.HasOne<ProcessInstance>().WithMany().HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ProcessTimeout>().WithMany().HasForeignKey(x => x.TimeoutId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProcessCompensation>(entity => { entity.ToTable("BuTruQuyTrinh", "fo_process"); entity.HasKey(x => x.CompensationId).HasName("PK_fo_process_BuTruQuyTrinh"); entity.Property(x => x.CompensationType).HasMaxLength(100); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); entity.Property(x => x.CommandReference).HasMaxLength(500); entity.Property(x => x.ErrorCode).HasMaxLength(100); entity.Property(x => x.ApprovalReference).HasMaxLength(500); entity.Property(x => x.AuditReference).HasMaxLength(500); entity.HasIndex(x => new { x.ProcessId, x.Status }).HasDatabaseName("IX_fo_process_BuTruQuyTrinh_Process_Status"); entity.HasOne<ProcessInstance>().WithMany().HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<ProcessStep>().WithMany().HasForeignKey(x => x.StepId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<ProcessManualAction>(entity => { entity.ToTable("HanhDongThuCongQuyTrinh", "fo_process"); entity.HasKey(x => x.ManualActionId).HasName("PK_fo_process_HanhDongThuCongQuyTrinh"); entity.Property(x => x.ActionType).HasMaxLength(100); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); entity.Property(x => x.RequiredPermission).HasMaxLength(120); entity.Property(x => x.AssignedRole).HasMaxLength(120); entity.Property(x => x.ResultCode).HasMaxLength(100); entity.Property(x => x.AuditReference).HasMaxLength(500); entity.HasIndex(x => new { x.Status, x.DueAtUtc }).HasDatabaseName("IX_fo_process_HanhDongThuCongQuyTrinh_Status_Due"); entity.HasOne<ProcessInstance>().WithMany().HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<ProcessHistory>(entity => { entity.ToTable("LichSuQuyTrinh", "fo_process"); entity.HasKey(x => x.HistoryId).HasName("PK_fo_process_LichSuQuyTrinh"); entity.Property(x => x.PreviousState).HasMaxLength(80); entity.Property(x => x.NewState).HasMaxLength(80); entity.Property(x => x.PreviousStep).HasMaxLength(120); entity.Property(x => x.NewStep).HasMaxLength(120); entity.Property(x => x.TransitionType).HasMaxLength(80); entity.Property(x => x.ErrorCode).HasMaxLength(100); entity.Property(x => x.ReasonCode).HasMaxLength(100); entity.HasIndex(x => new { x.ProcessId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_process_LichSuQuyTrinh_Process_Occurred"); entity.HasOne<ProcessInstance>().WithMany().HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Reservation>(entity => { entity.ToTable("DatPhong", "fo"); entity.HasKey(x => x.ReservationId).HasName("PK_fo_DatPhong"); entity.Property(x => x.ReservationNumber).HasMaxLength(40); entity.Property(x => x.GuestNameSnapshot).HasMaxLength(200); entity.Property(x => x.GuestPhoneSnapshot).HasMaxLength(50); entity.Property(x => x.GuestEmailSnapshot).HasMaxLength(200); entity.Property(x => x.Source).HasMaxLength(80); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.SpecialRequests).HasMaxLength(1000); entity.Property(x => x.InternalNotes).HasMaxLength(1000); entity.Property(x => x.CancellationReason).HasMaxLength(300); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.ReservationNumber }).IsUnique().HasDatabaseName("UQ_fo_DatPhong_CoSo_SoDatPhong"); entity.HasIndex(x => new { x.PropertyId, x.ArrivalDate, x.DepartureDate }).HasDatabaseName("IX_fo_DatPhong_CoSo_NgayO"); entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_DatPhong_CoSo_TrangThai"); entity.HasIndex(x => new { x.PropertyId, x.AssignedRoomId, x.ArrivalDate, x.DepartureDate }).HasDatabaseName("IX_fo_DatPhong_Phong_NgayO"); });
        modelBuilder.Entity<ReservationHistory>(entity => { entity.ToTable("LichSuDatPhong", "fo"); entity.HasKey(x => x.HistoryId).HasName("PK_fo_LichSuDatPhong"); entity.Property(x => x.Action).HasMaxLength(80); entity.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.ReasonCode).HasMaxLength(100); entity.Property(x => x.SafeSummary).HasMaxLength(300); entity.HasIndex(x => new { x.ReservationId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_LichSuDatPhong_DatPhong_Luc"); entity.HasOne<Reservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<HotelRoomType>(entity => { entity.ToTable("LoaiPhongKhachSan", "fo"); entity.HasKey(x => x.HotelRoomTypeId).HasName("PK_fo_LoaiPhongKhachSan"); entity.Property(x => x.Code).HasMaxLength(40); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_LoaiPhongKhachSan_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.IsActive }).HasDatabaseName("IX_fo_LoaiPhongKhachSan_CoSo_HoatDong"); entity.HasIndex(x => new { x.PropertyId, x.DisplayOrder }).HasDatabaseName("IX_fo_LoaiPhongKhachSan_CoSo_ThuTu"); });
        modelBuilder.Entity<HotelRoom>(entity => { entity.ToTable("PhongKhachSan", "fo"); entity.HasKey(x => x.HotelRoomId).HasName("PK_fo_PhongKhachSan"); entity.Property(x => x.RoomNumber).HasMaxLength(40); entity.Property(x => x.DisplayName).HasMaxLength(160); entity.Property(x => x.Floor).HasMaxLength(40); entity.Property(x => x.Building).HasMaxLength(80); entity.Property(x => x.Zone).HasMaxLength(80); entity.Property(x => x.Description).HasMaxLength(1000); entity.Property(x => x.OperationalStatus).HasConversion<string>().HasMaxLength(30); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.RoomNumber }).IsUnique().HasDatabaseName("UQ_fo_PhongKhachSan_CoSo_SoPhong"); entity.HasIndex(x => new { x.PropertyId, x.HotelRoomTypeId }).HasDatabaseName("IX_fo_PhongKhachSan_CoSo_LoaiPhong"); entity.HasIndex(x => new { x.PropertyId, x.OperationalStatus }).HasDatabaseName("IX_fo_PhongKhachSan_CoSo_TrangThai"); entity.HasIndex(x => new { x.PropertyId, x.IsActive }).HasDatabaseName("IX_fo_PhongKhachSan_CoSo_HoatDong"); entity.HasOne<HotelRoomType>().WithMany().HasForeignKey(x => x.HotelRoomTypeId).OnDelete(DeleteBehavior.Restrict); });

        ConfigureRestaurantModules(modelBuilder, useSqlServerRowVersion);
        ApplyCompanyQueryFilters(modelBuilder);

        modelBuilder.Entity<Item>().HasIndex(x => x.ItemCode).IsUnique();
        modelBuilder.Entity<Item>().HasIndex(x => x.ItemName).IsUnique();

        modelBuilder.Entity<Customer>().HasIndex(x => x.CustomerCode).IsUnique();
        modelBuilder.Entity<Supplier>().HasIndex(x => x.SupplierCode).IsUnique();

        modelBuilder.Entity<Warehouse>().HasIndex(x => x.WarehouseCode).IsUnique();
        modelBuilder.Entity<Warehouse>().HasIndex(x => x.ParentWarehouseId);

        modelBuilder.Entity<Department>().HasIndex(x => x.DepartmentCode).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => x.ParentDepartmentId);

        modelBuilder.Entity<Person>().HasIndex(x => x.PersonCode).IsUnique();
        modelBuilder.Entity<Person>().HasIndex(x => x.DepartmentId);
        modelBuilder.Entity<Person>().HasIndex(x => x.Phone);

        modelBuilder.Entity<StockReceipt>().HasIndex(x => x.ReceiptNo).IsUnique();
        modelBuilder.Entity<StockReceipt>().HasIndex(x => x.ReceiptDate);
        modelBuilder.Entity<StockReceipt>().HasIndex(x => x.JournalHeaderId);
        modelBuilder.Entity<StockReceipt>().HasIndex(x => x.CompanyId);
        modelBuilder.Entity<StockReceipt>().HasIndex(x => x.BranchId);
        modelBuilder.Entity<StockReceipt>().HasIndex(x => x.PostingStatus);
        modelBuilder.Entity<StockReceipt>().HasIndex(x => new { x.CompanyId, x.BranchId, x.PostingStatus, x.ReceiptDate });
        modelBuilder.Entity<StockReceipt>().Property(x => x.IsApproved).HasDefaultValue(false);
        modelBuilder.Entity<StockReceipt>().Property(x => x.PostedBy).HasMaxLength(255);
        modelBuilder.Entity<StockReceipt>().Property(x => x.PostingStatus).HasMaxLength(50);
        ConfigureRowVersion(modelBuilder.Entity<StockReceipt>().Property(x => x.RowVersion));
        modelBuilder.Entity<StockReceipt>().HasMany(x => x.Details).WithOne().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockReceiptDetail>().Property(x => x.VatRate).HasPrecision(5, 2);
        modelBuilder.Entity<StockReceiptDetail>().Property(x => x.VatAmount).HasPrecision(18, 2);

        modelBuilder.Entity<StockIssue>().HasIndex(x => x.IssueNo).IsUnique();
        modelBuilder.Entity<StockIssue>().HasIndex(x => x.IssueDate);
        modelBuilder.Entity<StockIssue>().HasIndex(x => x.JournalHeaderId);
        modelBuilder.Entity<StockIssue>().HasIndex(x => x.CompanyId);
        modelBuilder.Entity<StockIssue>().HasIndex(x => x.BranchId);
        modelBuilder.Entity<StockIssue>().HasIndex(x => x.PostingStatus);
        modelBuilder.Entity<StockIssue>().HasIndex(x => new { x.CompanyId, x.BranchId, x.PostingStatus, x.IssueDate });
        modelBuilder.Entity<StockIssue>().Property(x => x.IsApproved).HasDefaultValue(false);
        modelBuilder.Entity<StockIssue>().Property(x => x.PostedBy).HasMaxLength(255);
        modelBuilder.Entity<StockIssue>().Property(x => x.PostingStatus).HasMaxLength(50);
        ConfigureRowVersion(modelBuilder.Entity<StockIssue>().Property(x => x.RowVersion));
        modelBuilder.Entity<StockIssue>().HasMany(x => x.Details).WithOne().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockBalance>().HasIndex(x => new { x.WarehouseId, x.ItemId }).IsUnique();
        modelBuilder.Entity<AppSetting>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<AppPermission>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(120);
            entity.Property(x => x.Name).HasMaxLength(180);
            entity.Property(x => x.Group).HasMaxLength(80);
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<AppUserPermission>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.PermissionCode, x.CompanyId, x.BranchId }).IsUnique();
            entity.Property(x => x.PermissionCode).HasMaxLength(120);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.IsArchived).HasDefaultValue(false);
        });

        modelBuilder.Entity<AppRolePermission>(entity =>
        {
            entity.HasIndex(x => new { x.RoleId, x.PermissionCode }).IsUnique();
            entity.Property(x => x.PermissionCode).HasMaxLength(120);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppRoleInheritance>(entity =>
        {
            entity.HasIndex(x => new { x.RoleId, x.ParentRoleId }).IsUnique();
            entity.HasIndex(x => x.ParentRoleId);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.ParentRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ParentRole)
                .WithMany()
                .HasForeignKey(x => x.ParentRoleId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AppDataPermission>(entity =>
        {
            entity.HasIndex(x => new { x.SubjectType, x.SubjectId, x.ScopeType, x.ScopeId, x.CompanyId, x.BranchId }).IsUnique();
            entity.HasIndex(x => new { x.ScopeType, x.ScopeId });
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.BranchId);
            entity.Property(x => x.SubjectType).HasMaxLength(30);
            entity.Property(x => x.ScopeType).HasMaxLength(40);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<UserWorkspacePreference>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.Category, x.PreferenceKey, x.CompanyId, x.BranchId }).IsUnique();
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.PreferenceKey).HasMaxLength(120);
            entity.Property(x => x.ValueJson);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.Property(x => x.UserName).HasMaxLength(120);
            entity.Property(x => x.DisplayName).HasMaxLength(180);
            entity.Property(x => x.Email).HasMaxLength(255);
        });

        modelBuilder.Entity<AppUserRole>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BackgroundJobDefinition>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(180);
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<BackgroundJobSchedule>(entity =>
        {
            entity.HasIndex(x => x.JobDefinitionId).IsUnique();
            entity.HasIndex(x => new { x.IsPaused, x.NextRunAtUtc });
            entity.Property(x => x.CronExpression).HasMaxLength(120);
            entity.HasOne(x => x.JobDefinition).WithMany(x => x.Schedules).HasForeignKey(x => x.JobDefinitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BackgroundJobLog>(entity =>
        {
            entity.HasIndex(x => x.ExecutionId);
            entity.HasIndex(x => x.TimestampUtc);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.Execution).WithMany(x => x.Logs).HasForeignKey(x => x.ExecutionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BackgroundJobExecution>(entity =>
        {
            entity.HasIndex(x => x.JobName);
            entity.HasIndex(x => x.JobDefinitionId);
            entity.HasIndex(x => new { x.Status, x.StartedAtUtc });
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.StartedAt);
            entity.HasIndex(x => x.QueuedAtUtc);
            entity.Property(x => x.JobName).HasMaxLength(160);
            entity.Property(x => x.JobType).HasMaxLength(80);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.Error).HasMaxLength(4000);
            entity.Property(x => x.RequestedBy).HasMaxLength(255);
            entity.Property(x => x.CorrelationId).HasMaxLength(64);
            entity.Property(x => x.TriggerSource).HasMaxLength(40);
            entity.Property(x => x.ErrorMessage).HasMaxLength(4000);
            entity.HasOne(x => x.JobDefinition).WithMany(x => x.Executions).HasForeignKey(x => x.JobDefinitionId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<JobDefinition>(entity =>
        {
            entity.HasIndex(x => x.JobCode).IsUnique();
            entity.HasIndex(x => x.NextRunAt);
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.JobCode).HasMaxLength(100);
            entity.Property(x => x.JobName).HasMaxLength(180);
            entity.Property(x => x.JobType).HasMaxLength(80);
            entity.Property(x => x.ScheduleType).HasMaxLength(30);
            entity.Property(x => x.CronExpression).HasMaxLength(120);
        });

        modelBuilder.Entity<JobExecution>(entity =>
        {
            entity.HasIndex(x => x.JobDefinitionId);
            entity.HasIndex(x => x.JobCode);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.QueuedAt);
            entity.Property(x => x.JobCode).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.TriggeredBy).HasMaxLength(255);
            entity.Property(x => x.CorrelationId).HasMaxLength(64);
            entity.Property(x => x.ErrorMessage).HasMaxLength(4000);
            entity.HasOne(x => x.JobDefinition).WithMany().HasForeignKey(x => x.JobDefinitionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(x => x.Retries).WithOne(x => x.JobExecution).HasForeignKey(x => x.JobExecutionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Logs).WithOne(x => x.JobExecution).HasForeignKey(x => x.JobExecutionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobRetry>(entity =>
        {
            entity.HasIndex(x => x.JobExecutionId);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ErrorMessage).HasMaxLength(4000);
        });

        modelBuilder.Entity<JobLog>(entity =>
        {
            entity.HasIndex(x => x.JobExecutionId);
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.Level).HasMaxLength(30);
            entity.Property(x => x.Message).HasMaxLength(2000);
        });

        modelBuilder.Entity<AuditLog>().HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<AuditLog>().HasIndex(x => new { x.EntityName, x.EntityId });
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.Action);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.CompanyId);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.BranchId);
        modelBuilder.Entity<AuditLog>().HasIndex(x => new { x.CompanyId, x.BranchId, x.CreatedAt });
        modelBuilder.Entity<AuditLog>().Property(x => x.Username).HasMaxLength(100);
        modelBuilder.Entity<AuditLog>().Property(x => x.Action).HasMaxLength(200);
        modelBuilder.Entity<AuditLog>().Property(x => x.Detail).HasMaxLength(4000);
        modelBuilder.Entity<AuditLog>().Property(x => x.EntityName).HasMaxLength(100);
        modelBuilder.Entity<AuditLog>().Property(x => x.EntityId).HasMaxLength(100);
        modelBuilder.Entity<AuditLog>().Property(x => x.IpAddress).HasMaxLength(100);
        modelBuilder.Entity<AuditLog>().Property(x => x.CorrelationId).HasMaxLength(100);
        modelBuilder.Entity<AuditSession>(entity =>
        {
            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.StartedAtUtc });
            entity.HasIndex(x => new { x.Status, x.LastActivityAtUtc });
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).HasDefaultValue(AuditSessionStatus.Started);
            entity.Property(x => x.Source).HasMaxLength(120);
            entity.Property(x => x.ClientMachine).HasMaxLength(120);
            entity.Property(x => x.AppVersion).HasMaxLength(60);
            entity.Property(x => x.DeviceId).HasMaxLength(120);
            entity.Property(x => x.Platform).HasMaxLength(80);
            entity.Property(x => x.CompletedReason).HasMaxLength(200);
            entity.Property(x => x.SessionMetadataJson);
            entity.Property(x => x.UserId);
        });

        modelBuilder.Entity<AuditCorrelation>(entity =>
        {
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.ParentCorrelationId);
            entity.HasIndex(x => x.AuditSessionId);
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.HasIndex(x => new { x.Module, x.Action });
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.ParentCorrelationId).HasMaxLength(100);
            entity.Property(x => x.EntityType).HasMaxLength(120);
            entity.Property(x => x.EntityId).HasMaxLength(120);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Action).HasMaxLength(120);
        });

        modelBuilder.Entity<AuditEntitySnapshot>(entity =>
        {
            entity.HasIndex(x => x.AuditLogId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.EntityType, x.EntityId });
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.Property(x => x.EntityType).HasMaxLength(120);
            entity.Property(x => x.EntityId).HasMaxLength(120);
            entity.Property(x => x.BeforeJson);
            entity.Property(x => x.AfterJson);
            entity.Property(x => x.DiffJson);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
            entity.Property(x => x.CompressedPayload);
            entity.Property(x => x.CompressionType).HasMaxLength(20).HasDefaultValue("None");
            entity.Property(x => x.HashAlgorithm).HasMaxLength(20).HasDefaultValue("SHA256");
            entity.HasOne(x => x.AuditLog)
                .WithMany()
                .HasForeignKey(x => x.AuditLogId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditReplayEntry>(entity =>
        {
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.ParentCorrelationId);
            entity.HasIndex(x => x.AuditSessionId);
            entity.HasIndex(x => new { x.CorrelationId, x.SequenceNo }).IsUnique();
            entity.HasIndex(x => new { x.Module, x.Action });
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.HasIndex(x => x.TimestampUtc);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.ParentCorrelationId).HasMaxLength(100);
            entity.Property(x => x.Route).HasMaxLength(180);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Action).HasMaxLength(120);
            entity.Property(x => x.EntityType).HasMaxLength(120);
            entity.Property(x => x.EntityId).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
        });

        modelBuilder.Entity<AuditExportHistory>(entity =>
        {
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.CreatedAtUtc });
            entity.Property(x => x.ExportType).HasMaxLength(30);
            entity.Property(x => x.FilterJson);
            entity.Property(x => x.FilePath).HasMaxLength(1000);
            entity.Property(x => x.ExportHash).HasMaxLength(64);
            entity.Property(x => x.HashAlgorithm).HasMaxLength(20).HasDefaultValue("SHA256");
            entity.Property(x => x.CorrelationCoverageJson);
            entity.Property(x => x.IntegrityManifestJson);
        });
        modelBuilder.Entity<RuntimeMetricSnapshot>(entity =>
        {
            entity.HasIndex(x => x.RecordedAtUtc);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.RecordedAtUtc });
            entity.HasIndex(x => x.IsArchived);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Category).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.MetricName).HasMaxLength(120);
            entity.Property(x => x.Unit).HasMaxLength(40);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.UserName).HasMaxLength(120);
        });

        modelBuilder.Entity<TelemetryEvent>(entity =>
        {
            entity.HasIndex(x => x.RecordedAtUtc);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.RecordedAtUtc });
            entity.HasIndex(x => new { x.IsArchived, x.RecordedAtUtc });
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Category).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.EventType).HasMaxLength(60);
            entity.Property(x => x.EventName).HasMaxLength(180);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.SearchText).HasMaxLength(4000);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.UserName).HasMaxLength(120);
        });

        modelBuilder.Entity<SlowOperationLog>(entity =>
        {
            entity.HasIndex(x => x.RecordedAtUtc);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.RecordedAtUtc });
            entity.HasIndex(x => x.DurationMs);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Category).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.OperationName).HasMaxLength(180);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.UserName).HasMaxLength(120);
        });

        modelBuilder.Entity<RuntimeExceptionLog>(entity =>
        {
            entity.HasIndex(x => x.RecordedAtUtc);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.RecordedAtUtc });
            entity.HasIndex(x => new { x.ExceptionType, x.StackHash });
            entity.HasIndex(x => x.IsArchived);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Category).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.ExceptionType).HasMaxLength(200);
            entity.Property(x => x.StackHash).HasMaxLength(64);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.UserName).HasMaxLength(120);
        });

        modelBuilder.Entity<UiResponsivenessSnapshot>(entity =>
        {
            entity.HasIndex(x => x.RecordedAtUtc);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.RecordedAtUtc });
            entity.HasIndex(x => x.IsFrozen);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Category).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.OperationName).HasMaxLength(180);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.UserName).HasMaxLength(120);
        });

        modelBuilder.Entity<RecoveryRecommendation>(entity =>
        {
            entity.HasIndex(x => x.RecordedAtUtc);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.RecordedAtUtc });
            entity.HasIndex(x => x.RecommendationCode);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Category).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.RecommendationCode).HasMaxLength(120);
            entity.Property(x => x.SuggestedAction).HasMaxLength(500);
            entity.Property(x => x.ConfidenceScore).HasPrecision(18, 4);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.UserName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
        });

        modelBuilder.Entity<SystemResourceSnapshot>(entity =>
        {
            entity.HasIndex(x => x.RecordedAtUtc);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.RecordedAtUtc });
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Category).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.UserName).HasMaxLength(120);
        });
        modelBuilder.Entity<TT99AccountMapping>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.AccountCode }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.TT99Category });
            entity.Property(x => x.AccountCode).HasMaxLength(40);
            entity.Property(x => x.TT99Category).HasMaxLength(80);
            entity.Property(x => x.TT99ReportSection).HasMaxLength(80);
            entity.Property(x => x.TT99DeclarationGroup).HasMaxLength(80);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<TT99ReportTemplate>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.ReportId }).IsUnique();
            entity.Property(x => x.ReportId).HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(250);
            entity.Property(x => x.Description).HasMaxLength(1000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<TT99ReportLine>(entity =>
        {
            entity.HasIndex(x => x.TT99ReportTemplateId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.LineCode });
            entity.Property(x => x.LineCode).HasMaxLength(80);
            entity.Property(x => x.LineName).HasMaxLength(300);
            entity.Property(x => x.Formula).HasMaxLength(2000);
            entity.Property(x => x.MappingJson);
            entity.HasOne(x => x.Template)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.TT99ReportTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<TT99ValidationRule>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.RuleCode }).IsUnique();
            entity.Property(x => x.RuleCode).HasMaxLength(120);
            entity.Property(x => x.RuleName).HasMaxLength(300);
            entity.Property(x => x.Severity).HasMaxLength(20);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<TT99ComplianceIssue>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.DetectedAtUtc });
            entity.HasIndex(x => new { x.RuleCode, x.Severity });
            entity.Property(x => x.RuleCode).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(20);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.AffectedEntity).HasMaxLength(250);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<TT99DeclarationPeriod>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.PeriodCode }).IsUnique();
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(40);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<TT99ComplianceSnapshot>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.PeriodCode, x.CreatedAtUtc });
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
            entity.Property(x => x.ComplianceScore).HasPrecision(18, 4);
            entity.Property(x => x.DeclarationStatus).HasMaxLength(40);
            entity.Property(x => x.SnapshotJson);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<TT99ExportManifest>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.PeriodCode, x.ExportedAtUtc });
            entity.Property(x => x.ExportType).HasMaxLength(60);
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.ExportedBy).HasMaxLength(120);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.Property(x => x.ComplianceVersion).HasMaxLength(40);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<TT200AccountMapping>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.AccountCode }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.TT200Category });
            entity.Property(x => x.AccountCode).HasMaxLength(40);
            entity.Property(x => x.TT200Category).HasMaxLength(80);
            entity.Property(x => x.TT200ReportSection).HasMaxLength(80);
            entity.Property(x => x.TT200ClosingGroup).HasMaxLength(80);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<TT200ReportTemplate>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.ReportId }).IsUnique();
            entity.Property(x => x.ReportId).HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(250);
            entity.Property(x => x.Description).HasMaxLength(1000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<TT200ReportLine>(entity =>
        {
            entity.HasIndex(x => x.TT200ReportTemplateId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.LineCode });
            entity.Property(x => x.LineCode).HasMaxLength(80);
            entity.Property(x => x.LineName).HasMaxLength(300);
            entity.Property(x => x.Formula).HasMaxLength(2000);
            entity.Property(x => x.MappingJson);
            entity.HasOne(x => x.Template)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.TT200ReportTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<TT200ValidationRule>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.RuleCode }).IsUnique();
            entity.Property(x => x.RuleCode).HasMaxLength(120);
            entity.Property(x => x.RuleName).HasMaxLength(300);
            entity.Property(x => x.Severity).HasMaxLength(20);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<TT200ComplianceIssue>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.DetectedAtUtc });
            entity.HasIndex(x => new { x.RuleCode, x.Severity });
            entity.Property(x => x.RuleCode).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(20);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.AffectedEntity).HasMaxLength(250);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<TT200DeclarationPeriod>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.PeriodCode }).IsUnique();
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(40);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<TT200ComplianceSnapshot>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.PeriodCode, x.CreatedAtUtc });
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
            entity.Property(x => x.ComplianceScore).HasPrecision(18, 4);
            entity.Property(x => x.DeclarationStatus).HasMaxLength(40);
            entity.Property(x => x.SnapshotJson);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<TT200ExportManifest>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.PeriodCode, x.ExportedAtUtc });
            entity.Property(x => x.ExportType).HasMaxLength(60);
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.ExportedBy).HasMaxLength(120);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.Property(x => x.ComplianceVersion).HasMaxLength(40);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<DataRetentionPolicy>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.EntityName }).IsUnique();
            entity.HasIndex(x => x.IsEnabled);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.EntityName).HasMaxLength(120);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<ArchiveBatch>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.EntityName, x.CreatedAtUtc });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.EntityName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<ArchiveBatchItem>(entity =>
        {
            entity.HasIndex(x => x.ArchiveBatchId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.EntityName });
            entity.HasIndex(x => new { x.EntityName, x.EntityId });
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.EntityName).HasMaxLength(120);
            entity.Property(x => x.EntityId).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.PayloadJson);
            entity.Property(x => x.CompressionType).HasMaxLength(20).HasDefaultValue("None");
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasOne(x => x.ArchiveBatch)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ArchiveBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecoveryCheckpoint>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.CreatedAtUtc });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.EntityName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.MigrationVersion).HasMaxLength(100);
            entity.Property(x => x.SchedulerStateJson);
            entity.Property(x => x.WorkspaceMetadataJson);
            entity.Property(x => x.AuditSessionMetadataJson);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<RecoveryOperation>(entity =>
        {
            entity.HasIndex(x => x.RecoveryCheckpointId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.OperationType, x.CreatedAtUtc });
            entity.Property(x => x.OperationType).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasOne(x => x.RecoveryCheckpoint)
                .WithMany()
                .HasForeignKey(x => x.RecoveryCheckpointId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<ArchiveExportManifest>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.CreatedAtUtc });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.EntityName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.FilePath).HasMaxLength(1000);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<WorkflowExecution>(entity =>
        {
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.WorkflowType, x.WorkflowKey, x.StartedAtUtc });
            entity.HasIndex(x => new { x.WorkflowType, x.WorkflowKey, x.Status });
            entity.HasIndex(x => x.LastHeartbeatUtc);
            entity.HasIndex(x => x.ParentWorkflowExecutionId);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.WorkflowKey).HasMaxLength(160);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(160);
            entity.Property(x => x.ExecutionHash).HasMaxLength(64);
            entity.Property(x => x.PayloadHash).HasMaxLength(64);
            entity.Property(x => x.RecoveryStateJson);
            entity.Property(x => x.CompensationStateJson);
            entity.Property(x => x.ExceptionType).HasMaxLength(250);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.UserName).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.ParentWorkflowExecution)
                .WithMany(x => x.ChildWorkflowExecutions)
                .HasForeignKey(x => x.ParentWorkflowExecutionId)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<WorkflowExecutionStep>(entity =>
        {
            entity.HasIndex(x => x.WorkflowExecutionId);
            entity.HasIndex(x => new { x.WorkflowExecutionId, x.StepName, x.Status });
            entity.HasIndex(x => x.LastHeartbeatUtc);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.StepName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ExceptionType).HasMaxLength(250);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany(x => x.Steps).HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<WorkflowCompensationAction>(entity =>
        {
            entity.HasIndex(x => x.WorkflowExecutionId);
            entity.HasIndex(x => x.WorkflowExecutionStepId);
            entity.HasIndex(x => new { x.WorkflowExecutionId, x.StepName, x.Status });
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.StepName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.CompensationStateJson);
            entity.Property(x => x.ExceptionType).HasMaxLength(250);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany().HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.WorkflowExecutionStep).WithMany().HasForeignKey(x => x.WorkflowExecutionStepId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<WorkflowRetryHistory>(entity =>
        {
            entity.HasIndex(x => x.WorkflowExecutionId);
            entity.HasIndex(x => x.WorkflowExecutionStepId);
            entity.HasIndex(x => new { x.WorkflowType, x.RetryCount, x.StartedAtUtc });
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.StepName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ExceptionType).HasMaxLength(250);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany().HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.WorkflowExecutionStep).WithMany().HasForeignKey(x => x.WorkflowExecutionStepId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<WorkflowConsistencyViolation>(entity =>
        {
            entity.HasIndex(x => x.WorkflowExecutionId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.WorkflowType, x.RecordedAtUtc });
            entity.HasIndex(x => new { x.Category, x.Severity });
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.WorkflowKey).HasMaxLength(160);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.Recommendation).HasMaxLength(600);
            entity.Property(x => x.AffectedEntityModule).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany().HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<DistributedOperationMarker>(entity =>
        {
            entity.HasIndex(x => x.WorkflowExecutionId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.WorkflowType, x.WorkflowKey, x.StepName });
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.WorkflowKey).HasMaxLength(160);
            entity.Property(x => x.StepName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.UserName).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany().HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<IdempotencyKeyEntry>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.WorkflowType, x.WorkflowKey, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.WorkflowType, x.WorkflowKey, x.Status });
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.WorkflowKey).HasMaxLength(160);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(160);
            entity.Property(x => x.ExecutionHash).HasMaxLength(64);
            entity.Property(x => x.PayloadHash).HasMaxLength(64);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<WorkflowLease>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.WorkflowType, x.WorkflowKey, x.LeaseKey }).IsUnique();
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.WorkflowKey).HasMaxLength(160);
            entity.Property(x => x.LeaseKey).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.HolderMachineName).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany().HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<WorkflowDistributedLock>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.LockScope, x.LockKey }).IsUnique();
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.WorkflowKey).HasMaxLength(160);
            entity.Property(x => x.LockScope).HasMaxLength(120);
            entity.Property(x => x.LockKey).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.HolderMachineName).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany().HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<WorkflowDeadLetter>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.WorkflowType, x.WorkflowKey, x.DeadLetteredAtUtc });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.WorkflowKey).HasMaxLength(160);
            entity.Property(x => x.DeadLetterReason).HasMaxLength(400);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ExceptionType).HasMaxLength(250);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.PayloadJson);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany().HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<WorkflowRecoveryCheckpoint>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.WorkflowType, x.WorkflowKey, x.CreatedAtUtc });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.WorkflowKey).HasMaxLength(160);
            entity.Property(x => x.CheckpointName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.SnapshotJson);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany().HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<WorkflowExecutionMetric>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.WorkflowType, x.RecordedAtUtc });
            entity.HasIndex(x => x.CorrelationId);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.WorkflowType).HasMaxLength(120);
            entity.Property(x => x.WorkflowKey).HasMaxLength(160);
            entity.Property(x => x.MetricName).HasMaxLength(120);
            entity.Property(x => x.Category).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.WorkflowExecution).WithMany().HasForeignKey(x => x.WorkflowExecutionId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<ApprovalWorkflow>(entity =>
        {
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.SubmittedAtUtc });
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.EntityType).HasMaxLength(120);
            entity.Property(x => x.EntityId).HasMaxLength(160);
            entity.Property(x => x.Title).HasMaxLength(300);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.MetadataJson);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<ApprovalStep>(entity =>
        {
            entity.HasIndex(x => new { x.WorkflowId, x.Level });
            entity.Property(x => x.StepName).HasMaxLength(180);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Comment).HasMaxLength(1000);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.Workflow).WithMany(x => x.Steps).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<ApprovalAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.StepId, x.AssignedUserId });
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ActionComment).HasMaxLength(1000);
            entity.HasOne(x => x.Workflow).WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Step).WithMany(x => x.Assignments).HasForeignKey(x => x.StepId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<ApprovalDelegation>(entity =>
        {
            entity.HasIndex(x => new { x.AssignmentId, x.DelegatedAtUtc });
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<ApprovalEscalation>(entity =>
        {
            entity.HasIndex(x => new { x.WorkflowId, x.StepId, x.EscalatedAtUtc });
            entity.Property(x => x.Reason).HasMaxLength(400);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<ApprovalHistory>(entity =>
        {
            entity.HasIndex(x => new { x.WorkflowId, x.OccurredAtUtc });
            entity.Property(x => x.Action).HasMaxLength(80);
            entity.Property(x => x.Comment).HasMaxLength(1000);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.Workflow).WithMany(x => x.History).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Step).WithMany().HasForeignKey(x => x.StepId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<ApprovalSnapshot>(entity =>
        {
            entity.HasIndex(x => new { x.WorkflowId, x.CapturedAtUtc });
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.SnapshotJson);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
            entity.HasOne(x => x.Workflow).WithMany(x => x.Snapshots).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<ReconciliationSession>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.StartedAtUtc });
            entity.Property(x => x.Module).HasMaxLength(80);
            entity.Property(x => x.SessionCode).HasMaxLength(80);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<ReconciliationMatch>(entity =>
        {
            entity.HasIndex(x => new { x.SessionId, x.SourceType, x.TargetType });
            entity.Property(x => x.SourceType).HasMaxLength(80);
            entity.Property(x => x.SourceId).HasMaxLength(120);
            entity.Property(x => x.TargetType).HasMaxLength(80);
            entity.Property(x => x.TargetId).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.MatchScore).HasPrecision(18, 6);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<ReconciliationRule>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.RuleCode }).IsUnique();
            entity.Property(x => x.RuleCode).HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(180);
            entity.Property(x => x.Module).HasMaxLength(80);
            entity.Property(x => x.DefinitionJson);
        });
        modelBuilder.Entity<ReconciliationSnapshot>(entity =>
        {
            entity.HasIndex(x => new { x.SessionId, x.CapturedAtUtc });
            entity.Property(x => x.SnapshotJson);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
        });
        modelBuilder.Entity<ReconciliationAnomaly>(entity =>
        {
            entity.HasIndex(x => new { x.SessionId, x.Severity, x.DetectedAtUtc });
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.RelatedEntityType).HasMaxLength(120);
            entity.Property(x => x.RelatedEntityId).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<ReportSnapshot>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.ReportCode, x.PeriodCode, x.CapturedAtUtc });
            entity.Property(x => x.ReportCode).HasMaxLength(120);
            entity.Property(x => x.VersionCode).HasMaxLength(60);
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.SnapshotJson);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
        });
        modelBuilder.Entity<ExportExecution>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.ExportCode, x.QueuedAtUtc });
            entity.Property(x => x.ExportCode).HasMaxLength(120);
            entity.Property(x => x.ExportType).HasMaxLength(50);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<ExportRetentionPolicy>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.ExportCode }).IsUnique();
            entity.Property(x => x.ExportCode).HasMaxLength(120);
        });
        modelBuilder.Entity<ExportArtifact>(entity =>
        {
            entity.HasIndex(x => new { x.ExportExecutionId, x.CreatedAtUtc });
            entity.Property(x => x.FilePath).HasMaxLength(1000);
            entity.Property(x => x.FileType).HasMaxLength(20);
            entity.Property(x => x.Hash).HasMaxLength(64);
        });
        modelBuilder.Entity<DataIntegrityCheckpoint>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.CheckpointCode, x.CreatedAtUtc });
            entity.Property(x => x.CheckpointCode).HasMaxLength(120);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
            entity.Property(x => x.SnapshotJson);
        });
        modelBuilder.Entity<DataIntegrityScanResult>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Module, x.ScannedAtUtc });
            entity.Property(x => x.Module).HasMaxLength(80);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.EntityType).HasMaxLength(120);
            entity.Property(x => x.EntityId).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<DataRepairOperation>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.OperationCode, x.RequestedAtUtc });
            entity.Property(x => x.OperationCode).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.RequestedBy).HasMaxLength(120);
            entity.Property(x => x.ResultJson);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<CompliancePackage>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.PackageCode }).IsUnique();
            entity.Property(x => x.PackageCode).HasMaxLength(120);
            entity.Property(x => x.StandardCode).HasMaxLength(20);
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });
        modelBuilder.Entity<ComplianceManifest>(entity =>
        {
            entity.HasIndex(x => new { x.CompliancePackageId, x.ManifestType });
            entity.Property(x => x.ManifestType).HasMaxLength(50);
            entity.Property(x => x.FilePath).HasMaxLength(1000);
            entity.Property(x => x.Hash).HasMaxLength(64);
        });
        modelBuilder.Entity<ComplianceSubmission>(entity =>
        {
            entity.HasIndex(x => new { x.CompliancePackageId, x.CreatedAtUtc });
            entity.Property(x => x.SubmissionChannel).HasMaxLength(80);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ReferenceNo).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<ComplianceArchive>(entity =>
        {
            entity.HasIndex(x => new { x.CompliancePackageId, x.CreatedAtUtc });
            entity.Property(x => x.ArchivePath).HasMaxLength(1000);
            entity.Property(x => x.Hash).HasMaxLength(64);
        });
        modelBuilder.Entity<ProductionCertificationReport>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.PhaseCode, x.GeneratedAtUtc });
            entity.Property(x => x.PhaseCode).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ReportJson);
        });
        modelBuilder.Entity<FinancialTemplate>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.StandardCode, x.TemplateCode }).IsUnique();
            entity.Property(x => x.StandardCode).HasMaxLength(20);
            entity.Property(x => x.TemplateCode).HasMaxLength(120);
            entity.Property(x => x.Name).HasMaxLength(250);
            entity.Property(x => x.StatementType).HasMaxLength(80);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<FinancialTemplateVersion>(entity =>
        {
            entity.HasIndex(x => new { x.FinancialTemplateId, x.VersionCode }).IsUnique();
            entity.Property(x => x.VersionCode).HasMaxLength(60);
            entity.Property(x => x.HierarchyJson);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.FinancialTemplate).WithMany(x => x.Versions).HasForeignKey(x => x.FinancialTemplateId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<FinancialStatementFormula>(entity =>
        {
            entity.HasIndex(x => new { x.FinancialTemplateVersionId, x.LineCode });
            entity.Property(x => x.LineCode).HasMaxLength(120);
            entity.Property(x => x.Expression).HasMaxLength(1200);
            entity.Property(x => x.MetadataJson);
            entity.HasOne(x => x.FinancialTemplateVersion).WithMany(x => x.Formulas).HasForeignKey(x => x.FinancialTemplateVersionId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<FinancialStatementSnapshot>(entity =>
        {
            entity.HasIndex(x => new { x.FinancialTemplateVersionId, x.PeriodCode, x.CapturedAtUtc });
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.SnapshotJson);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasOne(x => x.FinancialTemplateVersion).WithMany().HasForeignKey(x => x.FinancialTemplateVersionId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<EnterpriseConsistencyViolation>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Category, x.RecordedAtUtc });
            entity.HasIndex(x => new { x.Module, x.EntityType });
            entity.Property(x => x.Category).HasMaxLength(120);
            entity.Property(x => x.Severity).HasMaxLength(30);
            entity.Property(x => x.Module).HasMaxLength(120);
            entity.Property(x => x.EntityType).HasMaxLength(120);
            entity.Property(x => x.EntityId).HasMaxLength(120);
            entity.Property(x => x.Recommendation).HasMaxLength(600);
            entity.Property(x => x.MetadataJson);
        });
        modelBuilder.Entity<BusinessDocument>().HasIndex(x => x.VirtualPath);
        modelBuilder.Entity<BusinessDocument>().HasIndex(x => new { x.EntityType, x.EntityId });
        modelBuilder.Entity<BusinessDocument>().HasIndex(x => x.DocumentDate);
        modelBuilder.Entity<BusinessDocument>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Supplier>().HasIndex(x => x.SupplierName);
        modelBuilder.Entity<Customer>().HasIndex(x => x.CustomerName);
        modelBuilder.Entity<VirtualDrive>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<VirtualFolder>().HasIndex(x => x.VirtualPath).IsUnique();
        modelBuilder.Entity<DocumentLink>().HasIndex(x => x.DocumentId);
        modelBuilder.Entity<DocumentLink>().HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });
        modelBuilder.Entity<LedgerAccount>().HasIndex(x => x.AccountCode).IsUnique();
        modelBuilder.Entity<LedgerAccount>().Property(x => x.AccountCode).HasMaxLength(50);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.AccountName).HasMaxLength(255);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.ReportName).HasMaxLength(255);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.ParentAccountCode).HasMaxLength(50);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.AccountKind).HasMaxLength(10);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.AccountNature).HasMaxLength(10);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.AccountType).HasMaxLength(40);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.AccountingStandardCode).HasMaxLength(20).HasDefaultValue("TT99");
        modelBuilder.Entity<LedgerAccount>().Property(x => x.AllowPosting).HasDefaultValue(true);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.IsSystemAccount).HasDefaultValue(false);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.IsEditable).HasDefaultValue(true);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.IsDeletable).HasDefaultValue(true);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.NormalBalance).HasMaxLength(20).HasDefaultValue("Debit");
        modelBuilder.Entity<LedgerAccount>().Property(x => x.AccountCategory).HasMaxLength(50);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.RequireCustomer).HasDefaultValue(false);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.RequireSupplier).HasDefaultValue(false);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.RequireWarehouse).HasDefaultValue(false);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.RequireItem).HasDefaultValue(false);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.RequireDepartment).HasDefaultValue(false);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.RequireEmployee).HasDefaultValue(false);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.RequireCurrency).HasDefaultValue(false);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.IsFinancialAccount).HasDefaultValue(true);
        modelBuilder.Entity<LedgerAccount>().Property(x => x.IsManagementAccount).HasDefaultValue(false);
        modelBuilder.Entity<LedgerAccount>().HasIndex(x => x.AccountingStandardCode);
        modelBuilder.Entity<LedgerAccount>().HasIndex(x => x.AccountCategory);
        modelBuilder.Entity<LedgerAccount>().HasIndex(x => x.AllowPosting);

        modelBuilder.Entity<AccountingStandard>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(20);
            entity.Property(x => x.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<AccountingPeriod>(entity =>
        {
            entity.HasIndex(x => x.PeriodCode);
            entity.HasIndex(x => new { x.CompanyId, x.PeriodCode }).IsUnique().HasFilter("[CompanyId] IS NOT NULL");
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.LockedBy).HasMaxLength(255);
            entity.Property(x => x.Status).HasConversion<int>();
        });

        modelBuilder.Entity<FiscalYear>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.Year }).IsUnique().HasFilter("[CompanyId] IS NOT NULL");
            entity.HasIndex(x => x.FiscalYearCode);
            entity.Property(x => x.FiscalYearCode).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.ClosedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<PeriodLock>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.PeriodCode }).IsUnique().HasFilter("[CompanyId] IS NOT NULL");
            entity.HasIndex(x => new { x.CompanyId, x.PeriodCode, x.IsLocked });
            entity.HasIndex(x => x.BranchId);
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.LockedBy).HasMaxLength(255);
            entity.Property(x => x.UnlockedBy).HasMaxLength(255);
            entity.Property(x => x.LockReason).HasMaxLength(1000);
            entity.Property(x => x.UnlockReason).HasMaxLength(1000);
        });

        modelBuilder.Entity<CompanyBranchPermission>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.CompanyId, x.BranchId, x.PermissionCode }).IsUnique().HasFilter("[CompanyId] IS NOT NULL");
            entity.HasIndex(x => new { x.CompanyId, x.BranchId });
            entity.Property(x => x.PermissionCode).HasMaxLength(120);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<JournalHeader>(entity =>
        {
            entity.HasIndex(x => x.JournalNo).IsUnique();
            entity.HasIndex(x => x.PeriodCode);
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.BranchId);
            entity.HasIndex(x => x.JournalDate);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.SourceType);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.PostingDate });
            entity.Property(x => x.JournalNo).HasMaxLength(50);
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.SourceNo).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.PeriodCode).HasMaxLength(20);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.TotalDebit).HasPrecision(18, 2);
            entity.Property(x => x.TotalCredit).HasPrecision(18, 2);
            entity.Property(x => x.Difference).HasPrecision(18, 2);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.Property(x => x.PostedBy).HasMaxLength(255);
            entity.Property(x => x.ReversedBy).HasMaxLength(255);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasMany(x => x.Lines)
                .WithOne(x => x.JournalHeader)
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JournalLine>(entity =>
        {
            entity.HasIndex(x => x.JournalHeaderId);
            entity.HasIndex(x => x.AccountCode);
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.BranchId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.AccountCode });
            entity.Property(x => x.AccountCode).HasMaxLength(50);
            entity.Property(x => x.AccountName).HasMaxLength(255);
            entity.Property(x => x.DebitAmount).HasPrecision(18, 2);
            entity.Property(x => x.CreditAmount).HasPrecision(18, 2);
            entity.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("VND");
            entity.Property(x => x.ExchangeRate).HasPrecision(18, 6).HasDefaultValue(1m);
            entity.Property(x => x.ObjectType).HasMaxLength(50);
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<PostingBatch>(entity =>
        {
            entity.HasIndex(x => x.BatchNo).IsUnique();
            entity.Property(x => x.BatchNo).HasMaxLength(50);
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.Property(x => x.Message).HasMaxLength(2000);
        });

        modelBuilder.Entity<PostingSource>(entity =>
        {
            entity.HasIndex(x => new { x.SourceType, x.SourceId }).IsUnique();
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.SourceNo).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PostingRule>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.SourceType, x.AccountingStandardCode });
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.AccountingStandardCode).HasMaxLength(20);
            entity.HasMany(x => x.Lines)
                .WithOne(x => x.PostingRule)
                .HasForeignKey(x => x.PostingRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostingRuleLine>(entity =>
        {
            entity.HasIndex(x => x.PostingRuleId);
            entity.Property(x => x.DebitCredit).HasConversion<int>();
            entity.Property(x => x.AccountRole).HasMaxLength(50);
            entity.Property(x => x.FixedAccountCode).HasMaxLength(50);
            entity.Property(x => x.Formula).HasMaxLength(500);
            entity.Property(x => x.DescriptionTemplate).HasMaxLength(500);
        });

        modelBuilder.Entity<AccountRoleMapping>(entity =>
        {
            entity.HasIndex(x => new { x.AccountingStandardCode, x.RoleCode, x.AccountCode }).IsUnique();
            entity.HasIndex(x => new { x.AccountingStandardCode, x.RoleCode, x.IsDefault });
            entity.Property(x => x.AccountingStandardCode).HasMaxLength(20);
            entity.Property(x => x.RoleCode).HasMaxLength(50);
            entity.Property(x => x.AccountCode).HasMaxLength(50);
        });

        modelBuilder.Entity<AccountTagMapping>(entity =>
        {
            entity.HasIndex(x => new { x.AccountCode, x.TagCode }).IsUnique();
            entity.Property(x => x.AccountCode).HasMaxLength(50);
            entity.Property(x => x.TagCode).HasMaxLength(50);
        });

        modelBuilder.Entity<ReceivableTransaction>(entity =>
        {
            entity.HasIndex(x => x.CustomerId);
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            entity.HasIndex(x => x.JournalHeaderId);
            entity.HasIndex(x => x.JournalLineId).IsUnique().HasFilter("[JournalLineId] IS NOT NULL");
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.BranchId);
            entity.HasIndex(x => x.SourceType);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.DocumentDate });
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.DocumentNo).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DebitAmount).HasPrecision(18, 2);
            entity.Property(x => x.CreditAmount).HasPrecision(18, 2);
            entity.Property(x => x.BalanceAmount).HasPrecision(18, 2);
            entity.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("VND");
            entity.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("Open");
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<JournalLine>()
                .WithMany()
                .HasForeignKey(x => x.JournalLineId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PayableTransaction>(entity =>
        {
            entity.HasIndex(x => x.SupplierId);
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            entity.HasIndex(x => x.JournalHeaderId);
            entity.HasIndex(x => x.JournalLineId).IsUnique().HasFilter("[JournalLineId] IS NOT NULL");
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.BranchId);
            entity.HasIndex(x => x.SourceType);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.DocumentDate });
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.DocumentNo).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DebitAmount).HasPrecision(18, 2);
            entity.Property(x => x.CreditAmount).HasPrecision(18, 2);
            entity.Property(x => x.BalanceAmount).HasPrecision(18, 2);
            entity.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("VND");
            entity.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("Open");
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<JournalLine>()
                .WithMany()
                .HasForeignKey(x => x.JournalLineId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PaymentAllocation>(entity =>
        {
            entity.HasIndex(x => new { x.TargetType, x.TargetTransactionId });
            entity.HasIndex(x => x.PaymentJournalHeaderId);
            entity.Property(x => x.PaymentSourceType).HasMaxLength(50);
            entity.Property(x => x.TargetType).HasMaxLength(20);
            entity.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.PaymentJournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DebtReconciliation>(entity =>
        {
            entity.HasIndex(x => x.ReconciliationNo).IsUnique();
            entity.HasIndex(x => new { x.PartnerType, x.PartnerId });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.ReconciliationNo).HasMaxLength(50);
            entity.Property(x => x.PartnerType).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.TotalDebit).HasPrecision(18, 2);
            entity.Property(x => x.TotalCredit).HasPrecision(18, 2);
            entity.Property(x => x.Balance).HasPrecision(18, 2);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.Property(x => x.ConfirmedBy).HasMaxLength(255);
            entity.HasMany(x => x.Lines)
                .WithOne(x => x.DebtReconciliation)
                .HasForeignKey(x => x.DebtReconciliationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DebtReconciliationLine>(entity =>
        {
            entity.HasIndex(x => x.DebtReconciliationId);
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.DocumentNo).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DebitAmount).HasPrecision(18, 2);
            entity.Property(x => x.CreditAmount).HasPrecision(18, 2);
            entity.Property(x => x.BalanceAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<AgingSnapshot>(entity =>
        {
            entity.HasIndex(x => x.SnapshotDate);
            entity.HasIndex(x => new { x.PartnerType, x.PartnerId });
            entity.Property(x => x.PartnerType).HasMaxLength(20);
            entity.Property(x => x.CurrentAmount).HasPrecision(18, 2);
            entity.Property(x => x.Overdue1To30).HasPrecision(18, 2);
            entity.Property(x => x.Overdue31To60).HasPrecision(18, 2);
            entity.Property(x => x.Overdue61To90).HasPrecision(18, 2);
            entity.Property(x => x.OverdueOver90).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ImportTemplate>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.TargetEntity);
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.TargetEntity).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<ImportSession>(entity =>
        {
            entity.HasIndex(x => x.TemplateCode);
            entity.HasIndex(x => x.TargetEntity);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.TemplateCode).HasMaxLength(100);
            entity.Property(x => x.FileName).HasMaxLength(255);
            entity.Property(x => x.OriginalFilePath).HasMaxLength(1000);
            entity.Property(x => x.TempFilePath).HasMaxLength(1000);
            entity.Property(x => x.TargetEntity).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasMany(x => x.Rows)
                .WithOne(x => x.ImportSession)
                .HasForeignKey(x => x.ImportSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportRow>(entity =>
        {
            entity.HasIndex(x => x.ImportSessionId);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.RawJson);
            entity.Property(x => x.NormalizedJson);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasMany(x => x.CellErrors)
                .WithOne(x => x.ImportRow)
                .HasForeignKey(x => x.ImportRowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportCellError>(entity =>
        {
            entity.HasIndex(x => x.ImportRowId);
            entity.HasIndex(x => x.Severity);
            entity.Property(x => x.ColumnName).HasMaxLength(100);
            entity.Property(x => x.CellAddress).HasMaxLength(20);
            entity.Property(x => x.Severity).HasMaxLength(20);
            entity.Property(x => x.ErrorCode).HasMaxLength(100);
            entity.Property(x => x.Message).HasMaxLength(500);
            entity.Property(x => x.SuggestedValue).HasMaxLength(500);
        });

        modelBuilder.Entity<InventoryCostLayer>(entity =>
        {
            entity.HasIndex(x => new { x.ItemId, x.WarehouseId });
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            entity.HasIndex(x => x.SourceDetailId);
            entity.HasIndex(x => x.DocumentDate);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.BranchId);
            entity.HasIndex(x => x.SourceType);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.DocumentDate });
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.DocumentNo).HasMaxLength(100);
            entity.Property(x => x.QuantityIn).HasPrecision(18, 4);
            entity.Property(x => x.QuantityOut).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 6);
            entity.Property(x => x.TotalCost).HasPrecision(18, 2);
            entity.Property(x => x.RemainingQuantity).HasPrecision(18, 4);
            entity.Property(x => x.RemainingCost).HasPrecision(18, 2);
            entity.Property(x => x.CostMethod).HasMaxLength(30);
            entity.Property(x => x.Status).HasMaxLength(30);
        });

        modelBuilder.Entity<InventoryCostTransaction>(entity =>
        {
            entity.HasIndex(x => new { x.ItemId, x.WarehouseId });
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            entity.HasIndex(x => x.SourceDetailId);
            entity.HasIndex(x => x.DocumentDate);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.BranchId);
            entity.HasIndex(x => x.SourceType);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.DocumentDate });
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.DocumentNo).HasMaxLength(100);
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 6);
            entity.Property(x => x.TotalCost).HasPrecision(18, 2);
            entity.Property(x => x.CostMethod).HasMaxLength(30);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ReportTemplate>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Category);
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.Category).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DataSource).HasMaxLength(100);
            entity.HasMany(x => x.Definitions)
                .WithOne(x => x.ReportTemplate)
                .HasForeignKey(x => x.ReportTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Parameters)
                .WithOne(x => x.ReportTemplate)
                .HasForeignKey(x => x.ReportTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReportDefinition>(entity =>
        {
            entity.HasIndex(x => x.ReportTemplateId);
            entity.HasIndex(x => x.IsPublished);
            entity.Property(x => x.DefinitionJson);
        });

        modelBuilder.Entity<ReportParameter>(entity =>
        {
            entity.HasIndex(x => x.ReportTemplateId);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.DisplayName).HasMaxLength(255);
            entity.Property(x => x.DataType).HasMaxLength(50);
            entity.Property(x => x.DefaultValue).HasMaxLength(500);
        });

        modelBuilder.Entity<ReportExecution>(entity =>
        {
            entity.HasIndex(x => x.ReportCode);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.BranchId);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.ReportCode, x.StartedAt });
            entity.Property(x => x.ReportCode).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.ExecutedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<TaxCode>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.TaxType);
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.TaxRate).HasPrecision(9, 4);
            entity.Property(x => x.TaxType).HasMaxLength(30);
            entity.Property(x => x.InputAccountCode).HasMaxLength(50);
            entity.Property(x => x.OutputAccountCode).HasMaxLength(50);
            entity.HasMany(x => x.VatTransactions)
                .WithOne(x => x.TaxCode)
                .HasForeignKey(x => x.TaxCodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VatTransaction>(entity =>
        {
            entity.HasIndex(x => x.TaxCodeId);
            entity.HasIndex(x => x.DeclarationPeriod);
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            entity.HasIndex(x => x.JournalHeaderId);
            entity.HasIndex(x => x.JournalLineId).IsUnique().HasFilter("[JournalLineId] IS NOT NULL");
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.BranchId);
            entity.HasIndex(x => x.SourceType);
            entity.HasIndex(x => x.Direction);
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.InvoiceNo).HasMaxLength(100);
            entity.Property(x => x.PartnerType).HasMaxLength(30);
            entity.Property(x => x.TaxBaseAmount).HasPrecision(18, 2);
            entity.Property(x => x.TaxRate).HasPrecision(9, 4);
            entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
            entity.Property(x => x.Direction).HasMaxLength(20);
            entity.Property(x => x.DeclarationPeriod).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<JournalLine>()
                .WithMany()
                .HasForeignKey(x => x.JournalLineId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TaxDeclarationDraft>(entity =>
        {
            entity.HasIndex(x => new { x.DeclarationPeriod, x.TaxType }).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.DeclarationPeriod).HasMaxLength(20);
            entity.Property(x => x.TaxType).HasMaxLength(30);
            entity.Property(x => x.InputVatAmount).HasPrecision(18, 2);
            entity.Property(x => x.OutputVatAmount).HasPrecision(18, 2);
            entity.Property(x => x.NetVatPayable).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(x => x.InvoiceNo).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.ProviderCode);
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.InvoiceDate });
            entity.Property(x => x.InvoiceNo).HasMaxLength(100);
            entity.Property(x => x.InvoiceSeries).HasMaxLength(50);
            entity.Property(x => x.TemplateCode).HasMaxLength(50);
            entity.Property(x => x.BuyerName).HasMaxLength(255);
            entity.Property(x => x.BuyerTaxCode).HasMaxLength(50);
            entity.Property(x => x.BuyerAddress).HasMaxLength(500);
            entity.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("VND");
            entity.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            entity.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.ProviderCode).HasMaxLength(30);
            entity.Property(x => x.ProviderInvoiceId).HasMaxLength(120);
            entity.HasMany(x => x.Lines).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.TaxBreakdowns).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.IssueLogs).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<JournalHeader>().WithMany().HasForeignKey(x => x.JournalHeaderId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ReceivableTransaction>().WithMany().HasForeignKey(x => x.ReceivableTransactionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<PayableTransaction>().WithMany().HasForeignKey(x => x.PayableTransactionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TaxDeclarationDraft>().WithMany().HasForeignKey(x => x.VatDeclarationDraftId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.HasIndex(x => x.InvoiceId);
            entity.HasIndex(x => x.ItemCode);
            entity.Property(x => x.ItemCode).HasMaxLength(100);
            entity.Property(x => x.ItemName).HasMaxLength(255);
            entity.Property(x => x.UnitName).HasMaxLength(50);
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 6);
            entity.Property(x => x.LineAmount).HasPrecision(18, 2);
            entity.Property(x => x.TaxRate).HasPrecision(9, 4);
            entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
            entity.Property(x => x.JournalAccountCode).HasMaxLength(50);
        });

        modelBuilder.Entity<InvoiceTaxBreakdown>(entity =>
        {
            entity.HasIndex(x => x.InvoiceId);
            entity.Property(x => x.TaxRate).HasPrecision(9, 4);
            entity.Property(x => x.TaxBaseAmount).HasPrecision(18, 2);
            entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
            entity.Property(x => x.TaxCode).HasMaxLength(50);
        });

        modelBuilder.Entity<InvoiceIssueLog>(entity =>
        {
            entity.HasIndex(x => x.InvoiceId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CorrelationId);
            entity.Property(x => x.ProviderCode).HasMaxLength(30);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.CorrelationId).HasMaxLength(64);
        });

        modelBuilder.Entity<InvoiceProviderConfiguration>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId, x.ProviderCode }).IsUnique().HasFilter("[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");
            entity.HasIndex(x => x.ProviderCode);
            entity.Property(x => x.ProviderCode).HasMaxLength(30);
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.EndpointUrl).HasMaxLength(500);
            entity.Property(x => x.TenantCode).HasMaxLength(100);
            entity.Property(x => x.Username).HasMaxLength(120);
            entity.Property(x => x.ProtectedSecret).HasMaxLength(1000);
        });

        modelBuilder.Entity<AccountingStandardTemplate>(entity =>
        {
            entity.HasIndex(x => x.StandardCode);
            entity.HasIndex(x => x.TemplateType);
            entity.HasIndex(x => x.TemplateCode).IsUnique();
            entity.Property(x => x.StandardCode).HasMaxLength(20);
            entity.Property(x => x.TemplateType).HasMaxLength(50);
            entity.Property(x => x.TemplateCode).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<AccountTemplate>(entity =>
        {
            entity.HasIndex(x => x.StandardCode);
            entity.HasIndex(x => new { x.StandardCode, x.AccountCode }).IsUnique();
            entity.Property(x => x.StandardCode).HasMaxLength(20);
            entity.Property(x => x.AccountCode).HasMaxLength(50);
            entity.Property(x => x.AccountName).HasMaxLength(255);
            entity.Property(x => x.ParentCode).HasMaxLength(50);
            entity.Property(x => x.AccountKind).HasMaxLength(10);
            entity.Property(x => x.NormalBalance).HasMaxLength(20);
            entity.Property(x => x.AccountCategory).HasMaxLength(50);
        });

        modelBuilder.Entity<FinancialStatementMapping>(entity =>
        {
            entity.HasIndex(x => x.StandardCode);
            entity.HasIndex(x => x.ReportCode);
            entity.HasIndex(x => x.LineCode);
            entity.HasIndex(x => new { x.StandardCode, x.ReportCode, x.LineCode }).IsUnique();
            entity.Property(x => x.StandardCode).HasMaxLength(20);
            entity.Property(x => x.ReportCode).HasMaxLength(100);
            entity.Property(x => x.LineCode).HasMaxLength(100);
            entity.Property(x => x.LineName).HasMaxLength(255);
            entity.Property(x => x.AccountFilter).HasMaxLength(500);
            entity.Property(x => x.AccountRoleFilter).HasMaxLength(500);
            entity.Property(x => x.Formula).HasMaxLength(1000);
        });

        modelBuilder.Entity<CompanyAccountingProfile>(entity =>
        {
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => x.AccountingStandardCode);
            entity.Property(x => x.CompanyName).HasMaxLength(255);
            entity.Property(x => x.AccountingStandardCode).HasMaxLength(20);
        });

        modelBuilder.Entity<AccountBalanceSnapshot>(entity =>
        {
            entity.HasIndex(x => x.FiscalYear);
            entity.HasIndex(x => x.Period);
            entity.HasIndex(x => x.AccountCode);
            entity.HasIndex(x => new { x.Period, x.AccountCode }).IsUnique();
            entity.Property(x => x.Period).HasMaxLength(20);
            entity.Property(x => x.AccountCode).HasMaxLength(50);
            entity.Property(x => x.OpeningDebit).HasPrecision(18, 2);
            entity.Property(x => x.OpeningCredit).HasPrecision(18, 2);
            entity.Property(x => x.PeriodDebit).HasPrecision(18, 2);
            entity.Property(x => x.PeriodCredit).HasPrecision(18, 2);
            entity.Property(x => x.ClosingDebit).HasPrecision(18, 2);
            entity.Property(x => x.ClosingCredit).HasPrecision(18, 2);
        });

        modelBuilder.Entity<FinancialStatementExecution>(entity =>
        {
            entity.HasIndex(x => x.FiscalYear);
            entity.HasIndex(x => x.Period);
            entity.HasIndex(x => x.StatementType);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.StatementType).HasMaxLength(50);
            entity.Property(x => x.Period).HasMaxLength(20);
            entity.Property(x => x.GeneratedBy).HasMaxLength(255);
            entity.Property(x => x.Status).HasMaxLength(30);
        });

        modelBuilder.Entity<ClosingEntry>(entity =>
        {
            entity.HasIndex(x => x.FiscalYear);
            entity.HasIndex(x => x.Period);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.Period).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasMany(x => x.Lines)
                .WithOne(x => x.ClosingEntry)
                .HasForeignKey(x => x.ClosingEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ClosingEntryLine>(entity =>
        {
            entity.HasIndex(x => x.ClosingEntryId);
            entity.HasIndex(x => x.AccountCode);
            entity.Property(x => x.AccountCode).HasMaxLength(50);
            entity.Property(x => x.DebitAmount).HasPrecision(18, 2);
            entity.Property(x => x.CreditAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CashFund>(entity =>
        {
            entity.HasIndex(x => x.FundCode).IsUnique();
            entity.HasIndex(x => x.IsDefault);
            entity.Property(x => x.FundCode).HasMaxLength(50);
            entity.Property(x => x.FundName).HasMaxLength(255);
            entity.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("VND");
            entity.Property(x => x.AccountCode).HasMaxLength(50);
            entity.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            entity.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            entity.HasMany(x => x.Transactions)
                .WithOne(x => x.CashFund)
                .HasForeignKey(x => x.CashFundId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Counts)
                .WithOne(x => x.CashFund)
                .HasForeignKey(x => x.CashFundId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CashTransaction>(entity =>
        {
            entity.HasIndex(x => x.TransactionNo).IsUnique();
            entity.HasIndex(x => x.CashFundId);
            entity.HasIndex(x => x.TransactionDate);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            entity.Property(x => x.TransactionNo).HasMaxLength(50);
            entity.Property(x => x.TransactionType).HasMaxLength(30);
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.PartnerType).HasMaxLength(30);
            entity.Property(x => x.CounterAccountCode).HasMaxLength(50);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DebitAmount).HasPrecision(18, 2);
            entity.Property(x => x.CreditAmount).HasPrecision(18, 2);
            entity.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.Property(x => x.PostedBy).HasMaxLength(255);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CashCount>(entity =>
        {
            entity.HasIndex(x => x.CountNo).IsUnique();
            entity.HasIndex(x => x.CashFundId);
            entity.HasIndex(x => x.CountDate);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.CountNo).HasMaxLength(50);
            entity.Property(x => x.BookBalance).HasPrecision(18, 2);
            entity.Property(x => x.ActualBalance).HasPrecision(18, 2);
            entity.Property(x => x.Difference).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.Property(x => x.ConfirmedBy).HasMaxLength(255);
            entity.HasMany(x => x.Lines)
                .WithOne(x => x.CashCount)
                .HasForeignKey(x => x.CashCountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CashCountLine>(entity =>
        {
            entity.HasIndex(x => x.CashCountId);
            entity.Property(x => x.Denomination).HasPrecision(18, 2);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasIndex(x => x.AccountNumber).IsUnique();
            entity.HasIndex(x => x.BankCode);
            entity.HasIndex(x => x.IsDefault);
            entity.Property(x => x.BankCode).HasMaxLength(50);
            entity.Property(x => x.BankName).HasMaxLength(255);
            entity.Property(x => x.BranchName).HasMaxLength(255);
            entity.Property(x => x.AccountNumber).HasMaxLength(100);
            entity.Property(x => x.AccountName).HasMaxLength(255);
            entity.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("VND");
            entity.Property(x => x.AccountCode).HasMaxLength(50);
            entity.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            entity.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            entity.HasMany(x => x.Transactions)
                .WithOne(x => x.BankAccount)
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Statements)
                .WithOne(x => x.BankAccount)
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BankTransaction>(entity =>
        {
            entity.HasIndex(x => x.TransactionNo).IsUnique();
            entity.HasIndex(x => x.BankAccountId);
            entity.HasIndex(x => x.TransactionDate);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            entity.Property(x => x.TransactionNo).HasMaxLength(50);
            entity.Property(x => x.TransactionType).HasMaxLength(30);
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.ReferenceNo).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DebitAmount).HasPrecision(18, 2);
            entity.Property(x => x.CreditAmount).HasPrecision(18, 2);
            entity.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            entity.Property(x => x.CounterAccountCode).HasMaxLength(50);
            entity.Property(x => x.PartnerType).HasMaxLength(30);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.Property(x => x.PostedBy).HasMaxLength(255);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BankStatement>(entity =>
        {
            entity.HasIndex(x => x.StatementNo).IsUnique();
            entity.HasIndex(x => x.BankAccountId);
            entity.HasIndex(x => new { x.FromDate, x.ToDate });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.StatementNo).HasMaxLength(50);
            entity.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            entity.Property(x => x.ClosingBalance).HasPrecision(18, 2);
            entity.Property(x => x.ImportedBy).HasMaxLength(255);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasMany(x => x.Lines)
                .WithOne(x => x.BankStatement)
                .HasForeignKey(x => x.BankStatementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BankStatementLine>(entity =>
        {
            entity.HasIndex(x => x.BankStatementId);
            entity.HasIndex(x => x.TransactionDate);
            entity.HasIndex(x => x.MatchStatus);
            entity.HasIndex(x => x.MatchedBankTransactionId);
            entity.Property(x => x.ReferenceNo).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DebitAmount).HasPrecision(18, 2);
            entity.Property(x => x.CreditAmount).HasPrecision(18, 2);
            entity.Property(x => x.Balance).HasPrecision(18, 2);
            entity.Property(x => x.MatchStatus).HasMaxLength(30);
            entity.HasOne<BankTransaction>()
                .WithMany()
                .HasForeignKey(x => x.MatchedBankTransactionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BankReconciliation>(entity =>
        {
            entity.HasIndex(x => x.ReconciliationNo).IsUnique();
            entity.HasIndex(x => x.BankAccountId);
            entity.HasIndex(x => x.ReconciliationDate);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.ReconciliationNo).HasMaxLength(50);
            entity.Property(x => x.BookBalance).HasPrecision(18, 2);
            entity.Property(x => x.StatementBalance).HasPrecision(18, 2);
            entity.Property(x => x.Difference).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.CreatedBy).HasMaxLength(255);
            entity.Property(x => x.ConfirmedBy).HasMaxLength(255);
            entity.HasMany(x => x.Lines)
                .WithOne(x => x.BankReconciliation)
                .HasForeignKey(x => x.BankReconciliationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BankReconciliationLine>(entity =>
        {
            entity.HasIndex(x => x.BankReconciliationId);
            entity.HasIndex(x => x.BankTransactionId);
            entity.HasIndex(x => x.BankStatementLineId);
            entity.Property(x => x.DifferenceAmount).HasPrecision(18, 2);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne<BankTransaction>()
                .WithMany()
                .HasForeignKey(x => x.BankTransactionId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<BankStatementLine>()
                .WithMany()
                .HasForeignKey(x => x.BankStatementLineId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<FixedAssetCategory>(entity =>
        {
            entity.HasIndex(x => x.CategoryCode).IsUnique();
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.CategoryCode).HasMaxLength(50);
            entity.Property(x => x.CategoryName).HasMaxLength(255);
            entity.Property(x => x.AssetAccountCode).HasMaxLength(50);
            entity.Property(x => x.DepreciationAccountCode).HasMaxLength(50);
            entity.Property(x => x.ExpenseAccountCode).HasMaxLength(50);
            entity.Property(x => x.DepreciationMethodDefault).HasMaxLength(50).HasDefaultValue("StraightLine");
            entity.HasMany(x => x.Assets)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FixedAsset>(entity =>
        {
            entity.HasIndex(x => x.AssetCode).IsUnique();
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.PurchaseDate);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.AssetCode).HasMaxLength(50);
            entity.Property(x => x.AssetName).HasMaxLength(255);
            entity.Property(x => x.SerialNumber).HasMaxLength(255);
            entity.Property(x => x.DepreciationMethod).HasMaxLength(50).HasDefaultValue("StraightLine");
            entity.Property(x => x.OriginalCost).HasPrecision(18, 2);
            entity.Property(x => x.SalvageValue).HasPrecision(18, 2);
            entity.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 2);
            entity.Property(x => x.RemainingValue).HasPrecision(18, 2);
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(x => x.Depreciations)
                .WithOne(x => x.FixedAsset)
                .HasForeignKey(x => x.FixedAssetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Adjustments)
                .WithOne(x => x.FixedAsset)
                .HasForeignKey(x => x.FixedAssetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Disposals)
                .WithOne(x => x.FixedAsset)
                .HasForeignKey(x => x.FixedAssetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FixedAssetDepreciation>(entity =>
        {
            entity.HasIndex(x => new { x.FixedAssetId, x.DepreciationPeriod }).IsUnique();
            entity.HasIndex(x => x.DepreciationPeriod);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.DepreciationPeriod).HasMaxLength(20);
            entity.Property(x => x.DepreciationAmount).HasPrecision(18, 2);
            entity.Property(x => x.AccumulatedAfter).HasPrecision(18, 2);
            entity.Property(x => x.RemainingValueAfter).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<FixedAssetAdjustment>(entity =>
        {
            entity.HasIndex(x => x.FixedAssetId);
            entity.HasIndex(x => x.AdjustmentDate);
            entity.HasIndex(x => x.AdjustmentType);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.AdjustmentType).HasMaxLength(30);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<FixedAssetDisposal>(entity =>
        {
            entity.HasIndex(x => x.FixedAssetId);
            entity.HasIndex(x => x.DisposalDate);
            entity.HasIndex(x => x.DisposalType);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.DisposalType).HasMaxLength(30);
            entity.Property(x => x.DisposalAmount).HasPrecision(18, 2);
            entity.Property(x => x.GainLossAmount).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ToolAssetCategory>(entity =>
        {
            entity.HasIndex(x => x.CategoryCode).IsUnique();
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.CategoryCode).HasMaxLength(50);
            entity.Property(x => x.CategoryName).HasMaxLength(255);
            entity.Property(x => x.ToolAccountCode).HasMaxLength(50);
            entity.Property(x => x.PrepaidExpenseAccountCode).HasMaxLength(50);
            entity.Property(x => x.ExpenseAccountCode).HasMaxLength(50);
            entity.HasMany(x => x.Tools)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ToolAsset>(entity =>
        {
            entity.HasIndex(x => x.ToolCode).IsUnique();
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.PurchaseDate);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.ToolCode).HasMaxLength(50);
            entity.Property(x => x.ToolName).HasMaxLength(255);
            entity.Property(x => x.OriginalCost).HasPrecision(18, 2);
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(x => x.Issues)
                .WithOne(x => x.ToolAsset)
                .HasForeignKey(x => x.ToolAssetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.AllocationSchedules)
                .WithOne(x => x.ToolAsset)
                .HasForeignKey(x => x.ToolAssetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ToolIssue>(entity =>
        {
            entity.HasIndex(x => x.IssueNo).IsUnique();
            entity.HasIndex(x => x.ToolAssetId);
            entity.HasIndex(x => x.IssueDate);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.IssueNo).HasMaxLength(50);
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ToolAllocationSchedule>(entity =>
        {
            entity.HasIndex(x => new { x.ToolAssetId, x.AllocationPeriod }).IsUnique();
            entity.HasIndex(x => x.AllocationPeriod);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.AllocationPeriod).HasMaxLength(20);
            entity.Property(x => x.AllocationAmount).HasPrecision(18, 2);
            entity.Property(x => x.AccumulatedAfter).HasPrecision(18, 2);
            entity.Property(x => x.RemainingAfter).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ToolAllocationRun>(entity =>
        {
            entity.HasIndex(x => x.RunNo).IsUnique();
            entity.HasIndex(x => x.AllocationPeriod);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.RunNo).HasMaxLength(50);
            entity.Property(x => x.AllocationPeriod).HasMaxLength(20);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.Message).HasMaxLength(2000);
        });

        modelBuilder.Entity<ToolRecovery>(entity =>
        {
            entity.HasIndex(x => x.RecoveryNo).IsUnique();
            entity.HasIndex(x => x.ToolAssetId);
            entity.HasIndex(x => x.RecoveryDate);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.RecoveryNo).HasMaxLength(50);
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasOne(x => x.ToolAsset)
                .WithMany()
                .HasForeignKey(x => x.ToolAssetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ToolDisposal>(entity =>
        {
            entity.HasIndex(x => x.DisposalNo).IsUnique();
            entity.HasIndex(x => x.ToolAssetId);
            entity.HasIndex(x => x.DisposalDate);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JournalHeaderId);
            entity.Property(x => x.DisposalNo).HasMaxLength(50);
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasOne(x => x.ToolAsset)
                .WithMany()
                .HasForeignKey(x => x.ToolAssetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalHeader>()
                .WithMany()
                .HasForeignKey(x => x.JournalHeaderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Person>()
            .HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, DepartmentCode = "HCKT", DepartmentName = "Hành chính - Kế toán", ParentDepartmentId = null, Level = 1, Note = "Khối hỗ trợ", IsActive = true },
            new Department { Id = 2, DepartmentCode = "NHAHANG", DepartmentName = "Nhà hàng", ParentDepartmentId = null, Level = 1, Note = "Khối vận hành", IsActive = true },
            new Department { Id = 3, DepartmentCode = "BEP", DepartmentName = "Bếp", ParentDepartmentId = 2, Level = 2, Note = "Bộ phận trực thuộc nhà hàng", IsActive = true }
        );

        modelBuilder.Entity<Person>().HasData(
            new Person
            {
                Id = 1,
                PersonCode = "NS-001",
                FullName = "Nguyễn Minh Hòa",
                DepartmentId = 1,
                Position = "Kế toán trưởng",
                Phone = "0901234567",
                Email = "hoa@asmile.local",
                Note = "Nhân sự nòng cốt",
                IsActive = true,
                CreatedAt = new DateTime(2026, 01, 15, 0, 0, 0, DateTimeKind.Utc)
            },
            new Person
            {
                Id = 2,
                PersonCode = "NS-002",
                FullName = "Trần Quốc An",
                DepartmentId = 2,
                Position = "Giám sát ca",
                Phone = "0912345678",
                Email = "an@asmile.local",
                Note = string.Empty,
                IsActive = true,
                CreatedAt = new DateTime(2026, 02, 02, 0, 0, 0, DateTimeKind.Utc)
            },
            new Person
            {
                Id = 3,
                PersonCode = "NS-003",
                FullName = "Phạm Thu Hà",
                DepartmentId = 3,
                Position = "Bếp phó",
                Phone = "0989123456",
                Email = "ha@asmile.local",
                Note = "Đang thử việc",
                IsActive = false,
                CreatedAt = new DateTime(2026, 03, 01, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<Warehouse>().HasData(
            new Warehouse
            {
                Id = 1,
                WarehouseCode = "KHO-TONG",
                WarehouseName = "Kho tổng",
                DepartmentName = "Hành chính - Kế toán",
                ResponsiblePersonName = "Nguyễn Minh Hòa",
                IsActive = true,
                IsConsumptionWarehouse = false
            },
            new Warehouse
            {
                Id = 2,
                WarehouseCode = "KHO-BEP",
                WarehouseName = "Kho bếp",
                DepartmentName = "Bếp",
                ResponsiblePersonName = "Phạm Thu Hà",
                IsActive = true,
                IsConsumptionWarehouse = true,
                ParentWarehouseId = 1
            }
        );

        modelBuilder.Entity<Supplier>().HasData(
            new Supplier { Id = 1, SupplierCode = "NCC-001", SupplierName = "Nhà cung cấp mặc định", IsActive = true }
        );

        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, CustomerCode = "KH-001", CustomerName = "Khách hàng mặc định", IsActive = true }
        );

        modelBuilder.Entity<Item>().HasData(
            new Item { Id = 1, ItemCode = "HH-001", ItemName = "Hàng mẫu", Unit = "Cái", GroupName = "Mặc định", SupplierCode = "NCC-001", SupplierName = "Nhà cung cấp mặc định", IsActive = true }
        );

        modelBuilder.Entity<AccountingStandard>().HasData(
            new AccountingStandard
            {
                Id = Guid.Parse("2E7CE9B2-4E4D-4F75-8E5E-000000000099"),
                Code = "TT99",
                Name = "Thông tư 99",
                EffectiveFrom = null,
                IsDefault = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccountingStandard
            {
                Id = Guid.Parse("2E7CE9B2-4E4D-4F75-8E5E-000000000200"),
                Code = "TT200",
                Name = "Thông tư 200",
                EffectiveFrom = null,
                IsDefault = false,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccountingStandard
            {
                Id = Guid.Parse("2E7CE9B2-4E4D-4F75-8E5E-000000000999"),
                Code = "CUSTOM",
                Name = "Tùy chỉnh",
                EffectiveFrom = null,
                IsDefault = false,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<AccountRoleMapping>().HasData(
            new AccountRoleMapping
            {
                Id = Guid.Parse("6F6B8F41-9C2D-4D66-9B2F-000000000156"),
                AccountingStandardCode = "TT99",
                RoleCode = "Inventory",
                AccountCode = "1561",
                IsDefault = true,
                IsActive = true
            },
            new AccountRoleMapping
            {
                Id = Guid.Parse("6F6B8F41-9C2D-4D66-9B2F-000000000331"),
                AccountingStandardCode = "TT99",
                RoleCode = "Payable",
                AccountCode = "3311",
                IsDefault = true,
                IsActive = true
            },
            new AccountRoleMapping
            {
                Id = Guid.Parse("6F6B8F41-9C2D-4D66-9B2F-000000000131"),
                AccountingStandardCode = "TT99",
                RoleCode = "Receivable",
                AccountCode = "1311",
                IsDefault = true,
                IsActive = true
            },
            new AccountRoleMapping
            {
                Id = Guid.Parse("6F6B8F41-9C2D-4D66-9B2F-000000000632"),
                AccountingStandardCode = "TT99",
                RoleCode = "COGS",
                AccountCode = "6321",
                IsDefault = true,
                IsActive = true
            }
        );

        var existingTt99RoleSeeds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TT99:Inventory:1561",
            "TT99:Payable:3311",
            "TT99:Receivable:1311",
            "TT99:COGS:6321"
        };
        modelBuilder.Entity<AccountRoleMapping>().HasData(
            AccountingStandardSeedData.RoleMappings
                .Where(x => !existingTt99RoleSeeds.Contains($"{x.AccountingStandardCode}:{x.RoleCode}:{x.AccountCode}"))
                .ToArray()
        );

        modelBuilder.Entity<AccountingStandardTemplate>().HasData(AccountingStandardSeedData.StandardTemplates);
        modelBuilder.Entity<AccountTemplate>().HasData(AccountingStandardSeedData.AccountTemplates);
        modelBuilder.Entity<FinancialStatementMapping>().HasData(AccountingStandardSeedData.FinancialMappings);
        modelBuilder.Entity<CompanyAccountingProfile>().HasData(new CompanyAccountingProfile
        {
            Id = Guid.Parse("9B2C65A2-1A31-4B6B-924C-000000000001"),
            CompanyId = null,
            CompanyName = "Default Company",
            AccountingStandardCode = "TT99",
            FiscalYearStartMonth = 1,
            FiscalYearStartDay = 1,
            EffectiveFrom = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<CashFund>().HasData(new CashFund
        {
            Id = Guid.Parse("7AC7AD91-6E6A-4B2C-9F9E-000000000001"),
            FundCode = "CASH_MAIN",
            FundName = "Quỹ tiền mặt chính",
            CurrencyCode = "VND",
            AccountCode = "1111",
            OpeningBalance = 0m,
            CurrentBalance = 0m,
            IsDefault = true,
            IsActive = true,
            CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<BankAccount>().HasData(new BankAccount
        {
            Id = Guid.Parse("8E4D05D8-0A7E-4E23-9F5A-000000000001"),
            BankCode = "MAIN_BANK",
            BankName = "Ngân hàng chính",
            BranchName = null,
            AccountNumber = "MAIN_BANK",
            AccountName = "Tài khoản ngân hàng chính",
            CurrencyCode = "VND",
            AccountCode = null,
            OpeningBalance = 0m,
            CurrentBalance = 0m,
            IsDefault = true,
            IsActive = true,
            CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<FixedAssetCategory>().HasData(new FixedAssetCategory
        {
            Id = Guid.Parse("9A71A150-2A6A-4E72-9B25-000000000001"),
            CategoryCode = "TSCD_DEFAULT",
            CategoryName = "Tài sản cố định mặc định",
            AssetAccountCode = null,
            DepreciationAccountCode = null,
            ExpenseAccountCode = null,
            UsefulLifeMonthsDefault = 60,
            DepreciationMethodDefault = "StraightLine",
            IsActive = true,
            CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<ToolAssetCategory>().HasData(new ToolAssetCategory
        {
            Id = Guid.Parse("A82E9C51-1F1D-4D3F-9B25-000000000001"),
            CategoryCode = "CCDC_DEFAULT",
            CategoryName = "Công cụ dụng cụ mặc định",
            ToolAccountCode = null,
            PrepaidExpenseAccountCode = null,
            ExpenseAccountCode = null,
            DefaultAllocationMonths = 12,
            IsActive = true,
            CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<TaxCode>().HasData(
            new TaxCode
            {
                Id = Guid.Parse("B7AC6C11-2A40-4D60-9B25-000000000000"),
                Code = "VAT0",
                Name = "Thuế GTGT 0%",
                TaxRate = 0m,
                TaxType = "VATInput",
                InputAccountCode = null,
                OutputAccountCode = null,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaxCode
            {
                Id = Guid.Parse("B7AC6C11-2A40-4D60-9B25-000000000005"),
                Code = "VAT5",
                Name = "Thuế GTGT 5%",
                TaxRate = 5m,
                TaxType = "VATInput",
                InputAccountCode = null,
                OutputAccountCode = null,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaxCode
            {
                Id = Guid.Parse("B7AC6C11-2A40-4D60-9B25-000000000008"),
                Code = "VAT8",
                Name = "Thuế GTGT 8%",
                TaxRate = 8m,
                TaxType = "VATInput",
                InputAccountCode = null,
                OutputAccountCode = null,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaxCode
            {
                Id = Guid.Parse("B7AC6C11-2A40-4D60-9B25-000000000010"),
                Code = "VAT10",
                Name = "Thuế GTGT 10%",
                TaxRate = 10m,
                TaxType = "VATInput",
                InputAccountCode = null,
                OutputAccountCode = null,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaxCode
            {
                Id = Guid.Parse("B7AC6C11-2A40-4D60-9B25-000000000999"),
                Code = "KCT",
                Name = "Không chịu thuế",
                TaxRate = 0m,
                TaxType = "NonTaxable",
                InputAccountCode = null,
                OutputAccountCode = null,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<ImportTemplate>().HasData(
            new ImportTemplate
            {
                Id = Guid.Parse("0E8BC591-4F9E-4F26-9B25-000000000001"),
                Code = "ITEMS_DEFAULT",
                Name = "Nhập danh mục hàng hóa",
                TargetEntity = "Items",
                Description = "Template chuẩn để nhập mã hàng, tên hàng, đơn vị tính, nhóm hàng và nhà cung cấp.",
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<ReportTemplate>().HasData(
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000001"),
                Code = "GENERAL_LEDGER",
                Name = "Sổ cái",
                Category = "Accounting",
                Description = "Báo cáo sổ cái từ Journal persisted.",
                DataSource = "Journal",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000002"),
                Code = "TRIAL_BALANCE",
                Name = "Cân đối phát sinh",
                Category = "Accounting",
                Description = "Tổng hợp phát sinh theo tài khoản từ Journal persisted.",
                DataSource = "Journal",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000003"),
                Code = "INVENTORY_SUMMARY",
                Name = "Tổng hợp tồn kho",
                Category = "Inventory",
                Description = "Tổng hợp số lượng và giá trị tồn kho.",
                DataSource = "StockBalance",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000004"),
                Code = "INVENTORY_MOVEMENT",
                Name = "Nhập xuất tồn",
                Category = "Inventory",
                Description = "Báo cáo phát sinh nhập xuất kho.",
                DataSource = "StockReceipt,StockIssue,InventoryCostTransaction",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000005"),
                Code = "AP_AGING",
                Name = "Tuổi nợ phải trả",
                Category = "Payable",
                Description = "Báo cáo tuổi nợ nhà cung cấp từ công nợ persisted.",
                DataSource = "PayableTransaction",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000006"),
                Code = "AR_AGING",
                Name = "Tuổi nợ phải thu",
                Category = "Receivable",
                Description = "Báo cáo tuổi nợ khách hàng từ công nợ persisted.",
                DataSource = "ReceivableTransaction",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000007"),
                Code = "VAT_INPUT",
                Name = "Bảng kê VAT đầu vào",
                Category = "Tax",
                Description = "Bảng kê thuế GTGT đầu vào từ giao dịch VAT persisted.",
                DataSource = "VatTransaction",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000008"),
                Code = "VAT_OUTPUT",
                Name = "Bảng kê VAT đầu ra",
                Category = "Tax",
                Description = "Bảng kê thuế GTGT đầu ra từ giao dịch VAT persisted.",
                DataSource = "VatTransaction",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000009"),
                Code = "BALANCE_SHEET",
                Name = "Bảng cân đối kế toán",
                Category = "Accounting",
                Description = "Báo cáo tài sản, nợ phải trả và vốn chủ sở hữu từ Journal persisted.",
                DataSource = "FinancialStatement",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000010"),
                Code = "INCOME_STATEMENT",
                Name = "Báo cáo kết quả kinh doanh",
                Category = "Accounting",
                Description = "Báo cáo doanh thu, chi phí và lợi nhuận từ Journal persisted.",
                DataSource = "FinancialStatement",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000011"),
                Code = "CASH_FLOW",
                Name = "Báo cáo lưu chuyển tiền tệ",
                Category = "Accounting",
                Description = "Skeleton báo cáo lưu chuyển tiền tệ từ tài khoản tiền và ngân hàng.",
                DataSource = "FinancialStatement",
                QueryDefinition = null,
                LayoutDefinition = null,
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 21, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<ReportTemplate>().HasData(
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000112"),
                Code = "TT99_BALANCE_SHEET",
                Name = "Bảng cân đối kế toán TT99",
                Category = "Compliance",
                Description = "Bảng cân đối kế toán theo nền TT99 compliance.",
                DataSource = "FinancialStatement",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000113"),
                Code = "TT99_INCOME_STATEMENT",
                Name = "Báo cáo kết quả kinh doanh TT99",
                Category = "Compliance",
                Description = "Báo cáo kết quả kinh doanh theo nền TT99 compliance.",
                DataSource = "FinancialStatement",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000114"),
                Code = "TT99_CASH_FLOW",
                Name = "Báo cáo lưu chuyển tiền tệ TT99",
                Category = "Compliance",
                Description = "Báo cáo lưu chuyển tiền tệ theo nền TT99 compliance.",
                DataSource = "FinancialStatement",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000115"),
                Code = "TT99_ACCOUNT_MAPPING",
                Name = "Đối chiếu mapping tài khoản TT99",
                Category = "Compliance",
                Description = "Đối chiếu mapping tài khoản theo TT99.",
                DataSource = "TT99",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000116"),
                Code = "TT99_COMPLIANCE_ISSUES",
                Name = "Danh sách lỗi tuân thủ TT99",
                Category = "Compliance",
                Description = "Danh sách issue tuân thủ TT99.",
                DataSource = "TT99",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000117"),
                Code = "TT99_DECLARATION_SUMMARY",
                Name = "Tổng hợp kỳ kê khai TT99",
                Category = "Compliance",
                Description = "Tổng hợp kỳ kê khai phục vụ nền TT99.",
                DataSource = "TT99",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<ReportTemplate>().HasData(
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000212"),
                Code = "TT200_BALANCE_SHEET",
                Name = "Bảng cân đối kế toán TT200",
                Category = "Compliance",
                Description = "Bảng cân đối kế toán theo nền TT200 compliance.",
                DataSource = "FinancialStatement",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000213"),
                Code = "TT200_INCOME_STATEMENT",
                Name = "Báo cáo kết quả kinh doanh TT200",
                Category = "Compliance",
                Description = "Báo cáo kết quả kinh doanh theo nền TT200 compliance.",
                DataSource = "FinancialStatement",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000214"),
                Code = "TT200_CASH_FLOW",
                Name = "Báo cáo lưu chuyển tiền tệ TT200",
                Category = "Compliance",
                Description = "Báo cáo lưu chuyển tiền tệ theo nền TT200 compliance.",
                DataSource = "FinancialStatement",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000215"),
                Code = "TT200_CLOSING_SUMMARY",
                Name = "Tổng hợp khóa sổ TT200",
                Category = "Compliance",
                Description = "Tổng hợp khóa sổ theo nền TT200 compliance.",
                DataSource = "TT200",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000216"),
                Code = "TT200_ACCOUNT_MAPPING",
                Name = "Đối chiếu mapping tài khoản TT200",
                Category = "Compliance",
                Description = "Đối chiếu mapping tài khoản theo TT200.",
                DataSource = "TT200",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000217"),
                Code = "TT200_COMPLIANCE_ISSUES",
                Name = "Danh sách lỗi tuân thủ TT200",
                Category = "Compliance",
                Description = "Danh sách issue tuân thủ TT200.",
                DataSource = "TT200",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new ReportTemplate
            {
                Id = Guid.Parse("4A9A7B4F-AC6E-4E91-9A11-000000000218"),
                Code = "TT200_DECLARATION_SUMMARY",
                Name = "Tổng hợp kỳ kê khai TT200",
                Category = "Compliance",
                Description = "Tổng hợp kỳ kê khai phục vụ nền TT200.",
                DataSource = "TT200",
                IsSystem = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 05, 23, 0, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<VirtualDrive>().HasData(
            new VirtualDrive { Id = Guid.Parse("1B6A72E1-8A8E-4E63-8A4D-8F2E5C1F0001"), Code = "C", Name = "SYSTEM", DisplayName = "C: SYSTEM", Icon = "drive-system.ico", SortOrder = 10, IsSystem = true, IsEnabled = true },
            new VirtualDrive { Id = Guid.Parse("1B6A72E1-8A8E-4E63-8A4D-8F2E5C1F0002"), Code = "D", Name = "ACCOUNTING", DisplayName = "D: ACCOUNTING", Icon = "drive-accounting.ico", SortOrder = 20, IsSystem = true, IsEnabled = true },
            new VirtualDrive { Id = Guid.Parse("1B6A72E1-8A8E-4E63-8A4D-8F2E5C1F0003"), Code = "E", Name = "SALES", DisplayName = "E: SALES", Icon = "drive-sales.ico", SortOrder = 30, IsSystem = true, IsEnabled = true },
            new VirtualDrive { Id = Guid.Parse("1B6A72E1-8A8E-4E63-8A4D-8F2E5C1F0004"), Code = "F", Name = "HR", DisplayName = "F: HR", Icon = "drive-hr.ico", SortOrder = 40, IsSystem = true, IsEnabled = true },
            new VirtualDrive { Id = Guid.Parse("1B6A72E1-8A8E-4E63-8A4D-8F2E5C1F0005"), Code = "G", Name = "LOGS", DisplayName = "G: LOGS", Icon = "drive-logs.ico", SortOrder = 50, IsSystem = true, IsEnabled = true },
            new VirtualDrive { Id = Guid.Parse("1B6A72E1-8A8E-4E63-8A4D-8F2E5C1F0006"), Code = "NET", Name = "NETWORK", DisplayName = "Network", Icon = "network.ico", SortOrder = 60, IsSystem = true, IsEnabled = true }
        );
    }

    private static void ConfigureRestaurantModules(ModelBuilder modelBuilder, bool useSqlServerRowVersion)
    {
        void ConfigureRestaurantJson(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<string?> property)
        {
            if (useSqlServerRowVersion)
            {
                property.HasColumnType("nvarchar(max)");
            }
        }

        void ConfigureRestaurantRowVersion(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<byte[]?> property)
        {
            if (useSqlServerRowVersion)
            {
                property.IsRowVersion();
                return;
            }

            property.IsConcurrencyToken(false).ValueGeneratedNever();
        }
        modelBuilder.Entity<DiningRoom>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.RoomType).HasMaxLength(40);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(40);
        });

        modelBuilder.Entity<DiningTable>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.DiningRoomId, x.SortOrder });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.Room).WithMany(x => x.Tables).HasForeignKey(x => x.DiningRoomId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TableReservation>(entity =>
        {
            entity.HasIndex(x => x.ReservationNo).IsUnique();
            entity.HasIndex(x => new { x.DiningTableId, x.ReservationDateTime, x.Status });
            entity.HasIndex(x => x.Phone);
            entity.Property(x => x.ReservationNo).HasMaxLength(60);
            entity.Property(x => x.CustomerName).HasMaxLength(255);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Room).WithMany(x => x.Reservations).HasForeignKey(x => x.DiningRoomId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Table).WithMany(x => x.Reservations).HasForeignKey(x => x.DiningTableId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AlacarteOrder>(entity =>
        {
            entity.HasIndex(x => x.OrderNo).IsUnique();
            entity.HasIndex(x => new { x.DiningTableId, x.Status });
            entity.HasIndex(x => x.RestaurantQuoteId);
            entity.HasIndex(x => x.OrderDate);
            entity.Property(x => x.OrderNo).HasMaxLength(60);
            entity.Property(x => x.OrderType).HasMaxLength(30);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.PaymentStatus).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.VatAmount).HasPrecision(18, 2);
            entity.Property(x => x.ServiceChargeAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.PaidAmount).HasPrecision(18, 2);
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            entity.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.DiningRoomId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Table).WithMany(x => x.Orders).HasForeignKey(x => x.DiningTableId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Quote).WithMany().HasForeignKey(x => x.RestaurantQuoteId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Lines).WithOne(x => x.Order).HasForeignKey(x => x.AlacarteOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.OrderTables).WithOne(x => x.Order).HasForeignKey(x => x.AlacarteOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.ExtraServices).WithOne(x => x.Order).HasForeignKey(x => x.AlacarteOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.PreparationTickets).WithOne(x => x.Order).HasForeignKey(x => x.AlacarteOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Deposits).WithOne(x => x.Order).HasForeignKey(x => x.AlacarteOrderId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AlacarteOrderLine>(entity =>
        {
            entity.HasIndex(x => new { x.AlacarteOrderId, x.LineNo }).IsUnique();
            entity.HasIndex(x => x.RestaurantOrderTableId);
            entity.Property(x => x.MenuItemCode).HasMaxLength(50);
            entity.Property(x => x.MenuItemName).HasMaxLength(255);
            entity.Property(x => x.UnitName).HasMaxLength(30);
            entity.Property(x => x.Qty).HasPrecision(18, 3);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.VatRate).HasPrecision(9, 4);
            entity.Property(x => x.VatAmount).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.SupplementQuantity).HasPrecision(18, 3);
            entity.Property(x => x.KitchenNote).HasMaxLength(500);
            entity.Property(x => x.SupplementNote).HasMaxLength(500);
            entity.Property(x => x.LineStatus).HasMaxLength(40);
            entity.Property(x => x.KitchenStatus).HasMaxLength(40);
            entity.Property(x => x.LineType).HasMaxLength(40);
            entity.Property(x => x.PricingRule).HasMaxLength(30);
            entity.HasOne(x => x.OrderTable).WithMany(x => x.Lines).HasForeignKey(x => x.RestaurantOrderTableId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantOrderTable>(entity =>
        {
            entity.HasIndex(x => new { x.AlacarteOrderId, x.SortOrder });
            entity.HasIndex(x => new { x.AlacarteOrderId, x.TableNo }).IsUnique();
            entity.HasIndex(x => x.DiningTableId);
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasOne(x => x.QuoteTable).WithMany().HasForeignKey(x => x.RestaurantQuoteTableId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<DiningTable>().WithMany().HasForeignKey(x => x.DiningTableId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RestaurantServingGroup>(entity =>
        {
            entity.HasIndex(x => new { x.AlacarteOrderId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.AlacarteOrderId, x.SortOrder });
            entity.Property(x => x.Code).HasMaxLength(60);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.ScopeType).HasMaxLength(40);
            entity.Property(x => x.ServingRule).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasOne(x => x.Order).WithMany(x => x.ServingGroups).HasForeignKey(x => x.AlacarteOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RestaurantServingGroupTable>(entity =>
        {
            entity.HasIndex(x => new { x.RestaurantServingGroupId, x.RestaurantOrderTableId }).IsUnique();
            entity.HasIndex(x => x.DiningTableId);
            entity.HasOne(x => x.ServingGroup).WithMany(x => x.Tables).HasForeignKey(x => x.RestaurantServingGroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.OrderTable).WithMany(x => x.ServingGroupTables).HasForeignKey(x => x.RestaurantOrderTableId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<DiningTable>().WithMany().HasForeignKey(x => x.DiningTableId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RestaurantExtraService>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.ServiceType).HasMaxLength(60);
            entity.Property(x => x.UnitName).HasMaxLength(40);
            entity.Property(x => x.DefaultPrice).HasPrecision(18, 2);
            entity.Property(x => x.VatRate).HasPrecision(9, 4);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.ImagePath).HasMaxLength(500);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<RestaurantOrderExtraService>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(255);
            entity.Property(x => x.Qty).HasPrecision(18, 3);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.VatRate).HasPrecision(9, 4);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasOne(x => x.ExtraService).WithMany().HasForeignKey(x => x.RestaurantExtraServiceId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RestaurantContract>(entity =>
        {
            entity.HasIndex(x => x.ContractNo).IsUnique();
            entity.Property(x => x.ContractNo).HasMaxLength(60);
            entity.Property(x => x.EventDescription).HasMaxLength(1000);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.DepositAmount).HasPrecision(18, 2);
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RestaurantDeposit>(entity =>
        {
            entity.HasIndex(x => x.DepositNo).IsUnique();
            entity.Property(x => x.DepositNo).HasMaxLength(60);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.PaymentMethod).HasMaxLength(40);
            entity.Property(x => x.Receiver).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.HasOne(x => x.Contract).WithMany(x => x.Deposits).HasForeignKey(x => x.RestaurantContractId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.TableReservationId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<CashFund>().WithMany().HasForeignKey(x => x.CashFundId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<BankAccount>().WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RestaurantQuote>(entity =>
        {
            entity.HasIndex(x => x.QuoteNo).IsUnique();
            entity.HasIndex(x => new { x.Status, x.EventDate });
            entity.HasIndex(x => x.ApprovedOrderId);
            entity.Property(x => x.QuoteNo).HasMaxLength(60);
            entity.Property(x => x.CustomerName).HasMaxLength(255);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.RoomName).HasMaxLength(255);
            entity.Property(x => x.StartTime).HasMaxLength(20);
            entity.Property(x => x.EndTime).HasMaxLength(20);
            entity.Property(x => x.PaymentDeadline).HasMaxLength(120);
            entity.Property(x => x.PaymentMethod).HasMaxLength(40);
            entity.Property(x => x.Promotion).HasMaxLength(1000);
            entity.Property(x => x.BankAccount).HasMaxLength(255);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.PreparedBy).HasMaxLength(120);
            entity.Property(x => x.DepositCashTransactionNo).HasMaxLength(60);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.DepositAmount).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.CancelFeePercent).HasPrecision(9, 4);
            entity.HasOne(x => x.DiningRoom).WithMany().HasForeignKey(x => x.DiningRoomId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.DiningTable).WithMany().HasForeignKey(x => x.DiningTableId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.TableReservationId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ApprovedOrder).WithMany().HasForeignKey(x => x.ApprovedOrderId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Deposit).WithMany().HasForeignKey(x => x.DepositId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<CashTransaction>().WithMany().HasForeignKey(x => x.DepositCashTransactionId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.QuoteTables).WithOne(x => x.Quote).HasForeignKey(x => x.RestaurantQuoteId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Lines).WithOne(x => x.Quote).HasForeignKey(x => x.RestaurantQuoteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RestaurantQuoteLine>(entity =>
        {
            entity.HasIndex(x => new { x.RestaurantQuoteId, x.LineNo }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.LineType).HasMaxLength(40);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.PricingRule).HasMaxLength(30);
            entity.Property(x => x.CalculatedQuantity).HasPrecision(18, 3);
            entity.Property(x => x.Total).HasPrecision(18, 2);
            entity.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RestaurantQuoteTable>(entity =>
        {
            entity.HasIndex(x => new { x.RestaurantQuoteId, x.SortOrder });
            entity.HasIndex(x => new { x.RestaurantQuoteId, x.TableNo }).IsUnique();
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(500);
        });

        modelBuilder.Entity<RestaurantPreparationTicket>(entity =>
        {
            entity.HasIndex(x => x.TicketNo).IsUnique();
            entity.Property(x => x.TicketNo).HasMaxLength(60);
            entity.Property(x => x.Station).HasMaxLength(50);
            entity.Property(x => x.TicketType).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.SentBy).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasMany(x => x.Lines).WithOne(x => x.Ticket).HasForeignKey(x => x.RestaurantPreparationTicketId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RestaurantPreparationTicketLine>(entity =>
        {
            entity.HasIndex(x => new { x.RestaurantPreparationTicketId, x.LineNo }).IsUnique();
            entity.Property(x => x.MenuItemName).HasMaxLength(255);
            entity.Property(x => x.Qty).HasPrecision(18, 3);
            entity.Property(x => x.KitchenNote).HasMaxLength(500);
            entity.HasOne(x => x.OrderLine).WithMany().HasForeignKey(x => x.AlacarteOrderLineId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BanquetRoom>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.Description).HasMaxLength(1000);
            ConfigureRestaurantJson(entity.Property(x => x.LayoutJson));
            entity.Property(x => x.Status).HasMaxLength(40);
        });

        modelBuilder.Entity<BanquetEvent>(entity =>
        {
            entity.HasIndex(x => x.EventNo).IsUnique();
            entity.HasIndex(x => new { x.BanquetRoomId, x.EventDate, x.StartTime, x.EndTime, x.Status });
            entity.Property(x => x.EventNo).HasMaxLength(60);
            entity.Property(x => x.EventType).HasMaxLength(60);
            entity.Property(x => x.CustomerName).HasMaxLength(255);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.BrideName).HasMaxLength(255);
            entity.Property(x => x.GroomName).HasMaxLength(255);
            entity.Property(x => x.ConferenceName).HasMaxLength(255);
            entity.Property(x => x.BirthdayPersonName).HasMaxLength(255);
            entity.Property(x => x.MeetingTopic).HasMaxLength(500);
            entity.Property(x => x.PartyTheme).HasMaxLength(500);
            entity.Property(x => x.OtherEventName).HasMaxLength(255);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.FinanciallyLockedBy).HasMaxLength(120);
            ConfigureRestaurantRowVersion(entity.Property(x => x.RowVersion));
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Room).WithMany(x => x.Events).HasForeignKey(x => x.BanquetRoomId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Menus).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Services).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Contracts).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Deposits).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.ChecklistItems).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.FloorPlans).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.TimelineItems).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.QuantificationBatches).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.PurchaseRequests).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BanquetMenuPackage>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.PricingMode).HasMaxLength(40);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.VatRate).HasPrecision(9, 4);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasMany(x => x.Lines).WithOne(x => x.Package).HasForeignKey(x => x.BanquetMenuPackageId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BanquetMenuPackageLine>(entity =>
        {
            entity.HasIndex(x => new { x.BanquetMenuPackageId, x.LineNo }).IsUnique();
            entity.Property(x => x.CourseType).HasMaxLength(40);
            entity.Property(x => x.PricingRule).HasMaxLength(40);
            entity.Property(x => x.Qty).HasPrecision(18, 3);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BanquetEventMenu>(entity =>
        {
            entity.Property(x => x.PricingMode).HasMaxLength(40);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Qty).HasPrecision(18, 3);
            entity.Property(x => x.ItemName).HasMaxLength(255);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.Package).WithMany().HasForeignKey(x => x.BanquetMenuPackageId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BanquetServiceLine>(entity =>
        {
            entity.Property(x => x.ServiceType).HasMaxLength(60);
            entity.Property(x => x.Description).HasMaxLength(255);
            entity.Property(x => x.UnitName).HasMaxLength(40);
            entity.Property(x => x.Qty).HasPrecision(18, 3);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.VatRate).HasPrecision(9, 4);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.CostPrice).HasPrecision(18, 2);
            entity.Property(x => x.CostAmount).HasPrecision(18, 2);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BanquetContract>(entity =>
        {
            entity.HasIndex(x => x.ContractNo).IsUnique();
            entity.Property(x => x.ContractNo).HasMaxLength(60);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.DepositAmount).HasPrecision(18, 2);
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(1000);
            ConfigureRestaurantRowVersion(entity.Property(x => x.RowVersion));
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BanquetDeposit>(entity =>
        {
            entity.HasIndex(x => x.DepositNo).IsUnique();
            entity.HasIndex(x => new { x.BanquetEventId, x.IdempotencyKey }).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
            entity.Property(x => x.DepositNo).HasMaxLength(60);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.RefundAmount).HasPrecision(18, 2);
            entity.Property(x => x.PaymentMethod).HasMaxLength(40);
            entity.Property(x => x.Receiver).HasMaxLength(120);
            entity.Property(x => x.CashTransactionNo).HasMaxLength(60);
            entity.Property(x => x.RefundCashTransactionNo).HasMaxLength(60);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(80);
            entity.Property(x => x.RefundedBy).HasMaxLength(120);
            entity.Property(x => x.RefundReason).HasMaxLength(1000);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.HasOne(x => x.Contract).WithMany(x => x.Deposits).HasForeignKey(x => x.BanquetContractId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<CashFund>().WithMany().HasForeignKey(x => x.CashFundId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<BankAccount>().WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<CashTransaction>().WithMany().HasForeignKey(x => x.CashTransactionId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<CashTransaction>().WithMany().HasForeignKey(x => x.RefundCashTransactionId).OnDelete(DeleteBehavior.NoAction);
            ConfigureRestaurantRowVersion(entity.Property(x => x.RowVersion));
        });

        modelBuilder.Entity<BanquetChecklist>(entity =>
        {
            entity.HasIndex(x => new { x.BanquetEventId, x.SortOrder });
            entity.Property(x => x.Title).HasMaxLength(255);
            entity.Property(x => x.DoneBy).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(1000);
        });

        modelBuilder.Entity<BanquetFloorPlan>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(255);
            ConfigureRestaurantJson(entity.Property(x => x.LayoutJson));
            entity.HasMany(x => x.Tables).WithOne(x => x.FloorPlan).HasForeignKey(x => x.BanquetFloorPlanId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BanquetFloorPlanTable>(entity =>
        {
            entity.HasIndex(x => new { x.BanquetFloorPlanId, x.TableNo }).IsUnique();
            entity.Property(x => x.TableNo).HasMaxLength(40);
            entity.Property(x => x.TableType).HasMaxLength(40);
            entity.Property(x => x.Note).HasMaxLength(500);
        });

        modelBuilder.Entity<BanquetTimeline>(entity =>
        {
            entity.HasIndex(x => new { x.BanquetEventId, x.SortOrder });
            entity.Property(x => x.Title).HasMaxLength(255);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.ResponsiblePerson).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
        });

        modelBuilder.Entity<BanquetQuantificationBatch>(entity =>
        {
            entity.HasIndex(x => new { x.BanquetEventId, x.VersionNo }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.SentAt });
            entity.Property(x => x.Fingerprint).HasMaxLength(128);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.SentBy).HasMaxLength(120);
            entity.Property(x => x.ReceivedBy).HasMaxLength(120);
            entity.Property(x => x.CompletedBy).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasMany(x => x.Lines).WithOne(x => x.Batch).HasForeignKey(x => x.BanquetQuantificationBatchId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BanquetQuantificationLine>(entity =>
        {
            entity.HasIndex(x => new { x.BanquetQuantificationBatchId, x.SortOrder });
            entity.Property(x => x.LineType).HasMaxLength(40);
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(255);
            entity.Property(x => x.UnitName).HasMaxLength(40);
            entity.Property(x => x.RequiredQuantity).HasPrecision(18, 3);
            entity.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            entity.Property(x => x.SourceText).HasMaxLength(500);
            entity.Property(x => x.Note).HasMaxLength(1000);
        });

        modelBuilder.Entity<BanquetPurchaseRequest>(entity =>
        {
            entity.HasIndex(x => x.RequestNo).IsUnique();
            entity.HasIndex(x => x.BanquetQuantificationBatchId).IsUnique();
            entity.HasIndex(x => new { x.RequiredDate, x.Status });
            entity.Property(x => x.RequestNo).HasMaxLength(60);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.RequestedBy).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.QuantificationBatch).WithOne(x => x.PurchaseRequest).HasForeignKey<BanquetPurchaseRequest>(x => x.BanquetQuantificationBatchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Lines).WithOne(x => x.Request).HasForeignKey(x => x.BanquetPurchaseRequestId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BanquetPurchaseRequestLine>(entity =>
        {
            entity.HasIndex(x => new { x.BanquetPurchaseRequestId, x.SortOrder }).IsUnique();
            entity.Property(x => x.ItemCode).HasMaxLength(80);
            entity.Property(x => x.ItemName).HasMaxLength(255);
            entity.Property(x => x.UnitName).HasMaxLength(40);
            entity.Property(x => x.RequestedQuantity).HasPrecision(18, 3);
            entity.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            entity.Property(x => x.Note).HasMaxLength(500);
        });

        modelBuilder.Entity<BanquetModuleSetting>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.BranchId }).IsUnique();
            entity.Property(x => x.VenueName).HasMaxLength(255);
            entity.Property(x => x.VenueCode).HasMaxLength(60);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.Website).HasMaxLength(255);
            entity.Property(x => x.TaxCode).HasMaxLength(40);
            entity.Property(x => x.DefaultDepositRate).HasPrecision(9, 4);
            entity.Property(x => x.DefaultVatRate).HasPrecision(9, 4);
            entity.Property(x => x.LunchStartTime).HasMaxLength(20);
            entity.Property(x => x.DinnerStartTime).HasMaxLength(20);
        });

        modelBuilder.Entity<QuoteChangeRequest>(entity =>
        {
            entity.HasIndex(x => x.QuoteId);
            entity.HasIndex(x => x.RequestNumber).IsUnique();
            entity.Property(x => x.RequestNumber).HasMaxLength(60);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.RequestedBy).HasMaxLength(200);
            entity.Property(x => x.ReviewedBy).HasMaxLength(200);
            entity.Property(x => x.AppliedBy).HasMaxLength(200);
            entity.Property(x => x.PreviousTotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.NewTotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.AmountDelta).HasPrecision(18, 2);
            entity.Property(x => x.AdjustmentPlanJson).HasMaxLength(4000);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.Quote).WithMany().HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PreviousOrder).WithMany().HasForeignKey(x => x.PreviousOrderId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.NewOrder).WithMany().HasForeignKey(x => x.NewOrderId).OnDelete(DeleteBehavior.NoAction).IsRequired(false);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasMany(x => x.Details).WithOne(x => x.ChangeRequest).HasForeignKey(x => x.ChangeRequestId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuoteChangeRequestDetail>(entity =>
        {
            entity.HasIndex(x => x.ChangeRequestId);
            entity.Property(x => x.ChangeType).HasMaxLength(40);
            entity.Property(x => x.ItemName).HasMaxLength(500);
            entity.Property(x => x.PreviousQuantity).HasPrecision(18, 4);
            entity.Property(x => x.NewQuantity).HasPrecision(18, 4);
            entity.Property(x => x.PreviousAmount).HasPrecision(18, 2);
            entity.Property(x => x.NewAmount).HasPrecision(18, 2);
            entity.Property(x => x.Note).HasMaxLength(500);
        });

        modelBuilder.Entity<QuoteAuditLog>(entity =>
        {
            entity.HasIndex(x => x.QuoteId);
            entity.HasIndex(x => x.ChangeRequestId);
            entity.HasIndex(x => x.Timestamp);
            entity.Property(x => x.Action).HasMaxLength(60);
            entity.Property(x => x.ChangedBy).HasMaxLength(200);
            entity.Property(x => x.FieldName).HasMaxLength(200);
            entity.Property(x => x.OldValue).HasMaxLength(500);
            entity.Property(x => x.NewValue).HasMaxLength(500);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.Quote).WithMany().HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.ChangeRequest).WithMany().HasForeignKey(x => x.ChangeRequestId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });
    }

    private void ApplyCompanyQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(ICompanyScoped).IsAssignableFrom(clrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(clrType, "entity");
            var companyProperty = BuildEfProperty(parameter, "CompanyId");
            var companyNull = Expression.Equal(companyProperty, Expression.Constant(null, typeof(Guid?)));
            var companyMatches = Expression.Equal(companyProperty, Expression.Property(Expression.Constant(this), nameof(CurrentCompanyId)));
            Expression filter = Expression.OrElse(companyNull, companyMatches);

            if (typeof(IBranchScoped).IsAssignableFrom(clrType))
            {
                var branchProperty = BuildEfProperty(parameter, "BranchId");
                var currentBranch = Expression.Property(Expression.Constant(this), nameof(CurrentBranchId));
                var noBranchSelected = Expression.Equal(currentBranch, Expression.Constant(null, typeof(Guid?)));
                var branchNull = Expression.Equal(branchProperty, Expression.Constant(null, typeof(Guid?)));
                var branchMatches = Expression.Equal(branchProperty, currentBranch);
                filter = Expression.AndAlso(filter, Expression.OrElse(noBranchSelected, Expression.OrElse(branchNull, branchMatches)));
            }

            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(filter, parameter));
        }
    }

    private static MethodCallExpression BuildEfProperty(ParameterExpression parameter, string propertyName)
    {
        return Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(Guid?)],
            parameter,
            Expression.Constant(propertyName));
    }
}
