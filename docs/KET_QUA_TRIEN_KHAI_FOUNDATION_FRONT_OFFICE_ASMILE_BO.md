# Kết quả triển khai Foundation Front Office Asmile-BO — Wave 0 + Wave 1

## 1. Executive Summary

Đã triển khai Foundation kỹ thuật cho Front Office theo mô hình modular monolith hiện hữu: boundary logic, Result/Error, UUIDv7 Strongly Typed ID, execution context, CQRS contracts, technical persistence, outbox, inbox, durable idempotency, audit adapter, save pipeline và test. Không có Reservation, Stay, Folio, Payment, màn hình WPF Front Office, integration adapter hay Process Manager runtime.

## 2. Repository Discovery

Solution là `ASMILE.BO.sln`, dùng .NET 8; App là WPF (`net8.0-windows`) với Generic Host/DI/Serilog. Layer đang có là Core, Application, Infrastructure, Shared, App, Unit Tests và Integration Tests. EF Core SQL Server 8.0.7; test dùng xUnit 2.9.3 và SQLite/InMemory. Không có MediatR/CQRS mediator hoặc Result abstraction dùng chung; đã có `PagedResult` và exception platform. Đã có `AppDbContext`, `EfUnitOfWork`, audit platform, SQL Server migration và một idempotency của workflow cũ.

Tám tài liệu thiết kế Front Office bắt buộc đã được đọc trước triển khai; hash nguồn đã kiểm tra khớp bộ tài liệu đã phê duyệt.

## 3. Architecture Decisions Applied

Không tách project mới để tránh song song kiến trúc. Boundary Front Office được đặt trong namespace/folder `FrontOffice/Foundation` của Core, Application và Infrastructure; WPF không tham chiếu DbContext/repository cho Front Office. Core không tham chiếu EF/WPF; Application không tham chiếu WPF. Existing audit store được dùng lại thay vì tạo audit table riêng.

## 4. Files Created

17 source/test file mới: 4 Core, 5 Application, 5 Infrastructure và 3 test. Tất cả nằm trong `FrontOffice/Foundation` hoặc là test Foundation. Không có binary, cache, migration generated hoặc UI file được tạo.

## 5. Files Modified

- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`: DbSet và EF mappings technical Foundation.
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`: đăng ký service scoped/singleton Foundation.

## 6. Projects and Dependencies

`Core` chứa ID, domain-event contract và technical persistence records; `Application` chứa context/CQRS/persistence contracts; `Infrastructure` hiện thực EF/clock/context/outbox/audit. `Shared` chứa Result/Error chung. App chỉ nhận registrations qua `AddInfrastructure`; chưa có navigation hoặc screen Front Office. Unit và Integration tests tham chiếu các layer theo dependency hiện hữu.

## 7. Result/Error Foundation

Đã thêm `Error` (Code, Message, Category, Severity, Retryable, Metadata) và `Result`/`Result<T>`. Có categories Validation, Business, Authorization, Concurrency, IntegrationPending, UnknownExternalOutcome và System. Business rejection dùng Result; lỗi hệ thống không bị nuốt. Test xác nhận rejection business không cần exception.

## 8. Strongly Typed ID Foundation

Đã có `PropertyId`, `ActorId`, `CorrelationId`, `CommandId`, `EventId`, `MessageId`, `ProcessId`, `IdempotencyKey` và UUIDv7 application-generated. ID hỗ trợ equality, parse/TryParse, empty guard qua `From`, JSON round-trip, `ToString` an toàn cho logging và tương thích WPF binding. EF mappings dùng explicit value conversion; không dùng reflection-heavy framework.

## 9. Context Foundation

Đã có Actor, Property, Business Date, Correlation và execution context scoped, cùng `IClock`/`SystemClock`. Business date chỉ đến từ `BusinessDateContext` đáng tin cậy; không suy diễn bằng `DateTime.Now`. Context accessor không singleton và scope được hoàn nguyên sau khi dispose. UI chưa có API để tự gán trusted context.

## 10. CQRS Foundation

Đã có `ICommand`, `ICommand<T>`, `IQuery<T>`, handlers, behavior delegate, command envelope, paging và `CancellationToken`. Không thêm MediatR vì repository chưa có mediator. Pipeline order được xác định: Correlation, Validation, Authorization, Idempotency, Transaction, Handler, Audit/Outbox, Result mapping, Logging. Chưa tạo use case hay generic CRUD handler.

## 11. EF Core Foundation

`AppDbContext` vẫn là DbContext duy nhất theo staged migration. Ba technical table mapping dùng schema `fo`, tên bảng/cột tiếng Việt không dấu, explicit key/index/constraint names, enum string, rowversion convention, UTC timestamps và `DateOnly` cho business date. Lazy loading/AutoInclude/global ledger soft-delete không được bật.

## 12. Outbox

Đã có domain-event contract, mapper contract, canonical JSON serializer, SHA-256 payload hash, outbox factory, lease/status/retry state và no-op publisher. `EfFrontOfficePersistenceSession` stage event, tạo outbox và ghi audit trong cùng SaveChanges/transaction; không external publish trước commit và không broker/worker thật.

## 13. Inbox

Đã có durable `fo_hop_thu_den`, composite unique identity `(ConsumerName, MessageId)`, payload-hash mismatch, reserved/completed/failed state và completion boundary. `EfInboxStore` sử dụng EF + database thật trong test SQLite, không mock DbSet. Chưa có consumer nghiệp vụ.

## 14. Idempotency

Đã có durable `fo_khoa_idempotency`, unique `(Namespace, Key)`, request hash, command ID, expiry, in-progress/completed/failed/expired states và safe result reference. `EfIdempotencyStore` xử lý reserve, request-hash mismatch, duplicate completed, in-progress và expiry. Không lưu raw response/PII.

## 15. Audit

Không tạo audit store Front Office song song. `FrontOfficeAuditIntent` và `FrontOfficeAuditWriter` chuyển intent sang `AuditLog` platform trong cùng persistence session, lưu action, aggregate ref, actor/correlation, result/failure/reason-code/approval-ref trong JSON an toàn. Không nhận free-text PII hay dùng Domain Event thay Audit record.

## 16. Unit of Work and Save Pipeline

`EfFrontOfficePersistenceSession` là boundary Front Office bổ sung bên cạnh `EfUnitOfWork` platform, để không refactor các module hiện có. Nó yêu cầu trusted context, mở relational transaction khi phù hợp, tạo outbox, drain audit, SaveChanges, commit/rollback và map `AppConcurrencyException` thành `Result` concurrency. Không có external call trong transaction.

## 17. Migration

Không tạo migration Foundation. Repository có migration strategy nhưng App hiện gọi `DatabaseMigrationService.InitializeAsync()` khi startup, tức có khả năng migrate runtime. Tạo migration/snapshot mới mà không có môi trường SQL Server được chỉ định sẽ mở đường cho DDL ngoài kiểm soát khi app chạy, trái với yêu cầu không sửa production. `dotnet ef migrations list --no-connect` chạy được và liệt kê migration hiện hữu; nó báo các lỗi DI validation có sẵn của App khi dựng host, sau đó fallback design-time và không kết nối database. Mapping được xác nhận bằng SQLite `EnsureCreated` integration test.

## 18. Architecture Tests

Đã thêm test assembly-reference enforce Core không phụ thuộc EntityFramework/WPF và Application không phụ thuộc WPF. Test chạy pass.

## 19. Unit Tests

`FrontOfficeFoundationTests` có 5 test: Result/Error, UUIDv7/parse/JSON, context scope, dependency boundary, và SQLite transaction cho outbox/audit/inbox/idempotency.

## 20. Integration Tests

`FrontOfficeFoundationIntegrationTests` sử dụng SQLite in-memory relational database thật và `EnsureCreated`, kiểm tra durable inbox/idempotency behavior. Không mock DbSet, không cần Testcontainer/SQL Server instance.

## 21. Build Results

`dotnet restore ASMILE.BO.sln`: thành công, tất cả up-to-date.

`dotnet build ASMILE.BO.App/ASMILE.BO.App.csproj --no-restore`: thành công, 0 warning, 0 error. XAML encoding check passed.

## 22. Test Results

- Targeted Foundation Unit: 5/5 passed.
- Full Unit suite: 222/222 passed.
- Targeted Foundation Integration: 1/1 passed.
- Full Integration suite: 12/12 passed.

## 23. Existing Failures

Không có build/test failure sau thay đổi. `dotnet ef migrations list --no-connect` ghi nhận DI validation warning từ các registration App có sẵn: singleton `RuntimeTelemetryService` phụ thuộc scoped audit service; `DesktopIconViewModel`/`ShellAppItemViewModel` thiếu `AppModuleInfo`; singleton `MainWindow` phụ thuộc scoped authentication service. Lệnh vẫn liệt kê migrations và không phải do Foundation tạo ra.

## 24. Deviations from Architecture Documents

Tài liệu dự kiến separation theo project FrontOffice; implementation dùng namespace/folder trong các project layer hiện hữu để tương thích low-risk modular monolith. Không tạo migration hoặc Process Manager persistence/runtime trong Wave này. Audit dùng platform `AuditLog` hiện hữu và safe JSON intent thay vì một audit table mới.

## 25. Security Review

Không thêm secret/connection string. Result không chứa stack trace. Payload hash dùng SHA-256. Idempotency lưu reference thay raw response. Audit intent nhận reason code/reference thay free-text PII. Không có external publisher, broker, API hoặc logging raw payload.

## 26. Known Limitations

Chưa có SQL Server migration nên table Foundation chưa xuất hiện trong database triển khai. Inbox/idempotency reserve hiện dựa trên unique index + EF persistence; Process Manager worker/claim coordinator chưa được triển khai. Context source chưa nối với shell/security/property master vì không có use case Front Office trong Wave 0+1.

## 27. Deferred Work

Tạo/review migration Foundation trong môi trường SQL Server được phê duyệt; Process Manager persistence/runtime/lease claim; command dispatcher behaviors cụ thể; context adapter từ security/property/business-date source; outbox publisher worker; monitoring/retention; use cases Reservation/Stay/Folio ở wave sau.

## 28. Recommended Next Sprint

Trước tiên phê duyệt migration chỉ cho ba table technical Foundation, chạy review SQL Server và bổ sung migration snapshot. Sau đó triển khai Process Manager Foundation trên outbox/inbox/idempotency boundary đã có, vẫn chưa triển khai nghiệp vụ reservation. Đồng thời xử lý các DI validation warning hiện hữu trước khi dùng EF tooling như một quality gate.

## 29. Git Diff Summary

Workspace hiện không expose `.git` tại `D:\ASMILE-BO`; vì vậy không thể chạy `git diff`/`git status`. Đã kiểm tra danh sách file Foundation: chỉ các file nêu ở sections 4–5 và báo cáo này. Không commit, push hoặc Pull Request.

## 30. Final Status

HOÀN THÀNH CÓ GIỚI HẠN

CHƯA SẴN SÀNG TRIỂN KHAI PROCESS MANAGER FOUNDATION.
