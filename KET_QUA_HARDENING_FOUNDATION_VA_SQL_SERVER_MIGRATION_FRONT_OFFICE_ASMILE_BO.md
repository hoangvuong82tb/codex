# Kết quả Hardening Foundation và SQL Server Migration Readiness — Front Office Asmile-BO

## 1. Executive Summary

Đã harden DI, runtime migration policy, EF design-time path và tạo migration SQL Server cho ba technical store Front Office: Outbox, Inbox và Durable Idempotency. Build, full unit test, full integration test và SQL Server LocalDB Foundation integration test đều pass. Chưa thể xác nhận migration chain toàn hệ thống từ database rỗng vì một migration lịch sử dừng trước Foundation do thiếu `LedgerAccounts`; vì vậy chưa đủ điều kiện triển khai Process Manager Foundation.

## 2. Baseline Results

Trước thay đổi: solution build 0 warning/0 error; Unit 222/222 pass; Integration 12/12 pass. EF tooling có các DI validation warning: singleton RuntimeTelemetry giữ scoped audit; hai ViewModel yêu cầu `AppModuleInfo` không được đăng ký; MainWindow singleton capture dependency chain có scoped authentication. Runtime startup tự gọi migrate vô điều kiện.

## 3. Repository Discovery

Solution dùng .NET 8, WPF Generic Host, EF Core SQL Server 8.0.7, `AppDbContext`, `IDesignTimeDbContextFactory`, xUnit và LocalDB (`MSSQLLocalDB`). Migration strategy là EF migration trong `ASMILE.BO.Infrastructure/Persistence/Migrations`. Không có SQL Server test database đã cấu hình sẵn; LocalDB được dùng với database tên ngẫu nhiên, disposable và Integrated Security.

## 4. DI Problems Found

- `RuntimeTelemetryService` là singleton/hosted service nhưng constructor nhận `IAuditService` scoped.
- `DesktopIconViewModel` và `ShellAppItemViewModel` yêu cầu `AppModuleInfo`, nhưng không có registration hợp lệ hoặc consumer DI.
- `MainWindow` và `DesktopShellViewModel` singleton giữ `LoginViewModel`; LoginViewModel phụ thuộc `IAuthenticationService` scoped.

## 5. DI Fixes Applied

`RuntimeTelemetryService` giữ lifetime singleton đúng cho hosted worker, nhưng tạo scope ngắn hạn trong audit path để resolve `IAuditService`. Hai ViewModel không có DI consumer đã được bỏ registration. Shell/MainWindow được đổi thành scoped; App giữ một application scope trong vòng đời MainWindow rồi dispose khi thoát. Host bật `ValidateScopes` và `ValidateOnBuild`; EF tooling chạy không còn DI validation error.

## 6. Runtime Migration Policy

Đã thêm `DatabaseMigrationMode`: `Disabled`, `ValidateOnly`, `ApplyPendingMigrations`, `DevelopmentAutoMigrate`.

- Mặc định appsettings: `Disabled`.
- ValidateOnly kiểm tra kết nối/pending migration, có thể fail-fast.
- DevelopmentAutoMigrate chỉ được phép trong Development.
- ApplyPendingMigrations ngoài Development yêu cầu `Database:AllowApplyPendingMigrations=true` explicit.
- Startup chỉ log mode, environment và database name; không log connection string/credential.

## 7. Design-time DbContext

`AppDbContextFactory` không còn hard-code credential. Nó chỉ nhận `ASMILE_BO_DESIGNTIME_CONNECTION`; nếu không có, dùng LocalDB `ASMILE_BO_DesignTime` với Integrated Security. Factory không khởi tạo WPF host, hosted service hoặc runtime migration. `dotnet ef dbcontext info` pass với SQL Server provider và safe design-time target.

## 8. Migration Scope

Migration `20260717102111_CreateFrontOfficeFoundationTechnicalStores` chỉ tạo schema `fo` và các table `fo_hop_thu_di`, `fo_hop_thu_den`, `fo_khoa_idempotency`. Không có Process Manager table, Reservation, Stay, Folio, Payment, projection hay audit store mới.

## 9. Migration Created

Đã tạo:

- `20260717102111_CreateFrontOfficeFoundationTechnicalStores.cs`
- `20260717102111_CreateFrontOfficeFoundationTechnicalStores.Designer.cs`

Migration chỉ có CreateTable/CreateIndex trong Up và DropTable ba technical table trong Down. Không có rename/drop/change column ngoài scope.

## 10. Tables and Constraints

`fo_hop_thu_di`: PK `PK_fo_hop_thu_di`, payload hash, correlation/causation, DateOnly `date`, status/retry/lease, rowversion; index status+retry, lease expiry và payload hash.

`fo_hop_thu_den`: composite PK `PK_fo_hop_thu_den` trên consumer/message, payload hash, status/retry/lease, rowversion.

`fo_khoa_idempotency`: PK `PK_fo_khoa_idempotency`, unique `UQ_fo_khoa_idempotency_khong_gian_khoa`, request hash, status/expiry index và rowversion.

## 11. Generated SQL Review

Đã sinh script idempotent chỉ cho Foundation tại `artifacts/front-office-foundation-only-idempotent.sql`. Script có `BEGIN TRANSACTION`/`COMMIT`, guard `__EFMigrationsHistory`, `CREATE SCHEMA [fo]`, đúng ba CreateTable, đúng PK/index, `nvarchar`, `datetime2`, `date`, `uniqueidentifier`, `rowversion`. Không có Banquet, Reservation, Folio hoặc Payment operation trong script Foundation.

## 12. SQL Server Test Environment

Đã dùng SQL Server LocalDB thật (`MSSQLLocalDB`) với database random `ASMILE_BO_FO_TEST_<guid>`, Integrated Security, timeout và `EnsureDeletedAsync` cleanup. Không dùng production/staging connection string hoặc dữ liệu nghiệp vụ.

## 13. SQL Server Integration Tests

`FrontOfficeSqlServerIntegrationTests`: 1/1 pass trên LocalDB thật. Test apply migration Foundation twice trên schema disposable có predecessor history, UUIDv7 Guid, DateOnly, Unicode/nvarchar, rowversion creation, outbox persist/hash, audit+outbox commit, transaction rollback, inbox dedupe/hash mismatch và idempotency same/different request hash.

Full `MigrateAsync()` từ database rỗng vẫn fail trước Foundation ở migration lịch sử vì `LedgerAccounts` không tồn tại. Đây là lỗi migration chain hiện hữu, không phải migration Foundation; không được che hoặc tự sửa ngoài scope.

## 14. Unit Test Results

Full Unit: 223/223 pass. Có thêm `DatabaseMigrationPolicyTests` xác nhận policy Production mặc định là Disabled và không tự migrate.

## 15. Integration Test Results

Full Integration: 13/13 pass, bao gồm SQLite tests hiện hữu và SQL Server LocalDB Foundation test mới.

## 16. Architecture Test Results

Architecture boundary test Foundation vẫn pass: Core không tham chiếu EF/WPF, Application không tham chiếu WPF. Scope scan không có Reservation/Stay/Folio/Payment/Process Manager runtime trong Foundation.

## 17. EF Tooling Results

`dotnet ef dbcontext info` pass: `AppDbContext`, SQL Server provider, database `ASMILE_BO_DesignTime`, LocalDB. `dotnet ef migrations list --no-connect` pass, có Foundation migration và không có DI validation error. Script idempotent Foundation đã sinh thành công.

## 18. Build Results

`dotnet build ASMILE.BO.sln --no-restore`: pass, 0 warning/0 error. XAML encoding check pass.

## 19. Security Review

Design-time hard-coded credential đã bị loại bỏ. Migration policy chỉ log database identifier an toàn, không log full connection string. Không có broker/API/external call mới. Script/migration không chứa secret. Appsettings connection hiện hữu vẫn chứa credential legacy; không thay đổi để tránh phá runtime configuration và cần được chuyển sang secret store ở work riêng.

## 20. Runtime Safety Review

Production default không tự apply migration. Apply ngoài Development cần explicit opt-in. ValidateOnly có pending count và fail-fast. Không có popup UI hoặc runtime migration từ EF tooling. `RuntimeTelemetryService` không còn giữ scoped service.

## 21. Files Created

- `ASMILE.BO.Tests.Unit/DatabaseMigrationPolicyTests.cs`
- `ASMILE.BO.Tests.Integration/FrontOfficeSqlServerIntegrationTests.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260717102111_CreateFrontOfficeFoundationTechnicalStores.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260717102111_CreateFrontOfficeFoundationTechnicalStores.Designer.cs`
- `artifacts/front-office-foundation-idempotent.sql`
- `artifacts/front-office-foundation-only-idempotent.sql`
- Báo cáo này.

## 22. Files Modified

- `App.xaml.cs`, `DependencyInjection.cs` trong App.
- `DatabaseOptions.cs`, `DatabaseMigrationService.cs`, `AppDbContextFactory.cs`, `RuntimeTelemetryService.cs`, `AppDbContext.cs` trong Infrastructure.
- `appsettings.json` (migration policy explicit).
- `RuntimeTelemetryServiceTests.cs` để phù hợp constructor an toàn.
- `AppDbContextModelSnapshot.cs`.

## 23. Package Changes

Không thêm hoặc nâng package. Dùng EF Core/SQL Server/LocalDB hiện có.

## 24. Migration Snapshot Changes

Snapshot có đúng mapping Foundation technical stores và restored `CostAmount`/`CostPrice` của migration Banquet lịch sử. Migration `20260716085141_AddBanquetServiceCosting` đã được phục hồi với đúng ID và Up/Down delta sau khi EF CLI với binary cũ cố gỡ nhầm migration đó; không đổi contract database của Banquet.

## 25. Deviations from Architecture

SQL naming giữ `fo_*` snake_case tiếng Việt không dấu đã chọn ở Foundation; constraint/index nay dùng prefix explicit `PK_`, `UQ_`, `IX_`. Audit tiếp tục dùng platform `AuditLog`, không thêm audit table. Không triển khai claim store, Process Manager runtime hay concurrency-conflict workflow.

## 26. Existing Issues

Migration chain toàn hệ thống từ database rỗng lỗi ở migration lịch sử trước Foundation: thiếu `LedgerAccounts`. EF generate cũng cảnh báo global query filter required relationship của RestaurantQuote/QuoteAuditLog và QuoteChangeRequest/Detail. Cả hai không do Foundation và không được sửa ngoài scope.

## 27. Known Limitations

Chưa có test full chain `Migrate()` từ database rỗng pass do existing migration defect. Chưa có atomic multi-worker outbox claim implementation/test. `AppDbContext` legacy concurrency retry chưa được thay đổi nên chưa có test conflict result end-to-end. Codebase-memory indexing tạo local graph artifact ngoài source scope; artifact không tham gia build/test/migration.

## 28. Deferred Work

Khôi phục/sửa migration chain `LedgerAccounts` bằng work riêng có owner Banquet/Accounting, thêm full clean-database migration test, outbox lease-claim atomic SQL implementation, concurrency result policy end-to-end, và chuyển legacy appsettings credential sang secret management.

## 29. Git Diff Summary

Workspace không expose git metadata ở `D:\ASMILE-BO`, nên không thể chạy git diff/status. Đã rà migration/script/test và không có business Front Office ngoài scope. Không commit, push hoặc PR.

## 30. Final Status

HOÀN THÀNH CÓ GIỚI HẠN

CHƯA SẴN SÀNG TRIỂN KHAI PROCESS MANAGER FOUNDATION.
