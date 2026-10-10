# RESTAURANT_ALACARTE_COMPLETION_REPORT

Ngày kiểm tra: 18/06/2026

## 1. Tóm tắt đã sửa

Đã hoàn thiện các lỗi P0/P1/P2 chính của module Nhà hàng À La Carte ở mức POS vận hành cơ bản:

- Sửa binding runtime `SelectedOrderTableDisplayName` sang `Mode=OneWay`.
- Sửa lưu báo giá để không lưu FK phòng/bàn giả khi ID không tồn tại trong database.
- Bọc lỗi UI/DB qua `RunBusyAsync`, có log chi tiết bằng Serilog và `StatusText` thân thiện.
- Bổ sung seed permission `restaurant.alacarte.view` và `restaurant.alacarte.manage`.
- Chặn mở đơn trên bàn khóa.
- Reuse đơn mở hiện có theo bàn.
- Validate đặt bàn thật: khách, số điện thoại, số khách, sức chứa, bàn khóa/đang phục vụ, trùng giờ.
- Thanh toán đơn thật: chặn đơn rỗng, set `Paid`, cập nhật tiền và trạng thái bàn.
- Hủy đơn thật: chặn đơn đã thanh toán, set `Cancelled`, tạo phiếu hủy cho món đã gửi bếp/bar.
- Thêm món từ panel phải vào đơn thật.
- Dịch vụ bổ sung đọc/ghi database, có seed mẫu qua nút khởi tạo.
- Hợp đồng À La Carte đọc/ghi database cơ bản.
- Bổ sung test nghiệp vụ bắt buộc cho À La Carte.
- Ổn định test scheduler flaky để `dotnet test` pass toàn bộ.

## 2. File đã sửa

- `ASMILE.BO.Application/DTOs/RestaurantDtos.cs`
- `ASMILE.BO.Application/Interfaces/IRestaurantServices.cs`
- `ASMILE.BO.Infrastructure/Services/RestaurantServices.cs`
- `ASMILE.BO.Infrastructure/Services/PermissionService.cs`
- `ASMILE.BO.Infrastructure/Security/SecuritySeedService.cs`
- `ASMILE.BO.App/ViewModels/RestaurantViewModels.cs`
- `ASMILE.BO.App/Views/AlacarteRestaurantView.xaml`
- `ASMILE.BO.Tests.Unit/RestaurantModuleTests.cs`
- `ASMILE.BO.Tests.Unit/BackgroundSchedulerServiceTests.cs`

Ghi chú: workspace hiện tại không có thư mục `.git`, nên không lấy được `git diff/stat`.

## 3. P0 đã sửa gì

- `SelectedOrderTableDisplayName`:
  - Hai binding trong `AlacarteRestaurantView.xaml` đã ép `Mode=OneWay`.

- FK báo giá:
  - `SaveQuoteAsync` kiểm tra `DiningRoomId` và `DiningTableId` tồn tại thật trước khi gán FK.
  - Nếu ID không tồn tại, service để FK `null` và giữ `RoomName` dạng text.
  - Có validate tiền cọc không âm và không vượt tổng báo giá.

- Bọc lỗi:
  - `RunBusyAsync` ghi log lỗi bằng Serilog và hiển thị `StatusText`.
  - Các flow refresh, save quote, save quote details, approve quote, bootstrap sample dùng luồng an toàn hơn.

- Permission:
  - Seed thêm `restaurant.alacarte.view/manage`.
  - Bổ sung legacy map sang `Sales.View` và `Sales.Edit`.
  - `NavigationService.GetPermissionCode()` đã có map `restaurant-alacarte => restaurant.alacarte.view`.

## 4. P1 đã sửa gì

- `OpenOrderAsync`:
  - Chặn bàn `Locked`.
  - Nếu bàn đã có đơn chưa `Paid/Cancelled`, trả về đơn hiện có.

- Đặt bàn:
  - Thêm `ValidateReservationAsync`.
  - Chặn thiếu khách/SĐT, số khách <= 0, quá sức chứa, bàn khóa, bàn đang phục vụ, trùng bàn trong khoảng 2 giờ.

- Thanh toán:
  - Thêm `PayOrderAsync`.
  - Chặn đơn rỗng, đơn đã thanh toán, đơn đã hủy.
  - Set `Status=Paid`, `PaymentStatus=Paid`, `RemainingAmount=0`, cập nhật bàn về `Cleaning`.

- Hủy đơn:
  - Thêm `CancelOrderAsync`.
  - Chặn đơn đã thanh toán.
  - Không xóa order khỏi DB.
  - Nếu có món đã gửi bếp/bar, tạo ticket hủy và đánh dấu line cancelled.
  - Cập nhật bàn về `Empty` hoặc `Reserved`.

- Thêm món:
  - Panel phải có danh sách món nhanh và nút thêm.
  - Service cộng dồn line chưa gửi thay vì tạo trùng.
  - Nếu đơn đã gửi bếp/bar, món thêm mới có `LineType=Supplemental`.

- Dịch vụ bổ sung:
  - Thêm DTO/request/service DB-backed.
  - Seed mẫu: Màn LED, MC, Ca sĩ, Âm thanh, Ánh sáng, Trang trí, Hoa tươi, Máy chiếu, Phí phòng VIP, Phí phục vụ, Photobooth.
  - Panel phải có nút thêm dịch vụ vào đơn.

- Hợp đồng:
  - Thêm service đọc/ghi hợp đồng DB-backed.
  - Có tạo hợp đồng từ báo giá ở tầng service.

- Đặt cọc:
  - Chặn tiền cọc âm.
  - Chặn tổng cọc vượt tổng đơn/hợp đồng ở `AddDepositAsync`.

## 5. P2 đã sửa gì

- Empty state có thêm property bool hỗ trợ: `HasRooms`, `HasTables`, `HasReservations`, `HasOpenOrders`, `HasContracts`, `HasServices`.
- Panel phải nối thao tác thật cho thêm món, thêm dịch vụ, thanh toán, hủy.
- Dịch vụ và hợp đồng không còn phụ thuộc sample/local khi refresh.
- Nút khởi tạo dữ liệu mẫu hiện seed dịch vụ bổ sung vào DB.

## 6. Logic nào đã chạy thật

- Mở đơn theo bàn.
- Reuse đơn mở theo bàn.
- Thêm món vào đơn.
- Thêm dịch vụ vào đơn.
- Gửi bếp/bar giữ logic chỉ gửi line chưa gửi.
- Hủy đơn và tạo phiếu hủy cho line đã gửi.
- Thanh toán đơn.
- Lưu báo giá an toàn FK.
- Duyệt báo giá tạo reservation/order.
- Seed và load dịch vụ bổ sung từ DB.
- Load/save hợp đồng cơ bản từ DB.

## 7. Logic nào còn placeholder

- Posting kế toán/công nợ thật chưa thực hiện theo yêu cầu an toàn.
- `PaidAt` không được set vì entity hiện không có field riêng; service dùng `UpdatedAt` và trạng thái thanh toán.
- In tạm tính vẫn dùng luồng hiện có, chưa thiết kế lại sâu trong lần sửa này.
- Cấu hình POS nâng cao ở tab Cài đặt vẫn có phần cục bộ/chưa lưu DB đầy đủ.

## 8. Dữ liệu nào DB thật

- `DiningRooms`
- `DiningTables`
- `TableReservations`
- `AlacarteOrders`
- `AlacarteOrderLines`
- `RestaurantOrderExtraServices`
- `RestaurantExtraServices`
- `RestaurantContracts`
- `RestaurantDeposits`
- `RestaurantQuotes`
- `RestaurantPreparationTickets`

## 9. Dữ liệu nào sample/local

- Một số settings POS hiển thị trong tab Cài đặt vẫn là collection cục bộ.
- `PopulateSampleDataInMemory()` vẫn còn trong ViewModel để fallback khi DB trống, nhưng nút khởi tạo hiện không ghi phòng/bàn demo tự động vào DB.

## 10. Migration có tạo mới không

Không tạo migration mới.

Lý do: các bảng cần dùng cho scope này đã tồn tại trong schema hiện tại. Không sửa migration cũ.

## 11. Permission đã seed ở đâu

- `ASMILE.BO.Infrastructure/Services/PermissionService.cs`
  - `DefaultPermissions()`
  - `LegacyPermissionMap`

- `ASMILE.BO.Infrastructure/Security/SecuritySeedService.cs`
  - `DefaultPermissions()`

- Navigation đã có:
  - `restaurant-alacarte => restaurant.alacarte.view`

## 12. Build result

Đã chạy:

```text
dotnet build
```

Kết quả:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## 13. Test result

Đã chạy test module:

```text
dotnet test --filter RestaurantModuleTests
```

Kết quả:

```text
Passed: 17, Failed: 0
```

Đã chạy full suite:

```text
dotnet test
```

Kết quả cuối:

```text
Integration: Passed 11, Failed 0
Unit: Passed 150, Failed 0
```

Ghi chú: lần chạy full suite đầu phát hiện test scheduler flaky khi chạy song song. Đã ổn định test bằng cách đợi execution chuyển `Running` trước khi cancel. Sau sửa, full suite pass.

## 14. Runtime log còn lỗi gì không

Chưa chạy app GUI trong báo cáo này trước khi ghi file. Build/test không còn lỗi compile/test.

Đã scan encoding các file liên quan bằng bộ pattern mojibake chuẩn của yêu cầu.

Kết quả: không có match trong các file vừa sửa.

## 15. Việc còn lại nếu có

- Nếu muốn vận hành thu tiền thật: cần thiết kế posting quỹ/ngân hàng/công nợ và quy tắc kế toán trước khi tự tạo bút toán.
- Nếu muốn quản trị cài đặt POS đầy đủ: cần DB-backed cho máy in, nhóm món bán, phân quyền thao tác manage, cấu hình đặt cọc/hóa đơn.
- Có thể bổ sung modal xác nhận khi thanh toán/hủy đơn để nhập lý do/phương thức thanh toán thay vì dùng mặc định.
