# Hotel Management — real-data readiness matrix

**Ngày kiểm tra:** 13/08/2026  
**Phạm vi:** toàn bộ module `Quản lý khách sạn` và các màn hình được mở từ Front Office/Housekeeping/Accounting.  
**Mục tiêu:** chốt màn hình nào đã có đường dữ liệu thật, màn hình nào còn mock/mixed, và điều kiện bắt buộc trước khi nối production.

> **Addendum kiểm tra lại 08/09/2026:** Bảng dưới đây là baseline lịch sử, không phải trạng thái mới nhất. Sau các đợt triển khai tiếp theo, `Room Block`, `Incident Log`, `Exchange Rate`, Group service/order và các luồng Folio đã được nối thêm EF hoặc chuyển sang fail-closed. Trạng thái hiện hành và bằng chứng mới nhất nằm trong [HOTEL_MANAGEMENT_MOCK_AUDIT_20260907.md](HOTEL_MANAGEMENT_MOCK_AUDIT_20260907.md), đặc biệt các mục rà soát ngày 08/09/2026. Không dùng dòng cũ trong bảng này để kết luận hiện trạng sau ngày kiểm tra.

## Kết luận

Module **chưa thể phát hành thương mại toàn bộ**. Một số aggregate lõi đã có repository/application service EF, nhưng nhiều màn hình vẫn dùng `LoadMockData`, `Random`, generator hoặc service trả DTO tĩnh. Không được suy luận “real” chỉ từ việc màn hình build được.

## Ma trận hiện trạng

| Phân hệ/nghiệp vụ | Đường dữ liệu đã xác nhận | Trạng thái | Việc cần làm trước production |
|---|---|---|---|
| Room Master / Danh sách phòng | `HotelRoomMasterDataApplicationService` → repository EF → `AppDbContext`/room tables; có property scope, row-version, audit intent | **Real foundation** | Integration test trên DB đã fingerprint; hoàn thiện mutation/UAT |
| Reservation / Đặt phòng | Reservation repository/handlers EF hiện hữu trong application/infrastructure | **Real-path partial** | Trace từng command UI, kiểm tra tenant/property scope và E2E |
| Stay / Lưu trú, Check-in/out | Stay repository/handlers EF hiện hữu; một số cửa sổ FO vẫn có mock loader | **Mixed** | Nối toàn bộ ViewModel vào canonical Stay/Folio, bỏ mock runtime |
| Folio / Cashiering / Payment | Folio/payment handlers và repository EF tồn tại; nhiều màn hình nghiệp vụ còn mock/đường mở cửa sổ lỗi | **Mixed** | Hoàn thiện contract mutation, idempotency, audit và smoke test từng card |
| Invoice / Hóa đơn | Có domain/application foundation nhưng UI workflow phát hành chưa chứng minh đủ A-B-C-D qua repository thật | **Mixed / not release** | Trace workflow phát hành, mapping invoice, retry/concurrency |
| Group Reservation / Hồ sơ đoàn | `FrontOfficeGroupReservations` map `fo.DatPhongDoan` | **Real-read foundation** | Repository/query integration và mutation contract |
| Group Room Assignment / Phân phòng | `FrontOfficeGroupRoomAssignments` map `fo.PhongDoan`, FK + unique assignment | **Real-read foundation** | Transaction assign/swap/lock và E2E |
| Rooming List | `EfRoomingListService` đọc/xuất từ `GroupRoomAssignment`, DI đã đăng ký | **Real read/export** | Import/gán khách/arrival/departure đang fail-closed; cần command + transaction |
| Room Block *(baseline 13/08)* | `GroupEngine.RoomBlockService` trả DTO tĩnh | **Đã supersede** | Xem `EfRoomBlockService` và migration `20260908090000_AddGroupRoomBlocks` trong audit mới |
| Group Billing / Billing instruction | `GroupBillingService` trả tổng hợp tĩnh; chưa có entity/DbSet đủ bằng chứng | **Mock** | Schema + allocation/tax/commission + posting/audit |
| Group Dashboard | `GroupDashboardService.GetDashboardAsync` trả số liệu cố định | **Mock** | Query projection từ canonical group/room/folio/payment |
| Guest Inquiry / Tra cứu khách | UI có generator/`BuildFakeData` và số lượng mock lớn | **Mock UI** | Nối GuestProfile/Stay/Folio/Invoice/Payment thật; phân trang server-side |
| Guest / Khách trong đoàn | Domain guest có entity liên quan nhưng các workflow UI còn mock/đường tab lỗi | **Mixed** | Canonical GuestProfile + document/preference persistence, E2E |
| Housekeeping Room Status | Nhiều ViewModel Housekeeping có `LoadMockData`/fake generator | **Mock-heavy** | Room status/task/inspection schema, repository, realtime/audit |
| Cleaning / Inspection / Dirty Room | Có các ViewModel mới nhưng dữ liệu fake; chưa có persistence contract đủ | **Mock-heavy** | Task/checklist/score/approval schema và workflow transaction |
| Lost & Found / Minibar / Workforce | UI có fake data hoặc service static; chưa chứng minh DB path end-to-end | **Mock-heavy** | Entity/repository/audit, workflow commands, role checks |
| Key/Card log | Có UI nhưng chưa xác nhận DbSet/repository production path | **Partial / unknown** | Chốt schema live, repository, audit/security |
| FO log / Shift handover | UI/service mixed; chưa đủ bằng chứng read/write thật cho mọi tab | **Mixed** | Canonical shift/log entities, audit and approval workflow |
| AR / Aging / Reconciliation | AR screens có static/random signals; payment/reconciliation foundation tồn tại riêng | **Mixed** | Nối query thật, phân biệt ledger source, reconciliation idempotency |

## Quy tắc phân loại

- **Real:** View/ViewModel → application service → repository/query handler → EF/API → mapping đã đối chiếu schema runtime.
- **Mixed:** có đường thật ở domain/infrastructure nhưng UI hoặc một workflow vẫn mock/chưa chứng minh.
- **Mock:** static DTO, `Random`, `LoadMockData`, `BuildFakeData`, generator hoặc service hard-code.
- Tài liệu thiết kế hoặc build PASS **không** phải bằng chứng dữ liệu thật.

## Bằng chứng phase hiện tại

- `EfRoomingListService` đã đọc/xuất Rooming List bằng `AsNoTracking` từ EF; thao tác ghi chưa đủ contract bị chặn bằng `NotSupportedException`.
- `HotelRoomMasterDataApplicationService` có property scope, row-version/concurrency và audit intent.
- `GroupDashboardService`, `RoomBlockService`, `GroupBillingService` vẫn là static/mock.
- Search inventory đã xác nhận nhiều UI còn `LoadMockData` trong FO/Housekeeping/Guest windows.

## Gate nối dữ liệu thật và migration

1. Chụp fingerprint read-only của **đúng** database runtime: server/database/schema, bảng, cột, FK, index/unique, row count và property scope.
2. Đối chiếu migration history/model snapshot. Diagnostic migration cho thấy model drift ngoài phạm vi; **không chạy** `dotnet ef database update` và không ghi migration history khi chưa có exact match.
3. Chốt canonical IDs (`PropertyId`, `CompanyId`, `BranchId`), concurrency token, role/permission và audit contract.
4. Chỉ sau đó mới tạo migration hẹp, idempotent, có backup `COPY_ONLY,CHECKSUM` và kiểm tra restore.

## Verification gần nhất

- App build: **PASS — 0 errors, 0 warnings**.
- XAML encoding check: **PASS**.
- Group unit tests: **14 passed, 0 failed, 0 skipped**.
- `git diff --check`: **PASS**.
- Chưa áp migration và chưa ghi dữ liệu thật.

## Lộ trình ưu tiên

1. P0: fingerprint DB + schema reconciliation.
2. P1: hoàn thiện Group Room Block/Billing và Guest/Housekeeping persistence.
3. P2: thay mock runtime bằng DI/application service, giữ mock chỉ ở design-time provider.
4. P3: integration/UAT theo luồng Reservation → Stay → Folio/Payment → Invoice → Group/Housekeeping → AR.

## Addendum kiểm tra sâu — 09/09/2026

Đã kiểm tra lại bằng graph của codebase và đối chiếu các điểm gọi trong `ASMILE.BO.App`, `ASMILE.BO.Application` và `ASMILE.BO.Infrastructure`:

| Khu vực | Kết quả kiểm tra hiện hành |
|---|---|
| Folio Workspace / Proforma / Split Folio / Transfer Posting | Đường đọc/ghi thật đã được nối qua repository/application service. Các generator `Random` còn lại nằm trong khối `#if false`, không được biên dịch/chạy. |
| Receipt Voucher | `ReceiptVoucherWindow` chuyển sang `PaymentReceiptsViewModel`; ViewModel đọc/ghi qua `IReceivableService`, và production DI đăng ký `EfReceivableService`. Nhánh fallback không dựng dữ liệu giả; chỉ hiển thị rỗng/fail-closed. |
| Group Booking | Wizard dùng `IGroupReservationQueryService` và `IGroupReservationCommandService.ConfigureBookingAsync`; room block/deposit được lưu EF và audit trong cùng persistence session. |
| Group Checkout | Đọc group/stay/folio/housekeeping/minibar/key-card từ service thật; xác nhận gọi `IGroupCheckOutCommandService`. Không dùng `LoadGroups`/`LoadRooms` giả. |
| Guest Inquiry / AR / Housekeeping ViewModels | Generator cũ vẫn còn trong mã nguồn để tham chiếu hoặc sau `return` không thể chạy; các màn hình production fail-closed khi thiếu repository, không tự sinh bản ghi giả. Guest Inquiry fake-data nằm trong `#if false`. |
| Mock services trong Infrastructure | Một số lớp `Mock*Service` còn tồn tại cho test/migration reference nhưng không được đăng ký trong production `AddInfrastructure`; các contract chính đang trỏ tới EF implementation hoặc unavailable provider có chủ đích. |
| E-Invoice, printer permissions, service status | E-Invoice vẫn dùng `Unavailable*` để chặn phát hành giả; danh sách máy in và ACL đọc qua Windows Print Spooler thật, gồm principal, Allow/Deny và quyền quản trị/in. Danh sách Windows Service và restart có đường Win32 thật, restart chỉ hoạt động khi bật `ServiceSupervisor:AllowRestart` và allow-list. System settings đã đọc từ bảng `AppSettings` thật qua `EfSystemSettingsService`; bảng cũ chỉ lưu key/value nên metadata được suy ra hoặc ghi rõ nguồn. |

### Kết luận sau lần kiểm tra này

- Chưa thấy đường runtime nào trong nhóm Hotel/Front Office nêu trên đang lấy danh sách từ `Random`, `LoadMockData` hoặc DTO tĩnh.
- Không nên xóa hàng loạt generator lịch sử ngay lúc này vì chúng đang bị vô hiệu hóa có chủ ý; xóa sẽ làm diff lớn và tăng rủi ro hồi quy.
- Backup database đã chuyển sang `SqlServerBackupService`: liệt kê file `.bak` thật, tạo `COPY_ONLY + CHECKSUM`, xác thực bằng `RESTORE VERIFYONLY`; restore vẫn khóa mặc định bằng `Database:AllowDatabaseRestore=false`.
- Settings Engine đã chuyển `SettingDefinition`, `SettingValue`, `AppModule` và `SettingChangeLog` từ repository tĩnh sang EF repository; migration `20260909100000_AddSettingsEnginePersistence` bổ sung bốn bảng mà migration chain trước đó còn thiếu.
- Các gap còn lại là nghiệp vụ/tích hợp thật chưa có backend tương ứng (invoice export/adjust/print, một số thao tác Group Guest/Housekeeping và provider E-Invoice), không phải lỗi “đang hiển thị mock”.
- Chưa áp migration vào production. Dữ liệu demo chỉ được tạo qua luồng training/seed có chủ đích, không tự chèn vào production runtime.

## Xác nhận sau triển khai ngày 09/09/2026

- `SystemSettings`, Settings Engine, backup provider, printer enumeration và service-status enumeration đã có đường dữ liệu thật tương ứng.
- ACL máy in Windows đã đọc thật qua Spooler security descriptor và trả principal/quyền Allow-Deny; E-Invoice vẫn fail-closed do cần contract/credential bên ngoài; restart service được bảo vệ bằng cấu hình và allow-list.
- Unit: **696/696 pass**; Integration LocalDB: **22/22 pass**; E2E: **4/4 pass**.
- Chưa áp migration và chưa ghi production database.

### Rate Engine

- Đã loại bỏ property ID và occupancy hard-code trong `RateEngineViewModel`.
- Occupancy được tính từ phòng active và Stay đang ở thật theo property/room type; màn hình được đăng ký qua presentation DI.
- `ISecuritySettingsService` đã đọc cấu hình `Security.RequireAdminConfirmation` từ Settings Engine/EF qua `EfSecuritySettingsService`; nếu thiếu/sai giá trị vẫn fail-safe ở `true`.

## Night Audit — cập nhật 09/09/2026

| Khu vực | Kết quả kiểm tra hiện hành |
|---|---|
| Night Audit pre-check | Đã bỏ số liệu mẫu cố định trong `NightAuditCenterWindow`. Folio mở đọc từ `IHotelFolioRepository`, khách quá ngày trả tính từ Stay thật, doanh thu đọc từ `GetRevenueSnapshotAsync`. |
| Chênh lệch thanh toán | Đã đọc settlement thật qua `IHotelPaymentRepository.GetSettlementSummaryAsync`; Folio có `Outstanding` được đánh dấu cần xử lý. |
| Đối soát thu ngân chờ xử lý | Đã đọc các ca Pending/VarianceDetected/Escalated qua `ICashReconciliationService.GetPendingAsync`; số liệu rỗng được hiểu là không còn ca chờ xử lý, không phải số 0 dựng sẵn. |
| Đồng bộ hệ thống | Đã dùng `IInfrastructureHealthService.CheckAsync` để đọc trạng thái kết nối database và migration thật; Warning/Unhealthy được đưa vào pre-check. |

Snapshot doanh thu là đầu ra của Night Audit nên trạng thái chưa có snapshot không chặn lần chạy đầu tiên; UI hiển thị “Sẽ tạo” và chỉ đánh giá snapshot sau bước ghi thật.

Night Audit pre-check hiện đã có nguồn dữ liệu thật cho cả sáu kiểm tra. Các kiểm tra vẫn fail-closed khi service/context không tồn tại hoặc khi query trả lỗi.
