# SEARCH RESERVATION VISUAL REDESIGN REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: SEARCH POPUP REDESIGNED — MODERN ASMILE-BO INTERFACE

---

## 1. VẤN ĐỀ UI TRƯỚC KHI SỬA

| Vấn đề | Mô tả |
|--------|-------|
| Filter hàng ngang | Control mặc định WPF, không bo góc, không label |
| DataGrid thô | Không style, row height nhỏ |
| Trạng thái | Text thuần, không badge |
| Số đêm | MultiBinding sai — hiển thị `7/18/202...` thay vì `1` |
| Detail Panel | Không có empty state, layout cũ |
| Khoảng trắng | Vùng trống lớn cuối bảng |
| Footer | Rời rạc, không card |

---

## 2. FILTER CARD (MỚI)

- Card trắng `CornerRadius=12`, border `#E2E8F0`
- Mỗi field có label phía trên (Từ khóa, Trạng thái, Ngày đến từ, Ngày đến đến)
- TextBox: `Height=38`, `CornerRadius=8`, border `#D8E2F0`, focus `#2F6BFF`
- ComboBox: style tương tự, arrow hiện đại
- DatePicker: icon lịch xanh trong `#EFF6FF`
- Buttons: Tìm (blue), Xóa lọc (gray)
- `WrapPanel` — tự xuống dòng khi hẹp

---

## 3. DATAGRID CARD (MỚI)

- Card riêng với title "Danh sách đặt phòng" + "N kết quả"
- Header `#F2F6FC`, font SemiBold 12px
- Row height 40px, alternating `#FAFBFD`
- Hover/selected handled by WPF defaults
- Các cột: Trạng thái (badge), Số ĐP (bold), Tên khách, Ngày đến/đi, Số đêm, Ng.lớn, Trẻ em, Nguồn

---

## 4. STATUS BADGE

Sử dụng `DataTrigger` trên `Status` value:

| Status | Background | Text Color |
|--------|-----------|------------|
| Confirmed | `#DBEAFE` | `#1E40AF` |
| Tentative | `#FFF7ED` | `#C2410C` |
| Draft | `#F1F5F9` | `#475569` |
| NoShow | `#FEE2E2` | `#DC2626` |
| Cancelled | `#F3F4F6` | `#6B7280` |
| CheckedIn | `#DCFCE7` | `#166534` |

- `CornerRadius=10`, `Padding=10,3`, `FontSize=12`

---

## 5. ROOT CAUSE CỘT SỐ ĐÊM SAI

**Trước**: MultiBinding bind cả `ArrivalDate` và `DepartureDate` không compute → WPF hiển thị raw `DateOnly` values.

```xml
<MultiBinding StringFormat="{}{0}">
  <Binding Path="ArrivalDate"/><Binding Path="DepartureDate"/>
</MultiBinding>
```

**Sau**: Dùng `StayNightsConverter` (IMultiValueConverter):

```csharp
Math.Max(0, departure.DayNumber - arrival.DayNumber)
```

Binding: `<MultiBinding Converter="{StaticResource NightsConv}">`

---

## 6. DETAIL PANEL (MỚI)

- Card riêng, `Width=300`
- **Empty state**: "Chọn một đặt phòng để xem chi tiết." (ẩn khi có selection)
- **Detail content**: Số đặt phòng, Trạng thái (badge), Khách (tên+ĐT+email), Lưu trú (đến→đi+số đêm), Người lớn/Trẻ em, Loại phòng, Số phòng, Nguồn
- Thông tin thiếu → "—"
- Action buttons: "Xem chi tiết" (blue), "Chọn đặt phòng" (gray) — enable khi có selection

---

## 7. FOOTER (MỚI)

- Card trắng `CornerRadius=10`
- Trái: "Tổng: N kết quả"
- Phải: ← Trước / Sau → (disable khi ở đầu/cuối)

---

## 8. RESPONSIVE

- `WrapPanel` cho filter → tự xuống dòng khi hẹp
- DataGrid stretches theo chiều rộng
- Detail Panel fixed 300px (MinWidth 300px)
- Window MinWidth 1050px → đủ cho DataGrid + Detail Panel

---

## 9. FILES ĐÃ SỬA / TẠO

| File | Change |
|------|--------|
| `Views/SearchReservationView.xaml` | **Rewrite** — 345 lines modern UI |
| `Views/SearchReservationView.xaml.cs` | Thêm `OnSelectClick` handler |
| `Converters/StayNightsConverter.cs` | **Tạo mới** — IMultiValueConverter tính số đêm |

---

## 10. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launch | 261MB |
| Exception log | Clean |
| Popup window | Unchanged |
| New Reservation | Untouched |
| Backend | Unchanged |

---

## 11. XÁC NHẬN

- [x] Chỉ sửa giao diện Tìm đặt phòng
- [x] Không sửa backend
- [x] Không sửa database
- [x] Không sửa cách mở popup
- [x] Không sửa Đặt phòng mới
- [x] Status badge hoạt động
- [x] Số đêm hiển thị integer
- [x] Detail Panel có empty state
