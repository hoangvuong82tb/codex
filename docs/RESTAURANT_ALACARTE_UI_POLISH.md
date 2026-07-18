# RESTAURANT À LA CARTE UI POLISH — POS Mockup Redesign

## Ngày: 2026-06-14

---

## File đã sửa

### `AlacarteRestaurantView.xaml` — Viết lại hoàn toàn theo mockup

| Khu vực | Mô tả |
|---|---|
| **Top bar (78px)** | Brand icon + title + status + 2 KPI stats (Doanh thu + Bàn đang dùng) + nút Làm mới |
| **Sidebar (78px)** | 7 icon tabs: Khu vực, Tổng quan, Đặt bàn, Đơn mở, Hợp đồng, Dịch vụ, Cài đặt — active state xanh dương + border trái 3px |
| **Main content** | ScrollViewer chứa: |
| | **KPI row** — 6 cards: Phòng đang dùng, Bàn đang phục vụ, Đặt bàn hôm nay, Doanh thu hôm nay, Đặt cọc hôm nay, Khách dự kiến. Mỗi card có icon màu + số liệu từ Dashboard |
| | **Toolbar** — "KHU VỰC / PHÒNG ĂN" + room chips (từ Rooms collection) + search box |
| | **Table cards** — WrapPanel với 4 màu trạng thái: |
| | · Trống: nền xanh lá nhạt (#EFFDF5), viền #BBF7D0, dot xanh |
| | · Đã đặt: nền cam nhạt (#FFF7ED), viền #FED7AA, dot cam |
| | · Đang phục vụ: nền đỏ nhạt (#FEF2F2), viền #FECACA, dot đỏ |
| | · Khóa bàn: nền xám nhạt (#F8FAFC), viền #CBD5E1, dot xám |
| | **Bottom triple box** — 3 cột: |
| | · Đặt bàn hôm nay: reservation list với cột: Giờ + Tên khách + Bàn + Số khách + Status pill |
| | · Dịch vụ phổ biến: grid 3x2 các service card (Màn LED, MC, Ca sĩ, Âm thanh, Trang trí) |
| | · Thông báo: 3 notice items (thanh toán, đặt bàn sắp đến, sự kiện tiệc cưới) |
| **Order panel (520px)** | Bên phải: |
| | · Order header: Tên bàn + Phòng + Order No + Status badge đỏ |
| | · Customer info bar: 5 cột (Khách hàng, SĐT, Số khách, Giờ đến, Ghi chú) |
| | · Tab bar: Món ăn (count) | Dịch vụ | Thông tin — tab active có underline xanh |
| | · Dish list: mỗi dòng có icon + tên món + SL + đơn giá + thành tiền |
| | · Summary card: Tạm tính, Giảm giá, VAT, Phí phục vụ, Đặt cọc → TỔNG CỘNG (nền cam nhạt #FFF7ED) |
| | · Payment methods: 5 chip (Tiền mặt/C.khoản/Thẻ/Ghi phòng/Công nợ) |
| | · Action buttons: Tạm tính + In tạm tính | Thanh toán (xanh) + Hủy đơn (đỏ) |

---

## Bindings sử dụng

| Binding | Nguồn |
|---|---|
| `Dashboard.ActiveRooms` | AlacarteDashboardDto |
| `Dashboard.ServingTables` | AlacarteDashboardDto |
| `Dashboard.TodayReservations` | AlacarteDashboardDto |
| `Dashboard.TodayRevenue` | AlacarteDashboardDto |
| `Dashboard.TodayDeposits` | AlacarteDashboardDto |
| `Dashboard.ExpectedGuests` | AlacarteDashboardDto |
| `StatusText` | ViewModel property |
| `Rooms` | ObservableCollection<DiningRoomDto> |
| `Tables` | ObservableCollection<DiningTableDto> |
| `Reservations` | ObservableCollection<TableReservationDto> |
| `SelectedOrder.TableName` | AlacarteOrderDto |
| `SelectedOrder.RoomName` | AlacarteOrderDto |
| `SelectedOrder.OrderNo` | AlacarteOrderDto |
| `SelectedOrder.Status` | AlacarteOrderDto |
| `SelectedOrder.OrderDate` | AlacarteOrderDto |
| `SelectedOrder.Note` | AlacarteOrderDto |
| `SelectedOrder.Lines` | AlacarteOrderLineDto collection |
| `SelectedOrder.SubtotalAmount` | AlacarteOrderDto |
| `SelectedOrder.DiscountAmount` | AlacarteOrderDto |
| `SelectedOrder.VatAmount` | AlacarteOrderDto |
| `SelectedOrder.ServiceChargeAmount` | AlacarteOrderDto |
| `SelectedOrder.TotalAmount` | AlacarteOrderDto |

## Commands sử dụng

| Command | Parameter |
|---|---|
| `RefreshCommand` | — |
| `NavigateTabCommand` | "0"-"3" (string) |
| `SelectRoomCommand` | DiningRoomDto |
| `OpenOrderCommand` | DiningTableDto |

---

## Không đụng vào

- Entity, DbContext, Migration, Service, DTO, ViewModel, Command, Navigation, Permission, Database
- Các file khác ngoài `AlacarteRestaurantView.xaml`

---

## Encoding

- File UTF-8 — Encoding check PASS
- Không mojibake, không lỗi tiếng Việt
- Toàn bộ text tiếng Việt viết trực tiếp (không dùng Unicode escape)

---

## Build status

**PASS** — 0 errors, 0 warnings. Encoding check: PASS.

---

## Khác biệt với mockup

| Mockup | Implementation | Lý do |
|---|---|---|
| Room sections có header + tables riêng | Tables hiển thị flat grid | ViewModel không group Tables theo Room — cần thêm filtered collection |
| Customer info có tên/SĐT thật | Hiển thị "—" | AlacarteOrderDto chưa có CustomerName, Phone, GuestCount fields |
| Dịch vụ tab có data | Chưa có tab content | ViewModel chưa có service lines collection |
| Thông báo realtime | Static demo text | Chưa có notification service |
| Room chips lọc tables | Chips hiển thị nhưng không lọc | SelectRoomCommand chỉ set SelectedRoom, không filter Tables |
