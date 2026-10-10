# UI_AUDIT_REPORT - ASMILE BO

Ngay lap: 2026-05-21
Pham vi: Audit UI/UX WPF hien co, khong redesign, khong rewrite, khong sua code nghiep vu.

## A. Tong quan UI hien tai

### Muc do hoan thien

UI hien tai dat muc **Partial / Enterprise shell dang hinh thanh**. Cac man hinh loi nhu So cai, Quan ly xuat nhap ton, Cong no phai thu/phai tra, danh muc khach hang/nha cung cap/hang hoa/kho da co giao dien WPF kha day du, co style Windows 11, card, DataGrid, pager va nhieu binding MVVM.

Tuy nhien backend core hien da di xa hon UI. Cac engine moi nhu CashFund, Banking, FixedAsset, ToolAsset, VAT, FinancialStatement, Closing, ReportEngine, ImportEngine da co service/DI nhung chua duoc expose thanh man hinh nghiep vu hoan chinh trong desktop shell. Mot so man van dung report card hard-code, print preview cu, mock settings service hoac fallback legacy.

### Diem manh

- Desktop shell da co module catalog, window manager, toast/notification va permission guard co ban.
- Style WPF da co nhieu resource dung chung: `FluentButtons`, `FluentCards`, `FluentPagination`, `AccountingDataGrid`, `SettingsStyles`.
- Cac man core ke toan/kho/cong no da co tab, KPI/card, filter, pager, detail panel va action button.
- So cai da wire qua `IGeneralLedgerService`, backend da co Journal persisted.
- Phieu nhap/phieu xuat da wire `IInventoryAppService`, co workflow tao/duyet/xoa, date filter, lookup, pager.
- Danh muc khach hang/nha cung cap/hang hoa/kho/nhan su da co control tach rieng, form/detail card va pager dung chung.
- Settings da co navigation noi bo, app settings, cai dat so cai, cai dat tai khoan va import/export tai khoan.

### Diem yeu

- Nhieu backend module moi chua co UI chuyen dung: Quy, Ngan hang, TSCD, CCDC, Thue, Bao cao tai chinh, Closing.
- Report UI chua dung `IReportEngine` lam nguon chinh; van co print preview/report card hard-code.
- Mot so settings/system services dang la mock: system settings, database admin, backup, service status, security settings, printer settings.
- View/XAML lon qua muc: `ReceivableView.xaml`, `GeneralLedgerView.xaml`, `InventoryMovementView.xaml`, `PayableView.xaml`, `SettingsView.xaml`.
- ViewModel lon va nhieu logic UI/nghiep vu tron nhau: `SettingsViewModel.cs`, `VirtualExplorerViewModel.cs`, `PaymentVouchersViewModel.cs`, `StockReceiptsViewModel.cs`, `StockIssuesViewModel.cs`.
- Van con dau vet mock/sample/mojibake trong source va dialog.
- Pagination, DataGrid, dialog, button, icon khong dong nhat 100% giua tat ca man.

## B. Module audit chi tiet

| Module | Status | Screens/File chinh | Da hoan chinh | Chua hoan chinh | UX/Technical issues | Priority |
|---|---|---|---|---|---|---|
| Tong quan | Partial | `DashboardView.xaml`, `DashboardViewModel.cs` | Co dashboard shell, co module icon, thong tin nhanh | Chua thay wire day du AR/AP/Cash/Bank/VAT/financial KPI; widget config con han che | Dashboard moi doc mot phan service, thieu drilldown va realtime refresh | High |
| So cai | Partial / Strong | `GeneralLedgerView.xaml`, `GeneralLedgerViewModel.cs`, `EfGeneralLedgerService` | Da wire service, doc Journal persisted, co tab but toan/can doi/phat sinh/bao cao/nhat ky, co async load | Report/export chua tap trung qua `IReportEngine`; reversal/source drilldown UI chua ro; `UnpostedVoucherCount => 0` la placeholder | XAML lon, workflow ke toan con nhieu thao tac chua role-aware | High |
| But toan/Journal Entry | Partial | `LedgerEntryDialog.xaml`, `GeneralLedgerViewModel.cs` | Co dialog tao but toan, service co create manual journal | Chua thay UI day du cho draft/post/reverse/source trace; validation UI chua enterprise-level | Dialog can keyboard-first, validate inline, line editor tot hon | High |
| Trial Balance / Kiem tra can doi | Partial | `GeneralLedgerView.xaml`, `EfGeneralLedgerService` | Co tab can doi va kiem tra can doi | Opening balance/snapshot/financial statement chua expose day du; chua co drilldown tu so du ve journal | DataGrid can column sizing/virtualization va export theo report engine | High |
| Kho / Nhap xuat ton | Partial / Strong | `InventoryMovementView.xaml`, `InventoryMovementViewModel.cs`, `StockReceiptsViewModel.cs`, `StockIssuesViewModel.cs` | Co dashboard kho, the kho/phat sinh, nhap-xuat theo chung tu, canh bao ton, kiem ke/dieu chinh, bao cao; phieu nhap/xuat wire service | `DataMode` van hien `Du lieu mau / In-memory`; report cards hard-code; costing detail chua expose ro; mot so report/print van dung preview cu | `InventoryMovementView.xaml` rat lon; data table paging/virtualization can ra soat voi data lon | High |
| Phieu nhap | Partial | `StockReceiptsView.xaml`, `StockReceiptsViewModel.cs` | Wire `IInventoryAppService`, co tao/sua/xoa/duyet, filter ngay, lookup hang/NCC/nhan vien, pager | Chua thay UI hien JournalHeaderId/posting status/costing status; approval/posting error state can ro hon | `_ = LoadAsync()` fire-and-forget; can cancellation/loading guard | High |
| Phieu xuat | Partial | `StockIssuesView.xaml`, `StockIssuesViewModel.cs` | Wire service, co tao/sua/xoa/duyet, lookup ton, pager | Chua hien ro gia von tu Costing Engine va posting journal; chua co workflow reverse/cancel enterprise | Fire-and-forget async, can validate ton/gia von inline | High |
| Cong no phai thu | Partial | `ReceivableView.xaml`, `ReceivableViewModel.cs`, `EfReceivableService` | Co dashboard, tab khach hang con no, chi tiet phat sinh, thu tien, doi chieu, tuoi no/bao cao | Persisted AR co backend nhung UI can xac nhan da dung lam nguon chinh het; payment allocation/drilldown chua ro | XAML rat lon; tung bi loi layout/tab/paging; can regression suite UI | High |
| Cong no phai tra | Partial | `PayableView.xaml`, `PayableViewModel.cs`, `EfPayableService` | Co dashboard, NCC con no, thanh toan/tam ung, doi chieu, tuoi no, bao cao | Reconciliation/report cards van co hard-code; AR/AP persisted can expose status/sync; payment allocation chua day du | XAML rat lon; event collection changed trong code-behind can cleanup | High |
| Phieu thu | Partial | `PaymentReceiptsView.xaml`, `PaymentReceiptsViewModel.cs` | Co tao phieu/duyet/da duyet, date filter, pager, goi receivable service tao phieu thu | Chua thay wire CashFund/Banking/Journal persisted ro rang; mot so UI tieng Viet/pagination tung loi | Workflow phe duyet con local-ish; can backend status va posting status | High |
| Phieu chi | Partial | `PaymentVouchersView.xaml`, `PaymentVouchersViewModel.cs` | Co tao phieu/duyet/cong no, goi payable service, pager | UI tung loi encoding/pagination/chan nut; CashFund/Banking/Journal status chua expose day du | Nhieu logic filter/tab/approve trong VM; can service workflow ro hon | High |
| Quy tien mat | Needs wiring | Backend `ICashFundService`, `CashFundService`; chua thay screen module rieng | Backend da co module persisted theo roadmap | Chua co desktop module/catalog/screen: danh muc quy, so quy, kiem ke quy, bao cao quy | UI chua ton tai nen user khong thao tac duoc | Critical |
| Ngan hang | Needs wiring | Backend `IBankingService`, `BankingService`; chua thay screen module rieng | Backend co bank account/transaction/statement/reconcile | Chua co UI tai khoan ngan hang, sao ke CSV, doi chieu ngan hang, so phu | Banking log co dau vet mojibake trong source | Critical |
| TSCD | Needs wiring | Backend `IFixedAssetService`, `FixedAssetService`; chua thay screen module rieng | Backend co fixed asset/depreciation | Chua co UI danh muc TSCD, khau hao, thanh ly, lich khau hao | Chua expose workflow va report | Critical |
| CCDC | Needs wiring | Backend `IToolAssetService`, `ToolAssetService`; chua thay screen module rieng | Backend co tool asset/allocation | Chua co UI xuat dung, phan bo, thu hoi, thanh ly | Chua expose lich phan bo/report | Critical |
| Thue/VAT | Needs wiring | Backend `ITaxService`, `TaxService`; chua thay screen module rieng | Backend co tax code/VAT transaction/declaration | Chua co UI ma thue, bang ke VAT, to khai nhap | Chua expose warning invoice/thue | Critical |
| Bao cao | Needs wiring | `ReportEngine`, report cards trong VM, `PrintPreviewDialog` | Backend `IReportEngine` co Execute/Export; mot so man co print preview/export cu | UI Report Center chua tap trung vao `IReportEngine`; `stock-report`, `debt-report` trong catalog khong co VM that | Report cards hard-code; PDF/Excel flow chua dong nhat | Critical |
| Cai dat he thong | Partial | `SettingsView.xaml`, `SettingsViewModel.cs` | Co sidebar/settings shell, app list, cai dat so cai/tai khoan, import/export tai khoan | Nhieu service system la mock; route/breadcrumb logic phuc tap; generic TODO setting rows | `SettingsViewModel.cs` qua lon, de gay regression khi sua | High |
| Import Excel | Partial | `SupplierImportPreviewDialog.xaml`, `ImportEngine`, `SettingsViewModel` account import | Backend engine co session/row/error/export; supplier preview/account import co UI | Chua thay wire chung cho tat ca module; error grid/chinh sua inline chua dong nhat | Can import center va handler UI dung chung | High |
| Khach hang | Partial / Usable | `CustomerPage.xaml`, `CustomerPageViewModel.cs`, controls customer | Co list/detail/form, search, pager, service | Validation/duplicate/import/role-aware action can bo sung; encoding tung co loi tren table | UI kha tot nhung can enterprise grid polish | Medium |
| Nha cung cap | Partial / Usable | `SupplierPage.xaml`, `SupplierPageViewModel.cs`, supplier controls | Co list/detail/form, import preview, pager | Can lien ket AP persisted, debt drilldown, validation doanh nghiep | Nhieu control rieng, style can dong bo voi customer | Medium |
| Hang hoa | Partial / Usable | `ItemsView.xaml`, `ItemsViewModel.cs`, item controls | Co list/detail/form, search, pager | Can expose costing/stock status, import engine, duplicate/barcode/lots | Table status text tung loi encoding; can validation inline | Medium |
| Kho | Partial / Usable | `WarehousesView.xaml`, `WarehousesViewModel.cs` | Co danh muc kho | Chua thay permission theo kho, stock reconciliation UI, transfer workflow day du | Can role-aware UI va large dataset paging | Medium |
| Bo phan & Nhan su | Partial | `DepartmentPeoplePage.xaml`, `DepartmentPeoplePageViewModel.cs` | Co tree/form/list | Chua ro workflow duyet/phan quyen bo phan, approval approver config | Can keyboard/role consistency | Medium |
| Virtual File Center | Partial | `VirtualExplorerView.xaml`, `VirtualExplorerViewModel.cs` | UI giong explorer, preview, file/document workflow | Mot so label English; co `.Result` async risk; document preview/export con can audit | `VirtualExplorerViewModel.cs` rat lon, nguy co UI thread blocking | High |
| Bao mat/phan quyen | Partial | `DesktopShellViewModel.cs`, security store/current user | Co login/security seed, permission guard module | `MockCurrentUserService`, broad permission mapping, chua role-aware actions/fields | Permission UI va audit action-level can hoan thien | High |

## C. Shared UI problems

### 1. Module backend moi chua co UI

Cac service da dang ky DI nhung chua co module screen/catalog tuong ung:

- `ICashFundService` / `CashFundService`
- `IBankingService` / `BankingService`
- `IFixedAssetService` / `FixedAssetService`
- `IToolAssetService` / `ToolAssetService`
- `ITaxService` / `TaxService`
- `IFinancialStatementService`, `IClosingService`

`DesktopShellViewModel.ResolveModuleViewModel` hien chi map cac module cu nhu dashboard, inventory, receivable, payable, general-ledger, items, customers, suppliers, warehouses, receipts/issues, settings. Cac module moi khong co man hinh rieng.

### 2. Report UI chua tap trung

`IReportEngine` da co trong Application/Infrastructure nhung UI hien van dung:

- `PrintPreviewDialog` trong So cai, Kho, Cong no.
- `ReportCards` hard-code trong `InventoryMovementViewModel` va `PayableViewModel`.
- `stock-report`, `debt-report`, `authorization` nam trong catalog nhung chua map ViewModel that, co nguy co mo placeholder.

### 3. Style va button consistency chua dong nhat

Project co nhieu style resource song song:

- `Buttons.xaml`, `FluentButtons.xaml`
- `DataGrid.xaml`, `AccountingDataGrid.xaml`, `FluentPreviewGrid.xaml`
- `Cards.xaml`, `FluentCards.xaml`, `ModuleModern.xaml`

Ket qua la mot so man da Win11, mot so dialog/button van trong nhu form cu hoac custom rieng. Can quy ve 1 bo style button/grid/pager/dialog cho tat ca module nghiep vu.

### 4. Dialog consistency

Cac dialog dang co nhieu dang:

- `LedgerEntryDialog`
- `PrintPreviewDialog`
- `ReconciliationDetailDialog`
- `PayablePaymentDialog`
- `ReceivablePaymentDialog`
- `InventoryAdjustmentDialog`
- `SettingsConfirmDialog`

`ReconciliationDetailDialog.xaml.cs` co `BuildMockRows()`. `PrintPreviewDialog.xaml.cs` con footer `In-memory sample mode`. Nen tach dialog style va data source ro.

### 5. DataGrid consistency

Da co `TablePagerControl` va `TablePagerViewModel` dung chung, nhung khong phai tat ca bang deu dung cung mot flow. Mot so man co custom pagination/toolbar. Virtualization chi thay ro o mot so dialog import; cac bang lon nhu cong no/so cai/kho can audit lai `EnableRowVirtualization`, server-side paging va `AsNoTracking`.

### 6. Encoding/mojibake

Da tung sua nhieu loi font, nhung audit van thay dau vet mojibake trong source, vi du `BankingService.cs` co chuoi log bi loi ma hoa. Mot so man phieu thu/phieu chi/hang hoa tren anh truoc do tung hien loi tieng Viet. Can co scan CI cho UTF-8/mojibake.

## D. Performance risks

| Risk | Evidence | Impact | Priority |
|---|---|---|---|
| View/XAML qua lon | `ReceivableView.xaml` ~158KB, `GeneralLedgerView.xaml` ~125KB, `InventoryMovementView.xaml` ~119KB, `PayableView.xaml` ~118KB, `SettingsView.xaml` ~105KB | Kho sua, de regression, load/render cham | High |
| ViewModel qua lon | `SettingsViewModel.cs` ~130KB, `VirtualExplorerViewModel.cs` ~98KB | Logic tap trung, kho test, risk memory/event leak | High |
| Async blocking | `VirtualExplorerViewModel.cs` dung `.Result` voi task | Co the block UI thread/deadlock | Critical |
| Fire-and-forget load | `StockReceiptsViewModel`, `StockIssuesViewModel` goi `_ = LoadAsync()` | Race condition, exception kho quan sat, stale selection | High |
| Large query/list | Mot so VM set source to observable list sau filter client-side | Dataset lon co the lag UI | High |
| Event subscription cleanup | Code-behind/VM co CollectionChanged/timer/event subscriptions | Leak neu window/module lifetime dai | Medium |
| Report export phan tan | Print/export goi rieng tung VM | Kho toi uu, kho audit, co the load het data | Medium |

## E. Recommended improvements

### Critical

- Tao UI module rieng cho Quy, Ngan hang, TSCD, CCDC, Thue, Bao cao tai chinh/Closing de expose backend da co.
- Wire Report Center vao `IReportEngine`, thay report cards hard-code bang template/report definition.
- Sua async blocking trong `VirtualExplorerViewModel` va chuan hoa load async co cancellation/error state.
- Thay mock system services hoac danh dau ro man nao la mock de khong nham voi chuc nang that.
- Them regression checklist cho mojibake UTF-8 va cac man tung loi font.

### High

- Tach cac View/XAML lon thanh component: header, filter, tabs, grid, right panel, action bar.
- Chuan hoa DataGrid enterprise: one grid style, column divider, row height, selection color, virtualization, pager.
- Chuan hoa pagination dung `TablePagerControl` cho tat ca bang nghiep vu.
- Bo sung posting status/journal drilldown tren phieu nhap/xuat, phieu thu/chi, cong no.
- Bo sung role-aware actions: an/disable nut theo permission va trang thai ky/posted/locked.
- Wire Import Engine chung vao UI import hang hoa, khach hang, NCC, kho, tai khoan.

### Medium

- Chuan hoa dialog/modal: confirm, edit form, print preview, import preview.
- Bo sung keyboard shortcuts cho power-user: Ctrl+N, Ctrl+S, F5, Enter/Esc, search focus.
- Cai thien empty/loading/error states tren tat ca module.
- Chuan hoa icon set va mau nut theo nghiep vu.
- Them drilldown tu KPI/card/report ve danh sach chi tiet.

### Low

- Them microcopy/hint cho filter phuc tap.
- Them column chooser, saved layout, density setting cho DataGrid.
- Them compact mode cho man hinh nho.

## F. UI roadmap de xuat

### Phase 1 - Critical fixes

- Fix `.Result`/fire-and-forget async nguy hiem.
- Scan va sua mojibake con sot lai.
- Dam bao build/pass va khong co crash khi mo cac module catalog hien co.
- Neu module catalog co key chua map ViewModel, hien thong bao ro hoac map den UI that.
- Chuan hoa pager/control cho cac bang dang loi phan trang.

### Phase 2 - Workflow completion

- Them UI chuyen dung cho: Quy, Ngan hang, TSCD, CCDC, Thue, Closing/Financial Statements.
- Wire phieu thu/chi vao CashFund/Banking/Journal persisted.
- Wire bao cao vao `IReportEngine`.
- Them posting status, reverse, source drilldown, period lock indicators.
- Wire Import Engine chung vao cac man import chinh.

### Phase 3 - Enterprise UX polish

- Tach cac man lon thanh reusable components.
- Chuan hoa toolbar/action bar/dialog/button/icon/grid.
- Them keyboard-heavy workflow va batch actions.
- Them column chooser, saved filters, saved layouts.
- Cai thien empty/loading/error/validation inline.

### Phase 4 - Performance optimization

- Server-side paging cho bang lon.
- Virtualization mac dinh cho DataGrid lon.
- `AsNoTracking` cho query list/report.
- CancellationToken cho tat ca load/filter/report/export.
- Background report/export/import jobs neu dataset lon.

## G. Enterprise readiness

| Tieu chi | Danh gia | Ghi chu |
|---|---|---|
| Multi-tab workflow | Partial | Shell co multi-window/module, nhung workflow phieu/report chua day du |
| Keyboard-heavy workflow | Weak | Chua thay shortcut system thong nhat |
| Excel-like behavior | Partial | Co DataGrid va import preview, nhung chua co copy/paste/edit grid dong nhat |
| Batch actions | Weak | Mot so action co, nhung chua enterprise-wide |
| Large dataset usability | Partial | Pager co san nhung khong dong nhat; virtualization can audit |
| Role-aware UI | Partial | Module-level permission co, action-level con thieu |
| Report Center | Weak | Backend co engine, UI chua tap trung |
| Audit visibility | Weak | Backend audit co, UI audit trail chua duoc expose day du |

## H. Man hinh can kiem tra dac biet

### Journal Entry / So cai

- Da co tao but toan qua dialog va service.
- Can bo sung UI reverse journal, xem nguon chung tu, journal status, period lock indicator.
- Export/report nen chuyen sang ReportEngine.

### Trial Balance

- Da co tab can doi phat sinh/kiem tra can doi.
- Can dam bao doc Journal persisted 100%, khong double count legacy.
- Can drilldown tu account balance ve journal lines.

### Inventory reports

- `InventoryMovementViewModel.ReportCards` hard-code.
- `GenerateReportAsync` dung `PrintPreviewDialog` thay vi `IReportEngine`.
- Can chuyen sang report templates: Inventory Summary, Movement, Stock Card, Costing.

### AR/AP aging

- UI cong no co tuoi no, doi chieu, report.
- Can xac nhan persisted AR/AP la nguon chinh, fallback legacy chi khi chua co data.
- Can expose payment allocation va reconciliation status.

### Dashboard

- Chua thay du KPI tu Journal/AR/AP/Cash/Bank/Inventory/VAT.
- Can widget settings va drilldown.

### Report Center

- Hien thieu UI tap trung duoc wire `IReportEngine`.
- Nen uu tien lam report center truoc khi polish tung report.

### Import Excel

- Engine moi co, nhung UI import chua dong nhat.
- Can import center: preview, validate, error grid, export errors, commit.

### Settings

- Man settings co nhieu chuc nang, nhung service system con mock va VM qua lon.
- Can tach settings pages hoac component noi bo, giu route/breadcrumb on dinh.

## I. Danh sach file/class quan trong da audit

| File | Vai tro | Nhan xet |
|---|---|---|
| `ASMILE.BO.App/App.xaml.cs` | Startup/DI/global exception | Co Serilog, migration init, global error dialog chung |
| `ASMILE.BO.Infrastructure/DependencyInjection.cs` | DI backend | Core services that da dang ky; system settings con mock |
| `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs` | Module catalog/navigation | Backend module moi chua duoc map UI; mot so catalog key co the placeholder |
| `ASMILE.BO.App/Services/AppNavigationService.cs` | Navigation event | Route model don gian, can guard/dirty state tot hon |
| `ASMILE.BO.App/ViewModels/GeneralLedgerViewModel.cs` | UI So cai | Service-wired, nhung report/export chua ReportEngine |
| `ASMILE.BO.App/Views/GeneralLedgerView.xaml` | UI So cai | Lon, nhieu tab, can tach component |
| `ASMILE.BO.App/ViewModels/InventoryMovementViewModel.cs` | UI Nhap xuat ton | Co data mode sample/hard-coded report cards |
| `ASMILE.BO.App/Views/InventoryMovementView.xaml` | UI Kho | Lon, nhieu tab, can tach component |
| `ASMILE.BO.App/ViewModels/PayableViewModel.cs` | UI AP | Co report cards hard-code, reconciliation logic lon |
| `ASMILE.BO.App/Views/PayableView.xaml` | UI AP | Lon, tung co nhieu regression layout |
| `ASMILE.BO.App/ViewModels/ReceivableViewModel.cs` | UI AR | Service-wired, nhung XAML lon can refactor |
| `ASMILE.BO.App/Views/ReceivableView.xaml` | UI AR | File XAML lon nhat, rui ro regression cao |
| `ASMILE.BO.App/ViewModels/SettingsViewModel.cs` | Settings | Qua lon, gom nhieu route/form/import; co TODO generic |
| `ASMILE.BO.App/Views/SettingsView.xaml` | Settings UI | Can giu layout nhung tach noi dung de de bao tri |
| `ASMILE.BO.App/ViewModels/VirtualExplorerViewModel.cs` | VFC | Qua lon, co `.Result` async risk |
| `ASMILE.BO.App/Controls/TablePagerControl.xaml` | Pager dung chung | Nen ap dung cho moi bang co pagination |
| `ASMILE.BO.App/ViewModels/TablePagerViewModel.cs` | Pager logic | Co page size options 15/20/50/100 |
| `ASMILE.BO.App/Themes/FluentPagination.xaml` | Style pagination | Nguon chuan de dong bo cac pager |
| `ASMILE.BO.App/Views/Dialogs/ReconciliationDetailDialog.xaml.cs` | Dialog doi chieu | Co mock rows |
| `ASMILE.BO.App/Views/Dialogs/PrintPreviewDialog.xaml.cs` | Print preview | Con text `In-memory sample mode` |

## J. Ket luan ngan

- UI hien tai khong can rewrite toan bo; nen tiep tuc theo huong **wire backend that + chuan hoa shared controls + tach man lon**.
- Rui ro lon nhat la **backend da co nhung UI chua expose**, khong phai thieu engine.
- Can uu tien Report Center, module Cash/Bank/TSCD/CCDC/VAT/Closing, va pager/grid consistency truoc khi polish nho.
- Moi thay doi UI tiep theo nen di theo module nho, build/test sau tung module, tranh sua dong loat XAML lon.
