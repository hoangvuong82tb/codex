# KET QUA KHAC PHUC ALL MODULE NAVIGATION REGRESSION - ASMILE-BO

## 1. Executive Summary

Trang thai: **CHUA HOAN THANH**.

Da xac dinh va sua nguyen nhan ky thuat lam nhieu module khong mo duoc: `NavigationService` resolve ViewModel tu DI root provider trong khi ViewModel hoac dependency cua no la scoped. Sua theo scope cua tung cua so module, scope duoc dispose sau khi cua so dong.

Chua dat trang thai HOAN THANH vi khong the thuc hien ma tran 10 module live tren database an toan: cau hinh runtime tu khai bao `Environment=Production`, database `ASMILE_BO`. Khong click vao instance nay de tuan thu yeu cau khong dung production database.

## 2. User-visible Symptom

Khi click module, UI hien thong bao chung: "Thao tac khong thanh cong. Vui long kiem tra nhat ky he thong."

## 3. Reproduction

Evidence da duoc giu nguyen trong:

- `ASMILE.BO.App/bin/Release/net8.0-windows/logs/asmile-bo-20260718.log`
- `ASMILE.BO.App/bin/Release/net8.0-windows/logs/ui-exceptions.log`

Runtime Release dang chay luc dieu tra. Log ghi nhan loi khi mo `restaurant-banquet`, `audit-center`, va `tax-financial`.

## 4. Actual Exception

Exception thuc te:

`System.InvalidOperationException: Cannot resolve scoped service 'ASMILE.BO.App.ViewModels.BanquetRestaurantViewModel' from root provider.`

Va:

`Cannot resolve 'ASMILE.BO.App.ViewModels.AuditCenterViewModel' from root provider because it requires scoped service 'ASMILE.BO.Application.Interfaces.IAuditService'.`

`Cannot resolve 'ASMILE.BO.App.ViewModels.TaxFinancialReportingViewModel' from root provider because it requires scoped service 'ASMILE.BO.Application.Interfaces.ITaxService'.`

## 5. Stack Trace Summary

Chuoi stack trace chung:

`NavigationService.ResolveModuleViewModel` -> `NavigationService.OpenTargetAsync` -> `DesktopShellViewModel.OpenModuleByKeyAsync` -> `DesktopShellViewModel.DesktopIconClick`.

Log chi ro `ResolveModuleViewModel` o `NavigationService.cs` da goi `GetRequiredService<T>` tren root provider. DI voi `ValidateScopes=true` dung cach da chan loi nay.

## 6. Root Cause

Root cause da duoc chung minh bang exception thuc te, khong phai suy doan:

- File: `ASMILE.BO.App/Services/NavigationService.cs`.
- Method cu: `ResolveModuleViewModel`.
- Loi: dung `_serviceProvider.GetRequiredService<T>()`; `_serviceProvider` la root provider cua singleton `NavigationService`.
- He qua: bat ky ViewModel nao la scoped, hoac can service scoped, deu fail ngay khi mo module.

Thay doi Front Office gan day lam `BanquetRestaurantViewModel` tro thanh scoped ro rang hon va `ValidateScopes` phat hien loi da co san trong navigation lifecycle. Property gate khong ap dung cho route khac; no khong phai root cause.

## 7. Why All Modules Were Affected

Tat ca route module deu dung cung `ResolveModuleViewModel`. Nhieu ViewModel transient van phu thuoc service scoped nhu `IAuditService`, `ITaxService`, DbContext-backed service, hoac ban than ViewModel scoped. Vi root provider duoc dung chung, moi route co dependency scoped deu bi chan cung mot kieu exception.

## 8. Files Investigated

- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/Services/WindowManagerService.cs`
- `ASMILE.BO.App/Models/AppWindowViewModel.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
- `ASMILE.BO.App/Services/FrontOfficePropertyContextHost.cs`
- `ASMILE.BO.App/App.xaml.cs`
- Release log va UI exception log ngay 2026-07-18.

## 9. Files Modified

- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/Models/AppWindowViewModel.cs`
- `ASMILE.BO.App/Services/WindowManagerService.cs`
- `ASMILE.BO.Tests.Unit/NavigationScopeRegressionTests.cs`
- File report nay.

Khong tao migration. Khong sua business logic cua module. Khong sua Property host hay registration cua Infrastructure.

## 10. Minimal Fix

`NavigationService` khong con giu hay dung root `IServiceProvider` de resolve module ViewModel.

Khi mo module da biet, navigation:

1. Tao `IServiceScope`.
2. Resolve ViewModel tu `moduleScope.ServiceProvider`.
3. Gan scope vao window vua tao.
4. Dispose ViewModel truoc, sau do dispose scope khi `WindowManagerService.Close` dong window.

Neu resolve that bai, scope vua tao duoc dispose ngay va exception duoc rethrow de global handler hien thi thong bao an toan.

## 11. Navigation Scope Lifecycle

`OpenTargetAsync` -> tao module scope -> resolve scoped ViewModel -> OpenOrActivate -> window song cung scope -> Close -> dispose ContentViewModel -> dispose module scope.

Scope cho `IWorkspaceService` va `IAuditService` van la scope ngan rieng cua thao tac navigation. No khong duoc gan vao ViewModel.

## 12. Front Office Gate Isolation

Property gate van chi dung duy nhat cho route `front-office-reservations`.

Khi khong co Property, Reservation tra `NavigationResult` khong thanh cong voi thong bao context an toan. Route khac khong can va khong doc Property context.

## 13. DI Validation

Da chay `UI_RES_U_019_resolves_workspace_through_validated_di_scope` voi `ValidateOnBuild=true` va `ValidateScopes=true`: PASS 1/1.

Kiem tra nay resolve `ReservationWorkspaceViewModel` trong DI scope hop le. Constructor cua `NavigationService` khong con nhan root `IServiceProvider`.

## 14. Exception Logging Improvement

Global exception handler da giu exception type, message, stack trace va inner chain.

`NavigationService` nay bo sung log Serilog an toan khi activation fail, gom route, module key, trang thai module scope va resolver strategy. Exception DI goc giu ViewModel type va resolution chain. Khong log password, token, connection string hay Guest PII.

## 15. Unit Test Results

- Navigation regression tests: PASS 7/7.
- Full unit tests: PASS 263/263.

Mot lan full-run truoc do co 1 failure cua `BackgroundSchedulerServiceTests.Cancellation_PropagatesToRunningExecution`; test rieng PASS ngay sau do va full-run tiep theo PASS 263/263. Khong lien quan navigation va khong co thay doi scheduler.

## 16. Integration Test Results

Full integration tests: PASS 20/20.

## 17. Navigation Regression Tests

`NavigationScopeRegressionTests` bo sung:

- Non-FrontOffice route khong bi Property gate: dashboard, restaurant-banquet, inventory, customers.
- Reservation khong co Property bi chan an toan, khong tao scope.
- ViewModel duoc resolve trong module scope va scope con song den khi window dong.
- Dong window dispose scope cu.
- Unknown route mo placeholder, khong crash shell.

## 18. Live Module Matrix

Chua thuc hien live click matrix. Ly do: runtime da khoi dong bao `Environment=Production`, database `ASMILE_BO`; yeu cau cam dung production database.

| Module | Route | Live opened |
|---|---|---|
| Tong quan | dashboard | Chua chay - runtime khong an toan |
| Thue va BCTC | tax-financial | Chua chay - runtime khong an toan |
| Phieu thu | payment-receipt | Chua chay - runtime khong an toan |
| Phieu chi | payment-voucher | Chua chay - runtime khong an toan |
| Nha hang Tiec | restaurant-banquet | Chua chay - runtime khong an toan |
| Quan ly kho | inventory | Chua chay - runtime khong an toan |
| Quan ly khach hang | customers | Chua chay - runtime khong an toan |
| Trung tam bao cao | report-center | Chua chay - runtime khong an toan |
| Virtual File Center | virtual-file-center | Chua chay - runtime khong an toan |
| Cai dat he thong | settings | Chua chay - runtime khong an toan |

## 19. Reservation Without Property

Automated regression: PASS. Route `front-office-reservations` tra ket qua khong thanh cong voi thong bao chon Property; khong crash shell va khong tao module scope.

## 20. Reservation With Property

Chua live verify, vi runtime an toan co `Environment=Development` va database LocalDB test rieng chua duoc cung cap. Khong tu y tao hoac sua cau hinh database.

## 21. Build Results

`dotnet build .\\ASMILE.BO.sln --no-restore`: PASS, 0 warning, 0 error.

XAML encoding check: PASS.

## 22. EF Model Check

`dotnet ef migrations has-pending-model-changes`: PASS, khong co model change ke tu migration gan nhat.

Co 2 warning da ton tai ve required relationship va global query filter cua RestaurantQuote/QuoteAuditLog va QuoteChangeRequest/QuoteChangeRequestDetail. Khong phat sinh do bugfix nay.

## 23. Remaining Issues

- Can mot profile Development an toan, su dung LocalDB database test tach biet, de thuc hien live matrix 10 module.
- Runtime hien tai ghi loi worker do thieu bang `fo_process.HenGioQuyTrinh`; loi nay nam ngoai pham vi navigation regression va khong duoc sua trong task nay.
- Chua build/replace instance Release dang chay cua nguoi dung.

## 24. Release Recommendation

Khong phat hanh dua tren build/test mot minh. Truoc khi release, khoi dong build da sua voi Development-safe LocalDB, chay live matrix, sau do kiem tra Reservation khi khong co va khi co Property.

## 25. Final Status

**CHUA HOAN THANH**.

Root cause da duoc sua va duoc bao ve bang regression tests, build, full unit va integration tests. Acceptance gate live 10 module va Reservation co Property chua dat vi khong co runtime Development-safe duoc cap.
