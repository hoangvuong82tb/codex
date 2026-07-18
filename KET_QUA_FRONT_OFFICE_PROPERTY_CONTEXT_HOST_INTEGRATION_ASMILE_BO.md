# KET QUA FRONT OFFICE PROPERTY CONTEXT HOST INTEGRATION ASMILE-BO

## 1. Executive Summary

Da bo sung host-level Front Office Property context cho WPF. Nguon tin cay la Branch hien hanh cua `ICurrentCompanyContext`; moi Branch active duoc map 1:1 thanh Front Office Property bang chinh Branch Id da ton tai. Khong tao Property schema, khong hardcode PropertyId, khong dung Guid.Empty fallback.

Trang thai: **CHUA HOAN THANH** theo acceptance gate nghiem ngat, chi vi live Reservation workflow chua duoc thao tac den cuoi. Development app da mo an toan va hien Property selector/module `Dat phong`, nhung co input cua nguoi dung tren cua so sau khi mo nen automation dung ngay, khong chon Property hay thao tac Reservation thay nguoi dung.

## 2. Scope

Da lam Property host, Branch-to-Property mapping, execution scope lifecycle, selector trong taskbar, navigation gate, Reservation workspace reset/reload, DI lifetime correction, unit/host DI tests va development-safe smoke quan sat.

Khong sua Reservation domain, numbering, schema/migration, CRM, RatePlan, Check-in, Payment, Folio hay Housekeeping.

## 3. Existing Session Architecture

`ICurrentUserService` la singleton giu user, session va permission codes. `ICurrentCompanyContext` la singleton giu Company/Branch va khoi phuc active company session. DesktopShell da tai Company/Branch context khi khoi tao.

## 4. Existing Property Concepts

Codebase chua co Property aggregate/table chinh thuc. Branch la don vi van hanh hien co, co Id/Code/Name va duoc `ICurrentCompanyContext` tai chi khi active. Branch la mapping phu hop va khong duplicate abstraction.

## 5. Trusted Property Source

`ICurrentCompanyContext.GetBranchesAsync()` la nguon Property option. Host chi chap nhan Branch Id nam trong danh sach Branch active cua Company context hien hanh. UI Reservation khong co input PropertyId.

## 6. Context Host Design

Da tao `IFrontOfficePropertyContextHost` va `FrontOfficePropertyContextHost` voi CurrentProperty, AvailableProperties, Version, ErrorCode, PropertyChanged, Initialize, SetCurrentProperty va ClearCurrentProperty.

## 7. Execution Context Wiring

Host tao `FrontOfficeExecutionContext` bang Branch Id, user/session that tu `ICurrentUserService`, roles, workstation, local business date va correlation id. `IFrontOfficeExecutionContextAccessor` duoc dang ky singleton de context cua app session duoc nhin thay boi handler scope cua Reservation.

## 8. Property Selector

Da them selector Property vao taskbar, dung `AvailableBranches`/`SelectedBranch` hien co. Selector chi hien Branch code, co tooltip an toan va khong expose EF entity, connection string hay permission internals.

## 9. Auto-selection

Neu chi co mot Branch active, host auto-select va goi `ICurrentCompanyContext.SetBranchAsync`. Neu co nhieu Branch va khong co Branch session hop le, context bi block cho den khi nguoi dung chon.

## 10. Last Property Restore

Host reuse Branch da duoc `ICurrentCompanyContext` khoi phuc tu active company session. Branch duoc revalidate trong danh sach active truoc khi tao execution context.

## 11. Navigation Gating

`NavigationService` tu choi `front-office-reservations` neu khong co Property context. DesktopShell cung deny module truoc khi navigation khi context vang mat.

## 12. Reservation Workspace Integration

Workspace subscribe host PropertyChanged. Khi doi/xoa Property, UI huy search/debounce, reset adapter DI scope, clear list/detail/room types/room panel/RowVersion va reload chi khi Property moi hop le.

## 13. Property Switch Lifecycle

Host dung clear-old-then-activate-new. Scope cu dispose truoc scope moi; Version tang sau moi transition. Workspace khong tai su dung DTO, token hay DbContext scope cua Property cu.

## 14. Logout Lifecycle

Quick logout action cua shell goi ClearCurrentPropertyAsync. App host dispose context scope khi host shutdown. Chua co logout workflow chinh thuc ngoai quick action hien co trong shell.

## 15. Permission Refresh

Reservation adapter reset scope moi khi Property change, do do `IAuthorizationService` scoped duoc resolve lai. Khi context khong ton tai, UI deny by default va navigation bi block.

## 16. Cancellation and Race Handling

Workspace huy CancellationToken cua search/debounce truoc reset. Host dung SemaphoreSlim de serialize property transitions; rapid switch test ket thuc o Property moi nhat.

## 17. DI Lifetimes

NavigationService la singleton va chi giu Property host singleton, khong giu scoped handler. Execution context accessor duoc singleton hoa cho app session. Reservation adapter tao scope rieng va reset/dispose scope khi context thay doi.

## 18. Security Review

Khong hardcode PropertyId, khong Guid.Empty, khong UI input PropertyId, khong log PII/token/connection string. Context absence va Property ngoai trusted Branch list deu deny by default.

## 19. Error Mapping

Da map FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED, NOT_AUTHORIZED, NOT_FOUND, INACTIVE va LOAD_FAILED sang thong diep UI tieng Viet khong dau.

## 20. Unit Test Results

Da them/bo sung context tests cho no selection, single auto-select, unauthorized reject, switch, clear/logout, restored Branch, rapid switch, unauthenticated deny, workspace clear va workspace reload.

Focused context + Reservation workspace tests: PASS, 15/15.

## 21. Host Integration Test Results

UI-RES-U-019 build full .NET Host voi ValidateOnBuild=true va ValidateScopes=true, resolve `ReservationWorkspaceViewModel` thanh cong. Context host lifecycle unit tests dung execution accessor that, khong dung Property database gia.

## 22. Reservation UI Regression

Reservation workspace tests PASS, gom list mapping, RowVersion replacement, concurrency no-retry, permission state, DI resolve, property clear va property reload.

## 23. Live Smoke Verification

Da phan loai app connection la LocalDB/development va mo Debug app. Quan sat duoc Windows 11 shell, Property selector va module `Dat phong`. Sau do co user input tren cua so, nen khong tiep tuc click/select/login de tranh can thiep vao phien nguoi dung. Live list/create/assign/status chua duoc xac nhan.

## 24. Visual Check

Da xac nhan bang cua so app that: Property selector hien trong taskbar va `Dat phong` xuat hien trong module shell. Khong tuyen bo pixel visual pass va khong co screenshot automation.

## 25. EF Model Check

`dotnet ef migrations has-pending-model-changes`: PASS, khong co model change tu migration gan nhat. EF co 2 warning query filter da ton tai cua RestaurantQuote/QuoteChangeRequest, khong lien quan pham vi nay.

## 26. Build Results

Full solution build PASS, 0 warning, 0 error va XAML encoding check PASS sau host code va test changes. Sau do co mot XAML-only refinement de hien Code - Name cua Property selector. App project duoc build lai vao `artifacts/frontoffice-property-context-build` de tranh DLL lock cua cua so Debug dang duoc nguoi dung mo: PASS, 0 warning, 0 error va XAML encoding check PASS. Khong dong process cua nguoi dung.

## 27. Full Unit Results

`dotnet test .\\ASMILE.BO.Tests.Unit\\ASMILE.BO.Tests.Unit.csproj --no-build`: PASS, 256/256.

## 28. Full Integration Results

`dotnet test .\\ASMILE.BO.Tests.Integration\\ASMILE.BO.Tests.Integration.csproj --no-build`: PASS, 20/20 LocalDB tests.

## 29. Files Created

- `ASMILE.BO.App/Services/FrontOfficePropertyContextHost.cs`
- `ASMILE.BO.Tests.Unit/FrontOfficePropertyContextHostTests.cs`
- Bao cao nay.

## 30. Files Modified

- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/Services/ReservationUiService.cs`
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
- `ASMILE.BO.App/ViewModels/ReservationWorkspaceViewModel.cs`
- `ASMILE.BO.App/Views/TaskbarView.xaml`
- `ASMILE.BO.Tests.Unit/ReservationWorkspaceViewModelTests.cs`

## 31. Package Changes

Khong them NuGet package.

## 32. Deviations

Dung Branch selector hien co thay vi them Property selector/Property database moi. Day la mapping chinh thuc Branch-to-Property cho deployment hien tai.

## 33. Existing Issues

`ICurrentCompanyContext` hien chua co per-user Branch permission model rieng; danh sach Branch active cua current Company la trusted source dang co. Backend Reservation van enforce Property isolation bang context va property-scoped query.

## 34. Known Limitations

- Live Reservation workflow chua duoc thao tac den cuoi do user input da tiep quan cua so smoke.
- Chua co logout workflow chinh thuc ngoai quick action shell hien co.
- Khong co public Property aggregate; Branch mapping can duoc duy tri ro rang trong cac module Front Office tiep theo.
- Chua co screenshot/pixel automation.

## 35. Release Recommendation

Khong release Front Office live workflow chi dua tren ket qua nay. Co the merge/dua vao development sau khi mot phien smoke co login duoc uy quyen kiem tra select Property, open Reservation, list request va property switch end-to-end.

## 36. Recommended Next Sprint

Chay manual smoke voi tai khoan development duoc uy quyen; xac nhan multi-Branch permission neu business yeu cau; bo sung logout chinh thuc va module-window lifecycle disposal khi WindowManager co close callback.

## 37. Final Status

**CHUA HOAN THANH**

Trusted Branch source, host context, selector, execution scope, navigation gating, workspace reload/reset, DI validation, EF check, unit va LocalDB integration deu pass. Chua dat acceptance chi vi live Reservation smoke chua duoc phep hoan tat tren cua so dang co phien nguoi dung.
