# RESTAURANT_ARCHITECTURE

## Mục tiêu

Thiết kế hai module mới theo hướng additive:

- `Nhà hàng À La Carte`
- `Nhà hàng Tiệc / Banquet`

Module mới dùng `Bán đồ ăn sáng & cà phê` làm chuẩn UI/UX và luồng POS, nhưng không sửa phá module hiện có.

## Nguyên tắc kiến trúc

- Không sửa migration cũ.
- Không tự seed dữ liệu demo vào database thật.
- Không tạo bút toán kế toán thật ở giai đoạn này.
- Không xóa/sửa dữ liệu hiện hữu.
- Entity mới tách namespace logic restaurant, nhưng dùng lại shared catalog.
- Các FK sang danh mục dùng `Restrict`, `SetNull` hoặc `NoAction`.
- Cascade chỉ dùng trong aggregate nội bộ như contract lines, deposits, checklist lines nếu không tạo multiple cascade path.
- Text file giữ UTF-8.

## Shared Layer

Shared layer là các module/dữ liệu đã có và được dùng chung.

### Customer

Nguồn: `Customer`

Dùng cho:

- Đặt bàn À La Carte.
- Order À La Carte.
- Hợp đồng nhà hàng.
- Sự kiện tiệc.
- Hợp đồng tiệc.
- Công nợ phải thu ở mức điểm tích hợp.

Quan hệ đề xuất:

- `CustomerId` nullable.
- Delete behavior: `SetNull` hoặc `NoAction`.

### Menu

Nguồn:

- `MenuCategory`
- `MenuItem`

Dùng cho:

- Order line À La Carte.
- Banquet menu package.
- Banquet package line.
- Phân tuyến bếp/bar theo `MenuItem.PreparationArea`.

Không tạo danh mục món riêng nếu không cần. Nếu cần phân loại bán riêng cho từng module, dùng bảng mapping:

- `RestaurantSalesMenuScope`
  - `MenuItemId`
  - `ModuleScope`: Alacarte, Banquet, BreakfastCafe
  - `IsActive`

### Recipe

Nguồn:

- `MenuItemRecipe`
- `MenuItemRecipeLine`

Dùng cho:

- Kiểm tra tồn trước khi gửi bếp/bar.
- Tính chi phí ước tính.
- Trừ kho khi thanh toán hoặc khi quy trình vận hành xác nhận.

Giai đoạn đầu:

- À La Carte có thể kế thừa giống Breakfast Cafe: trừ khi paid.
- Banquet chỉ chuẩn bị điểm tích hợp, chưa tự động xuất kho thật nếu chưa có yêu cầu xác nhận vận hành.

### Inventory

Nguồn:

- `Item`
- `Warehouse`
- costing/stock services hiện có.

Dùng cho:

- Kiểm tra đủ nguyên liệu.
- Cảnh báo món không có định lượng.
- Cảnh báo vượt tồn.
- Chuẩn bị phiếu đề xuất xuất kho cho tiệc.

Không tự tạo phiếu xuất kho thật trong scope ban đầu.

### Tax

Nguồn:

- VAT/tax service hiện có.
- `MenuItem.VatRate`

Dùng cho:

- VAT order line.
- VAT hợp đồng/dịch vụ.
- Tổng tiền hợp đồng.

### Cash

Nguồn:

- `CashFund`
- `ICashFundService`

Dùng cho:

- Thu cọc tiền mặt.
- Thanh toán À La Carte tiền mặt.
- Điểm tích hợp phiếu thu.

Không tự ghi phiếu thu thật trong giai đoạn đầu.

### Bank

Nguồn:

- `BankAccount`
- `IBankingService`

Dùng cho:

- Thu cọc chuyển khoản.
- Thanh toán chuyển khoản/card.
- Đối soát về sau.

Không tự ghi giao dịch ngân hàng thật trong giai đoạn đầu.

### AR

Nguồn:

- Công nợ phải thu hiện có.

Dùng cho:

- Tính phải thu theo hợp đồng.
- Còn phải thu sau đặt cọc/thanh toán.
- Chuẩn bị route mở chứng từ công nợ.

Không tự tạo bút toán công nợ thật trong scope ban đầu.

### AP

Nguồn:

- Công nợ phải trả hiện có.

Dùng cho:

- Điểm tích hợp nhà cung cấp dịch vụ ngoài: âm thanh, ca sĩ, trang trí, quay phim.
- Giai đoạn đầu chỉ ghi metadata/ghi chú, không tạo AP transaction thật.

## Alacarte Layer

### Entity

`DiningRoom`

- `Id`
- `CompanyId`
- `BranchId`
- `Code`
- `Name`
- `RoomType`: Standard, VIP, Private, Garden, Outdoor
- `Capacity`
- `Description`
- `Status`: Available, Occupied, Reserved, Maintenance, Closed
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

`DiningTable`

- `Id`
- `CompanyId`
- `BranchId`
- `DiningRoomId`
- `Code`
- `Name`
- `SeatCount`
- `Status`: Empty, Reserved, Serving, Cleaning, Locked
- `Note`
- `IsActive`
- `SortOrder`
- `CreatedAt`
- `UpdatedAt`

`TableReservation`

- `Id`
- `CompanyId`
- `BranchId`
- `ReservationNo`
- `CustomerId`
- `CustomerName`
- `Phone`
- `ReservationDateTime`
- `GuestCount`
- `DiningRoomId`
- `DiningTableId`
- `Status`: Draft, Confirmed, CheckedIn, Completed, Cancelled, NoShow
- `Note`
- `CreatedAt`
- `UpdatedAt`

`AlacarteOrder`

- `Id`
- `CompanyId`
- `BranchId`
- `OrderNo`
- `DiningTableId`
- `CustomerId`
- `ReservationId`
- `OrderDate`
- `Status`: Draft, SentKitchen, Serving, AwaitingPayment, Paid, Cancelled
- `PaymentStatus`: Unpaid, PartiallyPaid, Paid
- `SubtotalAmount`
- `DiscountAmount`
- `VatAmount`
- `ServiceChargeAmount`
- `TotalAmount`
- `PaidAmount`
- `RemainingAmount`
- `KitchenSentAt`
- `Note`
- `CreatedBy`
- `CreatedAt`
- `UpdatedAt`

`AlacarteOrderLine`

- `Id`
- `CompanyId`
- `BranchId`
- `AlacarteOrderId`
- `MenuItemId`
- `LineNo`
- `MenuItemCode`
- `MenuItemName`
- `UnitName`
- `Qty`
- `UnitPrice`
- `VAT`
- `Discount`
- `Amount`
- `KitchenNote`
- `LineStatus`: Ordered, Cooking, Ready, Served, Cancelled
- `KitchenStatus`: New, Sent, Cancelled
- `LineType`: Normal, Supplemental, CancelNotice
- `SentAt`
- `CreatedAt`
- `UpdatedAt`

`RestaurantExtraService`

- `Id`
- `CompanyId`
- `BranchId`
- `Code`
- `Name`
- `ServiceType`: LedScreen, Sound, Lighting, MC, Singer, Band, Decoration, FreshFlower, Projector, ServiceCharge, VipRoomFee, Other
- `DefaultPrice`
- `VatRate`
- `IsActive`
- `Note`

`RestaurantOrderExtraService`

- `Id`
- `AlacarteOrderId`
- `RestaurantExtraServiceId`
- `Description`
- `Qty`
- `UnitPrice`
- `VatRate`
- `Amount`

`RestaurantDeposit`

- `Id`
- `CompanyId`
- `BranchId`
- `DepositNo`
- `OrderId`
- `ContractId`
- `ReservationId`
- `Date`
- `Amount`
- `PaymentMethod`
- `CashFundId`
- `BankAccountId`
- `Receiver`
- `Note`
- `Status`: Draft, Confirmed, Cancelled

`RestaurantContract`

- `Id`
- `CompanyId`
- `BranchId`
- `ContractNo`
- `CustomerId`
- `EventDescription`
- `TotalAmount`
- `DepositAmount`
- `RemainingAmount`
- `Status`: Draft, Signed, InProgress, Completed, Cancelled
- `SignedDate`
- `Note`

`RestaurantPreparationTicket`

- Header phiếu bếp/bar/hủy/bổ sung.
- Dùng chung cho À La Carte.

`RestaurantPreparationTicketLine`

- Chi tiết món in theo từng phiếu để audit và in lại chính xác.

### Service

`IAlacarteRestaurantService`

- Room/table CRUD.
- Reservation CRUD/check-in/no-show/cancel.
- Open order.
- Add/update/remove order line.
- Send new lines to kitchen/bar.
- Send cancel ticket.
- Add/remove extra service.
- Apply discount/service charge.
- Add deposit.
- Create/update contract.
- Pay order.
- Get dashboard.
- Get open orders.
- Merge/split/change table.

### UI

Route: `restaurant-alacarte`

Tabs đề xuất:

- Tổng quan.
- Phòng/Bàn.
- Đặt bàn.
- Thực đơn.
- Đơn mở.
- Hợp đồng/Cọc.
- Đơn đóng.
- Báo cáo.
- Cài đặt.

Bố cục kế thừa Breakfast Cafe:

- Header KPI.
- Mini sidebar trái.
- Trung tâm card/list.
- Sidebar phải là order hiện tại.
- Preview phiếu trước khi in.
- Gửi Kitchen/Bar theo preparation area.

## Banquet Layer

### Entity

`BanquetRoom`

- `Id`
- `CompanyId`
- `BranchId`
- `Code`
- `Name`
- `Capacity`
- `MaxTables`
- `Description`
- `Status`: Available, Reserved, Running, Maintenance, Closed
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

`BanquetEvent`

- `Id`
- `CompanyId`
- `BranchId`
- `EventNo`
- `EventType`: Wedding, Birthday, Conference, Gala, Meeting, Anniversary, Other
- `CustomerId`
- `CustomerName`
- `Phone`
- `EventDate`
- `StartTime`
- `EndTime`
- `GuestCount`
- `TableCount`
- `BanquetRoomId`
- `Status`: Draft, Quotation, Deposited, Confirmed, Preparing, Running, Completed, Cancelled
- `Note`
- `CreatedAt`
- `UpdatedAt`

`BanquetMenuPackage`

- `Id`
- `CompanyId`
- `BranchId`
- `Code`
- `Name`
- `PricingMode`: PerTable, PerGuest
- `Price`
- `VatRate`
- `IsActive`
- `Note`

`BanquetMenuPackageLine`

- `Id`
- `BanquetMenuPackageId`
- `MenuItemId`
- `CourseType`: Appetizer, Main, Side, Dessert, Drink
- `LineNo`
- `Qty`
- `Note`

`BanquetEventMenu`

- `Id`
- `BanquetEventId`
- `BanquetMenuPackageId`
- `PricingMode`
- `TableCount`
- `GuestCount`
- `UnitPrice`
- `Amount`

`BanquetServiceLine`

- `Id`
- `CompanyId`
- `BranchId`
- `BanquetEventId`
- `ServiceType`: LedScreen, Sound, Lighting, Singer, MC, Video, Photo, Decoration, FreshFlower, Projector, Stage, Backdrop, Other
- `Description`
- `Qty`
- `UnitPrice`
- `VatRate`
- `Amount`
- `SupplierId`
- `Note`

`BanquetContract`

- `Id`
- `CompanyId`
- `BranchId`
- `ContractNo`
- `BanquetEventId`
- `SignDate`
- `CustomerId`
- `TotalAmount`
- `DepositAmount`
- `RemainingAmount`
- `Status`: Draft, Signed, InProgress, Completed, Cancelled
- `Note`

`BanquetDeposit`

- `Id`
- `CompanyId`
- `BranchId`
- `DepositNo`
- `BanquetEventId`
- `BanquetContractId`
- `DepositDate`
- `Amount`
- `PaymentMethod`
- `CashFundId`
- `BankAccountId`
- `Receiver`
- `Note`
- `Status`: Draft, Confirmed, Cancelled

Cho phép nhiều lần đặt cọc theo `BanquetEventId` hoặc `BanquetContractId`.

`BanquetChecklist`

- `Id`
- `BanquetEventId`
- `Title`
- `IsDone`
- `DoneAt`
- `DoneBy`
- `SortOrder`
- `Note`

Default checklist do service tạo khi tạo event nếu người dùng chọn, không seed database thật.

Các mục gợi ý:

- Đã chốt thực đơn.
- Đã nhận cọc.
- Đã xác nhận khách.
- Đã chuẩn bị phòng.
- Đã chuẩn bị âm thanh.
- Đã chuẩn bị màn LED.
- Đã chuẩn bị nhân sự.
- Đã chuẩn bị sân khấu.
- Đã hoàn tất thanh toán.

`BanquetFloorPlan`

- `Id`
- `BanquetEventId`
- `Name`
- `CanvasWidth`
- `CanvasHeight`
- `UpdatedAt`

`BanquetFloorPlanTable`

- `Id`
- `BanquetFloorPlanId`
- `TableNo`
- `TableType`: Round, Long, VIP
- `SeatCount`
- `X`
- `Y`
- `Width`
- `Height`
- `Rotation`
- `Note`

`BanquetTimeline`

- `Id`
- `BanquetEventId`
- `StartTime`
- `Title`
- `Description`
- `ResponsiblePerson`
- `SortOrder`
- `Status`: Planned, Running, Done, Cancelled

### Service

`IBanquetRestaurantService`

- Room CRUD.
- Event CRUD.
- Validate calendar conflicts.
- Menu package CRUD.
- Attach menu package to event.
- Service line CRUD.
- Contract CRUD.
- Deposit multiple times.
- Checklist CRUD/toggle.
- Floor plan CRUD.
- Timeline CRUD.
- Dashboard.
- Calculate totals.
- Get calendar day/week/month.

### Calendar Rules

Khi tạo/sửa `BanquetEvent`, service phải kiểm tra:

- Trùng phòng: cùng `BanquetRoomId`.
- Trùng ngày và giao khoảng giờ.
- Vượt sức chứa: `GuestCount > BanquetRoom.Capacity`.
- Vượt số bàn: `TableCount > BanquetRoom.MaxTables`.
- Bỏ qua event đang `Cancelled`.

### UI

Route: `restaurant-banquet`

Tabs đề xuất:

- Tổng quan.
- Phòng tiệc.
- Lịch phòng.
- Sự kiện.
- Thực đơn tiệc.
- Dịch vụ.
- Hợp đồng.
- Đặt cọc.
- Checklist.
- Sơ đồ bàn.
- Timeline.
- Báo cáo.

Bố cục:

- Header KPI.
- Sidebar trái theo workflow sự kiện.
- Trung tâm là calendar/list/floor plan/timeline.
- Sidebar phải là chi tiết event và tổng tiền.
- Không dùng DataGrid toàn màn hình.

## Navigation

Thêm module ngay dưới `Món ăn & Định lượng`:

- `restaurant-alacarte`
  - Title: `Nhà hàng À La Carte`
  - Description: `Quản lý phòng ăn, bàn, đặt bàn, gọi món và thanh toán`
  - Permission: `restaurant.alacarte.view`

- `restaurant-banquet`
  - Title: `Nhà hàng Tiệc`
  - Description: `Quản lý phòng tiệc, sự kiện, hợp đồng, cọc, checklist và lịch phòng`
  - Permission: `restaurant.banquet.view`

Resolve ViewModel:

- `restaurant-alacarte` -> `AlacarteRestaurantViewModel`
- `restaurant-banquet` -> `BanquetRestaurantViewModel`

## Permission

Thêm permission code:

- `restaurant.alacarte.view`
- `restaurant.alacarte.manage`
- `restaurant.banquet.view`
- `restaurant.banquet.manage`

Gợi ý nhóm:

- Group: `Restaurant`

Quy tắc UI:

- `view`: mở module, xem danh sách, xem chi tiết.
- `manage`: tạo/sửa/xóa/cancel, gửi bếp/bar, xác nhận cọc/hợp đồng.

## Migration

Migration mới đề xuất:

- `AddRestaurantAlacarteAndBanquetModules`

Không sửa migration cũ.

Delete behavior đề xuất:

- Room -> Table/Event: `Restrict` hoặc soft deactivate, không cascade.
- Event -> Menu/Service/Deposit/Checklist/FloorPlan/Timeline: cascade được nếu là aggregate thuần của event.
- Contract -> Event: `Restrict` hoặc `NoAction`.
- Deposit -> CashFund/BankAccount: `SetNull`.
- Entity -> Customer/Supplier/MenuItem/Warehouse: `SetNull`, `Restrict` hoặc `NoAction`.

Index đề xuất:

- Code unique theo company/branch cho room/table/package/service.
- EventNo unique.
- ContractNo unique.
- DepositNo unique.
- Calendar lookup: `BanquetEvent(BanquetRoomId, EventDate, StartTime, EndTime, Status)`.
- Table reservation lookup: `TableReservation(DiningTableId, ReservationDateTime, Status)`.
- Open order lookup: `AlacarteOrder(DiningTableId, Status)`.

## Tests

Unit/integration tests cần thêm:

- Tạo phòng ăn.
- Tạo bàn ăn.
- Đặt bàn.
- Tạo order À La Carte và tính tổng tiền.
- Gửi bếp/bar chỉ gửi món mới.
- Hủy món đã gửi tạo phiếu hủy.
- Tạo phòng tiệc.
- Tạo sự kiện tiệc.
- Kiểm tra trùng lịch.
- Kiểm tra vượt sức chứa.
- Tạo hợp đồng tiệc.
- Đặt cọc nhiều lần.
- Tính tổng tiền menu + dịch vụ + cọc + còn phải thu.

## Build và log

Sau triển khai phải chạy:

- `dotnet build`
- `dotnet test`

Xuất log:

- `KIEMTRA_RESTAURANT_MODULES.md`

Nội dung log:

- File thêm mới.
- File sửa.
- Migration.
- Build result.
- Test result.
- Cảnh báo.
- Hướng dẫn sử dụng.

## Lộ trình triển khai đề xuất

1. Thêm entity + DTO + service interface.
2. Thêm DbSet + model mapping.
3. Thêm service infrastructure với logic nghiệp vụ lõi.
4. Thêm DI.
5. Thêm navigation + permission.
6. Tạo migration mới.
7. Thêm ViewModel và XAML tối thiểu theo style Breakfast Cafe.
8. Thêm tests.
9. Chạy build/test.
10. Ghi log kiểm tra.
