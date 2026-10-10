using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Core.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Loyalty;
using ASMILE.BO.Core.FrontOffice.Feedback;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.TaskShift;
using ASMILE.BO.Core.FrontOffice.ServiceRequest;
using ASMILE.BO.Core.FrontOffice.Message;
using ASMILE.BO.Core.FrontOffice.WakeUpCall;
using ASMILE.BO.Core.FrontOffice.LostFound;
using ASMILE.BO.Core.FrontOffice.Cashier;
using ASMILE.BO.Core.FrontOffice.Incident;
using ASMILE.BO.Core.FrontOffice.ShiftLog;
using ASMILE.BO.Core.FrontOffice.Settlement;
using ASMILE.BO.Core.FrontOffice.NightAudit;
using ASMILE.BO.Core.FrontOffice.Marketing;
using ASMILE.BO.Core.FrontOffice.Maintenance;
using ASMILE.BO.Core.FrontOffice.DoorLock;
using ASMILE.BO.Core.FrontOffice.Concierge;
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

    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyCompanyScope();
        ApplyProviderSafeRowVersions();
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
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

            // Keep the provider-safe token shape identical to SQL Server rowversion.
            // Guid.ToByteArray() is 16 bytes, while all application concurrency
            // contracts and SQL Server rowversion columns use 8 bytes.
            entry.Property("RowVersion").CurrentValue = Guid.NewGuid().ToByteArray()[..8];
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
    public DbSet<LoginAudit> LoginAudits => Set<LoginAudit>();
    public DbSet<LoginSession> LoginSessions => Set<LoginSession>();
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
    public DbSet<NotificationComment> NotificationComments => Set<NotificationComment>();
    public DbSet<NotificationUserState> NotificationUserStates => Set<NotificationUserState>();
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
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();
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
    public DbSet<HotelBuilding> HotelBuildings => Set<HotelBuilding>();
    public DbSet<HotelFloor> HotelFloors => Set<HotelFloor>();
    public DbSet<HotelArea> HotelAreas => Set<HotelArea>();
    public DbSet<HotelReferenceCatalogItem> HotelReferenceCatalogItems => Set<HotelReferenceCatalogItem>();
    public DbSet<HotelMinibarItemProfile> HotelMinibarItemProfiles => Set<HotelMinibarItemProfile>();
    public DbSet<HotelRoomStatusRule> HotelRoomStatusRules => Set<HotelRoomStatusRule>();
    public DbSet<HotelPropertyProfile> HotelPropertyProfiles => Set<HotelPropertyProfile>();
    public DbSet<HotelStayPolicy> HotelStayPolicies => Set<HotelStayPolicy>();
    public DbSet<HotelCheckInOutRule> HotelCheckInOutRules => Set<HotelCheckInOutRule>();
    public DbSet<HotelRoomMoveUpgradeRule> HotelRoomMoveUpgradeRules => Set<HotelRoomMoveUpgradeRule>();
    public DbSet<HotelPropertyDocument> HotelPropertyDocuments => Set<HotelPropertyDocument>();
    public DbSet<HotelPromotionPackage> HotelPromotionPackages => Set<HotelPromotionPackage>();
    public DbSet<HotelGroupPolicy> HotelGroupPolicies => Set<HotelGroupPolicy>();
    public DbSet<HotelCashierShiftPolicy> HotelCashierShiftPolicies => Set<HotelCashierShiftPolicy>();
    public DbSet<HotelNightAuditPolicy> HotelNightAuditPolicies => Set<HotelNightAuditPolicy>();
    public DbSet<HotelApprovalMatrixRule> HotelApprovalMatrixRules => Set<HotelApprovalMatrixRule>();
    public DbSet<HotelFOPermissionPolicy> HotelFOPermissionPolicies => Set<HotelFOPermissionPolicy>();
    public DbSet<HotelFormPrinterProfile> HotelFormPrinterProfiles => Set<HotelFormPrinterProfile>();
    public DbSet<HotelMessagingIntegrationProfile> HotelMessagingIntegrationProfiles => Set<HotelMessagingIntegrationProfile>();
    public DbSet<HotelPostedCorrectionPolicy> HotelPostedCorrectionPolicies => Set<HotelPostedCorrectionPolicy>();
    public DbSet<HotelBusinessRuleProfile> HotelBusinessRuleProfiles => Set<HotelBusinessRuleProfile>();
    public DbSet<HotelTaxServiceChargeRule> HotelTaxServiceChargeRules => Set<HotelTaxServiceChargeRule>();
    public DbSet<HotelPaymentMethodProfile> HotelPaymentMethodProfiles => Set<HotelPaymentMethodProfile>();
    public DbSet<HotelDepositPolicy> HotelDepositPolicies => Set<HotelDepositPolicy>();
    public DbSet<HotelDiscountPolicy> HotelDiscountPolicies => Set<HotelDiscountPolicy>();
    public DbSet<HotelChargeCodeProfile> HotelChargeCodeProfiles => Set<HotelChargeCodeProfile>();
    public DbSet<HotelFolioProfile> HotelFolioProfiles => Set<HotelFolioProfile>();
    public DbSet<HotelGuestTypeProfile> HotelGuestTypeProfiles => Set<HotelGuestTypeProfile>();
    public DbSet<HotelProfileTypeProfile> HotelProfileTypeProfiles => Set<HotelProfileTypeProfile>();
    public DbSet<HotelVipLevelProfile> HotelVipLevelProfiles => Set<HotelVipLevelProfile>();
    public DbSet<HotelGuestPreferenceProfile> HotelGuestPreferenceProfiles => Set<HotelGuestPreferenceProfile>();
    public DbSet<HotelGuestRequestProfile> HotelGuestRequestProfiles => Set<HotelGuestRequestProfile>();
    public DbSet<HotelReservationTypeProfile> HotelReservationTypeProfiles => Set<HotelReservationTypeProfile>();
    public DbSet<HotelBookingSourceProfile> HotelBookingSourceProfiles => Set<HotelBookingSourceProfile>();
    public DbSet<HotelBookingChannelProfile> HotelBookingChannelProfiles => Set<HotelBookingChannelProfile>();
    public DbSet<HotelMarketSegmentProfile> HotelMarketSegmentProfiles => Set<HotelMarketSegmentProfile>();
    public DbSet<HotelHousekeepingRuleProfile> HotelHousekeepingRuleProfiles => Set<HotelHousekeepingRuleProfile>();
    public DbSet<HotelWakeupRuleProfile> HotelWakeupRuleProfiles => Set<HotelWakeupRuleProfile>();
    public DbSet<HotelComplaintProfile> HotelComplaintProfiles => Set<HotelComplaintProfile>();
    public DbSet<HotelCompanyProfile> HotelCompanyProfiles => Set<HotelCompanyProfile>();
    public DbSet<HotelTravelAgentProfile> HotelTravelAgentProfiles => Set<HotelTravelAgentProfile>();
    public DbSet<HotelCommissionRuleProfile> HotelCommissionRuleProfiles => Set<HotelCommissionRuleProfile>();
    public DbSet<HotelRoom> HotelRooms => Set<HotelRoom>();
    public DbSet<HotelStay> HotelStays => Set<HotelStay>();
    public DbSet<HotelStayHistory> HotelStayHistories => Set<HotelStayHistory>();
    public DbSet<HotelStayOccupant> HotelStayOccupants => Set<HotelStayOccupant>();
    public DbSet<HotelFolio> HotelFolios => Set<HotelFolio>();
    public DbSet<HotelFolioEntry> HotelFolioEntries => Set<HotelFolioEntry>();
    public DbSet<HotelFolioRoutingRule> HotelFolioRoutingRules => Set<HotelFolioRoutingRule>();
    public DbSet<HotelInvoice> HotelInvoices => Set<HotelInvoice>();
    public DbSet<HotelPayment> HotelPayments => Set<HotelPayment>();
    public DbSet<HotelPaymentAllocation> HotelPaymentAllocations => Set<HotelPaymentAllocation>();
    public DbSet<PaymentHistory> HotelPaymentHistory => Set<PaymentHistory>();
    // Canonical Night Audit business date. Do not introduce a parallel FO business-date entity.
    public DbSet<BusinessDateRecord> BusinessDates => Set<BusinessDateRecord>();
    public DbSet<NightAudit> NightAudits => Set<NightAudit>();
    public DbSet<NightAuditStepExecution> NightAuditSteps => Set<NightAuditStepExecution>();
    public DbSet<RevenueSnapshot> RevenueSnapshots => Set<RevenueSnapshot>();
    public DbSet<GuestProfile> GuestProfiles => Set<GuestProfile>();
    public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();
    public DbSet<GuestPreference> GuestPreferences => Set<GuestPreference>();
    public DbSet<RatePlan> RatePlans => Set<RatePlan>();
    public DbSet<RateCalendarEntry> RateCalendarEntries => Set<RateCalendarEntry>();
    public DbSet<RateRule> RateRules => Set<RateRule>();
    public DbSet<RateSeason> RateSeasons => Set<RateSeason>();
    public DbSet<HotelExchangeRate> HotelExchangeRates => Set<HotelExchangeRate>();
    public DbSet<GroupReservation> FrontOfficeGroupReservations => Set<GroupReservation>();
    public DbSet<GroupDeposit> FrontOfficeGroupDeposits => Set<GroupDeposit>();
    public DbSet<GroupDepositSchedule> FrontOfficeGroupDepositSchedules => Set<GroupDepositSchedule>();
    public DbSet<GroupRoomAssignment> FrontOfficeGroupRoomAssignments => Set<GroupRoomAssignment>();
    public DbSet<GroupRoomBlock> FrontOfficeGroupRoomBlocks => Set<GroupRoomBlock>();
    public DbSet<GroupServiceOrder> FrontOfficeGroupServiceOrders => Set<GroupServiceOrder>();
    public DbSet<FrontOfficeIncident> FrontOfficeIncidents => Set<FrontOfficeIncident>();
    public DbSet<FrontOfficeShiftLogEntry> FrontOfficeShiftLogEntries => Set<FrontOfficeShiftLogEntry>();
    public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<TaskHistory> TaskHistories => Set<TaskHistory>();
    public DbSet<RoomHousekeepingState> RoomHousekeepingStates => Set<RoomHousekeepingState>();
    public DbSet<RoomHousekeepingStatusHistory> RoomHousekeepingStatusHistories => Set<RoomHousekeepingStatusHistory>();
    public DbSet<RoomInspection> RoomInspections => Set<RoomInspection>();
    public DbSet<HousekeepingLinenType> HousekeepingLinenTypes => Set<HousekeepingLinenType>();
    public DbSet<HousekeepingLinenTransaction> HousekeepingLinenTransactions => Set<HousekeepingLinenTransaction>();
    public DbSet<HousekeepingAmenityType> HousekeepingAmenityTypes => Set<HousekeepingAmenityType>();
    public DbSet<HousekeepingAmenityBalance> HousekeepingAmenityBalances => Set<HousekeepingAmenityBalance>();
    public DbSet<HousekeepingAmenityRefillRequest> HousekeepingAmenityRefillRequests => Set<HousekeepingAmenityRefillRequest>();
    public DbSet<HousekeepingAmenityRefillLine> HousekeepingAmenityRefillLines => Set<HousekeepingAmenityRefillLine>();
    public DbSet<MinibarSession> MinibarSessions => Set<MinibarSession>();
    public DbSet<MinibarConsumption> MinibarConsumptions => Set<MinibarConsumption>();
    public DbSet<MinibarRestockTransaction> MinibarRestockTransactions => Set<MinibarRestockTransaction>();
    public DbSet<MinibarInventoryAudit> MinibarInventoryAudits => Set<MinibarInventoryAudit>();
    public DbSet<MinibarInventoryLine> MinibarInventoryLines => Set<MinibarInventoryLine>();
    public DbSet<KeyCardCredential> KeyCardCredentials => Set<KeyCardCredential>();
    public DbSet<KeyCardEvent> KeyCardEvents => Set<KeyCardEvent>();
    public DbSet<LostFoundItem> LostFoundItems => Set<LostFoundItem>();
    public DbSet<LostFoundHistory> LostFoundHistories => Set<LostFoundHistory>();
    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<CashReconciliation> CashReconciliations => Set<CashReconciliation>();
    public DbSet<MarketingRecord> MarketingRecords => Set<MarketingRecord>();
    public DbSet<ServiceRequest> FrontOfficeServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceHistory> FrontOfficeServiceHistories => Set<ServiceHistory>();
    public DbSet<GuestMessage> FrontOfficeGuestMessages => Set<GuestMessage>();
    public DbSet<MessageHistory> FrontOfficeMessageHistories => Set<MessageHistory>();
    public DbSet<MaintenanceWorkOrder> MaintenanceWorkOrders => Set<MaintenanceWorkOrder>();
    public DbSet<MaintenanceWorkOrderNote> MaintenanceWorkOrderNotes => Set<MaintenanceWorkOrderNote>();
    public DbSet<MaintenanceAsset> MaintenanceAssets => Set<MaintenanceAsset>();
    public DbSet<MaintenanceAssetFailure> MaintenanceAssetFailures => Set<MaintenanceAssetFailure>();
    public DbSet<PreventiveMaintenancePlan> PreventiveMaintenancePlans => Set<PreventiveMaintenancePlan>();
    public DbSet<PreventiveChecklistItem> PreventiveChecklistItems => Set<PreventiveChecklistItem>();
    public DbSet<PreventiveMaintenanceSchedule> PreventiveMaintenanceSchedules => Set<PreventiveMaintenanceSchedule>();
    public DbSet<DoorLockDevice> DoorLockDevices => Set<DoorLockDevice>();
    public DbSet<DoorLockEvent> DoorLockEvents => Set<DoorLockEvent>();
    public DbSet<ConciergeWakeUpCall> ConciergeWakeUpCalls => Set<ConciergeWakeUpCall>();
    public DbSet<ConciergeLuggageItem> ConciergeLuggageItems => Set<ConciergeLuggageItem>();
    public DbSet<ConciergeTransportationBooking> ConciergeTransportationBookings => Set<ConciergeTransportationBooking>();
    public DbSet<ConciergeTelephoneCall> ConciergeTelephoneCalls => Set<ConciergeTelephoneCall>();
    public DbSet<ConciergeTelephoneContact> ConciergeTelephoneContacts => Set<ConciergeTelephoneContact>();
    public DbSet<ReservationDenial> ReservationDenials => Set<ReservationDenial>();
    public DbSet<GuestFeedback> GuestFeedbacks => Set<GuestFeedback>();
    public DbSet<GuestFeedbackHistory> GuestFeedbackHistories => Set<GuestFeedbackHistory>();
    public DbSet<VipProfile> VipProfiles => Set<VipProfile>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var useSqlServerRowVersion = Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
        if (useSqlServerRowVersion)
        {
            modelBuilder.HasSequence<long>("TrinhTuSoDatPhong", "fo").StartsAt(1).IncrementsBy(1);
            modelBuilder.HasSequence<long>("TrinhTuSoLuuTru", "fo").StartsAt(1).IncrementsBy(1);
        }

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
            entity.Property(x => x.IdempotencyKey).HasMaxLength(160);
            entity.Property(x => x.TargetRoute).HasMaxLength(120);
            entity.Property(x => x.MetadataJson);
            entity.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("UQ_UserNotifications_IdempotencyKey");
        });

        modelBuilder.Entity<NotificationComment>(entity =>
        {
            entity.HasIndex(x => x.NotificationId);
            entity.HasIndex(x => new { x.NotificationId, x.UserId, x.CreatedAt });
            entity.Property(x => x.Kind).HasMaxLength(20);
            entity.Property(x => x.CommentText).HasMaxLength(4000);
            entity.Property(x => x.CreatedByName).HasMaxLength(128);
            entity.HasOne(x => x.Notification).WithMany(x => x.Comments).HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationUserState>(entity =>
        {
            entity.HasIndex(x => new { x.NotificationId, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.IsDismissed, x.UpdatedAt });
            entity.HasOne(x => x.Notification).WithMany(x => x.UserStates).HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<EmailOutboxMessage>(entity =>
        {
            entity.ToTable("EmailOutboxMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Recipient).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Body).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.Property(x => x.SourceType).HasMaxLength(80);
            entity.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
            entity.HasIndex(x => new { x.SourceType, x.SourceId });
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
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
        modelBuilder.Entity<Reservation>(entity => { entity.ToTable("DatPhong", "fo"); entity.HasKey(x => x.ReservationId).HasName("PK_fo_DatPhong"); entity.Property(x => x.ReservationNumber).HasMaxLength(40); entity.Property(x => x.GuestNameSnapshot).HasMaxLength(200); entity.Property(x => x.GuestPhoneSnapshot).HasMaxLength(50); entity.Property(x => x.GuestEmailSnapshot).HasMaxLength(200); entity.Property(x => x.Source).HasMaxLength(80); entity.Property(x => x.MarketSegment).HasMaxLength(80); entity.Property(x => x.CompanyName).HasMaxLength(200); entity.Property(x => x.TravelAgentName).HasMaxLength(200); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.GuaranteeType).HasConversion<string>().HasMaxLength(40); entity.Property(x => x.DepositStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.DepositAmount).HasColumnType("decimal(18,2)"); entity.Property(x => x.SpecialRequests).HasMaxLength(1000); entity.Property(x => x.InternalNotes).HasMaxLength(1000); entity.Property(x => x.CancellationReason).HasMaxLength(300); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.ReservationNumber }).IsUnique().HasDatabaseName("UQ_fo_DatPhong_CoSo_SoDatPhong"); entity.HasIndex(x => new { x.PropertyId, x.ArrivalDate, x.DepartureDate }).HasDatabaseName("IX_fo_DatPhong_CoSo_NgayO"); entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_DatPhong_CoSo_TrangThai"); entity.HasIndex(x => new { x.PropertyId, x.AssignedRoomId, x.ArrivalDate, x.DepartureDate }).HasDatabaseName("IX_fo_DatPhong_Phong_NgayO"); entity.HasIndex(x => new { x.PropertyId, x.BookingSourceProfileId, x.BookingChannelProfileId, x.MarketSegmentProfileId }).HasDatabaseName("IX_fo_DatPhong_CoSo_Nguon_Kenh_PhanKhuc"); entity.HasIndex(x => new { x.PropertyId, x.CompanyProfileId, x.TravelAgentProfileId }).HasDatabaseName("IX_fo_DatPhong_CoSo_DoiTac"); entity.HasOne<HotelBookingSourceProfile>().WithMany().HasForeignKey(x => x.BookingSourceProfileId).HasConstraintName("FK_fo_DatPhong_NguonDatPhongFO").OnDelete(DeleteBehavior.Restrict); entity.HasOne<HotelBookingChannelProfile>().WithMany().HasForeignKey(x => x.BookingChannelProfileId).HasConstraintName("FK_fo_DatPhong_KenhDatPhongFO").OnDelete(DeleteBehavior.Restrict); entity.HasOne<HotelMarketSegmentProfile>().WithMany().HasForeignKey(x => x.MarketSegmentProfileId).HasConstraintName("FK_fo_DatPhong_PhanKhucThiTruongFO").OnDelete(DeleteBehavior.Restrict); entity.HasOne<HotelCompanyProfile>().WithMany().HasForeignKey(x => x.CompanyProfileId).HasConstraintName("FK_fo_DatPhong_DoiTacCongTyFO").OnDelete(DeleteBehavior.Restrict); entity.HasOne<HotelTravelAgentProfile>().WithMany().HasForeignKey(x => x.TravelAgentProfileId).HasConstraintName("FK_fo_DatPhong_DaiLyDuLichFO").OnDelete(DeleteBehavior.Restrict); entity.HasOne<HotelCommissionRuleProfile>().WithMany().HasForeignKey(x => x.CommissionRuleProfileId).HasConstraintName("FK_fo_DatPhong_QuyTacHoaHongFO").OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<ReservationHistory>(entity => { entity.ToTable("LichSuDatPhong", "fo"); entity.HasKey(x => x.HistoryId).HasName("PK_fo_LichSuDatPhong"); entity.Property(x => x.Action).HasMaxLength(80); entity.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.ReasonCode).HasMaxLength(100); entity.Property(x => x.SafeSummary).HasMaxLength(300); entity.HasIndex(x => new { x.ReservationId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_LichSuDatPhong_DatPhong_Luc"); entity.HasOne<Reservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<HotelRoomType>(entity => { entity.ToTable("LoaiPhongKhachSan", "fo"); entity.HasKey(x => x.HotelRoomTypeId).HasName("PK_fo_LoaiPhongKhachSan"); entity.Property(x => x.Code).HasMaxLength(40); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_LoaiPhongKhachSan_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.IsActive }).HasDatabaseName("IX_fo_LoaiPhongKhachSan_CoSo_HoatDong"); entity.HasIndex(x => new { x.PropertyId, x.DisplayOrder }).HasDatabaseName("IX_fo_LoaiPhongKhachSan_CoSo_ThuTu"); });
        modelBuilder.Entity<HotelBuilding>(entity => { entity.ToTable("ToaNhaKhachSan", "fo"); entity.HasKey(x => x.HotelBuildingId).HasName("PK_fo_ToaNhaKhachSan"); entity.Property(x => x.Code).HasMaxLength(40); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ToaNhaKhachSan_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.IsActive }).HasDatabaseName("IX_fo_ToaNhaKhachSan_CoSo_HoatDong"); entity.HasIndex(x => new { x.PropertyId, x.DisplayOrder }).HasDatabaseName("IX_fo_ToaNhaKhachSan_CoSo_ThuTu"); });
        modelBuilder.Entity<HotelFloor>(entity => { entity.ToTable("TangKhachSan", "fo"); entity.HasKey(x => x.HotelFloorId).HasName("PK_fo_TangKhachSan"); entity.Property(x => x.Code).HasMaxLength(40); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.HotelBuildingId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_TangKhachSan_CoSo_ToaNha_Ma"); entity.HasIndex(x => new { x.PropertyId, x.HotelBuildingId, x.LevelNumber }).HasDatabaseName("IX_fo_TangKhachSan_CoSo_ToaNha_Tang"); entity.HasOne<HotelBuilding>().WithMany().HasForeignKey(x => x.HotelBuildingId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<HotelArea>(entity => { entity.ToTable("KhuVucKhachSan", "fo"); entity.HasKey(x => x.HotelAreaId).HasName("PK_fo_KhuVucKhachSan"); entity.Property(x => x.Code).HasMaxLength(40); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.AreaType).HasMaxLength(40); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_KhuVucKhachSan_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.HotelBuildingId, x.HotelFloorId }).HasDatabaseName("IX_fo_KhuVucKhachSan_CoSo_ViTri"); entity.HasOne<HotelBuilding>().WithMany().HasForeignKey(x => x.HotelBuildingId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<HotelFloor>().WithMany().HasForeignKey(x => x.HotelFloorId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<HotelReferenceCatalogItem>(entity => { entity.ToTable("DanhMucThamChieuKhachSan", "fo"); entity.HasKey(x => x.HotelReferenceCatalogItemId).HasName("PK_fo_DanhMucThamChieuKhachSan"); entity.Property(x => x.CatalogKey).HasMaxLength(80); entity.Property(x => x.Code).HasMaxLength(40); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.CatalogKey, x.Code }).IsUnique().HasDatabaseName("UQ_fo_DanhMucThamChieuKhachSan_CoSo_Nhom_Ma"); entity.HasIndex(x => new { x.PropertyId, x.CatalogKey, x.IsActive }).HasDatabaseName("IX_fo_DanhMucThamChieuKhachSan_CoSo_Nhom_HoatDong"); });
        modelBuilder.Entity<HotelMinibarItemProfile>(entity =>
        {
            entity.ToTable("HangMinibarFO", "fo");
            entity.HasKey(x => x.HotelMinibarItemProfileId).HasName("PK_fo_HangMinibarFO");
            entity.Property(x => x.ItemCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ItemName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ChargeCode).HasMaxLength(50);
            entity.Property(x => x.ServiceChargeCode).HasMaxLength(50);
            entity.Property(x => x.RetailPrice).HasPrecision(18, 2);
            entity.Property(x => x.VatRate).HasPrecision(9, 4);
            entity.Property(x => x.StandardQuantity).HasPrecision(18, 3);
            entity.Property(x => x.MinStock).HasPrecision(18, 3);
            entity.Property(x => x.MaxStock).HasPrecision(18, 3);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.ItemCode }).IsUnique().HasDatabaseName("UQ_fo_HangMinibarFO_CoSo_MaHang");
            entity.HasIndex(x => new { x.PropertyId, x.IsActive }).HasDatabaseName("IX_fo_HangMinibarFO_CoSo_HoatDong");
            entity.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_fo_HangMinibarFO_HangHoa");
        });
        modelBuilder.Entity<HotelRoomStatusRule>(entity => { entity.ToTable("QuyTacTrangThaiPhong", "fo"); entity.HasKey(x => x.HotelRoomStatusRuleId).HasName("PK_fo_QuyTacTrangThaiPhong"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.StatusDomain).HasMaxLength(12); entity.Property(x => x.FromStatus).HasMaxLength(80); entity.Property(x => x.ToStatus).HasMaxLength(80); entity.Property(x => x.RequiredRole).HasMaxLength(120); entity.Property(x => x.RequiredEvent).HasMaxLength(120); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_QuyTacTrangThaiPhong_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.StatusDomain, x.IsActive }).HasDatabaseName("IX_fo_QuyTacTrangThaiPhong_CoSo_LinhVuc_HoatDong"); });
        modelBuilder.Entity<HotelPropertyProfile>(entity => { entity.ToTable("HoSoCoSoLuuTru", "fo"); entity.HasKey(x => x.HotelPropertyProfileId).HasName("PK_fo_HoSoCoSoLuuTru"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(200); entity.Property(x => x.Address).HasMaxLength(500); entity.Property(x => x.Phone).HasMaxLength(40); entity.Property(x => x.Email).HasMaxLength(160); entity.Property(x => x.TaxCode).HasMaxLength(40); entity.Property(x => x.TimeZoneId).HasMaxLength(80); entity.Property(x => x.CurrencyCode).HasMaxLength(3); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => x.PropertyId).IsUnique().HasDatabaseName("UQ_fo_HoSoCoSoLuuTru_CoSo"); });
        modelBuilder.Entity<HotelStayPolicy>(entity => { entity.ToTable("ChinhSachLuuTru", "fo"); entity.HasKey(x => x.HotelStayPolicyId).HasName("PK_fo_ChinhSachLuuTru"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.DepositPercent).HasPrecision(5, 2); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChinhSachLuuTru_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_ChinhSachLuuTru_CoSo_HieuLuc"); });
        modelBuilder.Entity<HotelCheckInOutRule>(entity => { entity.ToTable("QuyTacNhanTraPhong", "fo"); entity.HasKey(x => x.HotelCheckInOutRuleId).HasName("PK_fo_QuyTacNhanTraPhong"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.EarlyCheckInApprovalRole).HasMaxLength(120); entity.Property(x => x.LateCheckOutApprovalRole).HasMaxLength(120); entity.Property(x => x.EarlyCheckInChargeCode).HasMaxLength(80); entity.Property(x => x.LateCheckOutChargeCode).HasMaxLength(80); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_QuyTacNhanTraPhong_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_QuyTacNhanTraPhong_CoSo_HieuLuc"); });
        modelBuilder.Entity<HotelRoomMoveUpgradeRule>(entity => { entity.ToTable("QuyTacDoiNangHangPhong", "fo"); entity.HasKey(x => x.HotelRoomMoveUpgradeRuleId).HasName("PK_fo_QuyTacDoiNangHangPhong"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.ApprovalRole).HasMaxLength(120); entity.Property(x => x.PriceDifferenceMode).HasMaxLength(40); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_QuyTacDoiNangHangPhong_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_QuyTacDoiNangHangPhong_CoSo_HieuLuc"); });
        modelBuilder.Entity<HotelPropertyDocument>(entity => { entity.ToTable("HoSoGiayPhepKhachSan", "fo"); entity.HasKey(x => x.HotelPropertyDocumentId).HasName("PK_fo_HoSoGiayPhepKhachSan"); entity.Property(x => x.DocumentType).HasMaxLength(80); entity.Property(x => x.DocumentNumber).HasMaxLength(120); entity.Property(x => x.IssuedBy).HasMaxLength(160); entity.Property(x => x.FileReference).HasMaxLength(500); entity.Property(x => x.Notes).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.DocumentType, x.DocumentNumber }).IsUnique().HasDatabaseName("UQ_fo_HoSoGiayPhepKhachSan_CoSo_Loai_So"); entity.HasIndex(x => new { x.PropertyId, x.ExpiryDate, x.IsActive }).HasDatabaseName("IX_fo_HoSoGiayPhepKhachSan_CoSo_HetHan"); });
        modelBuilder.Entity<HotelPromotionPackage>(entity => { entity.ToTable("KhuyenMaiGoiDichVu", "fo"); entity.HasKey(x => x.HotelPromotionPackageId).HasName("PK_fo_KhuyenMaiGoiDichVu"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.OfferType).HasMaxLength(40); entity.Property(x => x.DiscountMode).HasMaxLength(30); entity.Property(x => x.DiscountValue).HasPrecision(18, 2); entity.Property(x => x.IncludedServices).HasMaxLength(1000); entity.Property(x => x.BlackoutDates).HasMaxLength(1000); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_KhuyenMaiGoiDichVu_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_KhuyenMaiGoiDichVu_CoSo_HieuLuc"); });
        modelBuilder.Entity<HotelGroupPolicy>(entity => { entity.ToTable("ChinhSachDoanKhachSan", "fo"); entity.HasKey(x => x.HotelGroupPolicyId).HasName("PK_fo_ChinhSachDoanKhachSan"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.DepositPercent).HasPrecision(5, 2); entity.Property(x => x.DefaultRouting).HasMaxLength(40); entity.Property(x => x.DefaultBillingInstruction).HasMaxLength(1000); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChinhSachDoanKhachSan_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_ChinhSachDoanKhachSan_CoSo_HieuLuc"); });
        modelBuilder.Entity<HotelCashierShiftPolicy>(entity => { entity.ToTable("ChinhSachCaThuNgan", "fo"); entity.HasKey(x => x.HotelCashierShiftPolicyId).HasName("PK_fo_ChinhSachCaThuNgan"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.DefaultOpeningFloat).HasPrecision(18, 2); entity.Property(x => x.VarianceApprovalThreshold).HasPrecision(18, 2); entity.Property(x => x.SupervisorRole).HasMaxLength(120); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChinhSachCaThuNgan_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_ChinhSachCaThuNgan_CoSo_HieuLuc"); });
        modelBuilder.Entity<HotelNightAuditPolicy>(entity => { entity.ToTable("ChinhSachNightAudit", "fo"); entity.HasKey(x => x.HotelNightAuditPolicyId).HasName("PK_fo_ChinhSachNightAudit"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.SupervisorRole).HasMaxLength(120); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChinhSachNightAudit_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_ChinhSachNightAudit_CoSo_HieuLuc"); });
        modelBuilder.Entity<HotelApprovalMatrixRule>(entity => { entity.ToTable("MaTranPheDuyetKhachSan", "fo"); entity.HasKey(x => x.HotelApprovalMatrixRuleId).HasName("PK_fo_MaTranPheDuyetKhachSan"); entity.Property(x => x.Code).HasMaxLength(60); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Domain).HasMaxLength(80); entity.Property(x => x.Action).HasMaxLength(80); entity.Property(x => x.ThresholdAmount).HasPrecision(18, 2); entity.Property(x => x.RequiredRole).HasMaxLength(120); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_MaTranPheDuyetKhachSan_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Domain, x.Action, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_MaTranPheDuyetKhachSan_CoSo_NghiepVu"); });
        modelBuilder.Entity<HotelFOPermissionPolicy>(entity => { entity.ToTable("ChinhSachQuyenFO", "fo"); entity.HasKey(x => x.HotelFOPermissionPolicyId).HasName("PK_fo_ChinhSachQuyenFO"); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Module).HasMaxLength(80); entity.Property(x => x.Action).HasMaxLength(80); entity.Property(x => x.RequiredRole).HasMaxLength(120); entity.Property(x => x.DataScope).HasMaxLength(80); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChinhSachQuyenFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Module, x.Action, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_ChinhSachQuyenFO_CoSo_ChucNang"); });
        modelBuilder.Entity<HotelFormPrinterProfile>(entity => { entity.ToTable("MauBieuMayInFO", "fo"); entity.HasKey(x => x.HotelFormPrinterProfileId).HasName("PK_fo_MauBieuMayInFO"); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.DocumentType).HasMaxLength(80); entity.Property(x => x.TemplateReference).HasMaxLength(260); entity.Property(x => x.PrinterName).HasMaxLength(260); entity.Property(x => x.PaperSize).HasMaxLength(40); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_MauBieuMayInFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.DocumentType, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_MauBieuMayInFO_CoSo_NghiepVu"); });
        modelBuilder.Entity<HotelMessagingIntegrationProfile>(entity => { entity.ToTable("TichHopTinNhanFO", "fo"); entity.HasKey(x => x.HotelMessagingIntegrationProfileId).HasName("PK_fo_TichHopTinNhanFO"); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Channel).HasMaxLength(40); entity.Property(x => x.Provider).HasMaxLength(120); entity.Property(x => x.Endpoint).HasMaxLength(500); entity.Property(x => x.SecretReference).HasMaxLength(260); entity.Property(x => x.TemplateReference).HasMaxLength(260); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_TichHopTinNhanFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Channel, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_TichHopTinNhanFO_CoSo_Kenh"); });
        modelBuilder.Entity<HotelPostedCorrectionPolicy>(entity => { entity.ToTable("ChinhSachDieuChinhPosted", "fo"); entity.HasKey(x => x.HotelPostedCorrectionPolicyId).HasName("PK_fo_ChinhSachDieuChinhPosted"); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Domain).HasMaxLength(80); entity.Property(x => x.AllowedRoles).HasMaxLength(500); entity.Property(x => x.MaximumAmount).HasPrecision(18, 2); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChinhSachDieuChinhPosted_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Domain, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_ChinhSachDieuChinhPosted_CoSo_NghiepVu"); });
        modelBuilder.Entity<HotelBusinessRuleProfile>(entity => { entity.ToTable("QuyTacNghiepVuFO", "fo"); entity.HasKey(x => x.HotelBusinessRuleProfileId).HasName("PK_fo_QuyTacNghiepVuFO"); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Domain).HasMaxLength(80); entity.Property(x => x.ConditionReference).HasMaxLength(2000); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_QuyTacNghiepVuFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Domain, x.Priority, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_QuyTacNghiepVuFO_CoSo_UuTien"); });
        modelBuilder.Entity<HotelTaxServiceChargeRule>(entity => { entity.ToTable("QuyTacThuePhiDichVuFO", "fo"); entity.HasKey(x => x.HotelTaxServiceChargeRuleId).HasName("PK_fo_QuyTacThuePhiDichVuFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Rate).HasPrecision(9, 4); entity.Property(x => x.ApplicableService).HasMaxLength(120); entity.Property(x => x.ApplicableRatePlan).HasMaxLength(80); entity.Property(x => x.RoundingMode).HasMaxLength(40); entity.Property(x => x.AccountingMapping).HasMaxLength(160); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.Code }).IsUnique().HasDatabaseName("UQ_fo_QuyTacThuePhiDichVuFO_CoSo_Loai_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_QuyTacThuePhiDichVuFO_CoSo_HieuLuc"); });
        modelBuilder.Entity<HotelPaymentMethodProfile>(entity => { entity.ToTable("PhuongThucThanhToanFO", "fo"); entity.HasKey(x => x.HotelPaymentMethodProfileId).HasName("PK_fo_PhuongThucThanhToanFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.CurrencyCode).HasMaxLength(10); entity.Property(x => x.BankAccount).HasMaxLength(160); entity.Property(x => x.CardTerminal).HasMaxLength(120); entity.Property(x => x.FeeRate).HasPrecision(9, 4); entity.Property(x => x.AccountingMapping).HasMaxLength(160); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_PhuongThucThanhToanFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.IsActive }).HasDatabaseName("IX_fo_PhuongThucThanhToanFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelDepositPolicy>(entity => { entity.ToTable("ChinhSachDatCocFO", "fo"); entity.HasKey(x => x.HotelDepositPolicyId).HasName("PK_fo_ChinhSachDatCocFO"); entity.Property(x => x.DepositType).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Percentage).HasPrecision(9, 4); entity.Property(x => x.FixedAmount).HasPrecision(18, 2); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChinhSachDatCocFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.DepositType, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_ChinhSachDatCocFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelDiscountPolicy>(entity => { entity.ToTable("ChinhSachChietKhauFO", "fo"); entity.HasKey(x => x.HotelDiscountPolicyId).HasName("PK_fo_ChinhSachChietKhauFO"); entity.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Percentage).HasPrecision(9, 4); entity.Property(x => x.FixedAmount).HasPrecision(18, 2); entity.Property(x => x.MaximumDiscount).HasPrecision(18, 2); entity.Property(x => x.AllowedRoles).HasMaxLength(500); entity.Property(x => x.AccountingTreatment).HasMaxLength(160); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChinhSachChietKhauFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.DiscountType, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_ChinhSachChietKhauFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelChargeCodeProfile>(entity => { entity.ToTable("ChargeCodeFO", "fo"); entity.HasKey(x => x.HotelChargeCodeProfileId).HasName("PK_fo_ChargeCodeFO"); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Category).HasMaxLength(80); entity.Property(x => x.Unit).HasMaxLength(40); entity.Property(x => x.Price).HasPrecision(18, 2); entity.Property(x => x.TaxCode).HasMaxLength(80); entity.Property(x => x.ServiceChargeCode).HasMaxLength(80); entity.Property(x => x.RevenueDepartment).HasMaxLength(120); entity.Property(x => x.RevenueAccount).HasMaxLength(120); entity.Property(x => x.TaxAccount).HasMaxLength(120); entity.Property(x => x.CostCenter).HasMaxLength(120); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChargeCodeFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Category, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_ChargeCodeFO_CoSo_Nhom"); });
        modelBuilder.Entity<HotelFolioProfile>(entity => { entity.ToTable("LoaiFolioFO", "fo"); entity.HasKey(x => x.HotelFolioProfileId).HasName("PK_fo_LoaiFolioFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.ChargeRouting).HasMaxLength(500); entity.Property(x => x.PaymentRouting).HasMaxLength(500); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_LoaiFolioFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_LoaiFolioFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelGuestTypeProfile>(entity => { entity.ToTable("LoaiKhachFO", "fo"); entity.HasKey(x => x.HotelGuestTypeProfileId).HasName("PK_fo_LoaiKhachFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.RequiredFields).HasMaxLength(500); entity.Property(x => x.DefaultRateCode).HasMaxLength(80); entity.Property(x => x.DefaultPaymentMethodCode).HasMaxLength(80); entity.Property(x => x.DefaultMarketCode).HasMaxLength(80); entity.Property(x => x.DefaultSourceCode).HasMaxLength(80); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_LoaiKhachFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_LoaiKhachFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelProfileTypeProfile>(entity => { entity.ToTable("LoaiHoSoFO", "fo"); entity.HasKey(x => x.HotelProfileTypeProfileId).HasName("PK_fo_LoaiHoSoFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.RequiredFields).HasMaxLength(500); entity.Property(x => x.DefaultRateCode).HasMaxLength(80); entity.Property(x => x.DefaultPaymentMethodCode).HasMaxLength(80); entity.Property(x => x.DefaultMarketCode).HasMaxLength(80); entity.Property(x => x.DefaultSourceCode).HasMaxLength(80); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_LoaiHoSoFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_LoaiHoSoFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelVipLevelProfile>(entity => { entity.ToTable("CapVipFO", "fo"); entity.HasKey(x => x.HotelVipLevelProfileId).HasName("PK_fo_CapVipFO"); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.WelcomeBenefit).HasMaxLength(500); entity.Property(x => x.AmenityBenefit).HasMaxLength(500); entity.Property(x => x.DiscountPercent).HasPrecision(9, 4); entity.Property(x => x.NotificationChannels).HasMaxLength(300); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_CapVipFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.LevelNumber, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_CapVipFO_CoSo_Cap"); });
        modelBuilder.Entity<HotelGuestPreferenceProfile>(entity => { entity.ToTable("SoThichKhachFO", "fo"); entity.HasKey(x => x.HotelGuestPreferenceProfileId).HasName("PK_fo_SoThichKhachFO"); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Category).HasMaxLength(80); entity.Property(x => x.ValueKind).HasMaxLength(40); entity.Property(x => x.DefaultValue).HasMaxLength(500); entity.Property(x => x.Options).HasMaxLength(2000); entity.Property(x => x.Scope).HasMaxLength(40); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_SoThichKhachFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Category, x.Priority, x.IsActive }).HasDatabaseName("IX_fo_SoThichKhachFO_CoSo_Nhom"); });
        modelBuilder.Entity<HotelGuestRequestProfile>(entity => { entity.ToTable("YeuCauKhachFO", "fo"); entity.HasKey(x => x.HotelGuestRequestProfileId).HasName("PK_fo_YeuCauKhachFO"); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.RequestType).HasMaxLength(80); entity.Property(x => x.Department).HasMaxLength(120); entity.Property(x => x.AutoAssignment).HasMaxLength(160); entity.Property(x => x.NotificationChannels).HasMaxLength(500); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_YeuCauKhachFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.RequestType, x.Priority, x.IsActive }).HasDatabaseName("IX_fo_YeuCauKhachFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelReservationTypeProfile>(entity => { entity.ToTable("LoaiDatPhongFO", "fo"); entity.HasKey(x => x.HotelReservationTypeProfileId).HasName("PK_fo_LoaiDatPhongFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_LoaiDatPhongFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_LoaiDatPhongFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelBookingSourceProfile>(entity => { entity.ToTable("NguonDatPhongFO", "fo"); entity.HasKey(x => x.HotelBookingSourceProfileId).HasName("PK_fo_NguonDatPhongFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.DefaultMarketSegmentCode).HasMaxLength(80); entity.Property(x => x.TrackingCode).HasMaxLength(120); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_NguonDatPhongFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_NguonDatPhongFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelBookingChannelProfile>(entity => { entity.ToTable("KenhDatPhongFO", "fo"); entity.HasKey(x => x.HotelBookingChannelProfileId).HasName("PK_fo_KenhDatPhongFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.ConnectorKey).HasMaxLength(120); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_KenhDatPhongFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_KenhDatPhongFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelMarketSegmentProfile>(entity => { entity.ToTable("PhanKhucThiTruongFO", "fo"); entity.HasKey(x => x.HotelMarketSegmentProfileId).HasName("PK_fo_PhanKhucThiTruongFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.RevenueBucket).HasMaxLength(100); entity.Property(x => x.DefaultRateCode).HasMaxLength(80); entity.Property(x => x.ForecastCategory).HasMaxLength(100); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_PhanKhucThiTruongFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_PhanKhucThiTruongFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelHousekeepingRuleProfile>(entity => { entity.ToTable("QuyTacHousekeepingFO", "fo"); entity.HasKey(x => x.HotelHousekeepingRuleProfileId).HasName("PK_fo_QuyTacHousekeepingFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Department).HasMaxLength(100); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_QuyTacHousekeepingFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_QuyTacHousekeepingFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelWakeupRuleProfile>(entity => { entity.ToTable("QuyTacWakeupFO", "fo"); entity.HasKey(x => x.HotelWakeupRuleProfileId).HasName("PK_fo_QuyTacWakeupFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.NotificationChannel).HasMaxLength(80); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_QuyTacWakeupFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_QuyTacWakeupFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelComplaintProfile>(entity => { entity.ToTable("DanhMucKhieuNaiFO", "fo"); entity.HasKey(x => x.HotelComplaintProfileId).HasName("PK_fo_DanhMucKhieuNaiFO"); entity.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Category).HasMaxLength(100); entity.Property(x => x.Department).HasMaxLength(100); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_DanhMucKhieuNaiFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Severity, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_DanhMucKhieuNaiFO_CoSo_Muc"); });
        modelBuilder.Entity<HotelCompanyProfile>(entity => { entity.ToTable("DoiTacCongTyFO", "fo"); entity.HasKey(x => x.HotelCompanyProfileId).HasName("PK_fo_DoiTacCongTyFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(200); entity.Property(x => x.TaxCode).HasMaxLength(40); entity.Property(x => x.BusinessLicense).HasMaxLength(80); entity.Property(x => x.ContactName).HasMaxLength(160); entity.Property(x => x.Phone).HasMaxLength(40); entity.Property(x => x.Email).HasMaxLength(320); entity.Property(x => x.Address).HasMaxLength(500); entity.Property(x => x.CurrencyCode).HasMaxLength(10); entity.Property(x => x.CreditLimit).HasPrecision(18, 2); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_DoiTacCongTyFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_DoiTacCongTyFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelTravelAgentProfile>(entity => { entity.ToTable("DaiLyDuLichFO", "fo"); entity.HasKey(x => x.HotelTravelAgentProfileId).HasName("PK_fo_DaiLyDuLichFO"); entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(200); entity.Property(x => x.IataCode).HasMaxLength(40); entity.Property(x => x.LicenseNumber).HasMaxLength(80); entity.Property(x => x.ContactName).HasMaxLength(160); entity.Property(x => x.Phone).HasMaxLength(40); entity.Property(x => x.Email).HasMaxLength(320); entity.Property(x => x.Address).HasMaxLength(500); entity.Property(x => x.CommissionRuleCode).HasMaxLength(80); entity.Property(x => x.DefaultMarketSegmentCode).HasMaxLength(80); entity.Property(x => x.PaymentTerms).HasMaxLength(40); entity.Property(x => x.CreditLimit).HasPrecision(18, 2); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_DaiLyDuLichFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Kind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_DaiLyDuLichFO_CoSo_Loai"); });
        modelBuilder.Entity<HotelCommissionRuleProfile>(entity => { entity.ToTable("QuyTacHoaHongFO", "fo"); entity.HasKey(x => x.HotelCommissionRuleProfileId).HasName("PK_fo_QuyTacHoaHongFO"); entity.Property(x => x.PartnerKind).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Basis).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.CurrencyCode).HasMaxLength(10); entity.Property(x => x.RatePercent).HasPrecision(9, 4); entity.Property(x => x.FixedAmount).HasPrecision(18, 2); entity.Property(x => x.MinimumAmount).HasPrecision(18, 2); entity.Property(x => x.MaximumAmount).HasPrecision(18, 2); entity.Property(x => x.Description).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_QuyTacHoaHongFO_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.PartnerKind, x.EffectiveFrom, x.EffectiveTo, x.IsActive }).HasDatabaseName("IX_fo_QuyTacHoaHongFO_CoSo_DoiTac"); });
        modelBuilder.Entity<HotelRoom>(entity => { entity.ToTable("PhongKhachSan", "fo"); entity.HasKey(x => x.HotelRoomId).HasName("PK_fo_PhongKhachSan"); entity.Property(x => x.RoomNumber).HasMaxLength(40); entity.Property(x => x.DisplayName).HasMaxLength(160); entity.Property(x => x.Floor).HasMaxLength(40); entity.Property(x => x.Building).HasMaxLength(80); entity.Property(x => x.Zone).HasMaxLength(80); entity.Property(x => x.Description).HasMaxLength(1000); entity.Property(x => x.RoomCondition).HasMaxLength(30); entity.Property(x => x.View).HasMaxLength(80); entity.Property(x => x.BedType).HasMaxLength(40); entity.Property(x => x.OperationalStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.OperationalStatusReason).HasMaxLength(500); entity.Property(x => x.HousekeepingStatus).HasMaxLength(30); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.RoomNumber }).IsUnique().HasDatabaseName("UQ_fo_PhongKhachSan_CoSo_SoPhong"); entity.HasIndex(x => new { x.PropertyId, x.HotelRoomTypeId }).HasDatabaseName("IX_fo_PhongKhachSan_CoSo_LoaiPhong"); entity.HasIndex(x => new { x.PropertyId, x.HotelAreaId }).HasDatabaseName("IX_fo_PhongKhachSan_CoSo_KhuVuc"); entity.HasIndex(x => new { x.PropertyId, x.OperationalStatus }).HasDatabaseName("IX_fo_PhongKhachSan_CoSo_TrangThai"); entity.HasIndex(x => new { x.PropertyId, x.OperationalStatus, x.OperationalStatusExpiresAtUtc }).HasDatabaseName("IX_fo_PhongKhachSan_CoSo_TrangThai_HetHan"); entity.HasIndex(x => new { x.PropertyId, x.IsActive }).HasDatabaseName("IX_fo_PhongKhachSan_CoSo_HoatDong"); entity.HasOne<HotelRoomType>().WithMany().HasForeignKey(x => x.HotelRoomTypeId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<HotelArea>().WithMany().HasForeignKey(x => x.HotelAreaId).OnDelete(DeleteBehavior.Restrict); });
        // U-014: Night Audit owns the business date lifecycle. The existing domain model
        // is mapped directly to fo.NgayKinhDoanh so Front Office has one source of truth.
        modelBuilder.Entity<BusinessDateRecord>(entity =>
        {
            entity.ToTable("NgayKinhDoanh", "fo");
            entity.HasKey(x => x.BusinessDateId).HasName("PK_fo_NgayKinhDoanh");
            entity.Property(x => x.BusinessDate).HasColumnType("date");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.OpenedAtUtc).HasColumnType("datetime2");
            entity.Property(x => x.ClosedAtUtc).HasColumnType("datetime2");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.BusinessDate })
                .IsUnique()
                .HasDatabaseName("UQ_fo_NgayKinhDoanh_CoSo_Ngay");
        });
        modelBuilder.Entity<NightAudit>(entity =>
        {
            entity.ToTable("KiemDem", "fo");
            entity.HasKey(x => x.NightAuditId).HasName("PK_fo_KiemDem");
            entity.Property(x => x.AuditNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.BusinessDate).HasColumnType("date");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.CurrentStep).HasConversion<int>();
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
            entity.Property(x => x.RoomRevenue).HasPrecision(18, 2);
            entity.Property(x => x.TaxPosted).HasPrecision(18, 2);
            entity.Property(x => x.ServiceChargePosted).HasPrecision(18, 2);
            entity.Property(x => x.StartedAtUtc).HasColumnType("datetime2");
            entity.Property(x => x.CompletedAtUtc).HasColumnType("datetime2");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => x.AuditNumber).IsUnique().HasDatabaseName("UQ_fo_KiemDem_SoKiemDem");
            entity.HasIndex(x => new { x.PropertyId, x.BusinessDate }).HasDatabaseName("IX_fo_KiemDem_CoSo_Ngay");
            entity.HasMany(x => x.Steps)
                .WithOne()
                .HasForeignKey(x => x.NightAuditId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(x => x.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Ignore(x => x.Validations);
        });
        modelBuilder.Entity<NightAuditStepExecution>(entity =>
        {
            entity.ToTable("BuocKiemDem", "fo");
            entity.HasKey(x => x.NightAuditStepExecutionId).HasName("PK_fo_BuocKiemDem");
            entity.Property(x => x.Step).HasConversion<int>();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(1000);
            entity.Property(x => x.CompletedAtUtc).HasColumnType("datetime2");
            entity.HasIndex(x => new { x.NightAuditId, x.Step })
                .IsUnique()
                .HasDatabaseName("UQ_fo_BuocKiemDem_Audit_Step");
        });
        modelBuilder.Entity<RevenueSnapshot>(entity =>
        {
            entity.ToTable("DoanhThuNgay", "fo");
            entity.HasKey(x => x.RevenueSnapshotId).HasName("PK_fo_DoanhThuNgay");
            entity.Property(x => x.BusinessDate).HasColumnType("date");
            entity.Property(x => x.RoomRevenue).HasPrecision(18, 2);
            entity.Property(x => x.OtherRevenue).HasPrecision(18, 2);
            entity.Property(x => x.TotalRevenue).HasPrecision(18, 2);
            entity.Property(x => x.TotalTax).HasPrecision(18, 2);
            entity.Property(x => x.TotalPayments).HasPrecision(18, 2);
            entity.Property(x => x.TotalRefunds).HasPrecision(18, 2);
            entity.Property(x => x.OutstandingBalance).HasPrecision(18, 2);
            entity.Property(x => x.OccupancyRate).HasPrecision(9, 4);
            entity.Property(x => x.ADR).HasPrecision(18, 2);
            entity.Property(x => x.RevPAR).HasPrecision(18, 2);
            entity.Property(x => x.SnapshotJson).IsRequired();
            entity.HasIndex(x => x.NightAuditId).HasDatabaseName("IX_fo_DoanhThuNgay_KiemDem");
            entity.HasIndex(x => new { x.PropertyId, x.BusinessDate })
                .IsUnique()
                .HasDatabaseName("UQ_fo_DoanhThuNgay_CoSo_Ngay");
            entity.HasOne<NightAudit>()
                .WithMany()
                .HasForeignKey(x => x.NightAuditId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HotelStay>(entity => { entity.ToTable("LuuTruKhachSan", "fo"); entity.HasKey(x => x.HotelStayId).HasName("PK_fo_LuuTruKhachSan"); entity.Property(x => x.StayNumber).HasMaxLength(40); entity.Property(x => x.ReservationNumberSnapshot).HasMaxLength(40); entity.Property(x => x.GuestNameSnapshot).HasMaxLength(200); entity.Property(x => x.GuestPhoneSnapshot).HasMaxLength(50); entity.Property(x => x.RoomNumberSnapshot).HasMaxLength(40); entity.Property(x => x.RoomTypeCodeSnapshot).HasMaxLength(40); entity.Property(x => x.Source).HasMaxLength(80); entity.Property(x => x.InternalNotes).HasMaxLength(1000); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => x.StayNumber).IsUnique().HasDatabaseName("UQ_fo_LuuTruKhachSan_SoLuuTru"); entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_LuuTruKhachSan_CoSo_TrangThai"); entity.HasIndex(x => new { x.PropertyId, x.ScheduledArrivalDate }).HasDatabaseName("IX_fo_LuuTruKhachSan_CoSo_NgayDen"); entity.HasIndex(x => new { x.PropertyId, x.ScheduledDepartureDate }).HasDatabaseName("IX_fo_LuuTruKhachSan_CoSo_NgayDi"); entity.HasIndex(x => new { x.PropertyId, x.RoomId, x.Status }).HasDatabaseName("IX_fo_LuuTruKhachSan_CoSo_Phong_TrangThai"); entity.HasIndex(x => x.ReservationId).HasDatabaseName("IX_fo_LuuTruKhachSan_DatPhong"); entity.HasIndex(x => x.UpdatedAtUtc).HasDatabaseName("IX_fo_LuuTruKhachSan_CapNhat"); entity.HasIndex(x => x.ReservationId).HasFilter("[ReservationId] IS NOT NULL AND [Status] IN ('PendingArrival', 'InHouse')").IsUnique().HasDatabaseName("UQ_fo_LuuTruKhachSan_DatPhong_HoatDong"); entity.HasOne<Reservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<HotelRoomType>().WithMany().HasForeignKey(x => x.RoomTypeId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<HotelStayHistory>(entity => { entity.ToTable("LichSuLuuTruKhachSan", "fo"); entity.HasKey(x => x.HistoryId).HasName("PK_fo_LichSuLuuTruKhachSan"); entity.Property(x => x.Action).HasMaxLength(80); entity.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.SafeMetadata).HasMaxLength(300); entity.HasIndex(x => new { x.HotelStayId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_LichSuLuuTruKhachSan_LuuTru_Luc"); entity.HasOne<HotelStay>().WithMany().HasForeignKey(x => x.HotelStayId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<HotelStayOccupant>(entity =>
        {
            entity.ToTable("KhachDiCungLuuTru", "fo");
            entity.HasKey(x => x.OccupantId).HasName("PK_fo_KhachDiCungLuuTru");
            entity.Property(x => x.GuestNameSnapshot).HasMaxLength(200).IsRequired();
            entity.Property(x => x.GuestPhoneSnapshot).HasMaxLength(50);
            entity.Property(x => x.OccupantType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.HotelStayId, x.IsActive }).HasDatabaseName("IX_fo_KhachDiCungLuuTru_CoSo_LuuTru_HoatDong");
            entity.HasIndex(x => new { x.PropertyId, x.HotelStayId, x.GuestProfileId, x.IsActive })
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UQ_fo_KhachDiCungLuuTru_CoSo_LuuTru_Khach_HoatDong");
            entity.HasOne<HotelStay>().WithMany().HasForeignKey(x => x.HotelStayId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<GuestProfile>().WithMany().HasForeignKey(x => x.GuestProfileId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HotelFolio>(entity =>
        {
            entity.ToTable("SoFolio", "fo");
            entity.HasKey(x => x.HotelFolioId).HasName("PK_fo_SoFolio");
            entity.Property(x => x.FolioNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.Ignore(x => x.SettledAtUtc);
            entity.Ignore(x => x.SettledBy);
            entity.Ignore(x => x.ArchivedAtUtc);
            entity.HasIndex(x => x.FolioNumber).IsUnique().HasDatabaseName("UQ_fo_SoFolio_SoFolio");
            entity.HasIndex(x => x.HotelStayId);
            entity.HasIndex(x => new { x.PropertyId, x.HotelStayId }).IsUnique().HasDatabaseName("UQ_fo_SoFolio_CoSo_LuuTru");
            entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_SoFolio_CoSo_TrangThai");
            entity.HasOne<HotelStay>().WithMany().HasForeignKey(x => x.HotelStayId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HotelFolioRoutingRule>(entity =>
        {
            entity.ToTable("QuyTacChuyenFolio", "fo");
            entity.HasKey(x => x.HotelFolioRoutingRuleId).HasName("PK_fo_QuyTacChuyenFolio");
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.EntryType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Percent).HasColumnType("decimal(9,4)");
            entity.Property(x => x.Notes).HasMaxLength(1000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.SourceFolioId, x.IsActive, x.Priority })
                .HasDatabaseName("IX_fo_QuyTacChuyenFolio_CoSo_Nguon_HieuLuc");
            entity.HasIndex(x => new { x.PropertyId, x.SourceFolioId, x.TargetFolioId, x.EntryType, x.IsActive })
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UQ_fo_QuyTacChuyenFolio_CoSo_NguonDich_Loai_HieuLuc");
            entity.HasOne<HotelFolio>().WithMany().HasForeignKey(x => x.SourceFolioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<HotelFolio>().WithMany().HasForeignKey(x => x.TargetFolioId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HotelFolioEntry>(entity =>
        {
            entity.ToTable("ButToanFolio", "fo");
            entity.HasKey(x => x.HotelFolioEntryId).HasName("PK_fo_ButToanFolio");
            entity.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ReferenceType).HasMaxLength(80);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            // BusinessDate is the canonical posting date used by Night Audit.
            // Keep it persisted so financial metrics are grouped by the hotel's
            // business date instead of relying on CreatedAtUtc fallbacks.
            entity.Property(x => x.BusinessDate)
                .HasColumnType("date")
                .HasColumnName("BusinessDate");
            entity.HasIndex(x => new { x.HotelFolioId, x.CreatedAtUtc }).HasDatabaseName("IX_fo_ButToanFolio_Folio_ThoiGian");
            entity.HasIndex(x => new { x.HotelFolioId, x.SequenceNumber }).IsUnique().HasDatabaseName("UQ_fo_ButToanFolio_Folio_Sequence");
            entity.HasIndex(x => new { x.PropertyId, x.HotelFolioId }).HasDatabaseName("IX_fo_ButToanFolio_CoSo_Folio");
            entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL").HasDatabaseName("UQ_fo_ButToanFolio_Idempotency");
            entity.HasOne(x => x.HotelFolio).WithMany(x => x.Entries).HasForeignKey(x => x.HotelFolioId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HotelInvoice>(entity =>
        {
            entity.ToTable("HoaDon", "fo");
            entity.HasKey(x => x.HotelInvoiceId).HasName("PK_fo_HoaDon");
            entity.Property(x => x.InvoiceNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.OutstandingAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.StayDates).HasMaxLength(200);
            entity.Property(x => x.InvoiceDataJson).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => x.InvoiceNumber).IsUnique().HasDatabaseName("UQ_fo_HoaDon_SoHoaDon");
            entity.HasIndex(x => new { x.PropertyId, x.HotelFolioId }).HasDatabaseName("IX_fo_HoaDon_CoSo_Folio");
        });
        modelBuilder.Entity<HotelPayment>(entity =>
        {
            entity.ToTable("ThanhToan", "fo");
            entity.HasKey(x => x.HotelPaymentId).HasName("PK_fo_ThanhToan");
            entity.Property(x => x.PaymentNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Currency).HasConversion<string>().HasMaxLength(10).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.RefundedAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ExchangeRateToVnd).HasColumnType("decimal(18,6)");
            entity.Property(x => x.ExternalReference).HasMaxLength(200);
            entity.Property(x => x.AuthorizationCode).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.VoidReason).HasMaxLength(500);
            entity.Property(x => x.RefundReason).HasMaxLength(500);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.Ignore(x => x.AllocatedAmount);
            entity.Ignore(x => x.UnallocatedAmount);
            entity.HasIndex(x => x.PaymentNumber).IsUnique().HasDatabaseName("UQ_fo_ThanhToan_SoThanhToan");
            entity.HasOne<HotelFolio>().WithMany().HasForeignKey(x => x.HotelFolioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Allocations).WithOne().HasForeignKey(x => x.HotelPaymentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.History).WithOne().HasForeignKey(x => x.HotelPaymentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HotelPaymentAllocation>(entity =>
        {
            entity.ToTable("PhanBoThanhToan", "fo");
            entity.HasKey(x => x.HotelPaymentAllocationId).HasName("PK_fo_PhanBoThanhToan");
            entity.Property(x => x.ChargeDescription).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne<HotelFolioEntry>().WithMany().HasForeignKey(x => x.ChargeFolioEntryId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PaymentHistory>(entity =>
        {
            entity.ToTable("LichSuThanhToan", "fo");
            entity.HasKey(x => x.PaymentHistoryId).HasName("PK_fo_LichSuThanhToan");
            entity.Property(x => x.PaymentHistoryId).ValueGeneratedNever();
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
    entity.Property(x => x.Detail).HasMaxLength(500).IsRequired(false);
        });
        // U-005: map the existing guest master tables without changing their live schema.
        // Nationality/Country are explicitly capped at the live 80-character size and
        // RowVersion remains nullable because the existing table was created that way.
        modelBuilder.Entity<GuestProfile>(entity =>
        {
            entity.ToTable("HoSoKhach", "fo");
            entity.HasKey(x => x.GuestProfileId);
            entity.Property(x => x.GuestNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.Nationality).HasMaxLength(80);
            entity.Property(x => x.Country).HasMaxLength(80);
            entity.Property(x => x.Gender).HasMaxLength(20);
            entity.Property(x => x.EmergencyContact).HasMaxLength(200);
            entity.Property(x => x.EmergencyPhone).HasMaxLength(50);
            entity.Property(x => x.VipLevel).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.TotalRevenue).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AverageSpend).HasColumnType("decimal(18,2)");
            entity.Property(x => x.BlacklistReason).HasMaxLength(500);
            entity.Property(x => x.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.CompanyName).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.GuestNumber }).IsUnique();
            entity.HasIndex(x => new { x.PropertyId, x.FullName });
            entity.HasIndex(x => x.Phone);
            entity.HasIndex(x => x.Email);
        });
        modelBuilder.Entity<GuestDocument>(entity =>
        {
            entity.ToTable("GiayToKhach", "fo");
            entity.HasKey(x => x.GuestDocumentId);
            entity.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.IssuingCountry).HasMaxLength(80);
            entity.HasIndex(x => new { x.PropertyId, x.GuestProfileId });
            entity.HasOne(x => x.GuestProfile).WithMany(x => x.Documents).HasForeignKey(x => x.GuestProfileId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<GuestFeedback>(entity =>
        {
            entity.ToTable("PhanHoiKhach", "fo");
            entity.HasKey(x => x.GuestFeedbackId).HasName("PK_fo_PhanHoiKhach");
            entity.Property(x => x.FeedbackNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.FeedbackType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(80);
            entity.Property(x => x.Subject).HasMaxLength(255);
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.AssignedDepartment).HasMaxLength(120);
            entity.Property(x => x.Resolution).HasMaxLength(4000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.FeedbackNumber }).IsUnique().HasDatabaseName("UQ_fo_PhanHoiKhach_CoSo_SoPhanHoi");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.CreatedAtUtc }).HasDatabaseName("IX_fo_PhanHoiKhach_CoSo_TrangThai_ThoiGian");
            entity.HasIndex(x => new { x.PropertyId, x.RoomNumber, x.CreatedAtUtc }).HasDatabaseName("IX_fo_PhanHoiKhach_CoSo_Phong_ThoiGian");
            entity.HasMany(x => x.History).WithOne().HasForeignKey(x => x.GuestFeedbackId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.History).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.Entity<GuestFeedbackHistory>(entity =>
        {
            entity.ToTable("LichSuPhanHoiKhach", "fo");
            entity.HasKey(x => x.GuestFeedbackHistoryId).HasName("PK_fo_LichSuPhanHoiKhach");
            entity.Property(x => x.Action).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.PropertyId, x.GuestFeedbackId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_LichSuPhanHoiKhach_CoSo_PhanHoi_ThoiGian");
        });
        modelBuilder.Entity<VipProfile>(entity =>
        {
            entity.ToTable("HoSoVIP", "fo");
            entity.HasKey(x => x.VipProfileId).HasName("PK_fo_HoSoVIP");
            entity.Property(x => x.VipNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.Level).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Preferences).HasMaxLength(2000);
            entity.Property(x => x.SpecialRequests).HasMaxLength(2000);
            entity.Property(x => x.AssignedAccountManager).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.TotalRevenue).HasColumnType("decimal(18,2)");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.VipNumber }).IsUnique().HasDatabaseName("UQ_fo_HoSoVIP_CoSo_MaVIP");
            entity.HasIndex(x => new { x.PropertyId, x.Level, x.Status }).HasDatabaseName("IX_fo_HoSoVIP_CoSo_Hang_TrangThai");
            entity.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.VipProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.Entity<LoyaltyTransaction>(entity =>
        {
            entity.ToTable("GiaoDichLoyalty", "fo");
            entity.HasKey(x => x.LoyaltyTransactionId).HasName("PK_fo_GiaoDichLoyalty");
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.PropertyId, x.VipProfileId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_GiaoDichLoyalty_CoSo_VIP_ThoiGian");
        });
        modelBuilder.Entity<GuestPreference>(entity =>
        {
            entity.ToTable("SoThichKhach", "fo");
            entity.HasKey(x => x.GuestPreferenceId);
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.PropertyId, x.GuestProfileId });
            entity.HasOne(x => x.GuestProfile).WithMany(x => x.Preferences).HasForeignKey(x => x.GuestProfileId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RatePlan>(entity => { entity.ToTable("KeHoachGia", "fo"); entity.HasKey(x => x.RatePlanId).HasName("PK_fo_KeHoachGia"); entity.Property(x => x.PlanCode).HasMaxLength(40); entity.Property(x => x.PlanName).HasMaxLength(160); entity.Property(x => x.BaseRate).HasColumnType("decimal(18,2)"); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Hierarchy).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.YieldMinRate).HasColumnType("decimal(18,2)"); entity.Property(x => x.YieldMaxRate).HasColumnType("decimal(18,2)"); entity.Property(x => x.YieldAdjustmentPct).HasColumnType("decimal(8,4)"); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.PlanCode }).IsUnique().HasDatabaseName("UQ_fo_KeHoachGia_CoSo_Ma"); entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_KeHoachGia_CoSo_TrangThai"); });
        modelBuilder.Entity<RateCalendarEntry>(entity => { entity.ToTable("LichGia", "fo"); entity.HasKey(x => x.RateCalendarEntryId).HasName("PK_fo_LichGia"); entity.Property(x => x.Rate).HasColumnType("decimal(18,2)"); entity.HasIndex(x => new { x.RatePlanId, x.Date }).IsUnique().HasDatabaseName("UQ_fo_LichGia_KeHoach_Ngay"); entity.HasOne(x => x.RatePlan).WithMany(x => x.Calendar).HasForeignKey(x => x.RatePlanId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<RateRule>(entity => { entity.ToTable("QuyTacGia", "fo"); entity.HasKey(x => x.RateRuleId).HasName("PK_fo_QuyTacGia"); entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(50); entity.Property(x => x.Value).HasColumnType("decimal(18,4)"); entity.Property(x => x.Description).HasMaxLength(500); entity.HasIndex(x => new { x.RatePlanId, x.Priority }).HasDatabaseName("IX_fo_QuyTacGia_KeHoach_UuTien"); entity.HasOne(x => x.RatePlan).WithMany(x => x.Rules).HasForeignKey(x => x.RatePlanId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<RateSeason>(entity => { entity.ToTable("MuaGia", "fo"); entity.HasKey(x => x.RateSeasonId); entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Multiplier).HasColumnType("decimal(8,4)"); entity.Property(x => x.Description).HasMaxLength(500); entity.HasIndex(x => new { x.RatePlanId, x.FromDate, x.ToDate }).HasDatabaseName("IX_fo_MuaGia_KeHoach_Ngay"); entity.HasOne(x => x.RatePlan).WithMany(x => x.Seasons).HasForeignKey(x => x.RatePlanId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<HotelExchangeRate>(entity => { entity.ToTable("TyGia", "fo"); entity.HasKey(x => x.HotelExchangeRateId).HasName("PK_fo_TyGia"); entity.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired(); entity.Property(x => x.CurrencyName).HasMaxLength(160).IsRequired(); entity.Property(x => x.Symbol).HasMaxLength(20); entity.Property(x => x.BuyRate).HasColumnType("decimal(18,6)"); entity.Property(x => x.SellRate).HasColumnType("decimal(18,6)"); entity.Property(x => x.HotelRate).HasColumnType("decimal(18,6)"); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Notes).HasMaxLength(1000); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.CurrencyCode, x.EffectiveFromUtc }).IsUnique().HasDatabaseName("UQ_fo_TyGia_CoSo_Ma_TuNgay"); entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_TyGia_CoSo_TrangThai"); entity.HasIndex(x => new { x.PropertyId, x.EffectiveFromUtc, x.EffectiveToUtc }).HasDatabaseName("IX_fo_TyGia_CoSo_HieuLuc"); });
        modelBuilder.Entity<MarketingRecord>(entity =>
        {
            entity.ToTable("TiepThi", "fo");
            entity.HasKey(x => x.MarketingRecordId).HasName("PK_fo_TiepThi");
            entity.Property(x => x.Section).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.GroupName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Owner).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Revenue).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
            entity.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.Section, x.Code }).IsUnique().HasDatabaseName("UQ_fo_TiepThi_CoSo_PhanLoai_Ma");
            entity.HasIndex(x => new { x.PropertyId, x.Section, x.Status }).HasDatabaseName("IX_fo_TiepThi_CoSo_PhanLoai_TrangThai");
            entity.HasIndex(x => new { x.PropertyId, x.GroupName }).HasDatabaseName("IX_fo_TiepThi_CoSo_Nhom");
        });
        modelBuilder.Entity<GroupReservation>(entity => { entity.ToTable("DatPhongDoan", "fo"); entity.HasKey(x => x.GroupReservationId).HasName("PK_fo_DatPhongDoan"); entity.Property(x => x.GroupCode).HasMaxLength(40); entity.Property(x => x.GroupName).HasMaxLength(200); entity.Property(x => x.CompanyName).HasMaxLength(200); entity.Property(x => x.AgencyName).HasMaxLength(200); entity.Property(x => x.ContactPerson).HasMaxLength(200); entity.Property(x => x.ContactPhone).HasMaxLength(50); entity.Property(x => x.ContactEmail).HasMaxLength(200); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Source).HasMaxLength(80); entity.Property(x => x.SalesManager).HasMaxLength(200); entity.Property(x => x.MarketSegment).HasMaxLength(80); entity.Property(x => x.Routing).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.SpecialRequests).HasMaxLength(1000); entity.Property(x => x.Notes).HasMaxLength(1000); entity.Property(x => x.GuaranteeType).HasMaxLength(40); entity.Property(x => x.TotalDeposit).HasColumnType("decimal(18,2)"); entity.Property(x => x.TotalRevenue).HasColumnType("decimal(18,2)"); entity.Property(x => x.OutstandingBalance).HasColumnType("decimal(18,2)"); entity.Property(x => x.SplitPercentage).HasColumnType("decimal(5,2)"); ConfigureRowVersion(entity.Property(x => x.RowVersion)); entity.HasIndex(x => new { x.PropertyId, x.GroupCode }).IsUnique().HasDatabaseName("UQ_fo_DatPhongDoan_CoSo_MaDoan"); entity.HasIndex(x => new { x.PropertyId, x.ArrivalDate, x.DepartureDate }).HasDatabaseName("IX_fo_DatPhongDoan_CoSo_NgayO"); entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_DatPhongDoan_CoSo_TrangThai"); });
        modelBuilder.Entity<GroupDeposit>(entity =>
        {
            entity.ToTable("DatCocDoan", "fo");
            entity.HasKey(x => x.GroupDepositId).HasName("PK_fo_DatCocDoan");
            entity.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.PaymentMethod).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ExternalReference).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.ReversalReason).HasMaxLength(500);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.GroupReservationId, x.BusinessDate }).HasDatabaseName("IX_fo_DatCocDoan_CoSo_Doan_Ngay");
            entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_DatCocDoan_CoSo_TrangThai");
            entity.HasOne(x => x.GroupReservation).WithMany().HasForeignKey(x => x.GroupReservationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<GroupDepositSchedule>(entity =>
        {
            entity.ToTable("LichDatCocDoan", "fo");
            entity.HasKey(x => x.GroupDepositScheduleId).HasName("PK_fo_LichDatCocDoan");
            entity.Property(x => x.Percent).HasColumnType("decimal(9,2)");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.GroupReservationId, x.DueDate }).HasDatabaseName("IX_fo_LichDatCocDoan_CoSo_Doan_Han");
            entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_LichDatCocDoan_CoSo_TrangThai");
            entity.HasOne(x => x.GroupReservation).WithMany(x => x.DepositSchedules).HasForeignKey(x => x.GroupReservationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<GroupRoomAssignment>(entity => { entity.ToTable("PhongDoan", "fo"); entity.HasKey(x => x.GroupRoomAssignmentId).HasName("PK_fo_PhongDoan"); entity.Property(x => x.RoomNumber).HasMaxLength(40); entity.Property(x => x.RoomType).HasMaxLength(80); entity.Property(x => x.GuestName).HasMaxLength(200); entity.HasIndex(x => new { x.GroupReservationId, x.HotelRoomId }).IsUnique().HasDatabaseName("UQ_fo_PhongDoan_Doan_Phong"); entity.HasOne(x => x.GroupReservation).WithMany(x => x.RoomingList).HasForeignKey(x => x.GroupReservationId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<GroupRoomBlock>(entity =>
        {
            entity.ToTable("RoomBlockDoan", "fo");
            entity.HasKey(x => x.GroupRoomBlockId).HasName("PK_fo_RoomBlockDoan");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.NegotiatedRate).HasColumnType("decimal(18,2)");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.GroupReservationId, x.HotelRoomTypeId, x.ArrivalDate, x.DepartureDate }).IsUnique().HasDatabaseName("UQ_fo_RoomBlockDoan_CoSo_Doan_Loai_Ngay");
            entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_RoomBlockDoan_CoSo_TrangThai");
            entity.HasIndex(x => new { x.PropertyId, x.CutoffDate }).HasDatabaseName("IX_fo_RoomBlockDoan_CoSo_HanChot");
            entity.HasOne(x => x.GroupReservation).WithMany(x => x.RoomBlocks).HasForeignKey(x => x.GroupReservationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<HotelRoomType>().WithMany().HasForeignKey(x => x.HotelRoomTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<GroupServiceOrder>(entity =>
        {
            entity.ToTable("DichVuDoan", "fo");
            entity.HasKey(x => x.GroupServiceOrderId).HasName("PK_fo_DichVuDoan");
            entity.Property(x => x.ServiceCode).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ServiceName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
            entity.Property(x => x.SubCategory).HasMaxLength(80);
            entity.Property(x => x.Department).HasMaxLength(80);
            entity.Property(x => x.Provider).HasMaxLength(160);
            entity.Property(x => x.Location).HasMaxLength(160);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.ChargeTo).HasMaxLength(40).IsRequired();
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.Remark).HasMaxLength(2000);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DiscountRate).HasColumnType("decimal(9,4)");
            entity.Property(x => x.TaxRate).HasColumnType("decimal(9,4)");
            entity.Property(x => x.ServiceChargeRate).HasColumnType("decimal(9,4)");
            entity.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.ServiceCode }).IsUnique().HasDatabaseName("UQ_fo_DichVuDoan_CoSo_MaDV");
            entity.HasIndex(x => new { x.PropertyId, x.GroupReservationId, x.ServiceDate }).HasDatabaseName("IX_fo_DichVuDoan_CoSo_Doan_Ngay");
            entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_DichVuDoan_CoSo_TrangThai");
            entity.HasOne(x => x.GroupReservation).WithMany().HasForeignKey(x => x.GroupReservationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FrontOfficeIncident>(entity =>
        {
            entity.ToTable("SuCo", "fo");
            entity.HasKey(x => x.IncidentId).HasName("PK_fo_SuCo");
            entity.Property(x => x.IncidentNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.RoomReference).HasMaxLength(80);
            entity.Property(x => x.GuestReference).HasMaxLength(255);
            entity.Property(x => x.ReservationReference).HasMaxLength(80);
            entity.Property(x => x.Department).HasMaxLength(80);
            entity.Property(x => x.ReporterName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.AssignedTo).HasMaxLength(180);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Impact).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.RootCause).HasMaxLength(2000);
            entity.Property(x => x.TemporaryAction).HasMaxLength(2000);
            entity.Property(x => x.CorrectiveAction).HasMaxLength(2000);
            entity.Property(x => x.PreventiveAction).HasMaxLength(2000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.IncidentNumber }).IsUnique().HasDatabaseName("UQ_fo_SuCo_CoSo_MaSC");
            entity.HasIndex(x => new { x.PropertyId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_SuCo_CoSo_ThoiGian");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.Priority }).HasDatabaseName("IX_fo_SuCo_CoSo_TrangThai_UuTien");
        });
        modelBuilder.Entity<FrontOfficeShiftLogEntry>(entity =>
        {
            entity.ToTable("NhatKyCa", "fo");
            entity.HasKey(x => x.ShiftLogEntryId).HasName("PK_fo_NhatKyCa");
            entity.Property(x => x.ShiftName).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.RoomReference).HasMaxLength(80);
            entity.Property(x => x.GuestReference).HasMaxLength(255);
            entity.Property(x => x.ReservationReference).HasMaxLength(80);
            entity.Property(x => x.AssignedTo).HasMaxLength(180);
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_NhatKyCa_CoSo_ThoiGian");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.Priority }).HasDatabaseName("IX_fo_NhatKyCa_CoSo_TrangThai_UuTien");
            entity.HasIndex(x => new { x.PropertyId, x.Category }).HasDatabaseName("IX_fo_NhatKyCa_CoSo_Nhom");
        });
        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("YeuCauDichVu", "fo");
            entity.HasKey(x => x.ServiceRequestId).HasName("PK_fo_YeuCauDichVu");
            entity.Property(x => x.RequestNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.ServiceType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.AssignedDepartment).HasMaxLength(200);
            entity.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            entity.Property(x => x.ActualCost).HasPrecision(18, 2);
            entity.Property(x => x.ChargeType).HasConversion<string>().HasMaxLength(20).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.RequestNumber }).IsUnique().HasDatabaseName("UQ_fo_YeuCauDichVu_SoYeuCau");
            entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_YeuCauDichVu_CoSo_TrangThai");
            entity.HasIndex(x => new { x.PropertyId, x.SlaDeadlineAtUtc, x.Status }).HasDatabaseName("IX_fo_YeuCauDichVu_SlaDeadline");
            entity.HasMany(x => x.History).WithOne().HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ConciergeWakeUpCall>(entity =>
        {
            entity.ToTable("CuocGoiBaoThuc", "fo");
            entity.HasKey(x => x.CallId).HasName("PK_fo_CuocGoiBaoThuc");
            entity.Property(x => x.CallNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.CallNumber }).IsUnique().HasDatabaseName("UQ_fo_CuocGoiBaoThuc_CoSo_SoGoi");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.WakeTime }).HasDatabaseName("IX_fo_CuocGoiBaoThuc_CoSo_TrangThai_ThoiGian");
        });
        modelBuilder.Entity<ConciergeLuggageItem>(entity =>
        {
            entity.ToTable("HanhLyConcierge", "fo");
            entity.HasKey(x => x.ItemId).HasName("PK_fo_HanhLyConcierge");
            entity.Property(x => x.TagNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.StorageLocation).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.HandlerName).HasMaxLength(200);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.TagNumber }).IsUnique().HasDatabaseName("UQ_fo_HanhLyConcierge_CoSo_MaThe");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.StoredAtUtc }).HasDatabaseName("IX_fo_HanhLyConcierge_CoSo_TrangThai_ThoiGian");
            entity.HasIndex(x => new { x.PropertyId, x.GuestId }).HasDatabaseName("IX_fo_HanhLyConcierge_CoSo_Khach");
        });
        modelBuilder.Entity<ConciergeTransportationBooking>(entity =>
        {
            entity.ToTable("DatXeConcierge", "fo");
            entity.HasKey(x => x.BookingId).HasName("PK_fo_DatXeConcierge");
            entity.Property(x => x.BookingNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.DriverName).HasMaxLength(200);
            entity.Property(x => x.VehicleNumber).HasMaxLength(40);
            entity.Property(x => x.VehicleType).HasMaxLength(80);
            entity.Property(x => x.PickupLocation).HasMaxLength(300);
            entity.Property(x => x.DropoffLocation).HasMaxLength(300);
            entity.Property(x => x.FlightNumber).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Cost).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.BookingNumber }).IsUnique().HasDatabaseName("UQ_fo_DatXeConcierge_CoSo_SoDat");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.ScheduledAtUtc }).HasDatabaseName("IX_fo_DatXeConcierge_CoSo_TrangThai_ThoiGian");
        });
        modelBuilder.Entity<ConciergeTelephoneCall>(entity =>
        {
            entity.ToTable("NhatKyCuocGoi", "fo");
            entity.HasKey(x => x.CallId).HasName("PK_fo_NhatKyCuocGoi");
            entity.Property(x => x.Direction).HasMaxLength(20).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.Subject).HasMaxLength(300);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.PropertyId, x.Direction, x.StartedAtUtc }).HasDatabaseName("IX_fo_NhatKyCuocGoi_CoSo_Huong_ThoiGian");
            entity.HasIndex(x => new { x.PropertyId, x.PhoneNumber }).HasDatabaseName("IX_fo_NhatKyCuocGoi_CoSo_SoDienThoai");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<ConciergeTelephoneContact>(entity =>
        {
            entity.ToTable("DanhBaDienThoai", "fo");
            entity.HasKey(x => x.ContactId).HasName("PK_fo_DanhBaDienThoai");
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => new { x.PropertyId, x.PhoneNumber }).HasDatabaseName("IX_fo_DanhBaDienThoai_CoSo_SoDienThoai");
            entity.HasIndex(x => new { x.PropertyId, x.IsActive, x.DisplayName }).HasDatabaseName("IX_fo_DanhBaDienThoai_CoSo_HoatDong_Ten");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<ReservationDenial>(entity =>
        {
            entity.ToTable("DatPhongTuChoi", "fo");
            entity.HasKey(x => x.DenialId).HasName("PK_fo_DatPhongTuChoi");
            entity.Property(x => x.DenialNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.GuestName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.GuestPhone).HasMaxLength(50);
            entity.Property(x => x.Source).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Reason).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.ArrivalDate).HasColumnType("date");
            entity.Property(x => x.DepartureDate).HasColumnType("date");
            entity.HasIndex(x => new { x.PropertyId, x.DenialNumber }).IsUnique().HasDatabaseName("UQ_fo_DatPhongTuChoi_CoSo_SoTuChoi");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.CreatedAtUtc }).HasDatabaseName("IX_fo_DatPhongTuChoi_CoSo_TrangThai_ThoiGian");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
        });
        modelBuilder.Entity<ServiceHistory>(entity =>
        {
            entity.ToTable("LichSuYeuCau", "fo");
            entity.HasKey(x => x.ServiceHistoryId).HasName("PK_fo_LichSuYeuCau");
            entity.Property(x => x.ServiceHistoryId).ValueGeneratedNever();
            entity.Property(x => x.Action).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(1000);
            entity.HasIndex(x => new { x.ServiceRequestId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_LichSuYeuCau_YeuCau_ThoiGian");
        });
        modelBuilder.Entity<GuestMessage>(entity =>
        {
            entity.ToTable("TinNhanKhach", "fo");
            entity.HasKey(x => x.GuestMessageId).HasName("PK_fo_TinNhanKhach");
            entity.Property(x => x.MessageNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.GuestName).HasMaxLength(200);
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.MessageType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Channel).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(300);
            entity.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Sender).HasMaxLength(200);
            entity.Property(x => x.ResponseContent).HasMaxLength(4000);
            entity.Property(x => x.AssignedStaff).HasMaxLength(200);
            entity.Property(x => x.Department).HasMaxLength(120);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.MessageNumber }).IsUnique().HasDatabaseName("UQ_fo_TinNhanKhach_CoSo_SoTin");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.CreatedAtUtc }).HasDatabaseName("IX_fo_TinNhanKhach_CoSo_TrangThai_ThoiGian");
            entity.HasMany(x => x.History).WithOne().HasForeignKey(x => x.GuestMessageId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MessageHistory>(entity =>
        {
            entity.ToTable("LichSuTinNhanKhach", "fo");
            entity.HasKey(x => x.MessageHistoryId).HasName("PK_fo_LichSuTinNhanKhach");
            entity.Property(x => x.MessageHistoryId).ValueGeneratedNever();
            entity.Property(x => x.Action).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.GuestMessageId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_LichSuTinNhanKhach_TinNhan_ThoiGian");
        });
        modelBuilder.Entity<LostFoundItem>(entity =>
        {
            entity.ToTable("LostFoundItems");
            entity.HasKey(x => x.LostFoundItemId);
            entity.Property(x => x.ItemNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ItemName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasConversion<int>();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.ContactedBy).HasMaxLength(200);
            entity.Property(x => x.ContactMethod).HasMaxLength(50);
            entity.Property(x => x.ContactResult).HasMaxLength(1000);
            entity.Property(x => x.ContactNotes).HasMaxLength(2000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.Ignore(x => x.History);
            entity.HasIndex(x => new { x.PropertyId, x.ItemNumber });
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.FoundDateTime });
        });
        modelBuilder.Entity<LostFoundHistory>(entity =>
        {
            entity.ToTable("LostFoundHistories");
            entity.HasKey(x => x.LostFoundHistoryId);
            entity.Property(x => x.Action).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.LostFoundItemId, x.OccurredAtUtc });
            entity.HasOne<LostFoundItem>()
                .WithMany()
                .HasForeignKey(x => x.LostFoundItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CashierShift>(entity =>
        {
            entity.HasKey(x => x.CashierShiftId).HasName("PK_fo_CaLamViec");
            entity.Property(x => x.ShiftNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.OpeningCash).HasPrecision(18, 2);
            entity.Property(x => x.ClosingCash).HasPrecision(18, 2);
            entity.Property(x => x.ActualCash).HasPrecision(18, 2);
            entity.Property(x => x.BusinessDate).HasColumnType("date");
            entity.HasIndex(x => x.ShiftNumber).IsUnique().HasDatabaseName("UQ_fo_CaLamViec_SoCa");
            entity.HasIndex(x => new { x.PropertyId, x.CashierId, x.Status }).HasDatabaseName("IX_fo_CaLamViec_CoSo_ThuNgan_TrangThai");
            entity.HasMany(x => x.Movements)
                .WithOne()
                .HasForeignKey(x => x.CashierShiftId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(x => x.Movements).UsePropertyAccessMode(PropertyAccessMode.Field);
            if (useSqlServerRowVersion)
            {
                entity.Property(x => x.RowVersion).IsRowVersion();
            }
            else
            {
                // The legacy SQL Server table owns this value as ROWVERSION. The
                // lightweight providers used by unit tests do not have an
                // equivalent generated value; ignoring it keeps their round-trip
                // behavior faithful without weakening production concurrency.
                entity.Ignore(x => x.RowVersion);
            }
            entity.ToTable("CaLamViec", "fo");
        });

        modelBuilder.Entity<CashMovement>(entity =>
        {
            entity.HasKey(x => x.CashMovementId).HasName("PK_fo_BienDongTienMat");
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.CashierShiftId, x.CreatedAtUtc }).HasDatabaseName("IX_fo_BienDongTienMat_Ca_ThoiGian");
            entity.ToTable("BienDongTienMat", "fo");
        });

        modelBuilder.Entity<CashReconciliation>(entity =>
        {
            entity.ToTable("DoiSoatTienMat", "fo");
            entity.HasKey(x => x.CashReconciliationId).HasName("PK_fo_DoiSoatTienMat");
            entity.Property(x => x.ShiftNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ExpectedCash).HasPrecision(18, 2);
            entity.Property(x => x.ActualCash).HasPrecision(18, 2);
            entity.Property(x => x.Variance).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.ReasonCode).HasMaxLength(40);
            entity.Property(x => x.ReasonDetail).HasMaxLength(1000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.CashierShiftId }).IsUnique().HasDatabaseName("UQ_fo_DoiSoatTienMat_CoSo_Ca");
            entity.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("IX_fo_DoiSoatTienMat_CoSo_TrangThai");
            entity.HasOne<CashierShift>().WithMany().HasForeignKey(x => x.CashierShiftId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HousekeepingTask>(entity =>
        {
            entity.ToTable("DonDep", "fo");
            entity.HasKey(x => x.HousekeepingTaskId).HasName("PK_fo_DonDep");
            entity.Property(x => x.TaskNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.TaskType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
              entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(30).IsRequired();
              entity.Property(x => x.InspectorNotes).HasMaxLength(500);
              entity.Property(x => x.PauseReason).HasMaxLength(500);
              entity.Property(x => x.InspectionResult).HasConversion<string>().HasMaxLength(30);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.ScheduledDate });
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TaskAssignment>(entity =>
        {
            entity.ToTable("TaskAssignment", "fo");
            entity.HasKey(x => x.TaskAssignmentId).HasName("PK_fo_TaskAssignment");
            entity.Property(x => x.TaskNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.TaskType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SourceModule).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Department).HasMaxLength(120);
            entity.Property(x => x.AssignedShift).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.AssignedStaffName).HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            if (useSqlServerRowVersion)
            {
                entity.Property(x => x.RowVersion).IsRowVersion();
            }
            else
            {
                entity.Ignore(x => x.RowVersion);
            }
            entity.HasIndex(x => new { x.PropertyId, x.TaskNumber }).IsUnique().HasDatabaseName("UQ_fo_TaskAssignment_CoSo_SoTask");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.DueTime }).HasDatabaseName("IX_fo_TaskAssignment_CoSo_TrangThai_Han");
            entity.HasIndex(x => new { x.PropertyId, x.AssignedStaffId, x.AssignedShift }).HasDatabaseName("IX_fo_TaskAssignment_CoSo_NhanSu_Ca");
            entity.HasMany(x => x.History).WithOne().HasForeignKey(x => x.TaskAssignmentId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.History).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.Entity<TaskHistory>(entity =>
        {
            entity.ToTable("TaskHistory", "fo");
            entity.HasKey(x => x.TaskHistoryId).HasName("PK_fo_TaskHistory");
            entity.Property(x => x.Action).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.PropertyId, x.TaskAssignmentId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_TaskHistory_CoSo_Task_ThoiGian");
        });
        modelBuilder.Entity<RoomHousekeepingState>(entity =>
        {
            entity.ToTable("TrangThaiDonDep", "fo");
            entity.HasKey(x => x.HotelRoomId).HasName("PK_fo_TrangThaiDonDep");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RoomHousekeepingStatusHistory>(entity =>
        {
            entity.ToTable("LichSuTrangThaiDonDep", "fo");
            entity.HasKey(x => x.RoomHousekeepingStatusHistoryId).HasName("PK_fo_LichSuTrangThaiDonDep");
            entity.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => new { x.PropertyId, x.HotelRoomId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_LichSuTrangThaiDonDep_CoSo_Phong_ThoiGian");
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RoomInspection>(entity =>
        {
            entity.ToTable("RoomInspection", "fo");
            entity.HasKey(x => x.InspectionId).HasName("PK_fo_RoomInspection");
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Result).HasMaxLength(30);
            entity.Property(x => x.ChecklistData);
            entity.Property(x => x.Notes).HasMaxLength(500);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.HousekeepingTaskId }).IsUnique();
            entity.HasOne<HousekeepingTask>().WithMany().HasForeignKey(x => x.HousekeepingTaskId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<HousekeepingLinenType>(entity =>
        {
            entity.ToTable("LoaiDoVai", "fo");
            entity.HasKey(x => x.Id).HasName("PK_fo_LoaiDoVai");
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_LoaiDoVai_CoSo_Ma");
            entity.HasIndex(x => new { x.PropertyId, x.IsActive }).HasDatabaseName("IX_fo_LoaiDoVai_CoSo_HoatDong");
        });
        modelBuilder.Entity<HousekeepingLinenTransaction>(entity =>
        {
            entity.ToTable("GiaoDichDoVai", "fo");
            entity.HasKey(x => x.TransactionId).HasName("PK_fo_GiaoDichDoVai");
            entity.Property(x => x.LinenCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasIndex(x => new { x.PropertyId, x.HotelRoomId, x.LinenCode, x.OccurredAtUtc }).HasDatabaseName("IX_fo_GiaoDichDoVai_CoSo_Phong_Ma_ThoiGian");
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HousekeepingAmenityType>(entity =>
        {
            entity.ToTable("LoaiTienNghi", "fo");
            entity.HasKey(x => x.Id).HasName("PK_fo_LoaiTienNghi");
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_LoaiTienNghi_CoSo_Ma");
            entity.HasIndex(x => new { x.PropertyId, x.IsActive }).HasDatabaseName("IX_fo_LoaiTienNghi_CoSo_HoatDong");
        });
        modelBuilder.Entity<HousekeepingAmenityBalance>(entity =>
        {
            entity.ToTable("TonTienNghiPhong", "fo");
            entity.HasKey(x => x.Id).HasName("PK_fo_TonTienNghiPhong");
            entity.Property(x => x.AmenityCode).HasMaxLength(50).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.HotelRoomId, x.AmenityCode }).IsUnique().HasDatabaseName("UQ_fo_TonTienNghiPhong_CoSo_Phong_Ma");
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HousekeepingAmenityRefillRequest>(entity =>
        {
            entity.ToTable("YeuCauCapBuTienNghi", "fo");
            entity.HasKey(x => x.RequestId).HasName("PK_fo_YeuCauCapBuTienNghi");
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.PropertyId, x.HotelRoomId, x.RequestedAtUtc }).HasDatabaseName("IX_fo_YeuCauCapBuTienNghi_CoSo_Phong_ThoiGian");
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HousekeepingAmenityRefillLine>(entity =>
        {
            entity.ToTable("ChiTietYeuCauCapBuTienNghi", "fo");
            entity.HasKey(x => x.LineId).HasName("PK_fo_ChiTietYeuCauCapBuTienNghi");
            entity.Property(x => x.AmenityCode).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => new { x.RequestId, x.AmenityCode }).IsUnique().HasDatabaseName("UQ_fo_ChiTietYeuCauCapBuTienNghi_YeuCau_Ma");
            entity.HasOne<HousekeepingAmenityRefillRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MaintenanceWorkOrder>(entity =>
        {
            entity.ToTable("CongViecBaoTri", "fo");
            entity.HasKey(x => x.WorkOrderId).HasName("PK_fo_CongViecBaoTri");
            entity.Property(x => x.OrderNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.Type).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.AssignedEngineerName).HasMaxLength(180);
            entity.Property(x => x.Resolution).HasMaxLength(4000);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.OrderNumber }).IsUnique().HasDatabaseName("UQ_fo_CongViecBaoTri_CoSo_SoLenh");
            entity.HasIndex(x => new { x.PropertyId, x.Status, x.Priority }).HasDatabaseName("IX_fo_CongViecBaoTri_CoSo_TrangThai_UuTien");
            entity.HasIndex(x => new { x.PropertyId, x.RoomId, x.Status }).HasDatabaseName("IX_fo_CongViecBaoTri_CoSo_Phong_TrangThai");
            entity.HasMany(x => x.Notes).WithOne().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Notes).UsePropertyAccessMode(PropertyAccessMode.Property);
        });
        modelBuilder.Entity<MaintenanceWorkOrderNote>(entity =>
        {
            entity.ToTable("NhatKyCongViecBaoTri", "fo");
            entity.HasKey(x => x.NoteId).HasName("PK_fo_NhatKyCongViecBaoTri");
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(2000);
            entity.HasIndex(x => new { x.WorkOrderId, x.AtUtc }).HasDatabaseName("IX_fo_NhatKyCongViecBaoTri_Lenh_ThoiGian");
        });
        modelBuilder.Entity<MaintenanceAsset>(entity =>
        {
            entity.ToTable("TaiSanBaoTri", "fo");
            entity.HasKey(x => x.AssetId).HasName("PK_fo_TaiSanBaoTri");
            entity.Property(x => x.AssetCode).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(200);
            entity.Property(x => x.SerialNumber).HasMaxLength(120);
            entity.Property(x => x.Model).HasMaxLength(120);
            entity.Property(x => x.Vendor).HasMaxLength(160);
            entity.Property(x => x.ConditionScore).HasPrecision(5, 2);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.AssetCode }).IsUnique().HasDatabaseName("UQ_fo_TaiSanBaoTri_CoSo_MaTaiSan");
            entity.HasIndex(x => new { x.PropertyId, x.RoomId, x.IsActive }).HasDatabaseName("IX_fo_TaiSanBaoTri_CoSo_Phong_HoatDong");
            entity.HasMany(x => x.Failures).WithOne().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Failures).UsePropertyAccessMode(PropertyAccessMode.Property);
        });
        modelBuilder.Entity<MaintenanceAssetFailure>(entity =>
        {
            entity.ToTable("LichSuHongHocBaoTri", "fo");
            entity.HasKey(x => x.FailureId).HasName("PK_fo_LichSuHongHocBaoTri");
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => new { x.AssetId, x.ReportedAtUtc }).HasDatabaseName("IX_fo_LichSuHongHocBaoTri_TaiSan_ThoiGian");
        });
        modelBuilder.Entity<PreventiveMaintenancePlan>(entity =>
        {
            entity.ToTable("KeHoachBaoTriDinhKy", "fo");
            entity.HasKey(x => x.PlanId).HasName("PK_fo_KeHoachBaoTriDinhKy");
            entity.Property(x => x.PlanName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => new { x.PropertyId, x.Category, x.IsActive }).HasDatabaseName("IX_fo_KeHoachBaoTriDinhKy_CoSo_Loai_HoatDong");
            entity.HasMany(x => x.Checklist).WithOne().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Checklist).UsePropertyAccessMode(PropertyAccessMode.Property);
        });
        modelBuilder.Entity<PreventiveChecklistItem>(entity =>
        {
            entity.ToTable("ChiTietKeHoachBaoTri", "fo");
            entity.HasKey(x => x.ChecklistItemId).HasName("PK_fo_ChiTietKeHoachBaoTri");
            entity.Property(x => x.Code).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(1000);
            entity.HasIndex(x => new { x.PlanId, x.Code }).IsUnique().HasDatabaseName("UQ_fo_ChiTietKeHoachBaoTri_KeHoach_Ma");
        });
        modelBuilder.Entity<PreventiveMaintenanceSchedule>(entity =>
        {
            entity.ToTable("LichBaoTriDinhKy", "fo");
            entity.HasKey(x => x.ScheduleId).HasName("PK_fo_LichBaoTriDinhKy");
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.ScheduledAtUtc }).HasDatabaseName("IX_fo_LichBaoTriDinhKy_CoSo_Lich");
            entity.HasIndex(x => new { x.PropertyId, x.AssetId, x.CompletedAtUtc }).HasDatabaseName("IX_fo_LichBaoTriDinhKy_CoSo_TaiSan_HoanTat");
            entity.HasOne<PreventiveMaintenancePlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DoorLockDevice>(entity =>
        {
            entity.ToTable("ThietBiKhoaCua", "fo");
            entity.HasKey(x => x.DeviceId).HasName("PK_fo_ThietBiKhoaCua");
            entity.Property(x => x.DeviceCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.Vendor).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(120).IsRequired();
            entity.Property(x => x.FirmwareVersion).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.DeviceCode }).IsUnique().HasDatabaseName("UQ_fo_ThietBiKhoaCua_CoSo_MaThietBi");
            entity.HasIndex(x => new { x.PropertyId, x.RoomId, x.Status }).HasDatabaseName("IX_fo_ThietBiKhoaCua_CoSo_Phong_TrangThai");
        });
        modelBuilder.Entity<DoorLockEvent>(entity =>
        {
            entity.ToTable("SuKienKhoaCua", "fo");
            entity.HasKey(x => x.EventId).HasName("PK_fo_SuKienKhoaCua");
            entity.Property(x => x.RoomNumber).HasMaxLength(40);
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.KeyCode).HasMaxLength(80);
            entity.Property(x => x.AccessLevel).HasMaxLength(30);
            entity.Property(x => x.Detail).HasMaxLength(1000);
            entity.HasIndex(x => new { x.PropertyId, x.DeviceId, x.OccurredAtUtc }).HasDatabaseName("IX_fo_SuKienKhoaCua_CoSo_ThietBi_ThoiGian");
            entity.HasIndex(x => new { x.PropertyId, x.Granted, x.OccurredAtUtc }).HasDatabaseName("IX_fo_SuKienKhoaCua_CoSo_KetQua_ThoiGian");
        });
        modelBuilder.Entity<MinibarSession>(entity =>
        {
            entity.ToTable("MinibarSession", "fo");
            entity.HasKey(x => x.SessionId).HasName("PK_fo_MinibarSession");
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => new { x.PropertyId, x.StayId }).IsUnique();
            entity.HasOne<HotelStay>().WithMany().HasForeignKey(x => x.StayId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Consumptions).WithOne().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MinibarConsumption>(entity =>
        {
            entity.ToTable("MinibarConsumption", "fo");
            entity.HasKey(x => x.ConsumptionId).HasName("PK_fo_MinibarConsumption");
            entity.Property(x => x.ItemCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ItemName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,3)").HasDefaultValue(1m);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.LineAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.VatRate).HasColumnType("decimal(9,4)");
            entity.Property(x => x.VatAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.GrossAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.PropertyId, x.SessionId });
        });
        modelBuilder.Entity<MinibarRestockTransaction>(entity =>
        {
            entity.ToTable("MinibarRestockTransaction", "fo");
            entity.HasKey(x => x.TransactionId).HasName("PK_fo_MinibarRestockTransaction");
            entity.Property(x => x.ItemCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ItemName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(x => x.Warehouse).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasIndex(x => new { x.PropertyId, x.HotelRoomId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.PropertyId, x.ItemCode });
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<MinibarInventoryAudit>(entity =>
        {
            entity.ToTable("MinibarInventoryAudit", "fo");
            entity.HasKey(x => x.InventoryId).HasName("PK_fo_MinibarInventoryAudit");
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasIndex(x => new { x.PropertyId, x.HotelRoomId, x.CountedAtUtc });
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InventoryId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MinibarInventoryLine>(entity =>
        {
            entity.ToTable("MinibarInventoryLine", "fo");
            entity.HasKey(x => x.InventoryLineId).HasName("PK_fo_MinibarInventoryLine");
            entity.Property(x => x.ItemCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ItemName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.PropertyId, x.InventoryId });
        });
        modelBuilder.Entity<KeyCardCredential>(entity =>
        {
            entity.ToTable("KeyCardCredential", "fo");
            entity.HasKey(x => x.CredentialId).HasName("PK_fo_KeyCardCredential");
            entity.Property(x => x.CardSerial).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.AccessLevel).HasMaxLength(30).IsRequired();
            entity.Property(x => x.EncoderId).IsRequired();
            entity.Property(x => x.IsMaster).IsRequired();
            entity.Property(x => x.DisableReason).HasMaxLength(500);
            ConfigureRowVersion(entity.Property(x => x.RowVersion));
            entity.HasIndex(x => x.CardSerial).IsUnique().HasDatabaseName("UQ_fo_KeyCardCredential_CardSerial");
            entity.HasIndex(x => new { x.PropertyId, x.StayId });
            entity.HasOne<HotelStay>().WithMany().HasForeignKey(x => x.StayId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<GuestProfile>().WithMany().HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<HotelRoom>().WithMany().HasForeignKey(x => x.HotelRoomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Events).WithOne(x => x.Credential).HasForeignKey(x => x.CredentialId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<KeyCardEvent>(entity =>
        {
            entity.ToTable("KeyCardEvent", "fo");
            entity.HasKey(x => x.EventId).HasName("PK_fo_KeyCardEvent");
            entity.Property(x => x.EventType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.DoorId).HasMaxLength(80);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.PropertyId, x.CredentialId, x.OccurredAtUtc });
        });

        modelBuilder.Ignore<Core.FrontOffice.Stay.RoomMoveRecord>();
        modelBuilder.Ignore<Core.FrontOffice.Stay.StayTimelineEntry>();
        modelBuilder.Ignore<Core.FrontOffice.Stay.WakeUpCallRecord>();
        modelBuilder.Ignore<Core.FrontOffice.Stay.ComplaintRecord>();
        // Legacy UI-only record; persisted key-card data is represented by
        // KeyCardCredential/KeyCardEvent below.
        modelBuilder.Ignore<Core.FrontOffice.Stay.KeyCardRecord>();
        modelBuilder.Ignore<Core.FrontOffice.Payment.SplitTenderLine>();
        modelBuilder.Ignore<Core.FrontOffice.Payment.ExchangeRate>();
        modelBuilder.Ignore<Core.FrontOffice.Allocation.RoomCandidate>();

        ConfigureRestaurantModules(modelBuilder, useSqlServerRowVersion);

        // U-006/U-007: link reservations and hotel stays to the canonical guest profile.
        // The live tables already expose nullable GuestId columns; these mappings make
        // the relationship explicit without introducing a parallel guest model.
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(x => x.GuestId);
            entity.HasOne<GuestProfile>()
                .WithMany()
                .HasForeignKey(x => x.GuestId)
                .HasConstraintName("FK_fo_DatPhong_HoSoKhach")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HotelStay>(entity =>
        {
            entity.Property(x => x.GuestId);
            entity.HasOne<GuestProfile>()
                .WithMany()
                .HasForeignKey(x => x.GuestId)
                .HasConstraintName("FK_fo_LuuTruKhachSan_HoSoKhach")
                .OnDelete(DeleteBehavior.Restrict);
        });

        ApplyCompanyQueryFilters(modelBuilder);

        modelBuilder.Entity<Item>().HasIndex(x => x.ItemCode).IsUnique();
        modelBuilder.Entity<Item>().HasIndex(x => x.ItemName).IsUnique();
        modelBuilder.Entity<Item>().Property(x => x.RetailPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Item>().Property(x => x.VatRate).HasPrecision(9, 4);
        modelBuilder.Entity<Item>().Property(x => x.MinibarStandardQuantity).HasPrecision(18, 3);
        modelBuilder.Entity<Item>().Property(x => x.MinibarMinStock).HasPrecision(18, 3);
        modelBuilder.Entity<Item>().Property(x => x.MinibarMaxStock).HasPrecision(18, 3);

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
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
            entity.Property(x => x.Notes).HasMaxLength(1000);
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

        modelBuilder.Entity<LoginAudit>(entity =>
        {
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.Property(x => x.UserName).HasMaxLength(120);
            entity.Property(x => x.FailureReason).HasMaxLength(255);
            entity.Property(x => x.MachineName).HasMaxLength(120);
            entity.Property(x => x.IpAddress).HasMaxLength(100);
        });

        modelBuilder.Entity<LoginSession>(entity =>
        {
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.IsActive, x.ExpiresAt });
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.Ignore(x => x.Token);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
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
            entity.HasIndex(x => x.PropertyId);
            entity.HasIndex(x => x.FolioId);
            entity.HasIndex(x => new { x.PropertyId, x.FolioId, x.DocumentDate });
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
            entity.HasIndex(x => x.FolioId);
            entity.HasIndex(x => x.AdjustsInvoiceId);
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
            entity.Property(x => x.AdjustmentType).HasMaxLength(30);
            entity.Property(x => x.AdjustmentReason).HasMaxLength(2_000);
            entity.HasMany(x => x.Lines).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.TaxBreakdowns).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.IssueLogs).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<JournalHeader>().WithMany().HasForeignKey(x => x.JournalHeaderId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ReceivableTransaction>().WithMany().HasForeignKey(x => x.ReceivableTransactionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<PayableTransaction>().WithMany().HasForeignKey(x => x.PayableTransactionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TaxDeclarationDraft>().WithMany().HasForeignKey(x => x.VatDeclarationDraftId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Invoice>().WithMany().HasForeignKey(x => x.AdjustsInvoiceId).OnDelete(DeleteBehavior.NoAction);
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
            // Keep dependents aligned with the tenant-filtered required parent.
            // Without this matching filter EF can return orphaned detail rows
            // when the parent request is excluded by the current company scope.
            entity.HasQueryFilter(x => x.ChangeRequest != null);
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
            // Audit rows must follow the same visibility boundary as the
            // required quote navigation to avoid filtered-parent surprises.
            entity.HasQueryFilter(x => x.Quote != null);
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
            var companyMatches = Expression.Equal(companyProperty, Expression.Property(Expression.Constant(this), nameof(CurrentCompanyId)));
            // Unassigned rows are legacy/import data. They must not be visible
            // to every tenant through the global filter. Migration and recovery
            // code can opt into them explicitly with IgnoreQueryFilters plus a
            // documented scope, while normal application queries stay isolated.
            Expression filter = companyMatches;

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
