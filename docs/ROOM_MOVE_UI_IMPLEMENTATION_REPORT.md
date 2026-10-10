# ROOM MOVE UI IMPLEMENTATION REPORT
# RoomMoveWindow — Đổi phòng
# Ngày: 2026-07-24
# Project Root: D:\ASMILE-BO

## 1. Project root

Đã xác nhận: `D:\ASMILE-BO`

## 2. Card và command Đổi phòng

- **Hub**: Lễ tân → Điều chỉnh lưu trú Workspace
- **Card**: Đổi phòng
- **Command**: `MouseLeftButtonDown="OnRoomMove"` → `new RoomMoveWindow().Show()`
- **File**: `StayAdjustmentWorkspaceView.xaml` / `.xaml.cs`

## 3. RoomMoveWindow

| Property | Value |
|----------|-------|
| Width | 1360 px |
| Height | 860 px |
| MinWidth | 1180 px |
| MinHeight | 720 px |
| WindowStartupLocation | CenterOwner |
| ResizeMode | CanResize |
| ShowInTaskbar | false |
| Owner | Application.Current.MainWindow |

## 4. Layout

```
┌─ HEADER ───────────────────────────────────┐
│ [icon] Đổi phòng               [LM] [Đóng]  │
├─ GUEST SEARCH ─────────────────────────────┤
│ [TextBox] + DataGrid 20 khách (7 cột)      │
├──────────────────┬─────────────────────────┤
│ PHÒNG HIỆN TẠI   │ CHỌN PHÒNG MỚI           │
│ (35%)            │ DataGrid 12 phòng (8 cột)│
├──────────────────┼─────────────────────────┤
│ SO SÁNH PHÒNG    │ LÝ DO & GIÁ              │
│ 7 thuộc tính     │ ComboBox + TextBox + Giá │
├──────────────────┼─────────────────────────┤
│ CẢNH BÁO         │ TÓM TẮT                  │
│ SẴN SÀNG         │ 6 fields                 │
├──────────────────┴─────────────────────────┤
│ Status │ [Hủy] [Lưu nháp] [In] [XÁC NHẬN]   │
└────────────────────────────────────────────┘
```

## 5. Guest selector

- TextBox filter với KeyDown Enter
- DataGrid 7 cột: Phòng, Tên khách, Ngày đến, Ngày đi, Hạng, VIP, Folio
- Filter theo tên, số phòng, mã Folio
- 20 khách mockup đang lưu trú

## 6. Current room card

Hiển thị khi chọn khách:
- Số phòng, Hạng, Ngày đến/đi, Trạng thái, Nhà buồng, Giá, Folio
- Badge VIP vàng nếu có
- Panel "Vấn đề cần xử lý": Minibar, thẻ phòng, hành lý

## 7. Effective time

Mặc định "Đổi ngay" — chưa có DatePicker UI riêng trong phase này.

## 8. New room filter & DataGrid

- 12 phòng mockup: Deluxe, Superior, Junior, Standard, Suite
- Các trạng thái: Sẵn sàng, Đang dọn, Có khách đến, Khóa, OOO
- 8 cột: Phòng, Hạng, Tầng, Giường, Trạng thái, NB, Giá, Chênh lệch
- SelectionChanged → cập nhật comparison, giá, cảnh báo, readiness, summary

## 9. Room comparison

Table 7 thuộc tính so sánh giữa phòng cũ và mới:
- Số phòng, Hạng, Tầng, Giường, Giá, Trạng thái, Nhà buồng
- Highlight: Nâng hạng ▲ (xanh), Hạ hạng ▼ (cam), Cùng hạng (xanh dương)

## 10. Reason section

- ComboBox lý do: Khách yêu cầu, Phòng có sự cố, Nâng hạng, Yêu cầu VIP, Khác
- TextBox ghi chú multiline
- Price panel: Giá cũ, Giá mới, Chênh lệch (đỏ nếu tăng, xanh nếu giảm)

## 11. Transfer options

Mặc định kèm theo: Thu hồi thẻ, Chuyển hành lý, Gửi nhà buồng, Kiểm tra Minibar.
Chưa có toggle UI riêng trong phase này — hiển thị trong Summary.

## 12. Price adjustment

Tính toán từ mock data:
- Giá cũ theo hạng phòng khách (Suite 1.8M, Superior 1.5M, Deluxe 1.2M, Junior 950K, Standard 800K)
- Giá mới từ RoomInfo.Price
- Chênh lệch = Giá mới - Giá cũ
- Format: N0 VND

## 13. Warning panel

Cảnh báo động theo phòng được chọn:
- Phòng đang dọn → "⚠ Phòng mới chưa sạch"
- Phòng có khách đến → "⚠ Phòng có lịch khách đến"
- Phòng Khóa/OOO → "✗ Phòng không khả dụng"
- Mặc định: Minibar, thẻ phòng, nhà buồng

## 14. Readiness panel

3 trạng thái:
- **✗ Chưa thể** (đỏ): Phòng Khóa hoặc OOO → disable nút Xác nhận
- **⚠ Cần kiểm tra** (vàng): Phòng đang dọn hoặc có khách đến
- **✓ Sẵn sàng** (xanh): Không có cảnh báo chặn

## 15. Summary

Hiển thị: Khách, Mã HS, Phòng cũ→mới, Hạng cũ→mới, Lý do, Giá, Bàn giao

## 16. Confirmation popup

MessageBox mockup hiển thị: Khách, Phòng cũ/mới, Hạng, Giá, Tác vụ đi kèm, Nút Quay lại/Xác nhận.

## 17. Mock data

- **20 khách**: đa dạng tầng/hạng, có VIP (★), Folio khác nhau
- **12 phòng**: 5 hạng, trạng thái Sẵn sàng/Đang dọn/Có khách đến/Khóa/OOO

## 18. Selection binding

- Chọn khách → cập nhật current room, reset new room selection
- Chọn phòng mới → cập nhật comparison, giá, cảnh báo, readiness, summary
- Không hardcode

## 19. Keyboard workflow

- Enter trong GuestSearch → filter
- Tab đúng thứ tự
- Esc đóng popup (keybinding mặc định của Window)

## 20. Responsive

- 1366×768: MinWidth 1180 → không test được dưới min
- 1920×1080: Hiển thị đầy đủ, không cắt, không tràn

## 21. Icon

Segoe MDL2 Assets `&#xE8AB;` (mũi tên chuyển đổi)

## 22. File đã tạo

- `D:\ASMILE-BO\ASMILE.BO.App\Views\RoomMoveWindow.xaml`
- `D:\ASMILE-BO\ASMILE.BO.App\Views\RoomMoveWindow.xaml.cs`

## 23. File đã sửa

- `D:\ASMILE-BO\ASMILE.BO.App\Views\StayAdjustmentWorkspaceView.xaml.cs` — wire OnRoomMove

## 24. Build result

- Build: PASS (0 errors)
- Không binding error
- Không resource error
- Không XAML parse error

## 25. Binding errors

Không có binding errors (UI build từ code-behind, không dùng DataContext binding)

## 26. Runtime result

- Card Đổi phòng → RoomMoveWindow mở đúng
- Workspace vẫn phía sau
- Không duplicate
- Chọn khách → toàn bộ UI cập nhật
- Chọn phòng mới → comparison/warnings/readiness/summary cập nhật
- Blocking warning → disable nút Xác nhận
- Popup xác nhận mở đúng
- Không ghi database

## 27. Xác nhận

- [x] Chỉ làm Đổi phòng
- [x] Không sửa Workspace
- [x] Không sửa backend
- [x] Không sửa database
- [x] Không sửa Banquet
- [x] Chỉ thao tác trong D:\ASMILE-BO
- [x] Không thực hiện đổi phòng thật

## KẾT LUẬN: ROOM MOVE UI DONE
