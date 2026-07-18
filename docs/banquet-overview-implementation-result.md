# Kết quả triển khai Giai đoạn 1 — tab Tổng quan / Thông tin sự kiện

## 1. Kết luận báo cáo đã xác minh

| Kết luận | Đánh giá | Bằng chứng |
|---|---|---|
| Các trường đặc thù và số phát sinh chỉ có UI/VM, chưa lưu | Đúng | Trước thay đổi, `BanquetEvent`, `SaveBanquetEventRequest`, `BanquetEventDto` không có 10 trường UI. |
| Backend đã kiểm tra overlap phòng, sức chứa và loại trừ event đang sửa/hủy | Đúng | `BanquetRestaurantService.ValidateEventScheduleAsync` dùng `StartTime < EndTime` giao nhau, room/date/status và Id loại trừ. |
| Hợp đồng và cọc bị cascade từ event | Đúng | `AppDbContext` cấu hình cả hai FK với `DeleteBehavior.Cascade`. Đã đổi thành `Restrict`. |
| Cọc nối hợp đồng bằng cascade | Sai | `BanquetDeposit -> BanquetContract` đã là `DeleteBehavior.NoAction`. |
| Customer.Email/TaxCode/CreatedAt có thể lưu | Sai | Ba property này là `[NotMapped]`; `UpdatedAt` chưa tồn tại. Tab hiện chỉ lưu `CustomerName` và `Phone` vào `BanquetEvent`, không tạo/tìm Customer theo điện thoại. |

## 2. Thay đổi đã thực hiện

| File | Thay đổi | Lý do |
|---|---|---|
| `ASMILE.BO.Core/Entities/RestaurantEntities.cs` | Thêm 10 trường Event thực sự có trên form. | Lưu dữ liệu đặc thù hai chiều. |
| `ASMILE.BO.Application/DTOs/RestaurantDtos.cs` | Bổ sung request/response DTO. | Không mất dữ liệu khi save/load. |
| `ASMILE.BO.Infrastructure/Services/RestaurantServices.cs` | Mapping, validation backend theo event type/số âm và trả DTO đầy đủ. | Backend là nguồn validation chính. |
| `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs` | Cấu hình độ dài text và đổi FK Event→Contract/Deposit sang Restrict. | Bảo vệ dữ liệu tài chính. |
| `ASMILE.BO.App/ViewModels/RestaurantViewModels.cs` và `BanquetRestaurantViewModel.Partial.cs` | Save/load/reset/dirty snapshot cho trường mới; chặn double-save. | Không mất dữ liệu và không tạo event trùng. |
| `ASMILE.BO.App/Views/BanquetRestaurantView.xaml` | Disable nút Lưu khi đang lưu; bỏ nút “Sửa” trùng command Lưu. | Tránh bấm lưu nhiều lần. |
| `ASMILE.BO.Tests.Unit/RestaurantModuleTests.cs` | Kiểm tra tạo event lưu lại Bride/Groom và số phát sinh. | Regression test persistence mapping. |

## 3. Migration

- Tên: `20260710033555_CompleteBanquetOverviewEventFields`.
- Thay đổi: 10 cột `BanquetEvents` (text nullable, `BirthdayDate` nullable, số phát sinh default `0`) và hai FK Event→Contract/Deposit chuyển từ Cascade sang Restrict.
- Chưa áp dụng database.
- Lệnh developer áp dụng thủ công sau khi kiểm tra connection string development:

```powershell
dotnet ef database update --project ASMILE.BO.Infrastructure --startup-project ASMILE.BO.App --context AppDbContext
```

Migration generator đã phát hiện các model drift không liên quan (menu/à-la-carte); chúng đã bị loại khỏi Up/Down migration này và không thuộc Giai đoạn 1.

## 4. Build và test

- `dotnet restore ASMILE.BO.sln`: thành công.
- `dotnet build ASMILE.BO.sln --no-restore`: thành công, 0 error; còn 4 warning có sẵn (duplicate using `App.Models`, field `_counter` không dùng).
- `dotnet test ASMILE.BO.Tests.Unit/ASMILE.BO.Tests.Unit.csproj --no-restore --filter ...Banquet...`: 2/2 pass.
- Toàn bộ `dotnet test ASMILE.BO.sln --no-build --no-restore` đã được khởi chạy cùng build toàn solution nhưng vượt giới hạn 60 giây của phiên công cụ, nên không có kết quả pass/fail tổng để kết luận.
- Đã test tự động: tạo event/cọc/hợp đồng/tổng tiền; lưu/tải lại Bride/Groom và ExtraGuest/ExtraTable; overlap phòng và vượt sức chứa.
- Chưa chạy UI end-to-end với database development vì migration chưa được phép áp dụng.

## 5. Ngoài phạm vi Giai đoạn 1

- Customer master data: tìm/tái sử dụng theo số điện thoại, Email/TaxCode/audit fields.
- `GuestConfirmedCount`, `TableConfirmedCount`, `SetupTime`, `TeardownTime` chưa có UI nên không thêm.
- Cảnh báo khi đóng cửa sổ, báo cáo, cài đặt, timeline, checklist và buffer setup/teardown khi kiểm tra lịch.

## 6. Đánh giá tab Tổng quan

**Có thể kiểm thử nội bộ.** Luồng code và unit tests đã xác nhận persistence/validation, nhưng cần áp dụng migration trên development và chạy checklist UI TC-01, TC-02, TC-03, TC-05, TC-06, TC-07, TC-08, TC-11, TC-12 cùng TEST-A đến TEST-E trước khi chạy thử tại đơn vị.
