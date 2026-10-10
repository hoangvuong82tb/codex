# Kiểm tra và bổ sung migration module Quản lý khách sạn

Ngày kiểm tra: 2026-09-07  
Cơ sở dữ liệu kiểm tra: `ASMILE_BO` trên `localhost`

## Kết luận

Không được chạy `dotnet ef database update` trực tiếp trên database hiện tại.
Database đã có các bảng được tạo bởi nhánh migration cũ, trong khi source hiện
tại thiếu một số file migration đã được ghi nhận trong
`__EFMigrationsHistory`. Nếu chạy nối tiếp toàn bộ migration, nhiều lệnh
`CREATE TABLE` sẽ gặp lỗi trùng bảng.

Đã bổ sung script reconciliation idempotent:

`scripts/reconcile_hotel_schema_20260907.sql`

Script tạo phần schema còn thiếu theo `AppDbContextModelSnapshot`, có transaction,
kiểm tra đúng database/dependency, tạo khóa ngoại/index có điều kiện và không ghi
`__EFMigrationsHistory`.

## Các bảng còn thiếu so với model hiện tại

| Schema | Bảng | Nhóm |
|---|---|---|
| `dbo` | `AppModules` | Settings/module catalog |
| `dbo` | `SettingChangeLogs` | Settings/audit |
| `dbo` | `SettingDefinitions` | Settings |
| `dbo` | `SettingValues` | Settings |
| `dbo` | `LostFoundItems` | Lost & Found |
| `dbo` | `LostFoundHistories` | Lost & Found |
| `fo` | `DonDep` | Housekeeping |
| `fo` | `TrangThaiDonDep` | Housekeeping |
| `fo` | `RoomInspection` | Housekeeping |
| `fo` | `PhongDoan` | Group rooming list |
| `fo` | `KeyCardCredential` | Key card |
| `fo` | `KeyCardEvent` | Key card |
| `fo` | `MinibarSession` | Minibar |
| `fo` | `MinibarConsumption` | Minibar |

Tổng cộng: **14 bảng**. Đây là so sánh giữa model snapshot hiện tại và
`sys.tables` thực tế, không suy luận từ tên module trên giao diện.

## Migration/source drift đang tồn tại

Database đã có history tới:

`20260719230000_AddFrontOfficeSettingsPhase2`

Nhưng source checkout hiện tại không còn đầy đủ file của các migration
`20260719...` liên quan Guest Profile, Group Reservation, Rate Engine, Service
Request, Wake-up Call, Lost & Found, Concierge, Message Center, Guest Feedback,
VIP/Loyalty, Task/Shift, Notification Center, Audit Timeline và Settings.

Ngoài ra, local source còn 5 migration chưa có trong database history:

- `20260521080623_AddFinancialStatementEngine`
- `20260524060743_AddAdvancedFinancialStatementMappingFoundation`
- `20260720032319_AddReservationEnterpriseFields`
- `20260721065248_AddRatePlanGroupReservationAndReservationGuaranteeDeposit`
- `20260814231130_AddHousekeepingMinibarKeyCardContracts`

Hai migration Reservation Enterprise và Rate Plan/Group Reservation có nhiều
`CREATE TABLE` trùng với bảng đã tồn tại. Vì vậy chúng phải được xử lý bằng
reconciliation có kiểm soát hoặc khôi phục đúng source migration, không replay
nguyên chuỗi.

## Trạng thái triển khai

- Đã tạo script schema reconciliation cho 14 bảng thiếu.
- Script đã được kiểm tra cú pháp bằng SQL Server `PARSEONLY`.
- Đã tạo và xác minh backup `COPY_ONLY, CHECKSUM` tại
  `.tmp/ASMILE_BO_before_hotel_reconcile_20260907.bak`.
- Đã apply vào `ASMILE_BO`; kiểm tra sau apply cho thấy model snapshot không còn
  bảng thiếu (`MODEL_MISSING_COUNT=0`), đủ 11 khóa ngoại kiểm soát và các index
  kiểm soát.
- Đã chạy lại script lần hai thành công (`IDEMPOTENT_REAPPLY_OK`).
- Đã sửa tiếp các lệch cột của `DatPhong`, `LuuTruKhachSan`, rate/group và
  payment/guest indexes. Kiểm tra toàn model hiện tại: `289/289` bảng, không
  thiếu cột, `COLUMN_MISMATCHES=0`, `766/766` index spec khớp.
- Đã sửa migration history có kiểm soát bằng
  `scripts/repair_hotel_migration_history_20260907.sql`; 3 migration local đã
  được ghi nhận sau preflight/fingerprint, không replay DDL.
- Fingerprint schema trước history repair:
  `69E5D077FB561EDA7832FBD49B5B2ED23EC0C9B45F0E59553D0363575E813B39`.
- Lệnh `migrations has-pending-model-changes` báo model không có thay đổi so với
  snapshot; kết quả này không thay thế việc kiểm tra database thực tế.

## Bước triển khai tiếp theo

1. Chụp backup `COPY_ONLY, CHECKSUM` và chạy `RESTORE VERIFYONLY`.
2. Chạy script reconciliation trên đúng `ASMILE_BO`.
3. Kiểm tra đủ 14 bảng, khóa ngoại, index và `dotnet ef migrations has-pending-model-changes`.
4. Đối chiếu schema fingerprint với model snapshot.
5. Đã repair riêng `__EFMigrationsHistory` cho đúng 3 migration còn pending;
   `dotnet ef migrations list` với đúng connection hiện không còn dòng Pending.
6. Tiếp tục test nghiệp vụ: Đặt phòng, Lễ tân, Housekeeping, Chia/Gộp Folio,
   Thu ngân và Night Audit bằng dữ liệu thật/mẫu đã có.
