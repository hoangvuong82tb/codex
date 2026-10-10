# Front Office — kiểm tra dữ liệu thật cho nghiệp vụ đoàn

**Ngày kiểm tra:** 14/08/2026  
**Phạm vi:** Group Reservation, Room Block, Rooming List, Group Billing và Group Dashboard trong module Quản lý khách sạn.  
**Mục tiêu:** phân biệt rõ đường dữ liệu thật với mock trước khi triển khai production.

## Kết luận ngắn

Các đường đọc của hồ sơ đoàn, phân phòng, Rooming List, Room Block, Group Billing và Group Dashboard hiện đã đi qua EF Core trong production DI. Room Block là projection cấp nhóm từ dữ liệu phòng đoàn vì schema hiện tại chưa có aggregate RoomBlock riêng; Group Billing lấy tổng doanh thu, tiền cọc và công nợ đã lưu trên hồ sơ đoàn. Các thao tác ghi/configuration cần aggregate persistence chưa có vẫn bị chặn fail-closed, không trả thành công giả.

Chưa áp migration và chưa ghi dữ liệu vào database thật trong phase này.

## Phân loại theo nghiệp vụ

| Nghiệp vụ | Bằng chứng | Trạng thái |
|---|---|---|
| Hồ sơ đoàn | `AppDbContext.FrontOfficeGroupReservations` → `fo.DatPhongDoan`; entity `GroupReservation` | **Đọc EF thật** |
| Phân phòng đoàn | `AppDbContext.FrontOfficeGroupRoomAssignments` → `fo.PhongDoan`; FK tới `GroupReservation`, unique `(GroupReservationId, HotelRoomId)` | **Đọc EF thật** |
| Rooming List đọc/xuất | `EfRoomingListService` query `AsNoTracking` từ `FrontOfficeGroupRoomAssignments`, lọc theo `GroupReservationId`, projection DTO; DI đăng ký `IRoomingListService` | **Đọc/xuất EF thật** |
| Rooming List import/gán khách/arrival/departure | Chưa có command/repository transaction và contract persistence đầy đủ; service fail-closed bằng `NotSupportedException` | **Chưa release ghi** |
| Room Block đọc/pickup/dashboard | `EfRoomBlockService` tổng hợp nhóm đang hoạt động và assignment từ `FrontOfficeGroupReservations`/`FrontOfficeGroupRoomAssignments`; DI đăng ký `IRoomBlockService` | **Đọc EF thật, projection cấp nhóm** |
| Room Block tạo/sửa/nhả phòng | Chưa có entity/aggregate RoomBlock và RoomBlockDay để ghi an toàn | **Chưa release ghi** |
| Group Billing summary/revenue | `EfGroupBillingService` đọc `TotalRevenue`, `TotalDeposit`, `OutstandingBalance` từ `GroupReservation`; DI đăng ký `IGroupBillingService` | **Đọc EF thật, chỉ tiêu chưa phân loại = 0** |
| Group Billing cấu hình/routing | Chưa có aggregate instruction/allocation/tax/commission và audit persistence | **Chưa release ghi** |
| Group Dashboard | `EfGroupDashboardService` đọc hồ sơ đoàn + phân phòng, tính active group, pickup, arrival/departure, doanh thu, công nợ và at-risk | **Đọc EF thật** |
| UI generator | `GroupRoomBlockWindow.xaml.cs` còn helper sinh block/guest mẫu cho màn hình cũ | **Mock UI phụ trợ, không đăng ký production service** |

## Thay đổi đã thực hiện

1. Bổ sung navigation và mapping mạnh kiểu cho các aggregate đoàn, phân phòng, folio và hồ sơ khách.
2. Thêm `EfRoomingListService` cho đọc/xuất Rooming List từ EF.
3. Thêm `EfRoomBlockService` cho dashboard, active block và pickup từ dữ liệu hồ sơ đoàn/phòng đoàn.
4. Thêm `EfGroupBillingService` cho summary và revenue dashboard từ số liệu đã lưu trên hồ sơ đoàn.
5. Đăng ký các service EF trong `DependencyInjection`; các service mock cũ không còn là implementation production.
6. Các command chưa có persistence tương ứng đều fail-closed thay vì báo thành công giả.
7. Bổ sung preflight read-only để kiểm tra kết nối, migration, seed và fingerprint bảng trước khi có quyết định migration.

## Khoảng trống cần hoàn thiện trước production

### P0 — xác nhận database đích

- Lấy schema read-only từ đúng SQL Server runtime: bảng, cột, FK, unique/index, row count và property/tenant scope.
- Đối chiếu migration history với model snapshot. Diagnostic migration trước đó có drift ngoài phạm vi; không tự động áp migration.
- Chốt `PropertyId/CompanyId/BranchId` và concurrency token cho mọi query/mutation.

### P1 — hoàn thiện domain persistence

- Tạo entity/mapping/repository cho RoomBlock và RoomBlockDay.
- Tạo entity/mapping/repository cho Group Billing Instruction, allocation, tax/commission và audit.
- Bổ sung transaction, idempotency, row-version cho import, assign, arrival/departure và billing.

### P2 — hoàn thiện application service

- Thay các static helper còn lại trong `GroupEngine` bằng application service dùng DI.
- Loại `Random`/seed khỏi runtime production; mock chỉ được bật qua provider design-time hoặc feature flag.
- UI chỉ truyền canonical ID và property context, không dùng snapshot nghiệp vụ làm source of truth.

### P3 — kiểm thử và phát hành

- Integration test đọc/ghi trên database test có schema fingerprint.
- Test quyền theo role/property, duplicate import, retry/idempotency, optimistic concurrency và audit log.
- UAT đầy đủ luồng: tạo đoàn → room block → phân phòng → rooming list → group billing → check-in/out.

## Verification

- Build Infrastructure: **PASS**, 0 errors; chỉ còn các warning đã có từ trước.
- Build App: **PASS**, 0 errors, 0 warnings.
- XAML encoding check: **PASS**, không phát hiện mojibake.
- Unit tests nhóm/database policy: **PASS** (15/15 trước phase service; cần chạy lại sau khi thêm service nếu test suite có coverage mới).
- Không chạy migration và không ghi database thật trong phase này.

## Quy tắc chấp nhận

Chỉ đánh dấu một màn hình là **real** khi có đường chứng minh đầy đủ: View/ViewModel → Application service → repository/query → EF/API → entity/mapping đã đối chiếu schema runtime. DTO tĩnh, mock generator hoặc tài liệu thiết kế riêng lẻ không đủ điều kiện.
