# ALACARTE VIP P0 IMPLEMENTATION REPORT

## 1. Đã thêm entity/field nào

### Entity mới
- `RestaurantQuoteTable`
- `RestaurantOrderTable`

### Field mới
- `RestaurantQuote`
  - `TableStandard`
  - `QuoteTables`
- `RestaurantQuoteLine`
  - `PricingRule`
  - `CalculatedQuantity`
- `AlacarteOrder`
  - `DiningRoomId`
  - `RestaurantQuoteId`
  - `ActualGuestCount`
  - `OrderType`
  - `OrderTables`
- `AlacarteOrderLine`
  - `RestaurantOrderTableId`
  - `PricingRule`
  - `IsSupplement`

## 2. Migration mới là gì

- `20260618095047_AddAlacarteVipP0QuoteAndOrderTables`

Migration này tạo:
- bảng `RestaurantQuoteTables`
- bảng `RestaurantOrderTables`
- cột `TableStandard`, `PricingRule`, `CalculatedQuantity`
- cột liên kết quote/order cho luồng VIP

## 3. Đã thêm service/engine nào

- `ITableAllocationEngine`
- `IMenuCalculationEngine`
- `TableAllocationEngine`
- `MenuCalculationEngine`

## 4. QuoteTable hoạt động thế nào

- Khi lưu báo giá, service gọi `TableAllocationEngine`.
- Engine luôn sinh:
  - `Bàn Tổng` với `IsVirtual = true`, `SortOrder = 0`
  - các bàn thật `Bàn 01`, `Bàn 02`...
- Rule P0 đã áp dụng:
  - `31 khách / chuẩn 10` => `Bàn Tổng + 10 + 10 + 11`
  - `46 khách / chuẩn 10` => `Bàn Tổng + 10 + 10 + 10 + 10 + 6`
  - `8 khách / chuẩn 10` => `Bàn Tổng + 8`
- Khi sửa `GuestCount` hoặc `TableStandard` trước duyệt, `QuoteTables` cũ được xóa và sinh lại.

## 5. PricingRule hoạt động thế nào

`RestaurantQuoteLine` đã có `PricingRule` thật và `CalculatedQuantity`.

P0 đang hỗ trợ 4 rule:
- `PerPerson`
- `PerTable`
- `PerRoom`
- `Manual`

Quy tắc tính:
- `PerPerson` => `GuestCount * DefaultQuantity`
- `PerTable` => `RealTableCount * DefaultQuantity`
- `PerRoom` => `DefaultQuantity`
- `Manual` => `ManualQuantity`, nếu không có thì fallback `DefaultQuantity`

## 6. Approve quote tạo OrderTable thế nào

Khi duyệt báo giá:
- Quote phải chưa duyệt
- Quote phải có `QuoteTables`
- Service tạo `AlacarteOrder`
- Service tạo `RestaurantOrderTables` từ `RestaurantQuoteTables`
  - `Bàn Tổng` giữ `IsVirtual = true`
  - các bàn thật giữ `IsVirtual = false`
- `AlacarteOrder` được gắn:
  - `RestaurantQuoteId`
  - `DiningRoomId`
  - `ActualGuestCount`
  - `OrderType = "Advance"`

Phân bổ line P0:
- `PerPerson`: tách theo từng bàn thật, số lượng theo số khách từng bàn
- `PerTable`: tách theo từng bàn thật, mỗi bàn lấy `DefaultQuantity`
- `PerRoom`: gắn vào `Bàn Tổng`
- `Manual`: mặc định gắn vào `Bàn Tổng`

## 7. UI đã hiển thị được gì

### Tab Báo giá
- Có `GuestCount`
- Có `TableStandard`
- Sau khi lưu báo giá, hiển thị danh sách `Bàn Tổng` và các bàn thật
- Dòng món/dịch vụ hiển thị:
  - `PricingRule`
  - số lượng gốc
  - số lượng tính ra

### Tab DS Báo giá
- Khi chọn báo giá, hiển thị:
  - phòng
  - số khách
  - chuẩn bàn
  - danh sách bàn đã chia
  - `PricingRule`
  - `CalculatedQuantity`

### Tab Đơn mở
- Khi đơn tạo từ báo giá được mở, panel dữ liệu dùng `OrderTables` thật
- Có thể thấy `Bàn Tổng` và các bàn thật

## 8. Test kết quả

### Test mới/đã cover
- `TableAllocationEngine`
  - `31 / 10`
  - `46 / 10`
  - `8 / 10`
  - `guestCount <= 0`
- `MenuCalculationEngine`
  - `PerPerson`
  - `PerTable`
  - `PerRoom`
  - `Manual`
- Quote flow
  - save quote tạo `QuoteTables`
  - save quote tính `CalculatedQuantity`
  - sửa guest count trước duyệt sẽ recalculation
  - approve quote tạo `OrderTables`
  - approve quote tạo `Bàn Tổng`
- Regression
  - `RestaurantModuleTests` pass
  - full unit/integration suite pass

### Kết quả chạy thật
- `dotnet test ASMILE.BO.Tests.Unit/ASMILE.BO.Tests.Unit.csproj --filter RestaurantModuleTests`
  - Passed `28/28`
- `dotnet test ASMILE.BO.sln`
  - Integration: Passed `11/11`
  - Unit: Passed `161/161`

## 9. Build kết quả

- `dotnet build ASMILE.BO.sln`
  - Passed
  - `0 Warning(s)`
  - `0 Error(s)`

## 10. Những gì chưa làm và để P1/P2

### Chưa làm trong P0
- Calendar / timeline / room schedule
- Menu template / room package
- workflow nhiều trạng thái đầy đủ
- inventory / accounting posting
- drag-drop bàn
- custom allocation strategy ngoài `MinTables`
- UI polish Win11 sâu hơn cho phần VIP
- command chỉnh tay lại phân bổ line theo từng bàn sau approve

### P1 hợp lý tiếp theo
- command sửa phân bổ bàn sau lưu quote
- UI thao tác rõ hơn cho `PricingRule`
- phân bổ line theo bàn có thể chỉnh tay
- khóa sửa quote sau khi order đã phát sinh phục vụ thực tế

### P2
- lịch phòng VIP
- workflow nhiều bước
- template menu / gói phòng
- màn hình điều phối phục vụ theo từng bàn thật

## Ghi chú kỹ thuật

- Trong lúc tạo migration, EF vẫn sinh migration thành công nhưng có cảnh báo khởi tạo host do một số registration runtime không phù hợp cho design-time service provider. Không ảnh hưởng tới migration vừa tạo.
- Quét encoding trên các file đã sửa bằng mẫu:
  - `Ã`
  - `Ä`
  - `á»`
  - `Táº`
  - `Náº`
  - `Há»`
  - `CÃ`
  - Kết quả: không phát hiện trong các file vừa sửa.
