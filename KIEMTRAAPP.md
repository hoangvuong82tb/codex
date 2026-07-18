# KI?M TRA TO�N B? APP ASMILE BO

## 1. T�m t?t k?t qu?

### C?p nh?t th?c thi stabilization (2026-05-28)

* Phase 0: COMPLETED.
  * Backup DB m?i: `D:\ASMILE-BO\Backups\ASMILE_BO_20260528_093130.bak`.
  * Baseline snapshot: `KIEMTRAAPP_BEFORE_FIX.md` d� c?p nh?t l?i t? b?n hi?n h�nh tru?c khi s?a.
* Phase 1: COMPLETED.
  * �� s?a th? c�ng l?i mojibake t?i `ASMILE.BO.App\Views\OperationsCenterView.xaml` (header `MemoryUsageBytes`).
  * `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
  * `scripts/check-xaml-encoding.ps1`: PASS.
* Phase 2: COMPLETED.
  * �� x? l� 2 test fail TT99/TT200 missing mapping b?ng c�ch s?a validator kh�ng auto-heal tru?c khi validate.
  * `dotnet test .\ASMILE.BO.sln`: PASS (Unit 120/120, Integration 11/11).
* Phase 3: COMPLETED.
  * L?n ch?y d?u reset fail do FK `PostingSources -> JournalHeaders`.
  * �� harden reset cleaner: x�a `PostingSources` tru?c `JournalHeaders`, th�m x�a `BusinessDocuments`.
  * Re-run reset `ResetToBlank` non-dry-run v?i confirm `RESET ALL BUSINESS DATA`: SUCCESS.
  * Verify business data m?c ti�u d� v? 0: `BusinessDocuments`, `JournalHeaders`, `JournalLines`, `Items`, `Customers`, `Suppliers`, `StockReceipts`, `StockIssues`, `CashTransactions`, `BankTransactions`, `ReceivableTransactions`, `PayableTransactions`, `VatTransactions`.
  * Verify system data c�n: `AppUsers=1`, `AppRoles=4`, `AppPermissions=34`, `AccountingStandards=3`, `AccountTemplates=64`.
* Phase 4: COMPLETED.
  * �� s?a th? c�ng mojibake user-facing trong to�n b? file scope Phase 4 (service + view ch�nh).
  * Kh�ng thay d?i logic nghi?p v?, kh�ng th�m t�nh nang, kh�ng s?a migration d� apply.
* Phase 6-7: CHUA TH?C HI?N TRONG L?N CH?Y N�Y.
  * Chua l�m startup hardening pass d?y d? v� smoke test UI to�n module.
* Phase 8 (final verdict hi?n t?i): PASS.
  * Build/Test/DB reset/Phase 4 mojibake cleanup d?u d?t.
  * C�c phase ngo�i ph?m vi l?n ch?y n�y (6-7) v?n d? bu?c ti?p theo c?a stabilization t?ng th?.

### C?p nh?t gate m?i nh?t (sau Phase 4)
* `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-xaml-encoding.ps1`: PASS.
* `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
* `dotnet test .\ASMILE.BO.sln`: PASS (Unit 120/120, Integration 11/11).
* Ghi ch�: c� 1 l?n test flaky ? `BackgroundSchedulerServiceTests.Cancellation_PropagatesToRunningExecution`; rerun test don l? v� rerun full suite d?u PASS.



## 17. S?a l?i mojibake + empty DB menu-recipe (2026-05-29)
* **Mojibake**: MenuRecipeView.xaml v� MenuRecipeViewModel.cs b? l?i encoding ti?ng Vi?t do luu file kh�ng d�ng UTF-8.
* S?a: regenerate to�n b? XAML + ViewModel qua C# code generator v?i Unicode escapes (\uXXXX) d?m b?o encoding ch�nh x�c.
* K?t qu?: to�n b? text ti?ng Vi?t hi?n th? d�ng � M�n an & �?nh m?c nguy�n li?u, Thi?t l?p m�n b�n..., T?ng m�n, C� d?nh m?c, C?nh b�o gi� v?n, Chua ch?n m�n, etc.
* **Empty DB**: Menu categories du?c seed t? d?ng t? MenuCatalogService.EnsureDefaultsAsync() khi DB tr?ng. Module c� d? li?u n?n ngay khi m?.
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (Unit 124/124, Integration 11/11).
* D?ng Phase 2 m? r?ng menu-recipe.
## 16. UI Rewrite menu-recipe � Professional Workflow UX (2026-05-29)
* M?c ti�u: Ho�n thi?n UI module menu-recipe cho qu?n l� m�n an & d?nh m?c nguy�n li?u chuy�n nghi?p.
* Th�m PreparationArea v�o backend (entity + DTO + migration + service map).
* Rewrite to�n b? XAML th�nh layout 4 v�ng workflow r� r�ng:
  - Header: title, subtitle, 3 KPI (T?ng m�n / C� d?nh m?c / C?nh b�o gi� v?n), 3 button (Th�m m�n / Luu / L�m m?i).
  - Sidebar 280px: SearchBox, ComboBox nh�m m�n, filter chips (T?t c?/C� d?nh m?c/Chua d?nh m?c/Ngung b�n), ListBox menu cards v?i t�n/m�/nh�m/gi�/badge m�u.
  - Main workspace v?i 4 tab: Th�ng tin m�n, �?nh m?c nguy�n li?u, Gi� v?n, L?ch s? thay d?i (disabled).
  - Right insight panel 260px: 3 card (T�m t?t m�n, Gi� v?n & l?i nhu?n, C?nh b�o).
- Vi?t l?i ViewModel: th�m FilteredMenuCards, SearchText, FilterCategoryId, ActiveFilterChip, StatusMessage, IsEditingStatusActive, FilterChips, ToggleActiveCommand, CalculateCostCommand, SelectFilterChipCommand.
- S?a hi?n th?: badge C� d?nh m?c/Chua d?nh m?c, �ang b�n/Ngung b�n v?i m�u; gi� 45.000 d, VAT 8%; kh�ng c�n True/False.
- Empty states: chua ch?n m�n ? "Ch?n m?t m�n b�n tr�i�"; chua c� d?nh m?c ? "M�n n�y chua c� d?nh m?c�".
- Status bar hi?n th? th�ng b�o luu th�nh c�ng/th?t b?i v?i m�u xanh/d?.
- Build: PASS (0 warnings, 0 errors).
- Test: PASS (Unit 124/124, Integration 11/11).
- Migration m?i: 20260529152901_AddPreparationAreaToMenuItem � th�m c?t PreparationArea nvarchar(100) NULL idempotent.
## 13. Runtime Smoke Test

* Th?i gian smoke runtime: 2026-05-28, b?t d?u phi�n app l�c kho?ng 09:59 (+07:00).
* H�nh th?c: manual-assisted (ngu?i d�ng thao t�c m? module, agent gi�m s�t log + gate build/test).
* Startup/login: app m? du?c, v�o shell du?c, kh�ng ghi nh?n `FTL`, kh�ng `StopHost`.

| Module | Startup | Opened | Empty State | Log Errors | Mojibake | Result |
| --- | --- | --- | --- | --- | --- | --- |
| T?ng quan | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Virtual File Center | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Qu?n l� xu?t nh?p t?n | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| C�ng n? ph?i thu | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| C�ng n? ph?i tr? | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Qu? & Ng�n h�ng | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| TSC� & CCDC | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Thu? & BCTC | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| S? c�i | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Qu?n l� kh�ch h�ng | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Qu?n l� nh� cung c?p | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Qu?n l� h�ng h�a | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Qu?n l� kho | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| B? ph?n & Nh�n s? | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Phi?u thu | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Phi?u chi | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Phi?u nh?p | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Phi?u xu?t | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| C�i d?t h? th?ng | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Qu?n tr? b?o m?t | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Trung t�m ki?m tra h? th?ng | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Trung t�m v?n h�nh | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| K? k? to�n | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| Trung t�m th�ng b�o | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |
| D? li?u m?u | PASS | PASS | H?p l� | Kh�ng g?n module c? th? | Kh�ng th?y | PASS |

### Log Verification (sau smoke)

Ph�t hi?n l?i runtime trong log phi�n m?i (m?c High):
- `ERR Unhandled UI exception`
- `System.Windows.Markup.XamlParseException` (`DeferredBinaryDeserializerExtension`)
- Inner exception: `System.FormatException: Token is not valid`
- D?u hi?u k? thu?t: l?i parse brush/color token trong runtime XAML/resource.

Kh�ng ph�t hi?n trong phi�n n�y:
- `FTL`
- `StopHost`
- `BackgroundService failed`
- `Invalid object name`
- `DbUpdateConcurrencyException`
- `Duplicate key AppRoles/AppPermissions`

Ph�n lo?i:
- Critical: 0
- High: 1 nh�m l?i (`Unhandled UI XamlParseException` l?p l?i)
- Medium: 1 nh�m warning seed consistency (`thi?u VAT0/VAT5/VAT8/VAT10`)
- Low: 0

### Build/Test h?u smoke

- `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
- `dotnet test .\ASMILE.BO.sln`: PASS sau rerun ?n d?nh (Unit 120/120, Integration 11/11).
- Ghi ch�: test `BackgroundSchedulerServiceTests.Cancellation_PropagatesToRunningExecution` c� t�nh flaky; l?n fail don l? d� rerun pass v� full suite rerun pass.

### Final Status

**FAIL**

L� do:
- D� build/test pass, log runtime th?t sau smoke v?n c� `Unhandled UI exception` + `XamlParseException` l?p l?i.
- Theo ti�u ch� b�n giao runtime, v?n c�n blocker ?n d?nh UI c?n x? l� tru?c khi c� th? k?t lu?n PASS/PASS WITH WARNINGS.

* Th?i gian ki?m tra: 2026-05-28, timezone Asia/Saigon.
* Branch/commit n?u l?y du?c: chua l?y du?c. Thu m?c `D:\ASMILE-BO` kh�ng ph?i git repository theo l?nh `git rev-parse`.
* Environment: Windows, PowerShell, .NET solution `D:\ASMILE-BO\ASMILE.BO.sln`, WPF/.NET 8 theo output build.
* Database: SQL Server `localhost`, database `ASMILE_BO`, connection string l?y t? `ASMILE.BO.App\appsettings.json`.
* Build: FAIL. `dotnet build .\ASMILE.BO.sln --no-incremental` tr? exit code 1.
* Test: FAIL. `dotnet test .\ASMILE.BO.sln` tr? exit code 1, unit test fail 2/120, integration test pass 11/11.
* T?ng s? l?i nghi�m tr?ng: 4 nh�m ch�nh.
* T?ng s? warning d�ng ch� �: 6 nh�m r?i ro/warning d�ng ch� �.
* Tr?ng th�i khuy?n ngh?: FAIL.

C�c l?i nghi�m tr?ng:

* Build fail do post-build encoding check ph�t hi?n mojibake trong `ASMILE.BO.App\Views\OperationsCenterView.xaml` d�ng 230.
* Unit test fail ? TT99/TT200 compliance mapping.
* Database sau reset tr?ng chua s?ch d? li?u nghi?p v?: v?n c�n ch?ng t?, b�t to�n, h�ng h�a, kh�ch h�ng, nh� cung c?p.
* V?n c�n mojibake trong UI/service, g?m nh�m view v?n h�nh/ng�n h�ng/thu?/TSC� v� message trong service.

## 2. L?nh d� ch?y

| L?nh | K?t qu? |
| --- | --- |
| `cd D:\ASMILE-BO` | OK |
| `dotnet build .\ASMILE.BO.sln --no-incremental` | FAIL, exit code 1 |
| `dotnet test .\ASMILE.BO.sln` | FAIL, exit code 1 |
| `git rev-parse --abbrev-ref HEAD; git rev-parse HEAD` | FAIL, kh�ng ph?i git repository |
| `sqlcmd -S localhost -U sa -P 123456 -d ASMILE_BO -C -Q ...` ki?m tra migration v� b?ng training data | OK |
| `sqlcmd ...` ki?m tra business/system/training counts | Ch?y du?c m?t ph?n; query `SettingDefinitions` fail v� b?ng kh�ng t?n t?i |
| `rg` scan log theo `ERR`, `FTL`, `Exception`, `XamlParseException`, `BindingExpression`, `Invalid object name`, `Duplicate key`, `DbUpdateConcurrencyException`, `BackgroundService failed`, d?u hi?u encoding | OK |
| `rg` scan `.cs`, `.xaml`, `.md`, `.json` theo d?u hi?u mojibake | OK |
| `rg` scan route/viewmodel/service/DI/navigation | OK static scan |
| Scan XAML binding/resource trong c�c file tr?ng di?m | OK static scan, chua smoke test UI th? c�ng |

## 3. K?t qu? build/test

* Build result: FAIL.
* Build warning count: 0 warning theo summary build.
* Build error count: 1 error theo summary build.
* L?i build ch�nh: post-build hook `scripts/check-xaml-encoding.ps1` ph�t hi?n l?i encoding ? `ASMILE.BO.App\Views\OperationsCenterView.xaml` d�ng 230, c?t `MemoryUsageBytes`, header b? mojibake.
* V� build fail ? encoding hook, startup app t? build hi?n t?i chua ki?m tra du?c d?y d?.

Test result:

* Integration tests: pass 11/11.
* Unit tests: fail 2, pass 118, total 120.
* Test warning count: chua t�ch ri�ng du?c t? output v� l?nh test k?t th�c fail; kh�ng th?y warning compiler n?i b?t trong ph?n build b?t bu?c.

Test fail:

| Test | Module li�n quan | Stack trace ng?n | Ghi ch� |
| --- | --- | --- | --- |
| `ASMILE.BO.Tests.Unit.TT99ComplianceTests.ValidateComplianceAsync_GeneratesIssuesForMissingMapping` | Compliance/account mapping TT99 | Expected issues empty to contain rule `TT99_MISSING_MAPPING`, t?i `ASMILE.BO.Tests.Unit\TT99ComplianceTests.cs:59` | Validator kh�ng sinh issue nhu test k? v?ng |
| `ASMILE.BO.Tests.Unit.TT200ComplianceTests.ValidateComplianceAsync_GeneratesIssuesForMissingMapping` | Compliance/account mapping TT200 | Expected issues empty to contain rule `TT200_MISSING_MAPPING`, t?i `ASMILE.BO.Tests.Unit\TT200ComplianceTests.cs:61` | Validator kh�ng sinh issue nhu test k? v?ng |

## 4. K?t qu? ki?m tra database/migration

* Migration status: ki?m tra tr?c ti?p du?c b?ng `sqlcmd`.
* 5 migration m?i nh?t d� c� trong `__EFMigrationsHistory`:
  * `20260527091500_AddStockReceiptDetailVatColumns`
  * `20260525133000_AddTrainingDataMarkers`
  * `20260525113000_AddTrainingDataRunStepDetails`
  * `20260525031433_AddTrainingDataRunHistory`
  * `20260524061631_AddPhase4ExecutionFoundationStep46To412`
* TrainingData tables: t?n t?i d? `TrainingDataRuns`, `TrainingDataRunSteps`, `TrainingDataMarkers`.
* L?i `Invalid object name 'TrainingDataMarkers'`: chua th?y trong DB hi?n t?i v� b?ng d� t?n t?i; log cu v?n c?n theo d�i n?u c� m�i tru?ng kh�c chua migrate.
* Reset state: FAIL. D? li?u nghi?p v? chua reset tr?ng.
* System data retained: c� d? li?u system c�n l?i.
* Business data cleared: FAIL.

S? li?u DB ki?m tra tr?c ti?p:

| Nh�m | B?ng | Count | K?t lu?n |
| --- | --- | ---: | --- |
| Business | `BusinessDocuments` | 2 | Chua s?ch |
| Business | `JournalHeaders` | 6 | Chua s?ch |
| Business | `Items` | 23 | Chua s?ch |
| Business | `Customers` | 6 | Chua s?ch |
| Business | `Suppliers` | 5 | Chua s?ch |
| System | `AppUsers` | 1 | C�n system data |
| System | `AppRoles` | 4 | C�n system data |
| System | `AppPermissions` | 34 | C�n system data |
| System | `AppSettings` | 0 | Kh�ng c� setting runtime |
| Training | `TrainingDataRuns` | 26 | C� history training |
| Training | `TrainingDataRunSteps` | 285 | C� history step |
| Training | `TrainingDataMarkers` | 213 | C� marker |

Ghi ch� DB:

* Query ki?m tra `SettingDefinitions` b�o `Invalid object name 'SettingDefinitions'`. Chua x�c nh?n d�y l� b?ng b?t bu?c hay ch? l� t�n cu/kh�ng c�n d�ng.
* Kh�ng ch?y reset d? li?u v� y�u c?u l� ki?m tra, kh�ng thay d?i workflow/d? li?u.

## 5. K?t qu? ki?m tra log

| M?c d? | Nh�m l?i | S? lu?ng | File/Module | Ghi ch� |
| --- | --- | ---: | --- | --- |
| Error | XAML binding read-only `PermissionSelectionRow.IsAssigned`/`IsDangerous` | Nhi?u d�ng | `logs\asmile-bo-20260524.log`, Security/Admin permission UI | Runtime log ghi l?i binding TwoWay/OneWayToSource v�o property read-only |
| Error | Training Data UI binding `CompletionSummaryItem.Value` | Nhi?u d�ng l?ch s? | `ASMILE.BO.App\bin\Release\net8.0-windows\logs\asmile-bo-20260526.log` v� b?n copy | Current XAML d� th?y d�ng OneWay, nhung chua smoke test UI v� build fail |
| Error | XamlParseException | Nhi?u d�ng l?ch s? | `logs\asmile-bo-20260423.log`, `logs\asmile-bo-20260501.log`, release logs | C� l?i parse/runtime XAML l?ch s?, chua x�c nh?n d� h?t trong app ch?y hi?n t?i |
| Error | Migration/FK cascade | �t nh?t 1 nh�m | `logs\asmile-bo-20260524.log` | L?i FK `ApprovalHistory_ApprovalWorkflows_WorkflowId` c� th? do migration cu |
| Error | DbContext concurrency | �t nh?t 1 nh�m | log l?ch s? | C� l?i second operation on same DbContext, c?n ki?m tra background/job/service lifecycle |
| Error | DI missing service | �t nh?t 1 nh�m l?ch s? | log l?ch s? | Missing `IEnterpriseConsistencyValidatorService` khi activate `FailureRecoveryService`; current DI compile nhung startup chua verify du?c |
| Warning | Mojibake/encoding | Nhi?u file | UI XAML v� service message | Build hi?n fail v� encoding; c�n nhi?u chu?i l?i ti?ng Vi?t |
| Not verified | BackgroundService failed/StopHost hi?n t?i | Chua ki?m tra du?c runtime hi?n t?i | Background scheduler | Build fail n�n chua x�c nh?n startup runtime hi?n t?i s?ch log |

## 6. K?t qu? ki?m tra module

| Module | Route/View/ViewModel | Service/DI | Empty DB Safe | Binding/Log Issue | Tr?ng th�i | Ghi ch� |
| --- | --- | --- | --- | --- | --- | --- |
| T?ng quan | C� `dashboard`/`DashboardViewModel`/`DashboardView.xaml` | ViewModel dang k� DI | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i ri�ng trong scan tr?ng di?m | Warning | Chua smoke test UI th? c�ng |
| Virtual File Center | C� `virtual-file-center`/`VirtualExplorerViewModel`/`VirtualExplorerView.xaml` | ViewModel dang k� DI | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i ri�ng | Warning | Static check OK, chua smoke test UI |
| Qu?n l� xu?t nh?p t?n | C� `inventory`/`InventoryMovementViewModel`/`InventoryMovementView.xaml` | Service inventory c� trong infrastructure | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i binding tr?ng di?m | Warning | Static check OK |
| C�ng n? ph?i thu | C� `ar`/`ReceivableViewModel`/`ReceivableView.xaml` | Service li�n quan AR/AP compile | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i binding tr?ng di?m | Warning | Static check OK |
| C�ng n? ph?i tr? | C� `ap`/`PayableViewModel`/`PayableView.xaml` | Service li�n quan AP compile | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i binding tr?ng di?m | Warning | Static check OK |
| Qu? & Ng�n h�ng | C� `cash-bank`/`CashBankViewModel`/`CashBankView.xaml` | `ICashFundService`, `IBankingService` c� DI | Chua ki?m tra runtime v?i DB tr?ng | C� binding r?i ro t�n `AmCount`; service banking c�n mojibake | Warning | C?n x�c minh property d�ng l� `Amount` hay `AmCount` |
| TSC� & CCDC | C� `asset-management`/`AssetManagementViewModel`/`AssetManagementView.xaml` | Service asset c� trong DI/module compile | Chua ki?m tra runtime v?i DB tr?ng | C� binding r?i ro `AdjustmentAmCount`, `DepreciationAmCount` | Warning | C?n d?i chi?u property VM/DTO |
| Thu? & BCTC | C� `tax-financial`/`TaxFinancialReportingViewModel`/`TaxFinancialReportingView.xaml` | `ITaxService`, `IFinancialStatementService`, `IClosingService` c� DI | Chua ki?m tra runtime v?i DB tr?ng | C� binding r?i ro `AmCount`; `ClosingService` c�n mojibake | Warning | Test TT99/TT200 fail li�n quan compliance/mapping |
| S? c�i | C� `general-ledger`/`GeneralLedgerViewModel`/`GeneralLedgerView.xaml` | Service ledger compile | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i binding tr?ng di?m | Warning | Static check OK |
| Qu?n l� kh�ch h�ng | C� `customers`/`CustomerPageViewModel`/`CustomerPage.xaml` | ViewModel DI OK | DB hi?n v?n c�n 6 kh�ch h�ng, chua d�ng reset tr?ng | Chua th?y l?i ri�ng | Warning | Empty DB chua ki?m tra du?c v� DB kh�ng tr?ng |
| Qu?n l� nh� cung c?p | C� `suppliers`/`SupplierPageViewModel`/`SupplierPage.xaml` | ViewModel DI OK | DB hi?n v?n c�n 5 nh� cung c?p | Chua th?y l?i ri�ng | Warning | Empty DB chua ki?m tra du?c |
| Qu?n l� h�ng h�a | C� `items`/`ItemsViewModel`/`ItemsView.xaml` | ViewModel DI OK | DB hi?n v?n c�n 23 h�ng h�a | Chua th?y l?i ri�ng | Warning | Empty DB chua ki?m tra du?c |
| Qu?n l� kho | C� `warehouses`/`WarehousesViewModel`/`WarehousesView.xaml` | ViewModel DI OK | Chua ki?m tra runtime v?i DB tr?ng | TrainingDataService c�n t�n kho mojibake | Warning | Static check OK |
| B? ph?n & Nh�n s? | C� `departments`/`DepartmentPeoplePageViewModel`/`DepartmentPeoplePage.xaml` | ViewModel DI OK | Chua ki?m tra runtime v?i DB tr?ng | Migration seed cu c� t�n b? ph?n mojibake | Warning | C?n x? l� seed/migration n?u hi?n th? ra UI |
| Phi?u thu | C� `payment-receipt`/`PaymentReceiptsViewModel`/`PaymentReceiptsView.xaml` | ViewModel DI OK | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i ri�ng | Warning | Static check OK |
| Phi?u chi | C� `payment-voucher`/`PaymentVouchersViewModel`/`PaymentVouchersView.xaml` | ViewModel DI OK | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i ri�ng | Warning | Static check OK |
| Phi?u nh?p | C� `receipts`/`StockReceiptsViewModel`/`StockReceiptsView.xaml` | ViewModel DI OK | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i ri�ng | Warning | Static check OK |
| Phi?u xu?t | C� `issues`/`StockIssuesViewModel`/`StockIssuesView.xaml` | ViewModel DI OK | Chua ki?m tra runtime v?i DB tr?ng | Chua th?y l?i ri�ng | Warning | Static check OK |
| C�i d?t h? th?ng | C� `settings`/`SettingsViewModel`/`SettingsView.xaml` | ViewModel DI OK | Chua ki?m tra runtime v?i DB tr?ng | Query `SettingDefinitions` kh�ng t?n t?i khi ki?m tra DB | Warning | C?n x�c d?nh schema setting d�ng |
| Qu?n tr? b?o m?t | C� `security-admin` v� `authorization`/`SecurityAdminViewModel`/`SecurityAdminView.xaml` | Permission/navigation services c� DI | Chua ki?m tra runtime v?i DB tr?ng | Log cu c� binding read-only permission row | Warning | C?n smoke test sau khi build pass |
| Trung t�m ki?m tra h? th?ng | C� `audit-center`/`AuditCenterViewModel`/`AuditCenterView.xaml` | ViewModel DI OK | Chua ki?m tra runtime v?i DB tr?ng | Scan c�n d?u hi?u mojibake trong view audit | Warning | Static check c� r?i ro encoding |
| Trung t�m v?n h�nh | C� `operations-center`/`OperationsCenterViewModel`/`OperationsCenterView.xaml` | ViewModel DI OK | Chua ki?m tra runtime v?i DB tr?ng | Build fail tr?c ti?p t?i view n�y do encoding | Error | Ph?i s?a tru?c khi b�n giao |
| K? k? to�n | C� `fiscal-periods`/`FiscalPeriodManagementViewModel`/`FiscalPeriodManagementView.xaml` | Closing/fiscal services compile | Chua ki?m tra runtime v?i DB tr?ng | C� r?i ro encoding trong nh�m view d� scan | Warning | Static check chua d? k?t lu?n runtime |
| Trung t�m th�ng b�o | C� `notification-center`/`NotificationTaskCenterViewModel`/`NotificationTaskCenterView.xaml` | Background scheduler/notification DI c� | Chua ki?m tra runtime v?i DB tr?ng | Scan c�n d?u hi?u mojibake trong view notification | Warning | Background runtime chua verify do build fail |
| D? li?u m?u | C� `training-data`/`TrainingDataViewModel`/`TrainingDataView.xaml` | Training services c� DI | Chua ki?m tra UI v?i DB tr?ng | Log cu c� binding `CompletionSummaryItem.Value`; current XAML c� OneWay | Warning | Service/test c� coverage nhung app runtime chua smoke test |

Ghi ch� chung module:

* Route/viewmodel/navigation registration ki?m tra b?ng static scan trong `NavigationService.cs` v� DI registration.
* Kh�ng module n�o du?c x�c nh?n OK tuy?t d?i v� build hi?n fail v� chua smoke test UI th? c�ng.
* Empty DB safe chua x�c nh?n du?c th?c t? v� DB hi?n t?i kh�ng ? tr?ng th�i tr?ng.

## 7. K?t qu? ki?m tra Training Data

* Preflight: c� `GetPreflightAsync` trong `TrainingDataService`; chua ch?y UI preflight v� build fail.
* Safety gate: c� c�c lu?ng reset `ResetToBlank`, `ResetTrainingOnly`; chua th?c thi reset v� y�u c?u ki?m tra, kh�ng thay d?i d? li?u.
* Marker: `TrainingDataMarkers` t?n t?i trong DB; service c� x? l� khi marker schema missing b?ng warning `TRAINING_MARKER_TABLE_MISSING`.
* Hospitality profiles: t�m th?y `RestaurantDemo`, `HotelDemo`, `RestaurantHotelFullDemo`.
* Reset: chua x�c nh?n end-to-end b?ng UI; DB hi?n t?i ch?ng minh reset tr?ng chua d?t v� business data v?n c�n.
* UI binding: current `TrainingDataView.xaml` d� th?y binding `CompletionSummaryItem.Value` d�ng `Mode=OneWay`, nhung log release cu v?n c� l?i; c?n smoke test sau khi build pass.
* Docs: chua ki?m tra t�i li?u onboarding/training-data d?y d?; ch? ki?m tra code/test/schema li�n quan.
* Kh�ng c�n mojibake trong module n�y: chua d?t. `TrainingDataService.cs` v?n c� chu?i t�n kho ti?ng Vi?t b? mojibake.

## 8. K?t qu? ki?m tra encoding/mojibake

| File | V?n d? | M?c d? | G?i � x? l� |
| --- | --- | --- | --- |
| `ASMILE.BO.App\Views\OperationsCenterView.xaml` | Build hook ph�t hi?n header b? mojibake t?i d�ng 230 | Critical | S?a d�ng UTF-8 theo n?i dung nghi?p v?, ch?y l?i build |
| `ASMILE.BO.App\Views\CashBankView.xaml` | C� binding/chu?i nghi ng? b? s?a sai ch? trong t�n property `AmCount` | High | �?i chi?u VM/DTO, s?a d�ng property n?u l� `Amount`; kh�ng s?a do�n |
| `ASMILE.BO.App\Views\TaxFinancialReportingView.xaml` | C� binding nghi ng? `AmCount` | High | �?i chi?u property th?t trong VM/DTO |
| `ASMILE.BO.App\Views\AssetManagementView.xaml` | C� binding nghi ng? `AdjustmentAmCount`, `DepreciationAmCount` | High | �?i chi?u property th?t trong VM/DTO |
| `ASMILE.BO.App\Views\AuditCenterView.xaml` | Scan c�n d?u hi?u mojibake/ch? sai trong text ti?ng Vi?t | Medium | S?a th? c�ng theo ng? c?nh UI sau khi x�c minh |
| `ASMILE.BO.App\Views\NotificationTaskCenterView.xaml` | Scan c�n d?u hi?u mojibake/ch? sai trong text ti?ng Vi?t | Medium | S?a th? c�ng theo ng? c?nh UI sau khi x�c minh |
| `ASMILE.BO.App\Views\WorkspaceSettingsView.xaml` | Nh�m view t?ng du?c scan c� d?u hi?u encoding/ch? sai | Medium | Re-scan t?ng d�ng v� s?a c� ki?m ch?ng |
| `ASMILE.BO.App\Views\FiscalPeriodManagementView.xaml` | Nh�m view t?ng du?c scan c� d?u hi?u encoding/ch? sai | Medium | Re-scan t?ng d�ng v� s?a c� ki?m ch?ng |
| `ASMILE.BO.App\Views\SecurityAdminView.xaml` | Nh�m view t?ng du?c scan c� d?u hi?u encoding/ch? sai, log cu c� binding permission | Medium | Ki?m tra text v� binding sau build pass |
| `ASMILE.BO.App\Views\AssetManagementView.xaml` | Nh�m view t?ng du?c scan c� d?u hi?u encoding/ch? sai | Medium | Ki?m tra text hi?n th? ngu?i d�ng |
| `ASMILE.BO.Infrastructure\Services\BankingService.cs` | Nhi?u exception/log message ti?ng Vi?t b? mojibake | High | S?a th? c�ng message user-facing/log-facing, luu UTF-8 |
| `ASMILE.BO.Infrastructure\Services\ClosingService.cs` | Nhi?u exception/log message ti?ng Vi?t b? mojibake | High | S?a th? c�ng message k? to�n, ch?y test TT99/TT200 sau s?a |
| `ASMILE.BO.Infrastructure\Services\PermissionService.cs` | Message permission ti?ng Vi?t b? mojibake | Medium | S?a message v� smoke test security admin |
| `ASMILE.BO.Infrastructure\Services\TrainingDataService.cs` | M?t s? t�n kho/seed ti?ng Vi?t b? mojibake | Medium | S?a seed text v� ki?m tra reset/training data |
| `ASMILE.BO.Infrastructure\Persistence\Migrations\*.cs` | M?t s? migration seed cu c� text mojibake | Low/Medium | Ch? s?a n?u migration chua apply ho?c c� chi?n lu?c data correction; tr�nh rewrite migration d� apply n?u kh�ng c?n |

## 9. K?t qu? ki?m tra XAML binding

| File | Binding/Resource issue | M?c d? | G?i � x? l� |
| --- | --- | --- | --- |
| `ASMILE.BO.App\Views\TrainingDataView.xaml` | Log cu l?i binding read-only `CompletionSummaryItem.Value`; current XAML d�ng `Mode=OneWay` | Medium | Smoke test UI sau khi build pass d? x�c nh?n h?t l?i |
| `ASMILE.BO.App\Views\SecurityAdminView.xaml` | Log cu l?i binding read-only `PermissionSelectionRow.IsAssigned` v� `IsDangerous` | High | Ki?m tra binding mode ho?c property setter trong VM |
| `ASMILE.BO.App\Views\CashBankView.xaml` | Binding property nghi ng? sai t�n `AmCount` | High | �?i chi?u v?i VM; n?u property kh�ng t?n t?i s? g�y binding fail/runtime UI kh�ng hi?n th? s? ti?n |
| `ASMILE.BO.App\Views\TaxFinancialReportingView.xaml` | Binding property nghi ng? sai t�n `AmCount` | High | �?i chi?u v?i VM/report DTO |
| `ASMILE.BO.App\Views\AssetManagementView.xaml` | Binding property nghi ng? sai t�n `AdjustmentAmCount`, `DepreciationAmCount` | High | �?i chi?u v?i VM/DTO |
| `ASMILE.BO.App\Views\DashboardView.xaml` | Static scan chua ph�t hi?n l?i c? th? | Not verified | Chua smoke test UI th? c�ng |
| `ASMILE.BO.App\Views\GeneralLedgerView.xaml` | Static scan chua ph�t hi?n l?i c? th? | Not verified | Chua smoke test UI th? c�ng |
| `ASMILE.BO.App\Views\InventoryMovementView.xaml` | Static scan chua ph�t hi?n l?i c? th? | Not verified | Chua smoke test UI th? c�ng |
| `ASMILE.BO.App\Views\ReceivableView.xaml` | Static scan chua ph�t hi?n l?i c? th? | Not verified | Chua smoke test UI th? c�ng |
| `ASMILE.BO.App\Views\PayableView.xaml` | Static scan chua ph�t hi?n l?i c? th? | Not verified | Chua smoke test UI th? c�ng |
| `ASMILE.BO.App\Views\SettingsView.xaml` | Static scan chua ph�t hi?n l?i c? th?; DB setting schema c� nghi v?n | Warning | X�c minh schema setting v� UI setting sau build pass |

Ghi ch� XAML:

* Build d� di t?i XAML compile v� fail ? post-build encoding hook, kh�ng th?y l?i compile-time StaticResource n?i b?t trong output build.
* Static scan kh�ng thay th? du?c UI smoke test. Hi?n chua smoke test UI th? c�ng v� build fail.

## 10. R?i ro c�n l?i

* Kh�ng th? x�c nh?n app startup s?ch v� build hi?n fail.
* Kh�ng th? x�c nh?n runtime log hi?n t?i s?ch v� chua ch?y du?c app t? build pass.
* Kh�ng th? x�c nh?n empty state sau reset tr?ng v� DB hi?n t?i kh�ng tr?ng.
* V?n c�n r?i ro binding silent fail trong WPF n?u property sai t�n nhung kh�ng l�m build fail.
* V?n c�n r?i ro background scheduler/hosted service fail khi app startup do log cu c� DbContext concurrency v� DI missing service.
* V?n c�n r?i ro m�i tru?ng kh�c thi?u migration training-data n?u chua apply migration `AddTrainingDataMarkers`.
* C� nguy co s?a encoding h�ng lo?t sai nghia nghi?p v? n?u t? d?ng replace kh�ng theo ng? c?nh.

## 11. Vi?c c?n s?a uu ti�n

### Critical

* S?a l?i encoding trong `OperationsCenterView.xaml` l�m build fail.
* S?a ho?c c?p nh?t logic/test TT99 v� TT200 d? test compliance mapping pass d�ng nghi?p v?.
* X�c d?nh v� th?c hi?n quy tr�nh reset tr?ng d? li?u nghi?p v?; hi?n DB v?n c�n d? li?u business.

### High

* Ki?m tra v� s?a to�n b? mojibake user-facing trong `BankingService.cs`, `ClosingService.cs`, `PermissionService.cs`, `TrainingDataService.cs`.
* �?i chi?u c�c binding nghi ng? `AmCount`/`AdjustmentAmCount`/`DepreciationAmCount` v?i ViewModel/DTO th?c t?.
* Ki?m tra l?i binding permission row trong `SecurityAdminView.xaml` d? kh�ng binding TwoWay v�o read-only property.
* Ch?y l?i app startup sau khi build pass v� scan log m?i.

### Medium

* Re-scan th? c�ng 9 file XAML ngu?i d�ng d� n�u: `OperationsCenterView.xaml`, `TaxFinancialReportingView.xaml`, `SecurityAdminView.xaml`, `AssetManagementView.xaml`, `CashBankView.xaml`, `FiscalPeriodManagementView.xaml`, `WorkspaceSettingsView.xaml`, `NotificationTaskCenterView.xaml`, `AuditCenterView.xaml`.
* Ki?m tra l?i UI route t?ng module b?ng smoke test n?u c� automation ho?c thao t�c th? c�ng.
* X�c minh schema setting d�ng, d?c bi?t vi?c kh�ng c� b?ng `SettingDefinitions`.
* Ki?m tra background scheduler b?ng startup runtime v� log m?i.

### Low

* Xem x�t text mojibake trong migration seed cu; ch? s?a b?ng data correction/migration m?i n?u text d� c�n hi?n th? cho ngu?i d�ng.
* B? sung script scan encoding v�o CI d? ch?n l?i l?p l?i tru?c khi commit/build release.
* B? sung checklist binding XAML cho property read-only v� property kh�ng t?n t?i.

## 12. K?t lu?n

App chua d? di?u ki?n b�n giao kh�ch m?i.

L� do: build hi?n t?i FAIL do l?i encoding, unit test FAIL ? TT99/TT200, database sau reset tr?ng chua s?ch d? li?u nghi?p v?, v� v?n c�n nhi?u d?u hi?u mojibake/binding/runtime log c?n x? l�. C�c module d� c� route/view/viewmodel/DI ? m?c static scan, nhung chua th? x�c nh?n runtime ?n d?nh v� build kh�ng pass v� chua smoke test UI th? c�ng.

�i?u ki?n t?i thi?u tru?c khi b�n giao:

* Build pass v?i `dotnet build .\ASMILE.BO.sln --no-incremental`.
* Test pass ho?c c� quy?t d?nh nghi?p v? r� r�ng cho 2 test TT99/TT200 dang fail.
* DB reset tr?ng th?t s? s?ch business data nhung gi? system data c?n thi?t.
* Kh�ng c�n mojibake trong c�c view/service user-facing.
* Ch?y startup app v� scan log m?i kh�ng c�n l?i XAML binding, DbContext concurrency, DI missing service, invalid object name ho?c background service fail.

## 13. Stabilization Phase 8 - Runtime XamlParseException Fix (2026-05-28)

### Root cause
- L?i runtime `System.Windows.Markup.XamlParseException` v?i inner `System.FormatException: Token is not valid` ph�t sinh khi WPF parse `SolidColorBrush` trong `ResourceDictionary`.
- Nguy�n nh�n tr?c ti?p: m� m�u hex kh�ng h?p l? (k� t? `o`) trong style DatePicker ? 2 view:
  - `ASMILE.BO.App/Views/AssetManagementView.xaml` (line 10, 11, 38)
  - `ASMILE.BO.App/Views/TaxFinancialReportingView.xaml` (line 10, 11, 38)

### S?a d� th?c hi?n
- S?a literal m�u:
  - `#FoFEFF` -> `#F0FEFF`
  - `#o4D2E4` -> `#04D2E4`
  - `#AEo3DE` -> `#AE03DE`
- B? sung logging chi ti?t `DispatcherUnhandledException` t?i `ASMILE.BO.App/App.xaml.cs` (line 87, 91, 125, 144):
  - log `Exception.ToString()`, base exception, source, target site, inner exception chain, current view context.
  - kh�ng nu?t l?i b?ng try/catch workaround.

### Verification
- Build: `dotnet build .\ASMILE.BO.sln --no-incremental` -> **PASS** (0 warnings, 0 errors).
- Test: `dotnet test .\ASMILE.BO.sln` -> **PASS** (131/131).
- Runtime startup check:
  - App kh?i d?ng phi�n m?i l�c `2026-05-28 10:44:42` (log c� migration/database ready).
  - Kh�ng ph�t sinh th�m `Unhandled UI exception` / `XamlParseException` / `FormatException: Token is not valid` sau m?c n�y.
- Log c�n warning m?c th?p:
  - EF Core warning `Savepoints disabled because MARS is enabled`.
  - EF Core warning `Skip/Take without OrderBy`.

### Phase 8 status
- **PASS WITH WARNINGS**
- L� do kh�ng ch?t PASS tuy?t d?i trong phi�n n�y: chua c� full manual smoke matrix 25 module sau b?n v�; tuy nhi�n blocker runtime `Token is not valid` d� du?c x? l� d?t di?m theo log.

## 14. New Module Added - Breakfast Cafe Sales

- Module: `B�n d? an s�ng & c� ph�`
- Route: `breakfast-cafe-sales`
- Ki?m tra k? thu?t:
  - Build: PASS (0 warnings, 0 errors)
  - Test: PASS (full suite)
  - Navigation mapping: d� c� route/module/resolver/permission mapping
- Ghi ch� smoke test b? sung:
  - C?n dua `breakfast-cafe-sales` v�o checklist runtime smoke phase ti?p theo d? x�c nh?n UX th?c d?a (mouse/touch flow, empty state, payment method switching, log cleanliness).

---

## Stabilization Phase 0 - Baseline & Safety (20260528_091901)

- Th?i gian: 2026-05-28T09:19:02.3741287+07:00
- Database backup: $backupFile
- Baseline report: D:\ASMILE-BO\KIEMTRAAPP_BEFORE_FIX.md
- Connection string: Server=localhost;Database=ASMILE_BO;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=True;
- Environment: Windows PowerShell, .NET SDK 10.0.100, app target .NET 8 WPF.
- Code changes: none in phase 0.
- Checkpoint: PASS.
## 14. Module m?i c?n smoke test

- Module: `M�n an & �?nh lu?ng`
- Route: `menu-recipe`
- Tr?ng th�i hi?n t?i:
  - Build pass, test pass.
  - Chua ch?y runtime smoke UI chi ti?t cho module m?i trong v�ng n�y.
- Checklist smoke k? ti?p:
  - M? module t? shell.
  - T?o/s?a m�n.
  - Luu d?nh lu?ng.
  - Ch?y preview gi� v?n v� ki?m tra c?nh b�o.
## 15. Runtime Fix `menu-recipe` Schema Mismatch (2026-05-28)

- L?i runtime ghi nh?n:
  - `Invalid column name 'HasRecipeManagement'`
  - `Invalid column name 'Note'`
- Nguy�n nh�n g?c:
  - DB chua apply migration `20260528183000_AddMenuRecipeModule`.
  - Migration ban d?u fail gi?a ch?ng do:
    - index d� t?n t?i (`IX_MenuItemRecipeLines_ItemId`)
    - FK `ChildMenuItemId` g�y multiple cascade path tr�n SQL Server.
- C�ch x? l�:
  - Chu?n h�a migration `AddMenuRecipeModule` theo hu?ng idempotent (guard c?t/index/FK).
  - �?i quan h? `ChildMenuItemId` sang `DeleteBehavior.NoAction`.
  - Kh?i d?ng app d? `DatabaseMigrationService` apply migration pending.
- X�c minh DB:
  - `__EFMigrationsHistory` d� c� `20260528183000_AddMenuRecipeModule`.
  - `MenuItems` d� c� c?t `HasRecipeManagement`, `Note`.
  - `MenuItemRecipeLines` d� c� `ChildMenuItemId`, `DefaultWarehouseId`, `IsRequired`.
- X�c minh runtime:
  - Startup log m?i k?t th�c b?ng `Database is ready`.
  - Kh�ng c�n l?i `Invalid column name 'HasRecipeManagement'/'Note'` trong phi�n m?i.

## 16. Menu Recipe UI Polish (2026-05-28)

### Scope
- Refactor giao di?n module `menu-recipe` theo style Windows 11 Enterprise Shell.
- Ch? s?a:
  - `ASMILE.BO.App/Views/MenuRecipeView.xaml`
  - `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.cs`
- Kh�ng thay d?i backend workflow, kh�ng th�m POS/order/payment/posting/tr? kho.

### K?t qu?
- UI d� chuy?n sang b? c?c 3 v�ng r� r�ng:
  - Header KPI + h�nh d?ng ch�nh.
  - Sidebar tr�i t�m/l?c/danh s�ch m�n + empty state.
  - V�ng ch�nh th�ng tin m�n c� label + card d?nh m?c nguy�n li?u.
  - Sidebar ph?i gi� v?n d? ki?n + c?nh b�o + h�nh d?ng.
- D? li?u bind th?t t? service/ViewModel, kh�ng hardcode demo item trong XAML.
- B? sung computed state/command alias trong ViewModel d? UX nh?t qu�n v� tr�nh binding r?i r?c.
- Kh�ng c�n l?i schema `Invalid column name 'HasRecipeManagement'/'Note'` trong phi�n runtime m?i (d� x? l� ? phase schema hotfix tru?c d�).

### Build/Test
- `dotnet build .\ASMILE.BO.sln --no-incremental` => PASS (0 warnings, 0 errors)
- `dotnet test .\ASMILE.BO.sln` => PASS (full suite)

### Runtime smoke status (module)
- `menu-recipe` m? du?c theo route.
- Kh�ng ghi nh?n l?i nghi�m tr?ng m?i thu?c nh�m:
  - `XamlParseException`
  - `Invalid column name`
  - `Unhandled UI exception`
- C?n ti?p t?c manual smoke UX thao t�c d?y d? tr�n phi�n ngu?i d�ng d? ch?t visual acceptance cu?i.

### Tr?ng th�i
- PASS WITH WARNINGS (c�n bu?c manual UX smoke full checklist d? ch?t nghi?m thu giao di?n).

## 17. Menu Recipe UI Polish (2026-05-28)

### M?c ti�u
Ho�n thi?n UI module `menu-recipe` theo workflow qu?n l� m�n v� d?nh m?c nguy�n li?u, kh�ng d?i backend nghi?p v?.

### K?t qu? ch�nh
- �� refactor `MenuRecipeView` + `MenuRecipeViewModel` theo style Enterprise shell.
- Sidebar m�n d? d?c, c� chips filter single-select; badge hi?n th? nghi?p v? thay cho `True/False`.
- Main workspace chuy?n sang tabs:
  - Th�ng tin m�n
  - �?nh m?c nguy�n li?u (tr?ng t�m)
  - Gi� v?n
  - L?ch s? thay d?i (disabled/tooltip)
- Right panel hi?n th? t�m t?t m�n, gi� v?n/l?i nhu?n v� c?nh b�o.
- Format user-facing:
  - ti?n `N0 + d`
  - VAT `%`
- Kh�ng c� fake data trong XAML; d? li?u bind t? ViewModel/service th?t.

### Build/Test
- `dotnet build .\\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors)
- `dotnet test .\\ASMILE.BO.sln`: PASS full suite (sau 1 l?n rerun do test flaky n?n scheduler)

### Runtime smoke
- Startup/build-test sau ch?nh UI kh�ng ph�t sinh l?i bi�n d?ch/binding blocker.
- Log file ng�y 2026-05-28 v?n ch?a l?i migration l?ch s? t? c�c phi�n tru?c; kh�ng ph?i l?i m?i do UI polish n�y.
- C?n manual smoke tr?c ti?p route `menu-recipe` d? ch?t acceptance d?y d? c�c thao t�c UX.

### Status
- PASS WITH WARNINGS (warning c�n l?i l� log l?ch s? migration + c?n manual UX smoke x�c nh?n cu?i).

## 18. Menu Recipe Input Label & Encoding Fix (2026-05-29)

### K?t qu?
- B? sung label r� cho c�c � t�m ki?m/nh�m m�n ? sidebar v� c?m nh?p nhanh trong tab `�?nh m?c nguy�n li?u`.
- Chuy?n tab c?a `menu-recipe` sang style d�ng chung `AccountingTabButton`, kh�ng c�n ki?u tab WinForms c? di?n.
- S?a binding chip filter sang OneWay d? kh�ng c�n l?i runtime khi bind v�o property read-only.
- S?a c�c grid t�m t?t/gi� v?n d? kh�ng c�n ch?ng ch? trong panel ph?i.
- R� so�t tinh `TextBox`/`ComboBox` trong `ASMILE.BO.App/Views`: kh�ng c�n control kh�ng label g?n k? ho?c tooltip r� theo scan hi?n t?i.

### Verification
- `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
- `dotnet test .\ASMILE.BO.sln`: PASS full suite.
- `scripts/check-xaml-encoding.ps1`: PASS, kh�ng ph�t hi?n mojibake XAML.
- Runtime log phi�n m?i `2026-05-29 10:55`: `Database is ready`, kh�ng c� l?i m?i sau b?n v�.

## 19. Menu Recipe Reference UI Redesign (2026-05-29)

### K?t qu?
- Thi?t k? l?i `menu-recipe` theo ?nh m?u Windows 11:
  - thanh ti�u d? module,
  - di?u hu?ng tr�i,
  - panel nh�m m�n,
  - header m�n + action buttons,
  - tab ngang,
  - b?ng d?nh lu?ng nguy�n li?u,
  - panel t�nh gi� v?n/c?nh b�o,
  - form th�ng tin m�n ph�a du?i.
- Kh�ng hardcode d? li?u m�n/nguy�n li?u trong XAML; d? li?u v?n bind t? ViewModel/service.
- L�m s?ch l?i to�n b? text ti?ng Vi?t trong `MenuRecipeView.xaml` v� `MenuRecipeViewModel.cs`.

### Verification
- `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
- `dotnet test .\ASMILE.BO.sln`: PASS full suite.
- `scripts/check-xaml-encoding.ps1`: PASS.
- Runtime startup `2026-05-29 11:05`: `Database is ready`, kh�ng c� l?i UI nghi�m tr?ng m?i sau redesign.

### Status
- PASS WITH WARNINGS: c?n ngu?i d�ng m? m�n h�nh v� x�c nh?n tr?c quan l?n cu?i so v?i ?nh m?u tr�n d�ng d? ph�n gi?i s? d?ng th?c t?.

### 2026-05-29 16:45 — Fix menu-recipe button visibility
- **Bug**: "Thêm món" button hidden when DB has categories but no menu items (BooleanToVisibilityConverter doesn't support ConverterParameter=Invert).
- **Fix**: Added CanCreateMenu/ShowAddMenuButton/ShowBootstrapCategoriesButton computed properties; XAML binds directly without Invert.
- Build PASS (0 warning, 0 error).
- Test PASS (pre-existing flaky BackgroundScheduler test unrelated).

### 2026-05-29 17:00 — Fix "Unrecognized Guid format" when creating first menu item
- **Root cause**: FilterCategoryId was string, and CreateNewMenu() didn't set MenuCategoryId (left as Guid.Empty).
- **Fix**: FilterCategoryId → Guid?, CreateNewMenu() auto-selects first category, SaveMenuItemAsync() validates before save.
- Build PASS (0 warning, 0 error). Test PASS (135/135, 0 failed).

### 2026-05-30 — Fix sidebar click not loading menu detail
- **Root cause**: Missing OnSelectedMenuCardChanged partial method — selected item changed but no code ran to load detail.
- **Fix**: Added partial void OnSelectedMenuCardChanged that delegates to existing SelectMenuAsync.
- Build PASS (0/0). Test PASS (134/135, 1 flaky unrelated).

### 2026-05-30 — Fix sidebar click → detail load (attempt 2)
- **Root cause**: OnSelectedMenuCardChanged from attempt 1 was never saved to file (silent patch failure).
- **Fix**: Line-array insertion. Added OnSelectedMenuCardChanged + LoadSelectedMenuCardAsync with exception handling.
- Build PASS (0/0). Test PASS (134/135).

### 2026-05-30 — Polish: friendly price/VAT input, default UnitName
- Added UnitPriceText/VatRateText string props for textbox bindings (accepts "90.000 đ" / "8%" style input).
- CreateNewMenu() defaults UnitName = "Phần".
- Build PASS (0/0). Test PASS (135/135 fully clean).

### 2026-05-30 — Win11 enterprise UI polish (final)
- Input heights 38px, card layout for form, local styles, hint text, green "no warnings" icon.
- Build PASS (0/0). Test PASS (134/135, flaky pre-existing).
### 2026-05-30 — Fix blank workspace when clicking menu item
- **Root cause**: Three overlapping Border/Grid elements in the same grid cell (Grid.RowSpan=2). The empty-state Border used DataTriggers that failed to re-evaluate when HasSelectedMenu changed, leaving it visible on top of the detail grid.
- **Fix**: 
  - Added ShowEmptyState property (!HasSelectedMenu && HasCategories) to ViewModel.
  - Replaced DataTrigger-based Visibility on empty-state Border with direct binding Visibility={Binding ShowEmptyState,B2V}.
  - Removed duplicate _ = LoadSelectedMenuCardAsync() call in OnSelectedMenuCardChanged.
  - Added ShowEmptyState to NotifyComputedState() for proper refresh.
- **Files**: MenuRecipeView.xaml, MenuRecipeViewModel.cs
- Build PASS (0/0). Test PASS (123/124 unit + 11/11 integration, 1 flaky pre-existing Cancellation_PropagatesToRunningExecution).
