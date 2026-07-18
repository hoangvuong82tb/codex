# RESTAURANT ALACARTE REAL UI — Hoàn thiện Module Nhà hàng À La Carte

## Ngày: 2026-06-14

---

## File đã sửa

| File | Thay đổi |
|---|---|
| `ViewModels/RestaurantViewModels.cs` | Mở rộng AlacarteRestaurantViewModel: 7 tabs, contracts, services, alerts, settings, sample data command, sub-tab cho panel phải. Giữ nguyên BanquetRestaurantViewModel. |
| `Views/AlacarteRestaurantView.xaml` | Viết lại hoàn toàn: 7 sidebar tabs với nội dung đầy đủ, panel phải có 3 sub-tab (Món ăn/Dịch vụ/Thông tin) + payment card. |

---

## 7 Tab đã hoàn thiện

### 1. Khu vực (tab 1) — Tab chính
- **KPI cards**: 6 thẻ (Phòng đang dùng, Bàn đang phục vụ, Đặt bàn hôm nay, Doanh thu, Đặt cọc, Khách dự kiến)
- **Room chips**: Lọc phòng từ Rooms collection
- **Table cards**: 4 màu trạng thái (Trống xanh, Đã đặt cam, Đang phục vụ đỏ, Khóa xám)
- **Click bàn**: Mở đơn, cập nhật panel phải
- **Nút Khởi tạo DL mẫu**: Tạo phòng, bàn, đặt bàn, hợp đồng mẫu

### 2. Tổng quan (tab 0) — Dashboard
- 6 KPI cards + Đặt bàn hôm nay (danh sách reservation) + Cảnh báo vận hành

### 3. Đặt bàn (tab 2)
- Form nhanh: Tên khách, SĐT, Số khách, Chọn phòng, Lưu
- Danh sách đặt bàn dạng card với icon, thông tin khách, trạng thái

### 4. Đơn mở (tab 3)
- Danh sách đơn đang mở dạng card: mã đơn, bàn, phòng, giờ, số món, tổng tiền

### 5. Hợp đồng (tab 4)
- Danh sách hợp đồng dạng card 2 cột: số HĐ, mô tả, giá trị, cọc, còn lại, trạng thái

### 6. Dịch vụ (tab 5)
- Danh sách 12 dịch vụ dạng grid 3 cột: tên, giá, đơn vị, checkbox chọn

### 7. Cài đặt (tab 6)
- VAT, phí phục vụ, đặt cọc %, cảnh báo trước phút, auto màu bàn

---

## Panel phải — 3 Sub-tab

| Sub-tab | Nội dung |
|---|---|
| **Món ăn** | Danh sách món trong SelectedOrder.Lines + nút Thêm món / Ghi chú món |
| **Dịch vụ** | Danh sách AlacarteServices với checkbox |
| **Thông tin** | Bàn, Phòng, Mã đơn, Trạng thái, Ghi chú |

---

## Commands — Trạng thái

| Command | Trạng thái | Ghi chú |
|---|---|---|
| `NavigateTab` | **Hoạt động thật** | 0-6 |
| `NavigateOrderDetailTab` | **Hoạt động thật** | 0-2 |
| `SelectRoom` | **Hoạt động thật** | Set SelectedRoom |
| `OpenOrder` | **Hoạt động thật** | Gọi service mở đơn |
| `Refresh` | **Hoạt động thật** | Load từ DB |
| `BootstrapSampleData` | **Hoạt động thật** | Tạo phòng/bàn/đặt bàn/HĐ mẫu nếu trống |
| `SaveRoom / SaveTable / SaveReservation` | **Hoạt động thật** | Gọi service |
| `PayOrder` | Placeholder | Thông báo "đang chuẩn bị" |
| `CancelOrder` | Placeholder | Thông báo "đang chuẩn bị" |
| `PrintProvisional` | Placeholder | Thông báo "đang chuẩn bị" |
| `AddMenuItem` | Placeholder | Thông báo "đang chuẩn bị" |

---

## Dữ liệu

| Nguồn | Loại |
|---|---|
| **Từ DB** | Rooms, Tables, Reservations, OpenOrders (qua service) |
| **ViewModel local** | AlacarteServices, Alerts, Contracts (khởi tạo trong VM) |
| **Sample command** | BootstrapSampleData tạo dữ liệu vào DB nếu bảng trống |

---

## Build status

**PASS** — 0 errors, 0 warnings. Encoding check: PASS.

---

## Lưu ý

- Contracts collection hiện là local (ObservableCollection trong VM) — nếu cần lưu DB, cần thêm entity + migration
- Customer info trong panel phải hiển thị "—" vì AlacarteOrderDto chưa có CustomerName/Phone/GuestCount fields
- Payment methods và action buttons hiện là placeholder (hiện thông báo, không crash)
