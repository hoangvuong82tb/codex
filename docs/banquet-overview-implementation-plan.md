# Kế hoạch triển khai Giai đoạn 1 — tab Tổng quan / Thông tin sự kiện

## A. Phạm vi sửa

Chỉ hoàn thiện luồng tạo, sửa, lưu và tải lại hồ sơ sự kiện trong tab Tổng quan:

- Lưu hai chiều 10 trường đang có trên UI: `BrideName`, `GroomName`, `ConferenceName`, `BirthdayPersonName`, `BirthdayDate`, `MeetingTopic`, `PartyTheme`, `OtherEventName`, `ExtraGuestCount`, `ExtraTableCount`.
- Bổ sung validation backend theo loại sự kiện và các số lượng không âm; giữ kiểm tra sức chứa và overlap phòng ở backend hiện có.
- Ngăn double-save tại ViewModel và giữ trạng thái dirty/snapshot cho các trường mới.
- Bảo vệ hợp đồng và cọc bằng `DeleteBehavior.Restrict` cho quan hệ trực tiếp từ `BanquetEvent`; hủy sự kiện vẫn là chuyển trạng thái `Cancelled`, không hard-delete.

Ngoài phạm vi: các tab Báo cáo, Cài đặt, Timeline, Checklist và trường chưa có UI/không cần ngay (`GuestConfirmedCount`, `TableConfirmedCount`, `SetupTime`, `TeardownTime`). Các trường này không được thêm chỉ vì báo cáo đề xuất.

## B. Đối chiếu source trước khi sửa

| Field | UI | ViewModel | Save DTO | Response DTO | Entity | DB | Kết luận |
|---|---|---|---|---|---|---|---|
| BrideName, GroomName | Có | Có | Không | Không | Không | Không | Cần bổ sung |
| ConferenceName | Có | Có | Không | Không | Không | Không | Cần bổ sung |
| BirthdayPersonName, BirthdayDate | Có | Có | Không | Không | Không | Không | Cần bổ sung |
| MeetingTopic, PartyTheme, OtherEventName | Có | Có | Không | Không | Không | Không | Cần bổ sung |
| ExtraGuestCount, ExtraTableCount | Có | Có | Không | Không | Không | Không | Cần bổ sung |
| GuestConfirmedCount, TableConfirmedCount | Không | Không | Không | Không | Không | Không | Không thuộc Giai đoạn 1 |
| SetupTime, TeardownTime | Không | Không | Không | Không | Không | Không | Không thuộc Giai đoạn 1 |

Các quan hệ hiện hành trong `AppDbContext`: menu, dịch vụ, hợp đồng, cọc, checklist và timeline đều đang `Cascade`. Báo cáo đúng về rủi ro với `BanquetContract` và `BanquetDeposit`; Giai đoạn 1 chỉ đổi hai quan hệ tài chính này sang `Restrict`. Quan hệ cọc với hợp đồng đã là `NoAction`.

`Customer.Email`, `Customer.TaxCode` và `Customer.CreatedAt` hiện là `[NotMapped]`; `Customer.UpdatedAt` chưa tồn tại. Tab hiện lưu trực tiếp `CustomerName`/`Phone` vào `BanquetEvent`, truyền `CustomerId` nếu có nhưng không tìm/tạo/cập nhật `Customer`; vì vậy không có nguy cơ tạo trùng Customer từ luồng hiện tại, nhưng cũng không có tái sử dụng khách theo số điện thoại.

## C. File dự kiến sửa

- `ASMILE.BO.Core/Entities/RestaurantEntities.cs`
- `ASMILE.BO.Application/DTOs/RestaurantDtos.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Services/RestaurantServices.cs`
- `ASMILE.BO.App/ViewModels/RestaurantViewModels.cs`
- `ASMILE.BO.App/ViewModels/BanquetRestaurantViewModel.Partial.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/<timestamp>_CompleteBanquetOverviewEventFields.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

## D. Thứ tự thay đổi

1. Entity và EF configuration.
2. DTO request/response và service mapping/validation.
3. ViewModel save/load/reset/dirty-state/double-save.
4. Tạo và kiểm tra migration riêng.
5. Build, test và chạy checklist trong phạm vi.

## E. Rủi ro và kiểm soát

- Migration chỉ thêm cột nullable cho dữ liệu đặc thù và default `0` cho số phát sinh; không đổi tên/xóa cột.
- Không áp dụng migration vào database vì chưa xác nhận connection string là môi trường development.
- Dữ liệu cũ nhận `null` cho trường đặc thù; UI phải chịu được null.
- DTO thêm property tương thích ngược; service kiểm tra type-specific ở backend.
- Đổi FK hợp đồng/cọc có thể thất bại khi migration nếu database có dữ liệu không nhất quán; migration phải drop/recreate đúng hai FK.
- Không thay kiến trúc Customer trong giai đoạn này; việc chuẩn hóa email/tax code/customer lookup cần một hạng mục riêng.
- Kiểm tra overlap chỉ dùng khoảng thời gian sự kiện; setup/teardown buffer là TODO nghiệp vụ, chưa đưa vào Giai đoạn 1.
