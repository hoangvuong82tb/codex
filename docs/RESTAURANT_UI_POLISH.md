# RESTAURANT UI POLISH — Báo cáo redesign module Nhà hàng

## Ngày: 2026-06-14

---

## Triết lý UI

Toàn bộ module Nhà hàng được redesign theo **triết lý UX của module Bán đồ ăn sáng & Cà phê**:

- **POS-style flow**: Chọn khu vực → chọn bàn → xem đơn hàng bên phải
- **Fluent Design**: Corner radius đồng bộ (10-14px), soft shadow, card-based layout
- **Mini sidebar 65px**: Icon + text navigation giống POS
- **2-column layout 60/40**: Danh sách bên trái, chi tiết bên phải
- **Empty state đẹp**: Icon + hướng dẫn khi không có dữ liệu
- **Color coding trạng thái**: Xanh lá (Trống/Sẵn sàng), Cam (Đã đặt), Đỏ/Xanh dương (Đang phục vụ), Xám (Khóa)

---

## File đã sửa

### 1. `AlacarteRestaurantView.xaml` — Viết lại hoàn toàn

**Trước**: 3-column CRUD layout (nav trái | KPI + danh sách giữa | form nhập phải)
**Sau**: POS-style 2-column layout

| Thành phần | Thay đổi |
|---|---|
| **Top bar** | Title + KPI stats + nút Làm mới, giống POS header |
| **Sidebar 65px** | 4 icon tab: Tổng quan, Phòng bàn, Đặt bàn, Đơn mở |
| **Dashboard tab** | KPI cards (Phòng đang dùng, Đặt bàn hôm nay, Khách dự kiến) + Phòng ăn cards |
| **Phòng & Bàn tab** | Room chip filter + Table cards WrapPanel với 4 màu trạng thái (Empty=Green, Reserved=Orange, Serving=Blue, Locked=Gray) |
| **Đặt bàn tab** | Reservation list dạng card với customer info + status pill |
| **Đơn mở tab** | Open orders list với order total + status |
| **Right panel** | Khi chọn bàn → hiện Order detail: header, order lines, totals, nút Thanh toán/Hủy |
| **Empty states** | Icon + hướng dẫn cho từng tab khi không có dữ liệu |

**Bindings giữ nguyên**: `NavigateTabCommand`, `SelectRoomCommand`, `OpenOrderCommand`, `SelectedOrder`, `Dashboard`, `Rooms`, `Tables`, `Reservations`, `OpenOrders`

### 2. `BanquetRestaurantView.xaml` — Viết lại hoàn toàn

**Trước**: 3-column CRUD layout (nav trái | KPI + danh sách giữa | form nhập phải)
**Sau**: KPI row + 2-column event management layout

| Thành phần | Thay đổi |
|---|---|
| **Top bar** | Title + KPI mini stats + nút Làm mới |
| **KPI row** | 5 KPI cards: Tiệc hôm nay, Khách dự kiến, Đặt cọc hôm nay, Đã xác nhận, Cảnh báo lịch (có badge đỏ khi có conflict) |
| **Left panel** | Danh sách tiệc dạng event cards với: loại tiệc, tên khách, ngày/giờ, số khách, trạng thái pill, phòng + tiền cọc |
| **Right panel** | Chi tiết tiệc với **7 tabs**: |
| | Tab 1: **Thông tin** — Khách hàng, SĐT, Ngày giờ, Phòng, Số khách/bàn, Ghi chú, Tài chính |
| | Tab 2: **Hợp đồng** — Tổng hợp đơn giá, nút Tạo hợp đồng |
| | Tab 3: **Đặt cọc** — Số tiền đã cọc + form thêm cọc mới |
| | Tab 4: **Checklist** — Danh sách checklist với checkbox, strikethrough khi done |
| | Tab 5: **Timeline** — Empty state đẹp (chờ dữ liệu lịch sử) |
| | Tab 6: **Dịch vụ** — 10 checkbox dịch vụ (Màn LED, MC, Ca sĩ, Âm thanh, Trang trí...) |
| | Tab 7: **Thực đơn** — Gói thực đơn cards từ MenuPackages |
| **Footer** | Tổng cộng + 3 nút: Thanh toán, In hợp đồng, Hủy |

**Bindings giữ nguyên**: `NavigateTabCommand`, `SelectEventCommand`, `SelectRoomCommand`, `SelectedEvent`, `Dashboard`, `Rooms`, `Events`, `MenuPackages`, `ChecklistItems`, `ToggleChecklistCommand`, `CreateContractCommand`, `AddDepositCommand`, `DepositAmount`

### 3. `BanquetRestaurantCalendarView.xaml` — Tạo mới

File mới tạo để hiển thị lịch phòng tiệc dạng timeline.

| Thành phần | Mô tả |
|---|---|
| **Header** | Title + 3 nút filter: Hôm nay, 7 ngày, 30 ngày |
| **Room chips** | Danh sách phòng tiệc dạng chip (Sảnh A, Sảnh B, Sảnh C...) |
| **Timeline grid** | Cột trái: tên phòng + sức chứa + status. Cột phải: 7 cột ngày (Hôm nay → +6 ngày) |
| **Event blocks** | Mỗi sự kiện là 1 block màu trong timeline (Confirmed=Xanh lá, Draft=Xám) |
| **Empty state** | Icon calendar + hướng dẫn tạo phòng tiệc |

**Bindings**: `Rooms`, `Events`, `SelectRoomCommand`, `SelectEventCommand`

### 4. `BanquetRestaurantCalendarView.xaml.cs` — Tạo mới

Code-behind tối thiểu cho UserControl mới.

---

## Những gì được giữ nguyên

| Layer | Files không đụng |
|---|---|
| **Entities** | `RestaurantEntities.cs` — 0 thay đổi |
| **DTOs** | `RestaurantDtos.cs` — 0 thay đổi |
| **Services** | `RestaurantServices.cs` — 0 thay đổi |
| **ViewModels** | `RestaurantViewModels.cs` — 0 thay đổi |
| **Interfaces** | `IRestaurantServices.cs` — 0 thay đổi |
| **Migrations** | `20260614055304_AddRestaurantAlacarteAndBanquetModules.cs` — 0 thay đổi |
| **DbContext** | `AppDbContext.cs` — 0 thay đổi |
| **Commands** | Tất cả command bindings giữ nguyên tên và signature |
| **Navigation** | 0 thay đổi |
| **Permissions** | 0 thay đổi |

---

## Style system

Các style mới thêm trong XAML (local scope, không conflict với global):

| Style | Mục đích |
|---|---|
| `MiniSidebarBtn` | 65x60 icon button giống POS |
| `RoomChipBtn` | Filter chip cho phòng ăn |
| `TableCard` | Base style cho table card 150x110 |
| `EventCardBtn` | Event list item button |
| `KPICard` | KPI stat card |
| `DetailTabBtn` | Tab button cho event detail |
| `ActionPrimary/Success/Warning/Danger` | Nút hành động đồng bộ với POS |
| `EventBlock` | Calendar timeline event block |
| `CardShadow` | DropShadowEffect soft shadow |

---

## Build status

**PASS** — 0 errors, 0 warnings. Encoding check: PASS.

---

## Khuyến nghị tương lai

1. **À La Carte**: Khi ViewModel có thêm properties (order lines, payment methods, guest info), right panel sẽ tự động hiển thị thêm thông tin mà không cần sửa XAML
2. **Banquet Timeline tab**: Hiện là placeholder — cần thêm `BanquetTimelineDto` collection trong ViewModel
3. **Banquet Dịch vụ tab**: Checkbox hiện là static — cần binding tới `BanquetServiceLineDto` collection
4. **Calendar View**: Đang dùng `Rooms` chung — nên có ViewModel riêng với calendar-specific data
5. **In hợp đồng / In phiếu đặt bàn**: Cần thêm command trong ViewModel
