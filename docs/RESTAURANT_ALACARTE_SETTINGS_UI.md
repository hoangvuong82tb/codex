# RESTAURANT ALACARTE SETTINGS UI — Tab Cài đặt Nhà hàng À La Carte

## Ngày: 2026-06-14

---

## Đã thêm 11 nhóm cài đặt con

Layout: **Menu trái (280px) + Nội dung phải (*)** — giống triết lý BreakfastCafeSettingsView.

| # | Nhóm | Loại dữ liệu | Nội dung |
|---|---|---|---|
| 0 | **Khu vực / Phòng ăn** | Sample → Binding Rooms | DataGrid: Mã, Tên, Loại, Sức chứa, Trạng thái. Form thêm phòng. |
| 1 | **Bàn ăn** | Sample → Binding Tables | DataGrid: Mã bàn, Tên bàn, Số ghế, Phòng, Trạng thái, Khóa. Form thêm bàn + tạo nhanh (A01-A10). |
| 2 | **Máy in / Bếp bar** | Sample | DataGrid: 4 máy in (thu ngân, bếp, bar, hóa đơn). Model, Port, Online status, Nút Test in. Quy tắc in (bếp→bếp, uống→bar, hóa đơn→thu ngân). |
| 3 | **Nhóm món** | Sample | DataGrid: 10 nhóm món (Bữa sáng, Cà phê, Nước trái cây, Trà, Đồ ăn gọi món, Hải sản, Món nướng, Món lẩu, Đồ uống, Khác) kèm số món. |
| 4 | **Món bán** | Sample | DataGrid: 6 món mẫu (Tên, Nhóm, Giá, VAT, Khu CB Bếp/Bar, Có ĐL, Đang bán). Ghi chú "Ghé module Món ăn & Định lượng". |
| 5 | **Dịch vụ bổ sung** | Sample | DataGrid: 11 dịch vụ (Màn LED, MC, Ca sĩ, Âm thanh, Ánh sáng, Trang trí, Hoa tươi, Máy chiếu, Phí VIP, Phí PV, Photobooth). Đơn giá, ĐV tính, Chọn. |
| 6 | **Thanh toán** | Sample + Settings bindings | DataGrid: 6 phương thức (Tiền mặt, CK, POS/Thẻ, Ghi phòng, Công nợ, Ví điện tử) kèm bật/tắt. Form: VAT, Phí PV, Công nợ, Ghi phòng, Tự động VAT, Làm tròn. |
| 7 | **Đặt bàn / Đặt cọc** | Settings bindings | Form: Giữ bàn, Cảnh báo, Tỷ lệ cọc, Hủy sau, Checkbox đặt cọc nhiều lần, No-show, Tự động chuyển trạng thái. |
| 8 | **Hóa đơn / Chứng từ** | Settings bindings | Form: Số liên in, Checkbox in logo, hiển thị VAT. Placeholder mẫu in. |
| 9 | **Phân quyền** | Sample | 9 quyền (Xem module, Quản lý phòng/bàn, Mở đơn, Hủy đơn, Giảm giá, Thanh toán, Nhận cọc, Quản lý HĐ, Cấu hình) kèm checkbox Granted. |
| 10 | **Đồng bộ / Tích hợp** | Sample | 5 mục (Món ăn & ĐL, Khách hàng, Công nợ, Quỹ/NH, Kho) kèm pill "Đã sẵn sàng" / "Chưa cấu hình". |

---

## Dữ liệu

| Nhóm | Nguồn |
|---|---|
| 0, 1 | Sample (khởi tạo từ Rooms/Tables nếu có, nếu không thì sample cứng) |
| 2-5, 9-10 | **Sample/local** — chưa có service/DB |
| 6-8 | **Settings bindings** từ ViewModel properties (DefaultVatRate, DefaultServiceChargeRate, DefaultDepositRate...) |

---

## File đã sửa

| File | Thay đổi |
|---|---|
| `ViewModels/RestaurantViewModels.cs` | Thêm 30+ properties/collections cho settings: SettingsSubTabIndex, 11 IsSettings* computed, 9 ObservableCollection, 15 form field properties, NavigateSettingsSubTabCommand, SaveSettingsCommand, InitSettingsSampleData(), 7 helper classes (AlacarteSettingArea, AlacarteSettingTable, AlacarteSettingPrinter, AlacarteSettingMenuGroup, AlacarteSettingMenuItem, AlacarteSettingPaymentMethod, AlacarteSettingPermission, AlacarteSettingIntegration) |
| `Views/AlacarteRestaurantView.xaml` | Thay toàn bộ Tab 6: layout 2 cột (menu 280px + content *), 11 nút menu với active state xanh, 11 nội dung nhóm với DataGrid/form/checkbox |

---

## Build status

**PASS** — 0 errors, 0 warnings
