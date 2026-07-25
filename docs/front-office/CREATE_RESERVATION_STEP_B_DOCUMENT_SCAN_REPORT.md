# CREATE RESERVATION STEP B — DOCUMENT SCAN REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: STEP B COMPLETE — Guest selection + CCCD/Passport switching

---

## 1. BỐ CỤC

2 card giống Step A: Left (input 60%) + Right (preview 40%)

### Card trái

| Section | Fields |
|---------|--------|
| Chọn khách | ComboBox: "Khách 1 — Khách chính", "Khách 2 — Người lớn"... |
| Loại giấy tờ | ComboBox: CCCD / Hộ chiếu |
| Thao tác | Quét giấy tờ (disabled), Nạp ảnh (disabled), Tìm khách cũ, Xóa |
| CCCD fields | Họ tên, Giới tính, Ngày sinh, Quốc tịch, Số CCCD, Quê quán, Ngày cấp, Nơi cấp, Địa chỉ |
| Passport fields | Họ tên, Giới tính, Ngày sinh, Quốc tịch, Số hộ chiếu, Quốc gia cấp, Ngày cấp, Ngày hết hạn, Nơi cấp |

### Card phải

| Block | Content |
|-------|---------|
| Khách | Khách X — Vai trò |
| Giấy tờ | Loại + Số |
| Họ tên | Full name + DOB + Gender |
| Chi tiết | Ngày cấp, Nơi cấp, Địa chỉ |
| Status | Scan status badge |

---

## 2. ĐỒNG BỘ KHÁCH TỪ BƯỚC A

`SyncGuestsFromStepA()` reads `Modal.Workspace.Adults` + `Children` → creates `GuestDocument` objects. Called when clicking "Tiếp theo" từ Step A.

## 3. DỮ LIỆU TỪNG KHÁCH

`GuestDocument` class (13 properties): GuestIndex, Role, DocumentType, FullName, Gender, DateOfBirth, Nationality, BirthPlace, DocumentNumber, IssueDate, IssuePlace, ExpiryDate, PermanentAddress. Mỗi khách giữ dữ liệu riêng — chuyển qua lại không mất.

## 4. CCCD vs HỘ CHIẾU

Visibility toggle: `IsCCCD` / `IsPassport` bindings. CCCD hiện 9 fields, Passport hiện 9 fields khác (có Ngày hết hạn, Quốc gia cấp).

## 5. FILES MODIFIED

| File | Change |
|------|--------|
| `Views/ReservationCreateWindow.xaml` | Step B rewrite — guest selector, doc type, CCCD/Passport fields, preview |
| `Views/ReservationCreateWindow.xaml.cs` | +`GuestDocument` class, +`SyncGuestsFromStepA()`, +guest tracking |

## 6. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launched | YES — PID 45248, ~126MB |
| Backend unchanged | ✓ |
| Step A unchanged | ✓ |

---

**STEP B COMPLETE**
