# SEARCH RESERVATION WORKSPACE IMPLEMENTATION REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: DEDICATED SEARCH WORKSPACE DEPLOYED

---

## 1. NGUYÊN NHÂN GIAO DIỆN CŨ BỊ TRÙNG

Trước đây, `ReservationModalWorkspaceView` dùng chung một View cho tất cả card (Search, Status, Confirm, Cancel, AssignRoom) thông qua cơ chế `WorkspaceMode`. Điều này khiến:
- Mỗi card mở ra giao diện gần như giống hệt nhau
- Chức năng bị trùng lặp
- Mỗi nghiệp vụ không có trải nghiệm riêng

**Giải pháp**: Tách Search thành workspace riêng, không phụ thuộc vào `WorkspaceMode`.

---

## 2. FILE SEARCH WORKSPACE RIÊNG

| File | Vai trò |
|------|--------|
| `Views/SearchReservationView.xaml` | Dedicated search workspace UI (180 lines) |
| `Views/SearchReservationView.xaml.cs` | Code-behind: back navigation, button handlers (65 lines) |

---

## 3. VIEWMODEL SEARCH

Dùng trực tiếp `ReservationWorkspaceViewModel` (đã có sẵn toàn bộ search/filter/list logic). Không tạo ViewModel wrapper mới vì `ReservationWorkspaceViewModel` đã đủ cho search.

DataTemplate trong `ReservationHubView.xaml`:
```xml
<DataTemplate DataType="{x:Type vm:ReservationWorkspaceViewModel}">
    <views:SearchReservationView />
</DataTemplate>
```

Template này chỉ áp dụng trong context của `ReservationHubView` (local resource), không ảnh hưởng đến DataTemplate toàn cục trong `DataTemplates.xaml`.

---

## 4. DANH SÁCH FILTER

| # | Field | Control | TabIndex |
|---|-------|---------|----------|
| 1 | Từ khóa (số ĐP / tên khách) | TextBox | 1 |
| 2 | Trạng thái | ComboBox | 2 |
| 3 | Ngày đến từ | DatePicker | 3 |
| 4 | Ngày đến đến | DatePicker | 4 |
| 5 | Tìm | Button (blue) | 5 |
| 6 | Xóa lọc | Button (gray) | 6 |

**Chưa triển khai** (field backend chưa hỗ trợ): Market Segment, Company, Travel Agent, VIP, Nguồn, Loại phòng filter.

---

## 5. DANH SÁCH CỘT DATAGRID

| # | Cột | Width | Binding |
|---|-----|-------|---------|
| 1 | Trạng thái | 110 | Status |
| 2 | Số đặt phòng | 140 | ReservationNumber |
| 3 | Tên khách | 180 | GuestName |
| 4 | Ngày đến | 105 | ArrivalDate (dd/MM/yyyy) |
| 5 | Ngày đi | 105 | DepartureDate (dd/MM/yyyy) |
| 6 | Số đêm | 75 | Computed |
| 7 | Người lớn | 75 | AdultCount |
| 8 | Trẻ em | 65 | ChildCount |
| 9 | Nguồn | 100 | Source |

**Style**: Header `#F1F5F9`, row 38px, alternating `#F8FAFC`, font 13px Segoe UI.

---

## 6. DETAIL PANEL (300px bên phải)

**Title**: "Chi tiết đặt phòng"

**Sections** (readonly, không phải form input):

| Section | Fields |
|---------|--------|
| Đặt phòng | Số đặt phòng, Trạng thái |
| Khách | Tên khách, Điện thoại, Email |
| Lưu trú | Ngày đến, Ngày đi, Số đêm |
| Khách | Người lớn, Trẻ em |
| Phòng | Loại phòng, Số phòng |
| Nguồn | Source |

Thông tin thiếu → hiển thị "—"

**Action Buttons**:
- Xem chi tiết (blue)
- Chọn đặt phòng (gray)

---

## 7. TAB ORDER

| # | Control | TabIndex |
|---|---------|----------|
| 1 | Từ khóa | 1 |
| 2 | Trạng thái | 2 |
| 3 | Ngày đến từ | 3 |
| 4 | Ngày đến đến | 4 |
| 5 | Tìm | 5 |
| 6 | Xóa lọc | 6 |
| 7 | DataGrid | 7 |
| 8 | Xem chi tiết | 8 |
| 9 | Chọn đặt phòng | 9 |
| 10 | ← Trước | 10 |
| 11 | Sau → | 11 |

Ctrl+F focus ô tìm kiếm. Arrow Up/Down di chuyển DataGrid.

---

## 8. EMPTY STATE

Khi không có kết quả: "Không tìm thấy đặt phòng phù hợp." + "Xóa bộ lọc" button.

---

## 9. ERROR STATE

Khi query lỗi: "Không thể tải danh sách đặt phòng." + error message + "Thử lại" button. Không hiển thị SQL exception trực tiếp.

---

## 10. PHÂN TRANG

- Tổng: N kết quả
- ← Trước / Sau → (disable khi ở đầu/cuối)

---

## 11. FILE ĐÃ SỬA

| File | Change |
|------|--------|
| `Views/SearchReservationView.xaml` | **Tạo mới** — dedicated search workspace |
| `Views/SearchReservationView.xaml.cs` | **Tạo mới** — back nav, button handlers |
| `Views/ReservationHubView.xaml` | Thêm namespace imports + ContentControl cho ActiveWorkspace + local DataTemplate |
| `ViewModels/ReservationHubViewModel.cs` | Thêm `ActiveWorkspace`, `HasActiveWorkspace`, `OpenSearchWorkspaceAsync()`, `CloseWorkspaceCommand`; Search card dùng workspace thay vì modal |

---

## 12. KẾT QUẢ BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launch | 261MB |
| Exception log | Clean |
| New Reservation wizard | Untouched |
| Các card khác | Untouched |
| Backend | Unchanged |
| Database | Unchanged |

---

## 13. XÁC NHẬN

- [x] Chỉ làm Tìm đặt phòng
- [x] Không sửa Đặt phòng mới
- [x] Không sửa backend
- [x] Không thêm migration
- [x] Không dùng WorkspaceMode chung
- [x] Search có workspace riêng
- [x] Detail Panel không chứa tác vụ Sửa/Phân phòng/Xác nhận/Hủy
- [x] Không query field backend chưa có
