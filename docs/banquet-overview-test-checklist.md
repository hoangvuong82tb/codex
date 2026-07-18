# Banquet Overview Tab Test Checklist — ASMILE-BO

> Dành cho tester kiểm tra tab Tổng quan (Dashboard + Event Editor)
> Cập nhật: 2026-07-10

---

## 1. Tạo mới sự kiện

### TC-01: Tạo sự kiện mới với thông tin tối thiểu

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở module Nhà hàng Tiệc | Dashboard hiển thị với danh sách sự kiện |
| 2 | Click nút "+ Thêm mới" | Form tạo sự kiện hiển thị, tab "Chọn loại" active |
| 3 | Click card "Tiệc Cưới" | EventType = Wedding, chuyển sang tab "Thông tin" |
| 4 | Nhập Tên khách hàng: "Nguyễn Văn A" | Ô nhập hiển thị đúng |
| 5 | Nhập Số điện thoại: "0901234567" | Ô nhập hiển thị đúng |
| 6 | Chọn Ngày tổ chức: 01/08/2026 | DatePicker hiển thị đúng |
| 7 | Chọn Phòng: "Sảnh Diamond" | ComboBox hiển thị danh sách phòng từ DB |
| 8 | Để mặc định Giờ: 17:00 - 21:00, Số khách: 100, Số bàn: 10 | Form hiển thị giá trị mặc định |
| 9 | Click "Lưu" | Hiển thị "Đã lưu sự kiện BQT-..." trong status bar |
| 10 | Kiểm tra danh sách sự kiện bên trái | Sự kiện mới xuất hiện trong danh sách |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

### TC-02: Tạo sự kiện từng loại (Wedding, Conference, Birthday, Meeting, Party, Other)

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Tạo mới → chọn "Hội nghị" | Tab Thông tin hiển thị trường "Tên hội nghị" |
| 2 | Nhập tên hội nghị: "Hội nghị CNTT 2026" | Preview hiển thị đúng |
| 3 | Tạo mới → chọn "Sinh nhật" | Hiển thị trường "Người tổ chức" + "Ngày sinh" |
| 4 | Tạo mới → chọn "Họp mặt" | Hiển thị trường "Chủ đề họp mặt" |
| 5 | Tạo mới → chọn "Tiệc / Party" | Hiển thị trường "Chủ đề tiệc" |
| 6 | Tạo mới → chọn "Sự kiện khác" | Hiển thị trường "Tên sự kiện" |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

> **Cảnh báo (BUG-001):** Các trường đặc thù (cô dâu, chú rể, tên hội nghị...) HIỆN CHƯA ĐƯỢC LƯU VÀO DATABASE. Đây là lỗi P0.

---

## 2. Chỉnh sửa sự kiện

### TC-03: Mở và chỉnh sửa sự kiện đã lưu

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Click chọn một sự kiện từ danh sách | Form chi tiết hiển thị với dữ liệu đã lưu |
| 2 | Sửa Tên khách hàng: thêm ký tự | Preview cập nhật real-time |
| 3 | Sửa Số khách: 200 | Preview cập nhật, IsDirty = true |
| 4 | Click "Lưu" | Lưu thành công, IsDirty = false |
| 5 | Đóng và mở lại ứng dụng | Dữ liệu đã lưu được tải đúng |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

### TC-04: Undo/Redo thay đổi

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện, sửa tên khách hàng | IsDirty = true |
| 2 | Sửa số khách | IsDirty = true |
| 3 | Gọi Undo (nếu có phím tắt) | Quay lại trạng thái trước khi sửa |
| 4 | Gọi Redo | Khôi phục thay đổi đã undo |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 3. Validation

### TC-05: Validation bắt buộc

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Tạo mới, để trống Tên khách hàng, click Lưu | Báo lỗi "Nhập tên khách hàng." |
| 2 | Nhập tên, để Số khách = 0, click Lưu | Báo lỗi "Số khách phải lớn hơn 0." |
| 3 | Nhập số khách = 100, để Số bàn = 0, click Lưu | Báo lỗi "Số bàn phải lớn hơn 0." |
| 4 | Nhập số bàn = 10, đặt Giờ bắt đầu = 21:00, Kết thúc = 17:00 | Báo lỗi "Giờ bắt đầu phải nhỏ hơn giờ kết thúc." |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

### TC-06: Validation sức chứa phòng

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Chọn phòng có Capacity = 300 | OK |
| 2 | Nhập Số khách = 500 | Cảnh báo "Quy mô vượt sức chứa..." |
| 3 | Nhập Số bàn = 50 (MaxTables = 30) | Cảnh báo "Quy mô vượt sức chứa..." |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 4. Phòng và lịch

### TC-07: Kiểm tra trùng lịch phòng

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Tạo sự kiện A: Phòng Diamond, 01/08/2026, 17:00-21:00 | Lưu OK |
| 2 | Tạo sự kiện B: Phòng Diamond, 01/08/2026, 18:00-22:00 | Báo lỗi trùng lịch phòng |
| 3 | Sửa sự kiện B sang Phòng Ruby, cùng giờ | Lưu OK (khác phòng) |
| 4 | Tạo sự kiện C: Phòng Diamond, 01/08/2026, 12:00-16:00 | Lưu OK (không overlap) |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

### TC-08: Chọn phòng từ ComboBox

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở ComboBox Phòng / Sảnh | Hiển thị danh sách phòng từ DB |
| 2 | Chọn một phòng | SelectedEventRoomId được cập nhật |
| 3 | Kiểm tra preview bên phải | Hiển thị tên phòng đã chọn |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 5. Trạng thái sự kiện

### TC-09: Thay đổi trạng thái

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện, chọn trạng thái "Hold" từ ComboBox | EventStatus = "Hold" |
| 2 | Click Lưu | Trạng thái được lưu |
| 3 | Mở lại sự kiện | Trạng thái hiển thị "Hold" |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

> **Cảnh báo (BUG-101):** Hiện chưa có state machine — có thể chuyển từ "Completed" về "Draft".

---

## 6. Tìm kiếm và lọc

### TC-10: Lọc danh sách sự kiện

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Nhập text vào ô tìm kiếm | Danh sách lọc theo tên khách, mã event |
| 2 | Chọn Status filter = "Confirmed" | Chỉ hiện sự kiện Confirmed |
| 3 | Chọn Type filter = "Wedding" | Chỉ hiện Tiệc Cưới |
| 4 | Chọn Room filter | Chỉ hiện sự kiện ở phòng đó |
| 5 | Xóa tất cả filter | Hiển thị toàn bộ danh sách |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 7. Lưu và xử lý lỗi

### TC-11: Kiểm tra chống mất dữ liệu khi chưa lưu

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện, sửa tên khách hàng (không lưu) | IsDirty = true |
| 2 | Click chọn sự kiện khác trong dashboard | Cảnh báo "Thay đổi chưa lưu" (nếu có) |
| 3 | Chọn Yes | Bỏ thay đổi, mở sự kiện mới |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

### TC-12: Bấm Lưu nhiều lần liên tiếp

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện, sửa số khách | IsDirty = true |
| 2 | Click Lưu 3 lần nhanh liên tiếp | Chỉ tạo 1 bản cập nhật, không tạo trùng |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 8. Thực đơn

### TC-13: Chọn thực đơn preset

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện → tab "Thực đơn" | Hiển thị 2 cards: chọn món / chọn gói |
| 2 | Click "Chọn thực đơn có sẵn" | Mở modal danh sách gói thực đơn |
| 3 | Chọn một gói → Confirm | Gói thực đơn được gắn vào sự kiện |
| 4 | Kiểm tra tab "Tài chính" | Tổng tiền được cập nhật |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

### TC-14: Chọn món tùy chỉnh

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Tab "Thực đơn" → Click "Chọn món theo yêu cầu" | Mở modal chọn món |
| 2 | Chọn vài món từ catalog | Danh sách món đã chọn hiển thị bên phải |
| 3 | Click "Lưu" trên modal | Món đã chọn hiển thị trong event |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 9. Dịch vụ

### TC-15: Thêm và xóa dịch vụ

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện → tab "Dịch vụ" | Hiển thị danh sách dịch vụ (có thể trống) |
| 2 | Nhập "MC tiệc cưới", chọn ServiceType = "MC", giá = 2,000,000 | Form thêm hiển thị |
| 3 | Click "Thêm" | Dịch vụ xuất hiện trong danh sách |
| 4 | Click nút Xóa (✕) trên dòng dịch vụ | Dịch vụ bị xóa |
| 5 | Kiểm tra tab "Tài chính" | Tổng tiền được cập nhật |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 10. Checklist

### TC-16: Toggle checklist item

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện → tab "Checklist" | Hiển thị checklist mặc định (9 items) |
| 2 | Tích checkbox "Đã chốt thực đơn" | Item hiển thị IsDone = true |
| 3 | Bỏ tích | Item hiển thị IsDone = false |
| 4 | Thêm item mới: "Kiểm tra bàn ghế" | Item mới xuất hiện cuối danh sách |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 11. Timeline

### TC-17: Thêm mốc timeline

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện → tab "Timeline" | Hiển thị timeline mặc định |
| 2 | Nhập giờ: "18:00", tiêu đề: "Khai vị", mô tả, người phụ trách | Form nhập OK |
| 3 | Click "Thêm" | Mốc timeline mới xuất hiện |
| 4 | Click xóa một mốc | Mốc bị xóa khỏi danh sách |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 12. Đặt cọc

### TC-18: Thêm đặt cọc

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện → tab "Tài chính" | Hiển thị lịch sử cọc (có thể trống) |
| 2 | Nhập số tiền: 10,000,000 | Ô nhập hiển thị |
| 3 | Chọn phương thức: "Chuyển khoản" | ComboBox chọn |
| 4 | Nhập người nhận: "Thu ngân" | Mặc định |
| 5 | Click "Xác nhận đặt cọc" | Hiển thị "Đã ghi nhận đặt cọc tiệc." |
| 6 | Kiểm tra danh sách cọc | Cọc mới hiển thị với mã BDP-... |
| 7 | Kiểm tra trạng thái sự kiện | Chuyển sang "Deposited" |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

### TC-19: Validation đặt cọc

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Để Amount = 0, click "Xác nhận đặt cọc" | Báo lỗi "Số tiền cọc phải lớn hơn 0." |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 13. Hợp đồng

### TC-20: Tạo hợp đồng

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện → tab "Hợp đồng" | Hiển thị danh sách hợp đồng |
| 2 | Chọn ngày ký: hôm nay | DatePicker |
| 3 | Click "Tạo hợp đồng" | Hợp đồng mới hiển thị với mã BCT-... |
| 4 | Kiểm tra tổng tiền | TotalAmount = MenuAmount + ServiceAmount |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 14. Dashboard KPI

### TC-21: Kiểm tra số liệu Dashboard

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở Dashboard | Hiển thị danh sách sự kiện + KPI sidebar |
| 2 | Kiểm tra số lượng sự kiện | Trùng với danh sách đã lọc |
| 3 | Kiểm tra filter theo ngày | Lọc đúng khoảng thời gian |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 15. Phân quyền (nếu có)

### TC-22: Kiểm tra quyền truy cập

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Đăng nhập với vai trò không có quyền Banquet | Không thấy module hoặc báo lỗi truy cập |
| 2 | Đăng nhập với vai trò Lễ tân | Xem được, sửa được thông tin cơ bản |

**Trạng thái:** ⬜ Chưa implement — Ghi chú: ___

---

## 16. Stress test

### TC-23: Dữ liệu lớn

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Tạo 100+ sự kiện | Danh sách load không quá 3 giây |
| 2 | Scroll danh sách | UI không bị lag |
| 3 | Filter nhanh | Phản hồi < 500ms |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

### TC-24: Đóng ứng dụng khi đang edit

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở sự kiện, sửa tên (không lưu) | IsDirty = true |
| 2 | Đóng ứng dụng (Alt+F4) | Cảnh báo "Thay đổi chưa lưu" hoặc tự động lưu nháp |
| 3 | Mở lại ứng dụng | Dữ liệu chưa lưu bị mất — **cần xác nhận đây có phải bug** |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## 17. Regression test

### TC-25: Kiểm tra các chức năng khác không bị ảnh hưởng

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Mở module À La Carte | Hoạt động bình thường |
| 2 | Mở module Kế toán | Hoạt động bình thường |
| 3 | Mở module Kho | Hoạt động bình thường |

**Trạng thái:** ⬜ Pass / ⬜ Fail — Ghi chú: ___

---

## Tổng kết

| Section | Số test case | Pass | Fail | Blocked |
|---|---|---|---|---|
| 1. Tạo mới | 2 | | | |
| 2. Chỉnh sửa | 2 | | | |
| 3. Validation | 2 | | | |
| 4. Phòng & lịch | 2 | | | |
| 5. Trạng thái | 1 | | | |
| 6. Tìm kiếm & lọc | 1 | | | |
| 7. Lưu & lỗi | 2 | | | |
| 8. Thực đơn | 2 | | | |
| 9. Dịch vụ | 1 | | | |
| 10. Checklist | 1 | | | |
| 11. Timeline | 1 | | | |
| 12. Đặt cọc | 2 | | | |
| 13. Hợp đồng | 1 | | | |
| 14. Dashboard | 1 | | | |
| 15. Phân quyền | 1 | | | |
| 16. Stress test | 2 | | | |
| 17. Regression | 1 | | | |
| **Tổng** | **25** | | | |

---

*Checklist được tạo bởi Claude Code analysis ngày 2026-07-10*
