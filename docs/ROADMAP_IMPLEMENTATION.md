# ROADMAP_IMPLEMENTATION

## 2026-05-28 - Stabilization status update

- Phase 0 completed: DB backup created (`Backups\ASMILE_BO_20260528_093130.bak`), baseline report snapshotted.
- Phase 1 completed: build blocker fixed at `OperationsCenterView.xaml` (`MemoryUsageBytes` header mojibake).
- Phase 2 completed: TT99/TT200 missing-mapping tests fixed by preventing auto-heal inside `ValidateComplianceAsync`.
- Phase 3 completed: destructive reset hardened and executed successfully.
  - Added reset cleanup order for `PostingSources` before `JournalHeaders`.
  - Added reset cleanup for `BusinessDocuments`.
  - Verified business data tables are blank; system tables retained.
- Phase 4 completed: user-facing mojibake cleanup finished for scoped services/views (manual contextual fixes, no feature/architecture changes).
- Phase 7 executed (manual-assisted real UI smoke): **FAILED** runtime gate due to repeated `Unhandled UI exception` / `XamlParseException` (`FormatException: Token is not valid`) in session logs.
- Final gates currently pass:
  - `dotnet build .\ASMILE.BO.sln --no-incremental` => pass, 0 warnings.
  - `dotnet test .\ASMILE.BO.sln` => pass after flaky-test rerun (120 unit + 11 integration).
- Remaining stabilization work: fix root cause of runtime `XamlParseException` (invalid brush token), rerun Phase 7 full module smoke, then close Phase 6/8 final verification.

## Tr?ng th�i tri?n khai

| Step | N?i dung | Tr?ng th�i | Ghi ch� |
|---|---|---|---|
| 1 | Chu?n h�a encoding v� build s?ch | Completed | Build pass; ti?p t?c x? l� n?u ph�t hi?n mojibake runtime. |
| 2 | Accounting Core Entities | Completed | ?? t?o entity, enum, DbSet, Fluent API v? migration `AddAccountingCoreEntities`. |
| 3 | Posting Engine | Completed | ?? t?o interface/service/DTO, dang k? DI, build pass. |
| 4 | T?ch h?p phi?u nh?p/xu?t v?i Posting Engine | Completed | ?? t?ch h?p StockReceipt/StockIssue v?i PostingEngine v? migration `AddStockVoucherPosting`. |
| 5 | Accounting Period Engine v? kh?a k? th?t | Completed | ?? t?o service qu?n l? k?, enforce kh?a k? trong JournalService, PostingEngine v? InventoryAppService. |
| 6 | N?ng c?p S? c?i d?ng Journal persisted | Completed | S? c?i d?c Journal persisted l? ngu?n ch?nh; legacy fallback gi? l?i khi chua c? journal. |
| 7 | Chu?n h?a h? th?ng t?i kho?n TT99-ready | Completed | ?? th?m metadata LedgerAccount, enforce AllowPosting/required dimensions, d?i mapping m?u sang t?i kho?n chi ti?t. |
| 8 | AR/AP persisted | Completed | ?? t?o b?ng Receivable/Payable persisted, service d?ng b? t? Journal v? fallback legacy. |
| 9 | Import Excel Engine d?ng chung | Completed | ?? t?o Import Engine d?ng chung, migration `AddImportEngine`, export l?i Excel v? handler th? cho h?ng h?a. |
| 10 | Costing Engine cho kho | Completed | ?? t?o cost layer/transaction, MovingAverage, t?ch h?p duy?t phi?u nh?p/xu?t v? PostingEngine d?ng gi? v?n persisted. |
| 11 | Report Engine | Completed | ?? t?o Report Engine, template/report execution persisted, b?o c?o n?n t?ng v? export Excel/CSV/PDF t?i thi?u. |
| 12 | TT99/TT200 Accounting Standard Engine | Completed | ?? t?o Accounting Standard Engine, template t?i kho?n, role mapping, financial mapping v? profile m?c d?nh TT99. |
| 13 | Module Qu? | Completed | ?? t?o CashFund/CashTransaction/CashCount persisted, service s? qu?, journal integration v? migration `AddCashFundModule`. |
| 14 | Module Ng?n h?ng | Completed | ?? t?o BankAccount/BankTransaction/BankStatement/BankReconciliation persisted, service s? ng?n h?ng, journal integration v? migration `AddBankingModule`. |
| 15 | Module TSC? | Completed | ?? t?o FixedAsset/Category/Depreciation/Adjustment/Disposal persisted, service kh?u hao du?ng th?ng, journal integration v? migration `AddFixedAssetModule`. |
| 16 | Module CCDC | Completed | ?? t?o ToolAsset/Issue/Allocation persisted, ph?n b? du?ng th?ng, journal integration v? migration `AddToolAssetModule`. |
| 17 | VAT/Thu? co b?n | Completed | ?? t?o TaxCode/VatTransaction/TaxDeclarationDraft, TaxService, sync VAT t? Journal v? b?o c?o VAT_INPUT/VAT_OUTPUT. |
| 18 | Financial Statements + Closing Engine | Completed | ?? t?o snapshot s? du, b?o c?o t?i ch?nh n?n t?ng, closing engine v? migration `AddFinancialStatementEngine`. |
| 19 | Test t? d?ng + Audit + Hardening | Completed | ?? t?o Unit/Integration test project, audit trail persisted, RowVersion concurrency, validation hardening; build pass v? test pass 20/20. |
| 20 | Production readiness + architecture cleanup | Completed | ?? x? l? warning CA1416, th?m options/health/migration/seed validation/background job foundation, t?i li?u production; build 0 warning v? test pass 20/20. |

## Backend core roadmap summary
- Accounting Core: Completed
- Posting Engine + Journal persisted: Completed
- Inventory + Costing: Completed
- AR/AP persisted: Completed
- Cash/Bank: Completed
- Fixed Assets + Tool Assets: Completed
- VAT: Completed
- Financial Statements + Closing: Completed
- Audit + Automated Tests: Completed
- Production readiness foundation: Completed

Backend core roadmap status: Completed 20/20.

## Nguy�n t?c sau roadmap backend core
- Kh�ng t?o migration repair m?i; d� s?a migration g?c chua ghi history d? gi? d? li?u dev hi?n c�.
- Kh�ng thay d?i accounting/posting/report behavior hi?n t?i; ch? harden persistence/startup/Operations Center.
- Ch?y build/test sau m?i nh�m thay d?i.
- Uu ti?n phase ti?p theo: UI wiring, ph?n quy?n, multi-company, e-invoice, SQL Server performance tuning v? CI/CD.

## UI wiring sau backend core

| H?ng m?c | Tr?ng th�i | Ghi ch� |
|---|---|---|
| Report Center persisted wiring | Completed | ?? wire `stock-report` v? `debt-report` v?o Report Center d?ng `IReportEngine`, h? tr? ch?y/xu?t/l?ch s? cho 11 m? b?o c?o n?n t?ng. |
| General Ledger enterprise workflow | Completed | PHASE 2 STEP 2: d? wire persisted journal s?u hon, b? sung workflow ghi s?/d?o, debounce filter, virtualization v? keyboard workflow t?i thi?u. |
| Inventory enterprise workflow | Completed | PHASE 2 STEP 2.3: d? wire ReportEngine/ImportEngine, b? sung b?o c?o kho persisted, cost visibility, debounce filter, virtualization v? keyboard workflow t?i thi?u. |
| Cash/Bank enterprise workflow | Completed | PHASE 2 STEP 2.4: th?m module Qu? & Ng?n h?ng d?ng CashFund/Banking persisted, workflow approve/post/reverse, reconciliation, statement import qua ImportEngine, Cash/Bank reports qua ReportEngine, virtualization v? keyboard copy/paste. |
| Fixed Asset + Tool Asset enterprise workflow | Completed | PHASE 2 STEP 2.5: th�m module `asset-management` d�ng FixedAsset/ToolAsset persisted, workflow approve/post/reverse/dispose/adjust, depreciation/allocation batch, Asset reports qua ReportEngine, virtualization v� keyboard copy/paste. |
| Tax + Financial Statement enterprise workflow | Completed | PHASE 2 STEP 2.6: th�m module `tax-financial` d�ng Tax/FinancialStatement/Closing persisted, VAT declaration workflow, FS report center, closing preview/batch/reverse, period lock/unlock, drilldown metadata v� async/debounce/cancellation. |
| Navigation + Permission + Enterprise Shell | Completed | PHASE 2 STEP 2.7: th�m NavigationService router t?p trung, permission/workspace persisted, shell rail nh?, breadcrumb/status, global search, recent/pinned modules, drilldown route v� migration `AddEnterpriseShellAndPermissions`. |
| Enterprise Permission Management + Operational Workspace | Completed | PHASE 2 STEP 2.8: th�m `security-admin`, `workspace-settings`, `audit-center`, `operations-center`, permission catalog/user-role/job execution persisted, audit/health/job monitor v� migration `AddEnterpriseOperationsAndSecurityUi`. |
| Enterprise Stabilization + Performance Hardening | Completed | PHASE 2 STEP 2.9: chu?n h�a grid virtualization, th�m BusyScope/debounce/safe fire-and-forget/exception mapper, harden shell cleanup, report/export cancellation, import guards/temp cleanup; no migration. |

## 2026-05-25 - Training-data database reset execution (blank state)

Status: Completed.

- Muc tieu: xoa trang du lieu nghiep vu cu trong DB hien tai de onboarding khach moi nhap tu dau.
- Thuc hien bang module co san `training-data` (khong viet lai logic reset, khong SQL delete thu cong cho business workflow).
- Operation da chay: `ResetToBlank`, `IsDryRun=false`, confirm `RESET ALL BUSINESS DATA`.
- Migration/schema readiness:
  - Da bo sung cac migration training-data con thieu trong DB:
    - `20260525113000_AddTrainingDataRunStepDetails`
    - `20260525133000_AddTrainingDataMarkers`
  - Khong tao migration repair moi.
- Ket qua sau reset:
  - Du lieu nghiep vu da duoc xoa sach (journal, inventory, costing, AR/AP, cash/bank, VAT, customer/supplier/item/warehouse, markers).
  - Du lieu system can thiet van duoc giu lai (users/admin, roles, permissions, accounting standards/templates, migration history).
- Build/Test:
  - `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings.
  - `dotnet test .\ASMILE.BO.sln`: pass, 127 tests (119 unit + 8 integration).
- Luu y van hanh:
  - Khong nap du lieu mau sau reset theo yeu cau hien tai.
  - Nen tat lai cac co nguy hiem (`AllowDestructiveReset`, `AllowResetWithRealDataRisk`, `AllowInProduction`) trong cau hinh dung chung sau khi hoan tat setup.

### Nguy�n t?c UI wiring ti?p theo
- Kh�ng thay d?i accounting/posting/report behavior hi?n t?i; ch? harden persistence/startup/Operations Center.
- Uu ti�n wire backend persisted cho t?ng m�n.
- Gi? bridge/fallback cu cho t?i khi workflow m?i d? ?n d?nh.
- Build sau m?i nh�m thay d?i.


| Inventory Receipt/Issue enterprise UX | Completed | PHASE 2 STEP 2.3 completed: b? sung posting/reverse bridge, batch post phi?u xu?t, validation inline, cost visibility, ReportEngine inventory reports, ImportEngine UI, DataGrid virtualization v� keyboard copy/paste Excel rows. |
| Cash/Bank Enterprise UX | Completed | PHASE 2 STEP 2.4 completed: gi? phi?u thu/chi legacy, th�m `cash-bank` route enterprise m?i, statement CSV/XLSX preview/validate/commit, reconciliation drilldown, report export Excel/PDF/CSV v� async/debounce/cancellation. |
| Fixed Asset + Tool Asset Enterprise UX | Completed | PHASE 2 STEP 2.5 completed: gi? backend accounting hi?n c?, th?m `TSC? & CCDC` route enterprise m?i, depreciation/allocation preview/batch/reverse, asset report export Excel/PDF/CSV, posting visibility, drilldown metadata v? async/debounce/cancellation. |
| Tax + Financial Statement Enterprise UX | Completed | PHASE 2 STEP 2.6 completed: gi? Report Center/GL cu, th�m `Thu? & BCTC` route enterprise m?i, VAT declaration generate/lock/re-open/export, financial statement reports, closing workflow, period lock/unlock, posting/closing visibility v� virtualized async grids. |
| Navigation + Permission + Enterprise Shell | Completed | PHASE 2 STEP 2.7 completed: gi? workspace/taskbar cu, chuy?n module routing ra `NavigationService`, th�m persisted permission/workspace, global search debounce, breadcrumb/status guard, recent/pinned modules v� drilldown fallback. |
| Enterprise Permission Management + Operational Workspace | Completed | PHASE 2 STEP 2.8 completed: th�m permission admin UI, role/user assignment foundation, workspace customization UI, audit viewer, background job monitor, infrastructure health dashboard v� operational tools. |
| Enterprise Stabilization + Performance Hardening | Completed | PHASE 2 STEP 2.9 completed: kh�ng th�m module m?i, harden runtime async/resource cleanup, grid virtualization defaults, report/import cancellation safety, shell duplicate-command guard v� validation build/test s?ch. |

## Phase 3 foundation

| Step | N?i dung | Tr?ng th�i | Ghi ch� |
|---|---|---|---|
| PHASE 3 STEP 3.1 | Multi-company + optional multi-branch foundation | Completed | Added company/branch/access entities, nullable scope columns, global company filters with legacy fallback, optional branch filtering, permission/workspace isolation, lightweight shell selectors, migration `AddMultiCompanyFoundation`, and isolation tests. |
| PHASE 3 STEP 3.2 | SQL Server Performance & Data Scaling | Completed | Added hot-path indexes, compiled/paged journal query helper, diagnostics timings, dev scale seed helper, CSV streaming export, and report projection improvements. |
| PHASE 3 STEP 3.3 | Enterprise Permission System Complete | Completed | Added permission tree/matrix APIs, role inheritance, deny override preview, persisted data permissions, audit hooks, cache invalidation, and focused tests. |
| PHASE 3 STEP 3.4 | E-Invoice Integration Foundation | Completed | Added persisted e-invoice entities, Viettel/VNPT/MISA provider abstraction, XML/PDF preview foundations, async issue queue, VAT sync, and tests. |
| PHASE 3 STEP 3.5 | Background Job & Scheduler Engine | Completed | Added persisted scheduler entities, local scheduler service, manual trigger/retry/cancel, default accounting jobs, and tests. |
| PHASE 3 STEP 3.6 | Multi-Company Foundation + Context Isolation | Completed | Formalized multi-company foundation for the current Phase 3 numbering, reused additive migration `AddMultiCompanyFoundation`, verified company-aware service/report/workspace/permission isolation, and added audit isolation coverage. |
| PHASE 3 STEP 3.7 | Multi-Branch + Fiscal Period Engine | Completed | Added fiscal year, period lock, and branch permission entities; branch-aware period lock validation, fiscal-year rollover creation, fiscal period management UI route, branch-scoped report tests, and migration `AddFiscalPeriodAndBranchEngine`. |
| PHASE 3 STEP 3.8 | Enterprise Notification + Task Center | Completed | Added persisted notifications, task inbox, comments, attachment metadata, notification/task service, notification center UI route, unread counters, task assignment/complete/reopen/comment flow, and migration `AddEnterpriseNotificationCenter`. |
| PHASE 3 STEP 3.9 | Full Text Search + Global Command Palette | Completed | Added persisted search index and recent search entities, enterprise search service with scoring/ranking, company/branch isolation, cancellation-aware APIs, migration `AddEnterpriseSearchIndex`, and focused tests. |

### Phase 3 Step 3.1 validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 25/25 tests.

### Phase 3 Step 3.2 validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 27/27 tests.

### Phase 3 Step 3.3 validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 30/30 tests.

### Phase 3 Step 3.4 validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 32/32 tests.

### Phase 3 Step 3.5 validation
- Interim `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- Interim `dotnet test .\ASMILE.BO.sln --no-build`: pass, 34/34 tests.

### Phase 3 Step 3.6 validation
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj --filter MultiCompanyFoundationTests --no-restore`: pass, 6/6 tests.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 35/35 tests.

### Phase 3 Step 3.7 validation
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj --filter FiscalPeriodBranchEngineTests --no-build`: pass, 3/3 tests.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 38/38 tests.

### Phase 3 Step 3.8 validation
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj --filter NotificationTaskServiceTests --no-build`: pass, 3/3 tests.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 41/41 tests.

### Phase 3 Step 3.9 validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj --filter EnterpriseSearchIndexTests --no-build`: pass, 4/4 tests.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 45/45 tests.

| PHASE 3 STEP 3.10 | Background Processing + Scheduler Engine | Completed | Added persisted background scheduler foundation, hosted scheduler loop, safe queue/retry/cancel/pause/resume flow, Operations Center wiring, migration `AddBackgroundSchedulerEngine`, and focused tests. |

### Phase 3 Step 3.10 validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 47/47 tests.

| PHASE 3 STEP 3.11 | Advanced Audit + Activity Replay | Completed | Added persisted enterprise audit/session/correlation/snapshot/replay/export models, correlation-aware audit writing, replay/export service, Audit Center expansion, migration `EnhanceEnterpriseAuditTrail`, and focused tests. |

| Phase 3 Step 3.12 | Enterprise Audit Completion + Query Filter Hardening | Completed | Added audit session lifecycle, query-filter hardening, snapshot compression, replay expansion, export integrity metadata, parent correlation chain, migration CompleteEnterpriseAuditLifecycle, and 65-test validation baseline. |

### Enterprise Audit Lifecycle
- Audit sessions now track status, heartbeat activity, completion, abandonment, expiration, duration, client machine, device, platform, and completion reason.
- Session lifecycle is implemented through IAuditSessionService/AuditSessionService without changing existing AuditService compatibility.

### Correlation Architecture
- Correlation propagation now records current and parent correlation IDs for nested async flows.
- Replay entries and correlation rows can be grouped by session, parent-child correlation, entity, journal, and source document.

### Replay Architecture
- Replay supports timeline queries, session replay, journal replay, source replay, and metadata-only state reconstruction.
- Replay paths do not re-execute accounting operations or mutate production data.

### Snapshot Compression
- Snapshot payloads above the configured threshold are gzip-compressed with metadata fields for compression type, snapshot size, hash algorithm, and SHA256 verification.
- Replay consumers automatically decompress payloads and expose hash mismatch detection.

### Audit Export Pipeline
- Export supports JSON, CSV, NDJSON, and ZIP archive with streaming writes, cancellation awareness, file hash, row count, correlation coverage, and integrity manifest.

| Phase 3 Step 3.13 | Enterprise Data Retention + Archival + Recovery Hardening | Completed | Added persisted retention/archive/recovery entities, ArchiveService, RecoveryService, archive-aware search filtering, scheduler jobs, Operations Center retention tab, migration AddEnterpriseRetentionAndArchival, and retention test coverage. |

### Enterprise Retention Foundation
- Added DataRetentionPolicy, ArchiveBatch, ArchiveBatchItem, RecoveryCheckpoint, RecoveryOperation, and ArchiveExportManifest persistence.
- Retention scopes cover audit, reports, imports, notifications, scheduler history, workspace history, search index history, and session history through persisted policies seeded by ArchiveService.

### Archive Lifecycle
- ArchiveService supports preview, archive batch creation, soft restore, expired purge marking, archive history, gzip compression, and hash integrity validation.
- Archive batches remain replay-safe metadata records; hard deletion is deferred by design.

### Recovery Foundation
- RecoveryService captures database metadata, migration version, scheduler state, workspace metadata, and audit/session continuity metadata.
- Restore records recovery operations without destructive database restore orchestration.

### Operations Center Wiring
- Operations Center now exposes retention preview, archive history, integrity state, and recovery timeline without shell redesign.

### Scheduler Integration
- Added ARCHIVE_CLEANUP, RETENTION_ENFORCEMENT, and INTEGRITY_VERIFICATION scheduler jobs with persisted execution history, retry, timeout, stale recovery, and cancellation paths from the existing scheduler engine.

### Search/Audit Integration
- Enterprise search excludes archived records by default and supports optional archived inclusion.
- Archive and recovery services propagate correlation metadata and can audit lifecycle actions when audit services are available.

## Phase 3 Step 3.14 - Enterprise Diagnostics + Runtime Telemetry + Failure Recovery Hardening

Completed on 2026-05-23.

Completed items:
- Added persisted runtime telemetry entities, buffered telemetry service, failure recovery service, telemetry cleanup scheduler job, and Operations Center telemetry tab.
- Added telemetry hooks for scheduler execution plus report/import/replay/archive/recovery flows.
- Added tests for telemetry batching, duplicate exception suppression, slow operation tracking, cancellation handling, runtime summary, recovery suggestion generation, UI freeze detection, company isolation, and telemetry cleanup.
- Added migration `AddEnterpriseTelemetryAndDiagnostics`.

Telemetry Foundation:
- RuntimeMetricSnapshot, TelemetryEvent, SlowOperationLog, RuntimeExceptionLog, UiResponsivenessSnapshot, RecoveryRecommendation, and SystemResourceSnapshot are now persisted and company/branch scoped.
- RuntimeTelemetryService buffers writes through a bounded channel, flushes in batches, suppresses duplicate exception spam, and exposes summary/timeline/query APIs.

Failure Recovery:
- FailureRecoveryService analyzes recurring exceptions, UI freezes, and slow operations to generate non-destructive recovery recommendations.
- Recovery suggestions are persisted and audit-aware; no destructive recovery is automated.

Operations Center:
- Added telemetry summary cards and virtualized grids for timeline, slow operations, runtime exceptions, system resources, UI responsiveness, and recovery recommendations.

Scheduler/Workflow Integration:
- Scheduler execution now emits telemetry for exceptions, slow operations, and cleanup activity.
- Report/import/replay/archive/recovery flows now expose telemetry hooks on their main execution paths.

Migration:
- AddEnterpriseTelemetryAndDiagnostics

Validation:
- `dotnet build .\ASMILE.BO.sln --no-incremental`: passed, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pending final verification.
- `dotnet test .\ASMILE.BO.sln --no-build`: pending final verification.

### Final Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: passed, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: passed, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: passed, 82 total tests.

## Step 3.15 - Enterprise Workflow Resilience + Transaction Recovery
### Da hoan thanh
- Them workflow resilience persisted foundation va migration `AddWorkflowResilienceAndRecovery`.
- Trien khai `WorkflowResilienceService` voi idempotency, retry, compensation, stale recovery, consistency validation.
- Tich hop Background Scheduler de theo doi workflow execution theo correlation.
- Mo rong Operations Center voi Workflow timeline/steps/consistency violations.
- Tich hop Search de tim workflow execution va violation.
- Them test unit cho idempotency, retry, resume, compensation, stale recovery, consistency duplicate.

### Ranh gioi scope
- Kh�ng thay d?i accounting/posting/report behavior hi?n t?i; ch? harden persistence/startup/Operations Center.
- Kh�ng thay d?i accounting/posting/report behavior hi?n t?i; ch? harden persistence/startup/Operations Center.
- Kh�ng thay d?i accounting/posting/report behavior hi?n t?i; ch? harden persistence/startup/Operations Center.

| 16 | Enterprise Reliability + Distributed Coordination + Runtime Hardening | Completed | �� th�m lease/lock/dead-letter/checkpoint/replay, harden telemetry v� search, m? r?ng Operations Center, migration `AddEnterpriseReliabilityAndDistributedCoordination`. |

## PHASE 4 - K? ho?ch tri?n khai

| Step | N?i dung | Tr?ng th�i | Ghi ch� |
|---|---|---|---|
| 4.1 | Multi-company / Multi-branch Hardening | Completed | C� l?p company/branch, kho/qu?/ng�n h�ng, b�o c�o chi nh�nh, safety xuy�n company. |
| 4.2 | TT99 Compliance Foundation | Completed | �� th�m entity compliance TT99, service mapping/validation/snapshot/export manifest, report code TT99, tab Operations Center v� migration `AddTt99ComplianceFoundation`. |
| 4.3 | TT200 Compliance Foundation | Completed | �� th�m entity/compliance/service TT200, report code TT200, tab Operations Center v� migration `AddTt200ComplianceFoundation`. |
| 4.4 | Approval Workflow Engine | Completed | �� th�m approval workflow persisted, service `IApprovalWorkflowService`, tab Approval trong Operations Center, migration `AddEnterpriseApprovalWorkflowFoundation` v� harden FK NoAction. |
| 4.5 | Advanced Financial Statement Mapping | Completed | �� th�m foundation template/version/formula/snapshot, service `IFinancialStatementMappingService`, migration `AddAdvancedFinancialStatementMappingFoundation` v� harden FK NoAction. |
| 4.6 | Enterprise Reconciliation Engine | Completed | �� th�m foundation d?i so�t (session/match/rule/snapshot/anomaly), service `IReconciliationEngine`, migration Phase 4 foundation. |
| 4.7 | E-Invoice Integration Foundation | Completed | �� ho�n thi?n foundation provider/state/retry tracking tr�n n?n e-invoice, migration Phase 4 foundation. |
| 4.8 | SQL Server Performance Hardening | Completed | �� harden query/index foundation v� chu?n h�a pipeline theo SQL Server-first trong ph?m vi additive. |
| 4.9 | Advanced Reporting & Export Pipeline | Completed | �� th�m export pipeline foundation (execution/retention/artifact/report snapshot), service `IExportPipelineService`. |
| 4.10 | Enterprise Data Integrity & Recovery | Completed | �� th�m data integrity checkpoint/scan/repair foundation, service `IDataIntegrityService`. |
| 4.11 | Compliance Packaging & Legal Output | Completed | �� th�m compliance package/manifest/submission/archive foundation, service `ICompliancePackagingService`. |
| 4.12 | Phase 4 Stabilization & Production Certification | Completed | �� th�m production certification report foundation, ch?y full build/test v� ki?m tra app startup runtime. |

Xem chi ti?t: `PHASE_4_IMPLEMENTATION_PLAN.md`

## Phase 4 planning update (2026-05-24)

| Step | N?i dung | Tr?ng th�i | Ghi ch� |
|---|---|---|---|
| 4.4 | Enterprise Approval Workflow Foundation | Completed | �� implement foundation additive, harden FK NoAction v� startup pass sau auto-migrate. |
| 4.5 | Advanced Financial Statement Mapping | Completed | �� th�m foundation template/version/formula/snapshot, service `IFinancialStatementMappingService`, migration `AddAdvancedFinancialStatementMappingFoundation` v� harden FK NoAction. |
| 4.6 | Enterprise Reconciliation Engine | Completed | �� th�m foundation d?i so�t (session/match/rule/snapshot/anomaly), service `IReconciliationEngine`, migration Phase 4 foundation. |
| 4.7 | E-Invoice Integration Foundation | Completed | �� ho�n thi?n foundation provider/state/retry tracking tr�n n?n e-invoice, migration Phase 4 foundation. |
| 4.8 | SQL Server Performance Hardening | Completed | �� harden query/index foundation v� chu?n h�a pipeline theo SQL Server-first trong ph?m vi additive. |
| 4.9 | Advanced Reporting & Export Pipeline | Completed | �� th�m export pipeline foundation (execution/retention/artifact/report snapshot), service `IExportPipelineService`. |
| 4.10 | Enterprise Data Integrity & Recovery | Completed | �� th�m data integrity checkpoint/scan/repair foundation, service `IDataIntegrityService`. |
| 4.11 | Compliance Packaging & Legal Output | Completed | �� th�m compliance package/manifest/submission/archive foundation, service `ICompliancePackagingService`. |
| 4.12 | Phase 4 Stabilization & Production Certification | Completed | �� th�m production certification report foundation, ch?y full build/test v� ki?m tra app startup runtime. |

Ghi ch� tracking:
- Phase 4 d� ho�n t?t tri?n khai foundation cho d?i step 4.4 -> 4.12 v� d� ki?m tra startup Release.
- Kh�ng t?o migration repair m?i; d� s?a migration g?c chua ghi history d? gi? d? li?u dev hi?n c�.
- Kh�ng thay d?i accounting/posting/report behavior hi?n t?i; ch? harden persistence/startup/Operations Center.




## Phase 4 startup hardening completion (2026-05-24)
- �� s?a l?i SQL Server 1785 do multiple cascade paths trong migration approval Phase 4.
- �� �p du?c c�c migration `AddEnterpriseApprovalWorkflowFoundation`, `AddAdvancedFinancialStatementMappingFoundation`, `AddPhase4ExecutionFoundationStep46To412` tr�n DB dev hi?n c�.
- �� harden seed b?o m?t/permission d? kh�ng insert tr�ng `administrator`/permission khi DB dev d� c� d? li?u.
- App Release m? du?c sau auto-migrate; process PID 25112 c�n ch?y sau hon 135 gi�y theo d�i, log m?i kh�ng c�n l?i startup chung, SQL 1785, duplicate key ho?c overlap `DbContext`.

## UI Consolidation Phase 1 (2026-05-24)

| H?ng m?c | Tr?ng th�i | Ghi ch� |
|---|---|---|
| Shared enterprise controls | Completed | Th�m `EnterpriseCommandToolbar`, `EnterpriseFilterPanel`, `EnterpriseSummaryCard`, `EnterpriseStatePresenter`, `EnterpriseCommandItem`. |
| Master-data toolbar consolidation | Completed | Customer/Supplier/Warehouse/Item/DepartmentPeople toolbar d�ng shared command toolbar, gi? nguy�n command binding/workflow. |
| Global resource consolidation | Completed | Gom command menu, card/button/grid, detail grid, dialog grid, state/status resources v�o theme chung. |
| DataGrid drift cleanup | Completed | Scan kh�ng c�n local `DataGrid.Resources`, inline header/cell/row style, ho?c module `*DataGridStyle` trong app XAML. |
| Validation | Completed | `dotnet build ASMILE.BO.sln -c Release --no-restore -warnaserror` pass; `dotnet test ASMILE.BO.sln -c Release --no-build` pass 113 tests. |
| Remaining local resource normalization | In progress | Ti?p t?c gom c�c style ri�ng c�n l?i trong GeneralLedger/Receivable/Payable/InventoryMovement/payment/stock/dialog views n?u kh�ng ?nh hu?ng workflow. |

## PHASE 4 UI Shell Refinement - Windows 11 Floating Navigation (2026-05-24)

| H?ng m?c | Tr?ng th�i | Ghi ch� |
|---|---|---|
| Shell layout refactor | Completed | Removed the fixed left sidebar from `DesktopShellView`; workspace stays full width and taskbar remains at the bottom. |
| Floating navigation flyout | Completed | Added lazy-loaded `ShellNavigationFlyoutView` overlay at shell level; it floats over the wallpaper/workspace and reuses existing shell state/commands. |
| Search and module launch | Completed | Search box, pinned modules, recent modules, quick actions, recent reports, and global search results now render in the shared flyout. |
| Keyboard and click-outside behavior | Completed | `Ctrl+P` opens the flyout and focuses search; `ESC` and outside click close the flyout/search overlay. |
| Taskbar refinement | Completed | Updated shared taskbar resources and item control styling for a centered translucent Windows 11 feel with subtle hover and active glow. |
| Validation | Completed | Debug and Release builds passed with 0 warnings; Release test run passed 113 tests. |

## PHASE 4 - Enterprise UI Normalization & Vietnamese Localization (2026-05-24)

| H?ng m?c | Tr?ng th�i | Ghi ch� |
|---|---|---|
| Phase A - Qu?n tr? b?o m?t | Completed | `SecurityAdminView` d�ng shared enterprise toolbar/cards/filter/state/grid, Vi?t h�a text hi?n th?, gi? nguy�n permission service/workflow. |
| Phase A - Thi?t l?p kh�ng gian l�m vi?c | Completed | `WorkspaceSettingsView` d�ng shared controls, th�m filter UI-only tr�n danh s�ch module, gi? persisted workspace settings. |
| Phase A - K? k? to�n | Completed | `FiscalPeriodManagementView` d�ng shared controls/grid, Vi?t h�a action/status, gi? nguy�n period service v� lock/close workflow. |
| Phase A - Trung t�m th�ng b�o | Completed | `NotificationTaskCenterView` d�ng shared controls/grid, Vi?t h�a action/status, gi? notification/task service persisted. |
| Navigation localization | Completed | Navigation catalog hi?n th? t�n ti?ng Vi?t cho c�c route m?c ti�u, route key gi? nguy�n d? backward compatibility. |
| Phase A validation | Completed | Debug build `-warnaserror` pass 0 warnings; scan Phase A kh�ng c�n local DataGrid resources/header/cell/row style. |
| Phase B - Trung t�m ki?m tra h? th?ng | Completed | `AuditCenterView` d�ng shared toolbar/filter/cards/state/grid, Vi?t h�a filter/tab/grid/action, gi? nguy�n audit/replay/export services. |
| Phase B - Trung t�m v?n h�nh | Completed | `OperationsCenterView` d�ng shared toolbar/cards/state/grid, Vi?t h�a operation tabs/actions/headers, gi? nguy�n scheduler/telemetry/workflow/compliance/approval workflows. |
| Phase C - Qu? & Ng�n h�ng | Completed | `CashBankView` d�ng shared toolbar/filter/cards/state/grid; report labels v� validation/import/reconciliation text d� Vi?t h�a, workflow persisted gi? nguy�n. |
| Phase C - TSC� & CCDC | Completed | `AssetManagementView` d�ng shared controls; report labels, adjustment selector, validation/drilldown text d� Vi?t h�a, fixed asset/tool asset workflows gi? nguy�n. |
| Phase C - Thu? & BCTC | Completed | `TaxFinancialReportingView` d�ng shared controls; VAT direction/report labels/status text d� Vi?t h�a, Tax/FS/Closing workflows gi? nguy�n. |
| Phase C validation | Completed | Debug/Release build `-warnaserror` pass 0 warnings; Release tests pass 113; target scans pass for DataGrid drift, `.Result`/`.Wait()`, and encoding. |

### Phase C normalization tracking
- Files created: `ASMILE.BO.App/Models/EnterpriseReportOption.cs`.
- Files modified: `ASMILE.BO.App/Views/CashBankView.xaml`, `ASMILE.BO.App/Views/AssetManagementView.xaml`, `ASMILE.BO.App/Views/TaxFinancialReportingView.xaml`, `ASMILE.BO.App/ViewModels/CashBankViewModel.cs`, `ASMILE.BO.App/ViewModels/AssetManagementViewModel.cs`, `ASMILE.BO.App/ViewModels/TaxFinancialReportingViewModel.cs`.
- Shared components reused: `EnterpriseCommandToolbar`, `EnterpriseFilterPanel`, `EnterpriseSummaryCard`, `EnterpriseStatePresenter`, `EnterpriseModuleDataGrid`, shared command/card resources.
- Remaining legacy areas: outside the Phase C target set, some inventory and detail-dialog presentation still carries older conventions for a future consolidation pass.

## Phase 4 Additive - Training Data Module (2026-05-25)

| H?ng m?c | Tr?ng th�i | Ghi ch� |
|---|---|---|
| Contracts/DTO/Enums | Completed | �� th�m d?y d? contracts `ITrainingDataService/Cleaner/Seeder/Validator` v� DTO/enum cho operation, profile, scope. |
| Safety guard & options | Completed | Th�m `TrainingDataOptions` (destructive guard, production guard, confirmation text), bind DI v� �p d?ng trong service. |
| Persistence additive | Completed | Th�m entities `TrainingDataRun`, `TrainingDataRunStep`; migration additive `AddTrainingDataRunHistory`; kh�ng s?a migration cu. |
| Infrastructure services | Completed | �� th�m `TrainingDataService`, `TrainingDataCleaner`, `TrainingDataSeeder`, `TrainingDataValidator`; c� audit + run-step logging. |
| Navigation + permission | Completed | Route `training-data`, title `D? li?u m?u`, map permission `training-data.admin`, module hi?n th? trong catalog shell. |
| UI module | Completed | �� th�m `TrainingDataViewModel` + `TrainingDataView` theo shared enterprise controls, c� thao t�c, log bu?c v� l?ch s? ch?y. |
| Build/Test validation | Completed with note | `dotnet build --no-incremental` pass; `dotnet test --no-build` fail do vstest source-path issue m�i tru?ng; ch?y `dotnet test` x�c nh?n pass 113 tests. |

### Tracking ghi ch�
- Tri?n khai hi?n t?i l� additive v� gi? nguy�n ki?n tr�c di?u hu?ng/workflow nghi?p v? hi?n c�.
- Ph?n seed giao d?ch m?u m?i ? m?c n?n t?ng; c?n v�ng ti?p theo d? m? r?ng full transaction coverage theo to�n b? matrix nghi?p v? k? to�n Vi?t Nam.
### Training-data Step 2 (Full Matrix)
- Status: In Progress (d� ho�n t?t code seed/validator/test unit c?t l�i, ch? x�c nh?n build/test to�n solution).
- Ho�n th�nh:
  - `FullTraining` m? r?ng theo matrix nghi?p v? qua service hi?n c� (inventory/journal/cash/bank/tax/debt).
  - Validator m? r?ng th�m AR/AP partner, VAT, inventory cost, t?n �m, data presence cho s? c�i.
  - B? sung test chuy�n bi?t `TrainingDataServiceTests`.
- Ghi ch� ki?m th?:
  - `dotnet test --no-build` c� th? l?i ngu?n du?ng d?n m�i tru?ng; x�c nh?n pass/fail ch�nh d�ng `dotnet test .\\ASMILE.BO.sln`.

### Training-data Step 3 (E2E verification + encoding scope)
- Status: Completed.
- Ho�n th�nh:
  - Th�m `ASMILE.BO.Tests.Integration/TrainingDataE2ETests.cs` ch?y lu?ng `ResetToBlank -> SeedFoundation -> SeedFullTraining`.
  - Th�m assertions demo-usable cho journal/trial balance/AR/AP/inventory cost/VAT/cash/bank.
  - Th�m report verification qua `IReportEngine` cho `GENERAL_LEDGER`, `TRIAL_BALANCE`, `INVENTORY_SUMMARY`, `AR_AGING`, `AP_AGING`, `VAT_INPUT`, `VAT_OUTPUT`.
  - �p d?ng skip-guard khi m� report kh�ng c� trong catalog hi?n t?i (kh�ng t?o report m?i).
- Encoding scope cho Step 3:
  - Ch? scan tr�n 3 file m?c ti�u: `TrainingDataE2ETests.cs`, `DEV_PROGRESS.md`, `ROADMAP_IMPLEMENTATION.md`.
  - Kh�ng m? r?ng scan to�n repo trong d?t n�y.

### Training-data Step 4 (Destructive reset hardening on SQL Server)
- Status: Completed.
- Ho�n th�nh:
  - Harden `TrainingDataCleaner` destructive path theo entity-level step v?i `IgnoreQueryFilters`, before/deleted/after, status/error.
  - Th�m fallback delete an to�n khi provider kh�ng h? tr? `ExecuteDelete` (batch `RemoveRange` trong transaction di?u ph?i b?i service).
  - M? r?ng run-step model/DTO/persistence v?i `EntityName`, `BeforeCount`, `AfterCount`, `Status`.
  - Th�m migration additive `20260525113000_AddTrainingDataRunStepDetails`.
  - C?p nh?t `TrainingDataView` step logs grid d? hi?n th? chi ti?t entity/count/status/error (kh�ng redesign UI).
  - Th�m SQL Server integration test `TrainingDataSqlServerResetTests` d�ng env guard `TRAINING_DATA_SQLSERVER_TEST_CONNECTION`.
    - N?u thi?u env var: test ghi r� l� do skip v� return.
    - N?u c� env var: verify flow th?t `SeedFoundation -> SeedFullTraining -> ResetToBlank non-dry-run`, business data b? x�a, system data c�n nguy�n.

### Training-data Step 5 (Onboarding hardening + preflight + marker reset)
- Status: Completed.
- Ho�n th�nh:
  - M? r?ng profile onboarding: `NewCompanyBlank`, `NewCompanyFoundation`, `NewCompanyFullTraining`.
  - M? r?ng operation: `PreflightCheck`, `ResetTrainingOnly`.
  - Th�m preflight DTO/checklist v� warning policy `Warn + require confirm`.
  - Th�m marker table additive `TrainingDataMarkers` + migration `AddTrainingDataMarkers`.
  - Seeder ghi marker theo run/profile cho d? li?u training ch�nh.
  - `ResetTrainingOnly` x�a theo marker, kh�ng x�a d? li?u kh�ng marker, kh�ng d?i semantics `ResetToBlank`.
  - C?p nh?t UI `TrainingDataView/ViewModel` d? hi?n th? onboarding checklist + ph�n bi?t thao t�c destructive to�n b? v� training-only.
  - B? sung t�i li?u `docs/TRAINING_DATA_ONBOARDING.md`.
  - B? sung unit/integration tests cho preflight warning v� reset training-only.
- Validation:
  - `dotnet build .\\ASMILE.BO.sln --no-incremental`: pass, 0 warnings.
  - `dotnet test .\\ASMILE.BO.sln`: pass to�n b? (120 tests).

### Training-data Step 6 (Production-readiness safety gate + marker coverage)
- Status: Completed.
- Ho�n th�nh:
  - Safety gate theo operation:
    - Full reset (`ResetToBlank`/`ClearBusinessData`/`DeleteTrainingData`) y�u c?u confirm `RESET ALL BUSINESS DATA`.
    - `ResetTrainingOnly` y�u c?u confirm `RESET TRAINING DATA`.
  - Th�m gate ch?n full reset khi preflight `HasRealDataRisk=true` v� `AllowResetWithRealDataRisk=false`.
  - M? r?ng options: `TrainingDataOptions.AllowResetWithRealDataRisk` (default false), c?p nh?t c?u h�nh m?u.
  - Harden audit/log:
    - t?o `correlationId` cho m?i run,
    - log `ConfirmationMatched` (kh�ng luu raw input),
    - log risk level, provider/environment,
    - log `ExecuteBlocked` cho nh�nh b? ch?n.
  - M? r?ng marker coverage:
    - th�m marker v� reset marker-only cho `CashFunds`, `BankAccounts` (ngo�i c�c nh�m ch�nh d� c�).
  - C?p nh?t UI `TrainingData` (kh�ng redesign):
    - hi?n th? confirm summary (provider/environment/destructive/risk),
    - hi?n th? r� confirm text theo full reset v� training-only reset.
  - C?p nh?t docs:
    - `docs/TRAINING_DATA_ONBOARDING.md` th�m m?c **Production safety rules** + checklist tru?c khi d�ng cho kh�ch th?t.
  - M? r?ng test:
    - unit: reject confirm sai theo operation,
    - unit: block full reset khi real-data risk + option false,
    - unit: allow full reset khi option true + confirm d�ng,
    - integration: block full reset khi real-data risk + option false,
  - integration: ki?m tra marker coverage cho c�c nh�m entity ch�nh.

### Training-data Step 7 (Hospitality onboarding UX + migration safety)
- Status: Completed.
- Ho�n th�nh:
  - Th�m schema probe an to�n cho `TrainingDataMarkers`; khi thi?u b?ng th� tr? warning th�n thi?n v� block thao t�c marker-dependent thay v� crash UI.
  - Th�m profile hospitality: `RestaurantDemo`, `HotelDemo`, `RestaurantHotelFullDemo`; gi? `FullTraining` d? tuong th�ch ngu?c.
  - Th�m API/DTO preview onboarding cho UI (`GetDemoProfilesAsync`, `GetDemoPreviewAsync`).
  - M? r?ng seed foundation/transaction theo t�n hi?u d? li?u nh� h�ng v� kh�ch s?n.
  - C?p nh?t UI `TrainingData` theo hu?ng onboarding (framing, model selection, action grouping, danger zone, preview, completion state).
  - B? sung test unit/integration cho profile hospitality, preview, v� missing marker table handling.
  - C?p nh?t t�i li?u `docs/TRAINING_DATA_ONBOARDING.md` theo d?nh v? nh� h�ng/kh�ch s?n.

### Training-data Step 8 (Onboarding UI polish)
- Status: Completed.
- Ho�n th�nh:
  - Refactor `TrainingDataView` th�nh onboarding center 2 c?t (main/sidebar), t?i uu spacing/card hierarchy.
  - Card selection cho 3 profile demo v?i selected-state r� r�ng.
  - T�ch nh�m h�nh d?ng: onboarding an to�n, qu?n l� d? li?u m?u, v�ng nguy hi?m.
  - Sidebar chu?n h�a preflight/preview/safety checklist d? d?c.
  - Warning UI d?i t? DataGrid sang alert list; log c� empty state v� compact grid.
  - Completion card sau thao t�c seed v?i quick actions c� guard disabled.
  - Chu?n h�a text ti?ng Vi?t tr�n `TrainingDataView` v� `TrainingDataViewModel`.
  - B? sung t�i li?u `docs/TRAINING_DATA_UI_POLISH.md`.
- Kh�ng thay d?i:
  - Backend business logic training-data.
  - Route/module key `training-data`.
  - Shell/navigation/workflow k? to�n hi?n c�.

### Training-data Step 9 (Production-grade onboarding UI)
- Status: Completed.
- Hoan thanh:
  - Polish `TrainingDataView` thanh onboarding production-grade 2 cot (main/sidebar) voi hierarchy ro rang.
  - Header badge state that cho environment/database/profile/status.
  - Demo profile cards onboarding-state (selected tint/check/chips/stats) bind tu DTO preview that.
  - Preview mini dashboard bind `TrainingDataDemoPreviewDto`.
  - Preflight card key-value + risk/destructive badges + migration warning card.
  - Action groups t�ch ro onboarding an toan / quan ly du lieu mau / vung nguy hiem.
  - Warning UI doi sang alert stack, bo DataGrid warning.
  - Log UI co empty state compact va bang virtualized khi co du lieu.
  - Completion success card + quick actions disabled-safe tooltip.
- Khong thay doi:
  - Backend training-data workflow.
  - Accounting/reset business logic.
  - Shell/navigation contracts.
- Validation:
  - `dotnet build .\\ASMILE.BO.sln --no-incremental`: pass, 0 warnings.
  - `dotnet test .\\ASMILE.BO.sln`: pass, 127/127 tests.

---

## Stabilization Phase 0 - Baseline & Safety (20260528_091901)

- Th?i gian: 2026-05-28T09:19:02.3741287+07:00
- Database backup: $backupFile
- Baseline report: D:\ASMILE-BO\KIEMTRAAPP_BEFORE_FIX.md
- Connection string: Server=localhost;Database=ASMILE_BO;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=True;
- Environment: Windows PowerShell, .NET SDK 10.0.100, app target .NET 8 WPF.
- Code changes: none in phase 0.
- Checkpoint: PASS.

## Stabilization Phase 8 - Runtime XamlParseException

- Status: Completed (PASS WITH WARNINGS).
- Root cause fixed: invalid hex color tokens in XAML resources triggered `ParseHexColor -> FormatException: Token is not valid`.
- Files fixed:
  - `ASMILE.BO.App/Views/AssetManagementView.xaml`
  - `ASMILE.BO.App/Views/TaxFinancialReportingView.xaml`
  - `ASMILE.BO.App/App.xaml.cs` (enhanced unhandled UI exception diagnostics)
- Quality gates:
  - Build: pass 0 warnings
  - Test: full suite pass
  - Runtime log: no new `Unhandled UI exception` / `XamlParseException` / `Token is not valid` after fix session startup.
- Remaining non-blocking warnings: EF Core MARS savepoint warning and `Skip/Take` without `OrderBy`.

## Breakfast Cafe Sales - Phase 1

- Status: Completed (additive implementation).
- Route/module: `breakfast-cafe-sales` ("B�n d? an s�ng & c� ph�").
- Delivered:
  - New domain entities + EF mappings + migration `AddBreakfastCafeSalesModule`.
  - New DTO/contracts/services for menu/order/payment/summary.
  - WPF UI view + viewmodel for fast selling workflow.
  - Navigation + permissions integration.
  - Menu default seed (no production transaction seed).
  - Costing integration skeleton via `IBreakfastCafeCostingService` (non-breaking TODO path).
- Quality gates:
  - Build pass 0 warnings.
  - Test pass full suite.
## C?p nh?t 2026-05-28 � Module `menu-recipe`

- �� th�m module n?n `M�n an & �?nh lu?ng` (route `menu-recipe`) theo ph?m vi pre-POS.
- Tr?ng th�i:
  - Qu?n l� m�n + tr?ng th�i active/inactive.
  - Luu d?nh lu?ng nguy�n v?t li?u theo d�ng.
  - Preview gi� v?n + c?nh b�o margin/t?n kho/thi?u gi� v?n.
  - Kh�ng c� order/payment/posting, kh�ng tr? kho.
- K? thu?t:
  - Reuse entity/service menu hi?n c�, m? r?ng field v� contract.
  - Th�m migration additive `AddMenuRecipeModule`.
  - T�ch h?p seed hospitality cho menu/recipe m?u.
## 2026-05-28 � Stabilization update cho `menu-recipe`

- �� ho�n t?t s?a mismatch code/schema sau khi dua module `menu-recipe` v�o runtime:
  - root cause: migration module chua apply + conflict index/FK tr�n DB hi?n h?u.
  - action: harden migration `AddMenuRecipeModule` theo idempotent v� c?p nh?t delete behavior `ChildMenuItemId` sang `NoAction`.
- Tr?ng th�i hi?n t?i:
  - DB schema d� d?ng b? v?i entity `MenuItem/MenuItemRecipeLine`.
  - startup ?n d?nh, kh�ng c�n invalid column cho module `menu-recipe`.

## 2026-05-28 - Menu Recipe UI polish completion

- Status: Completed.
- Scope: UI/ViewModel presentation only (`MenuRecipeView`, `MenuRecipeViewModel`).
- Delivered:
  - Enterprise-shell layout 3 v�ng (header/sidebar/main/right insights) cho `menu-recipe`.
  - Real-binding only, kh�ng d�ng fake demo data trong UI.
  - Empty states + KPI + warning panels + action grouping theo workflow qu?n l� m�n/d?nh m?c.
  - Computed presentation properties v� command aliases d? ?n d?nh binding/UX.
- Constraints respected:
  - Kh�ng th�m POS/order/payment.
  - Kh�ng post k? to�n.
  - Kh�ng tr? kho.
  - Kh�ng d?i route `menu-recipe`, kh�ng ph� `breakfast-cafe-sales`.
  - Kh�ng rewrite backend nghi?p v?.
- Quality gates:
  - Build: PASS 0 warnings.
  - Test: PASS full suite (135/135).
- Remaining risk:
  - C?n manual visual smoke to�n m�n h�nh 1366x768 v� thao t�c end-to-end tr�n app UI d? ch?t UX runtime cu?i c�ng.

## 2026-05-28 - Menu Recipe UI polish completion (Windows 11 shell)

- Status: Completed.
- Scope: UI/ViewModel presentation only cho route `menu-recipe`.
- Delivered:
  - workflow layout 4 v�ng (header/sidebar/main tabs/right insight).
  - lo?i b? bool raw `True/False` trong user-facing badges/text.
  - chu?n h�a format hi?n th? ti?n v� VAT.
  - tab-based workspace (`Th�ng tin m�n`, `�?nh m?c nguy�n li?u`, `Gi� v?n`, `L?ch s? thay d?i` disabled).
  - recipe grid t?p trung, c� empty state r� v� c?t nghi?p v? d?y d?.
  - computed state/display trong ViewModel d? d?m b?o bind th?t, kh�ng fake data.
- Constraints honored:
  - kh�ng th�m POS/order/payment.
  - kh�ng post k? to�n, kh�ng tr? kho.
  - kh�ng rewrite backend workflow.
- Quality gates:
  - Build PASS 0 warnings.
  - Test PASS full suite (1 test flaky n?n du?c rerun pass).
- Remaining risk:
  - c?n manual smoke thao t�c UI tr?c ti?p trong app (ch?n m�n/luu m�n/luu d?nh m?c/t�nh gi� v?n) d? ch?t nghi?m thu UX runtime cu?i.

## 2026-05-29 - Menu Recipe UI correction pass

- Status: Completed.
- Scope: `menu-recipe` UI cleanup and static app-wide input label scan.
- Delivered:
  - label coverage for search/category/recipe quick-entry controls.
  - shared Win11 tab styling for `menu-recipe`.
  - fixed read-only chip binding runtime exception.
  - fixed right panel text overlap.
  - verified XAML encoding scan passes.
- Quality gates:
  - Build PASS 0 warnings.
  - Test PASS full suite.
- Runtime:
  - New startup at 10:55 reached `Database is ready`; no new severe UI exception logged after this patch.

## 2026-05-29 - Menu Recipe reference UI redesign

- Status: Completed.
- Delivered a new `menu-recipe` layout following the supplied Windows 11 reference screen.
- Scope remained UI/ViewModel presentation only:
  - no POS/order/payment,
  - no accounting posting,
  - no stock mutation,
  - no fake menu/ingredient rows in XAML.
- Quality gates:
  - Build PASS 0 warnings.
  - Test PASS full suite.
  - XAML encoding scan PASS.
- Runtime:
  - Startup log after 11:05 is clean (`Database is ready`, no new severe UI exception).

## 2026-05-29 � Ho�n thi?n UI menu-recipe (MenuItem Recipe Management)
* ? PreparationArea added to MenuItem entity + DTO + migration + service mapping.
* ? Full UI rewrite: 4-zone professional workflow (Header/Sidebar/Tabs/Insight Panel).
* ? Badges: colored chips showing C� d?nh m?c/Chua d?nh m?c + �ang b�n/Ngung b�n.
* ? Format: gi� 45.000 d, VAT 8%, no True/False display.
* ? Filter system: search text, category dropdown, 4 filter chips (T?t c?/C� d?nh m?c/Chua d?nh m?c/Ngung b�n).
* ? Empty states for no-menu-selected and no-recipe-lines.
* ? Tab system: Th�ng tin m�n, �?nh m?c nguy�n li?u, Gi� v?n, L?ch s? thay d?i (disabled).
* ? Status bar with success/failure messages.
* ? Build: 0 errors, 0 warnings.
* ? Tests: Unit 124/124, Integration 11/11, 0 failed.

## 2026-05-29 — Fix menu-recipe button visibility (Add Menu button hidden)
* Fixed BooleanToVisibilityConverter not supporting ConverterParameter=Invert by adding dedicated ViewModel visibility properties (CanCreateMenu, ShowAddMenuButton, ShowBootstrapCategoriesButton).
* User can now create first menu item from empty DB (after bootstrap categories) or when categories exist but no items yet.

## 2026-05-29 — Fix "Unrecognized Guid format" blocker
* Fixed FilterCategoryId from string to Guid? — eliminated Guid.Parse on user-controlled data.
* Fixed CreateNewMenu() to auto-set MenuCategoryId from first available category.
* Added client-side validation before save (category, code, name required).
* Build PASS (0/0). Test PASS (135/135).

## 2026-05-30 — Fix sidebar click → menu detail loading
* Added OnSelectedMenuCardChanged partial method so WPF SelectedItem binding triggers async detail/recipe load.

## 2026-05-30 — Fix sidebar click → detail (attempt 2, actually saved to file)
* Added OnSelectedMenuCardChanged + LoadSelectedMenuCardAsync using array insertion (previous String.Replace silently failed).

## 2026-05-30 — Refactor menu-recipe to shared Fluent styles
* Replaced local styles with BasedOn shared: EnterpriseModuleTextBox/ComboBox, AccountingPanelCard, AccountingKpiCard.
* Build PASS (0/0). Test PASS (135/135).

## 2026-05-30 — Full XAML rebuild (4-zone Fluent enterprise)
* Rebuilt MenuRecipeView.xaml from scratch: 157 lines, 107 bindings, 4 tabs, 3-zone + footer layout.
* All styles inherit from shared ASMILE BO dictionaries. Build PASS 0/0, Test PASS 134/135.

## 2026-05-30 — State fix + Định lượng as default tab
* Immediate HasSelectedMenu notify on selection. Tabs reordered: Định lượng→Giá vốn→Thông tin chung→Lịch sử.
### 2026-05-30 — Fix blank workspace bug (menu-recipe)
- **Issue**: Clicking menu items in sidebar showed blank workspace instead of detail.
- **Root cause**: Three overlapping XAML elements (Grid.RowSpan=2) with conflicting Visibility. Empty-state DataTriggers didn't respond to HasSelectedMenu updates.
- **Fix**: Added ShowEmptyState property with direct BooleanToVisibilityConverter binding. Cleaned up duplicate async call.
- Build PASS (0/0). Test PASS (134/135, 1 flaky scheduler test pre-existing).
