# CREATE RESERVATION MODAL UI V2 REPORT

> **Ngày**: 2026-07-21
> **Kết luận**: 3-TAB REDESIGN COMPLETE

---

## 1. THIẾT KẾ 3 TAB

| Tab | Nội dung |
|-----|---------|
| **Khách hàng** | Thông tin cá nhân (tên, giới tính, ngày sinh, quốc tịch, CMND, loại khách), Thông tin liên hệ (ĐT, Email), Khách đi cùng (DataGrid + Thêm/Sửa/Xóa) |
| **Đặt phòng** | Thời gian lưu trú (check-in/out, số đêm), Phòng (loại phòng, số lượng, người lớn/trẻ em), Giá & Thanh toán, Nguồn khách, Dịch vụ, Ghi chú |
| **Thông tin bổ sung** | Flight (đến/đi + giờ), Accounting (công ty, đại lý), Contract (số HĐ, ngày ký), Internal (Complimentary, No Post, Confidential), Audit (người tạo, ngày tạo - readonly) |

---

## 2. TAB STYLE

Modern radio-button tabs (không dùng TabControl mặc định):
- Active: nền trắng, border xanh #2563EB, chữ xanh đậm
- Inactive: trong suốt, chữ xám #64748B

---

## 3. SECTION CARD STYLE

Mỗi section là một `Border`:
- Nền trắng, border #E2E8F0, corner 10px, padding 18px
- Title FontSize 14 SemiBold #102E5A
- Khoảng cách giữa các section: 14px

---

## 4. BINDING STATUS

| Trường | Bind |
|--------|------|
| Tên khách | `Workspace.GuestName` ✓ |
| Nguồn đặt phòng | `Workspace.Source` ✓ |
| Ngày nhận/trả phòng | `Workspace.ArrivalDate/DepartureDate` ✓ |
| Người lớn/trẻ em | `Workspace.Adults/Children` ✓ |
| Loại phòng | `Workspace.RoomTypes/SelectedRoomType` ✓ |
| Ghi chú | `Workspace.InternalNotes` ✓ |
| Số đêm | `Workspace.Nights` (computed) ✓ |
| Error/Info | `Workspace.ErrorMessage/InfoMessage` ✓ |
| Giới tính | Placeholder |
| Ngày sinh | Placeholder |
| Quốc tịch | Placeholder |
| CMND/Hộ chiếu | Placeholder |
| SĐT/Email | Placeholder |
| Khách đi cùng | Placeholder |
| Giá & Thanh toán | Placeholder (Financial Engine) |
| Flight/Accounting/Contract | Placeholder |

---

## 5. FOOTER

3 nút: **Hủy** | **Lưu** | **Lưu & Đóng**
Footer cố định, background trắng, border-top.

---

## 6. FILE MODIFIED

| File | Change |
|------|--------|
| `Views/ReservationCreateWindow.xaml` | Complete rewrite — 3 tabs + section cards + modern styling |

## 7. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launched | YES — PID 49436, ~121MB |
| Backend unchanged | ✓ |

---

**CREATE RESERVATION MODAL UI V2 — 3 TABS COMPLETE**
