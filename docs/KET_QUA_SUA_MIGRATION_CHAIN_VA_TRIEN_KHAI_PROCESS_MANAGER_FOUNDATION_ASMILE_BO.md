# Kết quả sửa Migration Chain và triển khai Process Manager Foundation

Ngày thực hiện: 2026-07-17. Không chạy migration production, không dùng connection string production, không commit/push/PR và không triển khai workflow Reservation, Stay, Folio hoặc Payment.

## 1. Executive Summary

Đã sửa được full EF migration chain, xác minh clean install và upgrade path trên SQL Server LocalDB disposable. Đã tạo Process Manager Foundation persisted trong schema `fo_process`, gồm 8 bảng, migration riêng, repository/query, atomic timeout claim và worker timeout tối thiểu. Kết quả tổng thể: **HOÀN THÀNH CÓ GIỚI HẠN** vì độ phủ test chưa đạt toàn bộ acceptance gate Process Manager.

## 2. Baseline

Trước sửa, build/test nền pass nhưng clean migration dừng trước Foundation vì `LedgerAccounts` không tồn tại.

## 3. Migration Chain Root Cause

Migration `20260517090000_AddLedgerAccounts` có source và attribute Migration nhưng thiếu `[DbContext(typeof(AppDbContext))]`, nên EF không đưa migration này vào migration list. Schema source của migration cũng thiếu nhiều cột đã có trong snapshot trước migration metadata. Migration `20260520145845_AddLedgerAccountMetadata` vì vậy tham chiếu bảng/cột chưa tồn tại.

## 4. LedgerAccounts Ownership

Owner là General Ledger/Accounting: entity `LedgerAccount`, `AppDbContext.LedgerAccounts`, SQL table `dbo.LedgerAccounts`. Migration tạo bảng đúng là `20260517090000_AddLedgerAccounts`; migration đầu tiên mở rộng bảng là `20260520145845_AddLedgerAccountMetadata`. Không có bằng chứng rename bảng.

## 5. Migration Repair Strategy

Khôi phục migration lịch sử bị nhận diện thiếu thay vì tạo corrective migration sai thứ tự. ID không đổi. Bổ sung metadata DbContext và các cột đã được snapshot tại thời điểm trước metadata migration xác nhận. DB đã có history entry sẽ không chạy lại migration này; không sửa thủ công `__EFMigrationsHistory` và không drop bảng production.

## 6. Migration Files Changed

`20260517090000_AddLedgerAccounts.cs` được sửa. Thêm `20260717105902_CreateFrontOfficeProcessManagerFoundation`.

## 7. Clean Database Migration Result

LocalDB database trống đã áp dụng toàn bộ chain đến `20260717105902_CreateFrontOfficeProcessManagerFoundation` thành công, sau đó database disposable được xóa.

## 8. Existing Database Upgrade Result

LocalDB được migrate đến `20260716085141_AddBanquetServiceCosting`, sau đó upgrade đến latest thành công: Foundation technical stores và Process Manager migration đều áp dụng đúng một lần.

## 9. Process Manager Architecture Applied

Domain persistence records không phụ thuộc EF. Application có contracts repository và monitor query. Infrastructure chứa EF store, claim SQL Server và hosted timeout dispatcher. Không có state process trong singleton/dictionary memory.

## 10. Process Tables Created

Schema `fo_process`: `QuyTrinhVanHanh`, `BuocQuyTrinh`, `LanThucHienBuocQuyTrinh`, `TinHieuQuyTrinh`, `HenGioQuyTrinh`, `BuTruQuyTrinh`, `HanhDongThuCongQuyTrinh`, `LichSuQuyTrinh`.

## 11. EF Core Mapping

PK, unique process key, FK restricted, index subject/correlation/status/timeout, rowversion cho các record mutable và `DateOnly` business date đã được map. Bảng dùng tên PascalCase không dấu trong schema `fo_process`, là deviation có chủ ý theo naming cho phép của prompt.

## 12. Process Domain Foundation

`ProcessInstance` có UUIDv7 application-generated, process key, property/business date/correlation, trạng thái, transition và terminal guard. Có record step, attempt, signal, timeout, compensation, manual action, history.

## 13. Process Application Foundation

`IProcessRepository`, `IProcessMonitorQueries`, DTO monitor/timeline và kết quả reserve signal được khai báo. Không có generic repository hay workflow DSL.

## 14. Repository and Persistence

`EfProcessManagerStore` hỗ trợ load process, active key, append history, reserve signal, mark signal processed, save và monitor queries. Payload raw không được expose trong DTO monitor.

## 15. Signal Inbox

`TinHieuQuyTrinh` có composite unique `(ProcessId, SignalId)`, payload hash, schema version, correlation/causation, received/processed time và status. Hash khác trên signal ID trùng được phân loại mismatch.

## 16. Timeout Scheduler

`ProcessTimeoutHostedService` chạy theo `PeriodicTimer` 10 giây, có cancellation và graceful stop. `ProcessTimeoutDispatcher` chỉ tạo durable timeout signal/history; không hard-code nghiệp vụ Front Office.

## 17. Retry Engine

Persistence có attempt count/retry count/error code. Chưa có policy engine hoàn chỉnh cho các classification và backoff/jitter.

## 18. Compensation

Có persistence `BuTruQuyTrinh` với type, status, command/approval/audit reference, retry và error. Chưa có automatic compensation executor.

## 19. Manual Action

Có persistence `HanhDongThuCongQuyTrinh` với permission, role/actor assignment, reason/approval flags, due/completion/result/audit reference. Chưa có authorization command handler.

## 20. Resume and Recovery

State, timeout, signal và attempts đều durable. Lease hết hạn được đưa lại vào điều kiện claim. Chưa có test crash simulation giữa tất cả các bước.

## 21. Worker Claim and Leasing

Timeout claim dùng SQL Server atomic `UPDATE ... OUTPUT` trên CTE với `UPDLOCK, READPAST, ROWLOCK`; chỉ pending hoặc claimed lease-expired được claim. Test xác minh worker thứ hai không claim timeout đang leased.

## 22. Process Monitor Queries

Có Get by ID, search active, waiting manual action, reconciliation, due timeout, stuck process và timeline. Chưa tạo WPF screen.

## 23. Outbox/Inbox Integration

Foundation Outbox/Inbox hiện hữu vẫn được giữ. Process signal inbox là store riêng vì semantics `(ProcessId, SignalId)` khác Inbox integration tổng quát. Worker hiện không gọi external service trong DB transaction.

## 24. Migration Created

`20260717105902_CreateFrontOfficeProcessManagerFoundation` kèm designer và snapshot update. Migration chỉ tạo schema/tables Process Manager.

## 25. Generated SQL Review

Idempotent script đã sinh và review qua EF CLI: chứa conditional migration guards, restored `LedgerAccounts` create trước metadata operations và Process Manager migration. Không có `DROP` ngoài database disposable cleanup.

## 26. Unit Test Results

225/225 pass. Bổ sung tests UUIDv7 process, terminal transition, durable signal và timeout terminal operation.

## 27. SQL Server Integration Test Results

13/13 pass. LocalDB test bao gồm migrate Foundation + Process Manager, process/timeout persistence, duplicate signal và second claim không nhận timeout đang leased.

## 28. Worker Test Results

Dispatcher được compile và timeout claim được test qua repository. Chưa có test riêng poison timeout, cancellation timing hoặc lease-renewal.

## 29. Architecture Test Results

Existing Foundation architecture test pass; domain Process Manager nằm ở Core và không reference EF/WPF. Chưa có test reflection riêng cho toàn bộ Process Manager rules.

## 30. Build Results

`dotnet build .\ASMILE.BO.sln --no-restore` pass: 0 warning, 0 error; XAML encoding check pass.

## 31. Security Review

Không có secret/production connection được thêm. Process monitor không trả raw payload/PII. Worker không có quyền user UI; manual action chưa expose endpoint/UI nên chưa có bypass authorization.

## 32. Files Created

Core Process persistence, Application contracts, Infrastructure store/dispatcher, migration + designer, unit test và báo cáo này.

## 33. Files Modified

`AppDbContext.cs`, `DependencyInjection.cs`, repaired `AddLedgerAccounts.cs`, `FrontOfficeSqlServerIntegrationTests.cs`.

## 34. Package Changes

Không thay đổi package.

## 35. Migration Snapshot Changes

`AppDbContextModelSnapshot.cs` được EF cập nhật cho 8 process tables và schema `fo_process`.

## 36. Deviations

Tên SQL process tables là PascalCase không dấu thay vì snake_case tiếng Việt của technical Foundation; đây là lựa chọn được prompt cho phép để giữ tên nghiệp vụ rõ ràng và tránh transliteration khó đọc.

## 37. Existing Issues

EF vẫn cảnh báo hai relationship query filter của RestaurantQuote/QuoteAuditLog và QuoteChangeRequest/Detail; đây là issue tồn tại trước, không thuộc Process Manager.

## 38. Known Limitations

Chưa có state-machine definition/guard registry hoàn chỉnh, retry policy/backoff engine, compensation executor, manual-action authorization handler, out-of-order signal policy đầy đủ, crash simulation và race test bằng hai DbContext song song.

## 39. Deferred Work

Hoàn thiện acceptance tests PRC-DB-005 đến PRC-DB-015, worker poison/retry tests, metrics/structured logging đầy đủ, timeout dead-letter/manual escalation và allowlist artifact persist.

## 40. Recommended Next Sprint

Hoàn thiện Process Manager hardening trước khi dùng cho Reservation vertical slice: state definitions typed, command/outbox handoff, dual-worker race tests, recovery tests và authorization cho manual action.

## 41. Git Diff Summary

Workspace hiện không chứa `.git`, nên không thể tạo git diff/stat. Không có commit, push hay PR.

## 42. Final Status

**HOÀN THÀNH CÓ GIỚI HẠN.** Migration chain, clean install, upgrade path, migration Process Manager, persistence và baseline claim đều pass; chưa đủ test/hardening để qua toàn bộ acceptance gate.

CHƯA SẴN SÀNG TRIỂN KHAI RESERVATION VERTICAL SLICE.
