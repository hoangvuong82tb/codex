using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundSchedulerEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "BackgroundJobExecutions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "BackgroundJobExecutions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobDefinitionId",
                table: "BackgroundJobExecutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QueuedAtUtc",
                table: "BackgroundJobExecutions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryAttempt",
                table: "BackgroundJobExecutions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "BackgroundJobExecutions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerSource",
                table: "BackgroundJobExecutions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AppDataPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDataPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRoleInheritances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoleInheritances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRoleInheritances_AppRoles_ParentRoleId",
                        column: x => x.ParentRoleId,
                        principalTable: "AppRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppRoleInheritances_AppRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AppRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AllowConcurrentExecution = table.Column<bool>(type: "bit", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundJobLogs_BackgroundJobExecutions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "BackgroundJobExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyBranchPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBranchPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    FiscalYearCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EndpointUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProtectedSecret = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InvoiceSeries = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BuyerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BuyerTaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BuyerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderInvoiceId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivableTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayableTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VatDeclarationDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    XmlPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdfPreviewPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_PayableTransactions_PayableTransactionId",
                        column: x => x.PayableTransactionId,
                        principalTable: "PayableTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_ReceivableTransactions_ReceivableTransactionId",
                        column: x => x.ReceivableTransactionId,
                        principalTable: "ReceivableTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_TaxDeclarationDrafts_VatDeclarationDraftId",
                        column: x => x.VatDeclarationDraftId,
                        principalTable: "TaxDeclarationDrafts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ScheduleType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    CronExpression = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeriodLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UnlockedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnlockReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodLocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecentSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Query = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SelectedEntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SelectedEntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentSearches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchIndexEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetRoute = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TargetGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SearchText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BaseScore = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchIndexEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskInboxItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetRoute = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReopenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskInboxItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NotificationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetRoute = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    NextRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPaused = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundJobSchedules_BackgroundJobDefinitions_JobDefinitionId",
                        column: x => x.JobDefinitionId,
                        principalTable: "BackgroundJobDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceIssueLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestXml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceIssueLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceIssueLogs_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    JournalAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceTaxBreakdowns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxBaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceTaxBreakdowns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceTaxBreakdowns_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CancellationRequested = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobExecutions_JobDefinitions_JobDefinitionId",
                        column: x => x.JobDefinitionId,
                        principalTable: "JobDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskAttachmentMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskInboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    StorageProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAttachmentMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAttachmentMetadata_TaskInboxItems_TaskInboxId",
                        column: x => x.TaskInboxId,
                        principalTable: "TaskInboxItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskInboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommentText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MentionUserIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskComments_TaskInboxItems_TaskInboxId",
                        column: x => x.TaskInboxId,
                        principalTable: "TaskInboxItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobLogs_JobExecutions_JobExecutionId",
                        column: x => x.JobExecutionId,
                        principalTable: "JobExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobRetries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRetries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobRetries_JobExecutions_JobExecutionId",
                        column: x => x.JobExecutionId,
                        principalTable: "JobExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_BranchId",
                table: "VatTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_CompanyId",
                table: "VatTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_SourceType",
                table: "VatTransactions",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_BranchId",
                table: "StockReceipts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_CompanyId",
                table: "StockReceipts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_CompanyId_BranchId_PostingStatus_ReceiptDate",
                table: "StockReceipts",
                columns: new[] { "CompanyId", "BranchId", "PostingStatus", "ReceiptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_PostingStatus",
                table: "StockReceipts",
                column: "PostingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_BranchId",
                table: "StockIssues",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_CompanyId",
                table: "StockIssues",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_CompanyId_BranchId_PostingStatus_IssueDate",
                table: "StockIssues",
                columns: new[] { "CompanyId", "BranchId", "PostingStatus", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_PostingStatus",
                table: "StockIssues",
                column: "PostingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_BranchId",
                table: "ReportExecutions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_CompanyId",
                table: "ReportExecutions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_CompanyId_BranchId_ReportCode_StartedAt",
                table: "ReportExecutions",
                columns: new[] { "CompanyId", "BranchId", "ReportCode", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_BranchId",
                table: "ReceivableTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_CompanyId",
                table: "ReceivableTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_CompanyId_BranchId_Status_DocumentDate",
                table: "ReceivableTransactions",
                columns: new[] { "CompanyId", "BranchId", "Status", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_SourceType",
                table: "ReceivableTransactions",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_PayableTransactions_BranchId",
                table: "PayableTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayableTransactions_CompanyId",
                table: "PayableTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PayableTransactions_CompanyId_BranchId_Status_DocumentDate",
                table: "PayableTransactions",
                columns: new[] { "CompanyId", "BranchId", "Status", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PayableTransactions_SourceType",
                table: "PayableTransactions",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_BranchId",
                table: "JournalLines",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_CompanyId",
                table: "JournalLines",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_CompanyId_BranchId_AccountCode",
                table: "JournalLines",
                columns: new[] { "CompanyId", "BranchId", "AccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalHeaders_BranchId",
                table: "JournalHeaders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalHeaders_CompanyId",
                table: "JournalHeaders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalHeaders_CompanyId_BranchId_Status_PostingDate",
                table: "JournalHeaders",
                columns: new[] { "CompanyId", "BranchId", "Status", "PostingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalHeaders_JournalDate",
                table: "JournalHeaders",
                column: "JournalDate");

            migrationBuilder.CreateIndex(
                name: "IX_JournalHeaders_SourceType",
                table: "JournalHeaders",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_JournalHeaders_Status",
                table: "JournalHeaders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_BranchId",
                table: "InventoryCostTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_CompanyId",
                table: "InventoryCostTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_CompanyId_BranchId_DocumentDate",
                table: "InventoryCostTransactions",
                columns: new[] { "CompanyId", "BranchId", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_SourceType",
                table: "InventoryCostTransactions",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_BranchId",
                table: "InventoryCostLayers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_CompanyId",
                table: "InventoryCostLayers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_CompanyId_BranchId_Status_DocumentDate",
                table: "InventoryCostLayers",
                columns: new[] { "CompanyId", "BranchId", "Status", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_SourceType",
                table: "InventoryCostLayers",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobExecutions_JobDefinitionId",
                table: "BackgroundJobExecutions",
                column: "JobDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobExecutions_QueuedAtUtc",
                table: "BackgroundJobExecutions",
                column: "QueuedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobExecutions_Status_StartedAtUtc",
                table: "BackgroundJobExecutions",
                columns: new[] { "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_BranchId",
                table: "AuditLogs",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId",
                table: "AuditLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId_BranchId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "CompanyId", "BranchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppDataPermissions_BranchId",
                table: "AppDataPermissions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDataPermissions_CompanyId",
                table: "AppDataPermissions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDataPermissions_ScopeType_ScopeId",
                table: "AppDataPermissions",
                columns: new[] { "ScopeType", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppDataPermissions_SubjectType_SubjectId_ScopeType_ScopeId_CompanyId_BranchId",
                table: "AppDataPermissions",
                columns: new[] { "SubjectType", "SubjectId", "ScopeType", "ScopeId", "CompanyId", "BranchId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppRoleInheritances_ParentRoleId",
                table: "AppRoleInheritances",
                column: "ParentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRoleInheritances_RoleId_ParentRoleId",
                table: "AppRoleInheritances",
                columns: new[] { "RoleId", "ParentRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobDefinitions_Code",
                table: "BackgroundJobDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobLogs_ExecutionId",
                table: "BackgroundJobLogs",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobLogs_TimestampUtc",
                table: "BackgroundJobLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobSchedules_IsPaused_NextRunAtUtc",
                table: "BackgroundJobSchedules",
                columns: new[] { "IsPaused", "NextRunAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobSchedules_JobDefinitionId",
                table: "BackgroundJobSchedules",
                column: "JobDefinitionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranchPermissions_CompanyId_BranchId",
                table: "CompanyBranchPermissions",
                columns: new[] { "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranchPermissions_UserId_CompanyId_BranchId_PermissionCode",
                table: "CompanyBranchPermissions",
                columns: new[] { "UserId", "CompanyId", "BranchId", "PermissionCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_CompanyId_Year",
                table: "FiscalYears",
                columns: new[] { "CompanyId", "Year" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_FiscalYearCode",
                table: "FiscalYears",
                column: "FiscalYearCode");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceIssueLogs_CorrelationId",
                table: "InvoiceIssueLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceIssueLogs_InvoiceId",
                table: "InvoiceIssueLogs",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceIssueLogs_Status",
                table: "InvoiceIssueLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_ItemCode",
                table: "InvoiceLines",
                column: "ItemCode");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceProviderConfigurations_CompanyId_BranchId_ProviderCode",
                table: "InvoiceProviderConfigurations",
                columns: new[] { "CompanyId", "BranchId", "ProviderCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceProviderConfigurations_ProviderCode",
                table: "InvoiceProviderConfigurations",
                column: "ProviderCode");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CompanyId_BranchId_Status_InvoiceDate",
                table: "Invoices",
                columns: new[] { "CompanyId", "BranchId", "Status", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNo",
                table: "Invoices",
                column: "InvoiceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_JournalHeaderId",
                table: "Invoices",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PayableTransactionId",
                table: "Invoices",
                column: "PayableTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ProviderCode",
                table: "Invoices",
                column: "ProviderCode");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ReceivableTransactionId",
                table: "Invoices",
                column: "ReceivableTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_VatDeclarationDraftId",
                table: "Invoices",
                column: "VatDeclarationDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTaxBreakdowns_InvoiceId",
                table: "InvoiceTaxBreakdowns",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDefinitions_IsActive",
                table: "JobDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_JobDefinitions_JobCode",
                table: "JobDefinitions",
                column: "JobCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobDefinitions_NextRunAt",
                table: "JobDefinitions",
                column: "NextRunAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_JobCode",
                table: "JobExecutions",
                column: "JobCode");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_JobDefinitionId",
                table: "JobExecutions",
                column: "JobDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_QueuedAt",
                table: "JobExecutions",
                column: "QueuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_Status",
                table: "JobExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobLogs_CreatedAt",
                table: "JobLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobLogs_JobExecutionId",
                table: "JobLogs",
                column: "JobExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRetries_JobExecutionId",
                table: "JobRetries",
                column: "JobExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRetries_Status",
                table: "JobRetries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodLocks_BranchId",
                table: "PeriodLocks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodLocks_CompanyId_BranchId_PeriodCode",
                table: "PeriodLocks",
                columns: new[] { "CompanyId", "BranchId", "PeriodCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodLocks_CompanyId_PeriodCode_IsLocked",
                table: "PeriodLocks",
                columns: new[] { "CompanyId", "PeriodCode", "IsLocked" });

            migrationBuilder.CreateIndex(
                name: "IX_RecentSearches_UserId_CompanyId_BranchId_SearchedAt",
                table: "RecentSearches",
                columns: new[] { "UserId", "CompanyId", "BranchId", "SearchedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexEntries_CompanyId_BranchId_Category",
                table: "SearchIndexEntries",
                columns: new[] { "CompanyId", "BranchId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexEntries_EntityType_EntityId_CompanyId_BranchId",
                table: "SearchIndexEntries",
                columns: new[] { "EntityType", "EntityId", "CompanyId", "BranchId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexEntries_IsActive",
                table: "SearchIndexEntries",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexEntries_TargetRoute",
                table: "SearchIndexEntries",
                column: "TargetRoute");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAttachmentMetadata_TaskInboxId",
                table: "TaskAttachmentMetadata",
                column: "TaskInboxId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskInboxId",
                table: "TaskComments",
                column: "TaskInboxId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskInboxItems_AssignedToUserId_Status_UpdatedAt",
                table: "TaskInboxItems",
                columns: new[] { "AssignedToUserId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskInboxItems_CompanyId_BranchId_Status",
                table: "TaskInboxItems",
                columns: new[] { "CompanyId", "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskInboxItems_SourceType_SourceId",
                table: "TaskInboxItems",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_CompanyId_BranchId_CreatedAt",
                table: "UserNotifications",
                columns: new[] { "CompanyId", "BranchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_NotificationType",
                table: "UserNotifications",
                column: "NotificationType");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsRead_CreatedAt",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_BackgroundJobExecutions_BackgroundJobDefinitions_JobDefinitionId",
                table: "BackgroundJobExecutions",
                column: "JobDefinitionId",
                principalTable: "BackgroundJobDefinitions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BackgroundJobExecutions_BackgroundJobDefinitions_JobDefinitionId",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropTable(
                name: "AppDataPermissions");

            migrationBuilder.DropTable(
                name: "AppRoleInheritances");

            migrationBuilder.DropTable(
                name: "BackgroundJobLogs");

            migrationBuilder.DropTable(
                name: "BackgroundJobSchedules");

            migrationBuilder.DropTable(
                name: "CompanyBranchPermissions");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "InvoiceIssueLogs");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "InvoiceProviderConfigurations");

            migrationBuilder.DropTable(
                name: "InvoiceTaxBreakdowns");

            migrationBuilder.DropTable(
                name: "JobLogs");

            migrationBuilder.DropTable(
                name: "JobRetries");

            migrationBuilder.DropTable(
                name: "PeriodLocks");

            migrationBuilder.DropTable(
                name: "RecentSearches");

            migrationBuilder.DropTable(
                name: "SearchIndexEntries");

            migrationBuilder.DropTable(
                name: "TaskAttachmentMetadata");

            migrationBuilder.DropTable(
                name: "TaskComments");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "BackgroundJobDefinitions");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "JobExecutions");

            migrationBuilder.DropTable(
                name: "TaskInboxItems");

            migrationBuilder.DropTable(
                name: "JobDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_VatTransactions_BranchId",
                table: "VatTransactions");

            migrationBuilder.DropIndex(
                name: "IX_VatTransactions_CompanyId",
                table: "VatTransactions");

            migrationBuilder.DropIndex(
                name: "IX_VatTransactions_SourceType",
                table: "VatTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockReceipts_BranchId",
                table: "StockReceipts");

            migrationBuilder.DropIndex(
                name: "IX_StockReceipts_CompanyId",
                table: "StockReceipts");

            migrationBuilder.DropIndex(
                name: "IX_StockReceipts_CompanyId_BranchId_PostingStatus_ReceiptDate",
                table: "StockReceipts");

            migrationBuilder.DropIndex(
                name: "IX_StockReceipts_PostingStatus",
                table: "StockReceipts");

            migrationBuilder.DropIndex(
                name: "IX_StockIssues_BranchId",
                table: "StockIssues");

            migrationBuilder.DropIndex(
                name: "IX_StockIssues_CompanyId",
                table: "StockIssues");

            migrationBuilder.DropIndex(
                name: "IX_StockIssues_CompanyId_BranchId_PostingStatus_IssueDate",
                table: "StockIssues");

            migrationBuilder.DropIndex(
                name: "IX_StockIssues_PostingStatus",
                table: "StockIssues");

            migrationBuilder.DropIndex(
                name: "IX_ReportExecutions_BranchId",
                table: "ReportExecutions");

            migrationBuilder.DropIndex(
                name: "IX_ReportExecutions_CompanyId",
                table: "ReportExecutions");

            migrationBuilder.DropIndex(
                name: "IX_ReportExecutions_CompanyId_BranchId_ReportCode_StartedAt",
                table: "ReportExecutions");

            migrationBuilder.DropIndex(
                name: "IX_ReceivableTransactions_BranchId",
                table: "ReceivableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ReceivableTransactions_CompanyId",
                table: "ReceivableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ReceivableTransactions_CompanyId_BranchId_Status_DocumentDate",
                table: "ReceivableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ReceivableTransactions_SourceType",
                table: "ReceivableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PayableTransactions_BranchId",
                table: "PayableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PayableTransactions_CompanyId",
                table: "PayableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PayableTransactions_CompanyId_BranchId_Status_DocumentDate",
                table: "PayableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PayableTransactions_SourceType",
                table: "PayableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_JournalLines_BranchId",
                table: "JournalLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalLines_CompanyId",
                table: "JournalLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalLines_CompanyId_BranchId_AccountCode",
                table: "JournalLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalHeaders_BranchId",
                table: "JournalHeaders");

            migrationBuilder.DropIndex(
                name: "IX_JournalHeaders_CompanyId",
                table: "JournalHeaders");

            migrationBuilder.DropIndex(
                name: "IX_JournalHeaders_CompanyId_BranchId_Status_PostingDate",
                table: "JournalHeaders");

            migrationBuilder.DropIndex(
                name: "IX_JournalHeaders_JournalDate",
                table: "JournalHeaders");

            migrationBuilder.DropIndex(
                name: "IX_JournalHeaders_SourceType",
                table: "JournalHeaders");

            migrationBuilder.DropIndex(
                name: "IX_JournalHeaders_Status",
                table: "JournalHeaders");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCostTransactions_BranchId",
                table: "InventoryCostTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCostTransactions_CompanyId",
                table: "InventoryCostTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCostTransactions_CompanyId_BranchId_DocumentDate",
                table: "InventoryCostTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCostTransactions_SourceType",
                table: "InventoryCostTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCostLayers_BranchId",
                table: "InventoryCostLayers");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCostLayers_CompanyId",
                table: "InventoryCostLayers");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCostLayers_CompanyId_BranchId_Status_DocumentDate",
                table: "InventoryCostLayers");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCostLayers_SourceType",
                table: "InventoryCostLayers");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobExecutions_JobDefinitionId",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobExecutions_QueuedAtUtc",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobExecutions_Status_StartedAtUtc",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_BranchId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CompanyId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CompanyId_BranchId_CreatedAt",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropColumn(
                name: "JobDefinitionId",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropColumn(
                name: "QueuedAtUtc",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropColumn(
                name: "RetryAttempt",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "BackgroundJobExecutions");

            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "BackgroundJobExecutions");
        }
    }
}
