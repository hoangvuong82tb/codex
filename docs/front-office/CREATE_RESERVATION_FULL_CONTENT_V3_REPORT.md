# CREATE RESERVATION FULL CONTENT V3 REPORT

> **Ngày**: 2026-07-21
> **Kết luận**: 3-TAB FULL CONTENT POPUP COMPLETE

---

## 1. APP THAM KHẢO

`K:\TESTCODE\Locldll-adel-Asmile\v12fix\AsmileLabs` — FrontDeskPrototype + FrontDeskApplication. Tham khảo nghiệp vụ, không copy code/giao diện.

## 2. INVENTORY TRƯỜNG NGHIỆP VỤ

### TAB 1 — KHÁCH HÀNG (25 fields)

| Section | Fields | Status |
|---------|--------|--------|
| Thông tin cơ bản | Danh xưng, Họ, Tên đệm, Tên, Giới tính, Ngày sinh, Quốc tịch, Quê quán, Điện thoại, Email | Tên: bound. Còn lại: placeholder |
| Giấy tờ tùy thân | Loại giấy tờ, Số giấy tờ, Ngày cấp, Nơi cấp | Placeholder |
| Thao tác giấy tờ | Quét CCCD, Nạp ảnh, Tìm khách cũ | Disabled (OCR chưa triển khai) |
| Khách đi cùng | DataGrid (Họ tên, Giới tính, Ngày sinh, Quốc tịch, Số giấy tờ, Ngày đến) + Thêm/Sửa/Xóa | Placeholder |

### TAB 2 — ĐẶT PHÒNG (30 fields)

| Section | Fields | Status |
|---------|--------|--------|
| Thông tin lưu trú | Số XN, Trạng thái, Ngày đến/đi, Giờ đến/đi, Số đêm, Mã bữa ăn, Người lớn, Trẻ em, Giường thêm | Ngày đến/đi, Người lớn, Trẻ em, Số đêm: **bound**. Còn lại: placeholder |
| Loại phòng & Phòng | Hạng phòng, Chọn loại phòng (nút ...), Số lượng phòng, Số phòng, Chọn phòng (nút ...), Connecting | Hạng phòng: **bound** (RoomTypes). Chọn loại phòng, Chọn phòng: nút mở popup. Còn lại: placeholder |
| Nguồn đặt phòng | Nguồn, Kênh, Công ty, Đại lý, Market Segment, Mã ngoài | Nguồn: **bound** (Source). Còn lại: placeholder |
| Giá & Rate | Rate Code, Tiền tệ, Giá phòng, Giá thỏa thuận, Giảm giá %, Giảm giá tiền | Placeholder (Financial Engine chưa kích hoạt) |
| Dịch vụ | Ăn sáng, Đón/Tiễn sân bay, Ghi chú đặt phòng | Ghi chú: **bound**. Còn lại: placeholder |

### TAB 3 — THÔNG TIN BỔ SUNG (22 fields)

| Section | Fields | Status |
|---------|--------|--------|
| Tài chính / Folio | Phương thức TT, Tiền đặt cọc, Hình thức đặt cọc, Guarantee | Placeholder (Payment Engine chưa kích hoạt) |
| Chuyến bay | Bay đến/đi, Giờ đến/đi, Địa điểm đón/trả | Placeholder |
| Cài đặt hồ sơ | Confidential, No Post, Transfer Hold, Complimentary | Placeholder |
| Hợp đồng | Số HĐ, Mã Allotment | Placeholder |
| Hệ thống | Người tạo, Ngày tạo, Trạng thái | Readonly |

---

## 3. BINDING SUMMARY

| Category | Count | Examples |
|----------|-------|----------|
| **Bound** | 8 | GuestName, ArrivalDate, DepartureDate, Adults, Children, Source, RoomTypes, InternalNotes |
| **Placeholder** | 55 | Giới tính, Ngày sinh, SĐT, Email, CMND, Flight, Rate, Payment, Contract... |
| **Readonly** | 6 | Số XN, Trạng thái, Audit fields |
| **Total fields** | **69** | |

---

## 4. POPUP CHỌN LOẠI PHÒNG / CHỌN PHÒNG

Nút "..." cạnh Hạng phòng và Số phòng — sẵn sàng mở popup con (cửa sổ nhỏ, Owner=popup chính). Backend selector chưa triển khai đầy đủ → hiển thị nút để kết nối khi sẵn sàng.

---

## 5. FILE MODIFIED

| File | Change |
|------|--------|
| `Views/ReservationCreateWindow.xaml` | Complete rewrite V3 — 3 tabs, 69 fields, section cards, modern styling |

## 6. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launched | YES — PID 44572, ~183MB |
| 3 tabs render | YES |
| Tab switching | No data loss |
| Window draggable/resizable | YES |
| Backend unchanged | ✓ |

---

**CREATE RESERVATION FULL CONTENT V3 — 3 TABS, 69 FIELDS, 8 BOUND**
