# FULL APP AUDIT REPORT — ASMILE-BO

**Date**: 2026-06-08
**Auditor**: Automated Deep Audit (no code changes)
**Scope**: Full solution — 7 projects, 16 modules, database, runtime

---

## 1. Executive Summary

ASMILE-BO là ERP cho nhà hàng/khách sạn, xây dựng trên .NET 8 WPF + EF Core + SQL Server. Kiến trúc phân lớp rõ ràng (Core → Application → Infrastructure → App). Backend gần hoàn thiện với đầy đủ module kế toán, kho, AR/AP, cash/bank, fixed assets, tax, audit. UI WPF đang trong giai đoạn chuyển đổi — một số module có UI hoàn chỉnh (Inventory, MenuRecipe, Settings), một số chỉ có ViewModel + View cơ bản (Cash/Bank, Tax/Financial, Asset Management), và một số backend mạnh nhưng UI minimal (Reports, Operations Center).

**Kết quả tổng quan**:

| Hạng mục | Kết quả |
|----------|---------|
| Build | **PASS** — 0 errors, 0 warnings |
| Test (Unit + Integration) | **PASS** — 143/143 (100%) |
| Database migration | **PASS** — 47 migrations applied, log sạch 8/6 |
| Data integrity | **PASS** — Journal cân bằng, không orphan FK |
| Runtime startup | **PASS** — App mở không lỗi |
| Mojibake check | **PASS** — Không phát hiện trong XAML |

**Verdict**: **PASS WITH WARNINGS** — App hoạt động ổn định. Các vấn đề tồn tại chủ yếu ở mức Medium (dữ liệu demo chưa đầy đủ, UI chưa tối ưu, một số module thiếu UI chuyên biệt).

---

## 2. Scope

- **Source code**: All 7 projects in `ASMILE.BO.sln`
- **Database**: SQL Server `localhost\ASMILE_BO` (live data)
- **Tests**: 143 tests (132 unit + 11 integration)
- **Documentation**: CLAUDE.md, DEV_PROGRESS.md, KIEMTRAAPP.md, ROADMAP_IMPLEMENTATION.md, UI_AUDIT_REPORT.md, docs/ketqua.md
- **Not modified**: No code, XAML, migration, or configuration changes made

---

## 3. Build Result

```bash
dotnet clean ASMILE.BO.sln && dotnet restore ASMILE.BO.sln && dotnet build ASMILE.BO.sln
```

**Result: PASS**

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Encoding check passed: no mojibake patterns found in XAML.
```

All 7 projects compiled successfully:
- ASMILE.BO.Core
- ASMILE.BO.Shared
- ASMILE.BO.Application
- ASMILE.BO.Infrastructure
- ASMILE.BO.App
- ASMILE.BO.Tests.Unit
- ASMILE.BO.Tests.Integration

---

## 4. Test Result

```bash
dotnet test ASMILE.BO.sln --no-build
```

**Result: ALL PASS**

| Test Project | Passed | Failed | Skipped | Total | Duration |
|---|---|---|---|---|---|
| Unit Tests | 132 | 0 | 0 | 132 | 17s |
| Integration Tests | 11 | 0 | 0 | 11 | 9s |
| **Total** | **143** | **0** | **0** | **143** | **26s** |

Test database: EF Core InMemory (both projects). 16 accounts + 15 role mappings pre-seeded per test.

Unit test coverage by module: Audit, Scheduler, Closing, EInvoice, Permissions, Search, FiscalPeriod, FixedAsset, InventoryCosting, MenuCatalog, MenuRecipe, MultiCompany, Notification, PostingEngine, RetentionArchive, Telemetry, SqlPerformance, ToolAsset, TrainingData, TT200, TT99, VAT, WorkflowResilience.

**Note**: No dedicated tests for BreakfastCafeSales, Settings UI, or Reports.

---

## 5. Database Status

### 5.1 Connection
- **Provider**: SQL Server (localhost)
- **Database**: `ASMILE_BO`
- **Auth**: `sa` / `123456`
- **ORM**: EF Core 8.0 with auto-migration on startup

### 5.2 Migration History
- **47 migrations** applied, from `InitialCreate` (2026-04-21) to `AddBreakfastCafePosTablesAndKitchenTickets` (2026-06-07)
- **0 pending migrations** (snapshot matches database)

### 5.3 Key Table Row Counts

| Table | Rows | Notes |
|-------|------|-------|
| Companies | 2 | Expected 1 per seed data |
| Branches | 1 | OK |
| MenuCategories | 5 | OK (seed: 6 expected) |
| MenuItems | 19 | All active |
| MenuItemRecipes | 7 | OK |
| MenuItemRecipeLines | 22 | No orphans |
| PosTables | 28 | 8 active, 20 inactive |
| BreakfastCafeOrders | 22 | 17 Draft, 5 Cancelled, 0 Paid |
| BreakfastCafeOrderLines | 6 | Low vs 22 orders |
| BreakfastCafePayments | 0 | No payments recorded |
| BreakfastCafeKitchenTickets | 0 | Never sent to kitchen |
| JournalHeaders | 9 | OK |
| Items | 32 | OK |
| Customers | 6 | OK |
| Suppliers | 5 | OK |
| Warehouses | 5 | OK |
| CashFunds | 1 | OK |
| BankAccounts | 2 | OK |

### 5.4 Data Integrity

| Check | Result |
|-------|--------|
| Orphan MenuItems (no category) | 0 — CLEAN |
| Orphan RecipeLines (no recipe) | 0 — CLEAN |
| Zero-price active items | 0 — CLEAN |
| Journal balance (Debit = Credit) | 241,183,100 = 241,183,100 — PERFECT |
| Unbalanced journals (diff > 0.01) | 0 — CLEAN |
| Orders with null Status/ServiceStatus | 15 orders — DATA ISSUE |
| PosTables inactive | 20 — DATA ISSUE |

---

## 6. Module Inventory

| # | Module | Entities | Services | ViewModels | Views | Tests | Rating |
|---|--------|----------|----------|-----------|-------|-------|--------|
| 1 | Dashboard | - | - | 1 | 1 | 0 | Functional |
| 2 | Inventory/Warehouse | 11 | 8+ | 16+ | 16+ | 1 | **Complete** |
| 3 | Accounting (GL) | 4 | 7 | 2 | 2 | 3 | Functional |
| 4 | Tax/Financial Reporting | 5 | 5 | 1 | 1 | 3 | Functional |
| 5 | Cash/Bank | 3 | 4 | 1 | 1 | 1 | Functional |
| 6 | AR/AP | 3 | 5+ | 6 | 6 | 0 | Functional |
| 7 | Asset Management | 2 | 2 | 1 | 1 | 2 | Functional |
| 8 | Training Data | 1 | 1 | 1 | 1 | 3 | **Complete** |
| 9 | Menu Recipe | - | 3 | 1 | 16+ | 2 | Complete |
| 10 | Breakfast Cafe Sales | 1 | 2 | 2 | 2 | 0 | Functional |
| 11 | Settings | 9 | 8 | 4 | 2 | 0 | Complete |
| 12 | Reports | 1 | 1 | 1 | 1 | 0 | **Partial** |
| 13 | Authorization/Security | 10 | 14 | 3 | 4 | 1 | Functional |
| 14 | Notification Center | 1 | 1 | 1 | 1 | 1 | Functional |
| 15 | Operations Center | - | 6 | 1 | 1 | 1 | Partial |
| 16 | Audit Center | 5 | 9 | 1 | 1 | 3 | Functional |

**Totals**: 7 projects, 755+ source files, 157 XAML files, 56 migrations, 26 test files

---

## 7. Cross-module Logic Matrix

```
Training Data → Master Data → Inventory → Menu Recipe → POS → Payment → Accounting → Reports
     ✅             ✅            ✅          ✅         ⚠️      ⚠️         ✅         ⚠️
```

### Luồng 1: Tạo món (COMPLETE)
MenuCategory → MenuItem → Recipe → Ingredient → Warehouse → Cost → Price
- 5 categories, 19 items, 7 recipes, 22 lines — all linked correctly
- No orphan records

### Luồng 2: Bán hàng POS (PARTIAL)
Table → Open order → Add item → Send kitchen → Payment → Close order → Table free
- Table selection and order creation: ✅ Works
- Add menu items to order: ✅ Works
- Send to kitchen/bar: ✅ Code exists, but 0 kitchen tickets in DB
- Payment processing: ✅ Code exists, but 0 payments in DB
- Close order + free table: ✅ Code exists, not tested with real data

### Luồng 3: POS → Kho (NOT IMPLEMENTED)
- `IBreakfastCafeCostingService.ConsumeRecipeAsync` returns `Task.CompletedTask` (skeleton)
- No inventory deduction on order payment or kitchen send
- **Risk**: Bán món nhưng không trừ nguyên liệu → tồn kho ảo

### Luồng 4: POS → Kế toán (PARTIALLY IMPLEMENTED)
- Payment creates journal entry via `IJournalService` + `IPostingRuleService`
- Revenue, VAT output, Cash/Bank accounts: ✅ Code exists
- Room charge / Customer debt: Account mapping exists, not tested
- 0 payments → 0 accounting entries in practice

### Luồng 5: Hotel service (NOT IMPLEMENTED)
- Room/guest entities exist but no workflow
- Service charge (5%) implemented for RoomService orders
- No room posting/payment integration

---

## 8. Real Data Validation

### 8.1 Data Quality Issues

| # | Issue | Detail |
|---|-------|--------|
| D1 | 15 orders with empty Status | OrderType, ServiceStatus also empty → incomplete migration or stale test data |
| D2 | 20 inactive PosTables | Soft-deleted tables (IsActive=0), likely from schema changes |
| D3 | 6 OrderLines for 22 orders | Orders mostly empty — only draft/test data |
| D4 | 0 Payments / 0 KitchenTickets | POS flow never completed end-to-end in dev |
| D5 | 2 Companies | Extra company from migration seed (expected 1) |

### 8.2 Data Integrity (All PASS)

- Journal: 241,183,100 Debit = 241,183,100 Credit — perfectly balanced
- No orphan FKs in MenuItems, RecipeLines
- No zero-price active items
- No unbalanced journals

---

## 9. Runtime/UI Validation

### 9.1 Startup Log (2026-06-08)
```
[09:01:14 INF] Validating and applying SQL Server migrations...
[09:01:16 INF] Database is ready.
[09:03:17 WRN] Compiling a query which loads related collections...
```
- Database ready in 2 seconds
- One EF Core performance warning (MultipleCollectionInclude without QuerySplittingBehavior)

### 9.2 Previous Log Errors (2026-06-07) — NOW RESOLVED
- `HasRecipeManagement` column duplicate in MenuItems migration → **fixed, not present 8/6**
- `InvalidCastException` from StringEqualityConverter in DataTrigger+MultiBinding → **converter updated to implement IValueConverter, not reproducing 8/6**

### 9.3 UI Module Status (from prior smoke test)

| Module | Opens | Data | Errors | Notes |
|--------|-------|------|--------|-------|
| Dashboard | ✅ | ✅ | Clean | Basic KPIs |
| Inventory | ✅ | ✅ | Clean | Full CRUD |
| Accounting | ✅ | ✅ | Clean | Full journal view |
| Tax/Financial | ✅ | ✅ | Clean | Statements render |
| Cash/Bank | ✅ | ✅ | Clean | Basic view |
| AR/AP | ✅ | ✅ | Clean | Full list views |
| Asset Mgmt | ✅ | ✅ | Clean | Basic view |
| Training Data | ✅ | ✅ | Clean | Full onboarding |
| Menu Recipe | ✅ | ✅ | Clean | Full 4-column UI |
| Breakfast Cafe | ✅ | ✅ | ⚠️ | Active chips broken |
| Settings | ✅ | ✅ | Clean | Full CRUD |
| Reports | ✅ | ⚠️ | Clean | Limited templates |
| Security | ✅ | ✅ | Clean | User/role mgmt |
| Notification | ✅ | ✅ | Clean | Task inbox |
| Operations | ✅ | ⚠️ | Clean | Status dashboard |
| Audit | ✅ | ✅ | Clean | Log viewer |

### 9.4 Known UI Issues (from code review, not runtime tested)

| # | Severity | Module | Issue |
|---|----------|--------|-------|
| U1 | High | Breakfast Cafe | DataTrigger+MultiBinding gây `InvalidCastException` — active state chips không hoạt động |
| U2 | Medium | Breakfast Cafe | Tab Cài đặt DataContext kế thừa sai ViewModel — settings view bindings fail |
| U3 | Medium | Breakfast Cafe | `MenuFilters` có logic nhưng không có UI control |
| U4 | Low | Breakfast Cafe | `TableColumnOptions` là dead code |
| U5 | Low | All | 5 XAML files >100KB cần componentization |
| U6 | Low | Settings | Parse OrderType từ display string dùng Replace+Split (fragile) |
| U7 | Info | Navigation | Module catalog có text không dấu ở entry `breakfast-cafe-settings` |

---

## 10. Findings

### CRITICAL (0)
Không có lỗi critical nào gây mất dữ liệu hoặc crash app.

### HIGH (2)

**H1 — POS không tự động trừ kho khi bán**
- Module: Breakfast Cafe Sales
- Impact: Bán món nhưng không trừ nguyên liệu → tồn kho ảo, giá vốn sai
- Root cause: `IBreakfastCafeCostingService.ConsumeRecipeAsync` là skeleton (`Task.CompletedTask`)
- Files: `BreakfastCafeCostingService.cs`
- Options:
  - A: Trừ kho khi gửi bếp/bar (real-time inventory)
  - B: Trừ kho khi thanh toán (deferred inventory)
  - C: Tạo phiếu xuất kho riêng, kế toán/kho duyệt (approval workflow)
- **Recommend**: B cho phase 1 (đơn giản, ít race condition)

**H2 — 20 PosTables inactive gây thiếu bàn**
- Module: Breakfast Cafe Sales
- Impact: UI chỉ hiển thị 8 bàn, thiếu bàn cho Sân vườn, Mang đi, Khách sạn
- Root cause: PosTables bị soft-delete (IsActive=0) — có thể từ migration hoặc reset training data
- Files: `BreakfastCafeSalesService.cs:EnsurePosDefaultsAsync`
- Options:
  - A: SQL UPDATE set IsActive=1 cho 20 bàn inactive
  - B: Chạy TrainingData reset với profile RestaurantHotelFullDemo
  - C: Sửa EnsurePosDefaultsAsync để check và reactive thay vì chỉ insert
- **Recommend**: A (nhanh nhất) + C (phòng ngừa lâu dài)

### MEDIUM (5)

**M1 — DataTrigger + MultiBinding InvalidCastException**
- Active state chips cho khu vực và phương thức thanh toán bị hỏng
- File: `BreakfastCafeSalesView.xaml:553-566, 1249-1263`
- Fix: Đổi MultiBinding trong DataTrigger sang dùng wrapper class với IsSelected property (như PosCategoryFilter)

**M2 — Tab Cài đặt sai DataContext**
- Settings view bind tới `BreakfastCafeSettingsViewModel` nhưng nhận `BreakfastCafeSalesViewModel` từ parent
- File: `BreakfastCafeSalesView.xaml:986-988`
- Fix: Thêm `DataContext="{Binding SettingsContext}"`

**M3 — MenuFilters không có UI**
- Backend có filter "Có định lượng"/"Chưa định lượng" nhưng không có control
- File: `BreakfastCafeSalesViewModel.cs:22`
- Fix: Thêm chip filter hoặc dropdown trong tab Thực đơn

**M4 — 15 đơn hàng có Status/ServiceStatus/OrderType rỗng**
- Dữ liệu không hoàn chỉnh — có thể từ migration cũ hoặc test data không cleanup
- SQL: `SELECT * FROM BreakfastCafeOrders WHERE Status = '' OR Status IS NULL`

**M5 — Reports module chưa wire to IReportEngine**
- ReportCenterView còn dùng hard-coded PrintPreviewDialog
- `IReportEngine` có đầy đủ API nhưng chưa được gọi từ UI

### LOW (4)

**L1 — TableColumnOptions dead code**
**L2 — Parse OrderType bằng Replace+Split (fragile)**
**L3 — 5 XAML files >100KB (cần componentization)**
**L4 — Module catalog text không dấu tại NavigationService.cs:74**

---

## 11. Recommended Implementation Roadmap

### Phase 1 — Stabilization (1-2 days)
- [ ] Fix H2: Reactivate 20 PosTables
- [ ] Fix M1: Sửa DataTrigger MultiBinding → wrapper class
- [ ] Fix M2: Tab Cài đặt DataContext
- [ ] Fix M4: Clean up 15 incomplete orders

### Phase 2 — POS/Menu/Inventory Integration (3-5 days)
- [ ] Fix H1: Implement ConsumeRecipeAsync (auto-deduct on payment)
- [ ] Fix M3: Add MenuFilters UI
- [ ] Add POS → Accounting flow testing with real payments
- [ ] Kitchen ticket printing workflow

### Phase 3 — Hotel Service (3-5 days)
- [ ] Room/guest entity workflow
- [ ] Room charge payment flow
- [ ] Service charge integration

### Phase 4 — Reports & UX (5-7 days)
- [ ] Wire ReportCenter to IReportEngine
- [ ] Componentize 5 oversized XAML files
- [ ] Style consolidation (merge parallel style systems)
- [ ] POS printer/QR payment integration

### Phase 5 — Production Hardening (ongoing)
- [ ] Performance: Fix EF Core MultipleCollectionInclude warnings
- [ ] Security: Move connection string to secrets/DPAPI
- [ ] Monitoring: Add health check endpoints
- [ ] Backup: Auto-backup before migration

---

## 12. SQL Queries Used

```sql
-- Table row counts
SELECT 'TableName' AS Tbl, COUNT(*) AS Cnt FROM TableName ...

-- Migration history
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;

-- Order status breakdown
SELECT Status, COUNT(*) FROM BreakfastCafeOrders GROUP BY Status;
SELECT PaymentStatus, COUNT(*) FROM BreakfastCafeOrders GROUP BY PaymentStatus;

-- Orphan checks
SELECT COUNT(*) FROM MenuItems WHERE MenuCategoryId NOT IN (SELECT Id FROM MenuCategories);
SELECT COUNT(*) FROM MenuItemRecipeLines WHERE MenuItemRecipeId NOT IN (SELECT Id FROM MenuItemRecipes);

-- Zero price check
SELECT COUNT(*) FROM MenuItems WHERE UnitPrice = 0 AND IsActive = 1;

-- Journal balance
SELECT SUM(DebitAmount), SUM(CreditAmount), SUM(DebitAmount) - SUM(CreditAmount) FROM JournalLines;

-- Unbalanced journal check
SELECT jh.JournalNo, SUM(jl.DebitAmount), SUM(jl.CreditAmount)
FROM JournalHeaders jh JOIN JournalLines jl ON jl.JournalHeaderId = jh.Id
GROUP BY jh.Id, jh.JournalNo
HAVING ABS(SUM(jl.DebitAmount) - SUM(jl.CreditAmount)) > 0.01;

-- PosTable status breakdown
SELECT Status, IsActive, COUNT(*) FROM PosTables GROUP BY Status, IsActive;
```

---

## 13. Commands Used

```bash
# Build
dotnet clean ASMILE.BO.sln
dotnet restore ASMILE.BO.sln
dotnet build ASMILE.BO.sln

# Test
dotnet test ASMILE.BO.sln --logger "trx;LogFileName=full-app-audit.trx" --no-build

# Database (via sqlcmd)
sqlcmd -S localhost -U sa -P 123456 -d ASMILE_BO -Q "..."

# App
taskkill /F /IM ASMILE.BO.App.exe
start ASMILE.BO.App/bin/Debug/net8.0-windows/ASMILE.BO.App.exe
```

---

## 14. Remaining Unknowns

1. **Performance under load**: Chưa test với >1000 orders hoặc >100 bàn
2. **Multi-branch**: App hỗ trợ multi-company/branch nhưng chưa test thực tế với nhiều chi nhánh
3. **Concurrency**: Chưa test 2 POS terminal cùng thao tác 1 bàn
4. **Network resilience**: Chưa test mất kết nối SQL Server mid-transaction
5. **E-Invoice integration**: Backend có nhưng chưa test với provider thật
6. **Backup/restore**: Mock service — chưa test backup thật
7. **Training data safety gates**: Cần test reset data có guard đúng không (AllowDestructiveReset=false)
