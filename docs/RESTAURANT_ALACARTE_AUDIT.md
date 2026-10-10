# Audit Module Nhà Hàng À La Carte

Ngày kiểm tra: 18/06/2026  
Phạm vi: chỉ phân tích, không sửa code nghiệp vụ.

## 1. Tổng Quan Trạng Thái Module

Kết luận: **chưa đạt để dùng thực tế đầy đủ**.

Module đã có nền tảng thật gồm entity, migration, DbContext mapping, service cho phòng/bàn/đặt bàn/đơn mở/dòng món/gửi bếp-bar/hủy món/báo giá/duyệt báo giá. UI đã có phong cách POS và có nhiều binding thật.

Rủi ro chính:
- P0: log runtime đang ghi lỗi binding `SelectedOrderTableDisplayName` dạng TwoWay vào property chỉ đọc.
- P0: log runtime đang ghi lỗi FK khi lưu báo giá: `FK_RestaurantQuotes_DiningRooms_DiningRoomId`.
- P1: permission catalog chưa seed quyền `restaurant.alacarte.view/manage` dù navigation yêu cầu `restaurant.alacarte.view`.
- P1: thanh toán/hủy đơn/in tạm tính/thêm món trực tiếp trong đơn mở còn là placeholder trong ViewModel.
- P1: đặt bàn chưa validate trùng giờ/trùng bàn/quá sức chứa.
- P1: nhiều cấu hình, hợp đồng và dịch vụ bổ sung vẫn là local/in-memory hoặc file JSON, chưa đồng bộ CSDL đầy đủ.

## 2. Danh Sách File Đã Kiểm Tra

- `ASMILE.BO.Core/Entities/RestaurantEntities.cs`
- `ASMILE.BO.Application/DTOs/RestaurantDtos.cs`
- `ASMILE.BO.Application/Interfaces/IRestaurantServices.cs`
- `ASMILE.BO.Infrastructure/Services/RestaurantServices.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260614055304_AddRestaurantAlacarteAndBanquetModules.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260617031602_AddRestaurantQuotes.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260617043114_AddRestaurantQuoteGuestCount.cs`
- `ASMILE.BO.App/ViewModels/RestaurantViewModels.cs`
- `ASMILE.BO.App/Views/AlacarteRestaurantView.xaml`
- `ASMILE.BO.App/Views/AlacarteRestaurantView.xaml.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
- `ASMILE.BO.Tests.Unit/RestaurantModuleTests.cs`
- Runtime log: `ASMILE.BO.App/bin/Release/net8.0-windows/logs/asmile-bo-20260618.log`

## 3. Logic Nghiệp Vụ

Đúng/đã có:
- `DiningRoom`, `DiningTable`, `TableReservation`, `AlacarteOrder`, `AlacarteOrderLine`, `RestaurantExtraService`, `RestaurantContract`, `RestaurantDeposit` đã có entity.
- Service có tạo/sửa phòng và bàn, kiểm tra trùng `Code`, kiểm tra sức chứa/số ghế > 0.
- Mở đơn có chặn 1 bàn có nhiều đơn mở: `OpenOrderAsync` trả về đơn hiện tại nếu bàn còn order chưa `Paid/Cancelled`.
- Thêm món lấy từ `MenuItems` thật, chỉ lấy món `IsActive`.
- Gửi bếp/bar chỉ gửi dòng chưa `Sent`; phân bar theo `MenuItem.PreparationArea` chứa `bar`.
- Hủy dòng đã gửi có thể tạo `RestaurantPreparationTicket` loại `Cancel`.
- Báo giá được lưu vào DB qua `RestaurantQuotes/RestaurantQuoteLines`.
- Duyệt báo giá tạo `TableReservation`, `AlacarteOrder`, có tạo cọc/cash receipt nếu có service quỹ.

Sai/thiếu:
- Chưa có command/service xóa/khóa phòng; chưa chặn xóa phòng nếu còn bàn/đơn/đặt bàn vì chưa có luồng xóa.
- Mở đơn chưa chặn bàn khóa (`DiningTable.Status == Locked`) trong service.
- Đặt bàn chưa cảnh báo trùng giờ/trùng bàn, chưa cảnh báo quá sức chứa phòng/bàn.
- Click bàn trống ở tab phòng hiện mở đơn luôn, không có trạng thái “chưa có đơn” để xác nhận.
- Thanh toán đơn chưa có service/command thật. `PayOrder()` chỉ set `StatusText`.
- Hủy đơn chưa có service/command thật. `CancelOrder()` chỉ set `StatusText`.
- In tạm tính chưa có implementation thật trong command `PrintProvisional()`.
- `AddMenuItem()` ở panel phải chưa thêm món vào đơn, chỉ set `StatusText`.
- Phí phục vụ/giảm giá/VAT ở order có field nhưng UI chưa có workflow chỉnh/saved đầy đủ cho đơn mở.
- Dịch vụ bổ sung có entity, nhưng ViewModel đang dùng danh sách local `_defaultServices`; thiếu service CRUD/get thật cho `RestaurantExtraServices`.
- Hợp đồng À La Carte trong UI là sample/local (`PopulateSampleContracts`), chưa có service tạo/sửa hợp đồng thật.
- Cọc có service `AddDepositAsync`, nhưng duyệt báo giá tạo cọc `Confirmed` và cash receipt thật; chưa rõ đúng yêu cầu “nháp quỹ”.
- Quote approve dùng `DiningRoomId` từ UI; log thực tế có lỗi FK khi ID phòng không tồn tại trong DB.

## 4. UI/UX

Đúng/đã có:
- UI không phải DataGrid toàn màn hình; có POS shell, sidebar, KPI, card phòng/bàn, quote builder, DS báo giá, panel phải.
- Navigation tab hoạt động qua `NavigateTabCommand`.
- Tab báo giá có 3 vùng: thông tin khách, chọn món/dịch vụ/menu, tổng báo giá.
- Tab đơn mở có danh sách đơn và bàn của phòng, bấm bàn cập nhật ngữ cảnh panel phải.
- DS báo giá có tag trạng thái `Chờ duyệt/Đã duyệt`, chia món/dịch vụ.

Sai/thiếu:
- Tab `Dịch vụ` và `Cài đặt` có nhiều dữ liệu in-memory, chưa phải dữ liệu DB thật.
- Tab hợp đồng/DS báo giá có phần thật cho quotes, nhưng hợp đồng nhà hàng còn mock/sample.
- Một số nút nghiệp vụ quan trọng là placeholder: thanh toán, hủy đơn, in tạm tính, thêm món từ panel phải.
- Một số Empty State dùng binding `Count` với `BooleanToVisibilityConverter`; với converter mặc định, số khác 0/0 không phải boolean rõ ràng, có nguy cơ hiển thị sai.
- UI có nhiều DataGrid trong tab cài đặt, chưa đồng bộ hoàn toàn với style POS/card.
- Layout tổng thể build được, nhưng chưa có test UI tự động cho màn hình nhỏ/lớn.

Tab dữ liệu thật:
- Tổng quan: lấy DB qua `GetDashboardAsync`, sau đó ViewModel có thể override bằng `UpdateDashboardFromLocal`.
- Khu vực/phòng bàn: DB qua `GetRoomsAsync/GetTablesAsync`.
- Báo giá: menu DB qua `IMenuCatalogService`, quote DB qua `RestaurantQuotes`.
- Đơn mở: DB qua `GetOpenOrdersAsync`.
- DS báo giá: DB qua `GetQuotesAsync`.

Tab/mock/local:
- Dịch vụ: `_defaultServices` local.
- Hợp đồng mẫu/alerts/settings: phần lớn in-memory hoặc JSON local.
- Thông tin nhà hàng: lưu file `Data/alacarte_restaurant_settings.json`, không phải DB.

## 5. Binding

Binding/command đúng đã thấy:
- `NavigateTabCommand`, `RefreshCommand`, `BootstrapSampleDataCommand`, `OpenRoomOrderCommand`, `SelectOpenOrderCommand`, `SelectOpenOrderTableCommand`.
- `Rooms`, `FilteredRooms`, `Tables`, `Reservations`, `OpenOrders`, `SavedQuotes`, `QuoteLines`, `PagedQuoteMenuItems`, `AlacarteServices`.
- `SelectedOrderFoodLines`, `SelectedOrderServiceLines`, `SelectedOrderRoomTables`, `SelectedOrderRoomTableCount`.

Vấn đề:
- Runtime log ngày 18/06/2026 có lỗi:
  - `A TwoWay or OneWayToSource binding cannot work on the read-only property 'SelectedOrderTableDisplayName'`
  - Nguồn: `ASMILE.BO.App/bin/Release/net8.0-windows/logs/asmile-bo-20260618.log`, dòng 255 và 284.
- Runtime log ngày 18/06/2026 có lỗi lưu báo giá:
  - `FK_RestaurantQuotes_DiningRooms_DiningRoomId`
  - Nguồn: cùng log, dòng 45-128 và 161-244.
- `SelectedOrderTableDisplayName` là property chỉ đọc; binding hiện tại trong XAML cần ép `Mode=OneWay` hoặc chuyển sang property có setter nếu control nào đó suy luận TwoWay.
- `OpenRoomOrderAsync` chọn bàn đầu tiên của phòng, không chọn theo bàn cụ thể.
- `SelectedOrderRoomTables` sinh “Bàn Tổng” và bàn ảo từ quote; bấm từng bàn vẫn hiển thị cùng danh sách món/dịch vụ của cả order, chưa có chi tiết riêng từng bàn.
- `SaveRoomAsync()` trong ViewModel là `void` wrapper gọi `_ = SaveRoomImplAsync()` nên lỗi async có thể khó bắt ở command.
- Một số collection local (`Contracts`, `AlacarteServices`, `Alerts`, settings collections) không reload từ DB.

## 6. Database / EF Core

Đúng/đã có:
- DbSet đầy đủ: `DiningRooms`, `DiningTables`, `TableReservations`, `AlacarteOrders`, `AlacarteOrderLines`, `RestaurantExtraServices`, `RestaurantOrderExtraServices`, `RestaurantContracts`, `RestaurantDeposits`, `RestaurantQuotes`, `RestaurantQuoteLines`, `RestaurantPreparationTickets`.
- Decimal money có precision phù hợp: phần lớn `decimal(18,2)`, VAT rate `decimal(9,4)`, quantity `decimal(18,3)`.
- Index quan trọng đã có:
  - `DiningRooms.Code`, `DiningRooms.Status`
  - `DiningTables.Code`, `(DiningRoomId, SortOrder)`, `DiningTables.Status`
  - `TableReservations.ReservationNo`, `(DiningTableId, ReservationDateTime, Status)`, `Phone`
  - `AlacarteOrders.OrderNo`, `(DiningTableId, Status)`, `OrderDate`
  - `RestaurantQuotes.QuoteNo`, `(Status, EventDate)`, `ApprovedOrderId`
- DeleteBehavior phần FK nghiệp vụ chính đa số dùng `Restrict/SetNull/NoAction`, tránh cascade path nguy hiểm ở quan hệ cha ngoài order.

Rủi ro/vấn đề:
- `AlacarteOrder` cascade delete `Lines`, `ExtraServices`, `PreparationTickets`; nếu có xóa order thật, sẽ mất lịch sử vận hành.
- `RestaurantQuoteLines` cascade theo quote; hợp lý nếu quote bị xóa nhưng cần cân nhắc audit/lịch sử.
- Không có unique filtered index để đảm bảo “mỗi bàn chỉ có 1 đơn mở” ở DB; hiện chỉ đảm bảo bằng service.
- Chưa có index riêng `TableReservation.DiningRoomId + ReservationDateTime + Status` để cảnh báo theo phòng.
- Migration là EF migration chuẩn có `Up/Down`, không phải script idempotent riêng.
- `AddRestaurantQuoteGuestCount` thêm cột default 0 rồi update theo `TableCount * 10`; ổn cho dữ liệu cũ nhưng chỉ là suy đoán nghiệp vụ.

## 7. Navigation / Permission

Đúng/đã có:
- `DependencyInjection.cs` đăng ký `AlacarteRestaurantViewModel`.
- `DataTemplates.xaml` map `AlacarteRestaurantViewModel` sang `AlacarteRestaurantView`.
- `NavigationService.cs` có module `restaurant-alacarte`, dưới `menu-recipe`.
- `NavigationService.GetPermissionCode("restaurant-alacarte")` trả `restaurant.alacarte.view`.

Thiếu:
- `PermissionService.DefaultPermissions()` không có `restaurant.alacarte.view` hoặc `restaurant.alacarte.manage`.
- `SecuritySeedService.DefaultPermissions()` kiểu legacy cũng không có quyền restaurant.
- Chưa thấy quyền manage được dùng để ẩn/khóa các command tạo/sửa/xóa/duyệt.

## 8. Encoding Tiếng Việt

Lệnh quét source đã chạy:

```powershell
rg -n "Ã|Ä|á»|Táº|Náº|Há»|CÃ" <các file liên quan>
```

Kết quả: **không phát hiện mojibake trong các file source liên quan**.

Build cũng báo:

```text
Encoding check passed: no mojibake patterns found in XAML.
```

Ghi chú: một số output console trước đó có hiển thị méo tiếng Việt khi đọc bằng `Get-Content`, nhưng quét source bằng `rg` không xác nhận lỗi trong file nguồn.

## 9. Build / Test

Đã chạy thật:

```powershell
dotnet build
dotnet test
```

Kết quả:
- Build: succeeded, 0 warning, 0 error.
- Encoding check XAML: passed.
- Test integration: 11 passed.
- Test unit: 138 passed.
- Tổng test: 149 passed, 0 failed.

Ghi chú: build/test không bắt được lỗi runtime binding và lỗi FK đã ghi trong log release.

## 10. Kế Hoạch Sửa Theo Ưu Tiên

### P0 - Lỗi nghiêm trọng cần sửa ngay

1. Sửa binding `SelectedOrderTableDisplayName` để không tạo TwoWay binding vào property chỉ đọc.
2. Sửa luồng lưu báo giá gây FK `RestaurantQuotes.DiningRoomId` khi `SelectedRoom.Id` không tồn tại trong DB.
3. Bọc `SaveQuoteAsync`, `SaveSelectedQuoteDetailsAsync`, `ApproveQuoteAsync` bằng `RunBusyAsync/try-catch` để lỗi DB không thành unhandled UI exception.
4. Bổ sung seed permission `restaurant.alacarte.view` và `restaurant.alacarte.manage`.

### P1 - Lỗi nghiệp vụ

1. Implement thanh toán đơn thật: không thanh toán đơn rỗng, set `Paid`, cập nhật `PaidAmount/RemainingAmount`, giải phóng bàn/phòng đúng trạng thái.
2. Implement hủy đơn thật: không hủy đơn đã thanh toán, lưu lý do, cập nhật bàn/phòng.
3. Chặn mở đơn cho bàn `Locked`.
4. Validate đặt bàn trùng bàn/trùng giờ/quá sức chứa.
5. Tạo service CRUD thật cho `RestaurantExtraServices`.
6. Làm hợp đồng À La Carte DB-backed thay vì sample/local.
7. Chuẩn hóa cọc: xác định tạo phiếu thu nháp hay phiếu thu confirmed, liên kết order/quote/reservation rõ ràng.
8. Thêm DB-level guard hoặc transaction lock cho “mỗi bàn chỉ 1 đơn mở”.

### P2 - UI/UX

1. Rà soát Empty State dùng `Count` với converter boolean.
2. Hoàn thiện panel phải: thêm món/dịch vụ vào đơn mở thật, không chỉ xem.
3. Bàn trong phòng nếu là bàn ảo từ báo giá cần thể hiện rõ “cùng đơn/tổng đơn” hoặc có phân bổ chi tiết từng bàn.
4. Làm tab dịch vụ/cài đặt theo Win11 card/table chung, giảm DataGrid thô.
5. Thêm xác nhận khi click bàn đang phục vụ/đang đặt.

### P3 - Polish

1. Thống nhất trạng thái tiếng Việt/tiếng Anh: `Serving`, `Reserved`, `Paid`, `Cancelled` vs label tiếng Việt.
2. Thêm test cho quote approval, FK room/table, permission navigation.
3. Thêm test cho thanh toán/hủy đơn sau khi implement.
4. Thêm audit log cho thao tác duyệt báo giá, hủy đơn, thanh toán.

## 11. Prompt Sửa Tiếp Theo

```text
ASMILE-BO WPF/.NET PROJECT

NHIỆM VỤ:
Sửa các lỗi P0/P1 trong RESTAURANT_ALACARTE_AUDIT.md cho module Nhà hàng À La Carte.

YÊU CẦU:
- Không làm lỗi encoding tiếng Việt.
- Không sửa migration cũ.
- Không xóa dữ liệu thật.
- Không dùng file backup.
- Build/test phải pass.

PHẠM VI SỬA ƯU TIÊN:
1. Sửa binding SelectedOrderTableDisplayName để không phát sinh lỗi TwoWay vào property chỉ đọc.
2. Sửa SaveQuoteAsync/ToSaveQuoteRequest để không truyền DiningRoomId/DiningTableId không tồn tại trong DB; nếu chỉ có room name từ local thì lưu RoomName, để FK null.
3. Bọc SaveQuoteAsync, SaveSelectedQuoteDetailsAsync, ApproveQuoteAsync bằng error handling để không Unhandled UI exception.
4. Seed quyền restaurant.alacarte.view và restaurant.alacarte.manage vào permission catalog hiện tại.
5. Chặn OpenOrderAsync với bàn Locked.
6. Thêm validate đặt bàn trùng giờ/trùng bàn/quá sức chứa trong service.
7. Implement thanh toán/hủy đơn thật ở service + ViewModel: không thanh toán đơn rỗng, không hủy đơn đã paid, cập nhật trạng thái bàn/phòng.

SAU KHI SỬA:
- Chạy dotnet build
- Chạy dotnet test
- Ghi log kết quả vào báo cáo sửa lỗi
- Mở lại app Release cho kiểm tra
```
